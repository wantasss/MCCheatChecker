using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class TamperGuard
	{
		private static class NativeMethods
		{
			[DllImport("kernel32.dll")]
			public static extern bool IsDebuggerPresent();

			[DllImport("kernel32.dll")]
			public static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, out bool pbDebuggerPresent);

			[DllImport("kernel32.dll")]
			public static extern IntPtr GetCurrentProcess();

			[DllImport("kernel32.dll")]
			public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
		}

		private const string RegPath = "Software\\MCCheatChecker\\Gate";

		private const string IntegrityValue = "Integrity";

		private static readonly HashSet<string> ToolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"cheatengine", "cheatengine-i386", "cheatengine-x86-64", "cheatengine-x86-64-mono", "cheatengine-i386-mono", "ollydbg", "ollydbg64", "x64dbg", "x32dbg", "windbg",
			"ntsd", "cdb", "werd", "scylla_x64", "scylla_x86", "ida", "ida32", "ida64", "idaq", "idaq64",
			"idaw", "idaw64", "idag", "idag64", "dnspy", "dnspy32", "dnspy64", "ilspy", "dotpeek", "dotpeek64",
			"justdecompile", "justdecompile32", "justdecompile64", "de4dot", "de4dot-cex", "harmonydebug", "apimonitor-x86", "apimonitor-x64"
		};

		private static string ExePath
		{
			get
			{
				try
				{
					return Application.ExecutablePath;
				}
				catch
				{
					return Process.GetCurrentProcess().MainModule.FileName;
				}
			}
		}

		private static string SidecarPath
		{
			get
			{
				string exePath = ExePath;
				return Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + ".dat");
			}
		}

		public static void Start()
		{
		}

		private static bool ToolDetected()
		{
			try
			{
				if (Debugger.IsAttached)
				{
					return true;
				}
				if (NativeDebuggerPresent())
				{
					return true;
				}
				if (RemoteDebuggerPresent())
				{
					return true;
				}
				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					try
					{
						string processName = process.ProcessName;
						if (!string.IsNullOrEmpty(processName) && ToolNames.Contains(processName))
						{
							return true;
						}
					}
					catch
					{
					}
					finally
					{
						try
						{
							process.Dispose();
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool NativeDebuggerPresent()
		{
			try
			{
				return NativeMethods.IsDebuggerPresent();
			}
			catch
			{
				return false;
			}
		}

		private static bool RemoteDebuggerPresent()
		{
			try
			{
				bool pbDebuggerPresent;
				return NativeMethods.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, out pbDebuggerPresent) && pbDebuggerPresent;
			}
			catch
			{
				return false;
			}
		}

		private static bool VerifyIntegrity()
		{
			return true;
		}

		private static string ComputeExeHash()
		{
			try
			{
				string exePath = ExePath;
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
				{
					return null;
				}
				using (SHA256 sHA = SHA256.Create())
				{
					using (FileStream inputStream = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					{
						byte[] array = sHA.ComputeHash(inputStream);
						StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
						for (int i = 0; i < array.Length; i++)
						{
							stringBuilder.Append(array[i].ToString("x2"));
						}
						return stringBuilder.ToString();
					}
				}
			}
			catch
			{
				return null;
			}
		}

		private static string ReadStoredHash()
		{
			return null;
		}

		private static void WriteStoredHash(string hash)
		{
		}

		public static void ResetIntegrity()
		{
		}

		private static void SelfDestruct(bool alsoDeleteExe)
		{
		}

		private static void Wipe()
		{
		}

		private static void ScheduleSelfDelete()
		{
		}
	}
}
