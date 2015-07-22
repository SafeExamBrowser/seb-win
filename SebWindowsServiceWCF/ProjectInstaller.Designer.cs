namespace SebWindowsServiceWCF
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SebServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.SebServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // SebServiceProcessInstaller
            // 
            this.SebServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.SebServiceProcessInstaller.Password = null;
            this.SebServiceProcessInstaller.Username = null;
            // 
            // SebServiceInstaller
            // 
            this.SebServiceInstaller.ServiceName = "SebWindowsService";
            this.SebServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
          //this.SebServiceInstaller.Committed += new System.Configuration.Install.InstallEventHandler(this.SebServiceInstaller_Committed);
          //this.SebServiceInstaller.AfterUninstall += new System.Configuration.Install.InstallEventHandler(this.SebServiceInstaller_AfterUninstall);
          //this.SebServiceInstaller.BeforeUninstall += new System.Configuration.Install.InstallEventHandler(this.SebServiceInstaller_BeforeUninstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.SebServiceProcessInstaller,
            this.SebServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller SebServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller SebServiceInstaller;
    }
}