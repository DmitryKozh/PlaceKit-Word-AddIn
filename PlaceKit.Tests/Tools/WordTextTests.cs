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
    }
}
