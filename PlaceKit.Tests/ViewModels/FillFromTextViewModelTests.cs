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
        readonly FakeAiService _ai = new FakeAiService();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly List<bool?> _closes = new List<bool?>();

        FillFromTextViewModel Create()
        {
            var dialog = new FillFromTextViewModel(Names, _finder, _ai, _dialogs);
            dialog.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return dialog;
        }

        [TestMethod]
        public void Texts_NameTheChosenService()
        {
            _ai.Provider = AiProvider.OpenAI;
            FillFromTextViewModel dialog = Create();

            StringAssert.Contains(dialog.Intro, "ChatGPT finds the values for the 2 placeholders");
            StringAssert.StartsWith(dialog.ServiceNote, "The text and the placeholder names are sent to OpenAI.");
        }

        [TestMethod]
        public void Guide_KeyAddedInSettings_AsksTheService()
        {
            _ai.HasApiKey = false;
            _ai.OnOpenSettings = () => _ai.HasApiKey = true;
            _finder.Values["Date"] = "19 November 2026";
            FillFromTextViewModel dialog = Create();
            dialog.UseGuide();
            Assert.IsTrue(dialog.IsSample);

            dialog.ChangeServiceCommand.Execute(null);
            dialog.FindCommand.Execute(null);

            Assert.IsFalse(dialog.IsSample);
            Assert.AreEqual(1, _finder.Calls);
            StringAssert.StartsWith(_finder.LastText, "Hamburg, 19 November 2026", "the sample email, now sent");
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
            _finder.Error = new AiServiceException("Could not reach Claude. Check the internet connection.");
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(0, _closes.Count);
            Assert.IsTrue(dialog.HasError);
            Assert.AreEqual("Could not reach Claude. Check the internet connection.", dialog.Error);
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void EditingTheText_ClearsTheError()
        {
            _finder.Error = new AiServiceException("Could not reach Claude. Check the internet connection.");
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
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
        public void AiSettings_ShowsTheServiceChosenThere()
        {
            _finder.Error = new AiServiceException("Claude did not accept the API key. Check that it is complete and still active.");
            FillFromTextViewModel dialog = Create();
            dialog.Text = "From Contoso";
            dialog.FindCommand.Execute(null);
            _ai.OnOpenSettings = () => _ai.Provider = AiProvider.OpenAI;

            dialog.ChangeServiceCommand.Execute(null);

            Assert.AreEqual(1, _ai.SettingsOpened);
            Assert.AreEqual(AiProvider.OpenAI, dialog.Provider);
            StringAssert.Contains(dialog.Intro, "ChatGPT");
            Assert.IsFalse(dialog.HasError, "the error was about the old key");
        }
    }
}
