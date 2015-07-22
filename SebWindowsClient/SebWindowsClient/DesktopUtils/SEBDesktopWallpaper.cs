// -------------------------------------------------------------
//     Viktor tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SebWindowsClient.DesktopUtils
{
    public static class SEBDesktopWallpaper
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

        public static void BlankWallpaper()
        {
            //if (_currentWallpaper == null)
            //    _currentWallpaper = GetWallpaper();

            //SetWallpaper("");
        }

        public static void Reset()
        {
            //if(_currentWallpaper != null)
            //    SetWallpaper(_currentWallpaper);
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
