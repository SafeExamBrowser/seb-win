// -------------------------------------------------------------
//     Viktor tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows.Documents;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.DesktopUtils;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Xml.Serialization;
using System.IO;
using SebWindowsClient.CryptographyUtils;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
//using COM.Tools.VMDetect;
using SebWindowsClient.ServiceUtils;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualBasic.ApplicationServices;
using SebWindowsClient.XULRunnerCommunication;

//
//  SebWindowsClient.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2014 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss,
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
//  are Copyright (c) 2010-2014 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, 
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
                Logger.AddError("StartupNextInstanceEventArgs: " + es, null, null);
                if (!form.LoadFile(e.CommandLine[1]))
                {
                    Logger.AddError("LoadFile() from StartupNextInstanceEvent failed!", null, null);
                }
            }
        }

        protected override void OnCreateMainForm()
        {
            try
            {
                SebWindowsClientMain.InitSEBDesktop();
            }
            catch (Exception)
            {
                Logger.AddInformation("Unable to InitSEBDesktop");
                SEBClientInfo.SebWindowsClientForm.ExitApplication(false);
            }

            MainForm = SEBClientInfo.SebWindowsClientForm;
            if (!SEBClientInfo.SebWindowsClientForm.OpenSEBForm())
            {
                Logger.AddInformation("Unable to OpenSEBForm");
                SEBClientInfo.SebWindowsClientForm.ExitApplication(false);
            }
            SebWindowsClientMain.CloseSplash();
        }

        public void SetMainForm(Form newMainForm)
        {
            MainForm = newMainForm;
        }
    }

    static class SebWindowsClientMain
    {
        public static SingleInstanceController singleInstanceController;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumThreadWindows(int threadId, EnumThreadProc pfnEnum, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfterHwnd, IntPtr className, string windowText);

        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("User32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

        //[System.Runtime.InteropServices.DllImport("User32")]
        //private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        private const string VistaStartMenuCaption = "Start";
        private static IntPtr vistaStartMenuWnd = IntPtr.Zero;
        private delegate bool EnumThreadProc(IntPtr hwnd, IntPtr lParam);

        public static bool sessionCreateNewDesktop;

        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        private volatile static bool _loadingSebFile = false;
        public static bool clientSettingsSet { get; set; }

        public static SEBSplashScreen splash;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread] //Do not use this, it breaks the ability to switch to a new desktop
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[] arguments = Environment.GetCommandLineArgs();
            Logger.AddInformation("---------- INITIALIZING SEB - STARTING SESSION -------------");
            Logger.AddInformation(" Arguments: " + String.Join(", ", arguments));
            if (arguments.Count() == 1)
            {
                try
                {
                    if (!InitSebSettings())
                        return;
                }
                catch (Exception ex) 
                {
                    Logger.AddError("Unable to InitSebSettings",null, ex);
                    return;
                }
                var splashThread = new Thread(new ThreadStart(StartSplash));
                splashThread.Start();

                SEBClientInfo.SebWindowsClientForm = new SebWindowsClientForm();

                singleInstanceController = new SingleInstanceController();

                try
                {
                    singleInstanceController.Run(arguments);
                }
                catch (Exception)
                {
                    CloseSplash();
                }
            }
            else
            {
                try
                {
                    if (!InitSebSettings())
                        return;
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to InitSebSettings", null, ex);
                }
                SEBClientInfo.SebWindowsClientForm = new SebWindowsClientForm();
                singleInstanceController = new SingleInstanceController();
                singleInstanceController.Run(arguments);
            }
        }

        static public void StartSplash()
        {
            //Set the threads desktop to the new desktop if "Create new Desktop" is activated
            if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
                SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop);

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
        
        /// <summary>
        /// Detect if SEB Running inside VPC.
        /// </summary>
        /*private static bool IsInsideVPC()
        {
            if (VMDetect.IsInsideVPC)
            {
                //MessageBox.Show("Running inside Virtual PC!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.AddInformation("Running inside Virtual PC!", null, null);
                return true;
            }
            else
            {
                //MessageBox.Show("Not running inside Virtual PC!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.AddInformation("Not running inside Virtual PC!", null, null);
                return false;
            }
        }

        /// <summary>
        /// Detect if SEB Running inside VMWare.
        /// </summary>
        private static bool IsInsideVMWare()
        {
            if (VMDetect.IsInsideVMWare)
            {
                //MessageBox.Show("Running inside VMWare!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.AddInformation("Running inside VMWare!", null, null);
                return true;
            }
            else
            {
                //MessageBox.Show("Not running inside VMWare!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.AddInformation("Not running inside VMWare!", null, null);
                return false;
            }
        }*/


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
            //SebWindowsClientForm.SetVisibility(true);
            //SEBMessageBox.Show("Test", "Test, ob das Öffnen einer Message-Box createNewDesktop verunmöglicht.", SEBGlobalConstants.IND_MESSAGE_KIND_ERROR, MessageBoxButtons.OK);

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
                Logger.AddError("SEB client configuration set in InitSebSettings().", null, null);
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
                sessionCreateNewDesktop = (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop];
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
            //return InitSEBDesktop();
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

            //Blank the Wallpaper
            SEBDesktopWallpaper.BlankWallpaper();

            // Clean clipboard
            SEBClipboard.CleanClipboard();
            Logger.AddInformation("Clipboard cleaned.", null, null);

            //Search for permitted Applications (used in Taskswitcher (ALT-TAB) and in foreground watchdog
            SEBWindowHandler.AllowedExecutables.Clear();
            //Add the SafeExamBrowser to the allowed executables
            SEBWindowHandler.AllowedExecutables.Add("safeexambrowser");
            //Add allowed executables from all allowedProcessList
            foreach (Dictionary<string, object> process in SEBSettings.permittedProcessList)
            {
                if ((bool)process[SEBSettings.KeyActive])
                {
                    //First add the executable itself
                    SEBWindowHandler.AllowedExecutables.Add(
                        ((string)process[SEBSettings.KeyExecutable]).ToLower());
                    if (!String.IsNullOrWhiteSpace(process[SEBSettings.KeyWindowHandlingProcess].ToString()))
                    {
                        SEBWindowHandler.AllowedExecutables.Add(
                        ((string)process[SEBSettings.KeyWindowHandlingProcess]).ToLower());
                    }
                }
            }

