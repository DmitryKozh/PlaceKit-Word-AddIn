using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;

namespace PlaceKit.Tests.Models
{
    [TestClass]
    public class ReleaseInfoTests
    {
        [DataTestMethod]
        [DataRow("v1.2.0", "1.2.0")]
        [DataRow("V1.2", "1.2.0")]
        [DataRow(" 1.2.3.4 ", "1.2.3")]
        public void TryParseVersion_ReadsTagsAndAssemblyVersions(string text, string expected)
        {
            Assert.IsTrue(ReleaseInfo.TryParseVersion(text, out Version version));
            Assert.AreEqual(Version.Parse(expected), version);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("latest")]
        [DataRow("v1")]
        public void TryParseVersion_RejectsTextWithoutAVersion(string text)
        {
            Assert.IsFalse(ReleaseInfo.TryParseVersion(text, out _));
        }

        [TestMethod]
        public void IsNewerThan_ComparesThreeParts()
        {
            ReleaseInfo release = new ReleaseInfo(new Version(1, 2, 0, 0), new Uri("https://boringjob.work/placekit/"), null);

            Assert.AreEqual("1.2.0", release.VersionText);
            Assert.IsTrue(release.IsNewerThan("1.1.9"));
            Assert.IsFalse(release.IsNewerThan("1.2.0"));
            Assert.IsFalse(release.IsNewerThan("1.10.0"), "numbers, not text");
            Assert.IsFalse(release.IsNewerThan("unknown"));
        }
    }
}
