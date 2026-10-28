using System;
using System.Windows.Input;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class ProgressViewModel : ViewModelBase
    {
        readonly Action _cancel;

        public ProgressViewModel(string title, Action cancel)
        {
            Title = title;
            _cancel = cancel;
            CancelCommand = new RelayCommand(Cancel, () => !IsCanceling);
        }

        public string Title { get; }

        public int Current { get; private set; }

        public int Total { get; private set; }

        public bool IsCanceling { get; private set; }

        public bool IsIndeterminate => Total <= 0;

        public string StatusText =>
            IsCanceling ? "Canceling..."
            : Total > 0 ? $"{Current} of {Total}"
            : "Please wait...";

        public ICommand CancelCommand { get; }

        public void Update(ProgressInfo progress)
        {
            Total = Math.Max(progress.Total, progress.Current);
            Current = progress.Current;
        }

        void Cancel()
        {
            IsCanceling = true;
            _cancel();
        }
    }
}
