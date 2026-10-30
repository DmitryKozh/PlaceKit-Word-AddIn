using System;
using System.ClientModel;
using System.Net.Http;
using System.Threading.Tasks;
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

        // What the OpenAI SDK throws when every try failed, here without a TLS 1.2 connection.
        [TestMethod]
        public void OpenAI_RetriesThatNeverReachTheServer_AreANetworkError()
        {
            var failed = new AggregateException(
                "Retry failed after 4 tries.",
                new ClientResultException("An error occurred while sending the request.", null, new HttpRequestException("An error occurred while sending the request.")),
                new ClientResultException("An error occurred while sending the request."));

            ValueFinderException error = OpenAIValueFinder.Describe(failed);

            Assert.AreEqual("Could not reach OpenAI. Check the internet connection.", error.Message);
        }

        [TestMethod]
        public void OpenAI_RetriesThatTimeOut_SayItTookTooLong()
        {
            var failed = new AggregateException("Retry failed after 4 tries.", new HttpRequestException("x"), new TaskCanceledException());

            Assert.AreEqual("ChatGPT took too long to answer. Try again.", OpenAIValueFinder.Describe(failed).Message);
        }
    }
}
