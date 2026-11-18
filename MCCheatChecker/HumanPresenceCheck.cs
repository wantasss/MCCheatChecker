using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class HumanPresenceCheck
	{
		private struct LASTINPUTINFO
		{
			public uint cbSize;

			public int dwTime;
		}

		private static readonly string[] RemoteAccessNames = new string[]
		{
			"anydesk", "rudesktop", "ru desktop", "rdesktop", "teamviewer", "chrome remote", "vnc", "realvnc", "tightvnc", "rustdesk", "supremo",
			"logmein", "gotomeeting", "zoom", "skype", "microsoft teams", "hopetodesk", "parsec", "moonlight", "sunshine", "ammyy", "ultraviewer",
			"litemanager", "radmin", "splashtop", "aeroadmin", "dwservice", "screenshare"
		};

		private static readonly string[] JigglerNames = new string[]
		{
			"mouse jiggler", "screen jiggler", "power jiggler", "fakemouse", "kmbox", "makcu", "interception"
		};

		private static readonly string[] PowerPolicyKeys = new string[2] { "Control Panel\\PowerSettings\\238C9FA8-0AAD-41ED-83F4-97BE242C8F20\\7bc4a2f9-d3fc-4b3c-8508-02768899b6fb", "Control Panel\\PowerSettings\\238C9FA8-0AAD-41ED-83F4-97BE242C8F20\\bc5038f7-23e0-4960-96da-33abaf5935ee" };

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

		public static List<CheckResult> RunHumanPresenceCheck()
		{
			List<CheckResult> list = new List<CheckResult>();
			list.Add(CheckLastInput());
			list.Add(CheckRemoteAccessRunning());
			list.Add(CheckRemoteAccessConnected());
			list.Add(CheckJigglerPresence());
			list.Add(CheckUptime());
			if (PlatformHelper.IsWindows)
			{
				list.Add(CheckPowerSettings());
				list.Add(CheckSessionUser());
			}
			else
			{
				list.Add(CheckUnixSessionUser());
			}
			return list;
		}

		private static CheckResult CheckLastInput()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Последняя активность ввода (мышь / клавиатура)");
			if (!PlatformHelper.IsWindows)
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Мониторинг Direct Input активен для Windows (на " + PlatformHelper.OSDescription + " используются системные процессы)";
				return checkResult;
			}
			LASTINPUTINFO plii = new LASTINPUTINFO
			{
				cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO))
			};
			if (!GetLastInputInfo(ref plii))
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Не удалось получить состояние ввода (возможно, нужны права)";
				return checkResult;
			}
			int tickCount = Environment.TickCount;
			int dwTime = plii.dwTime;
			int num = (tickCount - dwTime + int.MaxValue) & 0x7FFFFFFF;
			int num2 = num / 1000;
			int num3 = num2 / 60;
			int num4 = num3 / 60;
			checkResult.Detail = "Система работает: " + FormatUptime(tickCount) + " | Последнее действие: " + num3 + " мин " + num2 % 60 + " сек назад";
			checkResult.Severity = CheckSeverity.Info;
			if (num3 > 15)
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Без ввода более 15 минут — возможно, за ПК сидит не игрок";
				checkResult.Evidence = "Idle: " + num3 + " мин " + num2 % 60 + " сек | Uptime: " + FormatUptime(tickCount);
			}
			else if (num3 > 5)
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Без ввода более 5 минут — проверь, что игрок ещё на месте";
				checkResult.Evidence = "Idle: " + num3 + " мин " + num2 % 60 + " сек | Uptime: " + FormatUptime(tickCount);
			}
			else
			{
				checkResult.Detected = false;
				checkResult.Detail = "Активность за последние 5 минут — игрок на месте";
			}
			return checkResult;
		}

		private static CheckResult CheckRemoteAccessRunning()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Запущенные программы удалённого доступа");
			List<string> info = new List<string>();
			List<string> list = new List<string>();
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				string text = process.ProcessName.ToLowerInvariant();
				string[] remoteAccessNames = RemoteAccessNames;
				foreach (string value in remoteAccessNames)
				{
					if (text.IndexOf(value, StringComparison.Ordinal) >= 0)
					{
						list.Add("Запущен: " + process.ProcessName + " (PID " + process.Id + ", RAM " + process.WorkingSet64 / 1024 + " КБ)");
						break;
					}
				}
			}
			Process[] processes2 = Process.GetProcesses();
			foreach (Process process2 in processes2)
			{
				try
				{
					string text2 = process2.MainWindowTitle.ToLowerInvariant();
					if (string.IsNullOrEmpty(text2))
					{
						continue;
					}
					string[] remoteAccessNames2 = RemoteAccessNames;
					foreach (string value2 in remoteAccessNames2)
					{
						if (text2.IndexOf(value2, StringComparison.Ordinal) >= 0)
						{
							list.Add("Открыто окно: \"" + text2 + "\" (PID " + process2.Id + ")");
							break;
						}
					}
				}
				catch
				{
				}
			}
			checkResult.Detail = ((list.Count > 0) ? ("Обнаружены программы удалённого доступа: " + list.Count) : "Программ удалённого доступа не обнаружено");
			checkResult.Detected = list.Count > 0;
			checkResult.Severity = ((list.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(info, list);
			return checkResult;
		}

		private static CheckResult CheckRemoteAccessConnected()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Активные сессии удалённого доступа");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			try
			{
				string[] anydeskFiles = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnyDesk", "ad.conf"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnyDesk", "system.conf"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AnyDesk", "system.conf")
				};
				foreach (string path in anydeskFiles)
				{
					if (File.Exists(path))
					{
						string text = "";
						try
						{
							using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
							{
								text = sr.ReadToEnd().ToLowerInvariant();
							}
						}
						catch { }
						if (text.Contains("remote_control"))
						{
							list2.Add("ℹ️ [Сессия AnyDesk активна]: " + Path.GetFileName(path) + " (разрешён удалённый доступ для проверки)");
						}
						if (text.Contains("run_as_daemon") || text.Contains("service"))
						{
							list2.Add("AnyDesk: запущен как сервис (работает в фоновом режиме)");
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				string rustdeskCfg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RustDesk", "config");
				if (Directory.Exists(rustdeskCfg))
				{
					string peersDir = Path.Combine(rustdeskCfg, "peers");
					if (Directory.Exists(peersDir))
					{
						string[] peerFiles = Directory.GetFiles(peersDir, "*.toml");
						if (peerFiles != null && peerFiles.Length > 0)
						{
							list2.Add("RustDesk: обнаружены сохранённые сессии удалённых партнёров (" + peerFiles.Length + " пиров в config/peers)");
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano -p tcp");
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute = false;
				psi.CreateNoWindow = true;
				using (Process p = Process.Start(psi))
				{
					string netOut = p.StandardOutput.ReadToEnd();
					if (!p.WaitForExit(3000)) { try { p.Kill(); } catch { } }
					string[] lines = netOut.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string rawLine in lines)
					{
						string line = rawLine.Trim();
						if (!line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase)) continue;
						if (line.Contains(":7070") || line.Contains(":21115") || line.Contains(":21116") || line.Contains(":21117") ||
							line.Contains(":5938") || line.Contains(":3389") || line.Contains(":5900"))
						{
							if (line.IndexOf("ESTABLISHED", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("SYN_SENT", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								list2.Add("🚨 [5.08 - Активный сокет удалённого управления]: " + line);
							}
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT SessionName, UserName, State FROM Win32_LogonSession"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						string text2 = Str(item, "UserName");
						string text3 = Str(item, "State");
						list.Add("Сессия: " + text2 + " | состояние: " + text3);
					}
				}
			}
			catch
			{
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Признаков удалённого подключения: " + list2.Count) : "Активных удалённых сессий не обнаружено");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckJigglerPresence()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Бустеры мыши, KMBox, Jiggler, автокликеры");
			List<string> info = new List<string>();
			List<string> list = new List<string>();
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				string text = process.ProcessName.ToLowerInvariant();
				string[] jigglerNames = JigglerNames;
				foreach (string value in jigglerNames)
				{
					if (text.IndexOf(value, StringComparison.Ordinal) >= 0)
					{
						info.Add("ℹ️ [Разрешено регламентом] Автокликер/джигглер: " + process.ProcessName + " (PID " + process.Id + ")");
						break;
					}
				}
			}
			string[] array = new string[3]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
			};
			string[] array2 = new string[]
			{
				"mouse jiggler.exe", "screen jiggler.exe", "power jiggler.exe", "fakemouse.exe", "kmbox.exe", "makcu.exe", "kmbox_net.exe", "kmbox_b.exe"
			};
			string[] array3 = array;
			foreach (string text2 in array3)
			{
				if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
				{
					continue;
				}
				try
				{
					string[] files = Directory.GetFiles(text2, "*.exe", SearchOption.TopDirectoryOnly);
					foreach (string text3 in files)
					{
						string text4 = Path.GetFileName(text3).ToLowerInvariant();
						if (CheatDatabases.IsWhitelistedPath(text3) || text4.Contains("mccheat") || text4.Contains("packages") || text4.Contains("проги для проверки")) continue;
						string[] array4 = array2;
						foreach (string text5 in array4)
						{
							if (text4 == text5)
							{
								info.Add("ℹ️ [Разрешено регламентом] Файл автокликера: " + text3);
								break;
							}
						}
					}
					string[] directories = Directory.GetDirectories(text2, "*", SearchOption.TopDirectoryOnly);
					foreach (string text6 in directories)
					{
						string text7 = Path.GetFileName(text6).ToLowerInvariant();
						if (CheatDatabases.IsWhitelistedPath(text6) || text7.Contains("mccheat") || text7.Contains("packages") || text7.Contains("проги для проверки")) continue;
						string[] jigglerNames2 = JigglerNames;
						foreach (string text8 in jigglerNames2)
						{
							if (!(text7 != text8) || text7.StartsWith(text8 + " ") || text7.StartsWith(text8 + "-"))
							{
								info.Add("ℹ️ [Разрешено регламентом] Папка автокликера: " + text6);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			try
			{
				string[] files2 = Directory.GetFiles(folderPath, "*.lnk");
				foreach (string text9 in files2)
				{
					string text10 = Path.GetFileNameWithoutExtension(text9).ToLowerInvariant();
					string[] jigglerNames3 = JigglerNames;
					foreach (string text11 in jigglerNames3)
					{
						if (text10.IndexOf(text11, StringComparison.Ordinal) >= 0)
						{
							list.Add("[ярлык] " + text9 + " (сигнатура: " + text11 + ")");
							break;
						}
					}
				}
			}
			catch
			{
			}
			checkResult.Detail = ((list.Count > 0) ? ("Обнаружены бустеры/кликеры на микроконтроллерах: " + list.Count) : "Бустеров и KMBox-устройств не найдено");
			checkResult.Detected = list.Count > 0;
			checkResult.Severity = ((list.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Evidence = Join(info, list);
			return checkResult;
		}

		private static CheckResult CheckUptime()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Время непрерывной работы системы (uptime)");
			int tickCount = Environment.TickCount;
			int num = ((tickCount >= 0) ? tickCount : 0);
			int num2 = num / 3600000;
			int num3 = num % 3600000 / 60000;
			int num4 = num % 60000 / 1000;
			checkResult.Detail = "Uptime: " + FormatUptime(num) + " (" + num2 + " ч " + num3 + " мин " + num4 + " сек)";
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detected = false;
			if (num < 180000)
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Система перезапущена менее 3 минут назад — игрок перезагрузил ПК после вызова";
				checkResult.Evidence = "Uptime: " + FormatUptime(num);
			}
			return checkResult;
		}

		private static CheckResult CheckPowerSettings()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Настройки энергосбережения (отключение сна)");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			string[] powerPolicyKeys = PowerPolicyKeys;
			foreach (string name in powerPolicyKeys)
			{
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
					{
						if (registryKey == null)
						{
							continue;
						}
						object value = registryKey.GetValue("Value");
						if (value != null)
						{
							int num = Convert.ToInt32(value);
							if (num == 0)
							{
								list2.Add("Энергосбережение отключено (Value=0): система не заснёт — возможно, для обхода проверок");
							}
							else
							{
								list.Add("Энергосбережение: Value=" + num);
							}
						}
					}
				}
				catch
				{
				}
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Настройки энергосбережения изменены (отключён сон): " + list2.Count) : "Настройки энергосбережения в норме");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckSessionUser()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Пользователь сеанса — сравнение с модератором");
			string userName = Environment.UserName;
			string machineName = Environment.MachineName;
			checkResult.Detail = "Пользователь сеанса: " + userName + " | Компьютер: " + machineName;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detected = false;
			checkResult.Evidence = "Это поле заполняется модератором в форме проверки.\nЕсли имя модератора ≠ \"" + userName + "\", игрок залогинен на своей машине.";
			return checkResult;
		}

		private static CheckResult NewResult(string category, string name)
		{
			CheckResult checkResult = new CheckResult();
			checkResult.Category = category;
			checkResult.Name = name;
			checkResult.Detail = "";
			checkResult.Evidence = "";
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			return checkResult;
		}

		private static string Join(List<string> info, List<string> hits)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (hits != null && hits.Count > 0)
			{
				stringBuilder.AppendLine("=== НАЙДЕНО ===");
				for (int i = 0; i < hits.Count && i < 200; i++)
				{
					stringBuilder.AppendLine(hits[i]);
				}
				stringBuilder.AppendLine();
			}
			if (info != null && info.Count > 0)
			{
				stringBuilder.AppendLine("=== СВЕДЕНИЯ ===");
				for (int j = 0; j < info.Count && j < 200; j++)
				{
					stringBuilder.AppendLine(info[j]);
				}
			}
			return stringBuilder.ToString();
		}

		private static string Str(ManagementObject o, string prop)
		{
			try
			{
				object obj = o[prop];
				return (obj == null) ? "" : obj.ToString();
			}
			catch
			{
				return "";
			}
		}

		private static string FormatUptime(int ms)
		{
			int num = ms / 1000;
			int num2 = num / 3600;
			int num3 = num % 3600 / 60;
			int num4 = num % 60;
			return num2 + "ч " + num3 + "м " + num4 + "с";
		}

		private static CheckResult CheckUnixSessionUser()
		{
			CheckResult checkResult = NewResult("Присутствие человека", "Текущий пользователь системы (" + PlatformHelper.OSDescription + ")");
			string userName = Environment.UserName;
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			checkResult.Detail = "Пользователь: " + userName + " | Домашняя папка: " + folderPath;
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Evidence = "OS: " + PlatformHelper.OSDescription + "\nUser: " + userName + "\nHome: " + folderPath;
			return checkResult;
		}
	}
}
