namespace PlaceKit.Models
{
    public sealed class PlaceholderOperationResult
    {
        public PlaceholderOperationResult(int processed, int skipped, bool wasCanceled)
        {
            Processed = processed;
            Skipped = skipped;
            WasCanceled = wasCanceled;
        }

        public int Processed { get; }

        public int Skipped { get; }

        public bool WasCanceled { get; }
    }
}
