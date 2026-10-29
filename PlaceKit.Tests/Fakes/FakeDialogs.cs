using System;
using System.Collections.Generic;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.Fakes
{
    // ShowDialog runs OnShowDialog "while the window is open", like the user clicking in it.
    sealed class FakeDialogs : IDialogService
    {
        public List<string> Messages { get; } = new List<string>();

        public List<DialogViewModelBase> Shown { get; } = new List<DialogViewModelBase>();

        public int Confirmations { get; private set; }

        public bool ConfirmAnswer { get; set; }

        public Action<DialogViewModelBase> OnShowDialog { get; set; } = viewModel => { };

        // What the user picks in the file dialogs; null means Cancel.
        public string OpenFileResult { get; set; }

        public string SaveFileResult { get; set; }

        public string SuggestedFileName { get; private set; }

        public bool? ShowDialog(DialogViewModelBase viewModel)
        {
            bool? result = null;
            void Closed(object sender, DialogCloseRequestedEventArgs e) => result = e.DialogResult;

            Shown.Add(viewModel);
            viewModel.CloseRequested += Closed;
            try
            {
                OnShowDialog(viewModel);
                return result;
            }
            finally
            {
                viewModel.CloseRequested -= Closed;
            }
        }

        public bool? ShowDialog<TViewModel>() where TViewModel : DialogViewModelBase => null;

        public void ShowMessage(string title, string message, MessageKind kind) => Messages.Add(message);

        public bool Confirm(string title, string message)
        {
            Confirmations++;
            return ConfirmAnswer;
        }

        public string ShowOpenFileDialog(string title, string filter) => OpenFileResult;

        public string ShowSaveFileDialog(string title, string filter, string fileName)
        {
            SuggestedFileName = fileName;
            return SaveFileResult;
        }
    }
}
