namespace PlaceKit.Models
{
    // What Finalize does with placeholders that were not filled in.
    public enum EmptyPlaceholderAction
    {
        // Back to markers, to fill them in later.
        KeepMarkers,

        // Removed from the text.
        Delete,
    }
}
