using System;
using System.Diagnostics;
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
				Task.Run(new Action(ResetOnStartup));
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
				host?.Close();
				Reset();
			}
			catch (Exception ex)
			{
				Logger.Log(ex,"Unable to stop service host!");
			}

			Logger.Log("--- STOPPED SEB SERVICE ---" + Environment.NewLine);
		}

		private void ResetOnStartup()
		{
			const int MAX_ATTEMPTS = 10;
			const int TEN_SECONDS = 10000;
			const int THIRTY_SECONDS = 30000;

			Logger.Log($"Waiting {TEN_SECONDS / 1000.0}s before attempting to reset any changes...");
			Thread.Sleep(TEN_SECONDS);

			for (var attempt = 1; attempt < MAX_ATTEMPTS + 1; attempt++)
			{
				if (IsSebRunning())
				{
					Logger.Log("Detected new SafeExamBrowser process! Stopping reset attempts.");

					break;
				}

				var success = Reset();

				if (success)
				{
					break;
				}
				else if (attempt < MAX_ATTEMPTS)
				{
					Logger.Log($"Attempt {attempt}/{MAX_ATTEMPTS} failed, waiting {THIRTY_SECONDS / 1000.0}s before trying again...");
					Thread.Sleep(THIRTY_SECONDS);
				}
				else
				{
					Logger.Log($"All {MAX_ATTEMPTS} attempts failed.");
				}
			}
		}

		private bool Reset()
		{
			var service = new RegistryService();
			var success = service.Reset();

			Logger.Log(success ? "Attempt to reset registry was successful." : "Failed to reset all registry changes!");

			return success;
		}

		private bool IsSebRunning()
		{
			try
			{
				var processes = Process.GetProcessesByName("safeexambrowser");
				var isRunning = processes.Length != 0;

				return isRunning;
			}
			catch (Exception e)
			{
				Logger.Log(e, "Failed to check whether SEB is running - assuming it is...");

				return true;
			}
		}
	}
}
