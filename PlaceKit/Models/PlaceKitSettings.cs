namespace PlaceKit.Models
{
    public sealed class PlaceKitSettings
    {
        public string OpeningMarker { get; set; } = PlaceholderPattern.Default.Opening;

        public string ClosingMarker { get; set; } = PlaceholderPattern.Default.Closing;

        public bool ShowCreateSummary { get; set; } = true;

        public bool OpenEditorAfterCreate { get; set; }

        public EditorPlacement EditorPlacement { get; set; } = EditorPlacement.SidePanel;

        public bool AskAboutEmptyPlaceholders { get; set; } = true;

        public EmptyPlaceholderAction EmptyPlaceholderAction { get; set; } = EmptyPlaceholderAction.KeepMarkers;

        // keys are in IApiKeyStore
        public AiProvider AiProvider { get; set; } = AiProvider.Claude;

        public string DetectNote { get; set; } = string.Empty;

        public bool CheckForUpdates { get; set; } = true;

        public PlaceholderPattern CreatePattern() => new PlaceholderPattern(OpeningMarker, ClosingMarker);
    }
}
