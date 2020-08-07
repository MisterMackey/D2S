using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Transformers;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using D2S.Library.Utilities;
namespace D2S.Library.Transformers.Tests
{
    [TestClass()]
    public class TransformerTests
    {
        int totalrecords; //to be used for progress monitoring

        [TestMethod()]
        public void StringSplitterTest()
        {
            StringSplitter stringSplitter = new StringSplitter("\"");
            stringSplitter.Delimiter = ",";

            ConcurrentQueue<string> input = new ConcurrentQueue<string>();
            ConcurrentQueue<object[]> output = new ConcurrentQueue<object[]>();
            ManualResetEvent pauseButton = new ManualResetEvent(true);
            Progress<int> progress = new Progress<int>();
            int numinput = 2000;
            for (int i = 0; i < numinput; i++)
            {
                input.Enqueue("\"Input, number\"," + i); 
            }
            totalrecords = 0;
            progress.ProgressChanged += progresshandler;
            var action = stringSplitter.GetReportingPausableWorkItem();
            List<Task> WorkList = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                WorkList.Add(Task.Factory.StartNew(() => action(input, output, pauseButton, progress))); 
            }
            while (!input.IsEmpty)
            {
                Task.Delay(200).Wait();
            }
            stringSplitter.SignalCompletion();
            Task.WaitAll(WorkList.ToArray());

            Assert.IsTrue(input.IsEmpty);

            Assert.IsTrue(output.Count == numinput);

            Assert.IsTrue(output.All(x => x.ElementAt(0).Equals("Input, number")));
            var linqtime = output.AsEnumerable();
            IEnumerable<int> numbers = linqtime.Select(x => Int32.Parse((string)x.ElementAt(1))); //forgive the weird casting it used to be a string collection
            int Sum = numbers.Sum();
            IEnumerable<int> checknumbers = Enumerable.Range(0, numinput); //range is non inclusive
            int CheckSum = checknumbers.Sum();
            Assert.IsTrue(Sum == CheckSum);
 
        }

        [TestMethod()]
        public void StringSplitterTestNoQualifier()
        {
            StringSplitter stringSplitter = new StringSplitter(null);
            stringSplitter.Delimiter = ",";
            ConcurrentQueue<string> input = new ConcurrentQueue<string>();
            ConcurrentQueue<object[]> output = new ConcurrentQueue<object[]>();
            ManualResetEvent pauseButton = new ManualResetEvent(true);
            Progress<int> progress = new Progress<int>();
            int numinput = 2000;
            for (int i = 0; i < numinput; i++)
            {
                input.Enqueue("\"Input, number\"," + i);
            }
            var action = stringSplitter.GetReportingPausableWorkItem();
            List<Task> WorkList = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                WorkList.Add(Task.Factory.StartNew(() => action(input, output, pauseButton, progress)));
            }
            while (!input.IsEmpty)
            {
                Task.Delay(200).Wait();
            }
            stringSplitter.SignalCompletion();
            Task.WaitAll(WorkList.ToArray());

            Assert.IsTrue(input.IsEmpty);

            Assert.IsTrue(output.Count == numinput);

            Assert.IsTrue(output.All(x => x.ElementAt(0).Equals("\"Input")));
            var linqtime = output.AsEnumerable();
            IEnumerable<int> numbers = linqtime.Select(x => Int32.Parse((string)x.ElementAt(2))); //forgive the weird casting it used to be a string collection
            int Sum = numbers.Sum();
            IEnumerable<int> checknumbers = Enumerable.Range(0, numinput); //range is non inclusive
            int CheckSum = checknumbers.Sum();
            Assert.IsTrue(Sum == CheckSum);
        }

        [TestMethod()]
        public void RecordToRowTransformerTest()
        {
            string[] columnNames = new string[] { "First Name", "Surname", "Age" };
            RecordToRowTransformer rowFactory = new RecordToRowTransformer(columnNames);
            object[] data = new object[3]
            {
                new object[] { "John", "Doe", 35},
                new object[] {"Jane", "Doe", 36 },
                new object[] {"Frenk", "Tank, de", 19 }
            };
            
            ConcurrentBag<object[]> input = new ConcurrentBag<object[]>();
            for (int i =0; i < 3; i++)
            {
                input.Add((object[])data[i]);
            }
            ConcurrentBag<Row> output = new ConcurrentBag<Row>();

            ManualResetEvent pause = new ManualResetEvent(true);
            Progress<int> progress = new Progress<int>();
            progress.ProgressChanged += progresshandler;

            var action = rowFactory.GetReportingPausableWorkItem();
            Task work = Task.Factory.StartNew(() => action(input, output, pause, progress));
            while (!input.IsEmpty) { Task.Delay(100).Wait(); }
            rowFactory.SignalCompletion();
            work.Wait();
            int sumTotalAge = 0;
            foreach (Row r in output)
            {
                sumTotalAge += (int)r["Age"].Item1;
            }
            Assert.AreEqual(expected: 35 + 36 + 19, actual: sumTotalAge);

            //check errorskipping
            rowFactory = new RecordToRowTransformer(columnNames, true);
            data = new object[3]
            {
                new object[] { "John", "Doe", 35, ""}, //this row has too many columns!
                new object[] {"Jane", "Doe", 36 },
                new object[] {"Frenk", "Tank, de", 19 }
            };
            for (int i = 0; i < 3; i++)
            {
                input.Add((object[])data[i]);
            }

            output = new ConcurrentBag<Row>();

            action = rowFactory.GetReportingPausableWorkItem();
            work = Task.Factory.StartNew(() => action(input, output, pause, progress));
            while (!input.IsEmpty) { Task.Delay(100).Wait(); }
            rowFactory.SignalCompletion();
            work.Wait();

            Assert.IsTrue(output.Count == 2);
        }

        [TestMethod()]
        public void DIALStringSplitterTest()
        {
            string Line = "6123981||00927123||657392||22-01-2019||Pietje Puk";
            DIALStringSpliter splitter = new DIALStringSpliter();
            ConcurrentQueue<object[]> result = new ConcurrentQueue<object[]>();
            ConcurrentQueue<string> source = new ConcurrentQueue<string>();
            source.Enqueue(Line);
            ManualResetEvent pause = new ManualResetEvent(true);
            Progress<int> prog = new Progress<int>();
            var action = splitter.GetReportingPausableWorkItem();

            Task work = Task.Factory.StartNew(() => action(source, result, pause, prog));

            while (!source.IsEmpty) { Thread.SpinWait(1000); }
            splitter.SignalCompletion();
            work.Wait();
            object[] resultingRow = new object[5];
            result.TryDequeue(out resultingRow);
            Assert.AreEqual(expected: "6123981", actual: resultingRow[0]);
            Assert.AreEqual(expected: "00927123", actual: resultingRow[1]);
            Assert.AreEqual(expected: "657392", actual: resultingRow[2]);
            Assert.AreEqual(expected: "22-01-2019", actual: resultingRow[3]);
            Assert.AreEqual(expected: "Pietje Puk", actual: resultingRow[4]);
        }

        private void progresshandler(object sender, int e)
        {
            Console.WriteLine((totalrecords += e) + " number of records transformed");
        }
    }   
}