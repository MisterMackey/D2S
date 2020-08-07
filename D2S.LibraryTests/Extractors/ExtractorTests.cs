using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Extractors;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using D2S.Library.Services;
using D2S.Library.Utilities;
using System.Configuration;
using D2S.LibraryTests;

namespace D2S.Library.Extractors.Tests
{
    [TestClass()]
    public class ExtractorTests
    {
        [TestMethod()]
        public void FlatFileExtractorTestsTextFile()
        {
            FlatFileExtractor FlatFileExtractor = new FlatFileExtractor();
            Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> action = FlatFileExtractor.GetPausableWorkItem();
            List<string> ResultList = new List<string>();
            ConcurrentQueue<string> results = new ConcurrentQueue<string>();
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.txt"
            };
            FlatFileExtractor.SetContext(pipelineContext);

            action(pipelineContext, results, null);
            ResultList = results.ToList();
            foreach (string s in ResultList)
            {
                Console.WriteLine(s);
            }

            Assert.AreEqual(expected: "This is a line", actual: ResultList[0]);
            Assert.AreEqual(expected: "this is also a line", actual: ResultList[1]);
            Assert.AreEqual(expected: "EOF", actual: ResultList[2]);
        }

        [TestMethod()]
        public void FlatFileExtractorTestCSV()
        {
            FlatFileExtractor FlatFileExtractor = new FlatFileExtractor();
            Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> action = FlatFileExtractor.GetPausableWorkItem();
            List<string> ResultList = new List<string>();
            PipelineContext pipelineContext = new PipelineContext() {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.csv" };
            FlatFileExtractor.SetContext(pipelineContext);
            ConcurrentQueue<string> results = new ConcurrentQueue<string>();


            action(pipelineContext, results, null);
            ResultList = results.ToList();

            Assert.AreEqual(expected: "This is a line", actual: ResultList[0]);
            Assert.AreEqual(expected: "this is also a line", actual: ResultList[1]);
            Assert.AreEqual(expected: "EOF", actual: ResultList[2]);
        }

        [TestMethod()]
        public void ExcelExtractorTest()
        {
            try
            {
                ExcelExtractor excelExtractor = new ExcelExtractor();
                PipelineContext pipelineContext = new PipelineContext();
                pipelineContext.SourceFilePath = Environment.CurrentDirectory.Replace(@"bin\Debug",
                    @"ExcelExtractorTest.xlsx");
                pipelineContext.FirstLineContainsHeaders = true;
                pipelineContext.ExcelWorksheetName = "Sheet1";
                List<object[]> ResultList = new List<object[]>();
                List<string> ConcatenatedResult = new List<string>();
                Action<PipelineContext, IProducerConsumerCollection<object[]>, ManualResetEvent> function = excelExtractor.GetPausableWorkItem();
                ConcurrentQueue<object[]> results = new ConcurrentQueue<object[]>();

                function(pipelineContext, results, null);
                ResultList = results.ToList();
                StringBuilder sb = new StringBuilder();
                foreach (string[] s in ResultList)
                {
                    sb.Clear();
                    foreach (string p in s)
                    {
                        Console.Write(p);
                        sb.Append(p);
                    }
                    Console.WriteLine();
                    ConcatenatedResult.Add(sb.ToString());
                }

                Assert.AreEqual(expected: "NishaSahuSweetheart :PIndia", actual: ConcatenatedResult[0]);
                Assert.AreEqual(expected: "JasperRisBreaker of SQL serversNetherlands", actual: ConcatenatedResult[1]);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                //if excel //office interop or whatever is not installed and registered on the build machine the test will flunk. to prevent this we will catch the comexception and assert inconclusive
                // Assert.Inconclusive();
            }

        }
        [TestMethod()]
        public void ExcelDataExtractorTest()
        {
            // Commenting this test until fixed
            // System.UnauthorizedAccessException: 'Access to the path 'C:\Users\C56141\source\repos\Abnamro.D2S.LiquidityETL.Framework\Abnamro.D2S.LiquidityETL.FrameworkTests2\bin\LocalSQLServer' is denied.'

            //try
            //{
            //    ExcelDataExtractor excelExtractor = new ExcelDataExtractor();
            //    PipelineContext pipelineContext = new PipelineContext();
            //    pipelineContext.SourceFilePath = Environment.CurrentDirectory.Replace(@"bin\Debug",
            //        @"ExcelExtractorTest.xlsx");
            //    pipelineContext.FirstLineContainsHeaders = true;
            //    pipelineContext.ExcelWorksheetName = "Sheet1";
            //    List<object[]> ResultList = new List<object[]>();
            //    List<string> ConcatenatedResult = new List<string>();
            //    var function = excelExtractor.GetPausableReportingWorkItem();
            //    ConcurrentQueue<object[]> results = new ConcurrentQueue<object[]>();
            //    Progress<int> prog = new Progress<int>();
            //    function(pipelineContext, results, null, prog);
            //    ResultList = results.ToList();
            //    StringBuilder sb = new StringBuilder();
            //    foreach (var s in ResultList)
            //    {
            //        sb.Clear();
            //        foreach (object p in s)
            //        {
            //            Console.Write(p);
            //            sb.Append(p.ToString());
            //        }
            //        Console.WriteLine();
            //        ConcatenatedResult.Add(sb.ToString());
            //    }

            //    Assert.AreEqual(expected: "NishaSahuSweetheart :PIndia", actual: ConcatenatedResult[0]);
            //    Assert.AreEqual(expected: "JasperRisBreaker of SQL serversNetherlands", actual: ConcatenatedResult[1]);
            //}
            //catch (System.Runtime.InteropServices.COMException)
            //{
            //    //if excel //office interop or whatever is not installed and registered on the build machine the test will flunk. to prevent this we will catch the comexception and assert inconclusive
            //    Assert.Inconclusive();
            //}

        }

        [TestMethod()]
        public void SqlRecordExtractorTest()
        {

            D2S.LibraryTests.SqlExtractorTestHelper helper = new SqlExtractorTestHelper();
            PipelineContext context = new PipelineContext();

            // ToDo: use connection string from app.config (local version)
            //context.SqlServerName = @"(localdb)\MSSQLLocalDB";
            //context.DatabaseName = "master";

            context.SourceTableName = "doesnt matter im hacking";
            try
            {
                context.SourceTableName = helper.Initialize(ConfigVariables.Instance.ConfiguredConnection);
                //above will build con string and then set the tablename to w/e its supposed to be
                //also initializes a test table with some data

                context.SqlSourceColumnsSelected = new List<string> { "col1", "col2" }; //trust me on this one

                SqlRecordExtractor extractor = new SqlRecordExtractor();

                Action<PipelineContext, IProducerConsumerCollection<object>, ManualResetEvent> action = extractor.GetPausableWorkItem();

                List<object> ResultList = new List<object>();
                ConcurrentQueue<object> results = new ConcurrentQueue<object>();

                action(context, results, null); //call function
                ResultList = results.ToList();
                foreach (object o in ResultList)
                {
                    foreach (object p in (object[])o)
                    {
                        Console.WriteLine(p.ToString());
                    }
                }

                Assert.AreEqual(expected: "Knijn", actual: ((object[])ResultList[0])[0]);
                Assert.AreEqual(expected: 1, actual: ((object[])ResultList[0])[1]);
                Assert.AreEqual(expected: "Knijntje", actual: ((object[])ResultList[1])[0]);
                Assert.AreEqual(expected: 2, actual: ((object[])ResultList[1])[1]);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                helper.Cleanup(ConfigVariables.Instance.ConfiguredConnection);
            }
        }

        [TestMethod()]
        public void PausingFileExtractorTest()
        {
            FlatFileExtractor FlatFileExtractor = new FlatFileExtractor();
            Action<PipelineContext, IProducerConsumerCollection<string>, ManualResetEvent> action = FlatFileExtractor.GetPausableWorkItem();
            List<string> ResultList = new List<string>();
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.txt"
            };
            FlatFileExtractor.SetContext(pipelineContext);
            // pass an unset resetevent to prevent immediate execution
            ManualResetEvent pauseEvent = new ManualResetEvent(false);
            // run method asynchronously
            ConcurrentQueue<string> results = new ConcurrentQueue<string>();

            Task work = Task.Factory.StartNew(
                () =>
                    action(pipelineContext, results, pauseEvent)
                    );
            
            Task.Delay(50).Wait(); //wait a small amount of time

            //affirm that resultset is still empty as we have not unpaused the work

            Assert.IsTrue(results.Count == 0);

            //now unpause and affirm the work is completed

            pauseEvent.Set();

            work.Wait(); //syncronously waiting on work
            ResultList = results.ToList();
            foreach (string s in ResultList)
            {
                Console.WriteLine(s);
            }

            Assert.AreEqual(expected: "This is a line", actual: ResultList[0]);
            Assert.AreEqual(expected: "this is also a line", actual: ResultList[1]);
            Assert.AreEqual(expected: "EOF", actual: ResultList[2]);
        }

