using System;
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
            if (IsWindows7)
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
            if (IsWindows7)
            {
                if (_currentWallpaper != null)
                {
                    SetWallpaper(_currentWallpaper);
                    if (File.Exists(GetDirectory()))
                    {
                        File.Delete(GetDirectory());
                    }
                }
            }
        }


        private static bool IsWindows7
        {
            get
            {
                return OSVersion.FriendlyName().Contains("7");
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
