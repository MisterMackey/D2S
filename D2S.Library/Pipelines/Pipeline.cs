using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Pipelines
{
    public abstract class Pipeline : IPipeline
    {
        public abstract bool IsPaused { get; }

        public abstract event EventHandler<int> LinesReadFromFile;

        public abstract Task StartAsync();

        /// <summary>
        /// Returns a pipeline based on the settings provided in the pipelinecontext and on the environment
        /// </summary>
        /// <returns></returns>
        public static IPipeline CreatePipeline(PipelineContext context)
        {                        
            if (context.CpuCountUsedToComputeParallalism > 4)
            {
                return new ScalingSequentialPipeline(context);
            }
            else
            {
                return new BasicSequentialPipeline(context);
            }
        }
        public static IPipeline CreatePipeline(PipelineContext context, PipelineCreationOptions options)
        {
            switch (options)
            {
                case PipelineCreationOptions.None:
                    return new ScalingParallelPipeline(context);
                case PipelineCreationOptions.PreferSequentialPipeline:
                    return CreatePipeline(context);
                case PipelineCreationOptions.InjectTransformationTask:
                    throw new NotImplementedException();
                case PipelineCreationOptions.UseBcdbPipeline:
                    throw new NotImplementedException();
                case PipelineCreationOptions.DestinationIsFile:
                    return new SqlTableToFlatFilePipeline(context);
                default:
                    throw new ArgumentException("invalid value of parameter", "options");
            }            
        }

        public abstract bool TogglePause();

        #region ProtectedMethods
        /// <summary>
        /// creates a table based on the given context
        /// </summary>
        /// <param name="context"></param>
        protected void CreateTable(PipelineContext context)
        {
            var tableMaker = new DestinationTableCreator(context);
            tableMaker.CreateTable();
        }
        /// <summary>
        /// truncates a table based on the given context
        /// </summary>
        /// <param name="context"></param>
        protected void TruncateTable(PipelineContext context)
        {
            var truncate = new DestinationTableTruncator(context);
            truncate.TruncateTable();
        }
        /// <summary>
        /// drops a table based on the given context
        /// </summary>
        /// <param name="context"></param>
        protected void DropTable(PipelineContext context)
        {
            var drop = new DestinationTableDropper(context);
            drop.DropTable();
        }

        #endregion
    }
}
