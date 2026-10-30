using System.Collections.Generic;

namespace PlaceKit.Interfaces
{
    // Finds the values of placeholders in a text, such as an email or notes.
    public interface IValueFinder
    {
        // The values by placeholder name; placeholders the text gives no value for are left out.
        // Shows the progress window. Throws ValueFinderException with a message for the user when the
        // values can't be found, and OperationCanceledException when the user cancels.
        IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names);
    }
}
