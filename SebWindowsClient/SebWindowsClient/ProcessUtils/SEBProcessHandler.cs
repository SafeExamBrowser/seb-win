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
        public static List<ExecutableInfo> ProhibitedExecutables = new List<ExecutableInfo>();

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

        public static bool HasOriginalName(this Process process, out string originalName)
        {
            var query = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + process.Id;

            originalName = string.Empty;

            try
            {
                var regularProcessName = process.ProcessName ?? "<NULL>";

                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    var processData = results.Cast<ManagementObject>().FirstOrDefault(p => Convert.ToInt32(p["ProcessId"]) == process.Id);

                    if (processData != null)
                    {
                        var executablePath = processData["ExecutablePath"] as string;

                        if (!String.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                        {
                            var processName = process.GetExecutableName();
                            var executableInfo = FileVersionInfo.GetVersionInfo(executablePath);

                            originalName = Path.GetFileNameWithoutExtension(executableInfo.OriginalFilename);

                            if (!String.IsNullOrWhiteSpace(originalName) && !processName.Equals(originalName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Logger.AddInformation(String.Format("Process '{0}' has been renamed from '{1}' to '{2}'!", executablePath, originalName, processName));
                            }

                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.AddError(String.Format("Failed to retrieve the original name of process '{0}'!", process.ProcessName ?? "<NULL>"), null, e, e.Message);
            }

            return false;
        }

        public static IList<ExecutableInfo> GetExecutableInfos()
        {
            var infos = new List<ExecutableInfo>();
            var query = "SELECT ProcessId, Name, ExecutablePath FROM Win32_Process";

            try
            {
                Logger.AddInformation(String.Format("Trying to retrieve executable infos for all running processes"));

                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    Logger.AddInformation(String.Format("Got executable infos for all running processes"));

                    var processes = results.Cast<ManagementObject>().ToList();

                    foreach (var processData in processes)
                    {
                        var id = Convert.ToInt32(processData["ProcessId"]);
                        var name = Path.GetFileNameWithoutExtension(processData["Name"] as string);
                        var executablePath = processData["ExecutablePath"] as string;
                        string originalName = null;

                        if (!String.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                        {
                            var executableInfo = FileVersionInfo.GetVersionInfo(executablePath);

                            originalName = Path.GetFileNameWithoutExtension(executableInfo.OriginalFilename);

                            if (!String.IsNullOrWhiteSpace(originalName) && !name.Equals(originalName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Logger.AddInformation(String.Format("Process '{0}' has been renamed from '{1}' to '{2}'!", executablePath, originalName, name));
                            }
                        }

                        infos.Add(new ExecutableInfo(name, originalName, id));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.AddError("Failed to retrieve executable infos!", null, e, e.Message);
            }

            return infos;
        }

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
		private bool stopped;
        private System.Timers.Timer checkRunningProcessesTimer;
        private List<RunningProcess> processes;

        public ProcessWatchDog()
        {
			processes = Process.GetProcesses().Select(p => new RunningProcess { Id = p.Id, Name = p.ProcessName }).ToList();
            checkRunningProcessesTimer = new System.Timers.Timer()
            {
                Interval = 5000,
                AutoReset = false,
                Enabled = false
            };
        }

		private void CheckRunningProcessesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (stopped)
			{
				return;
			}

			var running = Process.GetProcesses();
			var terminated = processes.Where(p => running.All(r => r.Id != p.Id)).ToList();
			var started = running.Where(p => processes.All(r => r.Id != p.Id)).ToList();

			foreach (var process in terminated)
			{
				processes.Remove(process);
				Logger.AddInformation($"Process '{process.Name}' (PID = {process.Id}) has been terminated.");
			}

			foreach (var process in started)
			{
				process.HasOriginalName(out string originalName);

				if (IsExplorerProcess(process, originalName))
				{
					ExplorerStarted();
				}
				else if (IsProhibtedProcess(process, originalName))
				{
					KillProhibitedProcess(process, originalName);
				}
				else if (IsNewFirefoxProcess(process, originalName))
				{
					KillNewFirefoxProcess(process);
				}
				else
				{
					processes.Add(new RunningProcess { Id = process.Id, Name = process.ProcessName });
					Logger.AddInformation($"Process '{process.ProcessName}' (PID = {process.Id}) has been started.");
				}
			}

			checkRunningProcessesTimer.Enabled = true;
		}

		private bool IsExplorerProcess(Process process, string originalName)
		{
			return "explorer.exe".Equals(process.ProcessName, StringComparison.InvariantCultureIgnoreCase)
				|| "explorer.exe".Equals(originalName, StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsProhibtedProcess(Process process, string originalName)
		{
			return SEBProcessHandler.ProhibitedExecutables.Any(p => p.Name.ToLower() == process.ProcessName.ToLower() || p.OriginalName.ToLower() == originalName.ToLower());
		}

		private bool IsNewFirefoxProcess(Process process, string originalName)
		{
			var isNewInstance = false;

			isNewInstance |= "firefox".Equals(process.ProcessName, StringComparison.InvariantCultureIgnoreCase);
			isNewInstance |= "firefox".Equals(originalName, StringComparison.InvariantCultureIgnoreCase);
			isNewInstance &= process.Id != SEBClientInfo.SebWindowsClientForm?.xulRunner?.Id;

			return isNewInstance;
		}

		private void KillNewFirefoxProcess(Process process)
		{
			try
			{
				Logger.AddWarning($"Detected new Firefox process (PID = {process.Id})! Trying to close process...");

				process.CloseMainWindow();
				process.WaitForExit(100);
				process.Refresh();

				for (var attempt = 0; attempt < 50 && !process.HasExited; attempt++)
				{
					Logger.AddWarning($"Failed to terminate Firefox process (PID = {process.Id}) within 100ms! Trying again...");
					process.Kill();
					process.WaitForExit(100);
					process.Refresh();
				}

				if (process.HasExited)
				{
					Logger.AddInformation($"Successfully terminated new Firefox process (PID = {process.Id}).");
				}
				else
				{
					Logger.AddWarning($"Failed to terminate Firefox process (PID = {process.Id}). Showing password dialog...");
					ShowMessageOrPasswordDialog(process.ProcessName);
				}
			}
			catch (Exception e)
			{
				Logger.AddError("Unexpected error while trying to terminate new Firefox process!", null, e);
			}
		}

		private void KillProhibitedProcess(Process process, string originalName)
        {
			Logger.AddWarning($"Prohibited process '{process.ProcessName}' (PID = {process.Id}) has been started!");

			if (!SEBNotAllowedProcessController.CloseProcess(process))
            {
                ShowMessageOrPasswordDialog(String.Format("{0} [OriginalName: {1}]", process.ProcessName, originalName));
            }
            
        }

        public void StartWatchDog()
        {
			checkRunningProcessesTimer.Elapsed += CheckRunningProcessesTimer_Elapsed;
			checkRunningProcessesTimer.Enabled = true;
        }

        private void ExplorerStarted()
        {
            Logger.AddWarning("Windows explorer has been restarted!", this);

            var success = SEBProcessHandler.KillExplorerShell();

            if (success)
            {
                Logger.AddInformation("Successfully terminated Windows explorer.", this);
            }
            else
            {
                Logger.AddError("Failed to terminate Windows explorer!", this, null);
            }
        }

        public void StopWatchDog()
        {
			stopped = true;
			checkRunningProcessesTimer.Enabled = false;
			checkRunningProcessesTimer.Elapsed -= CheckRunningProcessesTimer_Elapsed;
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
                SEBMessageBox.Show(SEBUIStrings.prohibitedProcessDetectedTitle,
                            SEBUIStrings.prohibitedProcessDetectedText + processName,
                            MessageBoxIcon.Error, MessageBoxButtons.OK);
            }
        }

        private void ShowPasswordDialog(string processName, string quitPassword)
        {
            var password = SebPasswordDialogForm.ShowPasswordDialogForm(SEBUIStrings.prohibitedProcessDetectedTitle, SEBUIStrings.prohibitedProcessDetectedQuitPassword + processName);

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

		private class RunningProcess
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}
    }
}
