using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using System.Data;
using System.Collections.Concurrent;
using System.Threading;
using D2S.Library.Extractors;
using D2S.Library.Services;

namespace D2S.Library.Loaders.Tests
{
    [TestClass()]
    public class LoaderTests
    {
        [TestMethod()]
        public void SQLLoaderTest()
        {
            D2S.LibraryTests.SqlExtractorTestHelper helper = new D2S.LibraryTests.SqlExtractorTestHelper();
            PipelineContext context = new PipelineContext();
            
            // ToDo: use connection string from app.config (local version)
            //context.SqlServerName = @"(localdb)\MSSQLLocalDB";
            //context.DatabaseName = "master";

            context.DestinationTableName = "loaderstest";

            try
            {
                context.DestinationTableName = helper.Initialize(ConfigVariables.Instance.ConfiguredConnection);
                context.ColumnNames = new string[] { "col1", "col2" };

                ConcurrentQueue<Row> row = new ConcurrentQueue<Row>();

                Row newRow = new Row();
                newRow["col1"] = new Tuple<object, Type>("TestValue", typeof(string));
                newRow["col2"] = new Tuple<object, Type>(482, typeof(int));
                
                row.Enqueue(newRow);

                newRow = new Row();
                newRow["col1"] = new Tuple<object, Type>("Hi", typeof(string));
                newRow["col2"] = new Tuple<object, Type>(483, typeof(int));

                row.Enqueue(newRow);

                SQLTableLoader loader = new SQLTableLoader();

                Action<PipelineContext, IProducerConsumerCollection<Row>> action = loader.GetWorkItem();

                action(context, row);

                SqlRecordExtractor reader = new SqlRecordExtractor();

                List<object> Results = new List<object>();
                ConcurrentQueue<object> result = new ConcurrentQueue<object>();
                Action<PipelineContext, IProducerConsumerCollection<object>, ManualResetEvent> readaction = reader.GetPausableWorkItem();
                context.SourceTableName = context.DestinationTableName;
                context.SqlSourceColumnsSelected = new List<string>() { "col1", "col2" };
                readaction(context, result, null);

                Results = result.ToList();
                Assert.AreEqual(expected: "TestValue", actual: ((object[])Results[2])[0]);
                Assert.AreEqual(expected: 482, actual: ((object[])Results[2])[1]);
                Assert.AreEqual(expected: "Hi", actual: ((object[])Results[3])[0]);
                Assert.AreEqual(expected: 483, actual: ((object[])Results[3])[1]);
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
        public void PausingReportingSQLLoaderTest()
        {
            D2S.LibraryTests.SqlExtractorTestHelper helper = new D2S.LibraryTests.SqlExtractorTestHelper();
            PipelineContext context = new PipelineContext();

            // ToDo: use connection string from app.config (local version)
            //context.SqlServerName = @"(localdb)\MSSQLLocalDB";
            //context.DatabaseName = "master";

            context.DestinationTableName = "loaderstest";
            try
            {
                context.DestinationTableName = helper.Initialize(ConfigVariables.Instance.ConfiguredConnection);
                context.ColumnNames = new string[] { "col1", "col2" };
                ConcurrentQueue<Row> input = new ConcurrentQueue<Row>();
                Row newRow = new Row();
                newRow["col1"] = new Tuple<object, Type>("TestValue", typeof(string));
                newRow["col2"] = new Tuple<object, Type>(482, typeof(int));

                input.Enqueue(newRow);

                newRow = new Row();
                newRow["col1"] = new Tuple<object, Type>("Hi", typeof(string));
                newRow["col2"] = new Tuple<object, Type>(483, typeof(int));

                input.Enqueue(newRow);

                SQLTableLoader loader = new SQLTableLoader();

                var action = loader.GetPausableReportingWorkItem();
                ManualResetEvent pauseButton = new ManualResetEvent(true);
                Progress<int> progress = new Progress<int>();

                Task work = Task.Factory.StartNew(() => action(context, input, pauseButton, progress));
                //wait for work to finish
                while (!input.IsEmpty)
                {
                    Task.Delay(200).Wait();
                }
                loader.SignalCompletion();

                work.Wait();

                //confirm that it worked
                SqlRecordExtractor reader = new SqlRecordExtractor();

                List<object> Results = new List<object>();
                ConcurrentQueue<object> result = new ConcurrentQueue<object>();
                Action<PipelineContext, IProducerConsumerCollection<object>, ManualResetEvent> readaction = reader.GetPausableWorkItem();
                context.SourceTableName = context.DestinationTableName;
                context.SqlSourceColumnsSelected = new List<string>() { "col1", "col2" };
                readaction(context, result, null);

                Results = result.ToList();
                Assert.AreEqual(expected: "TestValue", actual: ((object[])Results[2])[0]);
                Assert.AreEqual(expected: 482, actual: ((object[])Results[2])[1]);
                Assert.AreEqual(expected: "Hi", actual: ((object[])Results[3])[0]);
                Assert.AreEqual(expected: 483, actual: ((object[])Results[3])[1]);


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
    }
}