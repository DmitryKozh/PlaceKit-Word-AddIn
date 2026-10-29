using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class SettingsViewModelTests
    {
        readonly MemorySettings _store = new MemorySettings();

        [TestMethod]
        public void InvalidMarker_DisablesSave()
        {
            var settings = new SettingsViewModel(_store) { OpeningMarker = "" };

            Assert.IsFalse(settings.IsValid);
            Assert.IsNotNull(((IDataErrorInfo)settings)[nameof(SettingsViewModel.OpeningMarker)]);
            Assert.IsNull(((IDataErrorInfo)settings)[nameof(SettingsViewModel.ClosingMarker)]);
            Assert.IsFalse(settings.SaveCommand.CanExecute(null));
            Assert.AreEqual("-", settings.Preview);
        }

        [TestMethod]
        public void Preview_UsesTheMarkers()
        {
            var settings = new SettingsViewModel(_store) { OpeningMarker = "[[", ClosingMarker = "]]" };

            Assert.AreEqual("[[Client name]]", settings.Preview);
        }

        [TestMethod]
        public void AutoOpen_OnlyMattersWithoutTheSummary()
        {
            var settings = new SettingsViewModel(_store);

            Assert.IsFalse(settings.CanChooseAutoOpen);
            settings.ShowCreateSummary = false;
            Assert.IsTrue(settings.CanChooseAutoOpen);
        }

        [TestMethod]
        public void RadioButtons_SelectThePlacement()
        {
            var settings = new SettingsViewModel(_store);
            Assert.IsTrue(settings.OpenInSidePanel);

            // What WPF does when the user picks the other radio button.
            settings.OpenInWindow = true;
            settings.OpenInSidePanel = false;

            Assert.AreEqual(EditorPlacement.Window, settings.EditorPlacement);
            Assert.IsFalse(settings.OpenInSidePanel);
        }

        [TestMethod]
        public void EmptyPlaceholderAction_OnlyMattersWithoutTheQuestion()
        {
            var settings = new SettingsViewModel(_store);
            Assert.IsFalse(settings.CanChooseEmptyPlaceholderAction);
            Assert.IsTrue(settings.KeepEmptyPlaceholdersAsMarkers);

            settings.AskAboutEmptyPlaceholders = false;
            settings.DeleteEmptyPlaceholders = true;
            settings.KeepEmptyPlaceholdersAsMarkers = false;

            Assert.IsTrue(settings.CanChooseEmptyPlaceholderAction);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, settings.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Save_StoresTheSettingsAndCloses()
        {
            var settings = new SettingsViewModel(_store)
            {
                OpeningMarker = "<<",
                ClosingMarker = ">>",
                ShowCreateSummary = false,
                OpenInWindow = true,
                AskAboutEmptyPlaceholders = false,
                DeleteEmptyPlaceholders = true,
            };
            bool? closedWith = null;
            settings.CloseRequested += (sender, e) => closedWith = e.DialogResult;

            settings.SaveCommand.Execute(null);

            Assert.AreEqual(true, closedWith);
            Assert.AreEqual("<<", _store.Saved.OpeningMarker);
            Assert.IsFalse(_store.Saved.ShowCreateSummary);
            Assert.AreEqual(EditorPlacement.Window, _store.Saved.EditorPlacement);
            Assert.IsFalse(_store.Saved.AskAboutEmptyPlaceholders);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, _store.Saved.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Reset_RestoresTheDefaultsWithoutSaving()
        {
            var settings = new SettingsViewModel(_store) { OpeningMarker = "<<", OpenInWindow = true };

            settings.ResetCommand.Execute(null);

            Assert.AreEqual("{{", settings.OpeningMarker);
            Assert.IsTrue(settings.OpenInSidePanel);
            Assert.IsNull(_store.Saved);
        }
    }
}
