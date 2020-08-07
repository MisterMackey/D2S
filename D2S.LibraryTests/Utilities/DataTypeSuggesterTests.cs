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
    public class DataTypeSuggesterTests
    {
        [TestMethod()]
        public void SuggestDataTypeTest()
        {
            PipelineContext pipelineContext = new PipelineContext()
            {
                FirstLineContainsHeaders = true,
                SourceFilePath = @"..\..\..\D2S.LibraryTests\DataTypes.txt",
                StringPadding = 100
            };
            DataTypeSuggester suggester = new DataTypeSuggester(pipelineContext);
            string[] result = suggester.SuggestDataType();

            Assert.IsTrue(result[0] == "NVARCHAR(6)");
            Assert.IsTrue(result[1] == "INT");
            Assert.IsTrue(result[2] == "DEC(38,8)");
            Assert.IsTrue(result[3] == "CHAR");

            //check if dial sources would go correctly
            pipelineContext.SourceFileIsSourcedFromDial = true;
            result = suggester.SuggestDataType();
            Assert.IsTrue(result[0].Equals("NVARCHAR(2)"));
            Assert.IsTrue(result[1] == "INT");
            Assert.IsTrue(result[2] == "DEC(38,8)");
            Assert.IsTrue(result[3] == "CHAR");
        }
    }
}