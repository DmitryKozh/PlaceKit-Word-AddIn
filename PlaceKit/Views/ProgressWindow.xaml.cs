using System.ComponentModel;
using System.Windows;
using PlaceKit.ViewModels;

namespace PlaceKit.Views
{
    public partial class ProgressWindow : Window
    {
        bool _closingFromHost;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        internal void CloseFromHost()
        {
            _closingFromHost = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // X means cancel; the host closes the window when the work is done.
            if (!_closingFromHost)
            {
                e.Cancel = true;
                if (DataContext is ProgressViewModel viewModel && viewModel.CancelCommand.CanExecute(null))
                {
                    viewModel.CancelCommand.Execute(null);
                }
            }

            base.OnClosing(e);
        }
    }
}
