using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.CryptographyUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.XULRunnerCommunication;


namespace SebWindowsClient.UI
{
    public class SEBRestartExamToolStripButton : SEBToolStripButton
    {
        public SEBRestartExamToolStripButton()
        {
            // Get text (title/tool tip) for restarting exam
            string restartExamTitle = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamText)[SEBSettings.KeyRestartExamText];
            // If there was no individual restart exam text set, we use the default text (which is localized)
            if (String.IsNullOrEmpty(restartExamTitle))
            {
                restartExamTitle = SEBUIStrings.restartExamDefaultTitle;
            }
            this.ToolTipText = restartExamTitle;
            base.Image = (Bitmap)Resources.ResourceManager.GetObject("skipBack");
            this.Alignment = ToolStripItemAlignment.Right;
        }

        protected override void OnClick(EventArgs e)
        {
            string restartExamTitle = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamText)[SEBSettings.KeyRestartExamText];
            // If there was no individual restart exam text set, we use the default text (which is localized)
            if (String.IsNullOrEmpty(restartExamTitle))
            {
                restartExamTitle = SEBUIStrings.restartExamDefaultTitle;
            }

            string quitPassword = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyHashedQuitPassword)[SEBSettings.KeyHashedQuitPassword];

            if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamPasswordProtected)[SEBSettings.KeyRestartExamPasswordProtected] && !String.IsNullOrWhiteSpace(quitPassword))
            {
                var password = SebPasswordDialogForm.ShowPasswordDialogForm(restartExamTitle, SEBUIStrings.restartExamEnterPassword);

                //cancel button has been clicked
                if (password == null)
                {
                    return;
                }

                var hashedPassword = SEBProtectionController.ComputePasswordHash(password);
                if (String.IsNullOrWhiteSpace(password) ||
                    String.Compare(quitPassword, hashedPassword, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    SEBMessageBox.Show(restartExamTitle, SEBUIStrings.wrongQuitRestartPasswordText, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    return;
                }
                else
                {
                    SEBXULRunnerWebSocketServer.SendRestartExam();
                    return;
                }
            }

            if (SEBMessageBox.Show(restartExamTitle, SEBUIStrings.restartExamConfirm, MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SEBXULRunnerWebSocketServer.SendRestartExam();
            }
        }

    }
}
