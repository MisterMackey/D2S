using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using D2S.Library.Services;
using D2S.Library.Utilities;

namespace D2S.Library.Extractors
{
    /// <summary>
    /// FlatFileExtractor can extract the data out of flat files
    /// </summary>
    public class FlatFileExtractor : Extractor<String, int>
    {
        
        protected override Action<PipelineContext, IProducerConsumerCollection<String>, ManualResetEvent> PausableWorkItem
        {
            get 
            {
                return (context, collection, pauseEvent) =>
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
                        if (pauseEvent == null)
                        {
                            while ((line = Reader.ReadLine()) != null)
                            {
                                collection.TryAdd(line);
                            } 
                        }
                        else
                        {
                            while ((line = Reader.ReadLine()) != null)
                            {
                                pauseEvent.WaitOne();
                                collection.TryAdd(line);
                            }
                        }
                    }
                };
            }
        }

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
                if (pauseEvent == null)
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
