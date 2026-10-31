using System;
using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class AboutViewModel : DialogViewModelBase
    {
        public AboutViewModel(ProductInfo product, IUrlLauncher urlLauncher, IDialogService dialogs, Func<SettingsViewModel> createSettings)
            : base("About " + product.Name)
        {
            Product = product;
            CheckForUpdatesCommand = new RelayCommand(() => dialogs.RunAndReportErrors(() =>
            {
                SettingsViewModel settings = createSettings();
                settings.ShowUpdatesPage = true;
                settings.CheckNowCommand.Execute(null);
                dialogs.ShowDialog(settings);
            }));
            OpenWebsiteCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.Website, "about")));
            ContactCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.ContactPage, "about")));
            OpenLinkedInCommand = new RelayCommand(() => urlLauncher.Open(product.LinkedIn));
            OpenLicenseCommand = new RelayCommand(() => urlLauncher.Open(product.LicenseFile));
            OpenNoticesCommand = new RelayCommand(() => urlLauncher.Open(product.NoticesFile));
            AskForLicenseCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.ContactPage, "license")));
            CloseCommand = new RelayCommand(() => Close(true));
        }

        public ProductInfo Product { get; }

        public string VersionText => $"Version {Product.Version}";

        public string WebsiteText => Product.Website.Host;

        public string Introduction =>
            $"PlaceKit is a sample of my work. I'm {Product.Developer}, a C# and .NET developer with 11 years of experience, and I take the boring jobs off your desk.";

        public ICommand CheckForUpdatesCommand { get; }

        public ICommand OpenWebsiteCommand { get; }

        public ICommand ContactCommand { get; }

        public ICommand OpenLinkedInCommand { get; }

        public ICommand OpenLicenseCommand { get; }

        public ICommand OpenNoticesCommand { get; }

        public ICommand AskForLicenseCommand { get; }

        public ICommand CloseCommand { get; }
    }
}
