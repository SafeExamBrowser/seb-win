//
//  SEBWindowsClientForm.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SebWindowsClient.AudioUtils;
using SebWindowsClient.BlockShortcutsUtils;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DesktopUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.ServiceUtils;
using SebWindowsClient.UI;
using SebWindowsClient.XULRunnerCommunication;
using DictObj = System.Collections.Generic.Dictionary<string, object>;
using ListObj = System.Collections.Generic.List<object>;


namespace SebWindowsClient
{


	public partial class SebWindowsClientForm : Form
	{
		private static bool isStartup = true;

		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32.dll")]
		static extern IntPtr GetDesktopWindow();

		[DllImportAttribute("User32.dll")]
		private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

		private delegate bool EnumThreadProc(IntPtr hwnd, IntPtr lParam);

		private int taskbarHeight = 0;

		public bool closeSebClient = true;
		public string sebPassword = null;

		private SebCloseDialogForm sebCloseDialogForm;
		private SebApplicationChooserForm sebApplicationChooserForm;

		public Process xulRunner = new Process();
		private int xulRunnerExitCode;
        private IntPtr xulRunnerWindowHandle = IntPtr.Zero;

		public List<string> permittedProcessesCalls = new List<string>();
		public List<Process> permittedProcessesReferences = new List<Process>();
		public List<Image> permittedProcessesIconImages = new List<Image>();

		private IDictionary<string, IList<Process>> runningApplicationsToClose = new Dictionary<string, IList<Process>>();

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor - initialise components.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public SebWindowsClientForm()
		{
			InitializeComponent();

			SEBXULRunnerWebSocketServer.OnXulRunnerClearClipboard += OnXulRunnerClearClipboard;
			SEBXULRunnerWebSocketServer.OnXulRunnerCloseRequested += OnXULRunnerShutdDownRequested;
			SEBXULRunnerWebSocketServer.OnXulRunnerQuitLinkClicked += OnXulRunnerQuitLinkPressed;
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
			Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

			try
			{
				SEBProcessHandler.PreventSleep();
			}
			catch (Exception ex)
			{
				Logger.AddError("Unable to PreventSleep", null, ex);
			}
		}

		private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
		{
			PlaceFormOnDesktop(TapTipHandler.IsKeyboardVisible());
		}

		private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
		{
			Logger.AddWarning($"Detected session switch event: {e.Reason}");
			SEBXULRunnerWebSocketServer.SendUserSwitchLockScreen();
		}

