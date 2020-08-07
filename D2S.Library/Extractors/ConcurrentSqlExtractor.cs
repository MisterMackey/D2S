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
    public class ConcurrentSqlExtractor
    {
        #region Fields
        private readonly PipelineContext m_Context;
        private SqlConnection m_Connection;
        private SqlCommand m_Command;
        private SqlDataReader m_Reader;
        private bool m_ConnectionHasBeenInitialized;
        private readonly object m_SyncRoot;
        private bool m_IsFinishedReading;
        #endregion
        #region constructor
        public ConcurrentSqlExtractor(PipelineContext context)
        {
            m_Context = context;
            m_ConnectionHasBeenInitialized = false;
            m_IsFinishedReading = false;
            m_SyncRoot = new object();
        }
        #endregion

        #region public methods
        public bool TryExtractRecord(out object[] record)
        {
            lock (m_SyncRoot)
            {
                if (!m_ConnectionHasBeenInitialized && !m_IsFinishedReading)
                {
                    InitializeConnection();
                }
                bool success = false;
                record = null;
                if (m_ConnectionHasBeenInitialized && m_Reader.Read())
                {
                    record = new object[m_Reader.FieldCount];
                    success = true;
                    m_Reader.GetValues(record);
                    ReplaceDbNullValues(record);
                }
                if (!success)
                {
                    CloseConnection();
                }
                return success; 
            }
        }

        private void ReplaceDbNullValues(object[] record)
        {
            if (m_Context.DbNullStringValue != null)
            {
                for (int i = 0; i < record.Length; i++)
                {
                    if (record[i] == DBNull.Value)
                    {
                        record[i] = m_Context.DbNullStringValue;
                    }
                }
            }
        }

        public DataTable GetDataTable()
        {
            lock (m_SyncRoot)
            {
                if (!m_ConnectionHasBeenInitialized)
                {
                    InitializeConnection();
                }
                return m_Reader.GetSchemaTable();
            }
        }

        /// <summary>
        /// this very special method skips a result
        /// </summary>
        /// <returns></returns>
        public bool TrySkipRecord()
        {
            lock (m_SyncRoot)
            {
                if (!m_ConnectionHasBeenInitialized && !m_IsFinishedReading)
                {
                    InitializeConnection();
                }
                if (m_ConnectionHasBeenInitialized)
                {
                    m_Reader.Read();
                    return true; 
                }
                else
                {
                    return false;
                }
            }
        }

        public void Reset()
        {
            lock (m_SyncRoot)
            {
                if (m_ConnectionHasBeenInitialized)
                {
                    CloseConnection();
                }
                m_IsFinishedReading = false; 
            }
        }

        #endregion
        #region private methods
        private void InitializeConnection()
        {
            m_Connection = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection);
            m_Command = new SqlCommand();
            if (PotentialSqlInjectionIsPresent(m_Context.SourceTableName))
            {
                var message = $"Possible SQL injection attack detected, aborting. (suspicious value: {m_Context.SourceTableName}";
                ArgumentException ex = new ArgumentException(message);
                LogService.Instance.Error(ex, moreDetails: message);
                throw ex;
                
            }
            m_Command.CommandText = $"select * from {m_Context.SourceTableName}";
            m_Command.CommandTimeout = 0;
            m_Command.Connection = m_Connection;
            m_Connection.Open();
            m_Reader = m_Command.ExecuteReader();
            m_ConnectionHasBeenInitialized = true;
        }

        private void CloseConnection()
        {
            m_Reader.Close();
            m_Command.Dispose();
            m_Connection.Close();
            m_Connection.Dispose();
            m_IsFinishedReading = true;
            m_ConnectionHasBeenInitialized = false;
        }

        private bool PotentialSqlInjectionIsPresent(string value)
        {
            bool SqlInjectionIsPresent = true;
            //Table names must conform to one of two conditions (or both) in orer to be consideren safe.
            // option 1) name is of the form [schema].[table], this way the name will be interpreted by sql server as an actual name whatever is between the brackets. no commands can be injected
            //option 2) name is of form schema.table and has no whitespace. sql command are seperated by spaces, if we dont allow those there will be no injected commands either.
            if (Regex.IsMatch(value, @"^\[\w+\]\.\[\w+\]$")) // checks for [schemaname].[tablename]
            {
                SqlInjectionIsPresent = false;
            }
            else if (Regex.IsMatch(value, @"^\w+\.\w+$")) //check for schemaname.tablename
            {
                SqlInjectionIsPresent = false;
            }
            return SqlInjectionIsPresent;
        }
        #endregion
    }
}
