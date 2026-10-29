using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace PlaceKit.Views
{
    // Task panes host WinForms controls, so the WPF view sits in an ElementHost.
    public sealed class EditorPaneControl : UserControl
    {
        public EditorPaneControl(object viewModel)
        {
            Controls.Add(new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = new EditorView { DataContext = viewModel },
            });
        }
    }
}
