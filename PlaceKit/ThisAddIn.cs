using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Tools.Ribbon;
using PlaceKit.Interfaces;
using PlaceKit.Tools;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit
{
    public partial class ThisAddIn
    {
        ServiceProvider _services;
        bool _welcomeSettled;

        // Office asks for the ribbon before Startup, so the container is built here.
        // Application and CustomTaskPanes are not ready yet and are passed lazily.
        protected override IRibbonExtension[] CreateRibbonObjects()
        {
            FileLog.Start();
            NetworkSettings.UseModernTls();
            _services = CompositionRoot.Build(() => Application, () => CustomTaskPanes);
            return new IRibbonExtension[] { new PlaceKitRibbon(_services.GetRequiredService<RibbonViewModel>()) };
        }

        // Word doesn't wait for the answer; the Update button appears when one is out.
        void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            Application.DocumentBeforeClose += HandleDocumentBeforeClose;
            Application.WindowActivate += HandleWindowActivate;
            _ = _services?.GetRequiredService<IUpdateService>().CheckInBackgroundAsync();

            // Word may show its first window before the add-in loads, and then no activation follows.
            OfferWelcome();
        }

        void HandleWindowActivate(Word.Document document, Word.Window window) => OfferWelcome();

        // Posted, so Word finishes showing its window first. Asked again at each window until settled, since
        // Word may not be visible yet or may be run by a program for now.
        void OfferWelcome()
        {
            if (_services == null || _welcomeSettled)
            {
                return;
            }

            RibbonViewModel ribbon = _services.GetRequiredService<RibbonViewModel>();
            _services.GetRequiredService<SynchronizationContext>().Post(state =>
            {
                if (!_welcomeSettled && ribbon.ShowWelcomeOnce())
                {
                    SettleWelcome();
                }
            }, null);
        }

        // An activation while the welcome is open settles it too, so this can come twice.
        void SettleWelcome()
        {
            if (!_welcomeSettled)
            {
                _welcomeSettled = true;
                Application.WindowActivate -= HandleWindowActivate;
            }
        }

        // After an error the document still closes: a question that keeps failing must not trap it.
        void HandleDocumentBeforeClose(Word.Document document, ref bool cancel)
        {
            if (cancel || _services == null)
            {
                return;
            }

            IEditorService editors = _services.GetRequiredService<IEditorService>();
            bool keepOpen = false;
            _services.GetRequiredService<IDialogService>().RunAndReportErrors(() => keepOpen = !editors.CanCloseDocument(document));
            cancel = keepOpen;
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
