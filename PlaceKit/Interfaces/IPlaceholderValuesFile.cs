using System.Collections.Generic;

namespace PlaceKit.Interfaces
{
    public interface IPlaceholderValuesFile
    {
        // Names match ignoring case. Throws InvalidDataException for any other format.
        IReadOnlyDictionary<string, string> Read(string path);

        void Write(string path, IEnumerable<KeyValuePair<string, string>> values);
    }
}
