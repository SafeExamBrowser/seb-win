using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.CryptographyUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.UI;

namespace SebWindowsClient
{
    public partial class SebCloseDialogForm : Form
    {
        public SebCloseDialogForm()
        {
            InitializeComponent();
            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] ==
                true)
            {
                InitializeForTouch();
            }
            else
            {
                InitializeForNonTouch();
            }
        }

        public void InitializeForTouch()
        {
            this.Font = new Font(FontFamily.GenericSansSerif, 12);
            this.lblQuitPassword.Left = (Screen.PrimaryScreen.Bounds.Width / 2) - (this.lblQuitPassword.Width / 2);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Top = 0;
            this.Left = 0;
            this.Width = Screen.PrimaryScreen.Bounds.Width;
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.btnCancel.BackColor = Color.Red;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.Height = 35;
            this.btnCancel.Width = 120;
            this.btnCancel.Left = (Screen.PrimaryScreen.Bounds.Width / 2) - (this.btnCancel.Width / 2) + 100;
            this.btnOk.BackColor = Color.Green;
            this.btnOk.FlatStyle = FlatStyle.Flat;
            this.btnOk.Height = 35;
            this.btnOk.Width = 120;
            this.btnOk.Left = (Screen.PrimaryScreen.Bounds.Width / 2) - (this.btnOk.Width / 2) - 100;
            this.txtQuitPassword.Width = 400;
            this.txtQuitPassword.Left = (Screen.PrimaryScreen.Bounds.Width / 2) - (this.txtQuitPassword.Width / 2);
            this.txtQuitPassword.Height = 30;
        }

        public void InitializeForNonTouch()
        {
            this.Font = DefaultFont;
            this.lblQuitPassword.Left = (int)(12 * SEBClientInfo.scaleFactor);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Width = (int)(365 * SEBClientInfo.scaleFactor);
            this.Height = (int)(155 * SEBClientInfo.scaleFactor);
            this.Top = Screen.PrimaryScreen.Bounds.Height/2 - this.Height / 2;
            this.Left = Screen.PrimaryScreen.Bounds.Width/2 - this.Width/2;
            this.btnCancel.BackColor = SystemColors.Control;
            this.btnCancel.FlatStyle = FlatStyle.Standard;
            this.btnCancel.Height = (int)(23 * SEBClientInfo.scaleFactor);
            this.btnCancel.Width = (int)(75 * SEBClientInfo.scaleFactor);
            this.btnCancel.Left = (int)(180 * SEBClientInfo.scaleFactor);
            this.btnOk.BackColor = SystemColors.Control;
            this.btnOk.FlatStyle = FlatStyle.Standard;
            this.btnOk.Height = (int)(23 * SEBClientInfo.scaleFactor);
            this.btnOk.Width = (int)(75 * SEBClientInfo.scaleFactor);
            this.btnOk.Left = (int)(94 * SEBClientInfo.scaleFactor);
            this.txtQuitPassword.Width = (int)(325 * SEBClientInfo.scaleFactor);
            this.txtQuitPassword.Left = (int)(12 * SEBClientInfo.scaleFactor);
            this.txtQuitPassword.Height = (int)(20 * SEBClientInfo.scaleFactor);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.txtQuitPassword.Text = "";
            this.Visible = false;
            if ((bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized))
            {
                var topWindow = SEBWindowHandler.GetOpenWindows().FirstOrDefault();
                if (topWindow.Value != null)
                {
                    topWindow.Key.AdaptWindowToWorkingArea();
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string userQuitPassword = this.txtQuitPassword.Text;

            string hPassword = SEBProtectionController.ComputePasswordHash(userQuitPassword);
            string settingsPasswordHash = (string)SEBClientInfo.getSebSetting(SEBSettings.KeyHashedQuitPassword)[SEBSettings.KeyHashedQuitPassword];
            int quit = String.Compare(settingsPasswordHash, hPassword, StringComparison.OrdinalIgnoreCase);
            if (quit != 0)
            {
                this.Hide();
                SEBMessageBox.Show(SEBUIStrings.quittingFailed, SEBUIStrings.quittingFailedReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
                this.txtQuitPassword.Text = "";
                this.Visible = false;
            }
            else
            {
                this.Visible = false;
                SEBClientInfo.SebWindowsClientForm.ExitApplication();
            }
        }

        private void txtQuitPassword_Enter(object sender, EventArgs e)
        {
            TapTipHandler.ShowKeyboard();
        }

        private void txtQuitPassword_Leave(object sender, EventArgs e)
        {
            TapTipHandler.HideKeyboard();
        }
    }
}
