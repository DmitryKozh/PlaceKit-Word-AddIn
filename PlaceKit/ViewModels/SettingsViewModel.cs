using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    // Keys are saved or removed on Save, like the other settings.
    public sealed class SettingsViewModel : DialogViewModelBase, IDataErrorInfo
    {
        const string PreviewName = "Client name";

        readonly ISettingsService _settingsService;
        readonly IApiKeyStore _apiKeys;
        readonly IAiService _ai;
        readonly IUrlLauncher _urlLauncher;
        readonly Dictionary<AiProvider, string> _newApiKeys = new Dictionary<AiProvider, string>();
        readonly HashSet<AiProvider> _removedApiKeys = new HashSet<AiProvider>();

        public SettingsViewModel(ISettingsService settingsService, IApiKeyStore apiKeys, IAiService ai, IUrlLauncher urlLauncher)
            : base("PlaceKit settings")
        {
            _settingsService = settingsService;
            _apiKeys = apiKeys;
            _ai = ai;
            _urlLauncher = urlLauncher;
            StorageLocation = settingsService.Location;
            Apply(settingsService.Load());
            HasSavedApiKey = apiKeys.HasKey(AiProvider);

            SaveCommand = new RelayCommand(Save, () => IsValid);
            CancelCommand = new RelayCommand(() => Close(false));
            ResetCommand = new RelayCommand(() => Apply(new PlaceKitSettings()));
            SwitchPageCommand = new RelayCommand(() => ShowAiPage = !ShowAiPage);
            RemoveApiKeyCommand = new RelayCommand(RemoveApiKey, () => HasSavedApiKey);
            TestConnectionCommand = new RelayCommand(TestConnection, () => HasNewApiKey || HasSavedApiKey);
            OpenApiKeysPageCommand = new RelayCommand(() => _urlLauncher.Open(AiProviders.KeysPage(AiProvider)));
        }

        public bool ShowAiPage { get; set; }

        // ignore false from the other page's button
        public bool ShowGeneralPage
        {
            get => !ShowAiPage;
            set
            {
                if (value)
                {
                    ShowAiPage = false;
                }
            }
        }

        public string OpeningMarker { get; set; }

        public string ClosingMarker { get; set; }

        public bool ShowCreateSummary { get; set; }

        public bool OpenEditorAfterCreate { get; set; }

        public bool CanChooseAutoOpen => !ShowCreateSummary;

        public EditorPlacement EditorPlacement { get; set; }

        // ignore false from the other radio button
        public bool OpenInSidePanel
        {
            get => EditorPlacement == EditorPlacement.SidePanel;
            set => SetPlacement(value, EditorPlacement.SidePanel);
        }

        public bool OpenInWindow
        {
            get => EditorPlacement == EditorPlacement.Window;
            set => SetPlacement(value, EditorPlacement.Window);
        }

        public bool AskAboutEmptyPlaceholders { get; set; }

        public bool CanChooseEmptyPlaceholderAction => !AskAboutEmptyPlaceholders;

        public EmptyPlaceholderAction EmptyPlaceholderAction { get; set; }

        public bool KeepEmptyPlaceholdersAsMarkers
        {
            get => EmptyPlaceholderAction == EmptyPlaceholderAction.KeepMarkers;
            set => SetEmptyPlaceholderAction(value, EmptyPlaceholderAction.KeepMarkers);
        }

        public bool DeleteEmptyPlaceholders
        {
            get => EmptyPlaceholderAction == EmptyPlaceholderAction.Delete;
            set => SetEmptyPlaceholderAction(value, EmptyPlaceholderAction.Delete);
        }

        public AiProvider AiProvider { get; set; }

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

        // A saved key is never shown.
        public string NewApiKey
        {
            get => _newApiKeys.TryGetValue(AiProvider, out string apiKey) ? apiKey : string.Empty;
            set => _newApiKeys[AiProvider] = value ?? string.Empty;
        }

        public bool HasNewApiKey => !string.IsNullOrWhiteSpace(NewApiKey);

        public bool HasSavedApiKey { get; private set; }

        public string ApiKeyLabel => AiProvider == AiProvider.OpenAI ? "OpenAI API _key" : "Claude API _key";

        public string ApiKeyHint => HasSavedApiKey ? "A key is saved. Paste a new one to replace it." : $"Paste your {AiProviders.KeyName(AiProvider)}";

        public string ApiKeysPageLink => AiProviders.KeysPageLink(AiProvider);

        public string PrivacyNote => $"Text goes to {AiProviders.Company(AiProvider)} only when you use From text, and {AiProviders.Company(AiProvider)} bills your API key for it.";

        public string ConnectionMessage { get; private set; }

        public bool IsConnected { get; private set; }

        public bool HasConnectionMessage => ConnectionMessage != null;

        public bool IsValid =>
            PlaceholderPattern.ValidateMarker(OpeningMarker) == null && PlaceholderPattern.ValidateMarker(ClosingMarker) == null;

        public string Preview => IsValid ? OpeningMarker + PreviewName + ClosingMarker : "-";

        public string StorageLocation { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand SwitchPageCommand { get; }

        public ICommand RemoveApiKeyCommand { get; }

        public ICommand TestConnectionCommand { get; }

        public ICommand OpenApiKeysPageCommand { get; }

        string IDataErrorInfo.Error => null;

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case nameof(OpeningMarker):
                        return PlaceholderPattern.ValidateMarker(OpeningMarker);
                    case nameof(ClosingMarker):
                        return PlaceholderPattern.ValidateMarker(ClosingMarker);
                    default:
                        return null;
                }
            }
        }

        void OnAiProviderChanged()
        {
            HasSavedApiKey = _apiKeys.HasKey(AiProvider) && !_removedApiKeys.Contains(AiProvider);
            ConnectionMessage = null;
        }

        void OnNewApiKeyChanged() => ConnectionMessage = null;

        void Apply(PlaceKitSettings settings)
        {
            OpeningMarker = settings.OpeningMarker;
            ClosingMarker = settings.ClosingMarker;
            ShowCreateSummary = settings.ShowCreateSummary;
            OpenEditorAfterCreate = settings.OpenEditorAfterCreate;
            EditorPlacement = settings.EditorPlacement;
            AskAboutEmptyPlaceholders = settings.AskAboutEmptyPlaceholders;
            EmptyPlaceholderAction = settings.EmptyPlaceholderAction;
            AiProvider = settings.AiProvider;
        }

        void SetPlacement(bool isChecked, EditorPlacement placement)
        {
            if (isChecked)
            {
                EditorPlacement = placement;
            }
        }

        void SetEmptyPlaceholderAction(bool isChecked, EmptyPlaceholderAction action)
        {
            if (isChecked)
            {
                EmptyPlaceholderAction = action;
            }
        }

        void SetAiProvider(bool isChecked, AiProvider provider)
        {
            if (isChecked)
            {
                AiProvider = provider;
            }
        }

        void RemoveApiKey()
        {
            _removedApiKeys.Add(AiProvider);
            HasSavedApiKey = false;
            NewApiKey = string.Empty;
        }

        void TestConnection()
        {
            string apiKey = HasNewApiKey ? NewApiKey.Trim() : _apiKeys.Load(AiProvider);
            try
            {
                _ai.TestKey(AiProvider, apiKey);
                ConnectionMessage = $"Connected to {AiProviders.Name(AiProvider)}.";
                IsConnected = true;
            }
            catch (AiServiceException ex)
            {
                ConnectionMessage = ex.Message;
                IsConnected = false;
            }
            catch (OperationCanceledException)
            {
                ConnectionMessage = null;
            }
            catch (Exception ex)
            {
                // Exceptions must not reach Office from a dialog's command.
                Trace.TraceError("PlaceKit: {0}", ex);
                ConnectionMessage = $"The test failed: {ex.Message}";
                IsConnected = false;
            }
        }

        // Start from what's saved, to keep settings this window doesn't show.
        void Save()
        {
            PlaceKitSettings settings = _settingsService.Load();
            settings.OpeningMarker = OpeningMarker;
            settings.ClosingMarker = ClosingMarker;
            settings.ShowCreateSummary = ShowCreateSummary;
            settings.OpenEditorAfterCreate = OpenEditorAfterCreate;
            settings.EditorPlacement = EditorPlacement;
            settings.AskAboutEmptyPlaceholders = AskAboutEmptyPlaceholders;
            settings.EmptyPlaceholderAction = EmptyPlaceholderAction;
            settings.AiProvider = AiProvider;
            _settingsService.Save(settings);

            foreach (AiProvider provider in AiProviders.All)
            {
                if (_newApiKeys.TryGetValue(provider, out string apiKey) && !string.IsNullOrWhiteSpace(apiKey))
                {
                    _apiKeys.Save(provider, apiKey.Trim());
                }
                else if (_removedApiKeys.Contains(provider))
                {
                    _apiKeys.Delete(provider);
                }
            }

            Close(true);
        }
    }
}
