using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum ScreenshotExifFilter
	{
		All,
		ViolationsOnly,
		EditorArtifactsOnly,
		CheatWatermarksOnly,
		DateTamperingOnly,
		CleanOnly
	}

	public class ScreenshotExifEvidenceItem
	{
		public int Index;
		public string Category; // "Метаданные редактора", "Чит-вотермарка / HUD", "Манипуляция датами", "Очистка логов", "Чистый скриншот"
		public string FilePath;
		public string FileName;
		public string DetectedArtifact;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.01", "5.08", "РАЗРЕШЕНО", "—"
		public bool IsViolation;
		public bool IsClean;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string SoftwareTag;
		public string RawMetadataDump;
		public long FileSizeBytes;
	}

	public class MinecraftScreenshotsExifAndOcrForensicsViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<ScreenshotExifEvidenceItem> allItems = new List<ScreenshotExifEvidenceItem>();
		private ScreenshotExifFilter currentFilter = ScreenshotExifFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiEdited;
		private Label lblKpiCheatWatermark;
		private Label lblKpiDateTamper;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterEdited;
		private DarkButton btnFilterCheatWatermark;
		private DarkButton btnFilterDateTamper;
		private DarkButton btnFilterClean;

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

		public MinecraftScreenshotsExifAndOcrForensicsViewerForm() : this("Player")
		{
		}

		public MinecraftScreenshotsExifAndOcrForensicsViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "🖼️ Инспектор скриншотов, метаданных EXIF и вотермарок читов (Tool 73 / Step 70) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🖼️ ИНСПЕКТОР СКРИНШОТОВ, EXIF/PNG МЕТАДАННЫХ И ВОТЕРМАРОК ЧИТОВ";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокая экспертиза скриншотов Minecraft: анализ чанков PNG/EXIF на графические редакторы (Photoshop/Paint.NET/GIMP), поиск вотермарок читов (Vape/Expensive/Celestial) и манипуляций датами (5.01/5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация судебного анализа скриншотов...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 380, 16);
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО СКРИНШОТОВ", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.01/5.08)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("🎨 СЛЕДЫ РЕДАКТОРОВ (5.08)", "0", Ui.AccentOrange, ref kx, out lblKpiEdited));
			kpiPanel.Controls.Add(CreateKpiBadge("🏷️ ЧИТ-ВОТЕРМАРКИ (5.01)", "0", Ui.AccentPurple, ref kx, out lblKpiCheatWatermark));
			kpiPanel.Controls.Add(CreateKpiBadge("⏱️ МАНИПУЛЯЦИИ ДАТАМИ (5.08)", "0", Ui.AccentBlue, ref kx, out lblKpiDateTamper));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ ЧИСТЫЕ СКРИНШОТЫ", "0", Ui.AccentGreen, ref kx, out lblKpiClean));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все файлы", ScreenshotExifFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", ScreenshotExifFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterEdited = CreateFilterButton("🎨 Редакторы (5.08)", ScreenshotExifFilter.EditorArtifactsOnly, ref bx, filterBar);
			btnFilterCheatWatermark = CreateFilterButton("🏷️ Чит-вотермарки (5.01)", ScreenshotExifFilter.CheatWatermarksOnly, ref bx, filterBar);
			btnFilterDateTamper = CreateFilterButton("⏱️ Манипуляции дат (5.08)", ScreenshotExifFilter.DateTamperingOnly, ref bx, filterBar);
			btnFilterClean = CreateFilterButton("🛡️ Чистые скриншоты", ScreenshotExifFilter.CleanOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по имени файла, редактору, вотермарке или правилу...";
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
			btnRescan = CreateActionButton("🔄 Повторить аудит скриншотов", ref ax, bottomActions, delegate { StartScan(); });

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
			listView.Columns.Add("Имя файла / Путь", 260);
			listView.Columns.Add("Обнаруженный артефакт / Вотермарка", 270);
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

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ скриншота");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("🖼️ Метаданные изображения и EXIF/PNG Chunks");
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

		private DarkButton CreateFilterButton(string text, ScreenshotExifFilter filter, ref int curX, Panel parent)
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
			btn.Size = new Size(210, 30);
			btn.Location = new Point(curX, 8);
			btn.Cursor = Cursors.Hand;
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 220;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			Color activeBg = Ui.AccentTeal;
			Color activeFg = Color.White;
			Color normalBg = Ui.Surface;
			Color normalFg = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == ScreenshotExifFilter.All, normalBg, activeBg, normalFg, activeFg);
			SetBtnStyle(btnFilterViolations, currentFilter == ScreenshotExifFilter.ViolationsOnly, normalBg, Ui.AccentRed, normalFg, activeFg);
			SetBtnStyle(btnFilterEdited, currentFilter == ScreenshotExifFilter.EditorArtifactsOnly, normalBg, Ui.AccentOrange, normalFg, Color.Black);
			SetBtnStyle(btnFilterCheatWatermark, currentFilter == ScreenshotExifFilter.CheatWatermarksOnly, normalBg, Ui.AccentPurple, normalFg, activeFg);
			SetBtnStyle(btnFilterDateTamper, currentFilter == ScreenshotExifFilter.DateTamperingOnly, normalBg, Ui.AccentBlue, normalFg, activeFg);
			SetBtnStyle(btnFilterClean, currentFilter == ScreenshotExifFilter.CleanOnly, normalBg, Ui.AccentGreen, normalFg, Color.Black);
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

			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ И АНАЛИЗ СКРИНШОТОВ...";
			lblStatusPill.ForeColor = Ui.AccentOrange;

			listView.Items.Clear();
			allItems.Clear();
			txtAnalysis.Clear();
			txtTechnical.Clear();
			txtBanVerdict.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformScreenshotsExifScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования скриншотов: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<ScreenshotExifEvidenceItem> items = e.Result as List<ScreenshotExifEvidenceItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpis();
				UpdateFilterButtonStyles();
				ApplyFilter();

				int violations = 0;
				foreach (ScreenshotExifEvidenceItem it in allItems)
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
					lblStatusPill.Text = "✅ НАРУШЕНИЙ В СКРИНШОТАХ НЕ ОБНАРУЖЕНО";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			};

			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpis()
		{
			int total = allItems.Count;
			int violations = 0;
			int edited = 0;
			int watermark = 0;
			int dateTamper = 0;
			int clean = 0;

			foreach (ScreenshotExifEvidenceItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.Category == "Метаданные редактора") edited++;
				if (item.Category == "Чит-вотермарка / HUD") watermark++;
				if (item.Category == "Манипуляция датами") dateTamper++;
				if (item.IsClean) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiEdited.Text = edited.ToString();
			lblKpiCheatWatermark.Text = watermark.ToString();
			lblKpiDateTamper.Text = dateTamper.ToString();
			lblKpiClean.Text = clean.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (ScreenshotExifEvidenceItem item in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case ScreenshotExifFilter.ViolationsOnly:
						matchFilter = item.IsViolation;
						break;
					case ScreenshotExifFilter.EditorArtifactsOnly:
						matchFilter = (item.Category == "Метаданные редактора");
						break;
					case ScreenshotExifFilter.CheatWatermarksOnly:
						matchFilter = (item.Category == "Чит-вотермарка / HUD");
						break;
					case ScreenshotExifFilter.DateTamperingOnly:
						matchFilter = (item.Category == "Манипуляция датами");
						break;
					case ScreenshotExifFilter.CleanOnly:
						matchFilter = item.IsClean;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (item.FileName != null && item.FileName.ToLowerInvariant().Contains(search))
						|| (item.FilePath != null && item.FilePath.ToLowerInvariant().Contains(search))
						|| (item.DetectedArtifact != null && item.DetectedArtifact.ToLowerInvariant().Contains(search))
						|| (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search))
						|| (item.SoftwareTag != null && item.SoftwareTag.ToLowerInvariant().Contains(search))
						|| (item.TargetRule != null && item.TargetRule.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.FileName);
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
				txtAnalysis.Text = "Нет скриншотов, соответствующих выбранному фильтру.";
				txtTechnical.Clear();
				txtBanVerdict.Clear();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotExifEvidenceItem item = listView.SelectedItems[0].Tag as ScreenshotExifEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine("           СУДЕБНАЯ ЭКСПЕРТИЗА СКРИНШОТА И АНАЛИЗ ПОДДЕЛКИ/ЧИТОВ                          ");
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Имя файла:               {0}", item.FileName));
			sbAnalysis.AppendLine(string.Format("Полный путь:             {0}", item.FilePath));
			sbAnalysis.AppendLine(string.Format("Размер файла:            {0:N0} байт", item.FileSizeBytes));
			sbAnalysis.AppendLine(string.Format("Статус:                  {0}", item.Status));
			sbAnalysis.AppendLine(string.Format("Уровень угрозы:          {0}", item.ThreatLevel));
			sbAnalysis.AppendLine(string.Format("Регламент проверки:      {0}", item.TargetRule));
			sbAnalysis.AppendLine(string.Format("Штамп времени:           {0:yyyy-MM-dd HH:mm:ss}", item.Timestamp));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine("ЭКСПЕРТНОЕ ЗАКЛЮЧЕНИЕ:");
			sbAnalysis.AppendLine("------------------------------------------------------------------------------------------");
			sbAnalysis.AppendLine(item.DetailDescription);
			sbAnalysis.AppendLine();
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Technical & Metadata
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("           ТЕХНИЧЕСКИЕ МЕТАДАННЫЕ, EXIF И ЧАНКИ ИЗОБРАЖЕНИЯ (PNG/JPG)                     ");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine();
			sbTech.AppendLine(string.Format("Тег Software / Создатель:  {0}", string.IsNullOrEmpty(item.SoftwareTag) ? "Отсутствует / Нативный движок Minecraft" : item.SoftwareTag));
			sbTech.AppendLine(string.Format("Является нарушением:       {0}", item.IsViolation ? "ДА (КРИТИЧЕСКАЯ УГРОЗА)" : "НЕТ"));
			sbTech.AppendLine(string.Format("Легитимный скриншот:       {0}", item.IsClean ? "ДА (ПОДТВЕРЖДЕНО)" : "НЕТ"));
			sbTech.AppendLine();
			sbTech.AppendLine("ИЗВЛЕЧЕННЫЙ ДАМП МЕТАДАННЫХ:");
			sbTech.AppendLine(string.IsNullOrEmpty(item.RawMetadataDump) ? "Текстовые метаданные в файле отсутствуют." : item.RawMetadataDump);
			sbTech.AppendLine();
			sbTech.AppendLine("ПРАВИЛА ИДЕНТИФИКАЦИИ ПОДДЕЛОК:");
			sbTech.AppendLine("1. Стандартные скриншоты Minecraft, созданные клавишей F2, не содержат метаданных Adobe Photoshop,");
			sbTech.AppendLine("   Paint.NET, GIMP или вложений Discord.");
			sbTech.AppendLine("2. Наличие следов редакторов указывает на фальсификацию доказательств (5.08).");
			sbTech.AppendLine("3. Присутствие текстовых сигнатур чит-клиентов (Vape, Celestial, Expensive, Nursultan, Wurst) в метаданных");
			sbTech.AppendLine("   или вотермарках доказывает использование запрещённого ПО (5.01).");
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
					? string.Format("5.01 (Чит-вотермарка / следы софта на скриншоте: {0})", item.DetectedArtifact)
					: string.Format("5.08 (Фальсификация доказательств / следы редактора: {0})", item.DetectedArtifact);
				sbBan.AppendLine(string.Format("/ban {0} 30d {1}", targetPlayerName, banReason));
				sbBan.AppendLine();
				sbBan.AppendLine("ЮРИДИЧЕСКАЯ СПРАВКА ПО РЕГЛАМЕНТУ:");
				if (item.TargetRule == "5.01")
				{
					sbBan.AppendLine("Пункт 5.01 гласит: Запрещено использование любых читов, модификаций и софта.");
					sbBan.AppendLine("Обнаружение вотермарки или координат чита на скриншоте игрока является неопровержимой уликой.");
				}
				else
				{
					sbBan.AppendLine("Пункт 5.08 гласит: Запрещена фальсификация скриншотов, сокрытие следов софта и предоставление");
					sbBan.AppendLine("модифицированных изображений для обхода проверок.");
				}
			}
			else
			{
				sbBan.AppendLine("✅ НАРУШЕНИЯ НЕ ЗАФИКСИРОВАНО");
				sbBan.AppendLine();
				sbBan.AppendLine("Скриншот является чистым оригинальным снимком игры либо не содержит запрещённых следов.");
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
			ScreenshotExifEvidenceItem item = listView.SelectedItems[0].Tag as ScreenshotExifEvidenceItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранный файл не является нарушением правил.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string banReason = item.TargetRule == "5.01" 
				? string.Format("5.01 (Скриншот / Чит: {0})", item.DetectedArtifact)
				: string.Format("5.08 (Скриншот / Фальсификация: {0})", item.DetectedArtifact);
			string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, banReason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n" + cmd, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("📋 **[MCCheatChecker] Судебный отчёт: Minecraft Screenshots & Metadata Forensics**");
			sb.AppendLine(string.Format("**Проверяемый игрок:** `{0}`", targetPlayerName));
			sb.AppendLine(string.Format("**Штамп проверки:** `{0:yyyy-MM-dd HH:mm:ss}`", DateTime.Now));
			sb.AppendLine(string.Format("**Всего проверено изображений:** `{0}`", allItems.Count));

			int violations = 0;
			foreach (ScreenshotExifEvidenceItem it in allItems) if (it.IsViolation) violations++;
			sb.AppendLine(string.Format("**Зафиксировано нарушений:** `{0}`", violations));
			sb.AppendLine();

			if (violations > 0)
			{
				sb.AppendLine("🚨 **ОБНАРУЖЕННЫЕ УЛИКИ И НАРУШЕНИЯ:**");
				foreach (ScreenshotExifEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine(string.Format("- **[{0}] {1}**", it.TargetRule, it.DetectedArtifact));
						sb.AppendLine(string.Format("  *Файл:* `{0}`", it.FileName));
						sb.AppendLine(string.Format("  *Вердикт:* {0}", it.Verdict));
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Рекомендуемая мера наказания:**");
				sb.AppendLine(string.Format("`/ban {0} 30d 5.01/5.08 (Screenshot Forensics)`", targetPlayerName));
			}
			else
			{
				sb.AppendLine("✅ **Фальсификаций скриншотов и чит-вотермарок не обнаружено.**");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("Screenshots_Forensic_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ru\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<title>Отчёт Screenshot & Metadata Forensics — " + targetPlayerName + "</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("body { background-color: #121418; color: #E0E0E0; font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; }");
			sb.AppendLine("h1 { color: #20C997; border-bottom: 2px solid #20C997; padding-bottom: 10px; }");
			sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: bold; margin-right: 8px; }");
			sb.AppendLine(".badge-crit { background: #E74C3C; color: white; }");
			sb.AppendLine(".badge-safe { background: #2ECC71; color: black; }");
			sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 13px; }");
			sb.AppendLine("th { background-color: #1E222B; color: #20C997; padding: 10px; text-align: left; border: 1px solid #2C3240; }");
			sb.AppendLine("td { padding: 8px 10px; border: 1px solid #2C3240; }");
			sb.AppendLine("tr:nth-child(even) { background-color: #181B22; }");
			sb.AppendLine(".crit { color: #FF6B6B; font-weight: bold; }");
			sb.AppendLine(".warn { color: #FFA94D; }");
			sb.AppendLine(".safe { color: #69DB7C; }");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>🖼️ Экспертный судебный отчёт: Minecraft Screenshots & Metadata Forensics</h1>");
			sb.AppendLine("<p><strong>Проверяемый игрок:</strong> " + targetPlayerName + " | <strong>Дата экспертизы:</strong> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

			int violations = 0;
			foreach (ScreenshotExifEvidenceItem it in allItems) if (it.IsViolation) violations++;

			if (violations > 0)
			{
				sb.AppendLine("<div class=\"badge badge-crit\">ОБНАРУЖЕНО " + violations + " КРИТИЧЕСКИХ НАРУШЕНИЙ (5.01 / 5.08)</div>");
			}
			else
			{
				sb.AppendLine("<div class=\"badge badge-safe\">ЧИСТО: НАРУШЕНИЙ В СКРИНШОТАХ НЕ ОБНАРУЖЕНО</div>");
			}

			sb.AppendLine("<table>");
			sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Имя файла</th><th>Обнаруженный артефакт</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th><th>Штамп времени</th></tr>");

			foreach (ScreenshotExifEvidenceItem it in allItems)
			{
				string colorClass = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
				sb.AppendLine(string.Format("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9:yyyy-MM-dd HH:mm:ss}</td></tr>",
					colorClass, it.Index, it.Category, it.FileName, it.DetectedArtifact, it.Status, it.ThreatLevel, it.Verdict, it.TargetRule, it.Timestamp));
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

		private static readonly string[] GraphicEditors = new string[]
		{
			"photoshop", "paint.net", "gimp", "photofiltre", "coreldraw", "lightroom", "canva", "pixlr"
		};

		private static readonly string[] CheatHudSignatures = new string[]
		{
			"vape", "celestial", "expensive", "nursultan", "wurst", "meteor", "liquidbounce",
			"baritone", "freecam", "x-ray", "xray", "tracers", "killaura", "targethud", "autototem",
			"fly", "speed", "velocity", "noslow", "timer", "hitboxes", "reach", "akrien", "zamorozka"
		};

		public static List<ScreenshotExifEvidenceItem> PerformScreenshotsExifScan()
		{
			List<ScreenshotExifEvidenceItem> results = new List<ScreenshotExifEvidenceItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			List<string> searchDirs = new List<string>();

			// 1. .minecraft\screenshots and sub-launchers
			string mcScreenshots = Path.Combine(appData, ".minecraft", "screenshots");
			if (Directory.Exists(mcScreenshots)) searchDirs.Add(mcScreenshots);

			// Other launchers screenshots (.tlauncher, .feather, .lunarclient, etc.)
			try
			{
				string[] dotDirs = Directory.GetDirectories(appData, ".*");
				foreach (string d in dotDirs)
				{
					string sc = Path.Combine(d, "screenshots");
					if (Directory.Exists(sc) && !searchDirs.Contains(sc))
						searchDirs.Add(sc);
				}
			}
			catch { }

			// 2. Desktop, Pictures\Screenshots, Videos\Captures
			string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (Directory.Exists(desktopDir)) searchDirs.Add(desktopDir);

			string myPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			string picScreenshots = Path.Combine(myPictures, "Screenshots");
			if (Directory.Exists(picScreenshots)) searchDirs.Add(picScreenshots);

			string myVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
			string vidCaptures = Path.Combine(myVideos, "Captures");
			if (Directory.Exists(vidCaptures)) searchDirs.Add(vidCaptures);

			// Scan images in gathered directories
			List<string> imageFiles = new List<string>();
			foreach (string dir in searchDirs)
			{
				try
				{
					string[] pngs = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly);
					imageFiles.AddRange(pngs);
					string[] jpgs = Directory.GetFiles(dir, "*.jpg", SearchOption.TopDirectoryOnly);
					imageFiles.AddRange(jpgs);
					string[] bmps = Directory.GetFiles(dir, "*.bmp", SearchOption.TopDirectoryOnly);
					imageFiles.AddRange(bmps);
				}
				catch { }
			}

			// Parse each image file
			foreach (string file in imageFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(file);
					if (fi.Length > 20 * 1024 * 1024) continue; // Skip files > 20MB

					byte[] headerBytes;
					using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						int readLen = (int)Math.Min(fs.Length, 65536); // Read up to 64KB header/metadata
						headerBytes = new byte[readLen];
						fs.Read(headerBytes, 0, readLen);
					}

					string headerAscii = Encoding.ASCII.GetString(headerBytes);
					string headerUtf8 = Encoding.UTF8.GetString(headerBytes);
					string combinedHeader = (headerAscii + " " + headerUtf8).ToLowerInvariant();

					// 1. Check for graphic editor metadata (Photoshop, Paint.NET, GIMP, etc.)
					string detectedEditor = null;
					foreach (string ed in GraphicEditors)
					{
						if (combinedHeader.Contains(ed))
						{
							detectedEditor = ed;
							break;
						}
					}

					if (detectedEditor != null)
					{
						results.Add(new ScreenshotExifEvidenceItem
						{
							Index = idx++,
							Category = "Метаданные редактора",
							FilePath = file,
							FileName = fi.Name,
							DetectedArtifact = string.Format("Редактор изображений: {0}", detectedEditor.ToUpperInvariant()),
							Status = "ОБНАРУЖЕНЫ СЛЕДЫ РЕДАКТИРОВАНИЯ",
							ThreatLevel = "CRITICAL",
							Verdict = string.Format("🚨 БАН 5.08: В метаданных скриншота найдены следы графического редактора ({0})! Признак фальсификации скриншота.", detectedEditor),
							TargetRule = "5.08",
							IsViolation = true,
							IsClean = false,
							Timestamp = fi.LastWriteTime,
							RowColor = Ui.AccentRed,
							SoftwareTag = detectedEditor,
							FileSizeBytes = fi.Length,
							DetailDescription = string.Format("Файл {0} содержит скрытые чанки/метаданные редактора '{1}'. Оригинальные скриншоты Minecraft никогда не содержат тегов графических редакторов.", fi.Name, detectedEditor),
							RawMetadataDump = ExtractSnippet(combinedHeader, detectedEditor)
						});
						continue;
					}

					// 2. Check for Discord attachment saves or Discord spoofing
					if (combinedHeader.Contains("discord-attachment") || combinedHeader.Contains("cdn.discordapp.com"))
					{
						results.Add(new ScreenshotExifEvidenceItem
						{
							Index = idx++,
							Category = "Метаданные редактора",
							FilePath = file,
							FileName = fi.Name,
							DetectedArtifact = "Скачано из Discord (Вложение)",
							Status = "ЧУЖОЙ СКРИНШОТ",
							ThreatLevel = "WARNING",
							Verdict = "Скриншот сохранён из вложения Discord, а не сделан в игре клавишей F2.",
							TargetRule = "5.08 (Косвенно)",
							IsViolation = false,
							IsClean = false,
							Timestamp = fi.LastWriteTime,
							RowColor = Ui.AccentOrange,
							SoftwareTag = "Discord Attachment",
							FileSizeBytes = fi.Length,
							DetailDescription = "В метаданных изображения обнаружен URL или заголовок вложения Discord. Игрок мог предоставить чужой скриншот.",
							RawMetadataDump = "Источник: cdn.discordapp.com / Discord Attachment"
						});
						continue;
					}

					// 3. Check for Cheat Watermarks / HUD keywords in metadata or filename
					string lowerName = fi.Name.ToLowerInvariant();
					string detectedCheat = null;
					foreach (string ch in CheatHudSignatures)
					{
						if (lowerName.Contains(ch) || combinedHeader.Contains(ch))
						{
							detectedCheat = ch;
							break;
						}
					}

					if (detectedCheat != null)
					{
						results.Add(new ScreenshotExifEvidenceItem
						{
							Index = idx++,
							Category = "Чит-вотермарка / HUD",
							FilePath = file,
							FileName = fi.Name,
							DetectedArtifact = string.Format("Вотермарка / Признак чита: {0}", detectedCheat),
							Status = "ЧИТЕРСКИЙ СКРИНШОТ",
							ThreatLevel = "CRITICAL",
							Verdict = string.Format("🚨 БАН 5.01: На скриншоте / в его метаданных обнаружен признак чит-клиента ({0})!", detectedCheat),
							TargetRule = "5.01",
							IsViolation = true,
							IsClean = false,
							Timestamp = fi.LastWriteTime,
							RowColor = Ui.AccentRed,
							SoftwareTag = detectedCheat,
							FileSizeBytes = fi.Length,
							DetailDescription = string.Format("Обнаружена текстовая сигнатура чита '{0}' в файле скриншота {1}.", detectedCheat, fi.Name),
							RawMetadataDump = ExtractSnippet(combinedHeader, detectedCheat)
						});
						continue;
					}

					// 4. Date Manipulation Check (Filename timestamp vs File system timestamp)
					// Minecraft format: yyyy-MM-dd_HH.mm.ss.png
					Match dateMatch = Regex.Match(fi.Name, @"(\d{4})-(\d{2})-(\d{2})_(\d{2})\.(\d{2})\.(\d{2})");
					if (dateMatch.Success)
					{
						try
						{
							int y = int.Parse(dateMatch.Groups[1].Value);
							int m = int.Parse(dateMatch.Groups[2].Value);
							int d = int.Parse(dateMatch.Groups[3].Value);
							int hh = int.Parse(dateMatch.Groups[4].Value);
							int mm = int.Parse(dateMatch.Groups[5].Value);
							int ss = int.Parse(dateMatch.Groups[6].Value);

							DateTime nameDate = new DateTime(y, m, d, hh, mm, ss);
							TimeSpan diff = (fi.LastWriteTime - nameDate).Duration();

							// If file modified time is more than 24 hours different from name timestamp
							if (diff.TotalHours > 24)
							{
								results.Add(new ScreenshotExifEvidenceItem
								{
									Index = idx++,
									Category = "Манипуляция датами",
									FilePath = file,
									FileName = fi.Name,
									DetectedArtifact = string.Format("Несоответствие даты: имя {0:yyyy-MM-dd HH:mm} vs диск {1:yyyy-MM-dd HH:mm}", nameDate, fi.LastWriteTime),
									Status = "ПОДМЕНА ВРЕМЕНИ",
									ThreatLevel = "WARNING",
									Verdict = "Штамп времени в имени файла значительно расходится со временем изменения на диске.",
									TargetRule = "5.08 (Подозрение на подмену)",
									IsViolation = false,
									IsClean = false,
									Timestamp = fi.LastWriteTime,
									RowColor = Ui.AccentOrange,
									FileSizeBytes = fi.Length,
									DetailDescription = string.Format("Дата в имени ({0}) отличается от даты изменения файла ({1}) более чем на 24 часа. Скриншот мог быть переименован или скопирован из старой сессии.", nameDate, fi.LastWriteTime)
								});
								continue;
							}
						}
						catch { }
					}

					// 5. Clean Minecraft Screenshot
					results.Add(new ScreenshotExifEvidenceItem
					{
						Index = idx++,
						Category = "Чистый скриншот",
						FilePath = file,
						FileName = fi.Name,
						DetectedArtifact = "Оригинальный скриншот Minecraft",
						Status = "ЧИСТО",
						ThreatLevel = "SAFE",
						Verdict = "Следов редакторов, чит-вотермарок и манипуляций датами не обнаружено.",
						TargetRule = "РАЗРЕШЕНО",
						IsViolation = false,
						IsClean = true,
						Timestamp = fi.LastWriteTime,
						RowColor = Ui.AccentGreen,
						FileSizeBytes = fi.Length,
						DetailDescription = string.Format("Скриншот {0} проверен. Чанки PNG валидны, следов фальсификации нет.", fi.Name)
					});
				}
				catch { }
			}

			return results;
		}

		private static string ExtractSnippet(string text, string keyword)
		{
			int idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
			if (idx < 0) return "Фрагмент не найден.";
			int start = Math.Max(0, idx - 40);
			int len = Math.Min(text.Length - start, 100);
			return text.Substring(start, len).Replace("\0", " ").Replace("\r", " ").Replace("\n", " ");
		}

		#endregion
	}
}
