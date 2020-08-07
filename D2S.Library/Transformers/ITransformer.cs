using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace D2S.Library.Transformers
{
    interface ITransformer<TInput, TOutput, TProgress>
    {
        Action<IProducerConsumerCollection<TInput>, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> GetReportingPausableWorkItem();

        void SignalCompletion();
    }
}
