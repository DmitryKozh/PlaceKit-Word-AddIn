using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    [TestClass]
    public class FileLogTests
    {
        string _folder;
        FileLog _log;

        [TestInitialize]
        public void CreateLog()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PlaceKitTests", Guid.NewGuid().ToString("N"));
            _log = new FileLog(Path.Combine(_folder, "PlaceKit.log"));
        }

        [TestCleanup]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }

        [TestMethod]
        public void KeepsWarningsAndErrors_NotTheRest()
        {
            _log.TraceEvent(null, "Word", TraceEventType.Error, 0, "PlaceKit: {0}", "Something broke");
            _log.TraceEvent(null, "Word", TraceEventType.Warning, 0, "PlaceKit: cannot open {0}", "License.txt");
            _log.TraceEvent(null, "Word", TraceEventType.Information, 0, "PlaceKit: started");
            _log.WriteLine("library chatter");

            string[] lines = File.ReadAllLines(_log.FilePath);

            Assert.AreEqual(2, lines.Length);
            StringAssert.EndsWith(lines[0], " Error PlaceKit: Something broke");
            StringAssert.EndsWith(lines[1], " Warning PlaceKit: cannot open License.txt");
        }

        [TestMethod]
        public void BraceInTheFormatDoesNotBreakIt()
        {
            _log.TraceEvent(null, "Word", TraceEventType.Warning, 0, "PlaceKit: {{Name}} {0} {1}", "only one");

            StringAssert.Contains(File.ReadAllText(_log.FilePath), "PlaceKit: {{Name}} {0} {1} only one");
        }

        [TestMethod]
        public void StartsAgainWhenTooBig()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_log.FilePath, new string('x', (int)FileLog.MaxSize + 1));

            _log.TraceEvent(null, "Word", TraceEventType.Error, 0, "PlaceKit: after");

            Assert.AreEqual(FileLog.MaxSize + 1, new FileInfo(_log.OldFilePath).Length);
            StringAssert.EndsWith(File.ReadAllText(_log.FilePath).TrimEnd(), "PlaceKit: after");
        }
    }
}
