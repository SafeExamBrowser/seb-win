using System;
using System.Diagnostics;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.XULRunnerCommunication;
using DictObj = System.Collections.Generic.Dictionary<string, object>;
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
                {
                    return resource;
                }
                else if (((ListObj) resource[SEBSettings.KeyAdditionalResources]).Count > 0)
                {
                    var res = FindResourceById(id, (ListObj) resource[SEBSettings.KeyAdditionalResources]);
                    if (res != null)
                    {
                        return res;
                    }
                } 
            }
            return null;
        }

        private void OpenResource(DictObj resource)
        {
			var showMessage = resource[SEBSettings.KeyAdditionalResourcesConfirm] is true;

			if (showMessage)
			{
				var title = resource[SEBSettings.KeyAdditionalResourcesTitle] as string;
				var customMessage = resource[SEBSettings.KeyAdditionalResourcesConfirmText] as string;
				var defaultMessage = SEBUIStrings.AdditionalResourceConfirmMessage;
				var message = (String.IsNullOrWhiteSpace(customMessage) ? defaultMessage : customMessage).Replace("%%TITLE%%", title);
				var result = SEBMessageBox.Show(SEBUIStrings.AdditionalResourceConfirmTitle, message, MessageBoxIcon.Question, MessageBoxButtons.YesNo);

				if (result != DialogResult.Yes)
				{
					return;
				}
			}

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
                    SEBMessageBox.Show(SEBUIStrings.errorOpeningResource, ex.Message, MessageBoxIcon.Error, MessageBoxButtons.OK);
                }
            }
        }
    }
}