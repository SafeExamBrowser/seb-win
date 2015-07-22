using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.WlanUtils;

namespace SebWindowsClient.UI
{
    public class SEBWlanToolStripButton : SEBToolStripButton
    {
        private Timer timer;
        private WlanClient wlanClient;
        private WlanClient.WlanInterface wlanInterface;

        public SEBWlanToolStripButton()
        {
            this.Alignment = ToolStripItemAlignment.Right;
            try
            {
                //Find a wlan interface
                wlanClient = new WlanClient();
                wlanInterface = wlanClient.Interfaces[0];

                //Start the update timer
                timer = new Timer();
                timer.Tick += (x,y) => Update();
                timer.Interval = 1000;
                timer.Start();
            }
            catch (Exception ex)
            {
                Logger.AddError("No WiFi interface found",this,ex);
                base.Enabled = false;
                Update();
            }
        }

        private void Update()
        {
            try
            {
                if (wlanInterface == null)
                {
                    ChangeImage("nointerface");
                    this.ToolTipText = SEBUIStrings.toolTipNoWiFiInterface;
                }
                else if (wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                {
                    var rssi = wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected
                        ? wlanInterface.RSSI
                        : 0;
                    var strengthInPercent = rssi > -35
                        ? 100
                        : rssi < -95 ? 0 : Math.Round((decimal) 100/(-35 + 95)*((decimal) rssi + 95), 2);
                    UpdateSignalStrength((int) strengthInPercent);
                    this.ToolTipText = String.Format(SEBUIStrings.toolTipConnectedToWiFiNetwork, wlanInterface.CurrentConnection.profileName);
                }
                else
                {
                    ChangeImage("0");
                    this.ToolTipText = SEBUIStrings.toolTipNotConnectedToWiFiNetwork;
                }
            }
            catch (Exception ex)
            {
                ChangeImage("0");
                this.ToolTipText = SEBUIStrings.toolTipNotConnectedToWiFiNetwork;
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (this.Parent != null)
                Parent.Focus();
            base.OnMouseHover(e);
        }

        protected override void OnClick(EventArgs e)
        {
            var selector = new SEBWlanNetworkSelector();
            selector.ShowDialog();
        }

        private void UpdateSignalStrength(int percentage)
        { 
            if(percentage < 16)
                ChangeImage("1");
            else if(percentage < 49)
                ChangeImage("33");
            else if(percentage < 82)
                ChangeImage("66");
            else
                ChangeImage("100");
        }

        private string _oldImage = "";
        private void ChangeImage(string imageName)
        {
            if (_oldImage == imageName) return;

            try
            {
                base.Image = (Bitmap) Resources.ResourceManager.GetObject(String.Format("wlan{0}", imageName));
                _oldImage = imageName;
            }
            catch (Exception ex)
            {
                Logger.AddError("Could not change Image for SEBWLanToolStripButton",this,ex);
            }
        }

        private void InitializeComponent()
        {
            // 
            // SEBWlanToolStripButton
            // 
            this.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);

        }

        
    }

    public static class WlanAPIExtensions
    {
        public static string GetSSID(this Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }
    }
}
