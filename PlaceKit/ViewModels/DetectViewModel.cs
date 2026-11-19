using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class DetectViewModel : DialogViewModelBase
    {
        // ~25k tokens
        internal const int MaxTextLength = 100000;

        readonly IPlaceholderDocument _document;
        readonly string _documentText;
        readonly string _selectedText;
        readonly IReadOnlyList<string> _existingNames;
        readonly IPlaceholderDetector _detector;
        readonly IAiService _ai;
        readonly ISettingsService _settings;
        readonly IDialogService _dialogs;

        public DetectViewModel(IPlaceholderDocument document, string documentText, string selectedText, IReadOnlyList<string> existingNames,
            IPlaceholderDetector detector, IAiService ai, ISettingsService settings, IDialogService dialogs)
            : base("Detect placeholders")
        {
            _document = document;
            _documentText = documentText;
            _selectedText = string.IsNullOrWhiteSpace(selectedText) ? null : selectedText;
            _existingNames = existingNames;
            _detector = detector;
            _ai = ai;
            _settings = settings;
            _dialogs = dialogs;
            Provider = ai.Provider;
            Note = settings.Load().DetectNote;
            SetItems(new List<DetectedItemViewModel>());

            FindCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Find), () => IsScanStep);
            BackCommand = new RelayCommand(Back, () => IsReviewStep);
            CreateCommand = new RelayCommand(Create, () => IsReviewStep && ChosenCount > 0 && NameError == null);
            CancelCommand = new RelayCommand(() => Close(false));
            ChangeServiceCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(ChangeService));
            ShowCommand = new RelayCommand<DetectedItemViewModel>(item => _dialogs.RunAndReportErrors(() => Show(item)), item => item != null);
        }

        public AiProvider Provider { get; private set; }

        public string Intro => $"{AiProviders.Name(Provider)} reads the document and suggests the parts that change from one use to the next, such as names, dates and amounts. You choose which of them become placeholders.";

        public bool HasSelection => _selectedText != null;

        public bool ScanSelection { get; set; }

        // ignore false from the other radio button
        public bool ScanDocument
        {
            get => !ScanSelection;
            set
            {
                if (value)
                {
                    ScanSelection = false;
                }
            }
        }

        public string DocumentSize => $"The whole document, {Characters(_documentText)}";

        public string SelectionSize => HasSelection ? $"Only the selected text, {Characters(_selectedText)}" : null;

        public string NoteLabel => $"_Note for {AiProviders.Name(Provider)} (optional)";

        public string Note { get; set; } = string.Empty;

        public string SendNote => $"{(ScanSelection ? "The selected text" : "The text of the document")} and your note are sent to {AiProviders.Company(Provider)}. The document doesn't change until you create the placeholders.";

        public bool IsReviewStep { get; private set; }

        public bool IsScanStep => !IsReviewStep;

        public IReadOnlyList<DetectedItemViewModel> Items { get; private set; }

        public string ReviewIntro
        {
            get
            {
                int marked = Items.Count(item => item.IsMarker);
                int suggested = Items.Count - marked;
                string service = AiProviders.Name(Provider);
                if (marked == 0)
                {
                    return $"{service} suggests {TextFormat.Quantity(suggested, "placeholder")}. Clear the ones you don't want and rename any; every occurrence of a text becomes its placeholder, and its text stays as the value.";
                }

                string found = suggested == 0
                    ? $"The document has {TextFormat.Quantity(marked, "marker")}, and {service} found nothing else that changes."
                    : $"The document has {TextFormat.Quantity(marked, "marker")}, and {service} suggests {TextFormat.Quantity(suggested, "more placeholder")}.";
                return found + " Clear the ones you don't want and rename any. Every occurrence becomes its placeholder: a marker an empty one, and other text keeps its text as the value.";
            }
        }

        public int ChosenCount { get; private set; }

        public string CreateText => ChosenCount == 0 ? "_Create placeholders" : $"_Create {TextFormat.Quantity(ChosenCount, "placeholder")}";

        public string NameError { get; private set; }

        public string Error { get; private set; }

        public bool HasError => Error != null;

        public IReadOnlyList<DetectedPlaceholder> Chosen { get; private set; }

        public ICommand FindCommand { get; }

        public ICommand BackCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ChangeServiceCommand { get; }

        public ICommand ShowCommand { get; }

        static string Characters(string text) => text.Length == 1 ? "1 character" : $"{text.Length:N0} characters";

        void OnScanSelectionChanged() => Error = null;

        void OnNoteChanged() => Error = null;

        void Find()
        {
            string text = ScanSelection ? _selectedText : _documentText;
            if (text.Length > MaxTextLength)
            {
                Error = $"{(ScanSelection ? "The selected text" : "The document")} is too long to send: {Characters(text)}. Select at most {MaxTextLength:N0} characters, the part with the details that change, and try again.";
                return;
            }

            RememberNote();
            PlaceholderPattern markers = _settings.Load().CreatePattern();
            List<DetectedPlaceholder> marked = markers.FindMarkers(text)
                .Select(marker => new DetectedPlaceholder(marker.Name, marker.Text, "Marked in the document", WordText.CountWholeWords(_documentText, marker.Text), isMarker: true))
                .ToList();
            IReadOnlyList<DetectedPlaceholder> suggested;
            try
            {
                suggested = _detector.Detect(text, _documentText, Note, _existingNames, markers);
            }
            catch (AiServiceException ex)
            {
                Error = ex.Message;
                return;
            }

            // A suggestion around a marker would only repeat it.
            List<DetectedPlaceholder> found = marked.Concat(suggested.Where(placeholder => !marked.Any(marker => placeholder.Text.Contains(marker.Text)))).ToList();
            if (found.Count == 0)
            {
                Error = _existingNames.Count == 0
                    ? "No parts that change were found. A note that says what to look for may help."
                    : $"No new parts that change were found. The {TextFormat.Quantity(_existingNames.Count, "placeholder")} already in the document are left out; a note that says what to look for may help.";
                return;
            }

            SetItems(found.Select(placeholder => new DetectedItemViewModel(placeholder)).ToList());
            Error = null;
            IsReviewStep = true;
        }

        void Back()
        {
            IsReviewStep = false;
            Error = null;
        }

        // Placeholders match by their exact name, so names that differ only in case take one spelling:
        // an existing placeholder's, or else the first chosen one's (markers come first).
        void Create()
        {
            Dictionary<string, string> spellings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in _existingNames.Where(name => !spellings.ContainsKey(name)))
            {
                spellings.Add(name, name);
            }

            List<DetectedPlaceholder> chosen = new List<DetectedPlaceholder>();
            foreach (DetectedPlaceholder placeholder in Items.Where(item => item.IsChosen).Select(item => item.ToPlaceholder()))
            {
                if (!spellings.TryGetValue(placeholder.Name, out string spelling))
                {
                    spelling = placeholder.Name;
                    spellings.Add(spelling, spelling);
                }

                chosen.Add(placeholder.WithName(spelling));
            }

            Chosen = chosen;
            Close(true);
        }

        void Show(DetectedItemViewModel item)
        {
            if (!_document.SelectText(item.Text))
            {
                Error = $"\"{item.Text}\" is no longer in the document.";
            }
        }

        void ChangeService()
        {
            _ai.OpenSettings();
            Provider = _ai.Provider;
            Error = null;
        }

        void RememberNote()
        {
            PlaceKitSettings settings = _settings.Load();
            string note = Note?.Trim() ?? string.Empty;
            if (settings.DetectNote != note)
            {
                settings.DetectNote = note;
                _settings.Save(settings);
            }
        }

        void SetItems(List<DetectedItemViewModel> items)
        {
            if (Items != null)
            {
                foreach (DetectedItemViewModel item in Items)
                {
                    item.PropertyChanged -= HandleItemChanged;
                }
            }

            foreach (DetectedItemViewModel item in items)
            {
                item.PropertyChanged += HandleItemChanged;
            }

            Items = items;
            UpdateChoice();
        }

        void HandleItemChanged(object sender, PropertyChangedEventArgs e) => UpdateChoice();

        void UpdateChoice()
        {
            List<DetectedItemViewModel> chosen = Items.Where(item => item.IsChosen).ToList();
            ChosenCount = chosen.Count;

            // A marker names its placeholder, so other text may join it under the same name.
            string duplicate = chosen.GroupBy(item => item.Name?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Key.Length > 0 && group.Count() > 1 && !group.Any(item => item.IsMarker))
                .Select(group => group.Key)
                .FirstOrDefault();

            NameError = chosen.Any(item => string.IsNullOrWhiteSpace(item.Name)) ? "Every chosen placeholder needs a name."
                : chosen.Any(item => item.Name.IndexOfAny(new[] { '{', '}', '[', ']', '<', '>' }) >= 0) ? "Names can't contain { } [ ] < or >."
                : duplicate != null && !_existingNames.Contains(duplicate, StringComparer.OrdinalIgnoreCase) ? $"Two placeholders are named \"{duplicate}\". Give each its own name, or clear one."
                : null;

            if (IsReviewStep || NameError != null)
            {
                Error = NameError;
            }
        }
    }
}
