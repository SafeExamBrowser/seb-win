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

        public SEBAdditionalResourcesToolStripButton()
        {
            InitializeComponent();

            FileCompressor.CleanupTempDirectory();

            _menu = new ContextMenuStrip();
            if ((bool) SEBSettings.settingsCurrent[SEBSettings.KeyTouchOptimized])
            {
                _menu.Font = new Font("Segoe UI", 36F);
                _menu.ShowImageMargin = true;
                _menu.ImageScalingSize = new Size(48, 48);   
            }

            _fileCompressor = new FileCompressor();

            LoadItems();
        }

        ~SEBAdditionalResourcesToolStripButton()
        {
            FileCompressor.CleanupTempDirectory();
        }

        private void InitializeComponent()
        {
            base.Image = Resources.quit;
        }

        private void LoadItems()
        {
            foreach (DictObj l0Resource in ((ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources]))
            {
                if (!(bool)l0Resource[SEBSettings.KeyAdditionalResourcesActive])
                    continue;

                var l0Item = new SEBAdditionalResourceMenuItem();
                l0Item.Text = l0Resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                l0Item.Resource = l0Resource;
                l0Item.Click += OnItemClicked;
                AutoOpenResource(l0Resource);
                l0Item.Image = GetResourceIcon(l0Resource);

                foreach (DictObj l1Resource in ((ListObj)l0Resource[SEBSettings.KeyAdditionalResources]))
                {
                    if (!(bool)l1Resource[SEBSettings.KeyAdditionalResourcesActive])
                        continue;

                    var l1Item = new SEBAdditionalResourceMenuItem();
                    l1Item.Text = l1Resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                    l1Item.Resource = l1Resource;
                    l1Item.Click += OnItemClicked;
                    AutoOpenResource(l1Resource);

                    foreach (DictObj l2Resource in ((ListObj)l1Resource[SEBSettings.KeyAdditionalResources]))
                    {
                        if (!(bool)l0Resource[SEBSettings.KeyAdditionalResourcesActive])
                            continue;

                        var l2Item = new SEBAdditionalResourceMenuItem();
                        l2Item.Text = l2Resource[SEBSettings.KeyAdditionalResourcesTitle].ToString();
                        l2Item.Resource = l2Resource;
                        l2Item.Click += OnItemClicked;
                        AutoOpenResource(l2Resource);

                        l1Item.DropDownItems.Add(l2Item);
                    }
                    l0Item.DropDownItems.Add(l1Item);
                }
                _menu.Items.Add(l0Item);
            }
        }

        private void OnItemClicked(object sender, EventArgs e)
        {
            var item = sender as SEBAdditionalResourceMenuItem;

            if (item != null && item.Resource != null)
            {
                if (!string.IsNullOrEmpty((string) item.Resource[SEBSettings.KeyAdditionalResourcesResourceData]))
                {
                    OpenEmbededResource(item.Resource);
                }
                else if (!string.IsNullOrEmpty((string)item.Resource[SEBSettings.KeyAdditionalResourcesUrl]))
                {
                    SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.AdditionalResources, new { id = item.Resource[SEBSettings.KeyAdditionalResourcesIdentifier] }));
                }
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

            return Resources.SebGlobalDialogImage;
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
            if ((bool) resource[SEBSettings.KeyAdditionalResourcesAutoOpen] && !string.IsNullOrEmpty((string)resource[SEBSettings.KeyAdditionalResourcesResourceData]))
            {
                OpenEmbededResource(resource);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            _menu.Show(Parent,new Point(Bounds.X,Bounds.Y));
        }
       
    }

}
