using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace MCCheatChecker
{
	public class SleepWakeTimeJumpAndRebootViewerForm : Form
	{
		#region Data Models

		public enum PowerEventType
		{
			TimeJump,
			Sleep,
			Wake,
			HardResetPanic,
			CleanShutdown,
			BootStartup,
			BsodCrash,
			Other
		}

		public enum PowerEventFilterCategory
		{
			All,
			Violations508_504,
			TimeJumps,
			SleepAndWake,
			CrashesAndHardResets,
			CleanShutdowns
		}

		public class PowerForensicItem
		{
			public int Index;
			public DateTime EventTime;
			public string AgeDisplay;
			public int EventId;
			public string ProviderName;
			public PowerEventType EventType;
			public string TypeName;
			public string SummaryDetails;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsTimeJump;
			public bool IsPanicSleep;
			public bool IsHardReset;
			public Color RowColor;
			public string RawXml;
			public string FormattedDescription;
			public Dictionary<string, string> EventDataParams;
			public string ForensicReport;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private PowerEventFilterCategory currentFilter = PowerEventFilterCategory.All;
		private List<PowerForensicItem> allItems = new List<PowerForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtXmlData;
		private DarkTextBoxMultiline txtDescription;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiTimeJumps;
		private Label lblKpiSleeps;
		private Label lblKpiHardResets;
		private Label lblKpiUptime;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterTimeJumps;
		private DarkButton btnFilterSleepWake;
		private DarkButton btnFilterCrashes;
		private DarkButton btnFilterCleanShutdowns;

		private DarkButton btnCopyRow;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		private TabControl detailTabs;

		#endregion

		public SleepWakeTimeJumpAndRebootViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "⏱️ Инспектор событий сна, скачков системного времени и перезагрузок — MCCheatChecker";
			Size = new Size(1300, 780);
			MinimumSize = new Size(1060, 600);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			// Top Header Panel
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "⏱️ Инспектор событий сна, скачков времени и перезагрузок (Event 1 / 41 / 42 / 107 / 1074 / 6008)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Аудит событий питания и времени Windows: фиксация панического сна перед проверкой (Event 42), изменения системных часов (Time Jump Event 1) и сбросов питания/Hard Reset (Event 41/6008) по правилам 5.08 и 5.04.";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация чтения журнала System...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(35, 45, 55);
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(17, 68);
			topPanel.Controls.Add(lblStatusPill);

			// KPI Panel inside Header
			Panel kpiPanel = new Panel();
			kpiPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			kpiPanel.Size = new Size(650, 60);
			kpiPanel.Location = new Point(topPanel.Width - 666, 14);
			topPanel.Resize += delegate
			{
				kpiPanel.Location = new Point(topPanel.Width - 666, 14);
			};

			int kpiX = 0;
			kpiPanel.Controls.Add(CreateKpiBadge("Всего событий", "—", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("⏰ Скачков времени", "0", Ui.AccentOrange, ref kpiX, out lblKpiTimeJumps));
			kpiPanel.Controls.Add(CreateKpiBadge("💤 Событий сна", "0", Ui.AccentBlue, ref kpiX, out lblKpiSleeps));
			kpiPanel.Controls.Add(CreateKpiBadge("⚡ Аварийных ребутов", "0", Ui.AccentOrange, ref kpiX, out lblKpiHardResets));
			kpiPanel.Controls.Add(CreateKpiBadge("⏱️ Uptime", "—", Ui.AccentGreen, ref kpiX, out lblKpiUptime));
			topPanel.Controls.Add(kpiPanel);

			Controls.Add(topPanel);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все события", PowerEventFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08/5.04)", PowerEventFilterCategory.Violations508_504, ref fBtnX, filterBar);
			btnFilterTimeJumps = CreateFilterButton("⏰ Скачки времени (Event 1)", PowerEventFilterCategory.TimeJumps, ref fBtnX, filterBar);
			btnFilterSleepWake = CreateFilterButton("💤 Сон и пробуждение (42/1/107)", PowerEventFilterCategory.SleepAndWake, ref fBtnX, filterBar);
			btnFilterCrashes = CreateFilterButton("⚡ Аварийные сбросы (41/6008)", PowerEventFilterCategory.CrashesAndHardResets, ref fBtnX, filterBar);
			btnFilterCleanShutdowns = CreateFilterButton("🔄 Штатные выключения (1074/12)", PowerEventFilterCategory.CleanShutdowns, ref fBtnX, filterBar);

			// Search Box
			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(240, 26);
			searchBox.Location = new Point(filterBar.Width - 254, 10);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Resize += delegate
			{
				searchBox.Location = new Point(filterBar.Width - 254, 10);
			};
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextMuted;
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 320, 14);
			filterBar.Resize += delegate
			{
				lblSearch.Location = new Point(filterBar.Width - 320, 14);
			};
			filterBar.Controls.Add(lblSearch);

			Controls.Add(filterBar);

			// Main Split Container
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 420;
			split.SplitterWidth = 6;
			split.BackColor = Ui.Border;

			// Top Panel: ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("#", 45);
			listView.Columns.Add("Время события", 145);
			listView.Columns.Add("Давность", 100);
			listView.Columns.Add("ID", 65);
			listView.Columns.Add("Источник", 170);
			listView.Columns.Add("Тип операции", 180);
			listView.Columns.Add("Параметры / Разница", 320);
			listView.Columns.Add("Вердикт", 220);
			listView.Columns.Add("Регламент", 110);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			// Context Menu
			ContextMenuStrip cms = new ContextMenuStrip();
			cms.Renderer = new DarkMenuRenderer();
			ToolStripMenuItem miCopyRow = new ToolStripMenuItem("📋 Скопировать информацию о событии", null, delegate { CopySelectedRow(); });
			ToolStripMenuItem miCopyDiscord = new ToolStripMenuItem("💬 Скопировать строку для Discord / VK", null, delegate { CopySelectedDiscordRow(); });
			ToolStripMenuItem miCopyBan = new ToolStripMenuItem("🔨 Скопировать команду бана", null, delegate { CopySelectedBanCommand(); });
			ToolStripMenuItem miCopyXml = new ToolStripMenuItem("📋 Скопировать сырой XML события", null, delegate { CopySelectedXml(); });
			cms.Items.AddRange(new ToolStripItem[] { miCopyRow, new ToolStripSeparator(), miCopyDiscord, miCopyBan, miCopyXml });
			listView.ContextMenuStrip = cms;

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details with TabControl
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 8, 12, 8);

			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Font = Ui.FontBody(8.5f);

			// Tab 1: Forensic Report
			TabPage tabReport = new TabPage("📄 Судебное заключение");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.Font = Ui.FontMono(8.5f);
			txtForensicReport.ReadOnly = true;
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			// Tab 2: EventData Parameters
			TabPage tabXml = new TabPage("🔍 XML метаданные EventData");
			tabXml.BackColor = Ui.Surface;
			txtXmlData = new DarkTextBoxMultiline();
			txtXmlData.Dock = DockStyle.Fill;
			txtXmlData.Font = Ui.FontMono(8.5f);
			txtXmlData.ReadOnly = true;
			tabXml.Controls.Add(txtXmlData);
			detailTabs.TabPages.Add(tabXml);

			// Tab 3: Formatted Description
			TabPage tabDesc = new TabPage("📋 Системное описание Windows");
			tabDesc.BackColor = Ui.Surface;
			txtDescription = new DarkTextBoxMultiline();
			txtDescription.Dock = DockStyle.Fill;
			txtDescription.Font = Ui.FontMono(8.5f);
			txtDescription.ReadOnly = true;
			tabDesc.Controls.Add(txtDescription);
			detailTabs.TabPages.Add(tabDesc);

			detailContainer.Controls.Add(detailTabs);
			split.Panel2.Controls.Add(detailContainer);

			Controls.Add(split);

			// Bottom Action Bar
			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 48;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(14, 8, 14, 8);
			actionBar.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen p = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(p, 0, 0, actionBar.Width, 0);
				}
			};

			int aX = 14;
			btnCopyRow = CreateActionButton("📋 Скопировать инфо", ref aX, actionBar, delegate { CopySelectedRow(); });
			btnCopyDiscord = CreateActionButton("💬 Строка отчета", ref aX, actionBar, delegate { CopySelectedDiscordRow(); });
			btnCopyBanCmd = CreateActionButton("🔨 Команда бана", ref aX, actionBar, delegate { CopySelectedBanCommand(); });

			DarkButton btnExportHtml = CreateActionButton("📑 Экспорт в HTML", ref aX, actionBar, delegate { ExportToHtml(); });

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Size = new Size(160, 32);
			btnRescan.Location = new Point(actionBar.Width - 174, 8);
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Font = Ui.FontBody(8.5f, true);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Resize += delegate
			{
				btnRescan.Location = new Point(actionBar.Width - 174, 8);
			};
			actionBar.Controls.Add(btnRescan);

			Controls.Add(actionBar);
			split.BringToFront();

			KeyPreview = true;
			KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					Close();
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartScan();
				}
			};
		}

		private Panel CreateKpiBadge(string caption, string initialVal, Color valColor, ref int curX, out Label valLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(104, 52);
			p.Location = new Point(curX, 4);
			p.BackColor = Ui.SurfaceAlt;
			p.Padding = new Padding(4, 4, 4, 4);

			Label lblCaption = new Label();
			lblCaption.Text = caption;
			lblCaption.Font = Ui.FontBody(7.5f);
			lblCaption.ForeColor = Ui.TextMuted;
			lblCaption.Dock = DockStyle.Top;
			lblCaption.Height = 16;
			lblCaption.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(lblCaption);

			valLabel = new Label();
			valLabel.Text = initialVal;
			valLabel.Font = Ui.FontTitle(11.5f, true);
			valLabel.ForeColor = valColor;
			valLabel.Dock = DockStyle.Fill;
			valLabel.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(valLabel);
			valLabel.BringToFront();

			curX += 108;
			return p;
		}

		private DarkButton CreateFilterButton(string text, PowerEventFilterCategory cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(155, 30);
			btn.Location = new Point(curX, 8);
			btn.Font = Ui.FontBody(8.5f, cat == PowerEventFilterCategory.All);
			btn.Click += delegate
			{
				currentFilter = cat;
				UpdateFilterButtonStates();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 161;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int curX, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(145, 32);
			btn.Location = new Point(curX, 8);
			btn.Font = Ui.FontBody(8.5f);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 151;
			return btn;
		}

		private void UpdateFilterButtonStates()
		{
			btnFilterAll.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.All);
			btnFilterViolations.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.Violations508_504);
			btnFilterTimeJumps.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.TimeJumps);
			btnFilterSleepWake.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.SleepAndWake);
			btnFilterCrashes.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.CrashesAndHardResets);
			btnFilterCleanShutdowns.Font = Ui.FontBody(8.5f, currentFilter == PowerEventFilterCategory.CleanShutdowns);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			lblStatusPill.Text = "⏳ Анализ событий питания, сна и скачков времени...";
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
			List<PowerForensicItem> list = new List<PowerForensicItem>();
			int globalIdx = 1;

			string query = @"*[System[
				(Provider[@Name='Microsoft-Windows-Kernel-General'] and (EventID=1 or EventID=12 or EventID=13)) or
				(Provider[@Name='Microsoft-Windows-Power-Troubleshooter'] and EventID=1) or
				(Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=41 or EventID=42 or EventID=107)) or
				(Provider[@Name='USER32'] and EventID=1074) or
				(Provider[@Name='EventLog'] and (EventID=6005 or EventID=6006 or EventID=6008)) or
				(Provider[@Name='Microsoft-Windows-WER-SystemErrorReporting'] and EventID=1001) or
				(Provider[@Name='BugCheck'] and EventID=1001)
			]]";

			try
			{
				EventLogQuery elq = new EventLogQuery("System", PathType.LogName, query);
				elq.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord rec;
					int maxCount = 200;
					int processed = 0;

					while ((rec = reader.ReadEvent()) != null && processed < maxCount)
					{
						using (rec)
						{
							PowerForensicItem item = ParsePowerEvent(rec, globalIdx++);
							if (item != null)
							{
								list.Add(item);
							}
							processed++;
						}
					}
				}
			}
			catch { }

			e.Result = list;
		}

		private PowerForensicItem ParsePowerEvent(EventRecord rec, int index)
		{
			if (rec == null) return null;

			int id = rec.Id;
			string provider = rec.ProviderName ?? "Unknown";
			DateTime eventTime = rec.TimeCreated ?? DateTime.MinValue;
			if (eventTime == DateTime.MinValue) return null;

			string xml = "";
			try { xml = rec.ToXml(); } catch { }

			string desc = "";
			try { desc = rec.FormatDescription(); } catch { }

			Dictionary<string, string> eventData = ParseXmlEventData(xml);

			PowerForensicItem item = new PowerForensicItem();
			item.Index = index;
			item.EventTime = eventTime;
			item.AgeDisplay = FormatAge(eventTime);
			item.EventId = id;
			item.ProviderName = provider;
			item.RawXml = xml;
			item.FormattedDescription = desc;
			item.EventDataParams = eventData;

			TimeSpan ageSpan = DateTime.Now - eventTime;
			double ageMinutes = ageSpan.TotalMinutes;

			// Classify event
			if (provider.Equals("Microsoft-Windows-Kernel-General", StringComparison.OrdinalIgnoreCase) && id == 1)
			{
				// System Time Changed (Time Jump)
				item.EventType = PowerEventType.TimeJump;
				item.TypeName = "⏰ Скачок системного времени";

				string oldTime = eventData.ContainsKey("OldTime") ? eventData["OldTime"] : "";
				string newTime = eventData.ContainsKey("NewTime") ? eventData["NewTime"] : "";
				string reason = eventData.ContainsKey("ChangeReason") ? eventData["ChangeReason"] : "Синхронизация/Служба";

				double jumpSec = 0;
				DateTime dtOld = DateTime.MinValue;
				DateTime dtNew = DateTime.MinValue;
				if (DateTime.TryParse(oldTime, out dtOld) && DateTime.TryParse(newTime, out dtNew))
				{
					jumpSec = (dtNew - dtOld).TotalSeconds;
				}

				if (Math.Abs(jumpSec) >= 300.0) // 5 minutes or more
				{
					item.IsViolation = true;
					item.IsTimeJump = true;
					item.TargetRule = "5.08 Сокрытие улик";
					item.Verdict = string.Format("🚨 5.08 СКАЧОК ВРЕМЕНИ: {0:0.0} мин", jumpSec / 60.0);
					item.SummaryDetails = string.Format("Перевод часов: {0:0.0} мин | Было: {1} -> Стало: {2}", jumpSec / 60.0, dtOld.ToString("HH:mm:ss"), dtNew.ToString("HH:mm:ss"));
					item.RowColor = Color.FromArgb(255, 100, 100);
				}
				else
				{
					item.TargetRule = "Чисто / Синхронизация";
					item.Verdict = "Штатная синхронизация времени (<5 мин)";
					item.SummaryDetails = string.Format("Сдвиг: {0:0.0} сек | Причина: {1}", jumpSec, reason);
					item.RowColor = Ui.AccentOrange;
				}
			}
			else if (provider.Equals("Microsoft-Windows-Kernel-Power", StringComparison.OrdinalIgnoreCase) && id == 42)
			{
				// Entering Sleep
				item.EventType = PowerEventType.Sleep;
				item.TypeName = "💤 Переход в спящий режим (Sleep)";

				string sleepReason = eventData.ContainsKey("SleepReason") ? eventData["SleepReason"] : "";
				string reasonText = TranslateSleepReason(sleepReason);

				if (ageMinutes <= 60.0)
				{
					item.IsViolation = true;
					item.IsPanicSleep = true;
					item.TargetRule = "5.08 Сокрытие улик";
					item.Verdict = string.Format("🚨 5.08 ПАНИЧЕСКИЙ СОН: {0} назад", item.AgeDisplay);
					item.SummaryDetails = string.Format("Переход в сон за {0:0.0} мин до проверки | Причина: {1}", ageMinutes, reasonText);
					item.RowColor = Color.FromArgb(255, 110, 110);
				}
				else
				{
					item.TargetRule = "Чисто";
					item.Verdict = "Переход в режим сна";
					item.SummaryDetails = string.Format("Причина: {0}", reasonText);
					item.RowColor = Ui.AccentBlue;
				}
			}
			else if (provider.Equals("Microsoft-Windows-Power-Troubleshooter", StringComparison.OrdinalIgnoreCase) && id == 1)
			{
				// Wake up from Sleep
				item.EventType = PowerEventType.Wake;
				item.TypeName = "☀️ Пробуждение из сна (Wake)";

				string sTime = eventData.ContainsKey("SleepTime") ? eventData["SleepTime"] : "";
				string wTime = eventData.ContainsKey("WakeTime") ? eventData["WakeTime"] : "";
				DateTime dtSleep, dtWake;
				string durText = "—";
				if (DateTime.TryParse(sTime, out dtSleep) && DateTime.TryParse(wTime, out dtWake))
				{
					TimeSpan sDur = dtWake - dtSleep;
					durText = string.Format("{0:0.0} мин", sDur.TotalMinutes);
				}

				if (ageMinutes <= 60.0)
				{
					item.IsViolation = true;
					item.TargetRule = "5.08 Сокрытие улик";
					item.Verdict = string.Format("⚠️ Пробуждение перед проверкой (Сон: {0})", durText);
					item.SummaryDetails = string.Format("Выход из сна {0} назад | Длительность сна: {1}", item.AgeDisplay, durText);
					item.RowColor = Color.FromArgb(255, 170, 90);
				}
				else
				{
					item.TargetRule = "Чисто";
					item.Verdict = "Пробуждение системы";
					item.SummaryDetails = string.Format("Длительность сна: {0}", durText);
					item.RowColor = Ui.TextPrimary;
				}
			}
			else if (provider.Equals("Microsoft-Windows-Kernel-Power", StringComparison.OrdinalIgnoreCase) && id == 107)
			{
				item.EventType = PowerEventType.Wake;
				item.TypeName = "☀️ Возобновление работы (Resume)";
				item.SummaryDetails = "Система возобновила работу из спящего режима";
				item.TargetRule = "Чисто";
				item.Verdict = "Возобновление работы";
				item.RowColor = Ui.TextPrimary;
			}
			else if (provider.Equals("Microsoft-Windows-Kernel-Power", StringComparison.OrdinalIgnoreCase) && id == 41)
			{
				// Hard Reset / Kernel Panic / BSOD
				item.EventType = PowerEventType.HardResetPanic;
				item.TypeName = "⚡ Аварийное выключение / Hard Reset";
				item.IsHardReset = true;

				string bugcheck = eventData.ContainsKey("BugcheckCode") ? eventData["BugcheckCode"] : "0";
				string pwrBtn = eventData.ContainsKey("PowerButtonTimestamp") ? eventData["PowerButtonTimestamp"] : "0";

				if (ageMinutes <= 90.0)
				{
					item.IsViolation = true;
					item.TargetRule = "5.04 Уход от проверки / 5.08";
					item.Verdict = string.Format("🚨 5.04/5.08 СБРОС ПИТАНИЯ: {0} назад", item.AgeDisplay);
					item.SummaryDetails = string.Format("Внезапная перезагрузка без чистого выключения | BugCheck: {0}", bugcheck);
					item.RowColor = Color.FromArgb(255, 90, 90);
				}
				else
				{
					item.TargetRule = "Аудит сбоев";
					item.Verdict = "Аварийное выключение питания";
					item.SummaryDetails = string.Format("Сбой питания / Hard Reset | Bugcheck: {0}", bugcheck);
					item.RowColor = Ui.AccentOrange;
				}
			}
			else if (provider.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && id == 6008)
			{
				item.EventType = PowerEventType.HardResetPanic;
				item.TypeName = "⚡ Неожиданное выключение (Event 6008)";
				item.IsHardReset = true;

				if (ageMinutes <= 90.0)
				{
					item.IsViolation = true;
					item.TargetRule = "5.04 Уход от проверки / 5.08";
					item.Verdict = string.Format("🚨 5.04/5.08 НЕОЖИДАННОЕ ВЫКЛЮЧЕНИЕ: {0} назад", item.AgeDisplay);
					item.SummaryDetails = "Предыдущее выключение системы было неожиданным (сброс питания)";
					item.RowColor = Color.FromArgb(255, 90, 90);
				}
				else
				{
					item.TargetRule = "Аудит сбоев";
					item.Verdict = "Неожиданное выключение системы";
					item.SummaryDetails = "Система была выключена некорректно";
					item.RowColor = Ui.AccentOrange;
				}
			}
			else if (provider.Equals("USER32", StringComparison.OrdinalIgnoreCase) && id == 1074)
			{
				// Clean Shutdown / Restart initiated by user/process
				item.EventType = PowerEventType.CleanShutdown;
				item.TypeName = "🔄 Инициированное выключение / перезагрузка";

				string proc = eventData.ContainsKey("param1") ? eventData["param1"] : "winlogon.exe";
				string user = eventData.ContainsKey("param7") ? eventData["param7"] : "";
				string type = eventData.ContainsKey("param5") ? eventData["param5"] : "Выключение";

				if (ageMinutes <= 60.0)
				{
					item.IsViolation = true;
					item.TargetRule = "5.04 Уход от проверки / 5.08";
					item.Verdict = string.Format("⚠️ Выключение ПК незадолго до проверки ({0})", type);
					item.SummaryDetails = string.Format("Инициатор: {0} | Пользователь: {1} | Тип: {2}", proc, user, type);
					item.RowColor = Color.FromArgb(255, 170, 90);
				}
				else
				{
					item.TargetRule = "Чисто";
					item.Verdict = string.Format("Штатное действие: {0}", type);
					item.SummaryDetails = string.Format("Инициатор: {0} | Пользователь: {1}", proc, user);
					item.RowColor = Ui.TextMuted;
				}
			}
			else if (provider.Equals("Microsoft-Windows-Kernel-General", StringComparison.OrdinalIgnoreCase) && id == 12)
			{
				item.EventType = PowerEventType.BootStartup;
				item.TypeName = "🚀 Запуск операционной системы";
				item.SummaryDetails = "Старт ядра Windows (Boot Startup)";
				item.TargetRule = "Чисто";
				item.Verdict = "Запуск ОС";
				item.RowColor = Ui.AccentGreen;
			}
			else if (provider.Equals("Microsoft-Windows-Kernel-General", StringComparison.OrdinalIgnoreCase) && id == 13)
			{
				item.EventType = PowerEventType.CleanShutdown;
				item.TypeName = "🛑 Завершение работы ОС (Shutdown)";
				item.SummaryDetails = "Штатная остановка ядра Windows";
				item.TargetRule = "Чисто";
				item.Verdict = "Остановка ОС";
				item.RowColor = Ui.TextMuted;
			}
			else if (provider.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && id == 6005)
			{
				item.EventType = PowerEventType.BootStartup;
				item.TypeName = "📋 Служба EventLog запущена";
				item.SummaryDetails = "Старт службы журнала событий Windows";
				item.TargetRule = "Чисто";
				item.Verdict = "Служба журналов запущена";
				item.RowColor = Ui.TextMuted;
			}
			else if (provider.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && id == 6006)
			{
				item.EventType = PowerEventType.CleanShutdown;
				item.TypeName = "📋 Служба EventLog остановлена";
				item.SummaryDetails = "Остановка службы журнала событий перед выключением";
				item.TargetRule = "Чисто";
				item.Verdict = "Служба журналов остановлена";
				item.RowColor = Ui.TextMuted;
			}
			else
			{
				item.EventType = PowerEventType.Other;
				item.TypeName = string.Format("Событие {0}", id);
				item.SummaryDetails = string.IsNullOrEmpty(desc) ? provider : desc;
				item.TargetRule = "Чисто";
				item.Verdict = "Событие питания/системы";
				item.RowColor = Ui.TextPrimary;
			}

			item.ForensicReport = BuildEventReport(item);
			return item;
		}

		private static Dictionary<string, string> ParseXmlEventData(string xml)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrEmpty(xml)) return dict;

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);

				XmlNodeList dataNodes = doc.SelectNodes("//*[local-name()='Data']");
				if (dataNodes != null)
				{
					int unnamedIdx = 1;
					foreach (XmlNode n in dataNodes)
					{
						string name = n.Attributes != null && n.Attributes["Name"] != null ? n.Attributes["Name"].Value : ("param" + unnamedIdx++);
						string val = n.InnerText != null ? n.InnerText.Trim() : "";
						dict[name] = val;
					}
				}
			}
			catch { }

			return dict;
		}

		private static string TranslateSleepReason(string reasonCode)
		{
			int r;
			if (!int.TryParse(reasonCode, out r)) return string.IsNullOrEmpty(reasonCode) ? "Не указана" : reasonCode;

			switch (r)
			{
				case 0: return "Другая / Внутренняя (0)";
				case 1: return "Нажатие кнопки питания (1)";
				case 2: return "Закрытие крышки ноутбука (2)";
				case 3: return "Разряд батареи (3)";
				case 4: return "Инициировано приложением / скриптом (4)";
				case 5: return "Тайм-аут бездействия системы (5)";
				case 6: return "Команда спящего режима (6)";
				default: return string.Format("Код причины {0}", r);
			}
		}

		private static string FormatAge(DateTime dt)
		{
			TimeSpan diff = DateTime.Now - dt;
			if (diff.TotalMinutes < 1.0) return "Только что";
			if (diff.TotalMinutes < 60.0) return string.Format("{0:0} мин назад", diff.TotalMinutes);
			if (diff.TotalHours < 24.0) return string.Format("{0:0.0} ч назад", diff.TotalHours);
			if (diff.TotalDays < 2.0) return "Вчера";
			return string.Format("{0:0} дн назад", diff.TotalDays);
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			allItems = e.Result as List<PowerForensicItem> ?? new List<PowerForensicItem>();

			int total = allItems.Count;
			int viols = 0;
			int tj = 0;
			int sl = 0;
			int hr = 0;

			foreach (PowerForensicItem it in allItems)
			{
				if (it.IsViolation) viols++;
				if (it.EventType == PowerEventType.TimeJump && it.IsTimeJump) tj++;
				if (it.EventType == PowerEventType.Sleep) sl++;
				if (it.EventType == PowerEventType.HardResetPanic) hr++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = viols.ToString();
			lblKpiTimeJumps.Text = tj.ToString();
			lblKpiSleeps.Text = sl.ToString();
			lblKpiHardResets.Text = hr.ToString();

			// Current uptime
			TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
			lblKpiUptime.Text = string.Format("{0:0}ч {1:0}м", uptime.TotalHours, uptime.Minutes);

			if (viols > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ ПОДОЗРИТЕЛЬНЫЕ СОБЫТИЯ: {0} нарушений (Сон/Сброс/Скачки времени)", viols);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✅ Проверено {0} системных событий питания. Аномалий не зафиксировано.", total);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & ListView Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox != null ? searchBox.Text : "").Trim().ToLowerInvariant();

			int rowNumber = 1;
			foreach (PowerForensicItem item in allItems)
			{
				if (currentFilter == PowerEventFilterCategory.Violations508_504 && !item.IsViolation) continue;
				if (currentFilter == PowerEventFilterCategory.TimeJumps && item.EventType != PowerEventType.TimeJump) continue;
				if (currentFilter == PowerEventFilterCategory.SleepAndWake && item.EventType != PowerEventType.Sleep && item.EventType != PowerEventType.Wake) continue;
				if (currentFilter == PowerEventFilterCategory.CrashesAndHardResets && item.EventType != PowerEventType.HardResetPanic && item.EventType != PowerEventType.BsodCrash) continue;
				if (currentFilter == PowerEventFilterCategory.CleanShutdowns && item.EventType != PowerEventType.CleanShutdown && item.EventType != PowerEventType.BootStartup) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string matchTarget = string.Format("{0} {1} {2} {3} {4} {5} {6}",
						item.EventId, item.ProviderName, item.TypeName, item.SummaryDetails, item.Verdict, item.TargetRule, item.EventTime.ToString("yyyy-MM-dd HH:mm:ss")).ToLowerInvariant();
					if (!matchTarget.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(rowNumber.ToString());
				lvi.SubItems.Add(item.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.AgeDisplay);
				lvi.SubItems.Add(item.EventId.ToString());
				lvi.SubItems.Add(item.ProviderName);
				lvi.SubItems.Add(item.TypeName);
				lvi.SubItems.Add(item.SummaryDetails);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.TargetRule);

				lvi.ForeColor = item.RowColor;
				lvi.Tag = item;

				listView.Items.Add(lvi);
				rowNumber++;
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

			PowerForensicItem item = listView.SelectedItems[0].Tag as PowerForensicItem;
			if (item == null)
			{
				ClearDetails();
				return;
			}

			txtForensicReport.Text = item.ForensicReport ?? "Нет отчета.";
			txtDescription.Text = item.FormattedDescription ?? "(Описание отсутствует в манифесте)";

			StringBuilder sbXml = new StringBuilder();
			sbXml.AppendLine(string.Format("=== РАСПАРСЕННЫЕ ПАРАМЕТРЫ EVENTDATA (Event ID {0}) ===", item.EventId));
			sbXml.AppendLine(string.Format("Источник : {0}", item.ProviderName));
			sbXml.AppendLine(string.Format("Время    : {0:yyyy-MM-dd HH:mm:ss.fff}", item.EventTime));
			sbXml.AppendLine("--------------------------------------------------------------------------");
			if (item.EventDataParams != null && item.EventDataParams.Count > 0)
			{
				foreach (KeyValuePair<string, string> kvp in item.EventDataParams)
				{
					sbXml.AppendLine(string.Format("{0,-24} = {1}", kvp.Key, kvp.Value));
				}
			}
			else
			{
				sbXml.AppendLine("(Параметры EventData отсутствуют)");
			}
			sbXml.AppendLine();
			sbXml.AppendLine("=== СЫРОЙ XML ЖУРНАЛА СОБЫТИЙ ===");
			sbXml.AppendLine(item.RawXml ?? "");
			txtXmlData.Text = sbXml.ToString();
		}

		private void ClearDetails()
		{
			txtForensicReport.Text = "Выберите событие из списка выше для криминалистического анализа.";
			txtXmlData.Text = "";
			txtDescription.Text = "";
		}

		#endregion

		#region Forensic Report Construction

		private string BuildEventReport(PowerForensicItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("==========================================================================");
			sb.AppendLine("   СУДЕБНЫЙ ОТЧЕТ: АУДИТ СОБЫТИЙ ПИТАНИЯ, СНА И ТАЙМЛАЙНА СИСТЕМЫ");
			sb.AppendLine("==========================================================================");
			sb.AppendLine(string.Format("Целевой игрок проверки : {0}", targetPlayerName));
			sb.AppendLine(string.Format("Точное время фиксации  : {0:yyyy-MM-dd HH:mm:ss.fff}", item.EventTime));
			sb.AppendLine(string.Format("Давность события       : {0}", item.AgeDisplay));
			sb.AppendLine(string.Format("Идентификатор события  : Event ID {0} ({1})", item.EventId, item.ProviderName));
			sb.AppendLine(string.Format("Тип зафиксированного действия: {0}", item.TypeName));
			sb.AppendLine(string.Format("Параметры операции     : {0}", item.SummaryDetails));
			sb.AppendLine(string.Format("Судебный вердикт       : {0}", item.Verdict));
			sb.AppendLine(string.Format("Применимый пункт правил: {0}", item.TargetRule));
			sb.AppendLine("--------------------------------------------------------------------------");
			sb.AppendLine("КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ И МЕХАНИЗМ УГРОЗЫ:");

			if (item.EventType == PowerEventType.TimeJump && item.IsViolation)
			{
				sb.AppendLine("🚨 ОБНАРУЖЕНО ИСКУССТВЕННОЕ ИЗМЕНЕНИЕ СИСТЕМНОГО ВРЕМЕНИ (TIME JUMP)!");
				sb.AppendLine("Манипуляция системными часами — частая тактика читеров для фальсификации");
				sb.AppendLine("таймстампов Prefetch, USN Journal, Amcache и создания ложного алиби («чит");
				sb.AppendLine("запускался неделю назад, а не сегодня»).");
				sb.AppendLine("Данное действие трактуется как умышленное сокрытие улик (Пункт 5.08).");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.08 Сокрытие улик (Скачок системного времени {1})", targetPlayerName, item.SummaryDetails));
			}
			else if (item.EventType == PowerEventType.Sleep && item.IsPanicSleep)
			{
				sb.AppendLine("🚨 ОБНАРУЖЕН ПАНИЧЕСКИЙ ПЕРЕХОД ПК В СПЯЩИЙ РЕЖИМ (SLEEP / STANDBY)!");
				sb.AppendLine("Игрок перевел компьютер в спящий режим за несколько минут до или во время проверки.");
				sb.AppendLine("При переходе в сон сбрасываются энергозависимые участки RAM, закрываются сетевые сокеты,");
				sb.AppendLine("демонтируются виртуальные диски и стираются временные инжекты в память javaw.");
				sb.AppendLine("В соответствии с регламентом это признается сокрытием улик (Пункт 5.08).");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.08 Сокрытие улик (Панический перевод ПК в сон за {0} мин до SS)", item.AgeDisplay));
			}
			else if (item.EventType == PowerEventType.HardResetPanic && item.IsViolation)
			{
				sb.AppendLine("🚨 ОБНАРУЖЕНО АВАРИЙНОЕ ВЫКЛЮЧЕНИЕ / ПРИНУДИТЕЛЬНЫЙ СБРОС ПИТАНИЯ (HARD RESET)!");
				sb.AppendLine("Компьютер был экстренно выключен выдергиванием вилки из розетки или удержанием");
				sb.AppendLine("кнопки Power. Это делается для имитации краша ПК и ухода от проверки ScreenShare.");
				sb.AppendLine("Трактуется по пунктам 5.04 (Отказ/Уход от проверки) и 5.08 (Сокрытие улик).");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.04 Уход от проверки (Принудительный Hard Reset питания)", targetPlayerName));
			}
			else if (item.EventType == PowerEventType.CleanShutdown && item.IsViolation)
			{
				sb.AppendLine("⚠️ ЗАФИКСИРОВАНО ИНИЦИИРОВАННОЕ ВЫКЛЮЧЕНИЕ ПК ПЕРЕД ПРОВЕРКОЙ!");
				sb.AppendLine("Игрок отдал команду выключения/перезагрузки операционной системы прямо перед проверкой.");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.04 Отказ от проверки (Выключение ПК)", targetPlayerName));
			}
			else
			{
				sb.AppendLine("Событие отражает штатный цикл включения, выключения или штатной синхронизации времени ОС.");
			}

			sb.AppendLine("==========================================================================");
			return sb.ToString();
		}

		#endregion

		#region Context Actions

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0) return;
			PowerForensicItem item = listView.SelectedItems[0].Tag as PowerForensicItem;
			if (item == null) return;

			string text = string.Format("{0:yyyy-MM-dd HH:mm:ss} | {1} | Event {2} | {3} | {4}",
				item.EventTime, item.ProviderName, item.EventId, item.TypeName, item.SummaryDetails);
			Clipboard.SetText(text);
			MessageBox.Show("Данные события скопированы в буфер:\n" + text, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopySelectedDiscordRow()
		{
			if (listView.SelectedItems.Count == 0) return;
			PowerForensicItem item = listView.SelectedItems[0].Tag as PowerForensicItem;
			if (item == null) return;

			string row = string.Format("⏱️ [Power & Timeline] {0:yyyy-MM-dd HH:mm:ss} ({1}) | {2} | ID {3} | {4} | Вердикт: {5}",
				item.EventTime, item.AgeDisplay, item.ProviderName, item.EventId, item.SummaryDetails, item.Verdict);
			Clipboard.SetText(row);
			MessageBox.Show("Строка скопирована в буфер для отчета Discord/VK:\n" + row, "Строка отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopySelectedBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			PowerForensicItem item = listView.SelectedItems[0].Tag as PowerForensicItem;
			if (item == null) return;

			string cmd;
			if (item.IsTimeJump)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Искусственный скачок системного времени)", targetPlayerName);
			}
			else if (item.IsPanicSleep)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Панический перевод ПК в сон перед ScreenShare)", targetPlayerName);
			}
			else if (item.IsHardReset)
			{
				cmd = string.Format("/ban {0} 30d 5.04 Уход от проверки (Аварийный сброс питания Hard Reset)", targetPlayerName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик ({1})", targetPlayerName, item.TypeName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopySelectedXml()
		{
			if (listView.SelectedItems.Count == 0) return;
			PowerForensicItem item = listView.SelectedItems[0].Tag as PowerForensicItem;
			if (item == null || string.IsNullOrEmpty(item.RawXml)) return;

			Clipboard.SetText(item.RawXml);
			MessageBox.Show("XML события скопирован в буфер обмена.", "XML", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "HTML Report (*.html)|*.html|Текстовый отчет (*.txt)|*.txt";
				sfd.FileName = string.Format("PowerTimeline_Report_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

				if (sfd.ShowDialog() != DialogResult.OK) return;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("<!DOCTYPE html>");
				sb.AppendLine("<html lang=\"ru\">");
				sb.AppendLine("<head>");
				sb.AppendLine("<meta charset=\"utf-8\" />");
				sb.AppendLine("<title>Судебная экспертиза событий питания и сна — " + targetPlayerName + "</title>");
				sb.AppendLine("<style>");
				sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #121417; color: #E1E3E6; margin: 20px; }");
				sb.AppendLine("h1 { color: #4FA3E3; margin-bottom: 4px; }");
				sb.AppendLine(".sub { color: #8C939D; font-size: 13px; margin-bottom: 20px; }");
				sb.AppendLine(".kpi-container { display: flex; gap: 12px; margin-bottom: 24px; }");
				sb.AppendLine(".kpi-card { background: #1A1D21; border: 1px solid #282C34; border-radius: 6px; padding: 12px 16px; min-width: 120px; text-align: center; }");
				sb.AppendLine(".kpi-title { font-size: 11px; color: #8C939D; text-transform: uppercase; margin-bottom: 6px; }");
				sb.AppendLine(".kpi-val { font-size: 20px; font-weight: bold; }");
				sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 12px; }");
				sb.AppendLine("th, td { border: 1px solid #282C34; padding: 8px 10px; text-align: left; }");
				sb.AppendLine("th { background-color: #1E2228; color: #A0A8B4; }");
				sb.AppendLine("tr:nth-child(even) { background-color: #16191D; }");
				sb.AppendLine(".bad { color: #FF6E6E; font-weight: bold; }");
				sb.AppendLine(".warn { color: #FFB347; font-weight: bold; }");
				sb.AppendLine(".good { color: #78D98C; }");
				sb.AppendLine(".footer { margin-top: 30px; font-size: 11px; color: #5C6370; }");
				sb.AppendLine("</style>");
				sb.AppendLine("</head>");
				sb.AppendLine("<body>");
				sb.AppendLine("<h1>⏱️ Судебная экспертиза событий сна, скачков времени и перезагрузок</h1>");
				sb.AppendLine(string.Format("<div class=\"sub\">Игрок: <b>{0}</b> | Дата генерации: {1:dd.MM.yyyy HH:mm:ss} | Модуль MCCheatChecker</div>", targetPlayerName, DateTime.Now));

				sb.AppendLine("<div class=\"kpi-container\">");
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">Всего событий</div><div class=\"kpi-val\" style=\"color:#E1E3E6;\">{0}</div></div>", lblKpiTotal.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">🚨 Нарушений (5.08)</div><div class=\"kpi-val\" style=\"color:#FF6E6E;\">{0}</div></div>", lblKpiViolations.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">⏰ Скачков времени</div><div class=\"kpi-val\" style=\"color:#FFB347;\">{0}</div></div>", lblKpiTimeJumps.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">💤 Событий сна</div><div class=\"kpi-val\" style=\"color:#4FA3E3;\">{0}</div></div>", lblKpiSleeps.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">⚡ Сбросов питания</div><div class=\"kpi-val\" style=\"color:#FFB347;\">{0}</div></div>", lblKpiHardResets.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">⏱️ Текущий Uptime</div><div class=\"kpi-val\" style=\"color:#78D98C;\">{0}</div></div>", lblKpiUptime.Text));
				sb.AppendLine("</div>");

				sb.AppendLine("<table>");
				sb.AppendLine("<thead><tr><th>#</th><th>Время</th><th>Давность</th><th>ID</th><th>Источник</th><th>Тип</th><th>Параметры</th><th>Вердикт</th><th>Регламент</th></tr></thead>");
				sb.AppendLine("<tbody>");

				int r = 1;
				foreach (PowerForensicItem it in allItems)
				{
					string css = it.IsViolation ? "bad" : (it.EventType == PowerEventType.TimeJump ? "warn" : "");
					sb.AppendLine("<tr>");
					sb.AppendLine(string.Format("<td>{0}</td>", r++));
					sb.AppendLine(string.Format("<td>{0:yyyy-MM-dd HH:mm:ss}</td>", it.EventTime));
					sb.AppendLine(string.Format("<td>{0}</td>", it.AgeDisplay));
					sb.AppendLine(string.Format("<td><b>{0}</b></td>", it.EventId));
					sb.AppendLine(string.Format("<td>{0}</td>", it.ProviderName));
					sb.AppendLine(string.Format("<td><b>{0}</b></td>", it.TypeName));
					sb.AppendLine(string.Format("<td><code>{0}</code></td>", it.SummaryDetails));
					sb.AppendLine(string.Format("<td class=\"{0}\">{1}</td>", css, it.Verdict));
					sb.AppendLine(string.Format("<td>{0}</td>", it.TargetRule));
					sb.AppendLine("</tr>");
				}

				sb.AppendLine("</tbody></table>");
				sb.AppendLine("<div class=\"footer\">Отчет сформирован программным комплексом MCCheatChecker. Все права защищены.</div>");
				sb.AppendLine("</body></html>");

				File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
				MessageBox.Show("Отчет успешно сохранен в файл:\n" + sfd.FileName, "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка сохранения отчета: " + ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
