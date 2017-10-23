namespace SebWindowsClient
{
	partial class AboutWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutWindow));
            this.LicenseInfo = new System.Windows.Forms.TextBox();
            this.Version = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // LicenseInfo
            // 
            this.LicenseInfo.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            resources.ApplyResources(this.LicenseInfo, "LicenseInfo");
            this.LicenseInfo.BackColor = System.Drawing.Color.White;
            this.LicenseInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LicenseInfo.CausesValidation = false;
            this.LicenseInfo.Name = "LicenseInfo";
            // 
            // Version
            // 
            this.Version.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            resources.ApplyResources(this.Version, "Version");
            this.Version.BackColor = System.Drawing.Color.White;
            this.Version.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Version.CausesValidation = false;
            this.Version.Name = "Version";
            // 
            // AboutWindow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::SebWindowsClient.Properties.Resources.AboutSEB;
            this.Controls.Add(this.Version);
            this.Controls.Add(this.LicenseInfo);
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutWindow";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox LicenseInfo;
		private System.Windows.Forms.TextBox Version;
	}
}