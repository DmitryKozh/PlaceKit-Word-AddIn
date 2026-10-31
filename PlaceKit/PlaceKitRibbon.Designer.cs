namespace PlaceKit
{
    partial class PlaceKitRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public PlaceKitRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabPlaceKit = this.Factory.CreateRibbonTab();
            this.grpPlaceholders = this.Factory.CreateRibbonGroup();
            this.btnCreate = this.Factory.CreateRibbonButton();
            this.btnDetect = this.Factory.CreateRibbonButton();
            this.btnFill = this.Factory.CreateRibbonButton();
            this.btnFinalize = this.Factory.CreateRibbonButton();
            this.grpGeneral = this.Factory.CreateRibbonGroup();
            this.btnGuide = this.Factory.CreateRibbonButton();
            this.btnSettings = this.Factory.CreateRibbonButton();
            this.btnAbout = this.Factory.CreateRibbonButton();
            this.btnUpdate = this.Factory.CreateRibbonButton();
            this.grpBoringJob = this.Factory.CreateRibbonGroup();
            this.btnHire = this.Factory.CreateRibbonButton();
            this.tabPlaceKit.SuspendLayout();
            this.grpPlaceholders.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.grpBoringJob.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPlaceKit
            // 
            this.tabPlaceKit.Groups.Add(this.grpPlaceholders);
            this.tabPlaceKit.Groups.Add(this.grpGeneral);
            this.tabPlaceKit.Groups.Add(this.grpBoringJob);
            this.tabPlaceKit.Label = "PlaceKit";
            this.tabPlaceKit.Name = "tabPlaceKit";
            // 
            // grpPlaceholders
            // 
            this.grpPlaceholders.Items.Add(this.btnCreate);
            this.grpPlaceholders.Items.Add(this.btnDetect);
            this.grpPlaceholders.Items.Add(this.btnFill);
            this.grpPlaceholders.Items.Add(this.btnFinalize);
            this.grpPlaceholders.Label = "Placeholders";
            this.grpPlaceholders.Name = "grpPlaceholders";
            // 
            // btnCreate
            // 
            this.btnCreate.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnCreate.Label = "Create";
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.ScreenTip = "Create placeholders";
            this.btnCreate.ShowImage = true;
            this.btnCreate.SuperTip = "Turn every {{text}} marker in the document into a content control that you can fi" +
    "ll in later.";
            this.btnCreate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnCreate_Click);
            //
            // btnDetect
            //
            this.btnDetect.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnDetect.Label = "Detect";
            this.btnDetect.Name = "btnDetect";
            this.btnDetect.ScreenTip = "Detect placeholders";
            this.btnDetect.ShowImage = true;
            this.btnDetect.SuperTip = "Let Claude or ChatGPT find the parts of a finished document that change from one u" +
    "se to the next, such as names, dates and amounts. You choose which become placeholders.";
            this.btnDetect.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDetect_Click);
            // 
            // btnFill
            // 
            this.btnFill.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnFill.Label = "Fill";
            this.btnFill.Name = "btnFill";
            this.btnFill.ScreenTip = "Fill placeholders";
            this.btnFill.ShowImage = true;
            this.btnFill.SuperTip = "Open the editor to find, review and fill in all placeholders of the document in o" +
    "ne place. It opens in the side panel or in a separate window, as chosen in Settings.";
            this.btnFill.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnFill_Click);
            // 
            // btnFinalize
            // 
            this.btnFinalize.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnFinalize.Label = "Finalize";
            this.btnFinalize.Name = "btnFinalize";
            this.btnFinalize.ScreenTip = "Finalize the document";
            this.btnFinalize.ShowImage = true;
            this.btnFinalize.SuperTip = "Turn the placeholders into regular text when the document is ready. For placeholders tha" +
    "t are not filled in, you choose: turn them back into markers or delete them.";
            this.btnFinalize.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnFinalize_Click);
            // 
            // grpGeneral
            // 
            this.grpGeneral.Items.Add(this.btnGuide);
            this.grpGeneral.Items.Add(this.btnSettings);
            this.grpGeneral.Items.Add(this.btnAbout);
            this.grpGeneral.Items.Add(this.btnUpdate);
            this.grpGeneral.Label = "General";
            this.grpGeneral.Name = "grpGeneral";
            // 
            // btnGuide
            // 
            this.btnGuide.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnGuide.Label = "Guide";
            this.btnGuide.Name = "btnGuide";
            this.btnGuide.ScreenTip = "Try PlaceKit";
            this.btnGuide.ShowImage = true;
            this.btnGuide.SuperTip = "Open a sample warranty claim with the steps to try: create the placeholders, fill them in and finalize the d" +
    "ocument.";
            this.btnGuide.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnGuide_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnSettings.Label = "Settings";
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.ScreenTip = "Settings";
            this.btnSettings.ShowImage = true;
            this.btnSettings.SuperTip = "Change the placeholder markers and what happens after placeholders are created.";
            this.btnSettings.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSettings_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnAbout.Label = "About";
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.ScreenTip = "About PlaceKit";
            this.btnAbout.ShowImage = true;
            this.btnAbout.SuperTip = "Version, developer and website.";
            this.btnAbout.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAbout_Click);
            //
            // btnUpdate
            //
            this.btnUpdate.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnUpdate.Label = "Update";
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.ScreenTip = "A new version is available";
            this.btnUpdate.ShowImage = true;
            this.btnUpdate.SuperTip = "Download the new version of PlaceKit. Close Word before you run the installer; your settings and API keys stay.";
            this.btnUpdate.Visible = false;
            this.btnUpdate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnUpdate_Click);
            //
            // grpBoringJob
            // 
            this.grpBoringJob.Items.Add(this.btnHire);
            this.grpBoringJob.Label = "BoringJob";
            this.grpBoringJob.Name = "grpBoringJob";
            // 
            // btnHire
            // 
            this.btnHire.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnHire.Label = "Need a .NET Developer?";
            this.btnHire.Name = "btnHire";
            this.btnHire.ScreenTip = "C# and .NET development for your business";
            this.btnHire.ShowImage = true;
            this.btnHire.SuperTip = "PlaceKit is made by Dzmitry Kazhamiakin, a C# and .NET developer with 11 years of experience: WPF and desk" +
    "top business software, REST APIs and integrations, AI integration, Office add-ins, Excel and VBA automation, SQL" +
    " Server and reporting. Opens boringjob.work, where you can describe your problem; the initial assessment is free" +
    ".";
            this.btnHire.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnHire_Click);
            // 
            // PlaceKitRibbon
            // 
            this.Name = "PlaceKitRibbon";
            this.RibbonType = "Microsoft.Word.Document";
            this.Tabs.Add(this.tabPlaceKit);
            this.tabPlaceKit.ResumeLayout(false);
            this.tabPlaceKit.PerformLayout();
            this.grpPlaceholders.ResumeLayout(false);
            this.grpPlaceholders.PerformLayout();
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.grpBoringJob.ResumeLayout(false);
            this.grpBoringJob.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tabPlaceKit;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpPlaceholders;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnCreate;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDetect;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnFill;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnFinalize;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpGeneral;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnGuide;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSettings;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAbout;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnUpdate;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpBoringJob;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnHire;
    }

    partial class ThisRibbonCollection
    {
        internal PlaceKitRibbon PlaceKitRibbon
        {
            get { return this.GetRibbon<PlaceKitRibbon>(); }
        }
    }
}
