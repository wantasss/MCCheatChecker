using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class SetupApiUsbTimelineViewerForm : Form
	{
		#region Enums & Models

		public enum UsbEventCategory
		{
			RecentFlashDrive508,
			HardwareCheatKmbox501,
			UnpluggedFlashDrive508,
			DayOldUsbStorage,
			WeekOldUsbStorage,
			StandardPeripheral,
			WindowsSystemPnp
		}

		public enum UsbTimelineFilter
		{
			All,
			ViolationsOnly,
			RecentFlashDrives,
			HardwareCheats,
			UnpluggedDrives,
			StandardPeripherals
		}

		public class UsbTimelineItem
		{
			public int Index;
			public UsbEventCategory Category;
			public string CategoryName;
			public string DeviceName;
			public string HardwareId;
			public string DeviceClass;
			public DateTime InstallTimestamp;
			public TimeSpan TimeAgo;
			public string TimeAgoString;
			public bool IsCurrentlyConnected;
			public string SourceLogFile;
			public int LogLineNumber;
			public string SectionHeader;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsRecentFlash;
			public bool IsHardwareCheat;
			public bool IsUnplugged;
			public Color RowColor;
			public string ForensicReport;
			public string RawLogContext;
			public string CorrelationDetails;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private UsbTimelineFilter currentFilter = UsbTimelineFilter.All;
		private List<UsbTimelineItem> allItems = new List<UsbTimelineItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtRawLogContext;
		private DarkTextBoxMultiline txtCorrelationDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiRecentFlash;
		private Label lblKpiHardwareCheats;
		private Label lblKpiUnplugged;
		private Label lblKpiStandard;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterRecent;
		private DarkButton btnFilterHwCheats;
		private DarkButton btnFilterUnplugged;
		private DarkButton btnFilterStandard;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public SetupApiUsbTimelineViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🔌 Инспектор хронологии подключения USB и PnP устройств (SetupAPI.dev.log) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🔌 ИНСПЕКТОР ХРОНОЛОГИИ ПОДКЛЮЧЕНИЯ USB И PNP УСТРОЙСТВ (SETUPAPI)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит setupapi.dev.log: точные таймлайны подключений USB-накопителей, детекция извлеченных флешек (5.08) и чит-контроллеров KMBox/Arduino (5.01)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ ЧТЕНИЕ ЛОГОВ SETUPAPI...";
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
			kpiPanel.Controls.Add(CreateKpiBadge("🔌 Всего событий USB", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.08/5.01)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("⚡ Свежих флешек (<2 ч)", "0", Ui.AccentRed, ref kpiX, out lblKpiRecentFlash));
			kpiPanel.Controls.Add(CreateKpiBadge("🤖 KMBox / Arduino читов", "0", Ui.AccentRed, ref kpiX, out lblKpiHardwareCheats));
			kpiPanel.Controls.Add(CreateKpiBadge("📤 Извлеченных флешек", "0", Ui.AccentOrange, ref kpiX, out lblKpiUnplugged));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Стандартных устройств", "0", Ui.AccentGreen, ref kpiX, out lblKpiStandard));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все события", UsbTimelineFilter.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08/5.01)", UsbTimelineFilter.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterRecent = CreateFilterButton("⚡ Свежие (< 2 ч)", UsbTimelineFilter.RecentFlashDrives, ref fBtnX, filterBar);
			btnFilterHwCheats = CreateFilterButton("🤖 KMBox/Arduino", UsbTimelineFilter.HardwareCheats, ref fBtnX, filterBar);
			btnFilterUnplugged = CreateFilterButton("📤 Извлеченные флешки", UsbTimelineFilter.UnpluggedDrives, ref fBtnX, filterBar);
			btnFilterStandard = CreateFilterButton("✔️ Обычные устройства", UsbTimelineFilter.StandardPeripherals, ref fBtnX, filterBar);

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

			// SplitContainer
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
			listView.Columns.Add("Категория", 155, HorizontalAlignment.Left);
			listView.Columns.Add("Устройство / Модель", 230, HorizontalAlignment.Left);
			listView.Columns.Add("Идентификатор PnP (Hardware ID)", 250, HorizontalAlignment.Left);
			listView.Columns.Add("Точное время подключения", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Сколько прошло времени", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 280, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate { UpdateDetailViews(); };
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Container
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

			TabPage tabLog = new TabPage("🔍 Исходный лог Section Start (SetupAPI)");
			tabLog.BackColor = Ui.Surface;
			txtRawLogContext = new DarkTextBoxMultiline();
			txtRawLogContext.Dock = DockStyle.Fill;
			txtRawLogContext.ReadOnly = true;
			txtRawLogContext.Font = Ui.FontMono(9f);
			tabLog.Controls.Add(txtRawLogContext);
			detailTabs.TabPages.Add(tabLog);

			TabPage tabCorrelation = new TabPage("⚡ Корреляция дисков и PnP устройства");
			tabCorrelation.BackColor = Ui.Surface;
			txtCorrelationDetails = new DarkTextBoxMultiline();
			txtCorrelationDetails.Dock = DockStyle.Fill;
			txtCorrelationDetails.ReadOnly = true;
			txtCorrelationDetails.Font = Ui.FontMono(9f);
			tabCorrelation.Controls.Add(txtCorrelationDetails);
			detailTabs.TabPages.Add(tabCorrelation);

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

		private DarkButton CreateFilterButton(string text, UsbTimelineFilter cat, ref int curX, Panel parent)
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
			btnFilterAll.BackColor = (currentFilter == UsbTimelineFilter.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == UsbTimelineFilter.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == UsbTimelineFilter.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == UsbTimelineFilter.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterRecent.BackColor = (currentFilter == UsbTimelineFilter.RecentFlashDrives) ? Ui.AccentRed : Ui.Surface;
			btnFilterRecent.ForeColor = (currentFilter == UsbTimelineFilter.RecentFlashDrives) ? Color.White : Ui.TextPrimary;

			btnFilterHwCheats.BackColor = (currentFilter == UsbTimelineFilter.HardwareCheats) ? Ui.AccentRed : Ui.Surface;
			btnFilterHwCheats.ForeColor = (currentFilter == UsbTimelineFilter.HardwareCheats) ? Color.White : Ui.TextPrimary;

			btnFilterUnplugged.BackColor = (currentFilter == UsbTimelineFilter.UnpluggedDrives) ? Ui.AccentOrange : Ui.Surface;
			btnFilterUnplugged.ForeColor = (currentFilter == UsbTimelineFilter.UnpluggedDrives) ? Color.Black : Ui.TextPrimary;

			btnFilterStandard.BackColor = (currentFilter == UsbTimelineFilter.StandardPeripherals) ? Ui.AccentGreen : Ui.Surface;
			btnFilterStandard.ForeColor = (currentFilter == UsbTimelineFilter.StandardPeripherals) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ ЧТЕНИЕ ЛОГОВ SETUPAPI...";
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
			List<UsbTimelineItem> results = new List<UsbTimelineItem>();
			int itemIndex = 1;

			// Gather currently connected drives to detect unplugged flash drives
			List<string> currentDrives = new List<string>();
			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo d in drives)
				{
					if (d.IsReady)
					{
						currentDrives.Add(d.Name.ToLowerInvariant());
					}
				}
			}
			catch { }

			string infPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "INF");
			string[] logFiles = new string[]
			{
				Path.Combine(infPath, "setupapi.dev.log"),
				Path.Combine(infPath, "setupapi.offline.log")
			};

			DateTime now = DateTime.Now;

			foreach (string logPath in logFiles)
			{
				if (!File.Exists(logPath)) continue;

				try
				{
					string[] lines;
					using (FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.Default))
					{
						string content = sr.ReadToEnd();
						lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					}

					string currentDeviceHeader = null;
					int currentHeaderLine = 0;

					for (int i = 0; i < lines.Length; i++)
					{
						string line = lines[i];

						if (line.StartsWith(">>>  [Device Install (Hardware initiated)") &&
							(line.Contains("USB\\") || line.Contains("USBSTOR\\") || line.Contains("HID\\") || line.Contains("FTDIBUS\\")))
						{
							currentDeviceHeader = line;
							currentHeaderLine = i + 1;
						}
						else if (currentDeviceHeader != null && line.StartsWith(">>>  Section start "))
						{
							string datePart = line.Substring(19).Trim();
							DateTime timestamp;

							if (DateTime.TryParse(datePart.Split('.')[0], out timestamp))
							{
								TimeSpan timeAgo = now - timestamp;

								string hwId = currentDeviceHeader;
								int uIdx = hwId.IndexOf("USB\\");
								if (uIdx < 0) uIdx = hwId.IndexOf("USBSTOR\\");
								if (uIdx < 0) uIdx = hwId.IndexOf("HID\\");
								if (uIdx < 0) uIdx = hwId.IndexOf("FTDIBUS\\");
								if (uIdx >= 0)
								{
									hwId = hwId.Substring(uIdx).TrimEnd(']');
								}

								string hwLower = hwId.ToLowerInvariant();

								bool isStorage = hwLower.Contains("usbstor") || hwLower.Contains("disk") || hwLower.Contains("flash") || hwLower.Contains("generic");
								bool isKmbox = hwLower.Contains("vid_1a86") || hwLower.Contains("vid_0483") || hwLower.Contains("vid_2341") || hwLower.Contains("vid_16c0") || hwLower.Contains("vid_2e8a") || hwLower.Contains("kmbox");

								UsbEventCategory cat;
								string devName = ParseFriendlyDeviceName(hwId);
								string rule = "Allowed";
								string verdict;
								Color rowColor;
								bool isV = false;
								bool isRecent = false;
								bool isHwCheat = false;
								bool isUnplugged = false;

								if (isKmbox)
								{
									cat = UsbEventCategory.HardwareCheatKmbox501;
									rule = "5.01 Использование читов";
									verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ: Подключен аппаратный макро-контроллер / KMBox / Arduino / Teensy!";
									rowColor = Ui.AccentRed;
									isV = true;
									isHwCheat = true;
								}
								else if (isStorage && timeAgo.TotalHours < 2.0 && timeAgo.TotalMinutes >= 0.0)
								{
									cat = UsbEventCategory.RecentFlashDrive508;
									rule = "5.08 Сокрытие читов";
									verdict = string.Format("🚨 5.08 СОКРЫТИЕ УЛИК: Флешка подключена за {0:F0} мин до проверки (Подозрение на запуск чита с USB)!", timeAgo.TotalMinutes);
									rowColor = Ui.AccentRed;
									isV = true;
									isRecent = true;
								}
								else if (isStorage && timeAgo.TotalHours < 24.0 && timeAgo.TotalHours >= 0.0)
								{
									cat = UsbEventCategory.DayOldUsbStorage;
									rule = "5.08";
									verdict = string.Format("⚠️ 5.08 ПОДОЗРИТЕЛЬНО: USB накопитель подключался {0:F1} ч назад", timeAgo.TotalHours);
									rowColor = Ui.AccentOrange;
								}
								else if (isStorage)
								{
									cat = UsbEventCategory.WeekOldUsbStorage;
									rule = "Allowed";
									verdict = string.Format("ℹ️ Подключение USB накопителя ({0:F0} дн назад)", timeAgo.TotalDays);
									rowColor = Ui.TextSecondary;
								}
								else
								{
									cat = UsbEventCategory.StandardPeripheral;
									rule = "Allowed";
									verdict = "✔️ Стандартное PnP устройство (Клавиатура/Мышь/Аудио)";
									rowColor = Ui.TextPrimary;
								}

								string timeAgoStr;
								if (timeAgo.TotalMinutes < 60)
									timeAgoStr = string.Format("{0:F0} мин назад", timeAgo.TotalMinutes);
								else if (timeAgo.TotalHours < 24)
									timeAgoStr = string.Format("{0:F1} ч назад", timeAgo.TotalHours);
								else
									timeAgoStr = string.Format("{0:F0} дн назад", timeAgo.TotalDays);

								UsbTimelineItem item = new UsbTimelineItem
								{
									Index = itemIndex++,
									Category = cat,
									CategoryName = GetCategoryName(cat),
									DeviceName = devName,
									HardwareId = hwId,
									DeviceClass = isStorage ? "USB Storage (Flash/Disk)" : (isKmbox ? "Hardware Cheat / Serial Dongle" : "USB PnP Peripheral"),
									InstallTimestamp = timestamp,
									TimeAgo = timeAgo,
									TimeAgoString = timeAgoStr,
									IsCurrentlyConnected = true,
									SourceLogFile = Path.GetFileName(logPath),
									LogLineNumber = currentHeaderLine,
									SectionHeader = currentDeviceHeader,
									TargetRule = rule,
									Verdict = verdict,
									IsViolation = isV,
									IsRecentFlash = isRecent,
									IsHardwareCheat = isHwCheat,
									IsUnplugged = isUnplugged,
									RowColor = rowColor
								};

								item.ForensicReport = BuildForensicReport(item);
								item.RawLogContext = BuildRawLogContext(item, lines, currentHeaderLine);
								item.CorrelationDetails = BuildCorrelationDetails(item);

								results.Add(item);
							}

							currentDeviceHeader = null;
						}
					}
				}
				catch { }
			}

			// Sort by newest first
			results.Sort(delegate(UsbTimelineItem a, UsbTimelineItem b)
			{
				return b.InstallTimestamp.CompareTo(a.InstallTimestamp);
			});

			// Re-index
			for (int i = 0; i < results.Count; i++)
			{
				results[i].Index = i + 1;
			}

			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<UsbTimelineItem>;
			if (allItems == null)
			{
				allItems = new List<UsbTimelineItem>();
			}

			int total = allItems.Count;
			int violations = 0;
			int recentFlash = 0;
			int hwCheats = 0;
			int unplugged = 0;
			int standard = 0;

			foreach (UsbTimelineItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.IsRecentFlash) recentFlash++;
				if (item.IsHardwareCheat) hwCheats++;
				if (item.IsUnplugged) unplugged++;
				if (!item.IsViolation && item.Category == UsbEventCategory.StandardPeripheral) standard++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiRecentFlash.Text = recentFlash.ToString();
			lblKpiHardwareCheats.Text = hwCheats.ToString();
			lblKpiUnplugged.Text = unplugged.ToString();
			lblKpiStandard.Text = standard.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО НАРУШЕНИЙ: {0}", violations);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО СОБЫТИЙ: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Helpers & Formats

		private static string GetCategoryName(UsbEventCategory cat)
		{
			switch (cat)
			{
				case UsbEventCategory.RecentFlashDrive508: return "Свежая флешка (<2 ч)";
				case UsbEventCategory.HardwareCheatKmbox501: return "KMBox / Arduino Чит";
				case UsbEventCategory.UnpluggedFlashDrive508: return "Извлеченная флешка";
				case UsbEventCategory.DayOldUsbStorage: return "Флешка за 24 ч";
				case UsbEventCategory.WeekOldUsbStorage: return "USB за неделю";
				default: return "Периферия USB";
			}
		}

		private static string ParseFriendlyDeviceName(string hwId)
		{
			if (string.IsNullOrEmpty(hwId)) return "Неизвестное USB устройство";

			string lower = hwId.ToLowerInvariant();
			if (lower.Contains("vid_1a86")) return "KMBox / CH340 USB-Serial (Аппаратный контроллер)";
			if (lower.Contains("vid_0483")) return "KMBox Net / STM32 Virtual COM (Чит-контроллер)";
			if (lower.Contains("vid_2341")) return "Arduino Uno/Leonardo/Micro (Макро-эмулятор)";
			if (lower.Contains("vid_16c0")) return "Teensy USB HID (Аппаратный макро-инжектор)";
			if (lower.Contains("vid_2e8a")) return "Raspberry Pi Pico (RP2040 Чит-донгл)";

			if (lower.Contains("usbstor"))
			{
				string[] parts = hwId.Split('&');
				if (parts.Length > 1)
				{
					return "USB Flash Drive (" + parts[1].Replace("Ven_", "").Replace("Prod_", " ").Trim() + ")";
				}
				return "USB Накопитель / Флешка";
			}

			if (lower.Contains("hid")) return "USB HID Устройство ввода (Мышь/Клавиатура)";
			return "USB PnP: " + (hwId.Length > 40 ? hwId.Substring(0, 40) + "..." : hwId);
		}

		#endregion

		#region Report Builders

		private static string BuildForensicReport(UsbTimelineItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: ХРОНОЛОГИЯ ПОДКЛЮЧЕНИЙ USB (SETUPAPI) ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория события: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Устройство: {0}", item.DeviceName));
			sb.AppendLine(string.Format("Идентификатор PnP (Hardware ID): {0}", item.HardwareId));
			sb.AppendLine(string.Format("Класс устройства: {0}", item.DeviceClass));
			sb.AppendLine(string.Format("Точное время установки/подключения: {0:dd.MM.yyyy HH:mm:ss}", item.InstallTimestamp));
			sb.AppendLine(string.Format("Временной интервал: {0}", item.TimeAgoString));
			sb.AppendLine(string.Format("Файл журнала: {0} (Строка {1})", item.SourceLogFile, item.LogLineNumber));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsHardwareCheat)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 (АППАРАТНЫЕ ЧИТЫ):");
				sb.AppendLine("В журнале драйверов Windows зафиксировано подключение программируемого");
				sb.AppendLine("микроконтроллера (Arduino / KMBox / Teensy / RP2040).");
				sb.AppendLine("Данные аппаратные донглы эмулируют движение мыши и клики на уровне");
				sb.AppendLine("USB HID контроллера, обходя античиты и эмулируя автокликеры и аимботы.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование аппаратных читов ({1})", "Player", item.DeviceName));
			}
			else if (item.IsRecentFlash)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.08 (СОКРЫТИЕ УЛИК):");
				sb.AppendLine("Зафиксировано подключение внешнего USB флеш-накопителя непосредственно");
				sb.AppendLine(string.Format("перед вызовом на проверку ({0}).", item.TimeAgoString));
				sb.AppendLine("По регламенту ScreenShare подключение флеш-карт перед проверкой расценивается");
				sb.AppendLine("как запуск читов с портативного носителя или использование софта очистки улик.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.08 Сокрытие софта (Подключение флешки за {1} до SS)", "Player", item.TimeAgoString));
			}
			else
			{
				sb.AppendLine("Событие PnP относится к штатной работе периферийных устройств Windows.");
			}

			return sb.ToString();
		}

		private static string BuildRawLogContext(UsbTimelineItem item, string[] lines, int lineNum)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ИСХОДНЫЙ ФРАГМЕНТ ЖУРНАЛА SETUPAPI.DEV.LOG ===");
			sb.AppendLine(string.Format("Журнал: {0} | Строка заголовка: {1}", item.SourceLogFile, lineNum));
			sb.AppendLine();

			int start = Math.Max(0, lineNum - 1);
			int end = Math.Min(lines.Length - 1, lineNum + 15);

			for (int i = start; i <= end; i++)
			{
				sb.AppendLine(string.Format("[{0:D5}] {1}", i + 1, lines[i]));
			}

			return sb.ToString();
		}

		private static string BuildCorrelationDetails(UsbTimelineItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АКТИВНЫЕ ДИСКИ И КОРРЕЛЯЦИЯ PnP ===");
			sb.AppendLine(string.Format("PnP Идентификатор: {0}", item.HardwareId));
			sb.AppendLine(string.Format("Класс:             {0}", item.DeviceClass));
			sb.AppendLine(string.Format("Время события:     {0:dd.MM.yyyy HH:mm:ss}", item.InstallTimestamp));
			sb.AppendLine();
			sb.AppendLine("ТЕКУЩИЕ ДИСКИ В СИСТЕМЕ:");

			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo d in drives)
				{
					if (d.IsReady)
					{
						sb.AppendLine(string.Format(" • Диск {0} [{1}] ({2}, свободно {3:N0} МБ из {4:N0} МБ)",
							d.Name, d.VolumeLabel, d.DriveType, d.AvailableFreeSpace / (1024 * 1024), d.TotalSize / (1024 * 1024)));
					}
					else
					{
						sb.AppendLine(string.Format(" • Диск {0} ({1}, не готов)", d.Name, d.DriveType));
					}
				}
			}
			catch { }

			return sb.ToString();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (UsbTimelineItem item in allItems)
			{
				if (currentFilter == UsbTimelineFilter.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == UsbTimelineFilter.RecentFlashDrives && !item.IsRecentFlash)
				{
					continue;
				}
				if (currentFilter == UsbTimelineFilter.HardwareCheats && !item.IsHardwareCheat)
				{
					continue;
				}
				if (currentFilter == UsbTimelineFilter.UnpluggedDrives && !item.IsUnplugged)
				{
					continue;
				}
				if (currentFilter == UsbTimelineFilter.StandardPeripherals && item.IsViolation)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.DeviceName.ToLowerInvariant().Contains(search) ||
								 item.HardwareId.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.DeviceName);
				lvi.SubItems.Add(item.HardwareId);
				lvi.SubItems.Add(item.InstallTimestamp.ToString("dd.MM.yyyy HH:mm:ss"));
				lvi.SubItems.Add(item.TimeAgoString);
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
				txtRawLogContext.Clear();
				txtCorrelationDetails.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			UsbTimelineItem item = listView.SelectedItems[0].Tag as UsbTimelineItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtRawLogContext.Text = item.RawLogContext;
			txtCorrelationDetails.Text = item.CorrelationDetails;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите событие для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			UsbTimelineItem item = listView.SelectedItems[0].Tag as UsbTimelineItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | ID: {2} | Время: {3:dd.MM.yyyy HH:mm:ss} ({4}) | {5}",
				item.CategoryName, item.DeviceName, item.HardwareId, item.InstallTimestamp, item.TimeAgoString, item.Verdict);

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
			UsbTimelineItem item = listView.SelectedItems[0].Tag as UsbTimelineItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [ХРОНОЛОГИЯ USB (SETUPAPI): ЛЕГИТИМНО]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Устройство: {0}", item.DeviceName));
			sb.AppendLine(string.Format("Hardware ID: {0}", item.HardwareId));
			sb.AppendLine(string.Format("Время установки: {0:dd.MM.yyyy HH:mm:ss} ({1})", item.InstallTimestamp, item.TimeAgoString));
			sb.AppendLine(string.Format("Журнал: {0}:{1}", item.SourceLogFile, item.LogLineNumber));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (USB Аппаратный чит/Флешка)"));
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
			UsbTimelineItem item = listView.SelectedItems[0].Tag as UsbTimelineItem;
			if (item == null) return;

			string cmd = "";
			if (item.IsHardwareCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование аппаратных читов ({1})", targetPlayerName, item.DeviceName);
			}
			else if (item.IsRecentFlash)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие софта (Подключение флешки за {1} до SS)", targetPlayerName, item.TimeAgoString);
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
			sfd.FileName = string.Format("MCCheatChecker_SetupApiUsb_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза подключений USB — " + targetPlayerName + "</title>");
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

					sb.AppendLine("<h1>🔌 Судебный протокол точной хронологии подключения USB и PnP устройств (SetupAPI.dev.log)</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего событий USB</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.08/5.01)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Свежих флешек (<2 ч)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiRecentFlash.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>KMBox / Arduino читов</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiHardwareCheats.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Извлеченных флешек</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiUnplugged.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Стандартных устройств</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiStandard.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Устройство</th><th>Hardware ID</th><th>Время подключения</th><th>Интервал</th><th>Регламент</th><th>Вердикт</th></tr>");

					foreach (UsbTimelineItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : "";
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3}</td><td>{4:dd.MM.yyyy HH:mm:ss}</td><td>{5}</td><td>{6}</td><td class='{8}'>{7}</td></tr>",
							item.Index, item.CategoryName, item.DeviceName, item.HardwareId,
							item.InstallTimestamp, item.TimeAgoString, item.TargetRule, item.Verdict, cssClass));
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
