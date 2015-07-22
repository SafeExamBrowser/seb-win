using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
            if (m.Msg == 0x21 && this.CanFocus && !this.Focused)
                this.Focus();
            base.WndProc(ref m);
        }
    }
}
