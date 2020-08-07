using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using D2S.Library.Pipelines;

namespace D2S.Library.Extractors.Tests
{
    [TestClass()]
    public class ConcurrentExcelReaderTests
    {
        private PipelineContext context = new PipelineContext()
        {
            SourceFilePath = @"..\..\ExcelExtractorTest.xlsx",
            ExcelWorksheetName = "Sheet1"
        };


        [TestMethod()]
        public void TryExtractRecordTest()
        {
            ConcurrentExcelReader reader = new ConcurrentExcelReader(context);
            string[] line;
            List<string[]> results = new List<string[]>();
            while (reader.TryExtractRecord(out line))
            {
                results.Add(line);
            }
            string[] firstColumn = new string[2] { "Nisha", "Jasper" };
            Assert.AreEqual(firstColumn[0], results[0][0]);
            Assert.AreEqual(firstColumn[1], results[1][0]);
        }
        [TestMethod()]
        public void ExcelCheckExceptionWhenSheetNotFound()
        {
            PipelineContext p = context;
            p.ExcelWorksheetName = "FakeNameThatDoesn'tExist";
            
            Assert.ThrowsException<System.IO.IOException>(() => new ConcurrentExcelReader(p));
        }

    }
}