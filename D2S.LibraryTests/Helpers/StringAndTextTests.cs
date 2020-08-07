using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Helpers.Tests
{
    [TestClass()]
    public class StringAndTextTests
    {
        [TestMethod()]
        public void SplitRowTest()
        {
            //single char delimiter
            string input = @"foo|bar|zoo";

            string[] output = StringAndText.SplitRow(input, "|", @"\", true);

            Assert.IsTrue(output.Count() == 3);

            //double char delimiter
            input = @"foo|||bar||zoo";

            output = StringAndText.SplitRow(input, "||", @"\", true);

            Assert.IsTrue(output.Count() == 3);
            Assert.AreEqual(expected: "|bar", actual: output[1]);
        }
        
        [TestMethod()]
        public void QualifierSplitRowTest()
        {
            //no qualifier
            string input = @"foo|bar|zoo";
            string[] output = StringAndText.SplitRow(input, "|", null, false);
            Assert.IsTrue(output.Count() == 3);

            //qualifier
            input = "foo|\"b|a|r\"|zoo";
            output = StringAndText.SplitRow(input, "|", "\"", false);

            Assert.IsTrue(output.Count() == 3);
            Assert.AreEqual(expected: "b|a|r", actual: output[1]);
        }
    }
}