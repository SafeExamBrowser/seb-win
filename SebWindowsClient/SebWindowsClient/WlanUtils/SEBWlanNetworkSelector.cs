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
using Microsoft.VisualBasic.Devices;
using SebWindowsClient.ConfigurationUtils;
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
                listNetworks.Items.Add(SEBUIStrings.WlanNoNetworkInterfaceFound);
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
                    for (int i = 0; i < availableNetworks.Count; i++)
                    {
                        var network = availableNetworks[i];
                        listNetworks.Items.Add(String.Format("{0} ({1})", network.dot11Ssid.GetSSID(),
                            network.wlanSignalQuality));
                        try
                        {
                            if (wlanInterface.CurrentConnection.profileName == network.profileName)
                            {
                                listNetworks.SelectedIndex = i;
                            }
                        }
                        catch (Exception)
                        {
                            //cannot determine currently connected wlan network
                        }
                    }
                }
                else
                {
                    throw new Exception("No networks found!");
                }
            }
            catch (Exception ex)
            {
                listNetworks.Enabled = false;
                listNetworks.Items.Add(SEBUIStrings.WlanNoNetworksFound);
                listNetworks.Items.Add(SEBUIStrings.WlanYouCanOnlyConnectToNetworks);
                listNetworks.Items.Add(SEBUIStrings.WlanThatYouHaveUsedBefore);
                Logger.AddError("No Networks found", this, ex);
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            var profileName = availableNetworks[listNetworks.SelectedIndex].profileName;

            buttonConnect.Text = SEBUIStrings.WlanConnecting;
            buttonConnect.Enabled = false;
            listNetworks.Enabled = false;

            try
            {
                var profile = wlanInterface.GetProfileXml(profileName);
                wlanInterface.SetProfile(Wlan.WlanProfileFlags.AllUser, profile, true);
                wlanInterface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName);
                this.Close();
            }
            catch(Exception ex)
            {
                SEBMessageBox.Show(SEBUIStrings.WlanConnectionFailedMessageTitle,
                    SEBUIStrings.WlanConnectionFailedMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                Logger.AddError(ex.Message, this, ex);
            }
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
