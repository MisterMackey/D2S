using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using D2S.Library.Services;
using Microsoft.Rest;

namespace D2S.Library.Utilities.Tests
{
    [TestClass()]
    public class DestinationTableDropperTests
    {
        PipelineContext pipelineContext = new PipelineContext()
        {
            FirstLineContainsHeaders = true,
            SourceFilePath = @"..\..\..\D2S.LibraryTests\DataTypes.txt",
            StringPadding = 100
,
            DestinationTableName = "dbo.DropperTest"
,
            IsSuggestingDataTypes = true
        };

        [TestInitialize()]
        public void TestInit()
        {
            DestinationTableCreator d = new DestinationTableCreator(pipelineContext);
            d.CreateTable();
        }
        private bool TableExists()
        {
            object ret;
            using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
            {
                using (SqlCommand com = new SqlCommand())
                {
                    com.Connection = con;
                    con.Open();
                    com.CommandText = "select object_id('dbo.DropperTest')";
                    ret = com.ExecuteScalar();
                }
            }
            return ret == DBNull.Value ? false : true;
        }

        [TestMethod()]
        public void DropTableWithContextTest()
        {
            DestinationTableDropper d = new DestinationTableDropper(pipelineContext);
            d.DropTable();
            Assert.IsFalse(TableExists());
        }

        [TestMethod()]
        public void DropTableWithoutContextTest()
        {
            DestinationTableDropper d = new DestinationTableDropper(pipelineContext.DestinationTableName);
            d.DropTable();
            Assert.IsFalse(TableExists());
        }
    }
}