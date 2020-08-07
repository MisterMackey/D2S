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
    public class RowTests
    {
        [TestMethod()]
        public void CloneTest()
        {
            Row row = new Row();
            row["AnswerToTheUltimateQuestion"] = new Tuple<object, Type>(42, typeof(int));
            row["TheUltimateQuestion"] = new Tuple<object, Type>("Error", typeof(string));

            Row secondRow = (Row)row.Clone();

            Assert.AreNotSame(row, secondRow);
            foreach (var item in row)
            {
                Assert.IsTrue(item.Value.Item1 == secondRow[item.Key].Item1);
            }
        }
    }
}