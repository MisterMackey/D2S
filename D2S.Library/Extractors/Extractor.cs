using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Extractors
{
    /// <summary>
    /// base class for all extractors
    /// </summary>
    public abstract class Extractor<TOutput, TProgress> : IExtractor<TOutput, TProgress>
    {
        protected abstract Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent> PausableWorkItem { get; }
        protected abstract Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> ReportingWorkItem { get; }
        protected PipelineContext Context;
        public PipelineContext GetContext()
        {
            return Context;
        }
        
        public Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent> GetPausableWorkItem()
        {
            return PausableWorkItem;
        }

        public bool SetContext(PipelineContext context)
        {
            Context = context;
            return true;
        }

        public Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> GetPausableReportingWorkItem()
        {
            return ReportingWorkItem;
        }
    }
}
