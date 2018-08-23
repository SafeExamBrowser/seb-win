using System;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using SEBWindowsServiceContracts;
using SebWindowsServiceWCF.ServiceImplementations;

namespace SebWindowsServiceWCF
{
	public partial class SebWindowsServiceWCF : ServiceBase
	{
		private ServiceHost host;

		public SebWindowsServiceWCF()
		{
			InitializeComponent();
			ServiceName = "SebWindowsService";
		}

		private void InitializeHost()
		{
			if (host == null)
			{
				Logger.Log("Initializing service host...");

				try
				{
					var address = "net.pipe://localhost/SebWindowsServiceWCF/service";
					var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);

					host = new ServiceHost(typeof(RegistryService));
					host.AddServiceEndpoint(typeof(IRegistryServiceContract), binding, address);
				}
				catch (Exception ex)
				{
					Logger.Log(ex,"Unable to initialize service host!");
				}
			}
		}

		protected override void OnStart(string[] args)
		{
			Logger.Log("--- STARTING SEB SERVICE ---");

			InitializeHost();

			try
			{
				host.Open();
				Task.Delay(2500).ContinueWith(_ => Reset());
			}
			catch (Exception ex)
			{
				Logger.Log(ex,"Unable to start service host!");
			}
		}

		protected override void OnStop()
		{
			Logger.Log("Finalizing SEB service...");

			try
			{
				if (host != null)
				{
					host.Close();
				}

				var service = new RegistryService();
				var success = service.Reset();

				Logger.Log(success ? "Attempt to reset registry was successful." : "Failed to reset registry settings!");
			}
			catch (Exception ex)
			{
				Logger.Log(ex,"Unable to stop service host!");
			}

			Logger.Log("--- STOPPED SEB SERVICE ---" + Environment.NewLine);
		}

		private void Reset()
		{
			var service = new RegistryService();

			while (!service.Reset())
			{
				Logger.Log("Resetting registry values failed! Trying again...");
				Thread.Sleep(1000);
			}

			Logger.Log("Attempt to reset registry was successful.");
		}
	}
}
