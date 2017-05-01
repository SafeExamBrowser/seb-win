// -------------------------------------------------------------
//     Viktor tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SebWindowsClient.DiagnosticsUtils;
using System.Threading;
using System.Runtime.InteropServices;

namespace SebWindowsClient.ProcessUtils
{
    public class SEBClipboard
    {
        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();
        /// ----------------------------------------------------------------------------------------
        /// <summary>
        /// Clean clipboard.
        /// </summary>
        /// ----------------------------------------------------------------------------------------
        public static void CleanClipboard()
        {
            try
            {
                //Clipboard.Clear();
                //IntPtr handleWnd = GetOpenClipboardWindow();
                OpenClipboard(IntPtr.Zero);
                EmptyClipboard();
                CloseClipboard();
            }
            catch (Exception ex)
            {
                Logger.AddError("Error ocurred by cleaning Clipboard.", null, ex, ex.Message);
 
            }

        }
    }
}
