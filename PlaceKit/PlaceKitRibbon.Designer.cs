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
            this.btnFill = this.Factory.CreateRibbonButton();
            this.btnClear = this.Factory.CreateRibbonButton();
            this.grpGeneral = this.Factory.CreateRibbonGroup();
            this.btnSettings = this.Factory.CreateRibbonButton();
            this.btnAbout = this.Factory.CreateRibbonButton();
            this.tabPlaceKit.SuspendLayout();
            this.grpPlaceholders.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPlaceKit
            // 
            this.tabPlaceKit.Groups.Add(this.grpPlaceholders);
            this.tabPlaceKit.Groups.Add(this.grpGeneral);
            this.tabPlaceKit.Label = "PlaceKit";
            this.tabPlaceKit.Name = "tabPlaceKit";
            // 
            // grpPlaceholders
            // 
            this.grpPlaceholders.Items.Add(this.btnCreate);
            this.grpPlaceholders.Items.Add(this.btnFill);
            this.grpPlaceholders.Items.Add(this.btnClear);
            this.grpPlaceholders.Label = "Placeholders";
            this.grpPlaceholders.Name = "grpPlaceholders";
            // 
            // btnCreate
            // 
            this.btnCreate.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnCreate.Label = "Create";
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.OfficeImageId = "ContentControlText";
            this.btnCreate.ScreenTip = "Create placeholders";
            this.btnCreate.ShowImage = true;
            this.btnCreate.SuperTip = "Turn every {{text}} marker in the document into a content control that you can fi" +
    "ll in later.";
            this.btnCreate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnCreate_Click);
            // 
            // btnFill
            // 
            this.btnFill.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnFill.Label = "Fill";
            this.btnFill.Name = "btnFill";
            this.btnFill.OfficeImageId = "EditDocument";
            this.btnFill.ScreenTip = "Fill placeholders";
            this.btnFill.ShowImage = true;
            this.btnFill.SuperTip = "Open the editor to find, review and fill in all placeholders of the document in o" +
    "ne place. It opens in the side panel or in a separate window, as chosen in Settings.";
            this.btnFill.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnFill_Click);
            // 
            // btnClear
            // 
            this.btnClear.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnClear.Label = "Clear";
            this.btnClear.Name = "btnClear";
            this.btnClear.OfficeImageId = "ClearFormatting";
            this.btnClear.ScreenTip = "Clear placeholders";
            this.btnClear.ShowImage = true;
            this.btnClear.SuperTip = "Convert all placeholders back to regular text. Placeholders that were not filled " +
    "in become markers again.";
            this.btnClear.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnClear_Click);
            // 
            // grpGeneral
            // 
            this.grpGeneral.Items.Add(this.btnSettings);
            this.grpGeneral.Items.Add(this.btnAbout);
            this.grpGeneral.Label = "General";
            this.grpGeneral.Name = "grpGeneral";
            // 
            // btnSettings
            // 
            this.btnSettings.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnSettings.Label = "Settings";
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.OfficeImageId = "PropertySheet";
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
            this.btnAbout.OfficeImageId = "Info";
            this.btnAbout.ScreenTip = "About PlaceKit";
            this.btnAbout.ShowImage = true;
            this.btnAbout.SuperTip = "Version, developer and website.";
            this.btnAbout.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAbout_Click);
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
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tabPlaceKit;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpPlaceholders;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnCreate;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnFill;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnClear;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpGeneral;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSettings;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAbout;
    }

    partial class ThisRibbonCollection
    {
        internal PlaceKitRibbon PlaceKitRibbon
        {
            get { return this.GetRibbon<PlaceKitRibbon>(); }
        }
    }
}
