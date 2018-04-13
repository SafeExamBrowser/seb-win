﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DesktopUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.ProcessUtils;

//
//  SebWindowsClient.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2018 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss,
//  ETH Zurich, Educational Development and Technology (LET),
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen
//  Project concept: Thomas Piendl, Daniel R. Schneider,
//  Dirk Bauer, Kai Reuter, Tobias Halbherr, Karsten Burger, Marco Lehre,
//  Brigitte Schmucki, Oliver Rahs. French localization: Nicolas Dunand
//
//  ``The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License at
//  http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//  License for the specific language governing rights and limitations
//  under the License.
//
//  The Original Code is Safe Exam Browser for Windows.
//
//  The Initial Developers of the Original Code are Viktor Tomas, 
//  Dirk Bauer, Daniel R. Schneider, Pascal Wyss.
//  Portions created by Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss
//  are Copyright (c) 2010-2018 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, 
//  Pascal Wyss, ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

namespace SebWindowsClient
{
	public class SingleInstanceController : WindowsFormsApplicationBase
    {
        public SingleInstanceController()
        {
            IsSingleInstance = true;

            StartupNextInstance += this_StartupNextInstance;
        }

        void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
        {
            SebWindowsClientForm form = MainForm as SebWindowsClientForm; //My derived form type
            if (e.CommandLine.Count() > 1)
            {
                string es = string.Join(", ", e.CommandLine);
                Logger.AddInformation("StartupNextInstanceEventArgs: " + es);
                if (!form.LoadFile(e.CommandLine[1]))
                {
                    Logger.AddError("LoadFile() from StartupNextInstanceEvent failed!", null, null);
                }
            }
        }

