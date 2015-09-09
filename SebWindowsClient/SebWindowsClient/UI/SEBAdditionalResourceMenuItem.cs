using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SebWindowsClient.UI
{
    public class SEBAdditionalResourceMenuItem : ToolStripMenuItem
    {
        public Dictionary<string, object> Resource { get; set; }
    }
}
