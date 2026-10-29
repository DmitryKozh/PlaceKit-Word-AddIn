namespace PlaceKit.Tools
{
    public static class TextFormat
    {
        public static string Quantity(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
