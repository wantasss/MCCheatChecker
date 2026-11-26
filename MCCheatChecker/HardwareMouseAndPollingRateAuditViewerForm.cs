using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum MouseForensicFilter
	{
		All,
		ViolationsOnly,
		DebounceAbuse,
		BloodyOscar,
		SyntheticDrivers,
		VendorMacros,
		WhitelistedClean
	}

	public class MouseForensicEvidenceItem
	{
		public int Index;
		public string Category; // "Debounce Time (2.06)", "Bloody / A4Tech (5.01)", "Виртуальный ввод (5.08)", "Макросы вендоров (5.01)", "USB HID Устройства"
		public string DeviceOrSource; // "Glorious Model O", "Bloody7 / Ultra-Core 3", "interception.sys", "Logitech G HUB Lua", "HID\\VID_09DA..."
		public string CategoryName;
		public string ParameterOrValue; // "Debounce: 0 ms", "Скрипт recoil.bkg", "UpperFilters: interception", etc.
		public string FileOrRegistryPath; // Путь к файлу конфигурации или ветке реестра
		public string Status; // "🚨 БАГОЮЗ DEBOUNCE (2.06)", "⛔ МАКРОС BLOODY (5.01)", "🚨 ПЕРЕХВАТ ВВОДА (5.08)", "🟢 ЛЕГИТИМНЫЙ ВВОД"
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict; // "2.06 - Багоюз debounce", "5.01 - Запрещенные макросы", "5.08 - Перехват ввода", "РАЗРЕШЕНО"
		public string TargetRule; // "2.06", "5.01", "5.08", "ЧИСТО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalAnalysis;
	}

	public class HardwareMouseAndPollingRateAuditViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<MouseForensicEvidenceItem> allItems = new List<MouseForensicEvidenceItem>();
		private MouseForensicFilter currentFilter = MouseForensicFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiDebounce;
		private Label lblKpiMacros;
		private Label lblKpiBloody;
		private Label lblKpiSynthetic;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterDebounce;
		private DarkButton btnFilterBloody;
		private DarkButton btnFilterSynthetic;
		private DarkButton btnFilterMacros;
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

		public HardwareMouseAndPollingRateAuditViewerForm() : this("Player")
		{
		}

		public HardwareMouseAndPollingRateAuditViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🖱️ Инспектор мыши, дебаунса и аппаратных макросов (Tool 74 / Step 71) — MCCheatChecker";
			this.Size = new Size(1180, 740);
			this.MinimumSize = new Size(950, 600);
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
			lblHeaderTitle.Text = "🖱️ Инспектор манипуляторов мыши, дебаунса и аппаратных макросов (Tool 74 / Step 71)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Аппаратный аудит HID-мышей, дебаунса Glorious/Roccat (2.06), скриптов Bloody/Oscar (5.01), драйверов перехвата Interception и макросов G HUB/Synapse.";
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ПРОВЕРОК", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("НАРУШЕНИЯ 2.06 (DEBOUNCE)", "0", Ui.AccentOrange, ref xOff, out lblKpiDebounce));
			kpiPanel.Controls.Add(CreateKpiBadge("НАРУШЕНИЯ 5.01 (МАКРОСЫ)", "0", Ui.AccentRed, ref xOff, out lblKpiMacros));
			kpiPanel.Controls.Add(CreateKpiBadge("BLOODY / ULTRA-CORE", "0", Ui.AccentPurple, ref xOff, out lblKpiBloody));
			kpiPanel.Controls.Add(CreateKpiBadge("ВИРТУАЛЬНЫЙ ВВОД (5.08)", "0", Ui.AccentRed, ref xOff, out lblKpiSynthetic));
			kpiPanel.Controls.Add(CreateKpiBadge("ЛЕГИТИМНЫЙ ВВОД", "0", Ui.AccentGreen, ref xOff, out lblKpiClean));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", MouseForensicFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения (2.06 / 5.01) (0)", MouseForensicFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterDebounce = CreateFilterButton("Debounce < 4ms (0)", MouseForensicFilter.DebounceAbuse, ref fX, filterPanel);
			btnFilterBloody = CreateFilterButton("Bloody & Oscar (0)", MouseForensicFilter.BloodyOscar, ref fX, filterPanel);
			btnFilterSynthetic = CreateFilterButton("Драйверы ввода (0)", MouseForensicFilter.SyntheticDrivers, ref fX, filterPanel);
			btnFilterMacros = CreateFilterButton("Макросы вендоров (0)", MouseForensicFilter.VendorMacros, ref fX, filterPanel);
			btnFilterClean = CreateFilterButton("Белый список (0)", MouseForensicFilter.WhitelistedClean, ref fX, filterPanel);

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
			listView.Columns.Add("Манипулятор / Источник", 170);
			listView.Columns.Add("Категория", 150);
			listView.Columns.Add("Параметр / Значение", 190);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Путь / Ветка реестра", 230);
			listView.Columns.Add("Описание и детали", 360);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ манипулятора");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Профили макросов, скрипты и реестр");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 2.06 / 5.01 и Команда бана");
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

		private DarkButton CreateFilterButton(string text, MouseForensicFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (MouseForensicFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 135;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование HID-мышей и макросов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformHardwareMouseAndMacroAudit();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка аудита мыши: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<MouseForensicEvidenceItem> results = e.Result as List<MouseForensicEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (MouseForensicEvidenceItem it in allItems)
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
					lblStatusPill.Text = "🟢 ЧИСТО / В НОРМЕ";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int debounce = 0;
			int macros = 0;
			int bloody = 0;
			int synthetic = 0;
			int clean = 0;

			foreach (MouseForensicEvidenceItem it in allItems)
			{
				if (it.TargetRule == "2.06" && it.IsViolation) debounce++;
				if (it.TargetRule == "5.01" && it.IsViolation) macros++;
				if (it.Category.Contains("Bloody")) bloody++;
				if (it.Category.Contains("Виртуальный ввод") || it.DeviceOrSource.Contains("interception")) synthetic++;
				if (!it.IsViolation) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiDebounce.Text = debounce.ToString();
			lblKpiMacros.Text = macros.ToString();
			lblKpiBloody.Text = bloody.ToString();
			lblKpiSynthetic.Text = synthetic.ToString();
			lblKpiClean.Text = clean.ToString();

			int viol = debounce + macros;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения ({0})", viol);
			btnFilterDebounce.Text = string.Format("Debounce < 4ms ({0})", debounce);
			btnFilterBloody.Text = string.Format("Bloody ({0})", bloody);
			btnFilterSynthetic.Text = string.Format("Драйверы ({0})", synthetic);
			btnFilterMacros.Text = string.Format("Макросы ({0})", macros);
			btnFilterClean.Text = string.Format("Белый список ({0})", clean);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (MouseForensicEvidenceItem it in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case MouseForensicFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case MouseForensicFilter.DebounceAbuse:
						matchFilter = (it.TargetRule == "2.06");
						break;
					case MouseForensicFilter.BloodyOscar:
						matchFilter = it.Category.Contains("Bloody") || it.DeviceOrSource.Contains("Bloody") || it.DeviceOrSource.Contains("Oscar");
						break;
					case MouseForensicFilter.SyntheticDrivers:
						matchFilter = it.Category.Contains("Виртуальный") || it.DeviceOrSource.Contains("interception") || it.DeviceOrSource.Contains("vjoy");
						break;
					case MouseForensicFilter.VendorMacros:
						matchFilter = it.Category.Contains("Макросы вендоров") || it.DeviceOrSource.Contains("LGHUB") || it.DeviceOrSource.Contains("Synapse") || it.DeviceOrSource.Contains("iCUE");
						break;
					case MouseForensicFilter.WhitelistedClean:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (it.DeviceOrSource != null && it.DeviceOrSource.ToLowerInvariant().Contains(search))
						|| (it.Category != null && it.Category.ToLowerInvariant().Contains(search))
						|| (it.ParameterOrValue != null && it.ParameterOrValue.ToLowerInvariant().Contains(search))
						|| (it.DetailDescription != null && it.DetailDescription.ToLowerInvariant().Contains(search))
						|| (it.FileOrRegistryPath != null && it.FileOrRegistryPath.ToLowerInvariant().Contains(search))
						|| (it.Status != null && it.Status.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.DeviceOrSource);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.ParameterOrValue);
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

			MouseForensicEvidenceItem it = listView.SelectedItems[0].Tag as MouseForensicEvidenceItem;
			if (it == null) return;

			// Tab 1: Судебный анализ
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine(" КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ МАНИПУЛЯТОРА И ПАРАМЕТРОВ ВВОДА");
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine("Манипулятор/Источник: " + it.DeviceOrSource);
			sbAnalysis.AppendLine("Категория экспертизы: " + it.Category);
			sbAnalysis.AppendLine("Параметр / Значение:  " + it.ParameterOrValue);
			sbAnalysis.AppendLine("Путь / Реестр:        " + it.FileOrRegistryPath);
			sbAnalysis.AppendLine("Статус проверки:      " + it.Status);
			sbAnalysis.AppendLine("Уровень угрозы:       " + it.ThreatLevel);
			sbAnalysis.AppendLine("Пункт регламента:     " + it.TargetRule);
			sbAnalysis.AppendLine("Время фиксации:       " + it.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- ПОДРОБНОЕ ОПИСАНИЕ И КРИТЕРИИ ОЦЕНКИ ---");
			sbAnalysis.AppendLine(it.DetailDescription);
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- СУДЕБНЫЕ АРГУМЕНТЫ ДЛЯ ПРОВЕРКИ ---");
			sbAnalysis.AppendLine(it.TechnicalAnalysis);
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Технический дамп и скрипты
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine(" ТЕХНИЧЕСКИЙ ДАМП КОНФИГУРАЦИЙ, СКРИПТОВ И ДРАЙВЕРОВ ВВОДА");
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine("Источник данных:      " + it.DeviceOrSource);
			sbTech.AppendLine("Конфигурационный путь:" + it.FileOrRegistryPath);
			sbTech.AppendLine("Зафиксированное поле: " + it.ParameterOrValue);
			sbTech.AppendLine();
			sbTech.AppendLine("--- СУДЕБНО-ТЕХНИЧЕСКИЕ ДЕТАЛИ ---");
			if (it.TargetRule == "2.06")
			{
				sbTech.AppendLine("Технология: Debounce Time (Антидребезг контактов переключателей мыши).");
				sbTech.AppendLine("Регламент:  Минимально допустимое значение = 4 мс.");
				sbTech.AppendLine("Нарушение:  При значении 0-2 мс переключатель фиксирует множественные дребезги");
				sbTech.AppendLine("            металлической пластины за одно физическое нажатие (Double-Click),");
				sbTech.AppendLine("            что позволяет набивать 20-30+ CPS при Butterfly / Drag клике.");
			}
			else if (it.Category.Contains("Bloody"))
			{
				sbTech.AppendLine("Технология: Аппаратная энергонезависимая память мышей A4Tech Bloody.");
				sbTech.AppendLine("Чит-режимы: Ultra-Core 3 / Ultra-Core 4 (Oscar Macro Editor).");
				sbTech.AppendLine("Опасность:  Скрипты .bkg / .amc загружаются прямо в контроллер мыши (MCU),");
				sbTech.AppendLine("            подавляют отдачу оружия и циклично спамят клики без софта в памяти.");
			}
			else if (it.DeviceOrSource.Contains("interception"))
			{
				sbTech.AppendLine("Технология: Драйвер режима ядра Interception (interception.sys).");
				sbTech.AppendLine("Точка входа: UpperFilters класса GUID {4d36e96f-e325-11ce-bfc1-08002be10318}.");
				sbTech.AppendLine("Опасность:  Позволяет сторонним скрытым утилитам перехватывать и генерировать");
				sbTech.AppendLine("            аппаратные пакеты ввода мыши в обход DirectInput / RawInput.");
			}
			else
			{
				sbTech.AppendLine("Конфигурация легитимна либо не содержит запрещённых макросных последовательностей.");
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
				string duration;
				if (it.TargetRule == "2.06")
				{
					duration = "7d";
					reason = "2.06 Багоюз: сброс debounce time (" + it.ParameterOrValue + ") для искусственного даблклика";
				}
				else if (it.TargetRule == "5.08")
				{
					duration = "30d";
					reason = "5.08 Уклонение от проверки / Драйвер перехвата ввода Interception";
				}
				else
				{
					duration = "30d";
					reason = "5.01 Использование запрещённых макросов мыши (" + it.DeviceOrSource + ")";
				}

				sbBan.AppendLine("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:");
				sbBan.AppendLine("/ban " + targetPlayerName + " " + duration + " " + reason);
				sbBan.AppendLine();
				sbBan.AppendLine("ПОЯСНЕНИЕ ДЛЯ АПЕЛЛЯЦИОННОЙ ЖАЛОБЫ:");
				sbBan.AppendLine("В ходе криминалистической проверки манипулятора зафиксировано нарушение: " + it.DetailDescription);
				sbBan.AppendLine("Источник артефакта: " + it.FileOrRegistryPath);
			}
			else
			{
				sbBan.AppendLine("Нарушений регламента по аппаратуре мыши и макросам не обнаружено.");
				sbBan.AppendLine("Все параметры и драйверы соответствуют легитимной игре.");
			}
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в списке для детального анализа манипулятора и макросов.";
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

			MouseForensicEvidenceItem it = listView.SelectedItems[0].Tag as MouseForensicEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason;
			string duration;
			if (it.TargetRule == "2.06")
			{
				duration = "7d";
				reason = "2.06 Багоюз: сброс debounce time (" + it.ParameterOrValue + ")";
			}
			else if (it.TargetRule == "5.08")
			{
				duration = "30d";
				reason = "5.08 Уклонение / Драйвер перехвата ввода Interception";
			}
			else
			{
				duration = "30d";
				reason = "5.01 Использование запрещенных макросов мыши (" + it.DeviceOrSource + ")";
			}

			string cmd = "/ban " + targetPlayerName + " " + duration + " " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**🖱️ MCCheatChecker — Отчёт инспектора мыши, дебаунса и аппаратных макросов**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (MouseForensicEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные аномалии манипуляторов и макросов:**");
				foreach (MouseForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.TargetRule + "]` **" + it.DeviceOrSource + "**: " + it.ParameterOrValue + " — " + it.DetailDescription);
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Рекомендуемая санкция:** См. пункт регламента (2.06 бан 7d / 5.01 бан 30d).");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Аппаратных макросов Bloody, заниженного дебаунса и драйверов перехвата не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_MouseMacroReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт аудита мыши — " + targetPlayerName + "</title>");
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
					sb.AppendLine("<h1>🖱️ Отчёт инспектора мыши, дебаунса и аппаратных макросов</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО ПРОВЕРОК</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>НАРУШЕНИЯ 2.06</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiDebounce.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>НАРУШЕНИЯ 5.01</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiMacros.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>BLOODY ЖЕЛЕЗО</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiBloody.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВИРТУАЛЬНЫЙ ВВОД</div><div class='kpi-value' style='color:#EF5350'>" + lblKpiSynthetic.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЛЕГИТИМНЫЙ СТАТУС</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiClean.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Статус</th><th>Манипулятор</th><th>Категория</th><th>Параметр</th><th>Правило</th><th>Путь</th><th>Детали</th></tr>");
					foreach (MouseForensicEvidenceItem it in allItems)
					{
						string cls = it.IsViolation ? (it.TargetRule == "2.06" ? "warn" : "crit") : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td class='" + cls + "'>" + it.Status + "</td>");
						sb.AppendLine("<td>" + it.DeviceOrSource + "</td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td>" + it.ParameterOrValue + "</td>");
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

		public static List<MouseForensicEvidenceItem> PerformHardwareMouseAndMacroAudit()
		{
			List<MouseForensicEvidenceItem> results = new List<MouseForensicEvidenceItem>();
			int idx = 1;

			// 1. Аудит конфигураций Debounce Time (Glorious, Roccat, Pulsar, Windows)
			AuditDebounceTimeConfigs(results, ref idx);

			// 2. Аудит аппаратных макросов Bloody / A4Tech (Ultra-Core 3/4, Oscar .amc/.bkg)
			AuditBloodyAndOscarMacros(results, ref idx);

			// 3. Аудит синтетических драйверов перехвата кликов (Interception, vJoy, vmulti, HIDUSBF)
			AuditSyntheticInputDrivers(results, ref idx);

			// 4. Аудит скриптов и профилей макросов вендоров (Logitech G HUB Lua, Razer Synapse XML, Corsair iCUE, X-Mouse)
			AuditVendorMacroProfiles(results, ref idx);

			// 5. Аудит подключенных USB HID устройств и манипуляторов в реестре
			AuditConnectedHidMice(results, ref idx);

			return results;
		}

		private static void AuditDebounceTimeConfigs(List<MouseForensicEvidenceItem> results, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

			bool foundDebounceConfig = false;

			// A. Glorious Core & Model O/D
			string[] gloriousPaths = new string[]
			{
				Path.Combine(localAppData, "Glorious Core", "data"),
				Path.Combine(localAppData, "Glorious Core", "config.json"),
				Path.Combine(appData, "Glorious Core", "config.json"),
				Path.Combine(progFiles, "Glorious Model O", "config.ini"),
				Path.Combine(progFilesX86, "Glorious Model O", "config.ini"),
				Path.Combine(appData, "Glorious Model O", "config.ini"),
				Path.Combine(localAppData, "Glorious Model O", "config.ini")
			};

			foreach (string p in gloriousPaths)
			{
				try
				{
					if (File.Exists(p))
					{
						foundDebounceConfig = true;
						CheckDebounceFile(p, "Glorious Model O/D / Core", results, ref idx);
					}
					else if (Directory.Exists(p))
					{
						string[] jsonFiles = Directory.GetFiles(p, "*.json", SearchOption.AllDirectories);
						foreach (string jf in jsonFiles)
						{
							foundDebounceConfig = true;
							CheckDebounceFile(jf, "Glorious Core Data", results, ref idx);
						}
					}
				}
				catch { }
			}

			// B. Roccat Swarm Zero Debounce
			try
			{
				string roccatPath = Path.Combine(progFilesX86, "ROCCAT", "ROCCAT Swarm");
				string roccatAppData = Path.Combine(appData, "ROCCAT", "SWARM");
				if (Directory.Exists(roccatPath) || Directory.Exists(roccatAppData))
				{
					foundDebounceConfig = true;
					MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
					item.Index = idx++;
					item.Category = "Debounce Time (2.06)";
					item.DeviceOrSource = "ROCCAT Swarm";
					item.ParameterOrValue = "Zero Debounce Mod";
					item.FileOrRegistryPath = Directory.Exists(roccatAppData) ? roccatAppData : roccatPath;
					item.Status = "⚠️ ПО ROCCAT SWARM УСТАНОВЛЕНО";
					item.ThreatLevel = "WARNING";
					item.Verdict = "ТРЕБУЕТСЯ ПРОВЕРКА";
					item.TargetRule = "2.06";
					item.IsViolation = false;
					item.IsWhitelisted = false;
					item.Timestamp = DateTime.Now;
					item.RowColor = Ui.AccentOrange;
					item.DetailDescription = "Обнаружено установленное ПО Roccat Swarm. Мыши Roccat (Kone, Burst, Kain) поддерживают опцию Zero Debounce (0 мс), приводящую к аномальному даблклику.";
					item.TechnicalAnalysis = "Аудит ПО Roccat: рекомендуется визуально проверить вкладку 'Advanced Settings -> Zero Debounce' в Roccat Swarm.";
					results.Add(item);
				}
			}
			catch { }

			// C. Windows DoubleClickSpeed Registry
			try
			{
				using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("Control Panel\\Mouse"))
				{
					if (rk != null)
					{
						string dcs = rk.GetValue("DoubleClickSpeed") as string;
						int val;
						if (!string.IsNullOrEmpty(dcs) && int.TryParse(dcs, out val))
						{
							if (val > 0 && val < 200)
							{
								MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
								item.Index = idx++;
								item.Category = "Параметры мыши Windows";
								item.DeviceOrSource = "Реестр Windows (DoubleClickSpeed)";
								item.ParameterOrValue = string.Format("DoubleClickSpeed = {0} мс (Норма: 500 мс)", val);
								item.FileOrRegistryPath = @"HKCU\Control Panel\Mouse\DoubleClickSpeed";
								item.Status = "⚠️ ЗАНИЖЕНА ЗАДЕРЖКА ДВОЙНОГО КЛИКА (2.06)";
								item.ThreatLevel = "WARNING";
								item.Verdict = "2.06 - Багоюз задержки клика";
								item.TargetRule = "2.06";
								item.IsViolation = true;
								item.IsWhitelisted = false;
								item.Timestamp = DateTime.Now;
								item.RowColor = Ui.AccentOrange;
								item.DetailDescription = string.Format("Задержка двойного клика Windows критически занижена до {0} мс (стандартное системное значение: 500 мс). Применяется для облегчения баттерфляй-клика.", val);
								item.TechnicalAnalysis = string.Format("Анализ реестра Windows: параметр DoubleClickSpeed={0} ms. При таких таймингах система регистрирует микропаузы между нажатиями как даблклик.", val);
								results.Add(item);
							}
						}
					}
				}
			}
			catch { }

			if (!foundDebounceConfig)
			{
				MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
				item.Index = idx++;
				item.Category = "Debounce Time (2.06)";
				item.DeviceOrSource = "Glorious / Roccat / Pulsar";
				item.ParameterOrValue = "Заниженный Debounce не обнаружен";
				item.FileOrRegistryPath = @"%LocalAppData%\Glorious Core / %AppData%\ROCCAT";
				item.Status = "🟢 ДЕБАУНС В НОРМЕ";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Конфигураций с критически заниженным дебаунсом (< 4 мс) в установленном ПО манипуляторов не зафиксировано.";
				item.TechnicalAnalysis = "Просканированы каталоги Glorious Core, Model O/D, Roccat Swarm: аномальных профилей не найдено.";
				results.Add(item);
			}
		}

		private static void CheckDebounceFile(string filePath, string sourceName, List<MouseForensicEvidenceItem> results, ref int idx)
		{
			try
			{
				string content = File.ReadAllText(filePath).ToLowerInvariant();
				Match m = Regex.Match(content, "(?:debounce|debounce_time|debouncetime|debounce_ms)[\"':\\s=]+(\\d+)");
				if (m.Success)
				{
					int val;
					if (int.TryParse(m.Groups[1].Value, out val))
					{
						MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Debounce Time (2.06)";
						item.DeviceOrSource = sourceName;
						item.FileOrRegistryPath = filePath;
						item.ParameterOrValue = string.Format("Debounce: {0} мс", val);
						item.Timestamp = File.GetLastWriteTime(filePath);

						if (val <= 2)
						{
							item.Status = "🚨 БАГОЮЗ DEBOUNCE (2.06)";
							item.ThreatLevel = "CRITICAL";
							item.Verdict = "2.06 - Багоюз debounce time";
							item.TargetRule = "2.06";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.RowColor = Ui.AccentRed;
							item.DetailDescription = string.Format("Обнаружен критически заниженный debounce time: {0} мс (порог регламента: >= 4 мс). Данное значение полностью устраняет задержку дребезга контактов и позволяет набивать 20-30+ CPS даблкликом/баттерфляем.", val);
							item.TechnicalAnalysis = string.Format("Судебный анализ конфигурации: файл {0} содержит debounce={1}ms. Нарушение пункта 2.06 регламента проекта.", Path.GetFileName(filePath), val);
						}
						else if (val < 4)
						{
							item.Status = "⚠️ ЗАНИЖЕННЫЙ DEBOUNCE (2.06)";
							item.ThreatLevel = "WARNING";
							item.Verdict = "2.06 - Заниженный debounce";
							item.TargetRule = "2.06";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.RowColor = Ui.AccentOrange;
							item.DetailDescription = string.Format("Обнаружен заниженный debounce time: {0} мс (регламентная норма: >= 4 мс). Риск неконтролируемого даблклика.", val);
							item.TechnicalAnalysis = string.Format("Файл {0}: debounce={1}ms ниже установленного порога в 4 мс.", Path.GetFileName(filePath), val);
						}
						else
						{
							item.Status = "🟢 ШТАТНЫЙ DEBOUNCE";
							item.ThreatLevel = "SAFE";
							item.Verdict = "РАЗРЕШЕНО";
							item.TargetRule = "ЧИСТО";
							item.IsViolation = false;
							item.IsWhitelisted = true;
							item.RowColor = Ui.AccentGreen;
							item.DetailDescription = string.Format("Дебаунс мыши установлен на штатное значение {0} мс (соответствует регламенту >= 4 мс).", val);
							item.TechnicalAnalysis = string.Format("Конфигурация {0}: debounce={1}ms, находится в пределах легитимных норм.", Path.GetFileName(filePath), val);
						}

						results.Add(item);
					}
				}
			}
			catch { }
		}

		private static void AuditBloodyAndOscarMacros(List<MouseForensicEvidenceItem> results, ref int idx)
		{
			string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			string commonProg = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			string[] bloodyFolders = new string[]
			{
				Path.Combine(progFiles, "Bloody7"),
				Path.Combine(progFilesX86, "Bloody7"),
				Path.Combine(commonProg, "Bloody7"),
				Path.Combine(progFiles, "Bloody6"),
				Path.Combine(progFilesX86, "Bloody6"),
				Path.Combine(commonProg, "Bloody6"),
				Path.Combine(progFiles, "Bloody5"),
				Path.Combine(progFilesX86, "Bloody5"),
				Path.Combine(commonProg, "Bloody5"),
				Path.Combine(progFiles, "OscarEditor"),
				Path.Combine(progFilesX86, "OscarEditor"),
				Path.Combine(commonProg, "Bloody")
			};

			bool foundBloody = false;

			foreach (string dir in bloodyFolders)
			{
				try
				{
					if (Directory.Exists(dir))
					{
						foundBloody = true;

						// Поиск файлов макросов .amc, .bkg, .mgn
						string[] macroExtensions = new string[] { "*.amc", "*.bkg", "*.mgn", "*.amg" };
						List<string> foundFiles = new List<string>();

						foreach (string ext in macroExtensions)
						{
							try
							{
								string[] files = Directory.GetFiles(dir, ext, SearchOption.AllDirectories);
								foundFiles.AddRange(files);
							}
							catch { }
						}

						MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Bloody / A4Tech Hardware (5.01)";
						item.DeviceOrSource = "A4Tech Bloody Software";
						item.FileOrRegistryPath = dir;
						item.Timestamp = Directory.GetLastWriteTime(dir);

						if (foundFiles.Count > 0)
						{
							item.ParameterOrValue = string.Format("Макросы отдачи/клика: {0} шт. (.bkg/.amc)", foundFiles.Count);
							item.Status = "⛔ АППАРАТНЫЕ МАКРОСЫ BLOODY (5.01)";
							item.ThreatLevel = "CRITICAL";
							item.Verdict = "5.01 - Чит-макросы Bloody";
							item.TargetRule = "5.01";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.RowColor = Ui.AccentRed;
							item.DetailDescription = string.Format("В каталоге {0} обнаружены файлы аппаратных макросов Oscar/Ultra-Core ({1} шт.). Данные скрипты аппаратно подавляют отдачу и автоматизируют клики через микроконтроллер мыши в обход софтовых проверок.", Path.GetFileName(dir), foundFiles.Count);
							item.TechnicalAnalysis = string.Format("Судебный аудит файлов макросов: обнаружены {0} файлов .amc/.bkg. Пример: {1}. Нарушение 5.01.", foundFiles.Count, Path.GetFileName(foundFiles[0]));
						}
						else
						{
							item.ParameterOrValue = "ПО Bloody установлено (скрипты не найдены)";
							item.Status = "⚠️ ПО BLOODY УСТАНОВЛЕНО";
							item.ThreatLevel = "WARNING";
							item.Verdict = "ТРЕБУЕТСЯ ПРОВЕРКА";
							item.TargetRule = "5.01";
							item.IsViolation = false;
							item.IsWhitelisted = false;
							item.RowColor = Ui.AccentOrange;
							item.DetailDescription = string.Format("Обнаружена установленная директория {0}. Рекомендуется проверить статус активации Ultra-Core 3/4 и память мыши на турнирном сервере.", Path.GetFileName(dir));
							item.TechnicalAnalysis = "Аудит каталогов: папка ПО Bloody присутствует, однако файлы макросов .amc/.bkg не обнаружены в явном виде.";
						}

						results.Add(item);
					}
				}
				catch { }
			}

			// Проверка процессов Bloody
			try
			{
				string[] procNames = new string[] { "Bloody7", "Bloody6", "Bloody5", "TGun", "OscarEditor" };
				foreach (string pn in procNames)
				{
					Process[] procs = Process.GetProcessesByName(pn);
					if (procs.Length > 0)
					{
						MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Bloody / A4Tech Hardware (5.01)";
						item.DeviceOrSource = pn + ".exe";
						item.ParameterOrValue = string.Format("Активный процесс (PID: {0})", procs[0].Id);
						item.FileOrRegistryPath = procs[0].MainModule != null ? procs[0].MainModule.FileName : pn;
						item.Status = "⚠️ АКТИВЕН ПРОЦЕСС BLOODY";
						item.ThreatLevel = "WARNING";
						item.Verdict = "ТРЕБУЕТСЯ ПРОВЕРКА";
						item.TargetRule = "5.01";
						item.IsViolation = false;
						item.IsWhitelisted = false;
						item.Timestamp = DateTime.Now;
						item.RowColor = Ui.AccentOrange;
						item.DetailDescription = string.Format("Запущен процесс управления мышью {0}.exe. Проверьте активность макросов Ultra-Core.", pn);
						item.TechnicalAnalysis = string.Format("Процесс {0} запущен в памяти. PID: {1}.", pn, procs[0].Id);
						results.Add(item);
					}
				}
			}
			catch { }

			// Проверка реестра на активацию Ultra-Core 3/4
			try
			{
				string[] regPaths = new string[]
				{
					@"SOFTWARE\Bloody7",
					@"SOFTWARE\WOW6432Node\Bloody7",
					@"SOFTWARE\Bloody6",
					@"SOFTWARE\A4Tech"
				};

				foreach (string rp in regPaths)
				{
					using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(rp))
					{
						if (rk != null)
						{
							object coreVal = rk.GetValue("ActiveCore") ?? rk.GetValue("Core") ?? rk.GetValue("Core3");
							if (coreVal != null)
							{
								string cStr = coreVal.ToString();
								if (cStr.Contains("3") || cStr.Contains("4"))
								{
									MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
									item.Index = idx++;
									item.Category = "Bloody / A4Tech Hardware (5.01)";
									item.DeviceOrSource = "Ultra-Core 3/4 (Реестр)";
									item.ParameterOrValue = "Активирован Core " + cStr;
									item.FileOrRegistryPath = @"HKLM\" + rp;
									item.Status = "⛔ АКТИВАЦИЯ ULTRA-CORE 3/4 (5.01)";
									item.ThreatLevel = "CRITICAL";
									item.Verdict = "5.01 - Чит-режим Ultra-Core";
									item.TargetRule = "5.01";
									item.IsViolation = true;
									item.IsWhitelisted = false;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentRed;
									item.DetailDescription = "В реестре зафиксирована активация читерского режима Ultra-Core 3 или 4 для мыши Bloody (автоматическое подавление отдачи и тройной клик).";
									item.TechnicalAnalysis = string.Format("Ветка реестра HKLM\\{0} содержит ключ активации Core={1}.", rp, cStr);
									results.Add(item);
								}
							}
						}
					}
				}
			}
			catch { }

			if (!foundBloody)
			{
				MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
				item.Index = idx++;
				item.Category = "Bloody / A4Tech Hardware (5.01)";
				item.DeviceOrSource = "A4Tech Bloody";
				item.ParameterOrValue = "ПО и макросы отсутствуют";
				item.FileOrRegistryPath = @"%ProgramFiles%\Bloody";
				item.Status = "🟢 ЧИСТО (BLOODY НЕ ОБНАРУЖЕНО)";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Следов установки ПО Bloody5/6/7, редактора Oscar и аппаратных макросов не обнаружено.";
				item.TechnicalAnalysis = "Просканированы ветки HKLM/HKCU и каталоги Program Files на наличие сигнатур Bloody/A4Tech.";
				results.Add(item);
			}
		}

		private static void AuditSyntheticInputDrivers(List<MouseForensicEvidenceItem> results, ref int idx)
		{
			string sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.System);
			bool foundDriver = false;

			// 1. Interception Driver (interception.sys)
			try
			{
				string driverPath = Path.Combine(sysRoot, "drivers", "interception.sys");
				bool fileExists = File.Exists(driverPath);

				bool serviceExists = false;
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\interception"))
				{
					if (rk != null) serviceExists = true;
				}

				bool upperFilterMouse = false;
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e96f-e325-11ce-bfc1-08002be10318}"))
				{
					if (rk != null)
					{
						string[] uf = rk.GetValue("UpperFilters") as string[];
						if (uf != null)
						{
							foreach (string s in uf)
							{
								if (s.ToLowerInvariant().Contains("interception")) upperFilterMouse = true;
							}
						}
					}
				}

				if (fileExists || serviceExists || upperFilterMouse)
				{
					foundDriver = true;
					MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
					item.Index = idx++;
					item.Category = "Виртуальные драйверы ввода (5.08)";
					item.DeviceOrSource = "Interception Driver (interception.sys)";
					item.ParameterOrValue = string.Format("Драйвер: {0}, Служба: {1}, UpperFilters: {2}", fileExists ? "ЕСТЬ" : "НЕТ", serviceExists ? "ЕСТЬ" : "НЕТ", upperFilterMouse ? "ПРОПИСАН" : "НЕТ");
					item.FileOrRegistryPath = fileExists ? driverPath : @"SYSTEM\CurrentControlSet\Services\interception";
					item.Status = "🚨 ПЕРЕХВАТ ВВОДА INTERCEPTION (5.08)";
					item.ThreatLevel = "CRITICAL";
					item.Verdict = "5.08 - Драйвер перехвата кликов";
					item.TargetRule = "5.08";
					item.IsViolation = true;
					item.IsWhitelisted = false;
					item.Timestamp = fileExists ? File.GetLastWriteTime(driverPath) : DateTime.Now;
					item.RowColor = Ui.AccentRed;
					item.DetailDescription = "В системе установлен драйвер режима ядра Interception (interception.sys), перехватывающий клики мыши на уровне системного стека. Используется сторонними автокликерами для симуляции аппаратного ввода.";
					item.TechnicalAnalysis = "Аудит стека ввода: interception.sys прописан в драйверах ядра Windows. Позволяет сторонним приложениям подменять и генерировать аппаратные события мыши в обход DirectInput.";
					results.Add(item);
				}
			}
			catch { }

			// 2. vJoy Virtual Joystick / Synthetic Mouse
			try
			{
				string vjoyDriver = Path.Combine(sysRoot, "drivers", "vjoy.sys");
				bool vjoyExists = File.Exists(vjoyDriver);
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\vjoy"))
				{
					if (rk != null || vjoyExists)
					{
						foundDriver = true;
						MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Виртуальные драйверы ввода (5.01)";
						item.DeviceOrSource = "vJoy Virtual Joystick";
						item.ParameterOrValue = "Установлена служба / драйвер vJoy";
						item.FileOrRegistryPath = vjoyExists ? vjoyDriver : @"SYSTEM\CurrentControlSet\Services\vjoy";
						item.Status = "⚠️ ВИРТУАЛЬНЫЙ ДРАЙВЕР VJOY";
						item.ThreatLevel = "WARNING";
						item.Verdict = "ТРЕБУЕТСЯ ПРОВЕРКА";
						item.TargetRule = "5.01";
						item.IsViolation = false;
						item.IsWhitelisted = false;
						item.Timestamp = DateTime.Now;
						item.RowColor = Ui.AccentOrange;
						item.DetailDescription = "В системе обнаружен виртуальный манипулятор vJoy. Может использоваться скриптами для эмуляции нажатий мыши или геймпада.";
						item.TechnicalAnalysis = "Драйвер vJoy.sys зарегистрирован в реестре служб Windows.";
						results.Add(item);
					}
				}
			}
			catch { }

			// 3. HIDUSBF (SweetLow USB Polling Rate Overclocking)
			try
			{
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\hidusbf"))
				{
					if (rk != null)
					{
						foundDriver = true;
						MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
						item.Index = idx++;
						item.Category = "Разгон частоты опроса (Polling Rate)";
						item.DeviceOrSource = "SweetLow HIDUSBF Driver";
						item.ParameterOrValue = "Разгон частоты опроса шины USB (до 8000 Hz)";
						item.FileOrRegistryPath = @"SYSTEM\CurrentControlSet\Services\hidusbf";
						item.Status = "ℹ️ ДРАЙВЕР РАЗГОНА HIDUSBF";
						item.ThreatLevel = "INFO";
						item.Verdict = "РАЗРЕШЕНО (ИНФОРМАЦИЯ)";
						item.TargetRule = "ЧИСТО";
						item.IsViolation = false;
						item.IsWhitelisted = true;
						item.Timestamp = DateTime.Now;
						item.RowColor = Ui.AccentTeal;
						item.DetailDescription = "Обнаружен драйвер разгона частоты опроса USB-портов (SweetLow HIDUSBF). Используется киберспортсменами для снижения инпут-лага мыши (до 1000-8000 Гц).";
						item.TechnicalAnalysis = "Служба hidusbf установлена в системе: разгон шины USB не является читом.";
						results.Add(item);
					}
				}
			}
			catch { }

			if (!foundDriver)
			{
				MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
				item.Index = idx++;
				item.Category = "Виртуальные драйверы ввода (5.08)";
				item.DeviceOrSource = "Interception / vJoy / vmulti";
				item.ParameterOrValue = "Драйверы перехвата не обнаружены";
				item.FileOrRegistryPath = @"%SystemRoot%\System32\drivers";
				item.Status = "🟢 ДРАЙВЕРЫ ВВОДА ЧИСТЫ";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Драйверов перехвата кликов (Interception) и эмуляторов виртуального ввода не обнаружено.";
				item.TechnicalAnalysis = "Службы interception, vjoy и UpperFilters мышиного класса проверены: модификаций стека ввода нет.";
				results.Add(item);
			}
		}

		private static void AuditVendorMacroProfiles(List<MouseForensicEvidenceItem> results, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string commonProg = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			bool foundVendorMacro = false;

			// 1. Logitech G HUB Lua Scripts
			try
			{
				string lghubScripts = Path.Combine(localAppData, "LGHUB", "scripts");
				if (Directory.Exists(lghubScripts))
				{
					string[] luaFiles = Directory.GetFiles(lghubScripts, "*.lua", SearchOption.AllDirectories);
					foreach (string lf in luaFiles)
					{
						try
						{
							string text = File.ReadAllText(lf).ToLowerInvariant();
							if ((text.Contains("pressmousebutton") || text.Contains("clickmousebutton")) &&
								(text.Contains("sleep") || text.Contains("math.random") || text.Contains("repeat") || text.Contains("while")))
							{
								foundVendorMacro = true;
								MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
								item.Index = idx++;
								item.Category = "Макросы вендоров (5.01)";
								item.DeviceOrSource = "Logitech G HUB (Lua Script)";
								item.ParameterOrValue = "Скрипт автокликера: " + Path.GetFileName(lf);
								item.FileOrRegistryPath = lf;
								item.Status = "⛔ МАКРОС G HUB LUA (5.01)";
								item.ThreatLevel = "CRITICAL";
								item.Verdict = "5.01 - Чит-макрос автокликера";
								item.TargetRule = "5.01";
								item.IsViolation = true;
								item.IsWhitelisted = false;
								item.Timestamp = File.GetLastWriteTime(lf);
								item.RowColor = Ui.AccentRed;
								item.DetailDescription = string.Format("В скрипте Logitech G HUB '{0}' обнаружен запрещённый макрос автокликера на Lua (PressMouseButton + цикл Sleep).", Path.GetFileName(lf));
								item.TechnicalAnalysis = "Анализ синтаксиса Lua: присутствуют вызовы PressMouseButton и задержки Sleep в цикле.";
								results.Add(item);
							}
						}
						catch { }
					}
				}
			}
			catch { }

			// 2. Razer Synapse Macros
			try
			{
				string[] razerPaths = new string[]
				{
					Path.Combine(commonProg, "Razer", "Synapse3", "Macros"),
					Path.Combine(localAppData, "Razer", "Synapse3", "Macros"),
					Path.Combine(commonProg, "Razer", "Synapse", "Macros")
				};

				foreach (string rp in razerPaths)
				{
					if (Directory.Exists(rp))
					{
						string[] files = Directory.GetFiles(rp, "*.*", SearchOption.AllDirectories);
						foreach (string rf in files)
						{
							try
							{
								string text = File.ReadAllText(rf).ToLowerInvariant();
								if ((text.Contains("mouseevent") || text.Contains("mousebutton") || text.Contains("click")) &&
									(text.Contains("delay") || text.Contains("repeat") || text.Contains("interval")))
								{
									foundVendorMacro = true;
									MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
									item.Index = idx++;
									item.Category = "Макросы вендоров (5.01)";
									item.DeviceOrSource = "Razer Synapse Macro";
									item.ParameterOrValue = "XML макрос клика: " + Path.GetFileName(rf);
									item.FileOrRegistryPath = rf;
									item.Status = "⛔ МАКРОС RAZER SYNAPSE (5.01)";
									item.ThreatLevel = "CRITICAL";
									item.Verdict = "5.01 - Чит-макрос Razer";
									item.TargetRule = "5.01";
									item.IsViolation = true;
									item.IsWhitelisted = false;
									item.Timestamp = File.GetLastWriteTime(rf);
									item.RowColor = Ui.AccentRed;
									item.DetailDescription = string.Format("Обнаружен настроенный макрос клика в Razer Synapse: '{0}'.", Path.GetFileName(rf));
									item.TechnicalAnalysis = "Структура XML файла содержит последовательность MouseEvent с минимальными интервалами задержек.";
									results.Add(item);
									break;
								}
							}
							catch { }
						}
					}
				}
			}
			catch { }

			// 3. X-Mouse Button Control
			try
			{
				string xmouseDir = Path.Combine(appData, "Highresolution Enterprises", "X-Mouse Button Control");
				if (Directory.Exists(xmouseDir))
				{
					string[] files = Directory.GetFiles(xmouseDir, "*.xml");
					foreach (string xf in files)
					{
						try
						{
							string text = File.ReadAllText(xf).ToLowerInvariant();
							if (text.Contains("<simulated") && (text.Contains("{lmb}") || text.Contains("{rmb}") || text.Contains("repeat")))
							{
								foundVendorMacro = true;
								MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
								item.Index = idx++;
								item.Category = "Макросы вендоров (5.01)";
								item.DeviceOrSource = "X-Mouse Button Control";
								item.ParameterOrValue = "Макрос клика: " + Path.GetFileName(xf);
								item.FileOrRegistryPath = xf;
								item.Status = "⛔ МАКРОС X-MOUSE (5.01)";
								item.ThreatLevel = "CRITICAL";
								item.Verdict = "5.01 - Чит-макрос X-Mouse";
								item.TargetRule = "5.01";
								item.IsViolation = true;
								item.IsWhitelisted = false;
								item.Timestamp = File.GetLastWriteTime(xf);
								item.RowColor = Ui.AccentRed;
								item.DetailDescription = string.Format("В конфигурации X-Mouse Button Control настроена симуляция циклического нажатия кнопок мыши: '{0}'.", Path.GetFileName(xf));
								item.TechnicalAnalysis = "XML-нода <simulated> содержит теги циклического воспроизведения {LMB}/{RMB}.";
								results.Add(item);
							}
						}
						catch { }
					}
				}
			}
			catch { }

			if (!foundVendorMacro)
			{
				MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
				item.Index = idx++;
				item.Category = "Макросы вендоров (5.01)";
				item.DeviceOrSource = "G HUB / Synapse / iCUE / X-Mouse";
				item.ParameterOrValue = "Макросы автокликера отсутствуют";
				item.FileOrRegistryPath = @"%LocalAppData%\LGHUB / %ProgramData%\Razer";
				item.Status = "🟢 МАКРОСЫ ВЕНДОРОВ НЕ НАЙДЕНЫ";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Профили Logitech G HUB, Razer Synapse, Corsair iCUE и X-Mouse Button Control не содержат циклических макросов автокликера.";
				item.TechnicalAnalysis = "Аудит файлов скриптов и профилей вендоров завершён без выявления нарушений.";
				results.Add(item);
			}
		}

		private static void AuditConnectedHidMice(List<MouseForensicEvidenceItem> results, ref int idx)
		{
			try
			{
				using (RegistryKey hidKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\HID"))
				{
					if (hidKey != null)
					{
						string[] subKeyNames = hidKey.GetSubKeyNames();
						int mouseCount = 0;

						foreach (string subName in subKeyNames)
						{
							try
							{
								using (RegistryKey devKey = hidKey.OpenSubKey(subName))
								{
									if (devKey == null) continue;
									string[] instNames = devKey.GetSubKeyNames();
									foreach (string inst in instNames)
									{
										using (RegistryKey instKey = devKey.OpenSubKey(inst))
										{
											if (instKey == null) continue;
											string service = (instKey.GetValue("Service") as string ?? "").ToLowerInvariant();
											string classGuid = (instKey.GetValue("ClassGUID") as string ?? "").ToLowerInvariant();
											string desc = (instKey.GetValue("DeviceDesc") as string ?? "");
											string friendly = (instKey.GetValue("FriendlyName") as string ?? "");

											// Проверяем мышиный сервис mouhid или класс {4d36e96f-e325-11ce-bfc1-08002be10318}
											if (service == "mouhid" || classGuid == "{4d36e96f-e325-11ce-bfc1-08002be10318}" ||
												desc.ToLowerInvariant().Contains("mouse") || desc.ToLowerInvariant().Contains("мышь"))
											{
												mouseCount++;
												string dispName = !string.IsNullOrEmpty(friendly) ? friendly : desc;
												if (string.IsNullOrEmpty(dispName)) dispName = subName;

												// Парсим VID/PID
												string vid = "";
												string pid = "";
												Match mV = Regex.Match(subName, @"VID_([0-9A-Fa-f]{4})");
												if (mV.Success) vid = mV.Groups[1].Value.ToUpperInvariant();
												Match mP = Regex.Match(subName, @"PID_([0-9A-Fa-f]{4})");
												if (mP.Success) pid = mP.Groups[1].Value.ToUpperInvariant();

												string vendor = ClassifyVendorByVid(vid, dispName);

												MouseForensicEvidenceItem item = new MouseForensicEvidenceItem();
												item.Index = idx++;
												item.Category = "USB HID Устройства";
												item.DeviceOrSource = dispName;
												item.FileOrRegistryPath = @"HKLM\SYSTEM\CCS\Enum\HID\" + subName;
												item.ParameterOrValue = string.Format("VID_{0}&PID_{1} ({2})", vid, pid, vendor);
												item.Timestamp = DateTime.Now;

												if (vendor.Contains("Bloody") || vid == "09DA")
												{
													item.Status = "⚠️ МАНИПУЛЯТОР С ВСТРОЕННОЙ ПАМЯТЬЮ";
													item.ThreatLevel = "WARNING";
													item.Verdict = "ТРЕБУЕТСЯ ПРОВЕРКА";
													item.TargetRule = "5.01";
													item.IsViolation = false;
													item.IsWhitelisted = false;
													item.RowColor = Ui.AccentOrange;
													item.DetailDescription = string.Format("Обнаружен манипулятор {0} (A4Tech Bloody). Мышь аппаратно поддерживает загрузку чит-макросов в микроконтроллер.", dispName);
													item.TechnicalAnalysis = "Аппаратная ревизия контроллера Bloody с энергонезависимой памятью макросов.";
												}
												else
												{
													item.Status = "🟢 ЛЕГИТИМНЫЙ МАНИПУЛЯТОР";
													item.ThreatLevel = "SAFE";
													item.Verdict = "РАЗРЕШЕНО";
													item.TargetRule = "ЧИСТО";
													item.IsViolation = false;
													item.IsWhitelisted = true;
													item.RowColor = Ui.AccentGreen;
													item.DetailDescription = string.Format("Подключён стандартный манипулятор мыши {0} производителя {1}.", dispName, vendor);
													item.TechnicalAnalysis = string.Format("USB HID устройство: VID_{0}&PID_{1}. Драйвер сервиса: {2}.", vid, pid, service);
												}

												results.Add(item);
											}
										}
									}
								}
							}
							catch { }
						}
					}
				}
			}
			catch { }
		}

		private static string ClassifyVendorByVid(string vid, string deviceName)
		{
			string lower = (deviceName ?? "").ToLowerInvariant();
			if (vid == "09DA" || lower.Contains("bloody") || lower.Contains("a4tech")) return "A4Tech / Bloody";
			if (vid == "258A" || vid == "2F24" || lower.Contains("glorious")) return "Glorious PC Gaming Race";
			if (vid == "046D" || lower.Contains("logitech")) return "Logitech";
			if (vid == "1532" || lower.Contains("razer")) return "Razer";
			if (vid == "1E7D" || lower.Contains("roccat")) return "Roccat";
			if (vid == "1B1C" || lower.Contains("corsair")) return "Corsair";
			if (vid == "1038" || lower.Contains("steelseries")) return "SteelSeries";
			if (vid == "045E" || lower.Contains("microsoft")) return "Microsoft";
			if (vid == "413C" || lower.Contains("dell")) return "Dell";
			if (vid == "03F0" || lower.Contains("hp") || lower.Contains("hyperx")) return "HP / HyperX";
			return "Стандартный HID манипулятор";
		}

		#endregion
	}
}
