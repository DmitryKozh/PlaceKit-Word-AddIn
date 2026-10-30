using System;
using System.ClientModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class AiClientTests
    {
        // what the OpenAI SDK throws after its retries, here without TLS 1.2
        [TestMethod]
        public void OpenAI_RetriesThatNeverReachTheServer_AreANetworkError()
        {
            var failed = new AggregateException(
                "Retry failed after 4 tries.",
                new ClientResultException("An error occurred while sending the request.", null, new HttpRequestException("An error occurred while sending the request.")),
                new ClientResultException("An error occurred while sending the request."));

            Assert.AreEqual("Could not reach OpenAI. Check the internet connection.", OpenAIClient.Describe(failed, CancellationToken.None).Message);
        }

        [TestMethod]
        public void OpenAI_RetriesThatTimeOut_SayItTookTooLong()
        {
            var failed = new AggregateException("Retry failed after 4 tries.", new HttpRequestException("x"), new TaskCanceledException());

            Assert.AreEqual("ChatGPT took too long to answer. Try again.", OpenAIClient.Describe(failed, CancellationToken.None).Message);
        }

        [TestMethod]
        public void Errors_AfterTheUserCanceled_AreACancellation()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.IsInstanceOfType(OpenAIClient.Describe(new HttpRequestException("aborted"), cancellation.Token), typeof(OperationCanceledException));
                Assert.IsInstanceOfType(ClaudeClient.Describe(new TaskCanceledException(), cancellation.Token), typeof(OperationCanceledException));
            }
        }

        // A bug is not a failed request: it keeps its own exception and stack trace.
        [TestMethod]
        public void OtherErrors_PassThrough()
        {
            Assert.IsNull(OpenAIClient.Describe(new InvalidCastException(), CancellationToken.None));
            Assert.IsNull(ClaudeClient.Describe(new InvalidCastException(), CancellationToken.None));
        }

        [TestMethod]
        public void Claude_Timeouts_SayItTookTooLong()
        {
            Exception described = ClaudeClient.Describe(new TaskCanceledException(), CancellationToken.None);

            Assert.IsInstanceOfType(described, typeof(AiServiceException));
            Assert.AreEqual("Claude took too long to answer. Try again.", described.Message);
        }
    }
}
