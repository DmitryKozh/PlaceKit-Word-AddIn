using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    // Uses a throwaway key, never the user's real settings.
    [TestClass]
    public class RegistrySettingsServiceTests
    {
        const string TestRoot = @"Software\BoringJob\PlaceKit.Tests";

        string _keyPath;
        RegistrySettingsService _store;

        [TestInitialize]
        public void CreateStore()
        {
            _keyPath = TestRoot + @"\" + Guid.NewGuid().ToString("N");
            _store = new RegistrySettingsService(_keyPath);
        }

        [TestCleanup]
        public void DeleteKey() => Registry.CurrentUser.DeleteSubKeyTree(TestRoot, false);

        [TestMethod]
        public void Load_ReturnsDefaultsWhenNothingIsStored()
        {
            PlaceKitSettings settings = _store.Load();

            Assert.AreEqual("{{", settings.OpeningMarker);
            Assert.AreEqual("}}", settings.ClosingMarker);
            Assert.IsTrue(settings.ShowCreateSummary);
            Assert.IsFalse(settings.OpenEditorAfterCreate);
            Assert.AreEqual(EditorPlacement.SidePanel, settings.EditorPlacement);
            Assert.IsTrue(settings.AskAboutEmptyPlaceholders);
            Assert.AreEqual(EmptyPlaceholderAction.KeepMarkers, settings.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip()
        {
            _store.Save(new PlaceKitSettings
            {
                OpeningMarker = "[[",
                ClosingMarker = "]]",
                ShowCreateSummary = false,
                OpenEditorAfterCreate = true,
                EditorPlacement = EditorPlacement.Window,
                AskAboutEmptyPlaceholders = false,
                EmptyPlaceholderAction = EmptyPlaceholderAction.Delete,
            });

            PlaceKitSettings loaded = _store.Load();

            Assert.AreEqual("[[", loaded.OpeningMarker);
            Assert.AreEqual("]]", loaded.ClosingMarker);
            Assert.IsFalse(loaded.ShowCreateSummary);
            Assert.IsTrue(loaded.OpenEditorAfterCreate);
            Assert.AreEqual(EditorPlacement.Window, loaded.EditorPlacement);
            Assert.IsFalse(loaded.AskAboutEmptyPlaceholders);
            Assert.AreEqual(EmptyPlaceholderAction.Delete, loaded.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Save_StoresThePlacementByName()
        {
            _store.Save(new PlaceKitSettings { EditorPlacement = EditorPlacement.Window });

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                Assert.AreEqual("Window", key.GetValue("EditorPlacement"));
            }
        }

        [TestMethod]
        public void Load_FallsBackToDefaultsForInvalidValues()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue("OpeningMarker", "has space");
                key.SetValue("ClosingMarker", "", RegistryValueKind.String);
                key.SetValue("ShowCreateSummary", "yes", RegistryValueKind.String);
                key.SetValue("EditorPlacement", "7");
                key.SetValue("EmptyPlaceholderAction", "Shred");
            }

            PlaceKitSettings settings = _store.Load();

            Assert.AreEqual("{{", settings.OpeningMarker);
            Assert.AreEqual("}}", settings.ClosingMarker);
            Assert.IsTrue(settings.ShowCreateSummary);
            Assert.AreEqual(EditorPlacement.SidePanel, settings.EditorPlacement);
            Assert.AreEqual(EmptyPlaceholderAction.KeepMarkers, settings.EmptyPlaceholderAction);
        }

        [TestMethod]
        public void Location_IsTheFullKeyName()
        {
            Assert.AreEqual(@"HKEY_CURRENT_USER\Software\BoringJob\PlaceKit", new RegistrySettingsService().Location);
        }
    }
}
