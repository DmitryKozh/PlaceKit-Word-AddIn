using System.Collections.Generic;

namespace PlaceKit.Interfaces
{
    public interface IValueFinder
    {
        // Names the text has no value for are left out.
        IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names);
    }
}
