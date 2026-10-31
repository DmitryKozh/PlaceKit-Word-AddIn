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
        RibbonViewModel _ribbon;

        [TestInitialize]
        public void CreateRibbon()
        {
            _ribbon = new RibbonViewModel(null, null, _settings, _dialogs, null, null, _guide, new FakeAiService(), new FakeDetector(), TestProduct.Create(), _launcher,
                _updates, () => new SettingsViewModel(_settings, new MemoryApiKeyStore(), new FakeAiService(), _launcher, _updates, TestProduct.Create()));
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
