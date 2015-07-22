using System;
using System.Drawing;
using System.Windows.Forms;
using FontStyle = System.Drawing.FontStyle;

namespace SebWindowsClient.UI
{
    public class SEBWatchToolStripButton : SEBToolStripButton
    {
        private Timer timer;

        public SEBWatchToolStripButton()
        {
            InitializeComponent();

            timer = new Timer();
            timer.Tick += OnTimerTick;
            timer.Interval = 10000;
            timer.Start();
            Update();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            this.Text = DateTime.Now.ToShortTimeString();
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
            this.Alignment = ToolStripItemAlignment.Right;
            base.ForeColor = Color.Black;
            base.Font = new Font("Arial", base.FontSize, FontStyle.Bold);
            base.Enabled = false;
        }
    }
}
