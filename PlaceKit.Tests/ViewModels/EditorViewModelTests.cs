using System.Collections.Generic;
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
            _editor = new EditorViewModel(_document, _dialogs);
            _editor.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            _editor.Reload();
        }

        [TestMethod]
        public void BeforeLoading_IsEmpty()
        {
            var editor = new EditorViewModel(_document, _dialogs);

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
        public void Cancel_KeepsTheWindowOpenWhenTheUserSaysNo()
        {
            _editor.AttachTo(EditorPlacement.Window);
            _editor.Items[0].Value = "John";

            _editor.CancelCommand.Execute(null);

            Assert.AreEqual(0, _closes.Count);
        }

        [TestMethod]
        public void CommandErrors_AreReportedNotThrown()
        {
            var editor = new EditorViewModel(new FailingDocument(), _dialogs);

            editor.RefreshCommand.Execute(null);

            CollectionAssert.AreEqual(new[] { "Something went wrong: boom" }, _dialogs.Messages);
        }
    }
}
