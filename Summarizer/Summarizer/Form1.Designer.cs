namespace Summarizer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.imageList32 = new System.Windows.Forms.ImageList(this.components);
            this.wizardControl1 = new DevExpress.XtraWizard.WizardControl();
            this.wizardPage1 = new DevExpress.XtraWizard.WizardPage();
            this.lvwFiles = new System.Windows.Forms.ListView();
            this.colFilename = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDirectory = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList16 = new System.Windows.Forms.ImageList(this.components);
            this.completionWizardPage1 = new DevExpress.XtraWizard.CompletionWizardPage();
            this.wizardPage2 = new DevExpress.XtraWizard.WizardPage();
            this.progressBarControl1 = new DevExpress.XtraEditors.ProgressBarControl();
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.lvwWorksheets = new System.Windows.Forms.ListView();
            this.colTargetWorksheet = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSourceWorksheet = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            ((System.ComponentModel.ISupportInitialize)(this.wizardControl1)).BeginInit();
            this.wizardControl1.SuspendLayout();
            this.wizardPage1.SuspendLayout();
            this.wizardPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // imageList32
            // 
            this.imageList32.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList32.ImageStream")));
            this.imageList32.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList32.Images.SetKeyName(0, "excel32");
            // 
            // wizardControl1
            // 
            this.wizardControl1.Controls.Add(this.wizardPage1);
            this.wizardControl1.Controls.Add(this.completionWizardPage1);
            this.wizardControl1.Controls.Add(this.wizardPage2);
            this.wizardControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardControl1.Location = new System.Drawing.Point(0, 0);
            this.wizardControl1.LookAndFeel.SkinName = "Office 2013";
            this.wizardControl1.Name = "wizardControl1";
            this.wizardControl1.Pages.AddRange(new DevExpress.XtraWizard.BaseWizardPage[] {
            this.wizardPage1,
            this.wizardPage2,
            this.completionWizardPage1});
            this.wizardControl1.Size = new System.Drawing.Size(535, 341);
            this.wizardControl1.Text = "Merge worksheets content from many workbooks";
            this.wizardControl1.TitleImage = ((System.Drawing.Image)(resources.GetObject("wizardControl1.TitleImage")));
            this.wizardControl1.WizardStyle = DevExpress.XtraWizard.WizardStyle.WizardAero;
            // 
            // wizardPage1
            // 
            this.wizardPage1.Controls.Add(this.lvwFiles);
            this.wizardPage1.Name = "wizardPage1";
            this.wizardPage1.Size = new System.Drawing.Size(475, 179);
            this.wizardPage1.Text = "Drag and drop the files you want to merge";
            this.wizardPage1.PageValidating += new DevExpress.XtraWizard.WizardPageValidatingEventHandler(this.wizardPage1_PageValidating);
            // 
            // lvwFiles
            // 
            this.lvwFiles.CheckBoxes = true;
            this.lvwFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFilename,
            this.colDirectory});
            this.lvwFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwFiles.Location = new System.Drawing.Point(0, 0);
            this.lvwFiles.Name = "lvwFiles";
            this.lvwFiles.Size = new System.Drawing.Size(475, 179);
            this.lvwFiles.SmallImageList = this.imageList16;
            this.lvwFiles.TabIndex = 2;
            this.lvwFiles.UseCompatibleStateImageBehavior = false;
            this.lvwFiles.View = System.Windows.Forms.View.Details;
            // 
            // colFilename
            // 
            this.colFilename.Text = "Filename";
            this.colFilename.Width = 173;
            // 
            // colDirectory
            // 
            this.colDirectory.Text = "Directory";
            // 
            // imageList16
            // 
            this.imageList16.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList16.ImageStream")));
            this.imageList16.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList16.Images.SetKeyName(0, "worksheet");
            this.imageList16.Images.SetKeyName(1, "excel2013");
            // 
            // completionWizardPage1
            // 
            this.completionWizardPage1.Name = "completionWizardPage1";
            this.completionWizardPage1.Size = new System.Drawing.Size(475, 179);
            // 
            // wizardPage2
            // 
            this.wizardPage2.Controls.Add(this.progressBarControl1);
            this.wizardPage2.Controls.Add(this.simpleButton1);
            this.wizardPage2.Controls.Add(this.lvwWorksheets);
            this.wizardPage2.Name = "wizardPage2";
            this.wizardPage2.Size = new System.Drawing.Size(475, 179);
            this.wizardPage2.Text = "Set the source and target worksheets";
            // 
            // progressBarControl1
            // 
            this.progressBarControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarControl1.Location = new System.Drawing.Point(345, 63);
            this.progressBarControl1.Name = "progressBarControl1";
            this.progressBarControl1.Properties.ShowTitle = true;
            this.progressBarControl1.Size = new System.Drawing.Size(103, 18);
            this.progressBarControl1.TabIndex = 13;
            this.progressBarControl1.Visible = false;
            // 
            // simpleButton1
            // 
            this.simpleButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.simpleButton1.Location = new System.Drawing.Point(345, 17);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.Size = new System.Drawing.Size(103, 40);
            this.simpleButton1.TabIndex = 5;
            this.simpleButton1.Text = "Load from\r\nworkbooks...";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click);
            // 
            // lvwWorksheets
            // 
            this.lvwWorksheets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvwWorksheets.CheckBoxes = true;
            this.lvwWorksheets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTargetWorksheet,
            this.colSourceWorksheet});
            this.lvwWorksheets.HideSelection = false;
            this.lvwWorksheets.LabelEdit = true;
            this.lvwWorksheets.Location = new System.Drawing.Point(0, 0);
            this.lvwWorksheets.Name = "lvwWorksheets";
            this.lvwWorksheets.Size = new System.Drawing.Size(320, 179);
            this.lvwWorksheets.SmallImageList = this.imageList16;
            this.lvwWorksheets.TabIndex = 3;
            this.lvwWorksheets.UseCompatibleStateImageBehavior = false;
            this.lvwWorksheets.View = System.Windows.Forms.View.Details;
            this.lvwWorksheets.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvwWorksheets_ColumnClick);
            // 
            // colTargetWorksheet
            // 
            this.colTargetWorksheet.Text = "Target worksheet";
            this.colTargetWorksheet.Width = 117;
            // 
            // colSourceWorksheet
            // 
            this.colSourceWorksheet.Text = "Source worksheet";
            this.colSourceWorksheet.Width = 182;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 341);
            this.Controls.Add(this.wizardControl1);
            this.Name = "Form1";
            this.Text = "Workbook merger";
            ((System.ComponentModel.ISupportInitialize)(this.wizardControl1)).EndInit();
            this.wizardControl1.ResumeLayout(false);
            this.wizardPage1.ResumeLayout(false);
            this.wizardPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList imageList32;
        private DevExpress.XtraWizard.WizardControl wizardControl1;
        private DevExpress.XtraWizard.WizardPage wizardPage1;
        private DevExpress.XtraWizard.CompletionWizardPage completionWizardPage1;
        private System.Windows.Forms.ListView lvwFiles;
        private System.Windows.Forms.ColumnHeader colFilename;
        private System.Windows.Forms.ColumnHeader colDirectory;
        private DevExpress.XtraWizard.WizardPage wizardPage2;
        private System.Windows.Forms.ListView lvwWorksheets;
        private System.Windows.Forms.ColumnHeader colTargetWorksheet;
        private System.Windows.Forms.ImageList imageList16;
        private System.Windows.Forms.ColumnHeader colSourceWorksheet;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControl1;
    }
}

