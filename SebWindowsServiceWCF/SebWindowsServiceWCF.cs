using System;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using SebWindowsServiceWCF.ServiceContracts;
using SebWindowsServiceWCF.ServiceImplementations;

namespace SebWindowsServiceWCF
{
    public partial class SebWindowsServiceWCF : ServiceBase
    {
        private ServiceHost host;
        public SebWindowsServiceWCF()
        {
            InitializeComponent();

            this.ServiceName = "SebWindowsService";
        }

        private void InitializeHost()
        {
            if (host == null)
            {
                try
                {
                    host = new ServiceHost(
                    typeof(RegistryService),
                    new Uri[]
                    {
                        new Uri("net.pipe://localhost")
                    });
                    host.AddServiceEndpoint(typeof(IRegistryServiceContract),
                        new NetNamedPipeBinding(),
                        "SebWindowsService");
                }
                catch (Exception ex)
                {
                    Logger.Log(ex,"Unable to initialize service host!");
                }
                
            }
        }

        protected override void OnStart(string[] args)
        {
            InitializeHost();
            try
            {
                host.Open();

                using (var service = new RegistryService())
                {
                    Thread.Sleep(1000);
                    service.Reset();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex,"Unable to start service host!");
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (host != null)
                    host.Close();

                using (var service = new RegistryService())
                {
                    service.Reset();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex,"Unable to stop service host!");
            }
        }
    }
}
