using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Tools.Ribbon;
using PlaceKit.Interfaces;
using PlaceKit.Tools;
using PlaceKit.ViewModels;

namespace PlaceKit
{
    public partial class ThisAddIn
    {
        ServiceProvider _services;

        // Office asks for the ribbon before Startup, so the container is built here.
        // Application and CustomTaskPanes are not ready yet and are passed lazily.
        protected override IRibbonExtension[] CreateRibbonObjects()
        {
            NetworkSettings.UseModernTls();
            _services = CompositionRoot.Build(() => Application, () => CustomTaskPanes);
            return new IRibbonExtension[] { new PlaceKitRibbon(_services.GetRequiredService<RibbonViewModel>()) };
        }

        // Word doesn't wait for the answer; the Update button appears when one is out.
        void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            _ = _services?.GetRequiredService<IUpdateService>().CheckInBackgroundAsync();
        }

        void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            _services?.Dispose();
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        #endregion
    }
}
