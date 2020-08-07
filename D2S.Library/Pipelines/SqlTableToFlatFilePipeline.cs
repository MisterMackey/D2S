using D2S.Library.Extractors;
using D2S.Library.Loaders;
using D2S.Library.Services;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace D2S.Library.Pipelines
{
    public class SqlTableToFlatFilePipeline : Pipeline
    {
        #region Fields
        private readonly PipelineContext m_Context;
        #endregion

        #region Constr
        public SqlTableToFlatFilePipeline(PipelineContext context )
        {
            m_Context = context;
            //make sure the target is a csv or txt file and the source is set
            ConfirmSourceAndTargetAreSetCorrectly(m_Context);
        }

        #endregion

        #region Interface

        public override bool IsPaused => throw new NotImplementedException();

        public override event EventHandler<int> LinesReadFromFile;

        public override async Task StartAsync()
        {
            //handle some pre-start conditions            
            OutputToConsoleAndLog("Pipeline is starting");
            //create shared objects
            ConcurrentSqlExtractor reader = new ConcurrentSqlExtractor(m_Context);
            ConcurrentFlatFileWriter writer = new ConcurrentFlatFileWriter(m_Context);
            Tuple<ConcurrentSqlExtractor, ConcurrentFlatFileWriter, string> state = new Tuple<ConcurrentSqlExtractor, ConcurrentFlatFileWriter,string>(reader, writer, m_Context.Delimiter);
            int NumTasks = m_Context.CpuCountUsedToComputeParallalism;
            Task[] tasks = new Task[NumTasks];
            OutputToConsoleAndLog($"Creating {NumTasks} tasks");
            //create tasks, to future editors, ensure that the state object is the correct type or create a new processrecords method
            for (int i = 0; i < NumTasks; i++)
            {
                tasks[i] = new Task(new Action<object>(ProcessRecords), state);
            }
            //write header line if needed
            if (m_Context.FirstLineContainsHeaders)
            {
                WriteHeaderLine(reader, writer);
            }
            OutputToConsoleAndLog("Starting tasks");
            //start tasks
            for (int i = 0; i < NumTasks; i++)
            {
                tasks[i].Start();
            }
            OutputToConsoleAndLog("Awaiting tasks");
            //await the completion of asynchronous tasks
            await Task.WhenAll(tasks);
            writer.Close();
            OutputToConsoleAndLog($"{ NumTasks} have finished completing. Pipeline finished");
        }


        public override bool TogglePause()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private methods

        private void ConfirmSourceAndTargetAreSetCorrectly(PipelineContext m_Context)
        {
            var source = m_Context.SourceTableName;
            var dest = m_Context.DestinationFilePath;
            bool sourceCorrect = false;
            bool destCorrect = false;
            //confirm that source follows the format [schema].[table] or contains no spaces
            if (Regex.IsMatch(source, @"^\[\w+\]\.\[\w+\]$")) // checks for [schemaname].[tablename]
            {
                sourceCorrect = true;
            }
            else if (Regex.IsMatch(source, @"^\w+\.\w+$")) //check for schemaname.tablename
            {
                sourceCorrect = true;
            }
            // check that destination is a csv or txt file and that the path either exists or can be created (writer will create the file itself)
            if (Regex.IsMatch(dest, @"\.txt$|\.csv$"))
            {
                //filename correct, now check patch
                int pathInd = Regex.Match(dest, @"\\[^\\]+$").Index;
                string path = dest.Substring(0, pathInd);
                if (Directory.Exists(path))
                {
                    destCorrect = true;
                }
                else
                {
                    Directory.CreateDirectory(path);
                    destCorrect = true;
                }
            }
            if (sourceCorrect && destCorrect)
            {
                OutputToConsoleAndLog($"Source and Destination format verified, source: {source} , destination: {dest}.");
            }
            else
            {
                string msg = $"Failure to verify source and or destination. verification status source:{sourceCorrect}, verification status destination:{destCorrect}. source: {source} , destination: {dest}.";
                OutputErrorToConsoleAndLog(msg);
                throw new ArgumentException(msg);
            }
        }
        

        private void ProcessRecords(object state)
        {
            //cast state object
            var trueState = state as Tuple<ConcurrentSqlExtractor, ConcurrentFlatFileWriter, string>;
            var reader = trueState.Item1;
            var writer = trueState.Item2;
            var delim = trueState.Item3;
            //make stringbuilder
            StringBuilder builder = new StringBuilder();

            OutputToConsoleAndLog($"Thread {Thread.CurrentThread.Name} is starting execution");
            object[] SourceData;             
            int progress = 0;
            while (reader.TryExtractRecord(out SourceData))
            {
                builder.Clear();
                int objCount = SourceData.Count();
                //append items including delimiter
                for (int i = 0; i < objCount-1; i++)
                {
                    builder.Append(SourceData[i].ToString());
                    builder.Append(delim);
                }
                //append final item w/o delimiter
                builder.Append(SourceData[objCount-1].ToString());
                writer.WriteLine(builder.ToString());
                if (++progress % 1000 == 0)
                {
                    OnLinesWritten(Thread.CurrentThread.Name, 1000);
                }
            }
            OnLinesWritten(Thread.CurrentThread.Name, 1000);
        }

        private void OutputToConsoleAndLog(string msg)
        {
            Console.WriteLine(msg);
            LogService.Instance.Info(msg);
        }

        private void OnLinesWritten(object sender, int num)
        {
            LinesReadFromFile?.Invoke(sender, num);
        }


        private void WriteHeaderLine(ConcurrentSqlExtractor reader, ConcurrentFlatFileWriter writer)
        {
            var HeaderLineObject = reader.GetDataTable();
            StringBuilder builder = new StringBuilder();
            int count = HeaderLineObject.Rows.Count;
            string delim = m_Context.Delimiter;
            for (int i = 0; i < count-1; i++)
            {
                builder.Append(HeaderLineObject.Rows[i]["ColumnName"]);
                builder.Append(delim);
            }
            builder.Append(HeaderLineObject.Rows[count-1]["ColumnName"]);
            writer.WriteLine(builder.ToString());
        }

        private void OutputErrorToConsoleAndLog(string msg)
        {
            Console.WriteLine(msg);
            LogService.Instance.Error(msg);
        }
        #endregion
    }
}
