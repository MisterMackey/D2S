using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using D2S.Library.Services;
using D2S.Library.Utilities;

namespace D2S.Library.Extractors
{
    /// <summary>
    /// SqlRecordExtractor can connect to a sql table and extract records from it in a streaming fashion
    /// </summary>
    
    public class SqlRecordExtractor : Extractor<object, int>
    {
        //TODO: remove all the sanitizing stuff and place that in a seperate class whose job it is to do that stuff, freeing up this class to just read data and nothing else
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        protected override Action<PipelineContext, IProducerConsumerCollection<object>, ManualResetEvent> PausableWorkItem
        {
            get
            {
                return (context, collection, pauseEvent) =>
                {
                    if (context == null)
                    {
                        var outputMessage = "PipelineContext is not initialized for this instance of SqlRecordExtractor";
                        LogService.Instance.Error(outputMessage);
                        throw new InvalidOperationException(outputMessage);
                    }

                    #region sanitizing stuff
                    List<string> SelectedColumns = context.SqlSourceColumnsSelected;
                    List<string> AvailableColumns = new List<string>();
                    //instantiate a connection
                    using (SqlConnection Connection = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                    {
                        Connection.Open();
                        //grab a list of available columns to verify that the selected columns are available
                        using (SqlCommand Command = Connection.CreateCommand())
                        {
                            Command.CommandText = "select [name] from sys.columns where [object_id] = (select [object_id] from sys.tables where [name] = @sourcename and [schema_id] = (select [schema_id] from sys.schemas where name = @schema))";
                            //the tablename includes the schema, which is why we split
                            Command.Parameters.Add(new SqlParameter("@sourcename", context.SourceTableName.Split('.')[1]));
                            Command.Parameters.Add(new SqlParameter("@schema", context.SourceTableName.Split('.')[0]));

                            Command.ExecuteNonQuery();
                            using (SqlDataReader Reader = Command.ExecuteReader())
                            {
                                while (Reader.Read())
                                {
                                    AvailableColumns.Add((string)Reader.GetValue(0));
                                }
                            }
                            //check if the selection is valid, also deals with nasty injections
                            foreach (string selectedColumnName in SelectedColumns)
                            {
                                //forall selected columns, check if it exists in the list of available columns
                                if (!AvailableColumns.Any(availableName => availableName.Equals(selectedColumnName)))
                                {
                                    var outputMessage = $"The column with name: {selectedColumnName} does not exists in the source";
                                    LogService.Instance.Error(outputMessage);
                                    throw new ConstraintException(outputMessage);
                                }
                            }
                        }
                    }
                    #endregion

                    //actual reading
                    using (SqlConnection Connection = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                    {
                        Connection.Open();
                        using (SqlCommand Command = Connection.CreateCommand())
                        {
                            //build sql text
                            StringBuilder sb = new StringBuilder();
                            sb.Append("select ");
                            int columnCount = 0;
                            foreach (string column in SelectedColumns)
                            {
                                //place brackets around column name and escape all brackets that are already there. end with , space
                                sb.Append("[" + column.Replace("[", "[[").Replace("]", "]]") + "], ");
                                columnCount++;
                            }
                            //remove last comma, leave the space
                            sb.Remove(sb.Length - 2, 1);
                            //add from clause, check for some injection first
                            Regex TableNameChecker = new Regex(@" select | update | insert | delete | drop | create | alter | exec |;|\(|\)", RegexOptions.IgnoreCase);
                            if (TableNameChecker.IsMatch(context.SourceTableName))
                            {
                                var outputMessage = $"Invalid input detected in table name: {context.SourceTableName}";
                                LogService.Instance.Error(outputMessage);
                                throw new ArgumentException(outputMessage);
                            }
                            sb.Append("from " + context.SourceTableName);

                            //build command, input is checked for sql keywords, parentheses and ; characters
#pragma warning disable 2100
                            Command.CommandText = sb.ToString();
#pragma warning restore 2100
                            using (SqlDataReader Reader = Command.ExecuteReader())
                            {
                                if (pauseEvent == null)
                                {
                                    while (Reader.Read())
                                    {
                                        object[] newRow = new object[columnCount];
                                        Reader.GetValues(newRow);
                                        collection.TryAdd(newRow);
                                    } 
                                }
                                else
                                {
                                    pauseEvent.WaitOne();
                                    object[] newRow = new object[columnCount];
                                    Reader.GetValues(newRow);
                                    collection.TryAdd(newRow);
                                }
                            }
                        }
                    }
                };
            }
        }

        protected override Action<PipelineContext, IProducerConsumerCollection<object>, ManualResetEvent, IProgress<int>> ReportingWorkItem => throw new NotImplementedException();
    }
}
