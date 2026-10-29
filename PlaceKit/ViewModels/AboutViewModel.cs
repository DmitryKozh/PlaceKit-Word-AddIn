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
            OpenWebsiteCommand = new RelayCommand(() => urlLauncher.Open(product.Website));
            CloseCommand = new RelayCommand(() => Close(true));
        }

        public ProductInfo Product { get; }

        public string VersionText => $"Version {Product.Version}";

        public string WebsiteText => Product.Website.Host;

        public ICommand OpenWebsiteCommand { get; }

        public ICommand CloseCommand { get; }
    }
}
