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
using System.Diagnostics;

namespace D2S.Library.Pipelines
{
    public class ScalingSequentialPipeline : SequentialPipeline
    {
        #region Constructor
        internal ScalingSequentialPipeline(PipelineContext context)
        {
            m_Pause = new ManualResetEvent(true);
            m_LatestPauseState = false;
            m_DummyProgress = new Progress<int>();
            m_ActualProgress = new Progress<int>();
            m_PauseSyncRoot = new object();
            m_Context = context;
            m_LineBuffers = new List<BoundedConcurrentQueu<string>>();
            m_RecordBuffers = new List<BoundedConcurrentQueu<object[]>>();
            m_RowBuffers = new List<BoundedConcurrentQueu<Row>>();
            if (context.SourceFilePath.Contains(".xls"))
            {
                m_ExcelReader = new ExcelDataExtractor();
            }
            else
            {
                m_Reader = new DIALFlatFileExtractor();
                m_StringSplitter = new StringSplitter(context.Qualifier) { Delimiter = context.Delimiter };
            }
            m_RowBuilder = new RecordToRowTransformer(context.ColumnNames, context.IsSkippingError);
            m_Loader = new SQLTableLoader();
            //register event
            
            m_ActualProgress.ProgressChanged += OnReaderEvent;
        }
        #endregion

        #region PrivateFields
        private readonly ManualResetEvent m_Pause;
        private readonly Progress<int> m_DummyProgress;
        private readonly Progress<int> m_ActualProgress;
        private readonly object m_PauseSyncRoot;
        private bool m_LatestPauseState;
        private int[] m_TaskDistribution;
        #endregion

        #region Interface
        public override bool IsPaused { get => m_IsPaused(); }
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
        public override event EventHandler<int> LinesReadFromFile;
        public override async Task StartAsync()
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
            // indicing is based on the m_distribution object, where the startindex, length combo is equal to sum(m_distribution[0...i-1]),i
            if (m_StringSplitter != null)
            {
                int rankOrderOfTask = 1;               
                await UnwindStringSplitter(taskList, getStartIndex(rankOrderOfTask), m_TaskDistribution[rankOrderOfTask], m_LineBuffers);
                rankOrderOfTask++;
                await UnwindRowBuilder(taskList, getStartIndex(rankOrderOfTask), m_TaskDistribution[rankOrderOfTask], m_RecordBuffers);
                rankOrderOfTask++;
                await UnwindSqlLoader(taskList, getStartIndex(rankOrderOfTask), m_TaskDistribution[rankOrderOfTask], m_RowBuffers);
            }
            else
            {
                int rankOrderOfTask = 1;
                await UnwindRowBuilder(taskList, getStartIndex(rankOrderOfTask), m_TaskDistribution[rankOrderOfTask], m_RecordBuffers);
                rankOrderOfTask++;
                await UnwindSqlLoader(taskList, getStartIndex(rankOrderOfTask), m_TaskDistribution[rankOrderOfTask], m_RowBuffers);
            }
        }





        #endregion

        #region PrivateMethods
        private void OnReaderEvent(object sender, int e)
        {
            LinesReadFromFile?.Invoke(sender, e);
        }
        private bool m_IsPaused()
        {
            lock (m_PauseSyncRoot)
            {
                return m_LatestPauseState;
            }
        }
        private List<Task> InitializeTasks()
        {
            List<Task> taskList = new List<Task>();
            //determine discrete amount of tasks, excel requires 3, flatfiles require 4
            int numSeq = (m_Reader != null ? 4 : 3);
            int cpu = m_Context.CpuCountUsedToComputeParallalism;
            m_TaskDistribution = ScalingDistribution.GetScalingDistribution(numSeq, cpu).TaskDistribution;
            //we now have a nice int array telling us how many tasks we create per step. Too bad we still gotta create different paths for excel vs flatfile. oh well
            //also since we base the amount of collections on the amount of tasks and we round robin link tasks to their collection, we will have massive issues
            //if a subsequent step in the process has fewer tasks than the previous, since not all queus will be emptied and we will lose data. (see further comments also)
            //we therefore check that each item in this distribution is higher or equal to the previous
            for (int i = 1 ; i < m_TaskDistribution.Length; i++)
            {
                if (m_TaskDistribution[i] < m_TaskDistribution[i-1])
                {
                    throw new InvalidOperationException("Error while initializing task list for ScalingSequantialPipeline. Task distribution is not valid and would result in dataloss. Aborting.");
                }
            }
            
            if (m_Reader != null) //flatfile path
            {
                //init collections

                InitCollectionsFlatfile();

                //maek some tasks
                FillTaskListForFlatFile(taskList);
            }
            else //excel path
            {
                //init collections
                InitCollectionsExcelfile();
                //maek some tasks
                FillTaskListForExcelFile(taskList);

            }
            return taskList;
        }

