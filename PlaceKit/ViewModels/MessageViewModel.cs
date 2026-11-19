using System.Windows.Input;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class MessageViewModel : DialogViewModelBase
    {
        public MessageViewModel(string title, string message, MessageKind kind, bool canCancel = false)
            : base(title)
        {
            Message = message;
            Kind = kind;
            CanCancel = canCancel;
            AcceptCommand = new RelayCommand(() => Answer(true));
            RejectCommand = new RelayCommand(() => Answer(false));
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public string Message { get; }

        public MessageKind Kind { get; }

        public bool IsQuestion => Kind == MessageKind.Question;

        public bool CanCancel { get; }

        public string AcceptText => IsQuestion ? "_Yes" : "OK";

        // Yes or No; Cancel and the title bar's close button leave it null.
        public bool? Reply { get; private set; }

        public ICommand AcceptCommand { get; }

        public ICommand RejectCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand EscapeCommand => CanCancel ? CancelCommand : RejectCommand;

        void Answer(bool reply)
        {
            Reply = reply;
            Close(reply);
        }
    }
}
