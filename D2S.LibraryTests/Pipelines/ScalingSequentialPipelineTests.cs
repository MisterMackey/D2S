using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Pipelines;
using D2S.Library.Utilities;
using D2S.Library.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace D2S.Library.Pipelines.Tests
{
    [TestClass()]
    public class ScalingSequentialPipelineTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.ScalingequentialPipeline",
            IsCreatingTable = true,
            IsDroppingTable = true,
            CpuCountUsedToComputeParallalism = 16
        };
        private PipelineContext dialContext = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumnsDial.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.ScalingSequentialPipelineDial",
            IsCreatingTable = true,
            IsDroppingTable = true,
            SourceFileIsSourcedFromDial = true,
            CpuCountUsedToComputeParallalism = 5
        };
        private int DropTableAndReturnRows(string tableName)
        {
            int ret;
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                con.Open();
                using (SqlCommand com = new SqlCommand("", con))
                {
                    com.CommandType = System.Data.CommandType.Text;
                    com.CommandText = $"delete from {tableName}";
                    ret = com.ExecuteNonQuery();
                    com.CommandText = $"drop table {tableName}";
                    com.ExecuteNonQuery();
                }
            }
            return ret;
        }
        private int rowcount;

        [TestMethod()]
        public void TogglePauseTest()
        {
            var Pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(ScalingSequentialPipeline), actual: Pipe.GetType());

            bool isPause = Pipe.IsPaused;
            Assert.IsTrue(Pipe.TogglePause());
            Assert.AreNotEqual(notExpected: isPause, actual: Pipe.IsPaused);
        }

        [TestMethod()]
        public void StartAsyncTest()
        {
            var Pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(ScalingSequentialPipeline), actual: Pipe.GetType());
            var work = Pipe.StartAsync();
            work.Wait();
            int numRowLoaded = DropTableAndReturnRows(context.DestinationTableName);
            Assert.AreEqual(expected:3, actual: numRowLoaded);
        }

        [TestMethod()]
        public void DialStartAsyncTest()
        {
            var Pipe = Pipeline.CreatePipeline(dialContext);
            Assert.AreEqual(expected: typeof(ScalingSequentialPipeline), actual: Pipe.GetType());

            Pipe.StartAsync().Wait();
            int numRowLoaded = DropTableAndReturnRows(dialContext.DestinationTableName);
            Assert.AreEqual(expected: 3, actual: numRowLoaded);
        }

        [TestMethod()]
        public void RowCounterTest()
        {
            var Pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(ScalingSequentialPipeline), actual: Pipe.GetType());

            Pipe.LinesReadFromFile += Pipe_LinesReadFromFile;
            Pipe.StartAsync().Wait();
            Assert.AreEqual(expected: 3, actual: rowcount);
        }

        private void Pipe_LinesReadFromFile(object sender, int e)
        {
            rowcount = e;
        }
    }
}