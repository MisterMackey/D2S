using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Transformers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;
namespace D2S.Library.Transformers.Tests
{
    [TestClass()]
    public class RowDuplicaterTests
    {
        [TestMethod()]
        public void RowDuplicatorTests()
        {
            BoundedConcurrentQueu<Row> input = new BoundedConcurrentQueu<Row>();
            
            BoundedConcurrentQueu<Row> firstOut = new BoundedConcurrentQueu<Row>();
            BoundedConcurrentQueu<Row> secondOut = new BoundedConcurrentQueu<Row>();

            BoundedConcurrentQueu<Row>[] outputArray = new BoundedConcurrentQueu<Row>[] { firstOut, secondOut };

            ManualResetEvent pause = new ManualResetEvent(true);
            Progress<int> progress = new Progress<int>();
            RowDuplicater rowDuplicater = new RowDuplicater();

            var action = rowDuplicater.GetReportingPausableWorkItem();

            input.TryAdd(new Row());

            Task t = Task.Factory.StartNew(() =>         action(input, outputArray, pause, progress));

            Task.Delay(10).Wait();
            rowDuplicater.SignalCompletion();

            Assert.IsTrue(input.Count == 0);
            foreach (var queue in outputArray)
            {
                Assert.IsTrue(queue.Count == 1);
            }
        }
    }
}