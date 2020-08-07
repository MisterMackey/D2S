using D2S.Library.Extractors;
using D2S.Library.Services;
using D2S.Library.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace D2S.Library.Extractors
{
    public class DIALFlatFileExtractor : Extractor<string, int>
    {


        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> PausableWorkItem => throw new NotImplementedException();

        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReportableWorkItem;

        private void ReportableWorkItem(PipelineContext context, IProducerConsumerCollection<string> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            if (context == null)
            {
                var outputMessage = "PipelineContext is not initialized for this instance of FlatfileExtractor";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            using (StreamReader Reader = new StreamReader(context.SourceFilePath))
            {
                if (context.FirstLineContainsHeaders) { Reader.ReadLine(); }
                if (context.SourceFileIsSourcedFromDial) { Reader.ReadLine(); }
                string line;
                int progressCounter = 0;
                if (pauseEvent == null && context.SourceFileIsSourcedFromDial)
                {
                    line = Reader.ReadLine();
                    while (Reader.Peek() > -1)
                    {
                        output.TryAdd(line);
                        progressCounter++;
                        line = Reader.ReadLine();
                        if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                    }
                }
                else if (context.SourceFileIsSourcedFromDial)
                {
                    line = Reader.ReadLine();
                    while (Reader.Peek() > -1)
                    {
                        output.TryAdd(line);
                        pauseEvent.WaitOne();
                        progressCounter++;
                        line = Reader.ReadLine();
                        if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                    }
                }
                else if (pauseEvent == null && !(context.SourceFileIsSourcedFromDial))
                {
                    while ((line = Reader.ReadLine()) != null)
                    {
                        output.TryAdd(line);
                        progressCounter++;
                        if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                    }
                }
                else
                {
                    while ((line = Reader.ReadLine()) != null)
                    {
                        pauseEvent.WaitOne();
                        output.TryAdd(line);
                        progressCounter++;
                        if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                    }
                }
                progress.Report(progressCounter);
            }

        }

    }

}
