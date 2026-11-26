using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum EvasionVmFilter
	{
		All,
		ViolationsOnly,
		VirtualMachines,
		Sandboxes,
		StreamProofAndDisplays,
		Whitelisted
	}

	public class EvasionVmEvidenceItem
	{
		public int Index;
		public string Category; // "Виртуальная машина (VM)", "Песочница (Sandbox)", "Stream-Proof сокрытие", "Виртуальный монитор", "Ядерный обход"
		public string SourceArtifact;
		public string DetectedFeature;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.08", "5.01", "РАЗРЕШЕНО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
	}

	public class HypervisorVmAndSandboxEvasionViewerForm : Form
	{
		#region Win32 API

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		private const uint WDA_NONE = 0x00000000;
		private const uint WDA_MONITOR = 0x00000001;
		private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

		#endregion

		private readonly string targetPlayerName;
		private List<EvasionVmEvidenceItem> allItems = new List<EvasionVmEvidenceItem>();
		private EvasionVmFilter currentFilter = EvasionVmFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiVm;
		private Label lblKpiSandbox;
		private Label lblKpiStreamProof;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterVm;
		private DarkButton btnFilterSandbox;
		private DarkButton btnFilterStreamProof;
		private DarkButton btnFilterWhitelisted;

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

		public HypervisorVmAndSandboxEvasionViewerForm() : this("Player")
		{
		}

		public HypervisorVmAndSandboxEvasionViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "🛡️ Инспектор гипервизоров, виртуальных машин, песочниц и Stream-Proof сокрытия (Tool 70 / Step 67) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🛡️ ИНСПЕКТОР ГИПЕРВИЗОРОВ, ВИРТУАЛЬНЫХ МАШИН, SANDBOX И STREAM-PROOF СОКРЫТИЯ";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Детекция обхода проверок: виртуальные машины (VMware/VBox/Hyper-V/KVM), песочницы, Stream-Proof окна (WDA_EXCLUDEFROMCAPTURE), фейковые мониторы (Idd) и DBVM (5.08/5.01).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация глубокого сканирования среды...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 340, 16);
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ПРОВЕРОК", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.08/5.01)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("🖥️ ВИРТУАЛЬНЫЕ МАШИНЫ", "0", Ui.AccentOrange, ref kx, out lblKpiVm));
			kpiPanel.Controls.Add(CreateKpiBadge("📦 ПЕСОЧНИЦЫ (SANDBOX)", "0", Ui.AccentPurple, ref kx, out lblKpiSandbox));
			kpiPanel.Controls.Add(CreateKpiBadge("🪟 STREAM-PROOF / ЭКРАНЫ", "0", Ui.AccentBlue, ref kx, out lblKpiStreamProof));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ БЕЛЫЙ СПИСОК", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все параметры", EvasionVmFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08/5.01)", EvasionVmFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterVm = CreateFilterButton("🖥️ Виртуальные машины (VM)", EvasionVmFilter.VirtualMachines, ref bx, filterBar);
			btnFilterSandbox = CreateFilterButton("📦 Песочницы (Sandbox)", EvasionVmFilter.Sandboxes, ref bx, filterBar);
			btnFilterStreamProof = CreateFilterButton("🪟 Stream-Proof и Экраны", EvasionVmFilter.StreamProofAndDisplays, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Белый список", EvasionVmFilter.Whitelisted, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по вендору, драйверу, окну, WDA или правилу...";
			searchBox.Size = new Size(240, 26);
			searchBox.Location = new Point(filterBar.Width - 260, 8);
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
			btnRescan = CreateActionButton("🔄 Повторить аудит среды", ref ax, bottomActions, delegate { StartScan(); });

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
			listView.Columns.Add("Источник / Механизм", 220);
			listView.Columns.Add("Обнаруженный признак / Объект", 270);
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

			TabPage tabAnalysis = new TabPage("🔍 Судебный анализ виртуализации и сокрытия");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⏱️ Технические параметры среды");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			txtTechnical.Font = Ui.FontMono(9f);
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 / 5.01 и Команда бана");
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

			curX += 206;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, EvasionVmFilter mode, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8f, true);
			btn.Size = new Size(195, 28);
			btn.Location = new Point(x, 8);
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtonStyles();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			x += btn.Width + 6;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int x, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8.5f, true);
			btn.Size = new Size(220, 30);
			btn.Location = new Point(x, 8);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			x += btn.Width + 10;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			btnFilterAll.AccentStyle = (currentFilter == EvasionVmFilter.All);
			btnFilterViolations.AccentStyle = (currentFilter == EvasionVmFilter.ViolationsOnly);
			btnFilterVm.AccentStyle = (currentFilter == EvasionVmFilter.VirtualMachines);
			btnFilterSandbox.AccentStyle = (currentFilter == EvasionVmFilter.Sandboxes);
			btnFilterStreamProof.AccentStyle = (currentFilter == EvasionVmFilter.StreamProofAndDisplays);
			btnFilterWhitelisted.AccentStyle = (currentFilter == EvasionVmFilter.Whitelisted);
		}

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Идёт криминалистический аудит виртуализации и сокрытия...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			listView.Items.Clear();
			allItems.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<EvasionVmEvidenceItem> items = new List<EvasionVmEvidenceItem>();
			int idx = 1;

			// 1. Виртуальные машины (VMware, VBox, Hyper-V, QEMU, KVM, Xen)
			ScanVirtualMachines(items, ref idx);

			// 2. Песочницы (Windows Sandbox, Sandboxie, WDAGUtilityAccount)
			ScanSandboxes(items, ref idx);

			// 3. Stream-Proof окна (GetWindowDisplayAffinity / WDA_EXCLUDEFROMCAPTURE)
			ScanStreamProofWindows(items, ref idx);

			// 4. Виртуальные мониторы (IddSampleDriver, Amyuni, USB Display)
			ScanVirtualDisplays(items, ref idx);

			// 5. Ядерные драйверы обхода (DBVM dbk64.sys, TestSigning)
			ScanKernelAndTestSigningEvasion(items, ref idx);

			e.Result = items;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			allItems = e.Result as List<EvasionVmEvidenceItem>;
			if (allItems == null) allItems = new List<EvasionVmEvidenceItem>();

			int violations = 0;
			int vmCount = 0;
			int sandboxCount = 0;
			int streamProofCount = 0;
			int whitelisted = 0;

			foreach (EvasionVmEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.IsWhitelisted) whitelisted++;

				if (it.Category.Contains("Виртуальная машина")) vmCount++;
				else if (it.Category.Contains("Песочница")) sandboxCount++;
				else if (it.Category.Contains("Stream-Proof") || it.Category.Contains("монитор")) streamProofCount++;
			}

			lblKpiTotal.Text = allItems.Count.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiVm.Text = vmCount.ToString();
			lblKpiSandbox.Text = sandboxCount.ToString();
			lblKpiStreamProof.Text = streamProofCount.ToString();
			lblKpiWhitelisted.Text = whitelisted.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} НАРУШЕНИЙ СРЕДЫ (5.08 / 5.01)!", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Среда проверена: физический ПК, оверлеев и изоляции не найдено";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}

			UpdateFilterButtonStyles();
			ApplyFilter();
		}

		#region Detection Engines

		private void ScanVirtualMachines(List<EvasionVmEvidenceItem> items, ref int idx)
		{
			try
			{
				// 1. BIOS и системный вендор через WMI Win32_ComputerSystem
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string mfg = (mo["Manufacturer"] ?? "").ToString();
						string mdl = (mo["Model"] ?? "").ToString();
						string combined = (mfg + " " + mdl).ToLowerInvariant();

						bool isVm = combined.Contains("vmware") || combined.Contains("virtualbox") || combined.Contains("innotek") ||
									combined.Contains("qemu") || combined.Contains("kvm") || combined.Contains("xen") ||
									combined.Contains("parallels") || (combined.Contains("microsoft") && combined.Contains("virtual"));

						if (isVm)
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальная машина (VM)";
							it.SourceArtifact = "WMI Win32_ComputerSystem";
							it.DetectedFeature = string.Format("{0} / {1}", mfg, mdl);
							it.Status = "ОБНАРУЖЕНА VM";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ЗАПУСК НА ВИРТУАЛЬНОЙ МАШИНЕ (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nСистема запущена внутри виртуальной машины!\nПроизводитель: {0}\nМодель: {1}\nИспользование VM для прохождения проверки запрещено регламентом.", mfg, mdl);
							items.Add(it);
						}
						else
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальная машина (VM)";
							it.SourceArtifact = "WMI Win32_ComputerSystem";
							it.DetectedFeature = string.Format("{0} / {1}", mfg, mdl);
							it.Status = "ФИЗИЧЕСКИЙ ПК";
							it.ThreatLevel = "SAFE";
							it.Verdict = "ℹ️ Подлинное аппаратное обеспечение ПК";
							it.TargetRule = "РАЗРЕШЕНО";
							it.IsViolation = false;
							it.IsWhitelisted = true;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.TextSecondary;
							it.DetailDescription = string.Format("Аппаратный производитель: {0}, модель: {1}. Признаков эмуляции нет.", mfg, mdl);
							items.Add(it);
						}
					}
				}

				// 2. Реестр BIOS и материнской платы
				using (RegistryKey biosKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
				{
					if (biosKey != null)
					{
						string bVendor = (biosKey.GetValue("BIOSVendor") ?? "").ToString();
						string bVersion = (biosKey.GetValue("BIOSVersion") ?? "").ToString();
						string sModel = (biosKey.GetValue("SystemProductName") ?? "").ToString();
						string rawBios = (bVendor + " " + bVersion + " " + sModel).ToLowerInvariant();

						if (rawBios.Contains("vbox") || rawBios.Contains("vmware") || rawBios.Contains("qemu") || rawBios.Contains("bochs"))
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальная машина (VM)";
							it.SourceArtifact = @"HKLM\...\System\BIOS";
							it.DetectedFeature = string.Format("{0} {1} ({2})", bVendor, bVersion, sModel);
							it.Status = "АРТЕФАКТ BIOS VM";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 СЛЕДЫ BIOS ВИРТУАЛЬНОЙ МАШИНЫ (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("В реестре обнаружены строки гипервизора в параметрах BIOS:\nВендор: {0}, Версия: {1}, Модель: {2}.", bVendor, bVersion, sModel);
							items.Add(it);
						}
					}
				}

				// 3. Дисковые накопители (VBOX HARDDISK, VMware Virtual, QEMU, Red Hat VirtIO)
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_DiskDrive"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string dModel = (mo["Model"] ?? "").ToString();
						string dLower = dModel.ToLowerInvariant();
						if (dLower.Contains("vbox") || dLower.Contains("vmware") || dLower.Contains("qemu") || dLower.Contains("virtio"))
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальная машина (VM)";
							it.SourceArtifact = "WMI Win32_DiskDrive";
							it.DetectedFeature = dModel;
							it.Status = "ВИРТУАЛЬНЫЙ НАКОПИТЕЛЬ";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ВИРТУАЛЬНЫЙ ЖЁСТКИЙ ДИСК VM (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("Обнаружен виртуальный диск гипервизора: '{0}'. Подтверждает выполнение игры в изолированной среде.", dModel);
							items.Add(it);
						}
					}
				}

				// 4. Службы Guest Additions и интеграции
				string[] vmServices = new string[]
				{
					"VBoxGuest", "VBoxMouse", "VBoxSF", "VBoxService",
					"vmhgfs", "vmmouse", "vmmvmt", "vmrawdsk", "vmusbmouse", "vmvss", "vmx_svga",
					"virtio", "Balloon", "netkvm", "viostor", "qemu-ga",
					"prl_fs", "prl_mouse", "prl_tg"
				};

				foreach (string sName in vmServices)
				{
					using (RegistryKey sKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + sName))
					{
						if (sKey != null)
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальная машина (VM)";
							it.SourceArtifact = @"Services\" + sName;
							it.DetectedFeature = "Драйвер интеграции: " + sName;
							it.Status = "СЛУЖБА GUEST ADDITIONS";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ДРАЙВЕР СИСТЕМЫ ИНТЕГРАЦИИ VM (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("В реестре Windows зарегистрирована служба интеграции гипервизора: {0}.\nСлужит для обмена файлами и буфером с хост-системой.", sName);
							items.Add(it);
						}
					}
				}

				// 5. Сетевые MAC-адреса OUI
				string[] vmOuis = new string[] { "000569", "000C29", "005056", "080027", "00155D", "525400", "001C42" };
				NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in nics)
				{
					string nicNameLower = (nic.Name ?? "").ToLowerInvariant();
					if (nicNameLower.Contains("vethernet") || nicNameLower.Contains("wsl") || nicNameLower.Contains("default switch"))
					{
						continue;
					}

					string mac = nic.GetPhysicalAddress().ToString();
					if (mac.Length >= 6)
					{
						string oui = mac.Substring(0, 6).ToUpperInvariant();
						foreach (string vOui in vmOuis)
						{
							if (oui.Equals(vOui, StringComparison.OrdinalIgnoreCase))
							{
								EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
								it.Index = idx++;
								it.Category = "Виртуальная машина (VM)";
								it.SourceArtifact = "Сетевой адаптер: " + nic.Name;
								it.DetectedFeature = string.Format("MAC: {0} (OUI: {1})", nic.GetPhysicalAddress(), oui);
								it.Status = "MAC-АДРЕС ГИПЕРВИЗОРА";
								it.ThreatLevel = "CRITICAL";
								it.Verdict = "🚨 СЕТЕВОЙ АДАПТЕР ГИПЕРВИЗОРА (5.08)";
								it.TargetRule = "5.08";
								it.IsViolation = true;
								it.IsWhitelisted = false;
								it.Timestamp = DateTime.Now;
								it.RowColor = Ui.AccentRed;
								it.DetailDescription = string.Format("Сетевой адаптер '{0}' имеет виртуальный MAC-адрес (OUI {1}), зарегистрированный за разработчиками гипервизоров (VMware/VBox/Hyper-V/KVM).", nic.Name, oui);
								items.Add(it);
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				EvasionVmEvidenceItem err = new EvasionVmEvidenceItem();
				err.Index = idx++;
				err.Category = "Виртуальная машина (VM)";
				err.SourceArtifact = "VM Engine";
				err.DetectedFeature = ex.Message;
				err.Status = "ОШИБКА";
				err.ThreatLevel = "INFO";
				err.Verdict = "Ошибка аудита VM: " + ex.Message;
				err.TargetRule = "ИНФО";
				err.Timestamp = DateTime.Now;
				err.RowColor = Ui.TextMuted;
				items.Add(err);
			}
		}

		private void ScanSandboxes(List<EvasionVmEvidenceItem> items, ref int idx)
		{
			try
			{
				// 1. Проверка Windows Sandbox / WDAGUtilityAccount
				string uName = Environment.UserName.ToLowerInvariant();
				string mName = Environment.MachineName.ToLowerInvariant();

				if (uName.Contains("wdagutilityaccount") || mName.StartsWith("wdagutility"))
				{
					EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
					it.Index = idx++;
					it.Category = "Песочница (Sandbox)";
					it.SourceArtifact = "Учётная запись Windows";
					it.DetectedFeature = string.Format("Пользователь: {0}, ПК: {1}", Environment.UserName, Environment.MachineName);
					it.Status = "WINDOWS SANDBOX";
					it.ThreatLevel = "CRITICAL";
					it.Verdict = "🚨 ЗАПУСК В ПЕСОЧНИЦЕ WINDOWS SANDBOX (5.08)";
					it.TargetRule = "5.08";
					it.IsViolation = true;
					it.IsWhitelisted = false;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.AccentRed;
					it.DetailDescription = "Текущая сессия запущена в изолированной среде Windows Sandbox (WDAGUtilityAccount).\nВсе файлы и процессы уничтожаются сразу после закрытия песочницы.";
					items.Add(it);
				}

				// 2. Проверка Sandboxie (SbieDll.dll, SbieSvc)
				IntPtr hSbie = GetModuleHandle("SbieDll.dll");
				if (hSbie != IntPtr.Zero)
				{
					EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
					it.Index = idx++;
					it.Category = "Песочница (Sandbox)";
					it.SourceArtifact = "Внедрённая библиотека SbieDll.dll";
					it.DetectedFeature = "Sandboxie Hook DLL активна в процессе";
					it.Status = "SANDBOXIE АКТИВЕН";
					it.ThreatLevel = "CRITICAL";
					it.Verdict = "🚨 ИЗОЛЯЦИЯ ПРОЦЕССА В SANDBOXIE (5.08)";
					it.TargetRule = "5.08";
					it.IsViolation = true;
					it.IsWhitelisted = false;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.AccentRed;
					it.DetailDescription = "В память процесса загружена библиотека SbieDll.dll (Sandboxie).\nПриложение изолировано от реальной файловой системы и реестра.";
					items.Add(it);
				}

				// Проверка службы Sandboxie
				using (RegistryKey sKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SbieDrv"))
				{
					if (sKey != null)
					{
						EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
						it.Index = idx++;
						it.Category = "Песочница (Sandbox)";
						it.SourceArtifact = @"Services\SbieDrv";
						it.DetectedFeature = "Драйвер изоляции Sandboxie (SbieDrv)";
						it.Status = "СЛУЖБА ПЕСОЧНИЦЫ";
						it.ThreatLevel = "WARNING";
						it.Verdict = "⚠️ Драйвер Sandboxie установлен в ОС (5.08)";
						it.TargetRule = "5.08";
						it.IsViolation = false;
						it.IsWhitelisted = false;
						it.Timestamp = DateTime.Now;
						it.RowColor = Ui.AccentOrange;
						it.DetailDescription = "В операционной системе установлен драйвер изоляции Sandboxie. Проверьте, не запускалась ли игра в изолированной песочнице.";
						items.Add(it);
					}
				}

				// 3. Эвристика Cuckoo / Any.Run
				string[] suspectDirs = new string[] { @"C:\analysis", @"C:\sample", @"C:\agent" };
				foreach (string sd in suspectDirs)
				{
					if (Directory.Exists(sd))
					{
						EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
						it.Index = idx++;
						it.Category = "Песочница (Sandbox)";
						it.SourceArtifact = sd;
						it.DetectedFeature = "Папка автоматизированного анализатора";
						it.Status = "СЛЕДЫ АВТО-ПЕСОЧНИЦЫ";
						it.ThreatLevel = "WARNING";
						it.Verdict = "⚠️ Признак автоматизированной песочницы анализа (Cuckoo/AnyRun)";
						it.TargetRule = "5.08";
						it.IsViolation = false;
						it.IsWhitelisted = false;
						it.Timestamp = DateTime.Now;
						it.RowColor = Ui.AccentOrange;
						it.DetailDescription = string.Format("Обнаружена папка анализа: '{0}'. Часто создаётся средами динамического анализа малвари.", sd);
						items.Add(it);
					}
				}
			}
			catch { }
		}

		private void ScanStreamProofWindows(List<EvasionVmEvidenceItem> items, ref int idx)
		{
			try
			{
				int refIdx = idx;
				EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
				{
					try
					{
						if (!IsWindowVisible(hWnd)) return true;

						uint affinity;
						if (GetWindowDisplayAffinity(hWnd, out affinity) && affinity != WDA_NONE)
						{
							StringBuilder title = new StringBuilder(256);
							GetWindowText(hWnd, title, title.Capacity);

							StringBuilder cls = new StringBuilder(256);
							GetClassName(hWnd, cls, cls.Capacity);

							uint pid;
							GetWindowThreadProcessId(hWnd, out pid);

							string procName = "Unknown";
							string procPath = "";
							try
							{
								Process p = Process.GetProcessById((int)pid);
								procName = p.ProcessName;
								procPath = p.MainModule.FileName;
							}
							catch { }

							string wndTitle = title.ToString();
							string wndCls = cls.ToString();

							// Проверяем белый список (KeePass, 1Password, Windows Security)
							string lower = (procName + " " + wndTitle).ToLowerInvariant();
							bool isWhitelisted = lower.Contains("keepass") || lower.Contains("1password") || lower.Contains("bitwarden") || lower.Contains("credential");

							string affName = affinity == WDA_EXCLUDEFROMCAPTURE ? "WDA_EXCLUDEFROMCAPTURE (0x11)" : "WDA_MONITOR (0x1)";

							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = refIdx++;
							it.Category = "Stream-Proof сокрытие";
							it.SourceArtifact = string.Format("Окно hWnd 0x{0:X8} ({1})", hWnd.ToInt64(), procName);
							it.DetectedFeature = string.Format("Заголовок: \"{0}\", Класс: {1}", string.IsNullOrEmpty(wndTitle) ? "—" : wndTitle, wndCls);
							it.Status = affName;
							it.ThreatLevel = isWhitelisted ? "SAFE" : "CRITICAL";
							it.Verdict = isWhitelisted ? "ℹ️ Легитимное защищённое окно менеджера паролей" : "🚨 СОКРЫТИЕ ОКНА ОТ ЗАХВАТА ЭКРАНА (STREAM-PROOF 5.01/5.08)";
							it.TargetRule = isWhitelisted ? "РАЗРЕШЕНО" : "5.01 / 5.08";
							it.IsViolation = !isWhitelisted;
							it.IsWhitelisted = isWhitelisted;
							it.Timestamp = DateTime.Now;
							it.RowColor = isWhitelisted ? Ui.TextSecondary : Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.01 / 5.08:\nОкно процесса {0} (PID {1}) принудительно скрыто от захвата экрана и демонстрации через функцию SetWindowDisplayAffinity ({2})!\nТакие окна не отображаются в демонстрации экрана Discord, AnyDesk или OBS Studio, позволяя читерам незаметно использовать оверлей/меню читов прямо во время проверки.", procName, pid, affName);
							items.Add(it);
						}
					}
					catch { }
					return true;
				}, IntPtr.Zero);
				idx = refIdx;
			}
			catch { }
		}

		private void ScanVirtualDisplays(List<EvasionVmEvidenceItem> items, ref int idx)
		{
			try
			{
				// Проверка драйверов виртуальных экранов (IddSampleDriver, Amyuni, USB Display)
				string[] suspectDrivers = new string[] { "IddSampleDriver", "usbmmidd", "indirectdisplay", "LuminonCore", "Deskreen" };
				foreach (string dName in suspectDrivers)
				{
					using (RegistryKey dKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + dName))
					{
						if (dKey != null)
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Виртуальный монитор";
							it.SourceArtifact = @"Services\" + dName;
							it.DetectedFeature = "Драйвер скрытого монитора: " + dName;
							it.Status = "ФЕЙКОВЫЙ МОНИТОР";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ВИРТУАЛЬНЫЙ МОНИТОР ДЛЯ СОКРЫТИЯ GUI ЧИТОВ (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nВ системе обнаружен драйвер виртуального монитора: {0}!\nЧитеры используют создание фейкового второго экрана (IddSampleDriver), чтобы перемещать туда окна и меню читов (Vape/Drip GUI), транслируя модератору только основной рабочий стол.", dName);
							items.Add(it);
						}
					}
				}

				// Проверка количества физических экранов
				Screen[] screens = Screen.AllScreens;
				if (screens.Length > 1)
				{
					EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
					it.Index = idx++;
					it.Category = "Виртуальный монитор";
					it.SourceArtifact = "Конфигурация дисплеев Windows";
					it.DetectedFeature = string.Format("Обнаружено {0} активных экранов", screens.Length);
					it.Status = "МНОГОМОНИТОРНАЯ СИСТЕМА";
					it.ThreatLevel = "WARNING";
					it.Verdict = "⚠️ В системе активно более 1 монитора (Требуется проверка трансляции)";
					it.TargetRule = "5.08";
					it.IsViolation = false;
					it.IsWhitelisted = false;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.AccentOrange;
					it.DetailDescription = string.Format("В системе обнаружено {0} мониторов. Убедитесь, что игрок транслирует весь рабочий стол (Entire Screen), а не отдельное изолированное окно приложения.", screens.Length);
					items.Add(it);
				}
				else
				{
					EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
					it.Index = idx++;
					it.Category = "Виртуальный монитор";
					it.SourceArtifact = "Конфигурация дисплеев Windows";
					it.DetectedFeature = "1 основной физический монитор";
					it.Status = "ШТАТНО";
					it.ThreatLevel = "SAFE";
					it.Verdict = "ℹ️ Одиночный физический монитор";
					it.TargetRule = "РАЗРЕШЕНО";
					it.IsViolation = false;
					it.IsWhitelisted = true;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.TextSecondary;
					it.DetailDescription = "В системе подключён один дисплей. Признаков скрытых виртуальных экранов не обнаружено.";
					items.Add(it);
				}
			}
			catch { }
		}

		private void ScanKernelAndTestSigningEvasion(List<EvasionVmEvidenceItem> items, ref int idx)
		{
			try
			{
				// 1. Проверка DBVM / Cheat Engine ядерного драйвера (dbk64.sys)
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string dbkPath = Path.Combine(winDir, "System32", "drivers", "dbk64.sys");
				string dbk32Path = Path.Combine(winDir, "System32", "drivers", "dbk32.sys");

				if (File.Exists(dbkPath) || File.Exists(dbk32Path))
				{
					EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
					it.Index = idx++;
					it.Category = "Ядерный обход";
					it.SourceArtifact = @"System32\drivers\dbk64.sys";
					it.DetectedFeature = "Ядерный драйвер Dark Byte Virtual Machine (DBVM)";
					it.Status = "DBVM ДРАЙВЕР ОБНАРУЖЕН";
					it.ThreatLevel = "CRITICAL";
					it.Verdict = "🚨 ЯДЕРНЫЙ РУТКИТ DBVM / CHEAT ENGINE (5.01 / 5.08)";
					it.TargetRule = "5.01 / 5.08";
					it.IsViolation = true;
					it.IsWhitelisted = false;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.AccentRed;
					it.DetailDescription = "КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.01 / 5.08:\nНа диске обнаружен ядерный драйвер DBVM (dbk64.sys)!\nDBVM запускает гипервизор прямо на лету и позволяет читам модифицировать память в обход любых античитов и защит ядра.";
					items.Add(it);
				}

				// Проверка службы DBK64
				using (RegistryKey sKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\DBK64"))
				{
					if (sKey != null)
					{
						EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
						it.Index = idx++;
						it.Category = "Ядерный обход";
						it.SourceArtifact = @"Services\DBK64";
						it.DetectedFeature = "Служба DBK64 в реестре";
						it.Status = "СЛУЖБА РУТКИТА";
						it.ThreatLevel = "CRITICAL";
						it.Verdict = "🚨 СЛУЖБА ЯДЕРНОГО ДРАЙВЕРА DBVM (5.08)";
						it.TargetRule = "5.08";
						it.IsViolation = true;
						it.IsWhitelisted = false;
						it.Timestamp = DateTime.Now;
						it.RowColor = Ui.AccentRed;
						it.DetailDescription = "В реестре зарегистрирована служба ядерного драйвера DBK64.";
						items.Add(it);
					}
				}

				// 2. Проверка режима TestSigning / отключения проверки подписи драйверов
				using (RegistryKey ctrlKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
				{
					if (ctrlKey != null)
					{
						string sOptions = (ctrlKey.GetValue("SystemStartOptions") ?? "").ToString().ToUpperInvariant();
						if (sOptions.Contains("TESTSIGNING") || sOptions.Contains("DISABLE_INTEGRITY_CHECKS"))
						{
							EvasionVmEvidenceItem it = new EvasionVmEvidenceItem();
							it.Index = idx++;
							it.Category = "Ядерный обход";
							it.SourceArtifact = @"Control\SystemStartOptions";
							it.DetectedFeature = sOptions;
							it.Status = "TESTSIGNING ВКЛЮЧЁН";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 РЕЖИМ ТЕСТОВОЙ ПОДПИСИ TESTSIGNING ВКЛЮЧЁН (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nПараметры загрузки Windows содержат '{0}'.\nОтключение проверки подписи драйверов позволяет загружать неподписанные руткит-читы и скрывать их процессы.", sOptions);
							items.Add(it);
						}
					}
				}
			}
			catch { }
		}

		#endregion

		#region Filtering & ListView

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (EvasionVmEvidenceItem it in allItems)
			{
				if (currentFilter == EvasionVmFilter.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == EvasionVmFilter.VirtualMachines && !it.Category.Contains("Виртуальная машина")) continue;
				if (currentFilter == EvasionVmFilter.Sandboxes && !it.Category.Contains("Песочница")) continue;
				if (currentFilter == EvasionVmFilter.StreamProofAndDisplays && !it.Category.Contains("Stream-Proof") && !it.Category.Contains("монитор")) continue;
				if (currentFilter == EvasionVmFilter.Whitelisted && !it.IsWhitelisted) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.Category.ToLowerInvariant().Contains(search) ||
								 it.SourceArtifact.ToLowerInvariant().Contains(search) ||
								 it.DetectedFeature.ToLowerInvariant().Contains(search) ||
								 it.Status.ToLowerInvariant().Contains(search) ||
								 it.Verdict.ToLowerInvariant().Contains(search) ||
								 it.TargetRule.ToLowerInvariant().Contains(search) ||
								 it.DetailDescription.ToLowerInvariant().Contains(search);
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.SourceArtifact);
				lvi.SubItems.Add(it.DetectedFeature);
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.ThreatLevel);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Timestamp.ToString("dd.MM.yyyy HH:mm"));
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
				txtAnalysis.Text = "Нет записей, соответствующих выбранному фильтру.";
				txtTechnical.Text = "";
				txtBanVerdict.Text = "";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			EvasionVmEvidenceItem item = listView.SelectedItems[0].Tag as EvasionVmEvidenceItem;
			if (item == null) return;

			// Вкладка 1: Анализ
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ АНАЛИЗ ГИПЕРВИЗОРА, VM И STREAM-PROOF СОКРЫТИЯ ===");
			sb.AppendLine("Категория проверки: " + item.Category);
			sb.AppendLine("Источник артефакта: " + item.SourceArtifact);
			sb.AppendLine("Обнаруженный признак: " + item.DetectedFeature);
			sb.AppendLine("Статус детектора: " + item.Status);
			sb.AppendLine("Уровень угрозы: " + item.ThreatLevel);
			sb.AppendLine("Правило регламента: " + item.TargetRule);
			sb.AppendLine("Штамп времени: " + item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Вердикт детектора: " + item.Verdict);
			sb.AppendLine();
			sb.AppendLine("--- ПОДРОБНОЕ КРИМИНАЛИСТИЧЕСКОЕ ОПИСАНИЕ ---");
			sb.AppendLine(item.DetailDescription);
			txtAnalysis.Text = sb.ToString();

			// Вкладка 2: Технические параметры
			StringBuilder tb = new StringBuilder();
			tb.AppendLine("=== ТЕХНИЧЕСКИЙ ПРОФИЛЬ АППАРАТНОЙ И СЕТЕВОЙ СРЕДЫ ===");
			tb.AppendLine("Имя компьютера: " + Environment.MachineName);
			tb.AppendLine("Имя пользователя: " + Environment.UserName);
			tb.AppendLine("ОС: " + Environment.OSVersion.VersionString + " (" + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit") + ")");
			tb.AppendLine("Логических процессоров: " + Environment.ProcessorCount);
			tb.AppendLine("Аптайм системы: " + (Environment.TickCount / (1000 * 60)) + " мин.");
			tb.AppendLine();
			tb.AppendLine("--- СВОДНЫЙ СПИСОК ПРОВЕРОК СРЕДЫ ---");
			foreach (EvasionVmEvidenceItem ev in allItems)
			{
				string flag = ev.ThreatLevel == "CRITICAL" ? "[🚨]" : (ev.ThreatLevel == "WARNING" ? "[⚠️]" : "[ℹ️]");
				tb.AppendLine(string.Format("{0} {1} | {2}: {3}", flag, ev.Category, ev.DetectedFeature, ev.Status));
			}
			txtTechnical.Text = tb.ToString();

			// Вкладка 3: Команда бана
			StringBuilder bb = new StringBuilder();
			bb.AppendLine("=== РЕГЛАМЕНТ И ВЕРДИКТ НАКАЗАНИЯ ===");
			if (item.IsViolation)
			{
				string reason = string.Format("5.08 - Виртуализация / Сокрытие ({0})", item.Verdict.Replace("🚨 ", ""));
				string banCmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);

				bb.AppendLine("СУДЕБНЫЙ ВЕРДИКТ: ПОДТВЕРЖДЕНО КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА!");
				bb.AppendLine("Нарушенный пункт: " + item.TargetRule);
				bb.AppendLine("Срок блокировки: 30 дней");
				bb.AppendLine();
				bb.AppendLine("ГОТОВАЯ КОМАНДА ДЛЯ МОДЕРАТОРА:");
				bb.AppendLine(banCmd);
				bb.AppendLine();
				bb.AppendLine("Нажмите кнопку '⚖️ Копировать команду бана' внизу формы для быстрой вставки.");
			}
			else
			{
				bb.AppendLine("Критических нарушений для выбранного параметра не зафиксировано.");
				bb.AppendLine("Параметр квалифицирован как: " + item.ThreatLevel + " (" + item.Status + ")");
			}
			txtBanVerdict.Text = bb.ToString();
		}

		#endregion

		#region Action Handlers

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count > 0)
			{
				EvasionVmEvidenceItem item = listView.SelectedItems[0].Tag as EvasionVmEvidenceItem;
				if (item != null && item.IsViolation)
				{
					string reason = string.Format("5.08 - Виртуализация / Сокрытие ({0})", item.Verdict.Replace("🚨 ", ""));
					string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			foreach (EvasionVmEvidenceItem it in allItems)
			{
				if (it.IsViolation)
				{
					string reason = string.Format("5.08 - Виртуализация / Сокрытие ({0})", it.Verdict.Replace("🚨 ", ""));
					string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			string defCmd = string.Format("/ban {0} 30d 5.08 - Виртуальная машина / Stream-Proof сокрытие", targetPlayerName);
			Clipboard.SetText(defCmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n\n" + defCmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine("- [MCCheatChecker] СУДЕБНЫЙ АУДИТ ГИПЕРВИЗОРОВ, VM И STREAM-PROOF СОКРЫТИЯ");
			sb.AppendLine("+ Игрок: " + targetPlayerName);
			sb.AppendLine("+ Время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("+ Нарушений: " + lblKpiViolations.Text);
			sb.AppendLine("+ Виртуальные машины (VM): " + lblKpiVm.Text);
			sb.AppendLine("+ Песочницы (Sandbox): " + lblKpiSandbox.Text);
			sb.AppendLine("+ Stream-Proof / Оверлеи: " + lblKpiStreamProof.Text);
			sb.AppendLine();
			sb.AppendLine("--- НАИБОЛЕЕ ОПАСНЫЕ НАХОДКИ ---");
			int count = 0;
			foreach (EvasionVmEvidenceItem it in allItems)
			{
				if (it.ThreatLevel == "CRITICAL" || it.ThreatLevel == "WARNING")
				{
					string pfx = it.ThreatLevel == "CRITICAL" ? "- [🚨 НАКАЗАНИЕ]" : "! [⚠️ ВНИМАНИЕ]";
					sb.AppendLine(string.Format("{0} {1} | {2} ({3})", pfx, it.Category, it.DetectedFeature, it.Verdict));
					count++;
					if (count >= 8) break;
				}
			}
			if (count == 0)
			{
				sb.AppendLine("+ Признаков виртуализации, песочниц и Stream-Proof окон не обнаружено.");
			}
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована!", "Discord Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), "mccheatchecker_vm_evasion_report_" + DateTime.Now.Ticks + ".html");
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Hypervisor & VM Evasion Report</title>");
				html.AppendLine("<style>body{background:#0e1117;color:#c9d1d9;font-family:Segoe UI,sans-serif;padding:24px;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #30363d;padding:8px 12px;font-size:12px;text-align:left;} th{background:#161b22;color:#58a6ff;} tr:nth-child(even){background:#12161f;} .crit{color:#f85149;font-weight:bold;} .warn{color:#d29922;} .safe{color:#3fb950;}</style></head><body>");
				html.AppendLine("<h2>🛡️ Судебный отчёт по виртуальным машинам, песочницам и Stream-Proof сокрытию</h2>");
				html.AppendLine("<p><b>Игрок:</b> " + targetPlayerName + " | <b>Дата проверки:</b> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");
				html.AppendLine("<p><b>Нарушений:</b> " + lblKpiViolations.Text + " | <b>VM:</b> " + lblKpiVm.Text + " | <b>Sandbox:</b> " + lblKpiSandbox.Text + " | <b>Stream-Proof:</b> " + lblKpiStreamProof.Text + "</p>");
				html.AppendLine("<table><tr><th>№</th><th>Категория</th><th>Источник</th><th>Обнаруженный признак</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th></tr>");

				foreach (EvasionVmEvidenceItem it in allItems)
				{
					string cls = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
					html.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td class='{5}'>{6}</td><td>{7}</td><td>{8}</td></tr>",
						it.Index, it.Category, it.SourceArtifact, it.DetectedFeature, it.Status, cls, it.ThreatLevel, it.Verdict, it.TargetRule));
				}
				html.AppendLine("</table></body></html>");

				File.WriteAllText(tempPath, html.ToString(), Encoding.UTF8);
				Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка экспорта HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
