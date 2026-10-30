using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PlaceKit.Interfaces;

namespace PlaceKit.Services
{
    // Next to the settings, encrypted with Windows DPAPI: only the same Windows account can read it back.
    public sealed class ProtectedApiKeyStore : IApiKeyStore
    {
        const string DefaultKeyPath = @"Software\BoringJob\PlaceKit";
        const string ApiKeyValue = "ClaudeApiKey";

        static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PlaceKit Claude API key");

        readonly string _keyPath;

        public ProtectedApiKeyStore()
            : this(DefaultKeyPath)
        {
        }

        internal ProtectedApiKeyStore(string keyPath)
        {
            _keyPath = keyPath;
        }

        public bool HasKey => Load() != null;

        public string Load()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                if (!(key?.GetValue(ApiKeyValue) is byte[] data))
                {
                    return null;
                }

                try
                {
                    return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser));
                }
                catch (CryptographicException)
                {
                    // saved by another account or damaged, as good as missing
                    return null;
                }
            }
        }

        public void Save(string apiKey)
        {
            byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), Entropy, DataProtectionScope.CurrentUser);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue(ApiKeyValue, data, RegistryValueKind.Binary);
            }
        }

        public void Delete()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: true))
            {
                key?.DeleteValue(ApiKeyValue, throwOnMissingValue: false);
            }
        }
    }
}
