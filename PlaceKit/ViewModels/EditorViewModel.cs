using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        readonly IPlaceholderDocument _document;
        readonly IDialogService _dialogs;

        public EditorViewModel(IPlaceholderDocument document, IDialogService dialogs)
            : base("Fill placeholders")
        {
            _document = document;
            _dialogs = dialogs;
            SetItems(Enumerable.Empty<PlaceholderItemViewModel>());

            LocateCommand = new RelayCommand<PlaceholderItemViewModel>(item => _dialogs.RunAndReportErrors(() => Locate(item)), item => item != null);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty, () => HasSearchText);
            SaveCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Save), () => HasChanges);
            CancelCommand = new RelayCommand(Cancel);
            RefreshCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Reload));
            MoveCommand = new RelayCommand(() => _dialogs.RunAndReportErrors(Move));
        }

        public EditorPlacement Placement { get; private set; }

        public bool IsInSidePanel => Placement == EditorPlacement.SidePanel;

        public bool IsInWindow => Placement == EditorPlacement.Window;

        // null closes the editor for good
        public EditorPlacement? RequestedPlacement { get; private set; }

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

        public ICommand CancelCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand MoveCommand { get; }

        public void AttachTo(EditorPlacement placement)
        {
            Placement = placement;
            RequestedPlacement = null;
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

        void Save()
        {
            PlaceholderOperationResult result = _document.SavePlaceholders(GetChanges());
            if (result.WasCanceled)
            {
                _dialogs.ShowMessage(Caption, $"Saving was canceled. Placeholders updated so far: {result.Processed}. Press Ctrl+Z in Word to undo them.", MessageKind.Information);
            }
            else if (result.Skipped > 0)
            {
                _dialogs.ShowMessage(Caption, $"{TextFormat.Quantity(result.Skipped, "content control")} could not be updated. They may be locked.", MessageKind.Warning);
            }

            if (IsInSidePanel)
            {
                Reload();
            }
            else
            {
                Close(true);
            }
        }

        void Cancel()
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
