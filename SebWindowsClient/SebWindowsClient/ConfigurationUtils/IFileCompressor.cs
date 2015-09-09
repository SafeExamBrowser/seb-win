namespace SebWindowsClient.ConfigurationUtils
{
    public interface IFileCompressor
    {
        string CompressAndEncode(string filename);
        byte[] DeCompressAndDecode(string base64);
    }
}
