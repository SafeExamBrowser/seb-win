namespace SebWindowsClient.XULRunnerCommunication
{
    public class SEBXULMessage
    {
        public enum SEBXULHandler
        {
            AdditionalResources
        }

        public SEBXULMessage(SEBXULHandler handler, dynamic opts = null)
        {
            Handler = handler;
            Opts = opts;
        }
        public SEBXULHandler Handler { get; set; }
        public dynamic Opts { get; set; }
    }
}
