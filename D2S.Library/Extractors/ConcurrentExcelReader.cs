using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using System.IO;

namespace D2S.Library.Extractors
{
    public class ConcurrentExcelReader :IDisposable
    {
        #region Private Fields and methods
        private readonly PipelineContext m_Context;
        private readonly Stream m_Stream;
        private readonly IExcelDataReader m_Reader;
        private readonly object m_SyncronizationObject;
        private bool m_CanRead;

        #endregion
        #region Constructor
        public ConcurrentExcelReader(PipelineContext context)
        {
            m_Context = context;
            //Open a stream on the file that we wanna read and create an exceldatareader
            m_Stream = File.Open(m_Context.SourceFilePath, FileMode.Open, FileAccess.Read);
            m_Reader = ExcelReaderFactory.CreateReader(m_Stream);
            //the following snippet loops over all the sheets in the file until it finds the one we want and 
            //throws and exception if it isn't there.
            bool FoundSheet = false;
            do
            {
                if (m_Reader.Name.Equals(m_Context.ExcelWorksheetName, StringComparison.InvariantCultureIgnoreCase))
                {
                    FoundSheet = true;
                    break;
                }
            } while (m_Reader.NextResult());
            if (!FoundSheet)
            {
                throw new IOException($"Worksheet by name {m_Context.ExcelWorksheetName} was not found");
            }

            m_SyncronizationObject = new object();
            m_CanRead = true;

            //extra safety, should be redundant but may come in handy if we every move all methods out of the pipelinecontext
            if (m_Context.FirstLineContainsHeaders)
            {
                string[] header = new string[m_Reader.FieldCount];
                m_Reader.Read();
                for (int i = 0; i < m_Reader.FieldCount; i++)
                {
                    header[i] = m_Reader.GetValue(i).ToString();
                }
                m_Context.ColumnNames = header;
            }
        }
        #endregion
        #region Public Fields and methods
        public bool TryExtractRecord(out string[] line)
        {
            bool ret = false;
            line = null;
            //enter lock, exit after we are done with the exceldatareader
            lock (m_SyncronizationObject)
            {
                //if another thread closed the reader while we were waiting for the lock we want to return on false right away
                if (!m_CanRead) { return ret; }
                //otherwise go ahead and try to read
                ret = m_Reader.Read();
                if (ret)
                {
                    //if the read succeeded we will populate a new string array with the values and assign it to out line
                    line = new string[m_Reader.FieldCount];
                    for (int i = 0; i < m_Reader.FieldCount; i++)
                    {
                        var value = m_Reader.GetValue(i);
                        if (value != null)
                        {
                            line[i] = value.ToString();
                        }
                        else
                        {
                            line[i] = string.Empty;
                        }
                    }
                }
                else
                //otherwise we dispose
                {
                    this.Dispose();
                } 
            }

            return ret;
        }

        public void Dispose()
        {
            m_CanRead = false;
            m_Reader.Dispose();
            m_Stream.Dispose();
        }
        #endregion
    }
}
