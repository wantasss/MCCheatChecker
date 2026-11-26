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
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum DiscordForensicFilter
	{
		All,
		ViolationsOnly,
		CheatRpcOnly,
		IpcEndpointsOnly,
		ModificationsOnly,
		WhitelistedOnly
	}

	public class DiscordForensicEvidenceItem
	{
		public int Index;
		public string Category; // "Активность RPC", "Канал Discord IPC", "Модификация клиента", "LevelDB Артефакт", "Кэш и Токены"
		public string SourcePath;
		public string DetectedArtifact;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.01", "5.08", "РАЗРЕШЕНО", "—"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string ProcessOwner;
		public string RawPayloadOrSnippet;
	}

	public class DiscordRichPresenceAndRpcForensicsViewerForm : Form
	{
		#region Win32 API for Named Pipes

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

		private readonly string targetPlayerName;
		private List<DiscordForensicEvidenceItem> allItems = new List<DiscordForensicEvidenceItem>();
		private DiscordForensicFilter currentFilter = DiscordForensicFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiCheatRpc;
		private Label lblKpiIpc;
		private Label lblKpiMods;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCheatRpc;
		private DarkButton btnFilterIpc;
		private DarkButton btnFilterMods;
		private DarkButton btnFilterWhitelisted;

		private DarkTextBox searchBox;
		private DarkListView listView;
		private TabControl detailTabs;
		private DarkTextBoxMultiline txtAnalysis;
		private DarkTextBoxMultiline txtTechnical;
		private DarkTextBoxMultiline txtBanVerdict;

		private DarkButton btnCopyBanCmd;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		public DiscordRichPresenceAndRpcForensicsViewerForm() : this("Player")
		{
		}

		public DiscordRichPresenceAndRpcForensicsViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "💬 Инспектор Discord, Rich Presence и RPC криминалистики (Tool 71 / Step 68) — MCCheatChecker";
			this.Size = new Size(1360, 830);
			this.MinimumSize = new Size(1060, 640);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			// Панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "💬 ИНСПЕКТОР DISCORD, RICH PRESENCE И RPC КРИМИНАЛИСТИКИ";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentBlue;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокий аудит Discord LevelDB кэша, истории Rich Presence читов (Vape/Expensive/Celestial/Nursultan), каналов IPC (pipe/discord-ipc-*) и модификаций Vencord/BetterDiscord (5.01/5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация экспертизы Discord & RPC...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 360, 16);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblStatusPill.AutoSize = true;
			headerPanel.Controls.Add(lblStatusPill);

			// Карточки KPI
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int kx = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО АРТЕФАКТОВ", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.01/5.08)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💬 ЧИТ-АКТИВНОСТЬ RPC", "0", Ui.AccentOrange, ref kx, out lblKpiCheatRpc));
			kpiPanel.Controls.Add(CreateKpiBadge("🚰 DISCORD IPC КАНАЛЫ", "0", Ui.AccentPurple, ref kx, out lblKpiIpc));
			kpiPanel.Controls.Add(CreateKpiBadge("🧩 МОДИФИКАЦИИ КЛИЕНТА", "0", Ui.AccentBlue, ref kx, out lblKpiMods));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ БЕЛЫЙ СПИСОК", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все артефакты", DiscordForensicFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", DiscordForensicFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterCheatRpc = CreateFilterButton("💬 Чит-активность RPC", DiscordForensicFilter.CheatRpcOnly, ref bx, filterBar);
			btnFilterIpc = CreateFilterButton("🚰 Discord IPC каналы", DiscordForensicFilter.IpcEndpointsOnly, ref bx, filterBar);
			btnFilterMods = CreateFilterButton("🧩 Модификации клиента", DiscordForensicFilter.ModificationsOnly, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Белый список", DiscordForensicFilter.WhitelistedOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по процессу, артефакту, серверу, токену или правилу...";
			searchBox.Size = new Size(250, 26);
			searchBox.Location = new Point(filterBar.Width - 270, 8);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			// Панель нижних действий
			Panel bottomActions = new Panel();
			bottomActions.Dock = DockStyle.Bottom;
			bottomActions.Height = 46;
			bottomActions.BackColor = Ui.Surface;
			bottomActions.Padding = new Padding(12, 8, 12, 8);
			this.Controls.Add(bottomActions);

			int ax = 12;
			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана", ref ax, bottomActions, delegate { CopyBanCommand(); });
			btnCopyDiscord = CreateActionButton("📋 Сводка для Discord", ref ax, bottomActions, delegate { CopyDiscordSummary(); });
			btnExportHtml = CreateActionButton("🌐 Экспорт HTML-отчёта", ref ax, bottomActions, delegate { ExportHtmlReport(); });
			btnRescan = CreateActionButton("🔄 Повторить аудит Discord", ref ax, bottomActions, delegate { StartScan(); });

			// Разделитель
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 350;
			split.BackColor = Ui.Border;
			this.Controls.Add(split);

			// Список ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Columns.Add("№", 45);
			listView.Columns.Add("Категория", 180);
			listView.Columns.Add("Источник / Путь", 240);
			listView.Columns.Add("Обнаруженный артефакт / Процесс", 270);
			listView.Columns.Add("Статус", 110);
			listView.Columns.Add("Угроза", 95);
			listView.Columns.Add("Вердикт детектора", 290);
			listView.Columns.Add("Регламент", 95);
			listView.Columns.Add("Штамп времени", 130);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			split.Panel2.Controls.Add(detailTabs);

			TabPage tabAnalysis = new TabPage("🔍 Судебный анализ Discord & RPC");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("🚰 Discord IPC и Подключенные процессы");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			txtTechnical.Font = Ui.FontMono(9f);
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.01 / 5.08 и Команда бана");
			txtBanVerdict = new DarkTextBoxMultiline();
			txtBanVerdict.Dock = DockStyle.Fill;
			txtBanVerdict.ReadOnly = true;
			txtBanVerdict.Font = Ui.FontMono(9f);
			tabBan.Controls.Add(txtBanVerdict);
			detailTabs.TabPages.Add(tabBan);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(200, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.Surface;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(192, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(192, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 208;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, DiscordForensicFilter filter, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(155, 30);
			btn.Location = new Point(curX, 6);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtonStyles();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 162;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int curX, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(205, 30);
			btn.Location = new Point(curX, 8);
			btn.Cursor = Cursors.Hand;
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 215;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			Color activeBg = Ui.AccentBlue;
			Color activeFg = Color.White;
			Color normalBg = Ui.Surface;
			Color normalFg = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == DiscordForensicFilter.All, normalBg, activeBg, normalFg, activeFg);
			SetBtnStyle(btnFilterViolations, currentFilter == DiscordForensicFilter.ViolationsOnly, normalBg, Ui.AccentRed, normalFg, activeFg);
			SetBtnStyle(btnFilterCheatRpc, currentFilter == DiscordForensicFilter.CheatRpcOnly, normalBg, Ui.AccentOrange, normalFg, Color.Black);
			SetBtnStyle(btnFilterIpc, currentFilter == DiscordForensicFilter.IpcEndpointsOnly, normalBg, Ui.AccentPurple, normalFg, activeFg);
			SetBtnStyle(btnFilterMods, currentFilter == DiscordForensicFilter.ModificationsOnly, normalBg, Ui.AccentTeal, normalFg, Color.Black);
			SetBtnStyle(btnFilterWhitelisted, currentFilter == DiscordForensicFilter.WhitelistedOnly, normalBg, Ui.AccentGreen, normalFg, Color.Black);
		}

		private void SetBtnStyle(DarkButton btn, bool isActive, Color normBg, Color actBg, Color normFg, Color actFg)
		{
			if (btn == null) return;
			btn.BackColor = isActive ? actBg : normBg;
			btn.ForeColor = isActive ? actFg : normFg;
		}

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
				return;

			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ DISCORD & RPC...";
			lblStatusPill.ForeColor = Ui.AccentOrange;

			listView.Items.Clear();
			allItems.Clear();
			txtAnalysis.Clear();
			txtTechnical.Clear();
			txtBanVerdict.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformDiscordForensicScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования Discord: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<DiscordForensicEvidenceItem> items = e.Result as List<DiscordForensicEvidenceItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpis();
				UpdateFilterButtonStyles();
				ApplyFilter();

				int violations = 0;
				foreach (DiscordForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation) violations++;
				}

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} НАРУШЕНИЙ (5.01 / 5.08)", violations);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ НАРУШЕНИЙ DISCORD & RPC НЕ ОБНАРУЖЕНО";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			};

			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpis()
		{
			int total = allItems.Count;
			int violations = 0;
			int cheatRpc = 0;
			int ipc = 0;
			int mods = 0;
			int whitelisted = 0;

			foreach (DiscordForensicEvidenceItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.Category == "Активность RPC") cheatRpc++;
				if (item.Category == "Канал Discord IPC") ipc++;
				if (item.Category == "Модификация клиента") mods++;
				if (item.IsWhitelisted) whitelisted++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiCheatRpc.Text = cheatRpc.ToString();
			lblKpiIpc.Text = ipc.ToString();
			lblKpiMods.Text = mods.ToString();
			lblKpiWhitelisted.Text = whitelisted.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (DiscordForensicEvidenceItem item in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case DiscordForensicFilter.ViolationsOnly:
						matchFilter = item.IsViolation;
						break;
					case DiscordForensicFilter.CheatRpcOnly:
						matchFilter = (item.Category == "Активность RPC");
						break;
					case DiscordForensicFilter.IpcEndpointsOnly:
						matchFilter = (item.Category == "Канал Discord IPC");
						break;
					case DiscordForensicFilter.ModificationsOnly:
						matchFilter = (item.Category == "Модификация клиента");
						break;
					case DiscordForensicFilter.WhitelistedOnly:
						matchFilter = item.IsWhitelisted;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (item.DetectedArtifact != null && item.DetectedArtifact.ToLowerInvariant().Contains(search))
						|| (item.SourcePath != null && item.SourcePath.ToLowerInvariant().Contains(search))
						|| (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search))
						|| (item.Category != null && item.Category.ToLowerInvariant().Contains(search))
						|| (item.ProcessOwner != null && item.ProcessOwner.ToLowerInvariant().Contains(search))
						|| (item.TargetRule != null && item.TargetRule.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.SourcePath);
				lvi.SubItems.Add(item.DetectedArtifact);
				lvi.SubItems.Add(item.Status);
				lvi.SubItems.Add(item.ThreatLevel);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.ForeColor = item.RowColor;
				lvi.Tag = item;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtAnalysis.Text = "Нет артефактов, соответствующих выбранному фильтру.";
				txtTechnical.Clear();
				txtBanVerdict.Clear();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			DiscordForensicEvidenceItem item = listView.SelectedItems[0].Tag as DiscordForensicEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine("           СУДЕБНЫЙ КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ DISCORD & RPC АРТЕФАКТА                     ");
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Обнаруженный артефакт:   {0}", item.DetectedArtifact));
			sbAnalysis.AppendLine(string.Format("Источник / Путь:         {0}", item.SourcePath));
			sbAnalysis.AppendLine(string.Format("Статус артефакта:        {0}", item.Status));
			sbAnalysis.AppendLine(string.Format("Уровень угрозы:          {0}", item.ThreatLevel));
			sbAnalysis.AppendLine(string.Format("Регламент проверки:      {0}", item.TargetRule));
			sbAnalysis.AppendLine(string.Format("Штамп времени:           {0:yyyy-MM-dd HH:mm:ss}", item.Timestamp));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine("ПОДРОБНОЕ ЗАКЛЮЧЕНИЕ ЭКСПЕРТИЗЫ:");
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine(item.DetailDescription);
			sbAnalysis.AppendLine();
			if (!string.IsNullOrEmpty(item.RawPayloadOrSnippet))
			{
				sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
				sbAnalysis.AppendLine("ИЗВЛЕЧЕННЫЙ ФРАГМЕНТ / RAW СТРОКА ИЗ КЭША LEVELDB:");
				sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
				sbAnalysis.AppendLine(item.RawPayloadOrSnippet);
			}
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Technical & IPC
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("           ТЕХНИЧЕСКИЕ ДАННЫЕ ПРОЦЕССОВ И КАНАЛОВ DISCORD IPC                             ");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine();
			sbTech.AppendLine(string.Format("Владелец процесса / Клиент: {0}", string.IsNullOrEmpty(item.ProcessOwner) ? "Не применимо / Дисковый артефакт" : item.ProcessOwner));
			sbTech.AppendLine(string.Format("Расположение источника:    {0}", item.SourcePath));
			sbTech.AppendLine(string.Format("Является нарушением:       {0}", item.IsViolation ? "ДА (КРИТИЧЕСКАЯ УГРОЗА)" : "НЕТ"));
			sbTech.AppendLine(string.Format("В белом списке:            {0}", item.IsWhitelisted ? "ДА (ЛЕГИТИМНО)" : "НЕТ"));
			sbTech.AppendLine();
			sbTech.AppendLine("МЕХАНИКА DISCORD RPC ПРИМЕНИТЕЛЬНО К ЧИТАМ:");
			sbTech.AppendLine("1. Чит-клиенты Minecraft (Celestial, Expensive, Nursultan, Wild, Wurst, Meteor, Vape) подключаются к каналу");
			sbTech.AppendLine("   Named Pipe '\\\\.\\pipe\\discord-ipc-0'..9 от имени javaw.exe или скрытого процесса для отображения богатого статуса (Rich Presence).");
			sbTech.AppendLine("2. В кэше Local Storage (LevelDB) сохраняются JSON структуры с полями 'state', 'details', 'name', 'application_id'.");
			sbTech.AppendLine("   Даже если чит удален с диска, упоминания в LevelDB сохраняются с меткой времени.");
			sbTech.AppendLine("3. Модификации Discord (Vencord, BetterDiscord, Vesktop) могут содержать плагины сокрытия стрима (ScreenShare Bypass)");
			sbTech.AppendLine("   или фейковые оверлеи для обхода регламента 5.08.");
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Ban Verdict
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine("           РЕГЛАМЕНТ 5.01 / 5.08 И КОМАНДА БАНА ДЛЯ МОДЕРАТОРА                            ");
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine();
			if (item.IsViolation)
			{
				sbBan.AppendLine(string.Format("🚨 ВЕРДИКТ: ОБНАРУЖЕНО НАРУШЕНИЕ РЕГЛАМЕНТА {0}!", item.TargetRule));
				sbBan.AppendLine();
				sbBan.AppendLine("ГОТОВАЯ КОМАНДА ДЛЯ СЕРВЕРА (СКОПИРУЙТЕ В ЧАТ):");
				string banReason = item.TargetRule == "5.01" 
					? string.Format("5.01 (Использование запрещённого софта: {0})", item.DetectedArtifact)
					: string.Format("5.08 (Сокрытие читов / обход проверки через Discord: {0})", item.DetectedArtifact);
				sbBan.AppendLine(string.Format("/ban {0} 30d {1}", targetPlayerName, banReason));
				sbBan.AppendLine();
				sbBan.AppendLine("ЮРИДИЧЕСКАЯ СПРАВКА ПО РЕГЛАМЕНТУ:");
				if (item.TargetRule == "5.01")
				{
					sbBan.AppendLine("Пункт 5.01 гласит: Запрещено использование любых модификаций клиента, программ и скриптов,");
					sbBan.AppendLine("дающих преимущество в игре (читы, автокликеры, макросы, X-Ray, инжекторы). Присутствие подтверждённого");
					sbBan.AppendLine("Rich Presence чита или активного IPC подключения к читу является неопровержимым доказательством запуска.");
				}
				else
				{
					sbBan.AppendLine("Пункт 5.08 гласит: Запрещены любые действия, направленные на сокрытие использования читов,");
					sbBan.AppendLine("обход проверки, удаление следов, очистку логов или использование специализированных плагинов обхода screenshare.");
				}
			}
			else
			{
				sbBan.AppendLine("✅ НАРУШЕНИЯ НЕ ЗАФИКСИРОВАНО");
				sbBan.AppendLine();
				sbBan.AppendLine("Данный артефакт является легитимным компонентом Windows, стандартной установкой Discord");
				sbBan.AppendLine("или доверенной активностью игры/приложения из белого списка. Бан не требуется.");
			}
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите строку в таблице для копирования команды бана.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			DiscordForensicEvidenceItem item = listView.SelectedItems[0].Tag as DiscordForensicEvidenceItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранный артефакт не является нарушением правил.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string banReason = item.TargetRule == "5.01" 
				? string.Format("5.01 (Discord RPC: {0})", item.DetectedArtifact)
				: string.Format("5.08 (Discord сокрытие: {0})", item.DetectedArtifact);
			string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, banReason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n" + cmd, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("📋 **[MCCheatChecker] Судебный отчёт: Discord & RPC Forensics**");
			sb.AppendLine(string.Format("**Проверяемый игрок:** `{0}`", targetPlayerName));
			sb.AppendLine(string.Format("**Штамп проверки:** `{0:yyyy-MM-dd HH:mm:ss}`", DateTime.Now));
			sb.AppendLine(string.Format("**Всего проверено объектов:** `{0}`", allItems.Count));

			int violations = 0;
			foreach (DiscordForensicEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}
			sb.AppendLine(string.Format("**Зафиксировано нарушений:** `{0}`", violations));
			sb.AppendLine();

			if (violations > 0)
			{
				sb.AppendLine("🚨 **ОБНАРУЖЕННЫЕ УЛИКИ И НАРУШЕНИЯ:**");
				foreach (DiscordForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine(string.Format("- **[{0}] {1}**", it.TargetRule, it.DetectedArtifact));
						sb.AppendLine(string.Format("  *Источник:* `{0}`", it.SourcePath));
						sb.AppendLine(string.Format("  *Вердикт:* {0}", it.Verdict));
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Рекомендуемая мера наказания:**");
				sb.AppendLine(string.Format("`/ban {0} 30d 5.01/5.08 (Discord RPC Forensics)`", targetPlayerName));
			}
			else
			{
				sb.AppendLine("✅ **Читов и подозрительных RPC модификаций в Discord не обнаружено.**");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("Discord_RPC_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ru\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<title>Отчёт Discord & RPC Forensics — " + targetPlayerName + "</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("body { background-color: #121418; color: #E0E0E0; font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
			sb.AppendLine("h1 { color: #5865F2; border-bottom: 2px solid #5865F2; padding-bottom: 10px; }");
			sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: bold; margin-right: 8px; }");
			sb.AppendLine(".badge-crit { background: #E74C3C; color: white; }");
			sb.AppendLine(".badge-safe { background: #2ECC71; color: black; }");
			sb.AppendLine(".badge-info { background: #3498DB; color: white; }");
			sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 13px; }");
			sb.AppendLine("th { background-color: #1E222B; color: #5865F2; padding: 10px; text-align: left; border: 1px solid #2C3240; }");
			sb.AppendLine("td { padding: 8px 10px; border: 1px solid #2C3240; }");
			sb.AppendLine("tr:nth-child(even) { background-color: #181B22; }");
			sb.AppendLine(".crit { color: #FF6B6B; font-weight: bold; }");
			sb.AppendLine(".warn { color: #FFA94D; }");
			sb.AppendLine(".safe { color: #69DB7C; }");
			sb.AppendLine("pre { background-color: #0E1014; border: 1px solid #2C3240; padding: 10px; border-radius: 4px; font-family: Consolas, monospace; overflow-x: auto; }");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>💬 Экспертный судебный отчёт: Discord, Rich Presence & RPC Forensics</h1>");
			sb.AppendLine("<p><strong>Проверяемый игрок:</strong> " + targetPlayerName + " | <strong>Дата экспертизы:</strong> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

			int violations = 0;
			foreach (DiscordForensicEvidenceItem it in allItems) if (it.IsViolation) violations++;

			if (violations > 0)
			{
				sb.AppendLine("<div class=\"badge badge-crit\">ОБНАРУЖЕНО " + violations + " КРИТИЧЕСКИХ НАРУШЕНИЙ (5.01 / 5.08)</div>");
			}
			else
			{
				sb.AppendLine("<div class=\"badge badge-safe\">ЧИСТО: НАРУШЕНИЙ DISCORD & RPC НЕ ОБНАРУЖЕНО</div>");
			}

			sb.AppendLine("<table>");
			sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Источник / Путь</th><th>Обнаруженный артефакт</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th><th>Штамп времени</th></tr>");

			foreach (DiscordForensicEvidenceItem it in allItems)
			{
				string colorClass = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
				sb.AppendLine(string.Format("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9:yyyy-MM-dd HH:mm:ss}</td></tr>",
					colorClass, it.Index, it.Category, it.SourcePath, it.DetectedArtifact, it.Status, it.ThreatLevel, it.Verdict, it.TargetRule, it.Timestamp));
			}

			sb.AppendLine("</table>");
			sb.AppendLine("</body></html>");

			try
			{
				File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
				MessageBox.Show("HTML-отчёт успешно экспортирован:\r\n" + sfd.FileName, "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка сохранения отчёта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#region Core Scanning Logic

		private static readonly string[] CheatKeywords = new string[]
		{
			"vape", "vape.gg", "vape v4", "vape lite", "drip.gg", "drip lite", "slinky.gg",
			"celestial", "expensive", "expensive 3.1", "nursultan", "nursultan.fun", "zamorozka",
			"akrien", "wild client", "minced", "wurst", "meteor", "liquidbounce", "sigma",
			"novoline", "rise client", "tenacity", "intent.store", "speclient", "deadcode",
			"rusherhack", "future client", "kamiblue", "lambda client", "catshit", "aristois",
			"neverlose", "gamesense", "fatality", "primordial", "flint", "rockstar", "boberware",
			"thunderhack", "killaura", "triggerbot", "aimbot", "hitbox", "reach"
		};

		private static readonly string[] WhitelistedGamesAndApps = new string[]
		{
			"spotify", "steam", "code", "visual studio", "chrome", "firefox", "opera", "edge",
			"telegram", "obs", "nvidia", "amd", "epic games", "riot client", "blizzard",
			"craftpresence", "simple discord rpc", "lunar client", "badlion client", "feather client"
		};

		public static List<DiscordForensicEvidenceItem> PerformDiscordForensicScan()
		{
			List<DiscordForensicEvidenceItem> results = new List<DiscordForensicEvidenceItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			// 1. Audit Discord Client Installations & Flavors
			string[] discordFlavors = new string[]
			{
				"discord", "discordcanary", "discordptb", "discorddevelopment", "vesktop", "Lightcord", "ArmCord"
			};

			foreach (string flavor in discordFlavors)
			{
				string roamingDir = Path.Combine(appData, flavor);
				string localDir = Path.Combine(localAppData, flavor.Replace("discord", "Discord"));
				if (!Directory.Exists(localDir))
				{
					localDir = Path.Combine(localAppData, flavor);
				}

				if (Directory.Exists(roamingDir) || Directory.Exists(localDir))
				{
					results.Add(new DiscordForensicEvidenceItem
					{
						Index = idx++,
						Category = "Установка Discord",
						SourcePath = Directory.Exists(roamingDir) ? roamingDir : localDir,
						DetectedArtifact = string.Format("Discord клиент: {0}", flavor),
						Status = "УСТАНОВЛЕН",
						ThreatLevel = "INFO",
						Verdict = string.Format("Обнаружена установленная версия Discord ({0}) на рабочей станции.", flavor),
						TargetRule = "РАЗРЕШЕНО",
						IsViolation = false,
						IsWhitelisted = true,
						Timestamp = Directory.Exists(roamingDir) ? Directory.GetLastWriteTime(roamingDir) : DateTime.Now,
						RowColor = Ui.AccentGreen,
						DetailDescription = string.Format("Клиент {0} корректно расположен в каталогах пользователя. Производится криминалистический аудит хранилища LevelDB и подключений к Named Pipe.", flavor)
					});
				}
			}

			// 2. Audit Discord Named Pipes (\\.\pipe\discord-ipc-0..9)
			Dictionary<uint, string> activePids = BuildProcessPidMap();

			for (int p = 0; p < 10; p++)
			{
				string pipePath = string.Format(@"\\.\pipe\discord-ipc-{0}", p);
				try
				{
					IntPtr hPipe = CreateFile(pipePath, FILE_READ_ATTRIBUTES, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
					if (hPipe != IntPtr.Zero && hPipe.ToInt64() != -1)
					{
						uint serverPid = 0;
						uint clientPid = 0;
						GetNamedPipeServerProcessId(hPipe, out serverPid);
						GetNamedPipeClientProcessId(hPipe, out clientPid);
						CloseHandle(hPipe);

						string serverInfo = serverPid > 0 && activePids.ContainsKey(serverPid) ? activePids[serverPid] : string.Format("PID: {0}", serverPid);
						string clientInfo = clientPid > 0 && activePids.ContainsKey(clientPid) ? activePids[clientPid] : (clientPid > 0 ? string.Format("PID: {0}", clientPid) : "Нет подключенных клиентов");

						bool isCheatClient = false;
						string violationRule = "РАЗРЕШЕНО";
						string threat = "INFO";
						Color rowColor = Ui.AccentPurple;
						string verdict = string.Format("Активный канал Discord IPC. Сервер: {0}, Клиент: {1}", serverInfo, clientInfo);

						if (clientPid > 0)
						{
							string lowerClient = clientInfo.ToLowerInvariant();
							if (lowerClient.Contains("javaw") || lowerClient.Contains("java"))
							{
								// Cross-reference with Minecraft cheat presence
								verdict = string.Format("Внимание: процесс Minecraft ({0}) подключен к Discord IPC. Выполняется сверка Rich Presence.", clientInfo);
								threat = "SAFE";
								rowColor = Ui.AccentBlue;
							}
							else
							{
								foreach (string kw in CheatKeywords)
								{
									if (lowerClient.Contains(kw))
									{
										isCheatClient = true;
										violationRule = "5.01";
										threat = "CRITICAL";
										rowColor = Ui.AccentRed;
										verdict = string.Format("🚨 БАН 5.01: К каналу Discord IPC подключен чит-процесс: {0}!", clientInfo);
										break;
									}
								}
							}
						}

						results.Add(new DiscordForensicEvidenceItem
						{
							Index = idx++,
							Category = "Канал Discord IPC",
							SourcePath = pipePath,
							DetectedArtifact = string.Format("discord-ipc-{0} (Сервер: {1})", p, serverInfo),
							Status = clientPid > 0 ? "ПОДКЛЮЧЕН КЛИЕНТ" : "ОЖИДАЕТ КЛИЕНТА",
							ThreatLevel = threat,
							Verdict = verdict,
							TargetRule = violationRule,
							IsViolation = isCheatClient,
							IsWhitelisted = !isCheatClient,
							Timestamp = DateTime.Now,
							RowColor = rowColor,
							ProcessOwner = string.Format("Сервер: {0} | Клиент: {1}", serverInfo, clientInfo),
							DetailDescription = string.Format("Именованный канал Discord IPC используется сторонними приложениями для передачи Rich Presence. Клиентский PID: {0}. Серверный PID: {1}.", clientPid, serverPid)
						});
					}
				}
				catch { }
			}

			// 3. Deep LevelDB & Storage Forensic Inspection
			List<string> levelDbDirs = new List<string>();
			foreach (string flavor in discordFlavors)
			{
				string path = Path.Combine(appData, flavor, "Local Storage", "leveldb");
				if (Directory.Exists(path)) levelDbDirs.Add(path);

				string indexedPath = Path.Combine(appData, flavor, "IndexedDB");
				if (Directory.Exists(indexedPath))
				{
					try
					{
						string[] sub = Directory.GetDirectories(indexedPath, "*discord*.indexeddb.leveldb");
						levelDbDirs.AddRange(sub);
					}
					catch { }
				}
			}

			Regex rpcStateRegex = new Regex("\"(?:state|details|name)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
			Regex cheatInviteRegex = new Regex("discord\\.gg/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);

			foreach (string ldbDir in levelDbDirs)
			{
				string[] files = new string[0];
				try
				{
					files = Directory.GetFiles(ldbDir, "*.*");
				}
				catch { }

				foreach (string file in files)
				{
					string ext = Path.GetExtension(file).ToLowerInvariant();
					if (ext != ".ldb" && ext != ".log" && !Path.GetFileName(file).StartsWith("MANIFEST"))
						continue;

					try
					{
						FileInfo fi = new FileInfo(file);
						if (fi.Length > 25 * 1024 * 1024) continue; // Skip files > 25MB for scan responsiveness

						byte[] bytes;
						using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							bytes = new byte[fs.Length];
							fs.Read(bytes, 0, bytes.Length);
						}

						string content = Encoding.UTF8.GetString(bytes);

						// Scan for cheat keywords in LevelDB
						foreach (string cheat in CheatKeywords)
						{
							int hitIdx = content.IndexOf(cheat, StringComparison.OrdinalIgnoreCase);
							if (hitIdx >= 0)
							{
								// Extract context snippet
								int start = Math.Max(0, hitIdx - 60);
								int len = Math.Min(content.Length - start, 150);
								string snippet = content.Substring(start, len).Replace("\r", " ").Replace("\n", " ");

								// Check if context contains legitimate whitelist
								bool isWhitelisted = false;
								foreach (string white in WhitelistedGamesAndApps)
								{
									if (snippet.IndexOf(white, StringComparison.OrdinalIgnoreCase) >= 0)
									{
										isWhitelisted = true;
										break;
									}
								}

								if (!isWhitelisted)
								{
									results.Add(new DiscordForensicEvidenceItem
									{
										Index = idx++,
										Category = "Активность RPC",
										SourcePath = file,
										DetectedArtifact = string.Format("Чит-активность / RPC: {0}", cheat),
										Status = "ОБНАРУЖЕНО В КЭШЕ",
										ThreatLevel = "CRITICAL",
										Verdict = string.Format("🚨 БАН 5.01: В кэше Discord LevelDB обнаружены следы Rich Presence чита {0}!", cheat),
										TargetRule = "5.01",
										IsViolation = true,
										IsWhitelisted = false,
										Timestamp = fi.LastWriteTime,
										RowColor = Ui.AccentRed,
										RawPayloadOrSnippet = snippet,
										DetailDescription = string.Format("В дисковом файле базы данных Discord ({0}) обнаружена строка '{1}', характерная для Rich Presence чит-клиента. Сниппет: \"{2}\".", Path.GetFileName(file), cheat, snippet)
									});
								}
							}
						}

						// Scan for cheat Discord server invites
						MatchCollection inviteMatches = cheatInviteRegex.Matches(content);
						foreach (Match m in inviteMatches)
						{
							string inviteCode = m.Groups[1].Value.ToLowerInvariant();
							foreach (string cheat in CheatKeywords)
							{
								if (inviteCode.Contains(cheat))
								{
									results.Add(new DiscordForensicEvidenceItem
									{
										Index = idx++,
										Category = "LevelDB Артефакт",
										SourcePath = file,
										DetectedArtifact = string.Format("Инвайт на чит-сервер: discord.gg/{0}", inviteCode),
										Status = "КЭШ ЧАТА",
										ThreatLevel = "WARNING",
										Verdict = string.Format("В кэше Discord найден инвайт на читерский сервер ({0}).", inviteCode),
										TargetRule = "5.01 (Косвенная улика)",
										IsViolation = false,
										IsWhitelisted = false,
										Timestamp = fi.LastWriteTime,
										RowColor = Ui.AccentOrange,
										RawPayloadOrSnippet = m.Value,
										DetailDescription = string.Format("В истории Discord обнаружена ссылка-приглашение discord.gg/{0}, относящаяся к сообществу разработчиков/пользователей читов.", inviteCode)
									});
									break;
								}
							}
						}
					}
					catch { }
				}
			}

			// 4. Audit Modifications & Stealth Plugins (BetterDiscord, Vencord, Vesktop)
			string betterDiscordPlugins = Path.Combine(appData, "BetterDiscord", "plugins");
			if (Directory.Exists(betterDiscordPlugins))
			{
				AuditPluginDirectory(betterDiscordPlugins, "BetterDiscord", ref idx, results);
			}

			string vencordDir = Path.Combine(appData, "Vencord");
			if (Directory.Exists(vencordDir))
			{
				AuditVencordInstallation(vencordDir, ref idx, results);
			}

			string vesktopSettings = Path.Combine(appData, "vesktop", "settings", "settings.json");
			if (File.Exists(vesktopSettings))
			{
				AuditVesktopSettings(vesktopSettings, ref idx, results);
			}

			return results;
		}

		private static void AuditPluginDirectory(string dir, string clientType, ref int idx, List<DiscordForensicEvidenceItem> results)
		{
			try
			{
				string[] plugins = Directory.GetFiles(dir, "*.js");
				foreach (string p in plugins)
				{
					string name = Path.GetFileName(p);
					string lower = name.ToLowerInvariant();
					bool isStealth = lower.Contains("stream") || lower.Contains("bypass") || lower.Contains("stealth") || lower.Contains("screenshare") || lower.Contains("hide");

					results.Add(new DiscordForensicEvidenceItem
					{
						Index = idx++,
						Category = "Модификация клиента",
						SourcePath = p,
						DetectedArtifact = string.Format("{0} Плагин: {1}", clientType, name),
						Status = isStealth ? "ПОДОЗРИТЕЛЬНЫЙ ПЛАГИН" : "УСТАНОВЛЕН",
						ThreatLevel = isStealth ? "CRITICAL" : "WARNING",
						Verdict = isStealth 
							? string.Format("🚨 БАН 5.08: Обнаружен плагин сокрытия экрана / обхода проверки ({0})!", name)
							: string.Format("Установлен сторонний плагин {0} ({1}). Проверьте назначение.", clientType, name),
						TargetRule = isStealth ? "5.08" : "ВНИМАНИЕ",
						IsViolation = isStealth,
						IsWhitelisted = !isStealth,
						Timestamp = File.GetLastWriteTime(p),
						RowColor = isStealth ? Ui.AccentRed : Ui.AccentOrange,
						DetailDescription = string.Format("Плагин {0} в каталоге {1}. Размер: {2} байт.", name, dir, new FileInfo(p).Length)
					});
				}
			}
			catch { }
		}

		private static void AuditVencordInstallation(string vencordDir, ref int idx, List<DiscordForensicEvidenceItem> results)
		{
			bool hasStealthPlugin = false;
			string matchedReason = "";
			try
			{
				string settingsFile = Path.Combine(vencordDir, "settings", "settings.json");
				if (File.Exists(settingsFile))
				{
					string content = File.ReadAllText(settingsFile).ToLowerInvariant();
					if (content.Contains("screensharebypass") || content.Contains("streamerhide") || content.Contains("hidechannels") || (content.Contains("screenshare") && content.Contains("bypass")))
					{
						hasStealthPlugin = true;
						matchedReason = "Обнаружен плагин сокрытия экрана в настройках Vencord";
					}
				}
			}
			catch { }

			results.Add(new DiscordForensicEvidenceItem
			{
				Index = idx++,
				Category = "Модификация клиента",
				SourcePath = vencordDir,
				DetectedArtifact = "Модифицированный клиент: Vencord",
				Status = hasStealthPlugin ? "ОБХОД ЗАПИСИ ЭКРАНА" : "УСТАНОВЛЕН (ЛЕГИТИМЕН)",
				ThreatLevel = hasStealthPlugin ? "CRITICAL" : "INFO",
				Verdict = hasStealthPlugin 
					? ("🚨 БАН 5.08: " + matchedReason) 
					: "Установлен клиент Vencord (легитимная модификация Discord). Вредоносных плагинов обхода не найдено.",
				TargetRule = hasStealthPlugin ? "5.08" : "РАЗРЕШЕНО",
				IsViolation = hasStealthPlugin,
				IsWhitelisted = !hasStealthPlugin,
				Timestamp = Directory.GetLastWriteTime(vencordDir),
				RowColor = hasStealthPlugin ? Ui.AccentRed : Ui.AccentGreen,
				DetailDescription = "Vencord — легитимная пользовательская модификация Discord. Проверено на отсутствие плагинов сокрытия экрана (ScreenShare Bypass)."
			});
		}

		private static void AuditVesktopSettings(string settingsPath, ref int idx, List<DiscordForensicEvidenceItem> results)
		{
			try
			{
				string text = File.ReadAllText(settingsPath);
				bool hasScreenShareBypass = text.IndexOf("screenshare", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("bypass", StringComparison.OrdinalIgnoreCase) >= 0;

				results.Add(new DiscordForensicEvidenceItem
				{
					Index = idx++,
					Category = "Модификация клиента",
					SourcePath = settingsPath,
					DetectedArtifact = "Конфигурация Vesktop (settings.json)",
					Status = hasScreenShareBypass ? "ОБХОД ЗАПИСИ ЭКРАНА" : "АКТИВЕН",
					ThreatLevel = hasScreenShareBypass ? "CRITICAL" : "INFO",
					Verdict = hasScreenShareBypass 
						? "🚨 БАН 5.08: В настройках Vesktop включены плагины обхода захвата экрана!"
						: "Настройки клиента Vesktop проверены. Критических обходов не обнаружено.",
					TargetRule = hasScreenShareBypass ? "5.08" : "РАЗРЕШЕНО",
					IsViolation = hasScreenShareBypass,
					IsWhitelisted = !hasScreenShareBypass,
					Timestamp = File.GetLastWriteTime(settingsPath),
					RowColor = hasScreenShareBypass ? Ui.AccentRed : Ui.AccentGreen,
					DetailDescription = string.Format("Файл конфигурации Vesktop ({0}).", settingsPath)
				});
			}
			catch { }
		}

		private static Dictionary<uint, string> BuildProcessPidMap()
		{
			Dictionary<uint, string> map = new Dictionary<uint, string>();
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					try
					{
						map[(uint)p.Id] = string.Format("{0}.exe (PID: {1})", p.ProcessName, p.Id);
					}
					catch { }
				}
			}
			catch { }
			return map;
		}

		#endregion
	}
}
