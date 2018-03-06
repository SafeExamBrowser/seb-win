using System;
using System.Drawing;
using System.Windows.Forms;
using SebWindowsClient.AdditionalResourcesUtils;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.Properties;
using DictObj = System.Collections.Generic.Dictionary<string, object>;
using ListObj = System.Collections.Generic.List<object>;

namespace SebWindowsClient.UI
{
	public class SEBAdditionalResourcesToolStripButton : SEBToolStripButton
    {
        private readonly ContextMenuStrip _menu;
        private readonly IFileCompressor _fileCompressor;
        private readonly IAdditionalResourceHandler _additionalResourceHandler;
        private DictObj L0Resource;

        public SEBAdditionalResourcesToolStripButton(DictObj l0Resource)
        {
            this.L0Resource = l0Resource;

            _fileCompressor = new FileCompressor();
            _additionalResourceHandler = new AdditionalResourceHandler();

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
				var showButton = l1Resource.TryGetValue(SEBSettings.KeyAdditionalResourcesShowButton, out object show) && show as bool? == true;

				if (!(bool)l1Resource[SEBSettings.KeyAdditionalResourcesActive] || !showButton)
                    continue;

                var l1Item = new SEBAdditionalResourceMenuItem(l1Resource);
                l1Item.Image = GetResourceIcon(l1Resource);
                l1Item.Click += OnItemClicked;
                AutoOpenResource(l1Resource);

                foreach (DictObj l2Resource in ((ListObj)l1Resource[SEBSettings.KeyAdditionalResources]))
                {
					var showButton2 = l1Resource.TryGetValue(SEBSettings.KeyAdditionalResourcesShowButton, out object show2) && show2 as bool? == true;

					if (!(bool)l2Resource[SEBSettings.KeyAdditionalResourcesActive] || !showButton2)
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
                _additionalResourceHandler.OpenAdditionalResource(item.Resource);
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

        private void AutoOpenResource(DictObj resource)
        {
            if ((bool) resource[SEBSettings.KeyAdditionalResourcesAutoOpen])
            {
                _additionalResourceHandler.OpenAdditionalResource(resource);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            _additionalResourceHandler.OpenAdditionalResource(L0Resource);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (_menu.Items.Count > 0)
            {
                _menu.Show(Parent, new Point(Bounds.X, Bounds.Y));
            }
            else
            {
                base.OnMouseHover(e);    
            }
        } 
       
    }

}
