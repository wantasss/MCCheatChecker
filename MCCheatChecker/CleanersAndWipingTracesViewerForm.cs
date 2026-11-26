using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class CleanersAndWipingTracesViewerForm : Form
	{
		#region Data Models

		public enum CleanerCategory
		{
			All,
			ViolationsOnly,
			BatchScripts,
			CleanerApps,
			SystemAnomalies
		}

		public class CleanerEvidenceItem
		{
			public int Index;
			public string TargetName;
			public string ArtifactType;
			public CleanerCategory Category;
			public string Verdict;
			public string TargetRule;
			public string Details;
			public string PathOrSource;
			public DateTime DetectionTime;
			public bool IsViolation;
			public Color RowColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private CleanerCategory currentFilter = CleanerCategory.All;
		private List<CleanerEvidenceItem> allItems = new List<CleanerEvidenceItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiScripts;
		private Label lblKpiApps;
		private Label lblKpiAnomalies;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterScripts;
		private DarkButton btnFilterApps;
		private DarkButton btnFilterAnomalies;

		private DarkButton btnOpenFile;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyDetails;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public CleanersAndWipingTracesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "🧹 Детектор клинеров, батников очистки и улик сокрытия (Правило 5.08) — MCCheatChecker";
			Size = new Size(1240, 760);
			MinimumSize = new Size(1000, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🧹 Детектор клинеров, батников очистки и улик сокрытия (Правило 5.08)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Аудит попыток сокрытия улик: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1200, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotal = CreateKpiLabel("Всего проверок: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик сокрытия (5.08): 0", Ui.AccentRed, 180, kpiPanel);
			lblKpiScripts = CreateKpiLabel("Батников / скриптов: 0", Ui.AccentOrange, 400, kpiPanel);
			lblKpiApps = CreateKpiLabel("Программ-клинеров: 0", Ui.AccentTeal, 620, kpiPanel);
			lblKpiAnomalies = CreateKpiLabel("Аномалий журналов: 0", Ui.AccentPurple, 830, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все улики", CleanerCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Только нарушения (5.08)", CleanerCategory.ViolationsOnly, 126, filterPanel);
			btnFilterScripts = CreateFilterButton("📜 Батники и скрипты (.bat)", CleanerCategory.BatchScripts, 330, filterPanel);
			btnFilterApps = CreateFilterButton("🛠️ Программы-клинеры", CleanerCategory.CleanerApps, 544, filterPanel);
			btnFilterAnomalies = CreateFilterButton("⚡ Системные аномалии", CleanerCategory.SystemAnomalies, 738, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(940, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(995, 9);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			Controls.Add(filterPanel);

			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
			split.BackColor = Ui.Backdrop;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Объект / Вектор", 220);
			listView.Columns.Add("Тип артефакта", 140);
			listView.Columns.Add("Вердикт", 220);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Время обнаружения", 140);
			listView.Columns.Add("Путь / Источник", 360);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenFileLocation(); };
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Детализированный отчет об обнаруженном клинере или аномалии очистки:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(12, 30);
			txtDetails.Size = new Size(860, 130);
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			bottomContainer.Controls.Add(txtDetails);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(885, 20);
			actionsPanel.Size = new Size(330, 145);

			btnOpenFile = CreateActionButton("📄 Просмотреть файл / скрипт", 0, actionsPanel);
			btnOpenFile.Click += delegate { OpenSelectedFile(); };

			btnOpenFolder = CreateActionButton("📂 Открыть папку в Проводнике", 32, actionsPanel);
			btnOpenFolder.Click += delegate { OpenFileLocation(); };

			btnCopyDetails = CreateActionButton("📋 Копировать детали доказательства", 64, actionsPanel);
			btnCopyDetails.Click += delegate { CopyDetailsText(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 96, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана (5.08)", 128, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			bottomContainer.Controls.Add(actionsPanel);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(12, 166);
			btnRescan.Size = new Size(160, 26);
			btnRescan.Click += delegate { StartScan(); };
			bottomContainer.Controls.Add(btnRescan);

			split.Panel2.Controls.Add(bottomContainer);
			Controls.Add(split);

			filterPanel.BringToFront();
			split.BringToFront();
		}

		private Label CreateKpiLabel(string text, Color color, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.Font = Ui.FontBody(9f, true);
			lbl.ForeColor = color;
			lbl.BackColor = Ui.SurfaceAlt;
			lbl.AutoSize = true;
			lbl.Padding = new Padding(8, 4, 8, 4);
			lbl.Location = new Point(x, 4);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, CleanerCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == CleanerCategory.ViolationsOnly ? 198 : (cat == CleanerCategory.BatchScripts ? 208 : (cat == CleanerCategory.CleanerApps ? 188 : (cat == CleanerCategory.SystemAnomalies ? 188 : 108))), 28);
			btn.Font = Ui.FontBody(8.5f, true);
			btn.Click += delegate
			{
				currentFilter = cat;
				ResetFilterButtonColors();
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			if (cat == CleanerCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterScripts, btnFilterApps, btnFilterAnomalies };
			foreach (DarkButton b in btns)
			{
				b.BackColor = Ui.Surface;
				b.ForeColor = Ui.TextPrimary;
			}
		}

		private DarkButton CreateActionButton(string text, int y, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(0, y);
			btn.Size = new Size(320, 26);
			btn.Font = Ui.FontBody(8f, true);
			parent.Controls.Add(btn);
			return btn;
		}

		#region Scan Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Глубокий аудит следов зачистки, скриптов и клинеров (5.08)...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<CleanerEvidenceItem> list = new List<CleanerEvidenceItem>();
			int idx = 1;

			// Vector 1: Scan for Batch/Script cleaners (.bat, .cmd, .ps1, .vbs)
			ScanBatchScripts(list, ref idx);

			// Vector 2: Scan for known cleaner applications & binaries
			ScanCleanerApplications(list, ref idx);

			// Vector 3: System Journal & Forensic Wipe Indicators (Prefetch wipe, EventLog clear, USN recreation)
			ScanSystemWipeIndicators(list, ref idx);

			e.Result = list;
		}

		private void ScanBatchScripts(List<CleanerEvidenceItem> list, ref int idx)
		{
			List<string> targetDirs = new List<string>();

			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				string downloads = Path.Combine(userProfile, "Downloads");
				string temp = Path.GetTempPath();
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcDir = Path.Combine(appData, ".minecraft");

				if (Directory.Exists(desktop)) targetDirs.Add(desktop);
				if (Directory.Exists(downloads)) targetDirs.Add(downloads);
				if (Directory.Exists(temp)) targetDirs.Add(temp);
				if (Directory.Exists(mcDir)) targetDirs.Add(mcDir);
			}
			catch { }

			string[] scriptExtensions = new string[] { "*.bat", "*.cmd", "*.ps1", "*.vbs" };
			string[] dangerousCommands = new string[]
			{
				"fsutil usn deletejournal",
				"wevtutil cl",
				"del /f /q %temp%",
				"rmdir /s /q %temp%",
				"del /f /q c:\\windows\\prefetch",
				"vssadmin delete shadows",
				"clear-eventlog",
				"remove-itemproperty",
				"set-mppreference -disablerealtimemonitoring",
				"bcdedit /set testsigning",
				"fltmc unload",
				"net stop dps",
				"sc stop dps",
				"dps.exe",
				"usnjrnl"
			};

			foreach (string dir in targetDirs)
			{
				try
				{
					foreach (string ext in scriptExtensions)
					{
						IEnumerable<string> files = Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly);
						foreach (string file in files)
						{
							try
							{
								string content = File.ReadAllText(file);
								string lower = content.ToLowerInvariant();

								List<string> matchedCmds = new List<string>();
								foreach (string cmd in dangerousCommands)
								{
									if (lower.Contains(cmd))
									{
										matchedCmds.Add(cmd);
									}
								}

								string fName = Path.GetFileName(file).ToLowerInvariant();
								bool nameSuspicious = fName.Contains("clean") || fName.Contains("bypass") || fName.Contains("ss") || fName.Contains("clear");

								if (matchedCmds.Count > 0 || nameSuspicious)
								{
									CleanerEvidenceItem it = new CleanerEvidenceItem();
									it.Index = idx++;
									it.TargetName = Path.GetFileName(file);
									it.ArtifactType = "Скрипт очистки (" + Path.GetExtension(file) + ")";
									it.Category = CleanerCategory.BatchScripts;
									it.IsViolation = true;
									it.Verdict = "🚨 СКРИПТ ЗАЧИСТКИ СЛЕДОВ (5.08)";
									it.TargetRule = "5.08 (Сокрытие улик)";
									it.PathOrSource = file;
									it.DetectionTime = File.GetLastWriteTime(file);
									it.RowColor = Ui.AccentRed;

									StringBuilder sb = new StringBuilder();
									sb.AppendLine("Обнаружен скрипт с командами зачистки криминалистических артефактов Windows:");
									sb.AppendLine("Файл: " + file);
									sb.AppendLine("Размер: " + new FileInfo(file).Length + " байт");
									sb.AppendLine("Обнаруженные опасные команды:");
									foreach (string c in matchedCmds)
									{
										sb.AppendLine("  • " + c);
									}
									sb.AppendLine("\nФрагмент кода скрипта:\n" + (content.Length > 500 ? content.Substring(0, 500) + "\n..." : content));

									it.Details = sb.ToString();
									list.Add(it);
								}
							}
							catch { }
						}
					}
				}
				catch { }
			}
		}

		private void ScanCleanerApplications(List<CleanerEvidenceItem> list, ref int idx)
		{
			string[] knownCleaners = new string[]
			{
				"journaltrace", "usnjrnl", "bypassss", "cleanss", "echocleaner", "slapper",
				"redline", "dmccleaner", "mangocleaner", "koidcleaner", "everythingcleaner",
				"ccleaner", "bleachbit", "privazer", "regorganizer", "glaryutilities",
				"wisediskcleaner", "cleanmaster", "shellbagsview", "usbdeview"
			};

			// Scan Prefetch for cleaner launches
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(pfDir))
				{
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					foreach (string pf in pfFiles)
					{
						string pfName = Path.GetFileName(pf).ToLowerInvariant();
						foreach (string cl in knownCleaners)
						{
							if (pfName.Contains(cl))
							{
								CleanerEvidenceItem it = new CleanerEvidenceItem();
								it.Index = idx++;
								it.TargetName = Path.GetFileName(pf);
								it.ArtifactType = "Запуск клинера (Prefetch)";
								it.Category = CleanerCategory.CleanerApps;
								it.IsViolation = true;
								it.Verdict = "🚨 ЗАПУСК УТИЛИТЫ-КЛИНЕРА (5.08)";
								it.TargetRule = "5.08 (Сокрытие улик)";
								it.PathOrSource = pf;
								it.DetectionTime = File.GetLastWriteTime(pf);
								it.RowColor = Ui.AccentRed;
								it.Details = string.Format("Зафиксирован запуск программы для зачистки следов [{0}] в кэше Prefetch.\nВремя последнего запуска: {1}.\nИспользование клинеров перед ScreenShare запрещено пунктом 5.08.", cl, it.DetectionTime);
								list.Add(it);
								break;
							}
						}
					}
				}
			}
			catch { }

			// Scan running processes
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					try
					{
						string pName = p.ProcessName.ToLowerInvariant();
						foreach (string cl in knownCleaners)
						{
							if (pName.Contains(cl))
							{
								CleanerEvidenceItem it = new CleanerEvidenceItem();
								it.Index = idx++;
								it.TargetName = p.ProcessName + ".exe (PID: " + p.Id + ")";
								it.ArtifactType = "Активный процесс клинера";
								it.Category = CleanerCategory.CleanerApps;
								it.IsViolation = true;
								it.Verdict = "🚨 АКТИВНЫЙ ПРОЦЕСС КЛИНЕРА (5.08)";
								it.TargetRule = "5.08 (Сокрытие улик)";
								it.PathOrSource = p.ProcessName;
								it.DetectionTime = DateTime.Now;
								it.RowColor = Ui.AccentRed;
								it.Details = string.Format("Обнаружен запущенный процесс специализированной утилиты сокрытия / очистки следов: {0}.\nPID: {1}.", p.ProcessName, p.Id);
								list.Add(it);
								break;
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanSystemWipeIndicators(List<CleanerEvidenceItem> list, ref int idx)
		{
			// Indicator 1: Prefetch Directory Wipe check
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(pfDir))
				{
					string[] files = Directory.GetFiles(pfDir, "*.pf");
					long uptimeMinutes = Environment.TickCount / (1000 * 60);

					if (files.Length < 10 && uptimeMinutes > 30)
					{
						CleanerEvidenceItem it = new CleanerEvidenceItem();
						it.Index = idx++;
						it.TargetName = "Директория Windows Prefetch";
						it.ArtifactType = "Очистка системного кэша";
						it.Category = CleanerCategory.SystemAnomalies;
						it.IsViolation = true;
						it.Verdict = "🚨 ОЧИСТКА ПАПКИ PREFETCH (5.08)";
						it.TargetRule = "5.08 (Сокрытие улик)";
						it.PathOrSource = pfDir;
						it.DetectionTime = DateTime.Now;
						it.RowColor = Ui.AccentRed;
						it.Details = string.Format("Папка Prefetch содержит всего {0} файлов при непрерывной работе системы {1} мин.\nЭто прямое свидетельство выполнения команды 'del /q C:\\Windows\\Prefetch\\*'.", files.Length, uptimeMinutes);
						list.Add(it);
					}
				}
			}
			catch { }

			// Indicator 2: Windows Event Log cleared (Event ID 1102 / 104)
			try
			{
				string queryStr = "*[System[(EventID=1102 or EventID=104)]]";
				EventLogQuery query = new EventLogQuery("Security", PathType.LogName, queryStr);
				using (EventLogReader reader = new EventLogReader(query))
				{
					EventRecord rec;
					while ((rec = reader.ReadEvent()) != null)
					{
						using (rec)
						{
							CleanerEvidenceItem it = new CleanerEvidenceItem();
							it.Index = idx++;
							it.TargetName = "Security Event Log (Event ID 1102)";
							it.ArtifactType = "Очистка журнала событий";
							it.Category = CleanerCategory.SystemAnomalies;
							it.IsViolation = true;
							it.Verdict = "🚨 ОЧИСТКА ЖУРНАЛА СОБЫТИЙ WEVTUTIL (5.08)";
							it.TargetRule = "5.08 (Сокрытие улик)";
							it.PathOrSource = "Security / Event ID 1102";
							it.DetectionTime = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.Details = string.Format("Зафиксирован факт принудительной очистки журнала аудита безопасности Windows.\nВремя очистки: {0}.\nВыполнение команды 'wevtutil cl' является критическим нарушением регламента 5.08.", it.DetectionTime);
							list.Add(it);
						}
					}
				}
			}
			catch { }

			// Indicator 3: RunMRU wiped check
			try
			{
				using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"))
				{
					if (rk != null)
					{
						string[] valNames = rk.GetValueNames();
						if (valNames.Length <= 1)
						{
							CleanerEvidenceItem it = new CleanerEvidenceItem();
							it.Index = idx++;
							it.TargetName = "История диалога 'Выполнить' (RunMRU)";
							it.ArtifactType = "Очистка реестра";
							it.Category = CleanerCategory.SystemAnomalies;
							it.IsViolation = false;
							it.Verdict = "⚠️ История RunMRU пуста (Возможна очистка)";
							it.TargetRule = "Аудит реестра";
							it.PathOrSource = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
							it.DetectionTime = DateTime.Now;
							it.RowColor = Ui.AccentOrange;
							it.Details = "В ключе реестра RunMRU отсутствуют записи выполненных команд, что может указывать на недавнюю зачистку реестра.";
							list.Add(it);
						}
					}
				}
			}
			catch { }
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<CleanerEvidenceItem> res = e.Result as List<CleanerEvidenceItem>;
			if (res != null)
			{
				allItems = res;

				int violations = 0;
				int scripts = 0;
				int apps = 0;
				int anomalies = 0;

				foreach (CleanerEvidenceItem it in allItems)
				{
					if (it.IsViolation) violations++;
					if (it.Category == CleanerCategory.BatchScripts) scripts++;
					if (it.Category == CleanerCategory.CleanerApps) apps++;
					if (it.Category == CleanerCategory.SystemAnomalies) anomalies++;
				}

				lblKpiTotal.Text = "Всего проверок: " + allItems.Count;
				lblKpiViolations.Text = "Улик сокрытия (5.08): " + violations;
				lblKpiScripts.Text = "Батников / скриптов: " + scripts;
				lblKpiApps.Text = "Программ-клинеров: " + apps;
				lblKpiAnomalies.Text = "Аномалий журналов: " + anomalies;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 КРИТИЧЕСКОЕ НАРУШЕНИЕ: Обнаружено {0} улик преднамеренного сокрытия следов (5.08)!", violations);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ Аудит сокрытия завершен: клинеров, батников очистки и сброса журналов не выявлено.";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & UI Updates

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : string.Empty;

			foreach (CleanerEvidenceItem it in allItems)
			{
				if (currentFilter == CleanerCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == CleanerCategory.BatchScripts && it.Category != CleanerCategory.BatchScripts) continue;
				if (currentFilter == CleanerCategory.CleanerApps && it.Category != CleanerCategory.CleanerApps) continue;
				if (currentFilter == CleanerCategory.SystemAnomalies && it.Category != CleanerCategory.SystemAnomalies) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.TargetName.ToLowerInvariant().Contains(search) ||
						it.ArtifactType.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						it.PathOrSource.ToLowerInvariant().Contains(search) ||
						(it.Details != null && it.Details.ToLowerInvariant().Contains(search));

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.TargetName);
				lvi.SubItems.Add(it.ArtifactType);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.DetectionTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.PathOrSource);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtDetails.Text = "Нет записей, соответствующих выбранному фильтру.";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АКТ ФИКСАЦИИ УЛИКИ СОКРЫТИЯ (ПРАВИЛО 5.08) ===");
			sb.AppendLine("Объект: " + it.TargetName);
			sb.AppendLine("Тип артефакта: " + it.ArtifactType);
			sb.AppendLine("Вердикт проверки: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Время фиксации: " + it.DetectionTime.ToString("yyyy-MM-dd HH:mm:ss (МСК)"));
			sb.AppendLine("Источник / Путь: " + it.PathOrSource);
			sb.AppendLine();
			sb.AppendLine("=== ДОКАЗАТЕЛЬНАЯ БАЗА ===");
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions

		private void OpenFileLocation()
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null) return;

			if (File.Exists(it.PathOrSource))
			{
				try
				{
					Process.Start("explorer.exe", "/select,\"" + it.PathOrSource + "\"");
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				MessageBox.Show("Объект является системным событием или файл не существует:\n" + it.PathOrSource, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void OpenSelectedFile()
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null || !File.Exists(it.PathOrSource)) return;

			try
			{
				Process.Start("notepad.exe", it.PathOrSource);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyDetailsText()
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null) return;

			Clipboard.SetText(it.Details);
			MessageBox.Show("Детали доказательства скопированы в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ СОКРЫТИЯ УЛИК (ПРАВИЛО 5.08) ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Объект: " + it.TargetName);
			sb.AppendLine("Тип: " + it.ArtifactType);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт: " + it.TargetRule);
			sb.AppendLine("Источник: " + it.PathOrSource);
			sb.AppendLine("Время: " + it.DetectionTime.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			CleanerEvidenceItem it = listView.SelectedItems[0].Tag as CleanerEvidenceItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик ({1}: {2})", targetPlayerName, it.ArtifactType, it.TargetName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
