namespace WinConvertMerge
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
            this.lvwRecorders = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnulvwRecorders = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.propRecorder = new System.Windows.Forms.PropertyGrid();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadVariablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnulvwRecorders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvwRecorders
            // 
            this.lvwRecorders.AllowDrop = true;
            this.lvwRecorders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.lvwRecorders.ContextMenuStrip = this.mnulvwRecorders;
            this.lvwRecorders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwRecorders.FullRowSelect = true;
            this.lvwRecorders.HideSelection = false;
            this.lvwRecorders.Location = new System.Drawing.Point(0, 0);
            this.lvwRecorders.Name = "lvwRecorders";
            this.lvwRecorders.Size = new System.Drawing.Size(305, 246);
            this.lvwRecorders.TabIndex = 0;
            this.lvwRecorders.UseCompatibleStateImageBehavior = false;
            this.lvwRecorders.View = System.Windows.Forms.View.Details;
            this.lvwRecorders.SelectedIndexChanged += new System.EventHandler(this.lvwRecorders_SelectedIndexChanged);
            this.lvwRecorders.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvwRecorders_KeyDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Filename";
            this.columnHeader1.Width = 157;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "File type";
            this.columnHeader2.Width = 88;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Start time";
            // 
            // mnulvwRecorders
            // 
            this.mnulvwRecorders.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadVariablesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.removeUnknownToolStripMenuItem,
            this.removeSelectedToolStripMenuItem,
            this.removeallToolStripMenuItem});
            this.mnulvwRecorders.Name = "mnulvwRecorders";
            this.mnulvwRecorders.Size = new System.Drawing.Size(171, 120);
            // 
            // removeUnknownToolStripMenuItem
            // 
            this.removeUnknownToolStripMenuItem.Name = "removeUnknownToolStripMenuItem";
            this.removeUnknownToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.removeUnknownToolStripMenuItem.Text = "Remove &unknown";
            this.removeUnknownToolStripMenuItem.Click += new System.EventHandler(this.removeUnknownToolStripMenuItem_Click);
            // 
            // removeSelectedToolStripMenuItem
            // 
            this.removeSelectedToolStripMenuItem.Name = "removeSelectedToolStripMenuItem";
            this.removeSelectedToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.removeSelectedToolStripMenuItem.Text = "Remove selected";
            this.removeSelectedToolStripMenuItem.Click += new System.EventHandler(this.removeSelectedToolStripMenuItem_Click);
            // 
            // removeallToolStripMenuItem
            // 
            this.removeallToolStripMenuItem.Name = "removeallToolStripMenuItem";
            this.removeallToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.removeallToolStripMenuItem.Text = "Remove &all";
            this.removeallToolStripMenuItem.Click += new System.EventHandler(this.removeallToolStripMenuItem_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(187, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Drag and drop the recorder files below";
            // 
            // propRecorder
            // 
            this.propRecorder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propRecorder.Location = new System.Drawing.Point(0, 0);
            this.propRecorder.Name = "propRecorder";
            this.propRecorder.Size = new System.Drawing.Size(217, 246);
            this.propRecorder.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(15, 59);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lvwRecorders);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propRecorder);
            this.splitContainer1.Size = new System.Drawing.Size(526, 246);
            this.splitContainer1.SplitterDistance = 305;
            this.splitContainer1.TabIndex = 3;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(167, 6);
            // 
            // loadVariablesToolStripMenuItem
            // 
            this.loadVariablesToolStripMenuItem.Name = "loadVariablesToolStripMenuItem";
            this.loadVariablesToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.loadVariablesToolStripMenuItem.Text = "Load variables";
            this.loadVariablesToolStripMenuItem.Click += new System.EventHandler(this.loadVariablesToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(553, 317);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "WinRecorders";
            this.mnulvwRecorders.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lvwRecorders;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ContextMenuStrip mnulvwRecorders;
        private System.Windows.Forms.ToolStripMenuItem removeUnknownToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeallToolStripMenuItem;
        private System.Windows.Forms.PropertyGrid propRecorder;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem loadVariablesToolStripMenuItem;
    }
}

