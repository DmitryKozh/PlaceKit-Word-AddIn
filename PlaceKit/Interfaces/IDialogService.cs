using PlaceKit.Models;
using PlaceKit.ViewModels;

namespace PlaceKit.Interfaces
{
    public interface IDialogService
    {
        bool? ShowDialog(DialogViewModelBase viewModel);

        bool? ShowDialog<TViewModel>() where TViewModel : DialogViewModelBase;

        void ShowMessage(string title, string message, MessageKind kind);

        bool Confirm(string title, string message);
    }
}
