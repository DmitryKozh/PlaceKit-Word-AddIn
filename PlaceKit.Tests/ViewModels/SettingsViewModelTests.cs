using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class SettingsViewModelTests
    {
        readonly MemorySettings _store = new MemorySettings();
        readonly MemoryApiKeyStore _keys = new MemoryApiKeyStore();
        readonly FakeAiService _ai = new FakeAiService();
        readonly FakeUrlLauncher _urls = new FakeUrlLauncher();

        SettingsViewModel Create() => new SettingsViewModel(_store, _keys, _ai, _urls);

        [TestMethod]
        public void InvalidMarker_DisablesSave()
        {
            var settings = new SettingsViewModel(_store, _keys, _ai, _urls) { OpeningMarker = "" };

            Assert.IsFalse(settings.IsValid);
            Assert.IsNotNull(((IDataErrorInfo)settings)[nameof(SettingsViewModel.OpeningMarker)]);
            Assert.IsNull(((IDataErrorInfo)settings)[nameof(SettingsViewModel.ClosingMarker)]);
            Assert.IsFalse(settings.SaveCommand.CanExecute(null));
            Assert.AreEqual("-", settings.Preview);
        }

        [TestMethod]
        public void Preview_UsesTheMarkers()
        {
            var settings = new SettingsViewModel(_store, _keys, _ai, _urls) { OpeningMarker = "[[", ClosingMarker = "]]" };

            Assert.AreEqual("[[Client name]]", settings.Preview);
        }

        [TestMethod]
        public void AutoOpen_OnlyMattersWithoutTheSummary()
        {
            var settings = Create();

            Assert.IsFalse(settings.CanChooseAutoOpen);
            settings.ShowCreateSummary = false;
            Assert.IsTrue(settings.CanChooseAutoOpen);
        }

        [TestMethod]
        public void RadioButtons_SelectThePlacement()
        {
            var settings = Create();
            Assert.IsTrue(settings.OpenInSidePanel);

            // What WPF does when the user picks the other radio button.
            settings.OpenInWindow = true;
            settings.OpenInSidePanel = false;

            Assert.AreEqual(EditorPlacement.Window, settings.EditorPlacement);
            Assert.IsFalse(settings.OpenInSidePanel);
        }

        [TestMethod]
        public void EmptyPlaceholderAction_OnlyMattersWithoutTheQuestion()
        {
            var settings = Create();
            Assert.IsFalse(settings.CanChooseEmptyPlaceholderAction);
            Assert.IsTrue(settings.KeepEmptyPlaceholdersAsMarkers);

            settings.AskAboutEmptyPlaceholders = false;
            settings.DeleteEmptyPlaceholders = true;
            settings.KeepEmptyPlaceholdersAsMarkers = false;

            Assert.IsTrue(settings.CanChooseEmptyPlaceholderAction);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, settings.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Save_StoresTheSettingsAndCloses()
        {
            var settings = new SettingsViewModel(_store, _keys, _ai, _urls)
            {
                OpeningMarker = "<<",
                ClosingMarker = ">>",
                ShowCreateSummary = false,
                OpenInWindow = true,
                AskAboutEmptyPlaceholders = false,
                DeleteEmptyPlaceholders = true,
            };
            bool? closedWith = null;
            settings.CloseRequested += (sender, e) => closedWith = e.DialogResult;

            settings.SaveCommand.Execute(null);

            Assert.AreEqual(true, closedWith);
            Assert.AreEqual("<<", _store.Saved.OpeningMarker);
            Assert.IsFalse(_store.Saved.ShowCreateSummary);
            Assert.AreEqual(EditorPlacement.Window, _store.Saved.EditorPlacement);
            Assert.IsFalse(_store.Saved.AskAboutEmptyPlaceholders);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, _store.Saved.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Reset_RestoresTheDefaultsWithoutSaving()
        {
            var settings = new SettingsViewModel(_store, _keys, _ai, _urls) { OpeningMarker = "<<", OpenInWindow = true };

            settings.ResetCommand.Execute(null);

            Assert.AreEqual("{{", settings.OpeningMarker);
            Assert.IsTrue(settings.OpenInSidePanel);
            Assert.IsNull(_store.Saved);
        }

        [TestMethod]
        public void Pages_StartOnGeneralAndSwitch()
        {
            SettingsViewModel settings = Create();
            Assert.IsTrue(settings.ShowGeneralPage);

            settings.SwitchPageCommand.Execute(null);
            Assert.IsTrue(settings.ShowAiPage);

            // What WPF does when the user clicks General in the list.
            settings.ShowGeneralPage = true;
            settings.ShowAiPage = false;
            Assert.IsTrue(settings.ShowGeneralPage);
        }

        [TestMethod]
        public void AiProvider_IsLoadedAndSaved()
        {
            _store.Current.AiProvider = AiProvider.OpenAI;
            SettingsViewModel settings = Create();
            Assert.IsTrue(settings.UseOpenAI);

            settings.UseClaude = true;
            settings.UseOpenAI = false;
            settings.SaveCommand.Execute(null);

            Assert.AreEqual(AiProvider.Claude, _store.Saved.AiProvider);
        }

        [TestMethod]
        public void ApiKeys_AreTypedAndSavedPerService()
        {
            SettingsViewModel settings = Create();
            settings.NewApiKey = " claude key ";
            settings.UseOpenAI = true;
            Assert.AreEqual("", settings.NewApiKey, "each service has its own box");
            Assert.AreEqual("OpenAI API _key", settings.ApiKeyLabel);
            Assert.AreEqual("Get a key on the OpenAI platform", settings.ApiKeysPageLink);
            settings.NewApiKey = "openai key";

            settings.UseClaude = true;
            Assert.AreEqual(" claude key ", settings.NewApiKey);
            Assert.IsNull(_keys.Claude, "nothing is saved before Save");

            settings.SaveCommand.Execute(null);

            Assert.AreEqual("claude key", _keys.Claude);
            Assert.AreEqual("openai key", _keys.OpenAI);
        }

        [TestMethod]
        public void SavedKey_IsNeverShown()
        {
            _keys.Claude = "saved key";
            SettingsViewModel settings = Create();

            Assert.IsTrue(settings.HasSavedApiKey);
            Assert.AreEqual("", settings.NewApiKey);
            StringAssert.StartsWith(settings.ApiKeyHint, "A key is saved.");

            settings.UseOpenAI = true;
            Assert.IsFalse(settings.HasSavedApiKey);
            Assert.AreEqual("Paste your OpenAI API key", settings.ApiKeyHint);
        }

        [TestMethod]
        public void RemoveApiKey_DeletesOnlyThatServicesKeyWhenSaved()
        {
            _keys.Claude = "claude key";
            _keys.OpenAI = "openai key";
            SettingsViewModel settings = Create();

            settings.RemoveApiKeyCommand.Execute(null);

            Assert.IsFalse(settings.HasSavedApiKey);
            Assert.IsFalse(settings.RemoveApiKeyCommand.CanExecute(null));
            Assert.AreEqual("claude key", _keys.Claude, "not before Save");

            settings.SaveCommand.Execute(null);

            Assert.IsNull(_keys.Claude);
            Assert.AreEqual("openai key", _keys.OpenAI);
        }

        [TestMethod]
        public void Cancel_KeepsTheKeys()
        {
            _keys.Claude = "claude key";
            SettingsViewModel settings = Create();
            settings.RemoveApiKeyCommand.Execute(null);
            settings.UseOpenAI = true;
            settings.NewApiKey = "openai key";

            settings.CancelCommand.Execute(null);

            Assert.AreEqual("claude key", _keys.Claude);
            Assert.IsNull(_keys.OpenAI);
        }

        [TestMethod]
        public void Reset_DoesNotTouchTheKeys()
        {
            _keys.Claude = "claude key";
            _store.Current.AiProvider = AiProvider.OpenAI;
            SettingsViewModel settings = Create();

            settings.ResetCommand.Execute(null);
            settings.SaveCommand.Execute(null);

            Assert.AreEqual(AiProvider.Claude, _store.Saved.AiProvider);
            Assert.AreEqual("claude key", _keys.Claude);
        }

        [TestMethod]
        public void TestConnection_TriesTheTypedKeyOrElseTheSavedOne()
        {
            _keys.Claude = "saved key";
            SettingsViewModel settings = Create();

            settings.TestConnectionCommand.Execute(null);
            settings.NewApiKey = " typed key ";
            settings.TestConnectionCommand.Execute(null);

            CollectionAssert.AreEqual(new[] { "saved key", "typed key" }, _ai.TestedKeys.Select(test => test.Value).ToList());
            Assert.IsTrue(_ai.TestedKeys.All(test => test.Key == AiProvider.Claude));
            Assert.IsTrue(settings.IsConnected);
            Assert.AreEqual("Connected to Claude.", settings.ConnectionMessage);
        }

        [TestMethod]
        public void TestConnection_ShowsWhyItFailed()
        {
            _ai.TestError = new AiServiceException("OpenAI did not accept the API key. Check that it is complete and still active.");
            SettingsViewModel settings = Create();
            settings.UseOpenAI = true;
            settings.NewApiKey = "wrong key";

            settings.TestConnectionCommand.Execute(null);

            Assert.IsFalse(settings.IsConnected);
            Assert.IsTrue(settings.HasConnectionMessage);
            StringAssert.StartsWith(settings.ConnectionMessage, "OpenAI did not accept");
        }

        [TestMethod]
        public void TestConnection_NeedsAKeyAndForgetsOldResults()
        {
            SettingsViewModel settings = Create();
            Assert.IsFalse(settings.TestConnectionCommand.CanExecute(null));

            settings.NewApiKey = "typed key";
            settings.TestConnectionCommand.Execute(null);
            Assert.IsTrue(settings.HasConnectionMessage);

            settings.NewApiKey = "another key";
            Assert.IsFalse(settings.HasConnectionMessage);
        }

        [TestMethod]
        public void TestConnection_CanceledShowsNothing()
        {
            _ai.TestError = new OperationCanceledException();
            SettingsViewModel settings = Create();
            settings.NewApiKey = "typed key";

            settings.TestConnectionCommand.Execute(null);

            Assert.IsFalse(settings.HasConnectionMessage);
        }

        [TestMethod]
        public void ApiKeysPage_OpensThePageOfTheChosenService()
        {
            SettingsViewModel settings = Create();

            settings.OpenApiKeysPageCommand.Execute(null);
            settings.UseOpenAI = true;
            settings.OpenApiKeysPageCommand.Execute(null);

            CollectionAssert.AreEqual(
                new[] { "https://console.anthropic.com/settings/keys", "https://platform.openai.com/api-keys" },
                _urls.Opened.Select(url => url.AbsoluteUri).ToList());
        }
    }
}
