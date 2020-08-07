using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using D2S.Library.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;

namespace D2S.Library.Loaders
{
    public class SimpleSqlTableLoader
    {
        #region PrivateFields
        private readonly DataTable m_DataTable;
        private int m_NumRowsInBuffer;
        private readonly int m_BufferSize;
        private readonly string m_DestinationTableName;
        private readonly List<SqlBulkCopyColumnMapping> m_ColumnMappings;
        private readonly string m_DbNullStringValue;
        #endregion

        #region constructor
        public SimpleSqlTableLoader(PipelineContext context)
        {
            m_DestinationTableName = context.DestinationTableName;
            m_DataTable = new DataTable(m_DestinationTableName);
            m_BufferSize = context.TotalObjectsInSequentialPipe == 0 ? int.MaxValue : context.TotalObjectsInSequentialPipe;
            m_ColumnMappings = new List<SqlBulkCopyColumnMapping>();
            m_NumRowsInBuffer = 0;
            if (context.IsOrdinalColumnRanking)
            {
                for (int i = 0; i < context.ColumnNamesSelection.Count(); i++)
                {
                    m_ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, i));
                    m_DataTable.Columns.Add(context.ColumnNamesSelection[i]);
                }
            }
            else
            {
                foreach (var name in context.ColumnNamesSelection)
                {
                    m_ColumnMappings.Add(new SqlBulkCopyColumnMapping(name, name));
                    m_DataTable.Columns.Add(name);
                }
            }
            m_DbNullStringValue = context.DbNullStringValue;
        }
        #endregion

        #region PublicMethods
        /// <summary>
        /// Runs synchronously if the post does not trigger a datawrite.
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public void PostRecord(DataRow dataRow)
        {
            //scan for dbnull
            ReplaceDbNullStringRepresentations(dataRow);
            m_DataTable.Rows.Add(dataRow);
            if (++m_NumRowsInBuffer >= m_BufferSize)
            {
                m_NumRowsInBuffer = 0;
                WriteRecords();
            }
        }

        private void ReplaceDbNullStringRepresentations(DataRow dataRow)
        {
            if (m_DbNullStringValue != null)
            {
                for (int i = 0; i < dataRow.ItemArray.Length; i++)
                {
                    if (dataRow[i].ToString() == m_DbNullStringValue)
                    {
                        dataRow[i] = DBNull.Value;
                    }
                }
            }
        }

        public void WriteRecords()
        {
            using (SqlBulkCopy copy = new SqlBulkCopy(ConfigVariables.Instance.ConfiguredConnection, SqlBulkCopyOptions.TableLock))
            {
                copy.BulkCopyTimeout = 0;
                copy.DestinationTableName = m_DestinationTableName;
                copy.BatchSize = m_BufferSize;

                foreach (var mapping in m_ColumnMappings)
                {
                    copy.ColumnMappings.Add(mapping);
                }

                copy.WriteToServer(m_DataTable);
            }
            m_DataTable.Clear();
        }

        public DataRow GetEmptyRow()
        {
            return m_DataTable.NewRow();
        }
        #endregion
    }
}
