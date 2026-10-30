using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    // Asks for a text, such as an email, and finds the placeholders' values in it with Claude or ChatGPT and the
    // user's API key for it, which is entered here too. Closes with true and the Values; when they can't be found
    // it stays open with the reason, so the user can try again.
    public sealed class FillFromTextViewModel : DialogViewModelBase
    {
        // About 25,000 tokens: far more than an email or a form, and still a small request.
        internal const int MaxTextLength = 100000;

        static readonly Uri ClaudeKeysPage = new Uri("https://console.anthropic.com/settings/keys");
        static readonly Uri OpenAIKeysPage = new Uri("https://platform.openai.com/api-keys");

        readonly IReadOnlyList<string> _names;
        readonly IReadOnlyDictionary<AiProvider, IValueFinder> _finders;
        readonly IApiKeyStore _apiKeys;
        readonly IUrlLauncher _urlLauncher;
        readonly IDialogService _dialogs;

        public FillFromTextViewModel(IReadOnlyList<string> names, IEnumerable<IValueFinder> finders, IApiKeyStore apiKeys, IUrlLauncher urlLauncher, IDialogService dialogs)
            : base("Fill from text")
        {
            _names = names;
            _finders = finders.ToDictionary(finder => finder.Provider);
            _apiKeys = apiKeys;
            _urlLauncher = urlLauncher;
            _dialogs = dialogs;

            // The service the user has a key for; Claude when there are keys for both or for neither.
            Provider = !apiKeys.HasKey(AiProvider.Claude) && apiKeys.HasKey(AiProvider.OpenAI) ? AiProvider.OpenAI : AiProvider.Claude;
            HasSavedApiKey = apiKeys.HasKey(Provider);

            FindCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Find), () => HasText && (HasSavedApiKey || HasNewApiKey));
            CancelCommand = new RelayCommand(() => Close(false));
            RemoveApiKeyCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(RemoveApiKey), () => HasSavedApiKey);
            OpenApiKeysPageCommand = new RelayCommand(() => _urlLauncher.Open(IsOpenAI ? OpenAIKeysPage : ClaudeKeysPage));
        }

        public AiProvider Provider { get; set; }

        // Ignore false: unchecking the other radio button must not change the service.
        public bool UseClaude
        {
            get => Provider == AiProvider.Claude;
            set => SetProvider(value, AiProvider.Claude);
        }

        public bool UseOpenAI
        {
            get => Provider == AiProvider.OpenAI;
            set => SetProvider(value, AiProvider.OpenAI);
        }

        public string Intro => $"Paste a text that holds the values, such as an email or notes. {ServiceName} finds the values for the {TextFormat.Quantity(_names.Count, "placeholder")} in it, and you check them before saving.";

        public string Text { get; set; } = string.Empty;

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public string ApiKeyLabel => IsOpenAI ? "OpenAI API _key" : "Claude API _key";

        // What the user types or pastes; a saved key is never shown.
        public string NewApiKey { get; set; } = string.Empty;

        public bool HasNewApiKey => !string.IsNullOrWhiteSpace(NewApiKey);

        public bool HasSavedApiKey { get; private set; }

        public string ApiKeyHint => HasSavedApiKey
            ? "A key is saved. Paste a new one to replace it."
            : IsOpenAI ? "Paste your OpenAI API key" : "Paste your Claude API key";

        public string ApiKeysPageText => IsOpenAI ? "Get a key on the OpenAI platform" : "Get a key in the Claude Console";

        public string PrivacyNote => $"The text and the placeholder names are sent to {(IsOpenAI ? "OpenAI" : "Anthropic")} and processed with your API key. Nothing is written to the document until you save.";

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public IReadOnlyDictionary<string, string> Values { get; private set; }

        public ICommand FindCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RemoveApiKeyCommand { get; }

        public ICommand OpenApiKeysPageCommand { get; }

        bool IsOpenAI => Provider == AiProvider.OpenAI;

        string ServiceName => IsOpenAI ? "ChatGPT" : "Claude";

        // Called by Fody when Provider changes: the key box and its state belong to the chosen service.
        void OnProviderChanged()
        {
            HasSavedApiKey = _apiKeys.HasKey(Provider);
            NewApiKey = string.Empty;
            Error = null;
        }

        // Called by Fody when Text or NewApiKey changes: an old error is about the old input.
        void OnTextChanged() => Error = null;

        void OnNewApiKeyChanged() => Error = null;

        void SetProvider(bool isChecked, AiProvider provider)
        {
            if (isChecked)
            {
                Provider = provider;
            }
        }

        void Find()
        {
            if (Text.Length > MaxTextLength)
            {
                Error = $"The text is too long. Paste at most {MaxTextLength:N0} characters, the part that holds the values.";
                return;
            }

            // A new key is kept even if the service turns it down, like a key saved in settings.
            if (HasNewApiKey)
            {
                _apiKeys.Save(Provider, NewApiKey.Trim());
                HasSavedApiKey = true;
                NewApiKey = string.Empty;
            }

            try
            {
                Values = _finders[Provider].FindValues(Text, _names);
            }
            catch (ValueFinderException ex)
            {
                Error = ex.Message;
                return;
            }

            Close(true);
        }

        void RemoveApiKey()
        {
            _apiKeys.Delete(Provider);
            HasSavedApiKey = false;
        }
    }
}
