//
//  SebKeyCapture.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2019 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss,
//  ETH Zurich, Educational Development and Technology (LET),
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen
//  Project concept: Thomas Piendl, Daniel R. Schneider,
//  Dirk Bauer, Kai Reuter, Tobias Halbherr, Karsten Burger, Marco Lehre,
//  Brigitte Schmucki, Oliver Rahs. French localization: Nicolas Dunand
//
//  ``The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License at
//  http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//  License for the specific language governing rights and limitations
//  under the License.
//
//  The Original Code is Safe Exam Browser for Windows.
//
//  The Initial Developers of the Original Code are Viktor Tomas, 
//  Dirk Bauer, Daniel R. Schneider, Pascal Wyss.
//  Portions created by Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss
//  are Copyright (c) 2010-2019 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, 
//  Pascal Wyss, ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.ProcessUtils;
using SebWindowsClient.XULRunnerCommunication;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;

namespace SebWindowsClient.BlockShortcutsUtils
{
    /// <summary>
    /// Allows filtering of any keys, including special
    /// keys like CTRL, ALT, and Windows keys,
    /// Win32 windows hooks.
    /// </summary>
    /// <remarks>
    /// Original code example from:
    /// http://geekswithblogs.net/aghausman/archive/
    /// </remarks>
    public class SebKeyCapture
    {
        #region Imports

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }


        /// <summary>
        /// Information about the low-level keyboard input event.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }

        // Keyboard Constants
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCHAR = 0x0106;

        private const int WH_KEYBOARD_LL = 13;

