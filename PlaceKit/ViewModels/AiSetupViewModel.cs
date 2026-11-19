using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    // Three steps on one page: choose the service, get a key, paste it. Connect tests the key before it is saved,
    // so Detect or From text can go on right after.
    public sealed class AiSetupViewModel : DialogViewModelBase
    {
        readonly string _feature;
        readonly ISettingsService _settings;
        readonly IApiKeyStore _apiKeys;
        readonly IAiService _ai;
        readonly IUrlLauncher _urlLauncher;

        // feature: what asked for the key, such as "Detect"; null from the welcome window
        public AiSetupViewModel(string feature, ISettingsService settings, IApiKeyStore apiKeys, IAiService ai, IUrlLauncher urlLauncher)
            : base("Turn on AI")
        {
            _feature = feature;
            _settings = settings;
            _apiKeys = apiKeys;
            _ai = ai;
            _urlLauncher = urlLauncher;
            AiProvider = settings.Load().AiProvider;
            HasSavedApiKey = apiKeys.HasKey(AiProvider);

            ConnectCommand = new RelayCommand(Connect, () => HasNewApiKey || HasSavedApiKey);
            NotNowCommand = new RelayCommand(() => Close(false));
            OpenKeysPageCommand = new RelayCommand(() => _urlLauncher.Open(AiProviders.KeysPage(AiProvider)));
        }

        public string Intro => (_feature == null ? "Detect and From text use" : _feature + " uses") +
            " Claude or ChatGPT with your own API key. You set it up once, and everything else in PlaceKit works without it.";

        public AiProvider AiProvider { get; set; }

        // ignore false from the other radio button
        public bool UseClaude
        {
            get => AiProvider == AiProvider.Claude;
            set => SetAiProvider(value, AiProvider.Claude);
        }

        public bool UseOpenAI
        {
            get => AiProvider == AiProvider.OpenAI;
            set => SetAiProvider(value, AiProvider.OpenAI);
        }

        public string KeySteps => AiProvider == AiProvider.OpenAI
            ? "Sign in to the OpenAI platform, add credit under Billing and create a key under API keys. A ChatGPT subscription doesn't cover the API. OpenAI bills each use, usually a few cents."
            : "Sign in to the Claude Console, add credit under Billing and create a key under API keys. Anthropic bills each use, usually a few cents.";

        public string KeysPageLink => AiProviders.KeysPageLink(AiProvider);

        // A saved key is never shown.
        public string NewApiKey { get; set; } = string.Empty;

        public bool HasNewApiKey => !string.IsNullOrWhiteSpace(NewApiKey);

        public bool HasSavedApiKey { get; private set; }

        public string ApiKeyHint => HasSavedApiKey ? "A key is saved. Paste a new one or click Connect." : $"Paste your {AiProviders.KeyName(AiProvider)}";

        public string PrivacyNote => $"The key is saved encrypted for your Windows account. Text goes to {AiProviders.Company(AiProvider)} only when you use Detect or From text.";

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public ICommand ConnectCommand { get; }

        public ICommand NotNowCommand { get; }

        public ICommand OpenKeysPageCommand { get; }

        void OnAiProviderChanged()
        {
            HasSavedApiKey = _apiKeys.HasKey(AiProvider);
            NewApiKey = string.Empty;
            Error = null;
        }

        void OnNewApiKeyChanged() => Error = null;

        void SetAiProvider(bool isChecked, AiProvider provider)
        {
            if (isChecked)
            {
                AiProvider = provider;
            }
        }

        void Connect()
        {
            string apiKey = HasNewApiKey ? NewApiKey.Trim() : _apiKeys.Load(AiProvider);
            ApiKeyTest test = ApiKeyTest.Run(_ai, AiProvider, apiKey);
            if (!test.IsConnected)
            {
                Error = test.Error;
                return;
            }

            if (HasNewApiKey)
            {
                _apiKeys.Save(AiProvider, apiKey);
            }

            PlaceKitSettings settings = _settings.Load();
            settings.AiProvider = AiProvider;
            _settings.Save(settings);
            Close(true);
        }
    }
}
