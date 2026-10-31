using System.Collections.Generic;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IPlaceholderDetector
    {
        // text may be just the selection; documentText is always the whole document.
        IReadOnlyList<DetectedPlaceholder> Detect(string text, string documentText, string note, IReadOnlyList<string> existingNames);
    }
}
