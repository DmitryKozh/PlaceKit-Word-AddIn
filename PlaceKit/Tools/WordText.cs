using System;

namespace PlaceKit.Tools
{
    public static class WordText
    {
        const string LineBreak = "\v";

        public static string ToDisplay(string wordText) =>
            (wordText ?? string.Empty).Replace("\r", LineBreak).Replace(LineBreak, Environment.NewLine);

        // A plain-text control can't contain a paragraph mark, so every newline
        // becomes a manual line break.
        public static string ToWord(string displayText) =>
            (displayText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", LineBreak);
    }
}
