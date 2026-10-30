using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    // ServicePointManager is shared by the whole test run, so each test puts the old value back.
    [TestClass]
    public class NetworkSettingsTests
    {
        SecurityProtocolType _original;

        [TestInitialize]
        public void Remember() => _original = ServicePointManager.SecurityProtocol;

        [TestCleanup]
        public void Restore() => ServicePointManager.SecurityProtocol = _original;

        [TestMethod]
        public void UseModernTls_AddsTls12ToTheDefaultsOfDotNet40()
        {
            // What Office gives the add-in.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

            NetworkSettings.UseModernTls();

            Assert.IsTrue(ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12));
        }

        [TestMethod]
        public void UseModernTls_LeavesTheChoiceToWindowsWhenItHasIt()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

            NetworkSettings.UseModernTls();

            Assert.AreEqual(SecurityProtocolType.SystemDefault, ServicePointManager.SecurityProtocol);
        }
    }
}
