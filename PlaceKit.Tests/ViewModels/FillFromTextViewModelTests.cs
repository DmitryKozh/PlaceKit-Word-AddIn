using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class FillFromTextViewModelTests
    {
        static readonly string[] Names = { "Client name", "Date" };

        readonly FakeValueFinder _claude = new FakeValueFinder(AiProvider.Claude);
        readonly FakeValueFinder _openAI = new FakeValueFinder(AiProvider.OpenAI);
        readonly MemoryApiKeyStore _apiKeys = new MemoryApiKeyStore { Claude = "claude key" };
        readonly FakeUrlLauncher _urls = new FakeUrlLauncher();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly List<bool?> _closes = new List<bool?>();

        FillFromTextViewModel Create()
        {
            var dialog = new FillFromTextViewModel(Names, new[] { _claude, _openAI }, _apiKeys, _urls, _dialogs);
            dialog.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return dialog;
        }

        [TestMethod]
        public void Intro_NamesTheServiceAndCountsThePlaceholders()
        {
            FillFromTextViewModel dialog = Create();
            StringAssert.Contains(dialog.Intro, "Claude finds the values for the 2 placeholders");

            dialog.UseOpenAI = true;
            StringAssert.Contains(dialog.Intro, "ChatGPT finds the values for the 2 placeholders");
        }

        [TestMethod]
        public void Find_NeedsText()
        {
            FillFromTextViewModel dialog = Create();
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));

            dialog.Text = "  ";
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));

            dialog.Text = "Contoso";
            Assert.IsTrue(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void Find_ClosesWithTheValues()
        {
            _claude.Values["Client name"] = "Contoso";
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.FindCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
            Assert.AreEqual("Contoso", dialog.Values["Client name"]);
            Assert.AreEqual("From Contoso", _claude.LastText);
            CollectionAssert.AreEqual(Names, _claude.LastNames.ToList());
            Assert.AreEqual(0, _openAI.Calls);
        }

        [TestMethod]
        public void Find_UsesTheChosenService()
        {
            _apiKeys.OpenAI = "openai key";
            _openAI.Values["Date"] = "12 November";
            FillFromTextViewModel dialog = Create();
            dialog.Text = "On 12 November";

            dialog.UseOpenAI = true;
            dialog.UseClaude = false;
            dialog.FindCommand.Execute(null);

            Assert.AreEqual(AiProvider.OpenAI, dialog.Provider);
            Assert.AreEqual(1, _openAI.Calls);
            Assert.AreEqual(0, _claude.Calls);
            Assert.AreEqual("12 November", dialog.Values["Date"]);
        }

        [TestMethod]
        public void Provider_StartsWithTheServiceThatHasAKey()
        {
            Assert.AreEqual(AiProvider.Claude, Create().Provider);

            _apiKeys.Claude = null;
            _apiKeys.OpenAI = "openai key";
            Assert.AreEqual(AiProvider.OpenAI, Create().Provider);

            _apiKeys.OpenAI = null;
            Assert.AreEqual(AiProvider.Claude, Create().Provider, "neither has a key");
        }

        [TestMethod]
        public void ChangingTheService_ShowsItsKeyState()
        {
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
            dialog.NewApiKey = "typed for Claude";
            var changed = new List<string>();
            dialog.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            dialog.UseOpenAI = true;

            Assert.IsFalse(dialog.HasSavedApiKey);
            Assert.AreEqual("", dialog.NewApiKey, "a key typed for one service is not used for the other");
            Assert.AreEqual("OpenAI API _key", dialog.ApiKeyLabel);
            Assert.AreEqual("Paste your OpenAI API key", dialog.ApiKeyHint);
            Assert.AreEqual("Get a key on the OpenAI platform", dialog.ApiKeysPageText);
            StringAssert.Contains(dialog.PrivacyNote, "sent to OpenAI");
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));
            CollectionAssert.IsSubsetOf(new[] { nameof(dialog.Intro), nameof(dialog.ApiKeyLabel), nameof(dialog.ApiKeyHint), nameof(dialog.ApiKeysPageText), nameof(dialog.PrivacyNote) }, changed);

            dialog.UseClaude = true;

            Assert.IsTrue(dialog.HasSavedApiKey);
            Assert.AreEqual("Claude API _key", dialog.ApiKeyLabel);
            StringAssert.Contains(dialog.PrivacyNote, "sent to Anthropic");
        }

        [TestMethod]
        public void Find_StaysOpenWithTheReasonWhenItFails()
        {
            _claude.Error = new ValueFinderException("Could not reach Claude. Check the internet connection.");
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(0, _closes.Count);
            Assert.IsTrue(dialog.HasError);
            Assert.AreEqual("Could not reach Claude. Check the internet connection.", dialog.Error);
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void EditingTheTextOrTheKey_ClearsTheError()
        {
            _claude.Error = new ValueFinderException("Claude did not accept the API key. Check that it is complete and still active.");
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
            dialog.FindCommand.Execute(null);

            dialog.NewApiKey = "new key";
            Assert.IsFalse(dialog.HasError);

            dialog.FindCommand.Execute(null);
            dialog.Text = "From Contoso, 12 November";
            Assert.IsFalse(dialog.HasError);
        }

        [TestMethod]
        public void Find_StaysOpenQuietlyWhenCanceled()
        {
            _claude.Error = new OperationCanceledException();
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(0, _closes.Count);
            Assert.IsFalse(dialog.HasError);
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void Find_RefusesATooLongTextWithoutSendingIt()
        {
            FillFromTextViewModel dialog = Create();
            dialog.Text = new string('a', FillFromTextViewModel.MaxTextLength + 1);

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(0, _claude.Calls);
            Assert.AreEqual(0, _closes.Count);
            StringAssert.StartsWith(dialog.Error, "The text is too long.");
        }

        [TestMethod]
        public void Find_NeedsAKeyWhenNoneIsSaved()
        {
            _apiKeys.Claude = null;
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            Assert.IsFalse(dialog.HasSavedApiKey);
            Assert.AreEqual("Paste your Claude API key", dialog.ApiKeyHint);
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));

            dialog.NewApiKey = "new key";
            Assert.IsTrue(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void Find_SavesANewKeyForTheChosenServiceFirst()
        {
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
            dialog.UseOpenAI = true;
            dialog.NewApiKey = "  openai key  ";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual("openai key", _apiKeys.OpenAI);
            Assert.AreEqual("claude key", _apiKeys.Claude);
            Assert.AreEqual("", dialog.NewApiKey, "the box empties; the key is saved");
            Assert.IsTrue(dialog.HasSavedApiKey);
        }

        [TestMethod]
        public void SavedKey_IsNeverShown()
        {
            FillFromTextViewModel dialog = Create();

            Assert.IsTrue(dialog.HasSavedApiKey);
            Assert.AreEqual("", dialog.NewApiKey);
            StringAssert.StartsWith(dialog.ApiKeyHint, "A key is saved.");
        }

        [TestMethod]
        public void RemoveApiKey_DeletesOnlyTheChosenServicesKey()
        {
            _apiKeys.OpenAI = "openai key";
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.RemoveApiKeyCommand.Execute(null);

            Assert.IsNull(_apiKeys.Claude);
            Assert.AreEqual("openai key", _apiKeys.OpenAI);
            Assert.IsFalse(dialog.HasSavedApiKey);
            Assert.IsFalse(dialog.RemoveApiKeyCommand.CanExecute(null));
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void OpenApiKeysPage_OpensThePageOfTheChosenService()
        {
            FillFromTextViewModel dialog = Create();

            dialog.OpenApiKeysPageCommand.Execute(null);
            dialog.UseOpenAI = true;
            dialog.OpenApiKeysPageCommand.Execute(null);

            CollectionAssert.AreEqual(
                new[] { "https://console.anthropic.com/settings/keys", "https://platform.openai.com/api-keys" },
                _urls.Opened.Select(url => url.AbsoluteUri).ToList());
        }
    }
}
