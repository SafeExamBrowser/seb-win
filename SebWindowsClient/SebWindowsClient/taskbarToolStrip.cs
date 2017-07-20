using System;
using System.Windows.Forms;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient
{
    /// ----------------------------------------------------------------------------------------
    /// <summary>
    /// Custom ToolStrip class which activates and handles click in once.
    /// </summary>
    /// ----------------------------------------------------------------------------------------
    class TaskbarToolStrip : ToolStrip
    {
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            //WM_MOUSEACTIVATE = 0x21
            try
            {
                if (m.Msg == 0x21 && this.CanFocus && !this.Focused)
                    this.Focus();
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                Logger.AddError("Error in KeyCapture", null, ex);
            }
            
        }
    }
}
