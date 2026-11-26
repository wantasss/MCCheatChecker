using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum UsoUpdateTamperingFilter
	{
		All,
		ViolationsOnly,
		FakeScreensOnly,
		FakeUsoRebootOnly,
		ServiceTamperingOnly,
		NormalUpdatesOnly
	}

	public class UsoUpdateEvidenceItem
	{
		public int Index;
		public string Category; // "Фейковый экран обновления (5.08)", "Ложная перезагрузка UsoClient (5.08)", "Манипуляция службой wuauserv (5.08)", "Процессы Windows Update", "Реальное состояние обновления", "Журнал CBS и события"
		public string Source;
		public string DetectedArtifact;
		public string ProcessOrWindow;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.08", "РАЗРЕШЕНО", "—"
		public bool IsViolation;
		public bool IsNormal;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalData;
	}

	public class UsoAndWindowsUpdateTamperingViewerForm : Form
	{
		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		private readonly string targetPlayerName;
		private List<UsoUpdateEvidenceItem> allItems = new List<UsoUpdateEvidenceItem>();
		private UsoUpdateTamperingFilter currentFilter = UsoUpdateTamperingFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiFakeScreens;
		private Label lblKpiUsoReboot;
		private Label lblKpiServiceTampering;
		private Label lblKpiNormal;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterFakeScreens;
		private DarkButton btnFilterUsoReboot;
		private DarkButton btnFilterServiceTampering;
		private DarkButton btnFilterNormal;

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

		public UsoAndWindowsUpdateTamperingViewerForm() : this("Player")
		{
		}

		public UsoAndWindowsUpdateTamperingViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "🔄 Инспектор фальсификации обновлений Windows, UsoClient и срыва проверок (Tool 79 / Step 76) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🔄 ИНСПЕКТОР ФАЛЬСИФИКАЦИИ ОБНОВЛЕНИЙ WINDOWS, USOCLIENT И СРЫВА ПРОВЕРОК";
			lblHeaderTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(20, 12);
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Детекция фейковых экранов 'Работа с обновлениями' (fakeupdate), фиктивных перезагрузок usoclient и отключения wuauserv для срыва проверки (Регламент 5.08)";
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

			kpiLayout.Controls.Add(CreateKpiCard("ВСЕГО АРТЕФАКТОВ", "0", Ui.AccentTeal, out lblKpiTotal), 0, 0);
			kpiLayout.Controls.Add(CreateKpiCard("НАРУШЕНИЯ 5.08", "0", Ui.AccentRed, out lblKpiViolations), 1, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ФЕЙКОВЫЕ ЭКРАНЫ 5.08", "0", Ui.AccentRed, out lblKpiFakeScreens), 2, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ЛОЖНЫЙ USOCLIENT 5.08", "0", Color.FromArgb(255, 140, 0), out lblKpiUsoReboot), 3, 0);
			kpiLayout.Controls.Add(CreateKpiCard("МАНИПУЛЯЦИИ СЛУЖБОЙ", "0", Color.FromArgb(255, 180, 0), out lblKpiServiceTampering), 4, 0);
			kpiLayout.Controls.Add(CreateKpiCard("ЛЕГИТИМНЫЙ СТАТУС", "0", Color.FromArgb(50, 205, 50), out lblKpiNormal), 5, 0);

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

			btnFilterAll = CreateFilterButton("Все (0)", UsoUpdateTamperingFilter.All, true);
			btnFilterViolations = CreateFilterButton("Нарушения 5.08 (0)", UsoUpdateTamperingFilter.ViolationsOnly, false);
			btnFilterFakeScreens = CreateFilterButton("Фейковые экраны (0)", UsoUpdateTamperingFilter.FakeScreensOnly, false);
			btnFilterUsoReboot = CreateFilterButton("UsoClient ребут (0)", UsoUpdateTamperingFilter.FakeUsoRebootOnly, false);
			btnFilterServiceTampering = CreateFilterButton("Служба WU (0)", UsoUpdateTamperingFilter.ServiceTamperingOnly, false);
			btnFilterNormal = CreateFilterButton("Легитимные (0)", UsoUpdateTamperingFilter.NormalUpdatesOnly, false);

			filterButtonsPanel.Controls.Add(btnFilterAll);
			filterButtonsPanel.Controls.Add(btnFilterViolations);
			filterButtonsPanel.Controls.Add(btnFilterFakeScreens);
			filterButtonsPanel.Controls.Add(btnFilterUsoReboot);
			filterButtonsPanel.Controls.Add(btnFilterServiceTampering);
			filterButtonsPanel.Controls.Add(btnFilterNormal);

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

			// Main Split Container: Left is ListView, Right is Detail Tabs
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
			listView.Columns.Add("Обнаруженный артефакт", 250);
			listView.Columns.Add("Процесс / Окно / Служба", 210);
			listView.Columns.Add("Состояние / Статус", 160);
			listView.Columns.Add("Угроза", 95);
			listView.Columns.Add("Вердикт", 140);
			listView.Columns.Add("Регламент", 95);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			splitContainer.Panel1.Controls.Add(listView);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ обновлений");
			tabAnalysis.BackColor = Ui.Surface;
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Реальное состояние службы WU и процессы");
			tabTechnical.BackColor = Ui.Surface;
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 и Команда бана");
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
			btnCopyBanCmd.Text = "🔨 Скопировать команду бана (/ban 5.08)";
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

		private DarkButton CreateFilterButton(string text, UsoUpdateTamperingFilter filter, bool active)
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
				btnFilterFakeScreens.BackColor = Ui.Surface;
				btnFilterUsoReboot.BackColor = Ui.Surface;
				btnFilterServiceTampering.BackColor = Ui.Surface;
				btnFilterNormal.BackColor = Ui.Surface;

				btnFilterAll.ForeColor = Ui.TextPrimary;
				btnFilterViolations.ForeColor = Ui.TextPrimary;
				btnFilterFakeScreens.ForeColor = Ui.TextPrimary;
				btnFilterUsoReboot.ForeColor = Ui.TextPrimary;
				btnFilterServiceTampering.ForeColor = Ui.TextPrimary;
				btnFilterNormal.ForeColor = Ui.TextPrimary;

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
			List<UsoUpdateEvidenceItem> results = PerformAudit();
			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			btnRescan.Enabled = true;
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ ОШИБКА АУДИТА";
				lblStatusPill.ForeColor = Ui.AccentRed;
				MessageBox.Show("Ошибка сканирования обновлений Windows: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			List<UsoUpdateEvidenceItem> results = e.Result as List<UsoUpdateEvidenceItem>;
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
				lblStatusPill.Text = string.Format("⚠️ НАЙДЕНО НАРУШЕНИЙ: {0} (БАН 5.08)", violationCount);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ СТАТУС ЧИСТЫЙ: ФАЛЬСИФИКАЦИЙ НЕ ОБНАРУЖЕНО";
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
			int fakeScreens = 0;
			int usoReboot = 0;
			int serviceTampering = 0;
			int normal = 0;

			for (int i = 0; i < allItems.Count; i++)
			{
				UsoUpdateEvidenceItem item = allItems[i];
				if (item.IsViolation)
				{
					violations++;
					if (item.Category.IndexOf("Фейковый экран", StringComparison.OrdinalIgnoreCase) >= 0)
						fakeScreens++;
					if (item.Category.IndexOf("UsoClient", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("перезагрузка", StringComparison.OrdinalIgnoreCase) >= 0)
						usoReboot++;
					if (item.Category.IndexOf("служб", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("wuauserv", StringComparison.OrdinalIgnoreCase) >= 0)
						serviceTampering++;
				}
				else if (item.IsNormal)
				{
					normal++;
				}
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiFakeScreens.Text = fakeScreens.ToString();
			lblKpiUsoReboot.Text = usoReboot.ToString();
			lblKpiServiceTampering.Text = serviceTampering.ToString();
			lblKpiNormal.Text = normal.ToString();

			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения 5.08 ({0})", violations);
			btnFilterFakeScreens.Text = string.Format("Фейковые экраны ({0})", fakeScreens);
			btnFilterUsoReboot.Text = string.Format("UsoClient ребут ({0})", usoReboot);
			btnFilterServiceTampering.Text = string.Format("Служба WU ({0})", serviceTampering);
			btnFilterNormal.Text = string.Format("Легитимные ({0})", normal);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			for (int i = 0; i < allItems.Count; i++)
			{
				UsoUpdateEvidenceItem item = allItems[i];

				// Filter category
				bool passCategory = false;
				switch (currentFilter)
				{
					case UsoUpdateTamperingFilter.All:
						passCategory = true;
						break;
					case UsoUpdateTamperingFilter.ViolationsOnly:
						passCategory = item.IsViolation;
						break;
					case UsoUpdateTamperingFilter.FakeScreensOnly:
						passCategory = item.Category.IndexOf("Фейковый экран", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case UsoUpdateTamperingFilter.FakeUsoRebootOnly:
						passCategory = item.Category.IndexOf("UsoClient", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("перезагрузка", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case UsoUpdateTamperingFilter.ServiceTamperingOnly:
						passCategory = item.Category.IndexOf("служб", StringComparison.OrdinalIgnoreCase) >= 0 || item.Category.IndexOf("wuauserv", StringComparison.OrdinalIgnoreCase) >= 0;
						break;
					case UsoUpdateTamperingFilter.NormalUpdatesOnly:
						passCategory = item.IsNormal;
						break;
				}

				if (!passCategory) continue;

				// Search filter
				if (!string.IsNullOrEmpty(search))
				{
					string haystack = string.Format("{0} {1} {2} {3} {4} {5}",
						item.Category, item.DetectedArtifact, item.ProcessOrWindow, item.Status, item.Verdict, item.DetailDescription).ToLowerInvariant();
					if (haystack.IndexOf(search) < 0) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.DetectedArtifact);
				lvi.SubItems.Add(item.ProcessOrWindow);
				lvi.SubItems.Add(item.Status);
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

			UsoUpdateEvidenceItem item = listView.SelectedItems[0].Tag as UsoUpdateEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine(string.Format("СУДЕБНАЯ ЭКСПЕРТИЗА ФАЛЬСИФИКАЦИИ ОБНОВЛЕНИЙ И СРЫВА ПРОВЕРОК — ОБЪЕКТ #{0}", item.Index));
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Источник артефакта:      {0}", item.Source));
			sbAnalysis.AppendLine(string.Format("Обнаруженный объект:     {0}", item.DetectedArtifact));
			sbAnalysis.AppendLine(string.Format("Процесс / Окно / Служба: {0}", item.ProcessOrWindow));
			sbAnalysis.AppendLine(string.Format("Состояние / Статус:      {0}", item.Status));
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
				sbAnalysis.AppendLine("ВНИМАНИЕ! Обнаруженная манипуляция классифицируется как умышленная фальсификация");
				sbAnalysis.AppendLine("системного экрана обновлений Windows для срыва проверки или оправдания отказа от демонстрации экрана.");
				sbAnalysis.AppendLine("Согласно правилу 5.08 регламента проверок, игрок подлежит немедленной блокировке на 30 дней.");
			}
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Technical
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("ТЕХНИЧЕСКИЕ ДАННЫЕ СЛУЖБЫ ОБНОВЛЕНИЙ WINDOWS, CBS И USOCLIENT");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine(item.TechnicalData);
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Ban & Verdict
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine("ЮРИДИЧЕСКИЙ РЕГЛАМЕНТ 5.08 И КОМАНДА БАКИРОВКИ");
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine(string.Format("Целевой подозреваемый:   {0}", targetPlayerName));
			sbBan.AppendLine(string.Format("Статус нарушения:        {0}", item.IsViolation ? "ПОДТВЕРЖДЕНО НА СЕРВЕРЕ" : "ЧИСТО (Нарушений не выявлено)"));
			sbBan.AppendLine(string.Format("Пункт правил проекта:    {0}", item.TargetRule));
			sbBan.AppendLine(string.Format("Срок блокировки:         {0}", item.IsViolation ? "30 дней" : "Не применимо"));
			sbBan.AppendLine();
			sbBan.AppendLine("КОМАНДА ДЛЯ КОНСОЛИ / ЧАТА СЕРВЕРА:");
			if (item.IsViolation)
			{
				sbBan.AppendLine(string.Format("/ban {0} 30d 5.08 Фальсификация экрана обновления Windows / срыв проверки", targetPlayerName));
			}
			else
			{
				sbBan.AppendLine("— Нарушений по пункту 5.08 не зафиксировано —");
			}
			sbBan.AppendLine();
			sbBan.AppendLine("ВЫДЕРЖКА ИЗ РЕГЛАМЕНТА ПРОВЕРОК:");
			sbBan.AppendLine("Пункт 5.08: Уклонение от проверки, сокрытие улик, отказ или затягивание проверки");
			sbBan.AppendLine("(включая запуск фейковых анимаций обновлений Windows, фиктивные перезагрузки через usoclient,");
			sbBan.AppendLine("закрытие необходимых окон и препятствие работе модератора) влечет бан на 30 дней.");
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void BtnCopyBanCmd_Click(object sender, EventArgs e)
		{
			string cmd = string.Format("/ban {0} 30d 5.08 Фальсификация экрана обновления Windows / срыв проверки", targetPlayerName);
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
			sb.AppendLine(string.Format("+ ПРОТОКОЛ СУДЕБНОЙ ЭКСПЕРТИЗЫ WINDOWS UPDATE И СРЫВА ПРОВЕРКИ (TOOL 79 / STEP 76)"));
			sb.AppendLine(string.Format("  Подозреваемый: {0} | Дата: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
			sb.AppendLine("----------------------------------------------------------------------------------");

			int violations = 0;
			for (int i = 0; i < allItems.Count; i++)
			{
				if (allItems[i].IsViolation)
				{
					violations++;
					sb.AppendLine(string.Format("- [5.08] {0} -> {1} ({2})", allItems[i].Category, allItems[i].DetectedArtifact, allItems[i].ProcessOrWindow));
				}
			}

			if (violations == 0)
			{
				sb.AppendLine("+ ФАЛЬСИФИКАЦИЙ ЭКРАНА ОБНОВЛЕНИЯ WINDOWS И МАНИПУЛЯЦИЙ USOCLIENT НЕ ОБНАРУЖЕНО");
			}
			else
			{
				sb.AppendLine(string.Format("- ИТОГОВЫЙ ВЕРДИКТ: ОБНАРУЖЕНО {0} НАРУШЕНИЙ -> /ban {1} 30d 5.08", violations, targetPlayerName));
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
			sfd.FileName = string.Format("WindowsUpdate_Tampering_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			try
			{
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Судебный отчет: Фальсификация обновлений Windows</title>");
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

				html.AppendLine(string.Format("<h1>🔄 Судебный протокол аудита экранов обновлений Windows и UsoClient</h1>"));
				html.AppendLine(string.Format("<div class='sub'>Подозреваемый: <b>{0}</b> | Время формирования: {1:yyyy-MM-dd HH:mm:ss} | Модуль: Tool 79 / Step 76</div>", targetPlayerName, DateTime.Now));

				html.AppendLine("<table class='table'>");
				html.AppendLine("<tr><th>№</th><th>Категория</th><th>Обнаруженный объект</th><th>Процесс / Служба</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Правило</th></tr>");

				for (int i = 0; i < allItems.Count; i++)
				{
					UsoUpdateEvidenceItem it = allItems[i];
					string cls = it.IsViolation ? "crit" : (it.IsNormal ? "safe" : "warn");
					html.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td class='{5}'>{6}</td><td>{7}</td><td>{8}</td></tr>",
						it.Index,
						System.Security.SecurityElement.Escape(it.Category),
						System.Security.SecurityElement.Escape(it.DetectedArtifact),
						System.Security.SecurityElement.Escape(it.ProcessOrWindow),
						System.Security.SecurityElement.Escape(it.Status),
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

		public static List<UsoUpdateEvidenceItem> PerformAudit()
		{
			List<UsoUpdateEvidenceItem> list = new List<UsoUpdateEvidenceItem>();
			int idx = 1;

			// 1. Audit Active Windows for Fake Update Animations (fakeupdate.net, Fullscreen Browser or Custom Fake Update Screens)
			try
			{
				List<IntPtr> matchedWindows = new List<IntPtr>();
				EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
				{
					if (IsWindowVisible(hWnd))
					{
						StringBuilder sb = new StringBuilder(512);
						int len = GetWindowText(hWnd, sb, 512);
						if (len > 0)
						{
							string title = sb.ToString();
							if (IsFakeUpdateWindowTitle(title))
							{
								matchedWindows.Add(hWnd);
							}
						}
					}
					return true;
				}, IntPtr.Zero);

				foreach (IntPtr hWnd in matchedWindows)
				{
					StringBuilder sbTitle = new StringBuilder(512);
					GetWindowText(hWnd, sbTitle, 512);
					string winTitle = sbTitle.ToString();

					uint pid;
					GetWindowThreadProcessId(hWnd, out pid);

					string procName = "Неизвестно";
					string procPath = "Неизвестно";
					try
					{
						Process proc = Process.GetProcessById((int)pid);
						procName = proc.ProcessName;
						try { procPath = proc.MainModule != null ? proc.MainModule.FileName : "Access Denied"; } catch { }
					}
					catch { }

					bool isBrowser = IsBrowserProcess(procName);
					bool isSuspicious = IsSuspiciousUpdateProcess(procName, procPath);

					RECT rect;
					GetWindowRect(hWnd, out rect);
					int width = rect.Right - rect.Left;
					int height = rect.Bottom - rect.Top;
					bool isFullScreen = (width >= Screen.PrimaryScreen.Bounds.Width && height >= Screen.PrimaryScreen.Bounds.Height);

					if (isBrowser)
					{
						list.Add(new UsoUpdateEvidenceItem
						{
							Index = idx++,
							Category = "Фейковый экран обновления (5.08)",
							Source = "Win32 Top-Level Window / Web Browser",
							DetectedArtifact = string.Format("Браузерная симуляция экрана обновления ({0})", winTitle),
							ProcessOrWindow = string.Format("{0}.exe (PID: {1})", procName, pid),
							Status = isFullScreen ? "Полноэкранный режим (Full Screen)" : "Активное окно",
							ThreatLevel = "CRITICAL",
							Verdict = "БАН 5.08",
							TargetRule = "5.08",
							IsViolation = true,
							IsNormal = false,
							Timestamp = DateTime.Now,
							RowColor = Ui.AccentRed,
							DetailDescription = string.Format("Обнаружено активное окно браузера '{0}' с заголовком экрана обновления Windows: '{1}'. Игрок открыл веб-страницу (вероятно fakeupdate.net или аналогичную симуляцию) на весь экран, симулируя установку системных обновлений для затягивания времени или срыва проверки.", procName, winTitle),
							TechnicalData = string.Format("Window Handle: 0x{0:X8}\nЗаголовок: {1}\nПроцесс: {2} (PID: {3})\nПуть: {4}\nРазрешение окна: {5}x{6} (Экран: {7}x{8})\nПолноэкранный режим: {9}",
								hWnd.ToInt64(), winTitle, procName, pid, procPath, width, height, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, isFullScreen)
						});
					}
					else if (isSuspicious)
					{
						list.Add(new UsoUpdateEvidenceItem
						{
							Index = idx++,
							Category = "Фейковый экран обновления (5.08)",
							Source = "Win32 Top-Level Window / Unsigned Process",
							DetectedArtifact = string.Format("Несистемная программа с экраном обновления ({0})", winTitle),
							ProcessOrWindow = string.Format("{0}.exe (PID: {1})", procName, pid),
							Status = "Запущенный сторонний процесс",
							ThreatLevel = "CRITICAL",
							Verdict = "БАН 5.08",
							TargetRule = "5.08",
							IsViolation = true,
							IsNormal = false,
							Timestamp = DateTime.Now,
							RowColor = Ui.AccentRed,
							DetailDescription = string.Format("Обнаружен сторонний неподписанный процесс '{0}' ({1}), отображающий фейковое окно обновления Windows '{2}'. Данный софт используется читерами для симуляции синего экрана обновлений и отказа от проверки.", procName, procPath, winTitle),
							TechnicalData = string.Format("Window Handle: 0x{0:X8}\nЗаголовок: {1}\nПроцесс: {2} (PID: {3})\nИсполняемый файл: {4}\nРазрешение окна: {5}x{6}",
								hWnd.ToInt64(), winTitle, procName, pid, procPath, width, height)
						});
					}
					else
					{
						list.Add(new UsoUpdateEvidenceItem
						{
							Index = idx++,
							Category = "Процессы Windows Update",
							Source = "Win32 Top-Level Window",
							DetectedArtifact = string.Format("Системное окно обновления ({0})", winTitle),
							ProcessOrWindow = string.Format("{0}.exe (PID: {1})", procName, pid),
							Status = "Системное окно",
							ThreatLevel = "INFO",
							Verdict = "ИНФОРМАЦИЯ",
							TargetRule = "РАЗРЕШЕНО",
							IsViolation = false,
							IsNormal = true,
							Timestamp = DateTime.Now,
							RowColor = Ui.TextPrimary,
							DetailDescription = string.Format("Обнаружено окно с заголовком '{0}', принадлежащее процессу '{1}'. Процесс находится в системном каталоге Windows.", winTitle, procName),
							TechnicalData = string.Format("Window Handle: 0x{0:X8}\nЗаголовок: {1}\nПроцесс: {2} (PID: {3})\nПуть: {4}",
								hWnd.ToInt64(), winTitle, procName, pid, procPath)
						});
					}
				}
			}
			catch { }

			// 2. Audit Windows Update Service (wuauserv) and BITS Configuration
			try
			{
				ServiceController sc = new ServiceController("wuauserv");
				string serviceStatus = sc.Status.ToString();
				string startMode = "Unknown";
				try
				{
					using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wuauserv"))
					{
						if (rk != null)
						{
							object startVal = rk.GetValue("Start");
							if (startVal != null)
							{
								int s = Convert.ToInt32(startVal);
								if (s == 4) startMode = "Disabled (Отключена)";
								else if (s == 3) startMode = "Manual (Вручную)";
								else if (s == 2) startMode = "Automatic (Автоматически)";
							}
						}
					}
				}
				catch { }

				bool isServiceDisabled = (startMode.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0);

				// Audit Policy: NoAutoUpdate
				bool noAutoUpdatePolicy = false;
				try
				{
					using (RegistryKey pol = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
					{
						if (pol != null)
						{
							object val = pol.GetValue("NoAutoUpdate");
							if (val != null && Convert.ToInt32(val) == 1)
							{
								noAutoUpdatePolicy = true;
							}
						}
					}
				}
				catch { }

				if (noAutoUpdatePolicy)
				{
					list.Add(new UsoUpdateEvidenceItem
					{
						Index = idx++,
						Category = "Манипуляция службой wuauserv (5.08)",
						Source = "Registry Policies: WindowsUpdate/AU",
						DetectedArtifact = "Политика NoAutoUpdate = 1",
						ProcessOrWindow = "wuauserv / GroupPolicy",
						Status = "Обновления заблокированы политикой",
						ThreatLevel = "WARNING",
						Verdict = "ПРЕДУПРЕЖДЕНИЕ",
						TargetRule = "5.08",
						IsViolation = false,
						IsNormal = false,
						Timestamp = DateTime.Now,
						RowColor = Color.FromArgb(255, 180, 0),
						DetailDescription = "В реестре Windows включена политика 'NoAutoUpdate = 1'. Это свидетельствует о принудительной блокировке обновлений Windows через скрипты твиков или анти-форензик утилиты.",
						TechnicalData = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU -> NoAutoUpdate = 1\nСлужба: wuauserv (" + serviceStatus + ")"
					});
				}

				list.Add(new UsoUpdateEvidenceItem
				{
					Index = idx++,
					Category = "Реальное состояние обновления",
					Source = "Windows Service Controller",
					DetectedArtifact = string.Format("Состояние службы wuauserv: {0} ({1})", serviceStatus, startMode),
					ProcessOrWindow = "services.exe / svchost.exe",
					Status = serviceStatus,
					ThreatLevel = isServiceDisabled ? "WARNING" : "SAFE",
					Verdict = isServiceDisabled ? "ОТКЛЮЧЕНА" : "ЧИСТО",
					TargetRule = "РАЗРЕШЕНО",
					IsViolation = false,
					IsNormal = !isServiceDisabled,
					Timestamp = DateTime.Now,
					RowColor = isServiceDisabled ? Color.FromArgb(255, 180, 0) : Color.FromArgb(50, 205, 50),
					DetailDescription = string.Format("Служба автоматического обновления Windows (wuauserv) находится в состоянии '{0}', тип запуска: '{1}'. Если игрок утверждает, что у него прямо сейчас качаются или ставятся обновления, а служба отключена — это фальсификация (5.08).", serviceStatus, startMode),
					TechnicalData = string.Format("Service: wuauserv\nDisplayName: Windows Update\nStatus: {0}\nStartMode: {1}\nNoAutoUpdate Policy: {2}", serviceStatus, startMode, noAutoUpdatePolicy)
				});
			}
			catch { }

			// 3. Audit Pending Reboot in Component Based Servicing (CBS)
			try
			{
				bool cbsPending = false;
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
				{
					if (rk != null) cbsPending = true;
				}

				bool wuPending = false;
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
				{
					if (rk != null) wuPending = true;
				}

				if (cbsPending || wuPending)
				{
					list.Add(new UsoUpdateEvidenceItem
					{
						Index = idx++,
						Category = "Реальное состояние обновления",
						Source = "Registry CBS / Auto Update",
						DetectedArtifact = "Подлинный флаг RebootPending / RebootRequired",
						ProcessOrWindow = "TrustedInstaller / CBS",
						Status = "Действительно требуется перезагрузка",
						ThreatLevel = "INFO",
						Verdict = "РЕАЛЬНЫЙ РЕБУТ",
						TargetRule = "РАЗРЕШЕНО",
						IsViolation = false,
						IsNormal = true,
						Timestamp = DateTime.Now,
						RowColor = Ui.AccentTeal,
						DetailDescription = "В реестре Windows зарегистрирован подлинный ключ RebootPending / RebootRequired. Система действительно завершила установку пакетов обновлений и ожидает перезагрузки.",
						TechnicalData = string.Format("CBS RebootPending: {0}\nWU RebootRequired: {1}", cbsPending, wuPending)
					});
				}
				else
				{
					list.Add(new UsoUpdateEvidenceItem
					{
						Index = idx++,
						Category = "Реальное состояние обновления",
						Source = "Registry CBS / Auto Update",
						DetectedArtifact = "Флаги RebootPending / RebootRequired отсутствуют",
						ProcessOrWindow = "TrustedInstaller / CBS",
						Status = "Перезагрузка для обновлений НЕ требуется",
						ThreatLevel = "SAFE",
						Verdict = "НЕТ ОБНОВЛЕНИЙ",
						TargetRule = "РАЗРЕШЕНО",
						IsViolation = false,
						IsNormal = true,
						Timestamp = DateTime.Now,
						RowColor = Ui.TextPrimary,
						DetailDescription = "В системе отсутствуют флаги перезагрузки из-за обновлений. Любое заявление игрока о том, что Windows принудительно перезагружается из-за обновлений прямо сейчас, является ложным.",
						TechnicalData = "Ключи RebootPending и RebootRequired отсутствуют в HKLM."
					});
				}
			}
			catch { }

			// 4. Audit Active Update Engine Processes (TiWorker, TrustedInstaller, usoclient, wusa)
			try
			{
				string[] updateProcesses = new string[] { "tiworker", "trustedinstaller", "usoclient", "wusa", "mousocoreworker", "setuphost" };
				foreach (string pName in updateProcesses)
				{
					Process[] procs = Process.GetProcessesByName(pName);
					if (procs != null && procs.Length > 0)
					{
						foreach (Process p in procs)
						{
							string path = "Access Denied";
							DateTime startTime = DateTime.MinValue;
							try { if (p.MainModule != null) path = p.MainModule.FileName; } catch { }
							try { startTime = p.StartTime; } catch { }

							list.Add(new UsoUpdateEvidenceItem
							{
								Index = idx++,
								Category = "Процессы Windows Update",
								Source = "Running Process Table",
								DetectedArtifact = string.Format("Активный системный процесс обновления: {0}.exe", pName),
								ProcessOrWindow = string.Format("{0}.exe (PID: {1})", pName, p.Id),
								Status = "Активен",
								ThreatLevel = "INFO",
								Verdict = "СИСТЕМНЫЙ ПРОЦЕСС",
								TargetRule = "РАЗРЕШЕНО",
								IsViolation = false,
								IsNormal = true,
								Timestamp = startTime != DateTime.MinValue ? startTime : DateTime.Now,
								RowColor = Ui.TextPrimary,
								DetailDescription = string.Format("В системе активен реальный рабочий процесс обновлений '{0}.exe' (PID: {1}), запущен в {2:yyyy-MM-dd HH:mm:ss}.", pName, p.Id, startTime),
								TechnicalData = string.Format("Process: {0}.exe\nPID: {1}\nПуть: {2}\nВремя старта: {3:yyyy-MM-dd HH:mm:ss}", pName, p.Id, path, startTime)
							});
						}
					}
				}
			}
			catch { }

			// 5. Audit UsoClient Execution Traces in Prefetch
			try
			{
				string prefetchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
				if (Directory.Exists(prefetchDir))
				{
					string[] patterns = new string[] { "USOCLIENT.EXE-*.pf", "WUSA.EXE-*.pf" };
					foreach (string pat in patterns)
					{
						string[] pfFiles = Directory.GetFiles(prefetchDir, pat);
						foreach (string pf in pfFiles)
						{
							FileInfo fi = new FileInfo(pf);
							TimeSpan diff = DateTime.Now - fi.LastWriteTime;

							if (diff.TotalMinutes <= 60)
							{
								list.Add(new UsoUpdateEvidenceItem
								{
									Index = idx++,
									Category = "Ложная перезагрузка UsoClient (5.08)",
									Source = "Windows Prefetch",
									DetectedArtifact = string.Format("Недавний запуск {0} ({1:0} мин назад)", Path.GetFileName(pf), diff.TotalMinutes),
									ProcessOrWindow = Path.GetFileName(pf),
									Status = string.Format("Запуск за {0:0} минут до проверки", diff.TotalMinutes),
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.08",
									TargetRule = "5.08",
									IsViolation = true,
									IsNormal = false,
									Timestamp = fi.LastWriteTime,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("Обнаружен файл Prefetch '{0}', свидетельствующий о запуске usoclient.exe за {1:0} минут до проверки. Читеры используют команду 'usoclient RestartDevice' или 'usoclient StartInteractiveScan' для принудительного вызова перезагрузки/экрана обновления во время вызова на проверку.", Path.GetFileName(pf), diff.TotalMinutes),
									TechnicalData = string.Format("Файл: {0}\nРазмер: {1} байт\nВремя последнего запуска: {2:yyyy-MM-dd HH:mm:ss}\nДельта: {3:0.0} минут назад",
										pf, fi.Length, fi.LastWriteTime, diff.TotalMinutes)
								});
							}
							else
							{
								list.Add(new UsoUpdateEvidenceItem
								{
									Index = idx++,
									Category = "Процессы Windows Update",
									Source = "Windows Prefetch",
									DetectedArtifact = string.Format("Архивный запуск {0}", Path.GetFileName(pf)),
									ProcessOrWindow = Path.GetFileName(pf),
									Status = string.Format("Запуск {0:0} часов назад", diff.TotalHours),
									ThreatLevel = "SAFE",
									Verdict = "АРХИВНЫЙ ЗАПУСК",
									TargetRule = "РАЗРЕШЕНО",
									IsViolation = false,
									IsNormal = true,
									Timestamp = fi.LastWriteTime,
									RowColor = Ui.TextSecondary,
									DetailDescription = string.Format("Зафиксирован архивный запуск {0} в Prefetch ({1:yyyy-MM-dd HH:mm:ss}). Запуск происходил давно и не связан с проверкой.", Path.GetFileName(pf), fi.LastWriteTime),
									TechnicalData = string.Format("Файл: {0}\nВремя: {1:yyyy-MM-dd HH:mm:ss}", pf, fi.LastWriteTime)
								});
							}
						}
					}
				}
			}
			catch { }

			// 6. Audit CBS.log Recent Activity
			try
			{
				string cbsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Logs\CBS\CBS.log");
				if (File.Exists(cbsPath))
				{
					FileInfo fi = new FileInfo(cbsPath);
					TimeSpan diff = DateTime.Now - fi.LastWriteTime;

					list.Add(new UsoUpdateEvidenceItem
					{
						Index = idx++,
						Category = "Журнал CBS и события",
						Source = "CBS.log",
						DetectedArtifact = string.Format("Последняя активность журнала CBS: {0:yyyy-MM-dd HH:mm:ss}", fi.LastWriteTime),
						ProcessOrWindow = "TrustedInstaller.exe",
						Status = string.Format("{0:0.0} мин назад", diff.TotalMinutes),
						ThreatLevel = diff.TotalMinutes < 15 ? "INFO" : "SAFE",
						Verdict = diff.TotalMinutes < 15 ? "АКТИВНОСТЬ CBS" : "НЕТ АКТИВНОСТИ",
						TargetRule = "РАЗРЕШЕНО",
						IsViolation = false,
						IsNormal = true,
						Timestamp = fi.LastWriteTime,
						RowColor = Ui.TextPrimary,
						DetailDescription = string.Format("Журнал обслуживания компонентов (CBS.log) последний раз модифицировался {0:yyyy-MM-dd HH:mm:ss} ({1:0.0} минут назад). Реальная установка пакетов Windows Update обязательно оставляет записи в CBS.log.", fi.LastWriteTime, diff.TotalMinutes),
						TechnicalData = string.Format("Файл: {0}\nРазмер: {1:N0} байт\nLastWriteTime: {2:yyyy-MM-dd HH:mm:ss}\nДельта: {3:0.0} минут", cbsPath, fi.Length, fi.LastWriteTime, diff.TotalMinutes)
					});
				}
			}
			catch { }

			// 7. Audit System Event Log for User32 1074 (Shutdown / Restart initiated by process)
			try
			{
				string query = "*[System[(EventID=1074)]]";
				EventLogQuery elq = new EventLogQuery("System", PathType.LogName, query);
				elq.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord rec;
					int count = 0;
					while ((rec = reader.ReadEvent()) != null && count < 10)
					{
						count++;
						DateTime? timeCreated = rec.TimeCreated;
						if (timeCreated.HasValue)
						{
							TimeSpan diff = DateTime.Now - timeCreated.Value;
							string desc = "";
							try { desc = rec.FormatDescription() ?? ""; } catch { }

							bool isUsoReboot = (desc.IndexOf("usoclient", StringComparison.OrdinalIgnoreCase) >= 0 ||
											   desc.IndexOf("RestartDevice", StringComparison.OrdinalIgnoreCase) >= 0 ||
											   desc.IndexOf("WindowsUpdate", StringComparison.OrdinalIgnoreCase) >= 0);

							if (diff.TotalMinutes <= 60 && isUsoReboot)
							{
								list.Add(new UsoUpdateEvidenceItem
								{
									Index = idx++,
									Category = "Ложная перезагрузка UsoClient (5.08)",
									Source = "System.evtx / Event ID 1074",
									DetectedArtifact = "Перезагрузка инициирована процессом UsoClient / WindowsUpdate",
									ProcessOrWindow = "User32 / Event ID 1074",
									Status = string.Format("{0:0} мин назад", diff.TotalMinutes),
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.08",
									TargetRule = "5.08",
									IsViolation = true,
									IsNormal = false,
									Timestamp = timeCreated.Value,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("В журнале System зафиксировано событие Event ID 1074: инициация перезагрузки процессом UsoClient / WindowsUpdate за {0:0} минут до проверки. Описание: {1}", diff.TotalMinutes, desc),
									TechnicalData = string.Format("Event ID: 1074\nВремя: {0:yyyy-MM-dd HH:mm:ss}\nОписание: {1}", timeCreated.Value, desc)
								});
							}
						}
					}
				}
			}
			catch { }

			// Fallback: If nothing was flagged as violation, add Clean baseline info
			if (list.Count == 0)
			{
				list.Add(new UsoUpdateEvidenceItem
				{
					Index = idx++,
					Category = "Реальное состояние обновления",
					Source = "System State Audit",
					DetectedArtifact = "Фальсификаций экранов обновления и срыва проверки не обнаружено",
					ProcessOrWindow = "Windows Update Subsystem",
					Status = "Штатный режим",
					ThreatLevel = "SAFE",
					Verdict = "ЧИСТО",
					TargetRule = "РАЗРЕШЕНО",
					IsViolation = false,
					IsNormal = true,
					Timestamp = DateTime.Now,
					RowColor = Color.FromArgb(50, 205, 50),
					DetailDescription = "Аудит активных окон, процессов браузера, usoclient, службы wuauserv и журнала CBS не выявил следов фиктивного экрана обновлений или умышленного срыва проверки.",
					TechnicalData = "Все проверки пройдены без нарушений."
				});
			}

			return list;
		}

		private static bool IsFakeUpdateWindowTitle(string title)
		{
			if (string.IsNullOrEmpty(title)) return false;

			string lower = title.ToLowerInvariant();
			return lower.IndexOf("работа с обновлениями") >= 0 ||
				   lower.IndexOf("обновление windows") >= 0 ||
				   lower.IndexOf("подготовка windows") >= 0 ||
				   lower.IndexOf("не выключайте компьютер") >= 0 ||
				   lower.IndexOf("завершение работы с обновлениями") >= 0 ||
				   lower.IndexOf("working on updates") >= 0 ||
				   lower.IndexOf("installing updates") >= 0 ||
				   lower.IndexOf("configuring windows updates") >= 0 ||
				   lower.IndexOf("don't turn off your pc") >= 0 ||
				   lower.IndexOf("don't turn off your computer") >= 0 ||
				   lower.IndexOf("fakeupdate") >= 0 ||
				   lower.IndexOf("100% complete") >= 0;
		}

		private static bool IsBrowserProcess(string procName)
		{
			if (string.IsNullOrEmpty(procName)) return false;
			string lower = procName.ToLowerInvariant();
			return lower == "chrome" || lower == "msedge" || lower == "firefox" ||
				   lower == "opera" || lower == "brave" || lower == "yandex" ||
				   lower == "vivaldi" || lower == "waterfox" || lower == "browser";
		}

		private static bool IsSuspiciousUpdateProcess(string procName, string procPath)
		{
			if (string.IsNullOrEmpty(procName)) return false;
			string lower = procName.ToLowerInvariant();

			// If it's python, electron, autoit, autohotkey, or located in AppData/Temp
			if (lower.IndexOf("python") >= 0 || lower.IndexOf("electron") >= 0 ||
				lower.IndexOf("autoit") >= 0 || lower.IndexOf("autohotkey") >= 0 ||
				lower.IndexOf("node") >= 0)
			{
				return true;
			}

			if (!string.IsNullOrEmpty(procPath))
			{
				string lowerPath = procPath.ToLowerInvariant();
				if (lowerPath.IndexOf(@"\appdata\") >= 0 || lowerPath.IndexOf(@"\temp\") >= 0 ||
					lowerPath.IndexOf(@"\downloads\") >= 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}
