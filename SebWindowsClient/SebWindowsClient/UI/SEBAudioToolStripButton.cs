using System;
using System.Drawing;
using System.Windows.Forms;
using SebWindowsClient.AudioUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.WlanUtils;

namespace SebWindowsClient.UI
{
    public class SEBAudioToolStripButton : SEBToolStripButton
    {
        private Timer timer;
        private WlanClient wlanClient;
        private WlanClient.WlanInterface wlanInterface;

        public SEBAudioToolStripButton()
        {
            this.Alignment = ToolStripItemAlignment.Right;

            UpdateImage();
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (this.Parent != null)
                Parent.Focus();
            base.OnMouseHover(e);
        }
        

        protected override void OnClick(EventArgs e)
        {
            var selector = new SEBAudioControl();
            selector.Width = 160;
            selector.StartPosition = FormStartPosition.Manual;
            selector.Left = Bounds.Left - selector.Width;
            selector.Top = Screen.PrimaryScreen.Bounds.Height - selector.Height;
            selector.Closed += (sender, args) => UpdateImage();
            selector.ShowDialog();
        }

        private void UpdateImage()
        {
            if (new AudioControl().GetMute())
            {
                base.Image = (Bitmap)Resources.ResourceManager.GetObject("audioControlmute");
                return;
            }

            var audioLevel = SEBAudioControl.LastSetVolume ?? (int)(new AudioControl().GetVolumeScalar() * 100);
            int image = 0;
            
            if (audioLevel == 0)
            {
                image = 0;
            }
            else if (audioLevel < 25)
            {
                image = 1;
            }
            else if (audioLevel < 75)
            {
                image = 50;
            }
            else
            {
                image = 100;
            }

            base.Image = (Bitmap)Resources.ResourceManager.GetObject(string.Format("audioControl{0}",image));
        }
    }
}
