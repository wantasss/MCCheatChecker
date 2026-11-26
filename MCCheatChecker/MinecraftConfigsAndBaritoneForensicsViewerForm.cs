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
	public class MinecraftConfigsAndBaritoneForensicsViewerForm : Form
	{
		#region Data Models

		public enum ConfigFilterCategory
		{
			All,
			ViolationsOnly,
			BaritoneAndBots,
			OrphanedConfigs,
			OptionsTxtAndGamma,
			Whitelisted
		}

		public class ConfigForensicItem
		{
			public int Index;
			public string SourceEnvironment;
			public string ConfigFileName;
			public ConfigFilterCategory Category;
			public string DetectedCheatParameters;
			public DateTime LastModified;
			public string Verdict;
			public string TargetRule;
			public string FullFilePath;
			public bool IsViolation;
			public bool IsOrphaned;
			public bool IsAllowed;
			public Color RowColor;
			public string Details;
			public string RawSnippet;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ConfigFilterCategory currentFilter = ConfigFilterCategory.All;
		private List<ConfigForensicItem> allItems = new List<ConfigForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiBaritone;
		private Label lblKpiOrphaned;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterBaritone;
		private DarkButton btnFilterOrphaned;
		private DarkButton btnFilterGamma;
		private DarkButton btnFilterSafe;

		private DarkButton btnOpenFile;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public MinecraftConfigsAndBaritoneForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "📁 Форензика конфигураций .minecraft\\config, ботов Baritone и остаточных конфигов — MCCheatChecker";
			Size = new Size(1280, 780);
			MinimumSize = new Size(1040, 600);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "📁 Форензика конфигураций .minecraft\\config, ботов Baritone и остаточных файлов читов";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Аудит .minecraft\\config, папок Baritone/Meteor/Wurst/Expensive: детекция осиротевших конфигов удаленных читов (5.08), чит-параметров (killaura/reach/timer) и эксплойтов options.txt (Gamma 1000).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования конфигураций Minecraft...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(35, 45, 55);
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(18, 62);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			kpiPanel.Size = new Size(680, 50);
			kpiPanel.Location = new Point(topPanel.Width - 696, 50);
			kpiPanel.BackColor = Color.Transparent;

			lblKpiTotal = CreateKpiLabel("Конфигов: 0", Ui.TextPrimary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик читов: 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiBaritone = CreateKpiLabel("Baritone: 0", Ui.AccentOrange, 272, kpiPanel);
			lblKpiOrphaned = CreateKpiLabel("Осиротевших: 0", Ui.AccentPurple, 408, kpiPanel);
			lblKpiSafe = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Filter bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все конфиги", ConfigFilterCategory.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Только улики (5.01)", ConfigFilterCategory.ViolationsOnly, ref bx, filterBar);
			btnFilterBaritone = CreateFilterButton("🤖 Baritone & Боты", ConfigFilterCategory.BaritoneAndBots, ref bx, filterBar);
			btnFilterOrphaned = CreateFilterButton("📄 Осиротевшие (5.08)", ConfigFilterCategory.OrphanedConfigs, ref bx, filterBar);
			btnFilterGamma = CreateFilterButton("⚙️ options.txt (Gamma)", ConfigFilterCategory.OptionsTxtAndGamma, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Белый список", ConfigFilterCategory.Whitelisted, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(220, 26);
			searchBox.Location = new Point(filterBar.Width - 236, 9);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 305, 13);
			filterBar.Controls.Add(lblSearch);

			// Split Container
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 420;
			split.BackColor = Ui.Border;

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.GridLines = true;

			listView.Columns.Add("#", 45, HorizontalAlignment.Center);
			listView.Columns.Add("Окружение / Папка", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Файл конфигурации / Модуль", 240, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Обнаруженные чит-параметры / Значения", 300, HorizontalAlignment.Left);
			listView.Columns.Add("Дата изменения", 130, HorizontalAlignment.Center);
			listView.Columns.Add("Вердикт", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Полный путь к файлу", 260, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.MouseDoubleClick += delegate { OpenFileOrFolder(); };

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📁 Открыть папку / файл конфигурации", null, delegate { OpenFileOrFolder(); });
			contextMenu.Items.Add("📋 Скопировать путь к файлу", null, delegate { CopyFilePath(); });
			contextMenu.Items.Add("📑 Скопировать Discord-досье на улитку", null, delegate { CopyDiscordReport(); });
			contextMenu.Items.Add("🚨 Скопировать команду бана", null, delegate { CopyBanCommand(); });
			listView.ContextMenuStrip = contextMenu;

			split.Panel1.Controls.Add(listView);

			// Bottom Panel
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Fill;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12);

			Label lblDetailHeader = new Label();
			lblDetailHeader.Text = "📄 Детализированный криминалистический анализ содержимого конфигурации:";
			lblDetailHeader.Font = Ui.FontBody(8.5f, true);
			lblDetailHeader.ForeColor = Ui.TextSecondary;
			lblDetailHeader.Dock = DockStyle.Top;
			lblDetailHeader.Height = 22;
			bottomPanel.Controls.Add(lblDetailHeader);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Dock = DockStyle.Fill;
			txtDetails.Font = Ui.FontMono(9f);
			txtDetails.ReadOnly = true;
			bottomPanel.Controls.Add(txtDetails);

			// Button Bar
			Panel btnBar = new Panel();
			btnBar.Dock = DockStyle.Bottom;
			btnBar.Height = 44;
			btnBar.BackColor = Ui.SurfaceAlt;
			btnBar.Padding = new Padding(12, 6, 12, 6);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "📋 Скопировать Discord-отчет";
			btnCopyDiscord.Size = new Size(210, 32);
			btnCopyDiscord.Location = new Point(12, 6);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			btnBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🚨 Скопировать команду бана";
			btnCopyBanCmd.Size = new Size(210, 32);
			btnCopyBanCmd.Location = new Point(230, 6);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			btnBar.Controls.Add(btnCopyBanCmd);

			btnOpenFile = new DarkButton();
			btnOpenFile.Text = "📁 Открыть файл";
			btnOpenFile.Size = new Size(150, 32);
			btnOpenFile.Location = new Point(448, 6);
			btnOpenFile.Click += delegate { OpenFileOrFolder(); };
			btnBar.Controls.Add(btnOpenFile);

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(160, 32);
			btnRescan.Location = new Point(btnBar.Width - 172, 6);
			btnRescan.Click += delegate { StartScan(); };
			btnBar.Controls.Add(btnRescan);

			bottomPanel.Controls.Add(btnBar);
			split.Panel2.Controls.Add(bottomPanel);

			Controls.Add(split);
			Controls.Add(filterBar);
			Controls.Add(topPanel);

			Ui.ApplyDarkScrollbars(this);
			Ui.ApplyDarkTitleBar(this);
		}

		private Label CreateKpiLabel(string text, Color foreColor, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.ForeColor = foreColor;
			lbl.Font = Ui.FontBody(8.5f, true);
			lbl.BackColor = Color.FromArgb(28, 32, 38);
			lbl.TextAlign = ContentAlignment.MiddleCenter;
			lbl.Size = new Size(128, 44);
			lbl.Location = new Point(x, 2);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, ConfigFilterCategory cat, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(160, 30);
			btn.Location = new Point(x, 7);
			x += 166;
			btn.Click += delegate
			{
				currentFilter = cat;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			return btn;
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование конфигураций Minecraft, Baritone и параметров options.txt...";
			lblStatusPill.ForeColor = Ui.AccentTeal;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "";

			if (scanWorker != null && scanWorker.IsBusy) return;

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ConfigForensicItem> list = new List<ConfigForensicItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			// 1. Официальный .minecraft
			string dotMc = Path.Combine(appData, ".minecraft");
			if (Directory.Exists(dotMc))
			{
				ScanMinecraftDirectory(".minecraft (Официальный)", dotMc, list, ref idx);
			}

			// 2. Prism Launcher instances
			string prismDir = Path.Combine(appData, "PrismLauncher\\instances");
			if (Directory.Exists(prismDir))
			{
				ScanMultiInstanceDir("Prism Launcher", prismDir, list, ref idx);
			}

			// 3. MultiMC instances
			string mmcDir = Path.Combine(appData, "MultiMC\\instances");
			if (Directory.Exists(mmcDir))
			{
				ScanMultiInstanceDir("MultiMC", mmcDir, list, ref idx);
			}

			// 4. CurseForge instances
			string cfDir = Path.Combine(userProfile, "curseforge\\minecraft\\Instances");
			if (Directory.Exists(cfDir))
			{
				ScanMultiInstanceDir("CurseForge", cfDir, list, ref idx);
			}

			// 5. Feather client
			string featherDir = Path.Combine(appData, ".feather");
			if (Directory.Exists(featherDir))
			{
				ScanMinecraftDirectory("Feather Client", featherDir, list, ref idx);
			}

			e.Result = list;
		}

		private void ScanMinecraftDirectory(string envName, string mcDir, List<ConfigForensicItem> list, ref int idx)
		{
			// 1. options.txt (Gamma check)
			string optPath = Path.Combine(mcDir, "options.txt");
			if (File.Exists(optPath))
			{
				try
				{
					FileInfo fi = new FileInfo(optPath);
					string content = ReadFileSafe(optPath);
					string gammaStr = ExtractRegexValue(content, @"gamma\s*:\s*([0-9\.]+)");
					double gammaVal = 0.0;
					if (gammaStr != null && double.TryParse(gammaStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gammaVal))
					{
						bool isGammaExploit = gammaVal > 5.0;
						list.Add(new ConfigForensicItem
						{
							Index = idx++,
							SourceEnvironment = envName,
							ConfigFileName = "options.txt (Gamma)",
							Category = isGammaExploit ? ConfigFilterCategory.OptionsTxtAndGamma : ConfigFilterCategory.Whitelisted,
							DetectedCheatParameters = "gamma: " + gammaStr + (isGammaExploit ? " (ЭКСПЛОЙТ FULLBRIGHT)" : " (Штатно)"),
							LastModified = fi.LastWriteTime,
							Verdict = isGammaExploit ? "⚠️ ЭКСПЛОЙТ FULLBRIGHT ЧЕРЕЗ OPTIONS.TXT" : "Легитимный файл настроек options.txt",
							TargetRule = isGammaExploit ? "2.06 (Багоюз/Эксплойт)" : "Разрешено (5.01)",
							FullFilePath = optPath,
							IsViolation = isGammaExploit,
							IsAllowed = !isGammaExploit,
							RowColor = isGammaExploit ? Ui.AccentOrange : Ui.AccentGreen,
							Details = string.Format("Файл настроек Minecraft:\nПуть: {0}\nПараметр gamma: {1}\nСтатус: {2}", optPath, gammaStr, isGammaExploit ? "Аномально высокое значение (Fullbright). В ванилле максимум 1.0." : "В пределах нормы"),
							RawSnippet = "gamma:" + gammaStr
						});
					}
				}
				catch { }
			}

			// 2. Baritone (директория или файлы в корне mcDir)
			string baritoneDir = Path.Combine(mcDir, "baritone");
			if (Directory.Exists(baritoneDir))
			{
				try
				{
					DirectoryInfo di = new DirectoryInfo(baritoneDir);
					FileInfo[] barFiles = di.GetFiles("*.*", SearchOption.AllDirectories);

					list.Add(new ConfigForensicItem
					{
						Index = idx++,
						SourceEnvironment = envName,
						ConfigFileName = "Каталог Baritone (" + barFiles.Length + " файлов)",
						Category = ConfigFilterCategory.BaritoneAndBots,
						DetectedCheatParameters = "Автоматический бот / Pathing / AutoMine",
						LastModified = di.LastWriteTime,
						Verdict = "🚨 ОБНАРУЖЕН БОТ BARITONE (5.01 / 2.06)",
						TargetRule = "5.01 / 2.06 (Автоматизация)",
						FullFilePath = baritoneDir,
						IsViolation = true,
						IsAllowed = false,
						RowColor = Ui.AccentRed,
						Details = string.Format("Обнаружен каталог бота автоматизации Baritone:\nПуть: {0}\nКоличество файлов: {1}\nДата: {2}", baritoneDir, barFiles.Length, di.LastWriteTime),
						RawSnippet = "Baritone Path: " + baritoneDir
					});
				}
				catch { }
			}

			// 3. Каталоги известных чит-клиентов в корне .minecraft
			string[] cheatDirs = new string[]
			{
				"meteor-client", "wurst", "aristois", "liquidbounce", "celestial",
				"expensive", "deadcode", "akrien", "florian", "minced", "neverhook",
				"wild", "catlavan", "breeze", "inertia", "lambda", "kamiblue"
			};

			foreach (string cd in cheatDirs)
			{
				string fullCd = Path.Combine(mcDir, cd);
				if (Directory.Exists(fullCd))
				{
					DirectoryInfo di = new DirectoryInfo(fullCd);
					list.Add(new ConfigForensicItem
					{
						Index = idx++,
						SourceEnvironment = envName,
						ConfigFileName = "Папка чит-клиента: " + cd,
						Category = ConfigFilterCategory.ViolationsOnly,
						DetectedCheatParameters = "Каталог данных чит-клиента " + cd.ToUpper(),
						LastModified = di.LastWriteTime,
						Verdict = string.Format("🚨 КАТАЛОГ ДАННЫХ ЧИТА ({0})", cd.ToUpper()),
						TargetRule = "5.01 (Чит-клиент)",
						FullFilePath = fullCd,
						IsViolation = true,
						IsAllowed = false,
						RowColor = Ui.AccentRed,
						Details = string.Format("В корне .minecraft обнаружен каталог конфигурации чита {0}:\nПуть: {1}\nДата изменения: {2}", cd, fullCd, di.LastWriteTime),
						RawSnippet = "Cheat Directory: " + fullCd
					});
				}
			}

			// 4. Папка .minecraft\config
			string cfgDir = Path.Combine(mcDir, "config");
			if (Directory.Exists(cfgDir))
			{
				ScanConfigFolder(envName, cfgDir, mcDir, list, ref idx);
			}
		}

		private void ScanMultiInstanceDir(string launcherName, string instancesDir, List<ConfigForensicItem> list, ref int idx)
		{
			try
			{
				string[] subDirs = Directory.GetDirectories(instancesDir);
				foreach (string sub in subDirs)
				{
					string instName = Path.GetFileName(sub);
					string dotMcInInstance = Path.Combine(sub, ".minecraft");
					if (!Directory.Exists(dotMcInInstance)) dotMcInInstance = Path.Combine(sub, "minecraft");
					if (!Directory.Exists(dotMcInInstance)) dotMcInInstance = sub;

					ScanMinecraftDirectory(launcherName + " (" + instName + ")", dotMcInInstance, list, ref idx);
				}
			}
			catch { }
		}

		private void ScanConfigFolder(string envName, string configDir, string mcDir, List<ConfigForensicItem> list, ref int idx)
		{
			try
			{
				string modsDir = Path.Combine(mcDir, "mods");
				string[] installedMods = Directory.Exists(modsDir) ? Directory.GetFiles(modsDir, "*.jar") : new string[0];
				string modsCombined = string.Join(" ", installedMods).ToLowerInvariant();

				// Сканируем подкаталоги в config
				string[] subDirs = Directory.GetDirectories(configDir);
				foreach (string sd in subDirs)
				{
					string dirName = Path.GetFileName(sd);
					string cheat = DetectCheatInString(dirName);
					if (!string.IsNullOrEmpty(cheat))
					{
						DirectoryInfo di = new DirectoryInfo(sd);
						bool isModStillInstalled = modsCombined.Contains(cheat);

						string verdict = isModStillInstalled 
							? string.Format("🚨 АКТИВНЫЙ КОНФИГ ЧИТА ({0})", cheat.ToUpper())
							: string.Format("🚨 ОСИРОТЕВШИЙ КОНФИГ ЧИТА (5.08 - Чит {0} был удален перед СС)", cheat.ToUpper());

						string rule = isModStillInstalled ? "5.01 (Чит-клиент)" : "5.08 (Сокрытие улик)";

						list.Add(new ConfigForensicItem
						{
							Index = idx++,
							SourceEnvironment = envName,
							ConfigFileName = "Конфиг-папка: " + dirName,
							Category = isModStillInstalled ? ConfigFilterCategory.ViolationsOnly : ConfigFilterCategory.OrphanedConfigs,
							DetectedCheatParameters = "Чит-модуль: " + cheat.ToUpper(),
							LastModified = di.LastWriteTime,
							Verdict = verdict,
							TargetRule = rule,
							FullFilePath = sd,
							IsViolation = true,
							IsOrphaned = !isModStillInstalled,
							IsAllowed = false,
							RowColor = Ui.AccentRed,
							Details = string.Format("Папка конфигурации чита в .minecraft\\config:\nИмя: {0}\nПуть: {1}\nМод найден в /mods/: {2}\nДата изменения: {3}", dirName, sd, isModStillInstalled ? "Да" : "НЕТ (УДАЛЕН ПЕРЕД СС)", di.LastWriteTime),
							RawSnippet = "Config Folder: " + sd
						});
					}
				}

				// Сканируем файлы в config
				string[] files = Directory.GetFiles(configDir, "*.*");
				foreach (string f in files)
				{
					string fName = Path.GetFileName(f);
					string ext = Path.GetExtension(f).ToLowerInvariant();
					if (ext != ".json" && ext != ".toml" && ext != ".yaml" && ext != ".yml" && ext != ".cfg" && ext != ".txt") continue;

					FileInfo fi = new FileInfo(f);
					string cheatInName = DetectCheatInString(fName);
					string content = "";
					try { content = ReadFileSafe(f); } catch { }

					List<string> foundCheatParams = ScanContentForCheatParams(content);
					bool isCheat = !string.IsNullOrEmpty(cheatInName) || foundCheatParams.Count > 0;

					if (isCheat)
					{
						string cheatLabel = !string.IsNullOrEmpty(cheatInName) ? cheatInName.ToUpper() : "ЧИТ-ПАРАМЕТРЫ";
						string paramsSummary = foundCheatParams.Count > 0 ? string.Join(", ", foundCheatParams.ToArray()) : cheatLabel;

						list.Add(new ConfigForensicItem
						{
							Index = idx++,
							SourceEnvironment = envName,
							ConfigFileName = fName,
							Category = ConfigFilterCategory.ViolationsOnly,
							DetectedCheatParameters = paramsSummary,
							LastModified = fi.LastWriteTime,
							Verdict = string.Format("🚨 ЧИТЕРСКАЯ КОНФИГУРАЦИЯ ({0})", cheatLabel),
							TargetRule = "5.01 (Чит-клиент/Модули)",
							FullFilePath = f,
							IsViolation = true,
							IsAllowed = false,
							RowColor = Ui.AccentRed,
							Details = string.Format("Файл конфигурации с читерскими модулями:\nФайл: {0}\nПуть: {1}\nОбнаруженные модули: {2}\nДата: {3}", fName, f, paramsSummary, fi.LastWriteTime),
							RawSnippet = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
						});
					}
					else
					{
						// Легитимные конфиги (Sodium, Iris, FerriteCore и т.д.)
						list.Add(new ConfigForensicItem
						{
							Index = idx++,
							SourceEnvironment = envName,
							ConfigFileName = fName,
							Category = ConfigFilterCategory.Whitelisted,
							DetectedCheatParameters = "(Стандартные параметры)",
							LastModified = fi.LastWriteTime,
							Verdict = "Легитимный конфигурационный файл мода",
							TargetRule = "Разрешено (Белый список)",
							FullFilePath = f,
							IsViolation = false,
							IsAllowed = true,
							RowColor = Ui.AccentGreen,
							Details = string.Format("Файл конфигурации: {0}\nПуть: {1}\nДата: {2}", fName, f, fi.LastWriteTime),
							RawSnippet = content.Length > 400 ? content.Substring(0, 400) + "..." : content
						});
					}
				}
			}
			catch { }
		}

		private static List<string> ScanContentForCheatParams(string content)
		{
			List<string> hits = new List<string>();
			if (string.IsNullOrEmpty(content)) return hits;

			string[] cheatKeys = new string[]
			{
				"killaura", "aimbot", "triggerbot", "reach", "hitbox", "autoclicker",
				"velocity", "nofall", "freecam", "timer", "noslow", "xray", "esp",
				"tracers", "chestesp", "blockesp", "antiafk", "fastbreak", "autototem"
			};

			foreach (string ck in cheatKeys)
			{
				string pattern = string.Format(@"""{0}""\s*:\s*(?:true|[0-9\.]+)", ck);
				if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
				{
					hits.Add(ck);
				}
				else if (content.IndexOf(ck + "=true", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					hits.Add(ck);
				}
			}

			return hits;
		}

		private static string DetectCheatInString(string str)
		{
			if (string.IsNullOrEmpty(str)) return null;
			string s = str.ToLowerInvariant();

			string[] cheatNames = new string[]
			{
				"meteor", "wurst", "aristois", "liquidbounce", "celestial", "expensive",
				"deadcode", "akrien", "florian", "minced", "neverhook", "wild", "catlavan",
				"breeze", "inertia", "lambda", "kamiblue", "bleachhack", "rusherhack", "future",
				"baritone", "freecam", "xray", "autoclicker", "killaura"
			};

			foreach (string c in cheatNames)
			{
				if (s.Contains(c)) return c;
			}
			return null;
		}

		private static string ReadFileSafe(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		private static string ExtractRegexValue(string input, string pattern)
		{
			if (string.IsNullOrEmpty(input)) return null;
			Match m = Regex.Match(input, pattern, RegexOptions.Multiline);
			if (m.Success && m.Groups.Count > 1)
			{
				return m.Groups[1].Value.Trim();
			}
			return null;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result is List<ConfigForensicItem>)
			{
				allItems = (List<ConfigForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int violations = 0;
			int baritone = 0;
			int orphaned = 0;
			int safe = 0;

			foreach (ConfigForensicItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.Category == ConfigFilterCategory.BaritoneAndBots) baritone++;
				if (it.IsOrphaned) orphaned++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = "Конфигов: " + total;
			lblKpiViolations.Text = "Улик читов: " + violations;
			lblKpiBaritone.Text = "Baritone: " + baritone;
			lblKpiOrphaned.Text = "Осиротевших: " + orphaned;
			lblKpiSafe.Text = "Белый список: " + safe;

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} УЛИК ЧИТ-КОНФИГУРАЦИЙ (5.01 / 5.08)", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Конфигурации чисты: следов Baritone, чит-модулей и осиротевших конфигов не обнаружено";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : "";

			foreach (ConfigForensicItem it in allItems)
			{
				if (currentFilter == ConfigFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == ConfigFilterCategory.BaritoneAndBots && it.Category != ConfigFilterCategory.BaritoneAndBots && !it.IsViolation) continue;
				if (currentFilter == ConfigFilterCategory.OrphanedConfigs && !it.IsOrphaned) continue;
				if (currentFilter == ConfigFilterCategory.OptionsTxtAndGamma && it.Category != ConfigFilterCategory.OptionsTxtAndGamma) continue;
				if (currentFilter == ConfigFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.SourceEnvironment != null && it.SourceEnvironment.ToLowerInvariant().Contains(search))
						|| (it.ConfigFileName != null && it.ConfigFileName.ToLowerInvariant().Contains(search))
						|| (it.DetectedCheatParameters != null && it.DetectedCheatParameters.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search))
						|| (it.FullFilePath != null && it.FullFilePath.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.SourceEnvironment);
				lvi.SubItems.Add(it.ConfigFileName);
				lvi.SubItems.Add(it.Category.ToString());
				lvi.SubItems.Add(it.DetectedCheatParameters);
				lvi.SubItems.Add(it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.FullFilePath);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ConfigForensicItem it = listView.SelectedItems[0].Tag as ConfigForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛИЗИРОВАННЫЙ АНАЛИЗ КОНФИГУРАЦИОННОГО ФАЙЛА MINECRAFT ===");
			sb.AppendLine("Окружение / Клиент: " + it.SourceEnvironment);
			sb.AppendLine("Файл / Модуль: " + it.ConfigFileName);
			sb.AppendLine("Категория: " + it.Category);
			sb.AppendLine("Обнаруженные параметры: " + it.DetectedCheatParameters);
			sb.AppendLine("Дата изменения: " + it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Полный путь: " + it.FullFilePath);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine();
			sb.AppendLine("--- Подробный протокол проверки ---");
			sb.AppendLine(it.Details);
			sb.AppendLine();
			if (!string.IsNullOrEmpty(it.RawSnippet))
			{
				sb.AppendLine("--- Фрагмент содержимого конфигурации ---");
				sb.AppendLine(it.RawSnippet);
			}

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void OpenFileOrFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			ConfigForensicItem it = listView.SelectedItems[0].Tag as ConfigForensicItem;
			if (it == null || string.IsNullOrEmpty(it.FullFilePath)) return;

			try
			{
				if (Directory.Exists(it.FullFilePath))
				{
					Process.Start("explorer.exe", it.FullFilePath);
				}
				else if (File.Exists(it.FullFilePath))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", it.FullFilePath));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Ошибка открытия", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyFilePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			ConfigForensicItem it = listView.SelectedItems[0].Tag as ConfigForensicItem;
			if (it == null || string.IsNullOrEmpty(it.FullFilePath)) return;

			Clipboard.SetText(it.FullFilePath);
			MessageBox.Show("Путь скопирован в буфер:\n" + it.FullFilePath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			ConfigForensicItem it = listView.SelectedItems[0].Tag as ConfigForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ КОНФИГУРАЦИИ MINECRAFT ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Окружение: " + it.SourceEnvironment);
			sb.AppendLine("Файл: " + it.ConfigFileName);
			sb.AppendLine("Параметры: " + it.DetectedCheatParameters);
			sb.AppendLine("Путь: " + it.FullFilePath);
			sb.AppendLine("Дата изменения: " + it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Discord-отчет скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ConfigForensicItem it = listView.SelectedItems[0].Tag as ConfigForensicItem;
			if (it == null) return;

			string cmd;
			if (it.IsOrphaned)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Осиротевший конфиг удаленного чита {1})", targetPlayerName, it.ConfigFileName);
			}
			else if (it.Category == ConfigFilterCategory.BaritoneAndBots)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование ботов (Baritone botting)", targetPlayerName);
			}
			else if (it.Category == ConfigFilterCategory.OptionsTxtAndGamma)
			{
				cmd = string.Format("/ban {0} 7d 2.06 Багоюз (Fullbright gamma 1000 в options.txt)", targetPlayerName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Конфиг чита {1})", targetPlayerName, it.ConfigFileName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
