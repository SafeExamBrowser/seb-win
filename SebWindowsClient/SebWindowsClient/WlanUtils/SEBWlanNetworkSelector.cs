using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.UI;

namespace SebWindowsClient.WlanUtils
{
    public partial class SEBWlanNetworkSelector : Form
    {
        private WlanClient.WlanInterface wlanInterface;
        private WlanClient wlanClient;

        private List<Wlan.WlanAvailableNetwork> availableNetworks; 
        public SEBWlanNetworkSelector()
        {
            InitializeComponent();

            try
            {
                //Find a wlan interface
                wlanClient = new WlanClient();
                wlanInterface = wlanClient.Interfaces[0];
                RefreshNetworks();
            }
            catch (Exception ex)
            {
                listNetworks.Enabled = false;
                listNetworks.Items.Add("No network interface found");
                Logger.AddError("No Network interface found",this,ex);
            }
        }

        private void RefreshNetworks()
        {
            listNetworks.DataSource = null;
            listNetworks.Items.Clear();
            try
            {
                //All the available networks around that are known
                availableNetworks = wlanInterface.GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles).Where(network => !String.IsNullOrWhiteSpace(network.profileName)).ToList();
                if (availableNetworks.Count() > 0)
                {
                    listNetworks.DataSource =
                        availableNetworks.Select(network =>
                                String.Format("{0} Strength: {1}", network.dot11Ssid.GetSSID(), network.wlanSignalQuality))
                                .ToList();
                }
                else
                {
                    throw new Exception("No networks found!");
                }
            }
            catch (Exception ex)
            {
                listNetworks.Enabled = false;
                listNetworks.Items.Add("No known networks found!");
                listNetworks.Items.Add("you can only connect to networks");
                listNetworks.Items.Add("that you have used before.");
                Logger.AddError("No Networks found", this, ex);
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            var profileName = availableNetworks[listNetworks.SelectedIndex].profileName;

            buttonConnect.Text = "Connecting...";
            buttonConnect.Enabled = false;
            listNetworks.Enabled = false;

            var profile = wlanInterface.GetProfileXml(profileName);
            wlanInterface.SetProfile(Wlan.WlanProfileFlags.AllUser, profile, true);
            wlanInterface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName);

            this.Close();
        }

        private void listNetworks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listNetworks.SelectedItem != null)
            {
                buttonConnect.Enabled = true;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshNetworks();
        }
    }
}
