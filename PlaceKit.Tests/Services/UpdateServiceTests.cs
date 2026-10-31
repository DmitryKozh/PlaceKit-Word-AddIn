using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tests.Fakes;

namespace PlaceKit.Tests.Services
{
    [TestClass]
    public class UpdateServiceTests
    {
        readonly FakeReleaseSource _releases = new FakeReleaseSource { Latest = TestRelease.Create("1.2.3") };
        readonly MemorySettings _settings = new MemorySettings();
        readonly InlineSynchronizationContext _wordThread = new InlineSynchronizationContext();
        UpdateService _updates;
        int _changes;

        [TestInitialize]
        public void Create()
        {
            _updates = new UpdateService(_releases, _settings, new InlineProgress(), TestProduct.Create("1.2.3"), _wordThread);
            _updates.AvailableUpdateChanged += (sender, e) => _changes++;
        }

        [TestMethod]
        public void Check_ReturnsNothingForTheSameVersion()
        {
            Assert.IsNull(_updates.Check());
            Assert.IsNull(_updates.AvailableUpdate);
        }

        [TestMethod]
        public void Check_ReturnsAndRemembersANewerVersion()
        {
            _releases.Latest = TestRelease.Create("1.3.0");

            ReleaseInfo update = _updates.Check();

            Assert.AreEqual("1.3.0", update.VersionText);
            Assert.AreSame(update, _updates.AvailableUpdate);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void Check_PassesTheErrorOn()
        {
            _releases.Error = new UpdateCheckException("boringjob.work can't be reached.");

            Assert.ThrowsException<UpdateCheckException>(() => _updates.Check());
        }

        [TestMethod]
        public async Task CheckInBackground_AnnouncesAnUpdateOnWordsThread()
        {
            _releases.Latest = TestRelease.Create("2.0.0");

            await _updates.CheckInBackgroundAsync();

            Assert.AreEqual(1, _wordThread.Posts);
            Assert.AreEqual(1, _changes);
            Assert.AreEqual("2.0.0", _updates.AvailableUpdate.VersionText);
        }

        [TestMethod]
        public async Task CheckInBackground_AsksNothingWhenTurnedOff()
        {
            _settings.Current.CheckForUpdates = false;

            await _updates.CheckInBackgroundAsync();

            Assert.AreEqual(0, _releases.Calls);
        }

        [TestMethod]
        public async Task CheckInBackground_KeepsFailuresQuiet()
        {
            _releases.Error = new InvalidOperationException("boom");

            await _updates.CheckInBackgroundAsync();

            Assert.AreEqual(0, _wordThread.Posts);
            Assert.IsNull(_updates.AvailableUpdate);
        }
    }
}
