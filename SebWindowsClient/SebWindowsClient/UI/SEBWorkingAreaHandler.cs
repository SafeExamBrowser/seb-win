using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient.UI
{
    public static class SEBWorkingAreaHandler
    {
        #region Definitions

        private const Int32 SPIF_SENDWININICHANGE = 2;
        private const Int32 SPIF_UPDATEINIFILE = 1;
        private const Int32 SPIF_change = SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE;
        private const Int32 SPI_SETWORKAREA = 47;
        private const Int32 SPI_GETWORKAREA = 48;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public Int32 Left;
            public Int32 Top;   // top is before right in the native struct
            public Int32 Right;
            public Int32 Bottom;
        }

        private static bool _originalWorkingAreaSet = false;
        private static RECT _originalWorkingArea;

        #endregion

        #region Public Methods

        public static void SetTaskBarSpaceHeight(int taskbarHeight)
        {
            if(!_originalWorkingAreaSet)
            {
                _originalWorkingArea.Bottom = Screen.PrimaryScreen.WorkingArea.Bottom;
                _originalWorkingArea.Left = Screen.PrimaryScreen.WorkingArea.Left;
                _originalWorkingArea.Right = Screen.PrimaryScreen.WorkingArea.Right;
                _originalWorkingArea.Top = Screen.PrimaryScreen.WorkingArea.Top;
                _originalWorkingAreaSet = true;
            }
            

            SetWorkspace(new RECT()
            {
                Bottom = Screen.PrimaryScreen.Bounds.Height - taskbarHeight,
                Left = 0,
                Right = Screen.PrimaryScreen.Bounds.Width,
                Top = 0
            });
        }

        public static void ResetWorkspaceArea()
        {
            if (_originalWorkingAreaSet)
                SetWorkspace(_originalWorkingArea);
        }

        #endregion

        #region private Methods

        private static bool SetWorkspace(RECT rect)
        {
            try
            {
                bool result = SystemParametersInfo(SPI_SETWORKAREA,
                                               (int)IntPtr.Zero,
                                               ref rect,
                                               SPIF_change);
                return result;
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to set Working Area",null,ex);
                return false;
            }
        }

        #endregion

        #region DLLImports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
                                                        int uiAction,
                                                        int uiParam,
                                                        ref RECT pvParam,
                                                        int fWinIni);

        #endregion
    }
}
