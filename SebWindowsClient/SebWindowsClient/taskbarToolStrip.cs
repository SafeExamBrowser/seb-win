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
		internal bool ProcessWndProc { get; set; }

		public TaskbarToolStrip()
		{
			ProcessWndProc = true;
		}

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
			if (ProcessWndProc)
			{
				try
				{
					//WM_MOUSEACTIVATE = 0x21
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
}
