using System.Windows.Input;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class MessageViewModel : DialogViewModelBase
    {
        public MessageViewModel(string title, string message, MessageKind kind)
            : base(title)
        {
            Message = message;
            Kind = kind;
            AcceptCommand = new RelayCommand(() => Close(true));
            RejectCommand = new RelayCommand(() => Close(false));
        }

        public string Message { get; }

        public MessageKind Kind { get; }

        public bool IsQuestion => Kind == MessageKind.Question;

        public string AcceptText => IsQuestion ? "_Yes" : "OK";

        public ICommand AcceptCommand { get; }

        public ICommand RejectCommand { get; }
    }
}
