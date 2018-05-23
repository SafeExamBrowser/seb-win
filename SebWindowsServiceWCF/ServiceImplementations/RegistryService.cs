using System;
using System.Collections.Generic;
using System.ServiceProcess;
using SEBWindowsServiceContracts;
using SebWindowsServiceWCF.RegistryHandler;
using WUApiLib;

namespace SebWindowsServiceWCF.ServiceImplementations
{
	public class RegistryService : IRegistryServiceContract, IDisposable
	{
		//Simply returns true
		public bool TestServiceConnetcion()
		{
			return true;
		}

		/// <summary>
		/// Sets the registry values
		/// </summary>
		/// <param name="registryValues">The registry values to set</param>
		/// <param name="sid">The sid of the currently logged in user - needed to identify the correct registry key path</param>
		/// <param name="username">The username of the currently logged in user - needed to identify the correct registry key path</param>
		/// <returns>true if all operations succeeded, false if something went wrong. See the logfile for details then.</returns>
		public bool SetRegistryEntries(Dictionary<RegistryIdentifiers, object> registryValues, string sid, string username)
		{
			bool res = true;

			try
			{
				Logger.Log("SID: " + (sid ?? "<NULL>"));
				Logger.Log("Username: " + (username ?? "<NULL>"));

				if (string.IsNullOrEmpty(sid))
				{
					sid = SIDHandler.GetSIDFromUsername(username);

					if (String.IsNullOrWhiteSpace(sid))
					{
						Logger.Log($"Failed to set registry entries because SID could not be determined for user '{username}''!");

						return false;
					}
					else
					{
						Logger.Log("SID from Username: " + (sid ?? "<NULL>"));
					}
				}

				using (var persistentRegistryFile = new PersistentRegistryFile(username, sid))
				{
					Logger.Log("Attempting to set new registry values...");

					foreach (var registryValue in registryValues)
					{
						RegistryEntry regEntry;
						
						//If the class could not be instantiated it means either reflection did not work properly or the registry-class does not exists
						//don't interrupt the whole process but set the return value to false to indicate a possible error
						try
						{
							//Use Reflection
							var type = Type.GetType(String.Format("SebWindowsServiceWCF.RegistryHandler.Reg{0}", registryValue.Key));
							if (type == null) continue;
							regEntry = (RegistryEntry)Activator.CreateInstance(type, sid);
						}
						catch (Exception ex)
						{
							Logger.Log(ex, String.Format("Unable to instantiate registryclass: {0}", registryValue.Key));
							res = false;
							continue;
						}

						//If the registry value could not have been set correctly or something went wrong with the persistent registry file
						//don't interrupt the whole process but set the return value to false to indicate a possible error
						//but never change a registry key without successfully write the persistent registry file
						try
						{
							//If there is nothing to change, then do not change anything
							if (object.Equals(registryValue.Value,regEntry.DataValue)) continue;

							//Only store the entry in the persistent file if not already existing
							if (!persistentRegistryFile.FileContent.RegistryValues.ContainsKey(registryValue.Key))
							{
								persistentRegistryFile.FileContent.RegistryValues.Add(registryValue.Key, regEntry.DataValue);
								//Save after every change
								persistentRegistryFile.Save();
							}
							//Change the registry value if all operations succeeded until here
							regEntry.DataValue = registryValue.Value;
							
							if (!object.Equals(regEntry.DataValue,registryValue.Value))
							{
								Logger.Log(String.Format("Registry Key {0} could not have been set to {1}",
									registryValue.Key, registryValue.Value ?? "<NULL>"));
							}
						}
						catch (Exception ex)
						{
							Logger.Log(ex, String.Format("Unable to set the registry value for {0}: {1}: {2}", registryValue.Key, ex.Message, ex.StackTrace));
							res = false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(ex, string.Format("Unable to set Registry value: {0}: {1}", ex.Message, ex.StackTrace));
				res = false;
			}
			
			return res;
		}

		/// <summary>
		/// Resets the registry values if a PersistentRegistryFile is existing
		/// </summary>
		/// <returns></returns>
		public bool Reset()
		{
			bool res = true;
			try
			{
				Logger.Log("Attempting to reset registry values...");

				using (var persistentRegistryFile = new PersistentRegistryFile())
				{
					if (persistentRegistryFile.FileContent.Username != null)
					{
						//Reset the registry values
						res = this.SetRegistryEntries(persistentRegistryFile.FileContent.RegistryValues,
							persistentRegistryFile.FileContent.SID, persistentRegistryFile.FileContent.Username);
						//Enable the windows Service if necessary
						if (persistentRegistryFile.FileContent.EnableWindowsUpdate)
							SetWindowsUpdate(true);

						if (res)
							persistentRegistryFile.Delete();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(ex, string.Format("Unable to Reset Registrysettings: {0} : {1}", ex.Message, ex.StackTrace));
				res = false;
			}
			return res;
		}

		/// <summary>
		/// Disables the automatic windows update service
		/// </summary>
		public bool DisableWindowsUpdate()
		{
			if (SetWindowsUpdate(false))
			{
				using (var persistentRegistryFile = new PersistentRegistryFile())
				{
					persistentRegistryFile.FileContent.EnableWindowsUpdate = true;
					persistentRegistryFile.Save();
				}
				return true;
			}
			return false;
		}

		private bool SetWindowsUpdate(bool enable)
		{
			if (OSVersion.FriendlyName().Contains("10"))
			{
				//Stop per Windows Service on windows 10
				ServiceController service = new ServiceController("wuauserv");
				try
				{
					TimeSpan timeout = TimeSpan.FromMilliseconds(500);
					if (enable)
					{
						service.Start();
                        //service.WaitForStatus(ServiceControllerStatus.Running, timeout);
					}
					else
					{
						service.Stop();
                        if (SetRegistryEntries(new Dictionary<RegistryIdentifiers, object>
                        {
                            {RegistryIdentifiers.NoAutoRebootWithLoggedOnUsers, 1 }
                        }, null, null))
                        {
                            new CommandExecutor.CommandExecutor().ExecuteCommandAsync("gpupdate /force");
                        }
                        //service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    }

					Logger.Log(String.Format("Set windows update to {0}", enable));

					return true;
				}
				catch (Exception ex)
				{
                    Logger.Log(string.Format("Unable to disable Windows Update: {0} : ", ex.Message, ex.StackTrace));
					return false;
				}
			}

			//Use Automatic Update Class
			try
			{
				var auc = new AutomaticUpdates();

				if (enable)
					auc.Resume();
				else
					auc.Pause();

				Logger.Log(String.Format("Set windows update to {0}", enable));

				return true;
			}
			catch (Exception ex)
			{
                Logger.Log(string.Format("Unable to disable Windows Update: {0} : ", ex.Message, ex.StackTrace));
                return false;
			}            
		}


		public void Dispose()
		{
		}
	}
}
