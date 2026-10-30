using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class EditorViewModelTests
    {
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly FakeValuesFile _valuesFile = new FakeValuesFile();
        readonly FakeValueFinder _finder = new FakeValueFinder();
        readonly FakeAiService _ai = new FakeAiService();
        readonly List<bool?> _closes = new List<bool?>();
        FakeDocument _document;
        EditorViewModel _editor;

        [TestInitialize]
        public void Load()
        {
            _document = new FakeDocument(
                new PlaceholderInfo("Client name", "", 2, false),
                new PlaceholderInfo("Order", "42", 1, false),
                new PlaceholderInfo("Date", "", 1, false));
            _editor = new EditorViewModel(_document, _dialogs, _valuesFile, _ai, CreateFillFromText);
            _editor.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            _editor.Reload();
        }

        [TestMethod]
        public void BeforeLoading_IsEmpty()
        {
            var editor = new EditorViewModel(_document, _dialogs, _valuesFile, _ai, CreateFillFromText);

            Assert.IsTrue(editor.IsEmpty);
            Assert.IsFalse(editor.HasNoMatches);
            Assert.IsFalse(editor.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void Reload_ReadsThePlaceholders()
        {
            Assert.AreEqual(1, _document.Loads);
            CollectionAssert.AreEqual(new[] { "Client name", "Order", "Date" }, _editor.Items.Select(item => item.Name).ToList());
            Assert.AreEqual("3 placeholders", _editor.Summary);
        }

        [DataTestMethod]
        [DataRow("ord", 1)]
        [DataRow("42", 1)]
        [DataRow(" DATE ", 1)]
        [DataRow("a", 2)]
        [DataRow("", 3)]
        public void Search_FiltersByNameAndValue(string search, int visible)
        {
            _editor.SearchText = search;

            Assert.AreEqual(visible, _editor.VisibleCount);
            Assert.AreEqual(visible, _editor.ItemsView.Cast<object>().Count());
        }

        [TestMethod]
        public void Search_WithoutMatches()
        {
            var changed = new List<string>();
            _editor.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            _editor.SearchText = "zzz";

            Assert.IsTrue(_editor.HasNoMatches);
            Assert.AreEqual("3 placeholders, 0 shown", _editor.Summary);
            CollectionAssert.IsSubsetOf(new[] { "HasSearchText", "HasNoMatches", "Summary" }, changed);
            Assert.IsTrue(_editor.ClearSearchCommand.CanExecute(null));
        }

        [TestMethod]
        public void EditingAValue_EnablesSave()
        {
            _editor.Items[0].Value = "John";

            Assert.AreEqual(1, _editor.ModifiedCount);
            Assert.IsTrue(_editor.SaveCommand.CanExecute(null));
            Assert.AreEqual("John", _editor.GetChanges()["Client name"]);
            Assert.AreEqual("3 placeholders, 1 modified", _editor.Summary);
        }

        [TestMethod]
        public void Locate_CyclesThroughTheOccurrences()
        {
            for (int i = 0; i < 3; i++)
            {
                _editor.LocateCommand.Execute(_editor.Items[0]);
            }

            CollectionAssert.AreEqual(new[] { "Client name#0", "Client name#1", "Client name#0" }, _document.Selected);
        }

        [TestMethod]
        public void Locate_ReloadsWhenThePlaceholderIsGone()
        {
            _document.SelectResult = false;

            _editor.LocateCommand.Execute(_editor.Items[0]);

            Assert.AreEqual(2, _document.Loads);
            Assert.AreEqual(1, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void CanClose_AsksBeforeDiscardingChanges()
        {
            Assert.IsTrue(_editor.CanClose());

            _editor.Items[0].Value = "John";

            Assert.IsFalse(_editor.CanClose());
            Assert.AreEqual(1, _dialogs.Confirmations);
        }

        [TestMethod]
        public void Reload_KeepsUnsavedValuesAndTheSearch()
        {
            _editor.Items[0].Value = "John";
            _editor.SearchText = "a";
            _document.Placeholders.Add(new PlaceholderInfo("Manager", "", 1, false));

            _editor.Reload();

            Assert.AreEqual(4, _editor.Items.Count);
            Assert.AreEqual("John", _editor.Items[0].Value);
            Assert.AreEqual(1, _editor.ModifiedCount);
            Assert.AreEqual(3, _editor.VisibleCount);
        }

        [TestMethod]
        public void SaveInSidePanel_WritesAndStaysOpen()
        {
            _editor.AttachTo(EditorPlacement.SidePanel);
            _editor.Items[0].Value = "John";

            _editor.SaveCommand.Execute(null);

            Assert.AreEqual("John", _document.Saves.Single()["Client name"]);
            Assert.AreEqual(0, _closes.Count);
            Assert.IsFalse(_editor.HasChanges, "reloaded from the document");
            Assert.AreEqual("John", _editor.Items[0].Value);
        }

        [TestMethod]
        public void SaveInWindow_WritesAndCloses()
        {
            _editor.AttachTo(EditorPlacement.Window);
            _editor.Items[1].Value = "43";

            _editor.SaveCommand.Execute(null);

            Assert.AreEqual(1, _document.Saves.Count);
            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
        }

        [TestMethod]
        public void Move_FromTheSidePanelAsksForAWindow()
        {
            _editor.AttachTo(EditorPlacement.SidePanel);

            _editor.MoveCommand.Execute(null);

            Assert.AreEqual(EditorPlacement.Window, _editor.RequestedPlacement);
            CollectionAssert.AreEqual(new bool?[] { null }, _closes);
        }

        [TestMethod]
        public void Move_FromTheWindowAsksForTheSidePanel()
        {
            _editor.AttachTo(EditorPlacement.Window);
            _editor.Items[0].Value = "John";

            _editor.MoveCommand.Execute(null);

            Assert.AreEqual(EditorPlacement.SidePanel, _editor.RequestedPlacement);
            Assert.AreEqual(0, _dialogs.Confirmations, "the unsaved value moves along");
        }

        [TestMethod]
        public void AttachTo_ClearsTheRequest()
        {
            _editor.AttachTo(EditorPlacement.SidePanel);
            _editor.MoveCommand.Execute(null);

            _editor.AttachTo(EditorPlacement.Window);

            Assert.IsTrue(_editor.IsInWindow);
            Assert.IsNull(_editor.RequestedPlacement);
        }

        [TestMethod]
        public void Close_KeepsTheEditorOpenWhenTheUserSaysNo()
        {
            _editor.AttachTo(EditorPlacement.Window);
            _editor.Items[0].Value = "John";

            _editor.CloseCommand.Execute(null);

            Assert.AreEqual(0, _closes.Count);
        }

        [TestMethod]
        public void Close_ClosesTheSidePanelToo()
        {
            _editor.AttachTo(EditorPlacement.SidePanel);

            _editor.CloseCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { false }, _closes);
            Assert.IsFalse(_editor.FinalizeRequested);
        }

        [TestMethod]
        public void SaveAndFinalize_WritesTheChangesAndLeavesTheRestToTheHost()
        {
            _editor.AttachTo(EditorPlacement.SidePanel);
            _editor.Items[0].Value = "John";

            _editor.SaveAndFinalizeCommand.Execute(null);

            Assert.AreEqual("John", _document.Saves.Single()["Client name"]);
            Assert.IsTrue(_editor.FinalizeRequested);
            CollectionAssert.AreEqual(new bool?[] { true }, _closes);

            _editor.AttachTo(EditorPlacement.SidePanel);
            Assert.IsFalse(_editor.FinalizeRequested, "cleared when the host shows the editor again");
        }

        [TestMethod]
        public void SaveAndFinalize_WithoutChangesSavesNothing()
        {
            _editor.SaveAndFinalizeCommand.Execute(null);

            Assert.AreEqual(0, _document.Saves.Count);
            Assert.IsTrue(_editor.FinalizeRequested);
        }

        [TestMethod]
        public void SaveAndFinalize_NeedsPlaceholders()
        {
            var editor = new EditorViewModel(new FakeDocument(), _dialogs, _valuesFile, _ai, CreateFillFromText);
            editor.Reload();

            Assert.IsFalse(editor.SaveAndFinalizeCommand.CanExecute(null));
        }

        [TestMethod]
        public void Export_WritesTheFilledInValuesInDocumentOrder()
        {
            _editor.Items[0].Value = "John\r\nSmith";
            _dialogs.SaveFileResult = @"C:\Templates\Offer.json";

            _editor.ExportCommand.Execute(null);

            Assert.AreEqual("Offer.json", _dialogs.SuggestedFileName);
            CollectionAssert.AreEqual(
                new[] { new KeyValuePair<string, string>("Client name", "John\r\nSmith"), new KeyValuePair<string, string>("Order", "42") },
                _valuesFile.Files[@"C:\Templates\Offer.json"]);
        }

        [TestMethod]
        public void Export_NeedsAtLeastOneValue()
        {
            _editor.Items[1].Value = "";

            Assert.IsFalse(_editor.ExportCommand.CanExecute(null));
        }

        [TestMethod]
        public void Export_DoesNothingWhenCanceled()
        {
            _editor.ExportCommand.Execute(null);

            Assert.AreEqual(0, _valuesFile.Files.Count);
        }

        [TestMethod]
        public void Import_FillsMatchingNamesAsUnsavedChanges()
        {
            _valuesFile.Files["values.json"] = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("CLIENT NAME", "Contoso"),
                new KeyValuePair<string, string>("Date", "12 November"),
                new KeyValuePair<string, string>("Unknown", "x"),
            };
            _dialogs.OpenFileResult = "values.json";

            _editor.ImportCommand.Execute(null);

            Assert.AreEqual("Contoso", _editor.Items[0].Value);
            Assert.AreEqual("42", _editor.Items[1].Value, "not in the file, so not touched");
            Assert.AreEqual("12 November", _editor.Items[2].Value);
            Assert.AreEqual(2, _editor.ModifiedCount);
            Assert.AreEqual(0, _document.Saves.Count, "nothing is written until the user saves");
            StringAssert.StartsWith(_dialogs.Messages.Single(), "Filled in 2 placeholders from values.json.");
            StringAssert.EndsWith(_dialogs.Messages.Single(), "1 name in the file did not match a placeholder.");
        }

        [TestMethod]
        public void Import_ReportsAFileWithoutMatchingNames()
        {
            _valuesFile.Files["other.json"] = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Unknown", "x") };
            _dialogs.OpenFileResult = "other.json";

            _editor.ImportCommand.Execute(null);

            Assert.AreEqual(0, _editor.ModifiedCount);
            StringAssert.StartsWith(_dialogs.Messages.Single(), "None of the names in other.json");
        }

        [TestMethod]
        public void Import_ReportsAnInvalidFile()
        {
            _valuesFile.ReadError = new InvalidDataException("It is not a PlaceKit values file.");
            _dialogs.OpenFileResult = "broken.json";

            _editor.ImportCommand.Execute(null);

            Assert.AreEqual(0, _editor.ModifiedCount);
            CollectionAssert.AreEqual(new[] { "Could not import broken.json. It is not a PlaceKit values file." }, _dialogs.Messages);
        }

        [TestMethod]
        public void Import_NeedsPlaceholders()
        {
            var editor = new EditorViewModel(new FakeDocument(), _dialogs, _valuesFile, _ai, CreateFillFromText);
            editor.Reload();

            Assert.IsFalse(editor.ImportCommand.CanExecute(null));
            Assert.IsFalse(editor.FillFromTextCommand.CanExecute(null));
        }

        [TestMethod]
        public void FillFromText_FillsTheFoundValuesAsUnsavedChanges()
        {
            _finder.Values["Client name"] = "Contoso";
            _finder.Values["Date"] = "12 November";
            _dialogs.OnShowDialog = FindValuesIn("Contoso orders on 12 November.");

            _editor.FillFromTextCommand.Execute(null);

            CollectionAssert.AreEqual(new[] { "Client name", "Order", "Date" }, _finder.LastNames.ToList());
            Assert.AreEqual("Contoso", _editor.Items[0].Value);
            Assert.AreEqual("42", _editor.Items[1].Value, "not in the text, so not touched");
            Assert.AreEqual("12 November", _editor.Items[2].Value);
            Assert.AreEqual(2, _editor.ModifiedCount);
            Assert.AreEqual(0, _document.Saves.Count, "nothing is written until the user saves");
            StringAssert.StartsWith(_dialogs.Messages.Single(), "Filled in 2 placeholders from the text.");
            StringAssert.EndsWith(_dialogs.Messages.Single(), "The text gave no value for 1 placeholder.");
        }

        [TestMethod]
        public void FillFromText_ReportsWhenNothingIsFound()
        {
            _dialogs.OnShowDialog = FindValuesIn("Hello");

            _editor.FillFromTextCommand.Execute(null);

            Assert.AreEqual(0, _editor.ModifiedCount);
            CollectionAssert.AreEqual(new[] { "No values for these placeholders were found in the text." }, _dialogs.Messages);
        }

        [TestMethod]
        public void FillFromText_WithoutAKey_StopsWhenNoneIsSetUp()
        {
            _ai.HasApiKey = false;

            _editor.FillFromTextCommand.Execute(null);

            CollectionAssert.AreEqual(new[] { "From text" }, _ai.EnsuredFor);
            Assert.AreEqual(0, _dialogs.Shown.Count);
        }

        [TestMethod]
        public void FillFromText_WithoutAKey_GoesOnOnceOneIsSetUp()
        {
            _ai.HasApiKey = false;
            _ai.OnSetUpKey = () => _ai.HasApiKey = true;
            _finder.Values["Client name"] = "Contoso";
            _dialogs.OnShowDialog = FindValuesIn("Contoso");

            _editor.FillFromTextCommand.Execute(null);

            Assert.AreEqual("Contoso", _editor.Items[0].Value);
        }

        [TestMethod]
        public void FillFromText_CanceledChangesNothing()
        {
            _finder.Values["Client name"] = "Contoso";

            _editor.FillFromTextCommand.Execute(null);

            Assert.AreEqual(1, _dialogs.Shown.Count);
            Assert.AreEqual(0, _finder.Calls);
            Assert.AreEqual(0, _editor.ModifiedCount);
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        FillFromTextViewModel CreateFillFromText(IReadOnlyList<string> names) =>
            new FillFromTextViewModel(names, _finder, _ai, _dialogs);

        // What the user does in the Fill from text dialog: paste the text and click Find values.
        static Action<DialogViewModelBase> FindValuesIn(string text) => viewModel =>
        {
            var dialog = (FillFromTextViewModel)viewModel;
            dialog.Text = text;
            dialog.FindCommand.Execute(null);
        };

        [TestMethod]
        public void CommandErrors_AreReportedNotThrown()
        {
            var editor = new EditorViewModel(new FailingDocument(), _dialogs, _valuesFile, _ai, CreateFillFromText);

            editor.RefreshCommand.Execute(null);

            CollectionAssert.AreEqual(new[] { "Something went wrong: boom" }, _dialogs.Messages);
        }
    }
}
