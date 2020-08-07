using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using D2S.Library.Utilities;
using ExcelDataReader;
using System.Collections.Concurrent;
namespace D2S.Library.Extractors
{
    public class ExcelDataExtractor : Extractor<object[], int>
    {
        protected override Action<PipelineContext, IProducerConsumerCollection<object[]>, ManualResetEvent> PausableWorkItem => throw new NotImplementedException();
        /// <summary>
        /// pausing not actaully supported.
        /// </summary>
        protected override Action<PipelineContext, IProducerConsumerCollection<object[]>, ManualResetEvent, IProgress<int>> ReportingWorkItem => DoPausableReportableWork;

        private void DoPausableReportableWork(PipelineContext context, IProducerConsumerCollection<object[]> output, ManualResetEvent pause, IProgress<int> progress)
        {
            string filepath = context.SourceFilePath;
            int progressCounter = 0;
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    if (context.FirstLineContainsHeaders) { reader.Read(); }
                    while (reader.Read())
                    {
                        object[] currentRow = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            currentRow[i] = reader.GetValue(i); 
                        }
                        output.TryAdd(currentRow);
                        if (++progressCounter % 1000 == 0)
                        {
                            progress.Report(progressCounter);
                        }
                    }
                    progress.Report(progressCounter);
                }
            }
        }
    }
}
