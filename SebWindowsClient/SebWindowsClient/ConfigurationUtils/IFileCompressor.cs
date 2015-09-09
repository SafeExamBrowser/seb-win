namespace SebWindowsClient.ConfigurationUtils
{
    public interface IFileCompressor
    {
        string CompressAndEncode(string filename);

        void OpenCompressedAndEncodedFile(string base64, string filename);
    }
}
