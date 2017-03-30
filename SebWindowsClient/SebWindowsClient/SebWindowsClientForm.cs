//
//  SEBWindowsClientForm.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2017 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss,
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
//  are Copyright (c) 2010-2017 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, 
//  Pascal Wyss, ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.DesktopUtils;
using System.Net;
using System.IO;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.BlockShortcutsUtils;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SebWindowsClient.ServiceUtils;
using SebWindowsClient.UI;
using SebWindowsClient.XULRunnerCommunication;
using DictObj = System.Collections.Generic.Dictionary<string, object>;


namespace SebWindowsClient
{
    

    public partial class SebWindowsClientForm : Form
    {
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
        private static extern System.IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfterHwnd, IntPtr className, string windowText);

        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImportAttribute("User32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        private const string VistaStartMenuCaption = "Start";
        private static IntPtr vistaStartMenuWnd = IntPtr.Zero;
        private static IntPtr processWindowHandle = IntPtr.Zero;
        private delegate bool EnumThreadProc(IntPtr hwnd, IntPtr lParam);

        private int taskbarHeight = 0;

        //private SebApplicationChooserForm SebApplicationChooser = null;

        public bool closeSebClient = true;
        public string sebPassword = null;

        private SebCloseDialogForm sebCloseDialogForm;
        private SebApplicationChooserForm sebApplicationChooserForm;

        public Process xulRunner = new Process();
        private int xulRunnerExitCode;
        private DateTime xulRunnerExitTime;
        //private bool xulRunnerExitEventHandled;

        public List<string> permittedProcessesCalls = new List<string>();
        public List<Process> permittedProcessesReferences = new List<Process>();
        public List<Image> permittedProcessesIconImages = new List<Image>();

        private List<Process> runningProcessesToClose = new List<Process>();
        private List<string> runningApplicationsToClose = new List<string>();


        //public OpenFileDialog openFileDialog;

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor - initialise components.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public SebWindowsClientForm()
        {
            InitializeComponent();

            SEBXULRunnerWebSocketServer.OnXulRunnerCloseRequested += OnXULRunnerShutdDownRequested;
            SEBXULRunnerWebSocketServer.OnXulRunnerQuitLinkClicked += OnXulRunnerQuitLinkPressed;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (x, y) => PlaceFormOnDesktop(TapTipHandler.IsKeyboardVisible());

            try
            {
                SEBProcessHandler.PreventSleep();
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to PreventSleep", null, ex);
            }
        }

        private void OnXULRunnerShutdDownRequested(object sender, EventArgs e)
        {
            if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyAllowQuit])
            {
                Logger.AddInformation("Receiving Shutdown Request and opening ShowCloseDialogForm");
                this.BeginInvoke(new Action(this.ShowCloseDialogForm));
            }
        }

        private void OnXulRunnerQuitLinkPressed(object sender, EventArgs e)
        {
            Logger.AddInformation("Receiving Quit Link pressed and opening ShowCloseDialogForm");
            this.BeginInvoke(new Action(this.ShowCloseDialogFormConfirmation));
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// OnLoad: Get the file name from command line arguments and load it.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string[] args = Environment.GetCommandLineArgs();

            string es = string.Join(", ", args);
            Logger.AddInformation("OnLoad EventArgs: " + es, null, null);

            if (args.Length > 1)
            {
                if (!LoadFile(args[1])) {
                    Logger.AddError("LoadFile() in OnLoad() failed, exiting SEB!", null, null);
                    ExitApplication();
                }
            }
        }
        public bool LoadFile(string file)
        {
            Logger.AddInformation("Attempting to read new configuration file");
            if (!SebWindowsClientMain.isLoadingSebFile())
            {
                SebWindowsClientMain.LoadingSebFile(true);
                // Check if client settings were already set
                if (SebWindowsClientMain.clientSettingsSet == false)
                {
                    // We need to set the client settings first
                    if (SEBClientInfo.SetSebClientConfiguration())
                    {
                        SebWindowsClientMain.clientSettingsSet = true;
                        Logger.AddError("SEB client configuration set in LoadFile(URI).", null, null);
                    }
                }
                byte[] sebSettings = null;
                Uri uri;
                try
                {
                    uri = new Uri(file);
                }
                catch (Exception ex)
                {
                    Logger.AddError("SEB was opened with a wrong URI parameter", this, ex, ex.Message);
                    SebWindowsClientMain.LoadingSebFile(false);
                    return false;
                }
                // Check if we're running in exam mode already, if yes, then refuse to load a .seb file
                if (SEBClientInfo.examMode)
                {
                    //SEBClientInfo.SebWindowsClientForm.Activate();
                    SEBMessageBox.Show(SEBUIStrings.loadingSettingsNotAllowed, SEBUIStrings.loadingSettingsNotAllowedReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    SebWindowsClientMain.LoadingSebFile(false);
                    return false;
                }

                if (uri.Scheme == "seb" || uri.Scheme == "sebs")
                // The URI is holding a seb:// or sebs:// (secure) web address for a .seb settings file: download it
                {
                    // But only download and use the seb:// link to a .seb file if this is enabled
                    if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyDownloadAndOpenSebConfig))
                    {
                        try
                        {
                            WebClient myWebClient = new WebClient();

                            if (uri.Scheme == "seb")
                            {
                                // Try first by http
                                Logger.AddError("Trying to download .seb settings by http", null, null);
                                UriBuilder httpURL = new UriBuilder("http", uri.Host, uri.Port, uri.AbsolutePath);
                                using (myWebClient)
                                {
                                    sebSettings = myWebClient.DownloadData(httpURL.Uri);
                                }
                                if (sebSettings == null)
                                {
                                    Logger.AddError("Downloading .seb settings by http failed, try to download by https", null, null);
                                }
                            }
                            if (sebSettings == null)
                            {
                                // Download by https
                                Logger.AddError("Downloading .seb settings by https", null, null);
                                UriBuilder httpsURL = new UriBuilder("https", uri.Host, uri.Port, uri.AbsolutePath);
                                using (myWebClient)
                                {
                                    sebSettings = myWebClient.DownloadData(httpsURL.Uri);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SEBMessageBox.Show(SEBUIStrings.cannotOpenSEBLink, SEBUIStrings.cannotOpenSEBLinkMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                            //MessageBox.Show(new Form() { TopMost = true }, "Unable to follow the link!");
                            Logger.AddError("Unable to download a file from the "+ file + " link", this, ex);
                        }
                    }                    
                }
                else if (uri.IsFile)
                {
                    try
                    {
                        sebSettings = File.ReadAllBytes(file);
                    }
                    catch (Exception streamReadException)
                    {
                        // Write error into string with temporary log string builder
                        Logger.AddError("Settings could not be read from file.", this, streamReadException, streamReadException.Message);
                        SebWindowsClientMain.LoadingSebFile(false);
                        return false;
                    }
                }
                // If some settings got loaded in the end
                if (sebSettings == null)
                {
                    Logger.AddError("Loaded settings were empty.", this, null, null);
                    SebWindowsClientMain.LoadingSebFile(false);
                    return false;
                }
                Logger.AddInformation("Succesfully read the new configuration, length is " + sebSettings.Length);
                // Decrypt, parse and store new settings and restart SEB if this was successfull
                Logger.AddInformation("Attempting to StoreDecryptedSEBSettings");

                if (!SEBConfigFileManager.StoreDecryptedSEBSettings(sebSettings))
                {
                    Logger.AddInformation("StoreDecryptedSettings returned false, this means the user canceled when entering the password, didn't enter a right one after 5 attempts or new settings were corrupted, exiting");
                    Logger.AddError("Settings could not be decrypted or stored.", this, null, null);
                    SebWindowsClientMain.LoadingSebFile(false);
                    return false;
                }

                //Show splashscreen
                var splashThread = new Thread(SEBSplashScreen.StartSplash);


                if (!SEBXULRunnerWebSocketServer.Started)
                {
                    Logger.AddInformation("SEBXULRunnerWebSocketServer.Started returned false, this means the WebSocketServer communicating with the SEB XULRunner browser couldn't be started, exiting");
                    SEBMessageBox.Show(SEBUIStrings.webSocketServerNotStarted, SEBUIStrings.webSocketServerNotStartedMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    ExitApplication();
                    return false;
                }
                SEBSplashScreen.CloseSplash();

                Logger.AddInformation("Successfully StoreDecryptedSEBSettings");
                SebWindowsClientMain.LoadingSebFile(false);

                return true;
            }
            return false;
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
                SetForegroundWindow(SEBClientInfo.SebWindowsClientForm.Handle);
                SEBClientInfo.SebWindowsClientForm.Activate();
            }
            catch (Exception)
            {
            }
                
            //}
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        // Start xulRunner process.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private bool StartXulRunner(string userDefinedArguments)
        {
            //xulRunnerExitEventHandled = false;
            string xulRunnerPath = "";
            string desktopName = "";
            if (userDefinedArguments == null) userDefinedArguments="";
            try
            {
                // Create JSON object with XULRunner parameters to pass to xulrunner.exe as base64 string
                DictObj currentSettings = SEBSettings.settingsCurrent;
                var xulRunnerSettings = currentSettings.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);
                string XULRunnerParameters = SEBXulRunnerSettings.XULRunnerConfigDictionarySerialize(xulRunnerSettings);
                // Create the path to xulrunner.exe plus all arguments
                StringBuilder xulRunnerPathBuilder = new StringBuilder(SEBClientInfo.XulRunnerExePath);
                // Create all arguments, including user defined
                StringBuilder xulRunnerArgumentsBuilder = new StringBuilder(" -app \"").Append(Application.StartupPath).Append("\\").Append(SEBClientInfo.XulRunnerSebIniPath).Append("\"");
                // Check if there is a user defined -profile parameter, otherwise use the standard one 
                if (!(userDefinedArguments.ToLower()).Contains("-profile"))
                {
                    xulRunnerArgumentsBuilder.Append(" -profile \"").Append(SEBClientInfo.SebClientSettingsAppDataDirectory).Append("Profiles\"");
                }
                // If logging is enabled in settings and there is no custom xulrunner -logfile argument 
                if (!userDefinedArguments.ToLower().Contains("-logfile") && (bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableLogging))
                {
                    string logDirectory = (string)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyLogDirectoryWin);
                    if (String.IsNullOrEmpty(logDirectory))
                    {
                        // When there is no directory indicated, we use the placeholder for telling xulrunner to use the AppData directory to store the log
                        xulRunnerArgumentsBuilder.Append(" -logfile \"").Append(SEBClientInfo.SebClientSettingsAppDataDirectory).Append("\\seb.log\"");
                    }
                    else
                    {
                        logDirectory = Environment.ExpandEnvironmentVariables(logDirectory);
                        xulRunnerArgumentsBuilder.Append(" -logfile \"").Append(logDirectory).Append("\\seb.log\"");
                    }

                    if (!(userDefinedArguments.ToLower()).Contains("-debug"))
                    {
                        xulRunnerArgumentsBuilder.Append(" -debug 1");
                    }
                }
                xulRunnerArgumentsBuilder.Append(" ").Append(Environment.ExpandEnvironmentVariables(userDefinedArguments)).Append(" –purgecaches -ctrl \"").Append(XULRunnerParameters).Append("\"");
                string xulRunnerArguments = xulRunnerArgumentsBuilder.ToString();
                xulRunnerPathBuilder.Append(xulRunnerArguments);
                xulRunnerPath = xulRunnerPathBuilder.ToString();
                //Logger.AddError("Starting XULRunner with call: " + xulRunnerPath, this, null);

                desktopName = SEBClientInfo.DesktopName;
                xulRunner = SEBDesktopController.CreateProcess(xulRunnerPath, desktopName);
                xulRunner.EnableRaisingEvents = true;
                //if(!SEBXULRunnerWebSocketServer.IsXULRunnerConnected)
                xulRunner.Exited += xulRunner_Exited;

                //Maximize the Windows of the XulRunner if touch mode is enabled
                //if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] == true)
                //{
                //    while (!xulRunner.GetOpenWindows().Any())
                //    {
                //        Thread.Sleep(1000);
                //    }
                //    foreach (var oW in xulRunner.GetOpenWindows())
                //    {
                //        oW.Key.MaximizeWindow();
                //    }
                //}

                return true;

            }
            catch (Exception ex)
            {
                Logger.AddError("An error occurred starting XULRunner, path: "+xulRunnerPath+" desktop name: "+desktopName+" ", this, ex, ex.Message);
                return false;
            }
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Handle xulRunner_Exited event and display process information.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private void xulRunner_Exited(object sender, System.EventArgs e)
        {
            Logger.AddInformation("XULRunner exit event fired.", this, null);

            //xulRunnerExitEventHandled = true;
            // Is the handle for the XULRunner process valid?
            if (xulRunner != null)
            {
                try
                {
                    // Read the exit code.
                    xulRunnerExitCode = xulRunner.ExitCode;
                    xulRunnerExitTime = xulRunner.ExitTime;
                }
                catch (Exception ex)
                {
                    xulRunnerExitCode = -1;
                    // An error occured when reading exit code, probably XULRunner didn't actually exit yet
                    Logger.AddError("Error reading XULRunner exit code!", this, ex);
                }
            }
            else
            {
                // The XULRunner process didn't exist anymore
                xulRunnerExitCode = 0;
               // xulRunnerExitTime = ;
            }
            if (xulRunnerExitCode != 0)
            {
                // An error occured when exiting XULRunner, maybe it crashed?
                Logger.AddWarning("XulRunner exited with Exit Code. Exit code: " + xulRunnerExitCode.ToString(), this, null);
            }
            else
            {
                // If the flag for closing SEB is set, we exit
                if (SEBClientInfo.SebWindowsClientForm.closeSebClient)
                {
                    Logger.AddError("XULRunner was closed, SEB will exit now.", this, null);
                    ExitApplication();
                }
            }

        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Add permitted process names and icons to the SEB task bar (ToolStrip control) 
        /// and start permitted processes which have the autostart option set 
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private void addPermittedProcessesToTS()
        {
            // First clear the permitted processes toolstrip/lists in case of a SEB restart
            taskbarToolStrip.Items.Clear();
            permittedProcessesCalls.Clear();
            permittedProcessesReferences.Clear();
            permittedProcessesIconImages.Clear();

            List<object> permittedProcessList = (List<object>)SEBClientInfo.getSebSetting(SEBSettings.KeyPermittedProcesses)[SEBSettings.KeyPermittedProcesses];
            if (permittedProcessList.Count > 0)
            {
                // Check if permitted third party applications are already running
                List<Process> runningApplications = new List<Process>();
                runningApplications = Process.GetProcesses().ToList();
                //Process[] runningApplications = Process.GetProcesses();
                for (int i = 0; i < permittedProcessList.Count; i++)
                {
                    Dictionary<string, object> permittedProcess = (Dictionary<string, object>)permittedProcessList[i];
                    
                    //Do not kill permitted processses that are set to run in background
                    if ((bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyRunInBackground))
                        continue;
                    
                    SEBSettings.operatingSystems permittedProcessOS = (SEBSettings.operatingSystems)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyOS);
                    bool permittedProcessActive = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyActive);
                    if (permittedProcessOS == SEBSettings.operatingSystems.operatingSystemWin && permittedProcessActive)
                    {
                        string title = ((string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyTitle));
                        string executable = ((string)permittedProcess[SEBSettings.KeyExecutable]).ToLower();
                        if (String.IsNullOrEmpty(title)) title = executable;
                        string identifier = ((string)permittedProcess[SEBSettings.KeyIdentifier]).ToLower();
                        if (!(executable.Contains(SEBClientInfo.XUL_RUNNER) && !(bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser)))
                        {
                            // Check if the process is already running
                            //runningApplications = Process.GetProcesses();
                            int j = 0;
                            while (j < runningApplications.Count())
                            {
                                try
                                {
                                    // Get the name of the running process. This might fail if the process has terminated in between, we have to catch this case
                                    string name = runningApplications[j].ProcessName;
                                    if (executable.Contains(name.ToLower()))
                                    {
                                        //Define the running process
                                        var proc = runningApplications[j];
                                        //If it has another process handling the windows
                                        if (proc != null && !proc.HasExited && proc.MainWindowHandle == IntPtr.Zero)
                                        {
                                            //Get Process from WindowHandle by Name
                                            proc = SEBWindowHandler.GetWindowHandleByTitle(identifier).GetProcess();
                                        }

                                        // If the flag strongKill is set, then the process is killed without asking the user
                                        bool strongKill = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyStrongKill);
                                        if (strongKill)
                                        {
                                            Logger.AddError("Closing already running permitted process with strongKill flag set: " + name, null, null);

                                            SEBNotAllowedProcessController.CloseProcess(proc);
                                            // Remove the process from the list of running processes
                                            runningApplications.RemoveAt(j);
                                        }
                                        else
                                        {
                                            runningProcessesToClose.Add(proc);
                                            runningApplicationsToClose.Add(title == "SEB" ? (string)permittedProcess[SEBSettings.KeyExecutable] : title);
                                            //runningApplicationsToClose.Add((title == "SEB" ? "" : (title == "" ? "" : title + " - ")) + executable);
                                            j++;
                                        }
                                    }
                                    else
                                    {
                                        j++;
                                    }
                                }
                                catch (Exception)
                                {
                                    // Running process has been terminated in the meantime, so we remove it from the list
                                    runningApplications.RemoveAt(j);
                                }
                            }
                        }
                    }
                }
            }

            // If we found already running permitted or if there were prohibited processes on the list, 
            // we ask the user how to quit them
            if (runningProcessesToClose.Count > 0)
            {
                StringBuilder applicationsListToClose = new StringBuilder();
                foreach (string applicationToClose in runningApplicationsToClose)
                {
                    applicationsListToClose.AppendLine("    " + applicationToClose);
                }
                if (SEBMessageBox.Show(SEBUIStrings.closeProcesses, SEBUIStrings.closeProcessesQuestion + "\n\n" + applicationsListToClose.ToString(), MessageBoxIcon.Error, MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    foreach (Process processToClose in runningProcessesToClose)
                    {
                        SEBNotAllowedProcessController.CloseProcess(processToClose);
                    }
                    runningProcessesToClose.Clear();
                    runningApplicationsToClose.Clear();
                }
                else
                {
                    ExitApplication();
                    return;
                }
            }            

            // So if there are any permitted processes, we add them to the SEB task bar
            if (permittedProcessList.Count > 0)
            {
                for (int i = 0; i < permittedProcessList.Count; i++)
                {
                    Dictionary<string, object> permittedProcess = (Dictionary<string, object>)permittedProcessList[i];

                    //Do not add permitted processses that are set to run in background and not autostart
                    if ((bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyRunInBackground) &&
                        !(bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyAutostart))
                        continue;

                    SEBSettings.operatingSystems permittedProcessOS = (SEBSettings.operatingSystems)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyOS);
                    bool permittedProcessActive = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyActive);
                    //if (permittedProcessActive == null) permittedProcessActive = false;
                    if (permittedProcessOS == SEBSettings.operatingSystems.operatingSystemWin && permittedProcessActive)
                    {
                        string identifier = (string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyIdentifier);
                        string windowHandlingProcesses = (string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyWindowHandlingProcess);
                        string title = (string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyTitle);
                        string executable = (string)permittedProcess[SEBSettings.KeyExecutable];
                        if (String.IsNullOrEmpty(title)) title = executable;
                        if (!(executable.Contains(SEBClientInfo.XUL_RUNNER) && !(bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser)))
                        {
                            var toolStripButton = new SEBToolStripButton();

                            //Do not add processes that are meant to run in background
                            //if ((Boolean) SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyRunInBackground))
                            //    toolStripButton.Visible = false;

                            //Do not add processes that do not have an Icon in Taskbar
                            if (!(Boolean)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyIconInTaskbar))
                                toolStripButton.Visible = false;

                            toolStripButton.Padding = new Padding(5, 0, 5, 0);
                            toolStripButton.ToolTipText = title;
                            toolStripButton.Identifier = identifier;
                            toolStripButton.WindowHandlingProcess = windowHandlingProcesses;
                            Icon processIcon = null;
                            Bitmap processImage = null;
                            string fullPath;
                            if (executable.Contains(SEBClientInfo.XUL_RUNNER))
                                fullPath = Application.ExecutablePath;
                            else
                            {
                                //fullPath = GetApplicationPath(executable);
                                fullPath = GetPermittedApplicationPath(permittedProcess);
                            }
                            // Continue only if the application has been found
                            if (fullPath != null)
                            {
                                var x = toolStripButton.Height;
                                processImage = Iconextractor.ExtractHighResIconImage(fullPath, taskbarHeight-8);
                                if (processImage == null)
                                {
                                    processIcon = GetApplicationIcon(fullPath);
                                // If the icon couldn't be read, we try it again
                                if (processIcon == null && processImage == null) processIcon = GetApplicationIcon(fullPath);
                                // If it again didn't work out, we try to take the icon of SEB
                                if (processIcon == null) processIcon = GetApplicationIcon(Application.ExecutablePath);
                                    toolStripButton.Image = processIcon.ToBitmap();
                                }
                                else
                                {
                                    toolStripButton.Image = processImage;
                                }
                                permittedProcessesIconImages.Add(toolStripButton.Image);
                                toolStripButton.Click += new EventHandler(ToolStripButton_Click);

                                // We save the index of the permitted process to the toolStripButton.Name property
                                toolStripButton.Name = permittedProcessesCalls.Count.ToString();

                                //if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyShowTaskBar))
                                taskbarToolStrip.Items.Add(toolStripButton);
                                //toolStripButton.Checked = true;

                                // Treat XULRunner different than other processes
                                if (!executable.Contains(SEBClientInfo.XUL_RUNNER))
                                {
                                    StringBuilder startProcessNameBuilder = new StringBuilder(fullPath);
                                    List<object> argumentList = (List<object>)permittedProcess[SEBSettings.KeyArguments];
                                    for (int j = 0; j < argumentList.Count; j++)
                                    {
                                        Dictionary<string, object> argument = (Dictionary<string, object>)argumentList[j];
                                        if ((Boolean)argument[SEBSettings.KeyActive])
                                        {
                                            startProcessNameBuilder.Append(" ").Append((string)argument[SEBSettings.KeyArgument]);
                                        }
                                    }
                                    string fullPathArgumentsCall = startProcessNameBuilder.ToString();

                                    // Save the full path of the permitted process executable including arguments
                                    permittedProcessesCalls.Add(fullPathArgumentsCall);

                                }
                                else
                                {
                                    // The permitted process is XULRunner: Build list of arguments that are allowed to be user defined
                                    if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser))
                                    {
                                        StringBuilder startProcessNameBuilder = new StringBuilder("");
                                        List<object> argumentList = (List<object>)permittedProcess[SEBSettings.KeyArguments];
                                        for (int j = 0; j < argumentList.Count; j++)
                                        {
                                            Dictionary<string, object> argument = (Dictionary<string, object>)argumentList[j];
                                            if ((Boolean)argument[SEBSettings.KeyActive])
                                            {
                                                string argumentString = (string)argument[SEBSettings.KeyArgument];
                                                // The parameters -app and -ctrl cannot be changed by the user, we skip them 
                                                if (!argumentString.Contains("-app") && !argumentString.Contains("-ctrl")) 
                                                    startProcessNameBuilder.Append(" ").Append((string)argument[SEBSettings.KeyArgument]);
                                            }
                                        }
                                        string fullPathArgumentsCall = startProcessNameBuilder.ToString();

                                        // Save the full path of the permitted process executable including arguments
                                        permittedProcessesCalls.Add(fullPathArgumentsCall);
                                    }
                                }
                            }
                            else
                            {
                                // Permitted application has not been found: Set its call entry to null
                                permittedProcessesCalls.Add(null);
                                //SEBClientInfo.SebWindowsClientForm.Activate();
                                SEBMessageBox.Show(SEBUIStrings.permittedApplicationNotFound, SEBUIStrings.permittedApplicationNotFoundMessage.Replace("%s",title), MessageBoxIcon.Error, MessageBoxButtons.OK);
                            }
                        }
                    }
                }
            }

            //Filling System Icons

            //QuitButton
            if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAllowQuit])
            {
                var quitButton = new SEBQuitToolStripButton();
                quitButton.Click += (x, y) => ShowCloseDialogForm();
                taskbarToolStrip.Items.Add(quitButton);
            }

            //Wlan Control
            try
            {
                if ((bool)SEBClientInfo.getSebSetting(SEBSettings.KeyAllowWLAN)[SEBSettings.KeyAllowWLAN])
                {
                    taskbarToolStrip.Items.Add(new SEBWlanToolStripButton());
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to add WLANControl",this,ex);
            }
            

            //Add the OnScreenKeyboardControl (only if not in Create New Desktop Mode)

            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] ==
                true && !(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
            {
                var sebOnScreenKeyboardToolStripButton = new SEBOnScreenKeyboardToolStripButton();
                taskbarToolStrip.Items.Add(sebOnScreenKeyboardToolStripButton);
                TapTipHandler.RegisterXulRunnerEvents();
                TapTipHandler.OnKeyboardStateChanged += shown => this.BeginInvoke(new Action(
                    () => this.PlaceFormOnDesktop(shown)));
            }

            //Add the RestartExamButton if configured
            if (!String.IsNullOrEmpty(SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamURL)[SEBSettings.KeyRestartExamURL].ToString()) || (bool)SEBSettings.settingsCurrent[SEBSettings.KeyRestartExamUseStartURL] == true)
                taskbarToolStrip.Items.Add(new SEBRestartExamToolStripButton());

            //Add the ReloadhBrowserButton
            if ((bool)SEBClientInfo.getSebSetting(SEBSettings.KeyShowReloadButton)[SEBSettings.KeyShowReloadButton])
                taskbarToolStrip.Items.Add(new SEBReloadBrowserToolStripButton());

            //Add the BatterystatusControl to the toolbar
            try
            {
                //Always add it, and hide it if connected to power source
                //if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline)
                    taskbarToolStrip.Items.Add(new SEBBatterylifeToolStripButton());
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to add the Batterystatuscontrol",this,ex);
            }

            //KeyboardLayout Chooser
            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyShowInputLanguage)[SEBSettings.KeyShowInputLanguage] == true)
            {
                taskbarToolStrip.Items.Add(new SEBInputLanguageToolStripButton());
            }

            //Watch (Time)
            if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyShowTime)[SEBSettings.KeyShowTime] == true)
            {
                taskbarToolStrip.Items.Add(new SEBWatchToolStripButton());
            }

            // Start permitted processes
            int permittedProcessesIndex = 0;
            for (int i = 0; i < permittedProcessList.Count; i++)
            //foreach (string processCallToStart in permittedProcessesCalls)
            {
                Dictionary<string, object> permittedProcess = (Dictionary<string, object>)permittedProcessList[i];

                //Do not start permitted processses that are set to run in background and not autostart
                if ((bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyRunInBackground) &&
                    !(bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyAutostart))
                    continue;

                SEBSettings.operatingSystems permittedProcessOS = (SEBSettings.operatingSystems)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyOS);
                bool permittedProcessActive = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyActive);
                string executable = (string)permittedProcess[SEBSettings.KeyExecutable];
                if (permittedProcessOS == SEBSettings.operatingSystems.operatingSystemWin && permittedProcessActive)
                {
                    if (!executable.Contains(SEBClientInfo.XUL_RUNNER))
                    {
                        // Autostart processes which have the according flag set
                        Process newProcess = null;
                        if ((Boolean)permittedProcess[SEBSettings.KeyAutostart])
                        {
                            string fullPathArgumentsCall = permittedProcessesCalls[permittedProcessesIndex];
                            if (fullPathArgumentsCall != null) newProcess = CreateProcessWithExitHandler(fullPathArgumentsCall);
                            else newProcess = null;
                        }
                        // Save the process reference if the process was started, otherwise null
                        permittedProcessesReferences.Add(newProcess);
                        permittedProcessesIndex++;
                    }
                    else
                    {
                        if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser))
                        {
                            // Start XULRunner
                            StartXulRunner((string)permittedProcessesCalls[permittedProcessesIndex]);
                            // Save the process reference of XULRunner
                            permittedProcessesReferences.Add(xulRunner);
                            permittedProcessesIndex++;
                        }
                    }
                }
            }

            SEBToForeground();
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Get icon for a running process.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private Icon GetProcessIcon(Process process)
        {
            Icon processIcon;
            try
            {
                string processExecutableFileName = process.MainModule.FileName;
                processIcon = Icon.ExtractAssociatedIcon(processExecutableFileName);
            }
            catch (Exception)
            {
                processIcon = null;
            }
            return processIcon;
        }


        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Get icon for an application specified by a full path.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private Icon GetApplicationIcon(string fullPath)
        {
            Icon processIcon;
            try
            {
                //var icon = IconTools.GetIconForFile(fullPath, ShellIconSize.LargeIcon);
                //return new Icon();
            }catch{}
            
            try
            {
                processIcon = Icon.ExtractAssociatedIcon(fullPath);
            }
            catch (Exception)
            {
                processIcon = null;
            }
            return processIcon;
        }


        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Get the full path of an application from which we know the executable name 
        /// by searching the application paths which are set in the Registry.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public string GetApplicationPath(string executable, string executablePath = "")
        {
            // Check if executable string contained also a valid path
            if (File.Exists(executable)) return executable;

            // Check if executable is in the Programm Directory
            string programDir = SEBClientInfo.ProgramFilesX86Directory + "\\";
            if (File.Exists(programDir + executable)) return programDir;

            // Check if executable is in the System Directory
            string systemDirectory = Environment.SystemDirectory + "\\";
            if (File.Exists(systemDirectory + executable)) return systemDirectory;

            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, ""))
            {
                //// Get all paths from the PATH environement variable
                //// This code didn't get the results expected, just left here for reference
                //string RegKeyName = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
                //string pathVariableString = (string)Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegKeyName).GetValue
                //    ("Path", "", Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames);
                //string[] paths = pathVariableString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                //foreach (string subPath in paths)
                //{
                //    Console.WriteLine(subPath);
                //}

                string subKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + executable;
                using (Microsoft.Win32.RegistryKey subkey = key.OpenSubKey(subKeyName))
                {
                    if (subkey == null)
                    {
                        return null;
                    }

                    object path = subkey.GetValue("Path");

                    if (path != null)
                    {
                        return (string)path;
                    }
                }
            }
            return null;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Get the full path of an application from which we know the executable name 
        /// by searching the application paths which are set in the Registry.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public string GetPermittedApplicationPath(DictObj permittedProcess)
        {
            string executable = (string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyExecutable);
            if (executable == null) executable = "";
            string executablePath = (string)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyPath);
            if (executablePath == null) executablePath = "";
            bool allowChoosingApp = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyAllowUser);
            //if (allowChoosingApp == null) allowChoosingApp = false;
            string fullPath;

            // There is a permittedProcess.path value
            if (executablePath != "")
            {
                fullPath = executablePath + "\\" + executable;
                // In case path to the executable's directory + the file name of the executable is already the correct file, we return this full path
                if (File.Exists(fullPath)) return fullPath;
            }
            // Otherwise try to determine the applications full path
            string path = GetApplicationPath(executable);

            // If a path to the executable was found
            if (path != null)
            {
                fullPath = path + executable;
                // Maybe the executablePath information wasn't necessary to find the application, then we return this found path
                if (File.Exists(fullPath)) return fullPath;
 
                // But maybe the executable path is a relative path from the applications main directory to some subdirectory with the executable in it?
                fullPath = path + executablePath + "\\" + executable;
                if (File.Exists(fullPath)) return fullPath;
            }

            // In the end we try to find the application using one of the system's standard paths + subdirectory path + executable
            fullPath = null;
            path = GetApplicationPath(executablePath + "\\" + executable);
            if (path != null)
            {
                fullPath = path + executablePath + "\\" + executable;
            }

            // If we still didn't find the application and the setting for this permitted process allows user to find the application
            if (fullPath == null && allowChoosingApp == true && !String.IsNullOrEmpty(executable))
            {
                // Ask the user to locate the application
                SEBToForeground();
                return ThreadedDialog.ShowFileDialogForExecutable(executable);
            }
            return fullPath;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Handle click on permitted process in SEB taskbar: If process isn't running,
        /// it is started, otherwise the click is ignored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// ----------------------------------------------------------------------------------------
        protected void ToolStripButton_Click(object sender, EventArgs e)
        {
            // identify which button was clicked and perform necessary actions
            var toolStripButton = sender as SEBToolStripButton;

            int i = Convert.ToInt32(toolStripButton.Name);
            Process processReference = permittedProcessesReferences[i];

            if (xulRunner != null && processReference == xulRunner)
            {
                try
                {
                    // In case the XULRunner process exited but wasn't closed, this will throw an exception
                    if (xulRunner.HasExited)
                    {
                        StartXulRunner((string)permittedProcessesCalls[i]);
                    }
                    else
                    {
                        processReference.Refresh();

                        new WindowChooser(processReference, ((ToolStripButton)sender).Bounds.X, Screen.PrimaryScreen.Bounds.Height - taskbarHeight);
                    }
                }
                catch (Exception)  // XULRunner wasn't running anymore
                {
                    StartXulRunner((string)permittedProcessesCalls[i]);
                }
            }
            else
            {
                try
                {
                    if (processReference == null || processReference.HasExited == true)
                    {
                        StartPermittedProcessById(i);
                    }
                    else
                    {
                        processReference.Refresh();

                        //If the process has no mainWindowHandle try to find the window that belongs to the WindowHandlingProcess of the process (defined in config) which then is set to the tooltip of the button :)
                        if (processReference.MainWindowHandle == IntPtr.Zero &&
                            !String.IsNullOrWhiteSpace(toolStripButton.WindowHandlingProcess))
                        {
                            foreach (var oW in SEBWindowHandler.GetOpenWindows())
                            {
                                var proc = oW.Key.GetProcess();
                                if (toolStripButton.WindowHandlingProcess.ToLower()
                                        .Contains(proc.GetExecutableName().ToLower())
                                    ||
                                    proc.GetExecutableName()
                                        .ToLower()
                                        .Contains(toolStripButton.WindowHandlingProcess.ToLower()))
                                {
                                    processReference = proc;
                                    break;
                                }
                            }
                        }

                        //If the process still ha no mainWindowHandle try open by window name comparing with title set in config which then is set to the tooltip of the button :)
                        if (processReference.MainWindowHandle == IntPtr.Zero)
                            processReference =
                                SEBWindowHandler.GetWindowHandleByTitle(toolStripButton.Identifier).GetProcess();


                        new WindowChooser(processReference, ((ToolStripButton) sender).Bounds.X,
                            Screen.PrimaryScreen.Bounds.Height - taskbarHeight);

                    }
                }
                catch (ObjectDisposedException ex)
                {
                    StartPermittedProcessById(i);
                }
                catch (Exception ex)
                {
                    Logger.AddError("Error when trying to start permitted process by clicking in SEB taskbar: ", null, ex);
                }
            }
        }

        static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
            return true;
        }

        private void StartPermittedProcessById(int id)
        {
            string permittedProcessCall = (string)permittedProcessesCalls[id];
            Process newProcess = CreateProcessWithExitHandler(permittedProcessCall);
            permittedProcessesReferences[id] = newProcess;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Create a new process and add an exited event handler.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private Process CreateProcessWithExitHandler(string fullPathArgumentsCall)
        {
            Process newProcess = SEBDesktopController.CreateProcess(fullPathArgumentsCall, SEBClientInfo.DesktopName);
            //newProcess.EnableRaisingEvents = true;
            //newProcess.Exited += new EventHandler(permittedProcess_Exited);

            return newProcess;
        }


        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Handle xulRunner_Exited event and display process information.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private void permittedProcess_Exited(object sender, System.EventArgs e)
        {
            Process permittedProcess = (Process)sender;
            //int permittedProcessExitCode = permittedProcess.ExitCode;
            //DateTime permittedProcessExitTime = permittedProcess.ExitTime;
            //if (permittedProcessExitCode != 0)
            //{
            //    // An error occured when exiting the permitted process, maybe it crashed?
            //    Logger.AddError("An error occurred when exiting a permitted process. Exit code: " + permittedProcessExitCode.ToString() + ". Exit time: " + permittedProcess.ExitTime.ToString(), this, null);
            //}
        }

        private float scaleFactor = 1;

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Set form on Desktop.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private bool SetFormOnDesktop()
        {
            if (!(bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyShowTaskBar))
            {
                return false;
            }

            float dpiX;
            using (var g = this.CreateGraphics())
            {
                dpiX = g.DpiX;
            }
            scaleFactor = dpiX / 96;
            SEBClientInfo.scaleFactor = scaleFactor;
            Logger.AddInformation("Current display DPI setting: " + dpiX.ToString() + " and scale factor: " +scaleFactor.ToString());

            float sebTaskBarHeight = (int) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTaskBarHeight);
            if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] == true)
            {
                taskbarHeight = (int)(sebTaskBarHeight * 1.7 * scaleFactor);
                this.taskbarToolStrip.ImageScalingSize = new Size(taskbarHeight - 8, taskbarHeight -8);
            }
            else
            {
                taskbarHeight = (int)(sebTaskBarHeight * scaleFactor);
                this.taskbarToolStrip.ImageScalingSize = new Size(taskbarHeight - 8, taskbarHeight -8);
            }

            Logger.AddInformation("Taskbarheight from settings: " +sebTaskBarHeight.ToString() + " Current taskbar height: " + taskbarHeight.ToString());

            //Modify Working Area
            SEBWorkingAreaHandler.SetTaskBarSpaceHeight(taskbarHeight);

            this.FormBorderStyle = FormBorderStyle.None;
            // sezt das formular auf die Taskbar
            SetParent(this.Handle, GetDesktopWindow());
            //this.BackColor = Color.Red;

            this.TopMost = true;
            PlaceFormOnDesktop(false, true);            

            return true;
        }

        private void PlaceFormOnDesktop(bool KeyboardShown, bool isInitial = false)
        {
            if (KeyboardShown && TapTipHandler.IsKeyboardDocked() && (bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized))
            {
                this.Hide();
                var keyboardHeight = TapTipHandler.GetKeyboardWindowHandle().GetWindowHeight();
                Logger.AddInformation("Keyboard height from its window: " + keyboardHeight);

                SEBWorkingAreaHandler.SetTaskBarSpaceHeight(keyboardHeight);
                var topWindow = SEBWindowHandler.GetOpenWindows().FirstOrDefault();
                if (topWindow.Value != null)
                {
                    topWindow.Key.AdaptWindowToWorkingArea(keyboardHeight);
                }
                SEBXULRunnerWebSocketServer.SendKeyboardShown();
            }
            else
            {
                //Modify Working Area
                SEBWorkingAreaHandler.SetTaskBarSpaceHeight(taskbarHeight);
                if ((bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyShowTaskBar))
                {
                    int width = Screen.PrimaryScreen.Bounds.Width;
                    var x = 0;
                    var y = Screen.PrimaryScreen.Bounds.Height - taskbarHeight;
                    this.Height = taskbarHeight;
                    this.Width = width;
                    this.Location = new Point(x, y);
                    this.Show();
                }

                if ((bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized))
                {
                    var topWindow = SEBWindowHandler.GetOpenWindows().FirstOrDefault();
                    if (topWindow.Value != null)
                    {
                        if (isInitial)
                        {
                            //Maximize the XulRunner Window
                            foreach (var oW in xulRunner.GetOpenWindows())
                            {
                                oW.Key.MaximizeWindow();
                            }
                        }
                        else
                        {
                            topWindow.Key.AdaptWindowToWorkingArea(taskbarHeight);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the handle of the active window of a process
        /// </summary>
        /// <param name="taskBarWnd">window handle of taskbar</param>
        /// <returns>window handle of start menu</returns>
        private static IntPtr GetActiveWindowOfProcess(Process proc)
        {
            //if (proc != null)
            //{
            //    // enumerate all threads of that process...
            //    foreach (ProcessThread thread in proc.Threads)
            //    {
            //        if (EnumThreadWindows(thread.Id, MyEnumThreadWindowsForProcess, IntPtr.Zero)) break;
            //    }
            //}
            return vistaStartMenuWnd;
        }

        /// <summary>
        /// Callback method that is called from 'EnumThreadWindows' in 'GetVistaStartMenuWnd'.
        /// </summary>
        /// <param name="hWnd">window handle</param>
        /// <param name="lParam">parameter</param>
        /// <returns>true to continue enumeration, false to stop it</returns>
        private static bool MyEnumThreadWindowsForProcess(int pid, IntPtr lParam)
        {
                //if (pid ==)
                //{
                //    vistaStartMenuWnd = hWnd;
                //    return false;
                //}
            return true;
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
        /// Set registry values and close prohibited processes.
        /// </summary>
        /// <returns>true if succeed</returns>
        /// ----------------------------------------------------------------------------------------
        private bool InitClientRegistryAndKillProcesses()
        {

            // Add prohibited processes to the "processes not permitted to run" list 
            // which will be dealt with after checking if permitted processes are already running;
            // the user will be asked to quit all those processes him/herself or to let SEB kill them
            // Prohibited processes with the strongKill flag set can be killed without user consent

            List<object> prohibitedProcessList = (List<object>)SEBClientInfo.getSebSetting(SEBSettings.KeyProhibitedProcesses)[SEBSettings.KeyProhibitedProcesses];
            if (prohibitedProcessList.Count() > 0)
            {
                // Check if the prohibited processes are running
                Process[] runningApplications;
                runningProcessesToClose.Clear();
                runningApplicationsToClose.Clear();
                for (int i = 0; i < prohibitedProcessList.Count; i++)
                {
                    Dictionary<string, object> prohibitedProcess = (Dictionary<string, object>)prohibitedProcessList[i];
                    SEBSettings.operatingSystems prohibitedProcessOS = (SEBSettings.operatingSystems)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyOS);
                    bool prohibitedProcessActive = (bool)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyActive);
                    if (prohibitedProcessOS == SEBSettings.operatingSystems.operatingSystemWin && prohibitedProcessActive)
                    {
                        string title = (string)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyTitle);
                        if (title == null) title = "";
                        string executable = ((string)prohibitedProcess[SEBSettings.KeyExecutable]).ToLower();
                        // Check if the process is running
                        runningApplications = Process.GetProcesses();
                        for (int j = 0; j < runningApplications.Count(); j++)
                        {
                            string runningProcessName = runningApplications[j].ProcessName;
                            if (runningProcessName != null && executable.Contains(runningProcessName.ToLower()))
                            {
                                // If the flag strongKill is set, then the process is killed without asking the user
                                bool strongKill = (bool)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyStrongKill);
                                if (strongKill)
                                {
                                    SEBNotAllowedProcessController.CloseProcess(runningApplications[j]);
                                }
                                else
                                {
                                    runningProcessesToClose.Add(runningApplications[j]);
                                    runningApplicationsToClose.Add(title == "SEB" ? (string)prohibitedProcess[SEBSettings.KeyExecutable] : title);
                                    //runningApplicationsToClose.Add((title == "SEB" ? "" : (title == "" ? "" : title + " - ")) + executable);
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Show SEB Application Chooser Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void ShowApplicationChooserForm()
        {
            //SetForegroundWindow(this.Handle);
            //this.Activate();
            sebApplicationChooserForm.fillListApplications();
            sebApplicationChooserForm.Visible = true;
            //sebCloseDialogForm.Activate();
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Show SEB Application Chooser Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void SelectNextListItem()
        {
            sebApplicationChooserForm.SelectNextListItem();
            //sebApplicationChooserForm.Visible = true;
            //sebCloseDialogForm.Activate();
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Hide SEB Application Chooser Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void HideApplicationChooserForm()
        {
            sebApplicationChooserForm.Visible = false;
            //sebCloseDialogForm.Activate();
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Show SEB Close Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void ShowCloseDialogForm()
        {
            // Test if quitting SEB is allowed
            if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAllowQuit] == true)
            {
                SebWindowsClientMain.SEBToForeground();
                // Is a quit password set?
                string hashedQuitPassword = (string)SEBSettings.settingsCurrent[SEBSettings.KeyHashedQuitPassword];
                if (String.IsNullOrEmpty(hashedQuitPassword) == true)
                // If there is no quit password set, we just ask user to confirm quitting
                {
                    SetForegroundWindow(this.Handle);
                    ShowCloseDialogFormConfirmation();
                }
                else
                {
                    SetForegroundWindow(this.Handle);
                    if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyTouchOptimized])
                    {
                        sebCloseDialogForm.InitializeForTouch();
                    }
                    else
                    {
                        sebCloseDialogForm.InitializeForNonTouch();
                    }
                    

                    // Show testDialog as a modal dialog and determine if DialogResult = OK.
                    sebCloseDialogForm.Visible = true;
                    sebCloseDialogForm.Activate();
                    sebCloseDialogForm.txtQuitPassword.Focus();
                }
            }
        }

        private bool closeDialogConfirmationIsOpen;
        public void ShowCloseDialogFormConfirmation()
        {
            if (closeDialogConfirmationIsOpen)
                return;

            closeDialogConfirmationIsOpen = true;
            SebWindowsClientMain.SEBToForeground();
            this.TopMost = true;
            if (
                SEBMessageBox.Show(SEBUIStrings.confirmQuitting, SEBUIStrings.confirmQuittingQuestion,
                    MessageBoxIcon.Question, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                //SEBClientInfo.SebWindowsClientForm.closeSebClient = true;
                ExitApplication();
            }
            else
            {
                closeDialogConfirmationIsOpen = false;
            }
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Open SEB form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public bool OpenSEBForm()
        {
            Logger.AddInformation("entering Opensebform");
            if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyShowTaskBar))
            {
                //this.Show();
                Logger.AddInformation("attempting to position the taskbar");
                SetFormOnDesktop();
                Logger.AddInformation("finished taskbar positioning");
                //if (!this.Controls.Contains(this.taskbarToolStrip))
                //{
                //    this.Controls.Add(this.taskbarToolStrip);
                //    taskbarToolStrip.Show();
                //    Logger.AddInformation("Removed SEB taskbar re-added to form.", null, null);
                //}
            }
            else
            {
                Logger.AddInformation("hiding the taskbar");
                //this.Hide();
                this.Visible = false;
                this.Height = 1;
                this.Width = 1;
                //this.BackColor = Color.Transparent;
                this.Location = new System.Drawing.Point(-50000, -50000);

                taskbarHeight = 0;

                PlaceFormOnDesktop(false, true);
            }

            // Check if VM and SEB Windows Service are available and forbidden/required
            try
            {
                SebWindowsClientMain.CheckIfInsideVirtualMachine();
                SebWindowsClientMain.CheckServicePolicy(SebWindowsServiceHandler.IsServiceAvailable);

                //Set Registry Values to lock down CTRL+ALT+DELETE Menu (with SEBWindowsServiceWCF)
                try
                {
                    Logger.AddInformation("setting registry values");
                    if (SebWindowsServiceHandler.IsServiceAvailable &&
                        !SebWindowsServiceHandler.SetRegistryAccordingToConfiguration())
                    {
                        Logger.AddError("Unable to set Registry values", this, null);
                        SebWindowsClientMain.CheckServicePolicy(false);
                    }
                }
                catch (SEBNotAllowedToRunEception ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to set Registry values", this, ex);
                    SebWindowsClientMain.CheckServicePolicy(false);
                }

                //Disable windows update service (with SEBWindowsServiceWCF)
                try
                {
                    Logger.AddInformation("Disabling Windows update");
                    if (SebWindowsServiceHandler.IsServiceAvailable && !SebWindowsServiceHandler.DisableWindowsUpdate())
                        Logger.AddWarning("Unable to disable Windows update service", this, null);
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to disable Windows update service", this, ex);
                }

                try
                {
                    Logger.AddInformation("killing processes that are not allowed to run");
                    bool bClientRegistryAndProcesses = InitClientRegistryAndKillProcesses();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to kill processes that are running before start", this, ex);
                }

                Logger.AddInformation("attempting to start socket server");
                SEBXULRunnerWebSocketServer.StartServer();

                // Disable unwanted keys.
                SebKeyCapture.FilterKeys = true;

                try
                {
                    Logger.AddInformation("adding allowed processes to taskbar");
                    addPermittedProcessesToTS();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to addPermittedProcessesToTS", this, ex);
                }

                if (sebCloseDialogForm == null)
                {
                    Logger.AddInformation("creating close dialog form");
                    sebCloseDialogForm = new SebCloseDialogForm();
                    sebCloseDialogForm.TopMost = true;
                }
                if (sebApplicationChooserForm == null)
                {
                    Logger.AddInformation("building application chooser form");
                    sebApplicationChooserForm = new SebApplicationChooserForm();
                    sebApplicationChooserForm.TopMost = true;
                    sebApplicationChooserForm.Show();
                    sebApplicationChooserForm.Visible = false;
                }

                return true;
            }
            catch (SEBNotAllowedToRunEception ex)
            {
                // VM or service not available and set to be required
                Logger.AddInformation(string.Format("exiting without starting up because {0}", ex.Message));
                ExitApplication(false);
                return false;
            }
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Close SEB Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void CloseSEBForm()
        {
            {
                //Restore Registry Values
                try
                {
                    Logger.AddInformation("restoring registry entries");

                    if (!SebWindowsServiceHandler.IsServiceAvailable)
                    {
                        Logger.AddInformation("Restarting Service Connection");
                        SebWindowsServiceHandler.Reconnect();
                    }

                    Logger.AddInformation("windows service is available");
                    if (!SebWindowsServiceHandler.ResetRegistry())
                    {
                        Logger.AddWarning("Unable to reset Registry values", this, null);
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to reset Registry values",this,ex);
                }

                try
                {
                    Logger.AddInformation("attempting to reset workspacearea");
                    SEBWorkingAreaHandler.ResetWorkspaceArea();
                    Logger.AddInformation("workspace area resetted");
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to reset WorkingArea",this,ex);
                }

                // ShutDown Processes

                //Deregister SEB closed Event
                if (xulRunner != null)
                {
                    xulRunner.Exited -= xulRunner_Exited;   
                }

                Logger.AddInformation("closing processes that where started by seb");
                for(int i = 0; i < permittedProcessesReferences.Count; i++)
                {
                    try
                    {
                        var proc = permittedProcessesReferences[i];
                        if (proc != null && !proc.HasExited && proc.MainWindowHandle == IntPtr.Zero)
                        {
                            //Get Process from WindowHandle by Name
                            var permittedProcessSettings = (List<object>)SEBClientInfo.getSebSetting(SEBSettings.KeyPermittedProcesses)[SEBSettings.KeyPermittedProcesses];
                            var currentProcessData = (Dictionary<string, object>)permittedProcessSettings[i];
                            var title = (string)currentProcessData[SEBSettings.KeyIdentifier];
                            proc = SEBWindowHandler.GetWindowHandleByTitle(title).GetProcess();
                        }
                        if (proc != null && !proc.HasExited)
                        {
                            Logger.AddInformation("attempting to close " + proc.ProcessName);
                            SEBNotAllowedProcessController.CloseProcess(proc);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddError("Unable to shutdown process",null, ex);
                    }
                    
                }
                Logger.AddInformation("clearing running processes list");
                permittedProcessesReferences.Clear();

                //Disable Watchdogs
                Logger.AddInformation("disabling foreground watchdog");
                SEBWindowHandler.DisableForegroundWatchDog();
                Logger.AddInformation("disabling process watchdog");
                SEBProcessHandler.DisableProcessWatchDog();

                //Restore the hidden Windows
                try
                {
                    Logger.AddInformation("restoring hidden windows");
                    SEBWindowHandler.RestoreHiddenWindows();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to restore hidden windows", null, ex);
                }

                //Reset the Wallpaper
                SEBDesktopWallpaper.Reset();

                // Restart the explorer.exe shell
                if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyKillExplorerShell)[SEBSettings.KeyKillExplorerShell])
                {
                    if (SEBClientInfo.ExplorerShellWasKilled)
                    {
                        try
                        {
                            Logger.AddInformation("Attempting to start Explorer Shell");
                            SEBProcessHandler.StartExplorerShell();
                            Logger.AddInformation("Successfully started Explorer Shell");
                        }
                        catch (Exception ex)
                        {
                            Logger.AddError("Unable to StartExplorerShell",null,ex);
                        }
                    }
                }

                // Clean clipboard

                SEBClipboard.CleanClipboard();
                Logger.AddInformation("Clipboard deleted.", null, null);

                Logger.AddInformation("disabling filtered keys");
                SebKeyCapture.FilterKeys = false;

                Logger.AddInformation("returning from closesebform");
            }
        }


        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Load form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// ----------------------------------------------------------------------------------------
        public void SebWindowsClientForm_Load(object sender, EventArgs e)
        {
            //if (SebWindowsClientMain.InitSebSettings())
            //{
                //OpenSEBForm();
            //}
            //else {
            //    Application.Exit();
            //}
        }

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Close form, if Quit Password is correct.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// ----------------------------------------------------------------------------------------
        public void SebWindowsClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //moved code -> call ExitApplication()
        }

        /// <summary>
        /// Central code to exit the application
        /// Closes the form and asks for a quit password if necessary
        /// </summary>
        public void ExitApplication(bool showLoadingScreen = true)
        {
            //Only show the loading screen when not in CreateNewDesktop-Mode
            if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyCreateNewDesktop])
            {
                showLoadingScreen = false;
            }

            Thread loadingThread = null;
            if (showLoadingScreen)
            {
                SEBSplashScreen.CloseSplash();
                loadingThread = new Thread(SEBLoading.StartLoading);
                loadingThread.Start();
            }
            Logger.AddInformation("Attempting to CloseSEBForm in ExitApplication");
            try
            {
                CloseSEBForm();
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to CloseSEBForm()", this, ex);
            }
            Logger.AddInformation("Successfull CloseSEBForm");

            if (showLoadingScreen)
            {
                Logger.AddInformation("closing loading screen");
                SEBLoading.CloseLoading();
                try
                {
                   loadingThread.Abort();
                }
                catch{}
            }


            Logger.AddInformation("Attempting to ResetSEBDesktop in ExitApplication");
            SebWindowsClientMain.ResetSEBDesktop();
            Logger.AddInformation("Successfull ResetSEBDesktop");


            Logger.AddInformation("---------- EXITING SEB - ENDING SESSION -------------");
            //this.Close();
            Application.Exit();
            Environment.Exit(0);
        }



        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Show dialog asking whether SEB should be closed
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        private void noSelectButton1_Click(object sender, EventArgs e)
        {
            if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAllowQuit] == true)
            {
                ShowCloseDialogForm();
            }
        }       
    }

    /// ----------------------------------------------------------------------------------------
    /// <summary>
    /// Button subclass which makes the button-not-selectable. 
    /// Why this isn't a default attribute of a button???.
    /// </summary>
    /// ----------------------------------------------------------------------------------------
    class NoSelectButton : Button
    {
        public NoSelectButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
    }

}
