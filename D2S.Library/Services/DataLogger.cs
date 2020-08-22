﻿namespace D2S.Library.Services
{
    using System;
    using System.Data.SqlClient;
    using System.Data;
    using D2S.Library.Entities;
    using System.Collections.Generic;
    using System.Linq;

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
        private RunLogEntry _runLogEntry;
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

        #region Logger
        /// <summary>
        /// Opens a log entry for a run. Call CloseLogEntry before opening a new entry.
        /// </summary>
        public void OpenLogEntry(string Source, string Target)
        {
            if (_hasOpenLogEntry)
            {
                var outputMessage = $"A log entry is already open, please close the log for the current run before opening a new one";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                try
                {
                    _hasOpenLogEntry = true;
                    _runLogEntry = new RunLogEntry()
                    {
                        Source = Source, Target = Target,
                        MachineName = Environment.MachineName,
                        UserName = $"{Environment.UserDomainName}\\{Environment.UserName}",
                        Status = "Open",
                        StartTime = DateTime.Now,
                        Tasks = new List<TaskLogEntry>()
                    };
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
        /// If this method  is not called, any log entries written will be lost.
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
                DateTime endTime = DateTime.Now;
                string status = processWasSuccessfull ? "SUCCESS" : "FAILED";

                try
                {
                    _runLogEntry.Status = status;
                    _runLogEntry.EndTime = endTime;
                    D2SLogContext context = new D2SLogContext(false);
                    context.RunLogEntries.Add(_runLogEntry);
                    context.SaveChanges();
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
            }
        }

        /// <summary>
        /// Logs a message from the given taskName to sql log table. Call MarkTaskAsComplete afterwards to fill the end datetime field.
        /// </summary>
        /// <param name="taskName">the source of the log message</param>
        /// <param name="target">the log message</param>
        public void LogTaskToSql(string taskName, string target)
        {
            if (!_hasOpenLogEntry)
            {
                var outputMessage = "Attempted to log a task when no log entry was opened";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            else
            {
                _runLogEntry.Tasks.Add(new TaskLogEntry()
                {
                    TaskName = taskName,
                    Target = target,
                    Status = "Open",
                    StartTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Finds the task log entry with the given taskName and sets the End_Time field to the current datetime and the status flag.
        /// Also optionally sets a message string
        /// </summary>
        /// <param name="taskName">the source name of an existing log entry</param>
        public void MarkTaskAsComplete(string taskName, bool processWasSuccessfull, string message)
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
                TaskLogEntry entry = _runLogEntry.Tasks.Where(x => x.TaskName == taskName).FirstOrDefault();
                
                if (entry == null)
                {
                    LogService.Instance.Warn("Attempted to close a task entry that did not exist: " + taskName);
                }
                else
                {
                    string status = processWasSuccessfull ? "SUCCESS" : "FAILED";
                    entry.EndTime = DateTime.Now;
                    entry.Message = message;
                    entry.Status = status;                    
                }
            }
        }


        #endregion

        #endregion Methods
    }
}