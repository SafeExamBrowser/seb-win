using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.XULRunnerCommunication;


namespace SebWindowsClient.UI
{
	public class SEBOnScreenKeyboardToolStripButton : SEBToolStripButton
	{
		[DllImport("user32.dll")]
		static extern int GetSystemMetrics(SystemMetric smIndex);

		public enum SystemMetric
		{
			SM_CONVERTABLESLATEMODE = 0x2003,
			SM_SYSTEMDOCKED = 0x2004,
		}

		public SEBOnScreenKeyboardToolStripButton()
		{
			InitializeComponent();
			this.Alignment = ToolStripItemAlignment.Right;
			EnableOrDisableBasedOnState();
			SystemEvents.UserPreferenceChanging += SystemEvents_UserPreferenceChanging;
		}

		protected override void OnClick(EventArgs e)
		{
			bool firstOpen = TapTipHandler.FirstOpen;
			if (TapTipHandler.IsKeyboardVisible())
			{
				TapTipHandler.HideKeyboard();
			}
			else
			{
				//reset firstopen because it will be checked again
				TapTipHandler.FirstOpen = firstOpen;
				TapTipHandler.ShowKeyboard(true);
			}
		}

		private void InitializeComponent()
		{
			this.ToolTipText = SEBUIStrings.toolTipOnScreenKeyboard;
			base.Image = (Bitmap)Resources.ResourceManager.GetObject("keyboard");
		}

		private void EnableOrDisableBasedOnState()
		{
            var tabletMode = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "TabletMode", 1);
            this.Enabled = tabletMode == 1 && GetSystemMetrics(SystemMetric.SM_CONVERTABLESLATEMODE) == 0;
			this.ToolTipText = !this.Enabled ? SEBUIStrings.toolTipOnScreenKeyboardNotEnabled : SEBUIStrings.toolTipOnScreenKeyboard;
		}

		private void SystemEvents_UserPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
		{
			if (e.Category == UserPreferenceCategory.General)
			{
				EnableOrDisableBasedOnState();
			}
		}
	}

	public static class TapTipHandler
	{
		public delegate void KeyboardStateChangedEventHandler(bool shown);
		public static event KeyboardStateChangedEventHandler OnKeyboardStateChanged;

		public static void RegisterXulRunnerEvents()
		{
			SEBXULRunnerWebSocketServer.OnXulRunnerTextFocus += OnXulRunnerTextFocus;
			SEBXULRunnerWebSocketServer.OnXulRunnerTextBlur += OnXulRunnerTextBlur;
		}

		private static bool _textFocusHappened;

		private static void OnXulRunnerTextFocus(object sender, EventArgs e)
		{
			_textFocusHappened = true;
			ShowKeyboard();
		}

		private static void OnXulRunnerTextBlur(object sender, EventArgs e)
		{
			_textFocusHappened = false;
			var t = new System.Timers.Timer { Interval = 100 };
			t.Elapsed += (x, y) =>
			{
				if (!_textFocusHappened)
				{
					HideKeyboard();
				}
				t.Stop();
			};
			t.Start();
		}

		public static void ShowKeyboard(bool force = false)
		{
				try
				{
					if (IsPhysicalKeyboardAttached() && !force)
					{
						return;
					}

					if (!(bool)SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized))
					{
						return;
					}

					if (IsKeyboardVisible())
					{
						return;
					}

					if (!SEBWindowHandler.AllowedExecutables.Any(e => e.Name == "tabtip"))
						SEBWindowHandler.AllowedExecutables.Add(new ExecutableInfo("tabtip", "tabtip"));

					//TODO: Use Environment Variable here, but with SEB running as 32bit it always takes X86
					//string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
					string programFiles = @"C:\Program Files";
					string inkDir = @"Common Files\Microsoft Shared\ink";
					string onScreenKeyboardPath = Path.Combine(programFiles, inkDir, "TabTip.exe");
					Process.Start(onScreenKeyboardPath);
					if (OnKeyboardStateChanged != null)
					{
						var t = new System.Timers.Timer {Interval = 500};
						t.Elapsed += (sender, args) =>
						{
							if (!IsKeyboardVisible())
							{
								t.Stop();
								OnKeyboardStateChanged(false);
							}
						};
						t.Start();

						OnKeyboardStateChanged(true);
					}
				}
				catch
				{ }
		}

		public static void HideKeyboard()
		{
			if (IsKeyboardVisible())
			{
				uint WM_SYSCOMMAND = 274;
				IntPtr SC_CLOSE = new IntPtr(61536);
				PostMessage(GetKeyboardWindowHandle(), WM_SYSCOMMAND, SC_CLOSE, (IntPtr)0);
			}
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// The window is disabled. See http://msdn.microsoft.com/en-gb/library/windows/desktop/ms632600(v=vs.85).aspx.
		/// </summary>
		public const UInt32 WS_DISABLED = 0x8000000;
		public const UInt32 WS_MINIMIZE = 0x20000000;
		public const UInt32 WS_VISIBLE = 0x10000000;

		/// <summary>
		/// Specifies we wish to retrieve window styles.
		/// </summary>
		public const int GWL_STYLE = -16;

		public static bool FirstOpen = true;

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(String sClassName, String sAppName);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);


		/// <summary>
		/// Gets the window handler for the virtual keyboard.
		/// </summary>
		/// <returns>The handle.</returns>
		public static IntPtr GetKeyboardWindowHandle()
		{
			return FindWindow("IPTip_Main_Window", null);
		}

		/// <summary>
		/// Checks to see if the virtual keyboard is visible.
		/// </summary>
		/// <returns>True if visible.</returns>
		public static bool IsKeyboardVisible()
		{
			if (FirstOpen)
			{
				FirstOpen = false;
				return false;
			}

			IntPtr keyboardHandle = GetKeyboardWindowHandle();

			bool visible = false;

			if (keyboardHandle != IntPtr.Zero)
			{
				keyboardHandle.MaximizeWindow();
				UInt32 style = GetWindowLong(keyboardHandle, GWL_STYLE);
				visible = (style & WS_DISABLED) != WS_DISABLED && (style & WS_MINIMIZE) != WS_MINIMIZE && (style & WS_VISIBLE) == WS_VISIBLE;
			}

			return visible;
		}

		public static bool IsPhysicalKeyboardAttached()
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select Description from Win32_Keyboard");

			ManagementObjectCollection keyboards = searcher.Get();
			if (keyboards.Count == 1)
			{
				foreach (ManagementObject keyboard in keyboards)
				{
					if (keyboard.GetPropertyValue("Description").ToString().Contains("HID"))
					{
						return false;
					}
				}
			}
			return true;
		}

		public static bool IsKeyboardDocked()
		{
			int docked = 1;

			try
			{
				//HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7\EdgeTargetDockedState -> 0 = floating, 1 = docked
				docked = (int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7\", "EdgeTargetDockedState", 1);
			}
			catch { }

			return docked == 1;

		}
	}
}
