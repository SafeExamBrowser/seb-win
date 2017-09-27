using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.CryptographyUtils;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient.ProcessUtils
{
	/// <summary>
	/// Offers methods to handle windows
	/// </summary>
	public static class SEBProcessHandler
    {

        #region Public Members

        /// <summary>
        /// A list of not prohibited window executables
        /// </summary>
        public static List<string> ProhibitedExecutables = new List<string>();

        #endregion

        #region Private Members

        private static ProcessWatchDog _processWatchDog;

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the explorer shell if not running
        /// This task sleeps the Thread for six seconds to make sure the explorer shell starts up completely
        /// </summary>
        public static void StartExplorerShell(bool waitForStartup = true)
        {
            //Check if explorer is running by trying to get the TrayWindow Handle
            IntPtr lHwnd = FindWindow("Shell_TrayWnd", null);
            if (lHwnd == IntPtr.Zero)
            {
                //If not running, start explorer.exe
                string explorer = string.Format("{0}\\{1}", Environment.GetEnvironmentVariable("WINDIR"), "explorer.exe");
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = explorer,
                        UseShellExecute = true,
                        WorkingDirectory = Application.StartupPath,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                //Wait until the explorer is up again because its functions are needed in the next call
                for (int i = 0; i < 6; i++)
                {
                    Logger.AddInformation("waiting for explorer shell to get up " + i + " seconds");
                    if (FindWindow("Shell_TrayWnd", null) != IntPtr.Zero)
                        break;
                    Thread.Sleep(1000);
                }
                //Sleep six seconds to get the explorer running
                if (waitForStartup)
                {
                    Logger.AddInformation("waiting for explorer shell to finish starting 6 seconds");
                    Thread.Sleep(6000);
                }
                    
            }
        }

        const int WM_USER = 0x0400; //http://msdn.microsoft.com/en-us/library/windows/desktop/ms644931(v=vs.85).aspx
        /// <summary>
        /// Kills the explorer Shell
        /// </summary>
        /// <returns></returns>
        public static bool KillExplorerShell()
        {
            try
            {
                var handle = FindWindow("Shell_TrayWnd", null);
                if (handle != IntPtr.Zero)
                {
                    PostMessage(handle, WM_USER + 436, IntPtr.Zero, IntPtr.Zero);

                    //Wait until the explorer shell has been killed
                    while (FindWindow("Shell_TrayWnd", null) != IntPtr.Zero)
                    {
                        Thread.Sleep(500);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddInformation("{0} {1}", ex.Message, null, null);
                return false;
            }
        }

        public static void LogAllRunningProcesses()
        {
            var runningProcesses = "\nAll Running Processes:\n--------------";
            foreach (var process in Process.GetProcesses())
            {
                runningProcesses += "\n" + process.GetExecutableName();
            }
            Logger.AddInformation(runningProcesses);
        }

        #endregion

        #region Process WatchDog control

        /// <summary>
        /// Enables a process checker that kills processes that are prohibited
        /// </summary>
        public static void EnableProcessWatchDog()
        {
            if (_processWatchDog == null)
                _processWatchDog = new ProcessWatchDog();
            _processWatchDog.StartWatchDog();
        }

        /// <summary>
        /// Disables the process checker if enabled
        /// </summary>
        public static void DisableProcessWatchDog()
        {
            if (_processWatchDog != null)
            {
                _processWatchDog.StopWatchDog();
                _processWatchDog = null;
            }
        }

        #endregion

        #region Process Extensions

        public static string GetExecutableName(this Process process)
        {
            try
            {
                //This makes the method kind of obsolete but maybe in the future another method is appropriate to get the exact executable name instead of the process name
                return process.ProcessName;
            }
            catch (Exception)
            {
                Logger.AddWarning("Unable to GetExecutableName of process", null);
                return "";
            }
        }

        public static IEnumerable<KeyValuePair<IntPtr, string>> GetOpenWindows(this Process process)
        {
            return SEBWindowHandler.GetOpenWindows()
                .Where(oW => oW.Key.GetProcess().GetExecutableName() == process.GetExecutableName());
        }

		public static bool HasDifferentOriginalName(this Process process, out string originalName)
		{
			var query = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + process.Id;

			originalName = string.Empty;

			using (var searcher = new ManagementObjectSearcher(query))
			using (var results = searcher.Get())
			{
				var processData = results.Cast<ManagementObject>().FirstOrDefault(p => Convert.ToInt32(p["ProcessId"]) == process.Id);

				if (processData != null)
				{
					var executablePath = processData["ExecutablePath"] as string;

					if (!String.IsNullOrEmpty(executablePath))
					{
						var processName = process.GetExecutableName();
						var executableInfo = FileVersionInfo.GetVersionInfo(executablePath);

						originalName = Path.GetFileNameWithoutExtension(executableInfo.OriginalFilename);

						if (!String.IsNullOrWhiteSpace(originalName) && !processName.Equals(originalName, StringComparison.InvariantCultureIgnoreCase))
						{
							Logger.AddInformation(String.Format("Process '{0}' has been renamed from '{1}' to '{2}'!", executablePath, originalName, processName));

							return true;
						}
					}
				}
			}

			return false;
		}

		#endregion

		#region Private Methods



		#endregion

		#region Screensaver & Sleep

		[FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            // Legacy flag, should not be used.
            // ES_USER_PRESENT   = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
        }

        public static void PreventSleep()
        {
            if (SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED) == 0) //Away mode for Windows >= Vista
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED); //Windows < Vista, forget away mode
            }
        }

        #endregion

        #region DLL Imports

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        #endregion
    }

    class ProcessWatchDog
    {
        private List<ProcessInfo> _processesToWatch = new List<ProcessInfo>();

        public ProcessWatchDog()
        {
        }

        public void StartWatchDog()
        {
            if (_processesToWatch.Count == 0)
            {
                foreach (var processName in SEBProcessHandler.ProhibitedExecutables)
                {
                    var processToWatch = new ProcessInfo(processName);
                    processToWatch.Started += ProcessStarted;
                    _processesToWatch.Add(processToWatch);
                }
            }
        }

        private void ProcessStarted(object sender, EventArgs e)
        {
            var processName = ((ProcessInfo) sender).ProcessName;
            foreach (var process in Process.GetProcesses().Where(p => processName.Contains(p.ProcessName)))
            {
                if (!SEBNotAllowedProcessController.CloseProcess(process))
                {
                    ShowMessageOrPasswordDialog(processName);
                }
            }
        }

        public void StopWatchDog()
        {
            foreach(var processInfo in _processesToWatch)
                processInfo.Dispose();

            _processesToWatch.Clear();
        }

        private void ShowMessageOrPasswordDialog(string processName)
        {
            var quitPassword = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyHashedQuitPassword)[SEBSettings.KeyHashedQuitPassword];
            if (!string.IsNullOrEmpty(quitPassword))
            {
                ShowPasswordDialog(processName, quitPassword);
            }
            else
            {
                SEBMessageBox.Show("Prohibited Process detected",
                            "SEB detected a prohibited process which it was unable to exit: " + processName,
                            MessageBoxIcon.Error, MessageBoxButtons.OK);
            }
        }

        private void ShowPasswordDialog(string processName, string quitPassword)
        {
            var password = SebPasswordDialogForm.ShowPasswordDialogForm("Prohibited Process detected", "SEB detected a prohibited process which it was unable to exit, enter the quit password to continue: " + processName);

            //cancel button has been clicked
            if (password == null)
            {
                ShowPasswordDialog(processName, quitPassword);
            }

            var hashedPassword = SEBProtectionController.ComputePasswordHash(password);
            if (String.IsNullOrWhiteSpace(password) ||
                String.Compare(quitPassword, hashedPassword, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return;
            }
            else
            {
                ShowPasswordDialog(processName, quitPassword);
            }
        }
    }
}
