using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tests.Fakes;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    [TestClass]
    public class TextAndErrorTests
    {
        [DataTestMethod]
        [DataRow(0, "0 placeholders")]
        [DataRow(1, "1 placeholder")]
        [DataRow(2, "2 placeholders")]
        public void Quantity(int count, string expected)
        {
            Assert.AreEqual(expected, TextFormat.Quantity(count, "placeholder"));
        }

        [TestMethod]
        public void RunAndReportErrors_ShowsTheError()
        {
            var dialogs = new FakeDialogs();

            dialogs.RunAndReportErrors(() => throw new InvalidOperationException("Word is busy"));

            CollectionAssert.AreEqual(new[] { "Something went wrong: Word is busy" }, dialogs.Messages);
        }

        [TestMethod]
        public void RunAndReportErrors_IgnoresCancellation()
        {
            var dialogs = new FakeDialogs();

            dialogs.RunAndReportErrors(() => throw new OperationCanceledException());

            Assert.AreEqual(0, dialogs.Messages.Count);
        }
    }
}
