using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using ListObj = System.Collections.Generic.List<object>;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsConfig.Sections
{
    public partial class AdditionalResources : UserControl
    {
        private IFileCompressor _fileCompressor;

        public AdditionalResources()
        {
            InitializeComponent();

            _fileCompressor = new FileCompressor();

            groupBoxAdditionalResourceDetails.Visible = false;

            SEBSettings.additionalResourcesList = (ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources];
            textBoxAdditionalResourcesTitle.Text = "";
            treeViewAdditionalResources.Nodes.Clear();
            foreach (DictObj l0Resource in SEBSettings.additionalResourcesList)
            {
                var l0Node = treeViewAdditionalResources.Nodes.Add(l0Resource[SEBSettings.KeyAdditionalResourcesIdentifier].ToString(), GetDisplayTitle(l0Resource));
                foreach (DictObj l1Resource in (ListObj)l0Resource[SEBSettings.KeyAdditionalResources])
                {
                    var l1Node = l0Node.Nodes.Add(l1Resource[SEBSettings.KeyAdditionalResourcesIdentifier].ToString(), GetDisplayTitle(l1Resource));
                    foreach (DictObj l2Resource in (ListObj)l1Resource[SEBSettings.KeyAdditionalResources])
                    {
                        l1Node.Nodes.Add(l2Resource[SEBSettings.KeyAdditionalResourcesIdentifier].ToString(), GetDisplayTitle(l2Resource));
                    }
                }
            }
        }

        private string GetDisplayTitle(DictObj resource)
        {
            return string.Concat( 
                resource[SEBSettings.KeyAdditionalResourcesTitle],
                (bool) resource[SEBSettings.KeyAdditionalResourcesActive] ? "" : " (inactive)",
                (bool) resource[SEBSettings.KeyAdditionalResourcesAutoOpen] ? " (A)" : "",
                !string.IsNullOrEmpty((string)resource[SEBSettings.KeyAdditionalResourcesResourceData]) ? " (E)" : "",
                !string.IsNullOrEmpty((string)resource[SEBSettings.KeyAdditionalResourcesUrl]) ? " (U)" : "");
        }

        private void buttonAdditionalResourcesAdd_Click(object sender, EventArgs e)
        {
            // Get the process list
            SEBSettings.additionalResourcesList = (ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources];

            int newIndex = treeViewAdditionalResources.Nodes.Count;
            SEBSettings.additionalResourcesList.Insert(newIndex, CreateNewResource(newIndex.ToString()));

            treeViewAdditionalResources.SelectedNode = treeViewAdditionalResources.Nodes.Add(newIndex.ToString(), "New Resource");
            treeViewAdditionalResources.Focus();
        }

        private DictObj CreateNewResource(string identifier)
        {
            DictObj resourceData = new DictObj();
            resourceData[SEBSettings.KeyAdditionalResourcesIdentifier] = identifier;
            return SetDefaultValuesOnResource(resourceData);
        }

        private void buttonAdditionalResourcesAddSubResource_Click(object sender, EventArgs e)
        {
            var node = treeViewAdditionalResources.SelectedNode;
            if (node == null)
                MessageBox.Show("No node selected");
            if (node.Level == 2)
                MessageBox.Show("Maximum 3 levels");

            var selectedResource = GetSelectedResource();
            ListObj resourceList = (ListObj)selectedResource[SEBSettings.KeyAdditionalResources];

            var newIndex = node.Nodes.Count;
            if (node.Level == 0)
            {
                resourceList.Add(CreateNewResource(node.Index + "." + newIndex));
                treeViewAdditionalResources.SelectedNode = treeViewAdditionalResources.SelectedNode.Nodes.Add(node.Index + "." + newIndex, "New Resource");
            }
            if (node.Level == 1)
            {
                resourceList.Add(CreateNewResource(node.Parent.Index + "." + node.Index + "." + newIndex));
                treeViewAdditionalResources.SelectedNode = treeViewAdditionalResources.SelectedNode.Nodes.Add(node.Parent.Index + "." + node.Index + "." + newIndex, "New Resource");
            }
            treeViewAdditionalResources.Focus();
        }

        private void treeViewAdditionalResources_AfterSelect(object sender, TreeViewEventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            if (selectedResource != null)
            {
                textBoxAdditionalResourcesTitle.Text = (string)selectedResource[SEBSettings.KeyAdditionalResourcesTitle];
                checkBoxAdditionalResourceActive.Checked = (bool)selectedResource[SEBSettings.KeyAdditionalResourcesActive];
                textBoxAdditionalResourceUrl.Text = (string)selectedResource[SEBSettings.KeyAdditionalResourcesUrl];
                checkBoxAdditionalResourceAutoOpen.Checked = (bool)selectedResource[SEBSettings.KeyAdditionalResourcesAutoOpen];

                if (!string.IsNullOrEmpty((string)selectedResource[SEBSettings.KeyAdditionalResourcesResourceData]))
                {
                    buttonAdditionalResourceRemoveResourceData.Visible = true;
                    buttonAdditionalResourceEmbededResourceOpen.Visible = true;
                    labelAdditionalResourcesResourceDataLaunchWith.Visible = true;
                    comboBoxAdditionalResourcesResourceDataLauncher.Visible = true;
                    textBoxAdditionalResourceUrl.Enabled = false;

                    var indexBefore = (int) selectedResource[SEBSettings.KeyAdditionalResourcesResourceDataLauncher];
                    comboBoxAdditionalResourcesResourceDataLauncher.DataSource = GetLaunchers();
                    comboBoxAdditionalResourcesResourceDataLauncher.SelectedIndex = indexBefore;
                }
                else
                {
                    buttonAdditionalResourceRemoveResourceData.Visible = false;
                    buttonAdditionalResourceEmbededResourceOpen.Visible = false;
                    labelAdditionalResourcesResourceDataLaunchWith.Visible = false;
                    comboBoxAdditionalResourcesResourceDataLauncher.Visible = false;
                    textBoxAdditionalResourceUrl.Enabled = true;
                }

                if (!string.IsNullOrEmpty((string) selectedResource[SEBSettings.KeyAdditionalResourcesUrl]))
                {
                    buttonAdditionalResourceChooseEmbededResource.Enabled = false;
                }
                else
                {
                    buttonAdditionalResourceChooseEmbededResource.Enabled = true;
                }

                if (((ListObj) selectedResource[SEBSettings.KeyAdditionalResourcesResourceIcons]).Count > 0)
                {
                    var icon =
                        (DictObj) ((ListObj) selectedResource[SEBSettings.KeyAdditionalResourcesResourceIcons])[0];
                    var memoryStream =
                        new MemoryStream(
                            _fileCompressor.DeCompressAndDecode(
                                (string) icon[SEBSettings.KeyAdditionalResourcesResourceIconsIconData]));
                    var image = Image.FromStream(memoryStream);
                    pictureBoxAdditionalResourceIcon.Image = image;
                }
                else
                {
                    pictureBoxAdditionalResourceIcon.Image = null;
                }
                
            }
            groupBoxAdditionalResourceDetails.Visible = selectedResource != null;
        }

        private List<string> GetLaunchers()
        {
            var res = new List<string>();
            foreach (DictObj permittedProcess in SEBSettings.permittedProcessList)
            {
                res.Add((string)permittedProcess[SEBSettings.KeyTitle]);
            }
            return res;
        }

        private DictObj GetSelectedResource()
        {
            var node = treeViewAdditionalResources.SelectedNode;

            if (node.Level == 0)
            {
                return SetDefaultValuesOnResource((DictObj)SEBSettings.additionalResourcesList[node.Index]);
            }
            else if (node.Level == 1)
            {
                DictObj rootResource = (DictObj)SEBSettings.additionalResourcesList[node.Parent.Index];
                ListObj level1List = (ListObj)rootResource[SEBSettings.KeyAdditionalResources];
                return SetDefaultValuesOnResource((DictObj)level1List[node.Index]);
            }
            else if (node.Level == 2)
            {
                DictObj rootResource = (DictObj)SEBSettings.additionalResourcesList[treeViewAdditionalResources.SelectedNode.Parent.Parent.Index];
                ListObj level1List = (ListObj)rootResource[SEBSettings.KeyAdditionalResources];
                DictObj level1Resource = (DictObj)level1List[treeViewAdditionalResources.SelectedNode.Parent.Index];
                ListObj level2List = (ListObj)level1Resource[SEBSettings.KeyAdditionalResources];
                return SetDefaultValuesOnResource((DictObj)level2List[node.Index]);
            }
            return null;
        }

        private DictObj SetDefaultValuesOnResource(DictObj resourceData)
        {
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResources))
                resourceData[SEBSettings.KeyAdditionalResources] = new ListObj();
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesActive))
                resourceData[SEBSettings.KeyAdditionalResourcesActive] = true;
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesAutoOpen))
                resourceData[SEBSettings.KeyAdditionalResourcesAutoOpen] = false;
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesIdentifier))
                resourceData[SEBSettings.KeyAdditionalResourcesIdentifier] = "";
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesResourceIcons))
                resourceData[SEBSettings.KeyAdditionalResourcesResourceIcons] = new ListObj();
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesTitle))
                resourceData[SEBSettings.KeyAdditionalResourcesTitle] = "New Resource";
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesUrl))
                resourceData[SEBSettings.KeyAdditionalResourcesUrl] = "";
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesResourceData))
                resourceData[SEBSettings.KeyAdditionalResourcesResourceData] = "";
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesResourceDataFilename))
                resourceData[SEBSettings.KeyAdditionalResourcesResourceDataFilename] = "";
            if(!resourceData.ContainsKey(SEBSettings.KeyAdditionalResourcesResourceDataLauncher))
                resourceData[SEBSettings.KeyAdditionalResourcesResourceDataLauncher] = 0;

            return resourceData;
        }

        private void UpdateAdditionalResourceIdentifiers()
        {
            foreach (TreeNode l0Node in treeViewAdditionalResources.Nodes)
            {
                DictObj l0resource = (DictObj)SEBSettings.additionalResourcesList[l0Node.Index];
                l0resource[SEBSettings.KeyAdditionalResourcesIdentifier] = l0Node.Index.ToString();
                foreach (TreeNode l1Node in l0Node.Nodes)
                {
                    ListObj l1resources = (ListObj)l0resource[SEBSettings.KeyAdditionalResources];
                    DictObj l1resource = (DictObj) l1resources[l1Node.Index];
                    l1resource[SEBSettings.KeyAdditionalResourcesIdentifier] = l0Node.Index + "." + l1Node.Index;
                    foreach (TreeNode l2Node in l1Node.Nodes)
                    {
                        ListObj l2resources = (ListObj)l1resource[SEBSettings.KeyAdditionalResources];
                        DictObj l2resource = (DictObj)l2resources[l2Node.Index];
                        l2resource[SEBSettings.KeyAdditionalResourcesIdentifier] = l0Node.Index + "." + l1Node.Index + "." + l2Node.Index;
                    }
                }
            }
        }

        private void buttonAdditionalResourcesMoveUp_Click(object sender, EventArgs e)
        {
            var nodeToMove = treeViewAdditionalResources.SelectedNode;
            if (nodeToMove.Index == 0)
                return;

            var oldIndex = nodeToMove.Index;

            var parent = treeViewAdditionalResources.SelectedNode.Parent;
            if (parent == null)
            {
                var nodeToMoveDown = treeViewAdditionalResources.Nodes[oldIndex - 1];
                treeViewAdditionalResources.Nodes.RemoveAt(oldIndex - 1);
                treeViewAdditionalResources.Nodes.Insert(oldIndex, nodeToMoveDown);
                DictObj resourceToMoveDown = (DictObj)SEBSettings.additionalResourcesList[oldIndex - 1];
                SEBSettings.additionalResourcesList.RemoveAt(oldIndex -1);
                SEBSettings.additionalResourcesList.Insert(oldIndex, resourceToMoveDown);
            }
            else
            {
                var nodeToMoveDown = parent.Nodes[oldIndex - 1];
                parent.Nodes.RemoveAt(oldIndex - 1);
                parent.Nodes.Insert(oldIndex, nodeToMoveDown);
                DictObj parentResource = new DictObj();
                if (parent.Level == 0)
                {
                    parentResource = (DictObj)SEBSettings.additionalResourcesList[parent.Index];
                }
                if (parent.Level == 1)
                {
                    DictObj l0Resource = (DictObj)SEBSettings.additionalResourcesList[parent.Parent.Index];
                    ListObj l0ResourcesList = (ListObj)l0Resource[SEBSettings.KeyAdditionalResources];
                    parentResource = (DictObj)l0ResourcesList[parent.Index];
                }
                ListObj parentResourceList = (ListObj) parentResource[SEBSettings.KeyAdditionalResources];
                DictObj resourceToMoveDown = (DictObj)parentResourceList[oldIndex - 1];
                parentResourceList.RemoveAt(oldIndex -1);
                parentResourceList.Insert(oldIndex, resourceToMoveDown);
            }

            UpdateAdditionalResourceIdentifiers();
        }

        private void buttonAdditionalResourcesMoveDown_Click(object sender, EventArgs e)
        {
            var nodeToMove = treeViewAdditionalResources.SelectedNode;

            var oldIndex = nodeToMove.Index;

            var parent = treeViewAdditionalResources.SelectedNode.Parent;
            if (parent == null)
            {
                if (nodeToMove.Index == treeViewAdditionalResources.Nodes.Count -1)
                    return;
                var nodeToMoveUp = treeViewAdditionalResources.Nodes[oldIndex + 1];
                treeViewAdditionalResources.Nodes.RemoveAt(oldIndex + 1);
                treeViewAdditionalResources.Nodes.Insert(oldIndex, nodeToMoveUp);
                DictObj resourceToMoveUp = (DictObj) SEBSettings.additionalResourcesList[oldIndex + 1];
                SEBSettings.additionalResourcesList.RemoveAt(oldIndex + 1);
                SEBSettings.additionalResourcesList.Insert(oldIndex, resourceToMoveUp);
            }
            else
            {
                if (nodeToMove.Index == parent.Nodes.Count -1 )
                    return;
                var nodeToMoveUp = parent.Nodes[nodeToMove.Index + 1];
                parent.Nodes.RemoveAt(nodeToMove.Index + 1);
                parent.Nodes.Insert(oldIndex, nodeToMoveUp);
                DictObj parentResource = new DictObj();
                if (parent.Level == 0)
                {
                    parentResource = (DictObj)SEBSettings.additionalResourcesList[parent.Index];
                }
                if (parent.Level == 1)
                {
                    DictObj l0Resource = (DictObj)SEBSettings.additionalResourcesList[parent.Parent.Index];
                    ListObj l0ResourcesList = (ListObj)l0Resource[SEBSettings.KeyAdditionalResources];
                    parentResource = (DictObj)l0ResourcesList[parent.Index];
                }
                ListObj parentResourceList = (ListObj)parentResource[SEBSettings.KeyAdditionalResources];
                DictObj resourceToMoveDown = (DictObj)parentResourceList[oldIndex + 1];
                parentResourceList.RemoveAt(oldIndex + 1);
                parentResourceList.Insert(oldIndex, resourceToMoveDown);
            }

            UpdateAdditionalResourceIdentifiers();
        }

        private void buttonadditionalResourcesRemove_Click(object sender, EventArgs e)
        {
            var node = treeViewAdditionalResources.SelectedNode;

            if (node.Level == 0)
            {
                SEBSettings.additionalResourcesList.RemoveAt(node.Index);
            }
            else if (node.Level == 1)
            {
                DictObj rootResource = (DictObj)SEBSettings.additionalResourcesList[node.Parent.Index];
                ListObj level1List = (ListObj)rootResource[SEBSettings.KeyAdditionalResources];
                level1List.RemoveAt(node.Index);
            }
            else if (node.Level == 2)
            {
                DictObj rootResource = (DictObj)SEBSettings.additionalResourcesList[treeViewAdditionalResources.SelectedNode.Parent.Parent.Index];
                ListObj level1List = (ListObj)rootResource[SEBSettings.KeyAdditionalResources];
                DictObj level1Resource = (DictObj)level1List[treeViewAdditionalResources.SelectedNode.Parent.Index];
                ListObj level2List = (ListObj)level1Resource[SEBSettings.KeyAdditionalResources];
                level2List.RemoveAt(node.Index);
            }
            node.Remove();

            UpdateAdditionalResourceIdentifiers();
        }

        private void checkBoxAdditionalResourceActive_CheckedChanged(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            selectedResource[SEBSettings.KeyAdditionalResourcesActive] = checkBoxAdditionalResourceActive.Checked;

            treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);
        }

        private void checkBoxAdditionalResourceAutoOpen_CheckedChanged(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            selectedResource[SEBSettings.KeyAdditionalResourcesAutoOpen] = checkBoxAdditionalResourceAutoOpen.Checked;

            treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);
        }

        private void textBoxAdditionalResourcesTitle_TextChanged(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            selectedResource[SEBSettings.KeyAdditionalResourcesTitle] = textBoxAdditionalResourcesTitle.Text;

            treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);
        }

        private void textBoxAdditionalResourceUrl_TextChanged(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            selectedResource[SEBSettings.KeyAdditionalResourcesUrl] = textBoxAdditionalResourceUrl.Text;

            treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);

            buttonAdditionalResourceChooseEmbededResource.Enabled = string.IsNullOrEmpty(textBoxAdditionalResourceUrl.Text);
        }

        private void buttonAdditionalResourceChooseEmbededResource_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DictObj selectedResource = GetSelectedResource();
                selectedResource[SEBSettings.KeyAdditionalResourcesResourceDataFilename] = new FileInfo(openFileDialog.FileName).Name;
                selectedResource[SEBSettings.KeyAdditionalResourcesResourceData] = _fileCompressor.CompressAndEncode(openFileDialog.FileName);

                treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);

                buttonAdditionalResourceRemoveResourceData.Visible = true;
                buttonAdditionalResourceEmbededResourceOpen.Visible = true;
                labelAdditionalResourcesResourceDataLaunchWith.Visible = true;
                comboBoxAdditionalResourcesResourceDataLauncher.Visible = true;

                textBoxAdditionalResourceUrl.Text = "";
                textBoxAdditionalResourceUrl.Enabled = false;

                comboBoxAdditionalResourcesResourceDataLauncher.DataSource = GetLaunchers();
            }
        }

        private void buttonAdditionalResourceEmbededResourceOpen_Click(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            var filename = (string) selectedResource[SEBSettings.KeyAdditionalResourcesResourceDataFilename];
            var path =
                _fileCompressor.DecompressDecodeAndSaveFile(
                    (string) selectedResource[SEBSettings.KeyAdditionalResourcesResourceData], filename);
            Process.Start(path + filename);
        }

        private void buttonAdditionalResourceRemoveResourceData_Click(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();

            selectedResource[SEBSettings.KeyAdditionalResourcesResourceData] = "";
            
            treeViewAdditionalResources.SelectedNode.Text = GetDisplayTitle(selectedResource);

            buttonAdditionalResourceRemoveResourceData.Visible = false;
            buttonAdditionalResourceEmbededResourceOpen.Visible = false;
            labelAdditionalResourcesResourceDataLaunchWith.Visible = false;
            comboBoxAdditionalResourcesResourceDataLauncher.Visible = false;

            textBoxAdditionalResourceUrl.Enabled = true;
        }

        private void comboBoxAdditionalResourcesResourceDataLauncher_SelectedIndexChanged(object sender, EventArgs e)
        {
            DictObj selectedResource = GetSelectedResource();
            if ((int) selectedResource[SEBSettings.KeyAdditionalResourcesResourceDataLauncher] !=
                comboBoxAdditionalResourcesResourceDataLauncher.SelectedIndex)
            {
                selectedResource[SEBSettings.KeyAdditionalResourcesResourceDataLauncher] =
                    comboBoxAdditionalResourcesResourceDataLauncher.SelectedIndex;
            }
        }

        private void buttonAdditionalResourcesChooseIcon_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "PNG Images|*.png"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DictObj selectedResource = GetSelectedResource();

                var icon = new DictObj();
                icon[SEBSettings.KeyAdditionalResourcesResourceIconsIconData] =
                    _fileCompressor.CompressAndEncode(openFileDialog.FileName);
                icon[SEBSettings.KeyAdditionalResourcesResourceIconsFormat] = "png";

                var icons = (ListObj)selectedResource[SEBSettings.KeyAdditionalResourcesResourceIcons];
                if (icons.Count > 0)
                {
                    icons[0] = icon;
                }
                else
                {
                    icons.Add(icon);   
                }

                var memoryStream = new MemoryStream(_fileCompressor.DeCompressAndDecode((string)icon[SEBSettings.KeyAdditionalResourcesResourceIconsIconData]));
                var image = Image.FromStream(memoryStream);
                pictureBoxAdditionalResourceIcon.Image = image;
            }
        }
    }
}
