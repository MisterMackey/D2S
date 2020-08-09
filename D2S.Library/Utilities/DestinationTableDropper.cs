using System.Text;
using System.Data.SqlClient;
using D2S.Library.Services;
using System;
using System.Data;
using System.Linq;

namespace D2S.Library.Utilities
{
    /// <summary>
    /// Drop a table (only if it exists)
    /// </summary>
    public class DestinationTableDropper
    {
        private string DestinationTableName;
        /// <summary>
        /// Creates a <see cref="DestinationTableDropper"/> using the specified  <see cref="PipelineContext"/>
        /// </summary>
        /// <param name="c"></param>
        public DestinationTableDropper(PipelineContext c)
        {
            DestinationTableName = c.DestinationTableName;
        }
        /// <summary>
        /// Creates a <see cref="DestinationTableDropper"/> without using a <see cref="PipelineContext"/>
        /// </summary>
        /// <param name="destinationtableName"></param>
        public DestinationTableDropper(string destinationTableName)
        {
            DestinationTableName = destinationTableName;
        }

        /// <summary>
        /// Drop a table only if it exists (otherwise it will throw an exception and stop the process)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void DropTable()
        {
            DestinationTableName = DestinationTableName.Replace("]", "").Replace("[", "");
            var splitResults = DestinationTableName.Split('.');
            var schemaName = (splitResults.Count() > 0) ? splitResults[0] : string.Empty;
            if (string.IsNullOrEmpty(schemaName))
            {
                LogService.Instance.Error($"Dropping table failed because DestinationTableName (schemaName) was not specified.");
                return;
            }
            var tableName = (splitResults.Count() > 1) ? splitResults[1] : string.Empty;
            if (string.IsNullOrEmpty(tableName))
            {
                LogService.Instance.Error($"Dropping table failed because DestinationTableName (tableName) was not specified.");
                return;
            }

            LogService.Instance.Info($"Dropping table [{schemaName}].[{tableName}] (if exists)");

            StringBuilder sb = new StringBuilder();
            // Check if the table exists before trying to DROP it (otherwise it will throw an exception and stop the process)
            sb.AppendLine($"IF (EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableName}'))");
            sb.AppendLine($"BEGIN");
            sb.AppendLine($"DROP TABLE [{schemaName}].[{tableName}]");
            sb.AppendLine($"END");

            try
            {
#pragma warning disable S3649 // User-provided values should be sanitized before use in SQL statements
                using (SqlConnection con = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    con.Open();

                    using (SqlCommand comm = new SqlCommand(sb.ToString(), con))
                    {
                        comm.CommandType = CommandType.Text;
                        comm.ExecuteNonQuery();
                        comm.Dispose();
                    }

                    con.Close();
                }
#pragma warning restore S3649 // User-provided values should be sanitized before use in SQL statements
            }
            catch (SqlException sqlEx)
            {
                LogService.Instance.Error(sqlEx);
                throw new ApplicationException("SqlException : " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                LogService.Instance.Error(ex);
                throw new ApplicationException("Exception : " + ex.Message);
            }
        }
    }
}