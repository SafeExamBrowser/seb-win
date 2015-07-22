using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;

namespace SebWindowsClient.UI
{
    public class SEBToolStripButton : ToolStripButton
    {
        public SEBToolStripButton()
        {
            this.ImageScaling = ToolStripItemImageScaling.SizeToFit;
        }

        public string Identifier
        { get; set; }

        public string WindowHandlingProcess
        { get; set; }

        public int FontSize
        {
            get
            {
                float sebTaskBarHeight = (int) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTaskBarHeight);
                float fontSize = 10 * (sebTaskBarHeight/40) * SEBClientInfo.scaleFactor;
                if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized])
                {
                    return (int)Math.Round(1.7 * fontSize);
                }
                else
                {
                    return (int) Math.Round(fontSize);
                }
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (this.Parent != null)
                Parent.Focus();
            base.OnMouseHover(e);
        } 
    }
}
