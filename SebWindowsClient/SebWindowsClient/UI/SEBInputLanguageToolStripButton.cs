using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace SebWindowsClient.UI
{
    public class SEBInputLanguageToolStripButton : SEBToolStripButton
    {
        private Timer timer;
        private int currentIndex;


        public SEBInputLanguageToolStripButton()
        {
            this.Alignment = ToolStripItemAlignment.Right;
            base.ForeColor = Color.Black;
            base.Font = new Font("Arial", base.FontSize, FontStyle.Bold);

            try
            {
                currentIndex = InputLanguage.InstalledInputLanguages.IndexOf(InputLanguage.CurrentInputLanguage);

                //Start the update timer
                timer = new Timer();
                timer.Tick += (x,y) => Update();
                timer.Interval = 1000;
                timer.Start();
            }
            catch (Exception ex)
            {
                base.Enabled = false;
                Update();
            }
        }

        private void Update()
        {
            try
            {
                this.Text = InputLanguage.CurrentInputLanguage.Culture.ThreeLetterISOLanguageName.ToUpper();
                this.ToolTipText = String.Format(SEBUIStrings.KeyboardLayout_CURRENTCULTURE, InputLanguage.CurrentInputLanguage.Culture.DisplayName);
            }
            catch (Exception ex)
            {
                this.Text = "Error";
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (this.Parent != null)
                Parent.Focus();
            base.OnMouseHover(e);
        }

        protected override void OnClick(EventArgs e)
        {
            try
            {
                currentIndex++;
                currentIndex = currentIndex % (InputLanguage.InstalledInputLanguages.Count);

                InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[currentIndex];
            }
            catch
            {
            }
            Update();
        }

        
    }
}
