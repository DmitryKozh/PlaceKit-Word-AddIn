using System.Windows.Controls;

namespace PlaceKit.Views
{
    public partial class EditorView : UserControl
    {
        public EditorView()
        {
            InitializeComponent();
        }

        public void FocusSearch() => SearchBox.Focus();
    }
}
