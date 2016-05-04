using System;
using System.Drawing;
using System.Windows.Forms;
using SebWindowsClient.Properties;

namespace SebWindowsClient.AudioUtils
{
    public partial class SEBAudioControl : Form
    {
        public static int? LastSetVolume = null;
        public static bool? LatestMute = null;

        private readonly AudioControl audioControl;

       
        public SEBAudioControl()
        {
            InitializeComponent();
            audioControl = new AudioControl();
            this.Trackbar.Value = LastSetVolume ?? (int)(audioControl.GetVolumeScalar() * 100);
            LatestMute = LatestMute ?? audioControl.GetMute();
            SetMuteImage();
        }

        private void SetMuteImage()
        {
            if (LatestMute.GetValueOrDefault())
            {
                ButtonMute.BackgroundImage = (Bitmap) Resources.ResourceManager.GetObject("audioControlunmute");
            }
            else
            {
                ButtonMute.BackgroundImage = (Bitmap)Resources.ResourceManager.GetObject("audioControlmute");
            }
        }

        private void ButtonMute_Click(object sender, EventArgs e)
        {
            Mute(!LatestMute.GetValueOrDefault());
        }

        private void Mute(bool mute)
        {
            audioControl.Mute(mute);
            LatestMute = mute;
            SetMuteImage();
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private Timer _scrollingTimer = null;
        private void Trackbar_Scroll(object sender, EventArgs e)
        {
            if (_scrollingTimer == null)
            {
                // Will tick every 500ms (change as required)
                _scrollingTimer = new Timer()
                {
                    Enabled = false,
                    Interval = 500,
                    Tag = (sender as TrackBar).Value
                };
                _scrollingTimer.Tick += (s, ea) =>
                {
                    // check to see if the value has changed since we last ticked
                    if (Trackbar.Value == (int)_scrollingTimer.Tag)
                    {
                        // scrolling has stopped so we are good to go ahead and do stuff
                        _scrollingTimer.Stop();

                        audioControl.SetVolumeScalar((float)Trackbar.Value / 100);
                        LastSetVolume = Trackbar.Value;
                        Mute(false);

                        _scrollingTimer.Dispose();
                        _scrollingTimer = null;
                    }
                    else
                    {
                        // record the last value seen
                        _scrollingTimer.Tag = Trackbar.Value;
                    }
                };
                _scrollingTimer.Start();
            }
        }
    }
}
