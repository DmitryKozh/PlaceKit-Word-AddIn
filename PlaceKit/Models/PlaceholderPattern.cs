using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlaceKit.Models
{
    public sealed class PlaceholderPattern
    {
        public const int MaxMarkerLength = 10;
        public const int MaxNameLength = 128;

        const string WordWildcardCharacters = @"[](){}<>?*!@\-";

        readonly Regex _candidates;

        public PlaceholderPattern(string opening, string closing)
        {
            string error = ValidateMarker(opening) ?? ValidateMarker(closing);
            if (error != null)
            {
                throw new ArgumentException(error);
            }

            Opening = opening;
            Closing = closing;
            _candidates = new Regex(Regex.Escape(opening) + ".*?" + Regex.Escape(closing), RegexOptions.Singleline | RegexOptions.CultureInvariant);
        }

        public static PlaceholderPattern Default { get; } = new PlaceholderPattern("{{", "}}");

        public string Opening { get; }

        public string Closing { get; }

        public static string ValidateMarker(string marker)
        {
            if (string.IsNullOrEmpty(marker))
            {
                return "Enter a marker.";
            }

            if (marker.Length > MaxMarkerLength)
            {
                return $"Use at most {MaxMarkerLength} characters.";
            }

            if (marker.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            {
                return "Spaces are not allowed.";
            }

            return null;
        }

        public string Format(string name) => Opening + name + Closing;

        public bool TryGetName(string markedText, out string name)
        {
            name = null;
            if (markedText == null
                || markedText.Length < Opening.Length + Closing.Length
                || !markedText.StartsWith(Opening, StringComparison.Ordinal)
                || !markedText.EndsWith(Closing, StringComparison.Ordinal))
            {
                return false;
            }

            string candidate = markedText.Substring(Opening.Length, markedText.Length - Opening.Length - Closing.Length).Trim();
            if (candidate.Length == 0
                || candidate.Length > MaxNameLength
                || candidate.Contains(Opening)
                || candidate.Contains(Closing)
                || candidate.Any(char.IsControl))
            {
                return false;
            }

            name = candidate;
            return true;
        }

        // Only an estimate for the progress bar.
        public int CountCandidates(string text) => string.IsNullOrEmpty(text) ? 0 : _candidates.Matches(text).Count;

        // Word's * is lazy, so a match ends at the nearest closing marker.
        public string ToWordWildcards() => EscapeWildcards(Opening) + "*" + EscapeWildcards(Closing);

        static string EscapeWildcards(string text)
        {
            StringBuilder escaped = new StringBuilder(text.Length * 2);
            foreach (char c in text)
            {
                if (c == '^')
                {
                    escaped.Append("^^");
                }
                else
                {
                    if (WordWildcardCharacters.IndexOf(c) >= 0)
                    {
                        escaped.Append('\\');
                    }

                    escaped.Append(c);
                }
            }

            return escaped.ToString();
        }
    }
}
