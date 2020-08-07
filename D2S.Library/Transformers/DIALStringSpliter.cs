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

namespace D2S.Library.Transformers
{
    public class DIALStringSpliter : Transformer<string, object[], int>
    {
        private readonly string[] _Delimiter;
        [Obsolete("Regular stringsplitter now accepts double delimiters, please use it instead")]
        protected override Action<IProducerConsumerCollection<string>, IProducerConsumerCollection<object[]>, ManualResetEvent, IProgress<int>> ReportingWorkItem => DoWorkAndReport;

        /// <summary>
        /// Creates a new DIALStringSplitter, behaves the same as the regular stringsplitter but accepts multiple character delimiters and is thus suitable for data sourced from DIAL.
        /// </summary>
        /// <param name="delimiter">The delimiter to use, default is double pipe (||).</param>
        public DIALStringSpliter(String delimiter = "||")
        {
            HasWork = true;
            LockingObject = new object();
            _Delimiter = new string[] { delimiter };
        }

        public override void SignalCompletion()
        {
            lock (LockingObject) { HasWork = false; }
        }

        private void DoWorkAndReport(IProducerConsumerCollection<string> inputCollection, IProducerConsumerCollection<object[]> outputCollection, ManualResetEvent pauseEvent, IProgress<int> progress)
        {

            string[] _Delimiter = this._Delimiter;
            string InputString;
            int ProcessedCount = 0;
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
            progress.Report(ProcessedCount);
        }
    }

}

