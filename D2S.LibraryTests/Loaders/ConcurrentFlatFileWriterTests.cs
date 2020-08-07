using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Loaders;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Threading;

namespace D2S.Library.Loaders.Tests
{
    [TestClass()]
    public class ConcurrentFlatFileWriterTests
    {
        private PipelineContext context = new PipelineContext()
        {
            IsTruncatingTable = true,
            DestinationFilePath = @"ConcurrentFlatFileWriterTest.txt",
            TotalObjectsInSequentialPipe = 1000
        };
        private string TestLine = " is the number of this testline";
        [TestMethod()]
        public void WriteLineTest()
        {
            try
            {
                int rowcountPerWriter = 2000;
                int writersThreads = 3;
                ConcurrentFlatFileWriter writer = new ConcurrentFlatFileWriter(context);
                Task[] tasklist = new Task[writersThreads];
                for (int i = 0; i < writersThreads; i++)
                {
                    tasklist[i] = Task.Factory.StartNew(
                        () =>
                        {
                            for (int x = 0; x < rowcountPerWriter; x++)
                            {
                                writer.WriteLine($"Thread with ID: {Thread.CurrentThread.ManagedThreadId} is writing its {x} line: {TestLine}");
                            }
                        });
                }

                Task.WaitAll(tasklist);
                writer.Close();
                var result = File.ReadAllLines(context.DestinationFilePath);

                Assert.AreEqual(expected: writersThreads*rowcountPerWriter, actual: result.Count());
            }
            finally
            {
                File.Delete(context.DestinationFilePath);
            }
        }

        [TestMethod()]
        public void CloseTest()
        {
            try
            {
                ConcurrentFlatFileWriter writer = new ConcurrentFlatFileWriter(context);
                //just testing the closing here when the number of lines written to the buffer is less than the buffersize, not testing concurrent writing here
                int rowcount = 10;
                for (int i = 0; i < rowcount; i++)
                {
                    writer.WriteLine($"{i}{TestLine}");
                }
                writer.Close();
                var result = File.ReadAllLines(context.DestinationFilePath);

                Assert.AreEqual(expected: rowcount, actual: result.Count());
            }
            finally
            {
                File.Delete(context.DestinationFilePath);
            }
        }
    }
}