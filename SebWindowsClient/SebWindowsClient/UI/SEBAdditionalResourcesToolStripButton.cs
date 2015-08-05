using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Windows.Media;
using MetroFramework;
using MetroFramework.Controls;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.XULRunnerCommunication;
using Color = System.Drawing.Color;
using ListObj = System.Collections.Generic.List<object>;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsClient.UI
{
    public class SEBAdditionalResourcesToolStripButton : SEBToolStripButton
    {
        private ContextMenuStrip menu;
        public SEBAdditionalResourcesToolStripButton()
        {
            InitializeComponent();

            menu = new ContextMenuStrip();

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
                var l0item = new SEBAdditionalResourceMenuItem();

                l0item.Text = l0resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                l0item.Identifier = l0resource[SEBSettings.KeyIdentifier].ToString();
                l0item.Click += OnItemClicked;
                l0item.Image = Resources.closewindow;

                foreach (DictObj l1resource in ((ListObj)l0resource[SEBSettings.KeyAdditionalResources]))
                {
                    var l1item = new SEBAdditionalResourceMenuItem();
                    l1item.Text = l1resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                    l1item.Identifier = l1resource[SEBSettings.KeyIdentifier].ToString();
                    l1item.Click += OnItemClicked;

                    foreach (DictObj l2resource in ((ListObj)l1resource[SEBSettings.KeyAdditionalResources]))
                    {
                        var l2item = new SEBAdditionalResourceMenuItem();
                        l2item.Text = l2resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                        l2item.Identifier = l2resource[SEBSettings.KeyIdentifier].ToString();
                        l2item.Click += OnItemClicked;
                        l1item.DropDownItems.Add(l2item);
                    }
                    l0item.DropDownItems.Add(l1item);
                }
                menu.Items.Add(l0item);
            }
        }

        private void OnItemClicked(object sender, EventArgs e)
        {
            var item = sender as SEBAdditionalResourceMenuItem;

            if (item != null && item.Identifier != null)
            {
                SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.AdditionalResources, new { id = item.Identifier }));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            menu.Show(this.Parent,new Point(this.Bounds.X,this.Bounds.Y));
        }
       
    }

}
