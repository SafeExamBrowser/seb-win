using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SebWindowsServiceWCF.RegistryHandler
{
    public static class RegistryChangeNotifier
    {
        public static void ReReadRegistry()
        {
            User32Utils.Notify_SettingChange();
        }




        internal class User32Utils
        {

            #region USER32 Options
            private const int HWND_BROADCAST = 0xffff;
            private const int WM_WININICHANGE = 0x001a, WM_SETTINGCHANGE = WM_WININICHANGE, INI_INTL = 1;
            #endregion

            #region Interop

            [DllImport("user32.dll")]
            private static extern int SendMessage(int hWnd, uint wMsg, uint wParam, uint lParam);

            #endregion

            internal static void Notify_SettingChange()
            {
                System.Diagnostics.Process.Start(@"c:\windows\System32\RUNDLL32.EXE", "user32.dll, UpdatePerUserSystemParameters");
                //SendMessage(HWND_BROADCAST, WM_SETTINGCHANGE, 0, INI_INTL);
            }
        }
    }
}
