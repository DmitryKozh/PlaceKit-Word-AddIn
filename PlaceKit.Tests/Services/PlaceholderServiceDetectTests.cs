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
    public class PlaceholderServiceDetectTests
    {
        readonly PlaceholderService _service = new PlaceholderService();
        readonly ProgressRecorder _progress = new ProgressRecorder();

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        [TestMethod]
        public void GetText_ReadsEveryStoryAsPlainText()
        {
            Word.Document document = SampleDocument();

            string text = _service.GetText(document);

            StringAssert.Contains(text, "Dear Anna Berg, your order 4711 ships on 12 October 2026.\nMs Berg");
            StringAssert.Contains(text, "Contoso Ltd");
            StringAssert.Contains(text, "Header Anna Berg");
            StringAssert.Contains(text, "Box 4711");
            Assert.IsFalse(text.Contains("\r") || text.Contains("\a"));
        }

        [TestMethod]
        public void GetSelectedText_IsNullWithoutASelection()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg");
            document.Range(0, 0).Select();

            Assert.IsNull(_service.GetSelectedText(document));

            document.Range(5, 9).Select();
            Assert.AreEqual("Berg", _service.GetSelectedText(document));
        }

        // Text in a content control can't become a placeholder, so the AI service must not suggest it again. Markers
        // still to convert stay as they are.
        [TestMethod]
        public void GetText_ShowsContentControlsAsPlaceholderTags()
        {
            Word.Document document = WordInstance.NewDocument("Dear Anna Berg, order 4711 of Contoso Ltd. {{Date}}");
            Word.Range company = document.Range(30, 41);
            company.ContentControls.Add(Word.WdContentControlType.wdContentControlRichText, company);
            Word.Range order = document.Range(22, 26);
            Word.ContentControl orderControl = order.ContentControls.Add(Word.WdContentControlType.wdContentControlText, order);
            orderControl.Title = "Order";
            document.Sections[1].Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Anna Berg";
            Create(document, ("Client name", "Anna Berg"));

            string text = _service.GetText(document);

            StringAssert.StartsWith(text, "Dear <placeholder name=\"Client name\"/>, order <placeholder name=\"Order\"/> of <placeholder name=\"Field\"/>. {{Date}}");
            StringAssert.Contains(text, "\n<placeholder name=\"Client name\"/>", "the footer");
            Assert.IsFalse(text.Contains("Anna Berg") || text.Contains("4711") || text.Contains("Contoso"));

            document.Range(0, orderControl.Range.End + 1).Select();
            Assert.AreEqual("Dear <placeholder name=\"Client name\"/>, order <placeholder name=\"Order\"/>", _service.GetSelectedText(document));
        }

        [TestMethod]
        public void Create_TurnsEveryWholeWordOccurrenceIntoAPlaceholderWithItsText()
        {
            Word.Document document = SampleDocument();

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"), ("Order", "4711"));

            Assert.AreEqual(2, result.Processed);
            Assert.AreEqual(0, result.Skipped);
            List<Word.ContentControl> controls = WordInstance.PlaceKitControls(document);
            CollectionAssert.AreEquivalent(new[] { "Client name", "Client name", "Order", "Order" }, controls.Select(control => control.Title).ToList(),
                "body and header; body and text box, not 47110");
            foreach (Word.ContentControl control in controls)
            {
                Assert.AreEqual("PlaceKit-" + control.Title, control.Tag);
                Assert.IsFalse(control.ShowingPlaceholderText, "the text stays as the value");
            }

            Assert.AreEqual("Anna Berg", Control(document, "Client name").Range.Text);
            StringAssert.Contains(document.Content.Text, "Ms Berg and 47110");
        }

        [TestMethod]
        public void Create_TakesTheLongestTextFirst()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg and Ms Berg.");

            Create(document, ("Surname", "Berg"), ("Client name", "Anna Berg"));

            CollectionAssert.AreEqual(new[] { "Anna Berg", "Berg" },
                WordInstance.PlaceKitControls(document).OrderBy(control => control.Range.Start).Select(control => control.Range.Text).ToList());
        }

        // Word widens a range that spans table cells to whole rows; this used to loop forever.
        [TestMethod]
        [Timeout(60000)]
        public void Create_GoesOnAfterPlaceholdersThatFillTableCells()
        {
            Word.Document document = WordInstance.NewDocument("Dear Anna Berg, Ms Berg wrote.\rAnna Berg");
            Word.Range end = document.Content;
            end.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
            Word.Table table = document.Tables.Add(end, 2, 2);
            table.Cell(1, 1).Range.Text = "Anna Berg";
            table.Cell(1, 2).Range.Text = "Anna Berg";
            table.Cell(2, 1).Range.Text = "Ms Berg";
            Word.Section section = document.Sections[1];
            section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "From Anna Berg";
            section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Anna Berg, Ms Berg";
            dynamic shapes = document.Shapes;
            dynamic box = shapes.AddTextbox(1, 100, 100, 200, 50);
            box.TextFrame.TextRange.Text = "Anna Berg";

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"), ("Surname", "Berg"));

            Assert.AreEqual(2, result.Processed);
            List<string> titles = WordInstance.PlaceKitControls(document).Select(control => control.Title).ToList();
            Assert.AreEqual(7, titles.Count(title => title == "Client name"), "body twice, both cells of the first row, header, footer, text box");
            Assert.AreEqual(3, titles.Count(title => title == "Surname"), "Ms Berg in the body, the second row and the footer");
        }

        // Guide document: its markers are placeholders already.
        [TestMethod]
        [Timeout(60000)]
        public void Create_GoesOnAfterExistingPlaceholdersInTheGuide()
        {
            Word.Document document = new GuideService(new HiddenWordHost()).CreateGuide(PlaceholderPattern.Default);
            _service.CreatePlaceholders(document, PlaceholderPattern.Default, _progress, CancellationToken.None);
            string text = _service.GetText(document);
            string[] words = text.Split(new[] { ' ', '\n', ',', '.', ':' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3).Distinct().Take(12).ToArray();

            PlaceholderOperationResult result = _service.CreatePlaceholdersFromText(document,
                words.Select(word => new DetectedPlaceholder("P " + word, word, "", 1)).ToList(), _progress, CancellationToken.None);

            Assert.IsFalse(result.WasCanceled);
            Assert.AreEqual(words.Length, result.Processed + result.Skipped);
        }

        [TestMethod]
        public void Create_LeavesTextInOtherControlsAlone()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg wrote. Anna Berg");
            Word.Range existing = document.Range(17, 26);
            existing.ContentControls.Add(Word.WdContentControlType.wdContentControlText, existing);

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"));

            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(1, WordInstance.PlaceKitControls(document).Count);
            Assert.AreEqual(0, Control(document, "Client name").Range.Start - 1, "the first one only");
        }

        [TestMethod]
        public void Create_SkipsTextsThatAreNotThere()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg");

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"), ("Order", "4711"));

            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(1, result.Skipped);
        }

        // As Create makes them: empty, showing the name, in every story.
        [TestMethod]
        public void Create_TurnsMarkersIntoEmptyPlaceholders()
        {
            Word.Document document = WordInstance.NewDocument("Dear {{Customer}}, order 4711.");
            document.Sections[1].Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Claim of {{Customer}}";

            PlaceholderOperationResult result = _service.CreatePlaceholdersFromText(document, new[]
            {
                new DetectedPlaceholder("Client", "{{Customer}}", "", 2, isMarker: true),
                new DetectedPlaceholder("Order", "4711", "", 1),
            }, _progress, CancellationToken.None);

            Assert.AreEqual(2, result.Processed);
            List<Word.ContentControl> clients = WordInstance.PlaceKitControls(document).Where(control => control.Title == "Client").ToList();
            Assert.AreEqual(2, clients.Count, "body and footer");
            Assert.IsTrue(clients.All(control => control.ShowingPlaceholderText && control.Range.Text == "Client"));
            Assert.AreEqual("4711", Control(document, "Order").Range.Text);
            Assert.IsFalse(document.Content.Text.Contains("{{"));
        }

        [TestMethod]
        public void Create_SaysWhyATextWasSkipped()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg wrote.");
            Word.Range existing = document.Range(0, 9);
            existing.ContentControls.Add(Word.WdContentControlType.wdContentControlText, existing);

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"), ("Order", "4711"));

            CollectionAssert.AreEqual(new[] { "\"Anna Berg\": only inside other content controls", "\"4711\": not in the document as a whole word" },
                result.Problems.ToList());
        }

        // Word adds controls only while the document's window is active (error 4605). An invisible document starts
        // without one, like a document whose window lost the focus to the progress window.
        [TestMethod]
        public void Create_ActivatesTheWindowWhenWordRefuses()
        {
            Word.Document document = WordInstance.Application.Documents.Add(Visible: false);
            document.Content.Text = "Anna Berg";

            PlaceholderOperationResult result = Create(document, ("Client name", "Anna Berg"));

            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Problems.Count);
        }

        [TestMethod]
        public void Create_FindsTextWithWordsSpecialCharacters()
        {
            Word.Document document = WordInstance.NewDocument("Price 5^2 EUR");

            Create(document, ("Price", "5^2 EUR"));

            Assert.AreEqual("5^2 EUR", Control(document, "Price").Range.Text);
        }

        [TestMethod]
        public void Create_IsOneUndoEntry()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg, order 4711");

            Create(document, ("Client name", "Anna Berg"), ("Order", "4711"));
            document.Undo(1);

            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            Assert.AreEqual("Anna Berg, order 4711\r", document.Content.Text);
        }

        [TestMethod]
        public void Create_StopsWhenCanceled()
        {
            Word.Document document = WordInstance.NewDocument("Anna Berg, order 4711");
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                PlaceholderOperationResult result = _service.CreatePlaceholdersFromText(document, Placeholders(("Order", "4711")), _progress, cancellation.Token);

                Assert.IsTrue(result.WasCanceled);
                Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            }
        }

        [TestMethod]
        public void SelectText_SelectsTheFirstWholeWordOccurrence()
        {
            Word.Document document = WordInstance.NewDocument("Bergmann and Berg");

            Assert.IsTrue(_service.SelectText(document, "Berg"));
            Assert.AreEqual(13, WordInstance.Application.Selection.Start);
            Assert.IsFalse(_service.SelectText(document, "Fabrikam"));
        }

        // The same names in the body, a table, the header and a text box, and a longer number around one of them.
        static Word.Document SampleDocument()
        {
            Word.Document document = WordInstance.NewDocument(
                "Dear Anna Berg, your order 4711 ships on 12 October 2026.\r" +
                "Ms Berg and 47110.");

            Word.Range end = document.Content;
            end.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
            document.Tables.Add(end, 1, 2).Cell(1, 1).Range.Text = "Contoso Ltd";

            Word.Section section = document.Sections[1];
            section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text = "Header Anna Berg";

            dynamic shapes = document.Shapes;
            dynamic box = shapes.AddTextbox(1, 100, 100, 200, 50);
            box.TextFrame.TextRange.Text = "Box 4711";
            return document;
        }

        PlaceholderOperationResult Create(Word.Document document, params (string Name, string Text)[] placeholders) =>
            _service.CreatePlaceholdersFromText(document, Placeholders(placeholders), _progress, CancellationToken.None);

        static IReadOnlyList<DetectedPlaceholder> Placeholders(params (string Name, string Text)[] placeholders) =>
            placeholders.Select(placeholder => new DetectedPlaceholder(placeholder.Name, placeholder.Text, "", 1)).ToList();

        static Word.ContentControl Control(Word.Document document, string title) =>
            WordInstance.PlaceKitControls(document).First(control => control.Title == title);
    }
}
