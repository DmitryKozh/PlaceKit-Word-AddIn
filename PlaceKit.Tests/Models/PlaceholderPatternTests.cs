using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;

namespace PlaceKit.Tests.Models
{
    [TestClass]
    public class PlaceholderPatternTests
    {
        [TestMethod]
        public void TryGetName_TrimsTheName()
        {
            Assert.IsTrue(PlaceholderPattern.Default.TryGetName("{{  Client name }}", out string name));
            Assert.AreEqual("Client name", name);
        }

        [DataTestMethod]
        [DataRow("{{}}")]
        [DataRow("{{   }}")]
        [DataRow("{{a {{b}}")]
        [DataRow("{{line\vbreak}}")]
        [DataRow("{{line\rbreak}}")]
        [DataRow("Client}}")]
        [DataRow("{{Client")]
        [DataRow("")]
        [DataRow(null)]
        public void TryGetName_RejectsInvalidText(string text)
        {
            Assert.IsFalse(PlaceholderPattern.Default.TryGetName(text, out string name));
            Assert.IsNull(name);
        }

        [TestMethod]
        public void TryGetName_RejectsTooLongNames()
        {
            string longest = new string('a', PlaceholderPattern.MaxNameLength);

            Assert.IsTrue(PlaceholderPattern.Default.TryGetName("{{" + longest + "}}", out _));
            Assert.IsFalse(PlaceholderPattern.Default.TryGetName("{{" + longest + "a}}", out _));
        }

        [TestMethod]
        public void Format_WrapsTheNameInMarkers()
        {
            Assert.AreEqual("[[Order]]", new PlaceholderPattern("[[", "]]").Format("Order"));
        }

        [DataTestMethod]
        [DataRow("{{", "}}", @"\{\{*\}\}")]
        [DataRow("[[", "]]", @"\[\[*\]\]")]
        [DataRow("<<", ">>", @"\<\<*\>\>")]
        [DataRow("%", "%", "%*%")]
        [DataRow("${", "}", @"$\{*\}")]
        [DataRow("^", "^", "^^*^^")]
        public void ToWordWildcards_EscapesSpecialCharacters(string opening, string closing, string expected)
        {
            Assert.AreEqual(expected, new PlaceholderPattern(opening, closing).ToWordWildcards());
        }

        [TestMethod]
        public void CountCandidates_CountsMarkedFragments()
        {
            Assert.AreEqual(3, PlaceholderPattern.Default.CountCandidates("{{a}} text {{b}}{{c}} {{ unclosed"));
            Assert.AreEqual(0, PlaceholderPattern.Default.CountCandidates(null));
        }

        [TestMethod]
        public void ValidateMarker_AcceptsShortMarkersWithoutSpaces()
        {
            Assert.IsNull(PlaceholderPattern.ValidateMarker("{{"));
            Assert.IsNull(PlaceholderPattern.ValidateMarker(new string('#', PlaceholderPattern.MaxMarkerLength)));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("{ {")]
        [DataRow("\t")]
        [DataRow("12345678901")]
        public void ValidateMarker_RejectsInvalidMarkers(string marker)
        {
            Assert.IsNotNull(PlaceholderPattern.ValidateMarker(marker));
        }

        [TestMethod]
        public void Constructor_ThrowsForInvalidMarkers()
        {
            Assert.ThrowsException<ArgumentException>(() => new PlaceholderPattern("", "}}"));
        }
    }
}
