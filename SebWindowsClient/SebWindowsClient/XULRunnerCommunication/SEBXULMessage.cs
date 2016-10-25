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
            // 1. wenn der SEB File übertragen wurde: {Handler:"SebFileTransfer",Opts:false | true}
            // und danach
            // 2. die decryptisierte config Datei: {Handler:"Reconfigure",Opts:{configBase64:"BASE64STRING....."}}
            Reconfigure,
            SebFileTransfer,
            AdditionalResourceTriggered
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
