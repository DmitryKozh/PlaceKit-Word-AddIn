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

        // A Hidden page can't take the focus, so a window that opens on another page than General moves it here.
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is SettingsViewModel settings))
            {
                return;
            }

            if (settings.ShowAiPage)
            {
                ApiKeyBox.Focus();
            }
            else if (settings.ShowUpdatesPage)
            {
                (settings.IsUpdateAvailable ? DownloadButton : CheckNowButton).Focus();
            }
        }
    }
}
