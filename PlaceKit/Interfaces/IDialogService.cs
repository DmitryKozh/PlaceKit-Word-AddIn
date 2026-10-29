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

        // Return the chosen path, or null when the user cancels.
        string ShowOpenFileDialog(string title, string filter);

        string ShowSaveFileDialog(string title, string filter, string fileName);
    }
}
