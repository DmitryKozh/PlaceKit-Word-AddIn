using System.Windows.Input;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    // Shown once, when Word first opens a document window after PlaceKit is installed.
    // Closes with true when the user wants the sample, which the ribbon then opens.
    public sealed class WelcomeViewModel : DialogViewModelBase
    {
        readonly IAiService _ai;

        public WelcomeViewModel(IAiService ai, IUrlLauncher urlLauncher, ProductInfo product)
            : base("Welcome to PlaceKit")
        {
            _ai = ai;
            IsAiConnected = ai.HasApiKey;

            OpenSampleCommand = new RelayCommand(() => Close(true));
            SetUpAiCommand = new RelayCommand(SetUpAi);
            WatchVideoCommand = new RelayCommand(() => urlLauncher.Open(product.Link(product.Walkthrough, "welcome")));
            CloseCommand = new RelayCommand(() => Close(false));
        }

        public bool IsAiConnected { get; private set; }

        public bool CanSetUpAi => !IsAiConnected;

        public string AiStatus => IsAiConnected ? $"{AiProviders.Name(_ai.Provider)} is connected." : null;

        public ICommand OpenSampleCommand { get; }

        public ICommand SetUpAiCommand { get; }

        public ICommand WatchVideoCommand { get; }

        public ICommand CloseCommand { get; }

        void SetUpAi()
        {
            _ai.SetUp(null);
            IsAiConnected = _ai.HasApiKey;
        }
    }
}
