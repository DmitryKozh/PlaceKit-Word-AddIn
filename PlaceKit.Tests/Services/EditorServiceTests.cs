using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    [TestCategory(WordInstance.Category)]
    public class EditorServiceTests
    {
        readonly PlaceholderService _placeholders = new PlaceholderService();
        readonly MemorySettings _settings = new MemorySettings();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly FakePanes _panes = new FakePanes();
        EditorService _service;

        [TestInitialize]
        public void CreateService() =>
            _service = new EditorService(_placeholders, new InlineProgress(), _dialogs, _settings, _panes, new FakeValuesFile(), new FakeAiService(), names => null);

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        [TestMethod]
        public void Open_ShowsTheSidePanelByDefault()
        {
            Word.Document document = DocumentWithPlaceholders();

            _service.Open(document);

            CollectionAssert.AreEqual(new[] { "show" }, _panes.Calls);
            Assert.IsTrue(_panes.Shown.IsInSidePanel);
            Assert.AreEqual(2, _panes.Shown.Items.Count);
        }

        [TestMethod]
        public void SaveInSidePanel_WritesToTheDocument()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;

            Item(editor, "Client").Value = "Contoso";
            editor.SaveCommand.Execute(null);

            Assert.AreEqual("Contoso", ControlText(document, "Client"));
            Assert.IsFalse(editor.HasChanges);
            CollectionAssert.AreEqual(new[] { "show" }, _panes.Calls, "the panel stays open");
        }

        [TestMethod]
        public void OpenAgain_BringsBackTheSameEditorWithUnsavedValues()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Order").Value = "PK-1";

            // A new reference to the same document, as the ribbon gets it from ActiveDocument.
            _service.Open(WordInstance.Application.ActiveDocument);

            Assert.AreSame(editor, _panes.Shown);
            Assert.AreEqual("PK-1", Item(editor, "Order").Value);
        }

        [TestMethod]
        public void PopOutAndDock_KeepUnsavedValues()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Order").Value = "PK-1";
            string valueInWindow = null;
            _dialogs.OnShowDialog = window =>
            {
                valueInWindow = Item((EditorViewModel)window, "Order").Value;
                ((EditorViewModel)window).MoveCommand.Execute(null);
            };
            _panes.Calls.Clear();

            editor.MoveCommand.Execute(null);

            Assert.AreSame(editor, _dialogs.Shown.Single());
            Assert.AreEqual("PK-1", valueInWindow);
            CollectionAssert.AreEqual(new[] { "hide", "show" }, _panes.Calls);
            Assert.IsTrue(editor.IsInSidePanel);
        }

        [TestMethod]
        public void SaveInWindow_EndsTheSession()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Order").Value = "PK-1";
            _dialogs.OnShowDialog = window => ((EditorViewModel)window).SaveCommand.Execute(null);
            _panes.Calls.Clear();

            editor.MoveCommand.Execute(null);
            _service.Open(document);

            Assert.AreEqual("PK-1", ControlText(document, "Order"));
            CollectionAssert.AreEqual(new[] { "hide", "remove", "show" }, _panes.Calls);
            Assert.AreNotSame(editor, _panes.Shown, "the next Fill starts a new editor");
        }

        [TestMethod]
        public void Refresh_ReloadsTheOpenEditor()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            document.Content.InsertAfter(" Extra {{Manager}}.");
            Create(document);

            _service.Refresh(document);

            Assert.IsTrue(_panes.Shown.Items.Any(item => item.Name == "Manager"));
        }

        [TestMethod]
        public void ClosingTheDocument_RemovesItsEditor()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            _panes.Calls.Clear();

            ((Word._Document)document).Close(Word.WdSaveOptions.wdDoNotSaveChanges);
            _service.Refresh(WordInstance.NewDocument());

            CollectionAssert.AreEqual(new[] { "remove" }, _panes.Calls);
        }

        [TestMethod]
        public void WindowSetting_OpensAWindow()
        {
            _settings.Current.EditorPlacement = EditorPlacement.Window;
            Word.Document document = DocumentWithPlaceholders();
            _dialogs.OnShowDialog = window => ((EditorViewModel)window).CloseCommand.Execute(null);

            _service.Open(document);

            Assert.IsTrue(((EditorViewModel)_dialogs.Shown.Single()).IsInWindow);
            CollectionAssert.AreEqual(new[] { "hide", "remove" }, _panes.Calls);
        }

        [TestMethod]
        public void DocumentWithoutPlaceholders_ShowsAMessage()
        {
            _service.Open(WordInstance.NewDocument("No markers here."));

            Assert.AreEqual(0, _panes.Calls.Count);
            StringAssert.StartsWith(_dialogs.Messages.Single(), "This document has no placeholders yet.");
        }

        [TestMethod]
        public void CloseDocument_WithoutUnsavedValues_DoesNotAsk()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);

            Assert.IsTrue(_service.CanCloseDocument(document));
            Assert.AreEqual(0, _dialogs.Questions.Count);
            Assert.IsTrue(_service.CanCloseDocument(WordInstance.NewDocument("No editor here.")));
        }

        [TestMethod]
        public void CloseDocument_Yes_SavesTheValuesFirst()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Client").Value = "Contoso";
            _dialogs.QuestionAnswer = true;

            Assert.IsTrue(_service.CanCloseDocument(document));

            StringAssert.Contains(_dialogs.Questions.Single(), "1 unsaved change");
            Assert.AreEqual("Contoso", ControlText(document, "Client"));
            Assert.IsFalse(editor.HasChanges, "asked again only for new changes");
        }

        [TestMethod]
        public void CloseDocument_No_LeavesTheDocumentAsItIs()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            Item(_panes.Shown, "Client").Value = "Contoso";
            _dialogs.QuestionAnswer = false;

            Assert.IsTrue(_service.CanCloseDocument(document));

            Assert.IsTrue(WordInstance.PlaceKitControls(document).Single(control => control.Title == "Client").ShowingPlaceholderText);
        }

        [TestMethod]
        public void CloseDocument_Cancel_KeepsItOpenWithTheValues()
        {
            Word.Document document = DocumentWithPlaceholders();
            _service.Open(document);
            EditorViewModel editor = _panes.Shown;
            Item(editor, "Client").Value = "Contoso";
            _dialogs.QuestionAnswer = null;

            Assert.IsFalse(_service.CanCloseDocument(document));

            Assert.AreEqual("Contoso", Item(editor, "Client").Value);
            Assert.IsTrue(editor.HasChanges);
        }

        Word.Document DocumentWithPlaceholders()
        {
            Word.Document document = WordInstance.NewDocument("To {{Client}} about {{Order}}.");
            Create(document);
            return document;
        }

        void Create(Word.Document document) =>
            _placeholders.CreatePlaceholders(document, PlaceholderPattern.Default, new ProgressRecorder(), CancellationToken.None);

        static PlaceholderItemViewModel Item(EditorViewModel editor, string name) => editor.Items.Single(item => item.Name == name);

        static string ControlText(Word.Document document, string title) =>
            WordInstance.PlaceKitControls(document).Single(control => control.Title == title).Range.Text;
    }
}
