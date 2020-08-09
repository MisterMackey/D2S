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
    public class DestinationTableCreatorTests
    {
        PipelineContext pipelineContext = new PipelineContext()
        {
            FirstLineContainsHeaders = true,
            SourceFilePath = @"..\..\..\D2S.LibraryTests\DataTypes.txt",
            StringPadding = 100
    ,
            DestinationTableName = "dbo.CreationTest"
    ,
            IsSuggestingDataTypes = true
        };

        [TestMethod()]
        public void CreateTableWithContextTest()
        {
            /* result should look like this without spaces:
             *   string	167	6
                integer	56	4
                decimal	106	5
                char	175	1
                */


            DestinationTableCreator creater = new DestinationTableCreator(pipelineContext);

            try
            {
                creater.CreateTable();

                string[] nameTypeDataType = new string[4];
                using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    con.Open();
                    string commtext = "select name, CONVERT(nvarchar,system_type_id),CONVERT(nvarchar, max_length) from sys.columns where object_id = (select object_id from sys.tables where name = 'CreationTest')";
                    using (SqlCommand comm = new SqlCommand(commtext, con))
                    {
                        SqlDataReader result = comm.ExecuteReader();
                        for (int i =0; i <4; i++)
                        {
                            result.Read();
                            nameTypeDataType[i] = result.GetString(0) + result.GetString(1) + result.GetString(2);
                        }
                    }
                }


                Assert.IsTrue(nameTypeDataType[0] == "string23112");
                Assert.IsTrue(nameTypeDataType[1] == "integer564");
                Assert.IsTrue(nameTypeDataType[2] == "decimal10617");
                Assert.IsTrue(nameTypeDataType[3] == "char1751");
            }

            finally
            {
                using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    con.Open();
                    string commtext = "drop table dbo.CreationTest";
                    using (SqlCommand comm = new SqlCommand(commtext, con))
                    {
                        comm.ExecuteNonQuery();
                    }
                }
            }
        }

        [TestMethod()]
        public void CreateTableWithoutContextTest()
        {
            /* result should look like this without spaces:
             *   string	167	6
                integer	56	4
                decimal	106	5
                char	175	1
                */

            DestinationTableCreator creater = new DestinationTableCreator(pipelineContext.DestinationTableName, pipelineContext.ColumnNamesSelection, pipelineContext.DataTypes);

            try
            {
                creater.CreateTable();

                string[] nameTypeDataType = new string[4];
                using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    con.Open();
                    string commtext = "select name, CONVERT(nvarchar,system_type_id),CONVERT(nvarchar, max_length) from sys.columns where object_id = (select object_id from sys.tables where name = 'CreationTest')";
                    using (SqlCommand comm = new SqlCommand(commtext, con))
                    {
                        SqlDataReader result = comm.ExecuteReader();
                        for (int i = 0; i < 4; i++)
                        {
                            result.Read();
                            nameTypeDataType[i] = result.GetString(0) + result.GetString(1) + result.GetString(2);
                        }
                    }
                }


                Assert.IsTrue(nameTypeDataType[0] == "string23112");
                Assert.IsTrue(nameTypeDataType[1] == "integer564");
                Assert.IsTrue(nameTypeDataType[2] == "decimal10617");
                Assert.IsTrue(nameTypeDataType[3] == "char1751");
            }

            finally
            {
                using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    con.Open();
                    string commtext = "drop table dbo.CreationTest";
                    using (SqlCommand comm = new SqlCommand(commtext, con))
                    {
                        comm.ExecuteNonQuery();
                    }
                }
            }
        }

    }
}