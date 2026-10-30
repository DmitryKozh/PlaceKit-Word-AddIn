using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
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
            Assert.IsNull(_store.Load());
            Assert.IsFalse(_store.HasKey);
        }

        [TestMethod]
        public void Save_StoresTheKeyEncrypted()
        {
            _store.Save(TestApiKey);

            Assert.AreEqual(TestApiKey, _store.Load());
            Assert.IsTrue(_store.HasKey);
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                byte[] data = (byte[])key.GetValue("ClaudeApiKey");
                Assert.IsFalse(Encoding.UTF8.GetString(data).Contains(TestApiKey));
                Assert.IsFalse(Encoding.Unicode.GetString(data).Contains(TestApiKey));
            }
        }

        [TestMethod]
        public void Delete_RemovesTheKey()
        {
            _store.Save(TestApiKey);

            _store.Delete();
            _store.Delete();

            Assert.IsNull(_store.Load());
        }

        [TestMethod]
        public void Load_IgnoresDataItCannotDecrypt()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue("ClaudeApiKey", new byte[] { 1, 2, 3 }, RegistryValueKind.Binary);
            }

            Assert.IsNull(_store.Load());
            Assert.IsFalse(_store.HasKey);
        }
    }
}