		private void OnXulRunnerClearClipboard(object sender, EventArgs e)
		{
			SEBClipboard.CleanClipboard();
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
						Logger.AddInformation("SEB client configuration set in LoadFile(URI).", null, null);
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

                // Check if settings forbid to open SEB Config Files
                if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyDownloadAndOpenSebConfig) == false)
                {
                    SEBMessageBox.Show(SEBUIStrings.cannotOpenSEBConfig, SEBUIStrings.cannotOpenSEBConfigMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
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
								using (myWebClient)
								{
									sebSettings = myWebClient.DownloadData(uri.ToString().Replace("seb://", "http://"));
								}
								if (sebSettings == null)
								{
									Logger.AddError(
										"Downloading .seb settings by http failed, try to download by https", null, null);
								}
							}
							if (sebSettings == null)
							{
								// Download by https
								Logger.AddError("Downloading .seb settings by https", null, null);
								using (myWebClient)
								{
									sebSettings =
										myWebClient.DownloadData(
											uri.ToString().Replace("seb://", "https://").Replace("sebs://", "https://"));
								}
							}
							Logger.AddInformation(
								String.Format("File downloaded from {0}, checking if it's a valid seb file", uri));
							if (ReconfigureWithSettings(sebSettings, true))
							{
								Logger.AddInformation("Succesfully read the new configuration, length is " + sebSettings.Length);

								return true;
							}
							else
							{
								Logger.AddInformation("ReconfigureWithSettings for SEB-link returned false, this means the user canceled when entering the password, didn't enter a right one after 5 attempts or new settings were corrupted, exiting");
								Logger.AddError("Settings could not be decrypted or stored.", this, null, null);

								return false;
							}
						}
						catch (WebException wex)
						{
							if (wex.Response is HttpWebResponse &&
								(wex.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
							{
								Logger.AddInformation("Authorization required for download seb file");
							}
							else
							{
								SEBMessageBox.Show(SEBUIStrings.cannotOpenSEBLink, SEBUIStrings.cannotOpenSEBLinkMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
								//MessageBox.Show(new Form() { TopMost = true }, "Unable to follow the link!");
								Logger.AddError("Unable to download a file from the " + file + " link", this, wex);
							}
						}
						catch (Exception ex)
						{
							SEBMessageBox.Show(SEBUIStrings.cannotOpenSEBLink, SEBUIStrings.cannotOpenSEBLinkMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
							//MessageBox.Show(new Form() { TopMost = true }, "Unable to follow the link!");
							Logger.AddError("Unable to download a file from the "+ file + " link", this, ex);
						}

						
						//sebSettings seems to be some other content. There may be an authentication necessary to download the file, redirect the user to the website
						Logger.AddInformation("The downloaded content is not a seb file, there may be an authentication necessary to get the file.");
						SEBSettings.settingsCurrent[SEBSettings.KeyStartURL] = uri.ToString();
						//Start SEB Normally
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
						return true;
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

				if (!ReconfigureWithSettings(sebSettings, fromFile: true))
				{
					Logger.AddInformation("ReconfigureWithSettings returned false, this means the user canceled when entering the password, didn't enter a right one after 5 attempts or new settings were corrupted, exiting");
					Logger.AddError("Settings could not be decrypted or stored.", this, null, null);

					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Decrypt, parse and store new settings and restart SEB if this was successfull
		/// </summary>
		/// <param name="sebSettings"></param>
		/// <returns></returns>
		public bool ReconfigureWithSettings(byte[] sebSettings, bool suppressFileFormatError = false, bool fromFile = false)
		{
			var reconfigure = new Func<bool>(() =>
			{
				var wasStartup = isStartup;

				Logger.AddInformation("Attempting to StoreDecryptedSEBSettings");
				if (!SEBConfigFileManager.StoreDecryptedSEBSettings(sebSettings, suppressFileFormatError))
				{
					Logger.AddInformation("StoreDecryptedSettings returned false, this means the user canceled when entering the password, didn't enter a right one after 5 attempts or new settings were corrupted, exiting");
					Logger.AddError("Settings could not be decrypted or stored.", this, null, null);
					SebWindowsClientMain.LoadingSebFile(false);
					return false;
				}

				//Show splashscreen
				//var splashThread = new Thread(SEBSplashScreen.StartSplash);

				if (!SEBXULRunnerWebSocketServer.Started)
				{
					Logger.AddInformation("SEBXULRunnerWebSocketServer.Started returned false, this means the WebSocketServer communicating with the SEB XULRunner browser couldn't be started, exiting");
					SEBMessageBox.Show(SEBUIStrings.webSocketServerNotStarted, SEBUIStrings.webSocketServerNotStartedMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
					ExitApplication();
					return false;
				}
                //SEBSplashScreen.CloseSplash();

                // Convert new URL Filter rules to XUL seb2 rules
                // and add Start URL to allowed rules
                SEBURLFilter urlFilter = new SEBURLFilter();
                urlFilter.UpdateFilterRules();

                if (fromFile && !wasStartup)
				{
					var xulRunnerSettings = DeepClone(SEBSettings.settingsCurrent);
					var xulRunnerParameters = SEBXulRunnerSettings.XULRunnerConfigDictionarySerialize(xulRunnerSettings);

					SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.Reconfigure, new { configBase64 = xulRunnerParameters }));
				}

				Logger.AddInformation("Successfully StoreDecryptedSEBSettings");
				SebWindowsClientMain.LoadingSebFile(false);

				return true;
			});

			if (InvokeRequired)
			{
				return (bool) Invoke(reconfigure);
			}
			else
			{
				return reconfigure();
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
        /// Show an alert that reconfiguring SEB isn't allowed, because running in exam mode
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public static void ShowReconfigureNotAllowed()
        {
            SEBMessageBox.Show(SEBUIStrings.loadingSettingsNotAllowed, SEBUIStrings.loadingSettingsNotAllowedReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
        }


        private static T DeepClone<T>(T obj)
		{
			using (var ms = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(ms, obj);
				ms.Position = 0;

				return (T)formatter.Deserialize(ms);
			}
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		// Start xulRunner process.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private bool StartXulRunner(string userDefinedArguments)
		{
			HandleXulRunnerProfile();

			//StartXulRunnerWithSilentParameter();

			string xulRunnerPath = "";
			string desktopName = "";
			if (userDefinedArguments == null) userDefinedArguments="";
			try
			{
                // Convert new URL Filter rules to XUL seb2 rules
                // and add Start URL to allowed rules
                SEBURLFilter urlFilter = new SEBURLFilter();
                urlFilter.UpdateFilterRules();

                // Create JSON object with XULRunner parameters to pass to firefox.exe as base64 string
                var xulRunnerSettings = DeepClone(SEBSettings.settingsCurrent);
				string XULRunnerParameters = SEBXulRunnerSettings.XULRunnerConfigDictionarySerialize(xulRunnerSettings);
				// Create the path to firefox.exe plus all arguments
				StringBuilder xulRunnerPathBuilder = new StringBuilder(SEBClientInfo.XulRunnerExePath);
				// Create all arguments, including user defined
				StringBuilder xulRunnerArgumentsBuilder = new StringBuilder(" -app \"").Append(Application.StartupPath).Append("\\").Append(SEBClientInfo.XulRunnerSebIniPath).Append("\"");
				// Check if there is a user defined -profile parameter, otherwise use the standard one 
				if (!(userDefinedArguments.ToLower()).Contains("-profile"))
				{
					xulRunnerArgumentsBuilder.Append(" -profile \"").Append(SEBClientInfo.SebClientSettingsAppDataDirectory).Append("Profiles\"");
				}

				if (!(userDefinedArguments.ToLower()).Contains("-dictionaries"))
				{
					xulRunnerArgumentsBuilder.Append($@" -dictionaries ""{SEBClientInfo.XulRunnerAdditionalDictionariesDirectory}""");
				}

				// If logging is enabled in settings and there is no custom xulrunner -logfile argument 
				if (!userDefinedArguments.ToLower().Contains("-logpath") && (bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableLogging))
				{
					string logDirectory = (string)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyLogDirectoryWin);
					if (String.IsNullOrEmpty(logDirectory))
					{
						// When there is no directory indicated, we use the placeholder for telling xulrunner to use the AppData directory to store the log
						xulRunnerArgumentsBuilder.Append(" -logfile 1 -logpath \"").Append(SEBClientInfo.SebClientSettingsAppDataDirectory).Append("\\seb.log\"");
					}
					else
					{
						logDirectory = Environment.ExpandEnvironmentVariables(logDirectory);
						xulRunnerArgumentsBuilder.Append(" -logfile 1 -logpath \"").Append(logDirectory).Append("\\seb.log\"");
					}

					if (!(userDefinedArguments.ToLower()).Contains("-debug"))
					{
						xulRunnerArgumentsBuilder.Append(" -debug 1");
					}
				}
				xulRunnerArgumentsBuilder.Append(" ").Append(Environment.ExpandEnvironmentVariables(userDefinedArguments)).Append(" –purgecaches -config \"").Append(XULRunnerParameters).Append("\"");
				string xulRunnerArguments = xulRunnerArgumentsBuilder.ToString();
				xulRunnerPathBuilder.Append(xulRunnerArguments);
				xulRunnerPath = xulRunnerPathBuilder.ToString();

				desktopName = SEBClientInfo.DesktopName;
				xulRunner = SEBDesktopController.CreateProcess(xulRunnerPath, desktopName);
				xulRunner.EnableRaisingEvents = true;
				xulRunner.Exited += XulRunner_Exited;

				SaveXulRunnerWindowHandle();

				return true;

			}
			catch (Exception ex)
			{
				Logger.AddError("An error occurred starting XULRunner, path: "+xulRunnerPath+" desktop name: "+desktopName+" ", this, ex, ex.Message);
				return false;
			}
		}

		private void SaveXulRunnerWindowHandle()
		{
			var timer = new System.Timers.Timer();

			timer.Interval = 500;
			timer.AutoReset = true;
			timer.Elapsed += (o, args) =>
			{
				if (!xulRunner.HasExited)
				{
					var windows = SEBWindowHandler.GetWindowsByThread(xulRunner.Threads[0].Id);

					if (windows.Any())
					{
						xulRunnerWindowHandle = windows.First();
						timer.Stop();

						Logger.AddInformation("Found handle to main browser window: " + xulRunnerWindowHandle);
					}
				}
				else
				{
					timer.Stop();
				}
			};

			timer.Start();
		}

        public void ClosePreviousMainWindow()
        {
			Logger.AddInformation("Attempting to close previous browser window(s)...");

			try
			{
				xulRunner.Refresh();

				if (xulRunner.MainWindowHandle != IntPtr.Zero)
				{
					var windows = SEBWindowHandler.GetWindowsByThread(xulRunner.Threads[0].Id).Where(w => w != xulRunner.MainWindowHandle).ToList();

					Logger.AddInformation("Handle of new main browser window found: " + xulRunner.MainWindowHandle);
					Logger.AddInformation("Sending close message to other window(s): " + String.Join(", ", windows));

					foreach (var window in windows)
					{
						SEBWindowHandler.CloseWindow(window);
					}
				}
				else
				{
					var windows = SEBWindowHandler.GetWindowsByThread(xulRunner.Threads[0].Id);

					Logger.AddInformation("No window handle for browser process available! Falling back to manually retrieved handle...");
					Logger.AddInformation("Open browser windows: " + String.Join(", ", windows));
					Logger.AddInformation("Sending close message to old main browser window: " + xulRunnerWindowHandle);

					SEBWindowHandler.CloseWindow(xulRunnerWindowHandle);
					SaveXulRunnerWindowHandle();
				}
			}
			catch (Exception e)
			{
				Logger.AddError("Failed to close the previous browser window!", this, e);
			}
		}

        private void HandleXulRunnerProfile()
		{
			Logger.AddInformation("Attempting to handle Firefox profile folder...");

			try
			{
				var xulRunnerProfileFolder = string.Format(@"{0}Profiles\", SEBClientInfo.SebClientSettingsAppDataDirectory);
				var versionFile = SEBClientInfo.SebClientSettingsAppDataDirectory + "SEBVersion";
				var preferencesFile = Path.Combine(xulRunnerProfileFolder, "prefs.js");
				var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				var previousVersion = string.Empty;

				Logger.AddInformation("Firefox profile folder: " + xulRunnerProfileFolder);

				if (File.Exists(preferencesFile))
				{
					File.Delete(preferencesFile);
					Logger.AddInformation($"Deleted Firefox preferences ({preferencesFile}).");
				}

				//If it's not a new version of SEB, skip this
				if (File.Exists(versionFile) && (previousVersion = File.ReadAllText(versionFile)) == currentVersion)
				{
					Logger.AddInformation("Currently running version of SEB is equal to previously running version, no profile deletion necessary. Version: " + currentVersion);

					return;
				}

				Logger.AddInformation(String.Format("Currently running version ({0}) is different from previous version ({1}). Trying to delete profile folder...", currentVersion, previousVersion));

				//Delete the old profile directory if it exists
				if (Directory.Exists(xulRunnerProfileFolder))
				{
					DeleteDirectory(xulRunnerProfileFolder);
				}

				Logger.AddInformation("Successfully deleted old Firefox profile folder.");

				//Create the profile directory
				Directory.CreateDirectory(xulRunnerProfileFolder);
				Logger.AddInformation("Successfully created empty Firefox profile folder.");

				//Write the version file
				File.WriteAllText(versionFile, currentVersion);
				Logger.AddInformation("Successfully saved current SEB version to " + versionFile);
			}
			catch (Exception ex)
			{
				Logger.AddError("Could not check or delete old Firefox profile folder: ", this, ex, ex.Message);
			}
		}

		/// <summary>
		/// Attempt to fix the issue happening when deleting the Firefox profile directory (see SEBWIN-135).
		/// Source: https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/1703799#1703799
		/// </summary>
		private static void DeleteDirectory(string path)
		{
			foreach (string directory in Directory.GetDirectories(path))
			{
				DeleteDirectory(directory);
			}

			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException e)
			{
				Logger.AddWarning(String.Format("Failed to delete {0} with IOException: {1}", path, e.Message), null);
				Thread.Sleep(100);
				Directory.Delete(path, true);
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.AddWarning(String.Format("Failed to delete {0} with UnauthorizedAccessException: {1}", path, e.Message), null);
				Thread.Sleep(100);
				Directory.Delete(path, true);
			}
		}

		private void StartXulRunnerWithSilentParameter()
		{
			string path = string.Format("{0} -silent -profile \"{1}Profiles\"", SEBClientInfo.XulRunnerExePath, SEBClientInfo.SebClientSettingsAppDataDirectory);
			xulRunner = SEBDesktopController.CreateProcess(path, SEBClientInfo.DesktopName);
			while (!xulRunner.HasExited)
			{
				Thread.Sleep(100);
			}
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Handle xulRunner_Exited event and display process information.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private void XulRunner_Exited(object sender, System.EventArgs e)
		{
			Logger.AddInformation("XULRunner exit event fired.");

			//xulRunnerExitEventHandled = true;
			// Is the handle for the XULRunner process valid?
			if (xulRunner != null)
			{
				try
				{
					// Read the exit code
					xulRunnerExitCode = xulRunner.ExitCode;
				}
				catch (Exception ex)
				{
					xulRunnerExitCode = -1;
					// An error occured when reading exit code, probably XULRunner didn't actually exit yet
					Logger.AddError("Error reading XULRunner exit code!", null, ex);
				}
			}
			else
			{
				// The XULRunner process didn't exist anymore
				xulRunnerExitCode = 0;
			}
			if (xulRunnerExitCode != 0)
			{
				// An error occured when exiting XULRunner, maybe it crashed?
				Logger.AddInformation("An error occurred when exiting XULRunner. Exit code: " + xulRunnerExitCode.ToString());
			}
			else
			{
				// If the flag for closing SEB is set, we exit
				if (SEBClientInfo.SebWindowsClientForm.closeSebClient)
				{
					Logger.AddInformation("XULRunner was closed, SEB will exit now.");
					Invoke(new Action(() => ExitApplication()));
				}
			}

		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Add permitted process names and icons to the SEB taskbar (ToolStrip control) 
		/// and start permitted processes which have the autostart option set 
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private void addPermittedProcessesToTS(bool reconfiguring = false)
		{
			// First clear the permitted processes toolstrip/lists in case of a SEB restart

			var start = 0;
			if(!SEBXULRunnerWebSocketServer.HasBeenReconfiguredByMessage && !reconfiguring)
			{
				taskbarToolStrip.Items.Clear();
				permittedProcessesCalls.Clear();
				permittedProcessesReferences.Clear();
				permittedProcessesIconImages.Clear();
            }
			else
			{
				var tS = taskbarToolStrip.Items[0];
				var cpPC = permittedProcessesCalls[0];
				var cpPR = permittedProcessesReferences[0];
				var cpPII = permittedProcessesIconImages[0];
				taskbarToolStrip.Items.Clear();
				permittedProcessesCalls.Clear();
				permittedProcessesReferences.Clear();
				permittedProcessesIconImages.Clear();
				taskbarToolStrip.Items.Add(tS);
				permittedProcessesCalls.Add(cpPC);
				permittedProcessesReferences.Add(cpPR);
				permittedProcessesIconImages.Add(cpPII);
				start = 1;
			}

			List<object> permittedProcessList = (List<object>)SEBClientInfo.getSebSetting(SEBSettings.KeyPermittedProcesses)[SEBSettings.KeyPermittedProcesses];
			if (permittedProcessList.Count > 0)
			{
				// Check if permitted third party applications are already running
				List<Process> runningApplications = new List<Process>();
				runningApplications = Process.GetProcesses().ToList();
				//Process[] runningApplications = Process.GetProcesses();
				for (int i = start; i < permittedProcessList.Count; i++)
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
                        string originalName = ((string)permittedProcess[SEBSettings.KeyOriginalName]).ToLower();
                        if (String.IsNullOrEmpty(title)) title = executable;
						string identifier = ((string)permittedProcess[SEBSettings.KeyIdentifier]).ToLower();
                        if (!(executable.Contains(SEBClientInfo.XUL_RUNNER) && 
                            !(bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser)) && 
                            !(title.Equals(SEBClientInfo.SEB_SHORTNAME) && 
                            (executable.Equals("xulrunner.exe") || originalName.Equals("xulrunner.exe"))))
						{
							// Check if the process is already running
							//runningApplications = Process.GetProcesses();
							int j = start;
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
										if (!String.IsNullOrEmpty(identifier) && proc != null && !proc.HasExited && proc.MainWindowHandle == IntPtr.Zero)
										{
											//Get Process from WindowHandle by name if we have an identifier
											var handle = SEBWindowHandler.GetWindowHandleByTitle(identifier);

											if (handle != IntPtr.Zero)
											{
												proc = handle.GetProcess();
											}
										}

										if (reconfiguring && !isStartup && name == SEBClientInfo.XUL_RUNNER)
										{
											runningApplications.RemoveAt(j);

											continue;
										}

										// If the flag strongKill is set, then the process is killed without asking the user
										bool strongKill = (bool)SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyStrongKill);
										if (strongKill && SEBNotAllowedProcessController.CloseProcess(proc))
										{
											// Remove the process from the list of running processes
											runningApplications.RemoveAt(j);
										}
										else
										{
											title = title == SEBClientInfo.SEB_SHORTNAME ? (string) permittedProcess[SEBSettings.KeyExecutable] : title;

											if (!runningApplicationsToClose.ContainsKey(title))
											{
												runningApplicationsToClose[title] = new List<Process>();
											}

											runningApplicationsToClose[title].Add(proc);
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
			if (runningApplicationsToClose.Count > 0)
			{
				StringBuilder applicationsListToClose = new StringBuilder();
				foreach (string applicationToClose in runningApplicationsToClose.Keys)
				{
					applicationsListToClose.AppendLine("    " + applicationToClose);
					Logger.AddWarning("Found application which needs to be closed: " + applicationToClose);
				}
				if (SEBMessageBox.Show(SEBUIStrings.closeProcesses, SEBUIStrings.closeProcessesQuestion + "\n\n" + applicationsListToClose.ToString(), MessageBoxIcon.Error, MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					var closedApplications = new List<string>();

					foreach (var application in runningApplicationsToClose.Keys)
					{
						var closedProcesses = 0;

						foreach (var process in runningApplicationsToClose[application])
						{
							if (process.HasExited || SEBNotAllowedProcessController.CloseProcess(process))
							{
								closedProcesses++;
							}
						}

						if (runningApplicationsToClose[application].Count == closedProcesses)
						{
							closedApplications.Add(application);
							Logger.AddInformation("Successfully closed application: " + application);
						}
						else
						{
							Logger.AddWarning("Failed to close application: " + application);
						}
					}

					foreach (var application in closedApplications)
					{
						runningApplicationsToClose.Remove(application);
					}

					if (runningApplicationsToClose.Any())
					{
						SEBMessageBox.Show(SEBUIStrings.unableToCloseProcessesTitle,
							SEBUIStrings.unableToCloseProcessesText + "\n" + String.Join("\n", runningApplicationsToClose.Keys),
							MessageBoxIcon.Error, MessageBoxButtons.OK);

						Logger.AddWarning("Failed to close all running applications!");

						ExitApplication();
						return;
					}

					runningApplicationsToClose.Clear();
				}
				else
				{
					Logger.AddInformation("User aborted when prompted to close all running applications!");

					ExitApplication();
					return;
				}
			}            

			// So if there are any permitted processes, we add them to the SEB taskbar
			if (permittedProcessList.Count > 0)
			{
				for (int i = start; i < permittedProcessList.Count; i++)
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
                        string originalName = ((string)permittedProcess[SEBSettings.KeyOriginalName]).ToLower();
                        if (String.IsNullOrEmpty(title)) title = executable;
                        // Do not add XULRunner to taskbar if browser is disabled and SEB if xulrunner.exe is executable or original name (settings generated in SEB 2.1.x)
                        if (!(executable.Contains(SEBClientInfo.XUL_RUNNER) && 
                            !(bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser)) && 
                            !(title.Equals(SEBClientInfo.SEB_SHORTNAME) &&
                            (executable.Equals("xulrunner.exe") || originalName.Equals("xulrunner.exe"))))

						{
                            var toolStripButton = new SEBToolStripButton();

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
								try
								{
									processImage = Iconextractor.ExtractHighResIconImage(fullPath, taskbarHeight - 8);
								}
								catch (Exception)
								{
									Logger.AddError("Could not extract icon of file: " + fullPath, null, null);
								}
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
											var value = Environment.ExpandEnvironmentVariables((string)argument[SEBSettings.KeyArgument]);

											startProcessNameBuilder.Append(" ").Append(value);
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
								SEBMessageBox.Show(SEBUIStrings.permittedApplicationNotFound, SEBUIStrings.permittedApplicationNotFoundMessage.Replace("%s", title), MessageBoxIcon.Error, MessageBoxButtons.OK);
							}
						}
						else
						{
							// Permitted application is Firefox: Set its call entry to null
							permittedProcessesCalls.Add(null);
						}
					}
				}
			}

			//Filling System Icons

			//Additional Resources Icons
			FileCompressor.CleanupTempDirectory();
			foreach (DictObj l0resource in ((ListObj) SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources]))
			{
				var active = l0resource.TryGetValue(SEBSettings.KeyAdditionalResourcesActive, out object a) && a is true;
				var show = l0resource.TryGetValue(SEBSettings.KeyAdditionalResourcesShowButton, out object s) && s is true;

				if (active && show)
				{
					taskbarToolStrip.Items.Add(new SEBAdditionalResourcesToolStripButton(l0resource));
				}
			}

			//QuitButton
			if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAllowQuit])
			{
				var quitButton = new SEBQuitToolStripButton();
				quitButton.Click += (x, y) => ShowCloseDialogForm();
				taskbarToolStrip.Items.Add(quitButton);
			}

			//Audio Control
			try
			{
				if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAudioSetVolumeLevel])
				{
					int volume = (int)SEBSettings.settingsCurrent[SEBSettings.KeyAudioVolumeLevel];
					new AudioControl().SetVolumeScalar((float)volume / 100);
				}
                new AudioControl().Mute((bool)SEBSettings.settingsCurrent[SEBSettings.KeyAudioMute]);
                if ((bool)SEBClientInfo.getSebSetting(SEBSettings.KeyAudioControlEnabled)[SEBSettings.KeyAudioControlEnabled])
				{
					taskbarToolStrip.Items.Add(new SEBAudioToolStripButton());
				}
			}
			catch (Exception ex)
			{
				Logger.AddError("Unable to add AudioControl", this, ex);
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


			// Add the OnScreenKeyboardControl (only if not in Create New Desktop Mode)
			if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] == true && !(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
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

			//Add the ReloadBrowserButton
			if ((bool)SEBClientInfo.getSebSetting(SEBSettings.KeyShowReloadButton)[SEBSettings.KeyShowReloadButton])
			{
				var button = new SEBReloadBrowserToolStripButton();

				button.Enabled = (bool)SEBSettings.settingsCurrent[SEBSettings.KeyBrowserWindowAllowReload] || (bool)SEBSettings.settingsCurrent[SEBSettings.KeyNewBrowserWindowAllowReload];

				taskbarToolStrip.Items.Add(button);
			}

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
			for (int i = start; i < permittedProcessList.Count; i++)
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
                            Logger.AddInformation("Permitted process to start automatically (autostart = true): " + executable);
                            string fullPathArgumentsCall = permittedProcessesCalls[permittedProcessesIndex];
                            if (fullPathArgumentsCall != null)
                            {
                                Logger.AddInformation("Adding permitted process to autostart with path/arguments " + fullPathArgumentsCall);
                                newProcess = CreateProcessWithExitHandler(fullPathArgumentsCall);
                            }
                            else
                            {
                                Logger.AddWarning("Permitted process wasn't added to autostart, because it didn't had a valid path/arguments call set", null);
                                newProcess = null;
                            }
						}
						// Save the process reference if the process was started, otherwise null
						permittedProcessesReferences.Add(newProcess);
					}
					else
					{
						if ((bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableSebBrowser) && isStartup)
						{
							// Start XULRunner
							StartXulRunner((string)permittedProcessesCalls[permittedProcessesIndex]);
							// Save the process reference of XULRunner
							permittedProcessesReferences.Add(xulRunner);
						}
					}
                    permittedProcessesIndex++;
                }
            }

			SEBToForeground();
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
			string originalName = (string) SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyOriginalName) ?? string.Empty;
			string executablePath = (string) SEBSettings.valueForDictionaryKey(permittedProcess, SEBSettings.KeyPath);
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
				return ThreadedDialog.ShowFileDialogForExecutable(executable, originalName);
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
		public Process CreateProcessWithExitHandler(string fullPathArgumentsCall)
		{
			Process newProcess = SEBDesktopController.CreateProcess(fullPathArgumentsCall, SEBClientInfo.DesktopName);
			//newProcess.EnableRaisingEvents = true;
			//newProcess.Exited += new EventHandler(permittedProcess_Exited);

			return newProcess;
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

			Logger.AddInformation("Taskbar height from settings: " +sebTaskBarHeight.ToString() + " Current taskbar height: " + taskbarHeight.ToString());

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

		private bool CheckProhibitedProcesses()
		{
			// Add prohibited processes to the "processes not permitted to run" list 
			// which will be dealt with after checking if permitted processes are already running;
			// the user will be asked to quit all those processes him/herself or to let SEB kill them
			// Prohibited processes with the strongKill flag set can be killed without user consent

			var prohibitedProcessList = (List<object>) SEBClientInfo.getSebSetting(SEBSettings.KeyProhibitedProcesses)[SEBSettings.KeyProhibitedProcesses];
			var infos = SEBProcessHandler.GetExecutableInfos();

			if (prohibitedProcessList.Any())
			{
				var runningApplications = Process.GetProcesses().Select(p =>
				{
					var executableInfo = infos.FirstOrDefault(i => i.ProcessId == p.Id);

					return new
					{
						Name = p.ProcessName,
						OriginalName = executableInfo?.OriginalName ?? string.Empty,
						HasOriginalName = executableInfo?.HasOriginalName ?? false,
						Process = p
					};
				}).ToList();

				runningApplicationsToClose.Clear();

				foreach (var prohibitedProcess in prohibitedProcessList.Cast<Dictionary<string, object>>().ToList())
				{
					var prohibitedProcessOS = (SEBSettings.operatingSystems)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyOS);
					var prohibitedProcessActive = (bool)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyActive);

					if (prohibitedProcessOS == SEBSettings.operatingSystems.operatingSystemWin && prohibitedProcessActive)
					{
						var title = (string)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyTitle);
						var executable = Path.GetFileNameWithoutExtension(prohibitedProcess[SEBSettings.KeyExecutable] as string ?? string.Empty);
						var originalName = Path.GetFileNameWithoutExtension(prohibitedProcess[SEBSettings.KeyOriginalName] as string ?? string.Empty);

						foreach (var application in runningApplications)
						{
							var isProhibited = false;

							isProhibited |= !String.IsNullOrWhiteSpace(application.Name) && executable.Equals(application.Name, StringComparison.InvariantCultureIgnoreCase);
							isProhibited |= !String.IsNullOrWhiteSpace(originalName) && application.HasOriginalName && originalName.Equals(application.OriginalName, StringComparison.InvariantCultureIgnoreCase);

							if (isProhibited)
							{
								// If the flag strongKill is set, then the process is killed without asking the user
								var strongKill = (bool)SEBSettings.valueForDictionaryKey(prohibitedProcess, SEBSettings.KeyStrongKill);

								if (strongKill)
								{
									SEBNotAllowedProcessController.CloseProcess(application.Process);
								}
								else
								{
									if (String.IsNullOrWhiteSpace(title))
									{
										title = executable;
									}

									if (!runningApplicationsToClose.Keys.Contains(title))
									{
										runningApplicationsToClose[title] = new List<Process>();
									}

									runningApplicationsToClose[title].Add(application.Process);
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
			sebApplicationChooserForm.BeginInvoke(new Action(() =>
			{
				sebApplicationChooserForm.fillListApplications();
				sebApplicationChooserForm.Visible = true;
			}));
			//sebCloseDialogForm.Activate();
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Show SEB Application Chooser Form.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void SelectNextListItem()
		{
			sebApplicationChooserForm.BeginInvoke(new Action(sebApplicationChooserForm.SelectNextListItem));
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
			sebApplicationChooserForm.BeginInvoke(new Action(() => sebApplicationChooserForm.Visible = false));
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
			if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyQuitURLConfirm] == false)
			{
				ExitApplication();
			}

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
		public bool OpenSEBForm(bool reconfiguring = false)
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
				this.Visible = false;
				this.Height = 1;
				this.Width = 1;
				this.Location = new System.Drawing.Point(-50000, -50000);

				taskbarHeight = 0;

				PlaceFormOnDesktop(false, true);
			}

			// Check if VM and SEB Windows Service available and required
			try
            {
                SebWindowsClientMain.CheckIfTabletModeIsEnabled();
                SebWindowsClientMain.CheckIfInsideVirtualMachine();
                SebWindowsClientMain.CheckIfRunViaRemoteConnection();
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
                    Logger.AddInformation("disabling windows update");
                    if (SebWindowsServiceHandler.IsServiceAvailable && !SebWindowsServiceHandler.DisableWindowsUpdate())
                        Logger.AddWarning("Unable to disable windows upate service", this, null);
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to disable windows update service", this, ex);
                }

                try
                {
                    Logger.AddInformation("killing processes that are not allowed to run");
                    var res = CheckProhibitedProcesses();
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to kill processes that are running before start", this, ex);
                }


                Logger.AddInformation("attempting to start socket server");
                SEBXULRunnerWebSocketServer.StartServer();
                SEBXULRunnerWebSocketServer.OnXulRunnerFullscreenchanged += opts =>
                {
                    if (opts.fullscreen == true)
                    {
                        this.BeginInvoke(new Action(this.Hide));
                    }
                    else
                    {
                        this.BeginInvoke(new Action(this.Show));
                    }
                };

                // Disable unwanted keys.
                SebKeyCapture.FilterKeys = true;

				try
				{
					ExtractAdditionalDictionaries();
				}
				catch (Exception e)
				{
					Logger.AddError("Failed to extract additional dictionaries!", null, e);
				}

				try
                {
                    Logger.AddInformation("adding allowed processes to taskbar");
                    addPermittedProcessesToTS(reconfiguring && !isStartup);
                }
                catch (Exception ex)
                {
                    Logger.AddError("Unable to addPermittedProcessesToTS", this, ex);
                }

                //if none of the two kiosk modes are enabled, then we do not monitor the processes, otherwise we monitor the processes. The switch for monitoring processes has no longer any function.
                if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyKillExplorerShell] || (bool)SEBSettings.settingsCurrent[SEBSettings.KeyCreateNewDesktop])
                {
                    Logger.AddInformation("checking for processes that are starting from now on");
                    MonitorProcesses();
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

				isStartup = false;

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

		private void ExtractAdditionalDictionaries()
		{
			var compressor = new FileCompressor();
			var root = SEBClientInfo.XulRunnerAdditionalDictionariesDirectory;

			if (!Directory.Exists(root))
			{
				Directory.CreateDirectory(root);
			}

			Logger.AddInformation($"Directory for additional dictionaries: '{root}'");

			foreach (DictObj dictionary in SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalDictionaries] as ListObj)
			{
				var data = dictionary[SEBSettings.KeyAdditionalDictionaryData] as string;
				var locale = dictionary[SEBSettings.KeyAdditionalDictionaryLocale] as string;

				Logger.AddInformation($"Extracting data for '{locale}'...");

				var paths = compressor.DecodeAndDecompressDirectory(data, root);

				foreach (var path in paths)
				{
					if (File.Exists(path) && Path.GetFileNameWithoutExtension(path) != locale)
					{
						RenameFile(path, locale);
					}
				}
			}
		}

		private void RenameFile(string path, string newName)
		{
			var destination = Path.Combine(Path.GetDirectoryName(path), $"{newName}{Path.GetExtension(path)}");

			if (File.Exists(destination))
			{
				File.Delete(destination);
			}

			File.Move(path, destination);
		}

		private static void MonitorProcesses()
        {
            //Handle prohibited executables watching
            SEBProcessHandler.ProhibitedExecutables.Clear();
            //Add prohibited executables
            foreach (Dictionary<string, object> process in SEBSettings.prohibitedProcessList)
            {
                if ((bool)process[SEBSettings.KeyActive])
                {
                    var name = Path.GetFileNameWithoutExtension((string)process[SEBSettings.KeyExecutable] ?? string.Empty);
                    var originalName = Path.GetFileNameWithoutExtension((string)process[SEBSettings.KeyOriginalName] ?? string.Empty);

                    SEBProcessHandler.ProhibitedExecutables.Add(new ExecutableInfo(name, originalName));
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

			//This prevents the not allowed executables from poping up
			try
			{
				SEBWindowHandler.EnableForegroundWatchDog();
			}
			catch (Exception ex)
			{
				Logger.AddError("Unable to EnableForegroundWatchDog", null, ex);
			}
		}

        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Close SEB Form.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public void CloseSEBForm(bool reconfiguring = false)
		{
			if (!reconfiguring)
			{
				Logger.AddInformation("disabling filtered keys");
				SebKeyCapture.FilterKeys = false;
			}

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
				xulRunner.Exited -= XulRunner_Exited;   
			}

			Logger.AddInformation("closing processes that where started by seb");

			for (int i = 0; i < permittedProcessesReferences.Count; i++)
			{
				try
				{
					var proc = permittedProcessesReferences[i];

					if (proc != null && !proc.HasExited)
					{
						if (reconfiguring && proc.ProcessName.Contains("firefox"))
						{
							continue;
						}

						if (xulRunner?.Id == proc.Id || proc.ProcessName.Contains("firefox"))
						{
							Logger.AddInformation("Allowing seb2 Firefox to close...");
							SEBXULRunnerWebSocketServer.SendAllowCloseToXulRunner();
							Thread.Sleep(500);
						}

						Logger.AddInformation("Attempting to close " + proc.ProcessName);
						SEBNotAllowedProcessController.CloseProcess(proc);   
					}
				}
				catch (Exception ex)
				{
					Logger.AddError("Unable to Shutdown Process",null, ex);
				}
					
			}
			Logger.AddInformation("clearing running processes list");
			//permittedProcessesReferences.Clear();

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
			if (SEBClientInfo.ExplorerShellWasKilled && !reconfiguring)
			{
				try
				{
					Logger.AddInformation("Attempting to start explorer shell");
					SEBProcessHandler.StartExplorerShell();
					Logger.AddInformation("Successfully started explorer shell");
				}
				catch (Exception ex)
				{
					Logger.AddError("Unable to StartExplorerShell",null,ex);
				}
			}

			// Clean clipboard
			SEBClipboard.CleanClipboard();
			Logger.AddInformation("Clipboard deleted.", null, null);

			if (!reconfiguring)
			{
				Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
				Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
			}

			Logger.AddInformation("returning from closesebform");
		}

		/// <summary>
		/// Central code to exit the application
		/// Closes the form and asks for a quit password if necessary
		/// </summary>
		public void ExitApplication(bool showLoadingScreen = true)
		{
			// Only show the loading screen when not in CreateNewDesktop-Mode
			if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyCreateNewDesktop])
			{
				showLoadingScreen = false;
			}

			if (showLoadingScreen)
			{
				Thread loadingThread = null;
				SEBSplashScreen.CloseSplash();
				loadingThread = new Thread(SEBLoading.StartLoading);
				loadingThread.Start();
			}

			taskbarToolStrip.ProcessWndProc = false;

			SEBProcessHandler.LogAllRunningProcesses();

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
			}


			Logger.AddInformation("Attempting to ResetSEBDesktop in ExitApplication");
			SebWindowsClientMain.ResetSEBDesktop();
			Logger.AddInformation("Successfull ResetSEBDesktop");

			SEBDesktopWallpaper.Refresh();

			Logger.AddInformation("---------- EXITING SEB - ENDING SESSION -------------");
			//this.Close();
			Application.Exit();
			Environment.Exit(0);
		}

		private void taskbarToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}

		private void SebWindowsClientForm_SizeChanged(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Normal;
		}

		private void taskbarToolStrip_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				var menu = new ContextMenuStrip();
				var item = new ToolStripMenuItem("About SEB");

				item.Click += (o, args) => new AboutWindow().Show();
				menu.Items.Add(item);
				menu.Show(this, e.Location);
			}
		}
	}
}
