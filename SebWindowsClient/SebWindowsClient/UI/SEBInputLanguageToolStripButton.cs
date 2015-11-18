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

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

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

                SEBWindowHandler.ForegroundWatchDog.OnForegroundWindowChanged += handle => SetKeyboardLayoutAccordingToIndex();

                //Start the update timer
                timer = new Timer();
                timer.Tick += (x, y) => UpdateDisplayText();
                timer.Interval = 1000;
                timer.Start();
            }
            catch (Exception ex)
            {
                base.Enabled = false;
                UpdateDisplayText();
            }
        }

        private void SetKeyboardLayoutAccordingToIndex()
        {
            try
            {
                InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[currentIndex];

                //This is for Windows 7
                PostMessage(SEBWindowHandler.GetForegroundWindow(), 
                    WM_INPUTLANGCHANGEREQUEST, 
                    IntPtr.Zero,
                    LoadKeyboardLayout(InputLanguage.CurrentInputLanguage.Culture.KeyboardLayoutId.ToString("X8"), 1)
                );
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
