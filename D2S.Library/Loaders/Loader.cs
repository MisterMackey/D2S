using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library;
using D2S.Library.Utilities;

namespace D2S.Library.Loaders
{
    /// <summary>
    /// base class for all Loaders
    /// </summary>
    public abstract class Loader<TInput, TProgress> : ILoader<TInput, TProgress>
    {

        protected abstract Action<PipelineContext, IProducerConsumerCollection<TInput>> WorkItem { get; }
        protected abstract Action<PipelineContext, IProducerConsumerCollection<TInput>, ManualResetEvent, IProgress<TProgress>> PausableReportingWorkItem { get; }
        protected PipelineContext Context;
        protected bool HasWork;
        protected object LockObject;
        public virtual void SignalCompletion()
        {
            lock (LockObject)
            {
                HasWork = false;
            }
        }
        public PipelineContext GetContext()
        {
            return Context;
        }

        public Action<PipelineContext, IProducerConsumerCollection<TInput>> GetWorkItem()
        {
            return WorkItem;
        }


        public bool SetContext(PipelineContext context)
        {
            Context = context;
            return true;
        }

        public Action<PipelineContext, IProducerConsumerCollection<TInput>, ManualResetEvent, IProgress<TProgress>> GetPausableReportingWorkItem()
        {
            return PausableReportingWorkItem;
        }
    }
}
