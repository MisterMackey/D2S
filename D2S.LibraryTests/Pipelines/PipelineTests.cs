using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Pipelines;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Pipelines.Tests
{
    [TestClass()]
    public class PipelineTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.BasicSequentialPipeline",
            IsCreatingTable = true,
            IsDroppingTable = true,
            CpuCountUsedToComputeParallalism = 4
        };

        [TestMethod()]
        public void CreatePipelineTest()
        {
            var pipe = Pipeline.CreatePipeline(context);

            Assert.AreEqual(expected: typeof(BasicSequentialPipeline), actual: pipe.GetType());
        }

        [TestMethod()]
        public void CreatePipelineTest1()
        {
            var pipe = Pipeline.CreatePipeline(context, PipelineCreationOptions.None);

            Assert.AreEqual(expected: typeof(ScalingParallelPipeline), actual: pipe.GetType());
        }
    }
}