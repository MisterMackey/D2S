using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Extractors
{
    public class LinqFileExtractor : Extractor<string, int>
    {
        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> PausableWorkItem => throw new NotImplementedException();

        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReportingWork;

        private void ReportingWork(PipelineContext context, IProducerConsumerCollection<string> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            var lines =
                from line in File.ReadLines(context.SourceFilePath)
                select line;
            int progressCounter = 0;
            foreach (string line in lines)
            {
                pauseEvent.WaitOne();
                output.TryAdd(line);
                if (++progressCounter % 1000 == 0)
                {
                    progress.Report(progressCounter);
                }
            }
        }
    }
}
