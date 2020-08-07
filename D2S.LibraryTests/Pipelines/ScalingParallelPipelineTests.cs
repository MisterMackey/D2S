using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Pipelines.Tests
{
    [TestClass()]
    public class ScalingParallelPipelineTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.ScalingParallelPipelineTest",
            IsCreatingTable = true,
            IsDroppingTable = true
        };
        private PipelineContext excelcontext = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\ExcelExtractorTest.xlsx",
            ExcelWorksheetName = "Sheet1",
            DestinationTableName = "dbo.ScalingParallelPipelineTestExcel",
            IsCreatingTable = true,
            IsDroppingTable = true
        };
        private volatile int rowcount;

        
        [TestMethod()]
        public void StartAsyncTest()
        {
            var pipe = Pipeline.CreatePipeline(context, PipelineCreationOptions.None);

            Assert.AreEqual(expected: typeof(ScalingParallelPipeline), actual: pipe.GetType());
            pipe.LinesReadFromFile += OnLinesReadEvent;

            var work = pipe.StartAsync();
            work.Wait();

            Assert.AreEqual(expected: 3, actual: rowcount);
        }
        [TestMethod()]
        public void ExcelStartAsyncTest()
        {
            var pipe = Pipeline.CreatePipeline(excelcontext, PipelineCreationOptions.None);

            Assert.AreEqual(expected: typeof(ScalingParallelPipeline), actual: pipe.GetType());
            pipe.LinesReadFromFile += OnLinesReadEvent;

            var work = pipe.StartAsync();
            work.Wait();

            Assert.AreEqual(expected: 2, actual: rowcount);
        }
        [TestMethod()]
        public void StartAsyncWithColumnSelection()
        {
            string[] Selection = context.ColumnNames.Take(3).ToArray();
            context.ColumnNamesSelection = Selection;
            var pipe = Pipeline.CreatePipeline(context, PipelineCreationOptions.None);

            Assert.AreEqual(expected: typeof(ScalingParallelPipeline), actual: pipe.GetType());
            pipe.LinesReadFromFile += OnLinesReadEvent;

            var work = pipe.StartAsync();
            work.Wait();

        }


        private void OnLinesReadEvent(object sender, int e)
        {
            rowcount += e;
        }

        [TestMethod()]
        public void TogglePauseTest()
        {
            var pipe = Pipeline.CreatePipeline(context, PipelineCreationOptions.None);

            Assert.AreEqual(expected: typeof(ScalingParallelPipeline), actual: pipe.GetType());
            Assert.IsFalse(pipe.TogglePause());
        }
    }
}