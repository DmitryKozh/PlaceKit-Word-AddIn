using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.ViewModels;

namespace PlaceKit.Services
{
    public sealed class AiService : IAiService
    {
        readonly IReadOnlyDictionary<AiProvider, IAiClient> _clients;
        readonly ISettingsService _settings;
        readonly IApiKeyStore _apiKeys;
        readonly IProgressService _progress;
        readonly IDialogService _dialogs;
        readonly Func<SettingsViewModel> _createSettings;

        public AiService(IEnumerable<IAiClient> clients, ISettingsService settings, IApiKeyStore apiKeys, IProgressService progress, IDialogService dialogs, Func<SettingsViewModel> createSettings)
        {
            _clients = clients.ToDictionary(client => client.Provider);
            _settings = settings;
            _apiKeys = apiKeys;
            _progress = progress;
            _dialogs = dialogs;
            _createSettings = createSettings;
        }

        public AiProvider Provider => _settings.Load().AiProvider;

        public bool HasApiKey => _apiKeys.HasKey(Provider);

        public string Send(AiRequest request, string progressTitle)
        {
            AiProvider provider = Provider;
            string apiKey = _apiKeys.Load(provider);
            if (apiKey == null)
            {
                throw new AiServiceException($"Add your {AiProviders.KeyName(provider)} in Settings first.");
            }

            return Run(progressTitle, token => _clients[provider].SendAsync(apiKey, request, token));
        }

        public void TestKey(AiProvider provider, string apiKey) =>
            Run("Testing the connection", async token =>
            {
                await _clients[provider].TestKeyAsync(apiKey, token).ConfigureAwait(false);
                return true;
            });

        public bool EnsureApiKey(string feature)
        {
            if (HasApiKey)
            {
                return true;
            }

            if (!_dialogs.Confirm("Set up AI", $"{feature} uses Claude or ChatGPT with your own API key. Set it up in Settings now?"))
            {
                return false;
            }

            OpenSettings();
            return HasApiKey;
        }

        public void OpenSettings()
        {
            SettingsViewModel settings = _createSettings();
            settings.ShowAiPage = true;
            _dialogs.ShowDialog(settings);
        }

        // Word's thread is blocked while the progress window runs, so the request must not need it.
        T Run<T>(string title, Func<System.Threading.CancellationToken, Task<T>> request) =>
            _progress.Run(title, (progress, token) => Task.Run(() => request(token), token).GetAwaiter().GetResult());
    }
}
