using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    [TestCategory(WordInstance.Category)]
    public class PlaceholderServiceTests
    {
        readonly PlaceholderService _service = new PlaceholderService();
        readonly ProgressRecorder _progress = new ProgressRecorder();

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        [TestMethod]
        public void Create_ConvertsMarkersInEveryStory()
        {
            Word.Document document = SampleDocument();

            PlaceholderOperationResult result = Create(document);

            Assert.AreEqual(12, result.Processed);
            Assert.AreEqual(3, result.Skipped, "broken, nested outer and empty marker");
            Assert.IsFalse(result.WasCanceled);
            Assert.AreEqual(result.Processed + result.Skipped, _progress.Last.Current);
            Assert.IsTrue(WordInstance.Application.ScreenUpdating);

            List<Word.ContentControl> controls = WordInstance.PlaceKitControls(document);
            Assert.AreEqual(12, controls.Count);
            foreach (Word.ContentControl control in controls)
            {
                Assert.AreEqual(Word.WdContentControlType.wdContentControlText, control.Type);
                Assert.AreEqual("PlaceKit-" + control.Title, control.Tag);
                Assert.IsTrue(control.ShowingPlaceholderText);
                Assert.AreEqual(control.Title, control.Range.Text);
            }

            CollectionAssert.AreEquivalent(
                new[] { "Client name", "Order", "Date", "Client name", "A", "B", "C", "D", "Cell", "Box", "Header", "Client name" },
                controls.Select(control => control.Title).ToList());
        }

        [TestMethod]
        public void Create_LeavesInvalidMarkersAsText()
        {
            Word.Document document = SampleDocument();

            Create(document);

            string body = document.Content.Text;
            StringAssert.Contains(body, "{{ x\rnext }}");
            StringAssert.Contains(body, "Nested {{x ");
            StringAssert.Contains(body, "Empty {{}}.");
            Assert.IsFalse(body.Contains("{{Client name}}"));
        }

        [TestMethod]
        public void Create_IsOneUndoEntry()
        {
            Word.Document document = WordInstance.NewDocument("A {{One}} B {{Two}} C");

            Create(document);
            document.Undo(1);

            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            Assert.AreEqual("A {{One}} B {{Two}} C\r", document.Content.Text);
        }

        [TestMethod]
        public void Create_StopsWhenCanceled()
        {
            Word.Document document = WordInstance.NewDocument("{{One}} {{Two}}");
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                PlaceholderOperationResult result = _service.CreatePlaceholders(document, PlaceholderPattern.Default, _progress, cancellation.Token);

                Assert.IsTrue(result.WasCanceled);
                Assert.AreEqual(0, result.Processed);
                Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            }
        }

        [DataTestMethod]
        [DataRow("[[", "]]")]
        [DataRow("<<", ">>")]
        [DataRow("%", "%")]
        [DataRow("${", "}")]
        [DataRow("^", "^")]
        [DataRow("(", ")")]
        public void Create_SupportsCustomMarkers(string opening, string closing)
        {
            var pattern = new PlaceholderPattern(opening, closing);
            Word.Document document = WordInstance.NewDocument($"x {pattern.Format("First")} y {pattern.Format("Second")} z {{{{Other}}}}");

            PlaceholderOperationResult result = _service.CreatePlaceholders(document, pattern, _progress, CancellationToken.None);

            Assert.AreEqual(2, result.Processed);
            CollectionAssert.AreEqual(new[] { "First", "Second" }, WordInstance.PlaceKitControls(document).Select(control => control.Title).ToList());
        }

        [TestMethod]
        public void Create_SkipsMarkerInsideAnotherPlainTextControl()
        {
            Word.Document document = WordInstance.NewDocument("Outer {{Inner}} text");
            document.Content.ContentControls.Add(Word.WdContentControlType.wdContentControlText, document.Content);

            PlaceholderOperationResult result = Create(document);

            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(1, result.Skipped);
            StringAssert.Contains(document.Content.Text, "{{Inner}}");
        }

        [TestMethod]
        public void GetPlaceholders_ListsEachNameOnceInDocumentOrder()
        {
            Word.Document document = SampleDocument();
            Create(document);

            IReadOnlyList<PlaceholderInfo> placeholders = _service.GetPlaceholders(document, _progress, CancellationToken.None);

            Assert.AreEqual(10, placeholders.Count);
            Assert.AreEqual("Client name", placeholders[0].Name);
            Assert.AreEqual(3, placeholders[0].Occurrences, "body twice and the footer");
            Assert.AreEqual("", placeholders[0].Value);
            Assert.IsFalse(placeholders[0].HasMixedValues);
        }

        [TestMethod]
        public void GetPlaceholders_ThrowsWhenCanceled()
        {
            Word.Document document = WordInstance.NewDocument("{{One}}");
            Create(document);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.ThrowsException<OperationCanceledException>(() => _service.GetPlaceholders(document, _progress, cancellation.Token));
            }
        }

        [TestMethod]
        public void Update_WritesEveryOccurrence()
        {
            Word.Document document = SampleDocument();
            Create(document);

            PlaceholderOperationResult result = Update(document, ("Client name", "John\r\nSmith"), ("Order", "42"));

            Assert.AreEqual(4, result.Processed);
            Assert.AreEqual(0, result.Skipped);
            Word.ContentControl client = Control(document, "Client name");
            Assert.AreEqual("John\vSmith", client.Range.Text);
            Assert.IsFalse(client.ShowingPlaceholderText);

            IReadOnlyList<PlaceholderInfo> placeholders = _service.GetPlaceholders(document, _progress, CancellationToken.None);
            Assert.AreEqual("John\r\nSmith", placeholders.First(p => p.Name == "Client name").Value);
            Assert.AreEqual("42", placeholders.First(p => p.Name == "Order").Value);
        }

        [TestMethod]
        public void GetPlaceholders_FlagsOccurrencesWithDifferentValues()
        {
            Word.Document document = SampleDocument();
            Create(document);
            Update(document, ("Client name", "John"));

            WordInstance.PlaceKitControls(document).Last(control => control.Title == "Client name").Range.Text = "Jane";

            Assert.IsTrue(_service.GetPlaceholders(document, _progress, CancellationToken.None).First(p => p.Name == "Client name").HasMixedValues);
        }

        [TestMethod]
        public void Update_WithEmptyValueShowsThePlaceholderTextAgain()
        {
            Word.Document document = WordInstance.NewDocument("{{Date}}");
            Create(document);
            Update(document, ("Date", "12 November"));

            Update(document, ("Date", ""));

            Assert.IsTrue(Control(document, "Date").ShowingPlaceholderText);
        }

        [TestMethod]
        public void SelectPlaceholder_SelectsTheOccurrence()
        {
            Word.Document document = WordInstance.NewDocument("{{Order}} and {{Order}}");
            Create(document);
            Update(document, ("Order", "42"));

            Assert.IsTrue(_service.SelectPlaceholder(document, "Order", 1));
            Assert.AreEqual("42", WordInstance.Application.Selection.Range.Text);
            Assert.IsFalse(_service.SelectPlaceholder(document, "Missing", 0));
        }

        [TestMethod]
        public void Finalize_KeepsValuesAndRestoresMarkersOfEmptyPlaceholders()
        {
            Word.Document document = SampleDocument();
            Create(document);
            Update(document, ("Client name", "John\r\nSmith"), ("Order", "42"));

            PlaceholderOperationResult result = Finalize(document, EmptyPlaceholderAction.KeepMarkers);

            Assert.AreEqual(12, result.Processed);
            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            string body = document.Content.Text;
            StringAssert.Contains(body, "Dear John\vSmith,");
            StringAssert.Contains(body, "order 42 ships on {{Date}}.");
            StringAssert.Contains(body, "Adjacent {{A}}{{B}}.");
            StringAssert.Contains(document.Sections[1].Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text, "Header {{Header}}");
            StringAssert.Contains(document.Shapes[1].TextFrame.TextRange.Text, "Box {{Box}}");
        }

        [TestMethod]
        public void Finalize_IsOneUndoEntry()
        {
            Word.Document document = SampleDocument();
            Create(document);
            Finalize(document, EmptyPlaceholderAction.Delete);

            document.Undo(1);

            Assert.AreEqual(12, WordInstance.PlaceKitControls(document).Count);
        }

        [TestMethod]
        public void Finalize_DeletesEmptyPlaceholdersAndTheSpaceLeftBehind()
        {
            Word.Document document = WordInstance.NewDocument("Dear {{Name}}, order {{Order}} ships {{When}} today. Total: {{Sum}} EUR{{Tail}}.");
            Create(document);
            Update(document, ("Order", "42"));

            PlaceholderOperationResult result = Finalize(document, EmptyPlaceholderAction.Delete);

            Assert.AreEqual(5, result.Processed);
            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            Assert.AreEqual("Dear, order 42 ships today. Total: EUR.\r", document.Content.Text);
        }

        [TestMethod]
        public void Finalize_DeletesInEveryStory()
        {
            Word.Document document = SampleDocument();
            Create(document);

            Finalize(document, EmptyPlaceholderAction.Delete);

            Word.Section section = document.Sections[1];
            StringAssert.StartsWith(document.Content.Text, "Dear, order ships on.\rAgain. Adjacent.\r");
            Assert.AreEqual("Header\r", section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text);
            Assert.AreEqual("Footer\r", section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text);
            Assert.AreEqual("Box", document.Shapes[1].TextFrame.TextRange.Text.TrimEnd('\r'));
        }

        // Values written by Fill are tracked for the reviewers; creating and finalizing are not, since a tracked edit
        // keeps the old text as a deleted revision and a control would never become empty.
        [TestMethod]
        public void TrackChanges_OnlyFillIsTracked()
        {
            Word.Document document = WordInstance.NewDocument("Dear {{Name}}, order {{Order}}.");
            document.TrackRevisions = true;

            Create(document);
            Assert.AreEqual(0, document.Revisions.Count, "creating is not tracked");
            Assert.IsTrue(WordInstance.PlaceKitControls(document).All(control => control.ShowingPlaceholderText));

            document.TrackRevisions = false;
            Update(document, ("Order", "41"));
            document.TrackRevisions = true;
            Update(document, ("Order", "42"));
            Assert.IsTrue(document.Revisions.Count >= 2, "41 deleted and 42 inserted, both tracked");
            Assert.AreEqual("42", Value(document, "Order"), "read without the deleted 41");

            Finalize(document, EmptyPlaceholderAction.Delete);
            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            Assert.IsTrue(document.Revisions.Count >= 2, "the tracked value stays for the reviewers");
            Assert.IsTrue(document.TrackRevisions, "on again");
        }

        [TestMethod]
        public void TrackChanges_AValueDeletedWhileTrackedIsEmpty()
        {
            Word.Document document = WordInstance.NewDocument("Order {{Order}}.");
            Create(document);
            Update(document, ("Order", "41"));
            document.TrackRevisions = true;

            Update(document, ("Order", ""));

            Assert.AreEqual("", Value(document, "Order"));
            CollectionAssert.AreEqual(new[] { "Order" }, _service.GetEmptyPlaceholders(document, _progress, CancellationToken.None).ToList());
            Finalize(document, EmptyPlaceholderAction.KeepMarkers);
            Assert.AreEqual("Order {{Order}}.\r", document.Content.Text);
        }

        [TestMethod]
        public void GetEmptyPlaceholders_ListsEachEmptyOccurrence()
        {
            Word.Document document = SampleDocument();
            Create(document);
            Update(document, ("Order", "42"), ("Date", ""));

            IReadOnlyList<string> empty = _service.GetEmptyPlaceholders(document, _progress, CancellationToken.None);

            Assert.AreEqual(11, empty.Count, "all but Order");
            Assert.AreEqual(3, empty.Count(name => name == "Client name"), "twice in the text, once in the footer");
            CollectionAssert.DoesNotContain(empty.ToList(), "Order");
        }

        // Markers in the body, a table, the header, the footer and a text box, plus three invalid ones.
        static Word.Document SampleDocument()
        {
            Word.Document document = WordInstance.NewDocument(
                "Dear {{Client name}}, order {{ Order }} ships on {{Date}}.\r" +
                "Again {{Client name}}. Adjacent {{A}}{{B}}.\r" +
                "Broken {{ x\rnext }} and {{C}}. Nested {{x {{D}}. Empty {{}}.");

            Word.Range end = document.Content;
            end.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
            document.Tables.Add(end, 1, 2).Cell(1, 1).Range.Text = "{{Cell}}";

            Word.Section section = document.Sections[1];
            section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Header {{Header}}";
            section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Footer {{Client name}}";

            dynamic shapes = document.Shapes;
            dynamic box = shapes.AddTextbox(1, 100, 100, 200, 50);
            box.TextFrame.TextRange.Text = "Box {{Box}}";
            return document;
        }

        PlaceholderOperationResult Create(Word.Document document) =>
            _service.CreatePlaceholders(document, PlaceholderPattern.Default, _progress, CancellationToken.None);

        PlaceholderOperationResult Update(Word.Document document, params (string Name, string Value)[] values) =>
            _service.UpdatePlaceholders(document, values.ToDictionary(v => v.Name, v => v.Value), _progress, CancellationToken.None);

        PlaceholderOperationResult Finalize(Word.Document document, EmptyPlaceholderAction emptyPlaceholders) =>
            _service.FinalizePlaceholders(document, PlaceholderPattern.Default, emptyPlaceholders, _progress, CancellationToken.None);

        string Value(Word.Document document, string name) =>
            _service.GetPlaceholders(document, _progress, CancellationToken.None).Single(placeholder => placeholder.Name == name).Value;

        static Word.ContentControl Control(Word.Document document, string title) =>
            WordInstance.PlaceKitControls(document).First(control => control.Title == title);
    }
}
