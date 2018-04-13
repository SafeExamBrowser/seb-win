using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using SebWindowsClient.DiagnosticsUtils;

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
					DeleteDirectory(TempDirectory);
					Logger.AddInformation("Successfully deleted temporary directory.");
				}
			}
			catch (Exception e)
			{
				Logger.AddError("Error when trying to delete temporary directory: ", null, e);
			}
		}

		/// <summary>
		/// Attempt to fix the issue happening when deleting the TempDirectory (see SEBWIN-49).
		/// Source: https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/1703799#1703799
		/// </summary>
		private static void DeleteDirectory(string path)
		{
			foreach (string directory in Directory.GetDirectories(path))
			{
				DeleteDirectory(directory);
			}

			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException e)
			{
				Logger.AddWarning(String.Format("Failed to delete {0} with IOException: {1}", path, e.Message), null);
				Thread.Sleep(100);
				Directory.Delete(path, true);
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.AddWarning(String.Format("Failed to delete {0} with UnauthorizedAccessException: {1}", path, e.Message), null);
				Thread.Sleep(100);
				Directory.Delete(path, true);
			}
		}

		public string CompressAndEncodeFile(string filename)
		{
		    using (var stream = new MemoryStream())
		    {
		        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
		        {
		            var entryName = Path.GetFileName(filename);
		            archive.CreateEntryFromFile(filename, entryName);
		        }
			return base64_encode(stream.ToArray());
		}
		}

		public string CompressAndEncodeIcon(Icon icon)
		{
			//Save the file first locally
			icon.ToBitmap().Save(TempIconFilename, ImageFormat.Png);

			return CompressAndEncodeFile(TempIconFilename);
		}

		public string CompressAndEncodeFavicon(Uri uri)
		{
			if (File.Exists(TempIconFilename))
			{
				File.Delete(TempIconFilename);
			}
			if (!Directory.Exists(TempDirectory))
			{
				Directory.CreateDirectory(TempDirectory);
			}

			var client = new System.Net.WebClient();
			client.DownloadFile(
				string.Format(@"http://www.google.com/s2/favicons?domain_url={0}", uri.Host),
				TempIconFilename);
			return CompressAndEncodeFile(TempIconFilename);
		}

		public string CompressAndEncodeDirectory(string path, out List<string> containingFileNames)
		{
		    using (var stream = new MemoryStream())
		{
		        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
		        {
		            var fileNames = Directory.GetFiles(path);
		            containingFileNames = new List<string>();
		            foreach (var fileName in fileNames)
		            {
		                var entryName = Path.GetFileName(fileName);
		                archive.CreateEntryFromFile(fileName, entryName);
		                containingFileNames.Add(entryName);
		            }
		        }
			return base64_encode(stream.ToArray());
		}
		}

		/// <summary>
		/// Compresses the entire specified directory (preserving its relative structure) and returns the data as Base64-encoded string.
		/// </summary>
		// TODO Verify this works correctly
		public string CompressAndEncodeEntireDirectory(string path)
		{
			using (var stream = new MemoryStream())
			{
		        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
		        {
		            var fileNames = Directory.GetFiles(path);
		            foreach (var fileName in fileNames)
		            {
		                var entryName = Path.GetFileName(fileName);
		                archive.CreateEntryFromFile(fileName, entryName);
		            }
		        }
		        return base64_encode(stream.ToArray());
			}
		}

		/// <summary>
		/// Decodes the given Base64-encoded archive into the specified target directory, overwrites existing files if the overwrite flag
		/// is set and returns the absolute paths of all extracted elements.
		/// </summary>
		public IEnumerable<string> DecodeAndDecompressDirectory(string base64, string targetDirectory, bool overwrite = true)
		{
			var data = base64_decode(base64);
			var paths = new List<string>();

			using (var zipStream = new MemoryStream(data))
		    {
		        using (var zip = new ZipArchive(zipStream))
			{
				foreach (var entry in zip.Entries)
				{
		                var path = Path.Combine(targetDirectory, entry.FullName.Replace('/', '\\'));
		                entry.ExtractToFile(path, overwrite);
					paths.Add(path);
				}
			}

			return paths;
		}
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
		    var zip = new ZipArchive(stream);
			zip.ExtractToDirectory(tempPath);

			return tempPath;
		}

		public MemoryStream DeCompressAndDecode(string base64)
		{
			var data = base64_decode(base64);
			var zipStream = new MemoryStream(data);
			var zip = new ZipArchive(zipStream);
            var returnStream = new MemoryStream();

            var stream = zip.Entries.First().Open();
		    stream.CopyTo(returnStream);
		    return returnStream;
		}

		public IEnumerable<string> GetFileList(string base64)
		{
			var data = base64_decode(base64);
			var zipStream = new MemoryStream(data);
		    var zip = new ZipArchive(zipStream);

			return zip.Entries.Select(f=>f.Name);
		}

		private string base64_encode(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			return Convert.ToBase64String(data);
		}
		private byte[] base64_decode(string encodedData)
		{
			return Convert.FromBase64String(encodedData);
		}
	}
}
