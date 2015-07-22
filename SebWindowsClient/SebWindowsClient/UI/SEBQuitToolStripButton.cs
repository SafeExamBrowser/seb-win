using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.WlanUtils;

namespace SebWindowsClient.UI
{
    public class SEBQuitToolStripButton : SEBToolStripButton
    {

        public SEBQuitToolStripButton()
        {
            this.ToolTipText = SEBUIStrings.confirmQuitting;
            this.Alignment = ToolStripItemAlignment.Right;
            base.Image = (Bitmap) Resources.ResourceManager.GetObject("quit");
        }
    }
}
