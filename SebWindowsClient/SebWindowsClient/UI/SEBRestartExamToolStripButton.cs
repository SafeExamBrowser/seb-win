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
                    var password = SebPasswordDialogForm.ShowPasswordDialogForm(restartExamTitle, SEBUIStrings.restartExamMessage);
                    if (String.IsNullOrWhiteSpace(password)) return;
                    var hashedPassword = SEBProtectionController.ComputePasswordHash(password);
                    if (String.Compare(quitPassword, hashedPassword, StringComparison.OrdinalIgnoreCase) != 0)
                        return;
                }
            }
            SEBXULRunnerWebSocketServer.SendRestartExam();
        }
    }
}
