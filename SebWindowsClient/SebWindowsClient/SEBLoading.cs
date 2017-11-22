using System;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DesktopUtils;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient
{
	public partial class SEBLoading : Form
    {
        #region instance
        public SEBLoading()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Close the window - invoked via CloseLoading()
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void KillMe(object o, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region static Thread Access

        private static SEBLoading loading;

        /// <summary>
        /// Call via separate thread
        /// var thread = new Thread(SEBLoading.StartLoading);
        /// thread.Start();
        /// </summary>
        public static void StartLoading()
        {
            SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);

			// Set the threads desktop to the new desktop if "Create new Desktop" is activated
			if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyCreateNewDesktop)[SEBSettings.KeyCreateNewDesktop] || SEBClientInfo.CreateNewDesktopOldValue)
			{
				SEBDesktopController.SetCurrent(SEBClientInfo.SEBNewlDesktop);
			}
			else
			{
				SEBDesktopController.SetCurrent(SEBClientInfo.OriginalDesktop);
			}

			loading = new SEBLoading();
            loading.ShowDialog();
        }

        /// <summary>
        /// Invokes the running thread with the windows and closes it
        /// </summary>
        public static void CloseLoading()
        {
            if (loading == null)
                return;
            try
            {
                Logger.AddInformation("shutting down loading screen");
                // Shut down the loading screen
                if(loading.InvokeRequired)
                {
                    loading.Invoke(new EventHandler(loading.KillMe));
                }
                else
                {
                    loading.Close();
                }
                //loading.Dispose();
                Logger.AddInformation("loading screen shut down");
            }
            catch (Exception)
            { }
        }

        #endregion
    }
}
