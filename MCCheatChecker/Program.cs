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
		private static void Main(string[] args)
		{
			if (args != null && args.Length > 0 && args[0] == "--benchmark")
			{
				Console.WriteLine("Starting Benchmark...");
				CheatDetector.OnProgress += delegate(string s) { Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + s); };
				System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
				CheatDetector.RunFullScan("BenchmarkPlayer", System.Threading.CancellationToken.None);
				sw.Stop();
				Console.WriteLine("Total Benchmark Time: " + sw.ElapsedMilliseconds + " ms");
				return;
			}
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
