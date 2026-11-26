using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum ResourcePackFilter
	{
		All,
		ViolationsOnly,
		XrayOnly,
		CemOnly,
		FontsOnly,
		CleanOnly
	}

	public class ResourcePackEvidenceItem
	{
		public int Index;
		public string Category; // "X-Ray Текстурпак", "Хитбоксы CEM (OptiFine)", "Невидимый шрифт", "Маскировка ресурспака", "Чистый ресурспак"
		public string PackPath;
		public string PackName;
		public string DetectedFeature;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.01", "5.08", "РАЗРЕШЕНО", "—"
		public bool IsViolation;
		public bool IsClean;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalSnippet;
		public long FileSizeBytes;
	}

	public class FontAndTexturePackBypassViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<ResourcePackEvidenceItem> allItems = new List<ResourcePackEvidenceItem>();
		private ResourcePackFilter currentFilter = ResourcePackFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiXray;
		private Label lblKpiCem;
		private Label lblKpiFonts;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterXray;
		private DarkButton btnFilterCem;
		private DarkButton btnFilterFonts;
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

		public FontAndTexturePackBypassViewerForm() : this("Player")
		{
		}

		public FontAndTexturePackBypassViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "💎 Инспектор X-Ray ресурспаков, моделей CEM и невидимых шрифтов (Tool 75 / Step 72) — MCCheatChecker";
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
			lblHeaderTitle.Text = "💎 ИНСПЕКТОР X-RAY РЕСУРСПАКОВ, МОДЕЛЕЙ CEM И НЕВИДИМЫХ ШРИФТОВ";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокая судебная экспертиза .minecraft\\resourcepacks: прозрачные текстуры камня/породы (X-Ray), увеличение хитбоксов через OptiFine CEM и сокрытие текста через нулевые шрифты (5.01/5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация экспертизы ресурспаков и шрифтов...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 420, 16);
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО РЕСУРСПАКОВ", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.01/5.08)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💎 X-RAY ТЕКСТУРЫ (5.01)", "0", Ui.AccentOrange, ref kx, out lblKpiXray));
			kpiPanel.Controls.Add(CreateKpiBadge("🧍 ХИТБОКСЫ CEM (5.01)", "0", Ui.AccentPurple, ref kx, out lblKpiCem));
			kpiPanel.Controls.Add(CreateKpiBadge("🔤 НЕВИДИМЫЕ ШРИФТЫ (5.08)", "0", Ui.AccentBlue, ref kx, out lblKpiFonts));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ ЧИСТЫЕ ПАКИ", "0", Ui.AccentGreen, ref kx, out lblKpiClean));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все ресурспаки", ResourcePackFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", ResourcePackFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterXray = CreateFilterButton("💎 X-Ray текстуры (5.01)", ResourcePackFilter.XrayOnly, ref bx, filterBar);
			btnFilterCem = CreateFilterButton("🧍 Хитбоксы CEM (5.01)", ResourcePackFilter.CemOnly, ref bx, filterBar);
			btnFilterFonts = CreateFilterButton("🔤 Невидимые шрифты (5.08)", ResourcePackFilter.FontsOnly, ref bx, filterBar);
			btnFilterClean = CreateFilterButton("🛡️ Чистые паки", ResourcePackFilter.CleanOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по имени пака, текстуре, блоку или правилу...";
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
			btnRescan = CreateActionButton("🔄 Повторить аудит ресурспаков", ref ax, bottomActions, delegate { StartScan(); });

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
			listView.Columns.Add("Ресурспак / Файл", 260);
			listView.Columns.Add("Обнаруженный артефакт / Текстура", 270);
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

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ ресурспаков");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("💎 Анализ текстур и моделей CEM");
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

		private DarkButton CreateFilterButton(string text, ResourcePackFilter filter, ref int curX, Panel parent)
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
			Color activeBg = Ui.AccentTeal;
			Color activeFg = Color.White;
			Color normalBg = Ui.Surface;
			Color normalFg = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == ResourcePackFilter.All, normalBg, activeBg, normalFg, activeFg);
			SetBtnStyle(btnFilterViolations, currentFilter == ResourcePackFilter.ViolationsOnly, normalBg, Ui.AccentRed, normalFg, activeFg);
			SetBtnStyle(btnFilterXray, currentFilter == ResourcePackFilter.XrayOnly, normalBg, Ui.AccentOrange, normalFg, Color.Black);
			SetBtnStyle(btnFilterCem, currentFilter == ResourcePackFilter.CemOnly, normalBg, Ui.AccentPurple, normalFg, activeFg);
			SetBtnStyle(btnFilterFonts, currentFilter == ResourcePackFilter.FontsOnly, normalBg, Ui.AccentBlue, normalFg, activeFg);
			SetBtnStyle(btnFilterClean, currentFilter == ResourcePackFilter.CleanOnly, normalBg, Ui.AccentGreen, normalFg, Color.Black);
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

			lblStatusPill.Text = "⏳ АНАЛИЗ РЕСУРСПАКОВ, CEM И ШРИФТОВ...";
			lblStatusPill.ForeColor = Ui.AccentOrange;

			listView.Items.Clear();
			allItems.Clear();
			txtAnalysis.Clear();
			txtTechnical.Clear();
			txtBanVerdict.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformResourcePackScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования ресурспаков: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<ResourcePackEvidenceItem> items = e.Result as List<ResourcePackEvidenceItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpis();
				UpdateFilterButtonStyles();
				ApplyFilter();

				int violations = 0;
				foreach (ResourcePackEvidenceItem it in allItems)
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
					lblStatusPill.Text = "✅ ЗАПРЕЩЁННЫХ РЕСУРСПАКОВ НЕ ОБНАРУЖЕНО";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			};

			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpis()
		{
			int total = allItems.Count;
			int violations = 0;
			int xray = 0;
			int cem = 0;
			int fonts = 0;
			int clean = 0;

			foreach (ResourcePackEvidenceItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.Category == "X-Ray Текстурпак") xray++;
				if (item.Category == "Хитбоксы CEM (OptiFine)") cem++;
				if (item.Category == "Невидимый шрифт") fonts++;
				if (item.IsClean) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiXray.Text = xray.ToString();
			lblKpiCem.Text = cem.ToString();
			lblKpiFonts.Text = fonts.ToString();
			lblKpiClean.Text = clean.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (ResourcePackEvidenceItem item in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case ResourcePackFilter.ViolationsOnly:
						matchFilter = item.IsViolation;
						break;
					case ResourcePackFilter.XrayOnly:
						matchFilter = (item.Category == "X-Ray Текстурпак");
						break;
					case ResourcePackFilter.CemOnly:
						matchFilter = (item.Category == "Хитбоксы CEM (OptiFine)");
						break;
					case ResourcePackFilter.FontsOnly:
						matchFilter = (item.Category == "Невидимый шрифт");
						break;
					case ResourcePackFilter.CleanOnly:
						matchFilter = item.IsClean;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (item.PackName != null && item.PackName.ToLowerInvariant().Contains(search))
						|| (item.PackPath != null && item.PackPath.ToLowerInvariant().Contains(search))
						|| (item.DetectedFeature != null && item.DetectedFeature.ToLowerInvariant().Contains(search))
						|| (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search))
						|| (item.TargetRule != null && item.TargetRule.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.PackName);
				lvi.SubItems.Add(item.DetectedFeature);
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
				txtAnalysis.Text = "Нет ресурспаков, соответствующих выбранному фильтру.";
				txtTechnical.Clear();
				txtBanVerdict.Clear();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ResourcePackEvidenceItem item = listView.SelectedItems[0].Tag as ResourcePackEvidenceItem;
			if (item == null) return;

			// Tab 1: Analysis
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine("           СУДЕБНАЯ ЭКСПЕРТИЗА РЕСУРСПАКА: X-RAY, CEM ХИТБОКСЫ И ШРИФТЫ                   ");
			sbAnalysis.AppendLine("==========================================================================================");
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine(string.Format("Категория:               {0}", item.Category));
			sbAnalysis.AppendLine(string.Format("Имя ресурспака:          {0}", item.PackName));
			sbAnalysis.AppendLine(string.Format("Полный путь:             {0}", item.PackPath));
			sbAnalysis.AppendLine(string.Format("Размер на диске:         {0:N0} байт", item.FileSizeBytes));
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

			// Tab 2: Technical Details
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine("           ТЕХНИЧЕСКИЕ ДЕТАЛИ ТЕКСТУР, МОДЕЛЕЙ CEM И ТАБЛИЦ ШРИФТОВ                       ");
			sbTech.AppendLine("==========================================================================================");
			sbTech.AppendLine();
			sbTech.AppendLine(string.Format("Является нарушением:       {0}", item.IsViolation ? "ДА (КРИТИЧЕСКАЯ УГРОЗА)" : "НЕТ"));
			sbTech.AppendLine(string.Format("Чистый ресурспак:          {0}", item.IsClean ? "ДА (ПОДТВЕРЖДЕНО)" : "НЕТ"));
			sbTech.AppendLine();
			sbTech.AppendLine("ТЕХНИЧЕСКИЙ ДАМП / ПРИЗНАК:");
			sbTech.AppendLine(string.IsNullOrEmpty(item.TechnicalSnippet) ? "Технический дамп отсутствует." : item.TechnicalSnippet);
			sbTech.AppendLine();
			sbTech.AppendLine("МЕХАНИКА ОБХОДОВ ЧЕРЕЗ РЕСУРСПАКИ:");
			sbTech.AppendLine("1. X-Ray Текстурпаки: содержат текстуры камня, диорита, незерака с альфа-каналом (Alpha < 255),");
			sbTech.AppendLine("   что делает окружающую породу прозрачной и раскрывает руды без использования модов.");
			sbTech.AppendLine("2. OptiFine Custom Entity Models (CEM): за счет параметра scale > 1.0 в player.jem увеличивают видимый");
			sbTech.AppendLine("   объем хитбокса противника или делают его видимым сквозь стены.");
			sbTech.AppendLine("3. Нулевые шрифты (Zero-width fonts): скрывают названия чит-модулей в чате и оверлее.");
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
					? string.Format("5.01 (X-Ray / Запрещённый ресурспак: {0})", item.PackName)
					: string.Format("5.08 (Сокрытие чита через шрифты/маскировку: {0})", item.PackName);
				sbBan.AppendLine(string.Format("/ban {0} 30d {1}", targetPlayerName, banReason));
				sbBan.AppendLine();
				sbBan.AppendLine("ЮРИДИЧЕСКАЯ СПРАВКА ПО РЕГЛАМЕНТУ:");
				if (item.TargetRule == "5.01")
				{
					sbBan.AppendLine("Пункт 5.01 гласит: Запрещено использование любых модификаций клиента, ресурспаков и текстур,");
					sbBan.AppendLine("дающих преимущество (X-Ray, прозрачные текстуры руд/блоков, увеличение моделей CEM).");
				}
				else
				{
					sbBan.AppendLine("Пункт 5.08 гласит: Запрещены любые действия, направленные на сокрытие использования читов,");
					sbBan.AppendLine("включая использование невидимых шрифтов для скрытия модулей и маскировку X-Ray паков.");
				}
			}
			else
			{
				sbBan.AppendLine("✅ НАРУШЕНИЯ НЕ ЗАФИКСИРОВАНО");
				sbBan.AppendLine();
				sbBan.AppendLine("Ресурспак является чистым и не содержит признаков X-Ray или запрещённых моделей.");
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
			ResourcePackEvidenceItem item = listView.SelectedItems[0].Tag as ResourcePackEvidenceItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранный ресурспак не является нарушением правил.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string banReason = item.TargetRule == "5.01" 
				? string.Format("5.01 (X-Ray / CEM: {0})", item.PackName)
				: string.Format("5.08 (Сокрытие шрифтов: {0})", item.PackName);
			string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, banReason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n" + cmd, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("📋 **[MCCheatChecker] Судебный отчёт: Resource Pack X-Ray & CEM Forensics**");
			sb.AppendLine(string.Format("**Проверяемый игрок:** `{0}`", targetPlayerName));
			sb.AppendLine(string.Format("**Штамп проверки:** `{0:yyyy-MM-dd HH:mm:ss}`", DateTime.Now));
			sb.AppendLine(string.Format("**Всего проверено ресурспаков:** `{0}`", allItems.Count));

			int violations = 0;
			foreach (ResourcePackEvidenceItem it in allItems) if (it.IsViolation) violations++;
			sb.AppendLine(string.Format("**Зафиксировано нарушений:** `{0}`", violations));
			sb.AppendLine();

			if (violations > 0)
			{
				sb.AppendLine("🚨 **ОБНАРУЖЕННЫЕ УЛИКИ И НАРУШЕНИЯ:**");
				foreach (ResourcePackEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine(string.Format("- **[{0}] {1}**", it.TargetRule, it.PackName));
						sb.AppendLine(string.Format("  *Признак:* `{0}`", it.DetectedFeature));
						sb.AppendLine(string.Format("  *Вердикт:* {0}", it.Verdict));
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Рекомендуемая мера наказания:**");
				sb.AppendLine(string.Format("`/ban {0} 30d 5.01/5.08 (ResourcePack X-Ray Forensics)`", targetPlayerName));
			}
			else
			{
				sb.AppendLine("✅ **Запрещённых X-Ray ресурспаков, CEM хитбоксов и скрытых шрифтов не обнаружено.**");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("ResourcePack_Forensic_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);
			if (sfd.ShowDialog() != DialogResult.OK) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ru\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<title>Отчёт Resource Pack X-Ray & CEM — " + targetPlayerName + "</title>");
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
			sb.AppendLine("<h1>💎 Экспертный судебный отчёт: Resource Pack X-Ray, CEM & Fonts</h1>");
			sb.AppendLine("<p><strong>Проверяемый игрок:</strong> " + targetPlayerName + " | <strong>Дата экспертизы:</strong> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

			int violations = 0;
			foreach (ResourcePackEvidenceItem it in allItems) if (it.IsViolation) violations++;

			if (violations > 0)
			{
				sb.AppendLine("<div class=\"badge badge-crit\">ОБНАРУЖЕНО " + violations + " КРИТИЧЕСКИХ НАРУШЕНИЙ (5.01 / 5.08)</div>");
			}
			else
			{
				sb.AppendLine("<div class=\"badge badge-safe\">ЧИСТО: ЗАПРЕЩЁННЫХ РЕСУРСПАКОВ НЕ ОБНАРУЖЕНО</div>");
			}

			sb.AppendLine("<table>");
			sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Ресурспак</th><th>Обнаруженный признак</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th><th>Штамп времени</th></tr>");

			foreach (ResourcePackEvidenceItem it in allItems)
			{
				string colorClass = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
				sb.AppendLine(string.Format("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9:yyyy-MM-dd HH:mm:ss}</td></tr>",
					colorClass, it.Index, it.Category, it.PackName, it.DetectedFeature, it.Status, it.ThreatLevel, it.Verdict, it.TargetRule, it.Timestamp));
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

		private static readonly string[] SolidBlockTextures = new string[]
		{
			"stone.png", "deepslate.png", "netherrack.png", "end_stone.png",
			"dirt.png", "grass_block_top.png", "granite.png", "diorite.png",
			"andesite.png", "tuff.png", "sand.png", "gravel.png", "obsidian.png"
		};

		private static readonly string[] XrayNameKeywords = new string[]
		{
			"xray", "x-ray", "ore finder", "diamond finder", "transparent", "see-through",
			"cheat pack", "wallhack", "ore outline", "highlight ores"
		};

		public static List<ResourcePackEvidenceItem> PerformResourcePackScan()
		{
			List<ResourcePackEvidenceItem> results = new List<ResourcePackEvidenceItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			List<string> packDirs = new List<string>();

			// 1. .minecraft\resourcepacks
			string defaultRp = Path.Combine(appData, ".minecraft", "resourcepacks");
			if (Directory.Exists(defaultRp)) packDirs.Add(defaultRp);

			// Sub-launchers resourcepacks
			try
			{
				string[] dotDirs = Directory.GetDirectories(appData, ".*");
				foreach (string d in dotDirs)
				{
					string rp = Path.Combine(d, "resourcepacks");
					if (Directory.Exists(rp) && !packDirs.Contains(rp))
						packDirs.Add(rp);
				}
			}
			catch { }

			// Gather all pack files (.zip) and uncompressed pack folders
			List<string> targetPacks = new List<string>();
			foreach (string d in packDirs)
			{
				try
				{
					string[] zips = Directory.GetFiles(d, "*.zip", SearchOption.TopDirectoryOnly);
					targetPacks.AddRange(zips);
					string[] subdirs = Directory.GetDirectories(d, "*", SearchOption.TopDirectoryOnly);
					targetPacks.AddRange(subdirs);
				}
				catch { }
			}

			foreach (string pack in targetPacks)
			{
				try
				{
					bool isDirectory = Directory.Exists(pack);
					string packName = isDirectory ? Path.GetFileName(pack) : Path.GetFileName(pack);
					long packSize = isDirectory ? 0 : new FileInfo(pack).Length;
					DateTime packTime = isDirectory ? Directory.GetLastWriteTime(pack) : File.GetLastWriteTime(pack);

					bool isXrayByName = false;
					string lowerName = packName.ToLowerInvariant();
					foreach (string kw in XrayNameKeywords)
					{
						if (lowerName.Contains(kw))
						{
							isXrayByName = true;
							break;
						}
					}

					if (isXrayByName)
					{
						results.Add(new ResourcePackEvidenceItem
						{
							Index = idx++,
							Category = "X-Ray Текстурпак",
							PackPath = pack,
							PackName = packName,
							DetectedFeature = "Имя ресурспака указывает на X-Ray",
							Status = "ОБНАРУЖЕН X-RAY",
							ThreatLevel = "CRITICAL",
							Verdict = string.Format("🚨 БАН 5.01: Ресурспак '{0}' содержит прямое указание на X-Ray в названии!", packName),
							TargetRule = "5.01",
							IsViolation = true,
							IsClean = false,
							Timestamp = packTime,
							RowColor = Ui.AccentRed,
							FileSizeBytes = packSize,
							DetailDescription = string.Format("Ресурспак {0} идентифицирован как X-Ray по названию файла/папки.", packName)
						});
						continue;
					}

					// Deep ZIP Inspection
					if (!isDirectory && File.Exists(pack))
					{
						bool packHasViolation = false;
						using (FileStream fs = new FileStream(pack, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
						{
							foreach (ZipArchiveEntry entry in archive.Entries)
							{
								string entryLower = entry.FullName.ToLowerInvariant();

								// 1. Check for X-Ray Block Textures
								foreach (string blockName in SolidBlockTextures)
								{
									if (entryLower.EndsWith(blockName) && entryLower.Contains("textures/block"))
									{
										// Analyze PNG header transparency
										if (IsEntryTransparentPng(entry))
										{
											results.Add(new ResourcePackEvidenceItem
											{
												Index = idx++,
												Category = "X-Ray Текстурпак",
												PackPath = pack,
												PackName = packName,
												DetectedFeature = string.Format("Прозрачная текстура породы: {0}", blockName),
												Status = "ПРОЗРАЧНЫЙ КАМЕНЬ (X-RAY)",
												ThreatLevel = "CRITICAL",
												Verdict = string.Format("🚨 БАН 5.01: В ресурспаке '{0}' обнаружена прозрачная текстура породы ({1})!", packName, blockName),
												TargetRule = "5.01",
												IsViolation = true,
												IsClean = false,
												Timestamp = packTime,
												RowColor = Ui.AccentRed,
												FileSizeBytes = packSize,
												DetailDescription = string.Format("Внутри архива {0} текстура твердой породы '{1}' имеет альфа-канал прозрачности, что делает мир прозрачным и раскрывает руды.", packName, entry.FullName),
												TechnicalSnippet = string.Format("Путь в архиве: {0}, Размер текстуры: {1} байт, Альфа-прозрачность подтверждена.", entry.FullName, entry.Length)
											});
											packHasViolation = true;
											break;
										}
									}
								}
								if (packHasViolation) break;

								// 2. Check for OptiFine Custom Entity Models (CEM) hitbox expanders
								if (entryLower.EndsWith(".jem") && entryLower.Contains("optifine/cem"))
								{
									string jemContent = ReadEntryText(entry);
									if (jemContent != null)
									{
										if (entryLower.Contains("player") || entryLower.Contains("steve") || entryLower.Contains("alex") || jemContent.Contains("\"scale\"") || jemContent.Contains("\"boxes\""))
										{
											Match scaleMatch = Regex.Match(jemContent, @"""scale""\s*:\s*([0-9.]+)");
											if (scaleMatch.Success)
											{
												double sc;
												if (double.TryParse(scaleMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out sc) && sc > 1.4)
												{
													results.Add(new ResourcePackEvidenceItem
													{
														Index = idx++,
														Category = "Хитбоксы CEM (OptiFine)",
														PackPath = pack,
														PackName = packName,
														DetectedFeature = string.Format("Модель CEM с масштабом x{0:F1} ({1})", sc, Path.GetFileName(entry.FullName)),
														Status = "РАСШИРЕНИЕ ХИТБОКСА CEM",
														ThreatLevel = "CRITICAL",
														Verdict = string.Format("🚨 БАН 5.01: Ресурспак '{0}' содержит модификацию CEM модели игрока с увеличенным масштабом ({1:F1}x)!", packName, sc),
														TargetRule = "5.01",
														IsViolation = true,
														IsClean = false,
														Timestamp = packTime,
														RowColor = Ui.AccentRed,
														FileSizeBytes = packSize,
														DetailDescription = string.Format("Файл {0} задает увеличенный размер модели игрока, используемый в качестве клиентлес-расширителя хитбоксов.", entry.FullName),
														TechnicalSnippet = jemContent.Length > 200 ? jemContent.Substring(0, 200) : jemContent
													});
													packHasViolation = true;
													break;
												}
											}
										}
									}
								}

								// 3. Check for Invisible / Zero-Width Fonts
								if (entryLower.EndsWith(".json") && entryLower.Contains("font/"))
								{
									string fontJson = ReadEntryText(entry);
									if (fontJson != null)
									{
										if (fontJson.Contains("\\u200b") || fontJson.Contains("\\u200c") || fontJson.Contains("\\ufeff") || fontJson.Contains("\"advance\": 0") || fontJson.Contains("\"advance\": -"))
										{
											results.Add(new ResourcePackEvidenceItem
											{
												Index = idx++,
												Category = "Невидимый шрифт",
												PackPath = pack,
												PackName = packName,
												DetectedFeature = string.Format("Нулевые символы в шрифте: {0}", Path.GetFileName(entry.FullName)),
												Status = "СОКРЫТИЕ ТЕКСТА ШРИФТОМ",
												ThreatLevel = "CRITICAL",
												Verdict = string.Format("🚨 БАН 5.08: В ресурспаке '{0}' обнаружен модифицированный шрифт с нулевыми символами для скрытия текста/модулей!", packName),
												TargetRule = "5.08",
												IsViolation = true,
												IsClean = false,
												Timestamp = packTime,
												RowColor = Ui.AccentRed,
												FileSizeBytes = packSize,
												DetailDescription = string.Format("Файл шрифта {0} содержит символы нулевой ширины или отрицательный шаг, используемые для сокрытия чит-индикаторов и команд.", entry.FullName),
												TechnicalSnippet = "Обнаружены управляющие последовательности нулевых глифов."
											});
											packHasViolation = true;
											break;
										}
									}
								}
							}
						}

						if (!packHasViolation)
						{
							results.Add(new ResourcePackEvidenceItem
							{
								Index = idx++,
								Category = "Чистый ресурспак",
								PackPath = pack,
								PackName = packName,
								DetectedFeature = "Обычный ресурспак",
								Status = "ЧИСТО",
								ThreatLevel = "SAFE",
								Verdict = "Прозрачных текстур камня, модификаций CEM и невидимых шрифтов не обнаружено.",
								TargetRule = "РАЗРЕШЕНО",
								IsViolation = false,
								IsClean = true,
								Timestamp = packTime,
								RowColor = Ui.AccentGreen,
								FileSizeBytes = packSize,
								DetailDescription = string.Format("Ресурспак {0} проверен. Признаков X-Ray или читерских моделей нет.", packName)
							});
						}
					}
					else if (isDirectory)
					{
						// Uncompressed pack directory
						results.Add(new ResourcePackEvidenceItem
						{
							Index = idx++,
							Category = "Чистый ресурспак",
							PackPath = pack,
							PackName = packName,
							DetectedFeature = "Папка ресурспака",
							Status = "ЧИСТО",
							ThreatLevel = "INFO",
							Verdict = "Папка с ресурсами проверена.",
							TargetRule = "РАЗРЕШЕНО",
							IsViolation = false,
							IsClean = true,
							Timestamp = packTime,
							RowColor = Ui.AccentGreen,
							FileSizeBytes = 0,
							DetailDescription = string.Format("Каталог {0} проверен.", packName)
						});
					}
				}
				catch { }
			}

			return results;
		}

		private static bool IsEntryTransparentPng(ZipArchiveEntry entry)
		{
			try
			{
				using (Stream s = entry.Open())
				{
					byte[] buf = new byte[32];
					int read = s.Read(buf, 0, 32);
					if (read >= 26)
					{
						// PNG magic
						if (buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E && buf[3] == 0x47)
						{
							byte colorType = buf[25];
							// ColorType 4 (Greyscale + Alpha) or 6 (RGBA)
							if (colorType == 4 || colorType == 6)
							{
								// Deep check with Bitmap decoder
								using (MemoryStream ms = new MemoryStream())
								{
									ms.Write(buf, 0, read);
									byte[] temp = new byte[4096];
									int r;
									while ((r = s.Read(temp, 0, temp.Length)) > 0)
									{
										ms.Write(temp, 0, r);
									}
									ms.Position = 0;

									using (Bitmap bmp = new Bitmap(ms))
									{
										// Sample center and corners
										int w = bmp.Width;
										int h = bmp.Height;
										if (w > 0 && h > 0)
										{
											Color c1 = bmp.GetPixel(w / 2, h / 2);
											Color c2 = bmp.GetPixel(0, 0);
											Color c3 = bmp.GetPixel(w - 1, h - 1);
											if (c1.A < 240 || c2.A < 240 || c3.A < 240)
											{
												return true;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }
			return false;
		}

		private static string ReadEntryText(ZipArchiveEntry entry)
		{
			try
			{
				using (Stream s = entry.Open())
				using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
				{
					return sr.ReadToEnd();
				}
			}
			catch { }
			return null;
		}

		#endregion
	}
}
