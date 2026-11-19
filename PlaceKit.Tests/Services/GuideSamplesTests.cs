using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class GuideSamplesTests
    {
        static readonly string[] Names = { "Customer", "Order number", "Problem" };

        [TestMethod]
        public void FromText_AnswersWithTheValuesStillInTheText()
        {
            IReadOnlyDictionary<string, string> values = GuideSamples.ValueFinder.FindValues("Contoso Ltd says the order 4711 is late.", Names);

            CollectionAssert.AreEquivalent(new Dictionary<string, string> { ["Customer"] = "Contoso Ltd", ["Order number"] = "4711" }, (Dictionary<string, string>)values);
        }

        // A value counts only with the words around it in the sample email, so "2" isn't found in a date.
        [TestMethod]
        public void FromText_TakesNoShortValuesFromOtherText()
        {
            Assert.ThrowsException<AiServiceException>(() =>
                GuideSamples.ValueFinder.FindValues("Meeting on 12 May 2026 in room 2, about 4711 items.", new[] { "Quantity", "Order number", "Date" }));
        }

        [TestMethod]
        public void FromText_OtherTextNeedsTheRealService()
        {
            AiServiceException error = Assert.ThrowsException<AiServiceException>(() => GuideSamples.ValueFinder.FindValues("Dear Sir or Madam", Names));

            StringAssert.Contains(error.Message, "Set up AI");
        }

        // Only what is in the part Detect reads, such as the selection.
        [TestMethod]
        public void Detect_KeepsToTheTextItReads()
        {
            string document = string.Join("\n", GuideSamples.OrderConfirmation);

            IReadOnlyList<DetectedPlaceholder> found = GuideSamples.Detector.Detect("Dear Anna Berg,", document, "", new string[0], PlaceholderPattern.Default);

            Assert.AreEqual("Contact", ((List<DetectedPlaceholder>)found)[0].Name);
            Assert.AreEqual(1, found.Count);
        }
    }
}
