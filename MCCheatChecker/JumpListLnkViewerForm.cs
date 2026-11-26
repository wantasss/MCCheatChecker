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
	public class JumpListLnkViewerForm : Form
	{
		#region Models

		public enum LnkFilterMode
		{
			All,
			CheatsOnly,
			DeletedOnly,
			UsbOnly,
			CleanersOnly,
			MinecraftMods,
			Whitelist
		}

		public class LnkItem
		{
			public string ShortcutName;
			public string ShortcutPath;
			public string TargetPath;
			public string TargetFileName;
			public long TargetSize;
			public DateTime TargetCreated;
			public DateTime TargetModified;
			public DateTime LnkModified;
			public uint DriveType;
			public bool ExistsOnDisk;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsWhitelisted;
			public bool IsUsb;
			public string Verdict;
			public Color ForeColor;
			public string ViolationRule;
			public string BanReason;
			public string Arguments;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private LnkFilterMode currentFilter = LnkFilterMode.All;
		private List<LnkItem> allItems = new List<LnkItem>();
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
		private DarkButton btnFilterUsb;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterMods;
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

		public JumpListLnkViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			Text = "🔗 Инспектор ярлыков Recent (.lnk) и списков переходов Jump Lists (Форензика запусков)";
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
			lblHeaderTitle.Text = "🔗 Инспектор ярлыков Recent (.lnk) и списков переходов Jump Lists (MS-SHLLINK Forensics)";
			lblHeaderTitle.Font = Ui.FontTitle(11.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(12, 8);
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Криминалистический анализ истории открытия файлов, удаленных читов, запусков со съемных USB-флешек и списков AutomaticDestinations.";
			lblHeaderSubtitle.Font = Ui.FontBody(8f);
			lblHeaderSubtitle.ForeColor = Ui.TextMuted;
			lblHeaderSubtitle.AutoSize = true;
			lblHeaderSubtitle.Location = new Point(13, 30);
			topPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования ярлыков Recent и списков переходов...";
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

			btnFilterAll = CreateFilterButton("Все объекты", LnkFilterMode.All);
			btnFilterCheats = CreateFilterButton("🚨 Читы (5.01)", LnkFilterMode.CheatsOnly);
			btnFilterDeleted = CreateFilterButton("🚨 Удалены перед проверкой (5.08)", LnkFilterMode.DeletedOnly);
			btnFilterUsb = CreateFilterButton("🔌 Запуск с USB-флешки", LnkFilterMode.UsbOnly);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры / Вайперы (5.08)", LnkFilterMode.CleanersOnly);
			btnFilterMods = CreateFilterButton("☕ Моды и .jar", LnkFilterMode.MinecraftMods);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", LnkFilterMode.Whitelist);

			filterRow.Controls.Add(btnFilterAll);
			filterRow.Controls.Add(btnFilterCheats);
			filterRow.Controls.Add(btnFilterDeleted);
			filterRow.Controls.Add(btnFilterUsb);
			filterRow.Controls.Add(btnFilterCleaners);
			filterRow.Controls.Add(btnFilterMods);
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
			lblSearchHint.Text = "🔍 Поиск по имени, пути, диску...";
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
			btnOpenFolder.Size = new Size(170, 25);
			btnOpenFolder.Click += delegate { OpenSelectedInExplorer(); };
			actionRow.Controls.Add(btnOpenFolder);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Location = new Point(856, 2);
			btnCopyBan.Size = new Size(200, 25);
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			actionRow.Controls.Add(btnCopyBan);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📑 Экспорт протокола";
			btnExportReport.Location = new Point(1064, 2);
			btnExportReport.Size = new Size(130, 25);
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
			listView.Columns.Add("Целевой файл / Чит", 220);
			listView.Columns.Add("Дата открытия / Запуска", 150);
			listView.Columns.Add("Статус на диске", 170);
			listView.Columns.Add("Накопитель", 120);
			listView.Columns.Add("Полный путь к целевому файлу", 320);

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
			lblDetailVerdict.Text = "Выберите объект из таблицы выше для просмотра криминалистических улик.";
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
			lblHelp.Text = "💡 Подсказка: Ярлыки в папке Recent создаются Windows автоматически при запуске файла и сохраняются даже после удаления самого чита или извлечения USB.";
			lblHelp.Location = new Point(12, 7);
			lblHelp.AutoSize = true;
			lblHelp.ForeColor = Ui.TextMuted;
			lblHelp.Font = Ui.FontBody(8f);
			bottomBar.Controls.Add(lblHelp);

			Controls.Add(bottomBar);

			UpdateFilterButtonsStyle();
		}

		private DarkButton CreateFilterButton(string text, LnkFilterMode mode)
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
			btnFilterAll.BackColor = (currentFilter == LnkFilterMode.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterCheats.BackColor = (currentFilter == LnkFilterMode.CheatsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterDeleted.BackColor = (currentFilter == LnkFilterMode.DeletedOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterUsb.BackColor = (currentFilter == LnkFilterMode.UsbOnly) ? Ui.AccentOrange : Ui.Surface;
			btnFilterCleaners.BackColor = (currentFilter == LnkFilterMode.CleanersOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterMods.BackColor = (currentFilter == LnkFilterMode.MinecraftMods) ? Ui.AccentTeal : Ui.Surface;
			btnFilterWhitelist.BackColor = (currentFilter == LnkFilterMode.Whitelist) ? Ui.AccentGreen : Ui.Surface;
		}

		#endregion

		#region Scanning Logic

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "⏳ Сканирование ярлыков Recent (.lnk) и Jump Lists...";
			lblStatusPill.ForeColor = Ui.AccentTeal;

			ThreadPool.QueueUserWorkItem(delegate {
				List<LnkItem> results = new List<LnkItem>();
				HashSet<string> seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				try
				{
					// 1. Scan %APPDATA%\Microsoft\Windows\Recent\*.lnk
					string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					string recentDir = Path.Combine(appData, @"Microsoft\Windows\Recent");
					if (Directory.Exists(recentDir))
					{
						ScanLnkDirectory(results, recentDir, seenTargets, SearchOption.TopDirectoryOnly);
					}

					// 2. Scan Desktop and Downloads for .lnk shortcuts
					string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
					string desktop = Path.Combine(userProfile, "Desktop");
					if (Directory.Exists(desktop)) ScanLnkDirectory(results, desktop, seenTargets, SearchOption.TopDirectoryOnly);

					string downloads = Path.Combine(userProfile, "Downloads");
					if (Directory.Exists(downloads)) ScanLnkDirectory(results, downloads, seenTargets, SearchOption.TopDirectoryOnly);

					// 3. Scan Jump Lists (AutomaticDestinations and CustomDestinations)
					string autoDest = Path.Combine(recentDir, "AutomaticDestinations");
					if (Directory.Exists(autoDest)) ScanJumpLists(results, autoDest, seenTargets);

					string custDest = Path.Combine(recentDir, "CustomDestinations");
					if (Directory.Exists(custDest)) ScanJumpLists(results, custDest, seenTargets);
				}
				catch { }

				BeginInvoke(new Action(delegate {
					allItems = results;
					isScanning = false;

					int crit = allItems.Count(i => i.IsCheat || i.IsCleaner);
					int deleted = allItems.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner));

					if (deleted > 0)
					{
						lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ УДАЛЕННЫЕ ЧИТЫ: {0} шт. (Всего подозрительных: {1})", deleted, crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else if (crit > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ СЛЕДЫ ЧИТ-КЛИЕНТОВ: {0} шт.", crit);
						lblStatusPill.ForeColor = Ui.AccentRed;
					}
					else
					{
						lblStatusPill.Text = string.Format("✔ Проанализировано {0} ярлыков и переходов. Следов читов не обнаружено.", allItems.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
					}

					ApplyFilters();
				}));
			});
		}

		private void ScanLnkDirectory(List<LnkItem> results, string dir, HashSet<string> seenTargets, SearchOption opt)
		{
			try
			{
				string[] files = Directory.GetFiles(dir, "*.lnk", opt);
				foreach (string file in files)
				{
					try
					{
						LnkItem item = ParseLnkFile(file);
						if (item != null && !string.IsNullOrEmpty(item.TargetPath))
						{
							if (!seenTargets.Contains(item.TargetPath))
							{
								seenTargets.Add(item.TargetPath);
								results.Add(item);
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private LnkItem ParseLnkFile(string lnkPath)
		{
			try
			{
				byte[] b = File.ReadAllBytes(lnkPath);
				if (b.Length < 76) return null;

				int headerSize = BitConverter.ToInt32(b, 0);
				if (headerSize != 0x4C) return null;

				uint flags = BitConverter.ToUInt32(b, 20);
				long createTime = BitConverter.ToInt64(b, 28);
				long accessTime = BitConverter.ToInt64(b, 36);
				long writeTime = BitConverter.ToInt64(b, 44);
				uint targetSize = BitConverter.ToUInt32(b, 52);

				DateTime dtCreate = (createTime > 0) ? DateTime.FromFileTimeUtc(createTime).ToLocalTime() : DateTime.MinValue;
				DateTime dtWrite = (writeTime > 0) ? DateTime.FromFileTimeUtc(writeTime).ToLocalTime() : DateTime.MinValue;
				DateTime lnkMod = File.GetLastWriteTime(lnkPath);

				int offset = 76;
				if ((flags & 0x01) != 0) // HasLinkTargetIDList
				{
					if (offset + 2 > b.Length) return null;
					ushort idListSize = BitConverter.ToUInt16(b, offset);
					offset += 2 + idListSize;
				}

				string targetPath = "";
				uint driveType = 0;

				if ((flags & 0x02) != 0 && offset + 28 <= b.Length) // HasLinkInfo
				{
					int linkInfoStart = offset;
					int linkInfoSize = BitConverter.ToInt32(b, linkInfoStart);
					int linkInfoHeaderSize = BitConverter.ToInt32(b, linkInfoStart + 4);
					int volIdOffset = BitConverter.ToInt32(b, linkInfoStart + 12);
					int localBasePathOffset = BitConverter.ToInt32(b, linkInfoStart + 16);

					if (volIdOffset > 0 && linkInfoStart + volIdOffset + 16 <= b.Length)
					{
						driveType = BitConverter.ToUInt32(b, linkInfoStart + volIdOffset + 4);
					}

					if (localBasePathOffset > 0 && linkInfoStart + localBasePathOffset < b.Length)
					{
						int p = linkInfoStart + localBasePathOffset;
						int end = p;
						while (end < b.Length && b[end] != 0) end++;
						targetPath = Encoding.Default.GetString(b, p, end - p);
					}

					if (linkInfoHeaderSize >= 0x24 && linkInfoStart + 28 <= b.Length)
					{
						int localBasePathOffsetUnicode = BitConverter.ToInt32(b, linkInfoStart + 28);
						if (localBasePathOffsetUnicode > 0 && linkInfoStart + localBasePathOffsetUnicode + 2 <= b.Length)
						{
							int pu = linkInfoStart + localBasePathOffsetUnicode;
							int endu = pu;
							while (endu + 1 < b.Length && !(b[endu] == 0 && b[endu + 1] == 0)) endu += 2;
							string pathU = Encoding.Unicode.GetString(b, pu, endu - pu);
							if (!string.IsNullOrEmpty(pathU)) targetPath = pathU;
						}
					}
					offset += linkInfoSize;
				}

				if (string.IsNullOrEmpty(targetPath)) return null;

				LnkItem item = new LnkItem();
				item.ShortcutName = Path.GetFileName(lnkPath);
				item.ShortcutPath = lnkPath;
				item.TargetPath = targetPath;
				item.TargetFileName = Path.GetFileName(targetPath);
				item.TargetSize = targetSize;
				item.TargetCreated = dtCreate;
				item.TargetModified = dtWrite;
				item.LnkModified = lnkMod;
				item.DriveType = driveType;
				item.IsUsb = (driveType == 2); // DRIVE_REMOVABLE

				item.ExistsOnDisk = File.Exists(targetPath) || Directory.Exists(targetPath);

				CategorizeLnkItem(item);
				return item;
			}
			catch
			{
				return null;
			}
		}

		private void ScanJumpLists(List<LnkItem> results, string dir, HashSet<string> seenTargets)
		{
			try
			{
				string[] files = Directory.GetFiles(dir, "*.*destinations-ms");
				foreach (string jlFile in files)
				{
					try
					{
						FileInfo fi = new FileInfo(jlFile);
						if (fi.Length > 20971520) continue;

						byte[] bytes = File.ReadAllBytes(jlFile);
						if (bytes.Length < 64) continue;

						string ascii = Encoding.ASCII.GetString(bytes);
						string unicode = Encoding.Unicode.GetString(bytes);

						MatchCollection mcA = Regex.Matches(ascii, @"[a-zA-Z]:\\[^<>""/|?*\x00-\x1F\r\n\t]+?\.[a-zA-Z0-9]{2,5}");
						MatchCollection mcU = Regex.Matches(unicode, @"[a-zA-Z]:\\[^<>""/|?*\x00-\x1F\r\n\t]+?\.[a-zA-Z0-9]{2,5}");

						List<string> candidatePaths = new List<string>();
						foreach (Match m in mcA) candidatePaths.Add(m.Value.Trim());
						foreach (Match m in mcU) candidatePaths.Add(m.Value.Trim());

						foreach (string cPath in candidatePaths)
						{
							if (cPath.Length < 6 || seenTargets.Contains(cPath)) continue;

							string lower = cPath.ToLowerInvariant();
							bool isCheat = CheatPattern.IsMatch(lower);
							bool isCleaner = CleanerPattern.IsMatch(lower);
							bool isMod = lower.EndsWith(".jar") || lower.Contains(".minecraft");

							if (isCheat || isCleaner || isMod)
							{
								seenTargets.Add(cPath);

								LnkItem item = new LnkItem();
								item.ShortcutName = Path.GetFileName(jlFile);
								item.ShortcutPath = jlFile;
								item.TargetPath = cPath;
								item.TargetFileName = Path.GetFileName(cPath);
								item.LnkModified = fi.LastWriteTime;
								item.ExistsOnDisk = File.Exists(cPath) || Directory.Exists(cPath);

								CategorizeLnkItem(item);
								results.Add(item);
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void CategorizeLnkItem(LnkItem item)
		{
			string lowerName = (item.TargetFileName ?? "").ToLowerInvariant();
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
				item.Details = string.Format("Файл {0} является разрешенным ПО согласно регламенту проверок.\r\nПуть: {1}", item.TargetFileName, item.TargetPath);
				return;
			}

			if (isScreenShare)
			{
				item.IsWhitelisted = true;
				item.Verdict = "🛡️ РАЗРЕШЕНО (WHITE LIST: ScreenShare)";
				item.ForeColor = Ui.AccentGreen;
				item.ViolationRule = "ПО для демонстрации экрана";
				item.Details = string.Format("Программа {0} используется для трансляции проверки.\r\nПуть: {1}", item.TargetFileName, item.TargetPath);
				return;
			}

			// Cleaners / Wipers
			if (CleanerPattern.IsMatch(lowerName) || CleanerPattern.IsMatch(lowerPath))
			{
				item.IsCleaner = true;
				item.Verdict = "🚨 КЛИЕНЕР / ВАЙПЕР (5.08)";
				item.ForeColor = Ui.AccentRed;
				item.ViolationRule = "Пункт 5.08 (Утилита очистки следов и журналов)";
				item.BanReason = string.Format("Запуск утилиты очистки {0} (по ярлыку {1:dd.MM.yyyy HH:mm})", item.TargetFileName, item.LnkModified);
				item.Details = string.Format(
					"🚨 ОБНАРУЖЕНА УТИЛИТА ОЧИСТКИ СЛЕДОВ / ВАЙПЕР!\r\n" +
					"• Файл: {0}\r\n" +
					"• Путь: {1}\r\n" +
					"• Статус на диске: {2}\r\n" +
					"• Время запуска: {3:dd.MM.yyyy HH:mm:ss}\r\n" +
					"• Вердикт: Использование ПО для сокрытия улик (Регламент 5.08 — Бан 30 дней).",
					item.TargetFileName, item.TargetPath, item.ExistsOnDisk ? "Существует" : "Удален с диска", item.LnkModified);
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
					item.BanReason = string.Format("Удаление чита {0} перед проверкой (LNK {1:dd.MM.yyyy HH:mm})", item.TargetFileName, item.LnkModified);
					item.Details = string.Format(
						"🚨 УЛИКА СОКРЫТИЯ ЧИТА: ФАЙЛ БЫЛ УДАЛЕН ПЕРЕД ПРОВЕРКОЙ!\r\n" +
						"• Файл: {0}\r\n" +
						"• Исходный путь: {1}\r\n" +
						"• Время запуска/обращения: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Статус на диске: УДАЛЕН (Файл отсутствует по исходному адресу!).\r\n" +
						"• Вердикт: Сокрытие следов и удаление запрещенного софта (Регламент 5.08 — Бан 30 дней).",
						item.TargetFileName, item.TargetPath, item.LnkModified);
				}
				else if (item.IsUsb)
				{
					item.Verdict = "🚨 ЗАПУСК ЧИТА С USB-ФЛЕШКИ (5.01)";
					item.ForeColor = Ui.AccentRed;
					item.ViolationRule = "Пункт 5.01 (Запуск чита со съемного носителя)";
					item.BanReason = string.Format("Запуск чита {0} с USB-флешки", item.TargetFileName);
					item.Details = string.Format(
						"🚨 ОБНАРУЖЕН ЗАПУСК ЧИТА СО СЪЕМНОГО USB-НАКОПИТЕЛЯ!\r\n" +
						"• Файл: {0}\r\n" +
						"• Путь на съемном диске: {1}\r\n" +
						"• Время запуска: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Вердикт: Использование читов по Регламенту 5.01.",
						item.TargetFileName, item.TargetPath, item.LnkModified);
				}
				else
				{
					item.Verdict = "🚨 ЧИТ-КЛИЕНТ / СОФТ (5.01)";
					item.ForeColor = Ui.AccentRed;
					item.ViolationRule = "Пункт 5.01 (Использование читов)";
					item.BanReason = string.Format("Запуск чита {0} (LNK {1:dd.MM.yyyy HH:mm})", item.TargetFileName, item.LnkModified);
					item.Details = string.Format(
						"🚨 ОБНАРУЖЕН ЗАПУСК ЧИТ-КЛИЕНТА!\r\n" +
						"• Файл: {0}\r\n" +
						"• Путь на диске: {1}\r\n" +
						"• Время запуска: {2:dd.MM.yyyy HH:mm:ss}\r\n" +
						"• Размер: {3:N0} байт\r\n" +
						"• Вердикт: Запрещенное ПО по Регламенту 5.01.",
						item.TargetFileName, item.TargetPath, item.LnkModified, item.TargetSize);
				}
				return;
			}

			// Minecraft mods
			if (lowerName.EndsWith(".jar") || lowerPath.Contains(".minecraft"))
			{
				item.Verdict = "☕ МОД / ФАЙЛ MINECRAFT";
				item.ForeColor = Ui.AccentTeal;
				item.ViolationRule = "Файлы игры";
				item.Details = string.Format("Файл модификации или ресурса игры.\r\nПуть: {0}\r\nСтатус на диске: {1}", item.TargetPath, item.ExistsOnDisk ? "Существует" : "Удален");
				return;
			}

			item.Verdict = "ℹ️ ЯРЛЫК RECENT";
			item.ForeColor = Ui.TextMuted;
			item.ViolationRule = "Общий файл";
			item.Details = string.Format("Ярлык Recent / Список переходов.\r\nФайл: {0}\r\nПуть: {1}", item.TargetFileName, item.TargetPath);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilters()
		{
			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			List<LnkItem> filtered = allItems.Where(item => {
				// Mode filter
				switch (currentFilter)
				{
					case LnkFilterMode.CheatsOnly:
						if (!item.IsCheat) return false;
						break;
					case LnkFilterMode.DeletedOnly:
						if (item.ExistsOnDisk || (!item.IsCheat && !item.IsCleaner)) return false;
						break;
					case LnkFilterMode.UsbOnly:
						if (!item.IsUsb) return false;
						break;
					case LnkFilterMode.CleanersOnly:
						if (!item.IsCleaner) return false;
						break;
					case LnkFilterMode.MinecraftMods:
						if (!(item.TargetFileName ?? "").ToLowerInvariant().EndsWith(".jar") && !(item.TargetPath ?? "").ToLowerInvariant().Contains(".minecraft")) return false;
						break;
					case LnkFilterMode.Whitelist:
						if (!item.IsWhitelisted) return false;
						break;
				}

				// Search query
				if (!string.IsNullOrEmpty(query))
				{
					bool matchTarget = (item.TargetPath ?? "").ToLowerInvariant().Contains(query);
					bool matchName = (item.TargetFileName ?? "").ToLowerInvariant().Contains(query);
					bool matchShortcut = (item.ShortcutName ?? "").ToLowerInvariant().Contains(query);
					bool matchVerdict = (item.Verdict ?? "").ToLowerInvariant().Contains(query);
					bool matchDetails = (item.Details ?? "").ToLowerInvariant().Contains(query);
					if (!matchTarget && !matchName && !matchShortcut && !matchVerdict && !matchDetails) return false;
				}

				return true;
			}).ToList();

			// Sort order: Cheats / Cleaners first, then deleted, then by date
			filtered = filtered.OrderByDescending(i => i.IsCheat || i.IsCleaner)
				.ThenByDescending(i => !i.ExistsOnDisk)
				.ThenByDescending(i => i.LnkModified)
				.ToList();

			listView.BeginUpdate();
			listView.Items.Clear();

			foreach (LnkItem item in filtered)
			{
				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.ForeColor = item.ForeColor;
				lvi.SubItems.Add(item.TargetFileName ?? "");
				lvi.SubItems.Add((item.LnkModified > DateTime.MinValue) ? item.LnkModified.ToString("dd.MM.yyyy HH:mm:ss") : "-");
				lvi.SubItems.Add(item.ExistsOnDisk ? "[Существует на диске]" : "[🚨 УДАЛЕН С ДИСКА]");
				lvi.SubItems.Add(item.IsUsb ? "🔌 USB Flash" : "Локальный");
				lvi.SubItems.Add(item.TargetPath ?? "");
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

			LnkItem item = listView.SelectedItems[0].Tag as LnkItem;
			if (item == null) return;

			lblDetailVerdict.Text = item.Verdict;
			lblDetailVerdict.ForeColor = item.ForeColor;
			lblDetailRule.Text = "Регламент: " + (item.ViolationRule ?? "Общий аудит");

			if (item.IsCheat || item.IsCleaner)
			{
				string reason = !string.IsNullOrEmpty(item.BanReason) ? item.BanReason : item.TargetFileName;
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
				LnkItem firstCrit = allItems.FirstOrDefault(i => i.IsCheat || i.IsCleaner);
				if (firstCrit != null)
				{
					string ruleCode = firstCrit.IsCleaner || !firstCrit.ExistsOnDisk ? "5.08" : "5.01";
					string ban = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, firstCrit.BanReason ?? firstCrit.TargetFileName);
					Clipboard.SetText(ban);
					MessageBox.Show("Скопирована команда бана за нарушение по ярлыкам LNK:\r\n\r\n" + ban, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				MessageBox.Show("Выберите чит-файл из списка для копирования команды бана.", "Ярлыки Recent", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			LnkItem sel = listView.SelectedItems[0].Tag as LnkItem;
			if (sel != null)
			{
				string ruleCode = sel.IsCleaner || !sel.ExistsOnDisk ? "5.08" : "5.01";
				string reason = !string.IsNullOrEmpty(sel.BanReason) ? sel.BanReason : sel.TargetFileName;
				string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, ruleCode, reason);
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void OpenSelectedInExplorer()
		{
			if (listView.SelectedItems.Count == 0) return;
			LnkItem sel = listView.SelectedItems[0].Tag as LnkItem;
			if (sel == null) return;

			if (File.Exists(sel.TargetPath))
			{
				Process.Start("explorer.exe", "/select,\"" + sel.TargetPath + "\"");
			}
			else if (Directory.Exists(sel.TargetPath))
			{
				Process.Start("explorer.exe", "\"" + sel.TargetPath + "\"");
			}
			else if (File.Exists(sel.ShortcutPath))
			{
				Process.Start("explorer.exe", "/select,\"" + sel.ShortcutPath + "\"");
			}
			else
			{
				MessageBox.Show("Целевой файл удален с диска!\r\nПуть: " + sel.TargetPath, "Файл не существует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void ExportProtocolToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АУДИТА ЯРЛЫКОВ RECENT И JUMP LISTS ===");
			sb.AppendLine(string.Format("👤 Проверяемый игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("📅 Дата и время анализа: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Всего записей в анализе: {0} (Читов: {1}, Удаленных перед проверкой: {2})",
				allItems.Count, allItems.Count(i => i.IsCheat), allItems.Count(i => !i.ExistsOnDisk && (i.IsCheat || i.IsCleaner))));
			sb.AppendLine(new string('-', 85));

			foreach (LnkItem item in allItems.Where(i => i.IsCheat || i.IsCleaner))
			{
				sb.AppendLine(string.Format("[{0}] {1}", item.Verdict, item.TargetFileName));
				sb.AppendLine(string.Format("   Целевой путь: {0}", item.TargetPath));
				sb.AppendLine(string.Format("   Статус на диске: {0} | Накопитель: {1}", item.ExistsOnDisk ? "Существует" : "УДАЛЕН ПЕРЕД ПРОВЕРКОЙ", item.IsUsb ? "USB Flash" : "Локальный"));
				sb.AppendLine(string.Format("   Время запуска/изменения: {0:dd.MM.yyyy HH:mm:ss}", item.LnkModified));
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
				sb.AppendLine("✔ Запусков читов и утилит очистки в ярлыках Recent и списках Jump Lists не обнаружено.");
			}

			sb.AppendLine(new string('=', 85));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита Recent LNK & Jump Lists успешно скопирован в буфер обмена!", "Экспорт протокола", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
