using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MCCheatChecker
{
	public static class NetworkAndRemoteForensics
	{
		public static List<NetworkSocketItem> GetActiveMinecraftSockets()
		{
			List<NetworkSocketItem> list = new List<NetworkSocketItem>();
			try
			{
				Dictionary<int, string> dictionary = new Dictionary<int, string>();
				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					try
					{
						string text = process.ProcessName.ToLowerInvariant();
						bool isSuspect = text == "javaw" || text == "java" || text.Contains("minecraft") || text.Contains("bedrock") ||
							text.Contains("vape") || text.Contains("drip") || text.Contains("slinky") || text.Contains("itami") ||
							text.Contains("kura") || text.Contains("flint") || text.Contains("cellent") || text.Contains("archware") ||
							text.Contains("celestial") || text.Contains("expensive") || text.Contains("nursultan") || text.Contains("wexside") ||
							text.Contains("deadcode") || text.Contains("akrien") || text.Contains("clicker") || text.Contains("inject");
						if (!isSuspect)
						{
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && text.Contains(cl)) { isSuspect = true; break; }
							}
						}
						if (!isSuspect)
						{
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && text.Contains(ec.ToLowerInvariant())) { isSuspect = true; break; }
							}
						}
						if (isSuspect)
						{
							dictionary[process.Id] = process.ProcessName;
						}
					}
					catch
					{
					}
				}
				ProcessStartInfo processStartInfo = new ProcessStartInfo("netstat", "-ano -p tcp");
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				using (Process process2 = Process.Start(processStartInfo))
				{
					string text2 = process2.StandardOutput.ReadToEnd();
					if (!process2.WaitForExit(3000)) { try { process2.Kill(); } catch { } }
					string[] array = text2.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array2 = array;
					foreach (string text3 in array2)
					{
						string text4 = text3.Trim();
						if (!text4.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string[] array3 = Regex.Split(text4, "\\s+");
						int result;
						string value;
						if (array3.Length >= 5 && int.TryParse(array3[array3.Length - 1], out result) && dictionary.TryGetValue(result, out value))
						{
							NetworkSocketItem networkSocketItem = new NetworkSocketItem();
							networkSocketItem.Pid = result;
							networkSocketItem.ProcessName = value;
							networkSocketItem.LocalEndpoint = array3[1];
							networkSocketItem.RemoteEndpoint = array3[2];
							networkSocketItem.State = array3[3];
							string remoteEndpoint = networkSocketItem.RemoteEndpoint;
							bool isCheatProcess = !value.ToLowerInvariant().Contains("java") && !value.ToLowerInvariant().Contains("minecraft") && !value.ToLowerInvariant().Contains("bedrock");
							if (isCheatProcess)
							{
								networkSocketItem.IsSuspicious = true;
								networkSocketItem.Verdict = "🚨 [5.01 - Сокет стороннего чит-процесса]: " + value;
							}
							bool isCleanerProc = false;
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && value.ToLowerInvariant().Contains(cl)) { isCleanerProc = true; break; }
							}
							if (isCleanerProc)
							{
								networkSocketItem.IsSuspicious = true;
								networkSocketItem.Verdict = "🚨 [5.08 - Сетевой сокет клинера]: " + value;
							}
							else if (remoteEndpoint.StartsWith("127.0.0.1:") || remoteEndpoint.StartsWith("[::1]:") || remoteEndpoint.StartsWith("localhost:"))
							{
								networkSocketItem.IsSuspicious = true;
								networkSocketItem.Verdict = "🚨 [5.01 - Локальный IPC / Web-интерфейс чита (Vape/Drip/Slinky)]: " + remoteEndpoint;
							}
							else
							if (remoteEndpoint.EndsWith(":6667") || remoteEndpoint.EndsWith(":8088") || remoteEndpoint.EndsWith(":1337") || remoteEndpoint.EndsWith(":7777") || remoteEndpoint.EndsWith(":31337") ||
								remoteEndpoint.EndsWith(":4444") || remoteEndpoint.EndsWith(":5555") || remoteEndpoint.EndsWith(":8888") || remoteEndpoint.EndsWith(":9999") ||
								remoteEndpoint.EndsWith(":7070") || remoteEndpoint.EndsWith(":21115") || remoteEndpoint.EndsWith(":21116") || remoteEndpoint.EndsWith(":21117") || remoteEndpoint.EndsWith(":21118") || remoteEndpoint.EndsWith(":21119") ||
								remoteEndpoint.EndsWith(":5938") || remoteEndpoint.EndsWith(":3389") || remoteEndpoint.EndsWith(":5900") || remoteEndpoint.EndsWith(":5901"))
							{
								networkSocketItem.IsSuspicious = true;
								networkSocketItem.Verdict = "\ud83d\udea8 Подозрительное соединение (IRC / Cloud Sync)";
							}
							else if (remoteEndpoint.EndsWith(":25565") || remoteEndpoint.EndsWith(":25577") || remoteEndpoint.EndsWith(":19132"))
							{
								networkSocketItem.Verdict = "\ud83d\udfe2 Игровой сервер Minecraft";
							}
							else if (remoteEndpoint.EndsWith(":443") || remoteEndpoint.EndsWith(":80"))
							{
								networkSocketItem.Verdict = "Веб / Сервис аутентификации (HTTPS)";
							}
							else
							{
								networkSocketItem.Verdict = "Активное соединение";
							}
							list.Add(networkSocketItem);
						}
					}
				}
			}
			catch
			{
			}
			return list;
		}

		public static List<AnyDeskTraceItem> GetAnyDeskTraceEvents()
		{
			List<AnyDeskTraceItem> list = new List<AnyDeskTraceItem>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			// 1. AnyDesk Traces
			string[] anydeskTraces = new string[]
			{
				Path.Combine(folderPath, "AnyDesk", "ad.trace"),
				Path.Combine(folderPath2, "AnyDesk", "ad_svc.trace")
			};
			foreach (string p in anydeskTraces)
			{
				if (!File.Exists(p)) continue;
				try
				{
					List<string> list2 = new List<string>();
					using (FileStream stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
						{
							string item;
							while ((item = streamReader.ReadLine()) != null)
							{
								if (list2.Count > 1000) list2.RemoveAt(0);
								list2.Add(item);
							}
						}
					}
					int num = list2.Count - 1;
					while (num >= 0 && list.Count < 120)
					{
						string line = list2[num];
						string lineLower = line.ToLowerInvariant();
						string time = "";
						Match match = Regex.Match(line, "\\d{4}-\\d{2}-\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
						if (match.Success) time = match.Value;

						string remoteId = "—";
						Match idMatch = Regex.Match(line, "(?:Logged in from|Remote ID|aid:|incoming session request from|peer)\\s*([0-9]{8,11})", RegexOptions.IgnoreCase);
						if (idMatch.Success) remoteId = idMatch.Groups[1].Value;

						if (lineLower.Contains("session started"))
						{
							list.Add(new AnyDeskTraceItem
							{
								Time = time,
								Category = "AnyDesk: Начало сессии",
								RemoteId = remoteId,
								Description = "Подключение к удалённой сессии AnyDesk установлено",
								IsWarning = false
							});
						}
						else if (lineLower.Contains("file transfer") || lineLower.Contains("local_file_transfer") || lineLower.Contains("upgrading file transfer"))
						{
							list.Add(new AnyDeskTraceItem
							{
								Time = time,
								Category = "AnyDesk: Передача файлов",
								RemoteId = remoteId,
								Description = "Зафиксирована передача файлов через AnyDesk: " + ExtractTail(line),
								IsWarning = true
							});
						}
						else if (lineLower.Contains("remote shell permissions") || lineLower.Contains("remote shell"))
						{
							list.Add(new AnyDeskTraceItem
							{
								Time = time,
								Category = "AnyDesk: Remote Shell",
								RemoteId = remoteId,
								Description = "Запрос/активация прав удалённого шелла: " + ExtractTail(line),
								IsWarning = true
							});
						}
						else if (lineLower.Contains("the socket was closed remotely"))
						{
							list.Add(new AnyDeskTraceItem
							{
								Time = time,
								Category = "AnyDesk: Конец сессии",
								RemoteId = remoteId,
								Description = "Сессия удалённого доступа закрыта",
								IsWarning = false
							});
						}
						else if (lineLower.Contains("frontend startup"))
						{
							list.Add(new AnyDeskTraceItem
							{
								Time = time,
								Category = "AnyDesk: Запуск",
								RemoteId = remoteId,
								Description = "Запуск графического клиента AnyDesk",
								IsWarning = false
							});
						}
						num--;
					}
				}
				catch
				{
				}
			}

			// 2. RustDesk Logs
			try
			{
				string rustdeskLogDir = Path.Combine(folderPath, "RustDesk", "log");
				if (Directory.Exists(rustdeskLogDir))
				{
					string[] rLogs = Directory.GetFiles(rustdeskLogDir, "*.log");
					foreach (string rLog in rLogs)
					{
						using (FileStream rStream = new FileStream(rLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader rReader = new StreamReader(rStream, Encoding.UTF8))
							{
								List<string> rLines = new List<string>();
								string rLine;
								while ((rLine = rReader.ReadLine()) != null)
								{
									if (rLines.Count > 500) rLines.RemoveAt(0);
									rLines.Add(rLine);
								}
								for (int i = rLines.Count - 1; i >= 0 && list.Count < 160; i--)
								{
									string rl = rLines[i];
									string rlLower = rl.ToLowerInvariant();
									string time = "";
									Match match = Regex.Match(rl, "\\d{4}-\\d{2}-\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
									if (match.Success) time = match.Value;

									string rId = "—";
									Match idMatch = Regex.Match(rl, "peer[\\s_]*id[:\\s]+([0-9]+)", RegexOptions.IgnoreCase);
									if (idMatch.Success) rId = idMatch.Groups[1].Value;

									if (rlLower.Contains("file transfer") || rlLower.Contains("send file") || rlLower.Contains("recv file"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "RustDesk: Передача файлов",
											RemoteId = rId,
											Description = "Передача файлов через RustDesk: " + ExtractTail(rl),
											IsWarning = true
										});
									}
									else if (rlLower.Contains("connection accepted") || rlLower.Contains("session started") || rlLower.Contains("logged in"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "RustDesk: Сессия",
											RemoteId = rId,
											Description = "Установлено соединение RustDesk",
											IsWarning = false
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}

			// 2b. RuDesktop Logs
			try
			{
				string[] ruLogDirs = new string[]
				{
					Path.Combine(folderPath, "RuDesktop", "logs"),
					Path.Combine(localAppData, "RuDesktop", "logs"),
					Path.Combine(folderPath2, "RuDesktop", "logs")
				};
				foreach (string ruLogDir in ruLogDirs)
				{
					if (!Directory.Exists(ruLogDir)) continue;
					string[] ruLogs = Directory.GetFiles(ruLogDir, "*.log");
					foreach (string ruLog in ruLogs)
					{
						using (FileStream ruStream = new FileStream(ruLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader ruReader = new StreamReader(ruStream, Encoding.UTF8))
							{
								List<string> ruLines = new List<string>();
								string ruLine;
								while ((ruLine = ruReader.ReadLine()) != null)
								{
									if (ruLines.Count > 500) ruLines.RemoveAt(0);
									ruLines.Add(ruLine);
								}
								for (int i = ruLines.Count - 1; i >= 0 && list.Count < 180; i--)
								{
									string rl = ruLines[i];
									string rlLower = rl.ToLowerInvariant();
									string time = "";
									Match match = Regex.Match(rl, "\\d{4}-\\d{2}-\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
									if (match.Success) time = match.Value;
									string rId = "—";
									Match idMatch = Regex.Match(rl, "([0-9]{6,12})");
									if (idMatch.Success) rId = idMatch.Groups[1].Value;
									if (rlLower.Contains("file") || rlLower.Contains("transfer") || rlLower.Contains("download") || rlLower.Contains("upload"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "RuDesktop: Передача файлов",
											RemoteId = rId,
											Description = "Передача файлов через RuDesktop: " + ExtractTail(rl),
											IsWarning = true
										});
									}
									else if (rlLower.Contains("connected") || rlLower.Contains("session") || rlLower.Contains("start"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "RuDesktop: Сессия",
											RemoteId = rId,
											Description = "Сессия RuDesktop: " + ExtractTail(rl),
											IsWarning = false
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}

			// 3. TeamViewer Logs
			try
			{
				string[] tvDirs = new string[] { "C:\\Program Files\\TeamViewer", "C:\\Program Files (x86)\\TeamViewer", Path.Combine(localAppData, "TeamViewer") };
				foreach (string tvDir in tvDirs)
				{
					if (!Directory.Exists(tvDir)) continue;
					string[] tvLogs = Directory.GetFiles(tvDir, "*Logfile*.log");
					foreach (string tvLog in tvLogs)
					{
						using (FileStream tvStream = new FileStream(tvLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader tvReader = new StreamReader(tvStream, Encoding.UTF8))
							{
								List<string> tvLines = new List<string>();
								string tl;
								while ((tl = tvReader.ReadLine()) != null)
								{
									if (tvLines.Count > 300) tvLines.RemoveAt(0);
									tvLines.Add(tl);
								}
								for (int j = tvLines.Count - 1; j >= 0 && list.Count < 200; j--)
								{
									string row = tvLines[j];
									string rowLower = row.ToLowerInvariant();
									string time = "";
									Match m = Regex.Match(row, "\\d{4}/\\d{2}/\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
									if (m.Success) time = m.Value;

									if (rowLower.Contains("filetransfer") || rowLower.Contains("file transfer"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "TeamViewer: Передача файлов",
											RemoteId = "—",
											Description = "Зафиксирована передача файлов TeamViewer: " + ExtractTail(row),
											IsWarning = true
										});
									}
									else if (rowLower.Contains("participant") && rowLower.Contains("connected"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = time,
											Category = "TeamViewer: Сессия",
											RemoteId = "—",
											Description = "Подключение участника сессии TeamViewer",
											IsWarning = false
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}

			// 4. UltraViewer Logs
			try
			{
				string[] uvDirs = new string[]
				{
					Path.Combine(folderPath, "UltraViewer"),
					Path.Combine(folderPath2, "UltraViewer"),
					"C:\\Program Files (x86)\\UltraViewer",
					"C:\\Program Files\\UltraViewer"
				};
				foreach (string uvDir in uvDirs)
				{
					if (!Directory.Exists(uvDir)) continue;
					string[] uvLogs = Directory.GetFiles(uvDir, "*log*.txt");
					foreach (string uvLog in uvLogs)
					{
						using (FileStream uvStream = new FileStream(uvLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader uvReader = new StreamReader(uvStream, Encoding.UTF8))
							{
								List<string> uvLines = new List<string>();
								string uvl;
								while ((uvl = uvReader.ReadLine()) != null)
								{
									if (uvLines.Count > 300) uvLines.RemoveAt(0);
									uvLines.Add(uvl);
								}
								for (int u = uvLines.Count - 1; u >= 0 && list.Count < 220; u--)
								{
									string uRow = uvLines[u];
									string uLower = uRow.ToLowerInvariant();
									string uTime = "";
									Match m = Regex.Match(uRow, "\\d{4}-\\d{2}-\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
									if (m.Success) uTime = m.Value;
									if (uLower.Contains("transfer") || uLower.Contains("file"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = uTime,
											Category = "UltraViewer: Передача файлов",
											RemoteId = "—",
											Description = "Передача файлов через UltraViewer: " + ExtractTail(uRow),
											IsWarning = true
										});
									}
									else if (uLower.Contains("connect") || uLower.Contains("partner") || uLower.Contains("start"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = uTime,
											Category = "UltraViewer: Сессия",
											RemoteId = "—",
											Description = "Сессия UltraViewer: " + ExtractTail(uRow),
											IsWarning = false
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}

			// 5. AeroAdmin Logs
			try
			{
				string[] aeroDirs = new string[]
				{
					Path.Combine(localAppData, "AeroAdmin"),
					Path.Combine(folderPath, "AeroAdmin")
				};
				foreach (string aeroDir in aeroDirs)
				{
					if (!Directory.Exists(aeroDir)) continue;
					string[] aeroLogs = Directory.GetFiles(aeroDir, "*.log");
					foreach (string aeroLog in aeroLogs)
					{
						using (FileStream aStream = new FileStream(aeroLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader aReader = new StreamReader(aStream, Encoding.UTF8))
							{
								List<string> aLines = new List<string>();
								string al;
								while ((al = aReader.ReadLine()) != null)
								{
									if (aLines.Count > 300) aLines.RemoveAt(0);
									aLines.Add(al);
								}
								for (int a = aLines.Count - 1; a >= 0 && list.Count < 240; a--)
								{
									string aRow = aLines[a];
									string aLower = aRow.ToLowerInvariant();
									string aTime = "";
									Match m = Regex.Match(aRow, "\\d{4}/\\d{2}/\\d{2}\\s+\\d{2}:\\d{2}:\\d{2}");
									if (m.Success) aTime = m.Value;
									if (aLower.Contains("file") || aLower.Contains("transfer"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = aTime,
											Category = "AeroAdmin: Передача файлов",
											RemoteId = "—",
											Description = "Передача файлов через AeroAdmin: " + ExtractTail(aRow),
											IsWarning = true
										});
									}
									else if (aLower.Contains("session") || aLower.Contains("connect"))
									{
										list.Add(new AnyDeskTraceItem
										{
											Time = aTime,
											Category = "AeroAdmin: Сессия",
											RemoteId = "—",
											Description = "Сессия AeroAdmin: " + ExtractTail(aRow),
											IsWarning = false
										});
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}

			return list;
		}

		private static string ExtractTail(string line)
		{
			if (string.IsNullOrEmpty(line))
			{
				return "";
			}
			int num = line.IndexOf(" - ");
			if (num >= 0 && num + 3 < line.Length)
			{
				return line.Substring(num + 3).Trim();
			}
			if (line.Length <= 80)
			{
				return line;
			}
			return line.Substring(line.Length - 80);
		}

		public static string GetLocalAnyDeskId()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string[] array = new string[2]
			{
				Path.Combine(folderPath, "AnyDesk", "system.conf"),
				Path.Combine(folderPath2, "AnyDesk", "system.conf")
			};
			string[] array2 = array;
			foreach (string path in array2)
			{
				if (!File.Exists(path))
				{
					continue;
				}
				try
				{
					string[] array3 = File.ReadAllLines(path);
					foreach (string text in array3)
					{
						string text2 = text.Trim();
						if (text2.StartsWith("ad.anynet.id="))
						{
							string text3 = text2.Substring("ad.anynet.id=".Length).Trim();
							if (!string.IsNullOrEmpty(text3))
							{
								return text3;
							}
						}
					}
				}
				catch
				{
				}
			}
			return null;
		}

		public static string FormatAnyDeskId(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return "—";
			}
			string text = raw.Replace(" ", "").Trim();
			if (text.Length == 9)
			{
				return string.Format("{0} {1} {2}", text.Substring(0, 3), text.Substring(3, 3), text.Substring(6, 3));
			}
			if (text.Length == 10)
			{
				return string.Format("{0} {1} {2} {3}", text.Substring(0, 1), text.Substring(1, 3), text.Substring(4, 3), text.Substring(7, 3));
			}
			return text;
		}

		public static List<string> DetectActiveRemoteAccessTools()
		{
			List<string> list = new List<string>();
			string[] array = new string[]
			{
				"anydesk", "rudesktop", "hopetodesk", "teamviewer", "rustdesk", "ultraviewer", "ammyy", "parsec", "radmin", "litemanager", "tightvnc", "realvnc",
				"supremo"
			};
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				try
				{
					string text = process.ProcessName.ToLowerInvariant();
					string[] array2 = array;
					foreach (string value in array2)
					{
						if (text.Contains(value))
						{
							list.Add(string.Format("{0} (PID: {1})", process.ProcessName, process.Id));
							break;
						}
					}
				}
				catch
				{
				}
			}
			return list;
		}

		public static List<DnsCacheEntry> GetDnsCacheEntries()
		{
			List<DnsCacheEntry> list = new List<DnsCacheEntry>();
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo("ipconfig", "/displaydns");
				processStartInfo.CreateNoWindow = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.RedirectStandardOutput = true;
				try
				{
					processStartInfo.StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
				}
				catch
				{
					processStartInfo.StandardOutputEncoding = Encoding.Default;
				}
				string text;
				using (Process process = Process.Start(processStartInfo))
				{
					text = process.StandardOutput.ReadToEnd();
					process.WaitForExit(3000);
				}
				if (string.IsNullOrEmpty(text))
				{
					return list;
				}
				DnsCacheEntry dnsCacheEntry = new DnsCacheEntry();
				string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string[] array2 = array;
				foreach (string text2 in array2)
				{
					string text3 = text2.Trim();
					if (text3.Contains("---"))
					{
						continue;
					}
					if (text3.Contains(" : "))
					{
						string[] array3 = text3.Split(new string[1] { " : " }, StringSplitOptions.None);
						if (array3.Length == 2)
						{
							string text4 = array3[0].Trim().ToLowerInvariant();
							string text5 = array3[1].Trim();
							if (text4.Contains("имя") || text4.Contains("record name"))
							{
								dnsCacheEntry.Domain = text5;
							}
							else if (text4.Contains("тип") || text4.Contains("record type"))
							{
								dnsCacheEntry.RecordType = text5;
							}
							else if (text4.Contains("срок") || text4.Contains("time to live") || text4.Contains("ttl"))
							{
								dnsCacheEntry.Ttl = text5 + " сек";
							}
							else if (text4.Contains("запись") || text4.Contains("record") || text4.Contains("cname") || text4.Contains("host") || text4.Contains("узла"))
							{
								dnsCacheEntry.Data = text5;
							}
						}
					}
					else if (!string.IsNullOrEmpty(text3) && text3.Contains(".") && !text3.StartsWith("Настройка") && !text3.StartsWith("Windows") && !text3.Contains(" "))
					{
						if (string.IsNullOrEmpty(dnsCacheEntry.Domain))
						{
							dnsCacheEntry.Domain = text3;
						}
					}
					else if (text3.Length == 0 && !string.IsNullOrEmpty(dnsCacheEntry.Domain))
					{
						CheckDnsSuspicious(dnsCacheEntry);
						list.Add(dnsCacheEntry);
						dnsCacheEntry = new DnsCacheEntry();
					}
				}
				if (!string.IsNullOrEmpty(dnsCacheEntry.Domain))
				{
					CheckDnsSuspicious(dnsCacheEntry);
					list.Add(dnsCacheEntry);
				}
			}
			catch
			{
			}
			return list;
		}

		private static void CheckDnsSuspicious(DnsCacheEntry e)
		{
			if (string.IsNullOrEmpty(e.Domain))
			{
				return;
			}
			string text = e.Domain.ToLowerInvariant();
			foreach (string cleaner in CheatDatabases.KnownCleanerSignatures)
			{
				if (cleaner.Length >= 4 && text.Contains(cleaner.ToLowerInvariant()) && !text.Contains("chrome") && !text.Contains("discord") && !text.Contains("telegram") && !text.Contains("steam"))
				{
					e.IsSuspicious = true;
					e.Category = "🚨 [5.08 - DNS клинера / сервиса очистки]: " + cleaner;
					return;
				}
			}
			string[] extraCheatDomains = CheatDatabases.ExtraCheatDomains;
			foreach (string text2 in extraCheatDomains)
			{
				if (text2.Length >= 4 && text.Contains(text2.ToLowerInvariant()))
				{
					e.IsSuspicious = true;
					e.Category = "🚨 [5.01 - Чит-домен/лоадер]: " + text2;
					return;
				}
			}
			string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
			foreach (string text3 in extraCheatClients)
			{
				if (text3.Length >= 4 && text.Contains(text3.ToLowerInvariant()))
				{
					e.IsSuspicious = true;
					e.Category = "🚨 [5.01 - Чит-клиент]: " + text3;
					return;
				}
			}
			string[] array = new string[]
			{
				"discord.com", "discordapp.com", "pastebin.com", "paste.ee", "hastebin.com", "rentry.co", "rentry.org",
				"raw.githubusercontent.com", "gofile.io", "krakenfiles.com", "catbox.moe", "pixeldrain.com", "anonfiles.com", "workupload.com", "mega.nz", "mediafire.com"
			};
			foreach (string value in array)
			{
				if (text.Contains(value))
				{
					e.IsSuspicious = true;
					e.Category = "⚠️ Webhook / Pastebin (" + value + ")";
					return;
				}
			}
			string[] tunnelAndRatDomains = new string[]
			{
				"ngrok.io", "ngrok-free.app", "trycloudflare.com", "playit.gg", "bore.pub", "localtonet.com", "localtunnel.me", "portmap.io"
			};
			foreach (string tunnel in tunnelAndRatDomains)
			{
				if (text.Contains(tunnel))
				{
					e.IsSuspicious = true;
					e.Category = "🚨 [5.08 - Туннелирование/проброс портов]: " + tunnel;
					return;
				}
			}
			string[] remoteAccessDomains = new string[]
			{
				"anydesk.com", "rustdesk.com", "rudesktop.ru", "teamviewer.com", "hopetodesk.com", "parsec.app", "ultraviewer.net", "ammyy.com", "litemanager.ru"
			};
			foreach (string remote in remoteAccessDomains)
			{
				if (text.Contains(remote))
				{
					e.IsSuspicious = true;
					e.Category = "📡 [5.08 - Удалённый доступ]: " + remote;
					return;
				}
			}
			if (string.IsNullOrEmpty(e.Category))
			{
				e.Category = "Легитимный домен";
			}
		}
	}
}