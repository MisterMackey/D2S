using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Entities;

namespace D2S.Library.Services.Tests
{
    [TestClass()]
    public class DataLoggerTests
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            D2SLogContext context = new D2SLogContext(true);
            context.Database.CreateIfNotExists();
        }
        [TestMethod()]
        public void DataLoggerTest()
        {
            Assert.IsNotNull(DataLogger.Instance);
        }

        [TestMethod()]
        public void OpenAndCloseLogEntryTest()
        {
            var dl = DataLogger.Instance;
            //should complain
            Assert.ThrowsException<InvalidOperationException>(
                () => dl.CloseLogEntry(false));
            //open entry
            dl.OpenLogEntry("Test", "Testing logging");
            //try opening it again, it should complain since its alrdy open
            Assert.ThrowsException<InvalidOperationException>(
                () => dl.OpenLogEntry("lawl", "wut?"));
            //close it again (this will also trigger the DB write
            dl.CloseLogEntry(true);
            //close it some more.
            Assert.ThrowsException<InvalidOperationException>(
                () => dl.CloseLogEntry(false));

        }


        [TestMethod()]
        public void LogTaskToSqlAndMarkCompleteTest()
        {
            Assert.Fail();
        }

    }
}