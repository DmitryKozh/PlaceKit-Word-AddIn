using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Input;
using Microsoft.Office.Tools.Ribbon;
using PlaceKit.Tools;
using PlaceKit.ViewModels;
using PlaceKit.Views;

namespace PlaceKit
{
    public partial class PlaceKitRibbon
    {
        readonly RibbonViewModel _viewModel;

        public PlaceKitRibbon(RibbonViewModel viewModel)
            : this()
        {
            _viewModel = viewModel;
            try
            {
                SetImages(RibbonImages.Load(ScreenScale(), OfficeTheme.IsDark()));
            }
            catch (Exception ex)
            {
                // The icons are only cosmetic: the tab must load even without them.
                Trace.TraceError("PlaceKit: cannot load the ribbon images: {0}", ex);
                SetOfficeImages();
            }
        }

        // The system scale; a monitor with another scale gets the image resized by Office.
        static double ScreenScale()
        {
            using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero))
            {
                return screen.DpiX / 96;
            }
        }

        // Chosen once for the theme Word starts with; another theme chosen later shows after a restart.
        void SetImages(IReadOnlyDictionary<RibbonIcon, Bitmap> images)
        {
            btnCreate.Image = images[RibbonIcon.Create];
            btnFill.Image = images[RibbonIcon.Fill];
            btnFinalize.Image = images[RibbonIcon.Finalize];
            btnGuide.Image = images[RibbonIcon.Guide];
            btnSettings.Image = images[RibbonIcon.Settings];
            btnAbout.Image = images[RibbonIcon.About];
            btnHire.Image = images[RibbonIcon.Hire];
        }

        void SetOfficeImages()
        {
            btnCreate.OfficeImageId = "ContentControlText";
            btnFill.OfficeImageId = "EditDocument";
            btnFinalize.OfficeImageId = "ReviewAcceptChange";
            btnGuide.OfficeImageId = "Help";
            btnSettings.OfficeImageId = "PropertySheet";
            btnAbout.OfficeImageId = "Info";
            btnHire.OfficeImageId = "HappyFace";
        }

        void btnCreate_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.CreateCommand);

        void btnFill_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.FillCommand);

        void btnFinalize_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.FinalizeCommand);

        void btnGuide_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.GuideCommand);

        void btnSettings_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.SettingsCommand);

        void btnAbout_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.AboutCommand);

        void btnHire_Click(object sender, RibbonControlEventArgs e) => Execute(_viewModel.HireCommand);

        static void Execute(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
