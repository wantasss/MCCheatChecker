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

namespace MCCheatChecker
{
	public enum HollowingFilterMode
	{
		All,
		HollowingOnly,
		UnbackedRwx,
		PeHeaderPresent,
		MinecraftOnly,
		WhitelistedOnly
	}

	public class HollowingMemoryItem
	{
		public string ProcessName;
		public int ProcessId;
		public string BaseAddress;
		public long RegionSizeBytes;
		public string RegionSizeFormatted;
		public string State;
		public string Protect;
		public string MemoryType;
		public string ThreatLevel; // "CRITICAL", "WARNING", "INFO"
		public string DetectionReason;
		public string SignaturesFound;
		public string TargetRule; // "5.01" or "5.08"
		public bool HasPeHeader;
		public bool IsUnbacked;
		public bool IsMinecraft;
		public bool IsWhitelisted;
		public string DumpSnippet;
	}

	public class ProcessHollowingAndInjectedMemoryViewerForm : Form
	{
		#region Win32 API
		private const int PROCESS_QUERY_INFORMATION = 0x0400;
		private const int PROCESS_VM_READ = 0x0010;

		private const uint MEM_COMMIT = 0x1000;
		private const uint MEM_RESERVE = 0x2000;
		private const uint MEM_FREE = 0x10000;

		private const uint MEM_PRIVATE = 0x20000;
		private const uint MEM_MAPPED = 0x40000;
		private const uint MEM_IMAGE = 0x1000000;

