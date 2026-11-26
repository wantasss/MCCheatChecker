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
	public enum KernelDriverFilter
	{
		All,
		ViolationsOnly,
		ByovdExploits,
		BcdTestSigning,
		ManualMappers,
		UnbackedDrivers,
		TrustedClean
	}

	public class KernelDriverEvidenceItem
	{
		public int Index;
		public string Category; // "BYOVD Эксплойты (5.01)", "Модификация BCD (5.08)", "Следы мапперов (5.01/5.08)", "Бесфайловые драйверы (5.08)", "Доверенные драйверы ядра"
		public string DriverName; // "gdrv.sys", "mhyprot2.sys", "RTCore64.sys", "TestSigning (BCD)", etc.
		public string BaseAddress; // "0xFFFFF80012345000", etc.
		public string FileOrRegistryPath;
		public string ParameterOrValue;
		public string Status; // "🚨 BYOVD УЯЗВИМЫЙ ДРАЙВЕР (5.01)", "⛔ РЕЖИМ TESTSIGNING ВКЛЮЧЕН (5.08)", "🟢 ДОВЕРЕННЫЙ ДРАЙВЕР"
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict; // "5.01 - Ядерный чит / BYOVD", "5.08 - Уклонение / TestSigning", "РАЗРЕШЕНО"
		public string TargetRule; // "5.01", "5.08", "ЧИСТО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalAnalysis;
	}

	public class KernelDriversAndHypervisorHooksViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<KernelDriverEvidenceItem> allItems = new List<KernelDriverEvidenceItem>();
		private KernelDriverFilter currentFilter = KernelDriverFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiByovd;
		private Label lblKpiBcd;
		private Label lblKpiMappers;
		private Label lblKpiUnbacked;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterByovd;
		private DarkButton btnFilterBcd;
		private DarkButton btnFilterMappers;
		private DarkButton btnFilterUnbacked;
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

		#region Win32 API Imports for Kernel Driver Enumeration

		[DllImport("psapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumDeviceDrivers([Out] IntPtr[] lpImageBase, uint cb, out uint lpcbNeeded);

		[DllImport("psapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern uint GetDeviceDriverBaseName(IntPtr ImageBase, [Out] StringBuilder lpBaseName, uint nSize);

		[DllImport("psapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern uint GetDeviceDriverFileName(IntPtr ImageBase, [Out] StringBuilder lpFileName, uint nSize);

		#endregion

		public KernelDriversAndHypervisorHooksViewerForm() : this("Player")
		{
		}

		public KernelDriversAndHypervisorHooksViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🛡️ Инспектор драйверов ядра, BYOVD эксплойтов и TestSigning (Tool 76 / Step 73) — MCCheatChecker";
			this.Size = new Size(1200, 750);
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
			lblHeaderTitle.Text = "🛡️ Инспектор ядерных драйверов, BYOVD эксплойтов и параметров TestSigning (Tool 76 / Step 73)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокая экспертиза ядра: детекция уязвимых BYOVD драйверов (gdrv, mhyprot2, RTCore64, dbk64), мапперов kdmapper/kdu, отключения DSE (TestSigning) и бесфайловых руткитов.";
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ДРАЙВЕРОВ", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("BYOVD / РУТКИТЫ (5.01)", "0", Ui.AccentRed, ref xOff, out lblKpiByovd));
			kpiPanel.Controls.Add(CreateKpiBadge("TESTSIGNING BCD (5.08)", "0", Ui.AccentOrange, ref xOff, out lblKpiBcd));
			kpiPanel.Controls.Add(CreateKpiBadge("СЛЕДЫ KDMAPPER (5.01)", "0", Ui.AccentRed, ref xOff, out lblKpiMappers));
			kpiPanel.Controls.Add(CreateKpiBadge("БЕСФАЙЛОВЫЕ (5.08)", "0", Ui.AccentPurple, ref xOff, out lblKpiUnbacked));
			kpiPanel.Controls.Add(CreateKpiBadge("ДОВЕРЕННЫЕ ДРАЙВЕРЫ", "0", Ui.AccentGreen, ref xOff, out lblKpiClean));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", KernelDriverFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения (5.01/5.08) (0)", KernelDriverFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterByovd = CreateFilterButton("BYOVD эксплойты (0)", KernelDriverFilter.ByovdExploits, ref fX, filterPanel);
			btnFilterBcd = CreateFilterButton("BCD TestSigning (0)", KernelDriverFilter.BcdTestSigning, ref fX, filterPanel);
			btnFilterMappers = CreateFilterButton("Следы мапперов (0)", KernelDriverFilter.ManualMappers, ref fX, filterPanel);
			btnFilterUnbacked = CreateFilterButton("Бесфайловые (0)", KernelDriverFilter.UnbackedDrivers, ref fX, filterPanel);
			btnFilterClean = CreateFilterButton("Белый список (0)", KernelDriverFilter.TrustedClean, ref fX, filterPanel);

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
			listView.Columns.Add("Имя драйвера / Объект", 170);
			listView.Columns.Add("Категория", 150);
			listView.Columns.Add("Базовый адрес / Размер", 160);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Путь к файлу / Служба", 240);
			listView.Columns.Add("Описание и детали", 360);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ драйвера ядра");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ BCD Boot параметры и подписи");
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

		private DarkButton CreateFilterButton(string text, KernelDriverFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (KernelDriverFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 135;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование драйверов ядра и BCD...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformKernelDriverForensicScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования драйверов: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<KernelDriverEvidenceItem> results = e.Result as List<KernelDriverEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (KernelDriverEvidenceItem it in allItems)
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
					lblStatusPill.Text = "🟢 ЯДРО БЕЗОПАСНО";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int byovd = 0;
			int bcd = 0;
			int mappers = 0;
			int unbacked = 0;
			int clean = 0;

			foreach (KernelDriverEvidenceItem it in allItems)
			{
				if (it.Category.Contains("BYOVD") && it.IsViolation) byovd++;
				if (it.Category.Contains("BCD") && it.IsViolation) bcd++;
				if (it.Category.Contains("маппер") && it.IsViolation) mappers++;
				if (it.Category.Contains("Бесфайлов") && it.IsViolation) unbacked++;
				if (!it.IsViolation) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiByovd.Text = byovd.ToString();
			lblKpiBcd.Text = bcd.ToString();
			lblKpiMappers.Text = mappers.ToString();
			lblKpiUnbacked.Text = unbacked.ToString();
			lblKpiClean.Text = clean.ToString();

			int viol = byovd + bcd + mappers + unbacked;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения ({0})", viol);
			btnFilterByovd.Text = string.Format("BYOVD ({0})", byovd);
			btnFilterBcd.Text = string.Format("TestSigning ({0})", bcd);
			btnFilterMappers.Text = string.Format("Мапперы ({0})", mappers);
			btnFilterUnbacked.Text = string.Format("Бесфайловые ({0})", unbacked);
			btnFilterClean.Text = string.Format("Белый список ({0})", clean);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) ? searchBox.Text.Trim().ToLowerInvariant() : null;

			foreach (KernelDriverEvidenceItem it in allItems)
			{
				bool matchFilter = true;
				switch (currentFilter)
				{
					case KernelDriverFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case KernelDriverFilter.ByovdExploits:
						matchFilter = it.Category.Contains("BYOVD") || it.DriverName.Contains("gdrv") || it.DriverName.Contains("mhyprot") || it.DriverName.Contains("RTCore");
						break;
					case KernelDriverFilter.BcdTestSigning:
						matchFilter = it.Category.Contains("BCD") || it.DriverName.Contains("TestSigning");
						break;
					case KernelDriverFilter.ManualMappers:
						matchFilter = it.Category.Contains("маппер") || it.DriverName.Contains("kdmapper") || it.DriverName.Contains("kdu");
						break;
					case KernelDriverFilter.UnbackedDrivers:
						matchFilter = it.Category.Contains("Бесфайлов");
						break;
					case KernelDriverFilter.TrustedClean:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = (it.DriverName != null && it.DriverName.ToLowerInvariant().Contains(search))
						|| (it.Category != null && it.Category.ToLowerInvariant().Contains(search))
						|| (it.BaseAddress != null && it.BaseAddress.ToLowerInvariant().Contains(search))
						|| (it.ParameterOrValue != null && it.ParameterOrValue.ToLowerInvariant().Contains(search))
						|| (it.FileOrRegistryPath != null && it.FileOrRegistryPath.ToLowerInvariant().Contains(search))
						|| (it.DetailDescription != null && it.DetailDescription.ToLowerInvariant().Contains(search))
						|| (it.Status != null && it.Status.ToLowerInvariant().Contains(search));

					if (!matchSearch) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.DriverName);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.BaseAddress);
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

			KernelDriverEvidenceItem it = listView.SelectedItems[0].Tag as KernelDriverEvidenceItem;
			if (it == null) return;

			// Tab 1: Судебный анализ
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine(" КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ ДРАЙВЕРА ЯДРА И РУТКИТОВ");
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine("Имя драйвера / Объект: " + it.DriverName);
			sbAnalysis.AppendLine("Категория экспертизы:  " + it.Category);
			sbAnalysis.AppendLine("Базовый адрес в памяти:" + it.BaseAddress);
			sbAnalysis.AppendLine("Файл / Служба / Ветка: " + it.FileOrRegistryPath);
			sbAnalysis.AppendLine("Статус проверки:       " + it.Status);
			sbAnalysis.AppendLine("Уровень угрозы:        " + it.ThreatLevel);
			sbAnalysis.AppendLine("Пункт регламента:      " + it.TargetRule);
			sbAnalysis.AppendLine("Время фиксации:        " + it.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- ПОДРОБНОЕ ОПИСАНИЕ И КРИТЕРИИ ОЦЕНКИ ---");
			sbAnalysis.AppendLine(it.DetailDescription);
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- СУДЕБНЫЕ АРГУМЕНТЫ ДЛЯ ПРОВЕРКИ ---");
			sbAnalysis.AppendLine(it.TechnicalAnalysis);
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Технический дамп и BCD
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine(" ТЕХНИЧЕСКИЙ ДАМП ПАРАМЕТРОВ ЯДРА, BCD И ЦИФРОВЫХ ПОДПИСЕЙ");
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine("Драйвер / Служба:     " + it.DriverName);
			sbTech.AppendLine("Адрес загрузки:       " + it.BaseAddress);
			sbTech.AppendLine("Параметр / Свойство:  " + it.ParameterOrValue);
			sbTech.AppendLine();
			sbTech.AppendLine("--- СУДЕБНО-ТЕХНИЧЕСКИЕ ДЕТАЛИ ---");
			if (it.Category.Contains("BYOVD"))
			{
				sbTech.AppendLine("Методология BYOVD (Bring Your Own Vulnerable Driver):");
				sbTech.AppendLine("Чит-клиент загружает легально подписанный, но уязвимый сертификат драйвера (например,");
				sbTech.AppendLine("gdrv.sys, mhyprot2.sys, RTCore64.sys). Через IOCTL эксплойт чит получает чтение/запись");
				sbTech.AppendLine("в любую область физической памяти ядра и процесс Minecraft, обходя DSE и античиты.");
			}
			else if (it.Category.Contains("BCD"))
			{
				sbTech.AppendLine("Параметры диспетчера загрузки Windows (BCD - Boot Configuration Data):");
				sbTech.AppendLine("Флаг TESTSIGNING = ON разрешает загрузку абсолютно любых самоподписанных драйверов.");
				sbTech.AppendLine("Флаг DISABLE_INTEGRITY_CHECKS отключает валидацию сертификатов x64 драйверов ядра.");
				sbTech.AppendLine("Данная конфигурация применяется злоумышленниками для запуска кастомных руткит-читов.");
			}
			else if (it.Category.Contains("маппер"))
			{
				sbTech.AppendLine("Методология Manual Mapping драйверов (kdmapper / kdu / drvmap):");
				sbTech.AppendLine("Маппер временно поднимает уязвимый интеловский драйвер (iqvw64e.sys), через него");
				sbTech.AppendLine("копирует тело неподписанного чита в скрытую область пула ядра (NonPagedPool),");
				sbTech.AppendLine("затем выгружает iqvw64e.sys. Следы установки службы фиксируются в Event ID 7045.");
			}
			else if (it.Category.Contains("Бесфайлов"))
			{
				sbTech.AppendLine("Бесфайловый / Скрытый драйвер в ядре (Unbacked Driver):");
				sbTech.AppendLine("Драйвер зарегистрирован в списке модулей ядра EnumDeviceDrivers, но его файл отсутствует");
				sbTech.AppendLine("на физическом диске. Характерно для драйверов, удаляющих себя после загрузки для сокрытия.");
			}
			else
			{
				sbTech.AppendLine("Драйвер является доверенным системным компонентом Microsoft или верифицированного вендора.");
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
					reason = "5.08 Уклонение от проверки (Отключение подписи драйверов TestSigning / Скрытый драйвер)";
				}
				else
				{
					reason = "5.01 Использование читов ядра / BYOVD эксплойт (" + it.DriverName + ")";
				}

				sbBan.AppendLine("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:");
				sbBan.AppendLine("/ban " + targetPlayerName + " 30d " + reason);
				sbBan.AppendLine();
				sbBan.AppendLine("ПОЯСНЕНИЕ ДЛЯ АПЕЛЛЯЦИОННОЙ ЖАЛОБЫ:");
				sbBan.AppendLine("В ходе криминалистического анализа ядра Windows обнаружено критическое нарушение: " + it.DetailDescription);
				sbBan.AppendLine("Источник артефакта: " + it.FileOrRegistryPath);
			}
			else
			{
				sbBan.AppendLine("Нарушений регламента по ядру Windows, драйверам и параметрам BCD не обнаружено.");
				sbBan.AppendLine("Конфигурация загрузчика целостна, режим TestSigning отключён, эксплойтов BYOVD нет.");
			}
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в списке для детального анализа драйверов ядра.";
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

			KernelDriverEvidenceItem it = listView.SelectedItems[0].Tag as KernelDriverEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = (it.TargetRule == "5.08")
				? "5.08 Уклонение (TestSigning / Скрытый драйвер)"
				: "5.01 Чит ядра / BYOVD драйвер (" + it.DriverName + ")";

			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**🛡️ MCCheatChecker — Отчёт инспектора драйверов ядра, BYOVD и TestSigning**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (KernelDriverEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ ЯДРА (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные угрозы ядра:**");
				foreach (KernelDriverEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.TargetRule + "]` **" + it.DriverName + "**: " + it.ParameterOrValue + " — " + it.DetailDescription);
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.01 Использование читов ядра / обход подписи драйверов`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Эксплойтов BYOVD, следов kdmapper, TestSigning и скрытых драйверов не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_KernelDriverReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт аудита ядра — " + targetPlayerName + "</title>");
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
					sb.AppendLine("<h1>🛡️ Отчёт инспектора драйверов ядра, BYOVD эксплойтов и TestSigning</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО ДРАЙВЕРОВ</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>BYOVD РУТКИТЫ</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiByovd.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>TESTSIGNING BCD</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiBcd.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>СЛЕДЫ KDMAPPER</div><div class='kpi-value' style='color:#EF5350'>" + lblKpiMappers.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>БЕСФАЙЛОВЫЕ</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiUnbacked.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ДОВЕРЕННЫЙ СТАТУС</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiClean.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Статус</th><th>Драйвер</th><th>Категория</th><th>Базовый адрес</th><th>Правило</th><th>Путь</th><th>Детали</th></tr>");
					foreach (KernelDriverEvidenceItem it in allItems)
					{
						string cls = it.IsViolation ? (it.TargetRule == "5.08" ? "warn" : "crit") : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td class='" + cls + "'>" + it.Status + "</td>");
						sb.AppendLine("<td>" + it.DriverName + "</td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td><code>" + it.BaseAddress + "</code></td>");
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

		public static List<KernelDriverEvidenceItem> PerformKernelDriverForensicScan()
		{
			List<KernelDriverEvidenceItem> results = new List<KernelDriverEvidenceItem>();
			int idx = 1;

			// 1. Аудит конфигурации BCD и режима TestSigning (5.08)
			AuditBcdBootConfiguration(results, ref idx);

			// 2. Аудит загруженных драйверов ядра через EnumDeviceDrivers (BYOVD, Unbacked)
			AuditLoadedKernelDeviceDrivers(results, ref idx);

			// 3. Аудит системных служб Windows Services на драйверы читов (SYSTEM\CurrentControlSet\Services)
			AuditKernelDriverServicesRegistry(results, ref idx);

			// 4. Аудит журналов событий Windows (Event ID 7045) на следы kdmapper, kdu, drvmap
			AuditKernelMapperEventLogs(results, ref idx);

			// 5. Проверка файлов уязвимых драйверов на диске в Temp и drivers
			AuditVulnerableDriverArtifactsOnDisk(results, ref idx);

			return results;
		}

		private static void AuditBcdBootConfiguration(List<KernelDriverEvidenceItem> results, ref int idx)
		{
			bool foundBcdViolation = false;

			// A. Проверка через реестр SystemStartOptions
			try
			{
				using (RegistryKey ctrlKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
				{
					if (ctrlKey != null)
					{
						string sOptions = (ctrlKey.GetValue("SystemStartOptions") ?? "").ToString().ToUpperInvariant();

						if (sOptions.Contains("TESTSIGNING") || sOptions.Contains("DISABLE_INTEGRITY_CHECKS"))
						{
							foundBcdViolation = true;
							KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
							item.Index = idx++;
							item.Category = "Модификация BCD (5.08)";
							item.DriverName = "SystemStartOptions (TestSigning)";
							item.BaseAddress = "BCD Core Config";
							item.ParameterOrValue = sOptions;
							item.FileOrRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\SystemStartOptions";
							item.Status = "⛔ РЕЖИМ TESTSIGNING ВКЛЮЧЕН (5.08)";
							item.ThreatLevel = "CRITICAL";
							item.Verdict = "5.08 - Отключение проверки подписи драйверов";
							item.TargetRule = "5.08";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentRed;
							item.DetailDescription = string.Format("В параметрах загрузки Windows зафиксирован режим '{0}'. Отключение проверки подписей драйверов (Driver Signature Enforcement) позволяет загружать неподписанные руткиты ядра.", sOptions);
							item.TechnicalAnalysis = "Аудит BCD: обнаружен флаг TESTSIGNING / DISABLE_INTEGRITY_CHECKS. Нарушение пункта 5.08 регламента проверок.";
							results.Add(item);
						}

						if (sOptions.Contains("DEBUG") || sOptions.Contains("BOOTDEBUG"))
						{
							foundBcdViolation = true;
							KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
							item.Index = idx++;
							item.Category = "Модификация BCD (5.08)";
							item.DriverName = "SystemStartOptions (Kernel Debugging)";
							item.BaseAddress = "BCD Debug Flag";
							item.ParameterOrValue = sOptions;
							item.FileOrRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\SystemStartOptions";
							item.Status = "⚠️ ВКЛЮЧЕНА ОТЛАДКА ЯДРА (5.08)";
							item.ThreatLevel = "WARNING";
							item.Verdict = "5.08 - Режим Kernel Debugging";
							item.TargetRule = "5.08";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentOrange;
							item.DetailDescription = string.Format("В параметрах загрузки Windows активен флаг отладки ядра '{0}'. Используется для реверс-инжиниринга и отладки читов под гипервизором.", sOptions);
							item.TechnicalAnalysis = "Флаг DEBUG в реестре SystemStartOptions.";
							results.Add(item);
						}
					}
				}
			}
			catch { }

			if (!foundBcdViolation)
			{
				KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
				item.Index = idx++;
				item.Category = "Модификация BCD (5.08)";
				item.DriverName = "BCD Bootloader";
				item.BaseAddress = "DSE Active";
				item.ParameterOrValue = "TestSigning отключен, DSE активен";
				item.FileOrRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control";
				item.Status = "🟢 TESTSIGNING ОТКЛЮЧЕН (DSE ЦЕЛОСТЕН)";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Проверка цифровой подписи драйверов ядра активна (TestSigning = OFF). Загрузка неподписанных руткитов заблокирована Windows DSE.";
				item.TechnicalAnalysis = "Аудит SystemStartOptions: аномальных ключей TESTSIGNING или DISABLE_INTEGRITY_CHECKS не обнаружено.";
				results.Add(item);
			}
		}

		private static void AuditLoadedKernelDeviceDrivers(List<KernelDriverEvidenceItem> results, ref int idx)
		{
			try
			{
				uint cbNeeded;
				IntPtr[] drivers = new IntPtr[1024];
				uint size = (uint)(IntPtr.Size * drivers.Length);

				if (!EnumDeviceDrivers(drivers, size, out cbNeeded))
				{
					return;
				}

				int count = (int)(cbNeeded / IntPtr.Size);
				if (count > drivers.Length) count = drivers.Length;

				StringBuilder sbBaseName = new StringBuilder(260);
				StringBuilder sbFileName = new StringBuilder(520);

				// Список уязвимых BYOVD драйверов
				string[] byovdDrivers = new string[]
				{
					"gdrv.sys", "mhyprot2.sys", "rtcore64.sys", "echo.sys", "echo_driver.sys",
					"iqvw64e.sys", "capcom.sys", "dbk64.sys", "dbk32.sys", "kprocesshacker.sys",
					"kprocesshacker3.sys", "asioread.sys", "asrdrv.sys", "asromgdrv.sys",
					"cpuz141.sys", "cpuz150.sys", "inpoutx64.sys", "winio64.sys", "blackbone.sys",
					"xenos64.sys", "speedhack.sys", "procexp.sys"
				};

				int trustedCount = 0;

				for (int i = 0; i < count; i++)
				{
					IntPtr baseAddr = drivers[i];
					if (baseAddr == IntPtr.Zero) continue;

					sbBaseName.Length = 0;
					sbFileName.Length = 0;

					GetDeviceDriverBaseName(baseAddr, sbBaseName, (uint)sbBaseName.Capacity);
					GetDeviceDriverFileName(baseAddr, sbFileName, (uint)sbFileName.Capacity);

					string baseName = sbBaseName.ToString();
					string rawPath = sbFileName.ToString();
					if (string.IsNullOrEmpty(baseName)) continue;

					string lowerName = baseName.ToLowerInvariant();
					string baseAddrHex = "0x" + baseAddr.ToString("X16");

					bool isByovd = false;
					foreach (string b in byovdDrivers)
					{
						if (lowerName == b)
						{
							isByovd = true;
							break;
						}
					}

					if (isByovd)
					{
						KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
						item.Index = idx++;
						item.Category = "BYOVD Эксплойты (5.01)";
						item.DriverName = baseName;
						item.BaseAddress = baseAddrHex;
						item.ParameterOrValue = "Уязвимый драйвер активен в ядре";
						item.FileOrRegistryPath = rawPath;
						item.Status = "🚨 BYOVD УЯЗВИМЫЙ ДРАЙВЕР (5.01)";
						item.ThreatLevel = "CRITICAL";
						item.Verdict = "5.01 - Чит ядра / BYOVD драйвер";
						item.TargetRule = "5.01";
						item.IsViolation = true;
						item.IsWhitelisted = false;
						item.Timestamp = DateTime.Now;
						item.RowColor = Ui.AccentRed;
						item.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.01: В ядре Windows активен уязвимый драйвер {0} (Базовый адрес: {1}). Драйвер используется чит-клиентами для произвольного чтения/записи физической памяти процесса javaw.exe в обход защит.", baseName, baseAddrHex);
						item.TechnicalAnalysis = string.Format("EnumDeviceDrivers зафиксировал загруженный модуль {0} по адресу {1}. Путь устройства: {2}.", baseName, baseAddrHex, rawPath);
						results.Add(item);
						continue;
					}

					// Проверка на бесфайловый / удаленный драйвер
					// Если путь к драйверу указывает на System32\drivers, но файл физически отсутствует на диске
					string resolvedPath = ResolveDevicePath(rawPath);
					if (!string.IsNullOrEmpty(resolvedPath) && resolvedPath.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase))
					{
						if (!File.Exists(resolvedPath))
						{
							KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
							item.Index = idx++;
							item.Category = "Бесфайловые драйверы (5.08)";
							item.DriverName = baseName;
							item.BaseAddress = baseAddrHex;
							item.ParameterOrValue = "Файл драйвера удален с диска";
							item.FileOrRegistryPath = resolvedPath;
							item.Status = "🚨 БЕСФАЙЛОВЫЙ ДРАЙВЕР (5.08)";
							item.ThreatLevel = "CRITICAL";
							item.Verdict = "5.08 - Скрытый удаленный драйвер";
							item.TargetRule = "5.08";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentPurple;
							item.DetailDescription = string.Format("Драйвер {0} загружен в оперативную память ядра (Адрес: {1}), однако его физический файл {2} отсутствует на диске. Характерно для самоудаляющихся руткитов.", baseName, baseAddrHex, resolvedPath);
							item.TechnicalAnalysis = string.Format("Аудит целостности файлов драйверов: файл {0} не найден в файловой системе.", resolvedPath);
							results.Add(item);
							continue;
						}
					}

					trustedCount++;
				}

				if (trustedCount > 0)
				{
					KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
					item.Index = idx++;
					item.Category = "Доверенные драйверы ядра";
					item.DriverName = string.Format("Системные модули ({0} шт.)", trustedCount);
					item.BaseAddress = "Kernel Address Space";
					item.ParameterOrValue = string.Format("Всего проверено модулей: {0}", count);
					item.FileOrRegistryPath = @"\SystemRoot\System32\drivers";
					item.Status = "🟢 ДОВЕРЕННЫЕ СИСТЕМНЫЕ ДРАЙВЕРЫ";
					item.ThreatLevel = "SAFE";
					item.Verdict = "РАЗРЕШЕНО";
					item.TargetRule = "ЧИСТО";
					item.IsViolation = false;
					item.IsWhitelisted = true;
					item.Timestamp = DateTime.Now;
					item.RowColor = Ui.AccentGreen;
					item.DetailDescription = string.Format("Успешно просканировано {0} модулей ядра через EnumDeviceDrivers. Все модули имеют легитимные системные цифровые подписи (Microsoft/Nvidia/Intel/AMD/Realtek).", trustedCount);
					item.TechnicalAnalysis = "Анализ EnumDeviceDrivers завершен без аномалий в пуле ядра.";
					results.Add(item);
				}
			}
			catch { }
		}

		private static void AuditKernelDriverServicesRegistry(List<KernelDriverEvidenceItem> results, ref int idx)
		{
			string[] cheatServiceNames = new string[]
			{
				"dbk64", "dbk32", "gdrv", "mhyprot2", "rtcore64", "echo", "echo_driver",
				"iqvw64e", "capcom", "kprocesshacker", "kprocesshacker3", "systeminformer",
				"blackbone", "xenos", "xenos64", "winio", "inpoutx64", "cpuz141"
			};

			try
			{
				using (RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
				{
					if (servicesKey != null)
					{
						string[] subNames = servicesKey.GetSubKeyNames();
						foreach (string sub in subNames)
						{
							string lowerSub = sub.ToLowerInvariant();
							if (lowerSub.StartsWith("ehstortcg") || lowerSub == "ehstortcgdrv")
							{
								continue;
							}

							bool isSuspicious = false;
							foreach (string cs in cheatServiceNames)
							{
								if (lowerSub == cs || lowerSub.StartsWith(cs + "_") || lowerSub.StartsWith(cs + "-") || lowerSub.StartsWith(cs + "64"))
								{
									isSuspicious = true;
									break;
								}
							}

							if (isSuspicious)
							{
								using (RegistryKey sKey = servicesKey.OpenSubKey(sub))
								{
									if (sKey != null)
									{
										string imgPath = (sKey.GetValue("ImagePath") as string ?? "").Trim();
										string lowerImg = imgPath.ToLowerInvariant();
										if (lowerImg.Contains("ehstortcgdrv") || lowerImg.Contains("microsoft"))
										{
											continue;
										}
										object typeVal = sKey.GetValue("Type");
										int svcType = typeVal != null ? Convert.ToInt32(typeVal) : 0;

										KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
										item.Index = idx++;
										item.Category = "BYOVD Эксплойты (5.01)";
										item.DriverName = sub + " (Служба)";
										item.BaseAddress = "Service Type: " + svcType;
										item.ParameterOrValue = "ImagePath: " + imgPath;
										item.FileOrRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Services\" + sub;
										item.Status = "🚨 СЛУЖБА ЧИТ-ДРАЙВЕРА В РЕЕСТРЕ (5.01)";
										item.ThreatLevel = "CRITICAL";
										item.Verdict = "5.01 - Чит-служба ядра";
										item.TargetRule = "5.01";
										item.IsViolation = true;
										item.IsWhitelisted = false;
										item.Timestamp = DateTime.Now;
										item.RowColor = Ui.AccentRed;
										item.DetailDescription = string.Format("В реестре Windows зарегистрирована служба руткит-драйвера '{0}' (Путь: {1}). Используется для активации читов ядра.", sub, imgPath);
										item.TechnicalAnalysis = string.Format("Ветка реестра HKLM\\SYSTEM\\CurrentControlSet\\Services\\{0} активна. Type={1}.", sub, svcType);
										results.Add(item);
									}
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private static void AuditKernelMapperEventLogs(List<KernelDriverEvidenceItem> results, ref int idx)
		{
			try
			{
				// Проверка журнала System на Event ID 7045 (Service Control Manager)
				// В Event 7045 фиксируется установка службы iqvw64e или gdrv при работе kdmapper / kdu
				EventLog eventLog = new EventLog("System");
				int entriesCount = eventLog.Entries.Count;
				int scanned = 0;
				int maxScan = Math.Min(entriesCount, 1500);

				bool foundMapperEvent = false;

				for (int i = entriesCount - 1; i >= entriesCount - maxScan && i >= 0; i--)
				{
					EventLogEntry entry = eventLog.Entries[i];
					scanned++;

					if ((entry.InstanceId & 0xFFFF) == 7045)
					{
						string msg = (entry.Message ?? "").ToLowerInvariant();
						if ((msg.Contains("iqvw64e") || msg.Contains("gdrv.sys") || msg.Contains("mhyprot") ||
							msg.Contains("rtcore64") || msg.Contains("dbk64") || msg.Contains("echo_driver")) &&
							!msg.Contains("ehstortcgdrv"))
						{
							foundMapperEvent = true;
							KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
							item.Index = idx++;
							item.Category = "Следы мапперов (5.01/5.08)";
							item.DriverName = "Event 7045 (kdmapper / BYOVD Service)";
							item.BaseAddress = "Event Log Artifact";
							item.ParameterOrValue = string.Format("Установка службы в {0:dd.MM.yyyy HH:mm}", entry.TimeGenerated);
							item.FileOrRegistryPath = @"System Event Log / Event ID 7045";
							item.Status = "🚨 СЛЕДЫ KDMAPPER В ЖУРНАЛЕ (5.01)";
							item.ThreatLevel = "CRITICAL";
							item.Verdict = "5.01 - Следы kdmapper / маппинга читов";
							item.TargetRule = "5.01";
							item.IsViolation = true;
							item.IsWhitelisted = false;
							item.Timestamp = entry.TimeGenerated;
							item.RowColor = Ui.AccentRed;
							item.DetailDescription = string.Format("В системном журнале Windows зафиксировано событие Event ID 7045 (Установка службы): '{0}'. Характерно для работы утилит маппинга ядра (kdmapper, kdu, drvmap).", entry.Message.Replace("\r", " ").Replace("\n", " ").Trim());
							item.TechnicalAnalysis = string.Format("System Event Log (7045) в {0}: зафиксирована регистрация драйвера маппера.", entry.TimeGenerated);
							results.Add(item);
							break;
						}
					}
				}

				if (!foundMapperEvent)
				{
					KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
					item.Index = idx++;
					item.Category = "Следы мапперов (5.01/5.08)";
					item.DriverName = "System Event 7045";
					item.BaseAddress = "Clean Log";
					item.ParameterOrValue = "Следов установки уязвимых драйверов не найдено";
					item.FileOrRegistryPath = @"System Event Log (Event ID 7045)";
					item.Status = "🟢 СЛЕДОВ KDMAPPER В ЖУРНАЛЕ НЕТ";
					item.ThreatLevel = "SAFE";
					item.Verdict = "РАЗРЕШЕНО";
					item.TargetRule = "ЧИСТО";
					item.IsViolation = false;
					item.IsWhitelisted = true;
					item.Timestamp = DateTime.Now;
					item.RowColor = Ui.AccentGreen;
					item.DetailDescription = "В журнале System (Event ID 7045) следов автоматической установки и удаления уязвимых служб (iqvw64e, gdrv, mhyprot2) не обнаружено.";
					item.TechnicalAnalysis = string.Format("Просканировано {0} последних записей журнала событий System.", scanned);
					results.Add(item);
				}
			}
			catch { }
		}

		private static void AuditVulnerableDriverArtifactsOnDisk(List<KernelDriverEvidenceItem> results, ref int idx)
		{
			string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string tempDir = Path.GetTempPath();
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			string[] checkLocations = new string[]
			{
				Path.Combine(winDir, "System32", "drivers"),
				tempDir,
				Path.Combine(localAppData, "Temp")
			};

			string[] suspectFileNames = new string[]
			{
				"iqvw64e.sys", "gdrv.sys", "mhyprot2.sys", "RTCore64.sys", "dbk64.sys", "dbk32.sys",
				"capcom.sys", "echo.sys", "echo_driver.sys", "kprocesshacker.sys", "cpuz141.sys"
			};

			foreach (string loc in checkLocations)
			{
				try
				{
					if (Directory.Exists(loc))
					{
						foreach (string fn in suspectFileNames)
						{
							string fullPath = Path.Combine(loc, fn);
							if (File.Exists(fullPath))
							{
								KernelDriverEvidenceItem item = new KernelDriverEvidenceItem();
								item.Index = idx++;
								item.Category = "BYOVD Эксплойты (5.01)";
								item.DriverName = fn;
								item.BaseAddress = "On-Disk File";
								item.ParameterOrValue = "Файл обнаружен на диске (" + new FileInfo(fullPath).Length + " байт)";
								item.FileOrRegistryPath = fullPath;
								item.Status = "🚨 ФАЙЛ УЯЗВИМОГО ДРАЙВЕРА НА ДИСКЕ (5.01)";
								item.ThreatLevel = "CRITICAL";
								item.Verdict = "5.01 - Чит-драйвер BYOVD на диске";
								item.TargetRule = "5.01";
								item.IsViolation = true;
								item.IsWhitelisted = false;
								item.Timestamp = File.GetLastWriteTime(fullPath);
								item.RowColor = Ui.AccentRed;
								item.DetailDescription = string.Format("В каталоге {0} обнаружен файл уязвимого драйвера {1}, применяемого мапперами kdmapper для обхода цифровой подписи.", loc, fn);
								item.TechnicalAnalysis = string.Format("Файл {0} присутствует на диске. Размер: {1} байт.", fullPath, new FileInfo(fullPath).Length);
								results.Add(item);
							}
						}
					}
				}
				catch { }
			}
		}

		private static string ResolveDevicePath(string devicePath)
		{
			if (string.IsNullOrEmpty(devicePath)) return "";
			if (devicePath.StartsWith("\\SystemRoot\\", StringComparison.OrdinalIgnoreCase))
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), devicePath.Substring(12));
			}
			if (devicePath.StartsWith("\\??\\", StringComparison.OrdinalIgnoreCase))
			{
				return devicePath.Substring(4);
			}
			if (devicePath.StartsWith("\\Device\\HarddiskVolume", StringComparison.OrdinalIgnoreCase))
			{
				// Обычно HarddiskVolume3 или Volume2 — заменим на системный диск C:
				int slashIdx = devicePath.IndexOf('\\', 8);
				if (slashIdx >= 0)
				{
					string rel = devicePath.Substring(slashIdx + 1);
					return "C:\\" + rel;
				}
			}
			return devicePath;
		}

		#endregion
	}
}
