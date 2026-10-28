namespace PlaceKit.Models
{
    // Total == 0: the amount of work is not known yet.
    public sealed class ProgressInfo
    {
        public ProgressInfo(int current, int total)
        {
            Current = current;
            Total = total;
        }

        public int Current { get; }

        public int Total { get; }
    }
}