        protected override void OnCreateMainForm()
        {
            MainForm = SEBClientInfo.SebWindowsClientForm;
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Count() == 1)
            {
                var splashThread = new Thread(SebWindowsClientMain.StartSplash);
                splashThread.Start();

                try
                {
                    SebWindowsClientMain.InitSEBDesktop();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to InitSEBDesktop", null, ex);
                }

                if (!SEBClientInfo.SebWindowsClientForm.OpenSEBForm())
                {
                    Logger.AddError("Unable to OpenSEBForm", null, null);
                }

                SebWindowsClientMain.CloseSplash();
            }
        }
    }

    static class SebWindowsClientMain
    {
        public static SingleInstanceController singleInstanceController;

        public static bool sessionCreateNewDesktop;

        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        private volatile static bool _loadingSebFile = false;
        public static bool clientSettingsSet { get; set; }

        public static SEBSplashScreen splash;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[] arguments = Environment.GetCommandLineArgs();
            Logger.InitLogger();
            Logger.AddInformation("---------- INITIALIZING SEB - STARTING SESSION -------------");
            Logger.AddInformation(" Arguments: " + String.Join(", ", arguments));

                try
                {
                    if (!InitSebSettings())
                        return;
                }
                catch (Exception ex) 
                {
                Logger.AddError("Unable to InitSebSettings", null, ex);
                    return;
                }
                SEBProcessHandler.LogAllRunningProcesses();
                singleInstanceController = new SingleInstanceController();

                try
                {
                SEBClientInfo.SebWindowsClientForm = new SebWindowsClientForm();
                    singleInstanceController.Run(arguments);
                }
                catch (Exception ex)
                {
                Logger.AddError(ex.Message, null, ex);
                }
            }

        public static void StartSplash()
        {
			// Set the threads desktop to the new desktop if "Create new Desktop" is activated
			if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
			{
				SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop);
			}

			// Instance a splash form given the image names
			splash = new SEBSplashScreen();
			// Run the form
			Application.Run(splash);
        }

        public static void CloseSplash()
        {
            if (splash == null)
                return;
            try
            {
                // Shut down the splash screen
                splash.Invoke(new EventHandler(splash.KillMe));
                splash.Dispose();
                splash = null;
            }
            catch (Exception)
            {}
            
        }

        /// <summary>
        /// Set loading .seb file flag.
        /// </summary>
        public static void LoadingSebFile(bool loading)
        {
            _loadingSebFile = loading;
        }


        /// <summary>
        /// Get loading .seb file flag.
        /// </summary>
        public static bool isLoadingSebFile()
        {
            return _loadingSebFile;
        }


        /// <summary>
        /// Detect if running in various virtual machines.
        /// C# code only solution which is more compatible.
        /// </summary>
        private static bool IsInsideVM()
        {
            using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            {
                using (var items = searcher.Get())
                {
                    foreach (var item in items)
                    {
                        Logger.AddInformation("Win32_ComputerSystem Manufacturer: " + item["Manufacturer"].ToString() + ", Model: " + item["Model"].ToString(), null, null);

                        string manufacturer = item["Manufacturer"].ToString().ToLower();
                        string model = item["Model"].ToString().ToLower();
                        if ((manufacturer == "microsoft corporation" && !model.Contains("surface"))
                            || manufacturer.Contains("vmware")
                            || manufacturer.Contains("parallels software") 
                            || manufacturer.Contains("xen")
                            || model.Contains("xen")
                            || model.Contains("virtualbox"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Create and initialize SEB client settings and check system compatibility.
        /// This method needs to be executed only once when SEB first starts 
        /// (not when reconfiguring).
        /// </summary>
        /// <returns>true if succeed</returns>
        /// ----------------------------------------------------------------------------------------
        public static bool InitSebSettings()
        {
            Logger.AddInformation("Attempting to InitSebSettings");

            // If loading of a .seb file isn't in progress and client settings aren't set yet
            if (_loadingSebFile == false && clientSettingsSet == false)
            {
                // Set SebClient configuration
                if (!SEBClientInfo.SetSebClientConfiguration())
                {
                    SEBMessageBox.Show(SEBUIStrings.ErrorCaption, SEBUIStrings.ErrorWhenOpeningSettingsFile, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    Logger.AddError("Error when opening the file SebClientSettings.seb!", null, null);
                    return false;
                }
                clientSettingsSet = true;
                Logger.AddInformation("SEB client configuration set in InitSebSettings().", null, null);
            }

            // Check system version
            if (!SEBClientInfo.SetSystemVersionInfo())
            {
                SEBMessageBox.Show(SEBUIStrings.ErrorCaption, SEBUIStrings.OSNotSupported, MessageBoxIcon.Error, MessageBoxButtons.OK);
                Logger.AddError("Unknown OS. Exiting SEB.", null, null);
                return false;
            }

            //on NT4/NT5 ++ a new desktop is created
            if (SEBClientInfo.IsNewOS)
            {
				sessionCreateNewDesktop = (Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop];
				if (sessionCreateNewDesktop)
				{
					SEBClientInfo.OriginalDesktop = SEBDesktopController.GetCurrent();
					SEBDesktopController OriginalInput = SEBDesktopController.OpenInputDesktop();

					SEBClientInfo.SEBNewlDesktop = SEBDesktopController.CreateDesktop(SEBClientInfo.SEB_NEW_DESKTOP_NAME);
					SEBDesktopController.Show(SEBClientInfo.SEBNewlDesktop.DesktopName);
					if (!SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop))
					{
						Logger.AddError("SetThreadDesktop failed! Looks like the thread has hooks or windows in the current desktop.", null, null);
						SEBDesktopController.Show(SEBClientInfo.OriginalDesktop.DesktopName);
						SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);
						SEBClientInfo.SEBNewlDesktop.Close();
						SEBMessageBox.Show(SEBUIStrings.createNewDesktopFailed, SEBUIStrings.createNewDesktopFailedReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
						
						return false;
					}
					SEBClientInfo.DesktopName = SEBClientInfo.SEB_NEW_DESKTOP_NAME;
				}
				else
				{
					SEBClientInfo.OriginalDesktop = SEBDesktopController.GetCurrent();
					SEBClientInfo.DesktopName = SEBClientInfo.OriginalDesktop.DesktopName;
					//If you kill the explorer shell you don't need this!
					//SebWindowsClientForm.SetVisibility(false);
				}
			}

			Logger.AddInformation("Successfully InitSebSettings");
            return true;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Create and initialize new desktop.
        /// </summary>
        /// <returns>true if succeeded</returns>
        /// ----------------------------------------------------------------------------------------
        public static bool InitSEBDesktop()
        {
            //Info: For reverting this actions see SebWindowsClientForm::CloseSEBForm()

            Logger.AddInformation("Attempting to InitSEBDesktop");

            SEBDesktopWallpaper.BlankWallpaper();

            SEBClipboard.CleanClipboard();
            Logger.AddInformation("Clipboard cleaned.", null, null);

            //Search for permitted Applications (used in Taskswitcher (ALT-TAB) and in foreground watchdog
            SEBWindowHandler.AllowedExecutables.Clear();
            //Add the SafeExamBrowser to the allowed executables
            SEBWindowHandler.AllowedExecutables.Add(new ExecutableInfo("safeexambrowser", "safeexambrowser"));
            //Add allowed executables from all allowedProcessList
            foreach (Dictionary<string, object> process in SEBSettings.permittedProcessList)
            {
                if ((bool)process[SEBSettings.KeyActive])
                {
					var processName = Path.GetFileNameWithoutExtension(((string) process[SEBSettings.KeyExecutable] ?? string.Empty).ToLower());
					var originalProcessName = Path.GetFileNameWithoutExtension(((string) process[SEBSettings.KeyOriginalName] ?? string.Empty).ToLower());

					SEBWindowHandler.AllowedExecutables.Add(new ExecutableInfo(processName, originalProcessName));

					if (!String.IsNullOrWhiteSpace(process[SEBSettings.KeyWindowHandlingProcess].ToString()))
					{
						processName = Path.GetFileNameWithoutExtension(((string) process[SEBSettings.KeyWindowHandlingProcess]).ToLower());
						SEBWindowHandler.AllowedExecutables.Add(new ExecutableInfo(processName));
					}
				}
            }

#if DEBUG
            //Add visual studio to allowed executables for debugging
            SEBWindowHandler.AllowedExecutables.Add(new ExecutableInfo("devenv"));
#endif

			if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyKillExplorerShell)[SEBSettings.KeyKillExplorerShell])
			{
				KillExplorerShell();
			}

			Logger.AddInformation("Successfully InitSEBDesktop");

			return true;
        }

        private static void KillExplorerShell()
        {
            SEBClientInfo.ExplorerShellWasKilled = false;
            //Minimize all Open Windows
            try
            {
                SEBWindowHandler.MinimizeAllOpenWindows();
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to MinimizeAllOpenWindows", null, ex);
            }
            //Kill the explorer Shell
            try
            {
                SEBClientInfo.ExplorerShellWasKilled = SEBProcessHandler.KillExplorerShell();
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to KillExplorerShell", null, ex);
            }
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Reset desktop to the default one which was active before starting SEB.
        /// </summary>
        /// <returns>true if succeed</returns>
        /// ----------------------------------------------------------------------------------------
        public static void ResetSEBDesktop()
        {
            // Switch to Default Desktop
            if (sessionCreateNewDesktop)
            {
                Logger.AddInformation("Showing Original Desktop");
                SEBDesktopController.Show(SEBClientInfo.OriginalDesktop.DesktopName);
                Logger.AddInformation("Setting original Desktop as current");
                SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);
                Logger.AddInformation("Closing New Dekstop");
                SEBClientInfo.SEBNewlDesktop.Close();
            }
        }

        public static void CheckIfTabletModeIsEnabled()
        {
            if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized))
            {
                bool? tabletMode = null;
                try
                {
                   //returns null if the key is not existing (another windows version than 10)
                   tabletMode = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "TabletMode", 1) == 1;
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to check for tablet mode, assuming its not a Windows Version with a tablet mode and if so, ignore this error", null, ex, ex.StackTrace);
                }
                if (tabletMode != null && tabletMode == false)
                {
                    SEBMessageBox.Show(SEBUIStrings.tableModeNotEnabledWarningTitle, SEBUIStrings.tableModeNotEnabledWarningText, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    Logger.AddInformation("Windows Tablet mode was not enabled, exiting seb", null, null);
                    throw new SEBNotAllowedToRunEception("SEB not running without Tablet mode...");
                }
            }
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Check if running in VM and if SEB Windows Service is running or not.
        /// </summary>
        /// <returns>true if both checks are positive, false means SEB needs to quit.</returns>
        /// ----------------------------------------------------------------------------------------
        public static void CheckIfInsideVirtualMachine()
        {
            // Test if run inside virtual machine
            bool allowVirtualMachine = (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyAllowVirtualMachine)[SEBSettings.KeyAllowVirtualMachine];
            if (IsInsideVM() && (!allowVirtualMachine))
            {
                SEBMessageBox.Show(SEBUIStrings.detectedVirtualMachine, SEBUIStrings.detectedVirtualMachineForbiddenMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                Logger.AddError("Forbidden to run SEB on a virtual machine!", null, null);
                Logger.AddInformation("Safe Exam Browser is exiting", null, null);
                throw new SEBNotAllowedToRunEception("Forbidden to run SEB on a virtual machine!");
            }
        }

        public static void CheckIfRunViaRemoteConnection()
        {
            if(System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                SEBMessageBox.Show(SEBUIStrings.detectedRemoteConnection, SEBUIStrings.detectedRemoteConnectionMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                Logger.AddError("Forbidden to run SEB via Terminal Session!", null, null);
                Logger.AddInformation("Safe Exam Browser is exiting", null, null);
                throw new SEBNotAllowedToRunEception("Forbidden to run SEB via Terminal Session!");
            }
        }

        public static void CheckServicePolicy(bool isServiceAvailable)
        {
            int forceService = (Int32)SEBClientInfo.getSebSetting(SEBSettings.KeySebServicePolicy)[SEBSettings.KeySebServicePolicy];
            switch (forceService)
            {
                case (int)sebServicePolicies.ignoreService:
                    break;
                case (int)sebServicePolicies.indicateMissingService:
                    if (!isServiceAvailable)
                    {
                        //SEBClientInfo.SebWindowsClientForm.Activate();
                        SEBMessageBox.Show(SEBUIStrings.indicateMissingService, SEBUIStrings.indicateMissingServiceReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    }
                    break;
                case (int)sebServicePolicies.forceSebService:
                    if (!isServiceAvailable)
                    {
                        //SEBClientInfo.SebWindowsClientForm.Activate();
                        SEBMessageBox.Show(SEBUIStrings.indicateMissingService, SEBUIStrings.forceSebServiceMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                        Logger.AddError("SEB Windows service is not available and sebServicePolicies is set to forceSebService", null, null);
                        Logger.AddInformation("SafeExamBrowser is exiting", null, null);
                        throw new SEBNotAllowedToRunEception("SEB Windows service is not available and sebServicePolicies is set to forceSebService");
                    }
                    break;
            }
        }


        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Move SEB to the foreground.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public static void SEBToForeground()
        {
            //if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyShowTaskBar))
            //{
            try
            {
                if (SEBClientInfo.SebWindowsClientForm.InvokeRequired)
                {
                    SEBClientInfo.SebWindowsClientForm.Invoke((MethodInvoker)delegate { SEBToForeground(); });
                    return;
                }
                // this code will run on main (UI) thread 

                //SetForegroundWindow(SEBClientInfo.SebWindowsClientForm.Handle);
                SebApplicationChooserForm.forceSetForegroundWindow(SEBClientInfo.SebWindowsClientForm.Handle);
                SEBClientInfo.SebWindowsClientForm.Activate();
            }
            catch (Exception)
            {
            }
            
            //}
        }
    }
}
