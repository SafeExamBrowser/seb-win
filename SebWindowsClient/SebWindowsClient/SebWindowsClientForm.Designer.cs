using SebWindowsClient.BlockShortcutsUtils;
namespace SebWindowsClient
{
    partial class SebWindowsClientForm
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

            // Re-enable normal key events.
            //SebKeyCapture.FilterKeys = false;

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        public void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SebWindowsClientForm));
			this.ilProcessIcons = new System.Windows.Forms.ImageList(this.components);
			this.taskbarToolStrip = new SebWindowsClient.TaskbarToolStrip();
			this.SuspendLayout();
			// 
			// ilProcessIcons
			// 
			this.ilProcessIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilProcessIcons.ImageStream")));
			this.ilProcessIcons.TransparentColor = System.Drawing.Color.Transparent;
			this.ilProcessIcons.Images.SetKeyName(0, "AcrobatReader");
			this.ilProcessIcons.Images.SetKeyName(1, "calc");
			this.ilProcessIcons.Images.SetKeyName(2, "notepad");
			this.ilProcessIcons.Images.SetKeyName(3, "firefox");
			// 
			// taskbarToolStrip
			// 
			resources.ApplyResources(this.taskbarToolStrip, "taskbarToolStrip");
			this.taskbarToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.taskbarToolStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
			this.taskbarToolStrip.Name = "taskbarToolStrip";
			this.taskbarToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.taskbarToolStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.taskbarToolStrip_ItemClicked);
			this.taskbarToolStrip.MouseClick += new System.Windows.Forms.MouseEventHandler(this.taskbarToolStrip_MouseClick);
			// 
			// SebWindowsClientForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ControlBox = false;
			this.Controls.Add(this.taskbarToolStrip);
			this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SebWindowsClientForm";
			this.ShowIcon = false;
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			this.SizeChanged += new System.EventHandler(this.SebWindowsClientForm_SizeChanged);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ImageList ilProcessIcons;
        private TaskbarToolStrip taskbarToolStrip;
    }
}

