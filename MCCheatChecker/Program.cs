using System;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace MCCheatChecker
{
	internal static class Program
	{
		[DllImport("user32.dll")]
		private static extern bool SetProcessDPIAware();

		[STAThread]
		private static void Main()
		{
			try
			{
				if (Environment.OSVersion.Version.Major >= 6)
				{
					SetProcessDPIAware();
				}
			}
			catch
			{
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
