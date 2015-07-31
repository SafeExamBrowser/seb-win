namespace SebWindowsClient.XULRunnerCommunication
{
    public enum SEBXULHandler
    {
        AdditionalResources
    }

    public class SEBXULMessage
    {
        public SEBXULMessage(SEBXULHandler handler, dynamic opts = null)
        {
            Handler = handler;
            Opts = opts;
        }
        public SEBXULHandler Handler { get; set; }
        public dynamic Opts { get; set; }
    }
}
