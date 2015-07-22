using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using SebWindowsClient.Properties;
using FontStyle = System.Drawing.FontStyle;

namespace SebWindowsClient.UI
{
    public class SEBBatterylifeToolStripButton : SEBToolStripButton
    {
        private PowerStatus powerStatus = SystemInformation.PowerStatus;
        private Timer timer;

        public SEBBatterylifeToolStripButton()
        {
            InitializeComponent();

            timer = new Timer();
            timer.Tick += OnTimerTick;
            timer.Interval = 5000;
            timer.Start();
            Update();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            this.Visible = SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
            this.Text = String.Format(" {0}%", powerStatus.BatteryLifePercent*100);
        }

        protected override void Dispose(bool disposing)
        {
            timer.Tick -= OnTimerTick;
            timer.Stop();
            timer = null;
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // 
            // SEBBatterylifeToolStripButton
            // 
            base.Image = (Bitmap)Resources.ResourceManager.GetObject("battery");
            base.TextImageRelation = TextImageRelation.Overlay;
            this.Alignment = ToolStripItemAlignment.Right;
            base.ForeColor = Color.Black;
            base.Font = new Font("Arial", (int)(base.FontSize * 0.7), FontStyle.Bold);
            base.Enabled = false;
        }
    }
}
