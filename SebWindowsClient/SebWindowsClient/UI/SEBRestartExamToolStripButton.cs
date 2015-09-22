using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
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
            base.Image = (Bitmap)Resources.ResourceManager.GetObject("restartExam");
            this.Alignment = ToolStripItemAlignment.Right;
        }

        protected override void OnClick(EventArgs e)
        {
            if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamPasswordProtected)[SEBSettings.KeyRestartExamPasswordProtected])
            {
                var quitPassword = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyHashedQuitPassword)[SEBSettings.KeyHashedQuitPassword];
                // Get text (title/tool tip) for restarting exam
                string restartExamTitle = (String)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamText)[SEBSettings.KeyRestartExamText];
                // If there was no individual restart exam text set, we use the default text (which is localized)
                if (String.IsNullOrEmpty(restartExamTitle)) {
                    restartExamTitle = SEBUIStrings.restartExamDefaultTitle;
                }
                if (!String.IsNullOrWhiteSpace(quitPassword))
                {
                    var password = SebPasswordDialogForm.ShowPasswordDialogForm(restartExamTitle,
                        SEBUIStrings.restartExamMessage);
                    var hashedPassword = SEBProtectionController.ComputePasswordHash(password);
                    if (String.IsNullOrWhiteSpace(password) ||
                        String.Compare(quitPassword, hashedPassword, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        SEBMessageBox.Show(Resources.WrongPasswordTitle, Resources.WrongPasswordText,
                            MessageBoxIcon.Error, MessageBoxButtons.OK);
                        return;
                    }
                }
                else
                {
                    if (SEBMessageBox.Show("Are you sure?", "Do you really want to restart the exam?",
                        MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }
            }
            else
            {
                if (SEBMessageBox.Show("Are you sure?", "Do you really want to restart the exam?",
                        MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;   
                }
            }
            SEBXULRunnerWebSocketServer.SendRestartExam();
        }
    }
}
