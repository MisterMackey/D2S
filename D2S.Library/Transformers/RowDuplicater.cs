using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
namespace D2S.Library.Transformers
{
    public class RowDuplicater
    {
        private bool HasWork;
        private readonly object LockingObject;

        public RowDuplicater()
        {
            HasWork = true;
            LockingObject = new object();
        }

        public void SignalCompletion()
        {
            lock (LockingObject)
            {
                HasWork = false;
            }
        }

        public Action<IProducerConsumerCollection<Row>, IProducerConsumerCollection<Row>[], ManualResetEvent, IProgress<int>> GetReportingPausableWorkItem()
        {
            return ReportingWork;
        }

        private void ReportingWork(IProducerConsumerCollection<Row> input, IProducerConsumerCollection<Row>[] outputs, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            Row currentRow;
            int progressCounter = 0;
            while (HasWork)
            {
                pauseEvent.WaitOne();
                if (input.TryTake(out currentRow))
                {
                    foreach (var qeueu in outputs)
                    {
                        qeueu.TryAdd((Row)currentRow.Clone());
                    }
                    if (++progressCounter % 1000 == 0)
                    {
                        progress.Report(progressCounter);
                    }
                }
            }
            progress.Report(progressCounter);
        }
    }
}
