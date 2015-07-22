namespace SebWindowsClient
{
    partial class WindowChooser
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
            this.appList = new System.Windows.Forms.ListView();
            this.closeListView = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // appList
            // 
            this.appList.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.appList.BackColor = System.Drawing.Color.Black;
            this.appList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.appList.Dock = System.Windows.Forms.DockStyle.Top;
            this.appList.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.appList.ForeColor = System.Drawing.Color.White;
            this.appList.GridLines = true;
            this.appList.HoverSelection = true;
            this.appList.Location = new System.Drawing.Point(6, 6);
            this.appList.Margin = new System.Windows.Forms.Padding(4);
            this.appList.MultiSelect = false;
            this.appList.Name = "appList";
            this.appList.Scrollable = false;
            this.appList.ShowGroups = false;
            this.appList.Size = new System.Drawing.Size(82, 91);
            this.appList.TabIndex = 0;
            this.appList.UseCompatibleStateImageBehavior = false;
            // 
            // closeListView
            // 
            this.closeListView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.closeListView.BackColor = System.Drawing.Color.Black;
            this.closeListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.closeListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.closeListView.ForeColor = System.Drawing.Color.White;
            this.closeListView.GridLines = true;
            this.closeListView.HoverSelection = true;
            this.closeListView.Location = new System.Drawing.Point(6, 97);
            this.closeListView.Margin = new System.Windows.Forms.Padding(4);
            this.closeListView.MultiSelect = false;
            this.closeListView.Name = "closeListView";
            this.closeListView.Scrollable = false;
            this.closeListView.ShowGroups = false;
            this.closeListView.Size = new System.Drawing.Size(82, 27);
            this.closeListView.TabIndex = 1;
            this.closeListView.UseCompatibleStateImageBehavior = false;
            // 
            // WindowChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(94, 130);
            this.Controls.Add(this.closeListView);
            this.Controls.Add(this.appList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "WindowChooser";
            this.Opacity = 0.8D;
            this.Padding = new System.Windows.Forms.Padding(6);
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "WindowChooser";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView appList;
        private System.Windows.Forms.ListView closeListView;
    }
}