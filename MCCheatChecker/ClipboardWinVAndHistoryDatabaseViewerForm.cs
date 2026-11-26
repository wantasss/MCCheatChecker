using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum ClipboardForensicFilter
	{
		All,
		ViolationsOnly,
		CheatTokens501,
		CleanerCommands508,
		CheatUrls,
		DatabaseTampering,
		SafeClean
	}

	public class ClipboardForensicEvidenceItem
	{
		public int Index;
		public string Category; // "Токены и ключи читов (5.01)", "Команды клинеров (5.08)", "Ссылки чит-сервисов (5.01)", "Зачистка базы history.db (5.08)", "Активный буфер обмена", "Безопасные записи"
		public string SourceType; // "history.db", "history.db-wal", "Активный буфер Windows", "Реестр Clipboard"
		public string ContentSnippet;
		public string RawFullText;
		public string FileOrRegistryPath;
		public string Status; // "🚨 ЧИТ-ТОКЕН / КЛЮЧ (5.01)", "⛔ КОМАНДА КЛИНЕРА (5.08)", "⚠️ ССЫЛКА НА ЧИТ-СЕРВИС", "🚨 БАЗА ОБНУЛЕНА (5.08)", "🟢 БЕЗОПАСНАЯ ЗАПИСЬ"
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict; // "5.01 - Токен/ключ чита", "5.08 - Команда клинера", "5.08 - Зачистка history.db", "РАЗРЕШЕНО"
		public string TargetRule; // "5.01", "5.08", "ЧИСТО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalAnalysis;
	}

	public class ClipboardWinVAndHistoryDatabaseViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<ClipboardForensicEvidenceItem> allItems = new List<ClipboardForensicEvidenceItem>();
		private ClipboardForensicFilter currentFilter = ClipboardForensicFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiTokens;
		private Label lblKpiCleaners;
		private Label lblKpiUrls;
		private Label lblKpiTampering;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterTokens;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterUrls;
		private DarkButton btnFilterTampering;
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

		public ClipboardWinVAndHistoryDatabaseViewerForm() : this("Player")
		{
		}

		public ClipboardWinVAndHistoryDatabaseViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "📋 Инспектор истории буфера обмена Win+V и базы history.db (Tool 78 / Step 75) — MCCheatChecker";
			this.Size = new Size(1220, 750);
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
			lblHeaderTitle.Text = "📋 Инспектор истории буфера обмена Win+V и базы history.db (Tool 78 / Step 75)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокий судебный аудит базы SQLite history.db: извлечение токенов/ключей читов (Vape, Drip, Slinky), команд клинеров (fsutil, wevtutil, vssadmin) и детекция зачистки буфера.";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 38);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканера...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(40, 30, 20);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(260, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 280, 20);
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ЗАПИСЕЙ", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("ЧИТ-ТОКЕНЫ / КЛЮЧИ (5.01)", "0", Ui.AccentRed, ref xOff, out lblKpiTokens));
			kpiPanel.Controls.Add(CreateKpiBadge("КОМАНДЫ КЛИНЕРОВ (5.08)", "0", Ui.AccentOrange, ref xOff, out lblKpiCleaners));
			kpiPanel.Controls.Add(CreateKpiBadge("ССЫЛКИ ЧИТ-СЕРВИСОВ", "0", Ui.AccentOrange, ref xOff, out lblKpiUrls));
			kpiPanel.Controls.Add(CreateKpiBadge("ЗАЧИСТКА БАЗЫ (5.08)", "0", Ui.AccentPurple, ref xOff, out lblKpiTampering));
			kpiPanel.Controls.Add(CreateKpiBadge("БЕЗОПАСНЫЕ ЗАПИСИ", "0", Ui.AccentGreen, ref xOff, out lblKpiClean));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", ClipboardForensicFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения (5.01/5.08) (0)", ClipboardForensicFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterTokens = CreateFilterButton("Чит-токены (0)", ClipboardForensicFilter.CheatTokens501, ref fX, filterPanel);
			btnFilterCleaners = CreateFilterButton("Клинеры (0)", ClipboardForensicFilter.CleanerCommands508, ref fX, filterPanel);
			btnFilterUrls = CreateFilterButton("Ссылки (0)", ClipboardForensicFilter.CheatUrls, ref fX, filterPanel);
			btnFilterTampering = CreateFilterButton("Зачистка (0)", ClipboardForensicFilter.DatabaseTampering, ref fX, filterPanel);
			btnFilterClean = CreateFilterButton("Белый список (0)", ClipboardForensicFilter.SafeClean, ref fX, filterPanel);

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
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 30);
			btnExportHtml.Location = new Point(408, 9);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };
			bottomPanel.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(150, 30);
			btnRescan.Location = new Point(566, 9);
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
			listView.Columns.Add("Статус / Угроза", 175);
			listView.Columns.Add("Источник / Формат", 160);
			listView.Columns.Add("Категория", 150);
			listView.Columns.Add("Содержимое буфера", 260);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Файл / База", 220);
			listView.Columns.Add("Описание и криминалистика", 360);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ записи буфера");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ База history.db и сырые данные (Hex/Text)");
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

		private DarkButton CreateFilterButton(string text, ClipboardForensicFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (ClipboardForensicFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 135;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование базы history.db и буфера...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformClipboardDatabaseAudit();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования буфера: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<ClipboardForensicEvidenceItem> results = e.Result as List<ClipboardForensicEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (ClipboardForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation) violCount++;
				}

				if (violCount > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАРУШЕНИЯ ({0})", violCount);
					lblStatusPill.ForeColor = Ui.AccentRed;
					lblStatusPill.BackColor = Color.FromArgb(50, 20, 20);
				}
				else
				{
					lblStatusPill.Text = "🟢 БУФЕР БЕЗОПАСЕН";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int tokens = 0;
			int cleaners = 0;
			int urls = 0;
			int tampering = 0;
			int clean = 0;

			foreach (ClipboardForensicEvidenceItem it in allItems)
			{
				if (it.Category.Contains("Токены") && it.IsViolation) tokens++;
				if (it.Category.Contains("клинеров") && it.IsViolation) cleaners++;
				if (it.Category.Contains("Ссылки") && it.IsViolation) urls++;
				if (it.Category.Contains("Зачистка") && it.IsViolation) tampering++;
				if (!it.IsViolation) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiTokens.Text = tokens.ToString();
			lblKpiCleaners.Text = cleaners.ToString();
			lblKpiUrls.Text = urls.ToString();
			lblKpiTampering.Text = tampering.ToString();
			lblKpiClean.Text = clean.ToString();

			int viol = tokens + cleaners + urls + tampering;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения ({0})", viol);
			btnFilterTokens.Text = string.Format("Токены ({0})", tokens);
			btnFilterCleaners.Text = string.Format("Клинеры ({0})", cleaners);
			btnFilterUrls.Text = string.Format("Ссылки ({0})", urls);
			btnFilterTampering.Text = string.Format("Зачистка ({0})", tampering);
			btnFilterClean.Text = string.Format("Белый список ({0})", clean);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (ClipboardForensicEvidenceItem it in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case ClipboardForensicFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case ClipboardForensicFilter.CheatTokens501:
						matchFilter = it.Category.Contains("Токены") || it.TargetRule == "5.01";
						break;
					case ClipboardForensicFilter.CleanerCommands508:
						matchFilter = it.Category.Contains("клинеров");
						break;
					case ClipboardForensicFilter.CheatUrls:
						matchFilter = it.Category.Contains("Ссылки");
						break;
					case ClipboardForensicFilter.DatabaseTampering:
						matchFilter = it.Category.Contains("Зачистка");
						break;
					case ClipboardForensicFilter.SafeClean:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (it.ContentSnippet != null && it.ContentSnippet.ToLowerInvariant().Contains(search))
						|| (it.RawFullText != null && it.RawFullText.ToLowerInvariant().Contains(search))
						|| (it.SourceType != null && it.SourceType.ToLowerInvariant().Contains(search))
						|| (it.Category != null && it.Category.ToLowerInvariant().Contains(search))
						|| (it.FileOrRegistryPath != null && it.FileOrRegistryPath.ToLowerInvariant().Contains(search))
						|| (it.DetailDescription != null && it.DetailDescription.ToLowerInvariant().Contains(search))
						|| (it.Status != null && it.Status.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.SourceType);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.ContentSnippet);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.FileOrRegistryPath);
				lvi.SubItems.Add(it.DetailDescription);

				lvi.Tag = it;
				lvi.ForeColor = it.RowColor;

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

			ClipboardForensicEvidenceItem it = listView.SelectedItems[0].Tag as ClipboardForensicEvidenceItem;
			if (it == null) return;

			// Tab 1: Судебный анализ
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine(" КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ ЗАПИСИ БУФЕРА ОБМЕНА");
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine("Источник данных:       " + it.SourceType);
			sbAnalysis.AppendLine("Категория экспертизы:  " + it.Category);
			sbAnalysis.AppendLine("Файл / База / Ветка:   " + it.FileOrRegistryPath);
			sbAnalysis.AppendLine("Статус проверки:       " + it.Status);
			sbAnalysis.AppendLine("Уровень угрозы:        " + it.ThreatLevel);
			sbAnalysis.AppendLine("Пункт регламента:      " + it.TargetRule);
			sbAnalysis.AppendLine("Временная метка:       " + it.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- СОДЕРЖИМОЕ ЗАПИСИ (ФРАГМЕНТ) ---");
			sbAnalysis.AppendLine(it.ContentSnippet);
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- ПОДРОБНОЕ КРИМИНАЛИСТИЧЕСКОЕ ЗАКЛЮЧЕНИЕ ---");
			sbAnalysis.AppendLine(it.DetailDescription);
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- АРГУМЕНТАЦИЯ ДЛЯ ВЕРДИКТА ---");
			sbAnalysis.AppendLine(it.TechnicalAnalysis);
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Технический дамп и полный текст
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine(" СЫРОЙ ТЕКСТ / ТЕХНИЧЕСКИЙ ДАМП БУФЕРА ОБМЕНА");
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine("Длина текста:          " + (it.RawFullText != null ? it.RawFullText.Length : 0) + " символов");
			sbTech.AppendLine("Хранилище:             " + it.FileOrRegistryPath);
			sbTech.AppendLine();
			sbTech.AppendLine("--- ПОЛНЫЙ ИЗВЛЕЧЁННЫЙ ТЕКСТ ---");
			sbTech.AppendLine(!string.IsNullOrEmpty(it.RawFullText) ? it.RawFullText : "(Пустое содержимое)");
			sbTech.AppendLine();
			sbTech.AppendLine("--- КРИТЕРИИ ДЕТЕКЦИИ СИСТЕМЫ ---");
			if (it.Category.Contains("Токены"))
			{
				sbTech.AppendLine("Тип совпадения: Токен авторизации или ключ лицензии инжектора чита.");
				sbTech.AppendLine("Паттерны:       vape.gg/token, driplite.cc, slinky.gg, speclient, blast.hk token.");
			}
			else if (it.Category.Contains("клинеров"))
			{
				sbTech.AppendLine("Тип совпадения: Скрипт уничтожения цифровых следов (Anti-Forensics Cleaner).");
				sbTech.AppendLine("Паттерны:       fsutil usn deletejournal, wevtutil cl, vssadmin delete shadows, sdelete.");
			}
			else if (it.Category.Contains("Зачистка"))
			{
				sbTech.AppendLine("Тип совпадения: Принудительное удаление / обнуление базы данных history.db.");
				sbTech.AppendLine("Аномалия:       Служба ClipboardHistory включена в реестре, но файл базы равен 0 байт.");
			}
			else
			{
				sbTech.AppendLine("Запись является штатным текстом пользователя и не нарушает правил проекта.");
			}
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Вердикт бана
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("================================================================================");
			sbBan.AppendLine(" РЕШЕНИЕ МОДЕРАЦИИ И ВЕРДИКТ БАНА (РЕГЛАМЕНТ ПРОВЕРКИ)");
			sbBan.AppendLine("================================================================================");
			sbBan.AppendLine("Никнейм игрока:       " + targetPlayerName);
			sbBan.AppendLine("Статус проверки:      " + (it.IsViolation ? "НАРУШЕНИЕ ОБНАРУЖЕНО" : "ЧИСТО"));
			sbBan.AppendLine("Пункт регламента:     " + it.TargetRule);
			sbBan.AppendLine("Категория нарушения:  " + it.Category);
			sbBan.AppendLine();
			if (it.IsViolation)
			{
				string reason;
				if (it.TargetRule == "5.08")
				{
					reason = "5.08 Уклонение от проверки (Команды клинеров / Зачистка history.db)";
				}
				else
				{
					reason = "5.01 Использование читов (Токен/ключ чита в буфере обмена)";
				}

				sbBan.AppendLine("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:");
				sbBan.AppendLine("/ban " + targetPlayerName + " 30d " + reason);
				sbBan.AppendLine();
				sbBan.AppendLine("ПОЯСНЕНИЕ ДЛЯ АПЕЛЛЯЦИОННОЙ ЖАЛОБЫ:");
				sbBan.AppendLine("В истории буфера обмена (Win+V / history.db) зафиксирован запрещённый артефакт: " + it.DetailDescription);
				sbBan.AppendLine("Источник улик: " + it.FileOrRegistryPath);
			}
			else
			{
				sbBan.AppendLine("В буфере обмена и базе history.db запрещённых токенов, ключей читов и клинеров не обнаружено.");
			}
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в списке для детального анализа буфера обмена.";
			txtTechnical.Text = "Нет данных технического дампа.";
			txtBanVerdict.Text = "Нет выбранного вердикта.";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Сначала выберите нарушение в списке!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ClipboardForensicEvidenceItem it = listView.SelectedItems[0].Tag as ClipboardForensicEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = (it.TargetRule == "5.08")
				? "5.08 Уклонение от проверки (Клинеры / Зачистка history.db)"
				: "5.01 Использование читов (Чит-токен в буфере обмена)";

			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**📋 MCCheatChecker — Отчёт инспектора буфера обмена Win+V и history.db**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (ClipboardForensicEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные уловки и токены в буфере:**");
				foreach (ClipboardForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.TargetRule + "]` **" + it.Category + "**: `" + it.ContentSnippet + "` — " + it.DetailDescription);
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.01/5.08 Токены читов / зачистка буфера обмена`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Чит-токенов Vape/Drip/Slinky, команд клинеров и зачистки history.db не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_ClipboardReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт аудита буфера — " + targetPlayerName + "</title>");
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
					sb.AppendLine("<h1>📋 Отчёт инспектора истории буфера обмена Win+V и history.db</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО ЗАПИСЕЙ</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЧИТ-ТОКЕНЫ</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiTokens.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>КЛИНЕРЫ</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiCleaners.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ССЫЛКИ ЧИТОВ</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiUrls.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЗАЧИСТКА БАЗЫ</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiTampering.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>БЕЗОПАСНЫЕ</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiClean.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Статус</th><th>Источник</th><th>Категория</th><th>Содержимое</th><th>Правило</th><th>Путь</th><th>Детали</th></tr>");
					foreach (ClipboardForensicEvidenceItem it in allItems)
					{
						string cls = it.IsViolation ? (it.TargetRule == "5.08" ? "warn" : "crit") : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td class='" + cls + "'>" + it.Status + "</td>");
						sb.AppendLine("<td>" + it.SourceType + "</td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td><code>" + System.Web.HttpUtility.HtmlEncode(it.ContentSnippet) + "</code></td>");
						sb.AppendLine("<td>" + it.TargetRule + "</td>");
						sb.AppendLine("<td><code>" + it.FileOrRegistryPath + "</code></td>");
						sb.AppendLine("<td>" + it.DetailDescription + "</td>");
						sb.AppendLine("</tr>");
					}
					sb.AppendLine("</table>");
					sb.AppendLine("</body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("HTML-отчёт успешно сохранён:\n" + sfd.FileName, "Экспорт отчёта", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}

		#region Forensic Scanner Engine

		public static List<ClipboardForensicEvidenceItem> PerformClipboardDatabaseAudit()
		{
			List<ClipboardForensicEvidenceItem> results = new List<ClipboardForensicEvidenceItem>();
			int idx = 1;

			string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string clipDir = Path.Combine(localApp, "Microsoft", "Windows", "Clipboard");
			string dbPath = Path.Combine(clipDir, "history.db");
			string walPath = Path.Combine(clipDir, "history.db-wal");

			// 1. Проверка конфигурации реестра Windows (EnableClipboardHistory)
			bool historyEnabledInReg = false;
			try
			{
				using (RegistryKey cKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
				{
					if (cKey != null)
					{
						object val = cKey.GetValue("EnableClipboardHistory");
						if (val is int && (int)val == 1) historyEnabledInReg = true;
					}
				}
			}
			catch { }

			// 2. Аудит базы history.db
			if (File.Exists(dbPath))
			{
				FileInfo fi = new FileInfo(dbPath);

				if (fi.Length == 0)
				{
					// База обнулена (0 байт) — классический паттерн клинеров
					ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
					item.Index = idx++;
					item.Category = "Зачистка базы history.db (5.08)";
					item.SourceType = "history.db (0 байт)";
					item.ContentSnippet = "[Файл history.db имеет размер 0 байт]";
					item.RawFullText = "Размер файла: 0 байт. Последнее изменение: " + fi.LastWriteTime;
					item.FileOrRegistryPath = dbPath;
					item.Status = "🚨 БАЗА HISTORY.DB ОБНУЛЕНА (5.08)";
					item.ThreatLevel = "CRITICAL";
					item.Verdict = "5.08 - Принудительная зачистка базы буфера";
					item.TargetRule = "5.08";
					item.IsViolation = true;
					item.IsWhitelisted = false;
					item.Timestamp = fi.LastWriteTime;
					item.RowColor = Ui.AccentRed;
					item.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08: Файл базы истории буфера обмена history.db имеет нулевой размер (0 байт) при дате изменения {0}. Это прямое свидетельство применения софта-клинера для уничтожения истории перед проверкой.", fi.LastWriteTime);
					item.TechnicalAnalysis = "Файл %LocalAppData%\\Microsoft\\Windows\\Clipboard\\history.db был усечён до 0 байт. Нарушение пункта 5.08 регламента проверок.";
					results.Add(item);
				}
				else
				{
					// Считываем бинарные страницы history.db и history.db-wal
					List<string> extractedStrings = ReadAndExtractStringsFromDbFile(dbPath);
					if (File.Exists(walPath))
					{
						List<string> walStrings = ReadAndExtractStringsFromDbFile(walPath);
						extractedStrings.AddRange(walStrings);
					}

					AuditExtractedClipboardStrings(results, ref idx, extractedStrings, dbPath, fi.LastWriteTime);
				}
			}
			else
			{
				if (historyEnabledInReg)
				{
					// История включена, но файла нет — подозрение на удаление
					ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
					item.Index = idx++;
					item.Category = "Зачистка базы history.db (5.08)";
					item.SourceType = "Реестр / ФС";
					item.ContentSnippet = "Служба Win+V активна, файл history.db отсутствует";
					item.RawFullText = "EnableClipboardHistory = 1, но файл history.db не найден на диске.";
					item.FileOrRegistryPath = dbPath;
					item.Status = "⚠️ БАЗА УДАЛЕНА ПРИ ВКЛЮЧЕННОЙ СЛУЖБЕ";
					item.ThreatLevel = "WARNING";
					item.Verdict = "5.08 - Подозрение на удаление базы";
					item.TargetRule = "5.08";
					item.IsViolation = false;
					item.IsWhitelisted = false;
					item.Timestamp = DateTime.Now;
					item.RowColor = Ui.AccentOrange;
					item.DetailDescription = "В реестре параметр EnableClipboardHistory=1 (история буфера включена), но файл history.db отсутствует. Возможно, база была недавно стёрта скриптом очистки.";
					item.TechnicalAnalysis = "Аудит реестра и файловой системы: база history.db не найдена.";
					results.Add(item);
				}
				else
				{
					// История просто отключена
					ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
					item.Index = idx++;
					item.Category = "Безопасные записи";
					item.SourceType = "Реестр Windows";
					item.ContentSnippet = "История буфера обмена Windows (Win+V) отключена";
					item.RawFullText = "EnableClipboardHistory = 0";
					item.FileOrRegistryPath = @"HKCU\Software\Microsoft\Clipboard";
					item.Status = "🟢 ИСТОРИЯ БУФЕРА ОТКЛЮЧЕНА";
					item.ThreatLevel = "SAFE";
					item.Verdict = "РАЗРЕШЕНО";
					item.TargetRule = "ЧИСТО";
					item.IsViolation = false;
					item.IsWhitelisted = true;
					item.Timestamp = DateTime.Now;
					item.RowColor = Ui.AccentGreen;
					item.DetailDescription = "Служба истории буфера обмена Windows (Win+V) отключена в настройках системы. Это штатная конфигурация Windows.";
					item.TechnicalAnalysis = "Параметр EnableClipboardHistory не равен 1. Нарушений нет.";
					results.Add(item);
				}
			}

			// 3. Сканирование текущего активного буфера обмена Windows в отдельном STA-потоке
			ScanActiveClipboardMemory(results, ref idx);

			return results;
		}

		private static List<string> ReadAndExtractStringsFromDbFile(string path)
		{
			List<string> list = new List<string>();
			try
			{
				byte[] dbBytes = null;
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					using (MemoryStream ms = new MemoryStream())
					{
						byte[] buffer = new byte[8192];
						int read;
						while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
						{
							ms.Write(buffer, 0, read);
						}
						dbBytes = ms.ToArray();
					}
				}

				if (dbBytes != null && dbBytes.Length > 0)
				{
					// 1. ASCII / UTF-8
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < dbBytes.Length; i++)
					{
						byte b = dbBytes[i];
						if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13 || (b >= 192 && b <= 255))
						{
							sb.Append((char)b);
						}
						else
						{
							if (sb.Length >= 4)
							{
								list.Add(sb.ToString());
							}
							sb.Length = 0;
						}
					}
					if (sb.Length >= 4) list.Add(sb.ToString());

					// 2. UTF-16 LE
					StringBuilder sbUni = new StringBuilder();
					for (int i = 0; i < dbBytes.Length - 1; i += 2)
					{
						byte b1 = dbBytes[i];
						byte b2 = dbBytes[i + 1];
						if (b2 == 0 && ((b1 >= 32 && b1 <= 126) || b1 == 9 || b1 == 10 || b1 == 13))
						{
							sbUni.Append((char)b1);
						}
						else if (b2 == 4 && b1 >= 0) // Кириллица UTF-16
						{
							sbUni.Append((char)(0x0400 + b1));
						}
						else
						{
							if (sbUni.Length >= 4)
							{
								list.Add(sbUni.ToString());
							}
							sbUni.Length = 0;
						}
					}
					if (sbUni.Length >= 4) list.Add(sbUni.ToString());
				}
			}
			catch { }
			return list;
		}

		private static void AuditExtractedClipboardStrings(List<ClipboardForensicEvidenceItem> results, ref int idx, List<string> strings, string dbPath, DateTime dbTime)
		{
			// Паттерны токенов и ключей читов (5.01)
			string[] cheatTokenPatterns = new string[]
			{
				"vape.gg/token", "vape.gg", "driplite", "drip.gg", "slinky.gg",
				"celestial", "expensive", "nursultan", "neverhook", "minced",
				"wildclient", "speclient", "deadcode", "zamorozka", "akrien",
				"wurstclient", "meteorclient", "intent.store", "liquidbounce"
			};

			// Паттерны команд клинеров и зачистки улик (5.08)
			string[] cleanerPatterns = new string[]
			{
				"fsutil usn deletejournal", "wevtutil cl", "vssadmin delete shadows",
				"sdelete", "cipher /w", "clear-eventlog", "remove-itemproperty",
				"del /f /q %temp%", "rmdir /s /q", "reg delete"
			};

			// Паттерны ссылок на чит-сервисы и маркетплейсы читов
			string[] cheatUrlPatterns = new string[]
			{
				"blast.hk", "yougame.biz", "lolz.guru", "lolz.live", "funpay.com", "funpay.ru"
			};

			int cleanExtractedCount = 0;
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string s in strings)
			{
				string trimmed = s.Trim();
				if (trimmed.Length < 4) continue;
				if (seen.Contains(trimmed)) continue;
				seen.Add(trimmed);

				string lower = trimmed.ToLowerInvariant();

				// Проверка на команды клинеров (5.08)
				bool isCleaner = false;
				foreach (string cp in cleanerPatterns)
				{
					if (lower.Contains(cp))
					{
						isCleaner = true;
						ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Команды клинеров (5.08)";
						item.SourceType = "history.db SQLite";
						item.ContentSnippet = trimmed.Length > 80 ? trimmed.Substring(0, 80) + "..." : trimmed;
						item.RawFullText = trimmed;
						item.FileOrRegistryPath = dbPath;
						item.Status = "⛔ КОМАНДА КЛИНЕРА В БУФЕРЕ (5.08)";
						item.ThreatLevel = "CRITICAL";
						item.Verdict = "5.08 - Команда уничтожения цифровых улик";
						item.TargetRule = "5.08";
						item.IsViolation = true;
						item.IsWhitelisted = false;
						item.Timestamp = dbTime;
						item.RowColor = Ui.AccentRed;
						item.DetailDescription = string.Format("В истории буфера обмена history.db обнаружена скопированная команда зачистки/самоликвидации: '{0}'. Используется для затирания журналов USN, EventLog или теневых копий VSS перед проверкой.", trimmed);
						item.TechnicalAnalysis = string.Format("Сигнатура команды клинера: {0}. Файл базы: {1}.", cp, dbPath);
						results.Add(item);
						break;
					}
				}
				if (isCleaner) continue;

				// Проверка на токены и ключи читов (5.01)
				bool isCheatToken = false;
				foreach (string tp in cheatTokenPatterns)
				{
					if (lower.Contains(tp))
					{
						isCheatToken = true;
						ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Токены и ключи читов (5.01)";
						item.SourceType = "history.db SQLite";
						item.ContentSnippet = trimmed.Length > 80 ? trimmed.Substring(0, 80) + "..." : trimmed;
						item.RawFullText = trimmed;
						item.FileOrRegistryPath = dbPath;
						item.Status = "🚨 ЧИТ-ТОКЕН / КЛЮЧ В БУФЕРЕ (5.01)";
						item.ThreatLevel = "CRITICAL";
						item.Verdict = "5.01 - Токен/ключ чит-клиента";
						item.TargetRule = "5.01";
						item.IsViolation = true;
						item.IsWhitelisted = false;
						item.Timestamp = dbTime;
						item.RowColor = Ui.AccentRed;
						item.DetailDescription = string.Format("В истории буфера обмена history.db обнаружен токен авторизации или ключ чита: '{0}'. Игрок копировал токен для инжекта или запуска чит-клиента ({1}).", trimmed, tp);
						item.TechnicalAnalysis = string.Format("Сигнатура чит-токена: {0}. Зафиксировано в SQLite странице history.db.", tp);
						results.Add(item);
						break;
					}
				}
				if (isCheatToken) continue;

				// Проверка на ссылки чит-маркетов
				bool isCheatUrl = false;
				foreach (string up in cheatUrlPatterns)
				{
					if (lower.Contains(up))
					{
						isCheatUrl = true;
						ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Ссылки чит-сервисов (5.01)";
						item.SourceType = "history.db SQLite";
						item.ContentSnippet = trimmed.Length > 80 ? trimmed.Substring(0, 80) + "..." : trimmed;
						item.RawFullText = trimmed;
						item.FileOrRegistryPath = dbPath;
						item.Status = "⚠️ ССЫЛКА НА ЧИТ-СЕРВИС";
						item.ThreatLevel = "WARNING";
						item.Verdict = "5.01 - Ссылка на чит-форум / маркет";
						item.TargetRule = "5.01";
						item.IsViolation = true;
						item.IsWhitelisted = false;
						item.Timestamp = dbTime;
						item.RowColor = Ui.AccentOrange;
						item.DetailDescription = string.Format("В буфере обмена зафиксирована ссылка на ресурс распространения читов/аккаунтов: '{0}'.", trimmed);
						item.TechnicalAnalysis = string.Format("Чит-ресурс: {0}.", up);
						results.Add(item);
						break;
					}
				}
				if (isCheatUrl) continue;

				cleanExtractedCount++;
			}

			if (cleanExtractedCount > 0)
			{
				ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
				item.Index = idx++;
				item.Category = "Безопасные записи";
				item.SourceType = "history.db SQLite";
				item.ContentSnippet = string.Format("Штатные пользовательские записи ({0} шт.)", cleanExtractedCount);
				item.RawFullText = string.Format("Всего извлечено {0} легитимных текстовых фрагментов из history.db.", cleanExtractedCount);
				item.FileOrRegistryPath = dbPath;
				item.Status = "🟢 ЛЕГИТИМНЫЕ ЗАПИСИ БУФЕРА";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = dbTime;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = string.Format("Из базы history.db успешно извлечено и проанализировано {0} записей. Запрещённых токенов, ключей читов и клинеров среди них не найдено.", cleanExtractedCount);
				item.TechnicalAnalysis = "Парсинг бинарных страниц SQLite завершён без детектов читов.";
				results.Add(item);
			}
		}

		private static void ScanActiveClipboardMemory(List<ClipboardForensicEvidenceItem> results, ref int idx)
		{
			string activeText = null;

			// Вызов Clipboard.GetText() строго в STA потоке
			Thread staThread = new Thread(delegate()
			{
				try
				{
					if (Clipboard.ContainsText())
					{
						activeText = Clipboard.GetText();
					}
				}
				catch { }
			});
			staThread.SetApartmentState(ApartmentState.STA);
			staThread.Start();
			staThread.Join(500);

			if (!string.IsNullOrEmpty(activeText))
			{
				string trimmed = activeText.Trim();
				string lower = trimmed.ToLowerInvariant();

				bool isCheat = lower.Contains("vape.gg") || lower.Contains("driplite") || lower.Contains("slinky.gg") ||
					lower.Contains("celestial") || lower.Contains("expensive") || lower.Contains("nursultan") ||
					lower.Contains("fsutil usn") || lower.Contains("wevtutil cl") || lower.Contains("vssadmin delete");

				ClipboardForensicEvidenceItem item = new ClipboardForensicEvidenceItem();
				item.Index = idx++;
				item.SourceType = "Активный буфер Windows";
				item.ContentSnippet = trimmed.Length > 80 ? trimmed.Substring(0, 80) + "..." : trimmed;
				item.RawFullText = trimmed;
				item.FileOrRegistryPath = "RAM Clipboard Memory";
				item.Timestamp = DateTime.Now;

				if (isCheat)
				{
					item.Category = lower.Contains("fsutil") || lower.Contains("wevtutil") ? "Команды клинеров (5.08)" : "Токены и ключи читов (5.01)";
					item.Status = "🚨 ЧИТ / КЛИНЕР В АКТИВНОМ БУФЕРЕ!";
					item.ThreatLevel = "CRITICAL";
					item.Verdict = lower.Contains("fsutil") ? "5.08 - Клинер в буфере" : "5.01 - Чит-токен в буфере";
					item.TargetRule = lower.Contains("fsutil") ? "5.08" : "5.01";
					item.IsViolation = true;
					item.IsWhitelisted = false;
					item.RowColor = Ui.AccentRed;
					item.DetailDescription = string.Format("В текущем активном буфере обмена Windows прямо сейчас находится запрещённый артефакт: '{0}'.", trimmed);
					item.TechnicalAnalysis = "Аудит активного буфера через STA: зафиксирована вредоносная строка в памяти.";
				}
				else
				{
					item.Category = "Безопасные записи";
					item.Status = "🟢 АКТИВНЫЙ БУФЕР БЕЗОПАСЕН";
					item.ThreatLevel = "SAFE";
					item.Verdict = "РАЗРЕШЕНО";
					item.TargetRule = "ЧИСТО";
					item.IsViolation = false;
					item.IsWhitelisted = true;
					item.RowColor = Ui.AccentGreen;
					item.DetailDescription = "В текущем буфере обмена находится штатный пользовательский текст (не содержит токенов читов и команд клинеров).";
					item.TechnicalAnalysis = "Активный буфер проверен: читерских паттернов не найдено.";
				}

				results.Add(item);
			}
		}

		#endregion
	}
}
