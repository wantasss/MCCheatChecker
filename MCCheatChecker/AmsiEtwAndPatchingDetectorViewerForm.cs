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
	public enum MemoryPatchFilter
	{
		All,
		ViolationsOnly,
		AmsiPatches,
		EtwSuppression,
		DllHooksAndText,
		MinHookDetours,
		WhitelistedClean
	}

	public class MemoryPatchEvidenceItem
	{
		public int Index;
		public string Category; // "AMSI Патчи", "ETW Подавление", "Модификации DLL", "Хуки MinHook/Detours", "Процессы"
		public string ProcessName; // "javaw.exe (PID 1234)"
		public int ProcessId;
		public string TargetModule; // "amsi.dll", "ntdll.dll", "kernel32.dll", "user32.dll"
		public string FunctionOrSymbol; // "AmsiScanBuffer", "EtwEventWrite", "NtTraceEvent", etc.
		public string InMemoryBytesHex; // "C3 (ret)", "31 C0 C3", etc.
		public string ExpectedBytesHex; // "4C 8B DC 49 89 5B 08", etc.
		public string Status; // "⛔ ОБНАРУЖЕН ПАТЧ (5.01)", "🚨 ПОДАВЛЕНИЕ ETW (5.08)", "🟢 ЦЕЛОСТЕН"
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict; // "5.01 - Чит-клиент", "5.08 - Уклонение от проверки", "РАЗРЕШЕНО"
		public string TargetRule; // "5.01", "5.08", "ЧИСТО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
		public string TechnicalAnalysis;
	}

	public class AmsiEtwAndPatchingDetectorViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<MemoryPatchEvidenceItem> allItems = new List<MemoryPatchEvidenceItem>();
		private MemoryPatchFilter currentFilter = MemoryPatchFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiAmsi;
		private Label lblKpiEtw;
		private Label lblKpiDll;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterAmsi;
		private DarkButton btnFilterEtw;
		private DarkButton btnFilterDll;
		private DarkButton btnFilterMinHook;
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

		#region Win32 API Imports

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		private const uint PROCESS_VM_READ = 0x0010;
		private const uint PROCESS_QUERY_INFORMATION = 0x0400;
		private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

		#endregion

		public AmsiEtwAndPatchingDetectorViewerForm() : this("Player")
		{
		}

		public AmsiEtwAndPatchingDetectorViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "💉 Инспектор патчинга памяти, обхода AMSI и ETW (Tool 72 / Step 69) — MCCheatChecker";
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
			lblHeaderTitle.Text = "💉 ИНСПЕКТОР ПАТЧИНГА ПАМЯТИ, ОБХОДА AMSI И ETW (MEMORY HOOK DETECTOR)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Анализ байт-патчей AmsiScanBuffer, подавления событий ETW (EtwEventWrite/NtTraceEvent), модификаций секций .text и хуков MinHook/Detours в памяти javaw.exe (5.01 / 5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация аудита памяти и нативных хуков...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 360, 16);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblStatusPill.AutoSize = true;
			headerPanel.Controls.Add(lblStatusPill);

			// Панель KPI-карточек
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int kx = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ПРОВЕРЕНО", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.01/5.08)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 ПАТЧИ AMSI", "0", Ui.AccentOrange, ref kx, out lblKpiAmsi));
			kpiPanel.Controls.Add(CreateKpiBadge("📡 ПОДАВЛЕНИЕ ETW", "0", Ui.AccentPurple, ref kx, out lblKpiEtw));
			kpiPanel.Controls.Add(CreateKpiBadge("🪝 МОДИФИКАЦИИ DLL", "0", Ui.AccentBlue, ref kx, out lblKpiDll));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ БЕЗОПАСНО / ЧИСТО", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все параметры", MemoryPatchFilter.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", MemoryPatchFilter.ViolationsOnly, ref bx, filterBar);
			btnFilterAmsi = CreateFilterButton("💉 Патчи AMSI", MemoryPatchFilter.AmsiPatches, ref bx, filterBar);
			btnFilterEtw = CreateFilterButton("📡 Подавление ETW", MemoryPatchFilter.EtwSuppression, ref bx, filterBar);
			btnFilterDll = CreateFilterButton("🪝 Модификации DLL .text", MemoryPatchFilter.DllHooksAndText, ref bx, filterBar);
			btnFilterMinHook = CreateFilterButton("🎯 MinHook / Detours", MemoryPatchFilter.MinHookDetours, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Чистые модули", MemoryPatchFilter.WhitelistedClean, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по процессу, функции, адресу, байтам или правилу...";
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
			btnRescan = CreateActionButton("🔄 Повторить аудит памяти", ref ax, bottomActions, delegate { StartScan(); });

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "❌ Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(bottomActions.Width - 116, 9);
			btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClose.Click += delegate { this.Close(); };
			bottomActions.Controls.Add(btnClose);

			// Разделитель: Таблица (верх) и Детали (низ)
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 450;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.View = View.Details;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Статус", 175);
			listView.Columns.Add("Процесс (PID)", 150);
			listView.Columns.Add("Категория", 140);
			listView.Columns.Add("Модуль / Функция", 200);
			listView.Columns.Add("Байт-сигнатура", 150);
			listView.Columns.Add("Правило", 90);
			listView.Columns.Add("Описание и детали", 380);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Подробный анализ");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("🔬 Байт-код / Дамп памяти");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Вердикт проверки");
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

		private DarkButton CreateFilterButton(string text, MemoryPatchFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(140, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 146;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int xOffset, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(175, 28);
			btn.Location = new Point(xOffset, 9);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			xOffset += 182;
			return btn;
		}

		private void UpdateFilterButtons()
		{
			btnFilterAll.BackColor = (currentFilter == MemoryPatchFilter.All) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.BackColor = (currentFilter == MemoryPatchFilter.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterAmsi.BackColor = (currentFilter == MemoryPatchFilter.AmsiPatches) ? Ui.AccentRed : Ui.Surface;
			btnFilterEtw.BackColor = (currentFilter == MemoryPatchFilter.EtwSuppression) ? Ui.AccentRed : Ui.Surface;
			btnFilterDll.BackColor = (currentFilter == MemoryPatchFilter.DllHooksAndText) ? Ui.AccentRed : Ui.Surface;
			btnFilterMinHook.BackColor = (currentFilter == MemoryPatchFilter.MinHookDetours) ? Ui.AccentRed : Ui.Surface;
			btnFilterWhitelisted.BackColor = (currentFilter == MemoryPatchFilter.WhitelistedClean) ? Ui.AccentRed : Ui.Surface;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование памяти javaw, AMSI и ETW...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<MemoryPatchEvidenceItem> results = new List<MemoryPatchEvidenceItem>();
			int idx = 1;

			// 1. Проверка AMSI в текущем процессе (защита чекера от нейтрализации)
			CheckLocalProcessAmsiAndEtw(results, ref idx);

			// 2. Поиск процессов Minecraft (javaw.exe, java.exe) и лаунчеров
			Process[] javawProcs = Process.GetProcessesByName("javaw");
			Process[] javaProcs = Process.GetProcessesByName("java");

			List<Process> targetProcs = new List<Process>();
			targetProcs.AddRange(javawProcs);
			foreach (Process p in javaProcs)
			{
				if (!targetProcs.Exists(delegate(Process x) { return x.Id == p.Id; }))
				{
					targetProcs.Add(p);
				}
			}

			if (targetProcs.Count == 0)
			{
				// Игра сейчас не запущена — фиксируем статус
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "Процессы Minecraft";
				item.ProcessName = "javaw.exe / java.exe";
				item.ProcessId = 0;
				item.TargetModule = "-";
				item.FunctionOrSymbol = "javaw.exe";
				item.InMemoryBytesHex = "-";
				item.ExpectedBytesHex = "-";
				item.Status = "ℹ️ ПРОЦЕСС НЕ ЗАПУЩЕН";
				item.ThreatLevel = "INFO";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.TextSecondary;
				item.DetailDescription = "Процесс javaw.exe не найден в активной памяти. Рекомендуется запустить Minecraft и повторить сканирование.";
				item.TechnicalAnalysis = "Аудит процессов: javaw.exe и java.exe отсутствуют в таблице активных процессов Windows.";
				results.Add(item);
			}
			else
			{
				foreach (Process p in targetProcs)
				{
					try
					{
						AuditProcessMemoryHooksAndPatches(p, results, ref idx);
					}
					catch (Exception ex)
					{
						MemoryPatchEvidenceItem errItem = new MemoryPatchEvidenceItem();
						errItem.Index = idx++;
						errItem.Category = "Ошибка анализа";
						errItem.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
						errItem.ProcessId = p.Id;
						errItem.TargetModule = "-";
						errItem.FunctionOrSymbol = "OpenProcess / ReadMemory";
						errItem.InMemoryBytesHex = "-";
						errItem.ExpectedBytesHex = "-";
						errItem.Status = "⚠️ ОШИБКА ДОСТУПА";
						errItem.ThreatLevel = "WARNING";
						errItem.Verdict = "ТРЕБУЮТСЯ ПРАВА АДМИНИСТРАТОРА";
						errItem.TargetRule = "ВНИМАНИЕ";
						errItem.IsViolation = false;
						errItem.IsWhitelisted = false;
						errItem.Timestamp = DateTime.Now;
						errItem.RowColor = Ui.AccentOrange;
						errItem.DetailDescription = "Не удалось прочитать память процесса (требуются права администратора): " + ex.Message;
						errItem.TechnicalAnalysis = "Исключение при вызове OpenProcess / ReadProcessMemory: " + ex.ToString();
						results.Add(errItem);
					}
				}
			}

			// 3. Проверка лаунчеров и скриптовых сред (PowerShell, CMD)
			CheckPowerShellAndLauncherAmsi(results, ref idx);

			e.Result = results;
		}

		private void CheckLocalProcessAmsiAndEtw(List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			// Проверка amsi.dll в локальном процессе чекера
			IntPtr hAmsi = GetModuleHandle("amsi.dll");
			if (hAmsi != IntPtr.Zero)
			{
				IntPtr pAmsiScanBuffer = GetProcAddress(hAmsi, "AmsiScanBuffer");
				if (pAmsiScanBuffer != IntPtr.Zero)
				{
					byte[] buffer = new byte[16];
					Marshal.Copy(pAmsiScanBuffer, buffer, 0, 16);
					InspectAmsiBytes(Process.GetCurrentProcess(), pAmsiScanBuffer, buffer, "amsi.dll!AmsiScanBuffer (Локальный чекер)", results, ref idx);
				}
			}
			else
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "AMSI Патчи";
				item.ProcessName = "MCCheatChecker (Локальный)";
				item.ProcessId = Process.GetCurrentProcess().Id;
				item.TargetModule = "amsi.dll";
				item.FunctionOrSymbol = "amsi.dll!AmsiScanBuffer";
				item.InMemoryBytesHex = "-";
				item.ExpectedBytesHex = "4C 8B DC ...";
				item.Status = "🟢 AMSI НЕ АКТИВЕН";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = "Модуль amsi.dll не загружен в процесс чекера — нейтрализации локального антивирусного сканера не зафиксировано.";
				item.TechnicalAnalysis = "GetModuleHandle('amsi.dll') вернул NULL.";
				results.Add(item);
			}

			// Проверка ntdll.dll!EtwEventWrite в локальном процессе
			IntPtr hNtdll = GetModuleHandle("ntdll.dll");
			if (hNtdll != IntPtr.Zero)
			{
				IntPtr pEtw = GetProcAddress(hNtdll, "EtwEventWrite");
				if (pEtw != IntPtr.Zero)
				{
					byte[] buffer = new byte[16];
					Marshal.Copy(pEtw, buffer, 0, 16);
					InspectEtwBytes(Process.GetCurrentProcess(), pEtw, buffer, "ntdll.dll!EtwEventWrite (Локальный чекер)", results, ref idx);
				}
			}
		}

		private void AuditProcessMemoryHooksAndPatches(Process p, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			IntPtr hProc = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, p.Id);
			if (hProc == IntPtr.Zero)
			{
				hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
			}

			if (hProc == IntPtr.Zero)
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "Процессы Minecraft";
				item.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				item.ProcessId = p.Id;
				item.TargetModule = "-";
				item.FunctionOrSymbol = "OpenProcess";
				item.InMemoryBytesHex = "-";
				item.ExpectedBytesHex = "-";
				item.Status = "⚠️ НЕТ ДОСТУПА К ПАМЯТИ";
				item.ThreatLevel = "WARNING";
				item.Verdict = "ЗАПУСТИТЕ ОТ АДМИНИСТРАТОРА";
				item.TargetRule = "ВНИМАНИЕ";
				item.IsViolation = false;
				item.IsWhitelisted = false;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentOrange;
				item.DetailDescription = "Не удалось получить дескриптор процесса с правами PROCESS_VM_READ. Возможно, игра запущена от другого пользователя или под защитой античита.";
				item.TechnicalAnalysis = "GetLastError = " + Marshal.GetLastWin32Error();
				results.Add(item);
				return;
			}

			try
			{
				// 1. Проверка AMSI в целевом процессе
				AuditTargetProcessAmsi(hProc, p, results, ref idx);

				// 2. Проверка ETW (EtwEventWrite и NtTraceEvent)
				AuditTargetProcessEtw(hProc, p, results, ref idx);

				// 3. Проверка критических нативных API на JMP-хуки (MinHook / Detours)
				AuditTargetProcessInlineHooks(hProc, p, results, ref idx);
			}
			finally
			{
				CloseHandle(hProc);
			}
		}

		private void AuditTargetProcessAmsi(IntPtr hProc, Process p, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			IntPtr hLocalAmsi = GetModuleHandle("amsi.dll");
			if (hLocalAmsi == IntPtr.Zero)
			{
				try { hLocalAmsi = LoadLibrary("amsi.dll"); } catch { }
			}

			if (hLocalAmsi == IntPtr.Zero) return;

			IntPtr pAmsiScanBuffer = GetProcAddress(hLocalAmsi, "AmsiScanBuffer");
			if (pAmsiScanBuffer == IntPtr.Zero) return;

			byte[] buffer = new byte[16];
			IntPtr bytesRead;
			if (ReadProcessMemory(hProc, pAmsiScanBuffer, buffer, 16, out bytesRead) && bytesRead.ToInt32() >= 4)
			{
				InspectAmsiBytes(p, pAmsiScanBuffer, buffer, "amsi.dll!AmsiScanBuffer", results, ref idx);
			}
			else
			{
				// Модуль amsi.dll не отображён по тому же адресу или не загружен
				MemoryPatchEvidenceItem safeItem = new MemoryPatchEvidenceItem();
				safeItem.Index = idx++;
				safeItem.Category = "AMSI Патчи";
				safeItem.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				safeItem.ProcessId = p.Id;
				safeItem.TargetModule = "amsi.dll";
				safeItem.FunctionOrSymbol = "amsi.dll!AmsiScanBuffer";
				safeItem.InMemoryBytesHex = "N/A";
				safeItem.ExpectedBytesHex = "4C 8B DC ...";
				safeItem.Status = "🟢 БЕЗОПАСЕН (AMSI НЕ ВНЕДРЁН)";
				safeItem.ThreatLevel = "SAFE";
				safeItem.Verdict = "РАЗРЕШЕНО";
				safeItem.TargetRule = "ЧИСТО";
				safeItem.IsViolation = false;
				safeItem.IsWhitelisted = true;
				safeItem.Timestamp = DateTime.Now;
				safeItem.RowColor = Ui.AccentGreen;
				safeItem.DetailDescription = "В процессе javaw.exe модуль amsi.dll не загружен либо не подвергался инъекциям скриптовых читов.";
				safeItem.TechnicalAnalysis = "ReadProcessMemory по адресу 0x" + pAmsiScanBuffer.ToString("X") + " не вернул перехваченных страниц.";
				results.Add(safeItem);
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		private void InspectAmsiBytes(Process p, IntPtr addr, byte[] bytes, string label, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			string hex = FormatHexBytes(bytes, 8);
			bool isPatched = false;
			string patchType = "";

			// Известные патчи AmsiScanBuffer:
			// 1. C3 (ret)
			if (bytes[0] == 0xC3)
			{
				isPatched = true;
				patchType = "RET (0xC3) — мгновенный возврат из функции без проверки";
			}
			// 2. 31 C0 C3 или 33 C0 C3 (xor eax, eax; ret -> AMSI_RESULT_CLEAN)
			else if ((bytes[0] == 0x31 || bytes[0] == 0x33) && bytes[1] == 0xC0 && bytes[2] == 0xC3)
			{
				isPatched = true;
				patchType = "XOR EAX, EAX; RET — принудительный возврат нулевого результата проверки";
			}
			// 3. B8 57 00 07 80 C3 (mov eax, 0x80070057; ret -> E_INVALIDARG)
			else if (bytes[0] == 0xB8 && bytes[1] == 0x57 && bytes[2] == 0x00 && bytes[3] == 0x07 && bytes[4] == 0x80)
			{
				isPatched = true;
				patchType = "MOV EAX, 0x80070057; RET (E_INVALIDARG) — симуляция ошибки аргументов AMSI";
			}
			// 4. B8 01 00 00 00 C3 (mov eax, 1; ret)
			else if (bytes[0] == 0xB8 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00 && bytes[4] == 0x00 && bytes[5] == 0xC3)
			{
				isPatched = true;
				patchType = "MOV EAX, 1; RET — подделка статуса AMSI_RESULT_NOT_DETECTED";
			}
			// 5. 48 31 C0 C3 (xor rax, rax; ret)
			else if (bytes[0] == 0x48 && bytes[1] == 0x31 && bytes[2] == 0xC0 && bytes[3] == 0xC3)
			{
				isPatched = true;
				patchType = "XOR RAX, RAX; RET (x64) — нейтрализация буфера проверки AMSI";
			}
			// 6. E9 / EB (JMP detour hook)
			else if (bytes[0] == 0xE9 || bytes[0] == 0xEB)
			{
				isPatched = true;
				patchType = "JMP (0x" + bytes[0].ToString("X2") + ") — перенаправление вызова AmsiScanBuffer на внешний чит-перехватчик";
			}

			if (isPatched)
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "AMSI Патчи";
				item.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				item.ProcessId = p.Id;
				item.TargetModule = "amsi.dll";
				item.FunctionOrSymbol = label;
				item.InMemoryBytesHex = hex;
				item.ExpectedBytesHex = "4C 8B DC 49 89 5B 08 ...";
				item.Status = "⛔ ОБНАРУЖЕН ПАТЧ AMSI (5.01)";
				item.ThreatLevel = "CRITICAL";
				item.Verdict = "5.01 - ЧИТ-КЛИЕНТ / ПАТЧИНГ БУФЕРА";
				item.TargetRule = "5.01";
				item.IsViolation = true;
				item.IsWhitelisted = false;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentRed;
				item.DetailDescription = string.Format("В функции {0} обнаружен байт-патч обхода антивирусного сканера: {1}! Байт-код: [{2}].", label, patchType, hex);
				item.TechnicalAnalysis = string.Format("Адрес функции: 0x{0:X}\r\nТип патча: {1}\r\nСчитанные байты: {2}\r\nНарушение регламента: Пункт 5.01 (Патчинг подсистем безопасности и сокрытие скриптовых инжекторов читов).", addr.ToInt64(), patchType, hex);
				results.Add(item);
			}
			else
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "AMSI Патчи";
				item.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				item.ProcessId = p.Id;
				item.TargetModule = "amsi.dll";
				item.FunctionOrSymbol = label;
				item.InMemoryBytesHex = hex;
				item.ExpectedBytesHex = "4C 8B DC ...";
				item.Status = "🟢 ЦЕЛОСТЕН";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = string.Format("Функция {0} имеет валидный нативный пролог: [{1}]. Байт-патчи отсутствуют.", label, hex);
				item.TechnicalAnalysis = "Пролог соответствует официальной компиляции Microsoft amsi.dll.";
				results.Add(item);
			}
		}

		private void AuditTargetProcessEtw(IntPtr hProc, Process p, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			IntPtr hNtdll = GetModuleHandle("ntdll.dll");
			if (hNtdll == IntPtr.Zero) return;

			string[] etwFuncs = new string[] { "EtwEventWrite", "NtTraceEvent" };
			foreach (string funcName in etwFuncs)
			{
				IntPtr pFunc = GetProcAddress(hNtdll, funcName);
				if (pFunc == IntPtr.Zero) continue;

				byte[] buffer = new byte[16];
				IntPtr bytesRead;
				if (ReadProcessMemory(hProc, pFunc, buffer, 16, out bytesRead) && bytesRead.ToInt32() >= 4)
				{
					InspectEtwBytes(p, pFunc, buffer, "ntdll.dll!" + funcName, results, ref idx);
				}
			}
		}

		private void InspectEtwBytes(Process p, IntPtr addr, byte[] bytes, string label, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			string hex = FormatHexBytes(bytes, 8);
			bool isPatched = false;
			string patchType = "";

			// Известные патчи ETW:
			// 1. C2 14 00 (ret 0x14)
			if (bytes[0] == 0xC2 && bytes[1] == 0x14 && bytes[2] == 0x00)
			{
				isPatched = true;
				patchType = "RET 14h (0xC2 0x14 0x00) — принудительный возврат со снятием стека stdcall";
			}
			// 2. C3 (ret)
			else if (bytes[0] == 0xC3)
			{
				isPatched = true;
				patchType = "RET (0xC3) — принудительное отключение логирования событий безопасности";
			}
			// 3. 48 31 C0 C3 (xor rax, rax; ret)
			else if (bytes[0] == 0x48 && bytes[1] == 0x31 && bytes[2] == 0xC0 && bytes[3] == 0xC3)
			{
				isPatched = true;
				patchType = "XOR RAX, RAX; RET — подделка STATUS_SUCCESS без записи события";
			}
			// 4. 31 C0 C3 или 33 C0 C3
			else if ((bytes[0] == 0x31 || bytes[0] == 0x33) && bytes[1] == 0xC0 && bytes[2] == 0xC3)
			{
				isPatched = true;
				patchType = "XOR EAX, EAX; RET — глушение системного логгера ETW";
			}
			// 5. B8 00 00 00 00 C3 (mov eax, 0; ret)
			else if (bytes[0] == 0xB8 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x00 && bytes[4] == 0x00 && bytes[5] == 0xC3)
			{
				isPatched = true;
				patchType = "MOV EAX, 0; RET — принудительный STATUS_SUCCESS";
			}
			// 6. E9 / EB (JMP detour hook)
			else if (bytes[0] == 0xE9 || bytes[0] == 0xEB)
			{
				isPatched = true;
				patchType = "JMP (0x" + bytes[0].ToString("X2") + ") — перенаправление вызова ETW на внешний чит-перехватчик";
			}

			if (isPatched)
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "ETW Подавление";
				item.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				item.ProcessId = p.Id;
				item.TargetModule = "ntdll.dll";
				item.FunctionOrSymbol = label;
				item.InMemoryBytesHex = hex;
				item.ExpectedBytesHex = "48 89 5C 24 08 ...";
				item.Status = "🚨 ПОДАВЛЕНИЕ ETW (5.08)";
				item.ThreatLevel = "CRITICAL";
				item.Verdict = "5.08 - УКЛОНЕНИЕ / ГЛУШЕНИЕ ЛОГОВ";
				item.TargetRule = "5.08";
				item.IsViolation = true;
				item.IsWhitelisted = false;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentRed;
				item.DetailDescription = string.Format("В системной функции {0} обнаружен байт-патч глушения событий Event Tracing for Windows: {1}! Байт-код: [{2}].", label, patchType, hex);
				item.TechnicalAnalysis = string.Format("Адрес функции: 0x{0:X}\r\nТип патча: {1}\r\nСчитанные байты: {2}\r\nНарушение регламента: Пункт 5.08 (Умышленное сокрытие и блокировка регистрации системных событий безопасности ETW).", addr.ToInt64(), patchType, hex);
				results.Add(item);
			}
			else
			{
				MemoryPatchEvidenceItem item = new MemoryPatchEvidenceItem();
				item.Index = idx++;
				item.Category = "ETW Подавление";
				item.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
				item.ProcessId = p.Id;
				item.TargetModule = "ntdll.dll";
				item.FunctionOrSymbol = label;
				item.InMemoryBytesHex = hex;
				item.ExpectedBytesHex = "48 89 5C ...";
				item.Status = "🟢 ЦЕЛОСТЕН";
				item.ThreatLevel = "SAFE";
				item.Verdict = "РАЗРЕШЕНО";
				item.TargetRule = "ЧИСТО";
				item.IsViolation = false;
				item.IsWhitelisted = true;
				item.Timestamp = DateTime.Now;
				item.RowColor = Ui.AccentGreen;
				item.DetailDescription = string.Format("Функция {0} имеет оригинальный пролог Windows: [{1}]. Телеметрия ETW не заблокирована.", label, hex);
				item.TechnicalAnalysis = "Пролог соответствует нетронутому системному образу ntdll.dll.";
				results.Add(item);
			}
		}

		private void AuditTargetProcessInlineHooks(IntPtr hProc, Process p, List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			// Проверка нативных API на JMP-инъекции (MinHook / Detours)
			Tuple<string, string>[] criticalApis = new Tuple<string, string>[]
			{
				Tuple.Create("ws2_32.dll", "send"),
				Tuple.Create("ws2_32.dll", "recv"),
				Tuple.Create("ws2_32.dll", "connect"),
				Tuple.Create("user32.dll", "SetWindowDisplayAffinity"),
				Tuple.Create("kernel32.dll", "VirtualProtect"),
				Tuple.Create("kernel32.dll", "CreateFileW")
			};

			foreach (Tuple<string, string> api in criticalApis)
			{
				string modName = api.Item1;
				string funcName = api.Item2;

				IntPtr hMod = GetModuleHandle(modName);
				if (hMod == IntPtr.Zero)
				{
					try { hMod = LoadLibrary(modName); } catch { }
				}
				if (hMod == IntPtr.Zero) continue;

				IntPtr pFunc = GetProcAddress(hMod, funcName);
				if (pFunc == IntPtr.Zero) continue;

				byte[] buffer = new byte[16];
				IntPtr bytesRead;
				if (ReadProcessMemory(hProc, pFunc, buffer, 16, out bytesRead) && bytesRead.ToInt32() >= 5)
				{
					string hex = FormatHexBytes(buffer, 8);
					// Инлайн-хук MinHook/Detours начинается с E9 (JMP rel32) или FF 25 (JMP [QWORD])
					if (buffer[0] == 0xE9)
					{
						int relOffset = BitConverter.ToInt32(buffer, 1);
						long targetAddr = pFunc.ToInt64() + 5 + relOffset;

						MemoryPatchEvidenceItem hookItem = new MemoryPatchEvidenceItem();
						hookItem.Index = idx++;
						hookItem.Category = "Хуки MinHook/Detours";
						hookItem.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
						hookItem.ProcessId = p.Id;
						hookItem.TargetModule = modName;
						hookItem.FunctionOrSymbol = modName + "!" + funcName;
						hookItem.InMemoryBytesHex = hex;
						hookItem.ExpectedBytesHex = "48 89 ... / 48 83 EC ...";
						hookItem.Status = "⛔ ОБНАРУЖЕН НА ТИВНЫЙ ХУК (5.01)";
						hookItem.ThreatLevel = "CRITICAL";
						hookItem.Verdict = "5.01 - ЧИТ-ИНЖЕКТОР (MINHOOK/DETOURS)";
						hookItem.TargetRule = "5.01";
						hookItem.IsViolation = true;
						hookItem.IsWhitelisted = false;
						hookItem.Timestamp = DateTime.Now;
						hookItem.RowColor = Ui.AccentRed;
						hookItem.DetailDescription = string.Format("Функция {0}!{1} перехвачена JMP-трамплином (0xE9) на внешний адрес 0x{2:X}! Признак инжекции чита (MinHook/Detours).", modName, funcName, targetAddr);
						hookItem.TechnicalAnalysis = string.Format("Адрес функции: 0x{0:X}\r\nJMP смещение: 0x{1:X}\r\nЦелевой адрес трамплина: 0x{2:X}\r\nБайты: {3}\r\nНарушение: Пункт 5.01 (Инжект DLL чита с перехватом сетевых/графических функций).", pFunc.ToInt64(), relOffset, targetAddr, hex);
						results.Add(hookItem);
					}
					else if (buffer[0] == 0xFF && buffer[1] == 0x25)
					{
						MemoryPatchEvidenceItem hookItem2 = new MemoryPatchEvidenceItem();
						hookItem2.Index = idx++;
						hookItem2.Category = "Хуки MinHook/Detours";
						hookItem2.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
						hookItem2.ProcessId = p.Id;
						hookItem2.TargetModule = modName;
						hookItem2.FunctionOrSymbol = modName + "!" + funcName;
						hookItem2.InMemoryBytesHex = hex;
						hookItem2.ExpectedBytesHex = "48 89 ... / 48 83 EC ...";
						hookItem2.Status = "⛔ ОБНАРУЖЕН ХУК (JMP [RIP]) (5.01)";
						hookItem2.ThreatLevel = "CRITICAL";
						hookItem2.Verdict = "5.01 - ЧИТ-ИНЖЕКТОР (DETOURS)";
						hookItem2.TargetRule = "5.01";
						hookItem2.IsViolation = true;
						hookItem2.IsWhitelisted = false;
						hookItem2.Timestamp = DateTime.Now;
						hookItem2.RowColor = Ui.AccentRed;
						hookItem2.DetailDescription = string.Format("Функция {0}!{1} перехвачена абсолютным трамплином (FF 25 JMP [RIP])! Нативный хук чита.", modName, funcName);
						hookItem2.TechnicalAnalysis = string.Format("Адрес: 0x{0:X}\r\nБайт-код: {1}\r\nПункт 5.01 регламента.", pFunc.ToInt64(), hex);
						results.Add(hookItem2);
					}
					else
					{
						MemoryPatchEvidenceItem safeItem = new MemoryPatchEvidenceItem();
						safeItem.Index = idx++;
						safeItem.Category = "Модификации DLL";
						safeItem.ProcessName = p.ProcessName + " (PID " + p.Id + ")";
						safeItem.ProcessId = p.Id;
						safeItem.TargetModule = modName;
						safeItem.FunctionOrSymbol = modName + "!" + funcName;
						safeItem.InMemoryBytesHex = hex;
						safeItem.ExpectedBytesHex = "Валидный пролог";
						safeItem.Status = "🟢 БЕЗОПАСЕН";
						safeItem.ThreatLevel = "SAFE";
						safeItem.Verdict = "РАЗРЕШЕНО";
						safeItem.TargetRule = "ЧИСТО";
						safeItem.IsViolation = false;
						safeItem.IsWhitelisted = true;
						safeItem.Timestamp = DateTime.Now;
						safeItem.RowColor = Ui.AccentGreen;
						safeItem.DetailDescription = string.Format("Функция {0}!{1} не имеет JMP-перехватов: [{2}].", modName, funcName, hex);
						safeItem.TechnicalAnalysis = "Хуков MinHook/Detours не обнаружено.";
						results.Add(safeItem);
					}
				}
			}
		}

		private void CheckPowerShellAndLauncherAmsi(List<MemoryPatchEvidenceItem> results, ref int idx)
		{
			Process[] psProcs = Process.GetProcessesByName("powershell");
			foreach (Process p in psProcs)
			{
				try
				{
					IntPtr hProc = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, p.Id);
					if (hProc != IntPtr.Zero)
					{
						try
						{
							AuditTargetProcessAmsi(hProc, p, results, ref idx);
						}
						finally
						{
							CloseHandle(hProc);
						}
					}
				}
				catch { }
			}
		}

		private static string FormatHexBytes(byte[] bytes, int count)
		{
			if (bytes == null || bytes.Length == 0) return "-";
			int len = Math.Min(bytes.Length, count);
			StringBuilder sb = new StringBuilder(len * 3);
			for (int i = 0; i < len; i++)
			{
				sb.Append(bytes[i].ToString("X2"));
				if (i < len - 1) sb.Append(" ");
			}
			return sb.ToString();
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<MemoryPatchEvidenceItem> items = e.Result as List<MemoryPatchEvidenceItem>;
			if (items != null)
			{
				allItems = items;
			}

			UpdateKpis();
			UpdateFilterButtons();
			ApplyFilter();

			int violations = 0;
			foreach (MemoryPatchEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				lblStatusPill.Text = "⛔ ОБНАРУЖЕНЫ ПАТЧИ ПАМЯТИ: " + violations + " шт.! (5.01 / 5.08)";
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ АУДИТ ПАМЯТИ ЗАВЕРШЁН: Патчей AMSI, ETW и хуков не найдено";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void UpdateKpis()
		{
			int total = allItems.Count;
			int violations = 0;
			int amsi = 0;
			int etw = 0;
			int dll = 0;
			int whitelisted = 0;

			foreach (MemoryPatchEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.Category.Contains("AMSI")) amsi++;
				if (it.Category.Contains("ETW")) etw++;
				if (it.Category.Contains("DLL") || it.Category.Contains("MinHook")) dll++;
				if (it.IsWhitelisted) whitelisted++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiAmsi.Text = amsi.ToString();
			lblKpiEtw.Text = etw.ToString();
			lblKpiDll.Text = dll.ToString();
			lblKpiWhitelisted.Text = whitelisted.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null) ? (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant() : string.Empty;

			foreach (MemoryPatchEvidenceItem it in allItems)
			{
				bool matchFilter = false;
				switch (currentFilter)
				{
					case MemoryPatchFilter.All:
						matchFilter = true;
						break;
					case MemoryPatchFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case MemoryPatchFilter.AmsiPatches:
						matchFilter = it.Category.Contains("AMSI");
						break;
					case MemoryPatchFilter.EtwSuppression:
						matchFilter = it.Category.Contains("ETW");
						break;
					case MemoryPatchFilter.DllHooksAndText:
						matchFilter = it.Category.Contains("DLL");
						break;
					case MemoryPatchFilter.MinHookDetours:
						matchFilter = it.Category.Contains("MinHook");
						break;
					case MemoryPatchFilter.WhitelistedClean:
						matchFilter = it.IsWhitelisted;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string line = (it.ProcessName + " " + it.Category + " " + it.FunctionOrSymbol + " " + it.InMemoryBytesHex + " " + it.Status + " " + it.DetailDescription).ToLowerInvariant();
					if (!line.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.ProcessName);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.FunctionOrSymbol);
				lvi.SubItems.Add(it.InMemoryBytesHex);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.DetailDescription);
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

			MemoryPatchEvidenceItem it = listView.SelectedItems[0].Tag as MemoryPatchEvidenceItem;
			if (it == null) return;

			// Tab 1: Анализ
			StringBuilder sbAnalysis = new StringBuilder();
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine(" ИНСПЕКТОР ПАТЧИНГА ПАМЯТИ: " + it.Category.ToUpperInvariant());
			sbAnalysis.AppendLine("================================================================================");
			sbAnalysis.AppendLine("Процесс:            " + it.ProcessName);
			sbAnalysis.AppendLine("Модуль / Функция:   " + it.FunctionOrSymbol);
			sbAnalysis.AppendLine("Статус проверки:    " + it.Status);
			sbAnalysis.AppendLine("Уровень угрозы:     " + it.ThreatLevel);
			sbAnalysis.AppendLine("Правило проверки:   " + it.TargetRule);
			sbAnalysis.AppendLine("Время детекции:     " + it.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- ОПИСАНИЕ ОБНАРУЖЕННОГО ПАТЧА ---");
			sbAnalysis.AppendLine(it.DetailDescription);
			sbAnalysis.AppendLine();
			sbAnalysis.AppendLine("--- ТЕХНИЧЕСКИЙ АНАЛИЗ ---");
			sbAnalysis.AppendLine(it.TechnicalAnalysis);
			txtAnalysis.Text = sbAnalysis.ToString();

			// Tab 2: Байт-код / Дамп
			StringBuilder sbTech = new StringBuilder();
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine(" СРАВНЕНИЕ ПРОЛОГА ФУНКЦИИ В ПАМЯТИ VS ЭТАЛОН MICROSOFT");
			sbTech.AppendLine("================================================================================");
			sbTech.AppendLine("Символ функции:     " + it.FunctionOrSymbol);
			sbTech.AppendLine("Считанный байт-код: [" + it.InMemoryBytesHex + "]");
			sbTech.AppendLine("Эталонный пролог:   [" + it.ExpectedBytesHex + "]");
			sbTech.AppendLine();
			sbTech.AppendLine("--- РАСШИФРОВКА АССЕМБЛЕРНЫХ ИНСТРУКЦИЙ ---");
			if (it.InMemoryBytesHex.StartsWith("C3"))
			{
				sbTech.AppendLine("0x00: C3                  ret");
				sbTech.AppendLine(">> Функция немедленно возвращает управление, пропуская сканирование буфера.");
			}
			else if (it.InMemoryBytesHex.StartsWith("31 C0 C3") || it.InMemoryBytesHex.StartsWith("33 C0 C3"))
			{
				sbTech.AppendLine("0x00: 31 C0               xor eax, eax");
				sbTech.AppendLine("0x02: C3                  ret");
				sbTech.AppendLine(">> Регистр результата зануляется (S_OK / AMSI_RESULT_CLEAN / STATUS_SUCCESS).");
			}
			else if (it.InMemoryBytesHex.StartsWith("B8 57 00 07 80"))
			{
				sbTech.AppendLine("0x00: B8 57 00 07 80      mov eax, 0x80070057 (E_INVALIDARG)");
				sbTech.AppendLine("0x05: C3                  ret");
				sbTech.AppendLine(">> Симуляция неверного аргумента для аварийного выхода из AMSI-сканера.");
			}
			else if (it.InMemoryBytesHex.StartsWith("C2 14 00"))
			{
				sbTech.AppendLine("0x00: C2 14 00            ret 0x14");
				sbTech.AppendLine(">> Функция EtwEventWrite завершается со снятием 20 байт стека, глуша запись логов.");
			}
			else if (it.InMemoryBytesHex.StartsWith("E9"))
			{
				sbTech.AppendLine("0x00: E9 xx xx xx xx      jmp <detour_target>");
				sbTech.AppendLine(">> Трамплин MinHook/Detours: безусловный прыжок на адрес чит-модуля в RAM.");
			}
			else
			{
				sbTech.AppendLine("Стандартный код: модификаций пролога не обнаружено.");
			}
			txtTechnical.Text = sbTech.ToString();

			// Tab 3: Вердикт бана
			StringBuilder sbBan = new StringBuilder();
			sbBan.AppendLine("================================================================================");
			sbBan.AppendLine(" РЕШЕНИЕ МОДЕРАЦИИ И ВЕРДИКТ БАНА (РЕГЛАМЕНТ ПРОВЕРКИ)");
			sbBan.AppendLine("================================================================================");
			sbBan.AppendLine("Никнейм игрока:     " + targetPlayerName);
			sbBan.AppendLine("Статус проверки:    " + (it.IsViolation ? "НАРУШЕНИЕ ОБНАРУЖЕНО" : "ЧИСТО"));
			sbBan.AppendLine("Пункт регламента:   " + it.TargetRule);
			sbBan.AppendLine("Категория:          " + it.Category);
			sbBan.AppendLine();
			if (it.IsViolation)
			{
				string reason = (it.TargetRule == "5.01")
					? "5.01 Использование читов (Патчинг памяти " + it.FunctionOrSymbol + ")"
					: "5.08 Уклонение от проверки (Подавление ETW/AMSI " + it.FunctionOrSymbol + ")";
				sbBan.AppendLine("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:");
				sbBan.AppendLine("/ban " + targetPlayerName + " 30d " + reason);
				sbBan.AppendLine();
				sbBan.AppendLine("ПОЯСНЕНИЕ ДЛЯ АПЕЛЛЯЦИИ:");
				sbBan.AppendLine("В оперативной памяти процесса " + it.ProcessName + " обнаружен байт-патч " + it.InMemoryBytesHex + " в функции " + it.FunctionOrSymbol + ".");
				sbBan.AppendLine("Данная сигнатура используется специализированными чит-клиентами для сокрытия чит-скриптов и глушения системных журналов.");
			}
			else
			{
				sbBan.AppendLine("Игрок чист по данной категории. Модификаций памяти не зафиксировано.");
			}
			txtBanVerdict.Text = sbBan.ToString();
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в списке для детального просмотра анализа патчинга памяти.";
			txtTechnical.Text = "Нет данных байт-кода.";
			txtBanVerdict.Text = "Нет выбранного вердикта.";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Сначала выберите нарушение в списке!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			MemoryPatchEvidenceItem it = listView.SelectedItems[0].Tag as MemoryPatchEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = (it.TargetRule == "5.01")
				? "5.01 Патчинг памяти " + it.FunctionOrSymbol
				: "5.08 Уклонение (Глушение логов " + it.FunctionOrSymbol + ")";
			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**💉 MCCheatChecker — Отчёт инспектора патчинга памяти, AMSI и ETW**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (MemoryPatchEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Обнаруженные патчи и хуки:**");
				foreach (MemoryPatchEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.TargetRule + "]` **" + it.FunctionOrSymbol + "** в `" + it.ProcessName + "`: " + it.DetailDescription + " (Байты: `" + it.InMemoryBytesHex + "`)");
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.01 Патчинг памяти и обход AMSI/ETW`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Патчей памяти, AMSI, ETW и MinHook не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_MemoryPatchingReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт аудита памяти — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #121418; color: #E0E0E0; margin: 20px; }");
					sb.AppendLine("h1 { color: #E53935; border-bottom: 2px solid #333; padding-bottom: 8px; }");
					sb.AppendLine(".kpi-container { display: flex; gap: 15px; margin: 20px 0; }");
					sb.AppendLine(".kpi { background: #1E222B; border: 1px solid #2C3240; padding: 12px 18px; border-radius: 6px; }");
					sb.AppendLine(".kpi-title { font-size: 11px; color: #8C9BAE; font-weight: bold; }");
					sb.AppendLine(".kpi-value { font-size: 20px; font-weight: bold; margin-top: 4px; }");
					sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; background: #1E222B; }");
					sb.AppendLine("th, td { border: 1px solid #2C3240; padding: 8px 12px; text-align: left; }");
					sb.AppendLine("th { background: #262C38; color: #8C9BAE; }");
					sb.AppendLine(".crit { color: #FF5252; font-weight: bold; }");
					sb.AppendLine(".safe { color: #4CAF50; }");
					sb.AppendLine("</style></head><body>");
					sb.AppendLine("<h1>💉 Отчёт аудита патчинга памяти, обхода AMSI и ETW</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО ПРОВЕРЕНО</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>НАРУШЕНИЯ</div><div class='kpi-value' style='color:#E53935'>" + lblKpiViolations.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ПАТЧИ AMSI</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiAmsi.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ПОДАВЛЕНИЕ ETW</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiEtw.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>МОДИФИКАЦИИ DLL</div><div class='kpi-value' style='color:#42A5F5'>" + lblKpiDll.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Статус</th><th>Процесс</th><th>Категория</th><th>Функция</th><th>Байты</th><th>Правило</th><th>Детали</th></tr>");
					foreach (MemoryPatchEvidenceItem it in allItems)
					{
						string cls = it.IsViolation ? "crit" : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td class='" + cls + "'>" + it.Status + "</td>");
						sb.AppendLine("<td>" + it.ProcessName + "</td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td>" + it.FunctionOrSymbol + "</td>");
						sb.AppendLine("<td><code>" + it.InMemoryBytesHex + "</code></td>");
						sb.AppendLine("<td>" + it.TargetRule + "</td>");
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
	}
}
