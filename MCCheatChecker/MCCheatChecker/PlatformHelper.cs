using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class PlatformHelper
	{
		public static readonly PlatformOS CurrentOS;

		public static readonly bool IsWindows;

		public static readonly bool IsMacOS;

		public static readonly bool IsLinux;

		public static readonly string OSDescription;

		static PlatformHelper()
		{
			PlatformID platform = Environment.OSVersion.Platform;
			if (platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows || platform == PlatformID.Win32S || platform == PlatformID.WinCE)
			{
				CurrentOS = PlatformOS.Windows;
				IsWindows = true;
				OSDescription = "Windows (" + Environment.OSVersion.VersionString + ")";
				return;
			}
			bool flag = false;
			if (Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/Library"))
			{
				flag = true;
			}
			else
			{
				try
				{
					using (Process process = new Process())
					{
						process.StartInfo.FileName = "uname";
						process.StartInfo.Arguments = "-s";
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.Start();
						string text = process.StandardOutput.ReadToEnd().Trim();
						process.WaitForExit(1000);
						if (text.IndexOf("Darwin", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							flag = true;
						}
					}
				}
				catch
				{
				}
			}
			if (flag)
			{
				CurrentOS = PlatformOS.MacOS;
				IsMacOS = true;
				OSDescription = "macOS (Darwin)";
			}
			else if (platform == PlatformID.Unix || platform == (PlatformID)128)
			{
				CurrentOS = PlatformOS.Linux;
				IsLinux = true;
				OSDescription = "Linux (" + Environment.OSVersion.VersionString + ")";
			}
			else
			{
				CurrentOS = PlatformOS.Unknown;
				OSDescription = "Unknown OS";
			}
		}

		public static string GetMinecraftDirectory()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (IsWindows)
			{
				string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				return Path.Combine(folderPath2, ".minecraft");
			}
			if (IsMacOS)
			{
				string text = Path.Combine(folderPath, "Library", "Application Support", "minecraft");
				if (Directory.Exists(text))
				{
					return text;
				}
				string text2 = Path.Combine(folderPath, ".minecraft");
				if (Directory.Exists(text2))
				{
					return text2;
				}
				return text;
			}
			return Path.Combine(folderPath, ".minecraft");
		}

		public static string GetMinecraftSubDir(params string[] parts)
		{
			string minecraftDirectory = GetMinecraftDirectory();
			if (parts == null || parts.Length == 0)
			{
				return minecraftDirectory;
			}
			string text = minecraftDirectory;
			foreach (string path in parts)
			{
				text = Path.Combine(text, path);
			}
			return text;
		}

		public static List<string> GetAllMinecraftDirectories()
		{
			List<string> list = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (IsWindows)
			{
				list.Add(Path.Combine(folderPath2, ".minecraft"));
				list.Add(Path.Combine(folderPath, ".feather"));
				list.Add(Path.Combine(folderPath, ".lunarclient"));
				list.Add(Path.Combine(folderPath2, ".tlauncher", "legacy", "Minecraft", "game"));
				list.Add(Path.Combine(folderPath2, "PrismLauncher", "instances"));
				list.Add(Path.Combine(folderPath2, "CurseForge", "minecraft", "Instances"));
				list.Add(Path.Combine(folderPath2, "Badlion Client"));
			}
			else if (IsMacOS)
			{
				list.Add(Path.Combine(folderPath, "Library", "Application Support", "minecraft"));
				list.Add(Path.Combine(folderPath, ".minecraft"));
				list.Add(Path.Combine(folderPath, "Library", "Application Support", "feather"));
				list.Add(Path.Combine(folderPath, ".feather"));
				list.Add(Path.Combine(folderPath, ".lunarclient"));
				list.Add(Path.Combine(folderPath, "Library", "Application Support", "PrismLauncher", "instances"));
				list.Add(Path.Combine(folderPath, "Library", "Application Support", "curseforge", "minecraft", "Instances"));
			}
			else
			{
				list.Add(Path.Combine(folderPath, ".minecraft"));
				list.Add(Path.Combine(folderPath, ".feather"));
				list.Add(Path.Combine(folderPath, ".lunarclient"));
				list.Add(Path.Combine(folderPath, ".local", "share", "PrismLauncher", "instances"));
				list.Add(Path.Combine(folderPath, ".curseforge", "minecraft", "Instances"));
				list.Add(Path.Combine(folderPath, ".local", "share", "multimc", "instances"));
			}
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				if (Directory.Exists(item) && !list2.Contains(item))
				{
					list2.Add(item);
				}
			}
			return list2;
		}

		public static bool OpenFolder(string folderPath)
		{
			try
			{
				if (!Directory.Exists(folderPath))
				{
					return false;
				}
				if (IsWindows)
				{
					Process.Start("explorer.exe", "\"" + folderPath + "\"");
					return true;
				}
				if (IsMacOS)
				{
					Process.Start("open", "\"" + folderPath + "\"");
					return true;
				}
				Process.Start("xdg-open", "\"" + folderPath + "\"");
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool OpenFileInEditor(string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					return false;
				}
				if (IsWindows)
				{
					Process.Start("notepad.exe", "\"" + filePath + "\"");
					return true;
				}
				if (IsMacOS)
				{
					Process.Start("open", "-t \"" + filePath + "\"");
					return true;
				}
				Process.Start("xdg-open", "\"" + filePath + "\"");
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool SelectFileInFileManager(string filePath)
		{
			try
			{
				if (!File.Exists(filePath) && !Directory.Exists(filePath))
				{
					return false;
				}
				if (IsWindows)
				{
					Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
					return true;
				}
				if (IsMacOS)
				{
					Process.Start("open", "-R \"" + filePath + "\"");
					return true;
				}
				string folderPath = (Directory.Exists(filePath) ? filePath : Path.GetDirectoryName(filePath));
				return OpenFolder(folderPath);
			}
			catch
			{
				return false;
			}
		}

		public static List<string> GetShellHistoryFiles()
		{
			List<string> list = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (IsWindows)
			{
				list.Add(Path.Combine(folderPath2, "Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt"));
				list.Add(Path.Combine(folderPath2, "Microsoft\\Windows\\PowerShell\\PSReadLine\\Visual Studio Code Host_history.txt"));
			}
			list.Add(Path.Combine(folderPath, ".bash_history"));
			list.Add(Path.Combine(folderPath, ".zsh_history"));
			list.Add(Path.Combine(folderPath, ".history"));
			list.Add(Path.Combine(folderPath, ".sh_history"));
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				if (File.Exists(item) && !list2.Contains(item))
				{
					list2.Add(item);
				}
			}
			return list2;
		}

		public static bool CheckIsAuthorized()
		{
			return true;
		}

		public static void SaveAuthorized(bool auth)
		{
		}
	}
}
