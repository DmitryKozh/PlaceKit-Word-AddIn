using System;
using System.Collections.Generic;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    internal sealed class ProgressTracker
    {
        readonly int _total;
        readonly IProgress<ProgressInfo> _progress;
        readonly List<string> _problems = new List<string>();

        public ProgressTracker(int total, IProgress<ProgressInfo> progress)
        {
            _total = total;
            _progress = progress;
            _progress.Report(new ProgressInfo(0, total));
        }

        public int Processed { get; private set; }

        public int Skipped { get; private set; }

        public void Add(bool succeeded, string problem = null)
        {
            if (succeeded)
            {
                Processed++;
            }
            else
            {
                Skipped++;
            }

            if (problem != null)
            {
                _problems.Add(problem);
            }

            int done = Processed + Skipped;
            _progress.Report(new ProgressInfo(done, Math.Max(done, _total)));
        }

        public PlaceholderOperationResult ToResult(bool wasCanceled) =>
            new PlaceholderOperationResult(Processed, Skipped, wasCanceled, _problems);
    }
}
