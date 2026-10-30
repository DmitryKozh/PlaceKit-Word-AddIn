using System.Windows;
using PlaceKit.ViewModels;

namespace PlaceKit.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        // A Hidden page can't take the focus, so the AI page's key box gets it here.
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel settings && settings.ShowAiPage)
            {
                ApiKeyBox.Focus();
            }
        }
    }
}
