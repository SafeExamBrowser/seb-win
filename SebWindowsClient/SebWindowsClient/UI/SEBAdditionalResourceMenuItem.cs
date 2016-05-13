using System.Collections.Generic;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsClient.UI
{
    public class SEBAdditionalResourceMenuItem : ToolStripMenuItem
    {
        public SEBAdditionalResourceMenuItem(DictObj resource)
        {
            this.Resource = resource;
            this.Text = resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
        }

        public Dictionary<string, object> Resource { get; set; }
    }
}
