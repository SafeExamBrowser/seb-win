using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ionic.Zip;

namespace SebWindowsClient.ConfigurationUtils
{
    public class FileCompressor : IFileCompressor
    {
        private static readonly string TempDirectory = SEBClientInfo.SebClientSettingsAppDataDirectory + "temp\\";
        private static readonly string TempIconFilename = SEBClientInfo.SebClientSettingsAppDataDirectory + "temp\\tempIcon.png";

        public static void CleanupTempDirectory()
        {
            try
            {
                if (Directory.Exists(TempDirectory))
                {
                    Directory.Delete(TempDirectory, true);
                }
            }
            catch (Exception)
            {
                SEBMessageBox.Show("Could not cleanup temp directory", "Could not cleanup temp directory",
                    MessageBoxIcon.Error, MessageBoxButtons.OK);
            }
        }
        public string CompressAndEncodeFile(string filename)
        {
            var zip = new ZipFile();
            zip.AddFile(filename,"");
            var stream = new MemoryStream();
            zip.Save(stream);
            return base64_encode(stream.ToArray());
        }

        public string CompressAndEncodeIcon(Icon icon)
        {
            //Save the file first locally
            if (File.Exists(TempIconFilename))
            {
                File.Delete(TempIconFilename);
            }
            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
            icon.ToBitmap().Save(TempIconFilename, ImageFormat.Png);

            return CompressAndEncodeFile(TempIconFilename);
        }

        public string CompressAndEncodeFavicon(Uri uri)
        {
            var client = new System.Net.WebClient();
            client.DownloadFile(
                string.Format(@"http://www.google.com/s2/favicons?domain_url={0}", uri.Host),
                TempIconFilename);
            return CompressAndEncodeFile(TempIconFilename);
        }

        public string CompressAndEncodeDirectory(string path, out List<string> containingFilenames)
        {
            var zip = new ZipFile();
            zip.AddDirectory(path, "");
            var stream = new MemoryStream();
            zip.Save(stream);
            containingFilenames = zip.Entries.Select(x => x.FileName.Replace(path, "")).ToList();
            return base64_encode(stream.ToArray());
        }

        /// <summary>
        /// Saves the file to a temporary directory and returns the path to the file (without filename)
        /// </summary>
        /// <param name="base64">the encoded and compressed file content</param>
        /// <param name="filename">the filename of the file to save</param>
        /// <param name="directoryName">the subdirectory of the tempdir (usually the id of the additional resource</param>
        /// <returns></returns>
        public string DecompressDecodeAndSaveFile(string base64, string filename, string directoryName)
        {
            string tempPath = TempDirectory + directoryName + "\\";
            if (Directory.Exists(tempPath))
            {
                return tempPath;
            }
            Directory.CreateDirectory(tempPath);

            var data = base64_decode(base64);
            var stream = new MemoryStream(data);
            var zip = ZipFile.Read(stream);
            zip.ExtractAll(tempPath);

            return tempPath;
        }

        public MemoryStream DeCompressAndDecode(string base64)
        {
            var data = base64_decode(base64);
            var zipStream = new MemoryStream(data);
            var zip = ZipFile.Read(zipStream);
            var stream = new MemoryStream();
            zip.Entries.First().Extract(stream);
            return stream;
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
