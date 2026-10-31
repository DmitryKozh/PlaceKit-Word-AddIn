using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.Tools;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.ViewModels
{
    public sealed class RibbonViewModel : ViewModelBase
    {
        const string Caption = "PlaceKit";

        readonly IWordHost _wordHost;
        readonly IPlaceholderService _placeholders;
        readonly ISettingsService _settings;
        readonly IDialogService _dialogs;
        readonly IProgressService _progress;
        readonly IEditorService _editors;
        readonly IGuideService _guide;
        readonly IAiService _ai;
        readonly IPlaceholderDetector _detector;
        readonly Func<SettingsViewModel> _createSettings;

        public RibbonViewModel(IWordHost wordHost, IPlaceholderService placeholders, ISettingsService settings, IDialogService dialogs, IProgressService progress, IEditorService editors, IGuideService guide,
            IAiService ai, IPlaceholderDetector detector, ProductInfo product, IUrlLauncher urlLauncher, IUpdateService updates, Func<SettingsViewModel> createSettings)
        {
            _wordHost = wordHost;
            _placeholders = placeholders;
            _settings = settings;
            _dialogs = dialogs;
            _progress = progress;
            _editors = editors;
            _guide = guide;
            _ai = ai;
            _detector = detector;
            _createSettings = createSettings;
            AvailableUpdate = updates.AvailableUpdate;
            updates.AvailableUpdateChanged += (sender, e) => AvailableUpdate = updates.AvailableUpdate;

            CreateCommand = new RelayCommand(() => Run(CreatePlaceholders));
            DetectCommand = new RelayCommand(() => Run(DetectPlaceholders));
            FillCommand = new RelayCommand(() => Run(FillPlaceholders));
            FinalizeCommand = new RelayCommand(() => Run(FinalizePlaceholders));
            GuideCommand = new RelayCommand(() => Run(() => _guide.CreateGuide(_settings.Load().CreatePattern())));
            SettingsCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<SettingsViewModel>()));
            AboutCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<AboutViewModel>()));
            HireCommand = new RelayCommand(() => Run(() => urlLauncher.Open(product.Link(product.Website, "ribbon"))));
            UpdateCommand = new RelayCommand(() => Run(ShowUpdate));
        }

        // The Update button shows only while a newer version is out.
        public ReleaseInfo AvailableUpdate { get; private set; }

        public bool IsUpdateAvailable => AvailableUpdate != null;

        public string UpdateTip => IsUpdateAvailable ? $"PlaceKit {AvailableUpdate.VersionText} is available" : null;

        public ICommand CreateCommand { get; }

        public ICommand DetectCommand { get; }

        public ICommand FillCommand { get; }

        public ICommand FinalizeCommand { get; }

        public ICommand GuideCommand { get; }

        public ICommand SettingsCommand { get; }

        public ICommand AboutCommand { get; }

        public ICommand HireCommand { get; }

        public ICommand UpdateCommand { get; }

        void ShowUpdate()
        {
            SettingsViewModel settings = _createSettings();
            settings.ShowUpdatesPage = true;
            _dialogs.ShowDialog(settings);
        }

        void CreatePlaceholders()
        {
            if (!TryGetEditableDocument(out Word.Document document))
            {
                return;
            }

            PlaceKitSettings settings = _settings.Load();
            PlaceholderPattern pattern = settings.CreatePattern();
            PlaceholderOperationResult result = _progress.Run("Creating placeholders",
                (progress, token) => _placeholders.CreatePlaceholders(document, pattern, progress, token));

            if (result.WasCanceled)
            {
                _editors.Refresh(document);
                ShowMessage(TextFormat.Canceled(result.Processed, "created"), MessageKind.Information);
            }
            else if (result.Processed == 0)
            {
                ShowMessage(result.Skipped == 0
                        ? $"No text matching {pattern.Format("text")} was found in the document."
                        : $"No placeholders were created: {TextFormat.Quantity(result.Skipped, "marker")} could not be converted.",
                    MessageKind.Information);
            }
            else if (ShouldOpenEditor(result, settings,
                $"{TextFormat.Quantity(result.Skipped, "marker")} skipped: empty, too long, split across lines or inside another control."))
            {
                _editors.Open(document);
            }
            else
            {
                _editors.Refresh(document);
            }
        }

        void DetectPlaceholders()
        {
            if (!TryGetEditableDocument(out Word.Document document) || !_ai.EnsureApiKey("Detect"))
            {
                return;
            }

            IPlaceholderDocument placeholders = new PlaceholderDocument(document, _placeholders, _progress);
            string text = placeholders.ReadText();
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowMessage("The document has no text to look at.", MessageKind.Information);
                return;
            }

            IReadOnlyList<string> existingNames = placeholders.LoadPlaceholders().Select(placeholder => placeholder.Name).ToList();
            DetectViewModel dialog = new DetectViewModel(placeholders, text, placeholders.ReadSelectedText(), existingNames, _detector, _ai, _settings, _dialogs);
            if (_dialogs.ShowDialog(dialog) != true)
            {
                return;
            }

            PlaceholderOperationResult result = placeholders.CreatePlaceholders(dialog.Chosen);
            PlaceKitSettings settings = _settings.Load();
            string skipped = SkippedSuggestions(result);
            if (result.WasCanceled)
            {
                _editors.Refresh(document);
                ShowMessage(TextFormat.Canceled(result.Processed, "created"), MessageKind.Information);
            }
            else if (result.Processed == 0)
            {
                ShowMessage(skipped ?? "No placeholders were created.", MessageKind.Warning);
            }
            else
            {
                // Without the summary, suggestions that were left out would go unnoticed.
                if (skipped != null && !settings.ShowCreateSummary)
                {
                    ShowMessage(skipped, MessageKind.Warning);
                }

                if (ShouldOpenEditor(result, settings, skipped))
                {
                    _editors.Open(document);
                }
                else
                {
                    _editors.Refresh(document);
                }
            }
        }

        static string SkippedSuggestions(PlaceholderOperationResult result)
        {
            const int Listed = 8;
            if (result.Skipped == 0)
            {
                return null;
            }

            string more = result.Problems.Count > Listed ? $"\nand {result.Problems.Count - Listed} more" : string.Empty;
            return $"{TextFormat.Quantity(result.Skipped, "suggestion")} did not become placeholders:\n{string.Join("\n", result.Problems.Take(Listed))}{more}";
        }

        bool ShouldOpenEditor(PlaceholderOperationResult result, PlaceKitSettings settings, string skippedText)
        {
            if (!settings.ShowCreateSummary)
            {
                return settings.OpenEditorAfterCreate;
            }

            CreateSummaryViewModel summary = new CreateSummaryViewModel(result, skippedText);
            bool? answer = _dialogs.ShowDialog(summary);

            // Closing with X is not an answer to remember.
            if (answer.HasValue && summary.RememberChoice)
            {
                settings.ShowCreateSummary = false;
                settings.OpenEditorAfterCreate = answer.Value;
                _settings.Save(settings);
            }

            return answer == true;
        }

        void FillPlaceholders()
        {
            if (TryGetEditableDocument(out Word.Document document))
            {
                _editors.Open(document);
            }
        }

        void FinalizePlaceholders()
        {
            if (TryGetEditableDocument(out Word.Document document))
            {
                _editors.FinalizeDocument(document);
            }
        }

        bool TryGetEditableDocument(out Word.Document document)
        {
            document = _wordHost.ActiveDocument;
            if (document == null)
            {
                ShowMessage("Open a document first.", MessageKind.Information);
                return false;
            }

            if (document.ProtectionType != Word.WdProtectionType.wdNoProtection)
            {
                ShowMessage("The document is protected. Remove the protection and try again.", MessageKind.Warning);
                document = null;
                return false;
            }

            return true;
        }

        void ShowMessage(string message, MessageKind kind) => _dialogs.ShowMessage(Caption, message, kind);

        void Run(Action action) => _dialogs.RunAndReportErrors(action);
    }
}
