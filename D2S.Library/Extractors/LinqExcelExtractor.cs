using System;
using System.Collections.Concurrent;
using LinqToExcel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using Row = D2S.Library.Utilities.Row;
using ExcelRow = LinqToExcel.Row;

namespace D2S.Library.Extractors
{
    public class LinqExcelExtractor : Extractor<Row, int>
    {
        protected override Action<PipelineContext, IProducerConsumerCollection<Row>, ManualResetEvent> PausableWorkItem => throw new NotImplementedException();

        protected override Action<PipelineContext, IProducerConsumerCollection<Row>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReportingWork;

        private void ReportingWork(PipelineContext context, IProducerConsumerCollection<Row> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            string pathToExcel = context.SourceFilePath;
            string workSheetName = context.ExcelWorksheetName;

            var excelFile = new ExcelQueryFactory(pathToExcel);
            var dataRows =
                from r in excelFile.Worksheet<Row>(workSheetName)
                select r;
            int progressCounter = 0;

            foreach (Row row in dataRows)
            {
                pauseEvent.WaitOne();
                output.TryAdd(row);
                if (++progressCounter % 1000 == 0)
                {
                    progress.Report(progressCounter);
                }
            }
        }
    }
}
