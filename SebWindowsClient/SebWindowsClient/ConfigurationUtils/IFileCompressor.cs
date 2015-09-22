using System.IO;

namespace SebWindowsClient.ConfigurationUtils
{
    public interface IFileCompressor
    {
        string CompressAndEncode(string filename);

        string DecompressDecodeAndSaveFile(string base64, string filename);

        byte[] DeCompressAndDecode(string base64);
    }
}
