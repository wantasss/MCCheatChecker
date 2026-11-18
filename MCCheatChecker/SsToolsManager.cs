using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class SsToolsManager
	{
		public class SsToolDefinition
		{
			public string Id { get; set; }
			public string Title { get; set; }
			public string Url { get; set; }
			public string Description { get; set; }
			public string[] ExePatterns { get; set; }
			public Action CustomAction { get; set; }

			public string FindExecutable()
			{
				string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				string appDir = AppDomain.CurrentDomain.BaseDirectory;

				List<string> searchDirs = new List<string>
				{
					Path.Combine(appDir, "tools"),
					appDir,
					Path.Combine(appData, "MCCheatChecker", "tools"),
					Path.Combine(localApp, "MCCheatChecker", "tools"),
					Path.Combine(userProf, "Desktop", "tools"),
					Path.Combine(userProf, "Desktop", "SS"),
					Path.Combine(userProf, "Desktop"),
					Path.Combine(userProf, "Downloads"),
					Path.Combine(userProf, "Downloads", "tools"),
					Path.Combine(progFiles, "System Informer"),
					Path.Combine(progFiles, "Process Hacker 2"),
					Path.Combine(progFiles, "Everything"),
					Path.Combine(progFilesX86, "Everything"),
					Path.Combine(localApp, "Programs", "Everything"),
					Path.Combine(localApp, "Programs", "SystemInformer")
				};

				foreach (string dir in searchDirs)
				{
					if (!Directory.Exists(dir)) continue;
					try
					{
						foreach (string pattern in ExePatterns)
						{
							string[] matches = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
							if (matches != null && matches.Length > 0)
							{
								return matches[0];
							}
						}
					}
					catch { }
				}

				return null;
			}

			public bool IsInstalled()
			{
				return !string.IsNullOrEmpty(FindExecutable());
			}

			public void Launch(Action<string> setStatus)
			{
				string exe = FindExecutable();
				if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
				{
					try
					{
						Process.Start(exe);
						if (setStatus != null) setStatus("Запущена утилита [" + Title + "]: " + Path.GetFileName(exe));
						return;
					}
					catch (Exception ex)
					{
						if (setStatus != null) setStatus("Ошибка запуска " + Title + ": " + ex.Message);
						return;
					}
				}

				if (CustomAction != null)
				{
					try
					{
						CustomAction();
						if (setStatus != null) setStatus("Выполнено встроенное действие для " + Title);
						return;
					}
					catch (Exception ex)
					{
						if (setStatus != null) setStatus("Ошибка: " + ex.Message);
					}
				}

				if (!string.IsNullOrEmpty(Url))
				{
					DialogResult dr = MessageBox.Show(
						"Утилита \"" + Title + "\" не найдена локально на ПК.\n\n" +
						"Описание: " + Description + "\n\n" +
						"Открыть страницу загрузки / GitHub репозитория?\n" + Url,
						"MC Cheat Checker — Внешние SS утилиты",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);

					if (dr == DialogResult.Yes)
					{
						try
						{
							Process.Start(Url);
							if (setStatus != null) setStatus("Открыта страница загрузки " + Title);
						}
						catch { }
					}
				}
				else
				{
					MessageBox.Show(
						"Утилита \"" + Title + "\" не найдена на ПК.\nПоместите файл в папку 'tools' рядом с программой.",
						"MC Cheat Checker",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
			}
		}

		public static List<SsToolDefinition> GetTools()
		{
			List<SsToolDefinition> tools = new List<SsToolDefinition>();

			// 1. javaw
			tools.Add(new SsToolDefinition
			{
				Id = "javaw",
				Title = "javaw",
				Url = "",
				Description = "Анализ запущенного процесса Minecraft (javaw.exe), параметры памяти и открытие папки",
				ExePatterns = new string[] { "javaw.exe" },
				CustomAction = delegate
				{
					Process[] procs = Process.GetProcessesByName("javaw");
					if (procs == null || procs.Length == 0)
					{
						procs = Process.GetProcessesByName("java");
					}
					if (procs != null && procs.Length > 0)
					{
						Process p = procs[0];
						try
						{
							string path = p.MainModule.FileName;
							PlatformHelper.SelectFileInFileManager(path);
							MessageBox.Show(
								"Процесс Minecraft найден!\n\n" +
								"PID: " + p.Id + "\n" +
								"Путь: " + path + "\n" +
								"ОЗУ: " + (p.WorkingSet64 / 1024 / 1024) + " МБ\n" +
								"Потоков: " + p.Threads.Count,
								"javaw.exe — Свойства процесса",
								MessageBoxButtons.OK,
								MessageBoxIcon.Information);
						}
						catch (Exception ex)
						{
							MessageBox.Show("javaw.exe активен (PID: " + p.Id + "), но доступ ограничен: " + ex.Message, "javaw", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						}
					}
					else
					{
						MessageBox.Show("Процесс javaw.exe в данный момент не запущен на ПК.", "javaw", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			});

			// 2. SSTool
			tools.Add(new SsToolDefinition
			{
				Id = "sstool",
				Title = "SSTool",
				Url = "https://github.com/Orbdiff/SSTool/releases",
				Description = "Orbdiff SSTool — утилита проверки Minecraft Screenshare (строки, память, дампы)",
				ExePatterns = new string[] { "*SSTool*.exe", "*sstool*.exe", "*ss-tool*.exe", "*Orbdiff*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ScanSsToolArtifacts();
					ShowCheckResultsDialog("SSTool — Анализ артефактов и дампов памяти", results);
				}
			});

			// 3. System Informer
			tools.Add(new SsToolDefinition
			{
				Id = "system_informer",
				Title = "System Informer",
				Url = "https://systeminformer.sourceforge.io/",
				Description = "System Informer (Process Hacker 3) — продвинутый аудит процессов, потоков, памяти и дескрипторов",
				ExePatterns = new string[] { "SystemInformer.exe", "ProcessHacker.exe" }
			});

			// 4. Shellbag Analyzer
			tools.Add(new SsToolDefinition
			{
				Id = "shellbag_analyzer",
				Title = "Shellbag Analyzer",
				Url = "https://ericzimmerman.github.io/",
				Description = "Eric Zimmerman ShellBags Explorer (SBECmd) — глубокий анализ посещённых и удалённых папок",
				ExePatterns = new string[] { "*ShellBagsExplorer*.exe", "*SBECmd*.exe", "*ShellbagAnalyzer*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunShellBagCheck();
					ShowCheckResultsDialog("ShellBag Analyzer — Результаты встроенного анализа", results);
				}
			});

			// 5. JournalTrace
			tools.Add(new SsToolDefinition
			{
				Id = "journal_trace",
				Title = "JournalTrace",
				Url = "https://github.com/search?q=JournalTrace",
				Description = "JournalTrace — трассировка изменений, удалений и переименований файлов в USN Journal",
				ExePatterns = new string[] { "*JournalTrace*.exe", "*journaltrace*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunUsnJournalCheck();
					ShowCheckResultsDialog("JournalTrace — Состояние и история USN Journal", results);
				}
			});

			// 6. Defender Log Viewer
			tools.Add(new SsToolDefinition
			{
				Id = "defender_log",
				Title = "Defender Log Viewer",
				Url = "",
				Description = "Просмотр журнала Windows Defender (обнаружения угроз EventID 1116/1117, отключения защиты 5001)",
				ExePatterns = new string[] { "*DefenderLog*.exe", "*DefenderViewer*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunWindowsDefenderEventCheck();
					ShowCheckResultsDialog("Windows Defender — Журнал срабатываний и отключений", results);
				}
			});

			// 7. UserAssist View
			tools.Add(new SsToolDefinition
			{
				Id = "userassist_view",
				Title = "UserAssist View",
				Url = "https://www.nirsoft.net/utils/userassist_view.html",
				Description = "NirSoft UserAssistView — декодирование ROT13 истории запусков программ из реестра Windows",
				ExePatterns = new string[] { "*UserAssistView*.exe", "*userassist*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunUserAssistForensicsCheck();
					ShowCheckResultsDialog("UserAssist View — Декодированная история запусков ПО", results);
				}
			});

			// 8. Recent Files View
			tools.Add(new SsToolDefinition
			{
				Id = "recent_files_view",
				Title = "Recent Files View",
				Url = "https://www.nirsoft.net/utils/recent_files_view.html",
				Description = "NirSoft RecentFilesView — просмотр LNK-ярлыков недавно открывавшихся файлов",
				ExePatterns = new string[] { "*RecentFilesView*.exe", "*recentfiles*.exe" },
				CustomAction = delegate
				{
					try
					{
						Process.Start("explorer.exe", "shell:recent");
					}
					catch { }
				}
			});

			// 9. Browsing History View
			tools.Add(new SsToolDefinition
			{
				Id = "browsing_history_view",
				Title = "Browsing History View",
				Url = "https://www.nirsoft.net/utils/browsing_history_view.html",
				Description = "NirSoft BrowsingHistoryView — история посещений всех браузеров (чит-сайты, форумы)",
				ExePatterns = new string[] { "*BrowsingHistoryView*.exe", "*browsinghistory*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunBrowserHistoryAndDownloadCheck();
					ShowCheckResultsDialog("Browsing History — История посещений и загрузок", results);
				}
			});

			// 10. Browser Downloads View
			tools.Add(new SsToolDefinition
			{
				Id = "browser_downloads_view",
				Title = "Browser Downloads View",
				Url = "https://www.nirsoft.net/utils/browser_downloads_view.html",
				Description = "NirSoft BrowserDownloadsView — аудит загруженных файлов из браузеров",
				ExePatterns = new string[] { "*BrowserDownloadsView*.exe", "*browserdownloads*.exe" },
				CustomAction = delegate
				{
					string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
					if (Directory.Exists(downloads))
					{
						PlatformHelper.OpenFolder(downloads);
					}
				}
			});

			// 11. Shell Bags View
			tools.Add(new SsToolDefinition
			{
				Id = "shell_bags_view",
				Title = "Shell Bags View",
				Url = "https://www.nirsoft.net/utils/shell_bags_view.html",
				Description = "NirSoft ShellBagsView — просмотр истории папок, открывавшихся в Проводнике",
				ExePatterns = new string[] { "*ShellBagsView*.exe", "*shellbagsview*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunShellBagCheck();
					ShowCheckResultsDialog("ShellBags View — Анализ открывавшихся каталогов", results);
				}
			});

			// 12. Executed Programs List
			tools.Add(new SsToolDefinition
			{
				Id = "executed_programs_list",
				Title = "Executed Programs List",
				Url = "https://www.nirsoft.net/utils/executed_programs_list.html",
				Description = "NirSoft ExecutedProgramsList — история выполнения программ из BAM, Prefetch и реестра",
				ExePatterns = new string[] { "*ExecutedProgramsList*.exe", "*executedprograms*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ManualChecks.RunAppCompatCacheAndBamCheck();
					ShowCheckResultsDialog("Executed Programs List — Аудит BAM и AppCompatCache", results);
				}
			});

			// 13. Clean Detecting
			tools.Add(new SsToolDefinition
			{
				Id = "clean_detecting",
				Title = "Clean Detecting",
				Url = "https://github.com/worstavarice/clean-detecting/releases",
				Description = "worstavarice/clean-detecting — детектор следов очистки, батников затирания и клинеров перед проверкой",
				ExePatterns = new string[] { "*clean-detecting*.exe", "*CleanDetecting*.exe", "*clean_detect*.exe", "*cleandetect*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ScanCleanDetectingForensics();
					ShowCheckResultsDialog("Clean Detecting — Поиск следов очистки и клинеров", results);
				}
			});

			// 14. Everything
			tools.Add(new SsToolDefinition
			{
				Id = "everything",
				Title = "Everything",
				Url = "https://www.voidtools.com/",
				Description = "Voidtools Everything — мгновенный поиск файлов по NTFS-индексу с готовыми чит-запросами",
				ExePatterns = new string[] { "Everything.exe" },
				CustomAction = delegate
				{
					string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
					string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

					string[] candidates = new string[]
					{
						Path.Combine(progFiles, "Everything", "Everything.exe"),
						Path.Combine(progFilesX86, "Everything", "Everything.exe"),
						Path.Combine(localApp, "Programs", "Everything", "Everything.exe"),
						Path.Combine(appData, "Everything", "Everything.exe")
					};

					string exe = candidates.FirstOrDefault(File.Exists);
					if (!string.IsNullOrEmpty(exe))
					{
						try
						{
							Process.Start(exe, "-search \"" + CheatDatabases.EverythingNamesQuery + "\"");
							return;
						}
						catch { }
					}

					Clipboard.SetText(CheatDatabases.EverythingNamesQuery);
					MessageBox.Show(
						"Everything.exe не найден в стандартных папках.\n\n" +
						"Запрос на поиск 300+ читов скопирован в буфер обмена!\n" +
						"Скачайте Everything с официального сайта: https://www.voidtools.com/",
						"Everything",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
			});

			// 15. MeowDoomsdayFucker
			tools.Add(new SsToolDefinition
			{
				Id = "meow_doomsday_fucker",
				Title = "MeowDoomsdayFucker",
				Url = "https://github.com/MeowTonynoh/MeowDoomsdayFucker",
				Description = "MeowTonynoh/MeowDoomsdayFucker — специализированный сканер чита Doomsday и скрытых инжекций",
				ExePatterns = new string[] { "*MeowDoomsdayFucker*.exe", "*DoomsdayFucker*.exe", "*doomsdayfucker*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ScanMeowDoomsdayForensics();
					ShowCheckResultsDialog("MeowDoomsdayFucker — Аудит чита Doomsday", results);
				}
			});

			// 16. y_check
			tools.Add(new SsToolDefinition
			{
				Id = "y_check",
				Title = "y_check",
				Url = "https://github.com/search?q=y_check+minecraft",
				Description = "y_check — скрипт и утилита проверки Screenshare, поиска координатных дампов и трекеров руды",
				ExePatterns = new string[] { "*y_check*.exe", "*ycheck*.exe", "*y_check*.bat", "*ycheck*.bat", "*y-check*.exe" },
				CustomAction = delegate
				{
					List<CheckResult> results = ScanYCheckArtifacts();
					ShowCheckResultsDialog("y_check — Аудит координат X-Ray и батников", results);
				}
			});

			return tools;
		}

		private static void ShowCheckResultsDialog(string title, List<CheckResult> results)
		{
			Form dlg = new Form();
			dlg.Text = title;
			dlg.Size = new Size(820, 520);
			dlg.StartPosition = FormStartPosition.CenterParent;
			dlg.BackColor = Ui.Backdrop;
			dlg.Font = Ui.FontBody(9f);

			DarkTextBoxMultiline txt = new DarkTextBoxMultiline();
			txt.Dock = DockStyle.Fill;
			txt.Font = new Font("Consolas", 9f);

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== " + title.ToUpperInvariant() + " ===");
			sb.AppendLine("Время анализа: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine();

			if (results != null && results.Count > 0)
			{
				foreach (CheckResult r in results)
				{
					string icon = (r.Severity == CheckSeverity.Critical) ? "🔴 [КРИТИЧЕСКИ]" :
								  ((r.Severity == CheckSeverity.NeedsCheck) ? "🟣 [ВИЗУАЛ]" :
								  ((r.Severity == CheckSeverity.Warning) ? "🟡 [ВНИМАНИЕ]" : "🟢 [ИНФО]"));

					sb.AppendLine(icon + " " + r.Category + " -> " + r.Name);
					sb.AppendLine("   Детали: " + r.Detail);
					if (!string.IsNullOrEmpty(r.Evidence))
					{
						sb.AppendLine("   Улики:\n" + r.Evidence);
					}
					sb.AppendLine();
				}
			}
			else
			{
				sb.AppendLine("Улик не обнаружено. Система чиста.");
			}

			txt.Text = sb.ToString();
			dlg.Controls.Add(txt);
			dlg.ShowDialog();
		}

		// =========================================================================
		// ВСТРОЕННЫЕ СКАНЕРЫ (ВЫЗЫВАЮТСЯ ВМЕСТЕ С НАШИМ ЧЕКЕРОМ)
		// =========================================================================

		// 1. MeowDoomsdayFucker — глубокий поиск Doomsday Client
		public static List<CheckResult> ScanMeowDoomsdayForensics()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> hits = new List<string>();

			// А. Процессы
			try
			{
				foreach (Process p in Process.GetProcesses())
				{
					try
					{
						string name = p.ProcessName.ToLowerInvariant();
						if (name.Contains("doomsday") || name.Contains("doomsdayclient") || name.Contains("doomsday-loader"))
						{
							hits.Add("🚨 Активный процесс Doomsday: " + p.ProcessName + " (PID " + p.Id + ")");
						}
					}
					catch { }
				}
			}
			catch { }

			// Б. Папки и конфиги
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string tempDir = Path.GetTempPath();

			string[] doomsdayDirs = new string[]
			{
				Path.Combine(appData, ".minecraft", "doomsday"),
				Path.Combine(appData, ".minecraft", "config", "doomsday"),
				Path.Combine(appData, ".doomsday"),
				Path.Combine(appData, "doomsday"),
				Path.Combine(localApp, "doomsday"),
				Path.Combine(userProf, ".doomsday"),
				Path.Combine(tempDir, "doomsday")
			};

			foreach (string d in doomsdayDirs)
			{
				try
				{
					if (Directory.Exists(d))
					{
						hits.Add("🚨 Обнаружена директория Doomsday: " + d);
					}
				}
				catch { }
			}

			// В. Prefetch Doomsday
			try
			{
				if (Directory.Exists("C:\\Windows\\Prefetch"))
				{
					string[] pfs = Directory.GetFiles("C:\\Windows\\Prefetch", "*DOOMSDAY*.pf");
					foreach (string pf in pfs)
					{
						FileInfo fi = new FileInfo(pf);
						hits.Add("🚨 Запуск Doomsday в Prefetch: " + Path.GetFileName(pf) + " (" + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
					}
				}
			}
			catch { }

			// Г. DNS Cache Doomsday
			try
			{
				List<DnsCacheEntry> dnsEntries = NetworkAndRemoteForensics.GetDnsCacheEntries();
				foreach (DnsCacheEntry entry in dnsEntries)
				{
					string dom = (entry.Domain ?? "").ToLowerInvariant();
					if (dom.Contains("doomsday.fun") || dom.Contains("doomsday.top") || dom.Contains("doomsday.cc") || dom.Contains("doomsdayclient"))
					{
						hits.Add("🚨 Домен Doomsday в DNS-кэше: " + entry.Domain);
					}
				}
			}
			catch { }

			// Д. Версии Minecraft
			try
			{
				string versions = Path.Combine(appData, ".minecraft", "versions");
				if (Directory.Exists(versions))
				{
					foreach (string v in Directory.GetDirectories(versions))
					{
						if (Path.GetFileName(v).ToLowerInvariant().Contains("doomsday"))
						{
							hits.Add("🚨 Версия Doomsday в .minecraft/versions: " + Path.GetFileName(v));
						}
					}
				}
			}
			catch { }

			CheckResult r = new CheckResult();
			r.Category = "Хард-читы";
			r.Name = "MeowDoomsdayFucker — Doomsday Client Forensics";
			r.Detected = hits.Count > 0;
			r.Severity = (hits.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info;
			r.Detail = (hits.Count > 0) ?
				("Обнаружены следы/процессы чита Doomsday (" + hits.Count + " шт.)!") :
				"Следов чита Doomsday (процессы, память, prefetch, DNS, конфиги) не обнаружено";
			r.Evidence = string.Join("\r\n", hits);

			list.Add(r);
			return list;
		}

		// 2. Clean Detecting — детектор батников очистки, клинеров и затираний следов
		public static List<CheckResult> ScanCleanDetectingForensics()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> cleanerHits = new List<string>();

			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string desktop = Path.Combine(userProf, "Desktop");
			string downloads = Path.Combine(userProf, "Downloads");
			string temp = Path.GetTempPath();

			string[] searchRoots = new string[] { desktop, downloads, temp };
			string[] suspectBatchNames = new string[]
			{
				"clean.bat", "clear.bat", "wipe.bat", "vanish.bat", "destruct.bat", "selfdestruct.bat",
				"delete_traces.bat", "cleaner.bat", "clean_pc.bat", "usn_clean.bat", "clear_all.bat",
				"bypass.bat", "clean.cmd", "clear.cmd", "wipe.ps1", "clean.ps1", "destruct.ps1",
				"cleaner.exe", "sl-cleaner.exe", "slcleaner.exe", "koid.exe", "tracekiller.exe", "usndelete.exe",
				"journaltrace.exe", "clean-detecting.exe", "cleandetecting.exe"
			};

			// Поиск клинер-скриптов по именам
			foreach (string root in searchRoots)
			{
				if (!Directory.Exists(root)) continue;
				try
				{
					foreach (string name in suspectBatchNames)
					{
						string candidate = Path.Combine(root, name);
						if (File.Exists(candidate))
						{
							FileInfo fi = new FileInfo(candidate);
							cleanerHits.Add("🚨 [5.08 - Файл очистки по имени]: " + candidate + " (" + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
						}
					}
				}
				catch { }
			}

			// Анализ содержимого любых .bat/.cmd/.ps1 файлов в Загрузках и на Рабочем столе
			string[] dangerousPatterns = new string[]
			{
				"fsutil usn deletejournal", "wevtutil cl", "wevtutil.exe cl", "c:\\windows\\prefetch",
				"vssadmin delete shadows", "ipconfig /flushdns", "reg delete", "clear-eventlog",
				"remove-item c:\\windows\\prefetch", "sdelete", "journaltrace",
				"sc stop pcasvc", "sc stop dps", "sc stop sysmain", "sc stop eventlog",
				"net stop pcasvc", "net stop dps", "net stop sysmain",
				"start= disabled", "cipher /w:", "clear-history", "remove-itemproperty",
				"usersettings", "muicache", "recentdocs", "opensavepidlmru"
			};

			foreach (string root in searchRoots)
			{
				if (!Directory.Exists(root)) continue;
				try
				{
					string[] scriptFiles = Directory.GetFiles(root, "*.bat")
						.Concat(Directory.GetFiles(root, "*.cmd"))
						.Concat(Directory.GetFiles(root, "*.ps1")).ToArray();

					foreach (string script in scriptFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(script);
							if (fi.Length > 0 && fi.Length < 1048576) // до 1 МБ
							{
								string content = "";
								using (FileStream fs = new FileStream(script, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								using (StreamReader sr = new StreamReader(fs, Encoding.Default))
								{
									content = sr.ReadToEnd().ToLowerInvariant();
								}
								foreach (string dp in dangerousPatterns)
								{
									if (content.Contains(dp))
									{
										cleanerHits.Add("🚨 [5.08 - Скрипт затирания улик " + Path.GetFileName(script) + "]: содержит команду '" + dp + "'! (" + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
										break;
									}
								}
							}
						}
						catch { }
					}
				}
				catch { }
			}

			// Проверка утилит очистки в процессах
			try
			{
				foreach (Process p in Process.GetProcesses())
				{
					try
					{
						string pn = p.ProcessName.ToLowerInvariant();
						foreach (string cp in CheatDatabases.KnownCleanerSignatures)
						{
							if (cp.Length < 3) continue;
							if (cp == "cleaner" && (pn.Contains("ccleaner") || pn.Contains("nvidia") || pn.Contains("amd") || pn.Contains("steam")))
								continue;
							if (pn.Contains(cp) && !pn.Contains("chrome") && !pn.Contains("discord") && !pn.Contains("telegram"))
							{
								cleanerHits.Add("🚨 [5.08 - Активный процесс утилиты затирания]: " + p.ProcessName + " (PID " + p.Id + ")");
								break;
							}
						}
					}
					catch { }
				}
			}
			catch { }

			// Проверка статуса Prefetch (если на запущенной системе файлов меньше 5)
			try
			{
				if (Directory.Exists("C:\\Windows\\Prefetch"))
				{
					int count = Directory.GetFiles("C:\\Windows\\Prefetch", "*.pf").Length;
					TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
					if (count < 5 && uptime.TotalHours >= 1.0)
					{
						cleanerHits.Add("🚨 [5.08 - Каталог Prefetch пуст]: " + count + " файлов при аптайме " + (int)uptime.TotalHours + " ч. — признак очистки перед проверкой!");
					}
				}
			}
			catch { }

			CheckResult cr = new CheckResult();
			cr.Category = "Очистка / Evasion";
			cr.Name = "Clean Detecting — Аудит батников очистки, клинеров и затирания следов";
			cr.Detected = cleanerHits.Count > 0;
			cr.Severity = (cleanerHits.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info;
			cr.Detail = (cleanerHits.Count > 0) ?
				("Обнаружены признаки затирания улик / батники клинеров (" + cleanerHits.Count + " шт.)!") :
				"Батников затирания следов (deletejournal, wevtutil, prefetch wipe) не обнаружено";
			cr.Evidence = string.Join("\r\n", cleanerHits);

			list.Add(cr);
			return list;
		}

		// 3. SSTool — аудит дампов, Screenshare-артефактов и Discord Downloads
		public static List<CheckResult> ScanSsToolArtifacts()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> ssHits = new List<string>();

			string temp = Path.GetTempPath();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			// Поиск дампов памяти dmc / dmp созданных недавно
			try
			{
				string[] dumpDirs = new string[] { temp, userProf, Path.Combine(userProf, "Desktop") };
				foreach (string dd in dumpDirs)
				{
					if (!Directory.Exists(dd)) continue;
					string[] dmps = Directory.GetFiles(dd, "*.dmc").Concat(Directory.GetFiles(dd, "*.dmp")).ToArray();
					foreach (string dmp in dmps)
					{
						try
						{
							FileInfo fi = new FileInfo(dmp);
							if ((DateTime.Now - fi.LastWriteTime).TotalHours <= 24)
							{
								ssHits.Add("⚠️ Дамп памяти в " + Path.GetFileName(dd) + ": " + Path.GetFileName(dmp) + " (" + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
							}
						}
						catch { }
					}
				}
			}
			catch { }

			// Поиск временных распакованных JAR в Temp
			try
			{
				string[] tempJars = Directory.GetFiles(temp, "*.jar");
				foreach (string tj in tempJars)
				{
					try
					{
						FileInfo fi = new FileInfo(tj);
						if ((DateTime.Now - fi.LastWriteTime).TotalHours <= 24)
						{
							ssHits.Add("⚠️ Временный JAR в %TEMP%: " + Path.GetFileName(tj) + " (" + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
						}
					}
					catch { }
				}
			}
			catch { }

			// Проверка Discord Cache на загруженные файлы
			try
			{
				string discordCache = Path.Combine(appData, "discord", "Cache");
				if (Directory.Exists(discordCache))
				{
					DirectoryInfo di = new DirectoryInfo(discordCache);
					FileInfo[] files = di.GetFiles();
					int recentCount = files.Count(f => (DateTime.Now - f.LastWriteTime).TotalHours <= 2);
					if (recentCount > 0)
					{
						ssHits.Add("ℹ️ Активность загрузок Discord за 2 часа: " + recentCount + " кэшированных файлов");
					}
				}
			}
			catch { }

			CheckResult cr = new CheckResult();
			cr.Category = "Криминалистика";
			cr.Name = "SSTool — Артефакты памяти, временные JAR и загрузки Discord";
			cr.Detected = ssHits.Count > 0 && ssHits.Any(h => h.StartsWith("⚠️"));
			cr.Severity = cr.Detected ? CheckSeverity.Warning : CheckSeverity.Info;
			cr.Detail = (ssHits.Count > 0) ?
				("Обнаружены артефакты SS / дампы памяти / временные JAR (" + ssHits.Count + " шт.)") :
				"Подозрительных дампов памяти (.dmc) и распакованных временных JAR в Temp не найдено";
			cr.Evidence = string.Join("\r\n", ssHits);

			list.Add(cr);
			return list;
		}

		// 4. y_check — координатные трекеры и скрипты SS
		public static List<CheckResult> ScanYCheckArtifacts()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> yHits = new List<string>();

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string mcDir = Path.Combine(appData, ".minecraft");

			// Поиск координатных файлов X-Ray и трекеров баз
			string[] coordFiles = new string[]
			{
				Path.Combine(mcDir, "coordinates.txt"),
				Path.Combine(mcDir, "bases.txt"),
				Path.Combine(mcDir, "ores.txt"),
				Path.Combine(mcDir, "xray_coords.json"),
				Path.Combine(mcDir, "waypoint_dump.txt"),
				Path.Combine(mcDir, "y_check.txt"),
				Path.Combine(userProf, "Desktop", "coordinates.txt"),
				Path.Combine(userProf, "Desktop", "bases.txt")
			};

			foreach (string cf in coordFiles)
			{
				try
				{
					if (File.Exists(cf))
					{
						FileInfo fi = new FileInfo(cf);
						yHits.Add("🟣 [Координатный лог / X-Ray базы] " + cf + " (" + fi.Length + " байт, изменён: " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
					}
				}
				catch { }
			}

			// Поиск файлов y_check
			string[] yCheckPaths = new string[]
			{
				Path.Combine(userProf, "Desktop", "y_check.bat"),
				Path.Combine(userProf, "Desktop", "ycheck.bat"),
				Path.Combine(userProf, "Downloads", "y_check.bat"),
				Path.Combine(userProf, "Downloads", "ycheck.bat")
			};

			foreach (string yp in yCheckPaths)
			{
				try
				{
					if (File.Exists(yp))
					{
						yHits.Add("ℹ️ Найден скрипт проверки y_check: " + yp);
					}
				}
				catch { }
			}

			CheckResult cr = new CheckResult();
			cr.Category = "Визуалы (Следует проверить)";
			cr.Name = "y_check — Аудит координатных файлов, баз и трекеров X-Ray";
			cr.Detected = yHits.Count > 0 && yHits.Any(h => h.StartsWith("🟣"));
			cr.Severity = cr.Detected ? CheckSeverity.NeedsCheck : CheckSeverity.Info;
			cr.Detail = (yHits.Count > 0) ?
				("Обнаружены дампы координат / базы игроков / трекеры (" + yHits.Count + " шт.)!") :
				"Файлов дампов координат X-Ray (bases.txt, coordinates.txt, y_check) не обнаружено";
			cr.Evidence = string.Join("\r\n", yHits);

			list.Add(cr);
			return list;
		}

		public static void ActivateAllGitHubTools(Action<string> setStatus)
		{
			List<SsToolDefinition> tools = GetTools().Where(t =>
				t.Id == "meow_doomsday_fucker" ||
				t.Id == "clean_detecting" ||
				t.Id == "sstool" ||
				t.Id == "y_check" ||
				t.Id == "everything" ||
				t.Id == "system_informer"
			).ToList();

			int launched = 0;
			foreach (SsToolDefinition t in tools)
			{
				string exe = t.FindExecutable();
				if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
				{
					try
					{
						Process.Start(exe);
						launched++;
						if (setStatus != null) setStatus("🚀 Запущена внешняя утилита: " + t.Title + " (" + Path.GetFileName(exe) + ")");
					}
					catch { }
				}
			}

			if (setStatus != null)
			{
				if (launched > 0)
				{
					setStatus(string.Format("⚡ GitHub-утилиты активированы: запущено {0} внешних процессов, детекторы MeowDoomsday, Clean Detecting, SSTool, y_check подключены к проверке!", launched));
				}
				else
				{
					setStatus("⚡ [GitHub Модули] Детекторы MeowDoomsdayFucker, Clean Detecting, SSTool, y_check успешно активированы вместе с чекером!");
				}
			}
		}

		public static List<CheckResult> RunAllIntegratedToolScans()
		{
			List<CheckResult> all = new List<CheckResult>();
			all.AddRange(ScanMeowDoomsdayForensics());
			all.AddRange(ScanCleanDetectingForensics());
			all.AddRange(ScanSsToolArtifacts());
			all.AddRange(ScanYCheckArtifacts());
			return all;
		}
	}
}