#if DEBUG
                //Add visual studio to allowed executables for debugging
                SEBWindowHandler.AllowedExecutables.Add("devenv");
#endif

            //Process watching
            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyMonitorProcesses)[SEBSettings.KeyMonitorProcesses])
            {
                #region Foreground Window Watching (Allowed Executables)

                //This prevents the not allowed executables from poping up
                try
                {
                    SEBWindowHandler.EnableForegroundWatchDog();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to EnableForegroundWatchDog", null, ex);
                }
                #endregion

                #region Prohibited Executables watching
                //Handle prohibited executables watching
                SEBProcessHandler.ProhibitedExecutables.Clear();
                //Add prohibited executables
                foreach (Dictionary<string, object> process in SEBSettings.prohibitedProcessList)
                {
                    if ((bool)process[SEBSettings.KeyActive])
                    {
                        //First add the executable itself
                        SEBProcessHandler.ProhibitedExecutables.Add(
                            ((string)process[SEBSettings.KeyExecutable]).ToLower());
                    }
                }
                //This prevents the prohibited executables from starting up
                try
                {
                    SEBProcessHandler.EnableProcessWatchDog();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to EnableProcessWatchDog", null, ex);
                }
                #endregion
            }
            
            //Kill Explorer Shell
            // Global variable indicating if the explorer shell has been killed
            SEBClientInfo.ExplorerShellWasKilled = false;
            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyKillExplorerShell)[SEBSettings.KeyKillExplorerShell])
            {
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

            Logger.AddInformation("Successfully InitSEBDesktop");

            return true;
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
            else
            {
                //If you kill the explorer shell you don't need this!
                //SetVisibility(true);
            }
        }

        /// <summary>
        /// Hide or show the Windows taskbar and startmenu.
        /// </summary>
        /// <param name="show">true to show, false to hide</param>
        public static void SetVisibility(bool show)
        {
            // get taskbar window
            IntPtr taskBarWnd = FindWindow("Shell_TrayWnd", null);

            // try it the WinXP way first...
            IntPtr startWnd = FindWindowEx(taskBarWnd, IntPtr.Zero, "Button", "Start");

            if (startWnd == IntPtr.Zero)
            {
                // try an alternate way, as mentioned on CodeProject by Earl Waylon Flinn
                startWnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, "Start");
            }

            if (startWnd == IntPtr.Zero)
            {
                // ok, let's try the Vista easy way...
                startWnd = FindWindow("Button", null);

                if (startWnd == IntPtr.Zero)
                {
                    // no chance, we need to to it the hard way...
                    startWnd = GetVistaStartMenuWnd(taskBarWnd);
                }
            }

            ShowWindow(taskBarWnd, show ? SW_SHOW : SW_HIDE);
            ShowWindow(startWnd, show ? SW_SHOW : SW_HIDE);
        }

        /// <summary>
        /// Returns the window handle of the Vista start menu orb.
        /// </summary>
        /// <param name="taskBarWnd">windo handle of taskbar</param>
        /// <returns>window handle of start menu</returns>
        private static IntPtr GetVistaStartMenuWnd(IntPtr taskBarWnd)
        {
            // get process that owns the taskbar window
            int procId;
            GetWindowThreadProcessId(taskBarWnd, out procId);

            Process p = Process.GetProcessById(procId);
            if (p != null)
            {
                // enumerate all threads of that process...
                foreach (ProcessThread t in p.Threads)
                {
                    EnumThreadWindows(t.Id, MyEnumThreadWindowsProc, IntPtr.Zero);
                }
            }
            return vistaStartMenuWnd;
        }

        /// <summary>
        /// Callback method that is called from 'EnumThreadWindows' in 'GetVistaStartMenuWnd'.
        /// </summary>
        /// <param name="hWnd">window handle</param>
        /// <param name="lParam">parameter</param>
        /// <returns>true to continue enumeration, false to stop it</returns>
        private static bool MyEnumThreadWindowsProc(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder buffer = new StringBuilder(256);
            if (GetWindowText(hWnd, buffer, buffer.Capacity) > 0)
            {
                Console.WriteLine(buffer);
                if (buffer.ToString() == VistaStartMenuCaption)
                {
                    vistaStartMenuWnd = hWnd;
                    return false;
                }
            }
            return true;
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
