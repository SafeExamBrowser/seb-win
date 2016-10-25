using System;
using System.Diagnostics;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.XULRunnerCommunication;
using DictObj = System.Collections.Generic.Dictionary<string,object>;
using ListObj = System.Collections.Generic.List<object>;

namespace SebWindowsClient.AdditionalResourcesUtils
{
    public class AdditionalResourceHandler : IAdditionalResourceHandler
    {
        private readonly IFileCompressor _fileCompressor;

        public AdditionalResourceHandler()
        {
            _fileCompressor = new FileCompressor();
        }

        ~AdditionalResourceHandler()
        {
            FileCompressor.CleanupTempDirectory();
        }

        public void OpenAdditionalResourceById(string id)
        {
            OpenResource(FindResourceById(id));
        }

        public void OpenAdditionalResource(DictObj resource)
        {
            OpenResource(resource);
        }

        private DictObj FindResourceById(string id, ListObj recursiveResources = null)
        {
            ListObj resources = recursiveResources ?? (ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources];
            foreach (DictObj resource in resources)
            {
                if ((string) resource[SEBSettings.KeyAdditionalResourcesIdentifier] == id)
                    return resource;
                else if (((ListObj) resource[SEBSettings.KeyAdditionalResources]).Count > 0)
                    return FindResourceById(id, (ListObj) resource[SEBSettings.KeyAdditionalResources]);
            }
            return null;
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

        private void OpenEmbededResource(DictObj resource)
        {
            var launcher = (int)resource[SEBSettings.KeyAdditionalResourcesResourceDataLauncher];
            var filename = (string)resource[SEBSettings.KeyAdditionalResourcesResourceDataFilename];
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
    }
}