using System.Collections.Generic;

namespace PlaceKit.Models
{
    public sealed class PlaceholderOperationResult
    {
        public PlaceholderOperationResult(int processed, int skipped, bool wasCanceled, IReadOnlyList<string> problems = null)
        {
            Processed = processed;
            Skipped = skipped;
            WasCanceled = wasCanceled;
            Problems = problems ?? new string[0];
        }

        public int Processed { get; }

        public int Skipped { get; }

        public bool WasCanceled { get; }

        // Why items were skipped, where that is known, such as "[30]": not in the document as a whole word.
        public IReadOnlyList<string> Problems { get; }
    }
}
