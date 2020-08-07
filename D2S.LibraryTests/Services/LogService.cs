using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace D2S.Library.Services.Tests
{
    [TestClass()]
    public class LogServiceTests
    {
        [TestMethod()]
        public void WriteLogData()
        {
            var ex = new System.Exception("Test exception");

            LogService.Instance.Error(ex);

            // ToDo: Check if log file was added or updated
            Assert.Inconclusive();
        }
    }
}