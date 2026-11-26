using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class JvmCrashLogsAndHotspotDumpsViewerForm : Form
	{
		#region Enums & Models

		public enum JvmCrashCategory
		{
			CheatDllCrash501,
			SuspiciousJvmArgs501,
			TempLibraryInJvm501,
			RecentJvmCrash508,
			StandardJvmCrash
		}

		public enum JvmCrashFilter
		{
			All,
			ViolationsOnly,
			CheatDllCrashes,
			SuspiciousArgs,
			TempModules,
			StandardCrashes
		}

		public class JvmCrashLogItem
		{
			public int Index;
			public JvmCrashCategory Category;
			public string CategoryName;
			public string FileName;
			public string FilePath;
			public string ProblematicFrame;
			public string CrashTime;
			public DateTime FileTimestamp;
			public string VmArguments;
			public List<string> DetectedCheatDlls;
			public List<string> TempDlls;
			public List<string> AllDynamicLibraries;
			public string CrashSignal;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsCheatCrash;
			public bool IsSuspiciousArgs;
			public bool IsTempDll;
			public Color RowColor;
			public string ForensicReport;
			public string DynamicLibrariesReport;
			public string TechnicalCrashContext;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private JvmCrashFilter currentFilter = JvmCrashFilter.All;
		private List<JvmCrashLogItem> allItems = new List<JvmCrashLogItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtDynamicLibraries;
		private DarkTextBoxMultiline txtTechnicalContext;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiCheatCrashes;
		private Label lblKpiSuspiciousArgs;
		private Label lblKpiTempModules;
		private Label lblKpiStandard;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCheatCrashes;
		private DarkButton btnFilterArgs;
		private DarkButton btnFilterTemp;
		private DarkButton btnFilterStandard;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public JvmCrashLogsAndHotspotDumpsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🔥 Инспектор аварийных логов JVM HotSpot (hs_err_pid) и дампов сбоев — MCCheatChecker";
			Size = new Size(1340, 820);
			MinimumSize = new Size(1100, 640);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;

			// Header Panel
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔥 ИНСПЕКТОР АВАРИЙНЫХ ЛОГОВ JVM HOTSPOT (HS_ERR_PID) И СБОЕВ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит файлов hs_err_pid*.log: выявление сбоев от инъекций читов, проблемных фреймов, скрытых DLL в Dynamic Libraries и аргументов -javaagent (5.01 / 5.08)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ ПОИСК ДАМПОВ JVM HOTSPOT...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(250, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Container
			Panel topContainer = new Panel();
			topContainer.Dock = DockStyle.Top;
			topContainer.Height = 138;
			topContainer.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("🔥 Всего сбоев JVM", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01/5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Сбоев от чит-библиотек", "0", Ui.AccentRed, ref kpiX, out lblKpiCheatCrashes));
			kpiPanel.Controls.Add(CreateKpiBadge("⚡ Чит-аргументов (-javaagent)", "0", Ui.AccentOrange, ref kpiX, out lblKpiSuspiciousArgs));
			kpiPanel.Controls.Add(CreateKpiBadge("📁 Модулей из Temp в памяти", "0", Ui.AccentOrange, ref kpiX, out lblKpiTempModules));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Штатных сбоев JVM", "0", Ui.AccentGreen, ref kpiX, out lblKpiStandard));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все сбои", JvmCrashFilter.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", JvmCrashFilter.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterCheatCrashes = CreateFilterButton("💉 Чит-библиотеки", JvmCrashFilter.CheatDllCrashes, ref fBtnX, filterBar);
			btnFilterArgs = CreateFilterButton("⚡ Чит-аргументы", JvmCrashFilter.SuspiciousArgs, ref fBtnX, filterBar);
			btnFilterTemp = CreateFilterButton("📁 Модули из Temp", JvmCrashFilter.TempModules, ref fBtnX, filterBar);
			btnFilterStandard = CreateFilterButton("✔️ Штатные сбои", JvmCrashFilter.StandardCrashes, ref fBtnX, filterBar);

			// Search Box
			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(fBtnX + 16, 13);
			filterBar.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(fBtnX + 78, 10);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Controls.Add(filterBar);

			// SplitContainer
			SplitContainer splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 5;
			splitContainer.BackColor = Ui.Border;

			// DarkListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;

			listView.Columns.Add("№", 45, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 155, HorizontalAlignment.Left);
			listView.Columns.Add("Файл дампа (hs_err_pid)", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Проблемный фрейм (Crash Frame)", 240, HorizontalAlignment.Left);
			listView.Columns.Add("Найденные чит-модули / Аргументы", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 260, HorizontalAlignment.Left);
			listView.Columns.Add("Время сбоя", 140, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate { UpdateDetailViews(); };
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Container
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(12, 7, 12, 7);

			btnCopySelected = new DarkButton();
			btnCopySelected.Text = "📋 Копировать запись";
			btnCopySelected.Size = new Size(160, 28);
			btnCopySelected.Location = new Point(12, 8);
			btnCopySelected.Click += delegate { CopySelectedRow(); };
			actionBar.Controls.Add(btnCopySelected);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Команда бана (/ban)";
			btnCopyBanCmd.Size = new Size(185, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(566, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(724, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			btnClose = new DarkButton();
			btnClose.Text = "✖️ Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(887, 8);
			btnClose.Click += delegate { Close(); };
			actionBar.Controls.Add(btnClose);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Регламент");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabLibs = new TabPage("🔍 Загруженные Dynamic Libraries в момент сбоя");
			tabLibs.BackColor = Ui.Surface;
			txtDynamicLibraries = new DarkTextBoxMultiline();
			txtDynamicLibraries.Dock = DockStyle.Fill;
			txtDynamicLibraries.ReadOnly = true;
			txtDynamicLibraries.Font = Ui.FontMono(9f);
			tabLibs.Controls.Add(txtDynamicLibraries);
			detailTabs.TabPages.Add(tabLibs);

			TabPage tabContext = new TabPage("⚡ Проблемный стек и аргументы JVM (VM Arguments)");
			tabContext.BackColor = Ui.Surface;
			txtTechnicalContext = new DarkTextBoxMultiline();
			txtTechnicalContext.Dock = DockStyle.Fill;
			txtTechnicalContext.ReadOnly = true;
			txtTechnicalContext.Font = Ui.FontMono(9f);
			tabContext.Controls.Add(txtTechnicalContext);
			detailTabs.TabPages.Add(tabContext);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(170, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.SurfaceAlt;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(164, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(164, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 178;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, JvmCrashFilter cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8f);
			btn.Size = new Size(150, 28);
			btn.Location = new Point(curX, 8);
			btn.Click += delegate
			{
				currentFilter = cat;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.BackColor = (currentFilter == JvmCrashFilter.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == JvmCrashFilter.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == JvmCrashFilter.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == JvmCrashFilter.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterCheatCrashes.BackColor = (currentFilter == JvmCrashFilter.CheatDllCrashes) ? Ui.AccentRed : Ui.Surface;
			btnFilterCheatCrashes.ForeColor = (currentFilter == JvmCrashFilter.CheatDllCrashes) ? Color.White : Ui.TextPrimary;

			btnFilterArgs.BackColor = (currentFilter == JvmCrashFilter.SuspiciousArgs) ? Ui.AccentOrange : Ui.Surface;
			btnFilterArgs.ForeColor = (currentFilter == JvmCrashFilter.SuspiciousArgs) ? Color.Black : Ui.TextPrimary;

			btnFilterTemp.BackColor = (currentFilter == JvmCrashFilter.TempModules) ? Ui.AccentOrange : Ui.Surface;
			btnFilterTemp.ForeColor = (currentFilter == JvmCrashFilter.TempModules) ? Color.Black : Ui.TextPrimary;

			btnFilterStandard.BackColor = (currentFilter == JvmCrashFilter.StandardCrashes) ? Ui.AccentGreen : Ui.Surface;
			btnFilterStandard.ForeColor = (currentFilter == JvmCrashFilter.StandardCrashes) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ ПОИСК ДАМПОВ JVM HOTSPOT...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<JvmCrashLogItem> results = new List<JvmCrashLogItem>();
			int itemIndex = 1;

			List<string> searchDirs = new List<string>();

			try
			{
				string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				if (!string.IsNullOrEmpty(userProf) && Directory.Exists(userProf) && !searchDirs.Contains(userProf))
					searchDirs.Add(userProf);

				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop) && !searchDirs.Contains(desktop))
					searchDirs.Add(desktop);

				string tempDir = Path.GetTempPath();
				if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir) && !searchDirs.Contains(tempDir))
					searchDirs.Add(tempDir);

				foreach (string root in ManualChecks.MinecraftRoots())
				{
					if (Directory.Exists(root) && !searchDirs.Contains(root))
						searchDirs.Add(root);

					string instances = Path.Combine(root, "instances");
					if (Directory.Exists(instances))
					{
						try
						{
							string[] instDirs = Directory.GetDirectories(instances);
							foreach (string id in instDirs)
							{
								if (!searchDirs.Contains(id)) searchDirs.Add(id);
								string subMc = Path.Combine(id, ".minecraft");
								if (Directory.Exists(subMc) && !searchDirs.Contains(subMc)) searchDirs.Add(subMc);
							}
						}
						catch { }
					}
				}
			}
			catch { }

			List<string> candidateFiles = new List<string>();

			foreach (string d in searchDirs)
			{
				try
				{
					if (!Directory.Exists(d)) continue;
					string[] logs = Directory.GetFiles(d, "hs_err_pid*.log", SearchOption.TopDirectoryOnly);
					foreach (string log in logs)
					{
						if (!candidateFiles.Contains(log)) candidateFiles.Add(log);
					}
				}
				catch { }
			}

			foreach (string logPath in candidateFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(logPath);
					if (fi.Length > 30 * 1024 * 1024) continue;

					string[] lines;
					using (FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.Default))
					{
						string content = sr.ReadToEnd();
						lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					}

					string crashTime = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
					string problematicFrame = "N/A";
					string crashSignal = "Не указан";
					string vmArgs = "N/A";
					List<string> cheatDlls = new List<string>();
					List<string> tempDlls = new List<string>();
					List<string> allDynamicLibs = new List<string>();
					bool parsingDynamicLibs = false;

					foreach (string rawLine in lines)
					{
						string line = rawLine.Trim();

						if (line.StartsWith("Time:") && line.Length > 6)
						{
							crashTime = line.Substring(6).Trim();
						}
						else if (line.StartsWith("# Problematic frame:"))
						{
							problematicFrame = line.Substring(20).Trim();
						}
						else if (line.StartsWith("# A fatal error has been detected by the Java Runtime Environment:"))
						{
							crashSignal = line.Substring(67).Trim();
						}
						else if (line.StartsWith("VM Arguments:"))
						{
							vmArgs = line.Substring(13).Trim();
						}
						else if (line.StartsWith("Dynamic libraries:"))
						{
							parsingDynamicLibs = true;
						}
						else if (parsingDynamicLibs)
						{
							if (line.StartsWith("VM Arguments:") || line.StartsWith("VM Flags:") ||
								line.StartsWith("Registers:") || line.StartsWith("Stack:") || line.Length == 0)
							{
								parsingDynamicLibs = false;
								continue;
							}

							allDynamicLibs.Add(line);
							string lowerLib = line.ToLowerInvariant();

							// Match cheat signatures
							string[] extraCheats = CheatDatabases.ExtraCheatClients;
							foreach (string ch in extraCheats)
							{
								if (ch.Length >= 4 && lowerLib.Contains(ch.ToLowerInvariant()))
								{
									if (!cheatDlls.Contains(line)) cheatDlls.Add(line);
									break;
								}
							}

							// Check Temp DLL
							if ((lowerLib.Contains("\\appdata\\local\\temp\\") || lowerLib.Contains("\\temp\\")) &&
								lowerLib.EndsWith(".dll") &&
								!lowerLib.Contains("jna") && !lowerLib.Contains("jinput") &&
								!lowerLib.Contains("lwjgl") && !lowerLib.Contains("openal"))
							{
								if (!tempDlls.Contains(line)) tempDlls.Add(line);
							}
						}
					}

					// Analyze Problematic Frame for Cheats
					bool isCheatCrash = false;
					string frameLower = problematicFrame.ToLowerInvariant();
					foreach (string ch in CheatDatabases.ExtraCheatClients)
					{
						if (ch.Length >= 4 && frameLower.Contains(ch.ToLowerInvariant()))
						{
							isCheatCrash = true;
							break;
						}
					}

					// Analyze VM Args
					bool isSuspiciousArgs = false;
					string argsLower = vmArgs.ToLowerInvariant();
					if (argsLower.Contains("-javaagent:") || argsLower.Contains("-noverify") || argsLower.Contains("bootclasspath"))
					{
						isSuspiciousArgs = true;
					}

					bool isTempDll = tempDlls.Count > 0;
					bool hasCheatDll = cheatDlls.Count > 0;

					JvmCrashCategory cat;
					string rule;
					string verdict;
					Color rowCol;
					bool isV = false;

					if (isCheatCrash || hasCheatDll)
					{
						cat = JvmCrashCategory.CheatDllCrash501;
						rule = "5.01 Использование читов";
						verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ: Сбой JVM вызван инъекцией чита или найден чит-модуль!";
						rowCol = Ui.AccentRed;
						isV = true;
					}
					else if (isSuspiciousArgs)
					{
						cat = JvmCrashCategory.SuspiciousJvmArgs501;
						rule = "5.01 / 5.08";
						verdict = "🚨 5.01 ПОДОЗРИТЕЛЬНЫЕ АРГУМЕНТЫ: Обнаружен -javaagent / -noverify при сбое JVM!";
						rowCol = Ui.AccentOrange;
						isV = true;
					}
					else if (isTempDll)
					{
						cat = JvmCrashCategory.TempLibraryInJvm501;
						rule = "5.01 / 5.08";
						verdict = "⚠️ 5.01 МОДУЛЬ ИЗ TEMP: В памяти JVM в момент сбоя находилась библиотека из Temp!";
						rowCol = Ui.AccentOrange;
						isV = true;
					}
					else
					{
						cat = JvmCrashCategory.StandardJvmCrash;
						rule = "Allowed";
						verdict = "✔️ Штатный сбой Java HotSpot (Ошибок читов не обнаружено)";
						rowCol = Ui.AccentGreen;
					}

					string modulesSummary = "—";
					if (cheatDlls.Count > 0)
						modulesSummary = string.Format("Чит-библиотек: {0}", cheatDlls.Count);
					else if (isSuspiciousArgs)
						modulesSummary = "-javaagent / -noverify";
					else if (tempDlls.Count > 0)
						modulesSummary = string.Format("Temp DLL: {0}", tempDlls.Count);

					JvmCrashLogItem item = new JvmCrashLogItem
					{
						Index = itemIndex++,
						Category = cat,
						CategoryName = GetCategoryName(cat),
						FileName = fi.Name,
						FilePath = fi.FullName,
						ProblematicFrame = problematicFrame,
						CrashTime = crashTime,
						FileTimestamp = fi.LastWriteTime,
						VmArguments = vmArgs,
						DetectedCheatDlls = cheatDlls,
						TempDlls = tempDlls,
						AllDynamicLibraries = allDynamicLibs,
						CrashSignal = crashSignal,
						TargetRule = rule,
						Verdict = verdict,
						IsViolation = isV,
						IsCheatCrash = isCheatCrash || hasCheatDll,
						IsSuspiciousArgs = isSuspiciousArgs,
						IsTempDll = isTempDll,
						RowColor = rowCol
					};

					item.ForensicReport = BuildForensicReport(item);
					item.DynamicLibrariesReport = BuildDynamicLibrariesReport(item);
					item.TechnicalCrashContext = BuildTechnicalContext(item);

					results.Add(item);
				}
				catch { }
			}

			// Sort by newest first
			results.Sort(delegate(JvmCrashLogItem a, JvmCrashLogItem b)
			{
				return b.FileTimestamp.CompareTo(a.FileTimestamp);
			});

			for (int i = 0; i < results.Count; i++)
			{
				results[i].Index = i + 1;
			}

			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<JvmCrashLogItem>;
			if (allItems == null)
			{
				allItems = new List<JvmCrashLogItem>();
			}

			int total = allItems.Count;
			int violations = 0;
			int cheatCrashes = 0;
			int suspiciousArgs = 0;
			int tempModules = 0;
			int standard = 0;

			foreach (JvmCrashLogItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.IsCheatCrash) cheatCrashes++;
				if (item.IsSuspiciousArgs) suspiciousArgs++;
				if (item.IsTempDll) tempModules++;
				if (!item.IsViolation) standard++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiCheatCrashes.Text = cheatCrashes.ToString();
			lblKpiSuspiciousArgs.Text = suspiciousArgs.ToString();
			lblKpiTempModules.Text = tempModules.ToString();
			lblKpiStandard.Text = standard.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО НАРУШЕНИЙ: {0}", violations);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО ЛОГОВ JVM: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Helpers & Report Builders

		private static string GetCategoryName(JvmCrashCategory cat)
		{
			switch (cat)
			{
				case JvmCrashCategory.CheatDllCrash501: return "Чит в дампе сбоя";
				case JvmCrashCategory.SuspiciousJvmArgs501: return "Чит-аргументы JVM";
				case JvmCrashCategory.TempLibraryInJvm501: return "Temp модуль в Java";
				case JvmCrashCategory.RecentJvmCrash508: return "Свежий сбой JVM";
				default: return "Штатный сбой JVM";
			}
		}

		private static string BuildForensicReport(JvmCrashLogItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: АВАРИЙНЫЙ ДАМП JVM HOTSPOT (HS_ERR_PID) ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория объекта: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Файл дампа: {0}", item.FileName));
			sb.AppendLine(string.Format("Полный путь: {0}", item.FilePath));
			sb.AppendLine(string.Format("Время падения JVM: {0}", item.CrashTime));
			sb.AppendLine(string.Format("Сигнал сбоя: {0}", item.CrashSignal));
			sb.AppendLine(string.Format("Проблемный фрейм (Crash Frame): {0}", item.ProblematicFrame));
			sb.AppendLine(string.Format("Аргументы JVM (VM Arguments): {0}", item.VmArguments));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsViolation)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 / 5.08:");
				sb.AppendLine("Файл hs_err_pid*.log создается виртуальной машиной Java автоматически");
				sb.AppendLine("в момент критического падения (Access Violation / SIGSEGV).");
				sb.AppendLine("Дамп HotSpot фиксирует неизменяемый снимок всех загруженных в процесс");
				sb.AppendLine("динамических библиотек, адресное пространство и аргументы запуска.");
				if (item.DetectedCheatDlls.Count > 0)
				{
					sb.AppendLine("В памяти процесса на момент падения зафиксированы чит-библиотеки:");
					foreach (string cd in item.DetectedCheatDlls)
					{
						sb.AppendLine(" • " + cd);
					}
				}
				if (item.IsSuspiciousArgs)
				{
					sb.AppendLine("В аргументах запуска зафиксированы скрытые параметры внедрения JavaAgent:");
					sb.AppendLine(" " + item.VmArguments);
				}
				if (item.TempDlls.Count > 0)
				{
					sb.AppendLine("В память были загружены DLL из временной папки Temp:");
					foreach (string td in item.TempDlls)
					{
						sb.AppendLine(" • " + td);
					}
				}
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование читов (Сбой JVM HotSpot: {1})", "Player", item.FileName));
			}
			else
			{
				sb.AppendLine("Сбой виртуальной машины Java не связан с инъекцией читов.");
			}

			return sb.ToString();
		}

		private static string BuildDynamicLibrariesReport(JvmCrashLogItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Format("=== ДИНАМИЧЕСКИЕ БИБЛИОТЕКИ В ПАМЯТИ В МОМЕНТ СБОЯ ({0} модулей) ===", item.AllDynamicLibraries.Count));
			sb.AppendLine();

			if (item.DetectedCheatDlls.Count > 0)
			{
				sb.AppendLine("🚨 ОБНАРУЖЕННЫЕ ЧИТ-МОДУЛИ:");
				foreach (string d in item.DetectedCheatDlls) sb.AppendLine("  -> " + d);
				sb.AppendLine();
			}

			if (item.TempDlls.Count > 0)
			{
				sb.AppendLine("⚠️ БИБЛИОТЕКИ ИЗ ПАПКИ TEMP:");
				foreach (string d in item.TempDlls) sb.AppendLine("  -> " + d);
				sb.AppendLine();
			}

			sb.AppendLine("ПОЛНЫЙ СПИСОК ВСЕХ МОДУЛЕЙ ИЗ ДАМПА JVM:");
			foreach (string lib in item.AllDynamicLibraries)
			{
				sb.AppendLine(" • " + lib);
			}

			return sb.ToString();
		}

		private static string BuildTechnicalContext(JvmCrashLogItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ТЕХНИЧЕСКИЙ СТЕК И ПАРАМЕТРЫ СБОЯ JVM ===");
			sb.AppendLine(string.Format("Имя файла:            {0}", item.FileName));
			sb.AppendLine(string.Format("Время сбоя:           {0}", item.CrashTime));
			sb.AppendLine(string.Format("Сигнал ошибки:        {0}", item.CrashSignal));
			sb.AppendLine(string.Format("Проблемный фрейм:     {0}", item.ProblematicFrame));
			sb.AppendLine();
			sb.AppendLine("АРГУМЕНТЫ ВИРТУАЛЬНОЙ МАШИНЫ (VM Arguments):");
			sb.AppendLine(item.VmArguments);
			sb.AppendLine();
			sb.AppendLine("ФАЙЛОВЫЕ АТРИБУТЫ:");
			sb.AppendLine(string.Format("Путь: {0}", item.FilePath));
			sb.AppendLine(string.Format("Время изменения: {0:dd.MM.yyyy HH:mm:ss}", item.FileTimestamp));

			return sb.ToString();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (JvmCrashLogItem item in allItems)
			{
				if (currentFilter == JvmCrashFilter.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == JvmCrashFilter.CheatDllCrashes && !item.IsCheatCrash)
				{
					continue;
				}
				if (currentFilter == JvmCrashFilter.SuspiciousArgs && !item.IsSuspiciousArgs)
				{
					continue;
				}
				if (currentFilter == JvmCrashFilter.TempModules && !item.IsTempDll)
				{
					continue;
				}
				if (currentFilter == JvmCrashFilter.StandardCrashes && item.IsViolation)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.FileName.ToLowerInvariant().Contains(search) ||
								 item.ProblematicFrame.ToLowerInvariant().Contains(search) ||
								 item.VmArguments.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(item.ProblematicFrame);
				string modSummary = item.DetectedCheatDlls.Count > 0 ? string.Format("Чит: {0} шт.", item.DetectedCheatDlls.Count) :
					(item.IsSuspiciousArgs ? "-javaagent" : (item.TempDlls.Count > 0 ? "Temp DLL" : "—"));
				lvi.SubItems.Add(modSummary);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.CrashTime);

				lvi.Tag = item;
				lvi.ForeColor = item.RowColor;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtForensicReport.Clear();
				txtDynamicLibraries.Clear();
				txtTechnicalContext.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			JvmCrashLogItem item = listView.SelectedItems[0].Tag as JvmCrashLogItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtDynamicLibraries.Text = item.DynamicLibrariesReport;
			txtTechnicalContext.Text = item.TechnicalCrashContext;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			JvmCrashLogItem item = listView.SelectedItems[0].Tag as JvmCrashLogItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | Фрейм: {2} | Аргументы: {3} | {4}",
				item.CategoryName, item.FileName, item.ProblematicFrame, item.VmArguments, item.Verdict);

			Clipboard.SetText(text);
			MessageBox.Show("Запись успешно скопирована в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования отчета Discord.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			JvmCrashLogItem item = listView.SelectedItems[0].Tag as JvmCrashLogItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [СБОЙ JVM HOTSPOT: ШТАТНЫЙ]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Файл: {0} ({1})", item.FileName, item.CrashTime));
			sb.AppendLine(string.Format("Фрейм: {0}", item.ProblematicFrame));
			sb.AppendLine(string.Format("Сигнал: {0}", item.CrashSignal));
			if (item.DetectedCheatDlls.Count > 0)
			{
				sb.AppendLine(string.Format("Чит-библиотеки: {0}", string.Join(", ", item.DetectedCheatDlls.ToArray())));
			}
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (Чит в JVM HotSpot дампах)"));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord успешно скопирован в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением для генерации команды бана.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			JvmCrashLogItem item = listView.SelectedItems[0].Tag as JvmCrashLogItem;
			if (item == null) return;

			string cmd = "";
			if (item.IsCheatCrash)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Чит в дампе сбоя JVM {1})", targetPlayerName, item.FileName);
			}
			else if (item.IsSuspiciousArgs)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Чит-параметры в дампе JVM: {1})", targetPlayerName, item.FileName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, item.TargetRule);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_JvmCrash_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза дампов JVM HotSpot — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background:#0F1115; color:#ECF0F5; font-family:'Segoe UI',Arial,sans-serif; margin:20px; }");
					sb.AppendLine("h1 { color:#2EE6D6; font-size:20px; border-bottom:1px solid #262B33; padding-bottom:10px; }");
					sb.AppendLine(".kpi-container { display:flex; gap:12px; margin:15px 0; }");
					sb.AppendLine(".kpi-card { background:#171A1F; border:1px solid #262B33; padding:10px 14px; border-radius:6px; min-width:140px; }");
					sb.AppendLine(".kpi-title { font-size:11px; color:#8C96A3; }");
					sb.AppendLine(".kpi-val { font-size:18px; font-weight:bold; margin-top:4px; }");
					sb.AppendLine("table { width:100%; border-collapse:collapse; margin-top:15px; font-size:12px; }");
					sb.AppendLine("th, td { border:1px solid #262B33; padding:8px 10px; text-align:left; }");
					sb.AppendLine("th { background:#171A1F; color:#4FC3F7; }");
					sb.AppendLine("tr:nth-child(even) { background:#121418; }");
					sb.AppendLine(".crit { color:#EF5350; font-weight:bold; }");
					sb.AppendLine(".warn { color:#FFA726; font-weight:bold; }");
					sb.AppendLine(".ok { color:#66BB6A; }");
					sb.AppendLine("</style></head><body>");

					sb.AppendLine("<h1>🔥 Судебный протокол аудита аварийных логов JVM HotSpot (hs_err_pid)</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего сбоев JVM</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.01/5.08)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Сбоев от чит-библиотек</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiCheatCrashes.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Чит-аргументов</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiSuspiciousArgs.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Модулей из Temp</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiTempModules.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Штатных сбоев</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiStandard.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Файл сбоя</th><th>Проблемный фрейм</th><th>Модули/Аргументы</th><th>Регламент</th><th>Вердикт</th><th>Время сбоя</th></tr>");

					foreach (JvmCrashLogItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : "";
						string modSum = item.DetectedCheatDlls.Count > 0 ? string.Format("Чит: {0} шт.", item.DetectedCheatDlls.Count) : (item.IsSuspiciousArgs ? "-javaagent" : "—");
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td class='{8}'>{6}</td><td>{7}</td></tr>",
							item.Index, item.CategoryName, item.FileName, item.ProblematicFrame, modSum,
							item.TargetRule, item.Verdict, item.CrashTime, cssClass));
					}

					sb.AppendLine("</table>");
					sb.AppendLine("</body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					Process.Start(sfd.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при сохранении HTML отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
