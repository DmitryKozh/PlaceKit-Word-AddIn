using System.Collections.Generic;

namespace PlaceKit.Interfaces
{
    // A file of placeholder values, used as a template for other documents.
    public interface IPlaceholderValuesFile
    {
        // Names are matched ignoring case. Throws InvalidDataException when the file has another format.
        IReadOnlyDictionary<string, string> Read(string path);

        void Write(string path, IEnumerable<KeyValuePair<string, string>> values);
    }
}