        // System level function used to hook and unhook keyboard input
        private delegate IntPtr LowLevelProc(int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id,
            LowLevelProc callback, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook,
            int nCode, IntPtr wp, IntPtr lp);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);

        #endregion

        #region Private Variables

        private static IntPtr ptrKeyboardHook;
        private static IntPtr ptrMouseHook;
        private static LowLevelProc objKeyboardProcess;
        private static LowLevelProc objMouseProcess;
        private static bool _FilterKeys = false;
        private static Keys exitKey1;
        private static Keys exitKey2;
        private static Keys exitKey3;
        private static bool exitKey1_Pressed = false;
        private static bool exitKey2_Pressed = false;
        private static bool exitKey3_Pressed = false;

        // Ctrl-Q exit sequence
        private static bool ctrl_Pressed = false;
        private static bool Q_Pressed = false;

        // Alt-Tab sequence
        private static bool Alt_Pressed = false;
        private static bool Tab_Pressed = false;
        private static bool Tab_Pressed_First_Time = true;

        #endregion

        #region Public Properties

        public static bool FilterKeys
        {
            get { return SebKeyCapture._FilterKeys; }
            set
            {

                if (value && !SebKeyCapture._FilterKeys)
                {
                    UnregisterKeyboardHookMethod();
                    RegisterKeyboardHookMethod();
                }
                else if (!value)
                {
                    UnregisterKeyboardHookMethod();
                }
                SebKeyCapture._FilterKeys = value;
            }
        }

        /// <summary>
        /// Disable ot enabled Mouse Buttons from SebClient configuration
        ///</summary>
        private static bool DisableMouseButton(int nCode, IntPtr wp, IntPtr lp)
        {
            MSLLHOOKSTRUCT MouseButtonInfo = (MSLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(MSLLHOOKSTRUCT));

            if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableRightMouse)[SEBSettings.KeyEnableRightMouse])
            {
                //Console.WriteLine(String.Format("NCode: {0}, wp; {1} {2}",nCode,wp,(MouseMessages)wp));
                if (nCode >= 0 &&
                    (MouseMessages.WM_RBUTTONDOWN == (MouseMessages) wp ||
                     MouseMessages.WM_RBUTTONUP == (MouseMessages) wp))
                    return true;
            }
            if (
                !(Boolean)
                    SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltMouseWheel)[SEBSettings.KeyEnableAltMouseWheel])
            {
                KBDLLHOOKSTRUCT KeyInfo = (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
                if ((Control.ModifierKeys & Keys.Alt) != 0 && KeyInfo.flags < 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Disable not enabled Keys from SebClient configuration
        ///</summary>
        private static bool DisableKey(IntPtr wp, IntPtr lp)
        {
            KBDLLHOOKSTRUCT KeyInfo =
                (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

            try
            {
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableEsc)[SEBSettings.KeyEnableEsc] &&
                    (KeyInfo.key == Keys.Escape))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableCtrlEsc)[SEBSettings.KeyEnableCtrlEsc])
                {
                    if ((KeyInfo.flags == 0) && (KeyInfo.key == Keys.Escape))
                        return true;

                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltEsc)[SEBSettings.KeyEnableAltEsc])
                {
                    if ((KeyInfo.flags == 32) && (KeyInfo.key == Keys.Escape))
                        return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltTab)[SEBSettings.KeyEnableAltTab])
                {
                    if ((KeyInfo.flags == 32) && (KeyInfo.key == Keys.Tab))
                        return true;
                }
                if (
                    !(Boolean)
                        SEBClientInfo.getSebSetting(SEBSettings.KeyEnablePrintScreen)[SEBSettings.KeyEnablePrintScreen])
                {
                    if (KeyInfo.key == Keys.PrintScreen)
                        return true;
                }
                if (
                    !(Boolean)
                        SEBClientInfo.getSebSetting(SEBSettings.KeyEnableRightMouse)[SEBSettings.KeyEnableRightMouse])
                {
                    if (KeyInfo.key == Keys.Apps)
                        return true;
                }
                if ((Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltTab)[SEBSettings.KeyEnableAltTab])
                {
                    //if ((KeyInfo.flags == 32) && (KeyInfo.key == Keys.Tab))
                    //{
                    //    if (SebApplicationChooser == null)
                    //        SebApplicationChooser = new SebApplicationChooserForm();
                    //    SebApplicationChooser.fillListApplications();
                    //    SebApplicationChooser.Visible = true;
                    //    //SebApplicationChooser.Activate();
                    //    return true;
                    //}
                    //if (((KeyInfo.flags == 32) && (KeyInfo.key == Keys.LMenu)) || ((KeyInfo.flags == 33) && (KeyInfo.key == Keys.RMenu)))
                    //{
                    //    if (SebApplicationChooser == null)
                    //        SebApplicationChooser = new SebApplicationChooserForm();
                    //    SebApplicationChooser.fillListApplications();
                    //    SebApplicationChooser.Visible = true;
                    //    SebApplicationChooser.Activate();
                    //    return true;
                    //}
                }
                //if ((Boolean)SEBClientInfo.getSebSetting(SEBDefaultSettings.KeyEnableAltTab)[SEBDefaultSettings.KeyEnableAltTab])
                //{
                //    if (wp == (IntPtr)WM_SYSKEYUP)
                //    {
                //        if (KeyInfo.key == Keys.Tab)
                //        {
                //            SebApplicationChooser.Visible = false;
                //            return true;
                //        }
                //    }
                //}
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltF4)[SEBSettings.KeyEnableAltF4])
                {
                    if ((KeyInfo.flags == 32) && (KeyInfo.key == Keys.F4))
                        return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF1)[SEBSettings.KeyEnableF1] &&
                    (KeyInfo.key == Keys.F1))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF2)[SEBSettings.KeyEnableF2] &&
                    (KeyInfo.key == Keys.F2))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF3)[SEBSettings.KeyEnableF3] &&
                    (KeyInfo.key == Keys.F3))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF4)[SEBSettings.KeyEnableF4] &&
                    (KeyInfo.key == Keys.F4))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF5)[SEBSettings.KeyEnableF5] &&
                    (KeyInfo.key == Keys.F5))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF6)[SEBSettings.KeyEnableF6] &&
                    (KeyInfo.key == Keys.F6))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF7)[SEBSettings.KeyEnableF7] &&
                    (KeyInfo.key == Keys.F7))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF8)[SEBSettings.KeyEnableF8] &&
                    (KeyInfo.key == Keys.F8))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF9)[SEBSettings.KeyEnableF9] &&
                    (KeyInfo.key == Keys.F9))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF10)[SEBSettings.KeyEnableF10] &&
                    (KeyInfo.key == Keys.F10))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF11)[SEBSettings.KeyEnableF11] &&
                    (KeyInfo.key == Keys.F11))
                {
                    return true;
                }
                if (!(Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyEnableF12)[SEBSettings.KeyEnableF12] &&
                    (KeyInfo.key == Keys.F12))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("DisableKey: Failed with error. " + ex.Message, null, ex);
            }
            return false;
        }

        /// <summary>
        /// Set and Test Exit Key Sequence
        ///</summary>
        private static void SetExitKeys()
        {
            int iExitKey1 = (Int32) SEBClientInfo.getSebSetting(SEBSettings.KeyExitKey1)[SEBSettings.KeyExitKey1];
            int iExitKey2 = (Int32) SEBClientInfo.getSebSetting(SEBSettings.KeyExitKey2)[SEBSettings.KeyExitKey2];
            int iExitKey3 = (Int32) SEBClientInfo.getSebSetting(SEBSettings.KeyExitKey3)[SEBSettings.KeyExitKey3];
            switch (iExitKey1)
            {
                case 0:
                    exitKey1 = Keys.F1;
                    break;
                case 1:
                    exitKey1 = Keys.F2;
                    break;
                case 2:
                    exitKey1 = Keys.F3;
                    break;
                case 3:
                    exitKey1 = Keys.F4;
                    break;
                case 4:
                    exitKey1 = Keys.F5;
                    break;
                case 5:
                    exitKey1 = Keys.F6;
                    break;
                case 6:
                    exitKey1 = Keys.F7;
                    break;
                case 7:
                    exitKey1 = Keys.F8;
                    break;
                case 8:
                    exitKey1 = Keys.F9;
                    break;
                case 9:
                    exitKey1 = Keys.F10;
                    break;
                case 10:
                    exitKey1 = Keys.F11;
                    break;
                case 11:
                    exitKey1 = Keys.F12;
                    break;
                default:
                    exitKey1 = Keys.F3;
                    break;
            }
            switch (iExitKey2)
            {
                case 0:
                    exitKey2 = Keys.F1;
                    break;
                case 1:
                    exitKey2 = Keys.F2;
                    break;
                case 2:
                    exitKey2 = Keys.F3;
                    break;
                case 3:
                    exitKey2 = Keys.F4;
                    break;
                case 4:
                    exitKey2 = Keys.F5;
                    break;
                case 5:
                    exitKey2 = Keys.F6;
                    break;
                case 6:
                    exitKey2 = Keys.F7;
                    break;
                case 7:
                    exitKey2 = Keys.F8;
                    break;
                case 8:
                    exitKey2 = Keys.F9;
                    break;
                case 9:
                    exitKey2 = Keys.F10;
                    break;
                case 10:
                    exitKey2 = Keys.F11;
                    break;
                case 11:
                    exitKey2 = Keys.F12;
                    break;
                default:
                    exitKey2 = Keys.F11;
                    break;
            }
            switch (iExitKey3)
            {
                case 0:
                    exitKey3 = Keys.F1;
                    break;
                case 1:
                    exitKey3 = Keys.F2;
                    break;
                case 2:
                    exitKey3 = Keys.F3;
                    break;
                case 3:
                    exitKey3 = Keys.F4;
                    break;
                case 4:
                    exitKey3 = Keys.F5;
                    break;
                case 5:
                    exitKey3 = Keys.F6;
                    break;
                case 6:
                    exitKey3 = Keys.F7;
                    break;
                case 7:
                    exitKey3 = Keys.F8;
                    break;
                case 8:
                    exitKey3 = Keys.F9;
                    break;
                case 9:
                    exitKey3 = Keys.F10;
                    break;
                case 10:
                    exitKey3 = Keys.F11;
                    break;
                case 11:
                    exitKey3 = Keys.F12;
                    break;
                default:
                    exitKey3 = Keys.F6;
                    break;
            }
        }

        /// <summary>
        /// Set and Test ctrl-Q Exit Key Sequence
        ///</summary>
        private static bool SetAndTestCtrlQExitSequence(IntPtr wp, IntPtr lp)
        {
            KBDLLHOOKSTRUCT KeyInfo =
                (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

            if (KeyInfo.key == Keys.LControlKey)
            {
                ctrl_Pressed = true;
            }
            else if (KeyInfo.key == Keys.RControlKey)
            {
                ctrl_Pressed = true;
            }
            else if (KeyInfo.key == Keys.Q)
            {
                Q_Pressed = true;
            }
            else
            {
                ctrl_Pressed = false;
                Q_Pressed = false;
            }

            if (ctrl_Pressed && Q_Pressed)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset ctrl-Q Exit Key Sequence
        ///</summary>
        private static void ResetCtrlQExitSequence(IntPtr wp, IntPtr lp)
        {
            KBDLLHOOKSTRUCT KeyInfo =
                (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

            if (KeyInfo.key == Keys.LControlKey)
            {
                ctrl_Pressed = false;
            }
            if (KeyInfo.key == Keys.RControlKey)
            {
                ctrl_Pressed = false;
            }
            if (KeyInfo.key == Keys.Q)
            {
                Q_Pressed = false;
            }
        }

        /// <summary>
        /// Set and Test Exit Key Sequence
        ///</summary>
        private static bool SetAndTestExitKeySequence(IntPtr wp, IntPtr lp)
        {
            KBDLLHOOKSTRUCT KeyInfo =
                (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

            SetExitKeys();

            if (KeyInfo.key == exitKey1)
            {
                exitKey1_Pressed = true;
            }
            else if (KeyInfo.key == exitKey2)
            {
                exitKey2_Pressed = true;
            }
            else if (KeyInfo.key == exitKey3)
            {
                exitKey3_Pressed = true;
            }
            else
            {
                exitKey1_Pressed = false;
                exitKey2_Pressed = false;
                exitKey3_Pressed = false;
            }

            if (exitKey1_Pressed && exitKey2_Pressed && exitKey3_Pressed)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset Exit Key Sequence
        ///</summary>
        private static void ResetExitKeySequence(IntPtr wp, IntPtr lp)
        {
            KBDLLHOOKSTRUCT KeyInfo =
                (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
            if (KeyInfo.key == exitKey1)
            {
                exitKey1_Pressed = false;
            }
            if (KeyInfo.key == exitKey2)
            {
                exitKey2_Pressed = false;
            }
            if (KeyInfo.key == exitKey3)
            {
                exitKey3_Pressed = false;
            }
        }

        #endregion

        /// <summary>
        /// Capture keystrokes and filter which key events are permitted to continue.
        /// </summary>
        private static IntPtr CaptureMouseButton(int nCode, IntPtr wp, IntPtr lp)
        {

            // If the nCode is non-negative, filter the key stroke.
            if (nCode >= 0)
            {
                if (
                    (bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyTouchOptimized) &&
                    (bool)
                    SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyEnableTouchExit))
                {
                    TestTouchExitSequence(System.Windows.Forms.Cursor.Position);
                }
                //KBDLLHOOKSTRUCT KeyInfo =
                //  (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

                // Reject any key that's not on our list.
                if (DisableMouseButton(nCode, wp, lp))
                    return (IntPtr) 1;
            }

            // Pass the event to the next hook in the chain.
            return CallNextHookEx(ptrMouseHook, nCode, wp, lp);
        }

        private static DateTime TouchExitSequenceStartedTime;
        private static int TouchExitSequenceStartedX;

        private static void TestTouchExitSequence(Point cursorsPosition)
        {
            if (cursorsPosition.Y == 0)
            {
                TouchExitSequenceStartedTime = DateTime.Now;
                TouchExitSequenceStartedX = cursorsPosition.X;
            }
            else if (
                Math.Abs(cursorsPosition.X - TouchExitSequenceStartedX) < Screen.PrimaryScreen.WorkingArea.Width/3 &&
                DateTime.Now - TouchExitSequenceStartedTime < new TimeSpan(0, 0, 2) &&
                cursorsPosition.Y > Screen.PrimaryScreen.WorkingArea.Height/3 &&
                cursorsPosition.Y < Screen.PrimaryScreen.WorkingArea.Height/3*2)
            {
                SEBClientInfo.SebWindowsClientForm.ShowCloseDialogForm();
            }
        }

        /// <summary>
        /// Test app exit keys sequence
        /// </summary>
        private static void TestAppExitSequences(IntPtr wp, IntPtr lp)
        {
            if (wp == (IntPtr) WM_KEYDOWN)
            {
                if (SetAndTestCtrlQExitSequence(wp, lp))
                {
                    SEBClientInfo.SebWindowsClientForm.ShowCloseDialogForm();
                }
                if (SetAndTestExitKeySequence(wp, lp) &&
                    (bool) SEBSettings.valueForDictionaryKey(SEBSettings.settingsCurrent, SEBSettings.KeyIgnoreExitKeys) ==
                    false)
                {
                    SEBClientInfo.SebWindowsClientForm.ExitApplication();
                }
            }
            else if (wp == (IntPtr) WM_KEYUP)
            {
                ResetCtrlQExitSequence(wp, lp);
                ResetExitKeySequence(wp, lp);
            }
        }

        /// <summary>
        /// Capture keystrokes and filter which key events are permitted to continue.
        /// </summary>
        private static IntPtr CaptureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            try
            {
                // If the nCode is non-negative, filter the keqy stroke.
                if (nCode >= 0)
                {
                    TestAppExitSequences(wp, lp);

                    KBDLLHOOKSTRUCT KeyInfo =
                      (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));

                    //Console.WriteLine(String.Format("Ncode: {0}, wp:{1}, Key:{2}, KeyInt:{3}, flags: {4}",nCode, wp, KeyInfo.key, (int)KeyInfo.key, KeyInfo.flags));

                    if ((Boolean)SEBClientInfo.getSebSetting(SEBSettings.KeyEnableAltTab)[SEBSettings.KeyEnableAltTab])
                    {
                        //ALT-TAB for App-Switcher (wp 260 = keydown, wp 257 = keyup)
                        if (KeyInfo.key == Keys.Tab && KeyInfo.flags == 32 && (int)wp == 260)
                        {
                            if (Tab_Pressed_First_Time && (int)wp == 260)
                            {
                                SEBClientInfo.SebWindowsClientForm.ShowApplicationChooserForm();
                                if (Tab_Pressed_First_Time)
                                {
                                    Tab_Pressed_First_Time = false;
                                }
                                return (IntPtr)1;
                            }
                            else if (!Tab_Pressed_First_Time && (int)wp == 260)
                            {
                                SEBClientInfo.SebWindowsClientForm.SelectNextListItem();
                                return (IntPtr)1;
                            }
                        }
                        if (((KeyInfo.key == Keys.LMenu && KeyInfo.flags == 128) || (KeyInfo.key == Keys.RMenu && KeyInfo.flags == 129)) && (int)wp == 257)
                        {
                            SEBClientInfo.SebWindowsClientForm.HideApplicationChooserForm();
                            Tab_Pressed_First_Time = true;
                        }
                    }

                    //Reject any key that's not on our list.
                    if (DisableKey(wp, lp))
                        return (IntPtr)1;
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("Error in KeyCapture", null, ex);
            }
            // Pass the event to the next hook in the chain.
            return CallNextHookEx(ptrKeyboardHook, nCode, wp, lp);
        }

        /// <summary>
        // Register key capture method.
        /// </summary>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        private static void RegisterKeyboardHookMethod()
        {
                // Get Current Module
                ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;

                // Assign callback function each time keyboard process
                objKeyboardProcess = new LowLevelProc(CaptureKey);

                // Assign callback function each time mouse process
                objMouseProcess = new LowLevelProc(CaptureMouseButton);

                // Setting Hook of Keyboard Process for current module
                ptrKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, objKeyboardProcess,
                              GetModuleHandle(objCurrentModule.ModuleName), 0);

                ptrMouseHook = SetWindowsHookEx(WH_MOUSE_LL, objMouseProcess,
                    GetModuleHandle(objCurrentModule.ModuleName), 0);
        }

        /// <summary>
        // Unregister key capture method.
        /// </summary>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        private static void UnregisterKeyboardHookMethod()
        {
            if (ptrKeyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(ptrKeyboardHook);
                ptrKeyboardHook = IntPtr.Zero;
            }
            if (ptrMouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(ptrMouseHook);
                ptrMouseHook = IntPtr.Zero;
            }
        }
    }
}
