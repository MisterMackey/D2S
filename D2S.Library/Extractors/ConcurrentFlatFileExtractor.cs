using System;
using System.Collections.Generic;
using D2S.Library.Utilities;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Services;
using Microsoft.Azure.DataLake.Store;

namespace D2S.Library.Extractors
{
    public class ConcurrentFlatFileExtractor
    {
        private readonly IEnumerable<string> m_File;
        private readonly IEnumerator<string> m_Enumerator;
        private readonly object m_SyncRoot;
        private readonly PipelineContext m_context;
        private const int m_DefaultBufferSize = 40 * 1024 *1024;
        StreamReader m_DatalakeReader;
        #region Constructor
        public ConcurrentFlatFileExtractor(PipelineContext context)
        {
            m_context = context;
            m_SyncRoot = new object();
            m_File = InitializeEnumerable(context);
            m_Enumerator = m_File.GetEnumerator();

            //skip extra lines if need
            if (context.SourceFileIsSourcedFromDial)
            {
                //skip twice cuz we reverse the steps for dial (return the line first then move next)
                //we do this to be able to skip the last line in the file
                m_Enumerator.MoveNext();
                m_Enumerator.MoveNext();
            }
            if (context.FirstLineContainsHeaders)
            {
                m_Enumerator.MoveNext();
            }

        }

        //this method returns an ienumberable interface that wraps the underlying resource. For files present in the filesystem, this is the file.readlines method.
        //for data lake resources, a streamreader is opened on the resource and a private method is returned which wraps this reader.
        private IEnumerable<string> InitializeEnumerable(PipelineContext context)
        {
            if (context.TryMatchFileNameInSourceFolderBasedOnRegex)
            {
                var random = context.ColumnNames; ; //this call will force the context to resolve the regex match.
            }

            if (context.IsReadingFromDataLake)
            {
                AzureClient clientService = new AzureClient(context);
                var client = clientService.GetDataLakeClient(context.DataLakeAdress, context.PromptAzureLogin);
                //see if we need to set the filename dynamically

                VerifyAccessRights(client, context.SourceFilePath);
                VerifyFileExists(client, context.SourceFilePath);

                m_DatalakeReader = new StreamReader(client.GetReadStream(context.SourceFilePath, m_DefaultBufferSize));

                return readLineDataLake();
            }
            else
            {
                return File.ReadLines(context.SourceFilePath);
            }
        }
        #endregion

        public bool TryExtractLine(out string line)
        {
            bool success = false;
            line = null;
            if (!m_context.SourceFileIsSourcedFromDial)
            {
                lock (m_SyncRoot)
                {
                    if (m_Enumerator.MoveNext())
                    {
                        line = m_Enumerator.Current;
                        success = true;
                    }
                }
                
            }
            else
            {
                lock (m_SyncRoot)
                {
                    line = m_Enumerator.Current;
                    if (m_Enumerator.MoveNext())
                    {
                        success = true;
                    }
                    else
                    {
                        line = null;//this essentially skips the last line.
                    }
                }
            }
            return success;
        }

        private void VerifyAccessRights(AdlsClient client, string sourceFilePath)
        {
            if (client.CheckAccess(sourceFilePath, "r--"))
            {
                //no action required
            }
            else
            {
                var message = $"Insufficient access rights, read access is not given for this file: {sourceFilePath}";
                LogService.Instance.Fatal(message);
                throw new UnauthorizedAccessException(message);
            }
        }

        private void VerifyFileExists(AdlsClient client, string sourceFilePath)
        {
            if (client.CheckExists(sourceFilePath))
            {
                //no action required
            }
            else
            {
                var message = $"File not found: {sourceFilePath}";
                LogService.Instance.Fatal(message);
                throw new FileNotFoundException(message);
            }
        }
        //this method wraps the reader.readline function of the udnerlying streamreader in an ienumberable interface.
        private IEnumerable<string> readLineDataLake()
        {
            string line;

            while ((line = m_DatalakeReader.ReadLine()) != null)
            {
                yield return line;
            }
            m_DatalakeReader.Close();
        }
    }
}
