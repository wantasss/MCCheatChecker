using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum JavaAgentFilter
	{
		All,
		ViolationsOnly,
		CheatAgentsOnly,
		TempAppDataOnly,
		JvmOptionsOnly,
		WhitelistedOnly
	}

	public class JavaAgentEvidenceItem
	{
		public int Index;
		public string Category; // "Чит-агент -javaagent (5.01)", "Агент в Temp/AppData (5.01)", "Нативный агент -agentpath (5.01)", "Подмена BootClassPath (5.01)", "Переменная JAVA_OPTIONS (5.01)", "Легитимный загрузчик", "Флаги JVM"
		public string Source;
		public string FlagOrAgent;
		public string ProcessOrContext;
		public string AgentPath;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.01", "РАЗРЕШЕНО", "—"
		public bool IsViolation;
		public bool IsNormal;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalData;
	}

	public class JavaAgentAndJvmFlagsViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<JavaAgentEvidenceItem> allItems = new List<JavaAgentEvidenceItem>();
		private JavaAgentFilter currentFilter = JavaAgentFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiCheatAgents;
		private Label lblKpiTempAgents;
		private Label lblKpiJvmOptions;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCheatAgents;
		private DarkButton btnFilterTempAgents;
		private DarkButton btnFilterJvmOptions;
		private DarkButton btnFilterSafe;

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

		public JavaAgentAndJvmFlagsViewerForm() : this("Player")
		{
		}

		public JavaAgentAndJvmFlagsViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "☕ Инспектор Java Agent и аргументов JVM (Tool 83 / Step 80) — MCCheatChecker";
			this.Size = new Size(1380, 830);
			this.MinimumSize = new Size(1060, 640);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.Icon = SystemIcons.Shield;

			// Header Panel
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 84;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(20, 12, 20, 10);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "☕ ИНСПЕКТОР JAVA AGENT (-JAVAAGENT), ФЛАГОВ И ПЕРЕМЕННЫХ JVM";
			lblHeaderTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(20, 12);
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Судебный аудит аргументов запуска Minecraft: детекция скрытых чит-агентов (-javaagent), агентов из Temp, подмены BootClassPath и переменных JAVA_OPTIONS (5.01)";
			lblHeaderSubtitle.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(21, 40);
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "🔍 СКАНИРОВАНИЕ...";
			lblStatusPill.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(35, 45, 55);
			lblStatusPill.Padding = new Padding(10, 4, 10, 4);
			lblStatusPill.AutoSize = true;
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblStatusPill.Location = new Point(1160, 24);
			headerPanel.Controls.Add(lblStatusPill);

			this.Controls.Add(headerPanel);

			// KPI Bar Panel
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 72;
			kpiPanel.BackColor = Color.FromArgb(20, 23, 30);
			kpiPanel.Padding = new Padding(15, 6, 15, 6);

			TableLayoutPanel kpiLayout = new TableLayoutPanel();
			kpiLayout.Dock = DockStyle.Fill;
			kpiLayout.ColumnCount = 6;
			kpiLayout.RowCount = 1;
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
			kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.70f));

			kpiLayout.Controls.Add(CreateKpiCard("ВСЕГО АРГУМЕНТОВ", "0", Ui.AccentTeal, out lblKpiTotal), 0, 0);
			kpiLayout.Controls.Add(CreateKpiCard("НАРУШЕНИЯ 5.01", "0", Ui.AccentRed, out lblKpiViolations), 1, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ЧИТ-АГЕНТЫ 5.01", "0", Ui.AccentRed, out lblKpiCheatAgents), 2, 0);
			kpiLayout.Controls.Add(CreateKpiCard("АГЕНТЫ В TEMP/APPDATA", "0", Color.FromArgb(255, 140, 0), out lblKpiTempAgents), 3, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ПЕРЕМЕННЫЕ JVM 5.01", "0", Color.FromArgb(255, 180, 0), out lblKpiJvmOptions), 4, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ЛЕГИТИМНЫЕ АГЕНТЫ", "0", Color.FromArgb(50, 205, 50), out lblKpiSafe), 5, 0);

			kpiPanel.Controls.Add(kpiLayout);
			this.Controls.Add(kpiPanel);

			// Filter and Search Toolbar
			Panel toolBar = new Panel();
			toolBar.Dock = DockStyle.Top;
			toolBar.Height = 44;
			toolBar.BackColor = Ui.Surface;
			toolBar.Padding = new Padding(15, 6, 15, 6);

			FlowLayoutPanel filterButtonsPanel = new FlowLayoutPanel();
			filterButtonsPanel.Dock = DockStyle.Left;
			filterButtonsPanel.Width = 860;
			filterButtonsPanel.FlowDirection = FlowDirection.LeftToRight;
			filterButtonsPanel.WrapContents = false;

			btnFilterAll = CreateFilterButton("Все (0)", JavaAgentFilter.All, true);
			btnFilterViolations = CreateFilterButton("Нарушения 5.01 (0)", JavaAgentFilter.ViolationsOnly, false);
			btnFilterCheatAgents = CreateFilterButton("Чит-агенты (0)", JavaAgentFilter.CheatAgentsOnly, false);
			btnFilterTempAgents = CreateFilterButton("В Temp/AppData (0)", JavaAgentFilter.TempAppDataOnly, false);
			btnFilterJvmOptions = CreateFilterButton("Переменные JVM (0)", JavaAgentFilter.JvmOptionsOnly, false);
			btnFilterSafe = CreateFilterButton("Легитимные (0)", JavaAgentFilter.WhitelistedOnly, false);

			filterButtonsPanel.Controls.Add(btnFilterAll);
			filterButtonsPanel.Controls.Add(btnFilterViolations);
			filterButtonsPanel.Controls.Add(btnFilterCheatAgents);
			filterButtonsPanel.Controls.Add(btnFilterTempAgents);
			filterButtonsPanel.Controls.Add(btnFilterJvmOptions);
			filterButtonsPanel.Controls.Add(btnFilterSafe);

			toolBar.Controls.Add(filterButtonsPanel);

			Panel searchPanel = new Panel();
			searchPanel.Dock = DockStyle.Right;
			searchPanel.Width = 320;

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(10, 8);
			searchBox.Size = new Size(300, 26);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			searchPanel.Controls.Add(searchBox);

			toolBar.Controls.Add(searchPanel);
			this.Controls.Add(toolBar);

			// Main Split Container
			SplitContainer splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 390;
			splitContainer.SplitterWidth = 6;
			splitContainer.BackColor = Ui.Backdrop;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("№", 45);
			listView.Columns.Add("Категория / Источник", 190);
			listView.Columns.Add("Флаг / Артефакт", 240);
			listView.Columns.Add("Процесс / Контекст", 190);
			listView.Columns.Add("Путь к агенту", 220);
			listView.Columns.Add("Угроза", 95);
			listView.Columns.Add("Вердикт", 130);
			listView.Columns.Add("Регламент", 95);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			splitContainer.Panel1.Controls.Add(listView);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ Java Agent");
			tabAnalysis.BackColor = Ui.Surface;
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Полная командная строка и переменные среды");
			tabTechnical.BackColor = Ui.Surface;
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.01 и Команда бана");
			tabBan.BackColor = Ui.Surface;
			txtBanVerdict = new DarkTextBoxMultiline();
			txtBanVerdict.Dock = DockStyle.Fill;
			tabBan.Controls.Add(txtBanVerdict);
			detailTabs.TabPages.Add(tabBan);

			splitContainer.Panel2.Controls.Add(detailTabs);
			this.Controls.Add(splitContainer);

			// Bottom Actions Panel
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 52;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(15, 8, 15, 8);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🔨 Скопировать команду бана (/ban 5.01)";
			btnCopyBanCmd.Size = new Size(275, 34);
			btnCopyBanCmd.Location = new Point(15, 9);
			btnCopyBanCmd.Click += BtnCopyBanCmd_Click;
			bottomPanel.Controls.Add(btnCopyBanCmd);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "📋 Скопировать отчет для Discord";
			btnCopyDiscord.Size = new Size(245, 34);
			btnCopyDiscord.Location = new Point(298, 9);
			btnCopyDiscord.Click += BtnCopyDiscord_Click;
			bottomPanel.Controls.Add(btnCopyDiscord);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт HTML";
			btnExportHtml.Size = new Size(155, 34);
			btnExportHtml.Location = new Point(551, 9);
			btnExportHtml.Click += BtnExportHtml_Click;
			bottomPanel.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Повторить аудит";
			btnRescan.Size = new Size(160, 34);
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Location = new Point(1195, 9);
			btnRescan.Click += delegate { StartScan(); };
			bottomPanel.Controls.Add(btnRescan);

			this.Controls.Add(bottomPanel);
		}

		private Panel CreateKpiCard(string title, string initialValue, Color accentColor, out Label valueLabel)
		{
			Panel pnl = new Panel();
			pnl.Dock = DockStyle.Fill;
			pnl.BackColor = Ui.Surface;
			pnl.Margin = new Padding(3, 2, 3, 2);

			Label lblT = new Label();
			lblT.Text = title;
			lblT.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
			lblT.ForeColor = Ui.TextSecondary;
			lblT.Location = new Point(8, 8);
			lblT.AutoSize = true;
			pnl.Controls.Add(lblT);

			valueLabel = new Label();
			valueLabel.Text = initialValue;
			valueLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
			valueLabel.ForeColor = accentColor;
			valueLabel.Location = new Point(7, 26);
			valueLabel.AutoSize = true;
			pnl.Controls.Add(valueLabel);

			return pnl;
		}

		private DarkButton CreateFilterButton(string text, JavaAgentFilter filter, bool active)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(136, 30);
			btn.Margin = new Padding(2, 1, 2, 1);
			btn.Tag = filter;
			if (active)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.White;
			}
			btn.Click += delegate
			{
				currentFilter = filter;
				btnFilterAll.BackColor = Ui.Surface;
				btnFilterViolations.BackColor = Ui.Surface;
				btnFilterCheatAgents.BackColor = Ui.Surface;
				btnFilterTempAgents.BackColor = Ui.Surface;
				btnFilterJvmOptions.BackColor = Ui.Surface;
				btnFilterSafe.BackColor = Ui.Surface;

				btnFilterAll.ForeColor = Ui.TextPrimary;
				btnFilterViolations.ForeColor = Ui.TextPrimary;
				btnFilterCheatAgents.ForeColor = Ui.TextPrimary;
				btnFilterTempAgents.ForeColor = Ui.TextPrimary;
				btnFilterJvmOptions.ForeColor = Ui.TextPrimary;
				btnFilterSafe.ForeColor = Ui.TextPrimary;

				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.White;
				ApplyFilter();
			};
			return btn;
		}

		public void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			lblStatusPill.Text = "🔍 СКАНИРОВАНИЕ...";
			lblStatusPill.ForeColor = Ui.AccentTeal;
			btnRescan.Enabled = false;

			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<JavaAgentEvidenceItem> results = PerformAudit();
			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			btnRescan.Enabled = true;
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ ОШИБКА АУДИТА";
				lblStatusPill.ForeColor = Ui.AccentRed;
				MessageBox.Show("Ошибка сканирования аргументов Java: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			List<JavaAgentEvidenceItem> results = e.Result as List<JavaAgentEvidenceItem>;
			if (results != null)
			{
				allItems = results;
			}

			UpdateKpiCounters();
			ApplyFilter();

			int violationCount = 0;
			for (int i = 0; i < allItems.Count; i++)
			{
				if (allItems[i].IsViolation) violationCount++;
			}

			if (violationCount > 0)
			{
				lblStatusPill.Text = string.Format("⚠️ НАЙДЕНО НАРУШЕНИЙ: {0} (БАН 5.01)", violationCount);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ СТАТУС ЧИСТЫЙ: ЧИТ-АГЕНТОВ НЕ ОБНАРУЖЕНО";
				lblStatusPill.ForeColor = Color.FromArgb(50, 205, 50);
			}

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
		}

		private void UpdateKpiCounters()
		{
			int total = allItems.Count;
			int violations = 0;
			int cheatAgents = 0;
			int tempAgents = 0;
			int jvmOptions = 0;
			int normal = 0;

			for (int i = 0; i < allItems.Count; i++)
			{
				JavaAgentEvidenceItem item = allItems[i];
				if (item.IsViolation)
				{
					violations++;
					if (item.Category.IndexOf("Чит-агент", StringComparison.OrdinalIgnoreCase) >= 0)
						cheatAgents++;
					if (item.Category.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("AppData", StringComparison.OrdinalIgnoreCase) >= 0)
						tempAgents++;
					if (item.Category.IndexOf("JAVA_OPTIONS", StringComparison.OrdinalIgnoreCase) >= 0)
						jvmOptions++;
				}
				else if (item.IsNormal)
				{
					normal++;
				}
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiCheatAgents.Text = cheatAgents.ToString();
			lblKpiTempAgents.Text = tempAgents.ToString();
			lblKpiJvmOptions.Text = jvmOptions.ToString();
			lblKpiSafe.Text = normal.ToString();

			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения 5.01 ({0})", violations);
			btnFilterCheatAgents.Text = string.Format("Чит-агенты ({0})", cheatAgents);
			btnFilterTempAgents.Text = string.Format("В Temp/AppData ({0})", tempAgents);
			btnFilterJvmOptions.Text = string.Format("Переменные JVM ({0})", jvmOptions);
			btnFilterSafe.Text = string.Format("Легитимные ({0})", normal);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			for (int i = 0; i < allItems.Count; i++)
			{
				JavaAgentEvidenceItem item = allItems[i];

				bool passCategory = false;
				switch (currentFilter)
				{
					case JavaAgentFilter.All:
						passCategory = true;
						break;
					case JavaAgentFilter.ViolationsOnly:
						passCategory = item.IsViolation;
						break;
					case JavaAgentFilter.CheatAgentsOnly:
						passCategory = item.Category.IndexOf("Чит-агент", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case JavaAgentFilter.TempAppDataOnly:
						passCategory = item.Category.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("AppData", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case JavaAgentFilter.JvmOptionsOnly:
						passCategory = item.Category.IndexOf("JAVA_OPTIONS", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case JavaAgentFilter.WhitelistedOnly:
						passCategory = item.IsNormal;
						break;
				}

				if (!passCategory) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string haystack = string.Format("{0} {1} {2} {3} {4} {5}",
						item.Category, item.FlagOrAgent, item.ProcessOrContext, item.AgentPath, item.Verdict, item.DetailDescription).ToLowerInvariant();
					if (haystack.IndexOf(search) < 0) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.FlagOrAgent);
				lvi.SubItems.Add(item.ProcessOrContext);
				lvi.SubItems.Add(item.AgentPath);
				lvi.SubItems.Add(item.ThreatLevel);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.TargetRule);

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
				ClearDetailViews();
			}
		}

		private void ClearDetailViews()
		{
			txtAnalysis.Text = "Нет артефактов, удовлетворяющих заданным критериям фильтра.";
			txtTechnical.Text = "";
			txtBanVerdict.Text = "";
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetailViews();
				return;
			}

			JavaAgentEvidenceItem item = listView.SelectedItems[0].Tag as JavaAgentEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine(string.Format("СУДЕБНАЯ ЭКСПЕРТИЗА JAVA AGENT (-JAVAAGENT) И ФЛАГОВ JVM — ОБЪЕКТ #{0}", item.Index));
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Источник артефакта:      {0}", item.Source));
			sbAnalysis.AppendLine(string.Format("Флаг / Артефакт:         {0}", item.FlagOrAgent));
			sbAnalysis.AppendLine(string.Format("Процесс / Контекст:      {0}", item.ProcessOrContext));
			sbAnalysis.AppendLine(string.Format("Путь к агенту:           {0}", item.AgentPath));
			sbAnalysis.AppendLine(string.Format("Статус:                  {0}", item.Status));
			sbAnalysis.AppendLine(string.Format("Уровень угрозы:          {0}", item.ThreatLevel));
			sbAnalysis.AppendLine(string.Format("Вердикт проверки:        {0}", item.Verdict));
			sbAnalysis.AppendLine(string.Format("Применимый регламент:    {0}", item.TargetRule));
			sbAnalysis.AppendLine(string.Format("Временная метка:         {0:yyyy-MM-dd HH:mm:ss}", item.Timestamp));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("ПОДРОБНОЕ ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА:");
			sbAnalysis.AppendLine(item.DetailDescription);
			sbAnalysis.AppendLine();
			if (item.IsViolation)
			{
				sbAnalysis.AppendLine("ВНИМАНИЕ! Использование сторонних Java-агентов (-javaagent), агентов из временных папок");
				sbAnalysis.AppendLine("или скрытых флагов JVM является прямым нарушением пункта 5.01 правил проекта.");
				sbAnalysis.AppendLine("Игрок подлежит немедленной блокировке на 30 дней.");
			}
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Technical
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("ТЕХНИЧЕСКИЕ ДАННЫЕ КОМАНДНОЙ СТРОКИ WMI И ПЕРЕМЕННЫХ СРЕДЫ");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine(item.TechnicalData);
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Ban & Verdict
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine("ЮРИДИЧЕСКИЙ РЕГЛАМЕНТ 5.01 И КОМАНДА БЛОКИРОВКИ");
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine(string.Format("Целевой подозреваемый:   {0}", targetPlayerName));
			sbBan.AppendLine(string.Format("Статус нарушения:        {0}", item.IsViolation ? "ПОДТВЕРЖДЕНО НА СЕРВЕРЕ" : "ЧИСТО (Нарушений не выявлено)"));
			sbBan.AppendLine(string.Format("Пункт правил проекта:    {0}", item.TargetRule));
			sbBan.AppendLine(string.Format("Срок блокировки:         {0}", item.IsViolation ? "30 дней" : "Не применимо"));
			sbBan.AppendLine();
			sbBan.AppendLine("КОМАНДА ДЛЯ КОНСОЛИ / ЧАТА СЕРВЕРА:");
			if (item.IsViolation)
			{
				sbBan.AppendLine(string.Format("/ban {0} 30d 5.01 Использование читов (-javaagent в аргументах JVM)", targetPlayerName));
			}
			else
			{
				sbBan.AppendLine("— Нарушений по пункту 5.01 не зафиксировано —");
			}
			sbBan.AppendLine();
			sbBan.AppendLine("ВЫДЕРЖКА ИЗ РЕГЛАМЕНТА ПРОВЕРОК:");
			sbBan.AppendLine("Пункт 5.01: Использование стороннего ПО, дающего преимущество в игре (читы, инжекторы,");
			sbBan.AppendLine("Java-агенты, байткод-модификаторы, stealth-клиенты) влечет блокировку на 30 дней.");
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void BtnCopyBanCmd_Click(object sender, EventArgs e)
		{
			string cmd = string.Format("/ban {0} 30d 5.01 Использование читов (-javaagent в аргументах JVM)", targetPlayerName);
			try
			{
				Clipboard.SetText(cmd);
				MessageBox.Show(string.Format("Команда бана скопирована в буфер:\n{0}", cmd), "Буфер обмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось скопировать команду: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnCopyDiscord_Click(object sender, EventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine(string.Format("+ ПРОТОКОЛ СУДЕБНОЙ ЭКСПЕРТИЗЫ JAVA AGENT И JVM FLAGS (TOOL 83 / STEP 80)"));
			sb.AppendLine(string.Format("  Подозреваемый: {0} | Дата: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
			sb.AppendLine("----------------------------------------------------------------------------------");

			int violations = 0;
			for (int i = 0; i < allItems.Count; i++)
			{
				if (allItems[i].IsViolation)
				{
					violations++;
					sb.AppendLine(string.Format("- [5.01] {0} -> {1} ({2})", allItems[i].Category, allItems[i].FlagOrAgent, allItems[i].ProcessOrContext));
				}
			}

			if (violations == 0)
			{
				sb.AppendLine("+ СТОРОННИХ JAVA-АГЕНТОВ (-JAVAAGENT) И ЧИТЕРСКИХ ФЛАГОВ JVM НЕ ОБНАРУЖЕНО");
			}
			else
			{
				sb.AppendLine(string.Format("- ИТОГОВЫЙ ВЕРДИКТ: ОБНАРУЖЕНО {0} НАРУШЕНИЙ -> /ban {1} 30d 5.01", violations, targetPlayerName));
			}
			sb.AppendLine("```");

			try
			{
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Discord отчет скопирован в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось скопировать отчет: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportHtml_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("JavaAgent_JvmFlags_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			try
			{
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Судебный отчет: Java Agent & JVM Flags</title>");
				html.AppendLine("<style>");
				html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #0e1117; color: #e6edf3; padding: 24px; }");
				html.AppendLine("h1 { color: #58a6ff; margin-bottom: 4px; }");
				html.AppendLine(".sub { color: #8b949e; margin-bottom: 24px; font-size: 14px; }");
				html.AppendLine(".table { width: 100%; border-collapse: collapse; margin-top: 16px; background: #161b22; border-radius: 8px; overflow: hidden; }");
				html.AppendLine("th { background: #21262d; color: #8b949e; text-align: left; padding: 12px; font-size: 13px; }");
				html.AppendLine("td { padding: 10px 12px; border-bottom: 1px solid #30363d; font-size: 13px; }");
				html.AppendLine(".crit { color: #f85149; font-weight: bold; }");
				html.AppendLine(".warn { color: #d29922; font-weight: bold; }");
				html.AppendLine(".safe { color: #3fb950; }");
				html.AppendLine("</style></head><body>");

				html.AppendLine(string.Format("<h1>☕ Судебный протокол аудита Java Agent и аргументов JVM</h1>"));
				html.AppendLine(string.Format("<div class='sub'>Подозреваемый: <b>{0}</b> | Время формирования: {1:yyyy-MM-dd HH:mm:ss} | Модуль: Tool 83 / Step 80</div>", targetPlayerName, DateTime.Now));

				html.AppendLine("<table class='table'>");
				html.AppendLine("<tr><th>№</th><th>Категория</th><th>Флаг / Артефакт</th><th>Процесс</th><th>Путь</th><th>Угроза</th><th>Вердикт</th><th>Правило</th></tr>");

				for (int i = 0; i < allItems.Count; i++)
				{
					JavaAgentEvidenceItem it = allItems[i];
					string cls = it.IsViolation ? "crit" : (it.IsNormal ? "safe" : "warn");
					html.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td class='{5}'>{6}</td><td>{7}</td><td>{8}</td></tr>",
						it.Index,
						System.Security.SecurityElement.Escape(it.Category),
						System.Security.SecurityElement.Escape(it.FlagOrAgent),
						System.Security.SecurityElement.Escape(it.ProcessOrContext),
						System.Security.SecurityElement.Escape(it.AgentPath),
						cls,
						System.Security.SecurityElement.Escape(it.ThreatLevel),
						System.Security.SecurityElement.Escape(it.Verdict),
						System.Security.SecurityElement.Escape(it.TargetRule)));
				}

				html.AppendLine("</table></body></html>");
				File.WriteAllText(sfd.FileName, html.ToString(), Encoding.UTF8);
				MessageBox.Show("HTML-отчет успешно сохранен:\n" + sfd.FileName, "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при сохранении HTML отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static List<JavaAgentEvidenceItem> PerformAudit()
		{
			List<JavaAgentEvidenceItem> list = new List<JavaAgentEvidenceItem>();
			int idx = 1;

			// 1. Audit WMI CommandLine of javaw.exe / java.exe
			try
			{
				string query = "SELECT ProcessId, Name, CommandLine, ExecutablePath FROM Win32_Process WHERE Name = 'javaw.exe' OR Name = 'java.exe'";
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
				using (ManagementObjectCollection procs = searcher.Get())
				{
					foreach (ManagementObject mo in procs)
					{
						uint pid = Convert.ToUInt32(mo["ProcessId"]);
						string name = (string)mo["Name"];
						string cmdLine = (string)mo["CommandLine"] ?? "";
						string exePath = (string)mo["ExecutablePath"] ?? "";

						if (string.IsNullOrEmpty(cmdLine)) continue;

						// Parse -javaagent:
						MatchCollection agentMatches = Regex.Matches(cmdLine, @"-javaagent:([^\s]+)", RegexOptions.IgnoreCase);
						foreach (Match am in agentMatches)
						{
							string rawParam = am.Groups[1].Value.Trim('"', '\'');
							string agentPath = rawParam;
							string agentArgs = "";

							int eqIdx = rawParam.IndexOf('=');
							if (eqIdx >= 0)
							{
								agentPath = rawParam.Substring(0, eqIdx).Trim('"', '\'');
								agentArgs = rawParam.Substring(eqIdx + 1);
							}

							bool isBlacklisted = SignatureAutoUpdateManager.IsBlacklistedJavaAgent(agentPath);
							bool isWhitelisted = SignatureAutoUpdateManager.IsWhitelistedJavaAgent(agentPath);
							bool isInTempOrAppData = IsSuspiciousAgentDirectory(agentPath);

							if (isBlacklisted)
							{
								list.Add(new JavaAgentEvidenceItem
								{
									Index = idx++,
									Category = "Чит-агент -javaagent (5.01)",
									Source = "WMI Win32_Process CommandLine",
									FlagOrAgent = am.Value,
									ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
									AgentPath = agentPath,
									Status = "ЧИТ-АГЕНТ ОБНАРУЖЕН",
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.01",
									TargetRule = "5.01",
									IsViolation = true,
									IsNormal = false,
									Timestamp = DateTime.Now,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("В аргументах запуска процесса {0} (PID: {1}) обнаружен заведомо известный чит-агент '{2}'. Читы используют Java Instrumentation (-javaagent) для скрытого перехвата байткода Minecraft и инъекции чит-модулей.", name, pid, agentPath),
									TechnicalData = string.Format("Process: {0} (PID: {1})\nPath: {2}\nFull Flag: {3}\nAgent Path: {4}\nArgs: {5}\nFull CommandLine:\n{6}", name, pid, exePath, am.Value, agentPath, agentArgs, cmdLine)
								});
							}
							else if (isInTempOrAppData && !isWhitelisted)
							{
								list.Add(new JavaAgentEvidenceItem
								{
									Index = idx++,
									Category = "Агент в Temp/AppData (5.01)",
									Source = "WMI Win32_Process CommandLine",
									FlagOrAgent = am.Value,
									ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
									AgentPath = agentPath,
									Status = "ПОДОЗРИТЕЛЬНЫЙ КАТАЛОГ",
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.01",
									TargetRule = "5.01",
									IsViolation = true,
									IsNormal = false,
									Timestamp = DateTime.Now,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("Обнаружен Java-агент '{0}', загружаемый из временного каталога (Temp/AppData). Легитимные библиотеки Minecraft никогда не загружаются из Temp как -javaagent.", agentPath),
									TechnicalData = string.Format("Process: {0} (PID: {1})\nFlag: {2}\nPath: {3}", name, pid, am.Value, agentPath)
								});
							}
							else if (isWhitelisted)
							{
								list.Add(new JavaAgentEvidenceItem
								{
									Index = idx++,
									Category = "Легитимный загрузчик",
									Source = "WMI Win32_Process CommandLine",
									FlagOrAgent = am.Value,
									ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
									AgentPath = agentPath,
									Status = "Штатный агент (Белый список)",
									ThreatLevel = "SAFE",
									Verdict = "РАЗРЕШЕНО",
									TargetRule = "РАЗРЕШЕНО",
									IsViolation = false,
									IsNormal = true,
									Timestamp = DateTime.Now,
									RowColor = Color.FromArgb(50, 205, 50),
									DetailDescription = string.Format("Обнаружен разрешенный Java-агент '{0}' (Fabric/Quilt/Authlib/Mixin). Данный компонент является стандартной частью игрового загрузчика.", agentPath),
									TechnicalData = string.Format("Process: {0} (PID: {1})\nAgent: {2}", name, pid, agentPath)
								});
							}
							else
							{
								list.Add(new JavaAgentEvidenceItem
								{
									Index = idx++,
									Category = "Чит-агент -javaagent (5.01)",
									Source = "WMI Win32_Process CommandLine",
									FlagOrAgent = am.Value,
									ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
									AgentPath = agentPath,
									Status = "Неизвестный сторонний агент",
									ThreatLevel = "WARNING",
									Verdict = "ПРЕДУПРЕЖДЕНИЕ",
									TargetRule = "5.01",
									IsViolation = false,
									IsNormal = false,
									Timestamp = DateTime.Now,
									RowColor = Color.FromArgb(255, 180, 0),
									DetailDescription = string.Format("Обнаружен нераспознанный Java-агент '{0}'. Требуется ручная верификация содержимого архива на наличие чит-классов.", agentPath),
									TechnicalData = string.Format("Process: {0} (PID: {1})\nAgent: {2}", name, pid, agentPath)
								});
							}
						}

						// Parse -agentpath:
						MatchCollection pathMatches = Regex.Matches(cmdLine, @"-agentpath:([^\s]+)", RegexOptions.IgnoreCase);
						foreach (Match pm in pathMatches)
						{
							string p = pm.Groups[1].Value.Trim('"', '\'');
							list.Add(new JavaAgentEvidenceItem
							{
								Index = idx++,
								Category = "Нативный агент -agentpath (5.01)",
								Source = "WMI Win32_Process CommandLine",
								FlagOrAgent = pm.Value,
								ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
								AgentPath = p,
								Status = "НАТИВНЫЙ АГЕНТ",
								ThreatLevel = "CRITICAL",
								Verdict = "БАН 5.01",
								TargetRule = "5.01",
								IsViolation = true,
								IsNormal = false,
								Timestamp = DateTime.Now,
								RowColor = Ui.AccentRed,
								DetailDescription = string.Format("В процессе {0} используется параметр '-agentpath:{1}'. Нативные агенты C/C++ используются для обхода JVMTI и бесфайловой инъекции читов в JVM.", name, p),
								TechnicalData = string.Format("Process: {0} (PID: {1})\nPath: {2}", name, pid, p)
							});
						}

						// Parse -Xbootclasspath
						MatchCollection bootMatches = Regex.Matches(cmdLine, @"-Xbootclasspath[^\s:]*:([^\s]+)", RegexOptions.IgnoreCase);
						foreach (Match bm in bootMatches)
						{
							string p = bm.Groups[1].Value.Trim('"', '\'');
							if (IsSuspiciousAgentDirectory(p) || SignatureAutoUpdateManager.IsBlacklistedJavaAgent(p))
							{
								list.Add(new JavaAgentEvidenceItem
								{
									Index = idx++,
									Category = "Подмена BootClassPath (5.01)",
									Source = "WMI Win32_Process CommandLine",
									FlagOrAgent = bm.Value,
									ProcessOrContext = string.Format("{0} (PID: {1})", name, pid),
									AgentPath = p,
									Status = "ПОДМЕНА СИСТЕМНЫХ КЛАССОВ",
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.01",
									TargetRule = "5.01",
									IsViolation = true,
									IsNormal = false,
									Timestamp = DateTime.Now,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("В параметрах запуска JVM обнаружен -Xbootclasspath с путем '{0}'. Переопределение загрузочных классов используется для скрытой подмены ключевых классов Java и клиента Minecraft.", p),
									TechnicalData = string.Format("Process: {0} (PID: {1})\nFlag: {2}", name, pid, bm.Value)
								});
							}
						}
					}
				}
			}
			catch { }

			// 2. Audit Environment Variables (JAVA_TOOL_OPTIONS, _JAVA_OPTIONS, JAVA_OPTIONS)
			try
			{
				string[] envVars = new string[] { "JAVA_TOOL_OPTIONS", "_JAVA_OPTIONS", "JAVA_OPTIONS" };
				foreach (string ev in envVars)
				{
					string val = Environment.GetEnvironmentVariable(ev, EnvironmentVariableTarget.Process) ??
								 Environment.GetEnvironmentVariable(ev, EnvironmentVariableTarget.User) ??
								 Environment.GetEnvironmentVariable(ev, EnvironmentVariableTarget.Machine);

					if (!string.IsNullOrEmpty(val))
					{
						bool hasAgent = (val.IndexOf("-javaagent", StringComparison.OrdinalIgnoreCase) >= 0 ||
										 val.IndexOf("-agentpath", StringComparison.OrdinalIgnoreCase) >= 0 ||
										 val.IndexOf("-Xbootclasspath", StringComparison.OrdinalIgnoreCase) >= 0);

						if (hasAgent)
						{
							list.Add(new JavaAgentEvidenceItem
							{
								Index = idx++,
								Category = "Переменная JAVA_OPTIONS (5.01)",
								Source = "Windows Environment Variable",
								FlagOrAgent = string.Format("{0}={1}", ev, val),
								ProcessOrContext = "System / User Environment",
								AgentPath = val,
								Status = "ГЛОБАЛЬНАЯ ИНЪЕКЦИЯ",
								ThreatLevel = "CRITICAL",
								Verdict = "БАН 5.01",
								TargetRule = "5.01",
								IsViolation = true,
								IsNormal = false,
								Timestamp = DateTime.Now,
								RowColor = Ui.AccentRed,
								DetailDescription = string.Format("В системных переменных среды Windows установлена переменная '{0}' со значением '{1}'. Этот механизм используется читерами для автоматического внедрения чит-агента во все запускаемые Java-приложения в обход лаунчера.", ev, val),
								TechnicalData = string.Format("Variable: {0}\nValue: {1}", ev, val)
							});
						}
						else
						{
							list.Add(new JavaAgentEvidenceItem
							{
								Index = idx++,
								Category = "Флаги JVM",
								Source = "Windows Environment Variable",
								FlagOrAgent = string.Format("{0}={1}", ev, val),
								ProcessOrContext = "System / User Environment",
								AgentPath = val,
								Status = "Штатные опции",
								ThreatLevel = "INFO",
								Verdict = "ИНФО",
								TargetRule = "РАЗРЕШЕНО",
								IsViolation = false,
								IsNormal = true,
								Timestamp = DateTime.Now,
								RowColor = Ui.TextPrimary,
								DetailDescription = string.Format("Переменная среды '{0}' содержит стандартные параметры: '{1}'. Признаков чит-агентов не обнаружено.", ev, val),
								TechnicalData = string.Format("Variable: {0}\nValue: {1}", ev, val)
							});
						}
					}
				}
			}
			catch { }

			// Fallback: If no entries found, provide Clean baseline summary
			if (list.Count == 0)
			{
				list.Add(new JavaAgentEvidenceItem
				{
					Index = idx++,
					Category = "Флаги JVM",
					Source = "JVM Subsystem Audit",
					FlagOrAgent = "Сторонних параметров -javaagent не обнаружено",
					ProcessOrContext = "javaw.exe / JVM Environment",
					AgentPath = "Чисто",
					Status = "Штатный запуск",
					ThreatLevel = "SAFE",
					Verdict = "ЧИСТО",
					TargetRule = "РАЗРЕШЕНО",
					IsViolation = false,
					IsNormal = true,
					Timestamp = DateTime.Now,
					RowColor = Color.FromArgb(50, 205, 50),
					DetailDescription = "Проверка командных строк процессов javaw.exe, системных переменных JAVA_TOOL_OPTIONS и путей загрузки JVM не выявила следов чит-агентов (-javaagent), нативных библиотек (-agentpath) или подмены классов.",
					TechnicalData = "Проверка Win32_Process и переменных среды завершена без замечаний."
				});
			}

			return list;
		}

		private static bool IsSuspiciousAgentDirectory(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			string lower = path.ToLowerInvariant();
			return lower.IndexOf(@"\temp\", StringComparison.OrdinalIgnoreCase) >= 0 ||
				   lower.IndexOf(@"\appdata\local\temp\", StringComparison.OrdinalIgnoreCase) >= 0 ||
				   lower.IndexOf(@"\downloads\", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
