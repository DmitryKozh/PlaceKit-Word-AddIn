using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class PlaceholderValuesFileTests
    {
        readonly PlaceholderValuesFile _file = new PlaceholderValuesFile();
        string _folder;

        [TestInitialize]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PlaceKit.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void DeleteFolder() => Directory.Delete(_folder, true);

        [TestMethod]
        public void WriteAndRead_RoundTrip()
        {
            string path = Path.Combine(_folder, "values.json");
            var values = new[]
            {
                Pair("Client name", "Contoso \"Ltd.\""),
                Pair("Address", "221B Baker Street" + Environment.NewLine + "London"),
                Pair("Дата", "12/11/2026"),
            };

            _file.Write(path, values);
            IReadOnlyDictionary<string, string> read = _file.Read(path);

            Assert.AreEqual(3, read.Count);
            Assert.AreEqual("Contoso \"Ltd.\"", read["Client name"]);
            Assert.AreEqual("221B Baker Street" + Environment.NewLine + "London", read["Address"]);
            Assert.AreEqual("12/11/2026", read["Дата"]);
        }

        [TestMethod]
        public void Write_ProducesReadableJson()
        {
            string path = Path.Combine(_folder, "values.json");

            _file.Write(path, new[] { Pair("Client name", "Contoso"), Pair("Address", "Line 1\r\nLine 2") });

            string expected = "{\r\n  \"Client name\": \"Contoso\",\r\n  \"Address\": \"Line 1\\nLine 2\"\r\n}";
            Assert.AreEqual(expected, File.ReadAllText(path));
            CollectionAssert.AreNotEqual(Encoding.UTF8.GetPreamble(), Head(File.ReadAllBytes(path), 3), "no byte order mark");
        }

        [TestMethod]
        public void Read_AcceptsHandWrittenFiles()
        {
            string path = Write("{ \"client NAME\": \"Contoso\", \"Amount\": 42, \"Rate\": 1.5, \"Empty\": null, \"Lines\": \"a\\r\\nb\", \"Due\": \"2026-11-12T10:00:00\" }");

            IReadOnlyDictionary<string, string> read = _file.Read(path);

            Assert.AreEqual("Contoso", read["Client name"], "names are matched ignoring case");
            Assert.AreEqual("42", read["Amount"]);
            Assert.AreEqual("1.5", read["Rate"]);
            Assert.IsFalse(read.ContainsKey("Empty"));
            Assert.AreEqual("a" + Environment.NewLine + "b", read["Lines"]);
            Assert.AreEqual("2026-11-12T10:00:00", read["Due"], "dates stay text");
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("not json")]
        [DataRow("[ \"Client name\" ]")]
        [DataRow("{ \"Client name\": ")]
        [DataRow("{ \"Client name\": { \"first\": \"John\" } }")]
        [DataRow("{ \"Client name\": [ \"John\" ] }")]
        [DataRow("{ \"Approved\": true }")]
        public void Read_RejectsOtherFormats(string content)
        {
            string path = Write(content);

            Assert.ThrowsException<InvalidDataException>(() => _file.Read(path));
        }

        [TestMethod]
        public void Read_ThrowsForAMissingFile()
        {
            Assert.ThrowsException<FileNotFoundException>(() => _file.Read(Path.Combine(_folder, "missing.json")));
        }

        string Write(string content)
        {
            string path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, content);
            return path;
        }

        static KeyValuePair<string, string> Pair(string name, string value) => new KeyValuePair<string, string>(name, value);

        static byte[] Head(byte[] bytes, int count)
        {
            var head = new byte[Math.Min(count, bytes.Length)];
            Array.Copy(bytes, head, head.Length);
            return head;
        }
    }
}
