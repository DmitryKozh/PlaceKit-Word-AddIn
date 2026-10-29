using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class AboutViewModel : DialogViewModelBase
    {
        public AboutViewModel(ProductInfo product, IUrlLauncher urlLauncher)
            : base("About " + product.Name)
        {
            Product = product;
            OpenWebsiteCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.Website, "about")));
            ContactCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.ContactPage, "about")));
            CloseCommand = new RelayCommand(() => Close(true));
        }

        public ProductInfo Product { get; }

        public string VersionText => $"Version {Product.Version}";

        public string WebsiteText => Product.Website.Host;

        public string Introduction =>
            $"PlaceKit is a sample of my work. I'm {Product.Developer}, a C# and .NET developer with 11 years of experience, and I take the boring jobs off your desk.";

        public ICommand OpenWebsiteCommand { get; }

        public ICommand ContactCommand { get; }

        public ICommand CloseCommand { get; }
    }
}
