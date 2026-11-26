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
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class ScheduledTasksStartupViewerForm : Form
	{
		#region Models

		public enum StartupFilterMode
		{
			All,
			ThreatsAndCleaners,
			ScheduledTasksOnly,
			RegistryRunOnly,
			StartupFolderOnly
		}

		public class StartupForensicItem
		{
			public string Mechanism;      // "Планировщик задач (Tasks)", "Реестр (HKCU Run)", "Папка Автозагрузка"
			public string Name;           // Имя задачи или параметра
			public string Command;        // Полная команда или путь к бинарнику
			public string TargetFilePath; // Извлеченный путь к файлу
			public string FileStatus;     // "Существует на диске", "🚨 УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)"
			public string Category;       // "Скрипт очистки следов", "Автозапуск чита", "Штатная программа"
			public string Status;         // Вердикт
			public bool IsThreat;
			public string RuleViolation;  // "5.08", "5.01", "5.02", "-"
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private StartupFilterMode currentFilter = StartupFilterMode.All;
		private List<StartupForensicItem> allEntries = new List<StartupForensicItem>();
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
		private DarkButton btnFilterThreats;
		private DarkButton btnFilterTasks;
		private DarkButton btnFilterRegistry;
		private DarkButton btnFilterFolder;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyCommand;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CleanerRegex = new Regex(
			@"(?i)(wevtutil\s+cl|fsutil\s+usn\s+deletejournal|Set-MpPreference\s+-DisableRealtime|remove-item\s+-recurse|rmdir\s+/s\s+/q|del\s+/f\s+/q|del\s+/s|clear-history|clean-traces|vanish|selfdelete|wipe_logs|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|vape)",
			RegexOptions.Compiled);

		private static readonly Regex ObfuscatedPsRegex = new Regex(
			@"(?i)(powershell.*(?:-enc|-encodedcommand|-w\s+hidden|-windowstyle\s+hidden|downloadstring|iex|invoke-expression|bypass))",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ScheduledTasksStartupViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит планировщика задач, автозагрузки и скрытых скриптов — MCCheatChecker";
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 580);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.KeyPreview = true;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartScanAsync();
					e.Handled = true;
				}
			};

			// Верхняя панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "⏱️ Аудит планировщика задач Windows, реестра автозагрузки и скрытых скриптов";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Поиск задач очистки следов (wevtutil/fsutil), обфусцированных скриптов PowerShell (-enc), автозапуска читов и удаленных bat-файлов.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 42);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(260, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 280, 22);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Панель фильтров и поиска
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все записи", StartupFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Скрипты очистки (5.08)", StartupFilterMode.ThreatsAndCleaners, ref bx, filterBar);
			btnFilterTasks = CreateFilterButton("⏱️ Планировщик (Tasks)", StartupFilterMode.ScheduledTasksOnly, ref bx, filterBar);
			btnFilterRegistry = CreateFilterButton("🔑 Реестр (Run/RunOnce)", StartupFilterMode.RegistryRunOnly, ref bx, filterBar);
			btnFilterFolder = CreateFilterButton("📂 Папка Автозагрузка", StartupFilterMode.StartupFolderOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 250, 8);
			searchBox.Size = new Size(235, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 305, 12);
			lblSearch.AutoSize = true;
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			filterBar.Controls.Add(lblSearch);

			// Нижняя панель действий
			Panel bottomBar = new Panel();
			bottomBar.Dock = DockStyle.Bottom;
			bottomBar.Height = 48;
			bottomBar.BackColor = Ui.Surface;
			bottomBar.Padding = new Padding(14, 8, 14, 8);
			this.Controls.Add(bottomBar);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Font = Ui.FontTitle(9f, true);
			btnCopyBan.Location = new Point(14, 10);
			btnCopyBan.Size = new Size(230, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnCopyCommand = new DarkButton();
			btnCopyCommand.Text = "📄 Скопировать путь / команду";
			btnCopyCommand.Font = Ui.FontBody(9f, false);
			btnCopyCommand.Location = new Point(252, 10);
			btnCopyCommand.Size = new Size(230, 30);
			btnCopyCommand.Click += BtnCopyCommand_Click;
			bottomBar.Controls.Add(btnCopyCommand);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(490, 10);
			btnEvidenceCapture.Size = new Size(185, 30);
			btnEvidenceCapture.Click += delegate
			{
				try
				{
					EvidenceCaptureForm ev = new EvidenceCaptureForm(targetPlayerName, Environment.UserName);
					ev.Show(this);
				}
				catch { }
			};
			bottomBar.Controls.Add(btnEvidenceCapture);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📄 Экспорт отчета";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(683, 10);
			btnExportReport.Size = new Size(150, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 F5";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 90, 10);
			btnRefreshNow.Size = new Size(75, 30);
			btnRefreshNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			bottomBar.Controls.Add(btnRefreshNow);

			// SplitContainer
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 380;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Font = Ui.FontBody(9.5f, false);

			listView.Columns.Add("Механизм автозапуска", 190);
			listView.Columns.Add("Имя задачи / Параметра", 230);
			listView.Columns.Add("Исполняемая команда / Скрипт", 380);
			listView.Columns.Add("Статус файла на диске", 190);
			listView.Columns.Add("Вердикт / Регламент", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ О ЗАДАЧЕ ИЛИ АВТОЗАГРУЗКЕ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из таблицы.";
			lblDetailVerdict.Font = Ui.FontTitle(10.5f, true);
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailVerdict.Location = new Point(16, 32);
			lblDetailVerdict.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailVerdict);

			lblDetailRule = new Label();
			lblDetailRule.Text = "";
			lblDetailRule.Font = Ui.FontTitle(9f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Location = new Point(16, 56);
			lblDetailRule.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailRule);

			lblDetailBanCommand = new Label();
			lblDetailBanCommand.Text = "";
			lblDetailBanCommand.Font = Ui.FontMono(9f, true);
			lblDetailBanCommand.ForeColor = Ui.AccentTeal;
			lblDetailBanCommand.Location = new Point(16, 78);
			lblDetailBanCommand.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailBanCommand);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(16, 104);
			txtDetails.Size = new Size(pnlDetailsContainer.Width - 32, pnlDetailsContainer.Height - 114);
			txtDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			txtDetails.Font = Ui.FontMono(9f, false);
			txtDetails.ReadOnly = true;
			pnlDetailsContainer.Controls.Add(txtDetails);
		}

		private DarkButton CreateFilterButton(string text, StartupFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == StartupFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(160, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 166;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == StartupFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == StartupFilterMode.ThreatsAndCleaners);
			btnFilterTasks.AccentStyle = (currentFilter == StartupFilterMode.ScheduledTasksOnly);
			btnFilterRegistry.AccentStyle = (currentFilter == StartupFilterMode.RegistryRunOnly);
			btnFilterFolder.AccentStyle = (currentFilter == StartupFilterMode.StartupFolderOnly);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<StartupForensicItem> results = new List<StartupForensicItem>();

				// 1. Сканирование задач планировщика Windows Tasks
				ScanScheduledTasks(results);

				// 2. Сканирование реестра Run / RunOnce
				ScanRegistryRunKeys(results);

				// 3. Сканирование папок автозагрузки (Shell Startup)
				ScanStartupFolders(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(StartupForensicItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УГРОЗ В АВТОЗАГРУЗКЕ (5.08/5.01): {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Записей проверено: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanScheduledTasks(List<StartupForensicItem> results)
		{
			string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string tasksRoot = Path.Combine(windir, @"System32\Tasks");

			if (!Directory.Exists(tasksRoot)) return;

			try
			{
				string[] taskFiles = Directory.GetFiles(tasksRoot, "*", SearchOption.AllDirectories);
				foreach (string taskFile in taskFiles)
				{
					try
					{
						string relPath = taskFile.Substring(tasksRoot.Length).TrimStart('\\');
						// Пропуск системных подпапок Microsoft (если они не содержат подозрительных bat скриптов)
						bool isMsFolder = relPath.StartsWith("Microsoft\\", StringComparison.OrdinalIgnoreCase);

						string content;
						using (FileStream fs = new FileStream(taskFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
							{
								content = sr.ReadToEnd();
							}
						}

						// Извлечение команды и аргументов из XML
						Match execMatch = Regex.Match(content, @"<Command>(.*?)</Command>", RegexOptions.IgnoreCase);
						Match argsMatch = Regex.Match(content, @"<Arguments>(.*?)</Arguments>", RegexOptions.IgnoreCase);

						if (!execMatch.Success && isMsFolder) continue;

						string cmd = execMatch.Success ? execMatch.Groups[1].Value : "";
						string args = argsMatch.Success ? argsMatch.Groups[1].Value : "";
						string fullExec = (cmd + " " + args).Trim();

						bool isCleaner = CleanerRegex.IsMatch(fullExec) || CleanerRegex.IsMatch(relPath);
						bool isObfuscatedPs = ObfuscatedPsRegex.IsMatch(fullExec);
						bool isBatScript = cmd.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || cmd.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || cmd.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase) || cmd.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);

						if (isMsFolder && !isCleaner && !isObfuscatedPs && !isBatScript)
						{
							continue; // Стандартная задача Windows
						}

						// Проверка существования целевого файла на диске
						string targetPath = ExtractCleanPath(cmd);
						string fileStatus = "—";
						bool targetDeleted = false;

						if (!string.IsNullOrEmpty(targetPath) && (targetPath.Contains(":") || targetPath.StartsWith("\\\\")))
						{
							if (File.Exists(targetPath))
							{
								fileStatus = "Присутствует на диске";
							}
							else
							{
								fileStatus = "🚨 ФАЙЛ УДАЛЕН С ДИСКА (5.08)";
								targetDeleted = true;
							}
						}

						StartupForensicItem item = new StartupForensicItem();
						item.Mechanism = "Планировщик задач (Tasks)";
						item.Name = relPath;
						item.Command = fullExec;
						item.TargetFilePath = targetPath;
						item.FileStatus = fileStatus;

						if (isCleaner)
						{
							item.Category = "Скрипт очистки следов в планировщике";
							item.Status = "🚨 ЗАДАЧА ОЧИСТКИ СЛЕДОВ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Запланированная задача очистки улик / логов в Task Scheduler";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("В ПЛАНИРОВЩИКЕ ЗАДАЧ НАЙДЕН СКРИПТ ОЧИСТКИ УЛИК!\nЗадача: {0}\nКоманда: {1}\nСтатус файла: {2}\nНарушение: Пункт 5.08 (Использование скриптов/задач для удаления логов, USN или корзины).", relPath, fullExec, fileStatus);
							results.Add(item);
						}
						else if (targetDeleted && isBatScript)
						{
							item.Category = "Удаленный скрипт автозапуска";
							item.Status = "🚨 СКРИПТ АВТОЗАПУСКА УДАЛЕН (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Удаление скрипта автозапуска перед проверкой";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Исполняемый скрипт задачи планировщика был удален с диска перед проверкой!\nЗадача: {0}\nКоманда: {1}\nПуть: {2}\nНарушение: Пункт регламента 5.08.", relPath, fullExec, targetPath);
							results.Add(item);
						}
						else if (isObfuscatedPs)
						{
							item.Category = "Обфусцированный вызов PowerShell";
							item.Status = "🚨 СКРЫТЫЙ POWERSHELL СКРИПТ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Скрытая обфусцированная задача PowerShell в планировщике";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("ОБНАРУЖЕНА СКРЫТАЯ ЗАДАЧА POWERSHELL В ПЛАНИРОВЩИКЕ!\nЗадача: {0}\nКоманда: {1}\nНарушение: Пункт 5.08.", relPath, fullExec);
							results.Add(item);
						}
						else if (!isMsFolder)
						{
							item.Category = "Сторонняя задача планировщика";
							item.Status = "Сторонняя задача";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Задача: {0}\nКоманда: {1}\nФайл задачи: {2}", relPath, fullExec, taskFile);
							results.Add(item);
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanRegistryRunKeys(List<StartupForensicItem> results)
		{
			string[] runSubKeys = new string[]
			{
				@"Software\Microsoft\Windows\CurrentVersion\Run",
				@"Software\Microsoft\Windows\CurrentVersion\RunOnce"
			};

			// 1. HKCU
			foreach (string subKey in runSubKeys)
			{
				ScanSingleRegistryRunHive(Registry.CurrentUser, subKey, "Реестр (HKCU\\" + subKey + ")", results);
			}

			// 2. HKLM
			foreach (string subKey in runSubKeys)
			{
				ScanSingleRegistryRunHive(Registry.LocalMachine, subKey, "Реестр (HKLM\\" + subKey + ")", results);
			}
		}

		private void ScanSingleRegistryRunHive(RegistryKey root, string subKeyPath, string mechName, List<StartupForensicItem> results)
		{
			try
			{
				using (RegistryKey key = root.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string name in valNames)
					{
						object val = key.GetValue(name);
						if (val == null) continue;
						string cmd = val.ToString().Trim();

						bool isCleaner = CleanerRegex.IsMatch(cmd) || CleanerRegex.IsMatch(name);
						bool isObfuscatedPs = ObfuscatedPsRegex.IsMatch(cmd);

						string targetPath = ExtractCleanPath(cmd);
						string fileStatus = "—";
						bool targetDeleted = false;

						if (!string.IsNullOrEmpty(targetPath) && (targetPath.Contains(":") || targetPath.StartsWith("\\\\")))
						{
							if (File.Exists(targetPath))
							{
								fileStatus = "Присутствует на диске";
							}
							else
							{
								fileStatus = "🚨 ФАЙЛ УДАЛЕН С ДИСКА (5.08)";
								targetDeleted = true;
							}
						}

						StartupForensicItem item = new StartupForensicItem();
						item.Mechanism = mechName;
						item.Name = name;
						item.Command = cmd;
						item.TargetFilePath = targetPath;
						item.FileStatus = fileStatus;

						if (isCleaner)
						{
							item.Category = "Скрипт очистки в реестре Run";
							item.Status = "🚨 СКРИПТ ОЧИСТКИ В РЕЕСТРЕ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Команда зачистки следов в автозагрузке реестра";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("В АВТОЗАГРУЗКЕ РЕЕСТРА ЗАФИКСИРОВАН СКРИПТ ОЧИСТКИ СЛЕДОВ!\nКлюч: {0}\nПараметр: {1}\nКоманда: {2}\nСтатус файла: {3}\nНарушение: Пункт регламента 5.08.", mechName, name, cmd, fileStatus);
							results.Add(item);
						}
						else if (targetDeleted && (cmd.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || cmd.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || cmd.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase)))
						{
							item.Category = "Удаленный скрипт автозагрузки";
							item.Status = "🚨 СКРИПТ АВТОЗАГРУЗКИ УДАЛЕН (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Удаление скрипта автозагрузки перед проверкой";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Исполняемый файл из ветки автозагрузки был удален с диска перед проверкой!\nКлюч: {0}\nПараметр: {1}\nПуть: {2}\nНарушение: Пункт регламента 5.08.", mechName, name, targetPath);
							results.Add(item);
						}
						else if (isObfuscatedPs)
						{
							item.Category = "Скрытый вызов PowerShell";
							item.Status = "🚨 СКРЫТЫЙ ВЫЗОВ POWERSHELL (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Скрытая команда PowerShell в автозагрузке";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Обнаружена скрытая команда PowerShell в автозагрузке!\nКлюч: {0}\nПараметр: {1}\nКоманда: {2}", mechName, name, cmd);
							results.Add(item);
						}
						else
						{
							item.Category = "Автозапуск приложения";
							item.Status = "Штатный автозапуск";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Ключ: {0}\nИмя: {1}\nКоманда: {2}\nПуть: {3}", mechName, name, cmd, targetPath);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanStartupFolders(List<StartupForensicItem> results)
		{
			string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);

			ScanSingleStartupFolder(userStartup, "Папка Автозагрузка (User)", results);
			ScanSingleStartupFolder(commonStartup, "Папка Автозагрузка (Common)", results);
		}

		private void ScanSingleStartupFolder(string folderPath, string mechName, List<StartupForensicItem> results)
		{
			if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

			try
			{
				string[] files = Directory.GetFiles(folderPath);
				foreach (string file in files)
				{
					try
					{
						string fname = Path.GetFileName(file);
						string ext = Path.GetExtension(file).ToLowerInvariant();

						bool isCleaner = CleanerRegex.IsMatch(fname);
						string preview = "";

						if (ext == ".bat" || ext == ".cmd" || ext == ".ps1" || ext == ".vbs")
						{
							try
							{
								string content = File.ReadAllText(file, Encoding.Default);
								if (CleanerRegex.IsMatch(content)) isCleaner = true;
								preview = content.Length > 200 ? content.Substring(0, 197) + "..." : content;
							}
							catch { }
						}

						StartupForensicItem item = new StartupForensicItem();
						item.Mechanism = mechName;
						item.Name = fname;
						item.Command = file;
						item.TargetFilePath = file;
						item.FileStatus = "Присутствует на диске";

						if (isCleaner)
						{
							item.Category = "Скрипт очистки в папке Автозагрузка";
							item.Status = "🚨 СКРИПТ ОЧИСТКИ В STARTUP (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Скрипт зачистки следов в папке автозагрузки Windows";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("В ПАПКЕ АВТОЗАГРУЗКИ ОБНАРУЖЕН СКРИПТ ЗАТИРАНИЯ УЛИК!\nФайл: {0}\nПуть: {1}\nПревью содержимого:\n{2}\nНарушение: Пункт регламента 5.08.", fname, file, preview);
							results.Add(item);
						}
						else
						{
							item.Category = "Файл в папке Автозагрузка";
							item.Status = "Файл автозагрузки";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Файл: {0}\nПуть: {1}\nРазмер: {2} байт", fname, file, new FileInfo(file).Length);
							results.Add(item);
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private static string ExtractCleanPath(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return "";
			string s = raw.Trim();
			if (s.StartsWith("\""))
			{
				int endQ = s.IndexOf('\"', 1);
				if (endQ > 1) return s.Substring(1, endQ - 1);
			}
			int spaceIdx = s.IndexOf(' ');
			if (spaceIdx > 0 && s.Contains(":"))
			{
				string sub = s.Substring(0, spaceIdx);
				if (sub.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || sub.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || sub.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
				{
					return sub;
				}
			}
			return s;
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (StartupForensicItem item in allEntries)
			{
				if (currentFilter == StartupFilterMode.ThreatsAndCleaners && !item.IsThreat) continue;
				if (currentFilter == StartupFilterMode.ScheduledTasksOnly && !item.Mechanism.Contains("Планировщик")) continue;
				if (currentFilter == StartupFilterMode.RegistryRunOnly && !item.Mechanism.Contains("Реестр")) continue;
				if (currentFilter == StartupFilterMode.StartupFolderOnly && !item.Mechanism.Contains("Папка Автозагрузка")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.Mechanism != null && item.Mechanism.ToLowerInvariant().Contains(search)) ||
								   (item.Name != null && item.Name.ToLowerInvariant().Contains(search)) ||
								   (item.Command != null && item.Command.ToLowerInvariant().Contains(search)) ||
								   (item.TargetFilePath != null && item.TargetFilePath.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Mechanism);
				lvi.SubItems.Add(item.Name);
				lvi.SubItems.Add(item.Command);
				lvi.SubItems.Add(item.FileStatus);
				lvi.SubItems.Add(item.Status);
				lvi.ForeColor = item.ForeColor;
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

			StartupForensicItem item = listView.SelectedItems[0].Tag as StartupForensicItem;
			if (item == null) return;

			lblDetailVerdict.Text = item.Status;
			lblDetailVerdict.ForeColor = item.ForeColor;

			if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
			{
				lblDetailRule.Text = string.Format("⚖️ Пункт регламента: {0} ({1})", item.RuleViolation, item.BanReason);
				lblDetailBanCommand.Text = string.Format("📋 Команда: /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			}
			else
			{
				lblDetailRule.Text = "🛡️ Нарушений не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details ?? "Нет сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Запись не выбрана";
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailRule.Text = "";
			lblDetailBanCommand.Text = "";
			txtDetails.Text = "";
		}

		#endregion

		#region Actions

		private void BtnCopyBan_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			StartupForensicItem item = listView.SelectedItems[0].Tag as StartupForensicItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnCopyCommand_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			StartupForensicItem item = listView.SelectedItems[0].Tag as StartupForensicItem;
			if (item == null || string.IsNullOrEmpty(item.Command)) return;

			Clipboard.SetText(item.Command);
			MessageBox.Show("Команда/путь скопирован в буфер обмена:\n\n" + item.Command, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Планировщик_Автозагрузка_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА ПЛАНИРОВЩИКА ЗАДАЧ И АВТОЗАГРУЗКИ");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (StartupForensicItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.Mechanism, item.Name));
						sb.AppendLine(string.Format("Команда:     {0}", item.Command));
						sb.AppendLine(string.Format("Статус диска: {0}", item.FileStatus));
						sb.AppendLine(string.Format("Вердикт:     {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", item.Details));
						sb.AppendLine(new string('-', 80));
					}

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("Отчет успешно сохранен:\n" + sfd.FileName, "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при экспорте отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
