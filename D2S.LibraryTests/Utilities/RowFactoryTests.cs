using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Utilities;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Utilities.Tests
{
    [TestClass()]
    public class RowFactoryTests
    {
        [TestMethod()]
        public void CreateRowTest()
        {
            string[] columns = new string[] { "AnInteger", "AString", "ADouble", "ADate" };
            RowFactory Factory = new RowFactory(columns);
            int AnInteger = 5;
            string AString = "Number 1 sister";
            double ADouble = 3.50d;
            DateTime ADate = DateTime.Now;

            object[] Record = new object[] { AnInteger, AString, ADouble, ADate };

            Row MyRow = Factory.CreateRow(Record);

            Console.WriteLine(JsonConvert.SerializeObject(MyRow));

            for (int i= 0; i < 4; i++)
            {
                //check types
                Assert.AreEqual(expected: Record[i].GetType(), actual: MyRow[columns[i]].Item2);

                //check values
                Assert.AreEqual(expected: Record[i], actual: MyRow[columns[i]].Item1);
            }

            Record = new object[] { "haha" };
            bool errorThrown = false;
            try
            {
                MyRow = Factory.CreateRow(Record);
            }
            catch (Exception ex)
            {
                // if we reach here its good
                errorThrown = true;
            }
            finally
            {
                if (!errorThrown)
                {
                    Assert.Fail();
                }
            }

        }
    }
}