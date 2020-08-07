using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Utilities.Tests
{
    [TestClass()]
    public class BoundedConcurrentQueuTests
    {
        [TestMethod()]
        public void BoundedConcurrentQueuTest()
        {
            int boundedCapacity = 10;
            BoundedConcurrentQueu<int> bc = new BoundedConcurrentQueu<int>(boundedCapacity);
            //ensure basic adding and taking works within the boundery
            var t = Parallel.For(0, 5, inte => bc.TryAdd(1)); // add 5 integers
            while (!t.IsCompleted)
            {
                Task.Delay(10).Wait();
            }
            if (bc.Count != 5) { Assert.Fail("Failed to insert all 5 integers"); }
            int result;
            for (int i =0; i < 5; i++)
            {
                if (bc.TryTake(out result))
                {
                    continue;
                }
                else { Assert.Fail("failed to retrieve all 5 integers"); }
            }
            //fill up the collection to the max capacity
            for (int i = 0; i <10; i++)
            {
                bc.TryAdd(1);
            }
            //try to add one more item and check that the call to tryadd is successfully blocked
            Task attemptToExceedCapacity = Task.Factory.StartNew(
                () => bc.TryAdd(1));
            Task.Delay(50).Wait();
            //ensure task is started and is executing (i.e. waiting for the semaphore to be released)
            if (attemptToExceedCapacity.IsCompleted || attemptToExceedCapacity.Status != TaskStatus.Running)
            {
                Assert.Fail("Test managed to exceed the capacitiy of the collection or did not start");
            }
            //dequeu one item and check if the task succeeded
            bc.TryTake(out result);
            Task.Delay(50).Wait();
            if (!attemptToExceedCapacity.IsCompleted)
            {
                Assert.Fail("could not add new item after releasing the semaphore");
            }
            Assert.IsTrue(bc.Count == 10);
        }
    }
}