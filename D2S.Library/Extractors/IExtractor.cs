using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Extractors
{
    interface IExtractor<TOutput, TProgress>
    {

        Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent> GetPausableWorkItem(); // left in because it was alreeady implemented 
        Action<PipelineContext, IProducerConsumerCollection<TOutput>, ManualResetEvent, IProgress<TProgress>> GetPausableReportingWorkItem(); // left in because it was alreeady implemented 

    }
}
