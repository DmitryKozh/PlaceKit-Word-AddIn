using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class UrlLauncherTests
    {
        // Like License.txt after someone deleted it: a message, not an exception that reaches Word.
        [TestMethod]
        public void Open_MissingFile_SaysSo()
        {
            var dialogs = new FakeDialogs();
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "License.txt");

            new UrlLauncher(dialogs).Open(new Uri(path));

            StringAssert.Contains(dialogs.Messages.Single(), path);
        }
    }
}
