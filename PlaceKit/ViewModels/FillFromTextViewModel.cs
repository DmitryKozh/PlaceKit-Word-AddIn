using System;
using System.Collections.Generic;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class FillFromTextViewModel : DialogViewModelBase
    {
        // ~25k tokens
        internal const int MaxTextLength = 100000;

        readonly IReadOnlyList<string> _names;
        readonly IValueFinder _finder;
        readonly IAiService _ai;
        readonly IDialogService _dialogs;

        public FillFromTextViewModel(IReadOnlyList<string> names, IValueFinder finder, IAiService ai, IDialogService dialogs)
            : base("Fill from text")
        {
            _names = names;
            _finder = finder;
            _ai = ai;
            _dialogs = dialogs;
            Provider = ai.Provider;

            FindCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Find), () => HasText);
            CancelCommand = new RelayCommand(() => Close(false));
            ChangeServiceCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(ChangeService));
        }

        public AiProvider Provider { get; private set; }

        public string Intro => $"Paste a text that holds the values, such as an email or notes. {AiProviders.Name(Provider)} finds the values for the {TextFormat.Quantity(_names.Count, "placeholder")} in it, and you check them before saving.";

        public string Text { get; set; } = string.Empty;

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public string ServiceNote => $"The text and the placeholder names are sent to {AiProviders.Company(Provider)}. Nothing is written to the document until you save.";

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public IReadOnlyDictionary<string, string> Values { get; private set; }

        public ICommand FindCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ChangeServiceCommand { get; }

        void OnTextChanged() => Error = null;

        void Find()
        {
            if (Text.Length > MaxTextLength)
            {
                Error = $"The text is too long. Paste at most {MaxTextLength:N0} characters, the part that holds the values.";
                return;
            }

            try
            {
                Values = _finder.FindValues(Text, _names);
            }
            catch (AiServiceException ex)
            {
                Error = ex.Message;
                return;
            }

            Close(true);
        }

        void ChangeService()
        {
            _ai.OpenSettings();
            Provider = _ai.Provider;
            Error = null;
        }
    }
}
