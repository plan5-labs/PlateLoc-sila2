
namespace PlateLocCommandExecutor
{
    partial class PlateLocForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlateLocForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.NoticeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.profileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axPlateLoc1 = new AxPlateLocLib.AxPlateLoc();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axPlateLoc1)).BeginInit();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "PlateLoc";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NoticeToolStripMenuItem,
            this.profileToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(211, 104);
            // 
            // NoticeToolStripMenuItem
            // 
            this.NoticeToolStripMenuItem.Name = "NoticeToolStripMenuItem";
            this.NoticeToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.NoticeToolStripMenuItem.Text = "License";
            this.NoticeToolStripMenuItem.Click += new System.EventHandler(this.NoticeToolStripMenuItem_Click);
            // 
            // profileToolStripMenuItem
            // 
            this.profileToolStripMenuItem.Name = "profileToolStripMenuItem";
            this.profileToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.profileToolStripMenuItem.Text = "Profile Settings";
            this.profileToolStripMenuItem.Click += new System.EventHandler(this.profileToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(210, 24);
            this.ExitToolStripMenuItem.Text = "Exit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // axPlateLoc1
            // 
            this.axPlateLoc1.Enabled = true;
            this.axPlateLoc1.Location = new System.Drawing.Point(3, 4);
            this.axPlateLoc1.Margin = new System.Windows.Forms.Padding(4);
            this.axPlateLoc1.Name = "axPlateLoc1";
            this.axPlateLoc1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axPlateLoc1.OcxState")));
            this.axPlateLoc1.Size = new System.Drawing.Size(65, 56);
            this.axPlateLoc1.TabIndex = 0;
            this.axPlateLoc1.Error += new AxPlateLocLib._DPlateLocEvents_ErrorEventHandler(this.axPlateLoc1_Error);
            // 
            // PlateLocForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(197, 95);
            this.ControlBox = false;
            this.Controls.Add(this.axPlateLoc1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "PlateLocForm";
            this.ShowInTaskbar = false;
            this.Text = "PlateLoc";
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axPlateLoc1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AxPlateLocLib.AxPlateLoc axPlateLoc1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NoticeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem profileToolStripMenuItem;
    }
}

