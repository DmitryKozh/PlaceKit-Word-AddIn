using System;
using System.Collections.Generic;
using System.Linq;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Tests.Fakes
{
    sealed class FakeDocument : IPlaceholderDocument
    {
        public FakeDocument(params PlaceholderInfo[] placeholders)
        {
            Placeholders = placeholders.ToList();
        }

        public List<PlaceholderInfo> Placeholders { get; set; }

        public List<IReadOnlyDictionary<string, string>> Saves { get; } = new List<IReadOnlyDictionary<string, string>>();

        public List<string> Selected { get; } = new List<string>();

        public bool SelectResult { get; set; } = true;

        public int Loads { get; private set; }

        public IReadOnlyList<PlaceholderInfo> LoadPlaceholders()
        {
            Loads++;
            return Placeholders.ToList();
        }

        // Applies the values, so a reload sees what was saved.
        public PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values)
        {
            Saves.Add(values);
            Placeholders = Placeholders
                .Select(p => values.TryGetValue(p.Name, out string value) ? new PlaceholderInfo(p.Name, value, p.Occurrences, false) : p)
                .ToList();
            return new PlaceholderOperationResult(values.Count, 0, false);
        }

        public bool SelectPlaceholder(string name, int occurrence)
        {
            Selected.Add(name + "#" + occurrence);
            return SelectResult;
        }
    }

    sealed class FailingDocument : IPlaceholderDocument
    {
        public IReadOnlyList<PlaceholderInfo> LoadPlaceholders() => throw new InvalidOperationException("boom");

        public PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values) => throw new InvalidOperationException("boom");

        public bool SelectPlaceholder(string name, int occurrence) => throw new InvalidOperationException("boom");
    }
}
