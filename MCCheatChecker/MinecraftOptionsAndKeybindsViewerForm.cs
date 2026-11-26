using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum KeybindFilterMode
	{
		All,
		CheatBindsOnly,
		MacrosOnly,
		ServerBindsOnly,
		CpvpBindsOnly,
		StandardOnly
	}

	public class MinecraftKeybindItem
	{
		public int Index;
		public string Category; // "Чит-бинд (5.01)", "Макрос кликера (5.03)", "Серверный бинд (OK)", "CPVP оптимизатор (OK)", "Штатное управление"
		public string ParameterName;
		public string AssignedKey;
		public string TargetRule; // "5.01", "5.03", "ЧИСТО"
		public string Severity;   // "CRITICAL", "WARNING", "INFO"
		public string Verdict;    // "БАН 5.01", "ПОДОЗРЕНИЕ 5.03", "РАЗРЕШЕНО", "НОРМА"
		public string SourceFile;
		public DateTime FileModified;
		public bool IsViolation;
		public Color RowColor;
		public string Details;
		public string RawLine;
	}

	public class MinecraftOptionsAndKeybindsViewerForm : Form
	{
		private string targetPlayer;
		private List<MinecraftKeybindItem> allItems = new List<MinecraftKeybindItem>();
		private List<MinecraftKeybindItem> filteredItems = new List<MinecraftKeybindItem>();
		private KeybindFilterMode currentFilter = KeybindFilterMode.All;

		private Label lblTitle;
		private Label lblSubtitle;
		private DarkTextBox txtSearch;
		private DarkListView lvKeybinds;

		private KpiCard kpiTotal;
		private KpiCard kpiCheat;
		private KpiCard kpiMacro;
		private KpiCard kpiServer;
		private KpiCard kpiCpvp;
		private KpiCard kpiStandard;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheat;
		private DarkButton btnFilterMacro;
		private DarkButton btnFilterServer;
		private DarkButton btnFilterCpvp;
		private DarkButton btnFilterStandard;

		private DarkTabView tabDetails;
		private DarkTextBoxMultiline txtDetailProtocol;
		private DarkTextBoxMultiline txtRawConfigView;
		private DarkTextBoxMultiline txtBanVerdictTab;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		public MinecraftOptionsAndKeybindsViewerForm() : this("")
		{
		}

		public MinecraftOptionsAndKeybindsViewerForm(string player)
		{
			this.targetPlayer = string.IsNullOrEmpty(player) ? "DefaultPlayer" : player;
			InitializeComponentCustom();
			LoadKeybindData();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🎮 Инспектор настроек и биндов Minecraft (Tool 86 / Step 83) — MCCheatChecker";
			this.Size = new Size(1300, 840);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			this.KeyPreview = true;
			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape) this.Close();
				else if (e.KeyCode == Keys.F5) LoadKeybindData();
			};

			// Header Panel
			Panel pnlHeader = new Panel();
			pnlHeader.Dock = DockStyle.Top;
			pnlHeader.Height = 70;
			pnlHeader.BackColor = Ui.Surface;
			pnlHeader.Padding = new Padding(15, 10, 15, 5);

			lblTitle = new Label();
			lblTitle.Text = "ИНСПЕКТОР НАСТРОЕК И КЛАВИАТУРНЫХ БИНДОВ MINECRAFT";
			lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.AutoSize = true;
			lblTitle.Location = new Point(15, 10);
			pnlHeader.Controls.Add(lblTitle);

			lblSubtitle = new Label();
			lblSubtitle.Text = string.Format("Игрок: {0} | Глубокий парсинг options.txt, keybinds.txt и конфигураций модов на запрещенные бинды (5.01 / 5.03) с учетом WhiteList Атлантис", targetPlayer);
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

			kpiTotal = new KpiCard("ВСЕГО БИНДОВ", Ui.AccentTeal, "Все записи");
			kpiTotal.Size = new Size(200, 68);
			kpiTotal.Location = new Point(12, 8);

			kpiCheat = new KpiCard("ЧИТ-БИНДЫ (5.01)", Ui.AccentRed, "Aura/Fly/Speed/Xray");
			kpiCheat.Size = new Size(200, 68);
			kpiCheat.Location = new Point(220, 8);

			kpiMacro = new KpiCard("МАКРОСЫ (5.03)", Ui.AccentOrange, "AutoClicker/Binds");
			kpiMacro.Size = new Size(200, 68);
			kpiMacro.Location = new Point(428, 8);

			kpiServer = new KpiCard("СЕРВЕРНЫЕ (OK)", Ui.AccentGreen, "Вайтлист команд");
			kpiServer.Size = new Size(200, 68);
			kpiServer.Location = new Point(636, 8);

			kpiCpvp = new KpiCard("CPVP БИНДЫ (OK)", Ui.AccentBlue, "Атлантис WhiteList");
			kpiCpvp.Size = new Size(200, 68);
			kpiCpvp.Location = new Point(844, 8);

			kpiStandard = new KpiCard("ШТАТНЫЕ КЛАВИШИ", Ui.TextMuted, "Minecraft Default");
			kpiStandard.Size = new Size(200, 68);
			kpiStandard.Location = new Point(1052, 8);

			pnlKpis.Controls.Add(kpiTotal);
			pnlKpis.Controls.Add(kpiCheat);
			pnlKpis.Controls.Add(kpiMacro);
			pnlKpis.Controls.Add(kpiServer);
			pnlKpis.Controls.Add(kpiCpvp);
			pnlKpis.Controls.Add(kpiStandard);

			// Filter & Search Panel
			Panel pnlFilters = new Panel();
			pnlFilters.Dock = DockStyle.Top;
			pnlFilters.Height = 46;
			pnlFilters.BackColor = Ui.Surface;
			pnlFilters.Padding = new Padding(12, 8, 12, 8);

			btnFilterAll = new DarkButton();
			btnFilterAll.Text = "Все бинды";
			btnFilterAll.Size = new Size(110, 30);
			btnFilterAll.Location = new Point(12, 8);
			btnFilterAll.Click += delegate { SetFilter(KeybindFilterMode.All); };

			btnFilterCheat = new DarkButton();
			btnFilterCheat.Text = "🚨 Чит-бинды (5.01)";
			btnFilterCheat.Size = new Size(150, 30);
			btnFilterCheat.Location = new Point(128, 8);
			btnFilterCheat.Click += delegate { SetFilter(KeybindFilterMode.CheatBindsOnly); };

			btnFilterMacro = new DarkButton();
			btnFilterMacro.Text = "⚠️ Макросы (5.03)";
			btnFilterMacro.Size = new Size(140, 30);
			btnFilterMacro.Location = new Point(284, 8);
			btnFilterMacro.Click += delegate { SetFilter(KeybindFilterMode.MacrosOnly); };

			btnFilterServer = new DarkButton();
			btnFilterServer.Text = "🟢 Серверные (OK)";
			btnFilterServer.Size = new Size(140, 30);
			btnFilterServer.Location = new Point(430, 8);
			btnFilterServer.Click += delegate { SetFilter(KeybindFilterMode.ServerBindsOnly); };

			btnFilterCpvp = new DarkButton();
			btnFilterCpvp.Text = "⚔️ CPVP бинды";
			btnFilterCpvp.Size = new Size(120, 30);
			btnFilterCpvp.Location = new Point(576, 8);
			btnFilterCpvp.Click += delegate { SetFilter(KeybindFilterMode.CpvpBindsOnly); };

			btnFilterStandard = new DarkButton();
			btnFilterStandard.Text = "🎮 Штатные";
			btnFilterStandard.Size = new Size(110, 30);
			btnFilterStandard.Location = new Point(702, 8);
			btnFilterStandard.Click += delegate { SetFilter(KeybindFilterMode.StandardOnly); };

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
			pnlFilters.Controls.Add(btnFilterCheat);
			pnlFilters.Controls.Add(btnFilterMacro);
			pnlFilters.Controls.Add(btnFilterServer);
			pnlFilters.Controls.Add(btnFilterCpvp);
			pnlFilters.Controls.Add(btnFilterStandard);
			pnlFilters.Controls.Add(lblSearch);
			pnlFilters.Controls.Add(txtSearch);

			// SplitContainer: ListView on top, Detail Tabs on bottom
			DarkSplitContainer splitMain = new DarkSplitContainer();
			splitMain.Dock = DockStyle.Fill;
			splitMain.Orientation = Orientation.Horizontal;
			splitMain.SplitterDistance = 380;
			splitMain.SplitterWidth = 6;
			splitMain.BackColor = Ui.Border;

			// Top Panel: ListView
			lvKeybinds = new DarkListView();
			lvKeybinds.Dock = DockStyle.Fill;
			lvKeybinds.View = View.Details;
			lvKeybinds.FullRowSelect = true;
			lvKeybinds.GridLines = true;
			lvKeybinds.MultiSelect = false;

			lvKeybinds.Columns.Add("#", 45);
			lvKeybinds.Columns.Add("Категория", 140);
			lvKeybinds.Columns.Add("Параметр / Бинд", 210);
			lvKeybinds.Columns.Add("Клавиша", 110);
			lvKeybinds.Columns.Add("Правило", 75);
			lvKeybinds.Columns.Add("Вердикт", 120);
			lvKeybinds.Columns.Add("Файл-источник", 200);
			lvKeybinds.Columns.Add("Дата изменения", 130);
			lvKeybinds.Columns.Add("Детали", 320);

			lvKeybinds.SelectedIndexChanged += delegate { UpdateSelectedDetail(); };
			splitMain.Panel1.Controls.Add(lvKeybinds);

			// Bottom Panel: Details TabControl
			tabDetails = new DarkTabView();
			tabDetails.Dock = DockStyle.Fill;

			txtDetailProtocol = new DarkTextBoxMultiline();
			txtDetailProtocol.Dock = DockStyle.Fill;
			txtDetailProtocol.ReadOnly = true;

			txtRawConfigView = new DarkTextBoxMultiline();
			txtRawConfigView.Dock = DockStyle.Fill;
			txtRawConfigView.ReadOnly = true;

			txtBanVerdictTab = new DarkTextBoxMultiline();
			txtBanVerdictTab.Dock = DockStyle.Fill;
			txtBanVerdictTab.ReadOnly = true;

			tabDetails.AddPage("Криминалистический протокол бинда", txtDetailProtocol);
			tabDetails.AddPage("Полное содержимое конфигурации", txtRawConfigView);
			tabDetails.AddPage("Регламент Атлантис 5.01/5.03 и Команда бана", txtBanVerdictTab);

			splitMain.Panel2.Controls.Add(tabDetails);

			// Bottom Button Bar
			Panel pnlBottom = new Panel();
			pnlBottom.Dock = DockStyle.Bottom;
			pnlBottom.Height = 52;
			pnlBottom.BackColor = Ui.Surface;
			pnlBottom.Padding = new Padding(12, 10, 12, 10);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Size = new Size(210, 32);
			btnCopyBan.Location = new Point(12, 10);
			btnCopyBan.Click += delegate { CopyBanCommand(); };

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Discord отчет";
			btnCopyDiscord.Size = new Size(150, 32);
			btnCopyDiscord.Location = new Point(230, 10);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "📄 Экспорт HTML";
			btnExportHtml.Size = new Size(140, 32);
			btnExportHtml.Location = new Point(388, 10);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать (F5)";
			btnRescan.Size = new Size(170, 32);
			btnRescan.Location = new Point(536, 10);
			btnRescan.Click += delegate { LoadKeybindData(); };

			btnClose = new DarkButton();
			btnClose.Text = "❌ Закрыть";
			btnClose.Size = new Size(100, 32);
			btnClose.Location = new Point(714, 10);
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

		private void SetFilter(KeybindFilterMode mode)
		{
			currentFilter = mode;
			ApplyFilters();
		}

		private void ApplyFilters()
		{
			string search = (txtSearch.Text ?? "").ToLowerInvariant().Trim();
			filteredItems.Clear();

			foreach (MinecraftKeybindItem it in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case KeybindFilterMode.CheatBindsOnly:
						matchFilter = it.IsViolation && it.TargetRule == "5.01";
						break;
					case KeybindFilterMode.MacrosOnly:
						matchFilter = it.TargetRule == "5.03" || it.Category.Contains("Макрос");
						break;
					case KeybindFilterMode.ServerBindsOnly:
						matchFilter = it.Category.Contains("Серверный");
						break;
					case KeybindFilterMode.CpvpBindsOnly:
						matchFilter = it.Category.Contains("CPVP");
						break;
					case KeybindFilterMode.StandardOnly:
						matchFilter = it.Category.Contains("Штатное");
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (it.ParameterName != null && it.ParameterName.ToLowerInvariant().Contains(search)) ||
									   (it.AssignedKey != null && it.AssignedKey.ToLowerInvariant().Contains(search)) ||
									   (it.Details != null && it.Details.ToLowerInvariant().Contains(search)) ||
									   (it.SourceFile != null && it.SourceFile.ToLowerInvariant().Contains(search));
					if (!matchSearch) continue;
				}

				filteredItems.Add(it);
			}

			RenderListView();
		}

		private void RenderListView()
		{
			lvKeybinds.BeginUpdate();
			lvKeybinds.Items.Clear();

			foreach (MinecraftKeybindItem it in filteredItems)
			{
				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.ParameterName);
				lvi.SubItems.Add(it.AssignedKey);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.SourceFile);
				lvi.SubItems.Add(it.FileModified.ToString("dd.MM.yyyy HH:mm"));
				lvi.SubItems.Add(it.Details);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				lvKeybinds.Items.Add(lvi);
			}

			lvKeybinds.EndUpdate();
			if (lvKeybinds.Items.Count > 0)
			{
				lvKeybinds.Items[0].Selected = true;
			}
			else
			{
				txtDetailProtocol.Text = "Записей не найдено по текущему фильтру.";
				txtRawConfigView.Text = "";
				txtBanVerdictTab.Text = "";
			}
		}

		private void UpdateSelectedDetail()
		{
			if (lvKeybinds.SelectedItems.Count == 0) return;
			MinecraftKeybindItem it = lvKeybinds.SelectedItems[0].Tag as MinecraftKeybindItem;
			if (it == null) return;

			// Tab 1: Protocol
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНО-КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ БИНДА MINECRAFT ===");
			sb.AppendLine(string.Format("Параметр:          {0}", it.ParameterName));
			sb.AppendLine(string.Format("Назначенная клавиша: {0}", it.AssignedKey));
			sb.AppendLine(string.Format("Категория:         {0}", it.Category));
			sb.AppendLine(string.Format("Статья регламента: {0}", it.TargetRule));
			sb.AppendLine(string.Format("Уровень угрозы:    {0}", it.Severity));
			sb.AppendLine(string.Format("Криминалист. итог: {0}", it.Verdict));
			sb.AppendLine(string.Format("Файл конфигурации: {0}", it.SourceFile));
			sb.AppendLine(string.Format("Дата изменения:    {0:dd.MM.yyyy HH:mm:ss}", it.FileModified));
			sb.AppendLine(string.Format("Исходная строка:   {0}", it.RawLine));
			sb.AppendLine();
			sb.AppendLine("Детальное описание:");
			sb.AppendLine(it.Details);
			txtDetailProtocol.Text = sb.ToString();

			// Tab 2: Raw file view
			try
			{
				if (File.Exists(it.SourceFile))
				{
					txtRawConfigView.Text = File.ReadAllText(it.SourceFile);
				}
				else
				{
					txtRawConfigView.Text = "Файл недоступен или был перемещен: " + it.SourceFile;
				}
			}
			catch (Exception ex)
			{
				txtRawConfigView.Text = "Ошибка чтения файла: " + ex.Message;
			}

			// Tab 3: Ban & Rules
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("=== СООТВЕТСТВИЕ РЕГЛАМЕНТУ ПРОВЕРОК «АТЛАНТИС» ===");
			if (it.IsViolation && it.TargetRule == "5.01")
			{
				sbBan.AppendLine("🚨 ФИКСАЦИЯ НАРУШЕНИЯ ПРАВИЛА 5.01 (ИСПОЛЬЗОВАНИЕ ЧИТ-ПРОГРАММ / ЧИТ-БИНДОВ)");
				sbBan.AppendLine("В файле конфигурации Minecraft или мода обнаружен активный бинд, привязанный к функции чита.");
				sbBan.AppendLine(string.Format("Привязка: {0} -> {1}", it.ParameterName, it.AssignedKey));
				sbBan.AppendLine();
				sbBan.AppendLine("Команда для бана:");
				sbBan.AppendLine(string.Format("/ban {0} 30d 5.01 - Чит-бинд в options.txt [{1}:{2}]", targetPlayer, it.ParameterName, it.AssignedKey));
			}
			else if (it.TargetRule == "5.03")
			{
				sbBan.AppendLine("⚠️ ПОДОЗРЕНИЕ НА НАРУШЕНИЕ ПРАВИЛА 5.03 (КЛИКЕРЫ, АВТОМАТИЗАЦИЯ И МАКРОСЫ)");
				sbBan.AppendLine("Обнаружена подозрительная настройка атаки, макроса или автокликера.");
				sbBan.AppendLine();
				sbBan.AppendLine("Команда для бана (при подтверждении кликера):");
				sbBan.AppendLine(string.Format("/ban {0} 15d 5.03 - Запрещенный макрос/кликер в options.txt [{1}]", targetPlayer, it.ParameterName));
			}
			else if (it.Category.Contains("Серверный"))
			{
				sbBan.AppendLine("🟢 РАЗРЕШЕННЫЙ СЕРВЕРНЫЙ МАКРОС (РЕГЛАМЕНТ 5.03)");
				sbBan.AppendLine("Команды /home, /spawn, /tpa, /clan и стандартные чат-бинды полностью разрешены регламентом «Атлантис».");
			}
			else if (it.Category.Contains("CPVP"))
			{
				sbBan.AppendLine("🟢 РАЗРЕШЕННЫЙ CPVP БИНД («АТЛАНТИС» WHITELIST)");
				sbBan.AppendLine("Бинды оптимизаторов Marlow, Haram, End Crystal, Walksy Anchor, Hero's, CuteBow входят в белый список.");
			}
			else
			{
				sbBan.AppendLine("🟢 ШТАТНОЕ УПРАВЛЕНИЕ MINECRAFT");
				sbBan.AppendLine("Стандартная раскладка управления клиентом. Нарушений не зафиксировано.");
			}
			txtBanVerdictTab.Text = sbBan.ToString();
		}

		private void LoadKeybindData()
		{
			allItems = PerformAudit();

			int cTotal = allItems.Count;
			int cCheat = 0;
			int cMacro = 0;
			int cServer = 0;
			int cCpvp = 0;
			int cStandard = 0;

			foreach (MinecraftKeybindItem it in allItems)
			{
				if (it.IsViolation && it.TargetRule == "5.01") cCheat++;
				else if (it.TargetRule == "5.03" || it.Category.Contains("Макрос")) cMacro++;
				else if (it.Category.Contains("Серверный")) cServer++;
				else if (it.Category.Contains("CPVP")) cCpvp++;
				else cStandard++;
			}

			kpiTotal.SetValue(cTotal);
			kpiCheat.SetValue(cCheat);
			kpiMacro.SetValue(cMacro);
			kpiServer.SetValue(cServer);
			kpiCpvp.SetValue(cCpvp);
			kpiStandard.SetValue(cStandard);

			ApplyFilters();
		}

		public static List<MinecraftKeybindItem> PerformAudit()
		{
			List<MinecraftKeybindItem> list = new List<MinecraftKeybindItem>();
			int idx = 1;

			// List candidate options.txt & keybind paths
			List<string> candidateFiles = new List<string>();

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			string defaultOptions = Path.Combine(appData, @".minecraft\options.txt");
			if (File.Exists(defaultOptions)) candidateFiles.Add(defaultOptions);

			string defaultKeybinds = Path.Combine(appData, @".minecraft\keybinds.txt");
			if (File.Exists(defaultKeybinds)) candidateFiles.Add(defaultKeybinds);

			// Check client launchers
			string[] launcherRoots = new string[]
			{
				Path.Combine(appData, @".minecraft"),
				Path.Combine(appData, @".feather\player-options.txt"),
				Path.Combine(userProf, @".lunarclient\settings\game\options.txt"),
				Path.Combine(appData, @".tlauncher\legacy\Minecraft\game\options.txt"),
				Path.Combine(appData, @"PrismLauncher\instances"),
				Path.Combine(appData, @"ModrinthApp\profiles"),
				Path.Combine(appData, @".curseforge\minecraft\Instances")
			};

			foreach (string root in launcherRoots)
			{
				try
				{
					if (File.Exists(root) && !candidateFiles.Contains(root))
					{
						candidateFiles.Add(root);
					}
					else if (Directory.Exists(root))
					{
						string[] optFiles = Directory.GetFiles(root, "options*.txt", SearchOption.AllDirectories);
						foreach (string of in optFiles)
						{
							if (!candidateFiles.Contains(of)) candidateFiles.Add(of);
						}
					}
				}
				catch { }
			}

			// Also inspect config directory for mod keybinds / cheat binds
			string configDir = Path.Combine(appData, @".minecraft\config");
			if (Directory.Exists(configDir))
			{
				try
				{
					string[] cfgFiles = Directory.GetFiles(configDir, "*bind*.json", SearchOption.AllDirectories);
					foreach (string cf in cfgFiles)
					{
						if (!candidateFiles.Contains(cf)) candidateFiles.Add(cf);
					}
				}
				catch { }
			}

			if (candidateFiles.Count == 0)
			{
				// Add fallback info item
				list.Add(new MinecraftKeybindItem
				{
					Index = idx++,
					Category = "Штатное управление",
					ParameterName = "options.txt",
					AssignedKey = "N/A",
					TargetRule = "ЧИСТО",
					Severity = "INFO",
					Verdict = "НОРМА",
					SourceFile = defaultOptions,
					FileModified = DateTime.Now,
					IsViolation = false,
					RowColor = Ui.TextMuted,
					Details = "Файл options.txt не найден по стандартному пути .minecraft. Запуск без локальных настроек.",
					RawLine = "N/A"
				});
				return list;
			}

			foreach (string filePath in candidateFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(filePath);
					string[] lines = File.ReadAllLines(filePath);

					foreach (string line in lines)
					{
						string trimmed = line.Trim();
						if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

						int colon = trimmed.IndexOf(':');
						if (colon <= 0) continue;

						string param = trimmed.Substring(0, colon).Trim();
						string val = trimmed.Substring(colon + 1).Trim();

						string lowerParam = param.ToLowerInvariant();
						string lowerVal = val.ToLowerInvariant();

						// 1. Check for cheat bindings (KillAura, Speed, Fly, Xray, etc.)
						if (IsCheatKeybind(lowerParam, lowerVal))
						{
							list.Add(new MinecraftKeybindItem
							{
								Index = idx++,
								Category = "Чит-бинд (5.01)",
								ParameterName = param,
								AssignedKey = DecodeMinecraftKey(val),
								TargetRule = "5.01",
								Severity = "CRITICAL",
								Verdict = "БАН 5.01",
								SourceFile = filePath,
								FileModified = fi.LastWriteTime,
								IsViolation = true,
								RowColor = Ui.AccentRed,
								Details = string.Format("Обнаружен запрещенный чит-бинд '{0}' со значением '{1}'. В чит-клиентах клавиша привязана к боевым/перемещающим модулям чита.", param, val),
								RawLine = trimmed
							});
						}
						// 2. Check for macro / autoclicker anomalies (Rule 5.03)
						else if (IsMacroOrAutoclickerBind(lowerParam, lowerVal))
						{
							list.Add(new MinecraftKeybindItem
							{
								Index = idx++,
								Category = "Макрос кликера (5.03)",
								ParameterName = param,
								AssignedKey = DecodeMinecraftKey(val),
								TargetRule = "5.03",
								Severity = "WARNING",
								Verdict = "ПОДОЗРЕНИЕ 5.03",
								SourceFile = filePath,
								FileModified = fi.LastWriteTime,
								IsViolation = false,
								RowColor = Ui.AccentOrange,
								Details = string.Format("Обнаружена настройка макроса/двойной атаки '{0}' -> '{1}'. Проверьте на сторонние мышиные бинды и зажатие клавиши атаки.", param, val),
								RawLine = trimmed
							});
						}
						// 3. Check for Atlantis CPVP Whitelisted mod binds
						else if (ServerRulesProfileManager.IsCpvpWhitelisted(lowerParam) || ServerRulesProfileManager.IsCpvpWhitelisted(lowerVal))
						{
							list.Add(new MinecraftKeybindItem
							{
								Index = idx++,
								Category = "CPVP оптимизатор (OK)",
								ParameterName = param,
								AssignedKey = DecodeMinecraftKey(val),
								TargetRule = "ЧИСТО",
								Severity = "INFO",
								Verdict = "РАЗРЕШЕНО",
								SourceFile = filePath,
								FileModified = fi.LastWriteTime,
								IsViolation = false,
								RowColor = Ui.AccentBlue,
								Details = "Разрешенный бинд CPVP оптимизатора (Marlow/Haram/EndCrystal/Walksy/Hero's/CuteBow) согласно вайтлисту Атлантис.",
								RawLine = trimmed
							});
						}
						// 4. Check for server command binds (/home, /spawn, etc.)
						else if (IsServerCommandBind(lowerParam, lowerVal))
						{
							list.Add(new MinecraftKeybindItem
							{
								Index = idx++,
								Category = "Серверный бинд (OK)",
								ParameterName = param,
								AssignedKey = DecodeMinecraftKey(val),
								TargetRule = "ЧИСТО",
								Severity = "INFO",
								Verdict = "РАЗРЕШЕНО",
								SourceFile = filePath,
								FileModified = fi.LastWriteTime,
								IsViolation = false,
								RowColor = Ui.AccentGreen,
								Details = "Легитимный бинд серверной команды (/home, /spawn, /tpa). Полностью разрешен правилом 5.03.",
								RawLine = trimmed
							});
						}
						// 5. Standard Minecraft keybinds
						else if (lowerParam.StartsWith("key_") || lowerParam.StartsWith("soundcategory_") || lowerParam == "fov" || lowerParam == "gamma")
						{
							if (lowerParam.StartsWith("key_"))
							{
								list.Add(new MinecraftKeybindItem
								{
									Index = idx++,
									Category = "Штатное управление",
									ParameterName = param,
									AssignedKey = DecodeMinecraftKey(val),
									TargetRule = "ЧИСТО",
									Severity = "INFO",
									Verdict = "НОРМА",
									SourceFile = filePath,
									FileModified = fi.LastWriteTime,
									IsViolation = false,
									RowColor = Ui.TextMuted,
									Details = "Штатная клавиша управления Minecraft.",
									RawLine = trimmed
								});
							}
						}
					}
				}
				catch { }
			}

			if (list.Count == 0)
			{
				list.Add(new MinecraftKeybindItem
				{
					Index = idx++,
					Category = "Штатное управление",
					ParameterName = "Стандартная раскладка",
					AssignedKey = "WASD + Mouse",
					TargetRule = "ЧИСТО",
					Severity = "INFO",
					Verdict = "НОРМА",
					SourceFile = defaultOptions,
					FileModified = DateTime.Now,
					IsViolation = false,
					RowColor = Ui.TextPrimary,
					Details = "Все клавиатурные бинды проверены: сторонних чит-привязок и нелегитимных макросов не обнаружено.",
					RawLine = "Clean"
				});
			}

			return list;
		}

		private static bool IsCheatKeybind(string param, string val)
		{
			string combined = param + " " + val;
			string[] cheatKeywords = new string[]
			{
				"killaura", "aura", "aimbot", "triggerbot", "reach", "hitbox",
				"flight", "bhop", "timer", "vclip", "hclip", "packetfly",
				"xray", "chams", "wallhack", "chestesp", "tracers",
				"autototem", "autoarmor", "autopot", "scaffold", "fastplace",
				"velocity", "antiknockback", "nofall", "criticals",
				"freecam", "derp", "spinbot", "spider", "baritone",
				"#goto", "#mine", ".bind", ".vclip", ".say"
			};

			foreach (string kw in cheatKeywords)
			{
				if (combined.Contains(kw)) return true;
			}
			return false;
		}

		private static bool IsMacroOrAutoclickerBind(string param, string val)
		{
			if (param == "key_key.attack")
			{
				if (val.Contains("key.keyboard.space") || val.Contains("key.keyboard.enter") ||
					val.Contains("key.keyboard.r") || val.Contains("key.keyboard.f"))
				{
					return true;
				}
			}

			string combined = param + " " + val;
			if (combined.Contains("autoclicker") || combined.Contains("cps_boost") ||
				combined.Contains("fast_click") || combined.Contains("double_click"))
			{
				return true;
			}

			return false;
		}

		private static bool IsServerCommandBind(string param, string val)
		{
			string combined = param + " " + val;
			string[] serverCmds = new string[]
			{
				"/home", "/spawn", "/tpa", "/tpaccept", "/clan", "/ah",
				"/pay", "/kit", "/call", "/tell", "/msg", "/warp", "/feed"
			};

			foreach (string cmd in serverCmds)
			{
				if (combined.Contains(cmd)) return true;
			}
			return false;
		}

		private static string DecodeMinecraftKey(string rawVal)
		{
			if (string.IsNullOrEmpty(rawVal)) return "None";

			if (rawVal.StartsWith("key.keyboard."))
			{
				string k = rawVal.Substring("key.keyboard.".Length).ToUpperInvariant();
				return k;
			}
			if (rawVal.StartsWith("key.mouse."))
			{
				string m = rawVal.Substring("key.mouse.".Length);
				if (m == "left") return "LMB (ЛКМ)";
				if (m == "right") return "RMB (ПКМ)";
				if (m == "middle") return "MMB (СКМ)";
				return "Mouse " + m;
			}
			int code;
			if (int.TryParse(rawVal, out code))
			{
				switch (code)
				{
					case 17: return "W";
					case 30: return "A";
					case 31: return "S";
					case 32: return "D";
					case 57: return "SPACE";
					case 42: return "LSHIFT";
					case 29: return "LCONTROL";
					case 19: return "R";
					case 33: return "F";
					case -100: return "LMB (ЛКМ)";
					case -99: return "RMB (ПКМ)";
					case -98: return "MMB (СКМ)";
					default: return "Code: " + code;
				}
			}

			return rawVal;
		}

		private void CopyBanCommand()
		{
			if (lvKeybinds.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования команды бана.", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			MinecraftKeybindItem it = lvKeybinds.SelectedItems[0].Tag as MinecraftKeybindItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.01 - Чит-бинд в options.txt [{1}:{2}]", targetPlayer, it.ParameterName, it.AssignedKey);
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
			int cCheat = 0;
			int cMacro = 0;
			int cServer = 0;
			int cCpvp = 0;
			foreach (MinecraftKeybindItem it in allItems)
			{
				if (it.IsViolation && it.TargetRule == "5.01") cCheat++;
				else if (it.TargetRule == "5.03" || it.Category.Contains("Макрос")) cMacro++;
				else if (it.Category.Contains("Серверный")) cServer++;
				else if (it.Category.Contains("CPVP")) cCpvp++;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine(string.Format("ОТЧЕТ ПРОВЕРКИ БИНДОВ И ОПЦИЙ MINECRAFT — ИГРОК: {0}", targetPlayer));
			sb.AppendLine(string.Format("Всего записей:   {0}", allItems.Count));
			sb.AppendLine(string.Format("Чит-бинды 5.01:  {0}", cCheat));
			sb.AppendLine(string.Format("Макросы 5.03:    {0}", cMacro));
			sb.AppendLine(string.Format("Серверные бинды: {0}", cServer));
			sb.AppendLine(string.Format("CPVP бинды (OK): {0}", cCpvp));
			sb.AppendLine("```");

			try
			{
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Discord отчет скопирован в буфер обмена!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера обмена: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML отчет (*.html)|*.html";
			sfd.FileName = string.Format("KeybindAudit_{0}_{1:yyyyMMdd_HHmm}.html", targetPlayer, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Криминалистический аудит биндов Minecraft</title>");
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
					sb.AppendLine(string.Format("<h1>🎮 Криминалистический аудит биндов Minecraft: {0}</h1>", targetPlayer));
					sb.AppendLine(string.Format("<p>Дата проверки: {0:dd.MM.yyyy HH:mm:ss} | Регламент «Атлантис» 5.01-5.08</p>", DateTime.Now));
					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Категория</th><th>Параметр</th><th>Клавиша</th><th>Правило</th><th>Вердикт</th><th>Файл</th><th>Детали</th></tr>");

					foreach (MinecraftKeybindItem it in allItems)
					{
						string cls = (it.Severity == "CRITICAL") ? "crit" : ((it.Severity == "WARNING") ? "warn" : "ok");
						sb.AppendLine(string.Format("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td></tr>",
							cls, it.Index, it.Category, it.ParameterName, it.AssignedKey, it.TargetRule, it.Verdict, it.SourceFile, it.Details));
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
