using System;
using PlaceKit.Models;

namespace PlaceKit.ViewModels
{
    public sealed class PlaceholderItemViewModel : ViewModelBase
    {
        int _nextOccurrence;

        public PlaceholderItemViewModel(PlaceholderInfo placeholder)
        {
            Name = placeholder.Name;
            OriginalValue = placeholder.Value;
            Value = placeholder.Value;
            Occurrences = placeholder.Occurrences;
            HasMixedValues = placeholder.HasMixedValues;
        }

        public string Name { get; }

        public string OriginalValue { get; }

        public string Value { get; set; }

        public int Occurrences { get; }

        public bool HasMixedValues { get; }

        public bool IsUsedSeveralTimes => Occurrences > 1;

        public string UsageText => $"Used {Occurrences} times in the document. Saving fills in all of them.";

        public bool IsModified => !string.Equals(Value ?? string.Empty, OriginalValue, StringComparison.Ordinal);

        public bool Matches(string searchText)
        {
            string term = searchText?.Trim();
            return string.IsNullOrEmpty(term) || Contains(Name, term) || Contains(Value, term);
        }

        public int TakeNextOccurrence()
        {
            int occurrence = _nextOccurrence;
            _nextOccurrence = (_nextOccurrence + 1) % Occurrences;
            return occurrence;
        }

        static bool Contains(string text, string term) =>
            text != null && text.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
