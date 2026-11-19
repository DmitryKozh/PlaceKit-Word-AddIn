using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class AiSetupViewModelTests
    {
        readonly MemorySettings _settings = new MemorySettings();
        readonly MemoryApiKeyStore _keys = new MemoryApiKeyStore();
        readonly FakeAiService _ai = new FakeAiService();
        readonly FakeUrlLauncher _urls = new FakeUrlLauncher();
        readonly List<bool?> _closes = new List<bool?>();

        AiSetupViewModel Create(string feature = "Detect")
        {
            var setup = new AiSetupViewModel(feature, _settings, _keys, _ai, _urls);
            setup.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return setup;
        }

        [TestMethod]
        public void Connect_TestsTheKeyThenSavesItAndTheService()
        {
            AiSetupViewModel setup = Create();
            setup.UseOpenAI = true;
            Assert.IsFalse(setup.ConnectCommand.CanExecute(null));

            setup.NewApiKey = " sk-pasted ";
            setup.ConnectCommand.Execute(null);

            Assert.AreEqual(new KeyValuePair<AiProvider, string>(AiProvider.OpenAI, "sk-pasted"), _ai.TestedKeys.Single());
            Assert.AreEqual("sk-pasted", _keys.OpenAI);
            Assert.AreEqual(AiProvider.OpenAI, _settings.Current.AiProvider);
            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
        }

        [TestMethod]
        public void Connect_KeepsTheDialogOpenWhenTheKeyIsRefused()
        {
            _ai.TestError = new AiServiceException("Claude did not accept the API key.");
            AiSetupViewModel setup = Create();
            setup.NewApiKey = "wrong";

            setup.ConnectCommand.Execute(null);

            Assert.AreEqual("Claude did not accept the API key.", setup.Error);
            Assert.IsNull(_keys.Claude);
            Assert.AreEqual(0, _closes.Count);

            setup.NewApiKey = "right";
            Assert.IsFalse(setup.HasError, "typing again clears the error");
        }

        // A key saved earlier for the other service is enough to switch to it.
        [TestMethod]
        public void Connect_CanUseASavedKey()
        {
            _keys.OpenAI = "saved";
            AiSetupViewModel setup = Create();
            Assert.IsFalse(setup.HasSavedApiKey);

            setup.UseOpenAI = true;

            Assert.IsTrue(setup.HasSavedApiKey);
            StringAssert.StartsWith(setup.ApiKeyHint, "A key is saved.");
            setup.ConnectCommand.Execute(null);
            Assert.AreEqual("saved", _ai.TestedKeys.Single().Value);
            Assert.AreEqual(AiProvider.OpenAI, _settings.Current.AiProvider);
        }

        [TestMethod]
        public void SwitchingTheService_ClearsTheKeyAndChangesTheSteps()
        {
            AiSetupViewModel setup = Create();
            setup.NewApiKey = "sk-ant-pasted";
            StringAssert.Contains(setup.KeySteps, "Claude Console");

            setup.UseOpenAI = true;

            Assert.AreEqual(string.Empty, setup.NewApiKey);
            StringAssert.Contains(setup.KeySteps, "A ChatGPT subscription doesn't cover the API.");
            setup.OpenKeysPageCommand.Execute(null);
            Assert.AreEqual(AiProviders.KeysPage(AiProvider.OpenAI), _urls.Opened.Single());
        }

        [TestMethod]
        public void Intro_NamesTheFeatureThatAsked()
        {
            StringAssert.StartsWith(Create("From text").Intro, "From text uses Claude or ChatGPT");
            StringAssert.StartsWith(Create(null).Intro, "Detect and From text use Claude or ChatGPT");
        }

        [TestMethod]
        public void NotNow_ClosesWithoutSaving()
        {
            AiSetupViewModel setup = Create();
            setup.NewApiKey = "pasted";

            setup.NotNowCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { false }, _closes);
            Assert.IsNull(_keys.Claude);
        }
    }
}
