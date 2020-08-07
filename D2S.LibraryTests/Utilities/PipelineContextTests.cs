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
    public class PipelineContextTests
    {
        [TestMethod()]
        public void GetColumnSelectionTest()
        {
            PipelineContext context = new PipelineContext();
            //these are the columns we assume to be available in the source for this test
            var arrange = new string[] { "col1", "col2" };
            context.ColumnNames = arrange;
            var result = context.ColumnNamesSelection;

            //if selection is not made then the above call should just result in all columns being returned (we assume all columns are selected if no explicit statement is made)
            Assert.AreEqual(arrange[0], result[0]);
            Assert.AreEqual(arrange[1], result[1]);
            //now we set the selection to an invalid value ('f' and 'd' do no exist) and assert that an exception is thrown

            Assert.ThrowsException<ArgumentException>(new Action(
                () => context.ColumnNamesSelection = new string[] { "f", "d" }));

            //now we set a valid selection and assert it is returned correctly
            context.ColumnNamesSelection = new string[] { "col1" };
            result = context.ColumnNamesSelection;
            Assert.IsTrue(result.Count() == 1 && result[0] == "col1");
            //now lets check if it sorts correctly
            arrange = new string[] { "col1", "col2", "col3" };
            context.ColumnNames = arrange;
            context.ColumnNamesSelection = new string[] { "col3", "col2" };
            //this should return the same array but reversed (ie in correct order)
            result = context.ColumnNamesSelection;
            Assert.AreEqual(expected: "col2", actual: result[0]);
            Assert.AreEqual(expected: "col3", actual: result[1]);
        }
    }
}