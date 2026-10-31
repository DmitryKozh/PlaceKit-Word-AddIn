using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class AiServiceTests
    {
        static readonly AiRequest Request = new AiRequest("instructions", "prompt", ValueRequest.CreateSchema(new[] { "Name" }), AiEffort.Low);

        readonly FakeAiClient _claude = new FakeAiClient(AiProvider.Claude) { Answer = "from Claude" };
        readonly FakeAiClient _openAI = new FakeAiClient(AiProvider.OpenAI) { Answer = "from OpenAI" };
        readonly MemorySettings _settings = new MemorySettings();
        readonly MemoryApiKeyStore _keys = new MemoryApiKeyStore { Claude = "claude key", OpenAI = "openai key" };
        readonly FakeDialogs _dialogs = new FakeDialogs();
        AiService _ai;

        [TestInitialize]
        public void Create() =>
            _ai = new AiService(new[] { _claude, _openAI }, _settings, _keys, new InlineProgress(), _dialogs,
                () => new SettingsViewModel(_settings, _keys, _ai, new FakeUrlLauncher(), new FakeUpdateService(), TestProduct.Create()));

        [TestMethod]
        public void Send_UsesTheServiceAndKeyChosenInSettings()
        {
            Assert.AreEqual("from Claude", _ai.Send(Request, "Testing"));
            Assert.AreEqual("claude key", _claude.LastApiKey);
            Assert.AreSame(Request, _claude.LastRequest);

            _settings.Current.AiProvider = AiProvider.OpenAI;

            Assert.AreEqual(AiProvider.OpenAI, _ai.Provider);
            Assert.AreEqual("from OpenAI", _ai.Send(Request, "Testing"));
            Assert.AreEqual("openai key", _openAI.LastApiKey);
        }

        [TestMethod]
        public void Send_WithoutAKey_SaysWhereToAddIt()
        {
            _settings.Current.AiProvider = AiProvider.OpenAI;
            _keys.OpenAI = null;

            Assert.IsFalse(_ai.HasApiKey);
            AiServiceException error = Assert.ThrowsException<AiServiceException>(() => _ai.Send(Request, "Testing"));
            Assert.AreEqual("Add your OpenAI API key in Settings first.", error.Message);
            Assert.IsNull(_openAI.LastRequest);
        }

        [TestMethod]
        public void TestKey_UsesTheGivenServiceAndKey()
        {
            _ai.TestKey(AiProvider.OpenAI, "typed key");

            Assert.AreEqual("typed key", _openAI.LastApiKey);
            Assert.IsNull(_claude.LastApiKey);
        }

        [TestMethod]
        public void EnsureApiKey_DoesNotAskWhenThereIsAKey()
        {
            Assert.IsTrue(_ai.EnsureApiKey("From text"));
            Assert.AreEqual(0, _dialogs.Confirmations);
        }

        [TestMethod]
        public void EnsureApiKey_StopsWhenTheUserDeclines()
        {
            _keys.Claude = null;

            Assert.IsFalse(_ai.EnsureApiKey("From text"));
            Assert.AreEqual(1, _dialogs.Confirmations);
            Assert.AreEqual(0, _dialogs.Shown.Count);
        }

        [TestMethod]
        public void EnsureApiKey_OpensSettingsOnTheAiPage()
        {
            _keys.Claude = null;
            _dialogs.ConfirmAnswer = true;
            _dialogs.OnShowDialog = dialog =>
            {
                var settings = (SettingsViewModel)dialog;
                Assert.IsTrue(settings.ShowAiPage);
                settings.NewApiKey = "pasted key";
                settings.SaveCommand.Execute(null);
            };

            Assert.IsTrue(_ai.EnsureApiKey("Detect"));
            Assert.AreEqual("pasted key", _keys.Claude);
        }

        [TestMethod]
        public void EnsureApiKey_IsFalseWhenSettingsCloseWithoutAKey()
        {
            _keys.Claude = null;
            _dialogs.ConfirmAnswer = true;

            Assert.IsFalse(_ai.EnsureApiKey("Detect"));
            Assert.IsInstanceOfType(_dialogs.Shown.Single(), typeof(SettingsViewModel));
        }
    }
}
