using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class AntiForensicsViewerForm : Form
	{
		private enum TamperCategory
		{
			All,
			ViolationsOnly,
			Services,
			EventLogs,
			RegistryAndUsn,
			ByovdDrivers
		}

		private class TamperItem
		{
			public int Index;
			public string Component;
			public string CurrentStatus;
			public string ExpectedStatus;
			public bool IsViolation;
			public string Severity;
			public string Verdict;
			public string Details;
			public TamperCategory Category;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private TamperCategory currentCategory = TamperCategory.All;
		private List<TamperItem> allItems = new List<TamperItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailComponent;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterServices;
		private DarkButton btnFilterEventLogs;
		private DarkButton btnFilterRegistry;
		private DarkButton btnFilterDrivers;

		public AntiForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadAntiForensicsDataAsync();
		}

		private void InitializeComponent()
		{
			Text = "🛡️ Инспектор анти-криминалистики и сокрытия улик (Пункт 5.08)";
			Size = new Size(1180, 740);
			MinimumSize = new Size(920, 560);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 112;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🛡️  ИНСПЕКТОР АНТИ-КРИМИНАЛИСТИКИ И СОКРЫТИЯ УЛИК (Службы, Журналы, USN, Реестр 5.08)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Аудит обязательных служб (DPS, SysMain, EventLog), очистки журналов и реестра сокрытия...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование целостности...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(885, 8);
			btnRefresh.Size = new Size(135, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				LoadAntiForensicsDataAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnBan = new DarkButton();
			btnBan.Text = "🔨 Скопировать /ban 5.08";
			btnBan.Location = new Point(1026, 8);
			btnBan.Size = new Size(165, 26);
			btnBan.BackColor = Color.FromArgb(183, 28, 28);
			btnBan.ForeColor = Color.White;
			btnBan.Cursor = Cursors.Hand;
			btnBan.Click += delegate
			{
				CopyBanCommand();
			};
			topPanel.Controls.Add(btnBan);

			// Filter Row
			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1140, 50);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Всё (0)", TamperCategory.All, 90);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения 5.08 (0)", TamperCategory.ViolationsOnly, 150);
			btnFilterServices = CreateFilterButton("⚙️ Службы (0)", TamperCategory.Services, 110);
			btnFilterEventLogs = CreateFilterButton("📜 Журналы (0)", TamperCategory.EventLogs, 115);
			btnFilterRegistry = CreateFilterButton("🗝️ Реестр & USN (0)", TamperCategory.RegistryAndUsn, 135);
			btnFilterDrivers = CreateFilterButton("🛡️ Драйверы (0)", TamperCategory.ByovdDrivers, 120);

			int curX = 0;
			DarkButton[] filterBtns = { btnFilterAll, btnFilterViolations, btnFilterServices, btnFilterEventLogs, btnFilterRegistry, btnFilterDrivers };
			foreach (DarkButton b in filterBtns)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 4;
			}

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(curX + 6, 5);
			lblSearch.AutoSize = true;
			navRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(curX + 50, 2);
			searchBox.Size = new Size(160, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 214, 2);
			btnClear.Size = new Size(24, 22);
			btnClear.BackColor = Ui.SurfaceAlt;
			btnClear.ForeColor = Ui.TextMuted;
			btnClear.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			navRow.Controls.Add(btnClear);

			topPanel.Controls.Add(navRow);
			Controls.Add(topPanel);

			// SplitContainer
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Vertical;
			split.SplitterDistance = 680;
			split.SplitterWidth = 5;
			split.BackColor = Ui.Border;

			// Left ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontMono(8.5f);
			listView.Columns.Add("!", 35, HorizontalAlignment.Center);
			listView.Columns.Add("Компонент / Проверка", 210, HorizontalAlignment.Left);
			listView.Columns.Add("Текущее состояние", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Штатное состояние", 130, HorizontalAlignment.Left);
			listView.Columns.Add("Вердикт регламента", 220, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdateTamperDetails();
			};

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();

			ToolStripMenuItem copyRowItem = new ToolStripMenuItem("📋  Скопировать строку улик");
			copyRowItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					ListViewItem it = listView.SelectedItems[0];
					Clipboard.SetText(string.Format("{0} | {1} | {2} | {3}",
						it.SubItems[1].Text, it.SubItems[2].Text, it.SubItems[3].Text, it.SubItems[4].Text));
				}
			};

			ToolStripMenuItem copyAllReportItem = new ToolStripMenuItem("📋  Скопировать полный протокол целостности");
			copyAllReportItem.Click += delegate
			{
				CopyIntegrityReport();
			};

			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Сгенерировать команду /ban за сокрытие улик (5.08)");
			copyBanItem.Click += delegate
			{
				CopyBanCommand();
			};

			ctx.Items.Add(copyRowItem);
			ctx.Items.Add(copyAllReportItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(copyBanItem);
			listView.ContextMenuStrip = ctx;

			split.Panel1.Controls.Add(listView);

			// Right Details Panel
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;
			rightPanel.Padding = new Padding(12);

			Label lblDetHeader = new Label();
			lblDetHeader.Dock = DockStyle.Top;
			lblDetHeader.Height = 24;
			lblDetHeader.Font = Ui.FontTitle(9.5f, true);
			lblDetHeader.ForeColor = Ui.AccentTeal;
			lblDetHeader.Text = "🔍 Детальный анализ нарушения целостности";
			rightPanel.Controls.Add(lblDetHeader);

			lblDetailComponent = new Label();
			lblDetailComponent.Dock = DockStyle.Top;
			lblDetailComponent.Height = 22;
			lblDetailComponent.Font = Ui.FontBody(8.5f, true);
			lblDetailComponent.ForeColor = Ui.TextPrimary;
			lblDetailComponent.Text = "Компонент: —";
			rightPanel.Controls.Add(lblDetailComponent);

			lblDetailStatus = new Label();
			lblDetailStatus.Dock = DockStyle.Top;
			lblDetailStatus.Height = 22;
			lblDetailStatus.Font = Ui.FontBody(8.5f);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Text = "Состояние: —";
			rightPanel.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Dock = DockStyle.Top;
			lblDetailRule.Height = 40;
			lblDetailRule.Font = Ui.FontBody(8.5f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Text = "Регламент: —";
			rightPanel.Controls.Add(lblDetailRule);

			Label lblDescHeader = new Label();
			lblDescHeader.Dock = DockStyle.Top;
			lblDescHeader.Height = 22;
			lblDescHeader.Font = Ui.FontBody(8f, true);
			lblDescHeader.ForeColor = Ui.TextMuted;
			lblDescHeader.Text = "📄 Разъяснение регламента и улика:";
			rightPanel.Controls.Add(lblDescHeader);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Dock = DockStyle.Fill;
			txtDetailDescription.Font = Ui.FontMono(8.5f);
			txtDetailDescription.ReadOnly = true;
			txtDetailDescription.WordWrap = true;
			txtDetailDescription.ScrollBars = ScrollBars.Vertical;
			rightPanel.Controls.Add(txtDetailDescription);
			txtDetailDescription.BringToFront();

			split.Panel2.Controls.Add(rightPanel);
			Controls.Add(split);

			// Bottom Status Strip
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 36;
			bottomPanel.BackColor = Ui.SurfaceAlt;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			Label lblRuleHint = new Label();
			lblRuleHint.Text = "⚖️ Регламент проверок: Пункт 5.08 — Запрещено сокрытие улик, отключение служб Windows (DPS, SysMain, EventLog) и запуск клинеров.";
			lblRuleHint.Font = Ui.FontBody(8f);
			lblRuleHint.ForeColor = Ui.AccentGreen;
			lblRuleHint.Location = new Point(12, 10);
			lblRuleHint.AutoSize = true;
			bottomPanel.Controls.Add(lblRuleHint);

			DarkButton btnCopyReport = new DarkButton();
			btnCopyReport.Text = "📋 Скопировать протокол 5.08";
			btnCopyReport.Size = new Size(210, 24);
			btnCopyReport.Location = new Point(935, 6);
			btnCopyReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyReport.ForeColor = Color.White;
			btnCopyReport.Cursor = Cursors.Hand;
			btnCopyReport.Click += delegate
			{
				CopyIntegrityReport();
			};
			bottomPanel.Controls.Add(btnCopyReport);

			Controls.Add(bottomPanel);
		}

		private DarkButton CreateFilterButton(string text, TamperCategory cat, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 24);
			btn.Font = Ui.FontBody(8f);
			btn.Cursor = Cursors.Hand;
			btn.BackColor = (cat == currentCategory) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.ForeColor = (cat == currentCategory) ? Color.Black : Ui.TextPrimary;
			btn.Click += delegate
			{
				currentCategory = cat;
				UpdateFilterButtons();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateFilterButtons()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterViolations, btnFilterServices, btnFilterEventLogs, btnFilterRegistry, btnFilterDrivers };
			TamperCategory[] cats = { TamperCategory.All, TamperCategory.ViolationsOnly, TamperCategory.Services, TamperCategory.EventLogs, TamperCategory.RegistryAndUsn, TamperCategory.ByovdDrivers };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (cats[i] == currentCategory);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].ForeColor = active ? Color.Black : Ui.TextPrimary;
			}
		}

		private void LoadAntiForensicsDataAsync()
		{
			lblStatusPill.Text = "⏳ Сканирование служб и журналов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblHeaderStatus.Text = "Проверка состояния служб Windows, очистки логов, USN и уязвимых драйверов...";

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<TamperItem> items = new List<TamperItem>();

				AuditWindowsServices(items);
				AuditEventLogClearing(items);
				AuditRegistryAndUsn(items);
				AuditByovdDrivers(items);

				// Sort violations first
				items.Sort((a, b) =>
				{
					if (a.IsViolation != b.IsViolation) return b.IsViolation.CompareTo(a.IsViolation);
					return string.Compare(a.Component, b.Component, StringComparison.OrdinalIgnoreCase);
				});

				for (int i = 0; i < items.Count; i++)
				{
					items[i].Index = i + 1;
				}

				e.Result = items;
			};

			bw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				allItems = e.Result as List<TamperItem> ?? new List<TamperItem>();

				int total = allItems.Count;
				int viols = allItems.Count(i => i.IsViolation);
				int srvs = allItems.Count(i => i.Category == TamperCategory.Services);
				int logs = allItems.Count(i => i.Category == TamperCategory.EventLogs);
				int reg = allItems.Count(i => i.Category == TamperCategory.RegistryAndUsn);
				int drvs = allItems.Count(i => i.Category == TamperCategory.ByovdDrivers);

				btnFilterAll.Text = "🔍 Всё (" + total + ")";
				btnFilterViolations.Text = "🚨 Нарушения 5.08 (" + viols + ")";
				btnFilterServices.Text = "⚙️ Службы (" + srvs + ")";
				btnFilterEventLogs.Text = "📜 Журналы (" + logs + ")";
				btnFilterRegistry.Text = "🗝️ Реестр & USN (" + reg + ")";
				btnFilterDrivers.Text = "🛡️ Драйверы (" + drvs + ")";

				if (viols > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО СОКРЫТИЕ УЛИК ({0} нарушений)", viols);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ СИСТЕМА ЦЕЛОСТНА: Следов отключения служб нет";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}

				lblHeaderStatus.Text = string.Format("Проверка завершена: {0} проверок выполнено (нарушений регламента 5.08: {1})", total, viols);

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private static void AuditWindowsServices(List<TamperItem> list)
		{
			string[] criticalServices = {
				"DPS", "PcaSvc", "SysMain", "EventLog", "DiagTrack",
				"CDPSvc", "Dusmsvc", "bam", "AppInfo", "VSS", "Schedule"
			};

			Dictionary<string, string> serviceDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "DPS", "Служба политик диагностики (Diagnostic Policy Service). Отвечает за ведение SRUM, фиксацию запусков приложений и сетевого трафика. Отключение блокирует сбор доказательств." },
				{ "PcaSvc", "Служба помощника по совместимости программ (Program Compatibility Assistant). Фиксирует запуск исполняемых файлов в реестре PCA Store." },
				{ "SysMain", "Служба Superfetch / Prefetcher. Отвечает за генерацию .pf файлов в C:\\Windows\\Prefetch при каждом запуске софта." },
				{ "EventLog", "Журнал событий Windows. Фиксирует запуск процессов (4688), остановку служб (7036), скрутку времени (1)." },
				{ "DiagTrack", "Служба функциональных возможностей для подключенных пользователей и телеметрии." },
				{ "CDPSvc", "Служба платформы подключенных устройств (Connected Devices Platform)." },
				{ "Dusmsvc", "Служба использования данных сети (Data Usage)." },
				{ "bam", "Драйвер Background Activity Moderator (BAM), регистрирующий время запуска любого .exe." },
				{ "AppInfo", "Служба сведений о приложениях (UAC Elevation)." },
				{ "VSS", "Теневое копирование томов (Volume Shadow Copy). Позволяет восстановить удалённые читы." },
				{ "Schedule", "Планировщик заданий Windows (Task Scheduler)." }
			};

			foreach (string srvName in criticalServices)
			{
				string statusText = "Не установлена / Не найдена";
				int startType = -1;
				bool isRunning = false;
				bool isStopped = false;

				try
				{
					using (ServiceController sc = new ServiceController(srvName))
					{
						isRunning = (sc.Status == ServiceControllerStatus.Running);
						isStopped = (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending);
						statusText = isRunning ? "Работает (Running)" : (isStopped ? "Остановлена (Stopped)" : sc.Status.ToString());
					}
				}
				catch { }

				try
				{
					using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + srvName))
					{
						if (k != null)
						{
							object val = k.GetValue("Start");
							if (val != null) startType = Convert.ToInt32(val);
						}
					}
				}
				catch { }

				bool isCriticalService = (srvName == "DPS" || srvName == "EventLog" || srvName == "PcaSvc" || srvName == "SysMain");
				bool isViolation = (startType == 4) || (isStopped && isCriticalService);

				string verdict = isViolation ?
					"🚨 [5.08 - Сокрытие улик]: Служба отключена!" :
					(isRunning ? "✔ Штатная работа службы" : "ℹ️ Служба не активна");

				string desc = serviceDescriptions.ContainsKey(srvName) ? serviceDescriptions[srvName] : "Системная служба Windows.";
				if (startType == 4) desc += "\nТип запуска в реестре установлен в 4 (Отключено / Disabled)!";

				list.Add(new TamperItem
				{
					Component = "Служба " + srvName,
					CurrentStatus = statusText + (startType >= 0 ? " (Start=" + startType + ")" : ""),
					ExpectedStatus = "Работает (Автоматически / Start=2)",
					IsViolation = isViolation,
					Severity = isViolation ? "Критический (5.08)" : "Норма",
					Verdict = verdict,
					Details = desc,
					Category = TamperCategory.Services,
					ForeColor = isViolation ? Ui.AccentRed : Ui.TextPrimary
				});
			}
		}

		private static void AuditEventLogClearing(List<TamperItem> list)
		{
			try
			{
				// Check EventID 1102 (Security cleared) and 104 (System cleared)
				string[] logQueries = {
					"*[System[(EventID=1102)]]", // Security log cleared
					"*[System[(EventID=104)]]"   // System log cleared
				};

				string[] logNames = { "Security", "System" };

				for (int i = 0; i < logQueries.Length; i++)
				{
					try
					{
						EventLogQuery query = new EventLogQuery(logNames[i], PathType.LogName, logQueries[i]);
						query.ReverseDirection = true;
						using (EventLogReader reader = new EventLogReader(query))
						{
							EventRecord record = reader.ReadEvent();
							if (record != null)
							{
								DateTime time = record.TimeCreated ?? DateTime.MinValue;
								bool isRecent = (DateTime.Now - time).TotalHours < 72;

								list.Add(new TamperItem
								{
									Component = "Очистка журнала " + logNames[i] + " (EventID " + record.Id + ")",
									CurrentStatus = "Журнал был очищен! (" + time.ToString("dd.MM.yyyy HH:mm") + ")",
									ExpectedStatus = "Непрерывная история событий",
									IsViolation = isRecent,
									Severity = isRecent ? "Критический (5.08)" : "Предупреждение",
									Verdict = isRecent ? "🚨 [5.08 - Очистка логов]: Зафиксирован сброс журнала!" : "⚠️ Старая очистка журнала",
									Details = string.Format("Зафиксировано событие очистки журнала Windows {0} (EventID {1}). Время события: {2}.\nОчистка журнала событий перед или во время проверки является прямым нарушением правила 5.08.",
										logNames[i], record.Id, time.ToString("dd.MM.yyyy HH:mm:ss")),
									Category = TamperCategory.EventLogs,
									ForeColor = isRecent ? Ui.AccentRed : Ui.AccentOrange
								});
							}
							else
							{
								list.Add(new TamperItem
								{
									Component = "Целостность журнала " + logNames[i],
									CurrentStatus = "Очистки не зафиксировано",
									ExpectedStatus = "Непрерывная история событий",
									IsViolation = false,
									Severity = "Норма",
									Verdict = "✔ Журнал " + logNames[i] + " интактен",
									Details = "Событий принудительной очистки журнала (EventID 1102 / 104) не обнаружено.",
									Category = TamperCategory.EventLogs,
									ForeColor = Ui.TextPrimary
								});
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private static void AuditRegistryAndUsn(List<TamperItem> list)
		{
			try
			{
				// 1. Prefetch Registry
				using (RegistryKey pfKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"))
				{
					if (pfKey != null)
					{
						object val = pfKey.GetValue("EnablePrefetcher");
						int pfVal = (val != null) ? Convert.ToInt32(val) : 3;
						bool isPfDisabled = (pfVal == 0);

						list.Add(new TamperItem
						{
							Component = "Реестр: EnablePrefetcher",
							CurrentStatus = "EnablePrefetcher = " + pfVal,
							ExpectedStatus = "3 (Prefetch включен для приложений и загрузки)",
							IsViolation = isPfDisabled,
							Severity = isPfDisabled ? "Критический (5.08)" : "Норма",
							Verdict = isPfDisabled ? "🚨 [5.08 - Сокрытие улик]: Prefetch полностью отключен!" : "✔ Prefetch включен",
							Details = isPfDisabled ?
								"Параметр EnablePrefetcher установлен в 0 (Отключено). Это блокирует фиксацию запусков софта в C:\\Windows\\Prefetch. Квалифицируется как сокрытие улик (5.08)." :
								"Параметр EnablePrefetcher активен (значение " + pfVal + ").",
							Category = TamperCategory.RegistryAndUsn,
							ForeColor = isPfDisabled ? Ui.AccentRed : Ui.TextPrimary
						});
					}
				}

				// 2. ClearRecentDocsOnExit
				using (RegistryKey recKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
				{
					if (recKey != null)
					{
						object val = recKey.GetValue("ClearRecentDocsOnExit");
						int cVal = (val != null) ? Convert.ToInt32(val) : 0;
						bool isAutoClean = (cVal == 1);

						list.Add(new TamperItem
						{
							Component = "Реестр: ClearRecentDocsOnExit",
							CurrentStatus = "ClearRecentDocsOnExit = " + cVal,
							ExpectedStatus = "0 (История недавних документов сохраняется)",
							IsViolation = isAutoClean,
							Severity = isAutoClean ? "Критический (5.08)" : "Норма",
							Verdict = isAutoClean ? "🚨 [5.08 - Сокрытие улик]: Автоочистка недавних документов!" : "✔ Автоочистка отключена",
							Details = isAutoClean ?
								"Включена принудительная очистка списка недавних файлов при выходе из системы (ClearRecentDocsOnExit=1). Нарушение правила 5.08." :
								"История недавних файлов сохраняется штатно.",
							Category = TamperCategory.RegistryAndUsn,
							ForeColor = isAutoClean ? Ui.AccentRed : Ui.TextPrimary
						});
					}
				}

				// 3. USN Journal active check
				try
				{
					ProcessStartInfo psi = new ProcessStartInfo("fsutil", "usn queryjournal C:");
					psi.CreateNoWindow = true;
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = true;
					string usnOut = "";
					using (Process proc = Process.Start(psi))
					{
						usnOut = proc.StandardOutput.ReadToEnd();
						proc.WaitForExit(2000);
					}

					bool usnActive = usnOut.Contains("Usn") || usnOut.Contains("ID") || usnOut.Contains("Идентификатор");
					bool usnDeleted = usnOut.Contains("не существует") || usnOut.Contains("does not exist") || usnOut.Contains("удален") || usnOut.Contains("deleted");

					list.Add(new TamperItem
					{
						Component = "Журнал файловой системы NTFS ($UsnJrnl C:)",
						CurrentStatus = usnActive ? "Активен (Журнал ведётся)" : (usnDeleted ? "УДАЛЕН / ОТКЛЮЧЕН" : "Не определен"),
						ExpectedStatus = "Активен (Штатный NTFS журнал изменений)",
						IsViolation = usnDeleted,
						Severity = usnDeleted ? "Критический (5.08)" : "Норма",
						Verdict = usnDeleted ? "🚨 [5.08 - Удаление журнала USN]: Файловая история стёрта!" : "✔ Журнал USN активен",
						Details = usnDeleted ?
							"Журнал изменений файлов NTFS ($UsnJrnl) был принудительно удален командой fsutil usn deletejournal. Это признак умышленного сокрытия читов (5.08)." :
							"Журнал USN на системном диске C: активен и фиксирует изменения файлов.",
						Category = TamperCategory.RegistryAndUsn,
						ForeColor = usnDeleted ? Ui.AccentRed : Ui.TextPrimary
					});
				}
				catch { }
			}
			catch { }
		}

		private static void AuditByovdDrivers(List<TamperItem> list)
		{
			string[] vulnerableDrivers = {
				"gdrv.sys", "mhyprot2.sys", "procexp.sys", "kprocesshacker.sys",
				"dbk64.sys", "dbk32.sys", "RTCore64.sys", "physmem.sys", "echo.sys", "iqvw64e.sys"
			};

			string sysDrivers = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers");

			foreach (string dName in vulnerableDrivers)
			{
				string fullPath = Path.Combine(sysDrivers, dName);
				bool exists = File.Exists(fullPath);

				if (exists)
				{
					list.Add(new TamperItem
					{
						Component = "Уязвимый драйвер (BYOVD): " + dName,
						CurrentStatus = "ФАЙЛ ОБНАРУЖЕН: " + fullPath,
						ExpectedStatus = "Отсутствует в System32\\drivers",
						IsViolation = true,
						Severity = "Критический (5.01 / 5.08)",
						Verdict = "🚨 [5.01/5.08 - BYOVD Драйвер]: " + dName,
						Details = "Драйвер " + dName + " широко используется чит-инжекторами для обхода античитов и модификации памяти ядра (BYOVD - Bring Your Own Vulnerable Driver).",
						Category = TamperCategory.ByovdDrivers,
						ForeColor = Ui.AccentRed
					});
				}
			}
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = allItems.Where(it =>
			{
				switch (currentCategory)
				{
					case TamperCategory.ViolationsOnly:
						if (!it.IsViolation) return false;
						break;
					case TamperCategory.Services:
						if (it.Category != TamperCategory.Services) return false;
						break;
					case TamperCategory.EventLogs:
						if (it.Category != TamperCategory.EventLogs) return false;
						break;
					case TamperCategory.RegistryAndUsn:
						if (it.Category != TamperCategory.RegistryAndUsn) return false;
						break;
					case TamperCategory.ByovdDrivers:
						if (it.Category != TamperCategory.ByovdDrivers) return false;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = (it.Component != null && it.Component.ToLowerInvariant().Contains(query)) ||
						(it.CurrentStatus != null && it.CurrentStatus.ToLowerInvariant().Contains(query)) ||
						(it.ExpectedStatus != null && it.ExpectedStatus.ToLowerInvariant().Contains(query)) ||
						(it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(query)) ||
						(it.Details != null && it.Details.ToLowerInvariant().Contains(query));
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (TamperItem item in filtered)
			{
				string icon = item.IsViolation ? "🚨" : "✔";
				ListViewItem lvi = new ListViewItem(icon);
				lvi.SubItems.Add(item.Component);
				lvi.SubItems.Add(item.CurrentStatus);
				lvi.SubItems.Add(item.ExpectedStatus);
				lvi.SubItems.Add(item.Verdict);
				lvi.Tag = item;
				lvi.ForeColor = item.ForeColor;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearTamperDetails();
			}
		}

		private void UpdateTamperDetails()
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearTamperDetails();
				return;
			}

			TamperItem item = listView.SelectedItems[0].Tag as TamperItem;
			if (item == null) return;

			lblDetailComponent.Text = item.Component;
			lblDetailComponent.ForeColor = item.IsViolation ? Ui.AccentRed : Ui.AccentTeal;

			lblDetailStatus.Text = "Состояние: " + item.CurrentStatus + "  |  Штатно: " + item.ExpectedStatus;

			if (item.IsViolation)
			{
				lblDetailRule.Text = "⚖️ Нарушение: Пункт 5.08 (Сокрытие улик, отключение служб или очистка журналов)\nРекомендуемое наказание: Блокировка от 30 дней.";
				lblDetailRule.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblDetailRule.Text = "✔ Нарушений не обнаружено: Компонент функционирует штатно.";
				lblDetailRule.ForeColor = Ui.AccentGreen;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АНАЛИЗ ЦЕЛОСТНОСТИ И РЕГЛАМЕНТА ===");
			sb.AppendLine("Компонент: " + item.Component);
			sb.AppendLine("Текущее состояние: " + item.CurrentStatus);
			sb.AppendLine("Штатное состояние: " + item.ExpectedStatus);
			sb.AppendLine("Вердикт: " + item.Verdict);
			sb.AppendLine("Уровень угрозы: " + item.Severity);
			sb.AppendLine();
			sb.AppendLine("--- ДЕТАЛЬНОЕ РАЗЪЯСНЕНИЕ ---");
			sb.AppendLine(item.Details);

			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearTamperDetails()
		{
			lblDetailComponent.Text = "Компонент: —";
			lblDetailStatus.Text = "Состояние: —";
			lblDetailRule.Text = "Регламент: —";
			txtDetailDescription.Text = "Выберите запись в списке слева для подробного анализа.";
		}

		private void CopyBanCommand()
		{
			var viols = allItems.Where(i => i.IsViolation).ToList();
			string reason = "Сокрытие улик";
			if (viols.Count > 0)
			{
				reason = "Сокрытие улик: " + viols[0].Component;
			}

			string cmd = string.Format("/ban {0} 30d 5.08 ({1})", targetPlayerName, reason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена!\n\n" + cmd, "Генератор наказания", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyIntegrityReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("       ПРОТОКОЛ АУДИТА АНТИ-КРИМИНАЛИСТИКИ И СОКРЫТИЯ УЛИК (SCREENSHARE 5.08)   ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Дата и время: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего проверок: " + allItems.Count);
			sb.AppendLine("Нарушений регламента 5.08: " + allItems.Count(i => i.IsViolation));
			sb.AppendLine();

			sb.AppendLine("--- ОБНАРУЖЕННЫЕ ФАКТЫ СОКРЫТИЯ УЛИК (5.08) ---");
			var violList = allItems.Where(i => i.IsViolation).ToList();
			if (violList.Count == 0)
			{
				sb.AppendLine("Фактов отключения обязательных служб, очистки журналов и удаления USN НЕ обнаружено.");
			}
			else
			{
				foreach (var it in violList)
				{
					sb.AppendLine(string.Format("[{0}] {1} -> {2}", it.Verdict, it.Component, it.CurrentStatus));
					sb.AppendLine("   Детали: " + it.Details);
					sb.AppendLine();
				}
			}

			sb.AppendLine("--- ВСЕ ПРОВЕРЕННЫЕ СИСТЕМНЫЕ КОМПОНЕНТЫ ---");
			foreach (var it in allItems.Where(i => !i.IsViolation))
			{
				sb.AppendLine(string.Format("{0,-35} | {1,-30} | {2}", it.Component, it.CurrentStatus, it.Verdict));
			}

			sb.AppendLine();
			sb.AppendLine("Регламент: Отключение служб (DPS, SysMain, EventLog) и очистка следов наказывается по п. 5.08 (30 дней).");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита анти-криминалистики успешно скопирован в буфер обмена!", "Аудит 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
