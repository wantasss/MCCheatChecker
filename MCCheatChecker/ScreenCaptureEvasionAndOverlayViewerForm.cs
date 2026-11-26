using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ScreenCaptureEvasionAndOverlayViewerForm : Form
	{
		#region Win32 P/Invoke & Structures

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		private static long GetWindowLongAuto(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8)
			{
				return GetWindowLongPtr64(hWnd, nIndex).ToInt64();
			}
			return GetWindowLongPtr32(hWnd, nIndex).ToInt64();
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hWnd);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			public int Width { get { return Right - Left; } }
			public int Height { get { return Bottom - Top; } }
		}

		private const int GWL_STYLE = -16;
		private const int GWL_EXSTYLE = -20;

		private const long WS_VISIBLE = 0x10000000L;
		private const long WS_POPUP = 0x80000000L;
		private const long WS_CHILD = 0x40000000L;

		private const long WS_EX_LAYERED = 0x00080000L;
		private const long WS_EX_TRANSPARENT = 0x00000020L;
		private const long WS_EX_TOPMOST = 0x00000008L;
		private const long WS_EX_NOACTIVATE = 0x08000000L;
		private const long WS_EX_TOOLWINDOW = 0x00000080L;

		private const uint WDA_NONE = 0x00;
		private const uint WDA_MONITOR = 0x01;
		private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

		#endregion

		#region Enums & Data Models

		public enum OverlayItemCategory
		{
			WdaScreenBypass508,
			StealthEspOverlay501,
			SuspiciousOverlayHook501,
			SuspiciousLayeredWindow,
			MinecraftWindow,
			LegitimateSoftware,
			WindowsSystem
		}

		public enum OverlayFilterCategory
		{
			All,
			ViolationsOnly,
			WdaScreenBypass,
			StealthOverlays,
			HookDllsInJava,
			LegitimateAndSystem
		}

		public class ScreenOverlayItem
		{
			public int Index;
			public OverlayItemCategory Category;
			public string CategoryName;
			public IntPtr Hwnd;
			public string HwndHex;
			public string WindowTitle;
			public string WindowClassName;
			public string ProcessName;
			public uint ProcessPid;
			public string ProcessPath;
			public string ProcessCommandLine;
			public string Bounds;
			public int Width;
			public int Height;
			public int Left;
			public int Top;
			public uint Affinity;
			public string AffinityName;
			public long ExStyle;
			public string ExStyleHex;
			public string ExStyleFlags;
			public long Style;
			public bool IsVisible;
			public bool IsTopMost;
			public bool IsLayered;
			public bool IsTransparent;
			public bool IsNoActivate;
			public bool IsToolWindow;
			public bool MatchesMinecraftGeometry;
			public bool IsFullScreen;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsWdaBypass;
			public bool IsStealthOverlay;
			public bool IsHookDll;
			public Color RowColor;
			public string ForensicReport;
			public string TechnicalDetails;
			public string ProcessAndModules;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private OverlayFilterCategory currentFilter = OverlayFilterCategory.All;
		private List<ScreenOverlayItem> allItems = new List<ScreenOverlayItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtTechnicalDetails;
		private DarkTextBoxMultiline txtProcessAndModules;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiWdaBypass;
		private Label lblKpiStealthOverlays;
		private Label lblKpiHookDlls;
		private Label lblKpiLegitimate;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterWda;
		private DarkButton btnFilterOverlays;
		private DarkButton btnFilterHooks;
		private DarkButton btnFilterLegit;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public ScreenCaptureEvasionAndOverlayViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🪟 Инспектор сокрытия экрана DWM, OBS/Discord Bypass и графических оверлеев — MCCheatChecker";
			Size = new Size(1340, 820);
			MinimumSize = new Size(1100, 640);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;

			// Header Panel
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🪟 ИНСПЕКТОР СОКРЫТИЯ ЭКРАНА DWM, OBS/DISCORD BYPASS И ОВЕРЛЕЕВ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит оконных дескрипторов HWND, SetWindowDisplayAffinity (WDA_EXCLUDEFROMCAPTURE), стилей WS_EX_LAYERED/TRANSPARENT и оверлей-хуков в Java (5.08 / 5.01)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ОКОН И ХУКОВ...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(250, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Container
			Panel topContainer = new Panel();
			topContainer.Dock = DockStyle.Top;
			topContainer.Height = 138;
			topContainer.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("🪟 Всего окон/хуков", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.08/5.01)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ DWM Screen Bypass", "0", Ui.AccentRed, ref kpiX, out lblKpiWdaBypass));
			kpiPanel.Controls.Add(CreateKpiBadge("👻 Скрытые ESP Оверлеи", "0", Ui.AccentOrange, ref kpiX, out lblKpiStealthOverlays));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Оверлей-хуки в Java", "0", Ui.AccentTeal, ref kpiX, out lblKpiHookDlls));
			kpiPanel.Controls.Add(CreateKpiBadge("✅ Легитимные окна", "0", Ui.AccentGreen, ref kpiX, out lblKpiLegitimate));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все объекты", OverlayFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08/5.01)", OverlayFilterCategory.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterWda = CreateFilterButton("🛡️ DWM Bypass (WDA)", OverlayFilterCategory.WdaScreenBypass, ref fBtnX, filterBar);
			btnFilterOverlays = CreateFilterButton("👻 Скрытые оверлеи", OverlayFilterCategory.StealthOverlays, ref fBtnX, filterBar);
			btnFilterHooks = CreateFilterButton("💉 Оверлей-хуки DLL", OverlayFilterCategory.HookDllsInJava, ref fBtnX, filterBar);
			btnFilterLegit = CreateFilterButton("✅ Легитимные окна", OverlayFilterCategory.LegitimateAndSystem, ref fBtnX, filterBar);

			// Search Box
			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(fBtnX + 16, 13);
			filterBar.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(fBtnX + 78, 10);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Controls.Add(filterBar);

			// Main Splitter Container (ListView on top, Detail Tabs on bottom)
			SplitContainer splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 5;
			splitContainer.BackColor = Ui.Border;

			// DarkListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;

			listView.Columns.Add("№", 45, HorizontalAlignment.Left);
			listView.Columns.Add("Категория проверки", 155, HorizontalAlignment.Left);
			listView.Columns.Add("Заголовок окна / Имя модуля", 240, HorizontalAlignment.Left);
			listView.Columns.Add("Процесс (PID)", 150, HorizontalAlignment.Left);
			listView.Columns.Add("DWM Защита экрана (Affinity)", 190, HorizontalAlignment.Left);
			listView.Columns.Add("Стили окна (EX_STYLE)", 175, HorizontalAlignment.Left);
			listView.Columns.Add("Разрешение / Bounds", 130, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 260, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate { UpdateDetailViews(); };
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Container (Tabs + Action Buttons)
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(12, 7, 12, 7);

			btnCopySelected = new DarkButton();
			btnCopySelected.Text = "📋 Копировать запись";
			btnCopySelected.Size = new Size(160, 28);
			btnCopySelected.Location = new Point(12, 8);
			btnCopySelected.Click += delegate { CopySelectedRow(); };
			actionBar.Controls.Add(btnCopySelected);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Команда бана (/ban)";
			btnCopyBanCmd.Size = new Size(185, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(566, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(724, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			btnClose = new DarkButton();
			btnClose.Text = "✖️ Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(887, 8);
			btnClose.Click += delegate { Close(); };
			actionBar.Controls.Add(btnClose);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Регламент");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabTech = new TabPage("🔍 Параметры Win32 (HWND, Стили, Affinity, Rect)");
			tabTech.BackColor = Ui.Surface;
			txtTechnicalDetails = new DarkTextBoxMultiline();
			txtTechnicalDetails.Dock = DockStyle.Fill;
			txtTechnicalDetails.ReadOnly = true;
			txtTechnicalDetails.Font = Ui.FontMono(9f);
			tabTech.Controls.Add(txtTechnicalDetails);
			detailTabs.TabPages.Add(tabTech);

			TabPage tabModules = new TabPage("⚡ Процесс и графические модули");
			tabModules.BackColor = Ui.Surface;
			txtProcessAndModules = new DarkTextBoxMultiline();
			txtProcessAndModules.Dock = DockStyle.Fill;
			txtProcessAndModules.ReadOnly = true;
			txtProcessAndModules.Font = Ui.FontMono(9f);
			tabModules.Controls.Add(txtProcessAndModules);
			detailTabs.TabPages.Add(tabModules);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(170, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.SurfaceAlt;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(164, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(164, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 178;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, OverlayFilterCategory cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8f);
			btn.Size = new Size(150, 28);
			btn.Location = new Point(curX, 8);
			btn.Click += delegate
			{
				currentFilter = cat;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.BackColor = (currentFilter == OverlayFilterCategory.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == OverlayFilterCategory.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == OverlayFilterCategory.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == OverlayFilterCategory.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterWda.BackColor = (currentFilter == OverlayFilterCategory.WdaScreenBypass) ? Ui.AccentRed : Ui.Surface;
			btnFilterWda.ForeColor = (currentFilter == OverlayFilterCategory.WdaScreenBypass) ? Color.White : Ui.TextPrimary;

			btnFilterOverlays.BackColor = (currentFilter == OverlayFilterCategory.StealthOverlays) ? Ui.AccentOrange : Ui.Surface;
			btnFilterOverlays.ForeColor = (currentFilter == OverlayFilterCategory.StealthOverlays) ? Color.Black : Ui.TextPrimary;

			btnFilterHooks.BackColor = (currentFilter == OverlayFilterCategory.HookDllsInJava) ? Ui.AccentTeal : Ui.Surface;
			btnFilterHooks.ForeColor = (currentFilter == OverlayFilterCategory.HookDllsInJava) ? Color.Black : Ui.TextPrimary;

			btnFilterLegit.BackColor = (currentFilter == OverlayFilterCategory.LegitimateAndSystem) ? Ui.AccentGreen : Ui.Surface;
			btnFilterLegit.ForeColor = (currentFilter == OverlayFilterCategory.LegitimateAndSystem) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ОКОН И ХУКОВ...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ScreenOverlayItem> results = new List<ScreenOverlayItem>();
			int itemIndex = 1;

			// Step 1: Discover visible Minecraft javaw.exe / java.exe windows
			IntPtr mcHwnd = IntPtr.Zero;
			RECT mcRect = default(RECT);
			List<IntPtr> mcHwnds = new List<IntPtr>();
			List<RECT> mcRects = new List<RECT>();
			bool hasMcWindow = false;
			uint mcPid = 0;
			string mcProcName = "";
			string mcProcPath = "";

			try
			{
				HashSet<uint> mcPids = new HashSet<uint>();
				Dictionary<uint, Process> mcProcMap = new Dictionary<uint, Process>();
				foreach (Process jp in Process.GetProcessesByName("javaw"))
				{
					try { mcPids.Add((uint)jp.Id); mcProcMap[(uint)jp.Id] = jp; } catch { }
				}
				foreach (Process jp in Process.GetProcessesByName("java"))
				{
					try { mcPids.Add((uint)jp.Id); mcProcMap[(uint)jp.Id] = jp; } catch { }
				}

				if (mcPids.Count > 0)
				{
					EnumWindows(delegate(IntPtr hw, IntPtr lp)
					{
						try
						{
							if (!IsWindowVisible(hw)) return true;
							uint wPid = 0;
							GetWindowThreadProcessId(hw, out wPid);
							if (wPid != 0 && mcPids.Contains(wPid))
							{
								RECT rMc;
								if (GetWindowRect(hw, out rMc) && rMc.Width >= 200 && rMc.Height >= 200)
								{
									mcHwnds.Add(hw);
									mcRects.Add(rMc);
									if (!hasMcWindow)
									{
										mcHwnd = hw;
										mcRect = rMc;
										mcPid = wPid;
										Process mp;
										if (mcProcMap.TryGetValue(wPid, out mp) && mp != null)
										{
											mcProcName = mp.ProcessName;
											try { mcProcPath = mp.MainModule.FileName; } catch { }
										}
										hasMcWindow = true;
									}
								}
							}
						}
						catch { }
						return true;
					}, IntPtr.Zero);
				}
			}
			catch { }

			int screenW = Screen.PrimaryScreen.Bounds.Width;
			int screenH = Screen.PrimaryScreen.Bounds.Height;

			// If Minecraft window found, record it as a reference item
			if (hasMcWindow)
			{
				StringBuilder mcTitleSb = new StringBuilder(512);
				GetWindowText(mcHwnd, mcTitleSb, 512);
				StringBuilder mcClassSb = new StringBuilder(256);
				GetClassName(mcHwnd, mcClassSb, 256);

				uint mcAffinity = 0;
				GetWindowDisplayAffinity(mcHwnd, out mcAffinity);
				long mcExStyle = GetWindowLongAuto(mcHwnd, GWL_EXSTYLE);
				long mcStyle = GetWindowLongAuto(mcHwnd, GWL_STYLE);

				ScreenOverlayItem mcItem = new ScreenOverlayItem
				{
					Index = itemIndex++,
					Category = OverlayItemCategory.MinecraftWindow,
					CategoryName = "Игровой клиент",
					Hwnd = mcHwnd,
					HwndHex = "0x" + mcHwnd.ToInt64().ToString("X8"),
					WindowTitle = mcTitleSb.ToString().Trim(),
					WindowClassName = mcClassSb.ToString().Trim(),
					ProcessName = mcProcName + ".exe",
					ProcessPid = mcPid,
					ProcessPath = mcProcPath,
					ProcessCommandLine = GetProcessCommandLine(mcPid),
					Width = mcRect.Width,
					Height = mcRect.Height,
					Left = mcRect.Left,
					Top = mcRect.Top,
					Bounds = string.Format("{0}x{1} at ({2}, {3})", mcRect.Width, mcRect.Height, mcRect.Left, mcRect.Top),
					Affinity = mcAffinity,
					AffinityName = GetAffinityName(mcAffinity),
					ExStyle = mcExStyle,
					ExStyleHex = "0x" + mcExStyle.ToString("X8"),
					ExStyleFlags = ParseExStyleFlags(mcExStyle),
					Style = mcStyle,
					IsVisible = true,
					IsTopMost = (mcExStyle & WS_EX_TOPMOST) != 0,
					IsLayered = (mcExStyle & WS_EX_LAYERED) != 0,
					IsTransparent = (mcExStyle & WS_EX_TRANSPARENT) != 0,
					IsNoActivate = (mcExStyle & WS_EX_NOACTIVATE) != 0,
					IsToolWindow = (mcExStyle & WS_EX_TOOLWINDOW) != 0,
					MatchesMinecraftGeometry = true,
					IsFullScreen = mcRect.Width >= screenW - 10 && mcRect.Height >= screenH - 10,
					TargetRule = "Allowed",
					Verdict = "🎮 Активное окно клиента Minecraft (Базовый ориентир геометрии)",
					IsViolation = false,
					IsWdaBypass = false,
					IsStealthOverlay = false,
					IsHookDll = false,
					RowColor = Ui.AccentGreen
				};

				mcItem.ForensicReport = BuildForensicReport(mcItem);
				mcItem.TechnicalDetails = BuildTechnicalDetails(mcItem, mcRect);
				mcItem.ProcessAndModules = BuildProcessDetails(mcItem);
				results.Add(mcItem);
			}

			// Step 2: Enumerate Loaded Modules in Minecraft for Hook Injections
			try
			{
				Process[] javaProcs = Process.GetProcessesByName("javaw");
				if (javaProcs == null || javaProcs.Length == 0)
				{
					javaProcs = Process.GetProcessesByName("java");
				}

				if (javaProcs != null)
				{
					foreach (Process proc in javaProcs)
					{
						try
						{
							foreach (ProcessModule mod in proc.Modules)
							{
								try
								{
									string mName = (mod.ModuleName ?? "").ToLowerInvariant();
									string mPath = (mod.FileName ?? "").ToLowerInvariant();

									bool isHookViolation = false;
									string hookViolationTitle = "";
									string hookViolationDetails = "";

									if (mName == "discordhook64.dll" || mName == "discordhook.dll")
									{
										if (!mPath.Contains("\\discord\\app-") && !mPath.Contains("\\discordptb\\app-") && !mPath.Contains("\\discordcanary\\app-"))
										{
											isHookViolation = true;
											hookViolationTitle = "🚨 5.01 / 5.08 ФАЛЬШИВЫЙ DISCORD HOOK (Инъекция чита)";
											hookViolationDetails = "Модуль с именем discordhook загружен НЕ из каталога Discord (%LocalAppData%\\Discord\\app-*), а из стороннего пути (" + mod.FileName + "). Используется читами для рендера ESP через поддельный хук оверлея!";
										}
									}
									else if (mName == "gameoverlayrenderer64.dll" || mName == "gameoverlayrenderer.dll")
									{
										if (!mPath.Contains("\\steam\\"))
										{
											isHookViolation = true;
											hookViolationTitle = "🚨 5.01 ФАЛЬШИВЫЙ STEAM OVERLAY HOOK";
											hookViolationDetails = "Модуль GameOverlayRenderer загружен НЕ из папки Steam (" + mod.FileName + "). Чит мимикрирует под оверлей Steam для перехвата рендера OpenGL/DirectX.";
										}
									}
									else if (mName == "rtsshooks64.dll" || mName == "rtsshooks.dll")
									{
										if (!mPath.Contains("rivatuner") && !mPath.Contains("guru3d"))
										{
											isHookViolation = true;
											hookViolationTitle = "🚨 5.01 ФАЛЬШИВЫЙ RTSS HOOK";
											hookViolationDetails = "Модуль RTSSHooks загружен из недостоверного расположения (" + mod.FileName + ").";
										}
									}
									else if (mName.EndsWith(".dll") && (mPath.Contains("\\temp\\") || mPath.Contains("\\appdata\\local\\temp\\")))
									{
										// Exclude standard legitimate Minecraft unpacked native libraries
										if (!mName.Contains("jna") && !mName.Contains("jinput") && !mName.Contains("lwjgl") &&
											!mName.Contains("openal") && !mName.Contains("jemalloc") && !mName.Contains("freetype") &&
											!mName.Contains("shaderc") && !mName.Contains("spirv") && !mName.Contains("glfw") &&
											!mName.Contains("opus") && !mName.Contains("rnnoise") && !mName.Contains("speex") &&
											!mName.Contains("lame") && !mName.Contains("evo") && !mPath.Contains("lwjgl"))
										{
											isHookViolation = true;
											hookViolationTitle = "⚠️ 5.01 ПОДОЗРИТЕЛЬНАЯ DLL В ПАМЯТИ JAVA ИЗ ПАПКИ TEMP";
											hookViolationDetails = "В процесс Minecraft загружена динамическая библиотека прямо из временной папки Temp (" + mod.FileName + "). Внешние читы и инжекторы распаковывают полезную нагрузку в %TEMP%.";
										}
									}

									if (isHookViolation)
									{
										ScreenOverlayItem hookItem = new ScreenOverlayItem
										{
											Index = itemIndex++,
											Category = OverlayItemCategory.SuspiciousOverlayHook501,
											CategoryName = "Инъекция хука в Java",
											Hwnd = IntPtr.Zero,
											HwndHex = "DLL_INJECTION",
											WindowTitle = mod.ModuleName,
											WindowClassName = "ProcessModule: " + mod.ModuleName,
											ProcessName = proc.ProcessName + ".exe",
											ProcessPid = (uint)proc.Id,
											ProcessPath = mod.FileName,
											ProcessCommandLine = GetProcessCommandLine((uint)proc.Id),
											Bounds = "Память javaw.exe (Базовый адрес: 0x" + mod.BaseAddress.ToInt64().ToString("X8") + ")",
											Width = 0,
											Height = 0,
											Left = 0,
											Top = 0,
											Affinity = 0,
											AffinityName = "N/A (DLL Module)",
											ExStyle = 0,
											ExStyleHex = "0x00000000",
											ExStyleFlags = "N/A",
											Style = 0,
											IsVisible = true,
											IsTopMost = false,
											IsLayered = false,
											IsTransparent = false,
											IsNoActivate = false,
											IsToolWindow = false,
											MatchesMinecraftGeometry = false,
											IsFullScreen = false,
											TargetRule = "5.01 / 5.08",
											Verdict = hookViolationTitle,
											IsViolation = true,
											IsWdaBypass = false,
											IsStealthOverlay = false,
											IsHookDll = true,
											RowColor = Ui.AccentRed
										};

										hookItem.ForensicReport = string.Format("=== СУДЕБНЫЙ ПРОТОКОЛ: ОВЕРЛЕЙ-ХУК В ПАМЯТИ MINECRAFT ===\r\n" +
											"Подозреваемый игрок: {0}\r\n" +
											"Дата и время: {1:dd.MM.yyyy HH:mm:ss}\r\n" +
											"Процесс: {2} (PID: {3})\r\n" +
											"Библиотека: {4}\r\n" +
											"Полный путь: {5}\r\n" +
											"Регламент: 5.01 (Использование читов) / 5.08 (Сокрытие софта)\r\n\r\n" +
											"ОПИСАНИЕ УЛИКИ:\r\n{6}\r\n\r\n" +
											"РЕКОМЕНДУЕМЫЙ БАН:\r\n/ban {0} 30d 5.01 Использование читов (Фальшивый DLL-хук {4})",
											targetPlayerName, DateTime.Now, proc.ProcessName, proc.Id, mod.ModuleName, mod.FileName, hookViolationDetails);

										hookItem.TechnicalDetails = string.Format("Модуль: {0}\r\nБазовый адрес: 0x{1:X8}\r\nРазмер памяти: {2:N0} байт\r\nПуть: {3}",
											mod.ModuleName, mod.BaseAddress.ToInt64(), mod.ModuleMemorySize, mod.FileName);

										hookItem.ProcessAndModules = string.Format("Процесс-хост: {0} (PID: {1})\r\nПуть хоста: {2}\r\nМодуль: {3}",
											proc.ProcessName, proc.Id, proc.MainModule.FileName, mod.FileName);

										results.Add(hookItem);
									}
								}
								catch { }
							}
						}
						catch { }
					}
				}
			}
			catch { }

			// Step 3: Enumerate Windows via EnumWindows
			uint myPid = (uint)Process.GetCurrentProcess().Id;

			try
			{
				EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
				{
					try
					{
						if (hWnd == IntPtr.Zero || hWnd == mcHwnd || mcHwnds.Contains(hWnd))
						{
							return true;
						}

						uint pid = 0;
						GetWindowThreadProcessId(hWnd, out pid);
						if (pid == 0 || pid == myPid)
						{
							return true;
						}

						Process p = null;
						string pName = "";
						string pPath = "";
						try
						{
							p = Process.GetProcessById((int)pid);
							if (p != null)
							{
								pName = p.ProcessName.ToLowerInvariant();
								try { pPath = p.MainModule.FileName; } catch { }
							}
						}
						catch { }

						// Check Whitelist for Autoclickers & SS Tools (STRICTLY ALWAYS ALLOWED)
						bool isWhitelistedTool = IsWhitelistedSoftware(pName, pPath);

						// Fetch Window Titles & Class
						StringBuilder titleSb = new StringBuilder(512);
						GetWindowText(hWnd, titleSb, 512);
						string title = titleSb.ToString().Trim();

						StringBuilder classSb = new StringBuilder(256);
						GetClassName(hWnd, classSb, 256);
						string className = classSb.ToString().Trim();

						// Query Affinity (WDA)
						uint affinity = 0;
						bool hasAffinity = GetWindowDisplayAffinity(hWnd, out affinity);

						// Query Extended Styles
						long exStyle = GetWindowLongAuto(hWnd, GWL_EXSTYLE);
						long style = GetWindowLongAuto(hWnd, GWL_STYLE);

						RECT rect = default(RECT);
						GetWindowRect(hWnd, out rect);
						int w = rect.Width;
						int h = rect.Height;

						bool isVisible = IsWindowVisible(hWnd) || ((style & WS_VISIBLE) != 0);
						bool isTopMost = (exStyle & WS_EX_TOPMOST) != 0;
						bool isLayered = (exStyle & WS_EX_LAYERED) != 0;
						bool isTransparent = (exStyle & WS_EX_TRANSPARENT) != 0;
						bool isNoActivate = (exStyle & WS_EX_NOACTIVATE) != 0;
						bool isToolWindow = (exStyle & WS_EX_TOOLWINDOW) != 0;

						bool matchesMc = hasMcWindow &&
							Math.Abs(rect.Left - mcRect.Left) < 35 &&
							Math.Abs(rect.Top - mcRect.Top) < 35 &&
							Math.Abs(w - mcRect.Width) < 60 &&
							Math.Abs(h - mcRect.Height) < 60 &&
							w >= 250 && h >= 250;

						bool intersectsMc = false;
						for (int mr = 0; mr < mcRects.Count; mr++)
						{
							RECT rM = mcRects[mr];
							if (ManualChecks.DoRectsIntersect(rect.Left, rect.Top, rect.Right, rect.Bottom, rM.Left, rM.Top, rM.Right, rM.Bottom))
							{
								intersectsMc = true;
								break;
							}
						}

						bool isFullScreen = w >= screenW - 15 && h >= screenH - 15;

						// Check for Windows System / Core UI Whitelist
						bool isSystemProcess = IsSystemUiProcess(pName);

						// Check for Legitimate Overlays
						bool isLegitOverlay = IsLegitimateOverlay(pName, pPath);

						// EVALUATE VIOLATION CONDITIONS

						// Condition A: DWM Capture Bypass (WDA_EXCLUDEFROMCAPTURE = 0x11 or WDA_MONITOR = 0x01)
						bool isWdaBypass = isVisible && (affinity == WDA_EXCLUDEFROMCAPTURE || affinity == WDA_MONITOR) && !isSystemProcess && !isLegitOverlay && !isWhitelistedTool;

						// Condition B: Stealth Click-Through Transparent Topmost Overlay intersecting Minecraft
						bool isStealthOverlay = isVisible && isLayered && (isTransparent || isNoActivate) && isTopMost && (w >= 200 && h >= 200) && intersectsMc && !isSystemProcess && !isLegitOverlay && !isWhitelistedTool;

						// Condition C: Suspicious Class Name commonly used by cheat overlays
						bool isSuspiciousClass = IsSuspiciousOverlayClass(className);

						// If process is whitelisted tool (OP Auto Clicker, AnyDesk, etc.), never flag as violation
						if (isWhitelistedTool)
						{
							if (isLayered || isTopMost || affinity != 0 || matchesMc)
							{
								ScreenOverlayItem item = new ScreenOverlayItem
								{
									Index = itemIndex++,
									Category = OverlayItemCategory.LegitimateSoftware,
									CategoryName = "Легитимный софт",
									Hwnd = hWnd,
									HwndHex = "0x" + hWnd.ToInt64().ToString("X8"),
									WindowTitle = string.IsNullOrEmpty(title) ? "—" : title,
									WindowClassName = className,
									ProcessName = pName + ".exe",
									ProcessPid = pid,
									ProcessPath = pPath,
									ProcessCommandLine = GetProcessCommandLine(pid),
									Width = w,
									Height = h,
									Left = rect.Left,
									Top = rect.Top,
									Bounds = string.Format("{0}x{1} at ({2}, {3})", w, h, rect.Left, rect.Top),
									Affinity = affinity,
									AffinityName = GetAffinityName(affinity),
									ExStyle = exStyle,
									ExStyleHex = "0x" + exStyle.ToString("X8"),
									ExStyleFlags = ParseExStyleFlags(exStyle),
									Style = style,
									IsVisible = isVisible,
									IsTopMost = isTopMost,
									IsLayered = isLayered,
									IsTransparent = isTransparent,
									IsNoActivate = isNoActivate,
									IsToolWindow = isToolWindow,
									MatchesMinecraftGeometry = matchesMc,
									IsFullScreen = isFullScreen,
									TargetRule = "Allowed",
									Verdict = "✅ РАЗРЕШЕНО (Вайтлист регламента: Autoclicker / AnyDesk / RuDesktop)",
									IsViolation = false,
									IsWdaBypass = false,
									IsStealthOverlay = false,
									IsHookDll = false,
									RowColor = Ui.AccentGreen
								};
								item.ForensicReport = BuildForensicReport(item);
								item.TechnicalDetails = BuildTechnicalDetails(item, rect);
								item.ProcessAndModules = BuildProcessDetails(item);
								results.Add(item);
							}
							return true;
						}

						if (isWdaBypass)
						{
							ScreenOverlayItem item = new ScreenOverlayItem
							{
								Index = itemIndex++,
								Category = OverlayItemCategory.WdaScreenBypass508,
								CategoryName = "DWM Screen Bypass",
								Hwnd = hWnd,
								HwndHex = "0x" + hWnd.ToInt64().ToString("X8"),
								WindowTitle = string.IsNullOrEmpty(title) ? "<Без заголовка (Stealth)>" : title,
								WindowClassName = className,
								ProcessName = (p != null ? p.ProcessName : "PID " + pid) + ".exe",
								ProcessPid = pid,
								ProcessPath = pPath,
								ProcessCommandLine = GetProcessCommandLine(pid),
								Width = w,
								Height = h,
								Left = rect.Left,
								Top = rect.Top,
								Bounds = string.Format("{0}x{1} at ({2}, {3})", w, h, rect.Left, rect.Top),
								Affinity = affinity,
								AffinityName = GetAffinityName(affinity),
								ExStyle = exStyle,
								ExStyleHex = "0x" + exStyle.ToString("X8"),
								ExStyleFlags = ParseExStyleFlags(exStyle),
								Style = style,
								IsVisible = isVisible,
								IsTopMost = isTopMost,
								IsLayered = isLayered,
								IsTransparent = isTransparent,
								IsNoActivate = isNoActivate,
								IsToolWindow = isToolWindow,
								MatchesMinecraftGeometry = matchesMc,
								IsFullScreen = isFullScreen,
								TargetRule = "5.08 Сокрытие читов",
								Verdict = (affinity == WDA_EXCLUDEFROMCAPTURE) ?
									"🚨 5.08 СОКРЫТИЕ УЛИК (DWM Bypass: WDA_EXCLUDEFROMCAPTURE / Окно скрыто от Discord/OBS/AnyDesk стрима!)" :
									"⚠️ 5.08 ПОДОЗРИТЕЛЬНО: WDA_MONITOR (Окно исключено из захвата монитора)",
								IsViolation = true,
								IsWdaBypass = true,
								IsStealthOverlay = false,
								IsHookDll = false,
								RowColor = Ui.AccentRed
							};
							item.ForensicReport = BuildForensicReport(item);
							item.TechnicalDetails = BuildTechnicalDetails(item, rect);
							item.ProcessAndModules = BuildProcessDetails(item);
							results.Add(item);
							return true;
						}

						if (isStealthOverlay)
						{
							string geoNote = matchesMc ?
								"ГЕОМЕТРИЯ В ТОЧНОСТИ СОВПАДАЕТ С ОКНОМ MINECRAFT (Внешний ESP/Hitbox/Tracers)!" :
								(isFullScreen ? "ПОЛНОЭКРАННЫЙ ПРОЗРАЧНЫЙ ОВЕРЛЕЙ ПОВЕРХ ВСЕХ ОКОН!" : "Прозрачный кликабельный оверлей");

							ScreenOverlayItem item = new ScreenOverlayItem
							{
								Index = itemIndex++,
								Category = OverlayItemCategory.StealthEspOverlay501,
								CategoryName = "Скрытый ESP Оверлей",
								Hwnd = hWnd,
								HwndHex = "0x" + hWnd.ToInt64().ToString("X8"),
								WindowTitle = string.IsNullOrEmpty(title) ? "<Stealth / Без заголовка>" : title,
								WindowClassName = className,
								ProcessName = (p != null ? p.ProcessName : "PID " + pid) + ".exe",
								ProcessPid = pid,
								ProcessPath = pPath,
								ProcessCommandLine = GetProcessCommandLine(pid),
								Width = w,
								Height = h,
								Left = rect.Left,
								Top = rect.Top,
								Bounds = string.Format("{0}x{1} at ({2}, {3})", w, h, rect.Left, rect.Top),
								Affinity = affinity,
								AffinityName = GetAffinityName(affinity),
								ExStyle = exStyle,
								ExStyleHex = "0x" + exStyle.ToString("X8"),
								ExStyleFlags = ParseExStyleFlags(exStyle),
								Style = style,
								IsVisible = isVisible,
								IsTopMost = isTopMost,
								IsLayered = isLayered,
								IsTransparent = isTransparent,
								IsNoActivate = isNoActivate,
								IsToolWindow = isToolWindow,
								MatchesMinecraftGeometry = matchesMc,
								IsFullScreen = isFullScreen,
								TargetRule = "5.01 Использование читов",
								Verdict = "🚨 5.01 ВНЕШНИЙ ESP/ТРЕЙСЕР ОВЕРЛЕЙ: " + geoNote,
								IsViolation = true,
								IsWdaBypass = false,
								IsStealthOverlay = true,
								IsHookDll = false,
								RowColor = Ui.AccentRed
							};
							item.ForensicReport = BuildForensicReport(item);
							item.TechnicalDetails = BuildTechnicalDetails(item, rect);
							item.ProcessAndModules = BuildProcessDetails(item);
							results.Add(item);
							return true;
						}

						// If layered or topmost or suspicious class in non-system app, show for transparency
						if ((isLayered || isTopMost || isSuspiciousClass) && (w >= 100 && h >= 100))
						{
							OverlayItemCategory cat = isSystemProcess ? OverlayItemCategory.WindowsSystem : (isLegitOverlay ? OverlayItemCategory.LegitimateSoftware : OverlayItemCategory.SuspiciousLayeredWindow);
							bool isV = isVisible && intersectsMc && (cat == OverlayItemCategory.SuspiciousLayeredWindow && isSuspiciousClass);
							string rule = isV ? "5.01 / 5.08" : "Allowed";

							ScreenOverlayItem item = new ScreenOverlayItem
							{
								Index = itemIndex++,
								Category = cat,
								CategoryName = (cat == OverlayItemCategory.WindowsSystem) ? "Системный интерфейс" : ((cat == OverlayItemCategory.LegitimateSoftware) ? "Легитимный оверлей" : "Слоистое TopMost окно"),
								Hwnd = hWnd,
								HwndHex = "0x" + hWnd.ToInt64().ToString("X8"),
								WindowTitle = string.IsNullOrEmpty(title) ? "—" : title,
								WindowClassName = className,
								ProcessName = (p != null ? p.ProcessName : "PID " + pid) + ".exe",
								ProcessPid = pid,
								ProcessPath = pPath,
								ProcessCommandLine = GetProcessCommandLine(pid),
								Width = w,
								Height = h,
								Left = rect.Left,
								Top = rect.Top,
								Bounds = string.Format("{0}x{1} at ({2}, {3})", w, h, rect.Left, rect.Top),
								Affinity = affinity,
								AffinityName = GetAffinityName(affinity),
								ExStyle = exStyle,
								ExStyleHex = "0x" + exStyle.ToString("X8"),
								ExStyleFlags = ParseExStyleFlags(exStyle),
								Style = style,
								IsVisible = isVisible,
								IsTopMost = isTopMost,
								IsLayered = isLayered,
								IsTransparent = isTransparent,
								IsNoActivate = isNoActivate,
								IsToolWindow = isToolWindow,
								MatchesMinecraftGeometry = matchesMc,
								IsFullScreen = isFullScreen,
								TargetRule = rule,
								Verdict = isV ? "⚠️ Подозрительное графическое окно с оверлей-классом (" + className + ")" : ((cat == OverlayItemCategory.WindowsSystem) ? "✔️ Системное окно Windows" : "✔️ Легитимное оконное приложение"),
								IsViolation = isV,
								IsWdaBypass = false,
								IsStealthOverlay = false,
								IsHookDll = false,
								RowColor = isV ? Ui.AccentOrange : (isSystemProcess ? Ui.TextSecondary : Ui.TextPrimary)
							};
							item.ForensicReport = BuildForensicReport(item);
							item.TechnicalDetails = BuildTechnicalDetails(item, rect);
							item.ProcessAndModules = BuildProcessDetails(item);
							results.Add(item);
						}
					}
					catch { }
					return true;
				}, IntPtr.Zero);
			}
			catch { }

			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<ScreenOverlayItem>;
			if (allItems == null)
			{
				allItems = new List<ScreenOverlayItem>();
			}

			// Update KPI counters
			int total = allItems.Count;
			int violations = 0;
			int wdaBypass = 0;
			int stealthOverlays = 0;
			int hookDlls = 0;
			int legitimate = 0;

			foreach (ScreenOverlayItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.IsWdaBypass) wdaBypass++;
				if (item.IsStealthOverlay) stealthOverlays++;
				if (item.IsHookDll) hookDlls++;
				if (!item.IsViolation && (item.Category == OverlayItemCategory.LegitimateSoftware || item.Category == OverlayItemCategory.WindowsSystem || item.Category == OverlayItemCategory.MinecraftWindow))
				{
					legitimate++;
				}
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiWdaBypass.Text = wdaBypass.ToString();
			lblKpiStealthOverlays.Text = stealthOverlays.ToString();
			lblKpiHookDlls.Text = hookDlls.ToString();
			lblKpiLegitimate.Text = legitimate.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО НАРУШЕНИЙ: {0}", violations);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО ОБЪЕКТОВ: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Whitelist and Classification Helpers

		private static bool IsWhitelistedSoftware(string procName, string procPath)
		{
			string p = (procName ?? "").ToLowerInvariant();
			string path = (procPath ?? "").ToLowerInvariant();

			if (p.Contains("anydesk") || path.Contains("anydesk")) return true;
			if (p.Contains("rudesktop") || path.Contains("rudesktop")) return true;
			if (p.Contains("opautoclicker") || p.Contains("autoclicker") || path.Contains("opautoclicker")) return true;
			if (p.Contains("speedautoclicker") || path.Contains("speedautoclicker")) return true;
			if (p.Contains("tinytask") || path.Contains("tinytask")) return true;
			if (p.Contains("x-mouse") || p.Contains("xmouse") || path.Contains("x-mouse")) return true;
			if (p.Contains("autohotkey") || path.Contains("autohotkey")) return true;
			if (p.Contains("clickermann") || path.Contains("clickermann")) return true;
			if (p.Contains("keyran") || path.Contains("keyran")) return true;

			return false;
		}

		private static bool IsSystemUiProcess(string procName)
		{
			string p = (procName ?? "").ToLowerInvariant();
			return p == "explorer" || p == "dwm" || p == "dwmapi" ||
				   p == "textinputhost" || p == "shellexperiencehost" ||
				   p == "applicationframehost" || p == "systemsettings" ||
				   p == "searchhost" || p == "startmenuexperiencehost" ||
				   p == "ctfmon" || p == "lockapp" || p == "screenclippinghost" ||
				   p == "snippingtool" || p == "taskmgr";
		}

		private static bool IsLegitimateOverlay(string procName, string procPath)
		{
			if (ManualChecks.IsWhitelistedOverlayOrCaptureProcess(procName))
			{
				return true;
			}
			string p = (procName ?? "").ToLowerInvariant();
			if (p == "chrome" || p == "msedge" || p == "firefox" || p == "opera" || p == "yandex" || p == "brave" || p == "code")
			{
				return true;
			}
			return false;
		}

		private static bool IsSuspiciousOverlayClass(string className)
		{
			string c = (className ?? "").ToLowerInvariant();
			return c.Contains("imgui") || c.Contains("directx") || c.Contains("opengl") ||
				   c.Contains("overlay") || c.Contains("render") || c.Contains("unitywndclass") ||
				   c.Contains("sdl_app") || c.Contains("glfw30") || c.Contains("sfml_window") ||
				   c.Contains("glfw");
		}

		private static string GetAffinityName(uint affinity)
		{
			switch (affinity)
			{
				case WDA_NONE:
					return "WDA_NONE (0x00, Видимо для захвата)";
				case WDA_MONITOR:
					return "WDA_MONITOR (0x01, Скрыто от монитора)";
				case WDA_EXCLUDEFROMCAPTURE:
					return "WDA_EXCLUDEFROMCAPTURE (0x11, Полный обход захвата DWM/OBS)";
				default:
					return "Affinity 0x" + affinity.ToString("X2");
			}
		}

		private static string ParseExStyleFlags(long ex)
		{
			List<string> flags = new List<string>();
			if ((ex & WS_EX_LAYERED) != 0) flags.Add("WS_EX_LAYERED (0x80000)");
			if ((ex & WS_EX_TRANSPARENT) != 0) flags.Add("WS_EX_TRANSPARENT (0x20)");
			if ((ex & WS_EX_TOPMOST) != 0) flags.Add("WS_EX_TOPMOST (0x08)");
			if ((ex & WS_EX_NOACTIVATE) != 0) flags.Add("WS_EX_NOACTIVATE (0x08000000)");
			if ((ex & WS_EX_TOOLWINDOW) != 0) flags.Add("WS_EX_TOOLWINDOW (0x80)");

			if (flags.Count == 0) return "Стандартный стиль";
			return string.Join(" | ", flags.ToArray());
		}

		private static string GetProcessCommandLine(uint pid)
		{
			if (pid == 0) return "N/A";
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
					string.Format("SELECT CommandLine FROM Win32_Process WHERE ProcessId = {0}", pid)))
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						object cmd = obj["CommandLine"];
						if (cmd != null) return cmd.ToString();
					}
				}
			}
			catch { }
			return "Недоступна";
		}

		#endregion

		#region Report Builders

		private static string BuildForensicReport(ScreenOverlayItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: АУДИТ СОКРЫТИЯ ЭКРАНА DWM И ОВЕРЛЕЕВ ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория объекта: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Имя процесса: {0} (PID: {1})", item.ProcessName, item.ProcessPid));
			sb.AppendLine(string.Format("Путь к исполняемому файлу: {0}", string.IsNullOrEmpty(item.ProcessPath) ? "Недоступен" : item.ProcessPath));
			sb.AppendLine(string.Format("Дескриптор окна (HWND): {0}", item.HwndHex));
			sb.AppendLine(string.Format("Заголовок окна: \"{0}\"", item.WindowTitle));
			sb.AppendLine(string.Format("Класс окна Win32: {0}", item.WindowClassName));
			sb.AppendLine(string.Format("Геометрические размеры: {0}", item.Bounds));
			sb.AppendLine(string.Format("DWM Display Affinity: {0}", item.AffinityName));
			sb.AppendLine(string.Format("Расширенные стили: {0}", item.ExStyleFlags));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsWdaBypass)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.08 (СОКРЫТИЕ УЛИК):");
				sb.AppendLine("Окно настроено с вызовом Win32 API SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE = 0x11).");
				sb.AppendLine("Данная функция операционной системы Windows исключает окно из визуализации");
				sb.AppendLine("в подсистеме DWM (Desktop Window Manager) при захвате экрана:");
				sb.AppendLine(" • Демонстрация экрана в Discord: ОКНО ПОЛНОСТЬЮ НЕВИДИМО зрителям!");
				sb.AppendLine(" • Захват экрана и окон в OBS Studio: ОКНО ПОЛНОСТЬЮ НЕВИДИМО!");
				sb.AppendLine(" • Удаленное управление AnyDesk / RuDesktop: ОКНО НЕ ОТОБРАЖАЕТСЯ!");
				sb.AppendLine(" • Скриншоты PrintScreen / Win+Shift+S: ОКНО НЕ ПОПАДАЕТ В БУФЕР ОБМЕНА!");
				sb.AppendLine("При этом сам игрок видит меню чита, ESP и радар на своем мониторе!");
				sb.AppendLine("Использование данной технологии во время проверки является безусловным");
				sb.AppendLine("доказательством умышленного сокрытия читов (Stream-Proof / OBS Bypass).");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.08 Сокрытие софта через OBS/Discord Bypass (WDA_EXCLUDEFROMCAPTURE)", "Player"));
			}
			else if (item.IsStealthOverlay)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 (ИСПОЛЬЗОВАНИЕ ЧИТОВ):");
				sb.AppendLine("Обнаружено скрытое прозрачное окно с комбинацией стилей:");
				sb.AppendLine("WS_EX_LAYERED (слоистое) + WS_EX_TRANSPARENT (прозрачное для кликов) + WS_EX_TOPMOST (поверх всех окон).");
				if (item.MatchesMinecraftGeometry)
				{
					sb.AppendLine("ВНИМАНИЕ! Геометрические координаты окна в точности повторяют окно Minecraft!");
					sb.AppendLine("Это однозначно указывает на работу внешнего ESP/Hitbox/Tracers оверлея,");
					sb.AppendLine("который отрисовывает подсветку игроков поверх окна игры, перехватывая координаты.");
				}
				else if (item.IsFullScreen)
				{
					sb.AppendLine("Окно растянуто на весь экран и находится поверх игры в прозрачном режиме.");
				}
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование читов (Внешний ESP оверлей поверх Minecraft)", "Player"));
			}
			else if (item.Category == OverlayItemCategory.MinecraftWindow)
			{
				sb.AppendLine("Окно принадлежит легитимному процессу клиента Minecraft.");
				sb.AppendLine("Используется как точка отсчета геометрии для сопоставления внешних оверлеев.");
			}
			else if (item.Category == OverlayItemCategory.LegitimateSoftware)
			{
				sb.AppendLine("Окно принадлежит легитимному программному обеспечению из вайтлиста сервера.");
				sb.AppendLine("Претензий по регламенту 5.01-5.08 не имеется.");
			}
			else
			{
				sb.AppendLine("Окно прошло криминалистическую валидацию.");
			}

			return sb.ToString();
		}

		private static string BuildTechnicalDetails(ScreenOverlayItem item, RECT rect)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ТЕХНИЧЕСКИЕ ПАРАМЕТРЫ ОКНА WIN32 ===");
			sb.AppendLine(string.Format("Дескриптор HWND: {0} ({1})", item.HwndHex, item.Hwnd.ToInt64()));
			sb.AppendLine(string.Format("Класс окна: {0}", item.WindowClassName));
			sb.AppendLine(string.Format("Заголовок: \"{0}\"", item.WindowTitle));
			sb.AppendLine();
			sb.AppendLine("КООРДИНАТЫ И РАЗМЕРЫ (RECT):");
			sb.AppendLine(string.Format(" • Left:   {0}", rect.Left));
			sb.AppendLine(string.Format(" • Top:    {0}", rect.Top));
			sb.AppendLine(string.Format(" • Right:  {0}", rect.Right));
			sb.AppendLine(string.Format(" • Bottom: {0}", rect.Bottom));
			sb.AppendLine(string.Format(" • Width:  {0} px", rect.Width));
			sb.AppendLine(string.Format(" • Height: {0} px", rect.Height));
			sb.AppendLine();
			sb.AppendLine("ОКОННЫЕ СТИЛИ:");
			sb.AppendLine(string.Format(" • GWL_STYLE:    0x{0:X8}", item.Style));
			sb.AppendLine(string.Format(" • GWL_EXSTYLE:  {0}", item.ExStyleHex));
			sb.AppendLine(string.Format(" • Флаги стиля:  {0}", item.ExStyleFlags));
			sb.AppendLine(string.Format(" • WS_VISIBLE:   {0}", item.IsVisible));
			sb.AppendLine(string.Format(" • WS_EX_TOPMOST: {0}", item.IsTopMost));
			sb.AppendLine(string.Format(" • WS_EX_LAYERED: {0}", item.IsLayered));
			sb.AppendLine(string.Format(" • WS_EX_TRANSPARENT: {0}", item.IsTransparent));
			sb.AppendLine(string.Format(" • WS_EX_NOACTIVATE:  {0}", item.IsNoActivate));
			sb.AppendLine(string.Format(" • WS_EX_TOOLWINDOW:  {0}", item.IsToolWindow));
			sb.AppendLine();
			sb.AppendLine("DWM DISPLAY AFFINITY:");
			sb.AppendLine(string.Format(" • Значение:     0x{0:X2} ({1})", item.Affinity, item.Affinity));
			sb.AppendLine(string.Format(" • Расшифровка:  {0}", item.AffinityName));
			sb.AppendLine(string.Format(" • Совпадение с Minecraft: {0}", item.MatchesMinecraftGeometry ? "ДА (Абсолютное совпадение)" : "НЕТ"));
			sb.AppendLine(string.Format(" • Полноэкранный режим:    {0}", item.IsFullScreen ? "ДА" : "НЕТ"));

			return sb.ToString();
		}

		private static string BuildProcessDetails(ScreenOverlayItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СВЕДЕНИЯ О ПРОЦЕССЕ И МОДУЛЯХ ===");
			sb.AppendLine(string.Format("Имя процесса:   {0}", item.ProcessName));
			sb.AppendLine(string.Format("Идентификатор (PID): {0}", item.ProcessPid));
			sb.AppendLine(string.Format("Исполняемый файл:   {0}", string.IsNullOrEmpty(item.ProcessPath) ? "Недоступен" : item.ProcessPath));
			sb.AppendLine(string.Format("Командная строка:   {0}", item.ProcessCommandLine));
			sb.AppendLine();

			if (!string.IsNullOrEmpty(item.ProcessPath) && File.Exists(item.ProcessPath))
			{
				try
				{
					FileInfo fi = new FileInfo(item.ProcessPath);
					sb.AppendLine("ФАЙЛОВЫЕ АТРИБУТЫ:");
					sb.AppendLine(string.Format(" • Размер файла:    {0:N0} байт", fi.Length));
					sb.AppendLine(string.Format(" • Время создания:  {0:dd.MM.yyyy HH:mm:ss}", fi.CreationTime));
					sb.AppendLine(string.Format(" • Время изменения: {0:dd.MM.yyyy HH:mm:ss}", fi.LastWriteTime));
					sb.AppendLine(string.Format(" • Время доступа:   {0:dd.MM.yyyy HH:mm:ss}", fi.LastAccessTime));

					try
					{
						X509Certificate cert = X509Certificate.CreateFromSignedFile(item.ProcessPath);
						sb.AppendLine(string.Format(" • ЭЦП (Authenticode): ДЕЙСТВИТЕЛЬНА ({0})", cert.Subject));
					}
					catch
					{
						sb.AppendLine(" • ЭЦП (Authenticode): ОТСУТСТВУЕТ (Файл не подписан)");
					}
				}
				catch { }
			}

			// If process is active, enumerate loaded graphics modules
			if (item.ProcessPid != 0)
			{
				try
				{
					Process p = Process.GetProcessById((int)item.ProcessPid);
					if (p != null)
					{
						sb.AppendLine();
						sb.AppendLine("ЗАГРУЖЕННЫЕ ГРАФИЧЕСКИЕ И ХУК-МОДУЛИ В ПРОЦЕССЕ:");
						int gCount = 0;
						foreach (ProcessModule mod in p.Modules)
						{
							try
							{
								string mn = (mod.ModuleName ?? "").ToLowerInvariant();
								if (mn.Contains("d3d") || mn.Contains("dxgi") || mn.Contains("opengl") ||
									mn.Contains("hook") || mn.Contains("overlay") || mn.Contains("draw") ||
									mn.Contains("vulkan") || mn.Contains("render"))
								{
									sb.AppendLine(string.Format(" • {0} -> {1}", mod.ModuleName, mod.FileName));
									gCount++;
								}
							}
							catch { }
						}
						if (gCount == 0)
						{
							sb.AppendLine(" • Графических хук-модулей не зафиксировано.");
						}
					}
				}
				catch { }
			}

			return sb.ToString();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (ScreenOverlayItem item in allItems)
			{
				// Filter Category Check
				if (currentFilter == OverlayFilterCategory.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == OverlayFilterCategory.WdaScreenBypass && !item.IsWdaBypass)
				{
					continue;
				}
				if (currentFilter == OverlayFilterCategory.StealthOverlays && !item.IsStealthOverlay)
				{
					continue;
				}
				if (currentFilter == OverlayFilterCategory.HookDllsInJava && !item.IsHookDll)
				{
					continue;
				}
				if (currentFilter == OverlayFilterCategory.LegitimateAndSystem && item.IsViolation)
				{
					continue;
				}

				// Search Filter
				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.WindowTitle.ToLowerInvariant().Contains(search) ||
								 item.WindowClassName.ToLowerInvariant().Contains(search) ||
								 item.ProcessName.ToLowerInvariant().Contains(search) ||
								 item.ProcessPid.ToString().Contains(search) ||
								 item.HwndHex.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.WindowTitle);
				lvi.SubItems.Add(string.Format("{0} ({1})", item.ProcessName, item.ProcessPid));
				lvi.SubItems.Add(item.AffinityName);
				lvi.SubItems.Add(item.ExStyleFlags);
				lvi.SubItems.Add(item.Bounds);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);

				lvi.Tag = item;
				lvi.ForeColor = item.RowColor;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtForensicReport.Clear();
				txtTechnicalDetails.Clear();
				txtProcessAndModules.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenOverlayItem item = listView.SelectedItems[0].Tag as ScreenOverlayItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtTechnicalDetails.Text = item.TechnicalDetails;
			txtProcessAndModules.Text = item.ProcessAndModules;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ScreenOverlayItem item = listView.SelectedItems[0].Tag as ScreenOverlayItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | Процесс: {2} (PID: {3}) | HWND: {4} | Affinity: {5} | Стили: {6} | {7}",
				item.CategoryName, item.WindowTitle, item.ProcessName, item.ProcessPid, item.HwndHex, item.AffinityName, item.ExStyleFlags, item.Verdict);

			Clipboard.SetText(text);
			MessageBox.Show("Запись успешно скопирована в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования отчета Discord.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ScreenOverlayItem item = listView.SelectedItems[0].Tag as ScreenOverlayItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [ПРОВЕРКА DWM & OVERLAY: ЛЕГИТИМНО]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Категория: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Окно: \"{0}\" (Класс: {1})", item.WindowTitle, item.WindowClassName));
			sb.AppendLine(string.Format("Процесс: {0} (PID: {1})", item.ProcessName, item.ProcessPid));
			sb.AppendLine(string.Format("HWND: {0} | Размеры: {1}", item.HwndHex, item.Bounds));
			sb.AppendLine(string.Format("Affinity: {0}", item.AffinityName));
			sb.AppendLine(string.Format("Стили: {0}", item.ExStyleFlags));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (Сокрытие/Оверлей)"));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord успешно скопирован в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением для генерации команды бана.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ScreenOverlayItem item = listView.SelectedItems[0].Tag as ScreenOverlayItem;
			if (item == null) return;

			string cmd = "";
			if (item.IsWdaBypass)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие софта через OBS/Discord Bypass (WDA_EXCLUDEFROMCAPTURE)", targetPlayerName);
			}
			else if (item.IsStealthOverlay)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Внешний ESP оверлей поверх Minecraft)", targetPlayerName);
			}
			else if (item.IsHookDll)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Фальшивый DLL-хук {1})", targetPlayerName, item.WindowTitle);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, item.TargetRule);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_ScreenOverlay_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза сокрытия экрана DWM и Оверлеев — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background:#0F1115; color:#ECF0F5; font-family:'Segoe UI',Arial,sans-serif; margin:20px; }");
					sb.AppendLine("h1 { color:#2EE6D6; font-size:20px; border-bottom:1px solid #262B33; padding-bottom:10px; }");
					sb.AppendLine(".kpi-container { display:flex; gap:12px; margin:15px 0; }");
					sb.AppendLine(".kpi-card { background:#171A1F; border:1px solid #262B33; padding:10px 14px; border-radius:6px; min-width:140px; }");
					sb.AppendLine(".kpi-title { font-size:11px; color:#8C96A3; }");
					sb.AppendLine(".kpi-val { font-size:18px; font-weight:bold; margin-top:4px; }");
					sb.AppendLine("table { width:100%; border-collapse:collapse; margin-top:15px; font-size:12px; }");
					sb.AppendLine("th, td { border:1px solid #262B33; padding:8px 10px; text-align:left; }");
					sb.AppendLine("th { background:#171A1F; color:#4FC3F7; }");
					sb.AppendLine("tr:nth-child(even) { background:#121418; }");
					sb.AppendLine(".crit { color:#EF5350; font-weight:bold; }");
					sb.AppendLine(".warn { color:#FFA726; font-weight:bold; }");
					sb.AppendLine(".ok { color:#66BB6A; }");
					sb.AppendLine("</style></head><body>");

					sb.AppendLine("<h1>🪟 Судебный протокол аудита сокрытия экрана DWM, OBS/Discord Bypass и графических оверлеев</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего объектов</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.08/5.01)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>DWM Screen Bypass</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiWdaBypass.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Скрытые ESP Оверлеи</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiStealthOverlays.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Оверлей-хуки в Java</div><div class='kpi-val' style='color:#2EE6D6;'>{0}</div></div>", lblKpiHookDlls.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Легитимные окна</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiLegitimate.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Заголовок окна / Модуль</th><th>Процесс (PID)</th><th>DWM Affinity</th><th>Стили (EX_STYLE)</th><th>Bounds</th><th>Регламент</th><th>Вердикт</th></tr>");

					foreach (ScreenOverlayItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : (item.Category == OverlayItemCategory.MinecraftWindow ? "ok" : "");
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td class='{8}'>{9}</td></tr>",
							item.Index, item.CategoryName, item.WindowTitle, string.Format("{0} ({1})", item.ProcessName, item.ProcessPid),
							item.AffinityName, item.ExStyleFlags, item.Bounds, item.TargetRule, cssClass, item.Verdict));
					}

					sb.AppendLine("</table>");
					sb.AppendLine("</body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					Process.Start(sfd.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при сохранении HTML отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
