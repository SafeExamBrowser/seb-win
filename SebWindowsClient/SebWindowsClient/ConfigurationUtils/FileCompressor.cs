using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;

namespace SebWindowsClient.ConfigurationUtils
{
    public class FileCompressor : IFileCompressor
    {
        public string CompressAndEncodeFile(string filename)
        {
            return base64_encode(CompressFile(File.ReadAllBytes(filename)));
        }

        public string CompressAndEncodeDirectory(string path, out List<string> containingFilenames)
        {
            List<string> containingFileNames;
            var compressedBytes = CompressDirectory(path, out containingFilenames);
            return base64_encode(compressedBytes);
        }

        public byte[] DeCompressAndDecode(string base64)
        {
            return Decompress(base64_decode(base64));
        }
        /// <summary>
        /// Saves the file to a temporary directory and returns the path to the file (without filename)
        /// </summary>
        /// <param name="base64">the encoded and compressed file content</param>
        /// <param name="filename">the filename of the file to save</param>
        /// <returns></returns>
        public string DecompressDecodeAndSaveFile(string base64, string filename)
        {
            string tempPath = SEBClientInfo.SebClientSettingsAppDataDirectory + "\\temp\\";
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            if (!File.Exists(tempPath + filename))
            {
                File.WriteAllBytes(tempPath + filename, DeCompressAndDecode(base64));
            }
            return tempPath;
        }

        private byte[] CompressFile(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private byte[] CompressDirectory(string path, out List<string> containingFilenames)
        {
            containingFilenames = new List<string>();
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                foreach (var filename in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    containingFilenames.Add(filename.Replace(path,""));
                    var data = File.ReadAllBytes(filename);
                    zipStream.Write(data, 0, data.Length);
                }
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                var buffer = new byte[4096];
                int read;

                while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    resultStream.Write(buffer, 0, read);
                }

                return resultStream.ToArray();
            }
        }
        private string base64_encode(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            return Convert.ToBase64String(data);
        }
        private byte[] base64_decode(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            return encodedDataAsBytes;
        }
    }
}
