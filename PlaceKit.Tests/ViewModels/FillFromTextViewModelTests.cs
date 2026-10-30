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

        readonly FakeValueFinder _finder = new FakeValueFinder();
        readonly MemoryApiKeyStore _apiKeys = new MemoryApiKeyStore { Key = "saved key" };
        readonly FakeUrlLauncher _urls = new FakeUrlLauncher();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly List<bool?> _closes = new List<bool?>();

        FillFromTextViewModel Create()
        {
            var dialog = new FillFromTextViewModel(Names, _finder, _apiKeys, _urls, _dialogs);
            dialog.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return dialog;
        }

        [TestMethod]
        public void Intro_CountsThePlaceholders()
        {
            StringAssert.Contains(Create().Intro, "the 2 placeholders");
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
            _finder.Values["Client name"] = "Contoso";
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.FindCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
            Assert.AreEqual("Contoso", dialog.Values["Client name"]);
            Assert.AreEqual("From Contoso", _finder.LastText);
            CollectionAssert.AreEqual(Names, _finder.LastNames.ToList());
        }

        [TestMethod]
        public void Find_StaysOpenWithTheReasonWhenItFails()
        {
            _finder.Error = new ValueFinderException("Could not reach Claude. Check the internet connection.");
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
            _finder.Error = new ValueFinderException("Claude did not accept the API key. Check that it is complete and still active.");
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
            _finder.Error = new OperationCanceledException();
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

            Assert.AreEqual(0, _finder.Calls);
            Assert.AreEqual(0, _closes.Count);
            StringAssert.StartsWith(dialog.Error, "The text is too long.");
        }

        [TestMethod]
        public void Find_NeedsAKeyWhenNoneIsSaved()
        {
            _apiKeys.Key = null;
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            Assert.IsFalse(dialog.HasSavedApiKey);
            Assert.AreEqual("Paste your Claude API key", dialog.ApiKeyHint);
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));

            dialog.NewApiKey = "new key";
            Assert.IsTrue(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void Find_SavesANewKeyFirst()
        {
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
            dialog.NewApiKey = "  new key  ";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual("new key", _apiKeys.Key);
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
        public void RemoveApiKey_DeletesTheSavedKey()
        {
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.RemoveApiKeyCommand.Execute(null);

            Assert.IsNull(_apiKeys.Key);
            Assert.IsFalse(dialog.HasSavedApiKey);
            Assert.IsFalse(dialog.RemoveApiKeyCommand.CanExecute(null));
            Assert.IsFalse(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void OpenApiKeysPage_OpensTheClaudeConsole()
        {
            Create().OpenApiKeysPageCommand.Execute(null);

            Assert.AreEqual("https://console.anthropic.com/settings/keys", _urls.Opened.Single().AbsoluteUri);
        }
    }
}
