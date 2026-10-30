using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;

namespace PlaceKit.Tests.Services
{
    // What the finders do before they call their service; nothing here sends a request.
    [TestClass]
    public class ValueFinderTests
    {
        static readonly string[] Names = { "Client name", "Order" };

        [TestMethod]
        public void Claude_NeedsItsOwnKey()
        {
            var keys = new MemoryApiKeyStore { OpenAI = "openai key" };
            IValueFinder finder = new ClaudeValueFinder(keys, new InlineProgress());

            Assert.AreEqual(AiProvider.Claude, finder.Provider);
            ValueFinderException error = Assert.ThrowsException<ValueFinderException>(() => finder.FindValues("text", Names));
            Assert.AreEqual("Add your Claude API key first.", error.Message);
        }

        [TestMethod]
        public void OpenAI_NeedsItsOwnKey()
        {
            var keys = new MemoryApiKeyStore { Claude = "claude key" };
            IValueFinder finder = new OpenAIValueFinder(keys, new InlineProgress());

            Assert.AreEqual(AiProvider.OpenAI, finder.Provider);
            ValueFinderException error = Assert.ThrowsException<ValueFinderException>(() => finder.FindValues("text", Names));
            Assert.AreEqual("Add your OpenAI API key first.", error.Message);
        }
    }
}
