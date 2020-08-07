using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.DataLake.Store;

namespace D2S.Library.Services.Tests
{
    [TestClass()]
    public class AzureClientTests
    {
        [TestMethod()]
        public void GetDataLakeClientTest()
        {
            // cant test default login until server principal is provided
            Assert.Inconclusive();
        }

        [TestMethod()]
        public void GetDataLakeClientTest1()
        {
            // cant test default login until server principal is provided
            Assert.Inconclusive();
        }

        [TestMethod()]
        public void GetDataLakeClientTest2()
        {
            //Code is made unreachable so unit test can run on a remote machine. If you wish to test the login prompt, simple remove the inconclusive assert and the return statement and run the test.
            Assert.Inconclusive();
            return;

            AzureClient clientService = new AzureClient(new Utilities.PipelineContext()); //will use settings from app config
            AdlsClient client = null;
            client = clientService.GetDataLakeClient(true);            

            Assert.IsNotNull(client);
        }

        [TestMethod()]
        public void GetDataLakeClientTest3()
        {
            //Code is made unreachable so unit test can run on a remote machine. If you wish to test the login prompt, simple remove the inconclusive assert and the return statement and run the test.
            Assert.Inconclusive();
            return;

            AzureClient clientService = new AzureClient(new Utilities.PipelineContext());  //will use settings from app config
            AdlsClient client = null;
            client = clientService.GetDataLakeClient("csreadingadlmackey.azuredatalakestore.net", true);

            Assert.IsNotNull(client);
        }

        [TestMethod()]
        public void GetDataFactoryClientTest()
        {
            // cant test default login until server principal is provided
            Assert.Inconclusive();
        }

        [TestMethod()]
        public void GetDataFactoryClientTest1()
        {
            // cant test default login until server principal is provided
            Assert.Inconclusive();
        }
    }
}