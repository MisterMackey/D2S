using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace D2S.Library.Services.Tests
{
    [TestClass()]
    public class ConfigServiceTests
    {
        [TestMethod()]
        public void LoadConfigVariable()
        {
            var defaultFieldLength = ConfigVariables.Instance.Default_Field_Length;

            Assert.IsNotNull(defaultFieldLength);
        }
    }
}