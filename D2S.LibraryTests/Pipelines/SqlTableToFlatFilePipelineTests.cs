using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Data;
using System.Data.SqlClient;
using D2S.Library.Services;
using System.IO;

namespace D2S.Library.Pipelines.Tests
{
    
    [TestClass()]
    public class SqlTableToFlatFilePipelineTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceTableName = "dbo.SqlTableToFlatFilePipelineTests",
            DestinationFilePath = Environment.CurrentDirectory + "\\SqlTableToFlatFilePipelineTests.txt",
            IsTruncatingTable = true,
            Delimiter = "|~|"            
        };
        [ClassInitialize()]
        public static void init(TestContext testContext)
        {
            DataTable table = new DataTable();
            table.Columns.AddRange(new DataColumn[] { new DataColumn("OddNumbers"), new DataColumn("EvenNumbers") });
            for (int i = 0; i < 20*1000; i+=2)
            {
                var newRow = table.NewRow();
                newRow[0] = i + 1;
                newRow[1] = i;
                table.Rows.Add(newRow);
            }
            using (SqlConnection conn = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = "create table dbo.SqlTableToFlatFilePipelineTests (OddNumbers int, EvenNumbers int)";
                    conn.Open();
                    comm.ExecuteNonQuery();
                }
                using (SqlBulkCopy copy = new SqlBulkCopy(conn))
                {
                    copy.DestinationTableName = "dbo.SqlTableToFlatFilePipelineTests";
                    copy.WriteToServer(table);
                }
            }            
        }
        [ClassCleanup()]
        public static void clean()
        {
            using (SqlConnection conn = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = "drop table dbo.SqlTableToFlatFilePipelineTests";
                    conn.Open();
                    comm.ExecuteNonQuery();
                }
            }
            File.Delete(Environment.CurrentDirectory + "\\SqlTableToFlatFilePipelineTests.txt");
        }
        [TestMethod()]
        public void SqlTableToFlatFilePipelineTest()
        {
            PipelineCreationOptions options = PipelineCreationOptions.DestinationIsFile;
            var Pipe = Pipeline.CreatePipeline(context, options);
            Assert.AreEqual(expected: typeof(SqlTableToFlatFilePipeline), actual: Pipe.GetType());
            PipelineContext falseContext = new PipelineContext()
            {
                SourceTableName = " hufdspafhpawefawepjoifwaepjoifawefpoijaefjpoi",
                DestinationFilePath = "huyerfwaeafsahuifdsahuifd"
            };
            //check that exception is thrown when nonsense names are supplied in the context
            Assert.ThrowsException<ArgumentException>(
                () => Pipe = Pipeline.CreatePipeline(falseContext, options));
        }

        [TestMethod()]
        public void StartAsyncTest()
        {
            PipelineCreationOptions options = PipelineCreationOptions.DestinationIsFile;
            var Pipe = Pipeline.CreatePipeline(context, options);
            Task t = Pipe.StartAsync();
            Task.Delay(1000).Wait();
            Assert.IsTrue(File.Exists(context.DestinationFilePath));
            t.Wait();
            string[] fileContent = File.ReadAllLines(context.DestinationFilePath);
            Assert.IsTrue(fileContent.Count() == 10001); //10 000 records PLUS the headerline
            Assert.AreEqual(expected: "OddNumbers|~|EvenNumbers", actual: fileContent[0]);
            int SumEven = 0;
            int SumOdd = 0;
            int ExpectedEven = 0;
            int ExpectedOdd = 0;
            for (int i = 1; i < 10001; i++) //start at one!
            {
                SumEven += int.Parse(fileContent[i].Split(new[] { "|~|" }, StringSplitOptions.None).Last());
                SumOdd += int.Parse(fileContent[i].Split(new[] { "|~|" }, StringSplitOptions.None).First());
            }
            for (int i = 0; i < 20 * 1000; i += 2)
            {
                ExpectedOdd += i + 1;
                ExpectedEven += i;
            }
            Assert.AreEqual(expected: ExpectedEven, actual: SumEven);
            Assert.AreEqual(expected: ExpectedOdd, actual: SumOdd);
        }

        [TestMethod()]
        public void TogglePauseTest()
        {
            PipelineCreationOptions options = PipelineCreationOptions.DestinationIsFile;
            var Pipe = Pipeline.CreatePipeline(context, options);
            Assert.AreEqual(expected: typeof(SqlTableToFlatFilePipeline), actual: Pipe.GetType());

            Assert.ThrowsException<NotImplementedException>(
                () => Pipe.TogglePause());
            Assert.ThrowsException<NotImplementedException>(
                () => Pipe.IsPaused);
        }
    }
}