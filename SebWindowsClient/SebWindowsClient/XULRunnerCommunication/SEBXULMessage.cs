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
            Reconfigure,
            SebFileTransfer
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
