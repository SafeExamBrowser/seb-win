using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DesktopUtils;

namespace SebWindowsClient
{
    public partial class SEBSplashScreen : Form
    {
        #region Instance
        public SEBSplashScreen()
        {
            InitializeComponent();

            this.TxtVersion.Text = "Safe Exam Browser for Windows";
            try
            {
                this.TxtVersion.Text += " " + Application.ProductVersion;
            }
            catch (Exception)
            {
                //No Version info available
            }

            float dpiX;
            using (var g = this.CreateGraphics())
            {
                dpiX = g.DpiX;
            }
            float scaleFactor = dpiX / 96;

            float width = (float)this.pictureBox1.Width;
            //this.pictureBox1.Width = (int)Math.Round(width * scaleFactor);

            float height = (float)this.pictureBox1.Height;
            //this.pictureBox1.Height = (int)Math.Round(height * scaleFactor);

            this.Click += KillMe;
            this.pictureBox1.Click += KillMe;
            this.TxtVersion.Click += KillMe;
            this.lblLoading.Click += KillMe;

            var t = new Timer {Interval = 200};
            t.Tick += (sender, args) => Progress();
            t.Start();
        }

        private void Progress()
        {
            switch (lblLoading.Text)
            {
                case "Loading":
                    lblLoading.Text = "Loading .";
                    break;
                case "Loading .":
                    lblLoading.Text = "Loading ..";
                    break;
                case "Loading ..":
                    lblLoading.Text = "Loading ...";
                    break;
                default:
                    lblLoading.Text = "Loading";
                    break;
            }
        }

        /// <summary>
        /// Closes the window - invoked via CloseSplash();
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void KillMe(object o, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region static Thread Access

        private static SEBSplashScreen splash;

        /// <summary>
        /// Call via separate thread
        /// var thread = new Thread(SEBLoading.StartSplash);
        /// thread.Start();
        /// </summary>
        static public void StartSplash()
        {
            //Set the threads desktop to the new desktop if "Create new Desktop" is activated
            if (SEBClientInfo.SEBNewlDesktop != null && (Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
                SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop);
            else
                SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);

            // Instance a splash form given the image names
            splash = new SEBSplashScreen();
            // Run the form
            Application.Run(splash);
        }

        /// <summary>
        /// Invokes the thread with the window and closes it
        /// </summary>
        public static void CloseSplash()
        {
            if (splash == null)
                return;
            try
            {
                // Shut down the splash screen
                splash.Invoke(new EventHandler(splash.KillMe));
                splash.Dispose();
                splash = null;
            }
            catch (Exception)
            { }
        }

        #endregion

    }
}
