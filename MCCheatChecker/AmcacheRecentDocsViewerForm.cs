using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class AmcacheRecentDocsViewerForm : Form
	{
		#region Models

		public enum AmcacheFilterMode
		{
			All,
			CheatsOnly,
			DeletedOnly,
			CleanersOnly,
			MinecraftOnly,
			Whitelist
		}

		public class AmcacheEntry
		{
			public string Source; // "RecentDocs (.jar)", "RecentDocs (.exe)", "BAM (Background Activity)", "DAM"
			public string FullPath;
			public string FileName;
			public string Extension;
			public DateTime ExecutionTime;
			public bool ExistsOnDisk;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsMinecraft;
			public bool IsWhitelisted;
			public string Status;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private AmcacheFilterMode currentFilter = AmcacheFilterMode.All;
		private List<AmcacheEntry> allEntries = new List<AmcacheEntry>();
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
		private DarkButton btnSearchInHunter;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger)",
			RegexOptions.Compiled);

		private static readonly Regex CleanerRegex = new Regex(
			@"(?i)(cleaner|wiper|reghax|bleachbit|ccleaner|fsutil|usncleaner|journalcleaner|clearlogs|clean_history|wipetraces|vanish|selfdelete|delete_logs|antiscreen|clean_pc)",
			RegexOptions.Compiled);

		private static readonly string[] WhitelistTools = new string[]
		{
			"anydesk", "rudesktop", "discord", "telegram", "obs64", "steam",
			"opautoclicker", "op auto clicker", "speedautoclicker", "tinytask", "x-mouse", "xmouse", "autohotkey", "clickermann", "keyran",
			"lunarclient", "badlion", "feather", "tlauncher", "curseforge", "prism", "modrinth",
			"chrome", "firefox", "msedge", "opera", "yandex", "brave"
		};

		#endregion

		#region UI Initialization

		public AmcacheRecentDocsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит истории запуска и открытия файлов: RecentDocs, BAM & DAM — MCCheatChecker";
			this.Size = new Size(1180, 730);
			this.MinimumSize = new Size(960, 580);
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
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "📂 Криминалистический аудит: RecentDocs MRU & Журнал ядра BAM/DAM";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Извлечение записей RecentDocs (.jar, .exe, .dll, .cfg) и журнала активности ядра Windows (BAM) с детекцией удаленных файлов (5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование реестра...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(240, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 260, 22);
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
			btnFilterAll = CreateFilterButton("Все записи", AmcacheFilterMode.All, ref bx, filterBar);
			btnFilterCheats = CreateFilterButton("🚨 Читы (5.01)", AmcacheFilterMode.CheatsOnly, ref bx, filterBar);
			btnFilterDeleted = CreateFilterButton("🗑️ Удаленные (5.08)", AmcacheFilterMode.DeletedOnly, ref bx, filterBar);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры (5.08)", AmcacheFilterMode.CleanersOnly, ref bx, filterBar);
			btnFilterMinecraft = CreateFilterButton("🎮 Minecraft / JAR", AmcacheFilterMode.MinecraftOnly, ref bx, filterBar);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", AmcacheFilterMode.Whitelist, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 230, 8);
			searchBox.Size = new Size(215, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 285, 12);
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
			btnCopyBan.Size = new Size(220, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Открыть папку файла";
			btnOpenFolder.Font = Ui.FontBody(9f, false);
			btnOpenFolder.Location = new Point(242, 10);
			btnOpenFolder.Size = new Size(180, 30);
			btnOpenFolder.Click += BtnOpenFolder_Click;
			bottomBar.Controls.Add(btnOpenFolder);

			btnSearchInHunter = new DarkButton();
			btnSearchInHunter.Text = "🔍 Поиск в Hunter (Ctrl+Q)";
			btnSearchInHunter.Font = Ui.FontBody(9f, false);
			btnSearchInHunter.Location = new Point(430, 10);
			btnSearchInHunter.Size = new Size(185, 30);
			btnSearchInHunter.Click += delegate
			{
				try
				{
					FastCheatHunterForm hunter = new FastCheatHunterForm(targetPlayerName);
					hunter.Show(this);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};
			bottomBar.Controls.Add(btnSearchInHunter);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📄 Экспорт отчета в TXT";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(623, 10);
			btnExportReport.Size = new Size(175, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 Обновить (F5)";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 145, 10);
			btnRefreshNow.Size = new Size(130, 30);
			btnRefreshNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			bottomBar.Controls.Add(btnRefreshNow);

			// SplitContainer
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
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

			listView.Columns.Add("Источник", 160);
			listView.Columns.Add("Имя файла / Путь", 340);
			listView.Columns.Add("Статус на диске", 160);
			listView.Columns.Add("Время активности", 150);
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
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ ДАННЫЕ И НАЗНАЧЕНИЕ ФАЙЛА:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите файл из таблицы для детального анализа.";
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

		private DarkButton CreateFilterButton(string text, AmcacheFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == AmcacheFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(140, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 146;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == AmcacheFilterMode.All);
			btnFilterCheats.AccentStyle = (currentFilter == AmcacheFilterMode.CheatsOnly);
			btnFilterDeleted.AccentStyle = (currentFilter == AmcacheFilterMode.DeletedOnly);
			btnFilterCleaners.AccentStyle = (currentFilter == AmcacheFilterMode.CleanersOnly);
			btnFilterMinecraft.AccentStyle = (currentFilter == AmcacheFilterMode.MinecraftOnly);
			btnFilterWhitelist.AccentStyle = (currentFilter == AmcacheFilterMode.Whitelist);
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
				List<AmcacheEntry> results = new List<AmcacheEntry>();

				// 1. Сканирование RecentDocs (HKCU)
				ScanRecentDocs(results);

				// 2. Сканирование BAM / DAM (HKLM)
				ScanBamDam(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int cheatCount = results.Count(delegate(AmcacheEntry x) { return x.IsCheat; });
					int deletedCount = results.Count(delegate(AmcacheEntry x) { return !x.ExistsOnDisk && (x.IsCheat || x.IsCleaner); });

					if (deletedCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УДАЛЕННЫХ ЧИТОВ (5.08): {0}", deletedCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else if (cheatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНО ЧИТОВ (5.01): {0}", cheatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = "🛡️ Артефакты читов не найдены";
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanRecentDocs(List<AmcacheEntry> results)
		{
			try
			{
				using (RegistryKey recentRoot = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs"))
				{
					if (recentRoot == null) return;

					// Сканирование корневого ключа RecentDocs
					ProcessRecentDocsKey(recentRoot, "RecentDocs (Корень)", results);

					// Сканирование подразделов по расширениям
					string[] subKeyNames = recentRoot.GetSubKeyNames();
					foreach (string subName in subKeyNames)
					{
						using (RegistryKey subKey = recentRoot.OpenSubKey(subName))
						{
							if (subKey != null)
							{
								string srcName = string.Format("RecentDocs ({0})", subName);
								ProcessRecentDocsKey(subKey, srcName, results);
							}
						}
					}
				}
			}
			catch { }
		}

		private void ProcessRecentDocsKey(RegistryKey key, string sourceLabel, List<AmcacheEntry> results)
		{
			string[] valueNames = key.GetValueNames();
			foreach (string valName in valueNames)
			{
				if (valName.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;

				try
				{
					object val = key.GetValue(valName);
					byte[] rawBytes = val as byte[];
					if (rawBytes == null || rawBytes.Length < 4) continue;

					string fileName = ExtractUnicodeStringFromBytes(rawBytes);
					if (string.IsNullOrEmpty(fileName) || fileName.Length < 2) continue;

					// Исключаем системные мусорные строки
					if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 && !fileName.Contains("\\"))
					{
						// Если есть недопустимые символы и это не путь, пробуем очистить
						fileName = Regex.Replace(fileName, @"[^\w\.\-\s\(\)\[\]]", "");
						if (string.IsNullOrEmpty(fileName)) continue;
					}

					AnalyzeAndAddEntry(sourceLabel, fileName, DateTime.MinValue, results);
				}
				catch { }
			}
		}

		private string ExtractUnicodeStringFromBytes(byte[] bytes)
		{
			try
			{
				// Строка в RecentDocs хранится в формате UTF-16LE, заканчивающаяся 0x00 0x00
				int nullIdx = -1;
				for (int i = 0; i < bytes.Length - 1; i += 2)
				{
					if (bytes[i] == 0 && bytes[i + 1] == 0)
					{
						nullIdx = i;
						break;
					}
				}

				if (nullIdx <= 0)
				{
					nullIdx = Math.Min(bytes.Length, 260 * 2);
				}

				string str = Encoding.Unicode.GetString(bytes, 0, nullIdx).Trim();
				return str;
			}
			catch
			{
				return string.Empty;
			}
		}

		private void ScanBamDam(List<AmcacheEntry> results)
		{
			// Аудит BAM / DAM в HKLM
			string[] hivePaths = new string[]
			{
				@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\dam\State\UserSettings"
			};

			foreach (string hivePath in hivePaths)
			{
				try
				{
					using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(hivePath))
					{
						if (baseKey == null) continue;
						string[] userSids = baseKey.GetSubKeyNames();

						foreach (string sid in userSids)
						{
							using (RegistryKey sidKey = baseKey.OpenSubKey(sid))
							{
								if (sidKey == null) continue;
								string srcLabel = hivePath.Contains("bam") ? "BAM (Активность ядра)" : "DAM (Активность ядра)";

								string[] appPaths = sidKey.GetValueNames();
								foreach (string rawPath in appPaths)
								{
									if (rawPath.Equals("SequenceNumber", StringComparison.OrdinalIgnoreCase) ||
										rawPath.Equals("Version", StringComparison.OrdinalIgnoreCase))
										continue;

									try
									{
										object val = sidKey.GetValue(rawPath);
										byte[] bytes = val as byte[];
										DateTime execTime = DateTime.MinValue;

										if (bytes != null && bytes.Length >= 8)
										{
											long fileTime = BitConverter.ToInt64(bytes, 0);
											if (fileTime > 0)
											{
												try
												{
													execTime = DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
												}
												catch { }
											}
										}

										// Преобразуем имя устройства \Device\HarddiskVolumeX в букву диска
										string cleanPath = ConvertDevicePathToDosPath(rawPath);
										AnalyzeAndAddEntry(srcLabel, cleanPath, execTime, results);
									}
									catch { }
								}
							}
						}
					}
				}
				catch { }
			}
		}

		private string ConvertDevicePathToDosPath(string devicePath)
		{
			if (string.IsNullOrEmpty(devicePath)) return string.Empty;

			// Если путь начинается с \Device\HarddiskVolume
			if (devicePath.StartsWith(@"\Device\HarddiskVolume", StringComparison.OrdinalIgnoreCase))
			{
				int slashIdx = devicePath.IndexOf('\\', 22);
				if (slashIdx > 0)
				{
					// Заменяем на системный диск C: в 99% случаев
					string remainder = devicePath.Substring(slashIdx);
					string sysDrive = Path.GetPathRoot(Environment.SystemDirectory);
					if (sysDrive.EndsWith("\\")) sysDrive = sysDrive.Substring(0, sysDrive.Length - 1);
					return sysDrive + remainder;
				}
			}

			return devicePath;
		}

		private void AnalyzeAndAddEntry(string source, string filePathOrName, DateTime execTime, List<AmcacheEntry> results)
		{
			if (string.IsNullOrEmpty(filePathOrName)) return;

			string fileName = Path.GetFileName(filePathOrName);
			if (string.IsNullOrEmpty(fileName)) fileName = filePathOrName;

			string ext = Path.GetExtension(fileName).ToLowerInvariant();
			string lowerName = fileName.ToLowerInvariant();
			string lowerPath = filePathOrName.ToLowerInvariant();

			bool isWhitelisted = false;
			foreach (string wl in WhitelistTools)
			{
				if (lowerName.Contains(wl) || lowerPath.Contains(wl))
				{
					isWhitelisted = true;
					break;
				}
			}

			// Проверка разрешенных визуальных модификаций и модов
			if (!isWhitelisted && CheatDatabases.AllowedVisuals != null)
			{
				foreach (string vis in CheatDatabases.AllowedVisuals)
				{
					if (lowerName.Contains(vis.ToLowerInvariant()))
					{
						isWhitelisted = true;
						break;
					}
				}
			}

			bool isCheat = !isWhitelisted && (CheatRegex.IsMatch(lowerName) || CheatRegex.IsMatch(lowerPath));
			bool isCleaner = !isWhitelisted && (CleanerRegex.IsMatch(lowerName) || CleanerRegex.IsMatch(lowerPath));
			bool isMinecraft = lowerName.Contains("minecraft") || lowerName.Contains("javaw") || ext == ".jar";

			// Проверка физического присутствия на диске
			bool exists = false;
			string resolvedPath = filePathOrName;

			if (File.Exists(filePathOrName))
			{
				exists = true;
				resolvedPath = filePathOrName;
			}
			else
			{
				// Пробуем искать в стандартных каталогах (Downloads, Desktop, .minecraft, Temp)
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string[] probeDirs = new string[]
				{
					Path.Combine(userProfile, "Downloads"),
					Path.Combine(userProfile, "Desktop"),
					Path.Combine(appData, @".minecraft\mods"),
					Path.Combine(appData, ".minecraft"),
					Path.GetTempPath()
				};

				foreach (string dir in probeDirs)
				{
					if (Directory.Exists(dir))
					{
						string probe = Path.Combine(dir, fileName);
						if (File.Exists(probe))
						{
							exists = true;
							resolvedPath = probe;
							break;
						}
					}
				}
			}

			AmcacheEntry entry = new AmcacheEntry();
			entry.Source = source;
			entry.FullPath = resolvedPath;
			entry.FileName = fileName;
			entry.Extension = ext;
			entry.ExecutionTime = execTime;
			entry.ExistsOnDisk = exists;
			entry.IsCheat = isCheat;
			entry.IsCleaner = isCleaner;
			entry.IsMinecraft = isMinecraft;
			entry.IsWhitelisted = isWhitelisted;

			if (isWhitelisted)
			{
				entry.Status = exists ? "🛡️ Разрешенная программа (на диске)" : "🛡️ Разрешенная программа";
				entry.RuleViolation = "-";
				entry.BanReason = "-";
				entry.ForeColor = Ui.AccentGreen;
				entry.Details = string.Format("Файл: {0}\nИсточник: {1}\nСтатус: Входит в официальный белый список сервера (Screenshare / Легитимное ПО).", fileName, source);
			}
			else if (isCheat && !exists)
			{
				entry.Status = "🚨 ЧИТ УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)";
				entry.RuleViolation = "5.08";
				entry.BanReason = string.Format("Удаление чита {0} перед проверкой (обнаружено в {1})", fileName, source);
				entry.ForeColor = Ui.AccentRed;
				entry.Details = string.Format("ФАЙЛ ЧИТА УДАЛЕН С ДИСКА!\nИмя файла: {0}\nИсходный путь: {1}\nИсточник улик: {2}\nВремя фиксации активности: {3}\nВердикт: Игрок открывал или запускал запрещенный софт ({0}), после чего удалил его перед вызовом на проверку.\nЭто прямое нарушение пункта регламента 5.08 (Удаление софта/улик перед проверкой).",
					fileName, resolvedPath, source, execTime == DateTime.MinValue ? "Не указано" : execTime.ToString("yyyy-MM-dd HH:mm:ss"));
			}
			else if (isCheat && exists)
			{
				entry.Status = "🚨 НАЙДЕН ЧИТ-КЛИЕНТ / ИНЖЕКТОР (5.01)";
				entry.RuleViolation = "5.01";
				entry.BanReason = string.Format("Использование читов: {0}", fileName);
				entry.ForeColor = Ui.AccentRed;
				entry.Details = string.Format("ОБНАРУЖЕН ЗАПРЕЩЕННЫЙ СОФТ НА ДИСКЕ!\nИмя файла: {0}\nПуть: {1}\nИсточник улик: {2}\nВремя активности: {3}\nНарушение: Пункт регламента 5.01 (Использование стороннего запрещенного ПО / читов).",
					fileName, resolvedPath, source, execTime == DateTime.MinValue ? "Не указано" : execTime.ToString("yyyy-MM-dd HH:mm:ss"));
			}
			else if (isCleaner)
			{
				entry.Status = exists ? "🚨 ПРОГРАММА ОЧИСТКИ СЛЕДОВ (5.08)" : "🚨 КЛИНЕР БЫЛ ЗАПУЩЕН И УДАЛЕН (5.08)";
				entry.RuleViolation = "5.08";
				entry.BanReason = string.Format("Использование программы очистки следов {0}", fileName);
				entry.ForeColor = Ui.AccentRed;
				entry.Details = string.Format("ОБНАРУЖЕНА УТИЛИТА ЗАТИРАНИЯ СЛЕДОВ / ОЧИСТКИ!\nИмя: {0}\nПуть: {1}\nИсточник: {2}\nСтатус на диске: {3}\nНарушение: Пункт 5.08 (Использование ПО для сокрытия улик).",
					fileName, resolvedPath, source, exists ? "Присутствует" : "Удален с диска");
			}
			else if (ext == ".jar" || isMinecraft)
			{
				entry.Status = exists ? "🎮 Minecraft JAR / Мод" : "Файл JAR удален или перемещен";
				entry.RuleViolation = "-";
				entry.BanReason = "-";
				entry.ForeColor = exists ? Ui.AccentTeal : Ui.TextSecondary;
				entry.Details = string.Format("Файл модификации или ядра игры: {0}\nПуть: {1}\nИсточник: {2}", fileName, resolvedPath, source);
			}
			else
			{
				entry.Status = exists ? "Обычный файл (на диске)" : "Файл отсутствует на диске";
				entry.RuleViolation = "-";
				entry.BanReason = "-";
				entry.ForeColor = Ui.TextSecondary;
				entry.Details = string.Format("Файл: {0}\nПуть: {1}\nИсточник: {2}", fileName, resolvedPath, source);
			}

			results.Add(entry);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (AmcacheEntry entry in allEntries)
			{
				if (currentFilter == AmcacheFilterMode.CheatsOnly && !entry.IsCheat) continue;
				if (currentFilter == AmcacheFilterMode.DeletedOnly && entry.ExistsOnDisk) continue;
				if (currentFilter == AmcacheFilterMode.CleanersOnly && !entry.IsCleaner) continue;
				if (currentFilter == AmcacheFilterMode.MinecraftOnly && !entry.IsMinecraft && entry.Extension != ".jar") continue;
				if (currentFilter == AmcacheFilterMode.Whitelist && !entry.IsWhitelisted) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (entry.Source != null && entry.Source.ToLowerInvariant().Contains(search)) ||
								   (entry.FileName != null && entry.FileName.ToLowerInvariant().Contains(search)) ||
								   (entry.FullPath != null && entry.FullPath.ToLowerInvariant().Contains(search)) ||
								   (entry.Status != null && entry.Status.ToLowerInvariant().Contains(search)) ||
								   (entry.Details != null && entry.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem item = new ListViewItem(entry.Source);
				item.SubItems.Add(entry.FullPath);
				item.SubItems.Add(entry.ExistsOnDisk ? "✅ Присутствует" : "❌ Удален");
				item.SubItems.Add(entry.ExecutionTime == DateTime.MinValue ? "—" : entry.ExecutionTime.ToString("yyyy-MM-dd HH:mm:ss"));
				item.SubItems.Add(entry.Status);
				item.ForeColor = entry.ForeColor;
				item.Tag = entry;

				listView.Items.Add(item);
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

			AmcacheEntry entry = listView.SelectedItems[0].Tag as AmcacheEntry;
			if (entry == null) return;

			lblDetailVerdict.Text = entry.Status;
			lblDetailVerdict.ForeColor = entry.ForeColor;

			if (!string.IsNullOrEmpty(entry.RuleViolation) && entry.RuleViolation != "-")
			{
				lblDetailRule.Text = string.Format("⚖️ Пункт регламента: {0} ({1})", entry.RuleViolation, entry.BanReason);
				lblDetailBanCommand.Text = string.Format("📋 Команда: /ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason);
			}
			else
			{
				lblDetailRule.Text = "🛡️ Нарушений не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = entry.Details ?? "Нет дополнительных сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Файл не выбран";
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
				MessageBox.Show("Выберите запись с нарушением из таблицы для копирования команды бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			AmcacheEntry entry = listView.SelectedItems[0].Tag as AmcacheEntry;
			if (entry == null || string.IsNullOrEmpty(entry.RuleViolation) || entry.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи не предусмотрено наказание (нарушений нет).", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана успешно скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenFolder_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			AmcacheEntry entry = listView.SelectedItems[0].Tag as AmcacheEntry;
			if (entry == null) return;

			try
			{
				if (File.Exists(entry.FullPath))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", entry.FullPath));
				}
				else
				{
					string dir = Path.GetDirectoryName(entry.FullPath);
					if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					{
						Process.Start("explorer.exe", string.Format("\"{0}\"", dir));
					}
					else
					{
						MessageBox.Show("Файл или директория не существует на диске (возможно, удален):\n" + entry.FullPath, "Файл отсутствует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть каталог: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("RecentDocs_BAM_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ: RECENTDOCS & BAM ЖУРНАЛ АКТИВНОСТИ");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (AmcacheEntry entry in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1} (Диск: {2})", entry.Source, entry.FileName, entry.ExistsOnDisk ? "Присутствует" : "УДАЛЕН"));
						sb.AppendLine(string.Format("Полный путь: {0}", entry.FullPath));
						sb.AppendLine(string.Format("Время:       {0}", entry.ExecutionTime == DateTime.MinValue ? "Не указано" : entry.ExecutionTime.ToString("yyyy-MM-dd HH:mm:ss")));
						sb.AppendLine(string.Format("Вердикт:     {0}", entry.Status));
						if (!string.IsNullOrEmpty(entry.RuleViolation) && entry.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", entry.RuleViolation, entry.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", entry.Details));
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
