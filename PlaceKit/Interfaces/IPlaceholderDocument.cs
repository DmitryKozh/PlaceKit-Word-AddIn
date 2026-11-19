using System;
using System.Collections.Generic;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IPlaceholderDocument
    {
        string Name { get; }

        // The guide that PlaceKit made, where Detect and From text work without an API key
        bool IsGuide { get; }

        IReadOnlyList<PlaceholderInfo> LoadPlaceholders();

        PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values);

        bool SelectPlaceholder(string name, int occurrence);

        // Content controls read as <placeholder name="..."/>.
        string ReadText();

        // null when nothing is selected.
        string ReadSelectedText();

        PlaceholderOperationResult CreatePlaceholders(IReadOnlyList<DetectedPlaceholder> placeholders);

        bool SelectText(string text);
    }
}
