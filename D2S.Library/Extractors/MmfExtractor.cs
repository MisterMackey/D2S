using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using D2S.Library.Helpers;

namespace D2S.Library.Extractors
{
    public class MmfExtractor : Extractor<string[], int>
    {
        private long m_Buffer;
        private readonly Encoding m_encoding;
        private const int byteArraySize = 50000;
        private int m_LatestOffset;
        public MmfExtractor() : this(500, Encoding.UTF8)
        {

        }
        public MmfExtractor(long megabytesToAllocate) : this(megabytesToAllocate, Encoding.UTF8)
        {
            
        }
        public MmfExtractor(Encoding encoding) : this(500, encoding)
        {

        }
        public MmfExtractor(long megabutestToAllocate, Encoding encoding)
        {
            m_Buffer = megabutestToAllocate;
            m_encoding = encoding;
        }
        protected override Action<PipelineContext, IProducerConsumerCollection<string[]>, ManualResetEvent> PausableWorkItem => DoPausableWork;

        protected override Action<PipelineContext, IProducerConsumerCollection<string[]>, ManualResetEvent, IProgress<int>> ReportingWorkItem => DoReportingWork;
        #region DoReportingWork

        private void DoReportingWork(PipelineContext context, IProducerConsumerCollection<string[]> output, ManualResetEvent pause, IProgress<int> progress)
        {
            string filepath = context.SourceFilePath;
            Encoding encodeingToUse = m_encoding;
            char delim;
            if (context.Delimiter.Length == 1)
            {
                delim = context.Delimiter.ToCharArray()[0]; 
            }
            else
            {
                throw new InvalidCastException("MmfExtractor only supports single char delimiters");
            }
            int columncount = context.ColumnNames.Count();

            MemoryMappedFileSecurity security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new System.Security.AccessControl.AccessRule<MemoryMappedFileRights>("everyone", MemoryMappedFileRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));

            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                m_Buffer = m_Buffer > fs.Length ? fs.Length : m_Buffer;
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, "D2SMMF", m_Buffer, MemoryMappedFileAccess.Read, security, HandleInheritability.Inheritable, true))
                {
                    using (MemoryMappedViewStream view = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                    {
                        byte[] charArray = new byte[byteArraySize]; //initialize a bytearray to read bytes into from the stream
                        long length = view.Length; //determine how long the stream is
                        long currentPosition = 0; //determine where in the stream we are
                        //determine the offset when reading a new chunk of bytes, we need to do this because we cannot guarentee that the last byte is the end of a row
                        //if it isn't then the remainder will be placed at the beginning of the array and the offset will be updated so it is not overwritten when the next batch of bytes is read from the stream
                        int offset = 0;
                        while (currentPosition < length)
                        {
                            currentPosition +=
                                view.Read(charArray, offset, byteArraySize - offset);
                            foreach (string[] line in GetStrings(ref charArray, encodeingToUse, delim, columncount))
                            {
                                output.TryAdd(line);
                            }
                            offset = m_LatestOffset;
                        }
                        //when we break out of this loop we will be left with a char array holding the last line (which wont have an end of line character so getstrings() did not append it to the result list
                        //we know that the length of this line is m_latestoffset so we can simply read it and add it before closing all resources and completing the task
                        char[] rawlastRow = encodeingToUse.GetChars(charArray, 0, m_LatestOffset);
                        //we're gonna have a bunch of null chars /0 in here so were just gonna trim those out rly quick
                        char[] lastRow = TrimNulls(rawlastRow);
                        string lastRowAsString = new string(lastRow);
                        string[] finalResult = lastRowAsString.Split(delim);
                        output.TryAdd(finalResult);

                    }
                }
            }
        }
        private IEnumerable<string[]> GetStrings(ref byte[] input, Encoding encoding, char delim, int count)
        {
            List<string[]> returnlist = new List<string[]>(100); //initialize returnval
            string[] currRow = new string[count]; //current row to build
            char[] currInput = encoding.GetChars(input); //convert raw input to characterset
            int offset = 0; //tracks the position in the chararray from where we start reading when constructing a new field
            int currRowPos = 0; //tracks the index of the column we are going to write the next field to
            int lineOffset = 0; //counts the position in the char array so we know where the remainder starts
            int lengthOfCurrentLine = 0; //to count the length of the line currently being built
            for (int i = 0; i < currInput.Count(); i++)
            {
                if (currInput[i].Equals(delim))
                {
                    //construct char array out of the field
                    char[] aboutToBeAString = new char[lengthOfCurrentLine];
                    int iter = 0;
                    foreach (char c in currInput.Skip(offset).Take(i - offset))
                    {
                        aboutToBeAString[iter++] = c;
                    }
                    //build the field and append to line
                    currRow[currRowPos] = new string(aboutToBeAString);
                    offset = i + 1; //+ 1 to skip the delimiter
                    currRowPos++;
                }
                //i assume windows style line endings, so /r/n or char 13 followed by char 10
                else if (currInput[i] == 13 && currInput[i + 1] == 10)
                {
                    //line ending!
                    //add the final field to currrow
                    //
                    char[] aboutToBeAString = new char[lengthOfCurrentLine];
                    int iter = 0;
                    foreach (char c in currInput.Skip(offset).Take(i - offset))
                    {
                        aboutToBeAString[iter++] = c;
                    }
                    //build the last field in the line
                    currRow[currRowPos] = new string(aboutToBeAString);
                    //reset position in the row for the next row
                    currRowPos = 0;
                    offset = i + 2; //consume both chars /r/n
                    lineOffset = i + 2; //set lineoffset so we can determine the remainder of the byte array once we have extracted all full lines
                    returnlist.Add(currRow); //add to returnlist
                    currRow = new string[count]; //make new row for next input
                    lengthOfCurrentLine = 0; //reset this
                    i += 2; //set the iterator for the forloop forward by 2 so that it skips over the /n char and starts at the beginning of the next row.
                }
                lengthOfCurrentLine++; //increment this if we read a character that isnt a delimter or an end of line
            }
            //at the end of this loop we will have some characters left, we leave these in the start of the input array which will be reused.
            //we take the part of currinput that we havent converted to string yet and convert it back to a byte array which we will then put into the start of the input variable.
            byte[] remainder = encoding.GetBytes((currInput
                .Skip(lineOffset)
                .Take(byteArraySize - lineOffset)).ToArray());



            for (int i = 0; i < remainder.Count(); i++)
            {
                input[i] = remainder[i];
            }
            m_LatestOffset = remainder.Count(); // set remainder count
            lengthOfCurrentLine = 0; //reset this
            return returnlist;
        }

        private char[] TrimNulls(char[] input)
        {
            int length = 0;
            while (input[length] != 0)
            {
                length++;
            }
            //now that we have determined the length, we take that amount of chars starting from index 0 and voila
            char[] returnval = new char[length];

            for (int i = 0; i < length; i++)
            {
                returnval[i] = input[i];
            }
            return returnval;
        }

        #endregion

        #region DoPausableWork
        private void DoPausableWork(PipelineContext context, IProducerConsumerCollection<string[]> output, ManualResetEvent pauseEvent)
        {
            string filepath = context.SourceFilePath;
            long capacity = m_Buffer;
            char delim;
            if (context.Delimiter.Length == 1)
            {
                delim = context.Delimiter.ToCharArray()[0];
            }
            else
            {
                throw new InvalidCastException("MmfExtractor only supports single char delimiters");
            }
            string s_delim = context.Delimiter.ToString();
            int positionInByteArray = 0;
            //check if capacity isnt too high
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length < capacity) { capacity = fs.Length; }
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(filepath, FileMode.Open, "D2SMMF", capacity, MemoryMappedFileAccess.Read))
            {
                using (MemoryMappedViewStream view = mmf.CreateViewStream(0, capacity, MemoryMappedFileAccess.Read))
                {
                    byte[] currentChunk = new byte[byteArraySize];
                    //figure out how many bytes we can read into the array (i.e. will we reach the end of the stream before the array is full or not)
                    int bytesToRead = 0;
                    //while the end of stream isnt reached...
                    while (view.Position < view.Length)
                    {
                        //check how many bytes we read (max of the size of array)
                        if ((view.Length - view.Position) < byteArraySize) { bytesToRead =(int)( view.Length - view.Position); } else { bytesToRead = byteArraySize; }
                        //then read them
                        view.Read(currentChunk, m_LatestOffset, bytesToRead - m_LatestOffset);
                        //then loop over the array until a line break is encountered, extract the string and split it up.
                        for (int i =0; i < byteArraySize; i++)
                        {
                            if (currentChunk[i] == 10) //10 is /n char
                            {
                                // it might be that the previous character is a /r char, we dont want this in the result string so if this is the case we will 
                                //read one less byte
                                byte[] aboutToBeAString;
                                if (currentChunk[i-1] == 13)
                                {
                                    aboutToBeAString = currentChunk.Skip(positionInByteArray).Take(i - positionInByteArray - 1).ToArray();
                                }
                                else
                                {
                                    aboutToBeAString = currentChunk.Skip(positionInByteArray).Take(i - positionInByteArray).ToArray();
                                }
                                positionInByteArray = i + 1;
                                m_LatestOffset = byteArraySize - (i+1); //the amount of bytes left in the array that havent been read and converted to strings
                                //convert the bytearray to unicode if it isn't already and then make a string out of it
                                string currentLine;
                                if (!m_encoding.EncodingName.Equals("Unicode"))
                                {
                                    currentLine = Encoding.Unicode.GetString(
                                        Encoding.Convert(m_encoding, Encoding.Unicode, aboutToBeAString));
                                }
                                else
                                {
                                    currentLine = m_encoding.GetString(aboutToBeAString);
                                }
                                string[] currentRow = StringAndText.SplitRow(currentLine, s_delim, "\\", false);
                                output.TryAdd(currentRow);
                            }
                            //dont bother once null terminators are reached (well they arent null terminators but w/e)
                            else if (currentChunk[i] == 0)
                            {
                                break;
                            }
                        }
                        // place remainder back at start of array
                        int index = 0;
                        for (int i = positionInByteArray; i < byteArraySize; i++)
                        {
                            currentChunk[index] = currentChunk[i];
                            index++;
                        }
                    }
                    //when we break out of this loop we will have some remainder left as the last line often is not terminated by a line break. we handle that remainder here.
                    byte[] aboutToBeLastString = currentChunk.TakeWhile(b => b != 0).ToArray(); //take bytes untill /null is encountered
                    string lastLine;
                    if (!m_encoding.EncodingName.Equals("Unicode"))
                    {
                        lastLine = Encoding.Unicode.GetString(
                            Encoding.Convert(m_encoding, Encoding.Unicode, aboutToBeLastString));
                    }
                    else
                    {
                        lastLine = m_encoding.GetString(aboutToBeLastString);
                    }
                    string[] lastRow = StringAndText.SplitRow(lastLine, s_delim, "\\", false);
                    output.TryAdd(lastRow);
                }
            }
        }
        #endregion
    }
}
