using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D2S.Library.Transformers
{
    public abstract class Transformer<TInput, TOutput, TProgress> : ITransformer<TInput, TOutput, TProgress>
    {
        protected bool HasWork;
        protected object LockingObject;

        protected abstract Action<IProducerConsumerCollection<TInput>, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> ReportingWorkItem { get; }

        public Action<IProducerConsumerCollection<TInput>, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> GetReportingPausableWorkItem()
        {
            return ReportingWorkItem;
        }
        public abstract void SignalCompletion();
    }
}
