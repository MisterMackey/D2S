using D2S.Library.Services;
using D2S.Library.Utilities;
using Microsoft.Azure.DataLake.Store;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Extractors
{
    public class ParallelDatalakeFlatFileExtractor
    {
        private readonly PipelineContext m_Context;
        private byte[] m_Buffer1;
        private byte[] m_Buffer2;
        private byte[] m_PointerToActiveBuffer;
        private bool m_Buffer1IsActive;
        private AdlsInputStream m_Stream;
        private Task m_TaskInProgress;
        private int m_OffsetInActiveBuffer;
        private object m_Syncroot;
        private readonly int m_Buffersize;
        private bool EOF;
        public ParallelDatalakeFlatFileExtractor(PipelineContext context)
        {
            m_Context = context;
            m_Buffersize = m_Context.ADLStreamBufferSize;
            m_Buffer1 = new byte[m_Buffersize];
            m_Buffer2 = new byte[m_Buffersize];
            AzureClient client = new AzureClient(context);
            var adls = client.GetDataLakeClient(m_Context.DataLakeAdress, m_Context.PromptAzureLogin);
            m_Stream = adls.GetReadStream(m_Context.SourceFilePath, m_Buffersize);
            m_Syncroot = new object();
            //init buffer
            FillInactiveBuffer().Wait();
            m_PointerToActiveBuffer = m_Buffer1;
            m_Buffer1IsActive = true;

            //start filling the second buffer
            m_TaskInProgress = FillInactiveBuffer();
        }
        /// <summary>
        /// used for debugging
        /// </summary>
        /// <param name="context"></param>
        /// <param name="mockingStream"></param>
        public ParallelDatalakeFlatFileExtractor(PipelineContext context, AdlsInputStream mockingStream)
        {
            m_Context = context;
            int m_Buffersize = m_Context.ADLStreamBufferSize;
            m_Buffer1 = new byte[m_Buffersize];
            m_Buffer2 = new byte[m_Buffersize];
            m_Stream = mockingStream;
            m_Syncroot = new object();
            //init buffer
            FillInactiveBuffer().Wait();
            m_PointerToActiveBuffer = m_Buffer1;
            m_Buffer1IsActive = true;

            //start filling the second buffer
            m_TaskInProgress = FillInactiveBuffer();

        }

        public bool TryExtractLine(out string line)
        {
            line = null;
            bool success = false;

            StringBuilder sb = new StringBuilder();
            byte currChar;
            bool lineEndingFound = false;
            lock (m_Syncroot)
            {
                while (true)
                {
                    //go over buffer until we find an endline or until we reach the end of the buffer
                    //if we reach the end of the buffer, swap the buffers, start a fresh download and continue
                    //callers should NEVER encounter an empty buffer!

                    //grab the current char while incrementing the offset
                    currChar = m_PointerToActiveBuffer[m_OffsetInActiveBuffer++];

                    //are we at the end of the buffer?
                    if (m_OffsetInActiveBuffer == m_Buffersize)
                    {
                        YeOldeBaitAndSwitch(); //this will set offset back to 0
                    }

                    //found line ending or EOF?
                    switch (currChar)
                    {
                        case (byte)'\r':
                            lineEndingFound = true;
                            break;
                        case (byte)'\n':
                            lineEndingFound = true;
                            break;
                        case 0:
                            if (EOF) { lineEndingFound = true; }
                            //else go ahead and write your null values i guess
                            break;
                        default:
                            break;
                    }
                    if (lineEndingFound)
                    {
                        //check for windows style ending and consume that 
                        currChar = m_PointerToActiveBuffer[m_OffsetInActiveBuffer];
                        if (currChar == (byte)'\n')
                        {
                            m_OffsetInActiveBuffer++;
                        }
                        break; //will exit the lock, build the string and return it
                    }
                    else
                    {

                    }
                    
                }                
                
            }
            //check if a string with length > 0 can be built otherwise return false

            return success;
        }

        private void YeOldeBaitAndSwitch()
        {
            //dont forget to deal with the potential of the stream being closed when this code is reached.
        }

        private async Task FillInactiveBuffer()
        {
            int bytesRead;
            if (m_Buffer1IsActive)
            {
                bytesRead = await m_Stream.ReadAsync(m_Buffer2, 0, m_Context.ADLStreamBufferSize);
            }
            else
            {
                bytesRead = await m_Stream.ReadAsync(m_Buffer1, 0, m_Context.ADLStreamBufferSize);
            }
            if (bytesRead < m_Context.ADLStreamBufferSize)
            {
                m_Stream.Close();
                //should set some flag here i guess to indicate EOF otherwise the getline is just gonna read a bunch of nulls the whole time.
                EOF = true;
            }
        }


    }
}
