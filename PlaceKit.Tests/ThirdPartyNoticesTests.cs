using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlaceKit.Tests
{
    [TestClass]
    public class ThirdPartyNoticesTests
    {
        [TestMethod]
        public void Notices_NameEveryPackageTheAddInShips()
        {
            string repository = RepositoryFolder();
            string notices = File.ReadAllText(Path.Combine(repository, "THIRD-PARTY-NOTICES.txt"));

            // Fody weavers only run at build time.
            List<string> missing = XDocument.Load(Path.Combine(repository, "PlaceKit", "packages.config")).Root.Elements("package")
                .Where(package => (string)package.Attribute("developmentDependency") != "true" && !((string)package.Attribute("id")).EndsWith(".Fody"))
                .Select(package => $"{(string)package.Attribute("id")} {(string)package.Attribute("version")}")
                .Where(package => !notices.Contains(package))
                .ToList();

            Assert.AreEqual(0, missing.Count, "Add these, with their licenses, to THIRD-PARTY-NOTICES.txt: " + string.Join(", ", missing));
        }

        static string RepositoryFolder([CallerFilePath] string path = null) => Path.GetDirectoryName(Path.GetDirectoryName(path));
    }
}
