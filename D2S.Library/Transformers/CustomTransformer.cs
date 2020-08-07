using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Transformers
{
    public class CustomTransformer : Transformer<Row, Row, int>
    {
        public delegate void RowTransformerDelegate(ref Row row);
        private readonly RowTransformerDelegate m_Function;

        public CustomTransformer(RowTransformerDelegate function)
        {
            m_Function = function;
            HasWork = true;
            LockingObject = new object();
        }
        protected override Action<IProducerConsumerCollection<Row>, IProducerConsumerCollection<Row>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReportableWork;

        public override void SignalCompletion()
        {
            lock (LockingObject)
            {
                HasWork = false;
            }
        }

        private void ReportableWork(IProducerConsumerCollection<Row> input, IProducerConsumerCollection<Row> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            int processedCount = 0;
            Row currentRow;
            while (HasWork)
            {
                pauseEvent.WaitOne();
                if (input.TryTake(out currentRow))
                {
                    m_Function(ref currentRow);
                    output.TryAdd(currentRow);
                }               
                if (++processedCount % 1000 == 0)
                {
                    progress.Report(processedCount);
                }
            }
            progress.Report(processedCount);
        }

    }
}
