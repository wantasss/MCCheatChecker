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
	public class UserAssistMuiCacheViewerForm : Form
	{
		#region Models

		public enum ShellFilterMode
		{
			All,
			CheatsOnly,
			DeletedOnly,
			CleanersOnly,
			MinecraftOnly,
			Whitelist
		}

		public class ShellItem
		{
			public string Source; // "UserAssist (ROT13)", "MuiCache", "PCA Store", "AppSwitched"
			public string TargetPath;
			public string FileName;
			public string FriendlyName;
			public int RunCount;
			public int FocusCount;
			public int FocusTimeSeconds;
			public DateTime LastExecuted;
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
		private ShellFilterMode currentFilter = ShellFilterMode.All;
		private List<ShellItem> allItems = new List<ShellItem>();
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
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterMinecraft;
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

		public UserAssistMuiCacheViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			Text = "🖥️ Инспектор запусков Windows Shell: UserAssist, MuiCache и PCA Store";
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
			lblHeaderTitle.Text = "🖥️ Инспектор запусков Windows Shell: UserAssist (ROT13), MuiCache и PCA";
			lblHeaderTitle.Font = Ui.FontTitle(11.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(12, 8);
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Криминалистический анализ истории запусков графической оболочки Explorer: счётчики запусков, время фокуса окна, MuiCache и удалённые читы.";
			lblHeaderSubtitle.Font = Ui.FontBody(8f);
			lblHeaderSubtitle.ForeColor = Ui.TextMuted;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(13, 30);
			topPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования реестра Shell...";
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

			btnFilterAll = CreateFilterButton("Все записи", ShellFilterMode.All);
			btnFilterCheats = CreateFilterButton("🚨 Читы (5.01)", ShellFilterMode.CheatsOnly);
			btnFilterDeleted = CreateFilterButton("🚨 Удалены перед проверкой (5.08)", ShellFilterMode.DeletedOnly);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры / Вайперы (5.08)", ShellFilterMode.CleanersOnly);
			btnFilterMinecraft = CreateFilterButton("☕ Minecraft & Лаунчеры", ShellFilterMode.MinecraftOnly);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", ShellFilterMode.Whitelist);

			filterRow.Controls.Add(btnFilterAll);
			filterRow.Controls.Add(btnFilterCheats);
			filterRow.Controls.Add(btnFilterDeleted);
			filterRow.Controls.Add(btnFilterCleaners);
			filterRow.Controls.Add(btnFilterMinecraft);
			filterRow.Controls.Add(btnFilterWhitelist);
			topPanel.Controls.Add(filterRow);

			// Search & Actions row
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
			lblSearchHint.Text = "🔍 Поиск по имени, пути, источнику...";
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
			btnOpenFolder.Text = "📂 Открыть в проводнике";
			btnOpenFolder.Location = new Point(678, 2);
			btnOpenFolder.Size = new Size(175, 25);
			btnOpenFolder.Click += delegate { OpenSelectedInExplorer(); };
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
			listView.Columns.Add("Исполняемый файл (.exe / .jar)", 220);
			listView.Columns.Add("Последний запуск", 150);
			listView.Columns.Add("Статус на диске", 170);
			listView.Columns.Add("Запуски / Время фокуса", 170);
			listView.Columns.Add("Полный путь / Источник Shell", 320);

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
			lblDetailVerdict.Text = "Выберите запись из таблицы выше для криминалистического анализа.";
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
			lblHelp.Text = "💡 Подсказка: UserAssist шифруется алгоритмом ROT13 и фиксирует точное число запусков, а также суммарное время фокуса окна чита пользователем.";
			lblHelp.Location = new Point(12, 7);
			lblHelp.AutoSize = true;
			lblHelp.ForeColor = Ui.TextMuted;
			lblHelp.Font = Ui.FontBody(8f);
			bottomBar.Controls.Add(lblHelp);

			Controls.Add(bottomBar);

			UpdateFilterButtonsStyle();
		}

		private DarkButton CreateFilterButton(string text, ShellFilterMode mode)
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
			btnFilterAll.BackColor = (currentFilter == ShellFilterMode.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterCheats.BackColor = (currentFilter == ShellFilterMode.CheatsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterDeleted.BackColor = (currentFilter == ShellFilterMode.DeletedOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterCleaners.BackColor = (currentFilter == ShellFilterMode.CleanersOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterMinecraft.BackColor = (currentFilter == ShellFilterMode.MinecraftOnly) ? Ui.AccentTeal : Ui.Surface;
			btnFilterWhitelist.BackColor = (currentFilter == ShellFilterMode.Whitelist) ? Ui.AccentGreen : Ui.Surface;
		}

		#endregion

		#region Scanning Logic

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "⏳ Сканирование реестра UserAssist, MuiCache и PCA Store...";
			lblStatusPill.ForeColor = Ui.AccentTeal;

			ThreadPool.QueueUserWorkItem(delegate {
				List<ShellItem> results = new List<ShellItem>();
				HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				try
				{
					// 1. Scan UserAssist (ROT13)
					ScanUserAssist(results, seenPaths);

					// 2. Scan MuiCache
					ScanMuiCache(results, seenPaths);

					// 3. Scan PCA Store
					ScanPcaStore(results, seenPaths);

					// 4. Scan AppSwitched
					ScanAppSwitched(results, seenPaths);
				}
				catch { }

				BeginInvoke(new Action(delegate {
					allItems = results;
					isScanning = false;

					int crit = allItems.Count(i => i.IsCheat || i.IsCleaner);
					int deleted = allItems.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner));

					if (deleted > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ УДАЛЕННЫЕ ЧИТЫ: {0} шт. (Всего подозрительных: {1})", deleted, crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else if (crit > 0)
					{
						lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ ЗАПУСКИ ЧИТ-КЛИЕНТОВ: {0} шт.", crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else
					{
						lblStatusPill.Text = string.Format("✔ Проанализировано {0} записей Shell. Следов читов не обнаружено.", allItems.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
					}

					ApplyFilters();
				}));
			});
		}

		private static string Rot13(string input)
		{
			if (string.IsNullOrEmpty(input)) return "";
			char[] chars = input.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				char c = chars[i];
				if (c >= 'a' && c <= 'z') chars[i] = (char)((c - 'a' + 13) % 26 + 'a');
				else if (c >= 'A' && c <= 'Z') chars[i] = (char)((c - 'A' + 13) % 26 + 'A');
			}
			return new string(chars);
		}

		private void ScanUserAssist(List<ShellItem> results, HashSet<string> seenPaths)
		{
			string uaPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist";
			try
			{
				using (RegistryKey uaKey = Registry.CurrentUser.OpenSubKey(uaPath))
				{
					if (uaKey == null) return;
					foreach (string subKeyName in uaKey.GetSubKeyNames())
					{
						using (RegistryKey countKey = uaKey.OpenSubKey(subKeyName + @"\Count"))
						{
							if (countKey == null) continue;
							foreach (string valName in countKey.GetValueNames())
							{
								string decoded = Rot13(valName);
								if (string.IsNullOrEmpty(decoded)) continue;

								byte[] data = countKey.GetValue(valName) as byte[];
								int runCount = 0;
								int focusCount = 0;
								int focusTimeMs = 0;
								DateTime lastRun = DateTime.MinValue;

								if (data != null && data.Length >= 68)
								{
									runCount = BitConverter.ToInt32(data, 4);
									focusCount = BitConverter.ToInt32(data, 8);
									focusTimeMs = BitConverter.ToInt32(data, 12);
									long ft = BitConverter.ToInt64(data, 60);
									if (ft > 0)
									{
										try { lastRun = DateTime.FromFileTimeUtc(ft).ToLocalTime(); } catch { }
									}
								}

								// Clean path
								string cleanPath = decoded;
								int pIdx = decoded.IndexOf(@":\");
								if (pIdx > 1) cleanPath = decoded.Substring(pIdx - 1);

								if (!seenPaths.Contains(cleanPath))
								{
									seenPaths.Add(cleanPath);

									ShellItem item = new ShellItem();
									item.Source = "UserAssist (ROT13)";
									item.TargetPath = cleanPath;
									item.FileName = Path.GetFileName(cleanPath);
									item.RunCount = runCount;
									item.FocusCount = focusCount;
									item.FocusTimeSeconds = focusTimeMs / 1000;
									item.LastExecuted = lastRun;
									item.ExistsOnDisk = File.Exists(cleanPath) || Directory.Exists(cleanPath);

									CategorizeShellItem(item);
									results.Add(item);
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanMuiCache(List<ShellItem> results, HashSet<string> seenPaths)
		{
			string muiPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
			try
			{
				using (RegistryKey muiKey = Registry.CurrentUser.OpenSubKey(muiPath))
				{
					if (muiKey == null) return;
					foreach (string valName in muiKey.GetValueNames())
					{
						if (valName.EndsWith(".FriendlyAppName", StringComparison.OrdinalIgnoreCase))
						{
							string rawPath = valName.Substring(0, valName.Length - 16);
							string friendly = muiKey.GetValue(valName) as string;

							if (!seenPaths.Contains(rawPath))
							{
								seenPaths.Add(rawPath);

								ShellItem item = new ShellItem();
								item.Source = "MuiCache";
								item.TargetPath = rawPath;
								item.FileName = Path.GetFileName(rawPath);
								item.FriendlyName = friendly;
								item.ExistsOnDisk = File.Exists(rawPath) || Directory.Exists(rawPath);

								CategorizeShellItem(item);
								results.Add(item);
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanPcaStore(List<ShellItem> results, HashSet<string> seenPaths)
		{
			string pcaPath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store";
			try
			{
				using (RegistryKey pcaKey = Registry.CurrentUser.OpenSubKey(pcaPath))
				{
					if (pcaKey == null) return;
					foreach (string valName in pcaKey.GetValueNames())
					{
						if (!seenPaths.Contains(valName))
						{
							seenPaths.Add(valName);

							ShellItem item = new ShellItem();
							item.Source = "PCA Store";
							item.TargetPath = valName;
							item.FileName = Path.GetFileName(valName);
							item.ExistsOnDisk = File.Exists(valName) || Directory.Exists(valName);

							CategorizeShellItem(item);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanAppSwitched(List<ShellItem> results, HashSet<string> seenPaths)
		{
			string appSwitchPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched";
			try
			{
				using (RegistryKey asKey = Registry.CurrentUser.OpenSubKey(appSwitchPath))
				{
					if (asKey == null) return;
					foreach (string valName in asKey.GetValueNames())
					{
						if (!seenPaths.Contains(valName))
						{
							seenPaths.Add(valName);

							int count = 0;
							object v = asKey.GetValue(valName);
							if (v is int) count = (int)v;

							ShellItem item = new ShellItem();
							item.Source = "AppSwitched";
							item.TargetPath = valName;
							item.FileName = Path.GetFileName(valName);
							item.RunCount = count;
							item.ExistsOnDisk = File.Exists(valName) || Directory.Exists(valName);

							CategorizeShellItem(item);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void CategorizeShellItem(ShellItem item)
		{
			string lowerName = (item.FileName ?? "").ToLowerInvariant();
			string lowerPath = (item.TargetPath ?? "").ToLowerInvariant();

			// Whitelist Rules Check
			bool isAutoclicker = lowerName.Contains("opautoclicker") || lowerName.Contains("speedautoclicker") ||
				lowerName.Contains("tinytask") || lowerName.Contains("x-mouse") || lowerName.Contains("autohotkey") ||
				lowerName.Contains("clickermann") || lowerName.Contains("keyran") || lowerName.Contains("auto_clicker");

			bool isScreenShare = lowerName.Contains("anydesk") || lowerName.Contains("rudesktop") || lowerName.Contains("discord");

			if (isAutoclicker)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: Автокликер)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "Легитимный кликер (Разрешено)";
				item.Details = string.Format("Запуск разрешенного кликера ({0}).\r\nПуть: {1}\r\nИсточник: {2}", item.FileName, item.TargetPath, item.Source);
				return;
			}

			if (isScreenShare)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: ScreenShare)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "ПО для демонстрации экрана";
				item.Details = string.Format("Программа ScreenShare ({0}).\r\nПуть: {1}\r\nИсточник: {2}", item.FileName, item.TargetPath, item.Source);
				return;
			}

			// Cleaners / Wipers
			if (CleanerPattern.IsMatch(lowerName) || CleanerPattern.IsMatch(lowerPath))
			{
				item.IsCleaner = true;
				item.Verdict = "🚨 КЛИЕНЕР / ВАЙПЕР (5.08)";
				item.ForeColor = Ui.AccentRed;
				item.ViolationRule = "Пункт 5.08 (Утилита очистки следов и журналов)";
				item.BanReason = string.Format("Запуск утилиты очистки {0} ({1})", item.FileName, item.Source);
				item.Details = string.Format(
					"🚨 ОБНАРУЖЕНА УТИЛИТА ОЧИСТКИ СЛЕДОВ В РЕЕСТРЕ SHELL!\r\n" +
					"• Программа: {0}\r\n" +
					"• Исходный путь: {1}\r\n" +
					"• Источник: {2}\r\n" +
					"• Статус на диске: {3}\r\n" +
					"• Запусков: {4}\r\n" +
					"• Вердикт: Использование ПО для сокрытия улик (Регламент 5.08 — Бан 30 дней).",
					item.FileName, item.TargetPath, item.Source, item.ExistsOnDisk ? "Существует" : "Удален с диска", item.RunCount);
				return;
			}

			// Cheats Check
			bool isMatchCheat = CheatPattern.IsMatch(lowerName) || CheatPattern.IsMatch(lowerPath);

			if (isMatchCheat)
			{
				item.IsCheat = true;

				if (!item.ExistsOnDisk)
				{
					item.Verdict = "🚨 ЧИТ УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)";
					item.ForeColor = Ui.AccentRed;
					item.ViolationRule = "Пункт 5.08 / 5.01 (Удаление чита перед проверкой)";
					item.BanReason = string.Format("Удаление чита {0} перед проверкой ({1})", item.FileName, item.Source);
					item.Details = string.Format(
						"🚨 УЛИКА СОКРЫТИЯ ЧИТА: ФАЙЛ БЫЛ УДАЛЕН ПЕРЕД ПРОВЕРКОЙ!\r\n" +
						"• Чит: {0}\r\n" +
						"• Исходный путь: {1}\r\n" +
						"• Источник улик: {2}\r\n" +
						"• Статус на диске: УДАЛЕН (Файл отсутствует по исходному пути!).\r\n" +
						"• Запусков: {3}\r\n" +
						"• Время активности: {4} сек\r\n" +
						"• Последний запуск: {5:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Вердикт: Использование читов и сокрытие улик (Регламент 5.08 — Бан 30 дней).",
						item.FileName, item.TargetPath, item.Source, item.RunCount, item.FocusTimeSeconds, item.LastExecuted);
				}
				else
				{
					item.Verdict = "🚨 ЧИТ-КЛИЕНТ / СОФТ (5.01)";
					item.ForeColor = Ui.AccentRed;
					item.ViolationRule = "Пункт 5.01 (Использование читов)";
					item.BanReason = string.Format("Запуск чита {0} ({1})", item.FileName, item.Source);
					item.Details = string.Format(
						"🚨 ОБНАРУЖЕН ЗАПУСК ЧИТ-КЛИЕНТА В РЕЕСТРЕ WINDOWS SHELL!\r\n" +
						"• Чит: {0}\r\n" +
						"• Путь: {1}\r\n" +
						"• Источник: {2}\r\n" +
						"• Количество запусков: {3}\r\n" +
						"• Время активности в окне: {4} сек\r\n" +
						"• Последний запуск: {5:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Вердикт: Запрещенное ПО по Регламенту 5.01.",
						item.FileName, item.TargetPath, item.Source, item.RunCount, item.FocusTimeSeconds, item.LastExecuted);
				}
				return;
			}

			// Minecraft
			if (lowerName.Contains("javaw") || lowerName.Contains("java") || lowerName.Contains("tlauncher") ||
				lowerName.Contains("minecraft") || lowerName.Contains("curseforge") || lowerName.Contains("prism"))
			{
				item.IsMinecraft = true;
				item.Verdict = "☕ MINECRAFT / ЛАУНЧЕР";
				item.ForeColor = Ui.AccentTeal;
				item.ViolationRule = "Игровой процесс";
				item.Details = string.Format("Игровой клиент или лаунчер ({0}).\r\nПуть: {1}\r\nЗапусков: {2}\r\nИсточник: {3}", item.FileName, item.TargetPath, item.RunCount, item.Source);
				return;
			}

			item.Verdict = "ℹ️ ЗАПИСЬ WINDOWS SHELL";
			item.ForeColor = Ui.TextMuted;
			item.ViolationRule = "Общая программа";
			item.Details = string.Format("Запись Shell ({0}).\r\nФайл: {1}\r\nПуть: {2}\r\nЗапусков: {3}", item.Source, item.FileName, item.TargetPath, item.RunCount);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilters()
		{
			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			List<ShellItem> filtered = allItems.Where(item => {
				// Filter by mode
				switch (currentFilter)
				{
					case ShellFilterMode.CheatsOnly:
						if (!item.IsCheat) return false;
						break;
					case ShellFilterMode.DeletedOnly:
						if (item.ExistsOnDisk || (!item.IsCheat && !item.IsCleaner)) return false;
						break;
					case ShellFilterMode.CleanersOnly:
						if (!item.IsCleaner) return false;
						break;
					case ShellFilterMode.MinecraftOnly:
						if (!item.IsMinecraft) return false;
						break;
					case ShellFilterMode.Whitelist:
						if (!item.IsWhitelisted) return false;
						break;
				}

				// Search query
				if (!string.IsNullOrEmpty(query))
				{
					bool matchName = (item.FileName ?? "").ToLowerInvariant().Contains(query);
					bool matchPath = (item.TargetPath ?? "").ToLowerInvariant().Contains(query);
					bool matchSource = (item.Source ?? "").ToLowerInvariant().Contains(query);
					bool matchFriendly = (item.FriendlyName ?? "").ToLowerInvariant().Contains(query);
					bool matchVerdict = (item.Verdict ?? "").ToLowerInvariant().Contains(query);
					if (!matchName && !matchPath && !matchSource && !matchFriendly && !matchVerdict) return false;
				}

				return true;
			}).ToList();

			// Order: Cheats / Cleaners first, then deleted, then run count
			filtered = filtered.OrderByDescending(i => i.IsCheat || i.IsCleaner)
				.ThenByDescending(i => !i.ExistsOnDisk)
				.ThenByDescending(i => i.RunCount)
				.ThenByDescending(i => i.LastExecuted)
				.ToList();

			listView.BeginUpdate();
			listView.Items.Clear();

			foreach (ShellItem item in filtered)
			{
				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.ForeColor = item.ForeColor;
				lvi.SubItems.Add(item.FileName ?? "");
				lvi.SubItems.Add((item.LastExecuted > DateTime.MinValue) ? item.LastExecuted.ToString("dd.MM.yyyy HH:mm:ss") : "-");

				string diskStatus = item.ExistsOnDisk ? "[Существует на диске]" : "[🚨 УДАЛЕН С ДИСКА]";
				lvi.SubItems.Add(diskStatus);

				string usage = (item.RunCount > 0) ? string.Format("{0} раз ({1}с)", item.RunCount, item.FocusTimeSeconds) : "-";
				lvi.SubItems.Add(usage);

				string pathDisplay = string.Format("{0} [{1}]", item.TargetPath, item.Source);
				lvi.SubItems.Add(pathDisplay);
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

			ShellItem item = listView.SelectedItems[0].Tag as ShellItem;
			if (item == null) return;

			lblDetailVerdict.Text = item.Verdict;
			lblDetailVerdict.ForeColor = item.ForeColor;
			lblDetailRule.Text = "Регламент: " + (item.ViolationRule ?? "Общий аудит");

			if (item.IsCheat || item.IsCleaner)
			{
				string reason = !string.IsNullOrEmpty(item.BanReason) ? item.BanReason : item.FileName;
				string ruleCode = item.IsCleaner || !item.ExistsOnDisk ? "5.08" : "5.01";
				lblDetailBanCommand.Text = string.Format("Команда: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
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
				ShellItem firstCrit = allItems.FirstOrDefault(i => i.IsCheat || i.IsCleaner);
				if (firstCrit != null)
				{
					string ruleCode = firstCrit.IsCleaner || !firstCrit.ExistsOnDisk ? "5.08" : "5.01";
					string ban = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, firstCrit.BanReason ?? firstCrit.FileName);
					Clipboard.SetText(ban);
					MessageBox.Show("Скопирована команда бана за запуск чита в Shell:\r\n\r\n" + ban, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				MessageBox.Show("Выберите чит-файл из списка для копирования команды бана.", "Запуски Shell", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ShellItem sel = listView.SelectedItems[0].Tag as ShellItem;
			if (sel != null)
			{
				string ruleCode = sel.IsCleaner || !sel.ExistsOnDisk ? "5.08" : "5.01";
				string reason = !string.IsNullOrEmpty(sel.BanReason) ? sel.BanReason : sel.FileName;
				string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void OpenSelectedInExplorer()
		{
			if (listView.SelectedItems.Count == 0) return;
			ShellItem sel = listView.SelectedItems[0].Tag as ShellItem;
			if (sel == null) return;

			if (File.Exists(sel.TargetPath))
			{
				Process.Start("explorer.exe", "/select,\"" + sel.TargetPath + "\"");
			}
			else if (Directory.Exists(sel.TargetPath))
			{
				Process.Start("explorer.exe", "\"" + sel.TargetPath + "\"");
			}
			else
			{
				MessageBox.Show("Файл удален с диска!\r\nПуть: " + sel.TargetPath, "Файл не существует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void ExportProtocolToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АУДИТА ЗАПУСКОВ WINDOWS SHELL (USERASSIST & MUICACHE) ===");
			sb.AppendLine(string.Format("👤 Проверяемый игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("📅 Дата и время анализа: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Всего записей в анализе: {0} (Читов: {1}, Удаленных с диска: {2})",
				allItems.Count, allItems.Count(i => i.IsCheat), allItems.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner))));
			sb.AppendLine(new string('-', 85));

			foreach (ShellItem item in allItems.Where(i => i.IsCheat || i.IsCleaner))
			{
				sb.AppendLine(string.Format("[{0}] {1}", item.Verdict, item.FileName));
				sb.AppendLine(string.Format("   Исходный путь: {0}", item.TargetPath));
				sb.AppendLine(string.Format("   Статус на диске: {0} | Запусков: {1} | Фокус: {2} сек", item.ExistsOnDisk ? "Существует" : "УДАЛЕН С ДИСКА", item.RunCount, item.FocusTimeSeconds));
				sb.AppendLine(string.Format("   Источник: {0}", item.Source));
				sb.AppendLine(string.Format("   Нарушение: {0}", item.ViolationRule));
				if (!string.IsNullOrEmpty(item.BanReason))
				{
					string ruleCode = item.IsCleaner || !item.ExistsOnDisk ? "5.08" : "5.01";
					sb.AppendLine(string.Format("   Команда бана: /ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, item.BanReason));
				}
				sb.AppendLine();
			}

			if (allItems.Count(i => i.IsCheat || i.IsCleaner) == 0)
			{
				sb.AppendLine("✔ Запусков читов и утилит очистки в реестре UserAssist и MuiCache не обнаружено.");
			}

			sb.AppendLine(new string('=', 85));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита Windows Shell (UserAssist & MuiCache) успешно скопирован в буфер обмена!", "Экспорт протокола", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
