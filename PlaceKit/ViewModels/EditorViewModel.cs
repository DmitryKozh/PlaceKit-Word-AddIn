using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class EditorViewModel : DialogViewModelBase
    {
        const string Caption = "PlaceKit";
        const string ValuesFileFilter = "PlaceKit values (*.json)|*.json|All files (*.*)|*.*";

        readonly IPlaceholderDocument _document;
        readonly IDialogService _dialogs;
        readonly IPlaceholderValuesFile _valuesFile;
        readonly Func<IReadOnlyList<string>, FillFromTextViewModel> _createFillFromText;

        // createFillFromText: the Fill from text dialog for these placeholder names.
        public EditorViewModel(IPlaceholderDocument document, IDialogService dialogs, IPlaceholderValuesFile valuesFile, Func<IReadOnlyList<string>, FillFromTextViewModel> createFillFromText)
            : base("Fill placeholders")
        {
            _document = document;
            _dialogs = dialogs;
            _valuesFile = valuesFile;
            _createFillFromText = createFillFromText;
            SetItems(Enumerable.Empty<PlaceholderItemViewModel>());

            LocateCommand = new RelayCommand<PlaceholderItemViewModel>(item => _dialogs.RunAndReportErrors(() => Locate(item)), item => item != null);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty, () => HasSearchText);
            SaveCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Save), () => HasChanges);
            SaveAndFinalizeCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(SaveAndFinalize), () => !IsEmpty);
            CloseCommand = new RelayCommand(CloseEditor);
            RefreshCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Reload));
            MoveCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Move));
            ImportCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Import), () => !IsEmpty);
            FillFromTextCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(FillFromText), () => !IsEmpty);
            ExportCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Export), () => Items.Any(HasValue));
        }

        public EditorPlacement Placement { get; private set; }

        public bool IsInSidePanel => Placement == EditorPlacement.SidePanel;

        public bool IsInWindow => Placement == EditorPlacement.Window;

        // null closes the editor for good
        public EditorPlacement? RequestedPlacement { get; private set; }

        // Set by Save and finalize: the host finalizes the document after the editor closes.
        public bool FinalizeRequested { get; private set; }

        public IReadOnlyList<PlaceholderItemViewModel> Items { get; private set; }

        public ICollectionView ItemsView { get; private set; }

        public string SearchText { get; set; } = string.Empty;

        public bool HasSearchText => !string.IsNullOrEmpty(SearchText);

        public int VisibleCount { get; private set; }

        public int ModifiedCount { get; private set; }

        public bool IsEmpty => Items.Count == 0;

        public bool HasNoMatches => !IsEmpty && VisibleCount == 0;

        public bool HasChanges => ModifiedCount > 0;

        public string Summary
        {
            get
            {
                string summary = TextFormat.Quantity(Items.Count, "placeholder");
                if (VisibleCount != Items.Count)
                {
                    summary += $", {VisibleCount} shown";
                }

                if (ModifiedCount > 0)
                {
                    summary += $", {ModifiedCount} modified";
                }

                return summary;
            }
        }

        public ICommand LocateCommand { get; }

        public ICommand ClearSearchCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand SaveAndFinalizeCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand MoveCommand { get; }

        public ICommand ImportCommand { get; }

        public ICommand FillFromTextCommand { get; }

        public ICommand ExportCommand { get; }

        public void AttachTo(EditorPlacement placement)
        {
            Placement = placement;
            RequestedPlacement = null;
            FinalizeRequested = false;
        }

        public void Reload()
        {
            Dictionary<string, string> edits = Items.Where(item => item.IsModified)
                .ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

            SetItems(_document.LoadPlaceholders().Select(placeholder =>
            {
                PlaceholderItemViewModel item = new PlaceholderItemViewModel(placeholder);
                if (edits.TryGetValue(item.Name, out string value))
                {
                    item.Value = value;
                }

                return item;
            }));
        }

        public IReadOnlyDictionary<string, string> GetChanges() =>
            Items.Where(item => item.IsModified).ToDictionary(item => item.Name, item => item.Value ?? string.Empty, StringComparer.Ordinal);

        public override bool CanClose() => !HasChanges || ConfirmDiscardChanges();

        void SetItems(IEnumerable<PlaceholderItemViewModel> items)
        {
            if (Items != null)
            {
                foreach (PlaceholderItemViewModel item in Items)
                {
                    item.PropertyChanged -= HandleItemPropertyChanged;
                }
            }

            List<PlaceholderItemViewModel> list = items.ToList();
            foreach (PlaceholderItemViewModel item in list)
            {
                item.PropertyChanged += HandleItemPropertyChanged;
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(list);
            view.Filter = item => ((PlaceholderItemViewModel)item).Matches(SearchText);

            Items = list;
            ItemsView = view;
            VisibleCount = view.Cast<object>().Count();
            ModifiedCount = list.Count(item => item.IsModified);
        }

        // Called by Fody when SearchText changes.
        void OnSearchTextChanged()
        {
            ItemsView.Refresh();
            VisibleCount = ItemsView.Cast<object>().Count();
        }

        void HandleItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaceholderItemViewModel.IsModified))
            {
                ModifiedCount = Items.Count(item => item.IsModified);
            }
        }

        void Locate(PlaceholderItemViewModel item)
        {
            if (!_document.SelectPlaceholder(item.Name, item.TakeNextOccurrence()))
            {
                _dialogs.ShowMessage(Caption, $"\"{item.Name}\" is no longer in the document. The list will be reloaded.", MessageKind.Information);
                Reload();
            }
        }

        // Writes the edits to the document. False when the user canceled.
        public bool SaveChanges()
        {
            PlaceholderOperationResult result = _document.SavePlaceholders(GetChanges());
            if (result.WasCanceled)
            {
                _dialogs.ShowMessage(Caption, TextFormat.Canceled(result.Processed, "updated"), MessageKind.Information);
                return false;
            }

            if (result.Skipped > 0)
            {
                _dialogs.ShowMessage(Caption, $"{TextFormat.Quantity(result.Skipped, "content control")} could not be updated. They may be locked.", MessageKind.Warning);
            }

            return true;
        }

        void Save()
        {
            SaveChanges();
            if (IsInSidePanel)
            {
                Reload();
            }
            else
            {
                Close(true);
            }
        }

        void SaveAndFinalize()
        {
            if (HasChanges && !SaveChanges())
            {
                return;
            }

            FinalizeRequested = true;
            Close(true);
        }

        // Fills in the editor only; the user reviews the values and saves them like any other edit.
        void Import()
        {
            string path = _dialogs.ShowOpenFileDialog("Import values", ValuesFileFilter);
            if (path == null)
            {
                return;
            }

            IReadOnlyDictionary<string, string> values;
            try
            {
                values = _valuesFile.Read(path);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                _dialogs.ShowMessage(Caption, $"Could not import {Path.GetFileName(path)}. {ex.Message}", MessageKind.Warning);
                return;
            }

            int filled = FillIn(values);
            if (filled == 0)
            {
                _dialogs.ShowMessage(Caption, $"None of the names in {Path.GetFileName(path)} match a placeholder in this document.", MessageKind.Information);
                return;
            }

            string message = $"Filled in {TextFormat.Quantity(filled, "placeholder")} from {Path.GetFileName(path)}. Check the values and click Save to write them to the document.";
            int unmatched = values.Count - filled;
            if (unmatched > 0)
            {
                message += $" {TextFormat.Quantity(unmatched, "name")} in the file did not match a placeholder.";
            }

            _dialogs.ShowMessage(Caption, message, MessageKind.Information);
        }

        // Like Import, the values found only fill in the editor; the user checks them and saves.
        void FillFromText()
        {
            FillFromTextViewModel dialog = _createFillFromText(Items.Select(item => item.Name).ToList());
            if (_dialogs.ShowDialog(dialog) != true)
            {
                return;
            }

            int filled = FillIn(dialog.Values);
            if (filled == 0)
            {
                _dialogs.ShowMessage(Caption, "No values for these placeholders were found in the text.", MessageKind.Information);
                return;
            }

            string message = $"Filled in {TextFormat.Quantity(filled, "placeholder")} from the text. Check the values and click Save to write them to the document.";
            int missing = Items.Count - filled;
            if (missing > 0)
            {
                message += $" The text gave no value for {TextFormat.Quantity(missing, "placeholder")}.";
            }

            _dialogs.ShowMessage(Caption, message, MessageKind.Information);
        }

        int FillIn(IReadOnlyDictionary<string, string> values)
        {
            int filled = 0;
            foreach (PlaceholderItemViewModel item in Items)
            {
                if (values.TryGetValue(item.Name, out string value))
                {
                    item.Value = value;
                    filled++;
                }
            }

            return filled;
        }

        // Exports what the editor shows, unsaved edits included. Empty placeholders are left out,
        // so importing the file never clears a value.
        void Export()
        {
            string path = _dialogs.ShowSaveFileDialog("Export values", ValuesFileFilter, _document.Name + ".json");
            if (path == null)
            {
                return;
            }

            try
            {
                _valuesFile.Write(path, Items.Where(HasValue).Select(item => new KeyValuePair<string, string>(item.Name, item.Value)));
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                _dialogs.ShowMessage(Caption, $"Could not save {Path.GetFileName(path)}. {ex.Message}", MessageKind.Warning);
            }
        }

        static bool HasValue(PlaceholderItemViewModel item) => !string.IsNullOrEmpty(item.Value);

        static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException;

        void CloseEditor()
        {
            if (CanClose())
            {
                Close(false);
            }
        }

        void Move()
        {
            RequestedPlacement = IsInSidePanel ? EditorPlacement.Window : EditorPlacement.SidePanel;
            Close(null);
        }

        bool ConfirmDiscardChanges() =>
            _dialogs.Confirm("Discard changes?", $"You have {TextFormat.Quantity(ModifiedCount, "unsaved change")}. Close the editor without saving them?");
    }
}
