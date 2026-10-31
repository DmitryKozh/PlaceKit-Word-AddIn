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
        readonly IUpdateService _updates;
        readonly ProductInfo _product;
        readonly Dictionary<AiProvider, string> _newApiKeys = new Dictionary<AiProvider, string>();
        readonly HashSet<AiProvider> _removedApiKeys = new HashSet<AiProvider>();

        public SettingsViewModel(ISettingsService settingsService, IApiKeyStore apiKeys, IAiService ai, IUrlLauncher urlLauncher, IUpdateService updates, ProductInfo product)
            : base("PlaceKit settings")
        {
            _settingsService = settingsService;
            _apiKeys = apiKeys;
            _ai = ai;
            _urlLauncher = urlLauncher;
            _updates = updates;
            _product = product;
            StorageLocation = settingsService.Location;
            Apply(settingsService.Load());
            HasSavedApiKey = apiKeys.HasKey(AiProvider);
            AvailableUpdate = updates.AvailableUpdate;

            SaveCommand = new RelayCommand(Save, () => IsValid);
            CancelCommand = new RelayCommand(() => Close(false));
            ResetCommand = new RelayCommand(() => Apply(new PlaceKitSettings()));
            NextPageCommand = new RelayCommand(() => Page = (SettingsPage)(((int)Page + 1) % PageCount));
            PreviousPageCommand = new RelayCommand(() => Page = (SettingsPage)(((int)Page + PageCount - 1) % PageCount));
            RemoveApiKeyCommand = new RelayCommand(RemoveApiKey, () => HasSavedApiKey);
            TestConnectionCommand = new RelayCommand(TestConnection, () => HasNewApiKey || HasSavedApiKey);
            OpenApiKeysPageCommand = new RelayCommand(() => _urlLauncher.Open(AiProviders.KeysPage(AiProvider)));
            CheckNowCommand = new RelayCommand(CheckNow);
            DownloadUpdateCommand = new RelayCommand(() => _urlLauncher.Open(AvailableUpdate.Installer ?? ReleaseNotes()), () => IsUpdateAvailable);
            OpenReleaseNotesCommand = new RelayCommand(() => _urlLauncher.Open(ReleaseNotes()), () => IsUpdateAvailable);
        }

        static int PageCount => Enum.GetValues(typeof(SettingsPage)).Length;

        public SettingsPage Page { get; set; }

        // The list's radio buttons: ignore false from the other pages' buttons.
        public bool ShowGeneralPage
        {
            get => Page == SettingsPage.General;
            set => ShowPage(value, SettingsPage.General);
        }

        public bool ShowAiPage
        {
            get => Page == SettingsPage.Ai;
            set => ShowPage(value, SettingsPage.Ai);
        }

        public bool ShowUpdatesPage
        {
            get => Page == SettingsPage.Updates;
            set => ShowPage(value, SettingsPage.Updates);
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

        public string PrivacyNote => $"Text goes to {AiProviders.Company(AiProvider)} only when you use From text or Detect, and {AiProviders.Company(AiProvider)} bills your API key for it.";

        public string ConnectionMessage { get; private set; }

        public bool IsConnected { get; private set; }

        public bool HasConnectionMessage => ConnectionMessage != null;

        public bool CheckForUpdates { get; set; }

        public string InstalledVersion => $"You have PlaceKit {_product.Version}. New versions are published on {_product.UpdateFeed.Host}.";

        public string UpdatePrivacyNote => $"A check asks {_product.UpdateFeed.Host} only for the latest version. Nothing from your documents or settings is sent.";

        public ReleaseInfo AvailableUpdate { get; private set; }

        public bool IsUpdateAvailable => AvailableUpdate != null;

        public string UpdateHeading => IsUpdateAvailable ? $"PlaceKit {AvailableUpdate.VersionText} is available" : null;

        // The result of Check now, unless it found an update, which shows on its own.
        public string UpdateCheckMessage { get; private set; }

        public bool IsUpToDate { get; private set; }

        public bool HasUpdateCheckMessage => UpdateCheckMessage != null;

        public bool IsValid =>
            PlaceholderPattern.ValidateMarker(OpeningMarker) == null && PlaceholderPattern.ValidateMarker(ClosingMarker) == null;

        public string Preview => IsValid ? OpeningMarker + PreviewName + ClosingMarker : "-";

        public string StorageLocation { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand NextPageCommand { get; }

        public ICommand PreviousPageCommand { get; }

        public ICommand RemoveApiKeyCommand { get; }

        public ICommand TestConnectionCommand { get; }

        public ICommand OpenApiKeysPageCommand { get; }

        public ICommand CheckNowCommand { get; }

        public ICommand DownloadUpdateCommand { get; }

        public ICommand OpenReleaseNotesCommand { get; }

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
            CheckForUpdates = settings.CheckForUpdates;
        }

        void ShowPage(bool isChecked, SettingsPage page)
        {
            if (isChecked)
            {
                Page = page;
            }
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

        Uri ReleaseNotes() => _product.Link(AvailableUpdate.Page, "update");

        void CheckNow()
        {
            try
            {
                AvailableUpdate = _updates.Check();
                IsUpToDate = AvailableUpdate == null;
                UpdateCheckMessage = IsUpToDate ? $"PlaceKit {_product.Version} is the latest version." : null;
            }
            catch (UpdateCheckException ex)
            {
                UpdateCheckMessage = ex.Message;
                IsUpToDate = false;
            }
            catch (OperationCanceledException)
            {
                UpdateCheckMessage = null;
            }
            catch (Exception ex)
            {
                // Exceptions must not reach Office from a dialog's command.
                Trace.TraceError("PlaceKit: {0}", ex);
                UpdateCheckMessage = $"The check failed: {ex.Message}";
                IsUpToDate = false;
            }
        }

        // Start from what's saved, to keep settings this window doesn't show (the Detect note).
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
            settings.CheckForUpdates = CheckForUpdates;
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
