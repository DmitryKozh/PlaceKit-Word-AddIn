using System;

namespace PlaceKit.ViewModels
{
    public abstract class DialogViewModelBase : ViewModelBase
    {
        protected DialogViewModelBase(string title)
        {
            Title = title;
        }

        public event EventHandler<DialogCloseRequestedEventArgs> CloseRequested;

        public string Title { get; }

        public virtual bool CanClose() => true;

        protected void Close(bool? dialogResult) =>
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(dialogResult));
    }
}
