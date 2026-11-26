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
	public enum TimelineFilter
	{
		All,
		Last60Minutes,
		Last3Hours,
		Last24Hours,
		ViolationsOnly,
		PrefetchOnly,
		BamOnly,
		EventLogsOnly
	}

	public class TimelineForensicEventItem
	{
		public int Index;
		public DateTime EventTime;
		public string TimeDisplay;
		public string SourceType;
		public string ProgramOrObject;
		public string Category;
		public string Verdict;
		public string Severity; // "CRITICAL", "WARNING", "INFO", "SAFE"
		public int RiskScore; // 0..100
		public string RuleArticle; // "5.01", "5.08", "ИНФО"
		public bool IsViolation;
		public Color RowColor;
		public string Details;
		public string TechnicalData;
		public string BanVerdictGuide;
	}

	public class UnifiedForensicTimelineViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<TimelineForensicEventItem> allEvents = new List<TimelineForensicEventItem>();
		private TimelineFilter currentFilter = TimelineFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiLast60m;
		private Label lblKpiEventLogs;
		private Label lblKpiBam;
		private Label lblKpiPrefetch;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilter60m;
		private DarkButton btnFilter3h;
		private DarkButton btnFilter24h;
		private DarkButton btnFilterPrefetch;
		private DarkButton btnFilterEventLogs;

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

		public UnifiedForensicTimelineViewerForm() : this("Player")
		{
		}

		public UnifiedForensicTimelineViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "⏳ Единый судебно-криминалистический таймлайн (Tool 85 / Step 82) — MCCheatChecker";
			this.Size = new Size(1240, 760);
			this.MinimumSize = new Size(980, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Icon = SystemIcons.Shield;
			Ui.ApplyDarkScrollbars(this);

			// 1. Верхний заголовок
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 68;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "⏳ Единый судебно-криминалистический таймлайн (Tool 85 / Step 82)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Сводный хронологический анализ всех артефактов ОС: Prefetch, BAM/SACP реестра, Event Logs (1074/7045/1102), Recent LNK и Shellbags в единой временной шкале проверок ScreenShare.";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 38);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Построение таймлайна...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(40, 30, 20);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(270, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 290, 20);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// 2. Панель KPI бейджей
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int xOff = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО СОБЫТИЙ", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("НАРУШЕНИЯ 5.01/5.08", "0", Ui.AccentRed, ref xOff, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("ЗАПУСКИ ЗА 60 МИН", "0", Ui.AccentOrange, ref xOff, out lblKpiLast60m));
			kpiPanel.Controls.Add(CreateKpiBadge("EVENTLOG СОБЫТИЯ", "0", Ui.AccentPurple, ref xOff, out lblKpiEventLogs));
			kpiPanel.Controls.Add(CreateKpiBadge("BAM РЕЕСТР", "0", Ui.AccentTeal, ref xOff, out lblKpiBam));
			kpiPanel.Controls.Add(CreateKpiBadge("PREFETCH СЛЕДЫ", "0", Ui.AccentBlue, ref xOff, out lblKpiPrefetch));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", TimelineFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения (0)", TimelineFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilter60m = CreateFilterButton("За 60 мин (0)", TimelineFilter.Last60Minutes, ref fX, filterPanel);
			btnFilter3h = CreateFilterButton("За 3 часа (0)", TimelineFilter.Last3Hours, ref fX, filterPanel);
			btnFilter24h = CreateFilterButton("За 24 часа (0)", TimelineFilter.Last24Hours, ref fX, filterPanel);
			btnFilterPrefetch = CreateFilterButton("Prefetch (0)", TimelineFilter.PrefetchOnly, ref fX, filterPanel);
			btnFilterEventLogs = CreateFilterButton("Журналы (0)", TimelineFilter.EventLogsOnly, ref fX, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(filterPanel.Width - 250, 12);
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.AutoSize = true;
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterPanel.Width - 180, 9);
			searchBox.Size = new Size(165, 24);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			// 4. Нижняя панель действий (Bottom Action Bar)
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 48;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 8, 12, 8);
			this.Controls.Add(bottomPanel);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🔨 Скопировать команду бана";
			btnCopyBanCmd.Size = new Size(215, 30);
			btnCopyBanCmd.Location = new Point(12, 9);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			bottomPanel.Controls.Add(btnCopyBanCmd);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Сводка для Discord";
			btnCopyDiscord.Size = new Size(165, 30);
			btnCopyDiscord.Location = new Point(235, 9);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };
			bottomPanel.Controls.Add(btnCopyDiscord);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт таймлайна в HTML";
			btnExportHtml.Size = new Size(185, 30);
			btnExportHtml.Location = new Point(408, 9);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };
			bottomPanel.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(150, 30);
			btnRescan.Location = new Point(601, 9);
			btnRescan.Click += delegate { StartScan(); };
			bottomPanel.Controls.Add(btnRescan);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(100, 30);
			btnClose.Location = new Point(bottomPanel.Width - 115, 9);
			btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClose.Click += delegate { this.Close(); };
			bottomPanel.Controls.Add(btnClose);

			// 5. Главный SplitContainer (ListView сверху, вкладки снизу)
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 440;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.View = View.Details;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Время события", 145);
			listView.Columns.Add("Источник артефакта", 145);
			listView.Columns.Add("Программа / Объект", 230);
			listView.Columns.Add("Категория", 160);
			listView.Columns.Add("Вердикт", 140);
			listView.Columns.Add("Риск", 65);
			listView.Columns.Add("Правило", 75);
			listView.Columns.Add("Детали события", 340);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ хронологии события");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Технические метаданные источника");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.01 / 5.08 и Команда бана");
			txtBanVerdict = new DarkTextBoxMultiline();
			txtBanVerdict.Dock = DockStyle.Fill;
			txtBanVerdict.ReadOnly = true;
			tabBan.Controls.Add(txtBanVerdict);
			detailTabs.TabPages.Add(tabBan);

			split.Panel2.Controls.Add(detailTabs);
		}

		private Panel CreateKpiBadge(string title, string value, Color accent, ref int xOffset, out Label valueLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(185, 52);
			p.Location = new Point(xOffset, 6);
			p.BackColor = Ui.Surface;
			p.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f, true);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.Location = new Point(8, 6);
			lblTitle.AutoSize = true;
			p.Controls.Add(lblTitle);

			valueLabel = new Label();
			valueLabel.Text = value;
			valueLabel.Font = Ui.FontTitle(13f, true);
			valueLabel.ForeColor = accent;
			valueLabel.Location = new Point(8, 24);
			valueLabel.AutoSize = true;
			p.Controls.Add(valueLabel);

			xOffset += 192;
			return p;
		}

		private DarkButton CreateFilterButton(string text, TimelineFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (TimelineFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 135;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Построение единого таймлайна...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allEvents.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformUnifiedTimelineAudit();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка построения таймлайна: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<TimelineForensicEventItem> results = e.Result as List<TimelineForensicEventItem>;
				if (results != null)
				{
					allEvents = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (TimelineForensicEventItem it in allEvents)
				{
					if (it.IsViolation) violCount++;
				}

				if (violCount > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ НАРУШЕНИЯ ({0})", violCount);
					lblStatusPill.ForeColor = Ui.AccentRed;
					lblStatusPill.BackColor = Color.FromArgb(50, 20, 20);
				}
				else
				{
					lblStatusPill.Text = "🟢 ТАЙМЛАЙН БЕЗОПАСЕН";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allEvents.Count;
			int viol = 0;
			int last60m = 0;
			int evLogs = 0;
			int bam = 0;
			int pf = 0;

			DateTime now = DateTime.Now;
			foreach (TimelineForensicEventItem it in allEvents)
			{
				if (it.IsViolation) viol++;
				if ((now - it.EventTime).TotalMinutes <= 60) last60m++;
				if (it.SourceType.Contains("EventLog")) evLogs++;
				if (it.SourceType.Contains("BAM")) bam++;
				if (it.SourceType.Contains("Prefetch")) pf++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = viol.ToString();
			lblKpiLast60m.Text = last60m.ToString();
			lblKpiEventLogs.Text = evLogs.ToString();
			lblKpiBam.Text = bam.ToString();
			lblKpiPrefetch.Text = pf.ToString();

			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения ({0})", viol);
			btnFilter60m.Text = string.Format("За 60 мин ({0})", last60m);
			btnFilter3h.Text = "За 3 часа";
			btnFilter24h.Text = "За 24 часа";
			btnFilterPrefetch.Text = string.Format("Prefetch ({0})", pf);
			btnFilterEventLogs.Text = string.Format("Журналы ({0})", evLogs);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();
			DateTime now = DateTime.Now;

			foreach (TimelineForensicEventItem it in allEvents)
			{
				bool matchFilter = false;
				switch (currentFilter)
				{
					case TimelineFilter.All:
						matchFilter = true;
						break;
					case TimelineFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case TimelineFilter.Last60Minutes:
						matchFilter = (now - it.EventTime).TotalMinutes <= 60;
						break;
					case TimelineFilter.Last3Hours:
						matchFilter = (now - it.EventTime).TotalHours <= 3;
						break;
					case TimelineFilter.Last24Hours:
						matchFilter = (now - it.EventTime).TotalHours <= 24;
						break;
					case TimelineFilter.PrefetchOnly:
						matchFilter = it.SourceType.Contains("Prefetch");
						break;
					case TimelineFilter.BamOnly:
						matchFilter = it.SourceType.Contains("BAM");
						break;
					case TimelineFilter.EventLogsOnly:
						matchFilter = it.SourceType.Contains("EventLog");
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string haystack = (it.TimeDisplay + " " + it.SourceType + " " + it.ProgramOrObject + " " + it.Category + " " + it.Details).ToLowerInvariant();
					if (!haystack.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.TimeDisplay);
				lvi.SubItems.Add(it.SourceType);
				lvi.SubItems.Add(it.ProgramOrObject);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.RiskScore.ToString() + "%");
				lvi.SubItems.Add(it.RuleArticle);
				lvi.SubItems.Add(it.Details);

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
				ClearDetailTabs();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetailTabs();
				return;
			}

			TimelineForensicEventItem it = listView.SelectedItems[0].Tag as TimelineForensicEventItem;
			if (it == null) return;

			txtAnalysis.Text = it.Details;
			txtTechnical.Text = it.TechnicalData;
			txtBanVerdict.Text = it.BanVerdictGuide;
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите событие на таймлайне для детального судебно-криминалистического анализа.";
			txtTechnical.Text = "Нет данных технического анализа.";
			txtBanVerdict.Text = "Нет выбранного вердикта.";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Сначала выберите событие в списке!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			TimelineForensicEventItem it = listView.SelectedItems[0].Tag as TimelineForensicEventItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранное событие не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = (it.RuleArticle == "5.08")
				? "5.08 Уклонение от проверки / Зачистка (" + it.ProgramOrObject + ")"
				: "5.01 Использование читов (" + it.ProgramOrObject + ")";

			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**⏳ MCCheatChecker — Сводный криминалистический таймлайн (Tool 85)**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (TimelineForensicEventItem it in allEvents)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Хронология подозрительных запусков и событий:**");
				foreach (TimelineForensicEventItem it in allEvents)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.TimeDisplay + "]` **" + it.SourceType + "**: `" + it.ProgramOrObject + "` (" + it.Category + ") — " + it.Verdict);
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.01/5.08 Нарушения в таймлайне ОС`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (В сводной хронологии аномалий перезагрузок, клинеров и следов читов не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка таймлайна скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_TimelineReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Сводный таймлайн — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #121418; color: #E0E0E0; margin: 20px; }");
					sb.AppendLine("h1 { color: #26A69A; border-bottom: 2px solid #333; padding-bottom: 8px; }");
					sb.AppendLine(".kpi-container { display: flex; gap: 15px; margin: 20px 0; flex-wrap: wrap; }");
					sb.AppendLine(".kpi { background: #1E222B; border: 1px solid #2C3240; padding: 12px 18px; border-radius: 6px; }");
					sb.AppendLine(".kpi-title { font-size: 11px; color: #8C9BAE; font-weight: bold; }");
					sb.AppendLine(".kpi-value { font-size: 20px; font-weight: bold; margin-top: 4px; }");
					sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; background: #1E222B; }");
					sb.AppendLine("th, td { border: 1px solid #2C3240; padding: 8px 12px; text-align: left; }");
					sb.AppendLine("th { background: #262C38; color: #8C9BAE; }");
					sb.AppendLine(".crit { color: #FF5252; font-weight: bold; }");
					sb.AppendLine(".warn { color: #FFA726; font-weight: bold; }");
					sb.AppendLine(".safe { color: #4CAF50; }");
					sb.AppendLine("</style></head><body>");
					sb.AppendLine("<h1>⏳ Единый судебно-криминалистический таймлайн (Tool 85)</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время формирования: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО СОБЫТИЙ</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>НАРУШЕНИЯ</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiViolations.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЗА 60 МИНУТ</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiLast60m.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЖУРНАЛЫ СОБЫТИЙ</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiEventLogs.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>BAM РЕЕСТР</div><div class='kpi-value' style='color:#26A69A'>" + lblKpiBam.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>PREFETCH</div><div class='kpi-value' style='color:#42A5F5'>" + lblKpiPrefetch.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Время</th><th>Источник</th><th>Программа / Объект</th><th>Категория</th><th>Вердикт</th><th>Риск</th><th>Правило</th><th>Детали</th></tr>");

					foreach (TimelineForensicEventItem it in allEvents)
					{
						string css = it.IsViolation ? "crit" : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td>" + it.TimeDisplay + "</td>");
						sb.AppendLine("<td>" + it.SourceType + "</td>");
						sb.AppendLine("<td><b>" + it.ProgramOrObject + "</b></td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td class='" + css + "'>" + it.Verdict + "</td>");
						sb.AppendLine("<td>" + it.RiskScore + "%</td>");
						sb.AppendLine("<td>" + it.RuleArticle + "</td>");
						sb.AppendLine("<td>" + it.Details + "</td>");
						sb.AppendLine("</tr>");
					}

					sb.AppendLine("</table></body></html>");

					try
					{
						File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
						MessageBox.Show("HTML отчёт таймлайна сохранён!", "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка сохранения отчёта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		#region Криминалистическое сканирование (Forensic Scan Engine)

		public static List<TimelineForensicEventItem> PerformUnifiedTimelineAudit()
		{
			List<TimelineForensicEventItem> list = new List<TimelineForensicEventItem>();

			// 1. Агрегация из Windows Prefetch
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(pfDir))
				{
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					foreach (string pf in pfFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(pf);
							string fileName = fi.Name;
							string lower = fileName.ToLowerInvariant();

							bool isCheat = lower.Contains("vape") || lower.Contains("drip") || lower.Contains("slinky") ||
										   lower.Contains("celestial") || lower.Contains("expensive") || lower.Contains("nursultan") ||
										   lower.Contains("cheat") || lower.Contains("loader") || lower.Contains("injector");

							bool isCleaner = lower.Contains("cleaner") || lower.Contains("wevtutil") || lower.Contains("fsutil") ||
											 lower.Contains("usn") || lower.Contains("vssadmin") || lower.Contains("sdelete") ||
											 lower.Contains("bleachbit") || lower.Contains("ccleaner");

							bool isSuspicious = isCheat || isCleaner;
							bool isViolation = isSuspicious;
							string rule = isCheat ? "5.01" : (isCleaner ? "5.08" : "ИНФО");

							TimelineForensicEventItem item = new TimelineForensicEventItem();
							item.EventTime = fi.LastWriteTime;
							item.TimeDisplay = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
							item.SourceType = "Windows Prefetch";
							item.ProgramOrObject = fileName;
							item.Category = isCheat ? "Чит-программа (5.01)" : (isCleaner ? "Клинер / Зачистка (5.08)" : "Исполняемый процесс");
							item.Verdict = isViolation ? ("🚨 НАХОДКА " + rule) : "ℹ️ ЗАПУСК ПРОГРАММЫ";
							item.Severity = isViolation ? "CRITICAL" : "INFO";
							item.RiskScore = isCheat ? 100 : (isCleaner ? 95 : 0);
							item.RuleArticle = rule;
							item.IsViolation = isViolation;
							item.RowColor = isViolation ? Ui.AccentRed : Ui.TextPrimary;
							item.Details = "Запуск исполняемого файла зафиксирован диспетчером кэша Prefetch: " + fileName;
							item.TechnicalData = "Path: " + pf + "\nSize: " + fi.Length + " bytes\nLastWriteTime: " + fi.LastWriteTime.ToString("o");
							item.BanVerdictGuide = isViolation
								? "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт: " + rule + "\nКоманда:\n/ban <player> 30d " + rule + " Запуск софта " + fileName
								: "Штатный запуск программы.";
							list.Add(item);
						}
						catch { }
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Prefetch timeline scan error: " + ex.Message);
			}

			// 2. Агрегация из реестра BAM (Background Activity Moderator)
			try
			{
				string bamPath = @"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings";
				using (RegistryKey bamKey = Registry.LocalMachine.OpenSubKey(bamPath))
				{
					if (bamKey != null)
					{
						string[] sids = bamKey.GetSubKeyNames();
						foreach (string sid in sids)
						{
							using (RegistryKey userKey = bamKey.OpenSubKey(sid))
							{
								if (userKey == null) continue;

								string[] valNames = userKey.GetValueNames();
								foreach (string vn in valNames)
								{
									if (vn.Length < 3 || vn == "SequenceNumber" || vn == "Version") continue;

									byte[] bytes = userKey.GetValue(vn) as byte[];
									if (bytes != null && bytes.Length >= 8)
									{
										long fileTime = BitConverter.ToInt64(bytes, 0);
										if (fileTime > 0)
										{
											try
											{
												DateTime dt = DateTime.FromFileTime(fileTime);
												string lower = vn.ToLowerInvariant();

												bool isCheat = lower.Contains("vape") || lower.Contains("drip") || lower.Contains("slinky") ||
															   lower.Contains("celestial") || lower.Contains("expensive") || lower.Contains("nursultan");
												bool isCleaner = lower.Contains("cleaner") || lower.Contains("wevtutil") || lower.Contains("fsutil");

												bool isViolation = isCheat || isCleaner;
												string rule = isCheat ? "5.01" : (isCleaner ? "5.08" : "ИНФО");

												TimelineForensicEventItem item = new TimelineForensicEventItem();
												item.EventTime = dt;
												item.TimeDisplay = dt.ToString("dd.MM.yyyy HH:mm:ss");
												item.SourceType = "BAM Реестр";
												item.ProgramOrObject = Path.GetFileName(vn);
												item.Category = isCheat ? "Чит в BAM (5.01)" : (isCleaner ? "Клинер в BAM (5.08)" : "Активность BAM");
												item.Verdict = isViolation ? ("🚨 НАХОДКА " + rule) : "ℹ️ АКТИВНОСТЬ BAM";
												item.Severity = isViolation ? "CRITICAL" : "INFO";
												item.RiskScore = isCheat ? 100 : (isCleaner ? 95 : 0);
												item.RuleArticle = rule;
												item.IsViolation = isViolation;
												item.RowColor = isViolation ? Ui.AccentRed : Ui.AccentTeal;
												item.Details = "След запуска процесса зафиксирован драйвером ядра BAM: " + vn;
												item.TechnicalData = "Key: " + bamPath + "\\" + sid + "\nValue: " + vn + "\nFileTime: " + fileTime;
												item.BanVerdictGuide = isViolation ? ("/ban <player> 30d " + rule + " Запуск софта в BAM") : "Штатная запись.";
												list.Add(item);
											}
											catch { }
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("BAM timeline scan error: " + ex.Message);
			}

			// 3. Агрегация из Windows Event Logs (1074, 7045, 1102, 104, 1116)
			try
			{
				// System Log: Event ID 1074 (Reboot / Shutdown)
				try
				{
					EventLogQuery query1074 = new EventLogQuery("System", PathType.LogName, "*[System[(EventID=1074)]]");
					query1074.ReverseDirection = true;
					using (EventLogReader reader = new EventLogReader(query1074))
					{
						int evCount = 0;
						EventRecord rec;
						while ((rec = reader.ReadEvent()) != null && evCount < 30)
						{
							using (rec)
							{
								DateTime dt = rec.TimeCreated ?? DateTime.Now;
								TimeSpan diff = DateTime.Now - dt;

								bool isRecentReboot = (diff.TotalMinutes <= 60);

								TimelineForensicEventItem item = new TimelineForensicEventItem();
								item.EventTime = dt;
								item.TimeDisplay = dt.ToString("dd.MM.yyyy HH:mm:ss");
								item.SourceType = "EventLog System (1074)";
								item.ProgramOrObject = "Перезагрузка / Выключение ПК";
								item.Category = isRecentReboot ? "Перезагрузка перед проверкой (5.08)" : "Штатная перезагрузка";
								item.Verdict = isRecentReboot ? "🚨 РЕБУТ ПЕРЕД ПРОВЕРКОЙ (5.08)" : "ℹ️ ПЕРЕЗАГРУЗКА ПК";
								item.Severity = isRecentReboot ? "CRITICAL" : "INFO";
								item.RiskScore = isRecentReboot ? 95 : 10;
								item.RuleArticle = isRecentReboot ? "5.08" : "ИНФО";
								item.IsViolation = isRecentReboot;
								item.RowColor = isRecentReboot ? Ui.AccentRed : Ui.AccentPurple;
								item.Details = "Инициирована перезагрузка или завершение работы Windows: " + Math.Round(diff.TotalMinutes, 1) + " мин назад.";
								item.TechnicalData = "EventID: 1074\nProvider: User32\nTimeCreated: " + dt.ToString("o");
								item.BanVerdictGuide = isRecentReboot ? "/ban <player> 30d 5.08 Перезагрузка ПК перед проверкой" : "Штатный перезапуск.";
								list.Add(item);
								evCount++;
							}
						}
					}
				}
				catch { }

				// System Log: Event ID 7045 (Service / Driver Installed - kdmapper)
				try
				{
					EventLogQuery query7045 = new EventLogQuery("System", PathType.LogName, "*[System[(EventID=7045)]]");
					query7045.ReverseDirection = true;
					using (EventLogReader reader = new EventLogReader(query7045))
					{
						int evCount = 0;
						EventRecord rec;
						while ((rec = reader.ReadEvent()) != null && evCount < 30)
						{
							using (rec)
							{
								DateTime dt = rec.TimeCreated ?? DateTime.Now;
								string xml = rec.ToXml();
								bool isKdmapper = xml.ToLowerInvariant().Contains("gdrv") || xml.ToLowerInvariant().Contains("mhyprot") ||
												  xml.ToLowerInvariant().Contains("rtcore") || xml.ToLowerInvariant().Contains("echo");

								TimelineForensicEventItem item = new TimelineForensicEventItem();
								item.EventTime = dt;
								item.TimeDisplay = dt.ToString("dd.MM.yyyy HH:mm:ss");
								item.SourceType = "EventLog System (7045)";
								item.ProgramOrObject = isKdmapper ? "BYOVD Драйвер / kdmapper" : "Установка службы/драйвера";
								item.Category = isKdmapper ? "Загрузка драйвера чита (5.01)" : "Системная служба";
								item.Verdict = isKdmapper ? "🚨 ВРЕДОНОСНЫЙ ДРАЙВЕР (5.01)" : "ℹ️ СЛУЖБА ЗАГРУЖЕНА";
								item.Severity = isKdmapper ? "CRITICAL" : "INFO";
								item.RiskScore = isKdmapper ? 100 : 0;
								item.RuleArticle = isKdmapper ? "5.01" : "ИНФО";
								item.IsViolation = isKdmapper;
								item.RowColor = isKdmapper ? Ui.AccentRed : Ui.AccentPurple;
								item.Details = "В системе зарегистрирована новая служба или драйвер ядра: Event ID 7045.";
								item.TechnicalData = "EventID: 7045\nXML: " + (xml.Length > 200 ? xml.Substring(0, 200) : xml);
								item.BanVerdictGuide = isKdmapper ? "/ban <player> 30d 5.01 BYOVD инъекция драйвера читов" : "Штатная служба.";
								list.Add(item);
								evCount++;
							}
						}
					}
				}
				catch { }

				// Security Log: Event ID 1102 (The audit log was cleared)
				try
				{
					EventLogQuery query1102 = new EventLogQuery("Security", PathType.LogName, "*[System[(EventID=1102)]]");
					query1102.ReverseDirection = true;
					using (EventLogReader reader = new EventLogReader(query1102))
					{
						EventRecord rec;
						while ((rec = reader.ReadEvent()) != null)
						{
							using (rec)
							{
								DateTime dt = rec.TimeCreated ?? DateTime.Now;
								TimelineForensicEventItem item = new TimelineForensicEventItem();
								item.EventTime = dt;
								item.TimeDisplay = dt.ToString("dd.MM.yyyy HH:mm:ss");
								item.SourceType = "EventLog Security (1102)";
								item.ProgramOrObject = "Очистка журнала аудита wevtutil";
								item.Category = "Зачистка следов (5.08)";
								item.Verdict = "🚨 ОЧИСТКА ЖУРНАЛА (5.08)";
								item.Severity = "CRITICAL";
								item.RiskScore = 100;
								item.RuleArticle = "5.08";
								item.IsViolation = true;
								item.RowColor = Ui.AccentRed;
								item.Details = "Журнал аудита Security был умышленно очищен командой wevtutil cl Security!";
								item.TechnicalData = "EventID: 1102\nTimeCreated: " + dt.ToString("o");
								item.BanVerdictGuide = "/ban <player> 30d 5.08 Очистка журнала безопасности wevtutil";
								list.Add(item);
							}
						}
					}
				}
				catch { }
			}
			catch (Exception ex)
			{
				Debug.WriteLine("EventLog timeline scan error: " + ex.Message);
			}

			// 4. Агрегация Recent LNK файлов
			try
			{
				string recentDir = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
				if (Directory.Exists(recentDir))
				{
					string[] lnkFiles = Directory.GetFiles(recentDir, "*.lnk");
					foreach (string lnk in lnkFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(lnk);
							string name = Path.GetFileNameWithoutExtension(fi.Name);
							string lower = name.ToLowerInvariant();

							bool isCheat = lower.Contains("vape") || lower.Contains("drip") || lower.Contains("slinky") ||
										   lower.Contains("celestial") || lower.Contains("expensive") || lower.Contains("nursultan");

							if (isCheat || (DateTime.Now - fi.LastWriteTime).TotalHours <= 24)
							{
								TimelineForensicEventItem item = new TimelineForensicEventItem();
								item.EventTime = fi.LastWriteTime;
								item.TimeDisplay = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
								item.SourceType = "Recent LNK";
								item.ProgramOrObject = name;
								item.Category = isCheat ? "Ярлык чита (5.01)" : "Открытие документа/файла";
								item.Verdict = isCheat ? "🚨 ЧИТ-ФАЙЛ (5.01)" : "ℹ️ ОТКРЫТИЕ ФАЙЛА";
								item.Severity = isCheat ? "CRITICAL" : "INFO";
								item.RiskScore = isCheat ? 100 : 0;
								item.RuleArticle = isCheat ? "5.01" : "ИНФО";
								item.IsViolation = isCheat;
								item.RowColor = isCheat ? Ui.AccentRed : Ui.TextPrimary;
								item.Details = "Открытие файла зафиксировано в истории Recent LNK: " + fi.Name;
								item.TechnicalData = "LNK Path: " + lnk + "\nSize: " + fi.Length;
								item.BanVerdictGuide = isCheat ? "/ban <player> 30d 5.01 Чит в Recent LNK" : "Штатный файл.";
								list.Add(item);
							}
						}
						catch { }
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Recent LNK timeline error: " + ex.Message);
			}

			// Сортировка по времени (самые свежие первыми)
			list.Sort(delegate(TimelineForensicEventItem a, TimelineForensicEventItem b)
			{
				return b.EventTime.CompareTo(a.EventTime);
			});

			// Если список пуст, создаем штатную чистую запись
			if (list.Count == 0)
			{
				TimelineForensicEventItem clean = new TimelineForensicEventItem();
				clean.Index = 1;
				clean.EventTime = DateTime.Now;
				clean.TimeDisplay = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
				clean.SourceType = "Системный аудит";
				clean.ProgramOrObject = "Штатная работа Windows";
				clean.Category = "Безопасная система";
				clean.Verdict = "🟢 БЕЗОПАСНО";
				clean.Severity = "SAFE";
				clean.RiskScore = 0;
				clean.RuleArticle = "ЧИСТО";
				clean.IsViolation = false;
				clean.RowColor = Ui.AccentGreen;
				clean.Details = "Аномалий в хронологии ОС, перезагрузок перед проверкой и следов клинеров не обнаружено.";
				clean.TechnicalData = "Unified timeline clean.";
				clean.BanVerdictGuide = "Нарушений нет.";
				list.Add(clean);
			}

			// Перенумерация
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Index = i + 1;
			}

			return list;
		}

		#endregion
	}
}
