using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class CrashDumpsAndWerInspectorForm : Form
	{
		#region Models

		public enum CrashFilterMode
		{
			All,
			CheatsOnly,
			DeletedOnly,
			MinecraftCrashes,
			CleanersOnly,
			Whitelist
		}

		public class CrashEntry
		{
			public string SourceType; // "CrashDump (.dmp)" or "WER Report (Report.wer)"
			public string AppName;
			public string AppPath;
			public string FaultModule;
			public string ExceptionCode;
			public long FileSize;
			public DateTime CrashTime;
			public string ContainerPath;
			public bool ExistsOnDisk;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsMinecraft;
			public bool IsWhitelisted;
			public string Verdict;
			public Color ForeColor;
			public string ViolationRule;
			public string BanReason;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private CrashFilterMode currentFilter = CrashFilterMode.All;
		private List<CrashEntry> allEntries = new List<CrashEntry>();
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
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterMinecraft;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterWhitelist;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenFolder;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatPattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray)",
			RegexOptions.Compiled);

		private static readonly Regex CleanerPattern = new Regex(
			@"(?i)(cleaner|wiper|reghax|bleachbit|ccleaner|fsutil|usncleaner|journalcleaner|clearlogs|clean_history|wipetraces|vanish)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public CrashDumpsAndWerInspectorForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			Text = "💥 Инспектор аварийных дампов CrashDumps и отчетов об ошибках Windows Error Reporting (WER)";
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
			lblHeaderTitle.Text = "💥 Инспектор аварийных дампов CrashDumps и отчетов Windows Error Reporting (WER)";
			lblHeaderTitle.Font = Ui.FontTitle(11.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(12, 8);
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Криминалистический анализ сбоев чит-клиентов, крашей javaw.exe при инжекциях и отчетов Report.wer об удаленных программах.";
			lblHeaderSubtitle.Font = Ui.FontBody(8f);
			lblHeaderSubtitle.ForeColor = Ui.TextMuted;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(13, 30);
			topPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования дампов памяти и отчетов WER...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(13, 51);
			topPanel.Controls.Add(lblStatusPill);

			// Filter row
			FlowLayoutPanel filterRow = new FlowLayoutPanel();
			filterRow.Location = new Point(12, 75);
			filterRow.Size = new Size(1200, 32);
			filterRow.WrapContents = false;
			filterRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("Все сбои", CrashFilterMode.All);
			btnFilterCheats = CreateFilterButton("🚨 Сбои читов (5.01)", CrashFilterMode.CheatsOnly);
			btnFilterDeleted = CreateFilterButton("🚨 Удалены с диска (5.08)", CrashFilterMode.DeletedOnly);
			btnFilterMinecraft = CreateFilterButton("💥 Сбои Minecraft (javaw)", CrashFilterMode.MinecraftCrashes);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры / Вайперы (5.08)", CrashFilterMode.CleanersOnly);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", CrashFilterMode.Whitelist);

			filterRow.Controls.Add(btnFilterAll);
			filterRow.Controls.Add(btnFilterCheats);
			filterRow.Controls.Add(btnFilterDeleted);
			filterRow.Controls.Add(btnFilterMinecraft);
			filterRow.Controls.Add(btnFilterCleaners);
			filterRow.Controls.Add(btnFilterWhitelist);
			topPanel.Controls.Add(filterRow);

			// Search and actions row
			Panel actionRow = new Panel();
			actionRow.Location = new Point(12, 110);
			actionRow.Size = new Size(1200, 30);
			actionRow.BackColor = Color.Transparent;

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(0, 2);
			searchBox.Size = new Size(320, 24);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			actionRow.Controls.Add(searchBox);

			Label lblSearchHint = new Label();
			lblSearchHint.Text = "🔍 Поиск по имени, пути, модулю...";
			lblSearchHint.Location = new Point(328, 6);
			lblSearchHint.AutoSize = true;
			lblSearchHint.ForeColor = Ui.TextMuted;
			lblSearchHint.Font = Ui.FontBody(8f);
			actionRow.Controls.Add(lblSearchHint);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 Обновить";
			btnRefreshNow.Location = new Point(570, 2);
			btnRefreshNow.Size = new Size(100, 25);
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			actionRow.Controls.Add(btnRefreshNow);

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📂 Открыть папку с дампом";
			btnOpenFolder.Location = new Point(678, 2);
			btnOpenFolder.Size = new Size(175, 25);
			btnOpenFolder.Click += delegate { OpenSelectedFolder(); };
			actionRow.Controls.Add(btnOpenFolder);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Location = new Point(860, 2);
			btnCopyBan.Size = new Size(195, 25);
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			actionRow.Controls.Add(btnCopyBan);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📑 Экспорт протокола";
			btnExportReport.Location = new Point(1062, 2);
			btnExportReport.Size = new Size(135, 25);
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
			listView.Columns.Add("Сбойный процесс / Чит", 220);
			listView.Columns.Add("Дата и время сбоя", 150);
			listView.Columns.Add("Статус на диске", 170);
			listView.Columns.Add("Модуль сбоя / Исключение", 180);
			listView.Columns.Add("Исходный путь к файлу", 300);

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
			lblDetailVerdict.Text = "Выберите запись из таблицы выше для криминалистического анализа аварийного дампа.";
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
			lblHelp.Text = "💡 Подсказка: Даже если игрок удалил чит перед проверкой, отчеты Report.wer и дампы .dmp сохраняют точный путь, имя файла и время его запуска.";
			lblHelp.Location = new Point(12, 7);
			lblHelp.AutoSize = true;
			lblHelp.ForeColor = Ui.TextMuted;
			lblHelp.Font = Ui.FontBody(8f);
			bottomBar.Controls.Add(lblHelp);

			Controls.Add(bottomBar);

			UpdateFilterButtonsStyle();
		}

		private DarkButton CreateFilterButton(string text, CrashFilterMode mode)
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
			btnFilterAll.BackColor = (currentFilter == CrashFilterMode.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterCheats.BackColor = (currentFilter == CrashFilterMode.CheatsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterDeleted.BackColor = (currentFilter == CrashFilterMode.DeletedOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterMinecraft.BackColor = (currentFilter == CrashFilterMode.MinecraftCrashes) ? Ui.AccentOrange : Ui.Surface;
			btnFilterCleaners.BackColor = (currentFilter == CrashFilterMode.CleanersOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterWhitelist.BackColor = (currentFilter == CrashFilterMode.Whitelist) ? Ui.AccentGreen : Ui.Surface;
		}

		#endregion

		#region Scanning Logic

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "⏳ Сканирование дампов CrashDumps и архивов WER...";
			lblStatusPill.ForeColor = Ui.AccentTeal;

			ThreadPool.QueueUserWorkItem(delegate {
				List<CrashEntry> results = new List<CrashEntry>();

				try
				{
					// 1. Scan Local CrashDumps (*.dmp)
					string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					string crashDumpsDir = Path.Combine(localApp, "CrashDumps");
					if (Directory.Exists(crashDumpsDir))
					{
						ScanMinidumps(results, crashDumpsDir);
					}

					// 2. Scan WER ReportArchive (ProgramData & LocalAppData)
					string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
					string progDataWer = Path.Combine(progData, @"Microsoft\Windows\WER\ReportArchive");
					if (Directory.Exists(progDataWer)) ScanWerDirectory(results, progDataWer);

					string localWer = Path.Combine(localApp, @"Microsoft\Windows\WER\ReportArchive");
					if (Directory.Exists(localWer)) ScanWerDirectory(results, localWer);

					// 3. Scan WER ReportQueue
					string progDataQueue = Path.Combine(progData, @"Microsoft\Windows\WER\ReportQueue");
					if (Directory.Exists(progDataQueue)) ScanWerDirectory(results, progDataQueue);

					string localQueue = Path.Combine(localApp, @"Microsoft\Windows\WER\ReportQueue");
					if (Directory.Exists(localQueue)) ScanWerDirectory(results, localQueue);
				}
				catch { }

				BeginInvoke(new Action(delegate {
					allEntries = results;
					isScanning = false;

					int crit = allEntries.Count(i => i.IsCheat || i.IsCleaner);
					int deleted = allEntries.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner));

					if (deleted > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ СБОИ УДАЛЕННЫХ ЧИТОВ: {0} шт. (Всего сбоев читов: {1})", deleted, crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else if (crit > 0)
					{
						lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ ДАМПЫ СБОЕВ ЧИТОВ: {0} шт.", crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else
					{
						lblStatusPill.Text = string.Format("✔ Проанализировано {0} отчетов и дампов. Сбоев чит-клиентов не обнаружено.", allEntries.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
					}

					ApplyFilters();
				}));
			});
		}

		private void ScanMinidumps(List<CrashEntry> results, string dir)
		{
			try
			{
				string[] files = Directory.GetFiles(dir, "*.dmp", SearchOption.TopDirectoryOnly);
				foreach (string file in files)
				{
					try
					{
						FileInfo fi = new FileInfo(file);
						string fn = fi.Name;

						// Standard format: ProcessName.exe.PID.dmp
						string appName = fn;
						int firstDot = fn.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
						if (firstDot > 0) appName = fn.Substring(0, firstDot + 4);

						CrashEntry entry = new CrashEntry();
						entry.SourceType = "CrashDump (.dmp)";
						entry.AppName = appName;
						entry.AppPath = ""; // Path unknown directly from dump name without symbol resolution
						entry.ContainerPath = file;
						entry.FileSize = fi.Length;
						entry.CrashTime = fi.LastWriteTime;
						entry.FaultModule = "Minidump";
						entry.ExceptionCode = "CrashDump";

						CategorizeCrashEntry(entry);
						results.Add(entry);
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanWerDirectory(List<CrashEntry> results, string dir)
		{
			try
			{
				string[] subDirs = Directory.GetDirectories(dir, "AppCrash_*");
				foreach (string sub in subDirs)
				{
					try
					{
						string werFile = Path.Combine(sub, "Report.wer");
						if (!File.Exists(werFile)) continue;

						FileInfo fi = new FileInfo(werFile);
						if (fi.Length > 2097152) continue; // Skip files > 2 MB

						string appName = "";
						string appPath = "";
						string faultModule = "";
						string exceptionCode = "";
						DateTime crashTime = fi.LastWriteTime;

						string[] lines = File.ReadAllLines(werFile);
						foreach (string line in lines)
						{
							if (line.StartsWith("NsAppName=", StringComparison.OrdinalIgnoreCase))
								appName = line.Substring(10).Trim();
							else if (line.StartsWith("AppPath=", StringComparison.OrdinalIgnoreCase))
								appPath = line.Substring(8).Trim();
							else if (line.StartsWith("EventTime=", StringComparison.OrdinalIgnoreCase))
							{
								long ft;
								if (long.TryParse(line.Substring(10).Trim(), out ft) && ft > 0)
								{
									try { crashTime = DateTime.FromFileTimeUtc(ft).ToLocalTime(); } catch { }
								}
							}
							else if (line.StartsWith("Sig[2].Value=", StringComparison.OrdinalIgnoreCase))
								faultModule = line.Substring(13).Trim();
							else if (line.StartsWith("Sig[6].Value=", StringComparison.OrdinalIgnoreCase))
								exceptionCode = line.Substring(13).Trim();
						}

						if (string.IsNullOrEmpty(appName))
						{
							string dirName = Path.GetFileName(sub);
							int idx = dirName.IndexOf("AppCrash_", StringComparison.OrdinalIgnoreCase);
							if (idx >= 0 && dirName.Length > 9)
							{
								int nextUnderscore = dirName.IndexOf('_', 9);
								appName = (nextUnderscore > 9) ? dirName.Substring(9, nextUnderscore - 9) : dirName.Substring(9);
							}
						}

						CrashEntry entry = new CrashEntry();
						entry.SourceType = "WER Report (Report.wer)";
						entry.AppName = appName;
						entry.AppPath = appPath;
						entry.FaultModule = faultModule;
						entry.ExceptionCode = exceptionCode;
						entry.FileSize = fi.Length;
						entry.CrashTime = crashTime;
						entry.ContainerPath = werFile;

						CategorizeCrashEntry(entry);
						results.Add(entry);
					}
					catch { }
				}
			}
			catch { }
		}

		private void CategorizeCrashEntry(CrashEntry entry)
		{
			string lowerName = (entry.AppName ?? "").ToLowerInvariant();
			string lowerPath = (entry.AppPath ?? "").ToLowerInvariant();
			string lowerModule = (entry.FaultModule ?? "").ToLowerInvariant();

			// Check disk presence if path is known
			if (!string.IsNullOrEmpty(entry.AppPath))
			{
				entry.ExistsOnDisk = File.Exists(entry.AppPath);
			}
			else
			{
				entry.ExistsOnDisk = true; // Inconclusive if path is missing
			}

			// Whitelist Rules Check
			bool isAutoclicker = lowerName.Contains("opautoclicker") || lowerName.Contains("speedautoclicker") ||
				lowerName.Contains("tinytask") || lowerName.Contains("x-mouse") || lowerName.Contains("autohotkey") ||
				lowerName.Contains("clickermann") || lowerName.Contains("keyran") || lowerName.Contains("auto_clicker");

			bool isScreenShare = lowerName.Contains("anydesk") || lowerName.Contains("rudesktop") || lowerName.Contains("discord");

			if (isAutoclicker)
			{
				entry.IsWhitelisted = true;
				entry.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: Автокликер)";
				entry.ForeColor = Ui.AccentGreen;
				entry.ViolationRule = "Легитимный кликер (Разрешено)";
				entry.Details = string.Format("Сбой разрешенного автокликера ({0}).\r\nВремя: {1:dd.MM.yyyy HH:mm:ss}\r\nОтчет: {2}", entry.AppName, entry.CrashTime, entry.ContainerPath);
				return;
			}

			if (isScreenShare)
			{
				entry.IsWhitelisted = true;
				entry.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: ScreenShare)";
				entry.ForeColor = Ui.AccentGreen;
				entry.ViolationRule = "ПО для демонстрации экрана";
				entry.Details = string.Format("Сбой программы ScreenShare ({0}).\r\nВремя: {1:dd.MM.yyyy HH:mm:ss}\r\nОтчет: {2}", entry.AppName, entry.CrashTime, entry.ContainerPath);
				return;
			}

			// Cleaners / Wipers
			if (CleanerPattern.IsMatch(lowerName) || CleanerPattern.IsMatch(lowerPath))
			{
				entry.IsCleaner = true;
				entry.Verdict = "🚨 СБОЙ КЛИЕНЕРА / ВАЙПЕРА (5.08)";
				entry.ForeColor = Ui.AccentRed;
				entry.ViolationRule = "Пункт 5.08 (Утилита очистки следов и журналов)";
				entry.BanReason = string.Format("Сбой утилиты очистки {0} ({1:dd.MM.yyyy HH:mm})", entry.AppName, entry.CrashTime);
				entry.Details = string.Format(
					"🚨 ОБНАРУЖЕН АВАРИЙНЫЙ ДАМП УТИЛИТЫ ОЧИСТКИ СЛЕДОВ!\r\n" +
					"• Программа: {0}\r\n" +
					"• Путь: {1}\r\n" +
					"• Время сбоя: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
					"• Модуль: {3}\r\n" +
					"• Вердикт: Использование ПО для сокрытия улик (Регламент 5.08 — Бан 30 дней).",
					entry.AppName, entry.AppPath, entry.CrashTime, entry.FaultModule);
				return;
			}

			// Cheats Check
			bool isMatchCheat = CheatPattern.IsMatch(lowerName) || CheatPattern.IsMatch(lowerPath) || CheatPattern.IsMatch(lowerModule);

			if (isMatchCheat)
			{
				entry.IsCheat = true;

				if (!string.IsNullOrEmpty(entry.AppPath) && !entry.ExistsOnDisk)
				{
					entry.Verdict = "🚨 ЧИТ УДАЛЕН ПОСЛЕ СБОЯ (5.08)";
					entry.ForeColor = Ui.AccentRed;
					entry.ViolationRule = "Пункт 5.08 / 5.01 (Удаление чита после аварийного сбоя)";
					entry.BanReason = string.Format("Удаление чита {0} после сбоя ({1:dd.MM.yyyy HH:mm})", entry.AppName, entry.CrashTime);
					entry.Details = string.Format(
						"🚨 УЛИКА СОКРЫТИЯ ЧИТА: ЧИТ-КЛИЕНТ УДАЛЕН ПОСЛЕ АВАРИЙНОГО КРАША!\r\n" +
						"• Чит: {0}\r\n" +
						"• Исходный путь: {1}\r\n" +
						"• Статус на диске: УДАЛЕН (Файл отсутствует по исходному пути!).\r\n" +
						"• Время сбоя: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Модуль сбоя: {3}\r\n" +
						"• Код исключения: {4}\r\n" +
						"• Отчет WER: {5}\r\n" +
						"• Вердикт: Игрок запускал чит, произошел сбой, после чего файл был удален перед проверкой (Регламент 5.08 — Бан 30 дней).",
						entry.AppName, entry.AppPath, entry.CrashTime, entry.FaultModule, entry.ExceptionCode, entry.ContainerPath);
				}
				else
				{
					entry.Verdict = "🚨 АВАРИЙНЫЙ СБОЙ ЧИТА (5.01)";
					entry.ForeColor = Ui.AccentRed;
					entry.ViolationRule = "Пункт 5.01 (Запуск чит-клиента)";
					entry.BanReason = string.Format("Краш чита {0} в {1:dd.MM.yyyy HH:mm}", entry.AppName, entry.CrashTime);
					entry.Details = string.Format(
						"🚨 ЗАФИКСИРОВАН АВАРИЙНЫЙ ДАМП ЧИТ-КЛИЕНТА!\r\n" +
						"• Чит: {0}\r\n" +
						"• Путь: {1}\r\n" +
						"• Время сбоя: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Модуль: {3}\r\n" +
						"• Код исключения: {4}\r\n" +
						"• Вердикт: Использование читов по Регламенту 5.01.",
						entry.AppName, entry.AppPath, entry.CrashTime, entry.FaultModule, entry.ExceptionCode);
				}
				return;
			}

			// Minecraft crashes
			if (lowerName.Contains("javaw") || lowerName.Contains("java") || lowerName.Contains("minecraft"))
			{
				entry.IsMinecraft = true;
				bool isJvmCrash = lowerModule.Contains("jvm.dll") || lowerModule.Contains("ntdll.dll") || entry.ExceptionCode.Contains("c0000005");

				entry.Verdict = isJvmCrash ? "💥 СБОЙ MINECRAFT (Хук/Инжекция)" : "💥 СБОЙ MINECRAFT (javaw.exe)";
				entry.ForeColor = isJvmCrash ? Ui.AccentOrange : Ui.AccentTeal;
				entry.ViolationRule = "Пункт 5.01 (Подозрительный краш процесса игры)";
				entry.BanReason = string.Format("Краш javaw.exe в {0:dd.MM.yyyy HH:mm}", entry.CrashTime);
				entry.Details = string.Format(
					"💥 АВАРИЙНОЕ ЗАВЕРШЕНИЕ ПРОЦЕССА MINECRAFT (javaw.exe)!\r\n" +
					"• Время сбоя: {0:dd.MM.yyyy HH:mm:ss}\r\n" +
					"• Модуль сбоя: {1}\r\n" +
					"• Код исключения: {2}\r\n" +
					"• Путь к javaw: {3}\r\n" +
					"• Примечание: Сбои в модулях jvm.dll / ntdll.dll с кодом 0xc0000005 часто вызваны некорректным хуком или инжекцией стороннего чита.",
					entry.CrashTime, entry.FaultModule, entry.ExceptionCode, entry.AppPath);
				return;
			}

			entry.Verdict = "ℹ️ СБОЙ СТОРОННЕГО ПО";
			entry.ForeColor = Ui.TextMuted;
			entry.ViolationRule = "Общий сбой ПО";
			entry.Details = string.Format("Сбой программы {0}.\r\nВремя: {1:dd.MM.yyyy HH:mm:ss}\r\nОтчет: {2}", entry.AppName, entry.CrashTime, entry.ContainerPath);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilters()
		{
			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			List<CrashEntry> filtered = allEntries.Where(item => {
				// Mode filter
				switch (currentFilter)
				{
					case CrashFilterMode.CheatsOnly:
						if (!item.IsCheat) return false;
						break;
					case CrashFilterMode.DeletedOnly:
						if (item.ExistsOnDisk || (!item.IsCheat && !item.IsCleaner)) return false;
						break;
					case CrashFilterMode.MinecraftCrashes:
						if (!item.IsMinecraft) return false;
						break;
					case CrashFilterMode.CleanersOnly:
						if (!item.IsCleaner) return false;
						break;
					case CrashFilterMode.Whitelist:
						if (!item.IsWhitelisted) return false;
						break;
				}

				// Search query
				if (!string.IsNullOrEmpty(query))
				{
					bool matchName = (item.AppName ?? "").ToLowerInvariant().Contains(query);
					bool matchPath = (item.AppPath ?? "").ToLowerInvariant().Contains(query);
					bool matchModule = (item.FaultModule ?? "").ToLowerInvariant().Contains(query);
					bool matchVerdict = (item.Verdict ?? "").ToLowerInvariant().Contains(query);
					bool matchDetails = (item.Details ?? "").ToLowerInvariant().Contains(query);
					if (!matchName && !matchPath && !matchModule && !matchVerdict && !matchDetails) return false;
				}

				return true;
			}).ToList();

			// Order: Cheats / Cleaners first, then deleted, then newest
			filtered = filtered.OrderByDescending(i => i.IsCheat || i.IsCleaner)
				.ThenByDescending(i => !i.ExistsOnDisk)
				.ThenByDescending(i => i.IsMinecraft)
				.ThenByDescending(i => i.CrashTime)
				.ToList();

			listView.BeginUpdate();
			listView.Items.Clear();

			foreach (CrashEntry entry in filtered)
			{
				ListViewItem lvi = new ListViewItem(entry.Verdict);
				lvi.ForeColor = entry.ForeColor;
				lvi.SubItems.Add(entry.AppName ?? "");
				lvi.SubItems.Add((entry.CrashTime > DateTime.MinValue) ? entry.CrashTime.ToString("dd.MM.yyyy HH:mm:ss") : "-");

				string diskStatus = "[Существует]";
				if (!string.IsNullOrEmpty(entry.AppPath))
				{
					diskStatus = entry.ExistsOnDisk ? "[Существует на диске]" : "[🚨 УДАЛЕН С ДИСКА]";
				}
				lvi.SubItems.Add(diskStatus);

				lvi.SubItems.Add(string.Format("{0} ({1})", entry.FaultModule ?? "Unknown", entry.ExceptionCode ?? "-"));
				lvi.SubItems.Add(!string.IsNullOrEmpty(entry.AppPath) ? entry.AppPath : entry.ContainerPath);
				lvi.Tag = entry;
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

			CrashEntry entry = listView.SelectedItems[0].Tag as CrashEntry;
			if (entry == null) return;

			lblDetailVerdict.Text = entry.Verdict;
			lblDetailVerdict.ForeColor = entry.ForeColor;
			lblDetailRule.Text = "Регламент: " + (entry.ViolationRule ?? "Общий аудит");

			if (entry.IsCheat || entry.IsCleaner)
			{
				string reason = !string.IsNullOrEmpty(entry.BanReason) ? entry.BanReason : entry.AppName;
				string ruleCode = entry.IsCleaner || !entry.ExistsOnDisk ? "5.08" : "5.01";
				lblDetailBanCommand.Text = string.Format("Команда: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
			}
			else
			{
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = entry.Details;
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Сбой не выбран";
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
				CrashEntry firstCrit = allEntries.FirstOrDefault(i => i.IsCheat || i.IsCleaner);
				if (firstCrit != null)
				{
					string ruleCode = firstCrit.IsCleaner || !firstCrit.ExistsOnDisk ? "5.08" : "5.01";
					string ban = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, firstCrit.BanReason ?? firstCrit.AppName);
					Clipboard.SetText(ban);
					MessageBox.Show("Скопирована команда бана за сбой чита:\r\n\r\n" + ban, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				MessageBox.Show("Выберите чит-файл из списка для копирования команды бана.", "Сбои CrashDumps", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			CrashEntry sel = listView.SelectedItems[0].Tag as CrashEntry;
			if (sel != null)
			{
				string ruleCode = sel.IsCleaner || !sel.ExistsOnDisk ? "5.08" : "5.01";
				string reason = !string.IsNullOrEmpty(sel.BanReason) ? sel.BanReason : sel.AppName;
				string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void OpenSelectedFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			CrashEntry sel = listView.SelectedItems[0].Tag as CrashEntry;
			if (sel == null) return;

			if (File.Exists(sel.ContainerPath))
			{
				Process.Start("explorer.exe", "/select,\"" + sel.ContainerPath + "\"");
			}
			else if (Directory.Exists(Path.GetDirectoryName(sel.ContainerPath)))
			{
				Process.Start("explorer.exe", "\"" + Path.GetDirectoryName(sel.ContainerPath) + "\"");
			}
			else
			{
				MessageBox.Show("Папка отчета недоступна:\r\n" + sel.ContainerPath, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void ExportProtocolToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АУДИТА CRASHDUMPS И ОТЧЕТОВ WER ===");
			sb.AppendLine(string.Format("👤 Проверяемый игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("📅 Дата и время анализа: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Всего дампов и отчетов: {0} (Сбоев читов: {1}, Удаленных с диска: {2})",
				allEntries.Count, allEntries.Count(i => i.IsCheat), allEntries.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner))));
			sb.AppendLine(new string('-', 85));

			foreach (CrashEntry item in allEntries.Where(i => i.IsCheat || i.IsCleaner || i.IsMinecraft))
			{
				sb.AppendLine(string.Format("[{0}] {1} | {2:dd.MM.yyyy HH:mm:ss}", item.Verdict, item.AppName, item.CrashTime));
				sb.AppendLine(string.Format("   Исходный путь: {0}", !string.IsNullOrEmpty(item.AppPath) ? item.AppPath : "Не указан"));
				sb.AppendLine(string.Format("   Статус на диске: {0}", item.ExistsOnDisk ? "Существует" : "УДАЛЕН С ДИСКА"));
				sb.AppendLine(string.Format("   Модуль / Код: {0} ({1})", item.FaultModule, item.ExceptionCode));
				sb.AppendLine(string.Format("   Отчет: {0}", item.ContainerPath));
				sb.AppendLine(string.Format("   Нарушение: {0}", item.ViolationRule));
				if (!string.IsNullOrEmpty(item.BanReason))
				{
					string ruleCode = item.IsCleaner || !item.ExistsOnDisk ? "5.08" : "5.01";
					sb.AppendLine(string.Format("   Команда бана: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, item.BanReason));
				}
				sb.AppendLine();
			}

			if (allEntries.Count(i => i.IsCheat || i.IsCleaner) == 0)
			{
				sb.AppendLine("✔ Сбоев чит-клиентов и утилит сокрытия улик в CrashDumps и WER не обнаружено.");
			}

			sb.AppendLine(new string('=', 85));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита CrashDumps и WER успешно скопирован в буфер обмена!", "Экспорт протокола", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
