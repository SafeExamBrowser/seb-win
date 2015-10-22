using System.Collections.Generic;
using System.IO;

namespace SebWindowsClient.ConfigurationUtils
{
    public interface IFileCompressor
    {
        string CompressAndEncodeFile(string filename);

        string CompressAndEncodeDirectory(string path, out List<string> containingFileNames);

        string DecompressDecodeAndSaveFile(string base64, string filename, string subdirectory);

        MemoryStream DeCompressAndDecode(string base64);
    }
}
