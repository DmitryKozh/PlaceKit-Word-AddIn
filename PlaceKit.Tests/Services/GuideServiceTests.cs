using System;
using System.Collections.Generic;
using System.IO;
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
    public class GuideServiceTests
    {
        static readonly string[] SampleNames =
        {
            "Customer", "Customer address", "Date", "Supplier", "Supplier contact", "Claim number",
            "Delivery date", "Order number", "Item", "Quantity", "Problem", "Customer contact",
        };

        readonly PlaceholderService _placeholders = new PlaceholderService();
        readonly GuideService _guide = new GuideService(new HiddenWordHost());

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        [TestMethod]
        public void Guide_OpensAsANewUnchangedDocument()
        {
            Word.Document document = _guide.CreateGuide(PlaceholderPattern.Default);

            Assert.AreEqual(document.FullName, WordInstance.Application.ActiveDocument.FullName);
            Assert.IsTrue(document.Saved, "closing it without changes asks nothing");
            Assert.AreEqual(1, document.Tables.Count);
            StringAssert.StartsWith(document.Content.Text, "Try PlaceKit\r");
        }

        [DataTestMethod]
        [DataRow("Create.")]
        [DataRow("Fill.")]
        [DataRow("Finalize.")]
        [DataRow("Save and finalize")]
        [DataRow("Detect.")]
        [DataRow("From text.")]
        [DataRow("Export and Import.")]
        [DataRow("Settings.")]
        [DataRow("Updates page")]
        public void Guide_NamesEveryFunction(string function)
        {
            StringAssert.Contains(_guide.CreateGuide(PlaceholderPattern.Default).Content.Text, function);
        }

        [TestMethod]
        public void Guide_KeepsTheClaimWholeOnItsOwnPage()
        {
            Word.Document document = _guide.CreateGuide(PlaceholderPattern.Default);

            int PageOf(string start) => (int)document.Paragraphs.Cast<Word.Paragraph>()
                .First(paragraph => paragraph.Range.Text.StartsWith(start))
                .Range.get_Information(Word.WdInformation.wdActiveEndPageNumber);

            Assert.AreEqual(1, PageOf("Each step is a single undo"));
            Assert.AreEqual(2, PageOf("{{Customer}}"));
            Assert.AreEqual(2, PageOf("Kind regards"));
            Assert.AreEqual(3, PageOf("Order confirmation 4711"));
            Assert.AreEqual(3, document.ComputeStatistics(Word.WdStatistic.wdStatisticPages));
        }

        [TestMethod]
        public void Guide_IsRecognizedUntilSaved()
        {
            Word.Document guide = _guide.CreateGuide(PlaceholderPattern.Default);
            Assert.IsTrue(GuideService.IsGuide(guide));
            Assert.IsFalse(GuideService.IsGuide(WordInstance.NewDocument("A letter.")));

            // Saved, it is the user's own document, such as a template for real claims.
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".docx");
            try
            {
                guide.SaveAs2(path);
                Assert.IsFalse(GuideService.IsGuide(guide));
            }
            finally
            {
                ((Word._Document)guide).Close(Word.WdSaveOptions.wdDoNotSaveChanges);
                File.Delete(path);
            }
        }

        // The sample answer goes through the same checks as a real one, against the guide's own text.
        [TestMethod]
        public void SampleDetect_SuggestsThePartsOfTheOrderConfirmation()
        {
            Word.Document document = _guide.CreateGuide(PlaceholderPattern.Default);
            _placeholders.CreatePlaceholders(document, PlaceholderPattern.Default, new ProgressRecorder(), CancellationToken.None);
            string text = _placeholders.GetText(document);
            List<string> existing = _placeholders.GetPlaceholders(document, new ProgressRecorder(), CancellationToken.None).Select(p => p.Name).ToList();

            IReadOnlyList<DetectedPlaceholder> found = GuideSamples.Detector.Detect(text, text, "", existing, PlaceholderPattern.Default);
            PlaceholderOperationResult created = _placeholders.CreatePlaceholdersFromText(document, found, new ProgressRecorder(), CancellationToken.None);

            CollectionAssert.AreEqual(
                new[] { "Client", "Client address", "Contact", "Confirmation number", "Order date", "Product", "Delivery day", "Total" },
                found.Select(placeholder => placeholder.Name).ToList());
            Assert.AreEqual(2, found.Single(placeholder => placeholder.Name == "Confirmation number").Occurrences, "heading and text");
            Assert.AreEqual(found.Count, created.Processed);
            Assert.AreEqual(0, created.Skipped);
        }

        [TestMethod]
        public void SampleFromText_FillsEveryPlaceholderOfTheClaim()
        {
            Word.Document document = _guide.CreateGuide(PlaceholderPattern.Default);
            _placeholders.CreatePlaceholders(document, PlaceholderPattern.Default, new ProgressRecorder(), CancellationToken.None);
            List<string> names = _placeholders.GetPlaceholders(document, new ProgressRecorder(), CancellationToken.None).Select(p => p.Name).ToList();

            IReadOnlyDictionary<string, string> values = GuideSamples.ValueFinder.FindValues(GuideSamples.Email, names);

            CollectionAssert.AreEquivalent(SampleNames, values.Keys.ToList());
        }

        // The guide's own text must not look like a placeholder, whatever the markers.
        [DataTestMethod]
        [DataRow("{{", "}}")]
        [DataRow("[[", "]]")]
        [DataRow("<<", ">>")]
        [DataRow("%", "%")]
        public void Create_FindsOnlyTheSamplePlaceholders(string opening, string closing)
        {
            var pattern = new PlaceholderPattern(opening, closing);
            Word.Document document = _guide.CreateGuide(pattern);

            PlaceholderOperationResult result = _placeholders.CreatePlaceholders(document, pattern, new ProgressRecorder(), CancellationToken.None);
            IReadOnlyList<PlaceholderInfo> placeholders = _placeholders.GetPlaceholders(document, new ProgressRecorder(), CancellationToken.None);

            Assert.AreEqual(17, result.Processed);
            Assert.AreEqual(0, result.Skipped);
            CollectionAssert.AreEquivalent(SampleNames, placeholders.Select(placeholder => placeholder.Name).ToList());
            Assert.AreEqual(3, placeholders.Single(placeholder => placeholder.Name == "Customer").Occurrences, "address, signature and footer");
            Assert.AreEqual(3, placeholders.Single(placeholder => placeholder.Name == "Claim number").Occurrences, "heading, text and footer");
        }

        [TestMethod]
        public void TheStepsOfTheGuide_CreateFillFinalize()
        {
            Word.Document document = _guide.CreateGuide(PlaceholderPattern.Default);

            _placeholders.CreatePlaceholders(document, PlaceholderPattern.Default, new ProgressRecorder(), CancellationToken.None);
            _placeholders.UpdatePlaceholders(document, new Dictionary<string, string> { ["Customer"] = "Contoso Ltd.", ["Supplier contact"] = "Anna Smith" }, new ProgressRecorder(), CancellationToken.None);
            _placeholders.FinalizePlaceholders(document, PlaceholderPattern.Default, EmptyPlaceholderAction.KeepMarkers, new ProgressRecorder(), CancellationToken.None);

            string body = document.Content.Text;
            string footer = document.Sections[1].Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text;
            Assert.AreEqual(0, WordInstance.PlaceKitControls(document).Count);
            StringAssert.Contains(body, "Dear Anna Smith,");
            StringAssert.Contains(footer, "Contoso Ltd.");
            StringAssert.Contains(body, "{{Date}}", "left empty, so a marker again");
        }
    }
}
