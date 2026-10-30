using System;
using System.Collections.Generic;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    // Asks for a text, such as an email, and finds the placeholders' values in it with the user's Claude API key,
    // which is entered here too. Closes with true and the Values; when they can't be found it stays open with
    // the reason, so the user can try again.
    public sealed class FillFromTextViewModel : DialogViewModelBase
    {
        // About 25,000 tokens: far more than an email or a form, and still a small request.
        internal const int MaxTextLength = 100000;

        static readonly Uri ApiKeysPage = new Uri("https://console.anthropic.com/settings/keys");

        readonly IReadOnlyList<string> _names;
        readonly IValueFinder _finder;
        readonly IApiKeyStore _apiKeys;
        readonly IUrlLauncher _urlLauncher;
        readonly IDialogService _dialogs;

        public FillFromTextViewModel(IReadOnlyList<string> names, IValueFinder finder, IApiKeyStore apiKeys, IUrlLauncher urlLauncher, IDialogService dialogs)
            : base("Fill from text")
        {
            _names = names;
            _finder = finder;
            _apiKeys = apiKeys;
            _urlLauncher = urlLauncher;
            _dialogs = dialogs;
            HasSavedApiKey = apiKeys.HasKey;
            Intro = $"Paste a text that holds the values, such as an email or notes. Claude finds the values for the {TextFormat.Quantity(names.Count, "placeholder")} in it, and you check them before saving.";

            FindCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Find), () => HasText && (HasSavedApiKey || HasNewApiKey));
            CancelCommand = new RelayCommand(() => Close(false));
            RemoveApiKeyCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(RemoveApiKey), () => HasSavedApiKey);
            OpenApiKeysPageCommand = new RelayCommand(() => _urlLauncher.Open(ApiKeysPage));
        }

        public string Intro { get; }

        public string Text { get; set; } = string.Empty;

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        // What the user types or pastes; the saved key is never shown.
        public string NewApiKey { get; set; } = string.Empty;

        public bool HasNewApiKey => !string.IsNullOrWhiteSpace(NewApiKey);

        public bool HasSavedApiKey { get; private set; }

        public string ApiKeyHint => HasSavedApiKey ? "A key is saved. Paste a new one to replace it." : "Paste your Claude API key";

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public IReadOnlyDictionary<string, string> Values { get; private set; }

        public ICommand FindCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RemoveApiKeyCommand { get; }

        public ICommand OpenApiKeysPageCommand { get; }

        // Called by Fody when Text or NewApiKey changes: an old error is about the old input.
        void OnTextChanged() => Error = null;

        void OnNewApiKeyChanged() => Error = null;

        void Find()
        {
            if (Text.Length > MaxTextLength)
            {
                Error = $"The text is too long. Paste at most {MaxTextLength:N0} characters, the part that holds the values.";
                return;
            }

            // A new key is kept even if Claude turns it down, like a key saved in settings.
            if (HasNewApiKey)
            {
                _apiKeys.Save(NewApiKey.Trim());
                HasSavedApiKey = true;
                NewApiKey = string.Empty;
            }

            try
            {
                Values = _finder.FindValues(Text, _names);
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
            _apiKeys.Delete();
            HasSavedApiKey = false;
        }
    }
}
