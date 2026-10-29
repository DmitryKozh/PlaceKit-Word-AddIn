using System.Windows;

namespace PlaceKit.Views
{
    public partial class EditorWindow : Window
    {
        public EditorWindow()
        {
            InitializeComponent();
            Loaded += (sender, e) => Editor.FocusSearch();
        }
    }
}
