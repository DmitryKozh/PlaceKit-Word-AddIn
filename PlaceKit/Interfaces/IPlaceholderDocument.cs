using System;
using System.Collections.Generic;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IPlaceholderDocument
    {
        string Name { get; }

        IReadOnlyList<PlaceholderInfo> LoadPlaceholders();

        PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values);

        bool SelectPlaceholder(string name, int occurrence);

        // Content controls read as markers with their names.
        string ReadText(PlaceholderPattern pattern);

        // null when nothing is selected.
        string ReadSelectedText(PlaceholderPattern pattern);

        PlaceholderOperationResult CreatePlaceholders(IReadOnlyList<DetectedPlaceholder> placeholders);

        bool SelectText(string text);
    }
}
