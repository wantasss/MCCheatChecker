using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ProcessHandlesViewerForm : Form
	{
		#region Win32 Native APIs

		[DllImport("ntdll.dll")]
		private static extern int NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

		[DllImport("ntdll.dll")]
		private static extern int NtQueryInformationThread(IntPtr ThreadHandle, int ThreadInformationClass, out IntPtr ThreadInformation, int ThreadInformationLength, out int ReturnLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int GetProcessId(IntPtr Process);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

		private const int SystemExtendedHandleInformation = 64;
		private const int ThreadQuerySetWin32StartAddress = 9;

		private const uint PROCESS_TERMINATE = 0x0001;
		private const uint PROCESS_CREATE_THREAD = 0x0002;
		private const uint PROCESS_VM_OPERATION = 0x0008;
		private const uint PROCESS_VM_READ = 0x0010;
		private const uint PROCESS_VM_WRITE = 0x0020;
		private const uint PROCESS_DUP_HANDLE = 0x0040;
		private const uint PROCESS_QUERY_INFORMATION = 0x0400;
		private const uint PROCESS_SUSPEND_RESUME = 0x0800;
		private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
		private const uint PROCESS_ALL_ACCESS = 0x1FFFFF;

		private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
		private const uint THREAD_QUERY_INFORMATION = 0x0040;

		private const uint MEM_COMMIT = 0x1000;
		private const uint MEM_PRIVATE = 0x20000;
		private const uint MEM_MAPPED = 0x40000;
		private const uint MEM_IMAGE = 0x1000000;

		private const uint PAGE_EXECUTE = 0x10;
		private const uint PAGE_EXECUTE_READ = 0x20;
		private const uint PAGE_EXECUTE_READWRITE = 0x40;
		private const uint PAGE_EXECUTE_WRITECOPY = 0x80;

		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
		{
			public IntPtr Object;
			public IntPtr UniqueProcessId;
			public IntPtr HandleValue;
			public uint GrantedAccess;
			public ushort CreatorBackTraceIndex;
			public ushort ObjectTypeIndex;
			public uint HandleAttributes;
			public uint Reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION64
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

		#endregion

		#region Forensic Models

		public enum HandleFilterMode
		{
			All,
			InjectorsOnly,
			VmReadOnly,
			UnbackedThreads,
			SuspiciousDlls,
			Whitelist
		}

		public enum ForensicItemKind
		{
			ProcessHandle,
			InjectedThread,
			LoadedModule
		}

		public class ForensicEntry
		{
			public ForensicItemKind Kind;
			public string Verdict;
			public Color ForeColor;
			public string Name;
			public string Identifier; // Handle hex, Thread ID, or Base address
			public string Details;
			public string TargetProcess;
			public string FilePath;
			public uint AccessMask;
			public bool IsCritical;
			public bool IsSuspicious;
			public bool IsWhitelisted;
			public string ViolationRule;
			public string BanReason;
			public int OwnerPid;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private HandleFilterMode currentFilter = HandleFilterMode.All;
		private List<ForensicEntry> allItems = new List<ForensicEntry>();
		private int currentMinecraftPid = 0;
		private string currentMinecraftName = "";

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;
		private Label lblTargetProcess;
		private Label lblDetailVerdict;
		private Label lblDetailRule;
		private Label lblDetailBanCommand;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterInjectors;
		private DarkButton btnFilterVmRead;
		private DarkButton btnFilterThreads;
		private DarkButton btnFilterDlls;
		private DarkButton btnFilterWhitelist;

		private DarkButton btnCopyBan;
		private DarkButton btnKillProcess;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;
		private CheckBox chkAutoRefresh;
		private System.Windows.Forms.Timer autoRefreshTimer;

		private bool isScanning = false;

		private static readonly Regex CheatNamePattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject|killaura|velocity|aimbot|minhook|nativehook)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ProcessHandlesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			DetectMinecraftProcess();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			Text = "🔗 Инспектор дескрипторов процессов, инжекций в память и потоков Minecraft";
			Size = new Size(1220, 780);
			MinimumSize = new Size(950, 560);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 145;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔗 Инспектор дескрипторов (Handles), инжекций памяти и потоков javaw.exe";
			lblHeaderTitle.Font = Ui.FontTitle(11.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(12, 8);
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Анализ открытых дескрипторов сторонних процессов к Minecraft (VM_WRITE/VM_READ/AllAccess), unbacked потоков шеллкода и инжектированных DLL.";
			lblHeaderSubtitle.Font = Ui.FontBody(8f);
			lblHeaderSubtitle.ForeColor = Ui.TextMuted;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(13, 30);
			topPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования дескрипторов...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(13, 51);
			topPanel.Controls.Add(lblStatusPill);

			lblTargetProcess = new Label();
			lblTargetProcess.Text = "🎯 Целевой процесс: Поиск javaw.exe...";
			lblTargetProcess.Font = Ui.FontBody(8.5f, true);
			lblTargetProcess.ForeColor = Ui.AccentOrange;
			lblTargetProcess.AutoSize = true;
			lblTargetProcess.Location = new Point(480, 51);
			topPanel.Controls.Add(lblTargetProcess);

			// Row of Filter Buttons
			FlowLayoutPanel filterRow = new FlowLayoutPanel();
			filterRow.Location = new Point(12, 75);
			filterRow.Size = new Size(1180, 32);
			filterRow.WrapContents = false;
			filterRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("Все объекты", HandleFilterMode.All);
			btnFilterInjectors = CreateFilterButton("🚨 Инжекторы (VM_WRITE / AllAccess)", HandleFilterMode.InjectorsOnly);
			btnFilterVmRead = CreateFilterButton("⚠️ Чтение памяти (VM_READ)", HandleFilterMode.VmReadOnly);
			btnFilterThreads = CreateFilterButton("🧵 Unbacked Потоки javaw", HandleFilterMode.UnbackedThreads);
			btnFilterDlls = CreateFilterButton("📦 Сторонние DLL", HandleFilterMode.SuspiciousDlls);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", HandleFilterMode.Whitelist);

			filterRow.Controls.Add(btnFilterAll);
			filterRow.Controls.Add(btnFilterInjectors);
			filterRow.Controls.Add(btnFilterVmRead);
			filterRow.Controls.Add(btnFilterThreads);
			filterRow.Controls.Add(btnFilterDlls);
			filterRow.Controls.Add(btnFilterWhitelist);
			topPanel.Controls.Add(filterRow);

			// Search & Quick Action Row
			Panel actionRow = new Panel();
			actionRow.Location = new Point(12, 110);
			actionRow.Size = new Size(1180, 30);
			actionRow.BackColor = Color.Transparent;

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(0, 2);
			searchBox.Size = new Size(330, 24);
			searchBox.Text = "";
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			actionRow.Controls.Add(searchBox);

			Label lblSearchHint = new Label();
			lblSearchHint.Text = "🔍 Поиск по имени, PID, правам, адресу...";
			lblSearchHint.Location = new Point(338, 6);
			lblSearchHint.AutoSize = true;
			lblSearchHint.ForeColor = Ui.TextMuted;
			lblSearchHint.Font = Ui.FontBody(8f);
			actionRow.Controls.Add(lblSearchHint);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 Обновить";
			btnRefreshNow.Location = new Point(610, 2);
			btnRefreshNow.Size = new Size(100, 25);
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			actionRow.Controls.Add(btnRefreshNow);

			chkAutoRefresh = new CheckBox();
			chkAutoRefresh.Text = "Авто (4с)";
			chkAutoRefresh.Location = new Point(718, 5);
			chkAutoRefresh.AutoSize = true;
			chkAutoRefresh.ForeColor = Ui.TextPrimary;
			chkAutoRefresh.CheckedChanged += delegate {
				if (chkAutoRefresh.Checked) autoRefreshTimer.Start();
				else autoRefreshTimer.Stop();
			};
			actionRow.Controls.Add(chkAutoRefresh);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать бан (5.01)";
			btnCopyBan.Location = new Point(810, 2);
			btnCopyBan.Size = new Size(180, 25);
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			actionRow.Controls.Add(btnCopyBan);

			btnKillProcess = new DarkButton();
			btnKillProcess.Text = "⚡ Завершить процесс чита";
			btnKillProcess.Location = new Point(998, 2);
			btnKillProcess.Size = new Size(175, 25);
			btnKillProcess.Click += delegate { KillSelectedProcess(); };
			actionRow.Controls.Add(btnKillProcess);

			topPanel.Controls.Add(actionRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail pane
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1180, 560);
			try { split.SplitterDistance = 380; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 210);
			listView.Columns.Add("Процесс-источник / Имя", 200);
			listView.Columns.Add("Дескриптор / ID", 110);
			listView.Columns.Add("Права доступа (Access Mask)", 220);
			listView.Columns.Add("Целевой процесс", 150);
			listView.Columns.Add("Путь к файлу на диске / Модуль", 280);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Bottom Detail Panel
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(12, 8, 12, 8);

			Panel detailHeader = new Panel();
			detailHeader.Dock = DockStyle.Top;
			detailHeader.Height = 45;
			detailHeader.BackColor = Color.Transparent;

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите объект из списка выше для криминалистического анализа.";
			lblDetailVerdict.Font = Ui.FontBody(9f, true);
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailVerdict.AutoSize = true;
			lblDetailVerdict.Location = new Point(2, 2);
			detailHeader.Controls.Add(lblDetailVerdict);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Регламент: Проверка не выбрана";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.TextMuted;
			lblDetailRule.AutoSize = true;
			lblDetailRule.Location = new Point(2, 22);
			detailHeader.Controls.Add(lblDetailRule);

			lblDetailBanCommand = new Label();
			lblDetailBanCommand.Text = "";
			lblDetailBanCommand.Font = Ui.FontMono(8f);
			lblDetailBanCommand.ForeColor = Ui.AccentOrange;
			lblDetailBanCommand.AutoSize = true;
			lblDetailBanCommand.Location = new Point(480, 22);
			detailHeader.Controls.Add(lblDetailBanCommand);

			detailContainer.Controls.Add(detailHeader);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Dock = DockStyle.Fill;
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			txtDetails.BackColor = Ui.Surface;
			txtDetails.ForeColor = Ui.TextPrimary;
			detailContainer.Controls.Add(txtDetails);

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);

			// Bottom Status Strip
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 35;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 4, 12, 4);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📑 Экспорт протокола аудита дескрипторов";
			btnExportReport.Location = new Point(12, 5);
			btnExportReport.Size = new Size(290, 25);
			btnExportReport.Click += delegate { ExportProtocolToClipboard(); };
			bottomPanel.Controls.Add(btnExportReport);

			Label lblHelpNote = new Label();
			lblHelpNote.Text = "💡 Подсказка: Процессы с правами PROCESS_VM_WRITE или ALL_ACCESS способны модифицировать память Minecraft на лету (Вейп, Дрип, инжекторы).";
			lblHelpNote.Location = new Point(315, 9);
			lblHelpNote.AutoSize = true;
			lblHelpNote.ForeColor = Ui.TextMuted;
			lblHelpNote.Font = Ui.FontBody(8f);
			bottomPanel.Controls.Add(lblHelpNote);

			Controls.Add(bottomPanel);

			// Auto refresh timer
			autoRefreshTimer = new System.Windows.Forms.Timer();
			autoRefreshTimer.Interval = 4000;
			autoRefreshTimer.Tick += delegate {
				if (!isScanning) StartScanAsync();
			};

			UpdateFilterButtonsStyle();
		}

		private DarkButton CreateFilterButton(string text, HandleFilterMode mode)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.AutoSize = true;
			btn.Height = 27;
			btn.Margin = new Padding(0, 0, 8, 0);
			btn.Font = Ui.FontBody(8.5f);
			btn.Click += delegate {
				currentFilter = mode;
				UpdateFilterButtonsStyle();
				ApplyFilters();
			};
			return btn;
		}

		private void UpdateFilterButtonsStyle()
		{
			btnFilterAll.BackColor = (currentFilter == HandleFilterMode.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterInjectors.BackColor = (currentFilter == HandleFilterMode.InjectorsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterVmRead.BackColor = (currentFilter == HandleFilterMode.VmReadOnly) ? Ui.AccentOrange : Ui.Surface;
			btnFilterThreads.BackColor = (currentFilter == HandleFilterMode.UnbackedThreads) ? Ui.AccentTeal : Ui.Surface;
			btnFilterDlls.BackColor = (currentFilter == HandleFilterMode.SuspiciousDlls) ? Ui.AccentTeal : Ui.Surface;
			btnFilterWhitelist.BackColor = (currentFilter == HandleFilterMode.Whitelist) ? Ui.AccentGreen : Ui.Surface;
		}

		#endregion

		#region Process Detection & Scanning

		private void DetectMinecraftProcess()
		{
			try
			{
				Process[] procs = Process.GetProcesses();
				Process mc = procs.FirstOrDefault(p =>
					p.ProcessName.IndexOf("javaw", StringComparison.OrdinalIgnoreCase) >= 0 ||
					p.ProcessName.IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0 ||
					p.ProcessName.IndexOf("bedrock_server", StringComparison.OrdinalIgnoreCase) >= 0);

				if (mc == null)
				{
					// Check if any java process has minecraft in title or commandline
					mc = procs.FirstOrDefault(p => p.ProcessName.IndexOf("java", StringComparison.OrdinalIgnoreCase) >= 0);
				}

				if (mc != null)
				{
					currentMinecraftPid = mc.Id;
					currentMinecraftName = mc.ProcessName + ".exe";
					lblTargetProcess.Text = string.Format("🎯 Minecraft: {0} (PID: {1})", currentMinecraftName, currentMinecraftPid);
					lblTargetProcess.ForeColor = Ui.AccentGreen;
				}
				else
				{
					currentMinecraftPid = 0;
					currentMinecraftName = "";
					lblTargetProcess.Text = "⚠️ Minecraft (javaw.exe) не запущен! Сканируются все дескрипторы системы.";
					lblTargetProcess.ForeColor = Ui.AccentOrange;
				}
			}
			catch (Exception ex)
			{
				lblTargetProcess.Text = "Ошибка определения процесса: " + ex.Message;
			}
		}

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "⏳ Сканирование системной таблицы дескрипторов ядра...";
			lblStatusPill.ForeColor = Ui.AccentTeal;

			ThreadPool.QueueUserWorkItem(delegate {
				List<ForensicEntry> results = new List<ForensicEntry>();
				try
				{
					// 1. Scan Cross-Process Handles
					ScanCrossProcessHandles(results, currentMinecraftPid);

					// 2. Scan Threads of javaw.exe
					if (currentMinecraftPid > 0)
					{
						ScanTargetThreads(results, currentMinecraftPid);
						ScanTargetLoadedModules(results, currentMinecraftPid);
					}
				}
				catch (Exception ex)
				{
					ForensicEntry err = new ForensicEntry
					{
						Kind = ForensicItemKind.ProcessHandle,
						Verdict = "ОШИБКА АНАЛИЗА",
						ForeColor = Ui.AccentRed,
						Name = "Scanner",
						Details = "Исключение при сканировании дескрипторов: " + ex.Message,
						ViolationRule = "Ошибка"
					};
					results.Add(err);
				}

				BeginInvoke(new Action(delegate {
					allItems = results;
					isScanning = false;
					int critCount = allItems.Count(i => i.IsCritical);
					int suspCount = allItems.Count(i => i.IsSuspicious);

					if (critCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНО ИНЖЕКТОРОВ / ЧИТОВ: {0} шт. (Подозрительных: {1})", critCount, suspCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else if (suspCount > 0)
					{
						lblStatusPill.Text = string.Format("⚠️ Найдено подозрительных дескрипторов: {0} шт.", suspCount);
						lblStatusPill.ForeColor = Ui.AccentOrange;
					}
					else
					{
						lblStatusPill.Text = string.Format("✔ Проанализировано записей: {0}. Опасных инжекторов не обнаружено.", allItems.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
					}

					ApplyFilters();
				}));
			});
		}

		private void ScanCrossProcessHandles(List<ForensicEntry> results, int targetPid)
		{
			int myPid = Process.GetCurrentProcess().Id;
			IntPtr hSelf = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, myPid);

			int length = 0x100000;
			IntPtr ptr = Marshal.AllocHGlobal(length);
			int returnLength = 0;
			int status;

			try
			{
				while ((status = NtQuerySystemInformation(SystemExtendedHandleInformation, ptr, length, out returnLength)) == -1073741820) // STATUS_INFO_LENGTH_MISMATCH
				{
					Marshal.FreeHGlobal(ptr);
					length = Math.Max(returnLength + 0x40000, length * 2);
					ptr = Marshal.AllocHGlobal(length);
				}

				if (status != 0) return;

				long handleCount = Marshal.ReadIntPtr(ptr).ToInt64();
				IntPtr handlePtr = new IntPtr(ptr.ToInt64() + IntPtr.Size * 2);
				int entrySize = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

				// Dynamically identify ObjectTypeIndex for Process objects
				ushort processTypeIndex = 0;
				for (long i = 0; i < handleCount; i++)
				{
					SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX entry = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(
						new IntPtr(handlePtr.ToInt64() + i * entrySize), typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));
					if (entry.UniqueProcessId.ToInt32() == myPid && entry.HandleValue == hSelf)
					{
						processTypeIndex = entry.ObjectTypeIndex;
						break;
					}
				}

				// Cache process info to avoid repeated OpenProcess / Process.GetProcessById calls
				Dictionary<int, string> procNameCache = new Dictionary<int, string>();
				Dictionary<int, string> procPathCache = new Dictionary<int, string>();

				for (long i = 0; i < handleCount; i++)
				{
					SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX entry = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(
						new IntPtr(handlePtr.ToInt64() + i * entrySize), typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

					if (processTypeIndex > 0 && entry.ObjectTypeIndex != processTypeIndex) continue;

					int ownerPid = entry.UniqueProcessId.ToInt32();
					if (ownerPid <= 4 || ownerPid == myPid) continue;

					IntPtr hOwner = OpenProcess(PROCESS_DUP_HANDLE, false, ownerPid);
					if (hOwner == IntPtr.Zero) continue;

					IntPtr hDup;
					if (DuplicateHandle(hOwner, entry.HandleValue, GetCurrentProcess(), out hDup, 0, false, DUPLICATE_SAME_ACCESS))
					{
						int handleTargetPid = GetProcessId(hDup);
						CloseHandle(hDup);

						if (handleTargetPid > 0)
						{
							// If Minecraft is running, focus on handles pointing to Minecraft
							if (targetPid > 0 && handleTargetPid != targetPid)
							{
								CloseHandle(hOwner);
								continue;
							}

							string ownerName;
							if (!procNameCache.TryGetValue(ownerPid, out ownerName))
							{
								try { ownerName = Process.GetProcessById(ownerPid).ProcessName; }
								catch { ownerName = "PID:" + ownerPid; }
								procNameCache[ownerPid] = ownerName;
							}

							string ownerPath;
							if (!procPathCache.TryGetValue(ownerPid, out ownerPath))
							{
								ownerPath = GetProcessPathSafe(ownerPid);
								procPathCache[ownerPid] = ownerPath;
							}

							string targetName = (handleTargetPid == targetPid && !string.IsNullOrEmpty(currentMinecraftName)) ?
								currentMinecraftName : ("PID: " + handleTargetPid);

							ForensicEntry item = CategorizeProcessHandle(ownerPid, ownerName, ownerPath, entry.HandleValue, entry.GrantedAccess, handleTargetPid, targetName);
							results.Add(item);
						}
					}
					CloseHandle(hOwner);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
				if (hSelf != IntPtr.Zero) CloseHandle(hSelf);
			}
		}

		private ForensicEntry CategorizeProcessHandle(int ownerPid, string ownerName, string ownerPath, IntPtr handleValue, uint access, int targetPid, string targetName)
		{
			ForensicEntry item = new ForensicEntry();
			item.Kind = ForensicItemKind.ProcessHandle;
			item.OwnerPid = ownerPid;
			item.Name = ownerName + " (PID " + ownerPid + ")";
			item.Identifier = "0x" + handleValue.ToInt64().ToString("X");
			item.AccessMask = access;
			item.TargetProcess = targetName;
			item.FilePath = ownerPath;

			bool isVmWrite = (access & PROCESS_VM_WRITE) != 0;
			bool isVmOperation = (access & PROCESS_VM_OPERATION) != 0;
			bool isCreateThread = (access & PROCESS_CREATE_THREAD) != 0;
			bool isAllAccess = (access == PROCESS_ALL_ACCESS) || ((access & 0x1F0FFF) == 0x1F0FFF);
			bool isVmRead = (access & PROCESS_VM_READ) != 0;

			List<string> rights = new List<string>();
			if (isAllAccess) rights.Add("ALL_ACCESS");
			else
			{
				if (isVmWrite) rights.Add("VM_WRITE");
				if (isVmOperation) rights.Add("VM_OPERATION");
				if (isCreateThread) rights.Add("CREATE_THREAD");
				if (isVmRead) rights.Add("VM_READ");
				if ((access & PROCESS_DUP_HANDLE) != 0) rights.Add("DUP_HANDLE");
				if ((access & PROCESS_SUSPEND_RESUME) != 0) rights.Add("SUSPEND_RESUME");
				if ((access & PROCESS_QUERY_INFORMATION) != 0) rights.Add("QUERY_INFO");
				if ((access & PROCESS_QUERY_LIMITED_INFORMATION) != 0) rights.Add("QUERY_LIMITED");
			}

			string rightsStr = string.Join(" | ", rights.ToArray());
			if (string.IsNullOrEmpty(rightsStr)) rightsStr = "0x" + access.ToString("X");

			// Whitelist Rules Check
			string lowerName = ownerName.ToLowerInvariant();
			string lowerPath = (ownerPath ?? "").ToLowerInvariant();

			bool isAutoclicker = lowerName.Contains("opautoclicker") || lowerName.Contains("speedautoclicker") ||
				lowerName.Contains("tinytask") || lowerName.Contains("x-mouse") || lowerName.Contains("autohotkey") ||
				lowerName.Contains("clickermann") || lowerName.Contains("keyran") || lowerName.Contains("auto_clicker");

			bool isScreenShare = lowerName.Contains("anydesk") || lowerName.Contains("rudesktop") || lowerName.Contains("discord");
			bool isOverlay = lowerName.Contains("obs64") || lowerName.Contains("obs32") || lowerName.Contains("nvspcap64") ||
				lowerName.Contains("rtss") || lowerName.Contains("steam") || lowerName.Contains("overwolf");

			bool isSystem = lowerName.Equals("csrss") || lowerName.Equals("lsass") || lowerName.Equals("services") ||
				lowerName.Equals("smss") || lowerName.Equals("system") || lowerName.Equals("svchost") || lowerName.Equals("explorer") ||
				lowerName.Equals("taskhostw") || lowerName.Equals("sihost");

			bool isMinecraftSelf = lowerName.Contains("javaw") || lowerName.Contains("java") || lowerName.Contains("tlauncher") ||
				lowerName.Contains("minecraft") || lowerName.Contains("curseforge") || lowerName.Contains("prism");

			if (isAutoclicker)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: Автокликер)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "Легитимный кликер (Разрешено)";
				item.Details = string.Format("Процесс {0} является разрешенным ПО согласно регламенту проверок.\r\nДескриптор: {1}, Права: {2}", ownerName, item.Identifier, rightsStr);
				return item;
			}

			if (isScreenShare)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: ScreenShare)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "ПО для демонстрации экрана";
				item.Details = string.Format("Процесс {0} используется для трансляции проверки (AnyDesk / RuDesktop / Discord).\r\nДескриптор: {1}, Права: {2}", ownerName, item.Identifier, rightsStr);
				return item;
			}

			if (isOverlay)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (Оверлей / Захват экрана)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "Видеозахват / Оверлей";
				item.Details = string.Format("Процесс {0} удерживает дескриптор для захвата игрового кадра (OBS / ShadowPlay / Discord).\r\nДескриптор: {1}, Права: {2}", ownerName, item.Identifier, rightsStr);
				return item;
			}

			if (isSystem || isMinecraftSelf)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (Системный процесс / Java)";
				item.ForeColor = Ui.TextMuted;
				item.ViolationRule = "Системный / Лаунчер";
				item.Details = string.Format("Процесс {0} — доверенный компонент Windows или игровой лаунчер.\r\nДескриптор: {1}, Права: {2}", ownerName, item.Identifier, rightsStr);
				return item;
			}

			// Threat Evaluation
			bool isNameMatchCheat = CheatNamePattern.IsMatch(lowerName) || CheatNamePattern.IsMatch(lowerPath);

			if (isVmWrite || isVmOperation || isCreateThread || isAllAccess || isNameMatchCheat)
			{
				item.IsCritical = true;
				item.Verdict = "🚨 ЧИТ-ИНЖЕКТОР (5.01)";
				item.ForeColor = Ui.AccentRed;
				item.ViolationRule = "Пункт 5.01 (Инжекция / Модификация памяти игры)";
				item.BanReason = string.Format("Инжекция в {0}: {1} (права: {2})", targetName, ownerName, rightsStr);
				item.Details = string.Format(
					"🚨 ОБНАРУЖЕН СТОРОННИЙ ИНЖЕКТОР С ПРАВАМИ МОДИФИКАЦИИ ПАМЯТИ MINECRAFT!\r\n" +
					"• Процесс: {0} (PID: {1})\r\n" +
					"• Путь на диске: {2}\r\n" +
					"• Дескриптор ядра: {3}\r\n" +
					"• Права доступа: {4} (0x{5:X})\r\n" +
					"• Угроза: Сторонний софт имеет прямой доступ на запись байткода/памяти (VM_WRITE/VM_OPERATION) или удаленный вызов потоков (CREATE_THREAD) в javaw.exe.\r\n" +
					"• Вердикт: Использование читов / инжектора по Регламенту 5.01.",
					ownerName, ownerPid, ownerPath ?? "Скрытый путь / Отсутствует", item.Identifier, rightsStr, access);
				return item;
			}

			if (isVmRead)
			{
				item.IsSuspicious = true;
				item.Verdict = "⚠️ ЧТЕНИЕ ПАМЯТИ (5.01)";
				item.ForeColor = Ui.AccentOrange;
				item.ViolationRule = "Пункт 5.01 (Чтение памяти / Радар / ESP)";
				item.BanReason = string.Format("Чтение памяти {0}: {1}", targetName, ownerName);
				item.Details = string.Format(
					"⚠️ ОБНАРУЖЕН ПРОЦЕСС С ПРАВАМИ ЧТЕНИЯ ПАМЯТИ MINECRAFT (VM_READ)!\r\n" +
					"• Процесс: {0} (PID: {1})\r\n" +
					"• Путь: {2}\r\n" +
					"• Дескриптор: {3}\r\n" +
					"• Права: {4}\r\n" +
					"• Примечание: Процесс может использоваться для внешнего ESP/Радара или сканирования памяти игры.",
					ownerName, ownerPid, ownerPath, item.Identifier, rightsStr);
				return item;
			}

			item.Verdict = "ℹ️ ИНФОРМАЦИОННЫЙ (Дескриптор)";
			item.ForeColor = Ui.TextMuted;
			item.ViolationRule = "Ограниченный дескриптор";
			item.Details = string.Format("Процесс {0} удерживает дескриптор без прав на запись или чтение памяти.\r\nПрава: {1}", ownerName, rightsStr);
			return item;
		}

		private void ScanTargetThreads(List<ForensicEntry> results, int targetPid)
		{
			IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, targetPid);
			if (hProcess == IntPtr.Zero) return;

			try
			{
				Process p = Process.GetProcessById(targetPid);
				List<ProcessModule> modules = new List<ProcessModule>();
				try
				{
					foreach (ProcessModule m in p.Modules) modules.Add(m);
				}
				catch { }

				foreach (ProcessThread pt in p.Threads)
				{
					IntPtr hThread = OpenThread(THREAD_QUERY_INFORMATION, false, (uint)pt.Id);
					if (hThread == IntPtr.Zero) continue;

					IntPtr startAddr = IntPtr.Zero;
					int retLen = 0;
					int status = NtQueryInformationThread(hThread, ThreadQuerySetWin32StartAddress, out startAddr, IntPtr.Size, out retLen);
					CloseHandle(hThread);

					if (status != 0 || startAddr == IntPtr.Zero) continue;

					// Query memory information of start address
					MEMORY_BASIC_INFORMATION64 mbi;
					int qRes = VirtualQueryEx(hProcess, startAddr, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64)));
					if (qRes == 0) continue;

					ulong addr = (ulong)startAddr.ToInt64();
					ProcessModule matchedModule = modules.FirstOrDefault(m =>
						addr >= (ulong)m.BaseAddress.ToInt64() &&
						addr < ((ulong)m.BaseAddress.ToInt64() + (ulong)m.ModuleMemorySize));

					bool isUnbacked = (matchedModule == null);
					bool isRwx = (mbi.Protect == PAGE_EXECUTE_READWRITE);

					if (isUnbacked || isRwx)
					{
						ForensicEntry entry = new ForensicEntry();
						entry.Kind = ForensicItemKind.InjectedThread;
						entry.OwnerPid = targetPid;
						entry.Name = "Поток #" + pt.Id + " (" + currentMinecraftName + ")";
						entry.Identifier = "0x" + startAddr.ToString("X");
						entry.TargetProcess = currentMinecraftName;
						entry.FilePath = isUnbacked ? "[Unbacked / Dynamic Memory]" : matchedModule.FileName;

						if (isUnbacked && isRwx)
						{
							entry.IsCritical = true;
							entry.Verdict = "🚨 UNBACKED RWX ПОТОК (5.01)";
							entry.ForeColor = Ui.AccentRed;
							entry.ViolationRule = "Пункт 5.01 (Инжектированный шеллкод)";
							entry.BanReason = string.Format("Инжектированный unbacked RWX поток 0x{0:X} в javaw", addr);
							entry.Details = string.Format(
								"🚨 КРИТИЧЕСКАЯ УЛИКА: В MINECRAFT ОБНАРУЖЕН АНОМАЛЬНЫЙ ПОТОК!\r\n" +
								"• Thread ID: {0}\r\n" +
								"• Стартовый адрес: 0x{1:X}\r\n" +
								"• Тип памяти: {2}\r\n" +
								"• Защита: PAGE_EXECUTE_READWRITE (RWX)\r\n" +
								"• Размер региона: {3:N0} байт\r\n" +
								"• Модуль: ОТСУТСТВУЕТ (Код не привязан ни к одной легитимной DLL на диске!).\r\n" +
								"• Диагностика: Выполняется скрытый шеллкод инжектора (Manual Map / Reflective DLL Injection).",
								pt.Id, addr, (mbi.Type == MEM_PRIVATE ? "MEM_PRIVATE" : "MEM_MAPPED"), mbi.RegionSize);
						}
						else if (isUnbacked)
						{
							entry.IsSuspicious = true;
							entry.Verdict = "⚠️ ПОТОК БЕЗ МОДУЛЯ (Unbacked)";
							entry.ForeColor = Ui.AccentOrange;
							entry.ViolationRule = "Пункт 5.01 (Подозрительный поток)";
							entry.BanReason = string.Format("Unbacked поток 0x{0:X} в javaw", addr);
							entry.Details = string.Format(
								"⚠️ Поток #{0} стартует по адресу 0x{1:X} вне пространства загруженных библиотек.\r\n" +
								"Тип памяти: {2}, Защита: 0x{3:X}.",
								pt.Id, addr, (mbi.Type == MEM_PRIVATE ? "MEM_PRIVATE" : "MEM_MAPPED"), mbi.Protect);
						}
						results.Add(entry);
					}
				}
			}
			catch { }
			finally
			{
				CloseHandle(hProcess);
			}
		}

		private void ScanTargetLoadedModules(List<ForensicEntry> results, int targetPid)
		{
			try
			{
				Process p = Process.GetProcessById(targetPid);
				string tempPath = Path.GetTempPath().ToLowerInvariant();
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToLowerInvariant();
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToLowerInvariant();
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToLowerInvariant();
				string downloads = Path.Combine(userProfile, "downloads").ToLowerInvariant();

				foreach (ProcessModule m in p.Modules)
				{
					string fn = m.ModuleName.ToLowerInvariant();
					string fullPath = (m.FileName ?? "").ToLowerInvariant();

					bool isCheatName = CheatNamePattern.IsMatch(fn) || CheatNamePattern.IsMatch(fullPath);
					bool isSuspiciousLocation = fullPath.StartsWith(tempPath) || fullPath.StartsWith(downloads) ||
						(fullPath.Contains("temp\\") && !fullPath.Contains("jre"));

					// Whitelist standard Minecraft and OS DLLs
					bool isSystemDll = fullPath.Contains("windows\\system32") || fullPath.Contains("windows\\syswow64");
					bool isJreDll = fullPath.Contains("bin\\server") || fullPath.Contains("bin\\client") || fullPath.Contains("runtime") ||
						fn.Equals("jvm.dll") || fn.Equals("java.dll") || fn.Equals("net.dll") || fn.Equals("nio.dll") ||
						fn.Equals("awt.dll") || fn.Equals("fontmanager.dll") || fn.Equals("javajpeg.dll");

					bool isGameDriver = fn.Contains("nvoglv") || fn.Contains("nvwgf2") || fn.Contains("amdvlk") ||
						fn.Contains("atig") || fn.Contains("ig9ic") || fn.Contains("opengl32") || fn.Contains("d3d");

					bool isEngineDll = fn.Equals("glfw.dll") || fn.Equals("openal.dll") || fn.Equals("openal32.dll") ||
						fn.Equals("jemalloc.dll") || fn.Equals("lwjgl.dll");

					bool isKnownOverlay = fn.Contains("discordhook") || fn.Contains("graphics-hook") || fn.Contains("rtss") ||
						fn.Contains("steam_api") || fn.Contains("gameoverlayrenderer");

					if (isCheatName || isSuspiciousLocation)
					{
						ForensicEntry entry = new ForensicEntry();
						entry.Kind = ForensicItemKind.LoadedModule;
						entry.OwnerPid = targetPid;
						entry.Name = m.ModuleName;
						entry.Identifier = "0x" + m.BaseAddress.ToString("X");
						entry.TargetProcess = currentMinecraftName;
						entry.FilePath = m.FileName;

						if (isCheatName)
						{
							entry.IsCritical = true;
							entry.Verdict = "🚨 ЧИТ-МОДУЛЬ DLL (5.01)";
							entry.ForeColor = Ui.AccentRed;
							entry.ViolationRule = "Пункт 5.01 (Инжектированная DLL чита)";
							entry.BanReason = string.Format("Чит-DLL {0} в памяти javaw", m.ModuleName);
							entry.Details = string.Format(
								"🚨 В ПАМЯТИ MINECRAFT ОБНАРУЖЕНА СТОРОННЯЯ ЧИТ-БИБЛИОТЕКА!\r\n" +
								"• DLL: {0}\r\n" +
								"• Базовый адрес: 0x{1:X}\r\n" +
								"• Путь: {2}\r\n" +
								"• Размер: {3:N0} байт\r\n" +
								"• Вердикт: Использование стороннего софта / DLL-инжекция по Регламенту 5.01.",
								m.ModuleName, m.BaseAddress.ToInt64(), m.FileName, m.ModuleMemorySize);
						}
						else
						{
							entry.IsSuspicious = true;
							entry.Verdict = "⚠️ ПОДОЗРИТЕЛЬНАЯ DLL ИЗ TEMP";
							entry.ForeColor = Ui.AccentOrange;
							entry.ViolationRule = "Пункт 5.01 (DLL из временной папки)";
							entry.BanReason = string.Format("DLL из Temp {0} в javaw", m.ModuleName);
							entry.Details = string.Format(
								"⚠️ Библиотека {0} загружена из временной директории Temp/Downloads.\r\nПуть: {1}",
								m.ModuleName, m.FileName);
						}
						results.Add(entry);
					}
					else if (isKnownOverlay)
					{
						ForensicEntry entry = new ForensicEntry();
						entry.Kind = ForensicItemKind.LoadedModule;
						entry.OwnerPid = targetPid;
						entry.Name = m.ModuleName;
						entry.Identifier = "0x" + m.BaseAddress.ToString("X");
						entry.TargetProcess = currentMinecraftName;
						entry.FilePath = m.FileName;
						entry.IsWhitelisted = true;
						entry.Verdict = "🛡️ РАЗРЕШЕНО (Оверлей DLL)";
						entry.ForeColor = Ui.AccentGreen;
						entry.ViolationRule = "Хук захвата экрана / оверлея";
						entry.Details = string.Format("Легитимный хук захвата экрана/оверлея ({0}).\r\nПуть: {1}", m.ModuleName, m.FileName);
						results.Add(entry);
					}
				}
			}
			catch { }
		}

		private string GetProcessPathSafe(int pid)
		{
			try
			{
				IntPtr hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
				if (hProc != IntPtr.Zero)
				{
					StringBuilder sb = new StringBuilder(1024);
					int size = sb.Capacity;
					if (QueryFullProcessImageName(hProc, 0, sb, ref size))
					{
						CloseHandle(hProc);
						return sb.ToString();
					}
					CloseHandle(hProc);
				}
			}
			catch { }

			try
			{
				return Process.GetProcessById(pid).MainModule.FileName;
			}
			catch
			{
				return "";
			}
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilters()
		{
			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			List<ForensicEntry> filtered = allItems.Where(item => {
				// Mode filter
				switch (currentFilter)
				{
					case HandleFilterMode.InjectorsOnly:
						if (!item.IsCritical) return false;
						break;
					case HandleFilterMode.VmReadOnly:
						if (!item.IsSuspicious) return false;
						break;
					case HandleFilterMode.UnbackedThreads:
						if (item.Kind != ForensicItemKind.InjectedThread) return false;
						break;
					case HandleFilterMode.SuspiciousDlls:
						if (item.Kind != ForensicItemKind.LoadedModule) return false;
						break;
					case HandleFilterMode.Whitelist:
						if (!item.IsWhitelisted) return false;
						break;
				}

				// Search query filter
				if (!string.IsNullOrEmpty(query))
				{
					bool matchName = (item.Name ?? "").ToLowerInvariant().Contains(query);
					bool matchIdent = (item.Identifier ?? "").ToLowerInvariant().Contains(query);
					bool matchPath = (item.FilePath ?? "").ToLowerInvariant().Contains(query);
					bool matchVerdict = (item.Verdict ?? "").ToLowerInvariant().Contains(query);
					bool matchDetails = (item.Details ?? "").ToLowerInvariant().Contains(query);
					if (!matchName && !matchIdent && !matchPath && !matchVerdict && !matchDetails) return false;
				}

				return true;
			}).ToList();

			// Sort critical items to the top
			filtered = filtered.OrderByDescending(i => i.IsCritical)
				.ThenByDescending(i => i.IsSuspicious)
				.ThenBy(i => i.IsWhitelisted)
				.ThenBy(i => i.Name)
				.ToList();

			listView.BeginUpdate();
			listView.Items.Clear();

			foreach (ForensicEntry item in filtered)
			{
				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.ForeColor = item.ForeColor;
				lvi.SubItems.Add(item.Name ?? "");
				lvi.SubItems.Add(item.Identifier ?? "");

				string accessStr = "";
				if (item.AccessMask > 0)
				{
					accessStr = "0x" + item.AccessMask.ToString("X");
					if ((item.AccessMask & PROCESS_VM_WRITE) != 0) accessStr += " [VM_WRITE]";
					if ((item.AccessMask & PROCESS_ALL_ACCESS) == PROCESS_ALL_ACCESS) accessStr += " [ALL_ACCESS]";
					else if ((item.AccessMask & PROCESS_VM_READ) != 0) accessStr += " [VM_READ]";
				}
				else if (item.Kind == ForensicItemKind.InjectedThread)
				{
					accessStr = "Unbacked Поток";
				}
				else if (item.Kind == ForensicItemKind.LoadedModule)
				{
					accessStr = "Загруженная DLL";
				}
				lvi.SubItems.Add(accessStr);

				lvi.SubItems.Add(item.TargetProcess ?? "");
				lvi.SubItems.Add(item.FilePath ?? "");
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
				ClearDetails();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetails();
				return;
			}

			ForensicEntry item = listView.SelectedItems[0].Tag as ForensicEntry;
			if (item == null) return;

			lblDetailVerdict.Text = item.Verdict;
			lblDetailVerdict.ForeColor = item.ForeColor;
			lblDetailRule.Text = "Регламент: " + (item.ViolationRule ?? "Общий аудит");

			if (item.IsCritical || item.IsSuspicious)
			{
				string reason = !string.IsNullOrEmpty(item.BanReason) ? item.BanReason : item.Name;
				lblDetailBanCommand.Text = string.Format("Команда: /ban {0} 30d 5.01 ({1})", targetPlayerName, reason);
			}
			else
			{
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details;
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Объект не выбран";
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailRule.Text = "Регламент: -";
			lblDetailBanCommand.Text = "";
			txtDetails.Text = "";
		}

		#endregion

		#region Actions

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				// Check if there is any critical item in the full list
				ForensicEntry firstCrit = allItems.FirstOrDefault(i => i.IsCritical);
				if (firstCrit != null)
				{
					string ban = string.Format("/ban {0} 30d 5.01 ({1})", targetPlayerName, firstCrit.BanReason ?? firstCrit.Name);
					Clipboard.SetText(ban);
					MessageBox.Show("Скопирована команда бана за критический инжектор:\r\n\r\n" + ban, "Команда бана 5.01", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				MessageBox.Show("Выберите подозрительный объект из списка для копирования команды бана.", "Инспектор дескрипторов", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ForensicEntry sel = listView.SelectedItems[0].Tag as ForensicEntry;
			if (sel != null)
			{
				string reason = !string.IsNullOrEmpty(sel.BanReason) ? sel.BanReason : sel.Name;
				string cmd = string.Format("/ban {0} 30d 5.01 ({1})", targetPlayerName, reason);
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Бан 5.01 скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void KillSelectedProcess()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите процесс из списка для завершения.", "Завершение процесса", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ForensicEntry sel = listView.SelectedItems[0].Tag as ForensicEntry;
			if (sel == null || sel.OwnerPid <= 4)
			{
				MessageBox.Show("Невозможно завершить данный системный объект.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (sel.IsWhitelisted)
			{
				DialogResult drW = MessageBox.Show(
					"Внимание! Процесс " + sel.Name + " находится в белом списке разрешенного ПО.\r\nВы действительно хотите его принудительно завершить?",
					"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (drW != DialogResult.Yes) return;
			}
			else
			{
				DialogResult dr = MessageBox.Show(
					"Завершить подозрительный процесс " + sel.Name + " (PID: " + sel.OwnerPid + ")?",
					"Завершение процесса чита", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (dr != DialogResult.Yes) return;
			}

			try
			{
				Process p = Process.GetProcessById(sel.OwnerPid);
				p.Kill();
				p.WaitForExit(1000);
				MessageBox.Show("Процесс " + sel.Name + " успешно завершен!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
				StartScanAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка завершения процесса: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportProtocolToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АУДИТА ДЕСКРИПТОРОВ И ИНЖЕКЦИЙ ПАМЯТИ ===");
			sb.AppendLine(string.Format("👤 Проверяемый игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("🎯 Целевой процесс игры: {0} (PID: {1})", currentMinecraftName, currentMinecraftPid));
			sb.AppendLine(string.Format("📅 Дата и время анализа: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Всего записей в анализе: {0} (Критических инжекторов: {1}, Подозрительных: {2})",
				allItems.Count, allItems.Count(i => i.IsCritical), allItems.Count(i => i.IsSuspicious)));
			sb.AppendLine(new string('-', 85));

			foreach (ForensicEntry item in allItems.Where(i => i.IsCritical || i.IsSuspicious))
			{
				sb.AppendLine(string.Format("[{0}] {1}", item.Verdict, item.Name));
				sb.AppendLine(string.Format("   Дескриптор/Адрес: {0} | Права доступа: 0x{1:X}", item.Identifier, item.AccessMask));
				sb.AppendLine(string.Format("   Файл на диске: {0}", item.FilePath));
				sb.AppendLine(string.Format("   Нарушение: {0}", item.ViolationRule));
				if (!string.IsNullOrEmpty(item.BanReason))
				{
					sb.AppendLine(string.Format("   Рекомендуемый бан: /ban {0} 30d 5.01 ({1})", targetPlayerName, item.BanReason));
				}
				sb.AppendLine();
			}

			if (allItems.Count(i => i.IsCritical || i.IsSuspicious) == 0)
			{
				sb.AppendLine("✔ Опасных инжекторов, unbacked потоков и сторонних чит-DLL в javaw.exe не обнаружено.");
			}

			sb.AppendLine(new string('=', 85));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита дескрипторов успешно скопирован в буфер обмена!", "Экспорт протокола", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (autoRefreshTimer != null)
			{
				autoRefreshTimer.Stop();
				autoRefreshTimer.Dispose();
				autoRefreshTimer = null;
			}
			base.OnFormClosing(e);
		}

		#endregion
	}
}
