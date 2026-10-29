using System;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.ViewModels
{
    public sealed class RibbonViewModel
    {
        const string Caption = "PlaceKit";

        readonly IWordHost _wordHost;
        readonly IPlaceholderService _placeholders;
        readonly ISettingsService _settings;
        readonly IDialogService _dialogs;
        readonly IProgressService _progress;
        readonly IEditorService _editors;

        public RibbonViewModel(IWordHost wordHost, IPlaceholderService placeholders, ISettingsService settings, IDialogService dialogs, IProgressService progress, IEditorService editors)
        {
            _wordHost = wordHost;
            _placeholders = placeholders;
            _settings = settings;
            _dialogs = dialogs;
            _progress = progress;
            _editors = editors;

            CreateCommand = new RelayCommand(() => Run(CreatePlaceholders));
            FillCommand = new RelayCommand(() => Run(FillPlaceholders));
            ClearCommand = new RelayCommand(() => Run(ClearPlaceholders));
            SettingsCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<SettingsViewModel>()));
            AboutCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<AboutViewModel>()));
        }

        public ICommand CreateCommand { get; }

        public ICommand FillCommand { get; }

        public ICommand ClearCommand { get; }

        public ICommand SettingsCommand { get; }

        public ICommand AboutCommand { get; }

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
                ShowCanceled(result, "created");
            }
            else if (result.Processed == 0)
            {
                ShowMessage(result.Skipped == 0
                        ? $"No text matching {pattern.Format("text")} was found in the document."
                        : $"No placeholders were created: {TextFormat.Quantity(result.Skipped, "marker")} could not be converted.",
                    MessageKind.Information);
            }
            else if (ShouldOpenEditor(result, settings))
            {
                _editors.Open(document);
            }
            else
            {
                _editors.Refresh(document);
            }
        }

        bool ShouldOpenEditor(PlaceholderOperationResult result, PlaceKitSettings settings)
        {
            if (!settings.ShowCreateSummary)
            {
                return settings.OpenEditorAfterCreate;
            }

            CreateSummaryViewModel summary = new CreateSummaryViewModel(result);
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

        void ClearPlaceholders()
        {
            if (!TryGetEditableDocument(out Word.Document document))
            {
                return;
            }

            PlaceholderPattern pattern = _settings.Load().CreatePattern();
            PlaceholderOperationResult result = _progress.Run("Clearing placeholders",
                (progress, token) => _placeholders.ClearPlaceholders(document, pattern, progress, token));
            _editors.Refresh(document);

            if (result.WasCanceled)
            {
                ShowCanceled(result, "converted");
            }
            else if (result.Processed == 0 && result.Skipped == 0)
            {
                ShowMessage("This document has no placeholders to clear.", MessageKind.Information);
            }
            else if (result.Skipped > 0)
            {
                ShowMessage($"{TextFormat.Quantity(result.Processed, "placeholder")} converted to regular text, {result.Skipped} could not be converted.", MessageKind.Warning);
            }
            else
            {
                ShowMessage($"{TextFormat.Quantity(result.Processed, "placeholder")} converted to regular text.", MessageKind.Information);
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

        void ShowCanceled(PlaceholderOperationResult result, string verb) =>
            ShowMessage($"The operation was canceled. Placeholders {verb} so far: {result.Processed}. Press Ctrl+Z in Word to undo them.", MessageKind.Information);

        void ShowMessage(string message, MessageKind kind) => _dialogs.ShowMessage(Caption, message, kind);

        void Run(Action action) => _dialogs.RunAndReportErrors(action);
    }
}
