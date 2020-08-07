using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace D2S.LibraryTests
{
    public class SqlExtractorTestHelper
    {
        /// <summary>
        /// initializes a test table
        /// </summary>
        /// <param name="connectionstring"></param>
        /// <returns>the name of the test table</returns>
        public string Initialize(string connectionstring)
        {
            using (SqlConnection con = new SqlConnection(connectionstring))
            {
                con.Open();
                string cmdText = @"create table dbo.SqlExtractorTest (col1 nvarchar(10), col2 int)";
                   
                using (SqlCommand cmd = new SqlCommand(cmdText, con))
                {
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = @"insert into dbo.SqlExtractorTest (col1, col2) VALUES ('Knijn', 1), ('Knijntje', 2)";
                    cmd.ExecuteNonQuery();
                }

            }
            return "dbo.SqlExtractorTest";
        }
        public void Cleanup(string connectionstring)
        {
            using (SqlConnection con = new SqlConnection(connectionstring))
            {
                con.Open();
                string cmdText = @"drop table dbo.SqlExtractorTest";
                using (SqlCommand com = new SqlCommand(cmdText, con))
                {
                    com.ExecuteNonQuery();
                }
            }
        }

        public void CreateTestEntryForPortFolioDate(string connectionString)
        {
            string commtext = "if object_id('usp_Read_Portfolio_Date_Daily') is not null begin drop procedure dbo.usp_Read_Portfolio_Date_Daily end GO create procedure dbo.usp_Read_Portfolio_Date_Daily as select convert(datetime, '2018-06-30')";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand(commtext, con))
                {
                    con.Open();
                    comm.ExecuteNonQuery();
                }
            }
        }
        public void CleanTestEntryForPortfolioDate(string connectionString)
        {
            string commtext = "drop procedure dbo.usp_Read_Portfolio_Date_Daily";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand(commtext, con))
                {
                    con.Open();
                    comm.ExecuteNonQuery();
                }
            }

        }

    }

}
