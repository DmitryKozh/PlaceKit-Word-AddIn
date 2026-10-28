using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;

namespace PlaceKit.Tests.Models
{
    [TestClass]
    public class PlaceholderTagTests
    {
        [TestMethod]
        public void TagRoundTrip()
        {
            string tag = PlaceholderTag.Create("Client name");

            Assert.AreEqual("PlaceKit-Client name", tag);
            Assert.IsTrue(PlaceholderTag.TryGetName(tag, out string name));
            Assert.AreEqual("Client name", name);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("PlaceKit-")]
        [DataRow("Other-Client")]
        [DataRow("placekit-Client")]
        public void TryGetName_IgnoresForeignTags(string tag)
        {
            Assert.IsFalse(PlaceholderTag.TryGetName(tag, out _));
        }
    }
}
