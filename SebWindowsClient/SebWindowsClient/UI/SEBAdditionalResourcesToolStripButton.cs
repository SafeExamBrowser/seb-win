using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.Properties;
using SebWindowsClient.XULRunnerCommunication;
using ListObj = System.Collections.Generic.List<object>;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsClient.UI
{
    public class SEBAdditionalResourcesToolStripButton : SEBToolStripButton
    {
        private readonly ContextMenuStrip _menu;
        private readonly IFileCompressor _fileCompressor;
        private DictObj L0Resource;

        public SEBAdditionalResourcesToolStripButton(DictObj l0Resource)
        {
            this.L0Resource = l0Resource;

            _fileCompressor = new FileCompressor();

            InitializeComponent();

            _menu = new ContextMenuStrip();
            if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyTouchOptimized])
            {
                _menu.Font = new Font("Segoe UI", 36F);
                _menu.ShowImageMargin = true;
                _menu.ImageScalingSize = new Size(48, 48);   
            }

            LoadItems();
        }

        ~SEBAdditionalResourcesToolStripButton()
        {
            FileCompressor.CleanupTempDirectory();
        }

        private void InitializeComponent()
        {
            AutoOpenResource(L0Resource);
            base.Image = GetResourceIcon(L0Resource);
            this.ToolTipText = L0Resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
        }

        private void LoadItems()
        {
            foreach (DictObj l1Resource in ((ListObj)L0Resource[SEBSettings.KeyAdditionalResources]))
            {
                if (!(bool)l1Resource[SEBSettings.KeyAdditionalResourcesActive])
                    continue;

                var l1Item = new SEBAdditionalResourceMenuItem(l1Resource);
                l1Item.Image = GetResourceIcon(l1Resource);
                l1Item.Click += OnItemClicked;
                AutoOpenResource(l1Resource);

                foreach (DictObj l2Resource in ((ListObj)l1Resource[SEBSettings.KeyAdditionalResources]))
                {
                    if (!(bool)l2Resource[SEBSettings.KeyAdditionalResourcesActive])
                        continue;

                    var l2Item = new SEBAdditionalResourceMenuItem(l2Resource);
                    l2Item.Image = GetResourceIcon(l2Resource);
                    l2Item.Click += OnItemClicked;
                    AutoOpenResource(l2Resource);

                    l1Item.DropDownItems.Add(l2Item);
                }
                _menu.Items.Add(l1Item);
            }   
        }

        private void OnItemClicked(object sender, EventArgs e)
        {
            var item = sender as SEBAdditionalResourceMenuItem;

            if (item != null && item.Resource != null)
            {
                OpenResource(item.Resource);
            }
        }

        private void OpenResource(DictObj resource)
        {
            if (!string.IsNullOrEmpty((string)resource[SEBSettings.KeyAdditionalResourcesResourceData]))
            {
                OpenEmbededResource(resource);
            }
            else if (!string.IsNullOrEmpty((string)resource[SEBSettings.KeyAdditionalResourcesUrl]))
            {
                SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.AdditionalResources, new { id = resource[SEBSettings.KeyAdditionalResourcesIdentifier] }));
            }
        }

        private Image GetResourceIcon(DictObj resource)
        {
            if (((ListObj)resource[SEBSettings.KeyAdditionalResourcesResourceIcons]).Count > 0)
            {
                var icon =
                    (DictObj)((ListObj)resource[SEBSettings.KeyAdditionalResourcesResourceIcons])[0];
                var memoryStream =
                        _fileCompressor.DeCompressAndDecode(
                            (string)icon[SEBSettings.KeyAdditionalResourcesResourceIconsIconData]);
                return Image.FromStream(memoryStream);
            }

            return Resources.resource;
        }

        private void OpenEmbededResource(DictObj resource)
        {
            var launcher = (int) resource[SEBSettings.KeyAdditionalResourcesResourceDataLauncher];
            var filename = (string) resource[SEBSettings.KeyAdditionalResourcesResourceDataFilename];
            var path =
                _fileCompressor.DecompressDecodeAndSaveFile(
                    (string)resource[SEBSettings.KeyAdditionalResourcesResourceData], filename, resource[SEBSettings.KeyAdditionalResourcesIdentifier].ToString());
            //XulRunner
            if (launcher == 0)
            {
                SEBXULRunnerWebSocketServer.SendMessage(
                    new SEBXULMessage(
                        SEBXULMessage.SEBXULHandler.AdditionalResources, new
                        {
                            id = resource[SEBSettings.KeyAdditionalResourcesIdentifier], 
                            path = path
                        }
                    )
                );
            }
            else
            {
                var permittedProcess = (DictObj)SEBSettings.permittedProcessList[launcher];
                var fullPath = SEBClientInfo.SebWindowsClientForm.GetPermittedApplicationPath(permittedProcess);
                try
                {
                    Process process = SEBClientInfo.SebWindowsClientForm.CreateProcessWithExitHandler(string.Join(" ", fullPath, "\"" + path + filename + "\""));
                    if (SEBClientInfo.SebWindowsClientForm.permittedProcessesReferences[launcher] == null)
                    {
                        SEBClientInfo.SebWindowsClientForm.permittedProcessesReferences[launcher] = process;
                    }
                }
                catch (Exception ex)
                {
                    SEBMessageBox.Show("Error", ex.Message, MessageBoxIcon.Error, MessageBoxButtons.OK);
                }
            }
        }

        private void AutoOpenResource(DictObj resource)
        {
            if ((bool) resource[SEBSettings.KeyAdditionalResourcesAutoOpen])
            {
                OpenResource(resource);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            if (_menu.Items.Count > 0)
            {
                _menu.Show(Parent, new Point(Bounds.X, Bounds.Y));
            }
            else
            {
                OpenResource(L0Resource);   
            }
        }
       
    }

}
