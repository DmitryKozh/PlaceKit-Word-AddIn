using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    // DPAPI for the current user, values ClaudeApiKey and OpenAIApiKey.
    public sealed class ProtectedApiKeyStore : IApiKeyStore
    {
        const string DefaultKeyPath = @"Software\BoringJob\PlaceKit";

        readonly string _keyPath;

        public ProtectedApiKeyStore()
            : this(DefaultKeyPath)
        {
        }

        internal ProtectedApiKeyStore(string keyPath)
        {
            _keyPath = keyPath;
        }

        public bool HasKey(AiProvider provider) => Load(provider) != null;

        public string Load(AiProvider provider)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                if (!(key?.GetValue(ValueName(provider)) is byte[] data))
                {
                    return null;
                }

                try
                {
                    return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, Entropy(provider), DataProtectionScope.CurrentUser));
                }
                catch (CryptographicException)
                {
                    // saved by another account or damaged, as good as missing
                    return null;
                }
            }
        }

        public void Save(AiProvider provider, string apiKey)
        {
            byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), Entropy(provider), DataProtectionScope.CurrentUser);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue(ValueName(provider), data, RegistryValueKind.Binary);
            }
        }

        public void Delete(AiProvider provider)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: true))
            {
                key?.DeleteValue(ValueName(provider), throwOnMissingValue: false);
            }
        }

        static string ValueName(AiProvider provider) => provider + "ApiKey";

        // Different entropy per service, so one key can't be read as another.
        static byte[] Entropy(AiProvider provider) => Encoding.UTF8.GetBytes($"PlaceKit {provider} API key");
    }
}