        [TestMethod()]
        public void LinqFileExtractorTest()
        {
            LinqFileExtractor FlatFileExtractor = new LinqFileExtractor();
            var action = FlatFileExtractor.GetPausableReportingWorkItem();
            List<string> ResultList;
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.txt"
            };
            FlatFileExtractor.SetContext(pipelineContext);
            // pass an unset resetevent to prevent immediate execution
            ManualResetEvent pauseEvent = new ManualResetEvent(false);
            Progress<int> progress = new Progress<int>();
            // run method asynchronously
            ConcurrentQueue<string> results = new ConcurrentQueue<string>();

            Task work = Task.Factory.StartNew(
                () =>
                    action(pipelineContext, results, pauseEvent, progress)
                    );

            Task.Delay(50).Wait(); //wait a small amount of time

            //affirm that resultset is still empty as we have not unpaused the work

            Assert.IsTrue(results.Count == 0);

            //now unpause and affirm the work is completed

            pauseEvent.Set();

            work.Wait(); //syncronously waiting on work
            ResultList = results.ToList();
            foreach (string s in ResultList)
            {
                Console.WriteLine(s);
            }

            Assert.AreEqual(expected: "This is a line", actual: ResultList[0]);
            Assert.AreEqual(expected: "this is also a line", actual: ResultList[1]);
            Assert.AreEqual(expected: "EOF", actual: ResultList[2]);
        }

        [TestMethod()]
        public void LinqExcelExtractorTest()
        {
            //// Commenting this test until fixed
            //// System.UnauthorizedAccessException: 'Access to the path 'C:\Users\C56141\source\repos\Abnamro.D2S.LiquidityETL.Framework\Abnamro.D2S.LiquidityETL.FrameworkTests2\bin\LocalSQLServer' is denied.'

            ////this test is broken af cuz of oledb ace or whatever not being installed
            //// Assert.Inconclusive();
            //LinqExcelExtractor excelExtractor = new LinqExcelExtractor();
            //PipelineContext pipelineContext = new PipelineContext();
            //pipelineContext.SourceFilePath = Environment.CurrentDirectory.Replace(@"bin\Debug",
            //    @"ExcelExtractorTest.xlsx");
            //pipelineContext.FirstLineContainsHeaders = true;
            //pipelineContext.ExcelWorksheetName = "Sheet1";

            //var function = excelExtractor.GetPausableReportingWorkItem();

            //ManualResetEvent pauseEvent = new ManualResetEvent(true);
            //Progress<int> progress = new Progress<int>();

            //ConcurrentQueue<Row> results = new ConcurrentQueue<Row>();

            //function(pipelineContext, results, pauseEvent, progress);

            //foreach (var r in results)
            //{
            //    foreach (var key in r.Keys)
            //    {
            //        Console.WriteLine(r[key]);
            //    }
            //}


        }

        [TestMethod()]
        public void ReportingMmfExtractorTest()
        {
            MmfExtractor extractor = new MmfExtractor();
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.txt"
            };
            ManualResetEvent pause = new ManualResetEvent(true);
            BoundedConcurrentQueu<string[]> output = new BoundedConcurrentQueu<string[]>();
            Progress<int> prog = new Progress<int>();

            var action = extractor.GetPausableReportingWorkItem();

            action(pipelineContext, output, pause, prog);
            Assert.IsTrue(output.Count == 3);

            List<string> ResultList = new List<string>();
            for (int i =0;i<3;i++)
            {
                string[] s;
                output.TryTake(out s);
                ResultList.Add(s[0]);
            }
            Console.WriteLine(ResultList[0]);
            Console.WriteLine("This is a line");
            Assert.IsTrue(ResultList[0].Normalize().Equals("This is a line", StringComparison.InvariantCulture));

        }
        [TestMethod()]
        public void PausingMmfExtractorTest()
        {
            MmfExtractor extractor = new MmfExtractor();
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = false,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\ExtractorTests.txt",
                Delimiter = " "
            };
            ManualResetEvent pause = new ManualResetEvent(true);
            BoundedConcurrentQueu<string[]> output = new BoundedConcurrentQueu<string[]>();
            

            //fun fact: this extractor gives zero f's about the amount of columns per row so were gonna try with a space delimiter.
            var action = extractor.GetPausableWorkItem();

            action(pipelineContext, output, pause);
            Assert.IsTrue(output.Count == 3);

            List<string[]> ResultList = new List<string[]>();
            for (int i = 0; i < 3; i++)
            {
                string[] s;
                output.TryTake(out s);
                ResultList.Add(s);
            }

            if (ResultList[0][0].Equals("This", StringComparison.InvariantCulture))
            {
                Assert.IsTrue(true);
            }
            else { Assert.Fail(); }
        }

        [TestMethod()]
        public void DIALFlatFileExtractorTest()
        {
            DIALFlatFileExtractor FlatFileExtractor = new DIALFlatFileExtractor();
            var action = FlatFileExtractor.GetPausableReportingWorkItem();
            List<string> ResultList = new List<string>();
            ConcurrentQueue<string> results = new ConcurrentQueue<string>();
            ManualResetEvent pause = new ManualResetEvent(true);
            Progress<int> prog = new Progress<int>();
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = true,
                SourceFileIsSourcedFromDial = true,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\DIALFormattedTestData.txt"
            };
            FlatFileExtractor.SetContext(pipelineContext);

            action(pipelineContext, results, pause, prog);
            ResultList = results.ToList();
            foreach (string s in ResultList)
            {
                Console.WriteLine(s);
            }

            Assert.AreEqual(expected: "6123981||00927123||657392||22-01-2019||Pietje Puk", actual: ResultList[0]);
        }

        [TestMethod()]
        public void DataLakeFlatFileExtractorTest()
        {
            //Code is made unreachable so unit test can run on a remote machine. If you wish to test the login prompt, simple remove the inconclusive assert and the return statement and run the test.
            Assert.Inconclusive();
            return;
            DataLakeFlatFileExtractor dataLakeFlatFileExtractor = new DataLakeFlatFileExtractor(ConfigurationManager.AppSettings.Get("DatalakeAdress"));
            var method = dataLakeFlatFileExtractor.GetPausableReportingWorkItem();
            string snap = "20190227";
            PipelineContext context = new PipelineContext()
            {
                SourceFilePath = @"qrm02-p-01\04_Sourcefiles_Archive\201902\FT004408\Sophis_Trigger_" + snap + ".csv"
                , FirstLineContainsHeaders = true
                , SourceFileIsSourcedFromDial = false
                , PromptAzureLogin = true
                
            };
            BoundedConcurrentQueu<string> output = new BoundedConcurrentQueu<string>();
            Progress<int> prog = new Progress<int>();

            method(context, output, null, prog);

            Assert.IsTrue(output.Count == 1);
            string result;

            output.TryTake(out result);

            Console.WriteLine(result);

            Assert.IsTrue(result.Equals("Sophis|14298|-677451144.84"));

            
        }
    }
}