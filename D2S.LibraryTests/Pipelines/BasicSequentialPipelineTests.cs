using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Pipelines;
using D2S.Library.Utilities;
using D2S.Library.Services;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace D2S.Library.Pipelines.Tests
{
    [TestClass()]
    public class BasicSequentialPipelineTests
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
        private PipelineContext dialContext = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumnsDial.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.BasicSequentialPipelineDial",
            IsCreatingTable = true,
            IsDroppingTable = true,
            SourceFileIsSourcedFromDial = true,
            CpuCountUsedToComputeParallalism = 4
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
        public void StartAsyncTest()
        {

            var pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(BasicSequentialPipeline), actual: pipe.GetType());

            var work = pipe.StartAsync();

            work.Wait();

            Assert.IsTrue(work.Status == TaskStatus.RanToCompletion);
            int numRowsLoaded = DropTableAndReturnRows(context.DestinationTableName);
            Assert.IsTrue(numRowsLoaded == 3);
        }

        [TestMethod()]
        public void DialFormatStartAsyncTest()
        {
            var pipe = Pipeline.CreatePipeline(dialContext);
            var work = pipe.StartAsync();

            work.Wait();

            Assert.IsTrue(work.Status == TaskStatus.RanToCompletion);
            int numRowsLoaded = DropTableAndReturnRows(dialContext.DestinationTableName);
            Assert.IsTrue(numRowsLoaded == 3);
        }


        [TestMethod()]
        public void TogglePauseTest()
        {
            //todo: fix the context to throw a better exception when source is not initialized and someone tries to acces column names property.
            var pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(BasicSequentialPipeline), actual: pipe.GetType());

            bool initialState = pipe.IsPaused;
            Assert.IsTrue(pipe.TogglePause());
            Assert.AreNotEqual(notExpected: initialState, actual: pipe.IsPaused);
        }
        [TestMethod()]
        public void RowCounterTest()
        {
            var pipe = Pipeline.CreatePipeline(context);
            Assert.AreEqual(expected: typeof(BasicSequentialPipeline), actual: pipe.GetType());
            pipe.LinesReadFromFile += Pipe_LinesReadFromFile;
            rowcount = 0;
            var work = pipe.StartAsync();
            work.Wait();
            Assert.IsTrue(rowcount == 3);
        }

        private void Pipe_LinesReadFromFile(object sender, int e)
        {
            rowcount = e;
        }
    }
}