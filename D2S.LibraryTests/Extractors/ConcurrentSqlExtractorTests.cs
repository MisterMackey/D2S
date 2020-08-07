using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Data.SqlClient;
using D2S.Library.Services;
using D2S.Library.Pipelines;

namespace D2S.Library.Extractors.Tests
{
    [TestClass()]
    public class ConcurrentSqlExtractorTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceTableName = "dbo.ConcurrentSqlExtractorTest"
        };
        private void CreateTestTable()
        {
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                con.Open();
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.CommandText = $"create table {context.SourceTableName} (col1 int, col2 int, col3 int)";
                    comm.Connection = con;
                    comm.ExecuteNonQuery();
                    comm.CommandText = $"insert into {context.SourceTableName} (col1, col2, col3) values (1,2,3),(4,5,6),(7,8,9),(10,11,12),(13,14,15)";
                    comm.ExecuteNonQuery();
                }
            }
        }
        private void DestroyTestTable()
        {
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                con.Open();
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.CommandText = $"drop table {context.SourceTableName}";
                    comm.Connection = con;
                    comm.ExecuteNonQuery();
                }
            }
        }
        private void UpdateTestTable()
        {
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                con.Open();
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.CommandText = $"update {context.SourceTableName} set col1 = NULL, col2=NULL,col3=NULL";
                    comm.Connection = con;
                    comm.ExecuteNonQuery();
                }
            }

        }


        [TestMethod()]
        public void TryExtractRecordTest()
        {
                try
                {
                    CreateTestTable();


                    ConcurrentSqlExtractor reader = new ConcurrentSqlExtractor(context);
                    
                    BoundedConcurrentQueu<object[]> output = new BoundedConcurrentQueu<object[]>();
                    Action action = () =>
                    {
                        object[] currObject = null;
                        while (reader.TryExtractRecord(out currObject))
                        {
                            output.TryAdd(currObject);
                        }
                    };
                    List<Task> tasks = new List<Task>();
                    for (int i =0; i <3; i++)
                    {
                        tasks.Add(Task.Factory.StartNew(action));
                    }
                    Task.WhenAll(tasks).Wait() ;
                    Assert.IsTrue(output.Count == 5);
                }
                finally
                {
                DestroyTestTable();
                } 
            
        }

        [TestMethod()]
        public void GetDataTableTest()
        {            
            try
            {
                CreateTestTable();

                ConcurrentSqlExtractor reader = new ConcurrentSqlExtractor(context);
                var result = reader.GetDataTable();
                Assert.AreEqual(expected: 3, actual: result.Rows.Count);
                Assert.AreEqual(expected: "col1", actual: result.Rows[0]["ColumnName"]);
            }
            finally
            {
                DestroyTestTable();
            }
        }

        [TestMethod()]
        public void SkipperinoCappucinoMachiatoTest()
        {
            try
            {
            CreateTestTable();
                ConcurrentSqlExtractor reader = new ConcurrentSqlExtractor(context);
                BoundedConcurrentQueu<object[]> output = new BoundedConcurrentQueu<object[]>();
                object[] row = null;

                Assert.IsTrue(reader.TrySkipRecord());
                Assert.IsTrue(reader.TrySkipRecord());
                while (reader.TryExtractRecord(out row))
                {
                    output.TryAdd(row);
                }
                Assert.IsTrue(output.Count == 3);
            }
            finally
            {
                    DestroyTestTable();
            }
            
        }

        [TestMethod()]
        public void DbNullValueTest()
        {
            string OmegaLul = "HoHoHaHa";
            try
            {
                CreateTestTable();
                UpdateTestTable();
                PipelineContext context = new PipelineContext()
                {
                    SourceTableName = "dbo.ConcurrentSqlExtractorTest"
                    , DbNullStringValue = OmegaLul
                };
                ConcurrentSqlExtractor reader = new ConcurrentSqlExtractor(context);
                BoundedConcurrentQueu<object[]> output = new BoundedConcurrentQueu<object[]>();
                object[] row = null;

                while (reader.TryExtractRecord(out row))
                {
                    output.TryAdd(row);
                }
                Assert.IsTrue(output.Count == 5);
                object[] val = null;
                output.TryTake(out val);
                Assert.AreEqual(expected: OmegaLul, actual: val[0]);
            }
            finally
            {
                DestroyTestTable();
            }
        }
    }
}