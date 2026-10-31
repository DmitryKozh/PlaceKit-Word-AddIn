using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class WebsiteReleaseSourceTests
    {
        const string Feed = @"{
            ""version"": ""1.3.0"",
            ""page"": ""https://boringjob.work/placekit/"",
            ""download"": ""https://boringjob.work/placekit/PlaceKit.msi""
        }";

        [TestMethod]
        public async Task GetLatest_ReadsTheFeedAndNamesItsVersion()
        {
            var handler = new FakeHandler(request => Answer(HttpStatusCode.OK, Feed));

            ReleaseInfo release = await Source(handler).GetLatestAsync(CancellationToken.None);

            Assert.AreEqual("1.3.0", release.VersionText);
            Assert.AreEqual("https://boringjob.work/placekit/latest.json", handler.LastRequest.RequestUri.AbsoluteUri);
            Assert.AreEqual("PlaceKit/1.2.3", handler.LastRequest.Headers.UserAgent.ToString());
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, "No update information on boringjob.work yet.")]
        [DataRow(HttpStatusCode.ServiceUnavailable, "boringjob.work answered with error 503. Try again later.")]
        public async Task GetLatest_ExplainsAnError(HttpStatusCode status, string message)
        {
            var error = await Assert.ThrowsExceptionAsync<UpdateCheckException>(() =>
                Source(new FakeHandler(request => Answer(status, "<html></html>"))).GetLatestAsync(CancellationToken.None));

            Assert.AreEqual(message, error.Message);
        }

        [TestMethod]
        public async Task GetLatest_ExplainsNoConnectionAndNoAnswer()
        {
            var offline = await Assert.ThrowsExceptionAsync<UpdateCheckException>(() =>
                Source(new FakeHandler(request => throw new HttpRequestException("DNS"))).GetLatestAsync(CancellationToken.None));
            var slow = await Assert.ThrowsExceptionAsync<UpdateCheckException>(() =>
                Source(new FakeHandler(request => throw new TaskCanceledException())).GetLatestAsync(CancellationToken.None));

            StringAssert.StartsWith(offline.Message, "boringjob.work can't be reached.");
            StringAssert.StartsWith(slow.Message, "boringjob.work didn't answer in time.");
        }

        [TestMethod]
        public async Task GetLatest_LetsTheUserCancel()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try
                {
                    await Source(new FakeHandler(request => throw new TaskCanceledException())).GetLatestAsync(cancellation.Token);
                    Assert.Fail("not canceled");
                }
                catch (OperationCanceledException)
                {
                    // not an UpdateCheckException: the progress window was closed on purpose
                }
            }
        }

        [TestMethod]
        public void Parse_AcceptsAHandWrittenFile()
        {
            ReleaseInfo release = WebsiteReleaseSource.Parse("\uFEFF{\n  // 1.3 is out\n  \"version\": \"v1.3\",\n  \"page\": \"https://boringjob.work/placekit/\",\n}");

            Assert.AreEqual(new Version(1, 3, 0), release.Version);
            Assert.AreEqual("https://boringjob.work/placekit/", release.Page.AbsoluteUri);
            Assert.IsNull(release.Installer, "no download: the page offers it");
        }

        [DataTestMethod]
        [DataRow(@"{ ""version"": ""latest"", ""page"": ""https://boringjob.work/placekit/"" }", "\"latest\", which is not a version number")]
        [DataRow(@"{ ""version"": ""1.3.0"" }", "could not be read")]
        [DataRow(@"{ ""version"": ""1.3.0"", ""page"": ""http://boringjob.work/placekit/"" }", "could not be read")]
        [DataRow(@"{ ""version"": ""1.3.0"", ""page"": ""https://boringjob.work/"", ""download"": ""file:///C:/Windows/notepad.exe"" }", "could not be read")]
        [DataRow(@"{ ""version"": ""1.3.0"", ""page"": null }", "could not be read")]
        [DataRow("<!DOCTYPE html><title>Not found</title>", "could not be read")]
        public void Parse_RejectsWhatItCannotUse(string json, string reason)
        {
            UpdateCheckException error = Assert.ThrowsException<UpdateCheckException>(() => WebsiteReleaseSource.Parse(json));

            StringAssert.Contains(error.Message, reason);
        }

        static WebsiteReleaseSource Source(HttpMessageHandler handler) => new WebsiteReleaseSource(TestProduct.Create(), handler);

        static HttpResponseMessage Answer(HttpStatusCode status, string body) => new HttpResponseMessage(status) { Content = new StringContent(body) };

        sealed class FakeHandler : HttpMessageHandler
        {
            readonly Func<HttpRequestMessage, HttpResponseMessage> _answer;

            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer)
            {
                _answer = answer;
            }

            public HttpRequestMessage LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                LastRequest = request;
                try
                {
                    return Task.FromResult(_answer(request));
                }
                catch (Exception ex)
                {
                    return Task.FromException<HttpResponseMessage>(ex);
                }
            }
        }
    }
}
