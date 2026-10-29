using System;
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
            var product = new ProductInfo("PlaceKit", "1.2.3", "Description", "Developer", "Copyright", new Uri("https://boringjob.work"));
            var launcher = new FakeUrlLauncher();
            var about = new AboutViewModel(product, launcher);

            about.OpenWebsiteCommand.Execute(null);

            Assert.AreEqual("About PlaceKit", about.Title);
            Assert.AreEqual("Version 1.2.3", about.VersionText);
            Assert.AreEqual("boringjob.work", about.WebsiteText);
            Assert.AreEqual(product.Website, launcher.Opened[0]);
        }

        [TestMethod]
        public void ProductInfo_IsReadFromTheAssembly()
        {
            ProductInfo product = ProductInfo.FromAssembly(typeof(ProductInfo).Assembly);

            Assert.AreEqual("PlaceKit", product.Name);
            Assert.AreEqual("Dzmitry Kazhamiakin", product.Developer);
            Assert.AreEqual(new Uri("https://boringjob.work"), product.Website);
        }
    }
}