        private void FillTaskListForFlatFile(List<Task> taskList)
        {
            //there is always one single read task            
            var readTask = m_Reader.GetPausableReportingWorkItem();
            taskList.Add(
                new Task(
                    () => readTask(
                        m_Context, m_LineBuffers[0], m_Pause, m_ActualProgress)));
            //there may be multiple split tasks, each linked to m linebuffer and to their own record buffer. we can use simple indexing here.
            var splitTask = m_StringSplitter.GetReportingPausableWorkItem();
            for (int i = 0; i < m_TaskDistribution[1]; i++)
            {
                //the following line makes another local copy of i, which somehow prevents a bug (fkn compiler >:( )
                int ind = i;
                taskList.Add(
                    new Task(
                        () => splitTask(
                            m_LineBuffers[0], m_RecordBuffers[ind], m_Pause, m_DummyProgress)));
            }
            //this is where the fun starts, we add an amount of row tasks, each with their own output buffer.
            //for the input buffers, we utilize a queu to build a simple round-robin algorithm, selecting alternating buffers from the recordbuffer collection

            //Notice for readers: This is why we verify that each step has the same number or more tasks than the previous, otherwise the round robin would not cover
            //all collections and some of them would never be emptied. This would cause either an infinite loopstate or dataloss. not fun.

            var rowTask = m_RowBuilder.GetReportingPausableWorkItem();
            Queue<BoundedConcurrentQueu<object[]>> roundRobinQueuRowTask = new Queue<BoundedConcurrentQueu<object[]>>(m_RecordBuffers);
            for (int i = 0; i < m_TaskDistribution[2]; i++)
            {
                var currentVictim = roundRobinQueuRowTask.Dequeue();
                int ind = i;
                taskList.Add(
                    new Task(
                        () => rowTask(
                            currentVictim, m_RowBuffers[ind], m_Pause, m_DummyProgress)));
                //put the victim back in the queu
                roundRobinQueuRowTask.Enqueue(currentVictim);
            }
            //we take a similar approach for the sql loaders, round robin linking them to the rowbuffers
            var sqlTask = m_Loader.GetPausableReportingWorkItem();
            Queue<BoundedConcurrentQueu<Row>> roundRobinQueuSqlTask = new Queue<BoundedConcurrentQueu<Row>>(m_RowBuffers);
            for (int i = 0; i < m_TaskDistribution[3]; i++)
            {
                var currentVictim = roundRobinQueuSqlTask.Dequeue();
                taskList.Add(
                    new Task(
                        () => sqlTask(
                            m_Context, currentVictim, m_Pause, m_DummyProgress)));
                //put ze victim back
                roundRobinQueuSqlTask.Enqueue(currentVictim);
            }
        }

        private void InitCollectionsFlatfile()
        {
            //total number of collections is the sum of the taskdistribution array, minus the last item (load tasks use their own internal buffer)
            int totalNumberOfCollections = m_TaskDistribution.Sum() - m_TaskDistribution.Last();
            int buffersizePerCollection = m_Context.TotalObjectsInSequentialPipe / totalNumberOfCollections;
            //always one reader collection
            m_LineBuffers.Add(new BoundedConcurrentQueu<string>(buffersizePerCollection));
            //add one collection per splitter
            for (int i = 0; i < m_TaskDistribution[1]; i++)
            {
                m_RecordBuffers.Add(new BoundedConcurrentQueu<object[]>(buffersizePerCollection));
            }
            //one collection per rowbuilder
            for (int i = 0; i < m_TaskDistribution[2]; i++)
            {
                m_RowBuffers.Add(new BoundedConcurrentQueu<Row>(buffersizePerCollection));
            }
        }

        private void InitCollectionsExcelfile()
        {
            //total number of collections is the sum of the taskdistribution array, minus the last item (load tasks use their own internal buffer)
            int totalNumberOfCollections = m_TaskDistribution.Sum() - m_TaskDistribution.Last();
            int buffersizePerCollection = m_Context.TotalObjectsInSequentialPipe / totalNumberOfCollections;
            //always one reader collection
            m_RecordBuffers.Add(new BoundedConcurrentQueu<object[]>(buffersizePerCollection));

            //add one collection per rowbuilder
            for (int i = 0; i < m_TaskDistribution[1]; i++)
            {
                m_RowBuffers.Add(new BoundedConcurrentQueu<Row>(buffersizePerCollection));
            }
        }

        private void FillTaskListForExcelFile(List<Task> taskList)
        {
            //this function is a bit of copypasta from the one for flatfiles, read that one for better commenting.
            //there is always one single read task
            var readTask = m_ExcelReader.GetPausableReportingWorkItem();
            taskList.Add(
                new Task(
                    () => readTask(
                        m_Context, m_RecordBuffers[0], m_Pause, m_ActualProgress)));

            var rowTask = m_RowBuilder.GetReportingPausableWorkItem();
            for (int i = 0; i < m_TaskDistribution[1]; i++)
            {
                int ind = i;
                taskList.Add(
                    new Task(
                        () => rowTask(
                            m_RecordBuffers[0], m_RowBuffers[ind], m_Pause, m_DummyProgress)));
                
            }
            var sqlTask = m_Loader.GetPausableReportingWorkItem();
            Queue<BoundedConcurrentQueu<Row>> roundRobinQueuSqlTask = new Queue<BoundedConcurrentQueu<Row>>(m_RowBuffers);
            for (int i = 0; i < m_TaskDistribution[2]; i++)
            {
                var currentVictim = roundRobinQueuSqlTask.Dequeue();
                taskList.Add(
                    new Task(
                        () => sqlTask(
                            m_Context, currentVictim, m_Pause, m_DummyProgress)));
                //put ze victim back
                roundRobinQueuSqlTask.Enqueue(currentVictim);
            }
        }

        private int getStartIndex(int rankOrderOfTask)
        {
            int startIndex = 0;
            for (int i = 0; i < rankOrderOfTask; i++)
            {
                startIndex += m_TaskDistribution[i];
            }
            return startIndex;
        }

        #endregion


    }
}
