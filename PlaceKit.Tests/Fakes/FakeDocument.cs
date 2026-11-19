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

        public string Name { get; set; } = "Offer";

        public bool IsGuide { get; set; }

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

        public string Text { get; set; } = string.Empty;

        public string SelectedText { get; set; }

        public List<IReadOnlyList<DetectedPlaceholder>> Created { get; } = new List<IReadOnlyList<DetectedPlaceholder>>();

        public List<string> SelectedTexts { get; } = new List<string>();

        public string ReadText() => Text;

        public string ReadSelectedText() => SelectedText;

        public PlaceholderOperationResult CreatePlaceholders(IReadOnlyList<DetectedPlaceholder> placeholders)
        {
            Created.Add(placeholders);
            return new PlaceholderOperationResult(placeholders.Count, 0, false);
        }

        public bool SelectText(string text)
        {
            SelectedTexts.Add(text);
            return SelectResult;
        }
    }

    sealed class FailingDocument : IPlaceholderDocument
    {
        public string Name => "Failing";

        public bool IsGuide => false;

        public IReadOnlyList<PlaceholderInfo> LoadPlaceholders() => throw new InvalidOperationException("boom");

        public PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values) => throw new InvalidOperationException("boom");

        public bool SelectPlaceholder(string name, int occurrence) => throw new InvalidOperationException("boom");

        public string ReadText() => throw new InvalidOperationException("boom");

        public string ReadSelectedText() => throw new InvalidOperationException("boom");

        public PlaceholderOperationResult CreatePlaceholders(IReadOnlyList<DetectedPlaceholder> placeholders) => throw new InvalidOperationException("boom");

        public bool SelectText(string text) => throw new InvalidOperationException("boom");
    }
}
