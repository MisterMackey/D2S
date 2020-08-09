using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using D2S.Library.Services;

namespace D2S.Library.Utilities.Tests
{
    [TestClass()]
    public class DestinationTableTruncatorTests
    {
        PipelineContext pipelineContext = new PipelineContext()
        {
            FirstLineContainsHeaders = true,
            SourceFilePath = @"..\..\..\D2S.LibraryTests\DataTypes.txt",
            StringPadding = 100
,
            DestinationTableName = "dbo.TruncatorTest"
,
            IsSuggestingDataTypes = true
        };

        [TestInitialize()]
        public void TestInit()
        {
            DestinationTableCreator d = new DestinationTableCreator(pipelineContext);
            d.CreateTable();
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                using (SqlCommand com = new SqlCommand())
                {
                    com.Connection = con;
                    con.Open();
                    com.CommandText = "insert into dbo.TruncatorTest ([string], [integer], [decimal], [char]) values ('s', 1, 1, 's')";
                    com.ExecuteNonQuery();
                }
            }
        }
        [TestCleanup()]
        public void TestCleanup()
        {
            DestinationTableDropper d = new DestinationTableDropper(pipelineContext);
            d.DropTable();
        }

        private int CheckRowCount()
        {
            int r = -1;
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                using (SqlCommand com = new SqlCommand())
                {
                    com.Connection = con;
                    con.Open();
                    com.CommandText = "select count(*) from dbo.TruncatorTest";
                    r = int.Parse(com.ExecuteScalar().ToString());
                }
            }
            return r;
        }
    

        [TestMethod()]
        public void TruncateTableWithContextTest()
        {
            DestinationTableTruncator d = new DestinationTableTruncator(pipelineContext);
            d.TruncateTable();
        }

        [TestMethod()]
        public void TruncateTableWithoutContextTest()
        {
            DestinationTableTruncator d = new DestinationTableTruncator(pipelineContext.DestinationTableName);
            d.TruncateTable();
        }
    }
}