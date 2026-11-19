using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    // Commands that need no Word services; the others are covered in a real Word by the service tests.
    [TestClass]
    public class RibbonViewModelTests
    {
        readonly MemorySettings _settings = new MemorySettings();
        readonly FakeGuide _guide = new FakeGuide();
        readonly FakeUrlLauncher _launcher = new FakeUrlLauncher();
        readonly FakeDialogs _dialogs = new FakeDialogs();
        readonly FakeUpdateService _updates = new FakeUpdateService();
        readonly FakeWordHost _word = new FakeWordHost();
        RibbonViewModel _ribbon;

        [TestInitialize]
        public void CreateRibbon()
        {
            _ribbon = new RibbonViewModel(_word, null, _settings, _dialogs, null, null, _guide, new FakeAiService(), new FakeDetector(), TestProduct.Create(), _launcher,
                _updates, () => new SettingsViewModel(_settings, new MemoryApiKeyStore(), new FakeAiService(), _launcher, _updates, TestProduct.Create()),
                () => new WelcomeViewModel(new FakeAiService(), _launcher, TestProduct.Create()));
        }

        [TestMethod]
        public void Welcome_ShowsOnceAndOpensTheSample()
        {
            _dialogs.OnShowDialog = dialog => ((WelcomeViewModel)dialog).OpenSampleCommand.Execute(null);

            Assert.IsTrue(_ribbon.ShowWelcomeOnce());
            Assert.IsTrue(_ribbon.ShowWelcomeOnce(), "settled");

            Assert.IsInstanceOfType(_dialogs.Shown.Single(), typeof(WelcomeViewModel));
            Assert.IsTrue(_settings.Current.WelcomeShown);
            Assert.AreEqual(1, _guide.Patterns.Count, "the sample opened");
        }

        [TestMethod]
        public void Welcome_ClosedOpensNothing()
        {
            _dialogs.OnShowDialog = dialog => ((WelcomeViewModel)dialog).CloseCommand.Execute(null);

            _ribbon.ShowWelcomeOnce();

            Assert.AreEqual(0, _guide.Patterns.Count);
            Assert.IsTrue(_settings.Current.WelcomeShown);
        }

        // A program that runs Word in the background would wait for the window forever.
        [TestMethod]
        public void Welcome_WaitsForAPersonUsingWord()
        {
            _word.IsUserSession = false;

            Assert.IsFalse(_ribbon.ShowWelcomeOnce(), "asked again at the next window");

            Assert.AreEqual(0, _dialogs.Shown.Count);
            Assert.IsFalse(_settings.Current.WelcomeShown, "shown the next time a person starts Word");
        }

        [TestMethod]
        public void Guide_UsesTheMarkersFromSettings()
        {
            _settings.Current.OpeningMarker = "[[";
            _settings.Current.ClosingMarker = "]]";

            _ribbon.GuideCommand.Execute(null);

            Assert.AreEqual("[[Customer]]", _guide.Patterns.Single().Format("Customer"));
        }

        [TestMethod]
        public void Hire_OpensTheWebsite()
        {
            _ribbon.HireCommand.Execute(null);

            Assert.AreEqual("https://boringjob.work/?utm_source=placekit&utm_medium=add-in&utm_content=ribbon", _launcher.Opened.Single().AbsoluteUri);
        }

        [TestMethod]
        public void UpdateButton_FollowsTheUpdateService()
        {
            var changed = new List<string>();
            _ribbon.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);
            Assert.IsFalse(_ribbon.IsUpdateAvailable);

            _updates.Announce(TestRelease.Create("1.3.0"));

            Assert.IsTrue(_ribbon.IsUpdateAvailable);
            Assert.AreEqual("PlaceKit 1.3.0 is available", _ribbon.UpdateTip);
            CollectionAssert.Contains(changed, nameof(RibbonViewModel.IsUpdateAvailable));

            _updates.Announce(null);
            Assert.IsFalse(_ribbon.IsUpdateAvailable);
        }

        [TestMethod]
        public void Update_OpensTheUpdatesPage()
        {
            _updates.Announce(TestRelease.Create("1.3.0"));

            _ribbon.UpdateCommand.Execute(null);

            var settings = (SettingsViewModel)_dialogs.Shown.Single();
            Assert.IsTrue(settings.ShowUpdatesPage);
            Assert.IsTrue(settings.IsUpdateAvailable);
            Assert.AreEqual(0, _updates.Checks, "the update is known already");
        }
    }
}
