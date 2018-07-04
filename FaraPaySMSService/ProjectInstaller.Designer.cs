namespace FaraPaySMSService
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
            this.FaraPaySMSService1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.FaraPaySMSService = new System.ServiceProcess.ServiceInstaller();
            // 
            // FaraPaySMSService1
            // 
            this.FaraPaySMSService1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.FaraPaySMSService1.Password = null;
            this.FaraPaySMSService1.Username = null;
            // 
            // FaraPaySMSService
            // 
            this.FaraPaySMSService.ServiceName = "FaraPaySMSService";
            this.FaraPaySMSService.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.FaraPaySMSService1,
            this.FaraPaySMSService});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller FaraPaySMSService1;
        private System.ServiceProcess.ServiceInstaller FaraPaySMSService;
    }
}