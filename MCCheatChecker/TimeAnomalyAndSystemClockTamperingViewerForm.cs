using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum TimeAnomalyFilter
	{
		All,
		ViolationsOnly,
		ClockRewindOnly,
		RebootBeforeSsOnly,
		TimeTravelFilesOnly,
		NtpAndNormalOnly
	}

	public class TimeAnomalyEvidenceItem
	{
		public int Index;
		public string Category; // "Перевод часов назад (5.08)", "Перезагрузка перед SS (5.08)", "Файлы из будущего (Time Travel)", "Служба времени W32Time", "Синхронизация времени"
		public string Source;
		public string DetectedAnomaly;
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
		public TimeSpan TimeDelta;
	}

	public class TimeAnomalyAndSystemClockTamperingViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<TimeAnomalyEvidenceItem> allItems = new List<TimeAnomalyEvidenceItem>();
		private TimeAnomalyFilter currentFilter = TimeAnomalyFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiRewind;
		private Label lblKpiReboot;
		private Label lblKpiFutureFiles;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterRewind;
		private DarkButton btnFilterReboot;
		private DarkButton btnFilterFutureFiles;
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

		public TimeAnomalyAndSystemClockTamperingViewerForm() : this("Player")
		{
		}

		public TimeAnomalyAndSystemClockTamperingViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "⏱️ Инспектор аномалий системного времени, перевода часов и Time Skew (Tool 77 / Step 74) — MCCheatChecker";
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
			lblHeaderTitle.Text = "⏱️ ИНСПЕКТОР АНОМАЛИЙ СИСТЕМНОГО ВРЕМЕНИ, ПЕРЕВОДА ЧАСОВ И TIME SKEW";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentOrange;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Судебный аудит журнала System (Event ID 1/12/13/41): детекция перевода часов назад (сокрытие улик 5.08), аварийной перезагрузки перед проверкой, отключения NTP и файлов из будущего (Time Travel).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация судебного анализа времени и часов...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 440, 16);
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО СОБЫТИЙ", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.08)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("⏪ ПЕРЕВОД ЧАСОВ НАЗАД", "0", Ui.AccentOrange, ref kx, out lblKpiRewind));
			kpiPanel.Controls.Add(CreateKpiBadge("🔌 ПЕРЕЗАГРУЗКА ПЕРЕД SS", "0", Ui.AccentPurple, ref kx, out lblKpiReboot));
			kpiPanel.Controls.Add(CreateKpiBadge("⏳ ФАЙЛЫ ИЗ БУДУЩЕГО", "0", Ui.AccentBlue, ref kx, out lblKpiFutureFiles));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ НОРМА / NTP СИНХРОНИЗАЦИИ", "0", Ui.AccentGreen, ref kx, out lblKpiSafe));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все события", TimeAnomalyFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08)", TimeAnomalyFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterRewind = CreateFilterButton("⏪ Перевод часов назад", TimeAnomalyFilter.ClockRewindOnly, ref bx, filterBar);
			btnFilterReboot = CreateFilterButton("🔌 Перезагрузки перед SS", TimeAnomalyFilter.RebootBeforeSsOnly, ref bx, filterBar);
			btnFilterFutureFiles = CreateFilterButton("⏳ Файлы из будущего", TimeAnomalyFilter.TimeTravelFilesOnly, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Норма и NTP", TimeAnomalyFilter.NtpAndNormalOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по источнику, дельте времени, событию или правилу...";
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
			btnRescan = CreateActionButton("🔄 Повторить аудит времени", ref ax, bottomActions, delegate { StartScan(); });

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
			listView.Columns.Add("Категория", 190);
			listView.Columns.Add("Источник / Событие", 210);
			listView.Columns.Add("Обнаруженная аномалия", 270);
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

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ времени");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⏱️ События смены часов Event ID 1 и W32Time");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			txtTechnical.Font = Ui.FontMono(9f);
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 и Команда бана");
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

		private DarkButton CreateFilterButton(string text, TimeAnomalyFilter filter, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(160, 30);
			btn.Location = new Point(curX, 6);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtonStyles();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 168;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int curX, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(215, 30);
			btn.Location = new Point(curX, 8);
			btn.Cursor = Cursors.Hand;
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 225;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			Color activeBg = Ui.AccentOrange;
			Color activeFg = Color.Black;
			Color normalBg = Ui.Surface;
			Color normalFg = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == TimeAnomalyFilter.All, normalBg, activeBg, normalFg, activeFg);
			SetBtnStyle(btnFilterViolations, currentFilter == TimeAnomalyFilter.ViolationsOnly, normalBg, Ui.AccentRed, normalFg, Color.White);
			SetBtnStyle(btnFilterRewind, currentFilter == TimeAnomalyFilter.ClockRewindOnly, normalBg, Ui.AccentOrange, normalFg, Color.Black);
			SetBtnStyle(btnFilterReboot, currentFilter == TimeAnomalyFilter.RebootBeforeSsOnly, normalBg, Ui.AccentPurple, normalFg, Color.White);
			SetBtnStyle(btnFilterFutureFiles, currentFilter == TimeAnomalyFilter.TimeTravelFilesOnly, normalBg, Ui.AccentBlue, normalFg, Color.White);
			SetBtnStyle(btnFilterSafe, currentFilter == TimeAnomalyFilter.NtpAndNormalOnly, normalBg, Ui.AccentGreen, normalFg, Color.Black);
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

			lblStatusPill.Text = "⏳ АНАЛИЗ ЧАСОВ, ЖУРНАЛОВ И TIME SKEW...";
			lblStatusPill.ForeColor = Ui.AccentOrange;

			listView.Items.Clear();
			allItems.Clear();
			txtAnalysis.Clear();
			txtTechnical.Clear();
			txtBanVerdict.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformTimeAnomalyScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования времени: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<TimeAnomalyEvidenceItem> items = e.Result as List<TimeAnomalyEvidenceItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpis();
				UpdateFilterButtonStyles();
				ApplyFilter();

				int violations = 0;
				foreach (TimeAnomalyEvidenceItem it in allItems)
				{
					if (it.IsViolation) violations++;
				}

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} АНОМАЛИЙ ВРЕМЕНИ (5.08)", violations);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ АНОМАЛИЙ СИСТЕМНОГО ВРЕМЕНИ НЕ ОБНАРУЖЕНО";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			};

			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpis()
		{
			int total = allItems.Count;
			int violations = 0;
			int rewind = 0;
			int reboot = 0;
			int future = 0;
			int safe = 0;

			foreach (TimeAnomalyEvidenceItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.Category == "Перевод часов назад (5.08)") rewind++;
				if (item.Category == "Перезагрузка перед SS (5.08)") reboot++;
				if (item.Category == "Файлы из будущего (Time Travel)") future++;
				if (item.IsNormal) safe++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiRewind.Text = rewind.ToString();
			lblKpiReboot.Text = reboot.ToString();
			lblKpiFutureFiles.Text = future.ToString();
			lblKpiSafe.Text = safe.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (TimeAnomalyEvidenceItem item in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case TimeAnomalyFilter.ViolationsOnly:
						matchFilter = item.IsViolation;
						break;
					case TimeAnomalyFilter.ClockRewindOnly:
						matchFilter = (item.Category == "Перевод часов назад (5.08)");
						break;
					case TimeAnomalyFilter.RebootBeforeSsOnly:
						matchFilter = (item.Category == "Перезагрузка перед SS (5.08)");
						break;
					case TimeAnomalyFilter.TimeTravelFilesOnly:
						matchFilter = (item.Category == "Файлы из будущего (Time Travel)");
						break;
					case TimeAnomalyFilter.NtpAndNormalOnly:
						matchFilter = item.IsNormal;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (item.Source != null && item.Source.ToLowerInvariant().Contains(search))
						|| (item.DetectedAnomaly != null && item.DetectedAnomaly.ToLowerInvariant().Contains(search))
						|| (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search))
						|| (item.TargetRule != null && item.TargetRule.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.Source);
				lvi.SubItems.Add(item.DetectedAnomaly);
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
				txtAnalysis.Text = "Нет событий времени, соответствующих выбранному фильтру.";
				txtTechnical.Clear();
				txtBanVerdict.Clear();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			TimeAnomalyEvidenceItem item = listView.SelectedItems[0].Tag as TimeAnomalyEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine("           СУДЕБНАЯ ЭКСПЕРТИЗА СИСТЕМНОГО ВРЕМЕНИ И ЧАСОВ                                 ");
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Источник события:        {0}", item.Source));
			sbAnalysis.AppendLine(string.Format("Обнаруженная аномалия:   {0}", item.DetectedAnomaly));
			sbAnalysis.AppendLine(string.Format("Статус:                  {0}", item.Status));
			sbAnalysis.AppendLine(string.Format("Уровень угрозы:          {0}", item.ThreatLevel));
			sbAnalysis.AppendLine(string.Format("Регламент проверки:      {0}", item.TargetRule));
			sbAnalysis.AppendLine(string.Format("Штамп времени:           {0:yyyy-MM-dd HH:mm:ss}", item.Timestamp));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine("ПОДРОБНОЕ ЗАКЛЮЧЕНИЕ ЭКСПЕРТИЗЫ:");
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine(item.DetailDescription);
			sbAnalysis.AppendLine();
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Technical Details
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("           ТЕХНИЧЕСКИЕ ДАННЫЕ EVENT LOG И СЛУЖБЫ ВРЕМЕНИ W32TIME                          ");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine();
			sbTech.AppendLine(string.Format("Является нарушением:       {0}", item.IsViolation ? "ДА (КРИТИЧЕСКАЯ УГРОЗА)" : "НЕТ"));
			sbTech.AppendLine(string.Format("Легитимное событие:        {0}", item.IsNormal ? "ДА (ПОДТВЕРЖДЕНО)" : "НЕТ"));
			sbTech.AppendLine();
			sbTech.AppendLine("ТЕХНИЧЕСКИЙ ДАМП ПАРАМЕТРОВ:");
			sbTech.AppendLine(string.IsNullOrEmpty(item.TechnicalData) ? "Технический дамп отсутствует." : item.TechnicalData);
			sbTech.AppendLine();
			sbTech.AppendLine("МЕХАНИКА ОБХОДОВ ЧЕРЕЗ МАНИПУЛЯЦИИ ВРЕМЕНЕМ:");
			sbTech.AppendLine("1. Перевод часов назад (Event ID 1): используется для искажения временных меток файлов (LastWriteTime,");
			sbTech.AppendLine("   CreationTime) при удалении или запуске читов, чтобы выдать свежий чит за старый файл или скрыть Prefetch.");
			sbTech.AppendLine("2. Перезагрузка ПК перед проверкой (Event 12/13/41): очищает оперативную память процесса javaw.exe,");
			sbTech.AppendLine("   сбрасывает кэш DNS и завершает скрытые чит-инжекторы (5.08).");
			sbTech.AppendLine("3. Файлы из будущего: возникают, когда игрок переводит системные часы вперед, запускает чит/клинер,");
			sbTech.AppendLine("   а затем возвращает часы обратно.");
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Ban Verdict
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine("           РЕГЛАМЕНТ 5.08 И КОМАНДА БАНА ДЛЯ МОДЕРАТОРА                                   ");
			sbBan.AppendLine("==========================================================================================");
			sbBan.AppendLine();
			if (item.IsViolation)
			{
				sbBan.AppendLine(string.Format("🚨 ВЕРДИКТ: ОБНАРУЖЕНО НАРУШЕНИЕ РЕГЛАМЕНТА {0}!", item.TargetRule));
				sbBan.AppendLine();
				sbBan.AppendLine("ГОТОВАЯ КОМАНДА ДЛЯ СЕРВЕРА (СКОПИРУЙТЕ В ЧАТ):");
				string banReason = string.Format("5.08 (Манипуляция временем / сокрытие улик: {0})", item.DetectedAnomaly);
				sbBan.AppendLine(string.Format("/ban {0} 30d {1}", targetPlayerName, banReason));
				sbBan.AppendLine();
				sbBan.AppendLine("ЮРИДИЧЕСКАЯ СПРАВКА ПО РЕГЛАМЕНТУ:");
				sbBan.AppendLine("Пункт 5.08 гласит: Запрещены любые действия, направленные на сокрытие использования читов,");
				sbBan.AppendLine("обход проверки, удаление следов, очистку логов, перевод системного времени или экстренную перезагрузку.");
			}
			else
			{
				sbBan.AppendLine("✅ НАРУШЕНИЯ НЕ ЗАФИКСИРОВАНО");
				sbBan.AppendLine();
				sbBan.AppendLine("Данное событие времени является штатной синхронизацией NTP либо стандартным запуском Windows.");
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
			TimeAnomalyEvidenceItem item = listView.SelectedItems[0].Tag as TimeAnomalyEvidenceItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранное событие не является нарушением правил.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string cmd = string.Format("/ban {0} 30d 5.08 (Манипуляция системным временем: {1})", targetPlayerName, item.DetectedAnomaly);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n" + cmd, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("📋 **[MCCheatChecker] Судебный отчёт: Time Anomaly & Clock Tampering Forensics**");
			sb.AppendLine(string.Format("**Проверяемый игрок:** `{0}`", targetPlayerName));
			sb.AppendLine(string.Format("**Штамп проверки:** `{0:yyyy-MM-dd HH:mm:ss}`", DateTime.Now));
			sb.AppendLine(string.Format("**Всего проверено событий времени:** `{0}`", allItems.Count));

			int violations = 0;
			foreach (TimeAnomalyEvidenceItem it in allItems) if (it.IsViolation) violations++;
			sb.AppendLine(string.Format("**Зафиксировано нарушений:** `{0}`", violations));
			sb.AppendLine();

			if (violations > 0)
			{
				sb.AppendLine("🚨 **ОБНАРУЖЕННЫЕ УЛИКИ И НАРУШЕНИЯ:**");
				foreach (TimeAnomalyEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine(string.Format("- **[{0}] {1}**", it.TargetRule, it.DetectedAnomaly));
						sb.AppendLine(string.Format("  *Источник:* `{0}`", it.Source));
						sb.AppendLine(string.Format("  *Вердикт:* {0}", it.Verdict));
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Рекомендуемая мера наказания:**");
				sb.AppendLine(string.Format("`/ban {0} 30d 5.08 (Time Anomaly Forensics)`", targetPlayerName));
			}
			else
			{
				sb.AppendLine("✅ **Манипуляций системными часами и перезагрузок перед проверкой не обнаружено.**");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("Time_Anomaly_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ru\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<title>Отчёт Time Anomaly & Clock Tampering — " + targetPlayerName + "</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("body { background-color: #121418; color: #E0E0E0; font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
			sb.AppendLine("h1 { color: #FD7E14; border-bottom: 2px solid #FD7E14; padding-bottom: 10px; }");
			sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: bold; margin-right: 8px; }");
			sb.AppendLine(".badge-crit { background: #E74C3C; color: white; }");
			sb.AppendLine(".badge-safe { background: #2ECC71; color: black; }");
			sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 13px; }");
			sb.AppendLine("th { background-color: #1E222B; color: #FD7E14; padding: 10px; text-align: left; border: 1px solid #2C3240; }");
			sb.AppendLine("td { padding: 8px 10px; border: 1px solid #2C3240; }");
			sb.AppendLine("tr:nth-child(even) { background-color: #181B22; }");
			sb.AppendLine(".crit { color: #FF6B6B; font-weight: bold; }");
			sb.AppendLine(".warn { color: #FFA94D; }");
			sb.AppendLine(".safe { color: #69DB7C; }");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>⏱️ Экспертный судебный отчёт: System Clock & Time Anomaly Forensics</h1>");
			sb.AppendLine("<p><strong>Проверяемый игрок:</strong> " + targetPlayerName + " | <strong>Дата экспертизы:</strong> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

			int violations = 0;
			foreach (TimeAnomalyEvidenceItem it in allItems) if (it.IsViolation) violations++;

			if (violations > 0)
			{
				sb.AppendLine("<div class=\"badge badge-crit\">ОБНАРУЖЕНО " + violations + " КРИТИЧЕСКИХ НАРУШЕНИЙ (5.08)</div>");
			}
			else
			{
				sb.AppendLine("<div class=\"badge badge-safe\">ЧИСТО: АНОМАЛИЙ СИСТЕМНОГО ВРЕМЕНИ НЕ ОБНАРУЖЕНО</div>");
			}

			sb.AppendLine("<table>");
			sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Источник</th><th>Обнаруженная аномалия</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th><th>Штамп времени</th></tr>");

			foreach (TimeAnomalyEvidenceItem it in allItems)
			{
				string colorClass = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
				sb.AppendLine(string.Format("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9:yyyy-MM-dd HH:mm:ss}</td></tr>",
					colorClass, it.Index, it.Category, it.Source, it.DetectedAnomaly, it.Status, it.ThreatLevel, it.Verdict, it.TargetRule, it.Timestamp));
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

		public static List<TimeAnomalyEvidenceItem> PerformTimeAnomalyScan()
		{
			List<TimeAnomalyEvidenceItem> results = new List<TimeAnomalyEvidenceItem>();
			int idx = 1;

			// 1. Audit System Event Log for Kernel-General (Event 1, 12, 13) and Kernel-Power (Event 41)
			try
			{
				string query = "*[System[(EventID=1 or EventID=12 or EventID=13 or EventID=41 or EventID=1074) and TimeCreated[timediff(@SystemTime) <= 604800000]]]"; // Last 7 days
				EventLogQuery elq = new EventLogQuery("System", PathType.LogName, query);
				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord record;
					while ((record = reader.ReadEvent()) != null)
					{
						using (record)
						{
							int eventId = record.Id;
							DateTime eventTime = record.TimeCreated.HasValue ? record.TimeCreated.Value : DateTime.Now;

							if (eventId == 1) // Kernel-General: System time changed
							{
								DateTime oldTime = DateTime.MinValue;
								DateTime newTime = DateTime.MinValue;

								try
								{
									string xml = record.ToXml();
									Match mOld = Regex.Match(xml, @"<Data Name='OldTime'>([^<]+)</Data>");
									Match mNew = Regex.Match(xml, @"<Data Name='NewTime'>([^<]+)</Data>");
									if (mOld.Success && mNew.Success)
									{
										DateTime.TryParse(mOld.Groups[1].Value, out oldTime);
										DateTime.TryParse(mNew.Groups[1].Value, out newTime);
									}
								}
								catch { }

								if (oldTime != DateTime.MinValue && newTime != DateTime.MinValue)
								{
									TimeSpan delta = newTime - oldTime;
									// Check if clock was set backwards
									if (delta.TotalMinutes < -5.0)
									{
										results.Add(new TimeAnomalyEvidenceItem
										{
											Index = idx++,
											Category = "Перевод часов назад (5.08)",
											Source = "System.evtx (Event ID 1: Kernel-General)",
											DetectedAnomaly = string.Format("Перевод часов назад на {0:F1} мин", Math.Abs(delta.TotalMinutes)),
											Status = "ОТКАТ ЧАСОВ НАЗАД (5.08)",
											ThreatLevel = "CRITICAL",
											Verdict = string.Format("🚨 БАН 5.08: Системные часы были переведены назад на {0:F1} минут ({1:HH:mm:ss} -> {2:HH:mm:ss})!", Math.Abs(delta.TotalMinutes), oldTime, newTime),
											TargetRule = "5.08",
											IsViolation = true,
											IsNormal = false,
											Timestamp = eventTime,
											RowColor = Ui.AccentRed,
											TimeDelta = delta,
											TechnicalData = string.Format("OldTime: {0:yyyy-MM-dd HH:mm:ss.fff}, NewTime: {1:yyyy-MM-dd HH:mm:ss.fff}, Delta: {2}", oldTime, newTime, delta),
											DetailDescription = string.Format("Пользователь или скрипт-самоликвидатор перевёл часы назад на {0:F1} мин. Это используется для искажения временных меток файлов при удалении читов.", Math.Abs(delta.TotalMinutes))
										});
									}
									else if (delta.TotalMinutes > 30.0)
									{
										results.Add(new TimeAnomalyEvidenceItem
										{
											Index = idx++,
											Category = "Перевод часов назад (5.08)",
											Source = "System.evtx (Event ID 1: Kernel-General)",
											DetectedAnomaly = string.Format("Скачок часов вперед на {0:F1} мин", delta.TotalMinutes),
											Status = "СКАЧОК ЧАСОВ ВПЕРЕД",
											ThreatLevel = "WARNING",
											Verdict = string.Format("Часы были переведены вперед на {0:F1} мин ({1:HH:mm} -> {2:HH:mm}).", delta.TotalMinutes, oldTime, newTime),
											TargetRule = "5.08 (Подозрение)",
											IsViolation = false,
											IsNormal = false,
											Timestamp = eventTime,
											RowColor = Ui.AccentOrange,
											TimeDelta = delta,
											TechnicalData = string.Format("OldTime: {0}, NewTime: {1}, Delta: {2}", oldTime, newTime, delta),
											DetailDescription = "Крупный скачок времени вперед может использоваться для искусственного состаривания Prefetch/BAM логов."
										});
									}
									else
									{
										results.Add(new TimeAnomalyEvidenceItem
										{
											Index = idx++,
											Category = "Синхронизация времени",
											Source = "System.evtx (Event ID 1)",
											DetectedAnomaly = string.Format("Микро-коррекция часов ({0:F1} сек)", delta.TotalSeconds),
											Status = "НОРМА",
											ThreatLevel = "SAFE",
											Verdict = "Штатная фоновая синхронизация времени Windows.",
											TargetRule = "РАЗРЕШЕНО",
											IsViolation = false,
											IsNormal = true,
											Timestamp = eventTime,
											RowColor = Ui.AccentGreen,
											TimeDelta = delta,
											TechnicalData = string.Format("OldTime: {0}, NewTime: {1}, Delta: {2:F2}s", oldTime, newTime, delta.TotalSeconds),
											DetailDescription = "Обычная синхронизация NTP службы времени."
										});
									}
								}
							}
							else if (eventId == 41) // Kernel-Power: Improper shutdown
							{
								TimeSpan timeSince = DateTime.Now - eventTime;
								bool isRecent = timeSince.TotalMinutes < 120.0;

								results.Add(new TimeAnomalyEvidenceItem
								{
									Index = idx++,
									Category = "Перезагрузка перед SS (5.08)",
									Source = "System.evtx (Event ID 41: Kernel-Power)",
									DetectedAnomaly = string.Format("Аварийное выключение ПК ({0:F0} мин назад)", timeSince.TotalMinutes),
									Status = isRecent ? "ЖЕСТКИЙ СБРОС ПИТАНИЯ (5.08)" : "СТАРЫЙ СБОЙ ПИТАНИЯ",
									ThreatLevel = isRecent ? "CRITICAL" : "INFO",
									Verdict = isRecent 
										? string.Format("🚨 БАН 5.08: Зафиксировано принудительное отключение питания ПК за {0:F0} мин до проверки (очистка ОЗУ)!", timeSince.TotalMinutes)
										: "Событие отключения питания старше 2 часов.",
									TargetRule = isRecent ? "5.08" : "РАЗРЕШЕНО",
									IsViolation = isRecent,
									IsNormal = !isRecent,
									Timestamp = eventTime,
									RowColor = isRecent ? Ui.AccentRed : Ui.TextSecondary,
									TechnicalData = "Система перезагрузилась без предварительного чистого завершения работы (выключение кнопкой питания).",
									DetailDescription = "Аварийное выключение питания (зажатием кнопки Power) часто используется недобросовестными игроками при вызове на проверку для уничтожения данных в оперативной памяти."
								});
							}
							else if (eventId == 12 || eventId == 13) // Kernel-General: OS Boot / OS Shutdown
							{
								TimeSpan timeSince = DateTime.Now - eventTime;
								bool isRecent = timeSince.TotalMinutes < 60.0;

								results.Add(new TimeAnomalyEvidenceItem
								{
									Index = idx++,
									Category = "Перезагрузка перед SS (5.08)",
									Source = string.Format("System.evtx (Event ID {0}: Kernel-General)", eventId),
									DetectedAnomaly = string.Format("{0} системы ({1:F0} мин назад)", eventId == 12 ? "Запуск" : "Завершение работы", timeSince.TotalMinutes),
									Status = isRecent ? "ПЕРЕЗАГРУЗКА ПЕРЕД ПРОВЕРКОЙ" : "НОРМА",
									ThreatLevel = isRecent ? "WARNING" : "INFO",
									Verdict = isRecent
										? string.Format("Внимание: Компьютер был перезагружен за {0:F0} минут до текущей проверки.", timeSince.TotalMinutes)
										: "Штатный запуск системы.",
									TargetRule = isRecent ? "5.08 (Подозрение)" : "РАЗРЕШЕНО",
									IsViolation = false,
									IsNormal = !isRecent,
									Timestamp = eventTime,
									RowColor = isRecent ? Ui.AccentOrange : Ui.TextSecondary,
									TechnicalData = string.Format("Событие {0}, время: {1:yyyy-MM-dd HH:mm:ss}", eventId == 12 ? "OS Boot" : "OS Shutdown", eventTime),
									DetailDescription = "Перезагрузка перед проверкой сбрасывает сетевые сокеты и запущенные в фоне процессы читов."
								});
							}
						}
					}
				}
			}
			catch { }

			// 2. Audit Windows Time Service (w32time) Registry Configuration
			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\W32Time\Parameters"))
				{
					if (key != null)
					{
						string type = (key.GetValue("Type") ?? "").ToString();
						string ntpServer = (key.GetValue("NtpServer") ?? "").ToString();

						if (type.Equals("NoSync", StringComparison.OrdinalIgnoreCase))
						{
							results.Add(new TimeAnomalyEvidenceItem
							{
								Index = idx++,
								Category = "Служба времени W32Time",
								Source = @"HKLM\...\W32Time\Parameters",
								DetectedAnomaly = "Автоматическая синхронизация времени ОТКЛЮЧЕНА (Type = NoSync)",
								Status = "ОТКЛЮЧЕНА СИНХРОНИЗАЦИЯ",
								ThreatLevel = "WARNING",
								Verdict = "Служба времени Windows переведена в режим NoSync (время не сверяется с серверами).",
								TargetRule = "5.08 (Подозрение)",
								IsViolation = false,
								IsNormal = false,
								Timestamp = DateTime.Now,
								RowColor = Ui.AccentOrange,
								TechnicalData = string.Format("Type: {0}, NtpServer: {1}", type, ntpServer),
								DetailDescription = "Отключение автоматической синхронизации часов облегчает манипуляции с временными метками файлов."
							});
						}
						else
						{
							results.Add(new TimeAnomalyEvidenceItem
							{
								Index = idx++,
								Category = "Служба времени W32Time",
								Source = @"HKLM\...\W32Time\Parameters",
								DetectedAnomaly = string.Format("NTP Синхронизация активна ({0})", type),
								Status = "НОРМА",
								ThreatLevel = "SAFE",
								Verdict = "Служба W32Time работает в штатном режиме.",
								TargetRule = "РАЗРЕШЕНО",
								IsViolation = false,
								IsNormal = true,
								Timestamp = DateTime.Now,
								RowColor = Ui.AccentGreen,
								TechnicalData = string.Format("Type: {0}, NtpServer: {1}", type, ntpServer),
								DetailDescription = "Конфигурация службы времени соответствует стандартной политике Windows."
							});
						}
					}
				}
			}
			catch { }

			// 3. Scan for "Time Travel" Files (Modified in the future)
			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcDir = Path.Combine(appData, ".minecraft");
				string tempDir = Path.GetTempPath();
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");

				List<string> checkDirs = new List<string>();
				if (Directory.Exists(mcDir)) checkDirs.Add(mcDir);
				if (Directory.Exists(tempDir)) checkDirs.Add(tempDir);
				if (Directory.Exists(pfDir)) checkDirs.Add(pfDir);

				DateTime nowThreshold = DateTime.Now.AddMinutes(5.0);

				foreach (string dir in checkDirs)
				{
					try
					{
						string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
						foreach (string f in files)
						{
							try
							{
								FileInfo fi = new FileInfo(f);
								if (fi.LastWriteTime > nowThreshold || fi.CreationTime > nowThreshold)
								{
									DateTime futureDate = fi.LastWriteTime > fi.CreationTime ? fi.LastWriteTime : fi.CreationTime;
									TimeSpan diff = futureDate - DateTime.Now;

									results.Add(new TimeAnomalyEvidenceItem
									{
										Index = idx++,
										Category = "Файлы из будущего (Time Travel)",
										Source = f,
										DetectedAnomaly = string.Format("Файл из будущего: {0} (+{1:F0} мин)", fi.Name, diff.TotalMinutes),
										Status = "ВРЕМЕННАЯ АНОМАЛИЯ (5.08)",
										ThreatLevel = "CRITICAL",
										Verdict = string.Format("🚨 БАН 5.08: Файл '{0}' имеет штамп времени из будущего ({1:yyyy-MM-dd HH:mm:ss})!", fi.Name, futureDate),
										TargetRule = "5.08",
										IsViolation = true,
										IsNormal = false,
										Timestamp = futureDate,
										RowColor = Ui.AccentRed,
										TechnicalData = string.Format("CreationTime: {0}, LastWriteTime: {1}, CurrentTime: {2}", fi.CreationTime, fi.LastWriteTime, DateTime.Now),
										DetailDescription = string.Format("Штамп времени файла {0} опережает текущее системное время на {1:F0} минут. Это происходит, когда читер переводит часы вперед, создаёт/удаляет файлы, а затем возвращает время назад.", fi.Name, diff.TotalMinutes)
									});
								}
							}
							catch { }
						}
					}
					catch { }
				}
			}
			catch { }

			return results;
		}

		#endregion
	}
}
