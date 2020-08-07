using D2S.Library.Extractors;
using D2S.Library.Transformers;
using D2S.Library.Loaders;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace D2S.Library.Pipelines
{
    public class BasicSequentialPipeline : SequentialPipeline
    {
        #region constructor
        internal BasicSequentialPipeline(PipelineContext context)
        {
            m_Context = context;
            //decide which reader to use
            if (context.IsReadingFromDataLake)
            {
                m_Reader = new DataLakeFlatFileExtractor(context.DataLakeAdress);
            }
            else if (context.SourceFilePath.Contains(".xls"))
            {
                m_ExcelReader = new ExcelDataExtractor();
            }
            else 
            {
                m_Reader = new DIALFlatFileExtractor();
            }
            m_StringSplitter = new StringSplitter(context.Qualifier) { Delimiter = context.Delimiter};
            m_RowBuilder = new RecordToRowTransformer(context.ColumnNames, context.IsSkippingError);
            m_Loader = new SQLTableLoader();
            m_LineBuffers = new List<BoundedConcurrentQueu<string>> { new BoundedConcurrentQueu<string>(context.TotalObjectsInSequentialPipe / NumberOfBuffers) };
            m_RecordBuffers = new List<BoundedConcurrentQueu<object[]>> { new BoundedConcurrentQueu<object[]>(context.TotalObjectsInSequentialPipe / NumberOfBuffers) } ;
            m_RowBuffers = new List<BoundedConcurrentQueu<Row>> { new BoundedConcurrentQueu<Row>(context.TotalObjectsInSequentialPipe / NumberOfBuffers) };
            m_Pause = new ManualResetEvent(true);
            m_LatestPauseState = false;
            m_DummyProgress = new Progress<int>();
            m_ActualProgress = new Progress<int>();
            m_PauseSyncRoot = new object();

            //register event
            m_ActualProgress.ProgressChanged += OnReaderEvent;
        }

        #endregion

        #region PrivateFields
        private const int NumberOfBuffers = 3;              
        private readonly ManualResetEvent m_Pause;
        private readonly Progress<int> m_DummyProgress;
        private readonly Progress<int> m_ActualProgress;
        private readonly object m_PauseSyncRoot;
        private bool m_LatestPauseState;
        #endregion

        #region PublicFields

        #endregion

        #region Interface
        public override event EventHandler<int> LinesReadFromFile;

        public async override Task StartAsync()
        {
            //create, truncate, drop tables if specified
            if (m_Context.IsDroppingTable)
            {
                DropTable(m_Context);
            }
            if (m_Context.IsCreatingTable)
            {
                CreateTable(m_Context);
            }
            if (m_Context.IsTruncatingTable)
            {
                TruncateTable(m_Context);
            }
            List<Task> taskList = InitializeTasks();

            taskList.ForEach(task => task.Start());

            //we start this task from the factory and give it priority via the longrunning flag (this typically causes the threadpool to assign a dedicated thread to this task)
           //we do this because this task must not be scheduled behind other tasks as it could stick the pipeline in an infinite loop.
           //reason for this loop is that the pipeline does not know when its finished, it relies on the monitiroing task.
            await Task.Factory.StartNew(
                () => MonitorTasksWhileReading(taskList)
                , TaskCreationOptions.LongRunning);

            //unwind the pipeline, completing the steps as we go
            if (m_StringSplitter != null)
            {
                await UnwindStringSplitter(taskList, 1, 1, m_LineBuffers);
                await UnwindRowBuilder(taskList, 2, 1, m_RecordBuffers);
                await UnwindSqlLoader(taskList, 3, 2, m_RowBuffers);
            }
            else
            {
                await UnwindRowBuilder(taskList,1,1, m_RecordBuffers);
                await UnwindSqlLoader(taskList,2,2, m_RowBuffers);
            }
        }

        public override bool IsPaused { get => m_IsPaused();}
        public override bool TogglePause()
        {
            lock (m_PauseSyncRoot)
            {
                //set the manualreset event if the pipe was paused, allowing it to continue
                if (m_LatestPauseState)
                {
                    m_Pause.Set();
                }
                //else if it was running, reset the manualresetevent and block the threads
                else
                {
                    m_Pause.Reset();
                }
                //in any case toggle the latestpausestate flag
                m_LatestPauseState ^= true;
            }
            return true;
        }
        #endregion

        #region PrivateMethods
        private List<Task> InitializeTasks()
        {
            List<Task> tasks = new List<Task>();

            if (m_Reader != null)
            {
                var readTask = m_Reader.GetPausableReportingWorkItem();
                tasks.Add(
                    new Task(
                        () => readTask(m_Context, m_LineBuffers[0], m_Pause, m_ActualProgress)));
                var splitTask = m_StringSplitter.GetReportingPausableWorkItem();
                tasks.Add(
                    new Task(
                        () => splitTask(m_LineBuffers[0], m_RecordBuffers[0], m_Pause, m_DummyProgress)));
            }
            else
            {
                var readTask = m_ExcelReader.GetPausableReportingWorkItem();
                tasks.Add(
                    new Task(
                        () => readTask(m_Context, m_RecordBuffers[0], m_Pause, m_ActualProgress)));
            }
            var rowTask = m_RowBuilder.GetReportingPausableWorkItem();
            tasks.Add(
                new Task(
                    () => rowTask(m_RecordBuffers[0], m_RowBuffers[0], m_Pause, m_DummyProgress)));
            var writeTask = m_Loader.GetPausableReportingWorkItem();
            //use two sql tasks cuz its a bottleneck kinda
            for (int i =0; i<2; i++)
            {
                tasks.Add(
                    new Task(
                        () => writeTask(m_Context, m_RowBuffers[0], m_Pause, m_DummyProgress)));
            }
            return tasks;
        }

        private bool m_IsPaused()
        {
            lock (m_PauseSyncRoot)
            {
                return m_LatestPauseState;
            }
        }

        private void OnReaderEvent(object sender, int e)
        {
            LinesReadFromFile?.Invoke(sender, e);
        }

        #endregion
    }
}
