using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Data.SqlClient;
using D2S.Library.Services;

namespace D2S.Library.Loaders.Tests
{
    [TestClass()]
    public class SimpleSqlTableLoaderTests
    {
        private PipelineContext context = new PipelineContext()
        {
            DestinationTableName = "dbo.SimpleSqlTableLoaderTest"
            , SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
            FirstLineContainsHeaders = true,
            Delimiter = "||",
            TotalObjectsInSequentialPipe = 10000
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
        private object ReadScalar(int colIndex, int rowIndex, string tablename)
        {
            object o;
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                con.Open();
                using (SqlCommand com = new SqlCommand("", con))
                {
                    com.CommandType = System.Data.CommandType.Text;
                    com.CommandText = $"select * from {tablename}";
                    var ret = com.ExecuteReader();
                    for (int i = 0; i < rowIndex; i++)
                    {
                        ret.Read();
                    }
                    o = ret.GetValue(colIndex - 1);
                }
            }
            return o;
        }

        [TestMethod()]
        public void PostRecordTest()
        {
            SimpleSqlTableLoader loader = new SimpleSqlTableLoader(context);
            for (int i = 0; i < 10; i++)
            {
                var row = loader.GetEmptyRow();
                foreach (var column in context.ColumnNames)
                {
                    row[column] = $"Value{i}";
                }
                loader.PostRecord(row);
            }
            //if no errors we should be okay
        }

        [TestMethod()]
        public void WriteRecordsTest()
        {
            DestinationTableCreator destinationTableCreator = new DestinationTableCreator(context);
            destinationTableCreator.CreateTable();
            SimpleSqlTableLoader loader = new SimpleSqlTableLoader(context);
            int numberOfRows = 100;
            for (int i = 0; i < numberOfRows; i++)
            {
                var row = loader.GetEmptyRow();
                foreach (var column in context.ColumnNames)
                {
                    row[column] = $"Value{i}";
                }
                loader.PostRecord(row);
            }
            loader.WriteRecords();
            
            int rowCount = DropTableAndReturnRows(context.DestinationTableName);
            Assert.AreEqual(expected: numberOfRows, actual: rowCount);
        }

        [TestMethod()]
        public void GetEmptyRowTest()
        {
            SimpleSqlTableLoader loader = new SimpleSqlTableLoader(context);
            var row = loader.GetEmptyRow();

            Assert.IsNotNull(row);
            
        }

        [TestMethod()]
        public void WriteRecordNoHeaderTest()
        {
            var cont = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
                Delimiter = "||",
                TotalObjectsInSequentialPipe = 10,
                DestinationTableName = "dbo.SimpleSqlTableLoaderTest"
            };
            DestinationTableCreator table = new DestinationTableCreator(cont);              
            table.CreateTable();
            SimpleSqlTableLoader loader = new SimpleSqlTableLoader(cont);
            for (int i = 0; i < 5; i++)
            {
                var row = loader.GetEmptyRow();
                for (int j = 0; j < cont.ColumnNamesSelection.Count(); j ++)
                {
                    row[j] = $"col{j}";
                }
                loader.PostRecord(row);
            }
            try
            {
                loader.WriteRecords();
            }
            finally
            {
                int rowCount = DropTableAndReturnRows(cont.DestinationTableName);
                Assert.IsTrue(rowCount == 5);
            }
            
        }

        [TestMethod()]
        public void WriteDbNullValuesTest()
        {
            var cont = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
                Delimiter = "||",
                TotalObjectsInSequentialPipe = 10,
                DestinationTableName = "dbo.WriteDbNullValuesTest",
                DbNullStringValue = "nullerino"
            };
            DestinationTableCreator table = new DestinationTableCreator(cont);
            table.CreateTable();
            SimpleSqlTableLoader loader = new SimpleSqlTableLoader(cont);
            var row = loader.GetEmptyRow();
            for (int j = 0; j < cont.ColumnNamesSelection.Count(); j++)
            {
                row[j] = "nullerino";
            }
            loader.PostRecord(row);
            try
            {
                loader.WriteRecords();
            }
            finally
            {
                var val = ReadScalar(1, 1, "dbo.WriteDbNullValuesTest");
                Assert.AreEqual(expected: DBNull.Value, actual: val);
                DropTableAndReturnRows("dbo.WriteDbNullValuesTest");
            }
        }
    }
}