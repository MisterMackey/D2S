namespace D2S.Library.Services
{
    using System;
    using System.Data.SqlClient;
    using System.Data;

    /// <summary>
    /// Logging database related functionality
    /// <para>Can be used as a singleton for best performance, for example DataLogger.Instance</para>
    /// </summary>
    [Serializable]
    public class DataLogger
    {
        #region Members

        private static volatile DataLogger _instance;
        private static readonly object SyncRoot = new object();

        private bool _hasOpenLogEntry;
        private int _LogId;
        //following datatables are used to buffer log messages before they are written to sql
        private static DataTable _SqlTaskLog;
        private static DataTable _SqlErrorLog;
        private static SqlBulkCopyColumnMapping[] _SqlTaskLogMappings = new SqlBulkCopyColumnMapping[]
            {new SqlBulkCopyColumnMapping(0,1),
            new SqlBulkCopyColumnMapping(1,2),
            new SqlBulkCopyColumnMapping(2,3),
            new SqlBulkCopyColumnMapping(3,4),
            new SqlBulkCopyColumnMapping(4,5),
            new SqlBulkCopyColumnMapping(5,6)
            };
        private static SqlBulkCopyColumnMapping[] _SqlErrorLogMappings = new SqlBulkCopyColumnMapping[]
            {new SqlBulkCopyColumnMapping(0,1),
            new SqlBulkCopyColumnMapping(1,2),
            new SqlBulkCopyColumnMapping(2,3),
            new SqlBulkCopyColumnMapping(3,4),
            new SqlBulkCopyColumnMapping(4,5),
            new SqlBulkCopyColumnMapping(5,6)
            };
        private static DataColumn[] _sqlTaskLogColumns = new DataColumn[]
            {new DataColumn("Package_Log_ID", typeof(int)),
            new DataColumn("Source_Name", typeof(string)),
            new DataColumn("Source_ID", typeof(Guid)),
            new DataColumn("Start_DateTime", typeof(DateTime)),
            new DataColumn("End_DateTime", typeof(DateTime)),
            new DataColumn("LogMessage", typeof(string)) };
        private static DataColumn[] _sqlErrorLogColumns = new DataColumn[]
            {new DataColumn("Package_Log_ID", typeof(int)),
            new DataColumn("Source_Name", typeof(string)),
            new DataColumn("Source_ID", typeof(Guid)),
            new DataColumn("Error_Code", typeof(int)),
            new DataColumn("Error_Description", typeof(string)),
            new DataColumn("Log_DateTime", typeof(DateTime)) };
        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static DataLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataLogger();
                        }
                    }
                }

                return (_instance);
            }
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Constructor
        /// </summary>
        public DataLogger()
        {
        }

        #endregion Events

        #region Methods

        #region Liq_Dwh_Logger
        /// <summary>
        /// Opens a log entry that will be written to ssis.package_log in the liquidity database. The DataLogger class will keep track of this entry and update it and related records in related tables. Call CloseLogEntry before opening a new entry.
        /// </summary>
        public void OpenLogEntry()
        {
            if (_hasOpenLogEntry)
            {
                var outputMessage = $"Log entry is already opened with ID {_LogId}";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                //instantiate the parameters required to open the log entry
                int packageVersionId = 1; //not really applicable for our apps so we just default it i guess
                Guid guid = new Guid("a47d7764-645e-4537-bb37-0540882e33c2"); //taken from assembly info
                string machineName;

                if ((machineName = Environment.GetEnvironmentVariable("computername")) != null)
                {

                }
                else
                {
                    machineName = "Unknown";
                }

                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                DateTime startDatTime = DateTime.Now;
                string status = "Running";

                try
                {
                    using (SqlConnection conn = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                    {
                        conn.Open();
                        using (SqlCommand comm = new SqlCommand())
                        {
                            comm.Connection = conn;
                            comm.CommandType = CommandType.StoredProcedure;
                            comm.CommandText = "dbo.usp_Add_Entry_To_Package_Log";
                            comm.Parameters.AddWithValue("@versionId", packageVersionId);
                            comm.Parameters.AddWithValue("@GUID", guid);
                            comm.Parameters.AddWithValue("@Machine_Name", machineName);
                            comm.Parameters.AddWithValue("@UserId", userName);
                            comm.Parameters.AddWithValue("@StartDateTime", startDatTime);
                            comm.Parameters.AddWithValue("@status", status);
                            SqlParameter returnparam = new SqlParameter("@ReturnVal", SqlDbType.Int);
                            returnparam.Direction = ParameterDirection.ReturnValue;
                            comm.Parameters.Add(returnparam);

                            comm.ExecuteNonQuery();

                            int newId;
                            try
                            {
                                newId = (int)returnparam.Value;
                            }
                            catch
                            {
                                newId = 0;
                            }

                            _hasOpenLogEntry = true;
                            _LogId = newId;

                            comm.Dispose();
                        }

                        conn.Close();
                    }
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

        /// <summary>
        /// Closes the previously opened logentry with either a success or failure code. 
        /// </summary>
        /// <remarks>
        /// If this method or <see cref="DataLogger.FlushSqlLogMessages"/> is not called, any log entries written will be lost.
        /// </remarks>
        /// <param name="processWasSuccessfull">A bool indicating if the process has completed successfully</param>
        public void CloseLogEntry(bool processWasSuccessfull)
        {
            if (!_hasOpenLogEntry)
            {
                var outputMessage = "Attempted to close a log entry when no log entry was open";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                //clear buffered messages
                FlushSqlLogMessages();
                DateTime endTime = DateTime.Now;
                string status = processWasSuccessfull ? "SUCCESS" : "FAILED";

                try
                {
                    using (SqlConnection conn = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                    {
                        conn.Open();

                        using (SqlCommand comm = new SqlCommand())
                        {
                            comm.Connection = conn;
                            comm.CommandText = "update ssis.package_log set End_DateTime = @end, Status = @status where Package_Log_Id = @logId";
                            comm.Parameters.AddWithValue("@end", endTime);
                            comm.Parameters.AddWithValue("@status", status);
                            comm.Parameters.AddWithValue("@logId", _LogId);
                            comm.ExecuteNonQuery();

                            comm.Dispose();
                        }

                        conn.Close();
                    }
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

                _hasOpenLogEntry = false;
                _LogId = 0;
            }
        }

        /// <summary>
        /// Clears buffered messages and writes them to SQL. 
        /// </summary>
        public void FlushSqlLogMessages()
        {
            if (_SqlTaskLog == null && _SqlErrorLog == null) { }
            else
            {
                try
                { 
                    using (SqlConnection conn = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                    {
                        conn.Open();

                        using (SqlBulkCopy sbc = new SqlBulkCopy(conn))
                        {
                            if (_SqlTaskLog != null)
                            {
                                foreach (var mapping in _SqlTaskLogMappings)
                                {
                                    sbc.ColumnMappings.Add(mapping);
                                }
                                sbc.DestinationTableName = _SqlTaskLog.Namespace + "." + _SqlTaskLog.TableName;
                            
                                sbc.WriteToServer(_SqlTaskLog);
                            }
                            if (_SqlErrorLog != null)
                            {
                                sbc.ColumnMappings.Clear();
                                foreach (var mapping in _SqlErrorLogMappings)
                                {
                                    sbc.ColumnMappings.Add(mapping);
                                }
                                sbc.DestinationTableName = _SqlErrorLog.Namespace + "." + _SqlErrorLog.TableName;

                                sbc.WriteToServer(_SqlErrorLog);
                            }

                            sbc.Close();
                        }

                        conn.Close();
                    }
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

        /// <summary>
        /// Logs a message from the given source to sql log table. Call MarkTaskAsComplete afterwards to fill the end datetime field.
        /// </summary>
        /// <param name="sourceName">the source of the log message</param>
        /// <param name="message">the log message</param>
        public void LogTaskToSql(string sourceName, string message)
        {
            if (!_hasOpenLogEntry)
            {
                var outputMessage = "Attempted to log a task when no log entry was opened";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                if (_SqlTaskLog == null)
                {
                    _SqlTaskLog = new DataTable("Package_Task_Log", "ssis");
                    _SqlTaskLog.Columns.AddRange(_sqlTaskLogColumns);
                }
                DataRow newRow = _SqlTaskLog.NewRow();
                newRow[0] = _LogId;
                newRow[1] = sourceName;
                newRow[2] = DBNull.Value;
                newRow[3] = DateTime.Now;
                newRow[4] = DBNull.Value;
                newRow[5] = message;
                _SqlTaskLog.Rows.Add(newRow);
            }
        }

        /// <summary>
        /// Finds the task log entry with the given sourcename and sets the End_Time field to the current datetime
        /// </summary>
        /// <param name="sourceName">the source name of an existing log entry</param>
        public void MarkTaskAsComplete(string sourceName)
        {
            if (!_hasOpenLogEntry)
            {
                var outputMessage = "Attempted to log a task when no log entry was opened";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {

                //find the log entry if it exists
                bool success = false;
                foreach (DataRow row in _SqlTaskLog.Rows)
                {
                    if ((string)row[1] == sourceName)
                    {
                        row[4] = DateTime.Now;
                        success = true;
                        break;
                    }
                }
                if (!success)
                {
                    LogService.Instance.Warn("Attempted to close a log entry that did not exist: " + sourceName);
                }
            }
        }

        public void LogErrorToSql(string sourceName, string message, int errorCode)
        {
            if (!_hasOpenLogEntry)
            {
                var outputMessage = "Attempted to log a task when no log entry was opened";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                if (_SqlErrorLog == null)
                {
                    _SqlErrorLog = new DataTable("Package_Error_Log", "ssis");
                    _SqlErrorLog.Columns.AddRange(_sqlErrorLogColumns);
                }
                DataRow newRow = _SqlErrorLog.NewRow();
                newRow[0] = _LogId;
                newRow[1] = sourceName;
                newRow[2] = DBNull.Value;
                newRow[3] = errorCode;
                newRow[4] = message;
                newRow[5] = DateTime.Now;
                _SqlErrorLog.Rows.Add(newRow);
            }
        }
        #endregion

        #endregion Methods
    }
}