using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Threading;

namespace D2S.Library.Extractors.Tests
{
    [TestClass()]
    public class ConcurrentFlatFileExtractorTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumns.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.BasicSequentialPipeline",
            IsCreatingTable = true,
            IsDroppingTable = true,
            CpuCountUsedToComputeParallalism = 4
        };
        private PipelineContext dialContext = new PipelineContext()
        {
            SourceFilePath = @"..\..\..\D2S.LibraryTests\TestDataFiveColumnsDial.txt",
            Delimiter = "||",
            DestinationTableName = "dbo.BasicSequentialPipelineDial",
            IsCreatingTable = true,
            IsDroppingTable = true,
            SourceFileIsSourcedFromDial = true,
            CpuCountUsedToComputeParallalism = 4
        };
        [TestMethod()]
        public void TryExtractLineTest()
        {
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(context);

            string line;
            while (reader.TryExtractLine(out line))
            {
                output.TryAdd(line);
            }
            Assert.IsTrue(output.Count == 3);
        }
        [TestMethod()]
        public void TryExtractLineConcurrentlyTest()
        {
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(context);
            Task[] tasklist = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                object[] ob = new object[] { reader, output };
                tasklist[i] = new Task(obj
                    =>
                {
                    object[] objarray = obj as object[];
                    ConcurrentFlatFileExtractor r = objarray[0] as ConcurrentFlatFileExtractor;
                    BoundedConcurrentQueu<string> o = objarray[1] as BoundedConcurrentQueu<string>;
                    string l;
                    while (r.TryExtractLine(out l))
                    {
                        o.TryAdd(l);
                        Thread.Sleep(100); 
                    }
                }, ob);
            }

            for (int i = 0; i < 10; i++)
            {
                tasklist[i].Start();
            }
            Task.WaitAll(tasklist);

            Assert.IsTrue(output.Count == 3);
        }

        [TestMethod()]
        public void TryExtractLineDialTest()
        {
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(dialContext);

            string line;
            while (reader.TryExtractLine(out line))
            {
                output.TryAdd(line);
            }
            Assert.IsTrue(output.Count == 3);
        }
        [TestMethod()]
        public void TryExtractLineConcurrentlyDialTest()
        {
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(dialContext);
            Task[] tasklist = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                object[] ob = new object[] { reader, output };
                tasklist[i] = new Task(obj
                    =>
                {
                    object[] objarray = obj as object[];
                    ConcurrentFlatFileExtractor r = objarray[0] as ConcurrentFlatFileExtractor;
                    BoundedConcurrentQueu<string> o = objarray[1] as BoundedConcurrentQueu<string>;
                    string l;
                    while (r.TryExtractLine(out l))
                    {
                        o.TryAdd(l);
                        Thread.Sleep(100);
                    }
                }, ob);
            }

            for (int i = 0; i < 10; i++)
            {
                tasklist[i].Start();
            }
            Task.WaitAll(tasklist);

            Assert.IsTrue(output.Count == 3);
        }

        [TestMethod]
        public void TryExtractLineDataLake()
        {
            //Code is made unreachable so unit test can run on a remote machine. If you wish to test the login prompt, simple remove the inconclusive assert and the return statement and run the test.
            Assert.Inconclusive();
            return;
            PipelineContext context = new PipelineContext()
            {
                SourceFilePath = @"qrm02-p-01\04_Sourcefiles_Archive\201902\FT004408\Sophis_Trigger_20190227.csv"
                ,
                FirstLineContainsHeaders = true
                ,
                SourceFileIsSourcedFromDial = false
                ,
                PromptAzureLogin = true
                ,
                IsReadingFromDataLake = true
                ,
                DataLakeAdress = ConfigurationManager.AppSettings.Get("DatalakeAdress")
            };
            ConcurrentFlatFileExtractor reader = new ConcurrentFlatFileExtractor(context);
            
            string snap = "20190227";
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            string line;
            while (reader.TryExtractLine(out line))
            {
                output.TryAdd(line);
            }
            

            Assert.IsTrue(output.Count == 1);
            string result;

            output.TryTake(out result);

            Console.WriteLine(result);

            Assert.IsTrue(result.Equals("Sophis|14298|-677451144.84"));

        }
    }
}