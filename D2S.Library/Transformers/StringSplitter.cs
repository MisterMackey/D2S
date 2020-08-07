using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using D2S.Library.Helpers;
using D2S.Library.Services;

namespace D2S.Library.Transformers
{
    public class StringSplitter : Transformer<String, object[], int>
    {
        public string Delimiter { get; set; }
        private readonly string Qualifier;
        protected override Action<IProducerConsumerCollection<string>, IProducerConsumerCollection<object[]>, ManualResetEvent, IProgress<int>> ReportingWorkItem => DoWorkAndReport;

        public StringSplitter(string qualifier = "\"")
        {
            HasWork = true;
            LockingObject = new object();
            Qualifier = qualifier;
        }

        public override void SignalCompletion()
        {
            lock (LockingObject) { HasWork = false; }
        }

        private void DoWorkAndReport(IProducerConsumerCollection<string> inputCollection, IProducerConsumerCollection<object[]> outputCollection, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            if (Delimiter == null)
            {
                var outputMessage = "Delimiter is not set for this Stringsplitter";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            string[] _Delimiter = new string[] { Delimiter };
            string InputString;
            int ProcessedCount = 0;
            if (Qualifier != null)
            {
                while (HasWork)
                {
                    pauseEvent.WaitOne();
                    if (inputCollection.TryTake(out InputString))
                    {
                        string[] OutputString = StringAndText.SplitRow(InputString, Delimiter, Qualifier, false);

                        while (!outputCollection.TryAdd(OutputString)) { pauseEvent.WaitOne(); }
                        ProcessedCount++;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                    if (ProcessedCount % 1000 == 0)
                    {
                        progress.Report(ProcessedCount);
                    }
                } 
            }
            else
            {
                while (HasWork)
                {
                    pauseEvent.WaitOne();
                    if (inputCollection.TryTake(out InputString))
                    {
                        string[] OutputString = InputString.Split(_Delimiter, StringSplitOptions.None);

                        while (!outputCollection.TryAdd(OutputString)) { pauseEvent.WaitOne(); }
                        ProcessedCount++;
                    }
                    if (ProcessedCount % 1000 == 0)
                    {
                        progress.Report(ProcessedCount);
                    }
                }
            }
            progress.Report(ProcessedCount);
        }

    }
}
