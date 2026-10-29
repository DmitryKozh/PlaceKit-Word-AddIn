using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class DialogViewModelTests
    {
        [DataTestMethod]
        [DataRow(1, 0, "1 placeholder created")]
        [DataRow(12, 2, "12 placeholders created")]
        public void Summary_Headline(int created, int skipped, string headline)
        {
            var summary = new CreateSummaryViewModel(new PlaceholderOperationResult(created, skipped, false));

            Assert.AreEqual(headline, summary.Headline);
            Assert.AreEqual(skipped > 0, summary.HasSkipped);
        }

        [TestMethod]
        public void Summary_ClosesWithTheAnswer()
        {
            var summary = new CreateSummaryViewModel(new PlaceholderOperationResult(3, 0, false));
            bool? answer = null;
            summary.CloseRequested += (sender, e) => answer = e.DialogResult;

            summary.OpenEditorCommand.Execute(null);
            Assert.AreEqual(true, answer);

            summary.NotNowCommand.Execute(null);
            Assert.AreEqual(false, answer);
        }

        [DataTestMethod]
        [DataRow(new[] { "Date" }, "1 placeholder is not filled in", "Date")]
        [DataRow(new[] { "A", "B", "A" }, "2 placeholders are not filled in", "A, B")]
        [DataRow(new[] { "A", "B", "C", "D", "E", "F" }, "6 placeholders are not filled in", "A, B, C, D and 2\u00A0more")]
        public void EmptyPlaceholders_NamesEachOnce(string[] empty, string headline, string names)
        {
            var question = new EmptyPlaceholdersViewModel(empty, new PlaceholderPattern("[[", "]]"));

            Assert.AreEqual(headline, question.Headline);
            Assert.AreEqual(names, question.NamesText);
            StringAssert.Contains(question.QuestionText, "such as [[" + empty[0] + "]]");
        }

        [TestMethod]
        public void EmptyPlaceholders_ClosesWithTheChoice()
        {
            var question = new EmptyPlaceholdersViewModel(new[] { "Date" }, PlaceholderPattern.Default);
            var closes = new List<bool?>();
            question.CloseRequested += (sender, e) => closes.Add(e.DialogResult);

            question.DeleteCommand.Execute(null);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, question.Choice);

            question.KeepMarkersCommand.Execute(null);
            Assert.AreEqual(EmptyPlaceholderAction.KeepMarkers, question.Choice);

            question.CancelCommand.Execute(null);
            CollectionAssert.AreEqual(new bool?[] { true, true, false }, closes);
        }

        [TestMethod]
        public void Message_ButtonsDependOnTheKind()
        {
            Assert.AreEqual("OK", new MessageViewModel("PlaceKit", "Done.", MessageKind.Information).AcceptText);

            var question = new MessageViewModel("PlaceKit", "Discard?", MessageKind.Question);
            Assert.IsTrue(question.IsQuestion);
            Assert.AreEqual("_Yes", question.AcceptText);
        }

        [TestMethod]
        public void About_ShowsTheProductAndOpensTheWebsite()
        {
            var product = new ProductInfo("PlaceKit", "1.2.3", "Description", "Developer", "Copyright", new Uri("https://boringjob.work"), new Uri("https://boringjob.work/contact/"));
            var launcher = new FakeUrlLauncher();
            var about = new AboutViewModel(product, launcher);

            about.OpenWebsiteCommand.Execute(null);
            about.ContactCommand.Execute(null);

            Assert.AreEqual("About PlaceKit", about.Title);
            Assert.AreEqual("Version 1.2.3", about.VersionText);
            Assert.AreEqual("boringjob.work", about.WebsiteText);
            StringAssert.Contains(about.Introduction, "I'm Developer, a C# and .NET developer");
            CollectionAssert.AreEqual(
                new[]
                {
                    "https://boringjob.work/?utm_source=placekit&utm_medium=add-in&utm_content=about",
                    "https://boringjob.work/contact/?utm_source=placekit&utm_medium=add-in&utm_content=about",
                },
                launcher.Opened.Select(url => url.AbsoluteUri).ToList());
        }

        [TestMethod]
        public void ProductInfo_IsReadFromTheAssembly()
        {
            ProductInfo product = ProductInfo.FromAssembly(typeof(ProductInfo).Assembly);

            Assert.AreEqual("PlaceKit", product.Name);
            Assert.AreEqual("Dzmitry Kazhamiakin", product.Developer);
            Assert.AreEqual(new Uri("https://boringjob.work"), product.Website);
            Assert.AreEqual(new Uri("https://boringjob.work/contact/"), product.ContactPage);
        }
    }
}
