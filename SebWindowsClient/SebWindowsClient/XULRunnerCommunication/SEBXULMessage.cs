using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SebWindowsClient.XULRunnerCommunication
{
	public class SEBXULMessage
    {
        public enum SEBXULHandler
        {
            AdditionalResources,
            DisplaySettingsChanged,
            Reload,
            RestartExam,
            Close,
            KeyboardShown,
            ClearSession,
            // 1. when the SEB file was received: {Handler:"SebFileTransfer",Opts:false | true}
            // and afterwards
            // 2.send the decrypted config file: {Handler:"Reconfigure",Opts:{configBase64:"BASE64STRING....."}}
            Reconfigure,
            SebFileTransfer,
            AdditionalRessourceTriggered,
            ReconfigureAborted,
            FullScreenChanged,
            ReconfigureSuccess,
			ClearClipboard,
			UserSwitchLockScreen
		}

        public SEBXULMessage(SEBXULHandler handler, dynamic opts = null)
        {
            Handler = handler;
            Opts = opts;
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public SEBXULHandler Handler { get; set; }
        public dynamic Opts { get; set; }
    }
}
