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
    }
}
