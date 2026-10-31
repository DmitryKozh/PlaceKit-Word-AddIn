using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.ViewModels;
using PlaceKit.Views;
using Forms = System.Windows.Forms;

namespace PlaceKit.Services
{
    public sealed class DialogService : IDialogService
    {
        static readonly Dictionary<Type, Func<Window>> Views = new Dictionary<Type, Func<Window>>
        {
            [typeof(MessageViewModel)] = () => new MessageWindow(),
            [typeof(CreateSummaryViewModel)] = () => new CreateSummaryWindow(),
            [typeof(EmptyPlaceholdersViewModel)] = () => new EmptyPlaceholdersWindow(),
            [typeof(EditorViewModel)] = () => new EditorWindow(),
            [typeof(FillFromTextViewModel)] = () => new FillFromTextWindow(),
            [typeof(DetectViewModel)] = () => new DetectWindow(),
            [typeof(SettingsViewModel)] = () => new SettingsWindow(),
            [typeof(AboutViewModel)] = () => new AboutWindow(),
        };

        readonly IWordHost _wordHost;
        readonly IServiceProvider _services;

        readonly Stack<Window> _openDialogs = new Stack<Window>();

        public DialogService(IWordHost wordHost, IServiceProvider services)
        {
            _wordHost = wordHost;
            _services = services;
        }

        public bool? ShowDialog<TViewModel>() where TViewModel : DialogViewModelBase =>
            ShowDialog(_services.GetRequiredService<TViewModel>());

        public bool? ShowDialog(DialogViewModelBase viewModel)
        {
            if (!Views.TryGetValue(viewModel.GetType(), out Func<Window> createView))
            {
                throw new InvalidOperationException($"No view is registered for {viewModel.GetType().Name}.");
            }

            Window window = createView();
            window.DataContext = viewModel;
            SetOwner(window);

            bool closeRequested = false;

            void OnCloseRequested(object sender, DialogCloseRequestedEventArgs e)
            {
                closeRequested = true;
                if (e.DialogResult.HasValue)
                {
                    window.DialogResult = e.DialogResult;
                }
                else
                {
                    window.Close();
                }
            }

            // The title bar's close button bypasses the commands, so let the view model veto it.
            void OnClosing(object sender, CancelEventArgs e) => e.Cancel = !closeRequested && !viewModel.CanClose();

            viewModel.CloseRequested += OnCloseRequested;
            window.Closing += OnClosing;
            _openDialogs.Push(window);
            try
            {
                return window.ShowDialog();
            }
            finally
            {
                _openDialogs.Pop();
                window.Closing -= OnClosing;
                viewModel.CloseRequested -= OnCloseRequested;
            }
        }

        public void ShowMessage(string title, string message, MessageKind kind) =>
            ShowDialog(new MessageViewModel(title, message, kind));

        public bool Confirm(string title, string message) =>
            ShowDialog(new MessageViewModel(title, message, MessageKind.Question)) == true;

        public string ShowOpenFileDialog(string title, string filter)
        {
            using (var dialog = new Forms.OpenFileDialog { Title = title, Filter = filter })
            {
                return dialog.ShowDialog(FileDialogOwner()) == Forms.DialogResult.OK ? dialog.FileName : null;
            }
        }

        public string ShowSaveFileDialog(string title, string filter, string fileName)
        {
            using (var dialog = new Forms.SaveFileDialog { Title = title, Filter = filter, FileName = fileName })
            {
                return dialog.ShowDialog(FileDialogOwner()) == Forms.DialogResult.OK ? dialog.FileName : null;
            }
        }

        // WinForms dialogs take any window handle as owner, Word's too.
        Forms.IWin32Window FileDialogOwner() =>
            new WindowHandle(_openDialogs.Count > 0 ? new WindowInteropHelper(_openDialogs.Peek()).Handle : _wordHost.MainWindowHandle);

        void SetOwner(Window window)
        {
            if (_openDialogs.Count > 0)
            {
                window.Owner = _openDialogs.Peek();
                return;
            }

            IntPtr wordWindow = _wordHost.MainWindowHandle;
            if (wordWindow != IntPtr.Zero)
            {
                _ = new WindowInteropHelper(window) { Owner = wordWindow };
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        sealed class WindowHandle : Forms.IWin32Window
        {
            public WindowHandle(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
