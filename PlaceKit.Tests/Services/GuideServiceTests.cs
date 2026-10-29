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
