using System;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DesktopUtils;

namespace SebWindowsClient
{
    public partial class SEBSplashScreen : Form
    {
        #region Instance

        private Timer t;

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
            
            t = new Timer {Interval = 200};
            t.Tick += (sender, args) => Progress();
            t.Start();
        }

        private void Progress()
        {
            if (lblLoading.Text == SEBUIStrings.loadingString)
            {
                lblLoading.Text = SEBUIStrings.loadingString + " .";
            }
            else if (lblLoading.Text == SEBUIStrings.loadingString + " .")
            {
                lblLoading.Text = SEBUIStrings.loadingString + " ..";
            }
            else if (lblLoading.Text == SEBUIStrings.loadingString + " ..")
            {
                lblLoading.Text = SEBUIStrings.loadingString + " ...";
            }
            else
            {
                lblLoading.Text = SEBUIStrings.loadingString;
            }
        }

        /// <summary>
        /// Closes the window - invoked via CloseSplash();
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void KillMe(object o, EventArgs e)
        {
            t.Stop();
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
        public static void StartSplash()
        {
			// Set the threads desktop to the new desktop if "Create new Desktop" is activated
			if (SEBClientInfo.SEBNewlDesktop != null && (Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop])
			{
				SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop);
			}
			else
			{
				SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);
			}

			// Instance a splash form given the image names
			splash = new SEBSplashScreen();
			splash.ShowDialog();
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
                if (splash.InvokeRequired)
                {
                    splash.Invoke(new EventHandler(splash.KillMe));
                }
                else
                {
                    splash.KillMe(null, null);
                }
            }
            catch (Exception)
            { }
        }

        #endregion
    }
}
