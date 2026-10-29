using System.ComponentModel;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class SettingsViewModel : DialogViewModelBase, IDataErrorInfo
    {
        const string PreviewName = "Client name";

        readonly ISettingsService _settingsService;

        public SettingsViewModel(ISettingsService settingsService)
            : base("PlaceKit settings")
        {
            _settingsService = settingsService;
            StorageLocation = settingsService.Location;
            Apply(settingsService.Load());

            SaveCommand = new RelayCommand(Save, () => IsValid);
            CancelCommand = new RelayCommand(() => Close(false));
            ResetCommand = new RelayCommand(() => Apply(new PlaceKitSettings()));
        }

        public string OpeningMarker { get; set; }

        public string ClosingMarker { get; set; }

        public bool ShowCreateSummary { get; set; }

        public bool OpenEditorAfterCreate { get; set; }

        public bool CanChooseAutoOpen => !ShowCreateSummary;

        public EditorPlacement EditorPlacement { get; set; }

        // Ignore false: unchecking the other radio button must not change the value.
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

        public bool IsValid =>
            PlaceholderPattern.ValidateMarker(OpeningMarker) == null && PlaceholderPattern.ValidateMarker(ClosingMarker) == null;

        public string Preview => IsValid ? OpeningMarker + PreviewName + ClosingMarker : "-";

        public string StorageLocation { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ResetCommand { get; }

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

        void Apply(PlaceKitSettings settings)
        {
            OpeningMarker = settings.OpeningMarker;
            ClosingMarker = settings.ClosingMarker;
            ShowCreateSummary = settings.ShowCreateSummary;
            OpenEditorAfterCreate = settings.OpenEditorAfterCreate;
            EditorPlacement = settings.EditorPlacement;
        }

        void SetPlacement(bool isChecked, EditorPlacement placement)
        {
            if (isChecked)
            {
                EditorPlacement = placement;
            }
        }

        void Save()
        {
            _settingsService.Save(new PlaceKitSettings
            {
                OpeningMarker = OpeningMarker,
                ClosingMarker = ClosingMarker,
                ShowCreateSummary = ShowCreateSummary,
                OpenEditorAfterCreate = OpenEditorAfterCreate,
                EditorPlacement = EditorPlacement,
            });
            Close(true);
        }
    }
}
