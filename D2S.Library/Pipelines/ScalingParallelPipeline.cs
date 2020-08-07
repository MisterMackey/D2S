using System;
using D2S.Library.Transformers;
using D2S.Library.Loaders;
using D2S.Library.Extractors;
using D2S.Library.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Helpers;
using D2S.Library.Services;
using System.Threading;
using System.Text.RegularExpressions;

namespace D2S.Library.Pipelines
{
    public class ScalingParallelPipeline : Pipeline
    {
        #region PrivateFields
        private readonly PipelineContext m_Context;
        private readonly int numberOfLines;
        #endregion
        #region constructor
        internal ScalingParallelPipeline(PipelineContext context) :this(context, 1000)
        {

        }
        internal ScalingParallelPipeline(PipelineContext context, int NotifyBy)
        {
            m_Context = context;
            numberOfLines = NotifyBy;
        }
        #endregion
        #region PublicMethods
        public override bool IsPaused { get  { return false; } }

        public override event EventHandler<int> LinesReadFromFile;

        public override async Task StartAsync()
        {
            //create, truncate, drop tables if specified
            HandeTableOptions();

            List<Task> Tasklist = new List<Task>();

            if (Regex.IsMatch(m_Context.SourceFilePath, @"\.xls"))
            {
                ConcurrentExcelReader reader = new ConcurrentExcelReader(m_Context);
                for (int i = 0; i < m_Context.CpuCountUsedToComputeParallalism; i++)
                {
                    Tasklist.Add(
                        Task.Factory.StartNew(
                            x => ProcessRecordsExcel(x), reader));
                }
            }
            else
            {
                ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(m_Context);
                for (int i = 0; i < m_Context.CpuCountUsedToComputeParallalism; i++)
                {
                    Tasklist.Add(
                        Task.Factory.StartNew(
                            x => ProcessRecords(x), reader));
                }
            }
            await Task.WhenAll(Tasklist);
        }



        public override bool TogglePause()
        {
            return false;
        }
        #endregion
        #region Private Methods
        private void OnRecordsProcessed(object sender)
        {
            LinesReadFromFile?.Invoke(sender, numberOfLines);
        }
        private void OnRecordsProcessed(object sender, int numlines)
        {
            LinesReadFromFile?.Invoke(sender, numlines);
        }

        private void HandeTableOptions()
        {
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
        }

        private void ProcessRecords(object x)
        {
            ConcurrentFlatFileExtractor reader = x as ConcurrentFlatFileExtractor;
            SimpleSqlTableLoader writer = new SimpleSqlTableLoader(m_Context);
            string line;
            int rowsProcessed = 0;
            int numColumns = m_Context.ColumnNames.Count();
            //if a selection is made on the source columns we will compute the ordinal rankings we require here
            int[] ordinalRankings = null;
            //if these are not equal a selection is made.
            if (numColumns != m_Context.ColumnNamesSelection.Count())
            {
                ordinalRankings = new int[m_Context.ColumnNamesSelection.Count()];
                int indexRankings = 0;
                //for every name in the total list, check if it is present in the selection and if so write its ordinal ranking to the array.
                //the rankings will be sorted low to high by design which also suits the simplesqlWriter in case it is in ordinal mode.
                for (int i = 0; i < numColumns; i++)
                {
                    if (m_Context.ColumnNamesSelection.Any(
                        selectedName => selectedName.Equals(m_Context.ColumnNames[i], StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ordinalRankings[indexRankings++] = i;
                    }
                }
            }
            while (reader.TryExtractLine(out line))
            {
                string[] record = StringAndText.SplitRow(line, m_Context.Delimiter, m_Context.Qualifier, true);
                //assume the orindal rankings are identical (if all the pieces use the context.columnsnames property that will be the case
                //check the column count tho
                if (record.Count() != numColumns)
                {
                    var errorMsg = $"A row was skipped over because it had too many or too few columns, expected: {numColumns}, actual: {record.Count()}";
                    if (m_Context.IsSkippingError)
                    {
                        LogService.Instance.Warn(errorMsg);
                    }
                    else
                    {
                        Exception ex = new Exception(errorMsg);
                        LogService.Instance.Error(ex);
                        throw ex;
                    }
                }
                else
                {
                    var newRow = writer.GetEmptyRow();
                    //write all columns
                    if (ordinalRankings == null)
                    {
                        for (int i = 0; i < numColumns; i++)
                        {
                            newRow[i] = record[i];
                        } 
                    }
                    //else write only selected columns (the indices we want are in the ordinalrankings array)
                    else
                    {
                        for (int i = 0; i < ordinalRankings.Count(); i++)
                        {
                            newRow[i] = record[ordinalRankings[i]];
                        }
                    }
                    writer.PostRecord(newRow);
                    if (++rowsProcessed % numberOfLines == 0)
                    {
                        OnRecordsProcessed(Thread.CurrentThread.Name);
                    }
                }
            }
            //flush final records and trigger last event
            writer.WriteRecords();
            OnRecordsProcessed(Thread.CurrentThread.Name, rowsProcessed % numberOfLines);
        }


        private void ProcessRecordsExcel(object x)
        {
            ConcurrentExcelReader reader = x as ConcurrentExcelReader;
            SimpleSqlTableLoader writer = new SimpleSqlTableLoader(m_Context);
            string[] line;
            int rowsProcessed = 0;
            int numColumns = m_Context.ColumnNames.Count();
            while (reader.TryExtractRecord(out line))
            {
                
                    var newRow = writer.GetEmptyRow();
                    for (int i = 0; i < numColumns; i++)
                    {
                        newRow[i] = line[i];
                    }
                    writer.PostRecord(newRow);
                    if (++rowsProcessed % numberOfLines == 0)
                    {
                        OnRecordsProcessed(Thread.CurrentThread.Name);
                    }
            }
            //flush final records and trigger last event
            writer.WriteRecords();
            OnRecordsProcessed(Thread.CurrentThread.Name, rowsProcessed % numberOfLines);
        }
        #endregion
    }
}
