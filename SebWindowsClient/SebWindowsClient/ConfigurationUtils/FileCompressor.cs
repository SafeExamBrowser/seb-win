using System;
using System.IO;
using System.IO.Compression;

namespace SebWindowsClient.ConfigurationUtils
{
    public class FileCompressor : IFileCompressor
    {
        public string CompressAndEncode(string filename)
        {
            return base64_encode(Compress(File.ReadAllBytes(filename)));
        }

        public byte[] DeCompressAndDecode(string base64)
        {
            return Decompress(base64_decode(base64));
        }
        private byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
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
