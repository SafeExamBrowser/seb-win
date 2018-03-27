using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient.DesktopUtils
{
	public class SEBDesktopWallpaper
    {
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;
        const int SPI_GETDESKWALLPAPER = 0x73;
        const int MAX_PATH = 260;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(
            int uAction, int uParam, string lpvParam, int fuWinIni);

        private static string _currentWallpaper = null;

        public enum Style : int
        {
            Tiled, Centered, Stretched
        }

        private const string WallpaperInfoFile = "wallpaperinfo";


        public static void BlankWallpaper()
        {
            if (OSVersion.IsWindows7)
            {
                if (File.Exists(GetDirectory()))
                {
                    _currentWallpaper = File.ReadAllText(GetDirectory());
                }

                if (_currentWallpaper == null)
                {
                    _currentWallpaper = GetWallpaper();
                    File.WriteAllText(GetDirectory(), _currentWallpaper);
                }

                SetWallpaper("");
            }
        }

        public static void Reset()
        {
            if (OSVersion.IsWindows7)
            {
                if (_currentWallpaper != null)
                {
                    SetWallpaper(_currentWallpaper);
					Refresh();

                    if (File.Exists(GetDirectory()))
                    {
                        File.Delete(GetDirectory());
                    }
                }
            }
        }

		public static void Refresh()
		{
			if (OSVersion.IsWindows7)
			{
				// See https://superuser.com/questions/398605/how-to-force-windows-desktop-background-to-update-or-refresh.
				var startInfo = new ProcessStartInfo
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "cmd.exe",
					Arguments = "/C RUNDLL32.EXE USER32.DLL,UpdatePerUserSystemParameters 1, True"
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
				}
			}
		}

        private static string GetDirectory()
        {
            return SEBClientInfo.SebClientSettingsAppDataDirectory + WallpaperInfoFile;
        }

        private static string GetWallpaper()
        {
            var currentWallpaper = new string('\0', MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }

        private static void SetWallpaper(string path)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
