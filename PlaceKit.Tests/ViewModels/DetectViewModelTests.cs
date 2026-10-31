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
    public class DetectViewModelTests
    {
        const string DocumentText = "Dear Anna Berg, your order 4711 of 12 October 2026.";

        readonly FakeDocument _document = new FakeDocument();
        readonly FakeDetector _detector = new FakeDetector();
        readonly FakeAiService _ai = new FakeAiService();
        readonly MemorySettings _settings = new MemorySettings();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly List<bool?> _closes = new List<bool?>();

        [TestInitialize]
        public void Suggest()
        {
            _detector.Found.Add(new DetectedPlaceholder("Client name", "Anna Berg", "The customer", 1));
            _detector.Found.Add(new DetectedPlaceholder("Order number", "4711", "Differs per order", 2));
            _detector.Found.Add(new DetectedPlaceholder("Order date", "12 October 2026", "A date", 1));
        }

        DetectViewModel Create(string selectedText = null, params string[] existingNames)
        {
            var dialog = new DetectViewModel(_document, DocumentText, selectedText, existingNames, _detector, _ai, _settings, _dialogs);
            dialog.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return dialog;
        }

        [TestMethod]
        public void StartsWithTheScanAndTheLastNote()
        {
            _settings.Current.DetectNote = "Only names";
            DetectViewModel dialog = Create();

            Assert.IsTrue(dialog.IsScanStep);
            Assert.AreEqual("Only names", dialog.Note);
            Assert.IsFalse(dialog.HasSelection);
            Assert.AreEqual("_Note for Claude (optional)", dialog.NoteLabel);
            Assert.IsFalse(dialog.CreateCommand.CanExecute(null));
        }

        [TestMethod]
        public void Find_SendsTheDocumentTheNoteAndTheExistingNames()
        {
            DetectViewModel dialog = Create(null, "Signature");
            dialog.Note = " Only names ";

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(DocumentText, _detector.LastText);
            Assert.AreEqual(DocumentText, _detector.LastDocumentText);
            Assert.AreEqual(" Only names ", _detector.LastNote);
            CollectionAssert.AreEqual(new[] { "Signature" }, _detector.LastExistingNames.ToList());
            Assert.AreEqual("Only names", _settings.Saved.DetectNote, "kept for next time");
        }

        [TestMethod]
        public void Find_CanSendOnlyTheSelection()
        {
            DetectViewModel dialog = Create("Anna Berg, your order 4711");
            Assert.IsTrue(dialog.HasSelection);
            StringAssert.StartsWith(dialog.SelectionSize, "Only the selected text, 26 characters");

            // What WPF does when the user picks the second radio button.
            dialog.ScanSelection = true;
            dialog.ScanDocument = false;
            dialog.FindCommand.Execute(null);

            Assert.AreEqual("Anna Berg, your order 4711", _detector.LastText);
            Assert.AreEqual(DocumentText, _detector.LastDocumentText, "the texts are checked against the whole document");
            StringAssert.StartsWith(dialog.SendNote, "The selected text and your note are sent to Anthropic.");
        }

        [TestMethod]
        public void Find_ShowsTheSuggestionsAllChosen()
        {
            DetectViewModel dialog = Create();

            dialog.FindCommand.Execute(null);

            Assert.IsTrue(dialog.IsReviewStep);
            Assert.AreEqual(3, dialog.Items.Count);
            Assert.IsTrue(dialog.Items.All(item => item.IsChosen));
            Assert.AreEqual(3, dialog.ChosenCount);
            Assert.AreEqual("_Create 3 placeholders", dialog.CreateText);
            Assert.AreEqual("2 times", dialog.Items[1].UsesText);
            Assert.AreEqual("once", dialog.Items[0].UsesText);
            Assert.IsTrue(dialog.CreateCommand.CanExecute(null));
        }

        [TestMethod]
        public void Find_StaysOnTheScanWhenItFails()
        {
            _detector.Error = new AiServiceException("Could not reach Claude. Check the internet connection.");
            DetectViewModel dialog = Create();

            dialog.FindCommand.Execute(null);

            Assert.IsTrue(dialog.IsScanStep);
            Assert.AreEqual("Could not reach Claude. Check the internet connection.", dialog.Error);
        }

        [TestMethod]
        public void Find_SaysWhenNothingIsFound()
        {
            _detector.Found.Clear();
            DetectViewModel dialog = Create();

            dialog.FindCommand.Execute(null);

            Assert.IsTrue(dialog.IsScanStep);
            StringAssert.StartsWith(dialog.Error, "No parts that change were found.");
        }

        [TestMethod]
        public void Find_RefusesATooLongTextWithoutSendingIt()
        {
            var dialog = new DetectViewModel(_document, new string('a', DetectViewModel.MaxTextLength + 1), null, new string[0], _detector, _ai, _settings, _dialogs);

            dialog.FindCommand.Execute(null);

            Assert.AreEqual(0, _detector.Calls);
            StringAssert.StartsWith(dialog.Error, $"The document is too long to send: {DetectViewModel.MaxTextLength + 1:N0} characters.");
        }

        [TestMethod]
        public void Find_StaysQuietWhenCanceled()
        {
            _detector.Error = new OperationCanceledException();
            DetectViewModel dialog = Create();

            dialog.FindCommand.Execute(null);

            Assert.IsTrue(dialog.IsScanStep);
            Assert.IsFalse(dialog.HasError);
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void Create_ClosesWithTheChosenAndRenamedPlaceholders()
        {
            DetectViewModel dialog = Create();
            dialog.FindCommand.Execute(null);

            dialog.Items[1].IsChosen = false;
            dialog.Items[2].Name = " Purchase date ";
            Assert.AreEqual("_Create 2 placeholders", dialog.CreateText);
            dialog.CreateCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
            CollectionAssert.AreEqual(new[] { "Client name", "Purchase date" }, dialog.Chosen.Select(p => p.Name).ToList());
            CollectionAssert.AreEqual(new[] { "Anna Berg", "12 October 2026" }, dialog.Chosen.Select(p => p.Text).ToList());
        }

        [TestMethod]
        public void Create_NeedsAChoice()
        {
            DetectViewModel dialog = Create();
            dialog.FindCommand.Execute(null);

            foreach (DetectedItemViewModel item in dialog.Items)
            {
                item.IsChosen = false;
            }

            Assert.IsFalse(dialog.CreateCommand.CanExecute(null));
            Assert.AreEqual("_Create placeholders", dialog.CreateText);
        }

        [TestMethod]
        public void Names_MustBeThereAndDiffer()
        {
            DetectViewModel dialog = Create();
            dialog.FindCommand.Execute(null);

            dialog.Items[0].Name = " ";
            Assert.AreEqual("Every chosen placeholder needs a name.", dialog.Error);
            Assert.IsFalse(dialog.CreateCommand.CanExecute(null));

            dialog.Items[0].Name = "order date";
            StringAssert.StartsWith(dialog.Error, "Two placeholders are named \"order date\".");
            Assert.IsFalse(dialog.CreateCommand.CanExecute(null));

            dialog.Items[2].IsChosen = false;
            Assert.IsFalse(dialog.HasError, "a cleared suggestion doesn't count");
            Assert.IsTrue(dialog.CreateCommand.CanExecute(null));

            dialog.Items[0].Name = "Client {name}";
            Assert.AreEqual("Names can't contain { } [ ] < or >.", dialog.Error);
        }

        [TestMethod]
        public void Names_MayShareAnExistingPlaceholder()
        {
            DetectViewModel dialog = Create(null, "Client name");
            dialog.FindCommand.Execute(null);

            dialog.Items[1].Name = "Client name";

            Assert.IsFalse(dialog.HasError);
            Assert.IsTrue(dialog.CreateCommand.CanExecute(null));
        }

        [TestMethod]
        public void Back_ReturnsToTheScan()
        {
            DetectViewModel dialog = Create();
            dialog.FindCommand.Execute(null);

            dialog.BackCommand.Execute(null);

            Assert.IsTrue(dialog.IsScanStep);
            Assert.IsTrue(dialog.FindCommand.CanExecute(null));
        }

        [TestMethod]
        public void Show_SelectsTheTextInTheDocument()
        {
            DetectViewModel dialog = Create();
            dialog.FindCommand.Execute(null);

            dialog.ShowCommand.Execute(dialog.Items[1]);
            CollectionAssert.AreEqual(new[] { "4711" }, _document.SelectedTexts);

            _document.SelectResult = false;
            dialog.ShowCommand.Execute(dialog.Items[0]);
            Assert.AreEqual("\"Anna Berg\" is no longer in the document.", dialog.Error);
        }

        [TestMethod]
        public void AiSettings_ShowsTheServiceChosenThere()
        {
            DetectViewModel dialog = Create();
            _ai.OnOpenSettings = () => _ai.Provider = AiProvider.OpenAI;

            dialog.ChangeServiceCommand.Execute(null);

            Assert.AreEqual("_Note for ChatGPT (optional)", dialog.NoteLabel);
            StringAssert.Contains(dialog.SendNote, "sent to OpenAI");
        }
    }
}
