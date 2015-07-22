namespace SebWindowsClient
{
    partial class SebCloseDialogForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SebCloseDialogForm));
            this.txtQuitPassword = new System.Windows.Forms.MaskedTextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblQuitPassword = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtQuitPassword
            // 
            resources.ApplyResources(this.txtQuitPassword, "txtQuitPassword");
            this.txtQuitPassword.Name = "txtQuitPassword";
            this.txtQuitPassword.PasswordChar = '●';
            this.txtQuitPassword.Enter += new System.EventHandler(this.txtQuitPassword_Enter);
            this.txtQuitPassword.Leave += new System.EventHandler(this.txtQuitPassword_Leave);
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblQuitPassword
            // 
            resources.ApplyResources(this.lblQuitPassword, "lblQuitPassword");
            this.lblQuitPassword.Name = "lblQuitPassword";
            // 
            // SebCloseDialogForm
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ControlBox = false;
            this.Controls.Add(this.lblQuitPassword);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtQuitPassword);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SebCloseDialogForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblQuitPassword;
        public System.Windows.Forms.MaskedTextBox txtQuitPassword;
    }
}