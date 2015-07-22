namespace SebWindowsClient
{
    partial class SebApplicationChooserForm
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
            this.listApplications = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // listApplications
            // 
            this.listApplications.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listApplications.BackColor = System.Drawing.SystemColors.ControlLight;
            this.listApplications.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listApplications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listApplications.GridLines = true;
            this.listApplications.HoverSelection = true;
            this.listApplications.Location = new System.Drawing.Point(18, 17);
            this.listApplications.MultiSelect = false;
            this.listApplications.Name = "listApplications";
            this.listApplications.Size = new System.Drawing.Size(66, 108);
            this.listApplications.TabIndex = 0;
            this.listApplications.UseCompatibleStateImageBehavior = false;
            // 
            // SebApplicationChooserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(94, 130);
            this.ControlBox = false;
            this.Controls.Add(this.listApplications);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SebApplicationChooserForm";
            this.Opacity = 0.9D;
            this.Padding = new System.Windows.Forms.Padding(18, 17, 10, 5);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TransparencyKey = System.Drawing.Color.Transparent;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listApplications;
    }
}