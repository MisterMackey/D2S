using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Loaders
{
    interface ILoader<TInput, TProgress>
    {

        bool SetContext(PipelineContext context);
        PipelineContext GetContext();
        Action<PipelineContext, IProducerConsumerCollection<TInput>> GetWorkItem();
        Action<PipelineContext, IProducerConsumerCollection<TInput>, ManualResetEvent, IProgress<TProgress>> GetPausableReportingWorkItem();
        void SignalCompletion();
    }
}
