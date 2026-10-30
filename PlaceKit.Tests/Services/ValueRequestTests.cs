using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class ValueRequestTests
    {
        static readonly string[] Names = { "Client name", "Order", "Date" };

        [TestMethod]
        public void ParseValues_ReadsTheValuesOfTheAskedNames()
        {
            IReadOnlyDictionary<string, string> values = ValueRequest.ParseValues(
                "{\"values\":[{\"name\":\"Client name\",\"value\":\" Contoso \"},{\"name\":\"Date\",\"value\":\"12 November 2026\"}]}", Names);

            Assert.AreEqual(2, values.Count);
            Assert.AreEqual("Contoso", values["Client name"]);
            Assert.AreEqual("12 November 2026", values["Date"]);
        }

        [TestMethod]
        public void ParseValues_LeavesOutOtherNamesEmptyValuesAndRepeats()
        {
            IReadOnlyDictionary<string, string> values = ValueRequest.ParseValues(
                "{\"values\":[" +
                "{\"name\":\"Unknown\",\"value\":\"x\"}," +
                "{\"name\":\"client name\",\"value\":\"wrong case\"}," +
                "{\"name\":\"Order\",\"value\":\"  \"}," +
                "{\"name\":\"Date\",\"value\":\"first\"}," +
                "{\"name\":\"Date\",\"value\":\"second\"}]}", Names);

            CollectionAssert.AreEqual(new[] { "Date" }, values.Keys.ToList());
            Assert.AreEqual("first", values["Date"]);
        }

        [TestMethod]
        public void ParseValues_KeepsLineBreaks()
        {
            IReadOnlyDictionary<string, string> values = ValueRequest.ParseValues(
                "{\"values\":[{\"name\":\"Client name\",\"value\":\"Contoso\\nMain Street 1\"}]}", Names);

            Assert.AreEqual("Contoso\nMain Street 1", values["Client name"]);
        }

        [DataTestMethod]
        [DataRow("not json")]
        [DataRow("{\"other\":[]}")]
        [DataRow("{\"values\":[{\"name\":1,\"value\":\"x\"}]}")]
        [DataRow("{\"values\":[{\"value\":\"x\"}]}")]
        public void ParseValues_RejectsAnAnswerOfAnotherShape(string json)
        {
            AiServiceException error = Assert.ThrowsException<AiServiceException>(() => ValueRequest.ParseValues(json, Names));

            Assert.AreEqual("The answer could not be read. Try again.", error.Message);
        }

        [TestMethod]
        public void Schema_AllowsOnlyTheGivenNames()
        {
            using (JsonDocument schema = JsonDocument.Parse(JsonSerializer.Serialize(ValueRequest.CreateSchema(Names))))
            {
                JsonElement root = schema.RootElement;
                JsonElement item = root.GetProperty("properties").GetProperty("values").GetProperty("items");
                List<string> allowed = item.GetProperty("properties").GetProperty("name").GetProperty("enum").EnumerateArray().Select(name => name.GetString()).ToList();

                CollectionAssert.AreEqual(Names, allowed);
                Assert.IsFalse(root.GetProperty("additionalProperties").GetBoolean());
                Assert.IsFalse(item.GetProperty("additionalProperties").GetBoolean());
                CollectionAssert.AreEqual(new[] { "name", "value" }, item.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList());
            }
        }

        [TestMethod]
        public void Prompt_HoldsTheNamesAndTheText()
        {
            string prompt = ValueRequest.CreatePrompt("Hello,\nthe order is 42.", Names);

            Assert.AreEqual(
                "<placeholders>" + Environment.NewLine +
                "Client name" + Environment.NewLine +
                "Order" + Environment.NewLine +
                "Date" + Environment.NewLine +
                "</placeholders>" + Environment.NewLine +
                Environment.NewLine +
                "<text>" + Environment.NewLine +
                "Hello,\nthe order is 42." + Environment.NewLine +
                "</text>",
                prompt);
        }
    }
}
