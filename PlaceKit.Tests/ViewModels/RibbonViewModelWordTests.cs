using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    // The ribbon's Create and Detect on documents in a hidden Word.
    [TestClass]
    [TestCategory(WordInstance.Category)]
    public class RibbonViewModelWordTests
    {
        readonly MemorySettings _settings = new MemorySettings();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly FakeAiService _ai = new FakeAiService { HasApiKey = false };
        readonly HiddenWordHost _word = new HiddenWordHost();
        RibbonViewModel _ribbon;

        [TestInitialize]
        public void CreateRibbon() =>
            _ribbon = new RibbonViewModel(_word, new PlaceholderService(), _settings, _dialogs, new InlineProgress(), new FakeEditors(), new GuideService(_word), _ai,
                new FakeDetector(), TestProduct.Create(), new FakeUrlLauncher(), new FakeUpdateService(),
                () => throw new AssertFailedException("no settings"), () => throw new AssertFailedException("no welcome"));

        [TestCleanup]
        public void CloseDocuments() => WordInstance.CloseDocuments();

        // No markers often means a finished document, which is what Detect is for.
        [TestMethod]
        public void Create_WithoutMarkers_OffersDetect()
        {
            WordInstance.NewDocument("A finished letter to Anna Berg.");
            _dialogs.ConfirmAnswer = true;

            _ribbon.CreateCommand.Execute(null);

            Assert.AreEqual(1, _dialogs.Confirmations);
            CollectionAssert.AreEqual(new[] { "Detect" }, _ai.EnsuredFor, "Detect asks for the key it needs");
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }

        [TestMethod]
        public void Create_WithoutMarkers_DeclinedEndsThere()
        {
            WordInstance.NewDocument("A finished letter to Anna Berg.");

            _ribbon.CreateCommand.Execute(null);

            Assert.AreEqual(1, _dialogs.Confirmations);
            Assert.AreEqual(0, _ai.EnsuredFor.Count);
            Assert.AreEqual(0, _dialogs.Shown.Count);
        }

        [TestMethod]
        public void Detect_InTheGuideWithoutAKey_SuggestsTheSample()
        {
            new GuideService(_word).CreateGuide(PlaceholderPattern.Default);
            DetectViewModel shown = null;
            _dialogs.OnShowDialog = dialog =>
            {
                shown = (DetectViewModel)dialog;
                shown.FindCommand.Execute(null);
            };

            _ribbon.DetectCommand.Execute(null);

            Assert.AreEqual(0, _ai.EnsuredFor.Count, "no key is asked for");
            Assert.IsTrue(shown.IsSample);
            Assert.IsTrue(shown.IsReviewStep);
            Assert.AreEqual(8, shown.Items.Count(item => !item.IsMarker), "the order confirmation");
            Assert.AreEqual(12, shown.Items.Count(item => item.IsMarker), "the claim's markers");
            Assert.AreEqual(0, _dialogs.Messages.Count);
        }
    }
}
