using System;
using System.Text;

namespace PlaceKit.Tools
{
    public static class WordText
    {
        const string LineBreak = "\v";

        // Breaks become \n; cell, field and object marks are dropped.
        public static string ToPlain(string wordText)
        {
            StringBuilder plain = new StringBuilder((wordText ?? string.Empty).Length);
            foreach (char character in wordText ?? string.Empty)
            {
                if (character == '\r' || character == '\v' || character == '\f')
                {
                    plain.Append('\n');
                }
                else if (character == '\t' || character >= ' ')
                {
                    plain.Append(character);
                }
            }

            return plain.ToString();
        }

        public static int CountWholeWords(string text, string needle)
        {
            int count = 0;
            for (int index = text.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = text.IndexOf(needle, index + 1, StringComparison.Ordinal))
            {
                if (IsWholeWord(text, index, needle.Length))
                {
                    count++;
                }
            }

            return count;
        }

        // A letter or digit right before or after the match makes it part of a longer word, unless the match
        // itself starts or ends with something else, such as "(4711)".
        public static bool IsWholeWord(string text, int start, int length) =>
            !(start > 0 && char.IsLetterOrDigit(text[start - 1]) && char.IsLetterOrDigit(text[start]))
            && !(start + length < text.Length && char.IsLetterOrDigit(text[start + length]) && char.IsLetterOrDigit(text[start + length - 1]));

        public static string ToDisplay(string wordText) =>
            (wordText ?? string.Empty).Replace("\r", LineBreak).Replace(LineBreak, Environment.NewLine);

        // A plain-text control can't contain a paragraph mark, so every newline
        // becomes a manual line break.
        public static string ToWord(string displayText) =>
            (displayText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", LineBreak);
    }
}
