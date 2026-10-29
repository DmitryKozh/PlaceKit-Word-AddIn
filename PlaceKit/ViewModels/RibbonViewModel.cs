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
        readonly IGuideService _guide;

        public RibbonViewModel(IWordHost wordHost, IPlaceholderService placeholders, ISettingsService settings, IDialogService dialogs, IProgressService progress, IEditorService editors, IGuideService guide, ProductInfo product, IUrlLauncher urlLauncher)
        {
            _wordHost = wordHost;
            _placeholders = placeholders;
            _settings = settings;
            _dialogs = dialogs;
            _progress = progress;
            _editors = editors;
            _guide = guide;

            CreateCommand = new RelayCommand(() => Run(CreatePlaceholders));
            FillCommand = new RelayCommand(() => Run(FillPlaceholders));
            FinalizeCommand = new RelayCommand(() => Run(FinalizePlaceholders));
            GuideCommand = new RelayCommand(() => Run(() => _guide.CreateGuide(_settings.Load().CreatePattern())));
            SettingsCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<SettingsViewModel>()));
            AboutCommand = new RelayCommand(() => Run(() => _dialogs.ShowDialog<AboutViewModel>()));
            HireCommand = new RelayCommand(() => Run(() => urlLauncher.Open(product.Link(product.Website, "ribbon"))));
        }

        public ICommand CreateCommand { get; }

        public ICommand FillCommand { get; }

        public ICommand FinalizeCommand { get; }

        public ICommand GuideCommand { get; }

        public ICommand SettingsCommand { get; }

        public ICommand AboutCommand { get; }

        public ICommand HireCommand { get; }

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
