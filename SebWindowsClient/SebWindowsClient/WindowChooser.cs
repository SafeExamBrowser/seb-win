using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.Properties;

namespace SebWindowsClient
{
    public partial class WindowChooser : Form
    {
        private Process _process;
        private List<KeyValuePair<IntPtr, string>> _openedWindows;
        /// <summary>
        /// This displays a small window where the icons and titles of the opened windows are placed and shows them on above the icon in the taskbar (just like windows does)
        /// </summary>
        /// <param name="process">The process that handles the window(s)</param>
        /// <param name="left">The positiop where the icon on the taskbar is placed</param>
        /// <param name="top">The positiop where the icon on the taskbar is placed</param>
        public WindowChooser(Process process, int left, int top)
        {
            this.Left = 0;
            InitializeComponent();
            this.appList.Click += ShowWindow;

            int heightOfAppChooserList = (int)(82 * SEBClientInfo.scaleFactor);
            int heightOfIcons = (int)(32 * SEBClientInfo.scaleFactor);
            int heightOfCloseIcon = (int)(24 * SEBClientInfo.scaleFactor);

            try
            {
                _process = process;
                var appImages = new ImageList();
                ImageList closeImages = null;
                if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] == true)
                {
                    heightOfIcons = (int)(48*SEBClientInfo.scaleFactor);

                    appImages.ImageSize = new Size(heightOfIcons, heightOfIcons);
                    closeImages = new ImageList();
                    closeImages.ImageSize = new Size(heightOfIcons, heightOfCloseIcon);
                    closeImages.ColorDepth = ColorDepth.Depth32Bit;
                    this.closeListView.Click += CloseWindow;
                    this.appList.Height = heightOfAppChooserList;
                    this.closeListView.Height = heightOfCloseIcon + 12;
                }
                else
                {
                    appImages.ImageSize = new Size(heightOfIcons, heightOfIcons);
                    this.appList.Height = heightOfAppChooserList;
                }
                appImages.ColorDepth = ColorDepth.Depth32Bit;

                _openedWindows = process.GetOpenWindows().ToList();

                //Add the mainwindowhandle if not yet added
                if (_process.MainWindowHandle != IntPtr.Zero && !_openedWindows.Any(oW => oW.Key == _process.MainWindowHandle))
                {
                    _openedWindows.Add(new KeyValuePair<IntPtr, string>(_process.MainWindowHandle, _process.MainWindowTitle));
                }

                //Directly show the window if just one is opened
                if (_openedWindows.Count == 1)
                {
                    ShowWindow(_openedWindows.First().Key);
                }
                else
                {
                    //Add the icons
                    int index = 0;
                    foreach (var openWindow in _openedWindows)
                    {
                        Image image = GetSmallWindowIcon(openWindow.Key);
                        appImages.Images.Add(image);
                        var appItem = new ListViewItem(openWindow.Value, index);
                        appList.Items.Add(appItem);
                        if (closeImages != null)
                        {
                            closeImages.Images.Add(Resources.closewindow);
                            var closeItem = new ListViewItem("close", index);
                            closeListView.Items.Add(closeItem);
                        }

                        index++;
                    }

                    if (closeImages != null)
                    {
                        this.closeListView.View = View.LargeIcon;
                        this.closeListView.LargeImageList = closeImages;
                    }
                    this.appList.View = View.LargeIcon;
                    this.appList.LargeImageList = appImages;
                    this.Height = 6 + appList.Size.Height + (closeImages != null ? heightOfCloseIcon : 0) + 6;
                    this.Top = top - this.Height;

                    //Calculate the width
                    this.Width = Screen.PrimaryScreen.Bounds.Width;
                    this.Show();
                    this.appList.Focus();

                    //Hide it after 4 secs
                    var t = new Timer();
                    t.Tick += CloseIt;
                    if ((bool)SEBSettings.settingsCurrent[SEBSettings.KeyTouchOptimized])
                    {
                        t.Interval = 4000;
                    }
                    else
                    {
                        t.Interval = 3000;
                    }
                    t.Start();
                }
            }
            catch (Exception)
            {
                this.Close();
            }
        }

        private void CloseIt(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Console.WriteLine("Closing");
            this.appList.Click -= ShowWindow;
            base.OnClosing(e);
        }

        private void ShowWindow(object sender, EventArgs e)
        {
            var selectedIndex = appList.SelectedIndices[0];
            ShowWindow(_openedWindows[selectedIndex].Key);
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            var selectedIndex = closeListView.SelectedIndices[0];
            _openedWindows[selectedIndex].Key.CloseWindow();
            this.Close();
        }

        /// <summary>
        /// Shows the window
        /// </summary>
        /// <param name="windowHandle"></param>
        private void ShowWindow(IntPtr windowHandle)
        {
            windowHandle.BringToTop();

            //If we are working in touch optimized mode, open every window in full screen (e.g. maximized), except XULRunner because it seems not to accept the working area property and resizes to fully fullscreen
            if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized]
                 //            && !_process.ProcessName.Contains("xulrunner"))
            )
            {
               
                _openedWindows.First().Key.MaximizeWindow();
            }

            this.Close();
        }

        public static Image GetSmallWindowIcon(IntPtr hWnd)
        {
            try
            {
                try
                {
                    Process process = hWnd.GetProcess();
                    if (process.ProcessName.Contains("firefox"))
                    {
                        return Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap();
                    }
                    else
                    {
                        string fullPath = process.Modules[0].FileName;
                        return Icon.ExtractAssociatedIcon(fullPath).ToBitmap();
                    }
                }
                catch
                {
                }
                

                IntPtr hIcon = default(IntPtr);

                hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL2, IntPtr.Zero);

                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICON);

                if (hIcon == IntPtr.Zero)
                    hIcon = LoadIcon(IntPtr.Zero, (IntPtr)0x7F00/*IDI_APPLICATION*/);

                if (hIcon != IntPtr.Zero)
                    return new Bitmap(Icon.FromHandle(hIcon).ToBitmap(), 32, 32);
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return new IntPtr((long)GetClassLong32(hWnd, nIndex));
            else
                return GetClassLong64(hWnd, nIndex);
        }

        private static uint WM_GETICON = 0x007f;
        private static IntPtr ICON_SMALL2 = new IntPtr(2);
        private static int GCL_HICON = -14;


        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

    }



}
