using System.ComponentModel;

namespace SebWindowsServiceWCF
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {

        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
