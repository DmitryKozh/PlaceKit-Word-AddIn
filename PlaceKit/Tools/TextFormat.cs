namespace PlaceKit.Tools
{
    public static class TextFormat
    {
        public static string Quantity(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

        public static string Canceled(int processed, string verb) =>
            $"The operation was canceled. Placeholders {verb} so far: {processed}. Press Ctrl+Z in Word to undo them.";
    }
}
