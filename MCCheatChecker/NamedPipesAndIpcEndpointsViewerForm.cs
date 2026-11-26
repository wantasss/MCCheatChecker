using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class NamedPipesAndIpcEndpointsViewerForm : Form
	{
		#region P/Invoke

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetNamedPipeServerProcessId(IntPtr Pipe, out uint ServerProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetNamedPipeClientProcessId(IntPtr Pipe, out uint ClientProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		private const uint FILE_READ_ATTRIBUTES = 0x0080;
		private const uint FILE_SHARE_READ = 0x00000001;
		private const uint FILE_SHARE_WRITE = 0x00000002;
		private const uint OPEN_EXISTING = 3;

		#endregion

		#region Enums & Data Models

		public enum PipeItemCategory
		{
			KnownCheatPipe,
			SuspiciousAnonymousGuid,
			MinecraftJavawPipe,
			SuspiciousUserPipe,
			LegitimateSoftware,
			WindowsSystem
		}

		public enum PipeFilterCategory
		{
			All,
			Violations501,
			CheatInjectors,
			AnonymousGuids,
			MinecraftJavaw,
			LegitimateAndSystem
		}

		public class PipeForensicItem
		{
			public int Index;
			public PipeItemCategory Category;
			public string CategoryName;
			public string PipeFullName;
			public string PipeCleanName;
			public string ProcessOwnerName;
			public uint ProcessPid;
			public string ProcessPath;
			public string ProcessCommandLine;
			public string SignatureOrMatch;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsCheatPipe;
			public bool IsGuidPipe;
			public bool IsJavawPipe;
			public bool IsLegitimate;
			public Color RowColor;
			public string ForensicReport;
			public string TechnicalDetails;
			public string SecurityDetails;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private PipeFilterCategory currentFilter = PipeFilterCategory.All;
		private List<PipeForensicItem> allItems = new List<PipeForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtTechnicalDetails;
		private DarkTextBoxMultiline txtSecurityDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotalPipes;
		private Label lblKpiViolations;
		private Label lblKpiCheatPipes;
		private Label lblKpiAnonymousGuids;
		private Label lblKpiJavawPipes;
		private Label lblKpiLegitPipes;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCheatPipes;
		private DarkButton btnFilterAnonymousGuids;
		private DarkButton btnFilterJavawPipes;
		private DarkButton btnFilterLegitPipes;

		private DarkButton btnCopyRow;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		private TabControl detailTabs;

		#endregion

		public NamedPipesAndIpcEndpointsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🚰 Инспектор именованных каналов Named Pipes, IPC и межпроцессного взаимодействия — MCCheatChecker";
			Size = new Size(1320, 800);
			MinimumSize = new Size(1080, 620);
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
			lblHeaderTitle.Text = "🚰 ИНСПЕКТОР ИМЕНОВАННЫХ КАНАЛОВ NAMED PIPES И МЕЖПРОЦЕССНОГО ВЗАИМОДЕЙСТВИЯ (IPC)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит активных системных каналов \\\\.\\pipe\\: выявление IPC-мостов инжекторов (Vape, Drip, Celestial), анонимных GUID и связей с javaw.exe (5.01)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ PIPES...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(230, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Panel with Header + KPI
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 138;
			topPanel.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("🚰 Всего каналов", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotalPipes));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Чит-каналы IPC", "0", Ui.AccentOrange, ref kpiX, out lblKpiCheatPipes));
			kpiPanel.Controls.Add(CreateKpiBadge("⚠️ Анонимные GUID", "0", Ui.AccentPurple, ref kpiX, out lblKpiAnonymousGuids));
			kpiPanel.Controls.Add(CreateKpiBadge("⚡ Каналы javaw.exe", "0", Ui.AccentBlue, ref kpiX, out lblKpiJavawPipes));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Легитимных / ПО", "0", Ui.AccentGreen, ref kpiX, out lblKpiLegitPipes));
			topPanel.Controls.Add(kpiPanel);

			Controls.Add(topPanel);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все каналы", PipeFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01)", PipeFilterCategory.Violations501, ref fBtnX, filterBar);
			btnFilterCheatPipes = CreateFilterButton("💉 Читы & Инжекторы", PipeFilterCategory.CheatInjectors, ref fBtnX, filterBar);
			btnFilterAnonymousGuids = CreateFilterButton("⚠️ Анонимные GUID", PipeFilterCategory.AnonymousGuids, ref fBtnX, filterBar);
			btnFilterJavawPipes = CreateFilterButton("⚡ Каналы javaw", PipeFilterCategory.MinecraftJavaw, ref fBtnX, filterBar);
			btnFilterLegitPipes = CreateFilterButton("✔️ Системные и ПО", PipeFilterCategory.LegitimateAndSystem, ref fBtnX, filterBar);

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

			// SplitContainer for ListView and Bottom Details
			DarkSplitContainer splitContainer = new DarkSplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 6;
			splitContainer.BackColor = Ui.Border;

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("#", 45);
			listView.Columns.Add("Категория", 175);
			listView.Columns.Add("Имя канала Named Pipe", 260);
			listView.Columns.Add("Процесс-владелец (PID)", 230);
			listView.Columns.Add("Сигнатура / Тип", 190);
			listView.Columns.Add("Регламент", 115);
			listView.Columns.Add("Криминалистический вердикт", 235);

			listView.SelectedIndexChanged += delegate { UpdateSelectedDetails(); };
			listView.DoubleClick += delegate { CopySelectedRowText(); };
			Ui.ApplyDarkScrollbars(listView);
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Panel
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			// Action Bar
			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.SurfaceAlt;
			actionBar.Padding = new Padding(12, 6, 12, 6);

			btnCopyRow = new DarkButton();
			btnCopyRow.Text = "📋 Копировать строку";
			btnCopyRow.Size = new Size(160, 28);
			btnCopyRow.Location = new Point(12, 8);
			btnCopyRow.Click += delegate { CopySelectedRowText(); };
			actionBar.Controls.Add(btnCopyRow);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyForDiscord(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🔨 Копировать команду бана";
			btnCopyBanCmd.Size = new Size(205, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(586, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(744, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Аналитика");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabTech = new TabPage("🔍 Технические детали & Аргументы");
			tabTech.BackColor = Ui.Surface;
			txtTechnicalDetails = new DarkTextBoxMultiline();
			txtTechnicalDetails.Dock = DockStyle.Fill;
			txtTechnicalDetails.ReadOnly = true;
			txtTechnicalDetails.Font = Ui.FontMono(9f);
			tabTech.Controls.Add(txtTechnicalDetails);
			detailTabs.TabPages.Add(tabTech);

			TabPage tabSec = new TabPage("🛡️ Дескриптор безопасности & Права");
			tabSec.BackColor = Ui.Surface;
			txtSecurityDetails = new DarkTextBoxMultiline();
			txtSecurityDetails.Dock = DockStyle.Fill;
			txtSecurityDetails.ReadOnly = true;
			txtSecurityDetails.Font = Ui.FontMono(9f);
			tabSec.Controls.Add(txtSecurityDetails);
			detailTabs.TabPages.Add(tabSec);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string caption, string initialVal, Color valColor, ref int curX, out Label valLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(140, 52);
			p.Location = new Point(curX, 4);
			p.BackColor = Ui.SurfaceAlt;
			p.Padding = new Padding(4, 4, 4, 4);

			Label lblCaption = new Label();
			lblCaption.Text = caption;
			lblCaption.Font = Ui.FontBody(7.5f);
			lblCaption.ForeColor = Ui.TextMuted;
			lblCaption.Dock = DockStyle.Top;
			lblCaption.Height = 16;
			lblCaption.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(lblCaption);

			valLabel = new Label();
			valLabel.Text = initialVal;
			valLabel.Font = Ui.FontTitle(11.5f, true);
			valLabel.ForeColor = valColor;
			valLabel.Dock = DockStyle.Fill;
			valLabel.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(valLabel);

			curX += 146;
			return p;
		}

		private DarkButton CreateFilterButton(string text, PipeFilterCategory category, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(curX, 9);
			btn.Click += delegate
			{
				currentFilter = category;
				UpdateFilterButtonHighlights();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 136;
			return btn;
		}

		private void UpdateFilterButtonHighlights()
		{
			Color normalBg = Ui.Surface;
			Color activeBg = Ui.AccentBlue;
			Color activeText = Color.Black;
			Color normalText = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == PipeFilterCategory.All, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterViolations, currentFilter == PipeFilterCategory.Violations501, normalBg, Ui.AccentRed, normalText, Color.White);
			SetBtnStyle(btnFilterCheatPipes, currentFilter == PipeFilterCategory.CheatInjectors, normalBg, Ui.AccentOrange, normalText, Color.Black);
			SetBtnStyle(btnFilterAnonymousGuids, currentFilter == PipeFilterCategory.AnonymousGuids, normalBg, Ui.AccentPurple, normalText, Color.White);
			SetBtnStyle(btnFilterJavawPipes, currentFilter == PipeFilterCategory.MinecraftJavaw, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterLegitPipes, currentFilter == PipeFilterCategory.LegitimateAndSystem, normalBg, Ui.AccentGreen, normalText, Color.Black);
		}

		private void SetBtnStyle(DarkButton btn, bool isActive, Color normBg, Color actBg, Color normFg, Color actFg)
		{
			if (btn == null) return;
			btn.BackColor = isActive ? actBg : normBg;
			btn.ForeColor = isActive ? actFg : normFg;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
				return;

			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ PIPES...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			listView.Items.Clear();
			allItems.Clear();
			txtForensicReport.Clear();
			txtTechnicalDetails.Clear();
			txtSecurityDetails.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformPipeScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования Named Pipes: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.BackColor = Ui.AccentRed;
					lblStatusPill.ForeColor = Color.White;
					return;
				}

				List<PipeForensicItem> items = e.Result as List<PipeForensicItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpisAndStatus();
				UpdateFilterButtonHighlights();
				ApplyFilter();
			};
			scanWorker.RunWorkerAsync();
		}

		private List<PipeForensicItem> PerformPipeScan()
		{
			List<PipeForensicItem> results = new List<PipeForensicItem>();
			int idx = 1;

			// Database of known cheat pipe signatures
			string[] cheatSignatures = new string[]
			{
				"vape", "celestial", "expensive", "nurik", "nursultan", "kura", "minced", "slinky", "drip", "breezy",
				"catlavan", "catlean", "doomsday", "wexside", "richpremium", "flint", "rockstar", "boberware", "tenacity",
				"novoline", "riseclient", "thunderhack", "liquidbounce", "kmbox", "interception", "spoofer", "fluger",
				"zamorozka", "akrien", "exloader", "systemdlc", "aimbot", "triggerbot", "killaura", "hitbox", "reach",
				"velocity", "memesense", "midnight", "neverlose", "gamesense", "fatality", "weave", "matix", "wurst",
				"meteor", "future", "rusherhack", "kamiblue", "aristois", "pyro", "konas", "phobos", "fanta", "exhibition",
				"intent", "flux", "zeroday", "hummingbird", "alacrity", "croco", "monsoon", "axis", "pandora", "entropy",
				"whiteout", "koid", "phantom", "dream", "wild", "exire"
			};

			// Whitelist of legitimate application pipes
			string[] knownSoftwareKeywords = new string[]
			{
				"discord-ipc", "chrome.", "edge.", "crashpad", "mojo.", "steam.", "valve.",
				"vscode", "jetbrains", "wsl", "docker", "anydesk", "rudesktop", "autohotkey",
				"keyran", "clickermann", "vmware", "vbox", "powershell", "terminal"
			};

			// Whitelist of core Windows system pipes
			string[] coreSystemPipes = new string[]
			{
				"lsass", "ntsvcs", "spoolss", "samr", "netlogon", "srvsvc", "wkssvc", "epmapper",
				"initshutdown", "atsvc", "eventlog", "winreg", "trkwks", "scerpc", "winsock2",
				"lsm_api_service", "termsrv_api_service", "w32time_alt", "umpo_pipe",
				"rosharedfilemapping", "sqllocaldb", "certenroll", "keysvc"
			};

			// Cache active processes by PID
			Dictionary<uint, ProcessInfoCache> procCache = BuildProcessCache();

			string[] pipePaths = new string[0];
			try
			{
				pipePaths = Directory.GetFiles(@"\\.\pipe\");
			}
			catch { }

			Regex guidRegex = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.IgnoreCase);

			foreach (string pipePath in pipePaths)
			{
				string pipeName = Path.GetFileName(pipePath);
				string lowerName = pipeName.ToLowerInvariant();

				// Resolve Server Process ID via P/Invoke
				uint serverPid = 0;
				uint clientPid = 0;
				try
				{
					IntPtr hPipe = CreateFile(pipePath, FILE_READ_ATTRIBUTES, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
					if (hPipe != IntPtr.Zero && hPipe.ToInt64() != -1)
					{
						GetNamedPipeServerProcessId(hPipe, out serverPid);
						GetNamedPipeClientProcessId(hPipe, out clientPid);
						CloseHandle(hPipe);
					}
				}
				catch { }

				string ownerName = "Неизвестный владелец (Ограниченный доступ)";
				string procPath = "—";
				string procCmd = "—";

				if (serverPid > 0 && procCache.ContainsKey(serverPid))
				{
					ProcessInfoCache pic = procCache[serverPid];
					ownerName = string.Format("{0} (PID: {1})", pic.Name, serverPid);
					procPath = pic.Path;
					procCmd = pic.CommandLine;
				}
				else if (serverPid == 4)
				{
					ownerName = "NT Kernel & System (PID: 4)";
					procPath = "C:\\Windows\\System32\\ntoskrnl.exe";
				}

				// Check 1: Known Cheat Pipe Signature
				string matchedCheat = null;
				foreach (string sig in cheatSignatures)
				{
					if (lowerName.Contains(sig))
					{
						matchedCheat = sig;
						break;
					}
				}

				if (matchedCheat != null)
				{
					// 🚨 5.01 CHEAT PIPE DETECTED!
					results.Add(new PipeForensicItem
					{
						Index = idx++,
						Category = PipeItemCategory.KnownCheatPipe,
						CategoryName = "🚨 Чит-канал (Named Pipe)",
						PipeFullName = pipePath,
						PipeCleanName = pipeName,
						ProcessOwnerName = ownerName,
						ProcessPid = serverPid,
						ProcessPath = procPath,
						ProcessCommandLine = procCmd,
						SignatureOrMatch = "Сигнатура: " + matchedCheat,
						TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
						Verdict = string.Format("🚨 5.01 ИНЖЕКТОР (Канал чита {0})", matchedCheat.ToUpperInvariant()),
						IsViolation = true,
						IsCheatPipe = true,
						RowColor = Ui.AccentRed,
						TechnicalDetails = string.Format("Имя канала: {0}\r\nСервер PID: {1}\r\nКлиент PID: {2}\r\nПроцесс-владелец: {3}\r\nПуть процесса: {4}\r\nКомандная строка: {5}",
							pipePath, serverPid, clientPid, ownerName, procPath, procCmd),
						SecurityDetails = "Канал связи между инжектором чита и целевым процессом javaw.exe / Minecraft.",
						ForensicReport = string.Format(
							"🚨 ОБНАРУЖЕН АКТИВНЫЙ ИМЕНОВАННЫЙ КАНАЛ ЧИТА!\r\n\r\n" +
							"Имя канала: {0}\r\n" +
							"Сигнатура софта: {1}\r\n" +
							"Процесс-владелец: {2}\r\n\r\n" +
							"Каналы Named Pipes используются читерскими программами (Vape, Drip, Slinky, Celestial) для передачи команд, " +
							"координат ESP, синхронизации триггербота и обмена данными между внешним лоадером и внедренной в javaw.exe DLL библиотекой.\r\n\r\n" +
							"Нарушение регламента: 5.01 (Использование читов / стороннего софта).",
							pipePath, matchedCheat, ownerName)
					});
					continue;
				}

				// Check 2: Connection with Minecraft / javaw.exe
				bool isJavaw = ownerName.ToLowerInvariant().Contains("javaw") || ownerName.ToLowerInvariant().Contains("java");
				if (isJavaw)
				{
					results.Add(new PipeForensicItem
					{
						Index = idx++,
						Category = PipeItemCategory.MinecraftJavawPipe,
						CategoryName = "⚡ Канал javaw.exe",
						PipeFullName = pipePath,
						PipeCleanName = pipeName,
						ProcessOwnerName = ownerName,
						ProcessPid = serverPid,
						ProcessPath = procPath,
						ProcessCommandLine = procCmd,
						SignatureOrMatch = "Процесс Minecraft (javaw.exe)",
						TargetRule = "—",
						Verdict = "⚡ Активный канал процесса Minecraft",
						IsViolation = false,
						IsJavawPipe = true,
						RowColor = Ui.AccentBlue,
						TechnicalDetails = string.Format("Имя канала: {0}\r\nВладелец: {1}\r\nПуть: {2}\r\nКомандная строка: {3}", pipePath, ownerName, procPath, procCmd),
						SecurityDetails = "Канал принадлежит процессу виртуальной машины Java / Minecraft.",
						ForensicReport = "Канал " + pipePath + " открыт процессом javaw.exe."
					});
					continue;
				}

				// Check 3: Anonymous GUID Named Pipe
				if (guidRegex.IsMatch(lowerName))
				{
					// Check if it belongs to legitimate software
					bool isLegitSoftware = false;
					foreach (string kw in knownSoftwareKeywords)
					{
						if (lowerName.Contains(kw) || ownerName.ToLowerInvariant().Contains(kw))
						{
							isLegitSoftware = true;
							break;
						}
					}

					if (!isLegitSoftware)
					{
						results.Add(new PipeForensicItem
						{
							Index = idx++,
							Category = PipeItemCategory.SuspiciousAnonymousGuid,
							CategoryName = "⚠️ Анонимный GUID канал",
							PipeFullName = pipePath,
							PipeCleanName = pipeName,
							ProcessOwnerName = ownerName,
							ProcessPid = serverPid,
							ProcessPath = procPath,
							ProcessCommandLine = procCmd,
							SignatureOrMatch = "Формат GUID: " + guidRegex.Match(lowerName).Value,
							TargetRule = "5.01 ПРОВЕРКА",
							Verdict = "⚠️ Подозрительный анонимный GUID канал",
							IsViolation = false,
							IsGuidPipe = true,
							RowColor = Ui.AccentOrange,
							TechnicalDetails = string.Format("Имя канала: {0}\r\nВладелец: {1}\r\nПуть: {2}", pipePath, ownerName, procPath),
							SecurityDetails = "Анонимный GUID-канал без явного системного сопоставления.",
							ForensicReport = "Обнаружен анонимный канал с GUID идентификатором: " + pipePath + ". Лоадеры читов часто генерируют случайные GUID имена для сокрытия IPC каналов от строкового детектирования."
						});
						continue;
					}
				}

				// Check 4: Whitelisted Software (Discord, Chrome, Steam, AnyDesk, etc.)
				bool isKnownApp = false;
				string matchedApp = null;
				foreach (string kw in knownSoftwareKeywords)
				{
					if (lowerName.Contains(kw) || ownerName.ToLowerInvariant().Contains(kw))
					{
						isKnownApp = true;
						matchedApp = kw;
						break;
					}
				}

				if (isKnownApp)
				{
					results.Add(new PipeForensicItem
					{
						Index = idx++,
						Category = PipeItemCategory.LegitimateSoftware,
						CategoryName = "🌐 Программы и сервисы",
						PipeFullName = pipePath,
						PipeCleanName = pipeName,
						ProcessOwnerName = ownerName,
						ProcessPid = serverPid,
						ProcessPath = procPath,
						ProcessCommandLine = procCmd,
						SignatureOrMatch = "ПО: " + matchedApp,
						TargetRule = "—",
						Verdict = "✔️ Легитимное приложение",
						IsViolation = false,
						IsLegitimate = true,
						RowColor = Ui.AccentGreen,
						TechnicalDetails = string.Format("Имя канала: {0}\r\nПриложение: {1}\r\nВладелец: {2}", pipePath, matchedApp, ownerName),
						SecurityDetails = "Стандартный канал межпроцессного взаимодействия легитимного ПО.",
						ForensicReport = "Канал " + pipePath + " принадлежит легитимному приложению (" + matchedApp + "). Безопасен."
					});
					continue;
				}

				// Check 5: Core Windows System Pipe
				bool isSys = false;
				foreach (string sysKw in coreSystemPipes)
				{
					if (lowerName.Contains(sysKw))
					{
						isSys = true;
						break;
					}
				}

				results.Add(new PipeForensicItem
				{
					Index = idx++,
					Category = isSys ? PipeItemCategory.WindowsSystem : PipeItemCategory.LegitimateSoftware,
					CategoryName = isSys ? "🛡️ Системный Windows" : "⚙️ Системный сервис",
					PipeFullName = pipePath,
					PipeCleanName = pipeName,
					ProcessOwnerName = ownerName,
					ProcessPid = serverPid,
					ProcessPath = procPath,
					ProcessCommandLine = procCmd,
					SignatureOrMatch = isSys ? "Служба Windows" : "Системный IPC",
					TargetRule = "—",
					Verdict = "✔️ Системный канал",
					IsViolation = false,
					IsLegitimate = true,
					RowColor = Ui.TextPrimary,
					TechnicalDetails = string.Format("Имя канала: {0}\r\nВладелец: {1}", pipePath, ownerName),
					SecurityDetails = "Штатный интерфейс удаленного вызова процедур (RPC) или системный канал ОС.",
					ForensicReport = "Канал " + pipePath + " используется подсистемами Windows."
				});
			}

			return results;
		}

		private class ProcessInfoCache
		{
			public string Name;
			public string Path;
			public string CommandLine;
		}

		private Dictionary<uint, ProcessInfoCache> BuildProcessCache()
		{
			Dictionary<uint, ProcessInfoCache> dict = new Dictionary<uint, ProcessInfoCache>();
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					try
					{
						ProcessInfoCache pic = new ProcessInfoCache();
						pic.Name = p.ProcessName + ".exe";
						try { pic.Path = p.MainModule != null ? p.MainModule.FileName : "—"; } catch { pic.Path = "—"; }
						dict[(uint)p.Id] = pic;
					}
					catch { }
					finally
					{
						try { p.Dispose(); } catch { }
					}
				}

				// Query WMI for command lines
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						try
						{
							uint pid = Convert.ToUInt32(mo["ProcessId"]);
							string cmd = mo["CommandLine"] != null ? mo["CommandLine"].ToString() : "";
							if (dict.ContainsKey(pid))
							{
								dict[pid].CommandLine = cmd;
							}
						}
						catch { }
					}
				}
			}
			catch { }
			return dict;
		}

		#endregion

		#region KPI & Filtering

		private void UpdateKpisAndStatus()
		{
			int total = allItems.Count;
			int viols = 0;
			int cheatPipes = 0;
			int guids = 0;
			int javawPipes = 0;
			int legitPipes = 0;

			foreach (PipeForensicItem it in allItems)
			{
				if (it.IsViolation) viols++;
				if (it.IsCheatPipe) cheatPipes++;
				if (it.IsGuidPipe) guids++;
				if (it.IsJavawPipe) javawPipes++;
				if (it.IsLegitimate) legitPipes++;
			}

			lblKpiTotalPipes.Text = total.ToString();
			lblKpiViolations.Text = viols.ToString();
			lblKpiCheatPipes.Text = cheatPipes.ToString();
			lblKpiAnonymousGuids.Text = guids.ToString();
			lblKpiJavawPipes.Text = javawPipes.ToString();
			lblKpiLegitPipes.Text = legitPipes.ToString();

			if (viols > 0)
			{
				lblStatusPill.Text = string.Format("🚨 НАЙДЕНО ЧИТ-КАНАЛОВ: {0} (5.01)", viols);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else if (guids > 0)
			{
				lblStatusPill.Text = string.Format("⚠️ АНОНИМНЫХ GUID: {0}", guids);
				lblStatusPill.BackColor = Ui.AccentOrange;
				lblStatusPill.ForeColor = Color.Black;
			}
			else
			{
				lblStatusPill.Text = "✔️ ВСЕ КАНАЛЫ ЛЕГИТИМНЫ";
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("🚨 Нарушения ({0})", viols);
			btnFilterCheatPipes.Text = string.Format("💉 Читы ({0})", cheatPipes);
			btnFilterAnonymousGuids.Text = string.Format("⚠️ GUID ({0})", guids);
			btnFilterJavawPipes.Text = string.Format("⚡ javaw ({0})", javawPipes);
			btnFilterLegitPipes.Text = string.Format("✔️ Легитимные ({0})", legitPipes);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (PipeForensicItem item in allItems)
			{
				// Category Filter
				if (currentFilter == PipeFilterCategory.Violations501 && !item.IsViolation) continue;
				if (currentFilter == PipeFilterCategory.CheatInjectors && !item.IsCheatPipe) continue;
				if (currentFilter == PipeFilterCategory.AnonymousGuids && !item.IsGuidPipe) continue;
				if (currentFilter == PipeFilterCategory.MinecraftJavaw && !item.IsJavawPipe) continue;
				if (currentFilter == PipeFilterCategory.LegitimateAndSystem && !item.IsLegitimate) continue;

				// Text Search
				if (!string.IsNullOrEmpty(search))
				{
					bool match = (item.PipeFullName != null && item.PipeFullName.ToLowerInvariant().Contains(search)) ||
								 (item.PipeCleanName != null && item.PipeCleanName.ToLowerInvariant().Contains(search)) ||
								 (item.ProcessOwnerName != null && item.ProcessOwnerName.ToLowerInvariant().Contains(search)) ||
								 (item.SignatureOrMatch != null && item.SignatureOrMatch.ToLowerInvariant().Contains(search)) ||
								 (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.PipeCleanName);
				lvi.SubItems.Add(item.ProcessOwnerName);
				lvi.SubItems.Add(item.SignatureOrMatch);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);

				lvi.ForeColor = item.RowColor;
				lvi.Tag = item;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
				UpdateSelectedDetails();
			}
			else
			{
				txtForensicReport.Text = "Нет каналов Named Pipes, соответствующих выбранному фильтру.";
				txtTechnicalDetails.Clear();
				txtSecurityDetails.Clear();
			}
		}

		private void UpdateSelectedDetails()
		{
			if (listView.SelectedItems.Count == 0) return;
			PipeForensicItem item = listView.SelectedItems[0].Tag as PipeForensicItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport ?? "";
			txtTechnicalDetails.Text = item.TechnicalDetails ?? "";
			txtSecurityDetails.Text = string.Format(
				"Имя канала: {0}\r\nВладелец: {1}\r\nДескриптор:\r\n{2}",
				item.PipeFullName,
				item.ProcessOwnerName,
				item.SecurityDetails ?? "—");
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRowText()
		{
			if (listView.SelectedItems.Count == 0) return;
			PipeForensicItem item = listView.SelectedItems[0].Tag as PipeForensicItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | {2} | {3} | {4} | {5}",
				item.CategoryName, item.PipeFullName, item.ProcessOwnerName, item.SignatureOrMatch, item.TargetRule, item.Verdict);
			Clipboard.SetText(text);
			MessageBox.Show("Строка скопирована в буфер обмена:\r\n" + text, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyForDiscord()
		{
			if (listView.SelectedItems.Count == 0) return;
			PipeForensicItem item = listView.SelectedItems[0].Tag as PipeForensicItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine(item.IsViolation ? "- [НАРУШЕНИЕ 5.01 - NAMED PIPE ЧИТА]" : "+ [ПРОВЕРКА NAMED PIPES]");
			sb.AppendLine(string.Format("Игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Время проверки: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Канал: {0}", item.PipeFullName));
			sb.AppendLine(string.Format("Процесс-владелец: {0}", item.ProcessOwnerName));
			sb.AppendLine(string.Format("Сигнатура: {0}", item.SignatureOrMatch));
			sb.AppendLine(string.Format("Регламент: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Форматированный отчет для Discord скопирован!", "Discord Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите строку с нарушением регламента для генерации команды бана.", "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			PipeForensicItem item = listView.SelectedItems[0].Tag as PipeForensicItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранный канал не является нарушением правил 5.01.", "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Канал Named Pipe: {1})", targetPlayerName, item.PipeCleanName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_NamedPipes_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза Named Pipes IPC — " + targetPlayerName + "</title>");
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

					sb.AppendLine("<h1>🚰 Судебная экспертиза именованных каналов Named Pipes и IPC</h1>");
					sb.AppendLine(string.Format("<p>Игрок: <b>{0}</b> | Дата генерации: {1:yyyy-MM-dd HH:mm:ss}</p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🚰 Всего каналов</div><div class='kpi-val'>{0}</div></div>", lblKpiTotalPipes.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🚨 Нарушений (5.01)</div><div class='kpi-val crit'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>💉 Чит-каналы IPC</div><div class='kpi-val warn'>{0}</div></div>", lblKpiCheatPipes.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>⚠️ Анонимные GUID</div><div class='kpi-val'>{0}</div></div>", lblKpiAnonymousGuids.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>⚡ Каналы javaw.exe</div><div class='kpi-val'>{0}</div></div>", lblKpiJavawPipes.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>✔️ Легитимные / ПО</div><div class='kpi-val ok'>{0}</div></div>", lblKpiLegitPipes.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table><thead><tr><th>#</th><th>Категория</th><th>Имя канала Named Pipe</th><th>Процесс-владелец (PID)</th><th>Сигнатура / Тип</th><th>Регламент</th><th>Вердикт</th></tr></thead><tbody>");
					foreach (PipeForensicItem it in allItems)
					{
						string cls = it.IsViolation ? "crit" : (it.IsLegitimate ? "ok" : "warn");
						sb.AppendLine(string.Format(
							"<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td></tr>",
							cls, it.Index, it.CategoryName, it.PipeFullName, it.ProcessOwnerName, it.SignatureOrMatch, it.TargetRule, it.Verdict));
					}
					sb.AppendLine("</tbody></table></body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("HTML отчет успешно экспортирован:\r\n" + sfd.FileName, "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка сохранения HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
