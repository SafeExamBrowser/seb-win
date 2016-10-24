using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.ProcessUtils;

namespace SebWindowsClient.UI
{
    public class SEBInputLanguageToolStripButton : SEBToolStripButton
    {
        private Timer timer;
        private int currentIndex;
        private IntPtr[] languages;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr id);

        [DllImport("user32.dll")]
        static extern uint GetKeyboardLayout(uint idThread);

        private const int WM_INPUTLANGCHANGEREQUEST = 0x0050;

        public SEBInputLanguageToolStripButton()
        {
            this.Alignment = ToolStripItemAlignment.Right;
            base.ForeColor = Color.Black;
            base.Font = new Font("Arial", base.FontSize, FontStyle.Bold);

            try
            {
                currentIndex = InputLanguage.InstalledInputLanguages.IndexOf(InputLanguage.CurrentInputLanguage);

                if (InputLanguage.InstalledInputLanguages.Count < 2)
                {
                    throw new NotSupportedException("There is only one keyboard layout available");
                }

                if (OSVersion.IsWindows7)
                {
                    SEBWindowHandler.ForegroundWatchDog.OnForegroundWindowChanged += handle => SetKeyboardLayoutAccordingToIndex();
                    languages = new IntPtr[InputLanguage.InstalledInputLanguages.Count];
                    for (int i = 0; i < InputLanguage.InstalledInputLanguages.Count; i++)
                    {
                        languages[i] =
                            LoadKeyboardLayout(
                                InputLanguage.InstalledInputLanguages[i].Culture.KeyboardLayoutId.ToString("X8"), 1);
                    }

                    //Start the update timer
                    timer = new Timer();
                    timer.Tick += (x, y) => UpdateDisplayText();
                    timer.Interval = 1000;
                    timer.Start();
                }                
            }
            catch (Exception ex)
            {
                base.Enabled = false;
            }
            UpdateDisplayText();
        }

        private void SetKeyboardLayoutAccordingToIndex()
        {
            try
            {
                InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[currentIndex];

                if (OSVersion.IsWindows7)
                {
                    var threadIdOfCurrentForegroundWindow = GetWindowThreadProcessId(SEBWindowHandler.GetForegroundWindow(), IntPtr.Zero);
                    var currentKeyboardLayout = GetKeyboardLayout(threadIdOfCurrentForegroundWindow);
                    if (languages[currentIndex].ToInt32() != currentKeyboardLayout)
                    {
                        PostMessage(SEBWindowHandler.GetForegroundWindow(),
                            WM_INPUTLANGCHANGEREQUEST,
                            IntPtr.Zero,
                            languages[currentIndex]
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("Could not change InputLanguage", this, ex);
            }
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            try
            {
                this.Text = InputLanguage.CurrentInputLanguage.Culture.ThreeLetterISOLanguageName.ToUpper();
                this.ToolTipText = String.Format(SEBUIStrings.KeyboardLayout_CURRENTCULTURE, InputLanguage.CurrentInputLanguage.Culture.DisplayName);
            }
            catch (Exception ex)
            {
                this.Text = "Error";
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (this.Parent != null)
                Parent.Focus();
            base.OnMouseHover(e);
        }

        protected override void OnClick(EventArgs e)
        {
            try
            {
                currentIndex++;
                currentIndex = currentIndex % (InputLanguage.InstalledInputLanguages.Count);
            }
            catch (Exception ex)
            {
                Logger.AddError("Could not change InputLanguage",this, ex);
            }
            SetKeyboardLayoutAccordingToIndex();
        }

    }
}