		private const uint PAGE_NOACCESS = 0x01;
		private const uint PAGE_READONLY = 0x02;
		private const uint PAGE_READWRITE = 0x04;
		private const uint PAGE_WRITECOPY = 0x08;
		private const uint PAGE_EXECUTE = 0x10;
		private const uint PAGE_EXECUTE_READ = 0x20;
		private const uint PAGE_EXECUTE_READWRITE = 0x40;
		private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
		private const uint PAGE_GUARD = 0x100;

		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORY_BASIC_INFORMATION64
		{
			public ulong BaseAddress;
			public ulong AllocationBase;
			public uint AllocationProtect;
			public uint __alignment1;
			public ulong RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
			public uint __alignment2;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORY_BASIC_INFORMATION32
		{
			public uint BaseAddress;
			public uint AllocationBase;
			public uint AllocationProtect;
			public uint RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll", SetLastError = true, EntryPoint = "VirtualQueryEx")]
		private static extern IntPtr VirtualQueryEx32(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
		#endregion

		private readonly string targetPlayerName;
		private List<HollowingMemoryItem> allItems = new List<HollowingMemoryItem>();
		private HollowingFilterMode currentFilter = HollowingFilterMode.All;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblKpiTotalProcesses;
		private Label lblKpiExecPages;
		private Label lblKpiUnbackedRwx;
		private Label lblKpiHollowingFound;
		private Label lblKpiWarnings;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterHollowing;
		private DarkButton btnFilterUnbackedRwx;
		private DarkButton btnFilterPeHeader;
		private DarkButton btnFilterMinecraft;
		private DarkButton btnFilterWhitelisted;

		private DarkTextBox searchBox;
		private DarkListView listView;
		private TabControl detailTabs;
		private DarkTextBoxMultiline txtAnalysis;
		private DarkTextBoxMultiline txtHexDump;
		private DarkTextBoxMultiline txtBanVerdict;

		private DarkButton btnCopyBanCmd;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		private static readonly string[] WhitelistedProcessNames = new string[]
		{
			"anydesk", "rudesktop", "opautoclicker", "op auto clicker", "speedautoclicker",
			"tinytask", "x-mouse", "xmouse", "autohotkey", "clickermann", "keyran"
		};

		private static readonly string[] TargetSystemProcessNames = new string[]
		{
			"explorer", "svchost", "conhost", "dwm", "cmd", "powershell", "notepad",
			"javaw", "java", "Minecraft.GpuProcess", "taskhostw", "dllhost", "RuntimeBroker", "ctfmon", "lsass"
		};

		private static readonly Regex CheatSignatureRegex = new Regex(
			@"(?i)\b(vape|celestial|expensive|nursultan|akrien|kura|minced|slinky|drip|flint|boberware|rockstar|wild|delta|zamorozka|impact|wurst|aristois|meteor|liquidbounce|inertia|future|rusherhack|killaura|aimbot|reach|hitbox|freecam|esp|velocity|nofall|jesus|step|fastplace|timer|hclip|vclip|baritone)\b",
			RegexOptions.Compiled);

		public ProcessHollowingAndInjectedMemoryViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			ScanProcessesAndMemory();
		}

		private void InitializeComponent()
		{
			this.Text = "🧬 Детектор полого внедрения процессов и неразмеченной памяти (Process Hollowing) — MCCheatChecker";
			this.Size = new Size(1300, 780);
			this.MinimumSize = new Size(1000, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			// Панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🧬 ДЕТЕКТОР ПОЛОГО ВНЕДРЕНИЯ ПРОЦЕССОВ И ПАМЯТИ (PROCESS HOLLOWING & RUNPE)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Низкоуровневый аудит виртуальной памяти (VirtualQueryEx): выявление исполняемых страниц без DLL на диске, заголовков PE (MZ) и скрытых инжекций читов (5.01 / 5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			// Панель карточек KPI
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int kx = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ПРОЦЕССОВ ИЗУЧЕНО", "0", Ui.TextPrimary, ref kx, out lblKpiTotalProcesses));
			kpiPanel.Controls.Add(CreateKpiBadge("ИСПОЛНЯЕМЫХ СТРАНИЦ", "0", Ui.AccentBlue, ref kx, out lblKpiExecPages));
			kpiPanel.Controls.Add(CreateKpiBadge("НЕРАЗМЕЧЕННАЯ RWX ПАМЯТЬ", "0", Ui.AccentOrange, ref kx, out lblKpiUnbackedRwx));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 ПОЛОЕ ВНЕДРЕНИЕ (5.01)", "0", Ui.AccentRed, ref kx, out lblKpiHollowingFound));
			kpiPanel.Controls.Add(CreateKpiBadge("⚠️ ПОДОЗРИТЕЛЬНЫЕ РЕГИОНЫ", "0", Ui.AccentOrange, ref kx, out lblKpiWarnings));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ БЕЛЫЙ СПИСОК (JIT/SS)", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все страницы", HollowingFilterMode.All, ref bx, filterBar);
			btnFilterHollowing = CreateFilterButton("🚨 Внедрения RunPE (5.01)", HollowingFilterMode.HollowingOnly, ref bx, filterBar);
			btnFilterUnbackedRwx = CreateFilterButton("⚠️ Неразмеченная RWX", HollowingFilterMode.UnbackedRwx, ref bx, filterBar);
			btnFilterPeHeader = CreateFilterButton("🧬 Заголовок MZ в ОЗУ", HollowingFilterMode.PeHeaderPresent, ref bx, filterBar);
			btnFilterMinecraft = CreateFilterButton("☕ Minecraft javaw.exe", HollowingFilterMode.MinecraftOnly, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Белый список (JIT)", HollowingFilterMode.WhitelistedOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "Поиск по процессу, адресу, защите или сигнатуре...";
			searchBox.Size = new Size(260, 26);
			searchBox.Location = new Point(filterBar.Width - 280, 8);
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
			btnRescan = CreateActionButton("🔄 Пересканировать память", ref ax, bottomActions, delegate { ScanProcessesAndMemory(); });

			// Разделитель: Список сверху, Детали снизу
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 330;
			split.BackColor = Ui.Border;
			this.Controls.Add(split);

			// Список ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Columns.Add("Процесс (PID)", 170);
			listView.Columns.Add("Базовый адрес", 150);
			listView.Columns.Add("Размер", 90);
			listView.Columns.Add("Защита ОЗУ", 120);
			listView.Columns.Add("Тип памяти", 110);
			listView.Columns.Add("Угроза", 100);
			listView.Columns.Add("Причина детекции", 290);
			listView.Columns.Add("Найденные сигнатуры", 170);
			listView.Columns.Add("Правило", 80);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей снизу
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			split.Panel2.Controls.Add(detailTabs);

			TabPage tabAnalysis = new TabPage("🔍 Экспертный анализ внедрения");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabHex = new TabPage("📊 Шестнадцатеричный дамп (Hex / ASCII)");
			txtHexDump = new DarkTextBoxMultiline();
			txtHexDump.Dock = DockStyle.Fill;
			txtHexDump.ReadOnly = true;
			txtHexDump.Font = Ui.FontMono(8.5f);
			tabHex.Controls.Add(txtHexDump);
			detailTabs.TabPages.Add(tabHex);

			TabPage tabBan = new TabPage("⚖️ Регламент и Команда наказания");
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
			pnl.Size = new Size(195, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.Surface;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(188, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(188, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 202;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, HollowingFilterMode mode, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8f, true);
			btn.Size = new Size(185, 28);
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
			btn.Size = new Size(210, 30);
			btn.Location = new Point(x, 8);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			x += btn.Width + 10;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			btnFilterAll.AccentStyle = (currentFilter == HollowingFilterMode.All);
			btnFilterHollowing.AccentStyle = (currentFilter == HollowingFilterMode.HollowingOnly);
			btnFilterUnbackedRwx.AccentStyle = (currentFilter == HollowingFilterMode.UnbackedRwx);
			btnFilterPeHeader.AccentStyle = (currentFilter == HollowingFilterMode.PeHeaderPresent);
			btnFilterMinecraft.AccentStyle = (currentFilter == HollowingFilterMode.MinecraftOnly);
			btnFilterWhitelisted.AccentStyle = (currentFilter == HollowingFilterMode.WhitelistedOnly);
		}

		public void ScanProcessesAndMemory()
		{
			allItems.Clear();
			listView.Items.Clear();

			int scannedProcesses = 0;
			int execPagesCount = 0;
			int unbackedRwxCount = 0;
			int hollowingCount = 0;
			int warningsCount = 0;
			int whitelistedCount = 0;

			Process[] processes = Process.GetProcesses();
			foreach (Process proc in processes)
			{
				try
				{
					string procName = proc.ProcessName;
					string procLower = procName.ToLowerInvariant();

					bool isTarget = false;
					foreach (string target in TargetSystemProcessNames)
					{
						if (procLower.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							isTarget = true;
							break;
						}
					}

					// Также проверяем белый список
					bool isWhitelistedTool = false;
					foreach (string white in WhitelistedProcessNames)
					{
						if (procLower.IndexOf(white, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							isWhitelistedTool = true;
							break;
						}
					}

					if (!isTarget && !isWhitelistedTool) continue;

					scannedProcesses++;
					bool isMinecraft = procLower.Contains("java") || procLower.Contains("minecraft");

					ScanProcessVirtualMemory(proc, isMinecraft, isWhitelistedTool, ref execPagesCount, ref unbackedRwxCount, ref hollowingCount, ref warningsCount, ref whitelistedCount);
				}
				catch { }
			}

			lblKpiTotalProcesses.Text = scannedProcesses.ToString();
			lblKpiExecPages.Text = execPagesCount.ToString();
			lblKpiUnbackedRwx.Text = unbackedRwxCount.ToString();
			lblKpiHollowingFound.Text = hollowingCount.ToString();
			lblKpiWarnings.Text = warningsCount.ToString();
			lblKpiWhitelisted.Text = whitelistedCount.ToString();

			UpdateFilterButtonStyles();
			ApplyFilter();
		}

		private void ScanProcessVirtualMemory(
			Process proc,
			bool isMinecraft,
			bool isWhitelistedTool,
			ref int execPagesCount,
			ref int unbackedRwxCount,
			ref int hollowingCount,
			ref int warningsCount,
			ref int whitelistedCount)
		{
			IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, proc.Id);
			if (hProcess == IntPtr.Zero) return;

			try
			{
				bool is64Bit = Environment.Is64BitOperatingSystem;
				ulong currentAddress = 0;
				ulong maxAddress = is64Bit ? (ulong)0x7FFFFFFFFFFFL : (ulong)0x7FFFFFFFL;

				while (currentAddress < maxAddress)
				{
					ulong regionBase = 0;
					ulong regionSize = 0;
					uint state = 0;
					uint protect = 0;
					uint memType = 0;

					if (is64Bit)
					{
						MEMORY_BASIC_INFORMATION64 mbi;
						IntPtr result = VirtualQueryEx(hProcess, (IntPtr)(long)currentAddress, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64)));
						if (result == IntPtr.Zero) break;

						regionBase = mbi.BaseAddress;
						regionSize = mbi.RegionSize;
						state = mbi.State;
						protect = mbi.Protect;
						memType = mbi.Type;
					}
					else
					{
						MEMORY_BASIC_INFORMATION32 mbi32;
						IntPtr result = VirtualQueryEx32(hProcess, (IntPtr)(long)currentAddress, out mbi32, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION32)));
						if (result == IntPtr.Zero) break;

						regionBase = mbi32.BaseAddress;
						regionSize = mbi32.RegionSize;
						state = mbi32.State;
						protect = mbi32.Protect;
						memType = mbi32.Type;
					}

					if (regionSize == 0) break;
					currentAddress = regionBase + regionSize;

					// Нас интересует только активная выделенная память (MEM_COMMIT)
					if (state != MEM_COMMIT) continue;

					// Проверяем, исполняемая ли страница
					bool isExecutable = (protect == PAGE_EXECUTE ||
					                    protect == PAGE_EXECUTE_READ ||
					                    protect == PAGE_EXECUTE_READWRITE ||
					                    protect == PAGE_EXECUTE_WRITECOPY);

					if (!isExecutable) continue;
					execPagesCount++;

					// Неразмеченная память (MEM_PRIVATE или MEM_MAPPED, не MEM_IMAGE)
					bool isUnbacked = (memType == MEM_PRIVATE);
					bool isRwx = (protect == PAGE_EXECUTE_READWRITE || protect == PAGE_EXECUTE_WRITECOPY);

					if (isUnbacked && isRwx)
					{
						unbackedRwxCount++;
					}

					// Проверяем дамп заголовка
					byte[] buffer = new byte[Math.Min((int)regionSize, 4096)];
					IntPtr bytesRead;
					bool readSuccess = ReadProcessMemory(hProcess, (IntPtr)(long)regionBase, buffer, buffer.Length, out bytesRead);

					bool hasPeHeader = false;
					string hexDump = "";
					string signaturesFound = "";

					if (readSuccess && (int)bytesRead >= 64)
					{
						// Проверка на MZ (0x4D, 0x5A)
						if (buffer[0] == 0x4D && buffer[1] == 0x5A)
						{
							hasPeHeader = true;
						}

						// Проверка сигнатур в первых 4 КБ страницы
						string ascii = Encoding.ASCII.GetString(buffer, 0, (int)bytesRead);
						MatchCollection matches = CheatSignatureRegex.Matches(ascii);
						List<string> foundList = new List<string>();
						foreach (Match m in matches)
						{
							string val = m.Value.ToLowerInvariant();
							if (!foundList.Contains(val)) foundList.Add(val);
						}
						if (foundList.Count > 0)
						{
							signaturesFound = string.Join(", ", foundList.ToArray());
						}

						hexDump = FormatHexDump(buffer, (int)bytesRead);
					}

					// Классификация угрозы
					string threat = "INFO";
					string reason = "Стандартная исполняемая область";
					string rule = "—";

					if (isWhitelistedTool)
					{
						threat = "SAFE";
						reason = "Разрешённое ПО ScreenShare / Автокликер (Белый список регламента)";
						rule = "РАЗРЕШЕНО";
						whitelistedCount++;
					}
					else if (isUnbacked && hasPeHeader)
					{
						threat = "CRITICAL";
						reason = "🚨 Полое внедрение PE (Process Hollowing / Reflective DLL) в приватной памяти!";
						rule = "5.01 / 5.08";
						hollowingCount++;
					}
					else if (isUnbacked && !string.IsNullOrEmpty(signaturesFound))
					{
						threat = "CRITICAL";
						reason = "🚨 Неразмеченная память с сигнатурами читов: " + signaturesFound;
						rule = "5.01";
						hollowingCount++;
					}
					else if (isUnbacked && isRwx && !isMinecraft)
					{
						threat = "WARNING";
						reason = "⚠️ Подозрительная неразмеченная RWX память в системном процессе (риск инжекта)";
						rule = "5.08";
						warningsCount++;
					}
					else if (isMinecraft && isUnbacked && isRwx)
					{
						// В Minecraft JVM компилятор JIT (C1/C2) легитимно выделяет память под JIT код
						threat = "INFO";
						reason = "JVM HotSpot JIT Compiler Execution Buffer (Байткод JIT компилятора)";
						rule = "Одобрено JIT";
						whitelistedCount++;
					}

					// Добавляем запись, если это представляет криминалистический интерес
					if (threat != "INFO" || hasPeHeader || !string.IsNullOrEmpty(signaturesFound))
					{
						allItems.Add(new HollowingMemoryItem
						{
							ProcessName = proc.ProcessName + " (PID " + proc.Id + ")",
							ProcessId = proc.Id,
							BaseAddress = "0x" + regionBase.ToString("X12"),
							RegionSizeBytes = (long)regionSize,
							RegionSizeFormatted = FormatBytes((long)regionSize),
							State = "MEM_COMMIT",
							Protect = FormatProtect(protect),
							MemoryType = FormatType(memType),
							ThreatLevel = threat,
							DetectionReason = reason,
							SignaturesFound = string.IsNullOrEmpty(signaturesFound) ? "—" : signaturesFound,
							TargetRule = rule,
							HasPeHeader = hasPeHeader,
							IsUnbacked = isUnbacked,
							IsMinecraft = isMinecraft,
							IsWhitelisted = isWhitelistedTool,
							DumpSnippet = hexDump
						});
					}
				}
			}
			catch { }
			finally
			{
				CloseHandle(hProcess);
			}
		}

		private string FormatBytes(long bytes)
		{
			if (bytes >= 1024 * 1024) return (bytes / (1024 * 1024)).ToString() + " МБ";
			if (bytes >= 1024) return (bytes / 1024).ToString() + " КБ";
			return bytes.ToString() + " Б";
		}

		private string FormatProtect(uint protect)
		{
			if (protect == PAGE_EXECUTE_READWRITE) return "PAGE_EXECUTE_RW (RWX)";
			if (protect == PAGE_EXECUTE_READ) return "PAGE_EXECUTE_READ (RX)";
			if (protect == PAGE_EXECUTE) return "PAGE_EXECUTE (X)";
			if (protect == PAGE_EXECUTE_WRITECOPY) return "PAGE_EXECUTE_WC";
			if (protect == PAGE_READWRITE) return "PAGE_READWRITE (RW)";
			if (protect == PAGE_READONLY) return "PAGE_READONLY (R)";
			return "0x" + protect.ToString("X");
		}

		private string FormatType(uint type)
		{
			if (type == MEM_IMAGE) return "MEM_IMAGE (DLL/EXE)";
			if (type == MEM_MAPPED) return "MEM_MAPPED";
			if (type == MEM_PRIVATE) return "MEM_PRIVATE (Heap/Unbacked)";
			return "0x" + type.ToString("X");
		}

		private string FormatHexDump(byte[] buffer, int length)
		{
			StringBuilder sb = new StringBuilder();
			int lines = Math.Min(length / 16, 24); // до 384 байт дампа
			for (int i = 0; i < lines; i++)
			{
				int offset = i * 16;
				sb.Append(offset.ToString("X4")).Append(": ");
				for (int j = 0; j < 16; j++)
				{
					if (offset + j < length)
						sb.Append(buffer[offset + j].ToString("X2")).Append(" ");
					else
						sb.Append("   ");
				}
				sb.Append(" | ");
				for (int j = 0; j < 16; j++)
				{
					if (offset + j < length)
					{
						byte b = buffer[offset + j];
						sb.Append((b >= 32 && b <= 126) ? (char)b : '.');
					}
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (HollowingMemoryItem item in allItems)
			{
				if (currentFilter == HollowingFilterMode.HollowingOnly && item.ThreatLevel != "CRITICAL") continue;
				if (currentFilter == HollowingFilterMode.UnbackedRwx && !item.IsUnbacked) continue;
				if (currentFilter == HollowingFilterMode.PeHeaderPresent && !item.HasPeHeader) continue;
				if (currentFilter == HollowingFilterMode.MinecraftOnly && !item.IsMinecraft) continue;
				if (currentFilter == HollowingFilterMode.WhitelistedOnly && !item.IsWhitelisted) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string matchStr = (item.ProcessName + " " + item.BaseAddress + " " + item.Protect + " " + item.DetectionReason + " " + item.SignaturesFound).ToLowerInvariant();
					if (!matchStr.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(item.ProcessName);
				lvi.SubItems.Add(item.BaseAddress);
				lvi.SubItems.Add(item.RegionSizeFormatted);
				lvi.SubItems.Add(item.Protect);
				lvi.SubItems.Add(item.MemoryType);
				lvi.SubItems.Add(item.ThreatLevel);
				lvi.SubItems.Add(item.DetectionReason);
				lvi.SubItems.Add(item.SignaturesFound);
				lvi.SubItems.Add(item.TargetRule);
				lvi.Tag = item;

				if (item.ThreatLevel == "CRITICAL")
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 15, 15);
				}
				else if (item.ThreatLevel == "WARNING")
				{
					lvi.ForeColor = Ui.AccentOrange;
				}
				else if (item.IsWhitelisted)
				{
					lvi.ForeColor = Ui.AccentGreen;
				}
				else
				{
					lvi.ForeColor = Ui.TextPrimary;
				}

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ShowEmptyDetails();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			HollowingMemoryItem item = listView.SelectedItems[0].Tag as HollowingMemoryItem;
			if (item == null) return;

			// Анализ
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛЬНЫЙ СУДЕБНЫЙ АНАЛИЗ ВНЕДРЕНИЯ В ПАМЯТЬ ===");
			sb.AppendLine("Процесс: " + item.ProcessName);
			sb.AppendLine("Базовый адрес региона: " + item.BaseAddress);
			sb.AppendLine("Размер региона памяти: " + item.RegionSizeFormatted + " (" + item.RegionSizeBytes + " байт)");
			sb.AppendLine("Текущая защита (Protect): " + item.Protect);
			sb.AppendLine("Тип памяти (Type): " + item.MemoryType);
			sb.AppendLine("Наличие PE-заголовка (MZ / PE): " + (item.HasPeHeader ? "ДА (ОБНАРУЖЕН ЗАГОЛОВОК ИСПОЛНЯЕМОГО ФАЙЛА!)" : "НЕТ"));
			sb.AppendLine("Неразмеченная память (Unbacked): " + (item.IsUnbacked ? "ДА (MEM_PRIVATE, нет файла DLL/EXE на диске)" : "НЕТ (Привязана к образу на диске)"));
			sb.AppendLine("Найденные сигнатуры читов: " + item.SignaturesFound);
			sb.AppendLine("Уровень угрозы: " + item.ThreatLevel);
			sb.AppendLine("Заключение детектора: " + item.DetectionReason);
			sb.AppendLine("Статус регламента: " + item.TargetRule);
			sb.AppendLine();
			if (item.HasPeHeader)
			{
				sb.AppendLine("🚨 ВНИМАНИЕ: Наличие заголовка MZ (0x4D5A) в неразмеченной памяти указывает на метод внедрения Process Hollowing / RunPE или Reflective DLL Injection. Исполняемый файл чита был полностью загружен и запущен в памяти чужого процесса в обход файловой системы!");
			}
			txtAnalysis.Text = sb.ToString();

			// Hex дамп
			txtHexDump.Text = string.IsNullOrEmpty(item.DumpSnippet) ? "Дамп памяти недоступен или пуст." : item.DumpSnippet;

			// Вердикт
			StringBuilder vb = new StringBuilder();
			vb.AppendLine("=== КРИМИНАЛИСТИЧЕСКИЙ ВЕРДИКТ И ПРАВИЛА РЕГЛАМЕНТА ===");
			vb.AppendLine("Игрок: " + targetPlayerName);
			vb.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			vb.AppendLine();
			if (item.ThreatLevel == "CRITICAL")
			{
				vb.AppendLine("НАРУШЕНИЕ: ПРАВИЛО 5.01 / 5.08 (ЧИТЫ И СКРЫТОЕ ВНЕДРЕНИЕ В ПАМЯТЬ)");
				vb.AppendLine("Улика: В памяти процесса " + item.ProcessName + " по адресу " + item.BaseAddress + " выявлено полое внедрение PE-кода чита (Process Hollowing).");
				vb.AppendLine("Наказание: Бан на 30 дней.");
				vb.AppendLine();
				vb.AppendLine("Команда бана для чата сервера:");
				vb.AppendLine("/ban " + targetPlayerName + " 30d 5.01 (Process Hollowing / Injected Memory: " + item.ProcessName + ")");
			}
			else if (item.IsWhitelisted)
			{
				vb.AppendLine("СТАТУС: БЕЛЫЙ СПИСОК (НАРУШЕНИЕ ОТСУТСТВУЕТ)");
				vb.AppendLine("Процесс является одобренным инструментом ScreenShare (AnyDesk/RuDesktop) или легитимным JIT-буфером виртуальной машины Java.");
				vb.AppendLine("Банить по правилу 5.01 ЗАПРЕЩЕНО.");
			}
			else
			{
				vb.AppendLine("СТАТУС: ИНФОРМАЦИОННОЕ СОБЫТИЕ");
				vb.AppendLine("Прямых доказательств инжекта чита не выявлено. Требуется сопоставление с Prefetch и журналом AppCompatCache.");
			}
			txtBanVerdict.Text = vb.ToString();
		}

		private void ShowEmptyDetails()
		{
			txtAnalysis.Text = "Регионы памяти не выбраны или отсутствуют в данном фильтре.";
			txtHexDump.Text = "";
			txtBanVerdict.Text = "";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count > 0)
			{
				HollowingMemoryItem item = listView.SelectedItems[0].Tag as HollowingMemoryItem;
				if (item != null && item.ThreatLevel == "CRITICAL")
				{
					string cmd = "/ban " + targetPlayerName + " 30d 5.01 (Injected Memory: " + item.ProcessName + ")";
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.01", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			string defCmd = "/ban " + targetPlayerName + " 30d 5.01 (Process Hollowing / Injected Cheat)";
			Clipboard.SetText(defCmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n\n" + defCmd, "Бан 5.01", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine("- [MCCheatChecker] СУДЕБНЫЙ АУДИТ ПАМЯТИ И PROCESS HOLLOWING");
			sb.AppendLine("+ Игрок: " + targetPlayerName);
			sb.AppendLine("+ Время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("+ Процессов исследовано: " + lblKpiTotalProcesses.Text);
			sb.AppendLine("+ Неразмеченной RWX памяти: " + lblKpiUnbackedRwx.Text);
			sb.AppendLine("+ Критических внедрений (5.01): " + lblKpiHollowingFound.Text);
			sb.AppendLine();
			int critCount = 0;
			foreach (HollowingMemoryItem it in allItems)
			{
				if (it.ThreatLevel == "CRITICAL" && critCount < 5)
				{
					sb.AppendLine("! [5.01] " + it.ProcessName + " -> " + it.BaseAddress + " (" + it.RegionSizeFormatted + ") : " + it.DetectionReason);
					critCount++;
				}
			}
			if (critCount == 0)
			{
				sb.AppendLine("+ Признаков полого внедрения RunPE и чужих PE-заголовков в памяти не обнаружено.");
			}
			sb.AppendLine("```");
			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord скопирована в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), "mccheatchecker_hollowing_report_" + DateTime.Now.Ticks + ".html");
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Process Hollowing Report</title>");
				html.AppendLine("<style>body{background:#0e1117;color:#c9d1d9;font-family:Segoe UI,sans-serif;padding:24px;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #30363d;padding:8px 12px;font-size:12px;text-align:left;} th{background:#161b22;color:#58a6ff;} tr:nth-child(even){background:#12161f;} .crit{color:#f85149;font-weight:bold;} .warn{color:#d29922;} .safe{color:#3fb950;}</style></head><body>");
				html.AppendLine("<h2>🧬 Криминалистический отчёт по полому внедрению процессов и памяти</h2>");
				html.AppendLine("<p><b>Игрок:</b> " + targetPlayerName + " | <b>Дата:</b> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");
				html.AppendLine("<p><b>Процессов:</b> " + lblKpiTotalProcesses.Text + " | <b>RWX страниц:</b> " + lblKpiUnbackedRwx.Text + " | <b>Внедрений:</b> " + lblKpiHollowingFound.Text + "</p>");
				html.AppendLine("<table><tr><th>Процесс (PID)</th><th>Адрес</th><th>Размер</th><th>Защита</th><th>Тип</th><th>Угроза</th><th>Причина</th><th>Сигнатуры</th><th>Правило</th></tr>");
				foreach (HollowingMemoryItem it in allItems)
				{
					string cls = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
					html.AppendLine("<tr><td>" + it.ProcessName + "</td><td>" + it.BaseAddress + "</td><td>" + it.RegionSizeFormatted + "</td><td>" + it.Protect + "</td><td>" + it.MemoryType + "</td><td class='" + cls + "'>" + it.ThreatLevel + "</td><td>" + it.DetectionReason + "</td><td>" + it.SignaturesFound + "</td><td>" + it.TargetRule + "</td></tr>");
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
	}
}
