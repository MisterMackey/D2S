using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Services;
using D2S.Library.Utilities;
using Microsoft.Azure.DataLake.Store;

namespace D2S.Library.Extractors
{
    public class DataLakeFlatFileExtractor : Extractor<String, int>
    {

        public DataLakeFlatFileExtractor(string datalakeName)
        {

        }

        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> PausableWorkItem => throw new NotImplementedException();

        protected override Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReadFileFromAzureDatalake;

        private void ReadFileFromAzureDatalake(PipelineContext context, IProducerConsumerCollection<string> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            if (context == null || output == null || progress == null)
            {
                var outputMessage = "One or more parameters are null";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }
            // create an instance of the azureclient class in order to obtain a connection to datalake
            AzureClient clientService = new AzureClient(context);
            var client = clientService.GetDataLakeClient(context.DataLakeAdress, context.PromptAzureLogin);
            //see if we need to set the filename dynamically
            
            if (context.TryMatchFileNameInSourceFolderBasedOnRegex)
            {
                var random = context.ColumnNames; ; //this call will force the context to resolve the regex match.
            }
            VerifyAccessRights(client, context.SourceFilePath);
            VerifyFileExists(client, context.SourceFilePath);
            
            //we use & instead of && operator because we want to make sure the hasaccess variable is set correctly
                using (StreamReader Reader = new StreamReader(client.GetReadStream(context.SourceFilePath)))
                {
                    string line;
                    int progressCounter = 0;

                    if (context.SourceFileIsSourcedFromDial)
                    {
                        Reader.ReadLine();
                        Reader.ReadLine();
                    }
                    else
                    {
                        if (context.FirstLineContainsHeaders)
                        {
                            Reader.ReadLine();
                        }
                        //else no action needed
                    }
                    if (pauseEvent == null)
                    {
                        while ((line = Reader.ReadLine()) != null)
                        {
                            output.TryAdd(line);
                            progressCounter++;
                            if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                        }
                    }
                    else
                    {
                        while ((line = Reader.ReadLine()) != null)
                        {
                            pauseEvent.WaitOne();
                            output.TryAdd(line);
                            progressCounter++;
                            if (progressCounter % 1000 == 0) { progress.Report(progressCounter); }
                        }
                    }
                    progress.Report(progressCounter);
                }

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
    }
}
