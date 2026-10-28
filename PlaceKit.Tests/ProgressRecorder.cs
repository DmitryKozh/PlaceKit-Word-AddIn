using System;
using PlaceKit.Models;

namespace PlaceKit.Tests
{
    sealed class ProgressRecorder : IProgress<ProgressInfo>
    {
        public int Reports { get; private set; }

        public ProgressInfo Last { get; private set; }

        public void Report(ProgressInfo value)
        {
            Reports++;
            Last = value;
        }
    }
}
