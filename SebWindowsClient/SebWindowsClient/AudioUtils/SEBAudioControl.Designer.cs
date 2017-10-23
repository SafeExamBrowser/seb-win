namespace SebWindowsClient.AudioUtils
{
    partial class SEBAudioControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SEBAudioControl));
            this.ButtonMute = new System.Windows.Forms.Button();
            this.ButtonClose = new System.Windows.Forms.Button();
            this.Trackbar = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.Trackbar)).BeginInit();
            this.SuspendLayout();
            // 
            // ButtonMute
            // 
            resources.ApplyResources(this.ButtonMute, "ButtonMute");
            this.ButtonMute.BackgroundImage = global::SebWindowsClient.Properties.Resources.audioControlmute;
            this.ButtonMute.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.ButtonMute.FlatAppearance.BorderSize = 0;
            this.ButtonMute.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
            this.ButtonMute.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            this.ButtonMute.Name = "ButtonMute";
            this.ButtonMute.UseVisualStyleBackColor = true;
            this.ButtonMute.Click += new System.EventHandler(this.ButtonMute_Click);
            // 
            // ButtonClose
            // 
            resources.ApplyResources(this.ButtonClose, "ButtonClose");
            this.ButtonClose.Name = "ButtonClose";
            this.ButtonClose.UseVisualStyleBackColor = true;
            this.ButtonClose.Click += new System.EventHandler(this.ButtonClose_Click);
            // 
            // Trackbar
            // 
            resources.ApplyResources(this.Trackbar, "Trackbar");
            this.Trackbar.Maximum = 100;
            this.Trackbar.Name = "Trackbar";
            this.Trackbar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.Trackbar.Scroll += new System.EventHandler(this.Trackbar_Scroll);
            // 
            // SEBAudioControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.ButtonClose);
            this.Controls.Add(this.ButtonMute);
            this.Controls.Add(this.Trackbar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SEBAudioControl";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.Trackbar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonMute;
        private System.Windows.Forms.Button ButtonClose;
        private System.Windows.Forms.TrackBar Trackbar;

    }
}