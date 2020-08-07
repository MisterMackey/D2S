using System;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using D2S.Library.Services;

namespace D2S.Library.Utilities
{
    /// <summary>
    /// Create a table (only if it does NOT EXISTS)
    /// </summary>
    public class DestinationTableCreator
    {
        private readonly PipelineContext Context;
        public DestinationTableCreator(PipelineContext c)
        {
            Context = c;
        }

        /// <summary>
        /// Create a table if it NOT EXISTS
        /// (otherwise it will throw an exception and stop the process)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void CreateTable()
        {
            Context.DestinationTableName = Context.DestinationTableName.Replace("]", "").Replace("[", "");
            var splitResults = Context.DestinationTableName.Split('.');
            var schemaName = (splitResults.Count() > 0) ? splitResults[0] : string.Empty;
            if (string.IsNullOrEmpty(schemaName))
            {
                LogService.Instance.Error($"Creating table failed because DestinationTableName (schemaName) was not specified.");
                return;
            }
            var tableName = (splitResults.Count() > 1) ? splitResults[1] : string.Empty;
            if (string.IsNullOrEmpty(tableName))
            {
                LogService.Instance.Error($"Creating table failed because DestinationTableName (tableName) was not specified.");
                return;
            }

            LogService.Instance.Info($"Creating table [{schemaName}].[{tableName}] (if not exists)");

            StringBuilder sb = new StringBuilder();
            // Check if the table NOT EXISTS before trying to create it (otherwise it will throw an exception and stop the process)
            sb.AppendLine($"IF (NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableName}'))");
            sb.AppendLine($"BEGIN");
            // Create the table
            sb.AppendLine($"CREATE TABLE [{schemaName}].[{tableName}] (");
            string Name;
            //build create statement
            for (int i = 0; i < Context.ColumnNamesSelection.Length; i++)
            {
                // escape brackets inside column names
                if (Context.ColumnNamesSelection[i].Contains("]"))
                {
                    Name = Context.ColumnNamesSelection[i].Replace("]", "]]");
                }
                else
                {
                    Name = Context.ColumnNamesSelection[i];
                }
                //build create column statement
                sb.AppendLine("[" + Name + "]" + " " + Context.DataTypes[i] + ",");
            }
            sb.Remove(sb.Length - 3, 1);
            sb.Append(")");
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
