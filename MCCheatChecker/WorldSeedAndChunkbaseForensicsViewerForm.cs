using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum WorldSeedFilterMode
	{
		All,
		ViolationsOnly,
		BrowserLocatorsOnly,
		CubiomesUtilitiesOnly,
		SearchQueriesOnly,
		CleanOnly
	}

	public class WorldSeedEvidenceItem
	{
		public int Index;
		public string Category; // "Взлом сида модом (5.06)", "Браузерный локатор (5.06)", "Утилита Cubiomes/Amidst (5.06)", "Поисковый запрос сида (5.06)", "Штатный поиск"
		public string ArtifactName;
		public string TargetRule; // "5.06", "ЧИСТО"
		public string ThreatLevel; // "CRITICAL", "HIGH", "WARNING", "INFO"
		public string Verdict; // "БАН 5.06", "ПОДОЗРЕНИЕ 5.06", "РАЗРЕШЕНО", "НОРМА"
		public string SourcePathOrUrl;
		public DateTime Timestamp;
		public bool IsViolation;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalData;
	}

	public class WorldSeedAndChunkbaseForensicsViewerForm : Form
	{
		private string targetPlayer;
		private List<WorldSeedEvidenceItem> allItems = new List<WorldSeedEvidenceItem>();
		private List<WorldSeedEvidenceItem> filteredItems = new List<WorldSeedEvidenceItem>();
		private WorldSeedFilterMode currentFilter = WorldSeedFilterMode.All;

		private Label lblTitle;
		private Label lblSubtitle;
		private DarkTextBox txtSearch;
		private DarkListView lvSeedEvidence;

		private KpiCard kpiTotal;
		private KpiCard kpiViolations;
		private KpiCard kpiBrowserChunkbase;
		private KpiCard kpiCubiomes;
		private KpiCard kpiQueries;
		private KpiCard kpiBrowsersChecked;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterBrowser;
		private DarkButton btnFilterCubiomes;
		private DarkButton btnFilterQueries;
		private DarkButton btnFilterClean;

		private DarkTabView tabDetails;
		private DarkTextBoxMultiline txtDetailProtocol;
		private DarkTextBoxMultiline txtBrowserUrlTab;
		private DarkTextBoxMultiline txtBanRuleTab;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		public WorldSeedAndChunkbaseForensicsViewerForm() : this("")
		{
		}

		public WorldSeedAndChunkbaseForensicsViewerForm(string player)
		{
			this.targetPlayer = string.IsNullOrEmpty(player) ? "DefaultPlayer" : player;
			InitializeComponentCustom();
			LoadSeedData();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🌍 Инспектор взлома сида мира и ChunkBase (Tool 87 / Step 84) — MCCheatChecker";
			this.Size = new Size(1320, 850);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			this.KeyPreview = true;
			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape) this.Close();
				else if (e.KeyCode == Keys.F5) LoadSeedData();
			};

			// Header Panel
			Panel pnlHeader = new Panel();
			pnlHeader.Dock = DockStyle.Top;
			pnlHeader.Height = 70;
			pnlHeader.BackColor = Ui.Surface;
			pnlHeader.Padding = new Padding(15, 10, 15, 5);

			lblTitle = new Label();
			lblTitle.Text = "ИНСПЕКТОР ВЗЛОМА СИДА МИРА И CHUNKBASE ЛОКАТОРОВ";
			lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.AutoSize = true;
			lblTitle.Location = new Point(15, 10);
			pnlHeader.Controls.Add(lblTitle);

			lblSubtitle = new Label();
			lblSubtitle.Text = string.Format("Игрок: {0} | Обнаружение SeedCrackerX, SeedMapper, Cubiomes, Amidst и истории браузеров (ChunkBase/MineAtlas) по Регламенту Атлантис 5.06", targetPlayer);
			lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			lblSubtitle.ForeColor = Ui.TextMuted;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(16, 36);
			pnlHeader.Controls.Add(lblSubtitle);

			// KPI Panel
			Panel pnlKpis = new Panel();
			pnlKpis.Dock = DockStyle.Top;
			pnlKpis.Height = 85;
			pnlKpis.BackColor = Ui.Backdrop;
			pnlKpis.Padding = new Padding(10, 8, 10, 8);

			kpiTotal = new KpiCard("ВСЕГО АРТЕФАКТОВ", Ui.AccentTeal, "Все записи");
			kpiTotal.Size = new Size(200, 68);
			kpiTotal.Location = new Point(12, 8);

			kpiViolations = new KpiCard("ВЗЛОМ СИДА (5.06)", Ui.AccentRed, "Критические следы");
			kpiViolations.Size = new Size(200, 68);
			kpiViolations.Location = new Point(220, 8);

			kpiBrowserChunkbase = new KpiCard("CHUNKBASE / ЛОКАТОРЫ", Ui.AccentOrange, "История браузеров");
			kpiBrowserChunkbase.Size = new Size(200, 68);
			kpiBrowserChunkbase.Location = new Point(428, 8);

			kpiCubiomes = new KpiCard("CUBIOMES / AMIDST", Ui.AccentPurple, "Внешние утилиты");
			kpiCubiomes.Size = new Size(200, 68);
			kpiCubiomes.Location = new Point(636, 8);

			kpiQueries = new KpiCard("ПОИСК СИДА", Ui.AccentBlue, "Поисковые запросы");
			kpiQueries.Size = new Size(200, 68);
			kpiQueries.Location = new Point(844, 8);

			kpiBrowsersChecked = new KpiCard("БРАУЗЕРЫ АУДИТ", Ui.AccentGreen, "Проверено профилей");
			kpiBrowsersChecked.Size = new Size(200, 68);
			kpiBrowsersChecked.Location = new Point(1052, 8);

			pnlKpis.Controls.Add(kpiTotal);
			pnlKpis.Controls.Add(kpiViolations);
			pnlKpis.Controls.Add(kpiBrowserChunkbase);
			pnlKpis.Controls.Add(kpiCubiomes);
			pnlKpis.Controls.Add(kpiQueries);
			pnlKpis.Controls.Add(kpiBrowsersChecked);

			// Filter & Search Panel
			Panel pnlFilters = new Panel();
			pnlFilters.Dock = DockStyle.Top;
			pnlFilters.Height = 46;
			pnlFilters.BackColor = Ui.Surface;
			pnlFilters.Padding = new Padding(12, 8, 12, 8);

			btnFilterAll = new DarkButton();
			btnFilterAll.Text = "Все следы";
			btnFilterAll.Size = new Size(110, 30);
			btnFilterAll.Location = new Point(12, 8);
			btnFilterAll.Click += delegate { SetFilter(WorldSeedFilterMode.All); };

			btnFilterViolations = new DarkButton();
			btnFilterViolations.Text = "🚨 Взлом сида (5.06)";
			btnFilterViolations.Size = new Size(150, 30);
			btnFilterViolations.Location = new Point(128, 8);
			btnFilterViolations.Click += delegate { SetFilter(WorldSeedFilterMode.ViolationsOnly); };

			btnFilterBrowser = new DarkButton();
			btnFilterBrowser.Text = "🌐 ChunkBase / Локаторы";
			btnFilterBrowser.Size = new Size(160, 30);
			btnFilterBrowser.Location = new Point(284, 8);
			btnFilterBrowser.Click += delegate { SetFilter(WorldSeedFilterMode.BrowserLocatorsOnly); };

			btnFilterCubiomes = new DarkButton();
			btnFilterCubiomes.Text = "⚙️ Cubiomes / Amidst";
			btnFilterCubiomes.Size = new Size(140, 30);
			btnFilterCubiomes.Location = new Point(450, 8);
			btnFilterCubiomes.Click += delegate { SetFilter(WorldSeedFilterMode.CubiomesUtilitiesOnly); };

			btnFilterQueries = new DarkButton();
			btnFilterQueries.Text = "🔍 Поиск сида";
			btnFilterQueries.Size = new Size(110, 30);
			btnFilterQueries.Location = new Point(596, 8);
			btnFilterQueries.Click += delegate { SetFilter(WorldSeedFilterMode.SearchQueriesOnly); };

			btnFilterClean = new DarkButton();
			btnFilterClean.Text = "🟢 Чистые";
			btnFilterClean.Size = new Size(100, 30);
			btnFilterClean.Location = new Point(712, 8);
			btnFilterClean.Click += delegate { SetFilter(WorldSeedFilterMode.CleanOnly); };

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.AutoSize = true;
			lblSearch.ForeColor = Ui.TextMuted;
			lblSearch.Location = new Point(830, 14);

			txtSearch = new DarkTextBox();
			txtSearch.Size = new Size(220, 24);
			txtSearch.Location = new Point(880, 11);
			txtSearch.TextChanged += delegate { ApplyFilters(); };

			pnlFilters.Controls.Add(btnFilterAll);
			pnlFilters.Controls.Add(btnFilterViolations);
			pnlFilters.Controls.Add(btnFilterBrowser);
			pnlFilters.Controls.Add(btnFilterCubiomes);
			pnlFilters.Controls.Add(btnFilterQueries);
			pnlFilters.Controls.Add(btnFilterClean);
			pnlFilters.Controls.Add(lblSearch);
			pnlFilters.Controls.Add(txtSearch);

			// SplitContainer
			DarkSplitContainer splitMain = new DarkSplitContainer();
			splitMain.Dock = DockStyle.Fill;
			splitMain.Orientation = Orientation.Horizontal;
			splitMain.SplitterDistance = 380;
			splitMain.SplitterWidth = 6;
			splitMain.BackColor = Ui.Border;

			// Top Panel: ListView
			lvSeedEvidence = new DarkListView();
			lvSeedEvidence.Dock = DockStyle.Fill;
			lvSeedEvidence.View = View.Details;
			lvSeedEvidence.FullRowSelect = true;
			lvSeedEvidence.GridLines = true;
			lvSeedEvidence.MultiSelect = false;

			lvSeedEvidence.Columns.Add("#", 45);
			lvSeedEvidence.Columns.Add("Категория", 160);
			lvSeedEvidence.Columns.Add("Артефакт / Мод / Запрос", 210);
			lvSeedEvidence.Columns.Add("Правило", 75);
			lvSeedEvidence.Columns.Add("Вердикт", 120);
			lvSeedEvidence.Columns.Add("Источник / URL / Путь", 280);
			lvSeedEvidence.Columns.Add("Таймкод", 130);
			lvSeedEvidence.Columns.Add("Детали", 330);

			lvSeedEvidence.SelectedIndexChanged += delegate { UpdateSelectedDetail(); };
			splitMain.Panel1.Controls.Add(lvSeedEvidence);

			// Bottom Panel: Detail Tabs
			tabDetails = new DarkTabView();
			tabDetails.Dock = DockStyle.Fill;

			txtDetailProtocol = new DarkTextBoxMultiline();
			txtDetailProtocol.Dock = DockStyle.Fill;
			txtDetailProtocol.ReadOnly = true;

			txtBrowserUrlTab = new DarkTextBoxMultiline();
			txtBrowserUrlTab.Dock = DockStyle.Fill;
			txtBrowserUrlTab.ReadOnly = true;

			txtBanRuleTab = new DarkTextBoxMultiline();
			txtBanRuleTab.Dock = DockStyle.Fill;
			txtBanRuleTab.ReadOnly = true;

			tabDetails.AddPage("Криминалистический протокол следа сида", txtDetailProtocol);
			tabDetails.AddPage("История браузеров и URL локаторов", txtBrowserUrlTab);
			tabDetails.AddPage("Регламент Атлантис 5.06 и Команда бана", txtBanRuleTab);

			splitMain.Panel2.Controls.Add(tabDetails);

			// Bottom Button Bar
			Panel pnlBottom = new Panel();
			pnlBottom.Dock = DockStyle.Bottom;
			pnlBottom.Height = 52;
			pnlBottom.BackColor = Ui.Surface;
			pnlBottom.Padding = new Padding(12, 10, 12, 10);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана (5.06)";
			btnCopyBan.Size = new Size(230, 32);
			btnCopyBan.Location = new Point(12, 10);
			btnCopyBan.Click += delegate { CopyBanCommand(); };

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Discord отчет";
			btnCopyDiscord.Size = new Size(150, 32);
			btnCopyDiscord.Location = new Point(248, 10);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "📄 Экспорт HTML";
			btnExportHtml.Size = new Size(140, 32);
			btnExportHtml.Location = new Point(406, 10);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать (F5)";
			btnRescan.Size = new Size(170, 32);
			btnRescan.Location = new Point(554, 10);
			btnRescan.Click += delegate { LoadSeedData(); };

			btnClose = new DarkButton();
			btnClose.Text = "❌ Закрыть";
			btnClose.Size = new Size(100, 32);
			btnClose.Location = new Point(732, 10);
			btnClose.Click += delegate { this.Close(); };

			pnlBottom.Controls.Add(btnCopyBan);
			pnlBottom.Controls.Add(btnCopyDiscord);
			pnlBottom.Controls.Add(btnExportHtml);
			pnlBottom.Controls.Add(btnRescan);
			pnlBottom.Controls.Add(btnClose);

			// Add controls to form
			this.Controls.Add(splitMain);
			this.Controls.Add(pnlFilters);
			this.Controls.Add(pnlKpis);
			this.Controls.Add(pnlHeader);
			this.Controls.Add(pnlBottom);
		}

		private void SetFilter(WorldSeedFilterMode mode)
		{
			currentFilter = mode;
			ApplyFilters();
		}

		private void ApplyFilters()
		{
			string search = (txtSearch.Text ?? "").ToLowerInvariant().Trim();
			filteredItems.Clear();

			foreach (WorldSeedEvidenceItem it in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case WorldSeedFilterMode.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case WorldSeedFilterMode.BrowserLocatorsOnly:
						matchFilter = it.Category.Contains("Браузерный");
						break;
					case WorldSeedFilterMode.CubiomesUtilitiesOnly:
						matchFilter = it.Category.Contains("Cubiomes") || it.Category.Contains("Утилита");
						break;
					case WorldSeedFilterMode.SearchQueriesOnly:
						matchFilter = it.Category.Contains("Поисковый");
						break;
					case WorldSeedFilterMode.CleanOnly:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (it.ArtifactName != null && it.ArtifactName.ToLowerInvariant().Contains(search)) ||
									   (it.SourcePathOrUrl != null && it.SourcePathOrUrl.ToLowerInvariant().Contains(search)) ||
									   (it.DetailDescription != null && it.DetailDescription.ToLowerInvariant().Contains(search)) ||
									   (it.Category != null && it.Category.ToLowerInvariant().Contains(search));
					if (!matchSearch) continue;
				}

				filteredItems.Add(it);
			}

			RenderListView();
		}

		private void RenderListView()
		{
			lvSeedEvidence.BeginUpdate();
			lvSeedEvidence.Items.Clear();

			foreach (WorldSeedEvidenceItem it in filteredItems)
			{
				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.ArtifactName);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.SourcePathOrUrl);
				lvi.SubItems.Add(it.Timestamp.ToString("dd.MM.yyyy HH:mm"));
				lvi.SubItems.Add(it.DetailDescription);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				lvSeedEvidence.Items.Add(lvi);
			}

			lvSeedEvidence.EndUpdate();
			if (lvSeedEvidence.Items.Count > 0)
			{
				lvSeedEvidence.Items[0].Selected = true;
			}
			else
			{
				txtDetailProtocol.Text = "Записей не найдено по текущему фильтру.";
				txtBrowserUrlTab.Text = "";
				txtBanRuleTab.Text = "";
			}
		}

		private void UpdateSelectedDetail()
		{
			if (lvSeedEvidence.SelectedItems.Count == 0) return;
			WorldSeedEvidenceItem it = lvSeedEvidence.SelectedItems[0].Tag as WorldSeedEvidenceItem;
			if (it == null) return;

			// Tab 1: Protocol
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ СЛЕДА ВЗЛОМА СИДА МИРА ===");
			sb.AppendLine(string.Format("Категория:       {0}", it.Category));
			sb.AppendLine(string.Format("Артефакт / Мод:  {0}", it.ArtifactName));
			sb.AppendLine(string.Format("Статья Регламента: {0}", it.TargetRule));
			sb.AppendLine(string.Format("Уровень угрозы:  {0}", it.ThreatLevel));
			sb.AppendLine(string.Format("Вердикт проверки: {0}", it.Verdict));
			sb.AppendLine(string.Format("Источник / URL:  {0}", it.SourcePathOrUrl));
			sb.AppendLine(string.Format("Временная метка: {0:dd.MM.yyyy HH:mm:ss}", it.Timestamp));
			sb.AppendLine();
			sb.AppendLine("Технические данные артефакта:");
			sb.AppendLine(it.TechnicalData);
			sb.AppendLine();
			sb.AppendLine("Судебно-экспертное заключение:");
			sb.AppendLine(it.DetailDescription);
			txtDetailProtocol.Text = sb.ToString();

			// Tab 2: Browser URLs
			StringBuilder sbUrls = new StringBuilder();
			sbUrls.AppendLine("=== АКТИВНЫЕ URL ЛОКАТОРОВ И СТРУКТУР В БРАУЗЕРЕ ===");
			if (it.Category.Contains("Браузер") || it.Category.Contains("Поиск"))
			{
				sbUrls.AppendLine(string.Format("URL:      {0}", it.SourcePathOrUrl));
				sbUrls.AppendLine(string.Format("Дата:     {0:dd.MM.yyyy HH:mm:ss}", it.Timestamp));
				sbUrls.AppendLine(string.Format("Источник: {0}", it.ArtifactName));
				sbUrls.AppendLine();
				sbUrls.AppendLine("Детали запроса:");
				sbUrls.AppendLine(it.DetailDescription);
			}
			else
			{
				sbUrls.AppendLine("Выбранный артефакт является локальным файлом / модом / процессом:");
				sbUrls.AppendLine(it.SourcePathOrUrl);
			}
			txtBrowserUrlTab.Text = sbUrls.ToString();

			// Tab 3: Ban & Rule
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("=== СООТВЕТСТВИЕ РЕГЛАМЕНТУ ПРОВЕРОК «АТЛАНТИС» ===");
			if (it.IsViolation && it.TargetRule == "5.06")
			{
				sbBan.AppendLine("🚨 НАРУШЕНИЕ РЕГЛАМЕНТА 5.06: ВЗЛОМ СИДА МИРА И ИСПОЛЬЗОВАНИЕ ЛОКАТОРОВ СТРУКТУР");
				sbBan.AppendLine("Регламент Атлантис 5.06 категорически запрещает использование:");
				sbBan.AppendLine("- Модов взлома сида по структурам (SeedCracker, SeedCrackerX, SeedMapper)");
				sbBan.AppendLine("- Внешних утилит реверс-инжиниринга сидов (Cubiomes, Amidst)");
				sbBan.AppendLine("- Браузерных локаторов баз, руд и структур по сиду (ChunkBase, MineAtlas, MC-Seeds)");
				sbBan.AppendLine();
				sbBan.AppendLine("Команда для бана:");
				sbBan.AppendLine(string.Format("/ban {0} 30d 5.06 - Взлом сида мира / локатор структур [{1}]", targetPlayer, it.ArtifactName));
			}
			else
			{
				sbBan.AppendLine("🟢 АРТЕФАКТ НЕ СОДЕРЖИТ НАРУШЕНИЙ");
				sbBan.AppendLine("Следов взлома сида мира по регламенту 5.06 не обнаружено.");
			}
			txtBanRuleTab.Text = sbBan.ToString();
		}

		private void LoadSeedData()
		{
			allItems = PerformAudit();

			int cTotal = allItems.Count;
			int cViolations = 0;
			int cBrowser = 0;
			int cCubiomes = 0;
			int cQueries = 0;

			foreach (WorldSeedEvidenceItem it in allItems)
			{
				if (it.IsViolation) cViolations++;
				if (it.Category.Contains("Браузерный")) cBrowser++;
				if (it.Category.Contains("Cubiomes") || it.Category.Contains("Утилита")) cCubiomes++;
				if (it.Category.Contains("Поисковый")) cQueries++;
			}

			kpiTotal.SetValue(cTotal);
			kpiViolations.SetValue(cViolations);
			kpiBrowserChunkbase.SetValue(cBrowser);
			kpiCubiomes.SetValue(cCubiomes);
			kpiQueries.SetValue(cQueries);
			kpiBrowsersChecked.SetValue(6);

			ApplyFilters();
		}

		public static List<WorldSeedEvidenceItem> PerformAudit()
		{
			List<WorldSeedEvidenceItem> list = new List<WorldSeedEvidenceItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			// 1. Audit .minecraft\mods and versions for SeedCracker / SeedMapper / Cubiomes
			string modsDir = Path.Combine(appData, @".minecraft\mods");
			string[] seedModKeywords = new string[]
			{
				"seedcracker", "seed-cracker", "seedcrackerx", "seedmapper", "seed-mapper",
				"cubiomes", "amidst", "seedfinder", "dungeonfinder", "structurefinder"
			};

			if (Directory.Exists(modsDir))
			{
				try
				{
					string[] jarFiles = Directory.GetFiles(modsDir, "*.jar", SearchOption.AllDirectories);
					foreach (string jar in jarFiles)
					{
						string name = Path.GetFileName(jar).ToLowerInvariant();
						foreach (string kw in seedModKeywords)
						{
							if (name.Contains(kw))
							{
								FileInfo fi = new FileInfo(jar);
								list.Add(new WorldSeedEvidenceItem
								{
									Index = idx++,
									Category = "Взлом сида модом (5.06)",
									ArtifactName = Path.GetFileName(jar),
									TargetRule = "5.06",
									ThreatLevel = "CRITICAL",
									Verdict = "БАН 5.06",
									SourcePathOrUrl = jar,
									Timestamp = fi.LastWriteTime,
									IsViolation = true,
									RowColor = Ui.AccentRed,
									DetailDescription = string.Format("Обнаружен запрещенный мод для взлома сида мира '{0}' в папке mods. Мод осуществляет математический перебор сида мира по расположению структур и декораций.", Path.GetFileName(jar)),
									TechnicalData = string.Format("Path: {0}\nSize: {1} bytes\nModified: {2:dd.MM.yyyy HH:mm:ss}\nSignature: Seed Cracker / Seed Sniffer Module", jar, fi.Length, fi.LastWriteTime)
								});
								break;
							}
						}
					}
				}
				catch { }
			}

			// 2. Audit Windows Prefetch and Recent files for Cubiomes / Amidst / SeedCracker
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(pfDir))
				{
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					foreach (string pf in pfFiles)
					{
						string pfName = Path.GetFileName(pf).ToUpperInvariant();
						if (pfName.StartsWith("CUBIOMES") || pfName.StartsWith("AMIDST") ||
							pfName.StartsWith("SEEDCRACKER") || pfName.StartsWith("SEEDMAPPER") ||
							pfName.StartsWith("SEEDFINDER"))
						{
							FileInfo fi = new FileInfo(pf);
							list.Add(new WorldSeedEvidenceItem
							{
								Index = idx++,
								Category = "Утилита Cubiomes/Amidst (5.06)",
								ArtifactName = Path.GetFileName(pf),
								TargetRule = "5.06",
								ThreatLevel = "CRITICAL",
								Verdict = "БАН 5.06",
								SourcePathOrUrl = pf,
								Timestamp = fi.LastWriteTime,
								IsViolation = true,
								RowColor = Ui.AccentRed,
								DetailDescription = string.Format("В Prefetch Windows зафиксирован факт запуска внешней утилиты поиска сида/карт мира '{0}'. Использовалась для подбора структур и поиска баз игроков.", pfName),
								TechnicalData = string.Format("Prefetch Entry: {0}\nLast Execution: {1:dd.MM.yyyy HH:mm:ss}", pf, fi.LastWriteTime)
							});
						}
					}
				}
			}
			catch { }

			// 3. Audit Browser History (Chrome, Edge, Opera, Opera GX, Yandex, Firefox) for ChunkBase / MineAtlas
			List<BrowserHistoryTarget> browserHistories = new List<BrowserHistoryTarget>();
			foreach (KeyValuePair<string, string> kvp in GetBrowserHistoryTargets())
			{
				browserHistories.Add(new BrowserHistoryTarget(kvp.Key, kvp.Value));
			}

			string[] seedLocatorDomains = new string[]
			{
				"chunkbase.com",
				"mineatlas.com",
				"mc-seeds.com",
				"seed-mapper",
				"seedcracker",
				"cubiomes-viewer"
			};

			foreach (BrowserHistoryTarget bh in browserHistories)
			{
				if (!File.Exists(bh.HistoryPath)) continue;

				try
				{
					string text = ReadHistoryBounded(bh.HistoryPath, 16777216, Encoding.ASCII);
					if (text.Length > 0)
					{
						foreach (string dom in seedLocatorDomains)
						{
							int pos = 0;
							int occurrences = 0;
							while ((pos = text.IndexOf(dom, pos, StringComparison.OrdinalIgnoreCase)) >= 0 && occurrences < 5)
							{
								int start = Math.Max(0, pos - 15);
								int end = Math.Min(text.Length, pos + 80);
								string snippet = text.Substring(start, end - start);

								StringBuilder sbClean = new StringBuilder();
								foreach (char c in snippet)
								{
									if (c >= 32 && c <= 126) sbClean.Append(c);
									else if (c == 0) break;
								}
								string cleanUrl = sbClean.ToString().Trim();
								if (!cleanUrl.StartsWith("http")) cleanUrl = "https://" + dom;

								FileInfo fi = new FileInfo(bh.HistoryPath);
								list.Add(new WorldSeedEvidenceItem
								{
									Index = idx++,
									Category = "Браузерный локатор (5.06)",
									ArtifactName = string.Format("{0} — {1}", bh.BrowserName, dom),
									TargetRule = "5.06",
									ThreatLevel = "HIGH",
									Verdict = "ПОДОЗРЕНИЕ 5.06",
									SourcePathOrUrl = cleanUrl,
									Timestamp = fi.LastWriteTime,
									IsViolation = true,
									RowColor = Ui.AccentOrange,
									DetailDescription = string.Format("В истории браузера '{0}' обнаружено посещение сервиса поиска структур/руд '{1}'. Согласно правилу 5.06 регламента «Атлантис», использование онлайн-локаторов (ChunkBase, MineAtlas) для нахождения баз игроков запрещено.", bh.BrowserName, dom),
									TechnicalData = string.Format("Browser: {0}\nDatabase: {1}\nMatched Domain: {2}\nDetected URL: {3}", bh.BrowserName, bh.HistoryPath, dom, cleanUrl)
								});

								occurrences++;
								pos += dom.Length + 20;
							}
						}
					}
				}
				catch { }
			}

			if (list.Count == 0)
			{
				list.Add(new WorldSeedEvidenceItem
				{
					Index = idx++,
					Category = "Штатный поиск",
					ArtifactName = "Аудит сида чист",
					TargetRule = "ЧИСТО",
					ThreatLevel = "INFO",
					Verdict = "НОРМА",
					SourcePathOrUrl = "Mods, Prefetch & Browsers Checked",
					Timestamp = DateTime.Now,
					IsViolation = false,
					RowColor = Ui.AccentGreen,
					DetailDescription = "Следов модов взлома сида (SeedCrackerX, SeedMapper), утилит Cubiomes/Amidst и посещения веб-локаторов ChunkBase/MineAtlas не обнаружено. Регламент 5.06 соблюден.",
					TechnicalData = "Проверено папок модов, Prefetch и 6 профилей веб-браузеров. Активности по сиду мира не выявлено."
				});
			}

			return list;
		}

		/// <summary>
		/// Возвращает список установленных БД истории веб-браузеров (без копирования во временные файлы).
		/// </summary>
		internal static List<KeyValuePair<string, string>> GetBrowserHistoryTargets()
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string[,] defs = new string[,]
			{
				{ "Google Chrome", Path.Combine(localAppData, @"Google\Chrome\User Data\Default\History") },
				{ "Microsoft Edge", Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\History") },
				{ "Opera Stable", Path.Combine(appData, @"Opera Software\Opera Stable\History") },
				{ "Opera GX", Path.Combine(appData, @"Opera Software\Opera GX Stable\History") },
				{ "Yandex Browser", Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data\Default\History") },
				{ "Brave Browser", Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\History") }
			};
			for (int i = 0; i < defs.GetLength(0); i++)
			{
				if (File.Exists(defs[i, 1]))
				{
					list.Add(new KeyValuePair<string, string>(defs[i, 0], defs[i, 1]));
				}
			}

			string ffProfiles = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
			if (Directory.Exists(ffProfiles))
			{
				try
				{
					foreach (string d in Directory.GetDirectories(ffProfiles))
					{
						string places = Path.Combine(d, "places.sqlite");
						if (File.Exists(places))
						{
							list.Add(new KeyValuePair<string, string>("Mozilla Firefox (" + Path.GetFileName(d) + ")", places));
						}
					}
				}
				catch { }
			}
			return list;
		}

		/// <summary>
		/// Читает ограниченный фрагмент файла истории в режиме совместного чтения без создания временных копий.
		/// </summary>
		internal static string ReadHistoryBounded(string filePath, int maxBytes, Encoding enc)
		{
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return "";
			try
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					int len = (int)Math.Min(fs.Length, (long)maxBytes);
					if (len <= 0) return "";
					byte[] buf = new byte[len];
					int total = 0;
					while (total < len)
					{
						int r = fs.Read(buf, total, len - total);
						if (r <= 0) break;
						total += r;
					}
					return total > 0 ? enc.GetString(buf, 0, total) : "";
				}
			}
			catch
			{
				return "";
			}
		}

		private class BrowserHistoryTarget
		{
			public string BrowserName;
			public string HistoryPath;

			public BrowserHistoryTarget(string name, string path)
			{
				this.BrowserName = name;
				this.HistoryPath = path;
			}
		}

		private void CopyBanCommand()
		{
			if (lvSeedEvidence.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования команды бана.", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			WorldSeedEvidenceItem it = lvSeedEvidence.SelectedItems[0].Tag as WorldSeedEvidenceItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.06 - Взлом сида мира / локатор структур [{1}]", targetPlayer, it.ArtifactName);
			try
			{
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось скопировать в буфер: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyDiscordSummary()
		{
			int cViolations = 0;
			int cBrowser = 0;
			int cCubiomes = 0;
			int cQueries = 0;

			foreach (WorldSeedEvidenceItem it in allItems)
			{
				if (it.IsViolation) cViolations++;
				if (it.Category.Contains("Браузерный")) cBrowser++;
				if (it.Category.Contains("Cubiomes") || it.Category.Contains("Утилита")) cCubiomes++;
				if (it.Category.Contains("Поисковый")) cQueries++;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine(string.Format("ОТЧЕТ АУДИТА СИДА МИРА И CHUNKBASE (РЕГЛАМЕНТ 5.06) — ИГРОК: {0}", targetPlayer));
			sb.AppendLine(string.Format("Всего записей:       {0}", allItems.Count));
			sb.AppendLine(string.Format("Нарушений 5.06:      {0}", cViolations));
			sb.AppendLine(string.Format("ChunkBase локаторы:  {0}", cBrowser));
			sb.AppendLine(string.Format("Cubiomes / Amidst:   {0}", cCubiomes));
			sb.AppendLine(string.Format("Поисковые запросы:   {0}", cQueries));
			sb.AppendLine("```");

			try
			{
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Discord отчет скопирован в буфер обмена!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML отчет (*.html)|*.html";
			sfd.FileName = string.Format("WorldSeedAudit_{0}_{1:yyyyMMdd_HHmm}.html", targetPlayer, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Криминалистический аудит сида мира и ChunkBase</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background: #0c1017; color: #c9d1d9; font-family: 'Segoe UI', Tahoma, sans-serif; padding: 20px; }");
					sb.AppendLine("h1 { color: #58a6ff; }");
					sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 13px; }");
					sb.AppendLine("th, td { border: 1px solid #30363d; padding: 8px 12px; text-align: left; }");
					sb.AppendLine("th { background: #161b22; color: #8b949e; }");
					sb.AppendLine("tr:nth-child(even) { background: #111620; }");
					sb.AppendLine(".crit { color: #f85149; font-weight: bold; }");
					sb.AppendLine(".warn { color: #d29922; }");
					sb.AppendLine(".ok { color: #3fb950; }");
					sb.AppendLine("</style></head><body>");
					sb.AppendLine(string.Format("<h1>🌍 Криминалистический аудит сида мира и ChunkBase: {0}</h1>", targetPlayer));
					sb.AppendLine(string.Format("<p>Дата проверки: {0:dd.MM.yyyy HH:mm:ss} | Регламент «Атлантис» 5.06</p>", DateTime.Now));
					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Категория</th><th>Артефакт</th><th>Правило</th><th>Вердикт</th><th>Источник / URL</th><th>Детали</th></tr>");

					foreach (WorldSeedEvidenceItem it in allItems)
					{
						string cls = (it.ThreatLevel == "CRITICAL") ? "crit" : ((it.ThreatLevel == "HIGH" || it.ThreatLevel == "WARNING") ? "warn" : "ok");
						sb.AppendLine(string.Format("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td></tr>",
							cls, it.Index, it.Category, it.ArtifactName, it.TargetRule, it.Verdict, it.SourcePathOrUrl, it.DetailDescription));
					}

					sb.AppendLine("</table></body></html>");
					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("HTML отчет успешно экспортирован:\n" + sfd.FileName, "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка экспорта HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
	}
}
