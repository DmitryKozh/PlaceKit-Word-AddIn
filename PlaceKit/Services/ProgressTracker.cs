using System;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    internal sealed class ProgressTracker
    {
        readonly int _total;
        readonly IProgress<ProgressInfo> _progress;

        public ProgressTracker(int total, IProgress<ProgressInfo> progress)
        {
            _total = total;
            _progress = progress;
            _progress.Report(new ProgressInfo(0, total));
        }

        public int Processed { get; private set; }

        public int Skipped { get; private set; }

        public void Add(bool succeeded)
        {
            if (succeeded)
            {
                Processed++;
            }
            else
            {
                Skipped++;
            }

            int done = Processed + Skipped;
            _progress.Report(new ProgressInfo(done, Math.Max(done, _total)));
        }

        public PlaceholderOperationResult ToResult(bool wasCanceled) =>
            new PlaceholderOperationResult(Processed, Skipped, wasCanceled);
    }
}
