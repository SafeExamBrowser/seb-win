﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace SebWindowsClient.ConfigurationUtils
{
    public interface IFileCompressor
    {
        string CompressAndEncodeFile(string filename);

        string CompressAndEncodeIcon(Icon icon);
        string CompressAndEncodeFavicon(Uri uri);

        string CompressAndEncodeDirectory(string path, out List<string> containingFileNames);

        string DecompressDecodeAndSaveFile(string base64, string filename, string directoryName);

        MemoryStream DeCompressAndDecode(string base64);
    }
}
