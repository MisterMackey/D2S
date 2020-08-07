using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Pipelines
{
    [Flags]
    public enum PipelineCreationOptions
    {
        None = 0,
        PreferSequentialPipeline = (1 << 0),
        InjectTransformationTask = (1 << 1),
        UseBcdbPipeline = (1 << 2),
        DestinationIsFile = (1 << 3)
    }
}
