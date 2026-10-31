using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class DetectRequestTests
    {
        const string Document =
            "Warranty claim\n\nDear Anna Berg,\n\nyour Barista Pro 3 (order 4711) bought on 12 October 2026 is repaired. " +
            "Ms Berg, please collect it.\n\nContoso Ltd\nMain Street 1\nBergmann Logistics";

        static readonly string[] NoNames = new string[0];

        [TestMethod]
        public void Parse_KeepsTextsThatAreInTheDocument()
        {
            IReadOnlyList<DetectedPlaceholder> found = Parse(
                Suggestion("Client name", "Anna Berg", "The customer"),
                Suggestion("Order number", "4711", "Differs per order"),
                Suggestion("Purchase date", " 12 October 2026 ", "Date"));

            CollectionAssert.AreEqual(new[] { "Client name", "Order number", "Purchase date" }, found.Select(p => p.Name).ToList());
            CollectionAssert.AreEqual(new[] { "Anna Berg", "4711", "12 October 2026" }, found.Select(p => p.Text).ToList());
            Assert.AreEqual("The customer", found[0].Reason);
            Assert.AreEqual(1, found[0].Occurrences);
        }

        [TestMethod]
        public void Parse_LeavesOutTextsThatAreNotWholeWordsOfTheDocument()
        {
            IReadOnlyList<DetectedPlaceholder> found = Parse(
                Suggestion("Made up", "Fabrikam", ""),
                Suggestion("Part of a word", "Bergmann Log", ""),
                Suggestion("Inside a number", "471", ""),
                Suggestion("Two lines", "Contoso Ltd\nMain Street 1", ""),
                Suggestion("Too short", "1", ""),
                Suggestion("Surname", "Berg", ""));

            Assert.AreEqual("Berg", found.Single().Text);
            Assert.AreEqual(2, found.Single().Occurrences, "Anna Berg and Ms Berg, not Bergmann");
        }

        [TestMethod]
        public void Parse_KeepsTheFirstSuggestionForAText()
        {
            IReadOnlyList<DetectedPlaceholder> found = Parse(Suggestion("Client name", "Anna Berg", ""), Suggestion("Customer", "Anna Berg", ""));

            Assert.AreEqual("Client name", found.Single().Name);
        }

        [TestMethod]
        public void Parse_MakesNamesUsable()
        {
            IReadOnlyList<DetectedPlaceholder> found = Parse(
                Suggestion("  {{Client\n  name}} ", "Anna Berg", ""),
                Suggestion("Date", "12 October 2026", ""),
                Suggestion("date", "4711", ""),
                Suggestion(new string('x', 60), "Barista Pro 3", ""),
                Suggestion("{}", "Contoso Ltd", ""));

            CollectionAssert.AreEqual(new[] { "Client name", "Date", "date 2", new string('x', 50) }, found.Select(p => p.Name).ToList());
        }

        [TestMethod]
        public void Parse_KeepsTheNameOfAnExistingPlaceholderForEveryText()
        {
            IReadOnlyList<DetectedPlaceholder> found = DetectRequest.Parse(
                Answer(Suggestion("Client name", "Anna Berg", ""), Suggestion("Client name", "Ms Berg", "")), Document, new[] { "Client name" });

            CollectionAssert.AreEqual(new[] { "Client name", "Client name" }, found.Select(p => p.Name).ToList());
        }

        [DataTestMethod]
        [DataRow("not json")]
        [DataRow("{\"values\":[]}")]
        [DataRow("{\"placeholders\":[{\"name\":\"A\",\"text\":\"Anna Berg\"}]}")]
        public void Parse_RejectsAnAnswerOfAnotherShape(string json)
        {
            AiServiceException error = Assert.ThrowsException<AiServiceException>(() => DetectRequest.Parse(json, Document, NoNames));

            Assert.AreEqual("The answer could not be read. Try again.", error.Message);
        }

        [TestMethod]
        public void Prompt_HoldsTheNoteTheExistingNamesTheMarkersAndTheDocument()
        {
            string prompt = DetectRequest.CreatePrompt("The text", "  Only names. ", new[] { "Client name", "Date" }, new PlaceholderPattern("[[", "]]"));

            StringAssert.StartsWith(prompt, "<note>\r\nOnly names.\r\n</note>\r\n\r\n<existing_placeholders>\r\nClient name\r\nDate\r\n</existing_placeholders>");
            StringAssert.EndsWith(prompt, "<markers>\r\n[[name]]\r\n</markers>\r\n\r\n<document>\r\nThe text\r\n</document>");
        }

        [TestMethod]
        public void Prompt_LeavesOutAnEmptyNoteAndNoNames()
        {
            Assert.AreEqual("<markers>\r\n{{name}}\r\n</markers>\r\n\r\n<document>\r\nThe text\r\n</document>", DetectRequest.CreatePrompt("The text", " ", NoNames, PlaceholderPattern.Default));
        }

        [TestMethod]
        public void Request_ThinksMoreThanFromText()
        {
            Assert.AreEqual(AiEffort.Medium, DetectRequest.Create("text", "", NoNames, PlaceholderPattern.Default).Effort);
            Assert.AreEqual(AiEffort.Low, ValueRequest.Create("text", new[] { "Name" }).Effort);
        }

        [TestMethod]
        public void Schema_AllowsNothingElse()
        {
            using (JsonDocument schema = JsonDocument.Parse(JsonSerializer.Serialize(DetectRequest.CreateSchema())))
            {
                JsonElement item = schema.RootElement.GetProperty("properties").GetProperty("placeholders").GetProperty("items");

                Assert.IsFalse(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
                Assert.IsFalse(item.GetProperty("additionalProperties").GetBoolean());
                CollectionAssert.AreEqual(new[] { "name", "text", "reason" }, item.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList());
            }
        }

        static IReadOnlyList<DetectedPlaceholder> Parse(params string[] suggestions) => DetectRequest.Parse(Answer(suggestions), Document, NoNames);

        static string Answer(params string[] suggestions) => "{\"placeholders\":[" + string.Join(",", suggestions) + "]}";

        static string Suggestion(string name, string text, string reason) =>
            JsonSerializer.Serialize(new { name, text, reason });
    }
}
