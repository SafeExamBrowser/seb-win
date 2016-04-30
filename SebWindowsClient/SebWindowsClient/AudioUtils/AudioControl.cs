using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SebWindowsClient.AudioUtils
{
    public class AudioControl
    {
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;
        private IntPtr Handle;

        public AudioControl(IntPtr handle)
        {
            this.Handle = handle;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        public void Mute()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        public void VolDown()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        public void VolUp()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_UP);
        }

    }
}