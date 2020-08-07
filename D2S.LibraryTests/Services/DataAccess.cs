using D2S.LibraryTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace D2S.Library.Services.Tests
{
    [TestClass()]
    public class DataAccessTests
    {
        [TestMethod()]
        public void PortfolioDate_Read()
        {
            //todo: fix this dependancy on a recent version of the liqdatabase since we don't have it on a build agent
            Assert.Inconclusive();

            //SqlExtractorTestHelper helper = new SqlExtractorTestHelper();

            //try
            //{
            //    helper.CreateTestEntryForPortFolioDate(ConfigVariables.Instance.ConfiguredConnection);
            //    var result = DataAccess.Instance.PortfolioDate_Read(daily: true);

            //    Assert.IsTrue(result is DateTime); //inconclusive until it is assured that the underlying table actually has a value for this method to read.
            //    Assert.IsTrue(result.Equals(new DateTime(2018, 6, 30)));
            //}
            //catch (Exception)
            //{

            //    throw;
            //}
            //finally
            //{
            //    helper.CleanTestEntryForPortfolioDate(ConfigVariables.Instance.ConfiguredConnection);
            //}
        }
    }
}