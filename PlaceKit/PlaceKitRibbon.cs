using System.Windows.Input;
using Microsoft.Office.Tools.Ribbon;
using PlaceKit.ViewModels;

namespace PlaceKit
{
    public partial class PlaceKitRibbon
    {
        readonly RibbonViewModel _viewModel;

        public PlaceKitRibbon(RibbonViewModel viewModel)
            : this()
        {
            _viewModel = viewModel;
        }

        void btnCreate_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.CreateCommand);

        void btnFill_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.FillCommand);

        void btnClear_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.ClearCommand);

        void btnSettings_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.SettingsCommand);

        void btnAbout_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.AboutCommand);

        static void Execute(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
