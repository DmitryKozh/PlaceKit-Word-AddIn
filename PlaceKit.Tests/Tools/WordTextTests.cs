using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    [TestClass]
    public class WordTextTests
    {
        [TestMethod]
        public void ToDisplay_TurnsLineBreaksAndParagraphsIntoNewLines()
        {
            Assert.AreEqual("a\r\nb\r\nc", WordText.ToDisplay("a\vb\rc"));
            Assert.AreEqual("", WordText.ToDisplay(null));
        }

        [TestMethod]
        public void ToWord_UsesManualLineBreaks()
        {
            Assert.AreEqual("a\vb\vc\vd", WordText.ToWord("a\r\nb\nc\rd"));
            Assert.AreEqual("", WordText.ToWord(null));
        }

        [TestMethod]
        public void RoundTrip()
        {
            string text = "221B Baker Street\r\nLondon";

            Assert.AreEqual(text, WordText.ToDisplay(WordText.ToWord(text)));
        }

        [TestMethod]
        public void ToPlain_KeepsTheTextAndLeavesOutWordsMarks()
        {
            // a table cell ends with \r\a, a field shows its result between marks 19 to 21
            Assert.AreEqual("Cell\n\nLine 1\nLine 2\nPage\tTab", WordText.ToPlain("Cell\r\a\rLine 1\vLine 2\fPage\tTab"));
            Assert.AreEqual("12 October", WordText.ToPlain("\u001312 October\u0015"));
            Assert.AreEqual("", WordText.ToPlain(null));
        }

        [DataTestMethod]
        [DataRow("Dear Anna Berg, Ms Berg.", "Berg", 2)]
        [DataRow("Bergmann and Berg", "Berg", 1)]
        [DataRow("order 4711 and 47110", "4711", 1)]
        [DataRow("(4711)", "(4711)", 1)]
        [DataRow("x(4711)y", "(4711)", 1)]
        [DataRow("Anna Berg", "Anna Berg", 1)]
        [DataRow("", "Berg", 0)]
        public void CountWholeWords_CountsOnlyWholeWords(string text, string needle, int count)
        {
            Assert.AreEqual(count, WordText.CountWholeWords(text, needle));
        }
    }
}
