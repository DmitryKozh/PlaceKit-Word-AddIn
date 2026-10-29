using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests.Services
{
    // Finalize from the ribbon and Save and finalize from the editor, on a document in the hidden Word.
    [TestClass]
    [TestCategory(WordInstance.Category)]
    public class EditorServiceFinalizeTests
    {
        readonly PlaceholderService _placeholders = new PlaceholderService();
        readonly MemorySettings _settings = new MemorySettings();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly FakePanes _panes = new FakePanes();
        EditorService _service;
        Word.Document _document;

        [TestInitialize]
        public void CreateDocument()
        {
            _service = new EditorService(_placeholders, new InlineProgress(), _dialogs, _settings, _panes, new FakeValuesFile());
            _document = WordInstance.NewDocument("Dear {{Name}}, order {{Order}} ships on {{Date}}.");
            _placeholders.CreatePlaceholders(_document, PlaceholderPattern.Default, new ProgressRecorder(), CancellationToken.None);
            Fill(("Order", "42"));
        }

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        [TestMethod]
        public void Finalize_AsksAboutEmptyPlaceholdersAndRemembersTheChoice()
        {
            AnswerQuestion(question =>
            {
                question.RememberChoice = true;
                question.DeleteCommand.Execute(null);
            });

            _service.FinalizeDocument(_document);

            Assert.AreEqual("Name, Date", ((EmptyPlaceholdersViewModel)_dialogs.Shown.Single()).NamesText);
            Assert.AreEqual("Dear, order 42 ships on.\r", _document.Content.Text);
            Assert.IsFalse(_settings.Saved.AskAboutEmptyPlaceholders);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, _settings.Saved.EmptyPlaceholderAction);
            CollectionAssert.AreEqual(new[] { "1 placeholder converted to regular text, 2 empty ones deleted." }, _dialogs.Messages);
        }

        [TestMethod]
        public void Finalize_UsesTheRememberedChoiceWithoutAsking()
        {
            _settings.Current.AskAboutEmptyPlaceholders = false;
            _settings.Current.EmptyPlaceholderAction = EmptyPlaceholderAction.KeepMarkers;

            _service.FinalizeDocument(_document);

            Assert.AreEqual(0, _dialogs.Shown.Count);
            Assert.AreEqual("Dear {{Name}}, order 42 ships on {{Date}}.\r", _document.Content.Text);
            Assert.IsNull(_settings.Saved);
        }

        [TestMethod]
        public void Finalize_CanBeCanceledInTheQuestion()
        {
            AnswerQuestion(question => question.CancelCommand.Execute(null));

            _service.FinalizeDocument(_document);

            Assert.AreEqual(3, WordInstance.PlaceKitControls(_document).Count, "nothing changed");
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void Finalize_DoesNotAskWhenEverythingIsFilledIn()
        {
            Fill(("Name", "Anna"), ("Date", "Monday"));

            _service.FinalizeDocument(_document);

            Assert.AreEqual(0, _dialogs.Shown.Count);
            Assert.AreEqual("Dear Anna, order 42 ships on Monday.\r", _document.Content.Text);
        }

        [TestMethod]
        public void Finalize_OffersToSaveTheEditorsChangesAndClosesIt()
        {
            _service.Open(_document);
            Item(_panes.Shown, "Name").Value = "Anna";
            _dialogs.ConfirmAnswer = true;
            AnswerQuestion(question => question.DeleteCommand.Execute(null));

            _service.FinalizeDocument(_document);

            Assert.AreEqual(1, _dialogs.Confirmations);
            Assert.AreEqual("Dear Anna, order 42 ships on.\r", _document.Content.Text);
            CollectionAssert.AreEqual(new[] { "show", "remove" }, _panes.Calls);
        }

        [TestMethod]
        public void Finalize_WithUnsavedChangesDoesNothingWhenTheUserSaysNo()
        {
            _service.Open(_document);
            Item(_panes.Shown, "Name").Value = "Anna";
            _dialogs.ConfirmAnswer = false;

            _service.FinalizeDocument(_document);

            Assert.AreEqual(3, WordInstance.PlaceKitControls(_document).Count, "not finalized");
            Assert.IsTrue(_panes.Shown.HasChanges, "the edit is kept");
            CollectionAssert.AreEqual(new[] { "show" }, _panes.Calls);
        }

        [TestMethod]
        public void SaveAndFinalize_InTheSidePanel()
        {
            _service.Open(_document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Name").Value = "Anna";
            AnswerQuestion(question => question.KeepMarkersCommand.Execute(null));

            editor.SaveAndFinalizeCommand.Execute(null);

            Assert.AreEqual("Dear Anna, order 42 ships on {{Date}}.\r", _document.Content.Text);
            CollectionAssert.AreEqual(new[] { "show", "remove" }, _panes.Calls);
        }

        [TestMethod]
        public void SaveAndFinalize_InAWindow()
        {
            _settings.Current.EditorPlacement = EditorPlacement.Window;
            _dialogs.OnShowDialog = dialog =>
            {
                if (dialog is EditorViewModel editor)
                {
                    Item(editor, "Date").Value = "Monday";
                    editor.SaveAndFinalizeCommand.Execute(null);
                }
                else
                {
                    ((EmptyPlaceholdersViewModel)dialog).DeleteCommand.Execute(null);
                }
            };

            _service.Open(_document);

            Assert.AreEqual("Dear, order 42 ships on Monday.\r", _document.Content.Text);
            Assert.AreEqual(2, _dialogs.Shown.Count, "the window, then the question");
        }

        [TestMethod]
        public void SaveAndFinalize_CanceledInTheQuestionReturnsToTheEditor()
        {
            _service.Open(_document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Name").Value = "Anna";
            AnswerQuestion(question => question.CancelCommand.Execute(null));

            editor.SaveAndFinalizeCommand.Execute(null);

            Assert.AreEqual(3, WordInstance.PlaceKitControls(_document).Count, "not finalized");
            Assert.AreEqual("Anna", Item(editor, "Name").Value, "saved, and shown again");
            Assert.IsFalse(editor.HasChanges);
            CollectionAssert.AreEqual(new[] { "show", "show" }, _panes.Calls);
        }

        [TestMethod]
        public void SaveAndFinalize_CanceledCheckLeavesNoRequestBehind()
        {
            var service = new EditorService(_placeholders, new StoppingProgress("Checking placeholders", new OperationCanceledException()), _dialogs, _settings, _panes, new FakeValuesFile());
            service.Open(_document);
            EditorViewModel editor = _panes.Shown;

            editor.SaveAndFinalizeCommand.Execute(null);
            editor.CloseCommand.Execute(null);

            Assert.AreEqual(3, WordInstance.PlaceKitControls(_document).Count, "Close only closes");
            CollectionAssert.AreEqual(new[] { "show", "show", "remove" }, _panes.Calls);
        }

        [TestMethod]
        public void SaveAndFinalize_CanceledCheckInAWindowShowsTheWindowAgain()
        {
            _settings.Current.EditorPlacement = EditorPlacement.Window;
            var service = new EditorService(_placeholders, new StoppingProgress("Checking placeholders", new OperationCanceledException()), _dialogs, _settings, _panes, new FakeValuesFile());
            _dialogs.OnShowDialog = dialog =>
            {
                var editor = (EditorViewModel)dialog;
                if (_dialogs.Shown.Count == 1)
                {
                    editor.SaveAndFinalizeCommand.Execute(null);
                }
                else
                {
                    editor.CloseCommand.Execute(null);
                }
            };

            service.Open(_document);

            Assert.AreEqual(2, _dialogs.Shown.Count);
            Assert.AreSame(_dialogs.Shown[0], _dialogs.Shown[1]);
            Assert.AreEqual(3, WordInstance.PlaceKitControls(_document).Count);
        }

        [TestMethod]
        public void SaveAndFinalize_ErrorInAWindowDoesNotLeaveTheEditorBehind()
        {
            _settings.Current.EditorPlacement = EditorPlacement.Window;
            var service = new EditorService(_placeholders, new StoppingProgress("Checking placeholders", new InvalidOperationException("boom")), _dialogs, _settings, _panes, new FakeValuesFile());
            _dialogs.OnShowDialog = dialog => ((EditorViewModel)dialog).SaveAndFinalizeCommand.Execute(null);
            Assert.ThrowsException<InvalidOperationException>(() => service.Open(_document));

            _dialogs.OnShowDialog = dialog => ((EditorViewModel)dialog).CloseCommand.Execute(null);
            service.Open(_document);

            Assert.AreEqual(2, _dialogs.Shown.Count, "the next Fill opens the window again");
            Assert.AreNotSame(_dialogs.Shown[0], _dialogs.Shown[1], "with a new editor");
        }

        void AnswerQuestion(System.Action<EmptyPlaceholdersViewModel> answer) =>
            _dialogs.OnShowDialog = dialog => answer((EmptyPlaceholdersViewModel)dialog);

        void Fill(params (string Name, string Value)[] values) =>
            _placeholders.UpdatePlaceholders(_document, values.ToDictionary(value => value.Name, value => value.Value), new ProgressRecorder(), CancellationToken.None);

        static PlaceholderItemViewModel Item(EditorViewModel editor, string name) => editor.Items.Single(item => item.Name == name);

        // Throws instead of running the operation with the given title, as when the user cancels its progress window.
        sealed class StoppingProgress : IProgressService
        {
            readonly string _title;
            readonly Exception _exception;

            public StoppingProgress(string title, Exception exception)
            {
                _title = title;
                _exception = exception;
            }

            public T Run<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, T> operation) =>
                title == _title ? throw _exception : operation(new ProgressRecorder(), CancellationToken.None);
        }
    }
}
