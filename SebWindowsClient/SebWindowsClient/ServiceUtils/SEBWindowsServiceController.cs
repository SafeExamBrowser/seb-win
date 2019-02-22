// -------------------------------------------------------------
//     Viktor tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ServiceModel;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SEBWindowsServiceContracts;

namespace SebWindowsClient.ServiceUtils
{
	/// <summary>
	/// Static SebWindowsServiceHandler with singleton pattern
	/// </summary>
	public static class SebWindowsServiceHandler
    {
        private static bool _initialized = false;
        private static string _username;
        private static string _sid;
        private static IRegistryServiceContract _sebWindowsServicePipeProxy;

        private static void Initialize()
        {
            if (!_initialized)
            {
                Logger.AddInformation("initializing wcf service connection");
                var pipeFactory =
                    new ChannelFactory<IRegistryServiceContract>(
                        new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport),
                        new EndpointAddress(
                            "net.pipe://localhost/SebWindowsServiceWCF/service"));

                _sebWindowsServicePipeProxy = pipeFactory.CreateChannel();
                
                //Get the current sid or/and username - without the sid or username the registry entries cannot be set
                if (String.IsNullOrEmpty(_username))
                {
                    _username = GetCurrentUsername();
                }
                if (String.IsNullOrEmpty(_sid))
                {
                    _sid = GetCurrentUserSID();
                }

                if(string.IsNullOrEmpty(_sid) && string.IsNullOrEmpty(_username))
                    throw new Exception("Unable to get SID & Username");

                _initialized = true;
            }
        }

        private static string GetCurrentUserSID()
        {
            try
            {
                var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (windowsIdentity != null && windowsIdentity.User != null)
                {
                    return windowsIdentity.User.Value;
                }
                else
                {
                    Logger.AddWarning("Unable to get SID from WindowsIdentity", null, null);
                }
            }
            catch (Exception ex)
            {
                Logger.AddWarning("Unable to get SID from WindowsIdentity", null, ex);
            }
            return null;
        }

        private static string GetCurrentUsername()
        {
            //Get Username by Environment
            try
            {
                string username = Environment.UserName;
                if (String.IsNullOrEmpty(username))
                {
                    Logger.AddWarning("Unable to get Username from Environment", null, null);
                }
                else
                {
                    Logger.AddInformation("Username from Environment = " + username);
                    return username;
                }
            }
            catch (Exception ex)
            {
                Logger.AddWarning("Unable to get Username from Environment", null, ex);
            }

            return null;
        }

        /// <summary>
        /// Calls the windows service to set the registry values to the insideSeb Settings
        /// Throws Exception
        /// </summary>
        /// <returns>succeded or not</returns>
        public static bool SetRegistryAccordingToConfiguration()
        {
            var valuesToSet = new Dictionary<RegistryIdentifiers, object>
            {
				{RegistryIdentifiers.DisableChromeNotifications, 2 },
                {RegistryIdentifiers.DisableLockWorkstation, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableLockThisComputer )[SEBSettings.KeyInsideSebEnableLockThisComputer ] ? 0 : 1},
                {RegistryIdentifiers.DisableChangePassword, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableChangeAPassword  )[SEBSettings.KeyInsideSebEnableChangeAPassword  ] ? 0 : 1},
                {RegistryIdentifiers.DisableTaskMgr, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableStartTaskManager )[SEBSettings.KeyInsideSebEnableStartTaskManager ] ? 0 : 1},
                {RegistryIdentifiers.HideFastUserSwitching, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableSwitchUser       )[SEBSettings.KeyInsideSebEnableSwitchUser       ] ? 0 : 1},
                {RegistryIdentifiers.NoLogoff, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableLogOff           )[SEBSettings.KeyInsideSebEnableLogOff           ] ? 0 : 1},
                {RegistryIdentifiers.NoClose, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableShutDown         )[SEBSettings.KeyInsideSebEnableShutDown         ] ? 0 : 1},
				{RegistryIdentifiers.NoCloseWin7, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableShutDown         )[SEBSettings.KeyInsideSebEnableShutDown         ] ? 0 : 1},
				{RegistryIdentifiers.EnableShade, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableVmWareClientShade)[SEBSettings.KeyInsideSebEnableVmWareClientShade] ? 1 : 0},
                {RegistryIdentifiers.EnableShadeHorizon, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableVmWareClientShade)[SEBSettings.KeyInsideSebEnableVmWareClientShade] ? "True" : "False"},
                {RegistryIdentifiers.EaseOfAccess, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableEaseOfAccess     )[SEBSettings.KeyInsideSebEnableEaseOfAccess     ] ? 0 : 1},
                {RegistryIdentifiers.DontDisplayNetworkSelectionUI, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyInsideSebEnableNetworkConnectionSelector)[SEBSettings.KeyInsideSebEnableNetworkConnectionSelector] ? 0 : 1},
				{RegistryIdentifiers.NoAutoRebootWithLoggedOnUsers, 1},
                {RegistryIdentifiers.fDenyTSConnections, (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyAllowScreenSharing)[SEBSettings.KeyAllowScreenSharing] ? 0 : 1}
            };

            return SetRegistryAccordingToConfiguration(valuesToSet);
        }

        public static bool SetRegistryAccordingToConfiguration(Dictionary<RegistryIdentifiers, object> valuesToSet)
        {
            Initialize();
            return _sebWindowsServicePipeProxy.SetRegistryEntries(valuesToSet, _sid, _username);
        }

        /// <summary>
        /// Resets the registry values to what it was before
        /// Throws Exception
        /// </summary>
        /// <returns>succeded or not</returns>
        public static bool ResetRegistry()
        {
            Logger.AddInformation("resetting registry entries");
            Initialize();
            Logger.AddInformation("calling reset on wcf service");
            return _sebWindowsServicePipeProxy.Reset();
        }

        public static bool DisableWindowsUpdate()
        {
            Logger.AddInformation("calling disable windows update on wcf service");
            return _sebWindowsServicePipeProxy.DisableWindowsUpdate();   
        }

        /// <summary>
        /// Checks if the connection to the SebWindowsService is available and it the username of the current logged in user can be obtained
        /// </summary>
        /// <returns>available or not</returns>
        public static bool IsServiceAvailable
        {
            get
            {
                try
                {
                    Initialize();
                    if (_sebWindowsServicePipeProxy.TestServiceConnetcion())
                    {
                        Logger.AddInformation("SEB Windows service available", null, null);
                        return true;
                    }
                    else
                    {
                        Logger.AddInformation("SEB Windows service not available", null, null);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddInformation("SEB Windows service not available", ex, null);
                    return false;
                }
            }
        }

        /// <summary>
        /// Reconnect to the Windows Service over .NET Pipe
        /// </summary>
        /// <returns></returns>
        public static void Reconnect()
        {
            _initialized = false;
            Initialize();
        }
    }
}
