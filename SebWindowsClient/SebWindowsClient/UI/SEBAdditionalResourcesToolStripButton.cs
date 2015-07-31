using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using MetroFramework.Controls;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.XULRunnerCommunication;
using ListObj = System.Collections.Generic.List<object>;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsClient.UI
{
    public class SEBAdditionalResourcesToolStripButton : SEBToolStripButton
    {
        private ContextMenu menu;
        public SEBAdditionalResourcesToolStripButton()
        {
            InitializeComponent();

            menu = new ContextMenu();

            LoadItems();
        }

        private void InitializeComponent()
        {
            base.Image = Resources.quit;
        }

        private void LoadItems()
        {
            foreach (DictObj l0resource in ((ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources]))
            {
                var l0item = new MenuItem();
                l0item.Text = l0resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                l0item.Tag = l0resource;
                l0item.Click += OnItemClicked;

                foreach (DictObj l1resource in ((ListObj)l0resource[SEBSettings.KeyAdditionalResources]))
                {
                    var l1item = new MenuItem();
                    l1item.Text = l1resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                    l1item.Tag = l1resource;
                    l1item.Click += OnItemClicked;

                    foreach (DictObj l2resource in ((ListObj)l1resource[SEBSettings.KeyAdditionalResources]))
                    {
                        var l2item = new MenuItem();
                        l2item.Text = l2resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                        l2item.Tag = l2resource;
                        l2item.Click += OnItemClicked;
                        l1item.MenuItems.Add(l2item);
                    }
                    l0item.MenuItems.Add(l1item);
                }
                menu.MenuItems.Add(l0item);
            }
        }

        private void OnItemClicked(object sender, EventArgs e)
        {
            var item = sender as MenuItem;
            var resource = item.Tag as DictObj;
            SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.AdditionalResources,new { id = resource[SEBSettings.KeyIdentifier] }));
        }

        protected override void OnClick(EventArgs e)
        {
            menu.Show(this.Parent,new Point(this.Bounds.X,this.Bounds.Y));
        }
       
    }

}
