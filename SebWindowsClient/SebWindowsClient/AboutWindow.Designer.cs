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
			this.LicenseInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.LicenseInfo.BackColor = System.Drawing.Color.White;
			this.LicenseInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.LicenseInfo.CausesValidation = false;
			this.LicenseInfo.Enabled = false;
			this.LicenseInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LicenseInfo.Location = new System.Drawing.Point(327, 138);
			this.LicenseInfo.Margin = new System.Windows.Forms.Padding(3, 3, 13, 3);
			this.LicenseInfo.Multiline = true;
			this.LicenseInfo.Name = "LicenseInfo";
			this.LicenseInfo.Size = new System.Drawing.Size(225, 168);
			this.LicenseInfo.TabIndex = 4;
			this.LicenseInfo.Text = resources.GetString("LicenseInfo.Text");
			// 
			// Version
			// 
			this.Version.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.Version.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Version.BackColor = System.Drawing.Color.White;
			this.Version.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.Version.CausesValidation = false;
			this.Version.Enabled = false;
			this.Version.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Version.Location = new System.Drawing.Point(327, 114);
			this.Version.Multiline = true;
			this.Version.Name = "Version";
			this.Version.Size = new System.Drawing.Size(244, 18);
			this.Version.TabIndex = 5;
			// 
			// AboutWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.BackgroundImage = global::SebWindowsClient.Properties.Resources.AboutSEB;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.ClientSize = new System.Drawing.Size(574, 318);
			this.Controls.Add(this.Version);
			this.Controls.Add(this.LicenseInfo);
			this.DoubleBuffered = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AboutWindow";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "About Safe Exam Browser";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox LicenseInfo;
		private System.Windows.Forms.TextBox Version;
	}
}