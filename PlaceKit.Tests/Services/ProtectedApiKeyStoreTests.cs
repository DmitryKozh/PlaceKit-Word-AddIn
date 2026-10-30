using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using PlaceKit.Models;
using PlaceKit.Services;

namespace PlaceKit.Tests.Services
{
    // Uses a throwaway key, never the user's real settings.
    [TestClass]
    public class ProtectedApiKeyStoreTests
    {
        const string TestRoot = @"Software\BoringJob\PlaceKit.Tests";
        const string TestApiKey = "test key 123";

        string _keyPath;
        ProtectedApiKeyStore _store;

        [TestInitialize]
        public void CreateStore()
        {
            _keyPath = TestRoot + @"\" + Guid.NewGuid().ToString("N");
            _store = new ProtectedApiKeyStore(_keyPath);
        }

        [TestCleanup]
        public void DeleteKey() => Registry.CurrentUser.DeleteSubKeyTree(TestRoot, false);

        [TestMethod]
        public void Load_ReturnsNullWhenNoKeyIsSaved()
        {
            Assert.IsNull(_store.Load(AiProvider.Claude));
            Assert.IsFalse(_store.HasKey(AiProvider.OpenAI));
        }

        [DataTestMethod]
        [DataRow(AiProvider.Claude, "ClaudeApiKey")]
        [DataRow(AiProvider.OpenAI, "OpenAIApiKey")]
        public void Save_StoresTheKeyEncrypted(AiProvider provider, string valueName)
        {
            _store.Save(provider, TestApiKey);

            Assert.AreEqual(TestApiKey, _store.Load(provider));
            Assert.IsTrue(_store.HasKey(provider));
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                byte[] data = (byte[])key.GetValue(valueName);
                Assert.IsFalse(Encoding.UTF8.GetString(data).Contains(TestApiKey));
                Assert.IsFalse(Encoding.Unicode.GetString(data).Contains(TestApiKey));
            }
        }

        [TestMethod]
        public void Keys_AreKeptPerService()
        {
            _store.Save(AiProvider.Claude, "claude key");
            _store.Save(AiProvider.OpenAI, "openai key");

            _store.Delete(AiProvider.Claude);

            Assert.IsNull(_store.Load(AiProvider.Claude));
            Assert.AreEqual("openai key", _store.Load(AiProvider.OpenAI));
        }

        [TestMethod]
        public void Load_DoesNotDecryptAKeyAsAnotherServicesKey()
        {
            _store.Save(AiProvider.Claude, TestApiKey);
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: true))
            {
                key.SetValue("OpenAIApiKey", key.GetValue("ClaudeApiKey"), RegistryValueKind.Binary);
            }

            Assert.IsNull(_store.Load(AiProvider.OpenAI));
        }

        [TestMethod]
        public void Delete_RemovesTheKey()
        {
            _store.Save(AiProvider.Claude, TestApiKey);

            _store.Delete(AiProvider.Claude);
            _store.Delete(AiProvider.Claude);

            Assert.IsNull(_store.Load(AiProvider.Claude));
        }

        [TestMethod]
        public void Load_IgnoresDataItCannotDecrypt()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue("ClaudeApiKey", new byte[] { 1, 2, 3 }, RegistryValueKind.Binary);
            }

            Assert.IsNull(_store.Load(AiProvider.Claude));
            Assert.IsFalse(_store.HasKey(AiProvider.Claude));
        }
    }
}
