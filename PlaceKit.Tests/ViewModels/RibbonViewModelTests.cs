using System;
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
        RibbonViewModel _ribbon;

        [TestInitialize]
        public void CreateRibbon()
        {
            var product = new ProductInfo("PlaceKit", "1.2.3", "Description", "Developer", "Copyright", new Uri("https://boringjob.work"), new Uri("https://boringjob.work/contact/"));
            _ribbon = new RibbonViewModel(null, null, _settings, new FakeDialogs(), null, null, _guide, new FakeAiService(), new FakeDetector(), product, _launcher);
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
    }
}
