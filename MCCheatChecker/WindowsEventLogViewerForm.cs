using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class WindowsEventLogViewerForm : Form
	{
		#region Models

		public enum EventFilterMode
		{
			All,
			LogClearingOnly,      // 104, 1102, 1100, 3079, 142 (5.08)
			ServiceInstalls,      // 7045 (Drivers / Injections)
			ServiceTampering,     // 7040, 7036 (Disabling DPS, SysMain, EventLog, etc.)
			AppCrashes,           // 1000, 1001 (javaw crashes, cheat crashes)
			DefenderDetections,   // 1116, 1117, 5001, 5007 (Defender detections / disabled)
			TimeRewinds,          // 1 (Time changes / 5.08)
			UsbConnections        // 20001, 20003 (USB Storage)
		}

		public enum TimeRangeMode
		{
			Last24Hours,
			Last3Days,
			Last7Days,
			Last30Days,
			AllTime
		}

		public class EventLogHit
		{
			public string Channel;
			public int EventId;
			public DateTime TimeCreated;
			public string ProviderName;
			public string Verdict;
			public Color ForeColor;
			public string TargetOrSource;
			public string ShortSummary;
			public string FullDescription;
			public bool IsCritical;
			public bool IsSuspicious;
			public bool IsWhitelisted;
			public string ViolationRule;
			public string BanReason;
			public string RawXml;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private EventFilterMode currentFilter = EventFilterMode.All;
		private TimeRangeMode currentTimeRange = TimeRangeMode.Last24Hours;
		private List<EventLogHit> allHits = new List<EventLogHit>();
		private bool isScanning = false;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;
		private Label lblDetailVerdict;
		private Label lblDetailRule;
		private Label lblDetailBanCommand;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterClearing;
		private DarkButton btnFilterServices;
		private DarkButton btnFilterTampering;
		private DarkButton btnFilterCrashes;
		private DarkButton btnFilterDefender;
		private DarkButton btnFilterTime;
		private DarkButton btnFilterUsb;

		private ComboBox cmbTimeRange;
		private DarkButton btnCopyBan;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatKeywords = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|kprocesshacker|dbk64|minhook|nativehook|aimbot|killaura|velocity|autoclicker|cleaner|wiper|fsutil)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public WindowsEventLogViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			Text = "🛡️ Криминалистический аудит системных журналов Windows Event Log (5.01 / 5.08)";
			Size = new Size(1240, 790);
			MinimumSize = new Size(960, 580);
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
			lblHeaderTitle.Text = "🛡️ Криминалистический анализ системных журналов Windows Event Log (Регламент 5.01 / 5.08)";
			lblHeaderTitle.Font = Ui.FontTitle(11.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(12, 8);
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Трассировка очистки логов (1102/104), установки чит-драйверов (7045), отключения служб (7040/7036), сбоев javaw (1000), детектов Защитника (1116) и скрутки времени (1).";
			lblHeaderSubtitle.Font = Ui.FontBody(8f);
			lblHeaderSubtitle.ForeColor = Ui.TextMuted;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(13, 30);
			topPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования журналов Windows...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(13, 51);
			topPanel.Controls.Add(lblStatusPill);

			// Filter buttons row
			FlowLayoutPanel filterRow = new FlowLayoutPanel();
			filterRow.Location = new Point(12, 75);
			filterRow.Size = new Size(1200, 32);
			filterRow.WrapContents = false;
			filterRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("Все события", EventFilterMode.All);
			btnFilterClearing = CreateFilterButton("🚨 Очистка логов (5.08)", EventFilterMode.LogClearingOnly);
			btnFilterServices = CreateFilterButton("🚨 Установка служб / Драйверов (7045)", EventFilterMode.ServiceInstalls);
			btnFilterTampering = CreateFilterButton("⚠️ Отключение служб (7040/7036)", EventFilterMode.ServiceTampering);
			btnFilterCrashes = CreateFilterButton("💥 Сбои javaw / Читов (1000)", EventFilterMode.AppCrashes);
			btnFilterDefender = CreateFilterButton("🛡️ Детекты Защитника (1116)", EventFilterMode.DefenderDetections);
			btnFilterTime = CreateFilterButton("⏱️ Скрутка времени (1)", EventFilterMode.TimeRewinds);
			btnFilterUsb = CreateFilterButton("🔌 Подключения USB (20001)", EventFilterMode.UsbConnections);

			filterRow.Controls.Add(btnFilterAll);
			filterRow.Controls.Add(btnFilterClearing);
			filterRow.Controls.Add(btnFilterServices);
			filterRow.Controls.Add(btnFilterTampering);
			filterRow.Controls.Add(btnFilterCrashes);
			filterRow.Controls.Add(btnFilterDefender);
			filterRow.Controls.Add(btnFilterTime);
			filterRow.Controls.Add(btnFilterUsb);
			topPanel.Controls.Add(filterRow);

			// Search and actions row
			Panel actionRow = new Panel();
			actionRow.Location = new Point(12, 110);
			actionRow.Size = new Size(1200, 30);
			actionRow.BackColor = Color.Transparent;

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(0, 2);
			searchBox.Size = new Size(270, 24);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			actionRow.Controls.Add(searchBox);

			Label lblSearchHint = new Label();
			lblSearchHint.Text = "🔍 Поиск...";
			lblSearchHint.Location = new Point(275, 6);
			lblSearchHint.AutoSize = true;
			lblSearchHint.ForeColor = Ui.TextMuted;
			lblSearchHint.Font = Ui.FontBody(8f);
			actionRow.Controls.Add(lblSearchHint);

			Label lblRange = new Label();
			lblRange.Text = "Период:";
			lblRange.Location = new Point(345, 6);
			lblRange.AutoSize = true;
			lblRange.ForeColor = Ui.TextPrimary;
			lblRange.Font = Ui.FontBody(8.5f);
			actionRow.Controls.Add(lblRange);

			cmbTimeRange = new ComboBox();
			cmbTimeRange.Location = new Point(400, 2);
			cmbTimeRange.Size = new Size(150, 25);
			cmbTimeRange.DropDownStyle = ComboBoxStyle.DropDownList;
			cmbTimeRange.BackColor = Ui.SurfaceAlt;
			cmbTimeRange.ForeColor = Ui.TextPrimary;
			cmbTimeRange.Font = Ui.FontBody(8.5f);
			cmbTimeRange.Items.Add("За 24 часа");
			cmbTimeRange.Items.Add("За 3 дня");
			cmbTimeRange.Items.Add("За 7 дней");
			cmbTimeRange.Items.Add("За 30 дней");
			cmbTimeRange.Items.Add("Все время");
			cmbTimeRange.SelectedIndex = 0;
			cmbTimeRange.SelectedIndexChanged += delegate {
				switch (cmbTimeRange.SelectedIndex)
				{
					case 0: currentTimeRange = TimeRangeMode.Last24Hours; break;
					case 1: currentTimeRange = TimeRangeMode.Last3Days; break;
					case 2: currentTimeRange = TimeRangeMode.Last7Days; break;
					case 3: currentTimeRange = TimeRangeMode.Last30Days; break;
					case 4: currentTimeRange = TimeRangeMode.AllTime; break;
				}
				StartScanAsync();
			};
			actionRow.Controls.Add(cmbTimeRange);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 Обновить";
			btnRefreshNow.Location = new Point(565, 2);
			btnRefreshNow.Size = new Size(95, 25);
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			actionRow.Controls.Add(btnRefreshNow);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Location = new Point(675, 2);
			btnCopyBan.Size = new Size(220, 25);
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			actionRow.Controls.Add(btnCopyBan);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📑 Экспорт протокола аудита";
			btnExportReport.Location = new Point(905, 2);
			btnExportReport.Size = new Size(220, 25);
			btnExportReport.Click += delegate { ExportProtocolToClipboard(); };
			actionRow.Controls.Add(btnExportReport);

			topPanel.Controls.Add(actionRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail pane
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1200, 580);
			try { split.SplitterDistance = 380; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 210);
			listView.Columns.Add("Журнал / ID", 110);
			listView.Columns.Add("Дата и время", 140);
			listView.Columns.Add("Источник / Служба", 180);
			listView.Columns.Add("Краткое содержание / Нарушение", 320);
			listView.Columns.Add("Код / Дополнительно", 190);

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
			lblDetailVerdict.Text = "Выберите событие из таблицы выше для просмотра криминалистических улик.";
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

			// Bottom bar
			Panel bottomBar = new Panel();
			bottomBar.Dock = DockStyle.Bottom;
			bottomBar.Height = 32;
			bottomBar.BackColor = Ui.Surface;
			bottomBar.Padding = new Padding(12, 4, 12, 4);

			Label lblHelp = new Label();
			lblHelp.Text = "💡 Подсказка: Очистка журналов (1102/104) или удаление USN Journal (3079) квалифицируется по Правилу 5.08 (Бан 30 дней).";
			lblHelp.Location = new Point(12, 7);
			lblHelp.AutoSize = true;
			lblHelp.ForeColor = Ui.TextMuted;
			lblHelp.Font = Ui.FontBody(8f);
			bottomBar.Controls.Add(lblHelp);

			Controls.Add(bottomBar);

			UpdateFilterButtonsStyle();
		}

		private DarkButton CreateFilterButton(string text, EventFilterMode mode)
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
			btnFilterAll.BackColor = (currentFilter == EventFilterMode.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterClearing.BackColor = (currentFilter == EventFilterMode.LogClearingOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterServices.BackColor = (currentFilter == EventFilterMode.ServiceInstalls) ? Ui.AccentRed : Ui.Surface;
			btnFilterTampering.BackColor = (currentFilter == EventFilterMode.ServiceTampering) ? Ui.AccentOrange : Ui.Surface;
			btnFilterCrashes.BackColor = (currentFilter == EventFilterMode.AppCrashes) ? Ui.AccentOrange : Ui.Surface;
			btnFilterDefender.BackColor = (currentFilter == EventFilterMode.DefenderDetections) ? Ui.AccentRed : Ui.Surface;
			btnFilterTime.BackColor = (currentFilter == EventFilterMode.TimeRewinds) ? Ui.AccentOrange : Ui.Surface;
			btnFilterUsb.BackColor = (currentFilter == EventFilterMode.UsbConnections) ? Ui.AccentTeal : Ui.Surface;
		}

		#endregion

		#region Event Scanning

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "⏳ Сканирование системных журналов Windows...";
			lblStatusPill.ForeColor = Ui.AccentTeal;

			DateTime cutoff = DateTime.MinValue;
			switch (currentTimeRange)
			{
				case TimeRangeMode.Last24Hours: cutoff = DateTime.Now.AddHours(-24); break;
				case TimeRangeMode.Last3Days: cutoff = DateTime.Now.AddDays(-3); break;
				case TimeRangeMode.Last7Days: cutoff = DateTime.Now.AddDays(-7); break;
				case TimeRangeMode.Last30Days: cutoff = DateTime.Now.AddDays(-30); break;
				case TimeRangeMode.AllTime: cutoff = DateTime.MinValue; break;
			}

			ThreadPool.QueueUserWorkItem(delegate {
				List<EventLogHit> results = new List<EventLogHit>();

				// 1. Scan System Log
				ScanChannel(results, "System", "*[System[(EventID=104 or EventID=7045 or EventID=7040 or EventID=7036 or EventID=1 or EventID=20001 or EventID=20003)]]", cutoff, 400);

				// 2. Scan Application Log
				ScanChannel(results, "Application", "*[System[(EventID=1000 or EventID=1001 or EventID=3079 or EventID=3080)]]", cutoff, 300);

				// 3. Scan Windows Defender Operational Log
				ScanChannel(results, "Microsoft-Windows-Windows Defender/Operational", "*[System[(EventID=1116 or EventID=1117 or EventID=5001 or EventID=5007)]]", cutoff, 200);

				// 4. Scan NTFS Operational Log
				ScanChannel(results, "Microsoft-Windows-NTFS/Operational", "*[System[(EventID=142 or EventID=3079)]]", cutoff, 100);

				// 5. Scan Security Log (Requires elevation)
				try
				{
					ScanChannel(results, "Security", "*[System[(EventID=1102 or EventID=1100)]]", cutoff, 100);
				}
				catch { }

				BeginInvoke(new Action(delegate {
					allHits = results;
					isScanning = false;

					int crit = allHits.Count(i => i.IsCritical);
					int susp = allHits.Count(i => i.IsSuspicious);

					if (crit > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ КРИТИЧЕСКИЕ СОБЫТИЯ (5.01/5.08): {0} шт. (Подозрительных: {1})", crit, susp);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else if (susp > 0)
					{
						lblStatusPill.Text = string.Format("⚠️ Найдено подозрительных записей: {0} шт.", susp);
						lblStatusPill.ForeColor = Ui.AccentOrange;
					}
					else
					{
						lblStatusPill.Text = string.Format("✔ Проанализировано {0} событий журнала. Следов очисток и инжекторов не обнаружено.", allHits.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
					}

					ApplyFilters();
				}));
			});
		}

		private void ScanChannel(List<EventLogHit> results, string channelName, string queryXPath, DateTime cutoff, int maxRecords)
		{
			try
			{
				EventLogQuery elq = new EventLogQuery(channelName, PathType.LogName, queryXPath);
				elq.ReverseDirection = true; // Newest first

				using (EventLogReader reader = new EventLogReader(elq))
				{
					int count = 0;
					EventRecord rec;
					while ((rec = reader.ReadEvent()) != null && count < maxRecords)
					{
						DateTime tc = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.MinValue;
						if (tc < cutoff) break;

						EventLogHit hit = ParseEventRecord(rec, channelName);
						if (hit != null)
						{
							results.Add(hit);
							count++;
						}
					}
				}
			}
			catch { }
		}

		private EventLogHit ParseEventRecord(EventRecord rec, string channel)
		{
			EventLogHit hit = new EventLogHit();
			hit.Channel = channel;
			hit.EventId = rec.Id;
			hit.TimeCreated = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.MinValue;
			hit.ProviderName = rec.ProviderName ?? "";

			string desc = "";
			try { desc = rec.FormatDescription() ?? ""; } catch { }
			hit.FullDescription = desc;

			try { hit.RawXml = rec.ToXml(); } catch { }

			string lowerDesc = desc.ToLowerInvariant();
			int id = rec.Id;

			// 1. Audit Log Cleared (Event 104, 1102, 1100) -> 5.08
			if (id == 104 || id == 1102 || id == 1100)
			{
				hit.IsCritical = true;
				hit.Verdict = "🚨 ОЧИСТКА ЖУРНАЛА (5.08)";
				hit.ForeColor = Ui.AccentRed;
				hit.ViolationRule = "Пункт 5.08 (Очистка следов / Удаление системного журнала)";
				hit.TargetOrSource = channel + " Log";
				hit.ShortSummary = "Системный журнал событий Windows был очищен пользователем!";
				hit.BanReason = string.Format("Очистка системного журнала событий ({0} EventID {1}) в {2:dd.MM.yyyy HH:mm:ss}", channel, id, hit.TimeCreated);
				hit.FullDescription = string.Format(
					"🚨 ФАКТ СОКРЫТИЯ УЛИК / ОЧИСТКИ ЖУРНАЛОВ!\r\n" +
					"• Журнал: {0}\r\n" +
					"• Event ID: {1}\r\n" +
					"• Время очистки: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
					"• Источник: {3}\r\n" +
					"• Вердикт: Очистка системного журнала событий Windows квалифицируется как умышленное сокрытие следов использования запрещенного ПО (Регламент 5.08 — Бан 30 дней).",
					channel, id, hit.TimeCreated, hit.ProviderName) + "\r\n\r\n" + desc;
				return hit;
			}

			// 2. USN Journal Deleted (3079, 3080, 142) -> 5.08
			if (id == 3079 || id == 3080 || id == 142 || lowerDesc.Contains("fsutil") || lowerDesc.Contains("usn journal"))
			{
				hit.IsCritical = true;
				hit.Verdict = "🚨 УДАЛЕНИЕ USN JOURNAL (5.08)";
				hit.ForeColor = Ui.AccentRed;
				hit.ViolationRule = "Пункт 5.08 (Удаление NTFS USN Journal через fsutil)";
				hit.TargetOrSource = "NTFS $UsnJrnl";
				hit.ShortSummary = "Удаление журнала файловых операций NTFS USN Journal перед проверкой!";
				hit.BanReason = string.Format("Удаление NTFS USN Journal в {0:dd.MM.yyyy HH:mm:ss}", hit.TimeCreated);
				hit.FullDescription = string.Format(
					"🚨 ФАКТ УДАЛЕНИЯ СИСТЕМНОГО ЖУРНАЛА ФАЙЛОВ NTFS!\r\n" +
					"• Время удаления: {0:dd.MM.yyyy HH:mm:ss}\r\n" +
					"• Источник: {1}\r\n" +
					"• Вердикт: Использование утилит очистки / fsutil usn deletejournal для уничтожения улик запуска читов (Регламент 5.08 — Бан 30 дней).",
					hit.TimeCreated, hit.ProviderName) + "\r\n\r\n" + desc;
				return hit;
			}

			// 3. New Service Installed (7045) -> Injector Driver / Rootkit Check
			if (id == 7045)
			{
				bool isCheatDriver = CheatKeywords.IsMatch(lowerDesc);
				string svcName = ExtractServiceField(desc, "Имя службы:", "Service Name:");
				string imagePath = ExtractServiceField(desc, "Имя файла службы:", "Service File Name:");

				hit.TargetOrSource = !string.IsNullOrEmpty(svcName) ? svcName : "Новая служба";

				if (isCheatDriver)
				{
					hit.IsCritical = true;
					hit.Verdict = "🚨 ЧИТ-ДРАЙВЕР / ИНЖЕКТОР (5.01)";
					hit.ForeColor = Ui.AccentRed;
					hit.ViolationRule = "Пункт 5.01 (Установка драйвера инжектора читов)";
					hit.ShortSummary = "Установлен вредоносный драйвер инжектора: " + svcName;
					hit.BanReason = string.Format("Установка драйвера чита {0} ({1:dd.MM.yyyy HH:mm})", svcName, hit.TimeCreated);
				}
				else
				{
					hit.Verdict = "ℹ️ УСТАНОВКА СЛУЖБЫ (7045)";
					hit.ForeColor = Ui.TextMuted;
					hit.ViolationRule = "Системное событие";
					hit.ShortSummary = "Установка службы: " + svcName;
				}
				return hit;
			}

			// 4. Service Start Type Changed (7040) / Service Stopped (7036) -> 5.08 Tampering
			if (id == 7040 || id == 7036)
			{
				bool isDps = lowerDesc.Contains("diagnostic policy service") || lowerDesc.Contains("dps");
				bool isSysMain = lowerDesc.Contains("sysmain") || lowerDesc.Contains("superfetch");
				bool isEventLog = lowerDesc.Contains("windows event log") || lowerDesc.Contains("eventlog");
				bool isPca = lowerDesc.Contains("pcasvc") || lowerDesc.Contains("program compatibility assistant");
				bool isBam = lowerDesc.Contains("bam") || lowerDesc.Contains("background activity moderator");
				bool isDefenderSvc = lowerDesc.Contains("windefend") || lowerDesc.Contains("securityhealthservice");

				string svcName = ExtractServiceField(desc, "Имя службы:", "Service Name:");
				hit.TargetOrSource = !string.IsNullOrEmpty(svcName) ? svcName : (isDps ? "DPS" : (isSysMain ? "SysMain" : (isEventLog ? "EventLog" : "Служба")));

				if (isDps || isSysMain || isEventLog || isPca || isBam || isDefenderSvc)
				{
					bool isDisabled = lowerDesc.Contains("отключена") || lowerDesc.Contains("disabled") || id == 7036;
					hit.IsCritical = true;
					hit.Verdict = "🚨 ОТКЛЮЧЕНИЕ СЛУЖБЫ СЛЕЖЕНИЯ (5.08)";
					hit.ForeColor = Ui.AccentRed;
					hit.ViolationRule = "Пункт 5.08 (Отключение служб DPS/SysMain/EventLog)";
					hit.ShortSummary = string.Format("Отключение критической службы {0} для сокрытия улик!", hit.TargetOrSource);
					hit.BanReason = string.Format("Отключение службы {0} в {1:dd.MM.yyyy HH:mm:ss}", hit.TargetOrSource, hit.TimeCreated);
					return hit;
				}

				hit.Verdict = "ℹ️ СОСТОЯНИЕ СЛУЖБЫ (" + id + ")";
				hit.ForeColor = Ui.TextMuted;
				hit.ViolationRule = "Служебное событие";
				hit.ShortSummary = "Изменение службы " + hit.TargetOrSource;
				return hit;
			}

			// 5. System Time Changed (Event ID 1) -> 5.08 Time Rewind
			if (id == 1 && (rec.ProviderName.Contains("Kernel-General") || rec.ProviderName.Contains("Time")))
			{
				hit.IsSuspicious = true;
				hit.Verdict = "⏱️ ИЗМЕНЕНИЕ ВРЕМЕНИ ПК (5.08)";
				hit.ForeColor = Ui.AccentOrange;
				hit.ViolationRule = "Пункт 5.08 (Скрутка системного времени ПК)";
				hit.TargetOrSource = "Windows Kernel-General";
				hit.ShortSummary = "Системное время Windows было изменено вручную или синхронизировано.";
				hit.BanReason = string.Format("Скрутка системного времени ПК в {0:dd.MM.yyyy HH:mm:ss}", hit.TimeCreated);
				return hit;
			}

			// 6. Application Crash (1000 / 1001) -> javaw crash or cheat crash
			if (id == 1000 || id == 1001)
			{
				bool isJavaw = lowerDesc.Contains("javaw.exe") || lowerDesc.Contains("java.exe");
				bool isCheatCrash = CheatKeywords.IsMatch(lowerDesc);

				string appName = ExtractServiceField(desc, "Имя сбойного приложения:", "Faulting application name:");
				hit.TargetOrSource = !string.IsNullOrEmpty(appName) ? appName : (isJavaw ? "javaw.exe" : "Приложение");

				if (isCheatCrash)
				{
					hit.IsCritical = true;
					hit.Verdict = "🚨 АВАРИЙНЫЙ СБОЙ ЧИТА (5.01)";
					hit.ForeColor = Ui.AccentRed;
					hit.ViolationRule = "Пункт 5.01 (Краш стороннего чит-клиента)";
					hit.ShortSummary = "Зафиксирован краш чита: " + hit.TargetOrSource;
					hit.BanReason = string.Format("Краш чита {0} в {1:dd.MM.yyyy HH:mm}", hit.TargetOrSource, hit.TimeCreated);
					return hit;
				}
				else if (isJavaw)
				{
					hit.IsSuspicious = true;
					hit.Verdict = "💥 СБОЙ MINECRAFT (javaw.exe)";
					hit.ForeColor = Ui.AccentOrange;
					hit.ViolationRule = "Пункт 5.01 (Аномальный сбой процесса игры)";
					hit.ShortSummary = "Аварийное завершение javaw.exe (возможный сбой инжектора/хука)";
					hit.BanReason = string.Format("Сбой javaw.exe в {0:dd.MM.yyyy HH:mm}", hit.TimeCreated);
					return hit;
				}

				hit.Verdict = "ℹ️ СБОЙ ПРИЛОЖЕНИЯ (1000)";
				hit.ForeColor = Ui.TextMuted;
				hit.ViolationRule = "Сбой стороннего ПО";
				hit.ShortSummary = "Сбой приложения: " + hit.TargetOrSource;
				return hit;
			}

			// 7. Windows Defender Detections (1116, 1117, 5001, 5007)
			if (id == 1116 || id == 1117)
			{
				bool isCheatThreat = CheatKeywords.IsMatch(lowerDesc) || lowerDesc.Contains("hacktool") || lowerDesc.Contains("injector");
				string threatName = ExtractServiceField(desc, "Имя:", "Name:");
				hit.TargetOrSource = !string.IsNullOrEmpty(threatName) ? threatName : "Угроза Защитника";

				if (isCheatThreat)
				{
					hit.IsCritical = true;
					hit.Verdict = "🚨 ДЕТЕКТ ЗАЩИТНИКА WINDOWS (5.01)";
					hit.ForeColor = Ui.AccentRed;
					hit.ViolationRule = "Пункт 5.01 (Обнаружение чита Защитником Windows)";
					hit.ShortSummary = "Защитник Windows обнаружил чит/инжектор: " + hit.TargetOrSource;
					hit.BanReason = string.Format("Обнаружение чита {0} Защитником Windows ({1:dd.MM.yyyy HH:mm})", hit.TargetOrSource, hit.TimeCreated);
				}
				else
				{
					hit.IsSuspicious = true;
					hit.Verdict = "⚠️ ДЕТЕКТ ЗАЩИТНИКА (1116)";
					hit.ForeColor = Ui.AccentOrange;
					hit.ViolationRule = "Обнаружение нежелательного софта";
					hit.ShortSummary = "Защитник обнаружил угрозу: " + hit.TargetOrSource;
				}
				return hit;
			}

			if (id == 5001)
			{
				hit.IsCritical = true;
				hit.Verdict = "🚨 ЗАЩИТНИК ОТКЛЮЧЕН (5.08)";
				hit.ForeColor = Ui.AccentRed;
				hit.ViolationRule = "Пункт 5.08 (Отключение антивируса для запуска софта)";
				hit.TargetOrSource = "Windows Defender";
				hit.ShortSummary = "Защита в реальном времени Защитника Windows была отключена!";
				hit.BanReason = string.Format("Отключение Защитника Windows в {0:dd.MM.yyyy HH:mm:ss}", hit.TimeCreated);
				return hit;
			}

			if (id == 5007)
			{
				hit.IsSuspicious = true;
				hit.Verdict = "⚠️ ИСКЛЮЧЕНИЯ ЗАЩИТНИКА (5.08)";
				hit.ForeColor = Ui.AccentOrange;
				hit.ViolationRule = "Пункт 5.08 (Добавление исключений для читов)";
				hit.TargetOrSource = "Конфигурация Defender";
				hit.ShortSummary = "Изменена конфигурация Защитника Windows (добавлены пути-исключения).";
				hit.BanReason = string.Format("Изменение исключений Защитника Windows в {0:dd.MM.yyyy HH:mm:ss}", hit.TimeCreated);
				return hit;
			}

			// 8. USB Storage Connections (20001, 20003)
			if (id == 20001 || id == 20003)
			{
				hit.Verdict = "🔌 ПОДКЛЮЧЕНИЕ USB-ФЛЕШКИ";
				hit.ForeColor = Ui.AccentTeal;
				hit.ViolationRule = "Аудит съемных носителей (5.01)";
				hit.TargetOrSource = "USB Storage Device";
				hit.ShortSummary = "Подключение или установка драйвера съемного USB-накопителя.";
				return hit;
			}

			hit.Verdict = "ℹ️ СОБЫТИЕ (" + id + ")";
			hit.ForeColor = Ui.TextMuted;
			hit.ViolationRule = "Общее событие";
			hit.TargetOrSource = hit.ProviderName;
			hit.ShortSummary = "Запись журнала " + channel;
			return hit;
		}

		private static string ExtractServiceField(string text, string prefixRu, string prefixEn)
		{
			if (string.IsNullOrEmpty(text)) return "";
			string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				string trimmed = line.Trim();
				if (trimmed.StartsWith(prefixRu, StringComparison.OrdinalIgnoreCase))
				{
					return trimmed.Substring(prefixRu.Length).Trim();
				}
				if (trimmed.StartsWith(prefixEn, StringComparison.OrdinalIgnoreCase))
				{
					return trimmed.Substring(prefixEn.Length).Trim();
				}
			}
			return "";
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilters()
		{
			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			List<EventLogHit> filtered = allHits.Where(item => {
				// Filter by mode
				switch (currentFilter)
				{
					case EventFilterMode.LogClearingOnly:
						if (item.EventId != 104 && item.EventId != 1102 && item.EventId != 1100 && item.EventId != 3079 && item.EventId != 142) return false;
						break;
					case EventFilterMode.ServiceInstalls:
						if (item.EventId != 7045) return false;
						break;
					case EventFilterMode.ServiceTampering:
						if (item.EventId != 7040 && item.EventId != 7036) return false;
						break;
					case EventFilterMode.AppCrashes:
						if (item.EventId != 1000 && item.EventId != 1001) return false;
						break;
					case EventFilterMode.DefenderDetections:
						if (item.EventId != 1116 && item.EventId != 1117 && item.EventId != 5001 && item.EventId != 5007) return false;
						break;
					case EventFilterMode.TimeRewinds:
						if (item.EventId != 1) return false;
						break;
					case EventFilterMode.UsbConnections:
						if (item.EventId != 20001 && item.EventId != 20003) return false;
						break;
				}

				// Search query
				if (!string.IsNullOrEmpty(query))
				{
					bool matchSummary = (item.ShortSummary ?? "").ToLowerInvariant().Contains(query);
					bool matchDesc = (item.FullDescription ?? "").ToLowerInvariant().Contains(query);
					bool matchTarget = (item.TargetOrSource ?? "").ToLowerInvariant().Contains(query);
					bool matchId = item.EventId.ToString().Contains(query);
					bool matchVerdict = (item.Verdict ?? "").ToLowerInvariant().Contains(query);
					if (!matchSummary && !matchDesc && !matchTarget && !matchId && !matchVerdict) return false;
				}

				return true;
			}).ToList();

			// Order: critical first, then newest
			filtered = filtered.OrderByDescending(i => i.IsCritical)
				.ThenByDescending(i => i.IsSuspicious)
				.ThenByDescending(i => i.TimeCreated)
				.ToList();

			listView.BeginUpdate();
			listView.Items.Clear();

			foreach (EventLogHit hit in filtered)
			{
				ListViewItem lvi = new ListViewItem(hit.Verdict);
				lvi.ForeColor = hit.ForeColor;
				lvi.SubItems.Add(string.Format("{0} / {1}", hit.Channel, hit.EventId));
				lvi.SubItems.Add(hit.TimeCreated.ToString("dd.MM.yyyy HH:mm:ss"));
				lvi.SubItems.Add(hit.TargetOrSource ?? "");
				lvi.SubItems.Add(hit.ShortSummary ?? "");
				lvi.SubItems.Add(hit.ProviderName ?? "");
				lvi.Tag = hit;
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

			EventLogHit hit = listView.SelectedItems[0].Tag as EventLogHit;
			if (hit == null) return;

			lblDetailVerdict.Text = hit.Verdict;
			lblDetailVerdict.ForeColor = hit.ForeColor;
			lblDetailRule.Text = "Регламент: " + (hit.ViolationRule ?? "Общий аудит");

			if (hit.IsCritical || hit.IsSuspicious)
			{
				string reason = !string.IsNullOrEmpty(hit.BanReason) ? hit.BanReason : hit.ShortSummary;
				string ruleCode = (hit.ViolationRule ?? "").Contains("5.08") ? "5.08" : "5.01";
				lblDetailBanCommand.Text = string.Format("Команда: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
			}
			else
			{
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = hit.FullDescription;
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Событие не выбрано";
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
				EventLogHit firstCrit = allHits.FirstOrDefault(i => i.IsCritical);
				if (firstCrit != null)
				{
					string ruleCode = (firstCrit.ViolationRule ?? "").Contains("5.08") ? "5.08" : "5.01";
					string ban = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, firstCrit.BanReason ?? firstCrit.ShortSummary);
					Clipboard.SetText(ban);
					MessageBox.Show("Скопирована команда бана за критическое нарушение в Event Log:\r\n\r\n" + ban, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				MessageBox.Show("Выберите подозрительное событие из списка для копирования команды бана.", "Event Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			EventLogHit sel = listView.SelectedItems[0].Tag as EventLogHit;
			if (sel != null)
			{
				string ruleCode = (sel.ViolationRule ?? "").Contains("5.08") ? "5.08" : "5.01";
				string reason = !string.IsNullOrEmpty(sel.BanReason) ? sel.BanReason : sel.ShortSummary;
				string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void ExportProtocolToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АУДИТА СИСТЕМНЫХ ЖУРНАЛОВ WINDOWS EVENT LOG ===");
			sb.AppendLine(string.Format("👤 Проверяемый игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("📅 Дата и время анализа: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Всего записей в анализе: {0} (Критических 5.08/5.01: {1}, Подозрительных: {2})",
				allHits.Count, allHits.Count(i => i.IsCritical), allHits.Count(i => i.IsSuspicious)));
			sb.AppendLine(new string('-', 85));

			foreach (EventLogHit hit in allHits.Where(i => i.IsCritical || i.IsSuspicious))
			{
				sb.AppendLine(string.Format("[{0}] {1} (ID: {2}) | {3:dd.MM.yyyy HH:mm:ss}", hit.Verdict, hit.Channel, hit.EventId, hit.TimeCreated));
				sb.AppendLine(string.Format("   Объект/Служба: {0}", hit.TargetOrSource));
				sb.AppendLine(string.Format("   Кратко: {0}", hit.ShortSummary));
				sb.AppendLine(string.Format("   Нарушение: {0}", hit.ViolationRule));
				if (!string.IsNullOrEmpty(hit.BanReason))
				{
					string ruleCode = (hit.ViolationRule ?? "").Contains("5.08") ? "5.08" : "5.01";
					sb.AppendLine(string.Format("   Команда бана: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, hit.BanReason));
				}
				sb.AppendLine();
			}

			if (allHits.Count(i => i.IsCritical || i.IsSuspicious) == 0)
			{
				sb.AppendLine("✔ Очисток журналов (1102/104), удалений USN Journal (3079) и чит-драйверов в Event Log не обнаружено.");
			}

			sb.AppendLine(new string('=', 85));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита Windows Event Log успешно скопирован в буфер обмена!", "Экспорт протокола", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
