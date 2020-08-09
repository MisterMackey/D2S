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
    public class DestinationTableDropperTests
    {
        PipelineContext pipelineContext = new PipelineContext()
        {
            FirstLineContainsHeaders = true,
            SourceFilePath = @"..\..\..\D2S.LibraryTests\DataTypes.txt",
            StringPadding = 100
,
            DestinationTableName = "dbo.DropperTest"
,
            IsSuggestingDataTypes = true
        };

        [TestInitialize()]
        public void TestInit()
        {
            DestinationTableCreator d = new DestinationTableCreator(pipelineContext);
            d.CreateTable();
        }

        [TestMethod()]
        public void DropTableWithContextTest()
        {
            DestinationTableDropper d = new DestinationTableDropper(pipelineContext);
            d.DropTable();
        }

        [TestMethod()]
        public void DropTableWithoutContextTest()
        {
            DestinationTableDropper d = new DestinationTableDropper(pipelineContext.DestinationTableName);
            d.DropTable();
        }
    }
}