using System.Windows.Forms;

namespace SebWindowsClient
{
	public partial class AboutWindow : Form
	{
		public AboutWindow()
		{
			InitializeComponent();
			Version.Text = "Safe Exam Browser for Windows " + Application.ProductVersion;
		}
	}
}
