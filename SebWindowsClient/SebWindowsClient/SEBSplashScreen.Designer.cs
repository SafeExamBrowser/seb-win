namespace SebWindowsClient
{
    partial class SEBSplashScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SEBSplashScreen));
            this.lblLoading = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.TxtVersion = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblLoading
            // 
            this.lblLoading.BackColor = System.Drawing.Color.Transparent;
            this.lblLoading.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLoading.Location = new System.Drawing.Point(0, 302);
            this.lblLoading.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblLoading.Name = "lblLoading";
            this.lblLoading.Padding = new System.Windows.Forms.Padding(12, 0, 0, 5);
            this.lblLoading.Size = new System.Drawing.Size(594, 22);
            this.lblLoading.TabIndex = 0;
            this.lblLoading.Text = "Loading ...";
            this.lblLoading.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, -4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(594, 324);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // TxtVersion
            // 
            this.TxtVersion.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.TxtVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtVersion.BackColor = System.Drawing.Color.White;
            this.TxtVersion.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TxtVersion.CausesValidation = false;
            this.TxtVersion.Enabled = false;
            this.TxtVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtVersion.Location = new System.Drawing.Point(350, 123);
            this.TxtVersion.Multiline = true;
            this.TxtVersion.Name = "TxtVersion";
            this.TxtVersion.Size = new System.Drawing.Size(244, 18);
            this.TxtVersion.TabIndex = 2;
            // 
            // textBox2
            // 
            this.textBox2.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.BackColor = System.Drawing.Color.White;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.CausesValidation = false;
            this.textBox2.Enabled = false;
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.Location = new System.Drawing.Point(350, 146);
            this.textBox2.Margin = new System.Windows.Forms.Padding(3, 3, 13, 3);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(225, 178);
            this.textBox2.TabIndex = 3;
            this.textBox2.Text = resources.GetString("textBox2.Text");
            // 
            // SEBSplashScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(594, 324);
            this.ControlBox = false;
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.TxtVersion);
            this.Controls.Add(this.lblLoading);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimizeBox = false;
            this.Name = "SEBSplashScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox TxtVersion;
        private System.Windows.Forms.TextBox textBox2;
    }
}