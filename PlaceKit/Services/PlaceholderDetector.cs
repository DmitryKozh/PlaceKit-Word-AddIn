using System.Collections.Generic;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    public sealed class PlaceholderDetector : IPlaceholderDetector
    {
        readonly IAiService _ai;

        public PlaceholderDetector(IAiService ai)
        {
            _ai = ai;
        }

        public IReadOnlyList<DetectedPlaceholder> Detect(string text, string documentText, string note, IReadOnlyList<string> existingNames) =>
            DetectRequest.Parse(_ai.Send(DetectRequest.Create(text, note, existingNames), "Detecting placeholders"), documentText, existingNames);
    }
}
