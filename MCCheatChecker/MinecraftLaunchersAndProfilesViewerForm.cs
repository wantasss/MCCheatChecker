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
	public class MinecraftLaunchersAndProfilesViewerForm : Form
	{
		#region Data Models

		public enum LauncherFilterCategory
		{
			All,
			ViolationsOnly,
			VersionProfiles,
			AccountsAndTwinks,
			WipedLogsAndAntiForensics,
			Whitelisted
		}

		public class LauncherForensicItem
		{
			public int Index;
			public string LauncherName;
			public string ProfileOrAccountName;
			public LauncherFilterCategory Category;
			public string MainClassOrArgs;
			public DateTime LastModified;
			public string Verdict;
			public string TargetRule;
			public string ConfigPath;
			public bool IsViolation;
			public bool IsAllowed;
			public bool IsAltAccount;
			public bool IsWiped;
			public Color RowColor;
			public string Details;
			public string RawSnippet;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private LauncherFilterCategory currentFilter = LauncherFilterCategory.All;
		private List<LauncherForensicItem> allItems = new List<LauncherForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiProfiles;
		private Label lblKpiAccounts;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterProfiles;
		private DarkButton btnFilterAccounts;
		private DarkButton btnFilterSafe;

		private DarkButton btnOpenFolder;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public MinecraftLaunchersAndProfilesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🚀 Инспектор лаунчеров Minecraft, профилей версий и аккаунтов — MCCheatChecker";
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
			lblHeaderTitle.Text = "🚀 Инспектор лаунчеров Minecraft, профилей версий и истории аккаунтов (Alt-Accounts)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Глубокий аудит 10+ лаунчеров (.minecraft, TLauncher, Prism, MultiMC, Lunar, Badlion, Feather): детекция чит-версий (mainClass), аргументов -javaagent, твинков (5.02) и зачистки логов (5.08).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования...";
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

			lblKpiTotal = CreateKpiLabel("Объектов: 0", Ui.TextPrimary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Нарушений: 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiProfiles = CreateKpiLabel("Версий: 0", Ui.AccentBlue, 272, kpiPanel);
			lblKpiAccounts = CreateKpiLabel("Твинков: 0", Ui.AccentOrange, 408, kpiPanel);
			lblKpiSafe = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Bar with filters & search
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все объекты", LauncherFilterCategory.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", LauncherFilterCategory.ViolationsOnly, ref bx, filterBar);
			btnFilterProfiles = CreateFilterButton("🚀 Профили версий", LauncherFilterCategory.VersionProfiles, ref bx, filterBar);
			btnFilterAccounts = CreateFilterButton("👤 Твинки (5.02)", LauncherFilterCategory.AccountsAndTwinks, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Белый список", LauncherFilterCategory.Whitelisted, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(240, 26);
			searchBox.Location = new Point(filterBar.Width - 256, 9);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 325, 13);
			filterBar.Controls.Add(lblSearch);

			// Main Split Container
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
			listView.Columns.Add("Лаунчер / Экосистема", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Имя профиля / Никнейм / Файл", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Основной класс (mainClass) / Аргументы JVM", 280, HorizontalAlignment.Left);
			listView.Columns.Add("Дата изменения", 130, HorizontalAlignment.Center);
			listView.Columns.Add("Вердикт", 180, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Путь к файлу конфигурации", 280, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.MouseDoubleClick += delegate { OpenConfigFileFolder(); };

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📁 Открыть папку с конфигурацией", null, delegate { OpenConfigFileFolder(); });
			contextMenu.Items.Add("📋 Скопировать путь к файлу", null, delegate { CopyFilePath(); });
			contextMenu.Items.Add("📑 Скопировать Discord-досье на улитку", null, delegate { CopyDiscordReport(); });
			contextMenu.Items.Add("🚨 Скопировать команду бана", null, delegate { CopyBanCommand(); });
			listView.ContextMenuStrip = contextMenu;

			split.Panel1.Controls.Add(listView);

			// Bottom Detail Panel
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Fill;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12);

			Label lblDetailHeader = new Label();
			lblDetailHeader.Text = "📄 Детализированный криминалистический анализ выбранного объекта лаунчера:";
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

			// Bottom Button Bar
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

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Открыть папку";
			btnOpenFolder.Size = new Size(150, 32);
			btnOpenFolder.Location = new Point(448, 6);
			btnOpenFolder.Click += delegate { OpenConfigFileFolder(); };
			btnBar.Controls.Add(btnOpenFolder);

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

		private DarkButton CreateFilterButton(string text, LauncherFilterCategory cat, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(185, 30);
			btn.Location = new Point(x, 7);
			x += 192;
			btn.Click += delegate
			{
				currentFilter = cat;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			return btn;
		}

		#endregion

		#region Forensic Scanning Engine

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование профилей лаунчеров Minecraft и истории аккаунтов...";
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
			List<LauncherForensicItem> list = new List<LauncherForensicItem>();
			int idx = 1;

			try
			{
				// 1. Официальный Minecraft Launcher (%APPDATA%\.minecraft)
				ScanOfficialDotMinecraft(list, ref idx);

				// 2. TLauncher (%APPDATA%\.tlauncher)
				ScanTLauncher(list, ref idx);

				// 3. Prism Launcher (%APPDATA%\PrismLauncher, %LOCALAPPDATA%\PrismLauncher)
				ScanPrismLauncher(list, ref idx);

				// 4. MultiMC (%APPDATA%\MultiMC, %LOCALAPPDATA%\MultiMC)
				ScanMultiMC(list, ref idx);

				// 5. Lunar Client (%USERPROFILE%\.lunarclient)
				ScanLunarClient(list, ref idx);

				// 6. Badlion Client (%APPDATA%\badlion-client, %APPDATA%\.minecraft\badlion)
				ScanBadlionClient(list, ref idx);

				// 7. Feather Client (%APPDATA%\.feather)
				ScanFeatherClient(list, ref idx);

				// 8. CurseForge Launcher (%USERPROFILE%\curseforge\minecraft\Instances)
				ScanCurseForge(list, ref idx);

				// 9. Modrinth App (%APPDATA%\com.modrinth.theseus)
				ScanModrinth(list, ref idx);

				// 10. Поиск зачищенных логов (5.08)
				ScanWipedLogs(list, ref idx);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Launcher scan error: " + ex.Message);
			}

			e.Result = list;
		}

		private void ScanOfficialDotMinecraft(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dotMinecraft = Path.Combine(appData, ".minecraft");
			if (!Directory.Exists(dotMinecraft)) return;

			// 1.1 launcher_profiles.json
			string profilesJson = Path.Combine(dotMinecraft, "launcher_profiles.json");
			if (File.Exists(profilesJson))
			{
				try
				{
					string content = ReadFileSafe(profilesJson);
					ParseLauncherProfilesJson(".minecraft (Официальный)", profilesJson, content, list, ref idx);
				}
				catch { }
			}

			// 1.2 launcher_accounts.json (Alt-Accounts)
			string accountsJson = Path.Combine(dotMinecraft, "launcher_accounts.json");
			if (File.Exists(accountsJson))
			{
				try
				{
					string content = ReadFileSafe(accountsJson);
					ParseLauncherAccountsJson(".minecraft (Официальный)", accountsJson, content, list, ref idx);
				}
				catch { }
			}

			// 1.3 .minecraft/versions/ (Audit all version JSONs)
			string versionsDir = Path.Combine(dotMinecraft, "versions");
			if (Directory.Exists(versionsDir))
			{
				ScanVersionsDirectory(".minecraft (Официальный)", versionsDir, list, ref idx);
			}
		}

		private void ScanTLauncher(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string tlauncherDir = Path.Combine(appData, ".tlauncher");
			if (!Directory.Exists(tlauncherDir)) return;

			// tlauncher-2.0.properties
			string propsPath = Path.Combine(tlauncherDir, "tlauncher-2.0.properties");
			if (File.Exists(propsPath))
			{
				try
				{
					FileInfo fi = new FileInfo(propsPath);
					string content = ReadFileSafe(propsPath);
					string jvmArgs = ExtractRegexValue(content, @"minecraft\.args\s*=\s*(.*)");
					string selectedVer = ExtractRegexValue(content, @"login\.version\s*=\s*(.*)");

					bool isViolation = false;
					string verdict = "Легитимная конфигурация TLauncher";
					string rule = "Разрешено (5.01)";
					Color color = Ui.AccentGreen;

					if (!string.IsNullOrEmpty(jvmArgs) && jvmArgs.IndexOf("-javaagent", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						isViolation = true;
						verdict = "🚨 ВНЕДРЕНИЕ ЧИТ-АГЕНТА (-javaagent)";
						rule = "5.01 (Инжектор)";
						color = Ui.AccentRed;
					}
					else if (!string.IsNullOrEmpty(jvmArgs) && (jvmArgs.IndexOf("-noverify", StringComparison.OrdinalIgnoreCase) >= 0 || jvmArgs.IndexOf("-Xverify:none", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						isViolation = true;
						verdict = "⚠️ ОТКЛЮЧЕНИЕ ПРОВЕРКИ БАЙТКОДА (-noverify)";
						rule = "5.01 (Инжектор)";
						color = Ui.AccentOrange;
					}

					list.Add(new LauncherForensicItem
					{
						Index = idx++,
						LauncherName = "TLauncher",
						ProfileOrAccountName = "Параметры запуска: " + (string.IsNullOrEmpty(selectedVer) ? "default" : selectedVer),
						Category = isViolation ? LauncherFilterCategory.ViolationsOnly : LauncherFilterCategory.VersionProfiles,
						MainClassOrArgs = string.IsNullOrEmpty(jvmArgs) ? "(Стандартные JVM параметры)" : jvmArgs,
						LastModified = fi.LastWriteTime,
						Verdict = verdict,
						TargetRule = rule,
						ConfigPath = propsPath,
						IsViolation = isViolation,
						IsAllowed = !isViolation,
						RowColor = color,
						Details = string.Format("Конфигурация TLauncher:\nПуть: {0}\nВыбранная версия: {1}\nJVM Аргументы: {2}\nДата изменения: {3}", propsPath, selectedVer, jvmArgs, fi.LastWriteTime),
						RawSnippet = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
					});
				}
				catch { }
			}

			// TLauncher accounts
			string tlAccounts = Path.Combine(tlauncherDir, "tlauncher-accounts.json");
			if (File.Exists(tlAccounts))
			{
				try
				{
					string content = ReadFileSafe(tlAccounts);
					ParseLauncherAccountsJson("TLauncher", tlAccounts, content, list, ref idx);
				}
				catch { }
			}
		}

		private void ScanPrismLauncher(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			string[] dirs = new string[] { Path.Combine(appData, "PrismLauncher"), Path.Combine(localAppData, "PrismLauncher") };
			foreach (string dir in dirs)
			{
				if (!Directory.Exists(dir)) continue;

				// Accounts
				string accPath = Path.Combine(dir, "accounts.json");
				if (File.Exists(accPath))
				{
					try
					{
						string content = ReadFileSafe(accPath);
						ParseLauncherAccountsJson("Prism Launcher", accPath, content, list, ref idx);
					}
					catch { }
				}

				// Instances
				string instancesDir = Path.Combine(dir, "instances");
				if (Directory.Exists(instancesDir))
				{
					ScanGenericInstances("Prism Launcher", instancesDir, list, ref idx);
				}
			}
		}

		private void ScanMultiMC(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			string[] dirs = new string[] { Path.Combine(appData, "MultiMC"), Path.Combine(localAppData, "MultiMC") };
			foreach (string dir in dirs)
			{
				if (!Directory.Exists(dir)) continue;

				string accPath = Path.Combine(dir, "accounts.json");
				if (File.Exists(accPath))
				{
					try
					{
						string content = ReadFileSafe(accPath);
						ParseLauncherAccountsJson("MultiMC", accPath, content, list, ref idx);
					}
					catch { }
				}

				string instancesDir = Path.Combine(dir, "instances");
				if (Directory.Exists(instancesDir))
				{
					ScanGenericInstances("MultiMC", instancesDir, list, ref idx);
				}
			}
		}

		private void ScanLunarClient(List<LauncherForensicItem> list, ref int idx)
		{
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string lunarDir = Path.Combine(userProfile, ".lunarclient");
			if (!Directory.Exists(lunarDir)) return;

			// settings/game/accounts.json
			string accFile = Path.Combine(lunarDir, @"settings\game\accounts.json");
			if (File.Exists(accFile))
			{
				try
				{
					string content = ReadFileSafe(accFile);
					MatchCollection mc = Regex.Matches(content, @"""name""\s*:\s*""([^""]+)""");
					foreach (Match m in mc)
					{
						string nick = m.Groups[1].Value;
						bool isTarget = string.Equals(nick, targetPlayerName, StringComparison.OrdinalIgnoreCase);

						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = "Lunar Client",
							ProfileOrAccountName = "Аккаунт Lunar: " + nick,
							Category = LauncherFilterCategory.AccountsAndTwinks,
							MainClassOrArgs = "Лицензионный профиль Lunar Client",
							LastModified = new FileInfo(accFile).LastWriteTime,
							Verdict = isTarget ? "Основной аккаунт проверки" : "👤 Обнаружен твинк / альт-аккаунт",
							TargetRule = isTarget ? "Белый список" : "5.02 (Мультиаккаунт)",
							ConfigPath = accFile,
							IsViolation = false,
							IsAltAccount = !isTarget,
							IsAllowed = true,
							RowColor = isTarget ? Ui.AccentGreen : Ui.AccentOrange,
							Details = string.Format("Аккаунт Lunar Client:\nНикнейм: {0}\nФайл: {1}\nСовпадает с подозреваемым: {2}", nick, accFile, isTarget ? "Да" : "Нет (Твинк)"),
							RawSnippet = m.Value
						});
					}
				}
				catch { }
			}

			// Check logs for wipes
			string logFile = Path.Combine(lunarDir, @"logs\launcher\renderer.log");
			if (File.Exists(logFile))
			{
				FileInfo fi = new FileInfo(logFile);
				if (fi.Length == 0 && (DateTime.Now - fi.LastWriteTime).TotalHours < 24)
				{
					list.Add(new LauncherForensicItem
					{
						Index = idx++,
						LauncherName = "Lunar Client",
						ProfileOrAccountName = "Журнал renderer.log",
						Category = LauncherFilterCategory.WipedLogsAndAntiForensics,
						MainClassOrArgs = "Файл очищен до 0 байт",
						LastModified = fi.LastWriteTime,
						Verdict = "🚨 ЗАЧИСТКА ЖУРНАЛОВ ЛАУНЧЕРА (5.08)",
						TargetRule = "5.08 (Сокрытие улик)",
						ConfigPath = logFile,
						IsViolation = true,
						IsWiped = true,
						RowColor = Ui.AccentRed,
						Details = "Файл лога Lunar Client был зачищен до 0 байт перед проверкой ScreenShare.",
						RawSnippet = "Length: 0 bytes"
					});
				}
			}
		}

		private void ScanBadlionClient(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string blDir = Path.Combine(appData, "badlion-client");
			if (!Directory.Exists(blDir)) return;

			string userJson = Path.Combine(blDir, "user.json");
			if (File.Exists(userJson))
			{
				try
				{
					string content = ReadFileSafe(userJson);
					Match m = Regex.Match(content, @"""username""\s*:\s*""([^""]+)""");
					if (m.Success)
					{
						string nick = m.Groups[1].Value;
						bool isTarget = string.Equals(nick, targetPlayerName, StringComparison.OrdinalIgnoreCase);

						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = "Badlion Client",
							ProfileOrAccountName = "Аккаунт Badlion: " + nick,
							Category = LauncherFilterCategory.AccountsAndTwinks,
							MainClassOrArgs = "Профиль Badlion Client",
							LastModified = new FileInfo(userJson).LastWriteTime,
							Verdict = isTarget ? "Основной аккаунт проверки" : "👤 Обнаружен твинк / альт-аккаунт",
							TargetRule = isTarget ? "Белый список" : "5.02 (Мультиаккаунт)",
							ConfigPath = userJson,
							IsViolation = false,
							IsAltAccount = !isTarget,
							IsAllowed = true,
							RowColor = isTarget ? Ui.AccentGreen : Ui.AccentOrange,
							Details = string.Format("Аккаунт Badlion Client:\nНик: {0}\nПуть: {1}", nick, userJson),
							RawSnippet = m.Value
						});
					}
				}
				catch { }
			}
		}

		private void ScanFeatherClient(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string featherDir = Path.Combine(appData, ".feather");
			if (!Directory.Exists(featherDir)) return;

			string accFile = Path.Combine(featherDir, "accounts.json");
			if (File.Exists(accFile))
			{
				try
				{
					string content = ReadFileSafe(accFile);
					MatchCollection mc = Regex.Matches(content, @"""name""\s*:\s*""([^""]+)""");
					foreach (Match m in mc)
					{
						string nick = m.Groups[1].Value;
						bool isTarget = string.Equals(nick, targetPlayerName, StringComparison.OrdinalIgnoreCase);

						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = "Feather Client",
							ProfileOrAccountName = "Аккаунт Feather: " + nick,
							Category = LauncherFilterCategory.AccountsAndTwinks,
							MainClassOrArgs = "Профиль Feather Client",
							LastModified = new FileInfo(accFile).LastWriteTime,
							Verdict = isTarget ? "Основной аккаунт проверки" : "👤 Обнаружен твинк / альт-аккаунт",
							TargetRule = isTarget ? "Белый список" : "5.02 (Мультиаккаунт)",
							ConfigPath = accFile,
							IsViolation = false,
							IsAltAccount = !isTarget,
							IsAllowed = true,
							RowColor = isTarget ? Ui.AccentGreen : Ui.AccentOrange,
							Details = string.Format("Аккаунт Feather Client:\nНик: {0}\nПуть: {1}", nick, accFile),
							RawSnippet = m.Value
						});
					}
				}
				catch { }
			}
		}

		private void ScanCurseForge(List<LauncherForensicItem> list, ref int idx)
		{
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string cfDir = Path.Combine(userProfile, @"curseforge\minecraft\Instances");
			if (Directory.Exists(cfDir))
			{
				ScanGenericInstances("CurseForge", cfDir, list, ref idx);
			}
		}

		private void ScanModrinth(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string mrDir = Path.Combine(appData, @"com.modrinth.theseus\profiles");
			if (Directory.Exists(mrDir))
			{
				ScanGenericInstances("Modrinth App", mrDir, list, ref idx);
			}
		}

		private void ScanVersionsDirectory(string launcherName, string versionsDir, List<LauncherForensicItem> list, ref int idx)
		{
			try
			{
				string[] subDirs = Directory.GetDirectories(versionsDir);
				foreach (string subDir in subDirs)
				{
					string dirName = Path.GetFileName(subDir);
					string jsonPath = Path.Combine(subDir, dirName + ".json");
					if (!File.Exists(jsonPath))
					{
						// Check any .json inside
						string[] jsons = Directory.GetFiles(subDir, "*.json");
						if (jsons.Length > 0) jsonPath = jsons[0];
						else continue;
					}

					try
					{
						FileInfo fi = new FileInfo(jsonPath);
						string content = ReadFileSafe(jsonPath);
						string mainClass = ExtractRegexValue(content, @"""mainClass""\s*:\s*""([^""]+)""");
						string jvmArgs = ExtractRegexValue(content, @"""jvmArgs""\s*:\s*""([^""]+)""");
						if (string.IsNullOrEmpty(jvmArgs))
						{
							jvmArgs = ExtractRegexValue(content, @"""minecraftArguments""\s*:\s*""([^""]+)""");
						}

						bool isViolation = false;
						string verdict = "Легитимная версия Minecraft";
						string rule = "Разрешено (Белый список)";
						Color color = Ui.AccentGreen;

						// 1. Проверка подозрительного mainClass
						string cheatName = DetectCheatInString(mainClass) ?? DetectCheatInString(dirName);
						if (!string.IsNullOrEmpty(cheatName))
						{
							isViolation = true;
							verdict = string.Format("🚨 ОБНАРУЖЕН ЧИТ-КЛИЕНТ ({0})", cheatName.ToUpper());
							rule = "5.01 (Чит-клиент)";
							color = Ui.AccentRed;
						}
						// 2. Проверка внедрения JVM аргументов
						else if (!string.IsNullOrEmpty(jvmArgs) && jvmArgs.IndexOf("-javaagent", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							isViolation = true;
							verdict = "🚨 ВНЕДРЕНИЕ ЧИТ-АГЕНТА (-javaagent)";
							rule = "5.01 (Инжектор)";
							color = Ui.AccentRed;
						}
						// 3. Проверка маскировки чита под Forge/Vanilla
						else if (IsDisguisedCheatVersion(dirName, mainClass))
						{
							isViolation = true;
							verdict = "🚨 МАСКИРОВКА ЧИТА ПОД ВАНИЛЬНУЮ ВЕРСИЮ";
							rule = "5.01 / 5.08 (Маскировка)";
							color = Ui.AccentRed;
						}
						// 4. Проверка изменения версии прямо перед проверкой
						else if ((DateTime.Now - fi.LastWriteTime).TotalMinutes < 90)
						{
							verdict = "⚠️ Версия изменена перед проверкой (<90м)";
							rule = "Внимание (5.01/5.08)";
							color = Ui.AccentOrange;
						}

						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = launcherName,
							ProfileOrAccountName = dirName,
							Category = isViolation ? LauncherFilterCategory.ViolationsOnly : LauncherFilterCategory.VersionProfiles,
							MainClassOrArgs = string.IsNullOrEmpty(mainClass) ? "(mainClass не указан)" : mainClass,
							LastModified = fi.LastWriteTime,
							Verdict = verdict,
							TargetRule = rule,
							ConfigPath = jsonPath,
							IsViolation = isViolation,
							IsAllowed = !isViolation,
							RowColor = color,
							Details = string.Format("Версия: {0}\nЛаунчер: {1}\nmainClass: {2}\nАргументы: {3}\nПуть: {4}\nДата изменения: {5}\nВердикт: {6}", dirName, launcherName, mainClass, jvmArgs, jsonPath, fi.LastWriteTime, verdict),
							RawSnippet = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
						});
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanGenericInstances(string launcherName, string instancesDir, List<LauncherForensicItem> list, ref int idx)
		{
			try
			{
				string[] subDirs = Directory.GetDirectories(instancesDir);
				foreach (string subDir in subDirs)
				{
					string instName = Path.GetFileName(subDir);
					string cfgPath = Path.Combine(subDir, "instance.cfg");
					string mmcPack = Path.Combine(subDir, "mmc-pack.json");
					string targetFile = File.Exists(cfgPath) ? cfgPath : (File.Exists(mmcPack) ? mmcPack : null);

					if (targetFile != null)
					{
						FileInfo fi = new FileInfo(targetFile);
						string content = ReadFileSafe(targetFile);
						string jvmArgs = ExtractRegexValue(content, @"(?:JvmArgs|extraArgs|minecraftArguments)\s*=\s*(.*)");

						bool isViolation = false;
						string verdict = "Легитимный инстанс " + launcherName;
						string rule = "Разрешено (5.01)";
						Color color = Ui.AccentGreen;

						string cheat = DetectCheatInString(instName) ?? DetectCheatInString(jvmArgs);
						if (!string.IsNullOrEmpty(cheat))
						{
							isViolation = true;
							verdict = string.Format("🚨 ОБНАРУЖЕН ЧИТ В ИНСТАНСЕ ({0})", cheat.ToUpper());
							rule = "5.01 (Чит-клиент)";
							color = Ui.AccentRed;
						}
						else if (!string.IsNullOrEmpty(jvmArgs) && jvmArgs.IndexOf("-javaagent", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							isViolation = true;
							verdict = "🚨 ВНЕДРЕНИЕ ЧИТ-АГЕНТА В ИНСТАНС (-javaagent)";
							rule = "5.01 (Инжектор)";
							color = Ui.AccentRed;
						}

						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = launcherName,
							ProfileOrAccountName = "Инстанс: " + instName,
							Category = isViolation ? LauncherFilterCategory.ViolationsOnly : LauncherFilterCategory.VersionProfiles,
							MainClassOrArgs = string.IsNullOrEmpty(jvmArgs) ? "(Стандартные параметры)" : jvmArgs,
							LastModified = fi.LastWriteTime,
							Verdict = verdict,
							TargetRule = rule,
							ConfigPath = targetFile,
							IsViolation = isViolation,
							IsAllowed = !isViolation,
							RowColor = color,
							Details = string.Format("Инстанс: {0}\nЛаунчер: {1}\nКонфигурация: {2}\nАргументы: {3}\nДата: {4}", instName, launcherName, targetFile, jvmArgs, fi.LastWriteTime),
							RawSnippet = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
						});
					}
				}
			}
			catch { }
		}

		private void ScanWipedLogs(List<LauncherForensicItem> list, ref int idx)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dotMc = Path.Combine(appData, ".minecraft");
			if (!Directory.Exists(dotMc)) return;

			string[] checkLogs = new string[]
			{
				Path.Combine(dotMc, "launcher_log.txt"),
				Path.Combine(dotMc, @"logs\latest.log"),
				Path.Combine(dotMc, @"logs\debug.log")
			};

			foreach (string logPath in checkLogs)
			{
				if (File.Exists(logPath))
				{
					FileInfo fi = new FileInfo(logPath);
					if (fi.Length == 0 && (DateTime.Now - fi.LastWriteTime).TotalHours < 24)
					{
						list.Add(new LauncherForensicItem
						{
							Index = idx++,
							LauncherName = ".minecraft",
							ProfileOrAccountName = "Очищенный лог: " + fi.Name,
							Category = LauncherFilterCategory.WipedLogsAndAntiForensics,
							MainClassOrArgs = "Файл очищен до 0 байт",
							LastModified = fi.LastWriteTime,
							Verdict = "🚨 ЗАЧИСТКА ЖУРНАЛОВ MINECRAFT (5.08)",
							TargetRule = "5.08 (Сокрытие улик)",
							ConfigPath = logPath,
							IsViolation = true,
							IsWiped = true,
							RowColor = Ui.AccentRed,
							Details = string.Format("Лог-файл {0} был обнулен (размер 0 байт) недавно ({1}). Это характерный след использования батника или клинера перед ScreenShare.", fi.FullName, fi.LastWriteTime),
							RawSnippet = "Length: 0 bytes"
						});
					}
				}
			}
		}

		private void ParseLauncherProfilesJson(string launcherName, string path, string content, List<LauncherForensicItem> list, ref int idx)
		{
			try
			{
				FileInfo fi = new FileInfo(path);
				MatchCollection profMatches = Regex.Matches(content, @"""name""\s*:\s*""([^""]+)""");
				foreach (Match m in profMatches)
				{
					string name = m.Groups[1].Value;
					string cheat = DetectCheatInString(name);
					bool isCheat = !string.IsNullOrEmpty(cheat);

					list.Add(new LauncherForensicItem
					{
						Index = idx++,
						LauncherName = launcherName,
						ProfileOrAccountName = "Профиль: " + name,
						Category = isCheat ? LauncherFilterCategory.ViolationsOnly : LauncherFilterCategory.VersionProfiles,
						MainClassOrArgs = "Профиль launcher_profiles.json",
						LastModified = fi.LastWriteTime,
						Verdict = isCheat ? string.Format("🚨 ЧИТ В ИМЕНИ ПРОФИЛЯ ({0})", cheat.ToUpper()) : "Стандартный профиль запуска",
						TargetRule = isCheat ? "5.01 (Чит-клиент)" : "Разрешено (5.01)",
						ConfigPath = path,
						IsViolation = isCheat,
						IsAllowed = !isCheat,
						RowColor = isCheat ? Ui.AccentRed : Ui.AccentGreen,
						Details = string.Format("Профиль лаунчера: {0}\nФайл: {1}\nВердикт: {2}", name, path, isCheat ? "Чит обнаружен" : "Белый список"),
						RawSnippet = m.Value
					});
				}
			}
			catch { }
		}

		private void ParseLauncherAccountsJson(string launcherName, string path, string content, List<LauncherForensicItem> list, ref int idx)
		{
			try
			{
				FileInfo fi = new FileInfo(path);
				MatchCollection matches = Regex.Matches(content, @"""name""\s*:\s*""([^""]+)""");
				foreach (Match m in matches)
				{
					string nick = m.Groups[1].Value;
					if (nick.Length < 3 || nick.Length > 24) continue;
					if (string.Equals(nick, "Mojang", StringComparison.OrdinalIgnoreCase) || string.Equals(nick, "Xbox", StringComparison.OrdinalIgnoreCase)) continue;

					bool isTarget = string.Equals(nick, targetPlayerName, StringComparison.OrdinalIgnoreCase);

					list.Add(new LauncherForensicItem
					{
						Index = idx++,
						LauncherName = launcherName,
						ProfileOrAccountName = "Аккаунт: " + nick,
						Category = LauncherFilterCategory.AccountsAndTwinks,
						MainClassOrArgs = "Авторизованный профиль игрока",
						LastModified = fi.LastWriteTime,
						Verdict = isTarget ? "Основной никнейм проверки" : "👤 Обнаружен твинк / мультиаккаунт",
						TargetRule = isTarget ? "Белый список" : "5.02 (Мультиаккаунт)",
						ConfigPath = path,
						IsViolation = false,
						IsAltAccount = !isTarget,
						IsAllowed = true,
						RowColor = isTarget ? Ui.AccentGreen : Ui.AccentOrange,
						Details = string.Format("Зафиксированный аккаунт: {0}\nЛаунчер: {1}\nПуть: {2}\nСовпадает с проверяемым: {3}", nick, launcherName, path, isTarget ? "Да (Основной)" : "Нет (Потенциальный твинк 5.02)"),
						RawSnippet = m.Value
					});
				}
			}
			catch { }
		}

		private static string DetectCheatInString(string str)
		{
			if (string.IsNullOrEmpty(str)) return null;
			string s = str.ToLowerInvariant();

			string[] cheatNames = new string[]
			{
				"celestial", "expensive", "deadcode", "wild", "karon", "monolith", "akrien",
				"exclient", "florian", "nur", "minced", "delta", "rich", "neverhook", "zamorozka",
				"wexside", "vape", "liquidbounce", "wurst", "meteor", "aristois", "impact",
				"sigma", "future", "rusherhack", "krypton", "matix", "huzuni", "wolfram", "boze",
				"bleachhack", "ares", "kamiblue", "lambda", "rusher", "aresclient", "fanta",
				"novoline", "rise", "tenacity", "intent", "augustus", "pandora", "moon", "flux"
			};

			foreach (string c in cheatNames)
			{
				if (s.Contains(c)) return c;
			}
			return null;
		}

		private static bool IsDisguisedCheatVersion(string dirName, string mainClass)
		{
			if (string.IsNullOrEmpty(mainClass)) return false;

			// Если в имени папки заявлена чистая версия (OptiFine, Forge, Vanilla), а mainClass ведет на сторонний чит-пакет
			string lowDir = dirName.ToLowerInvariant();
			string lowMc = mainClass.ToLowerInvariant();

			bool isNamedLegit = lowDir.Contains("optifine") || lowDir.Contains("forge") || lowDir.Contains("fabric") || lowDir.Contains("vanilla") || lowDir.Contains("1.16") || lowDir.Contains("1.12") || lowDir.Contains("1.20");

			bool isKnownLegitClass = lowMc.StartsWith("net.minecraft.") || lowMc.StartsWith("cpw.mods.") || lowMc.StartsWith("net.fabricmc.") || lowMc.StartsWith("net.neoforged.") || lowMc.Contains("launchwrapper");

			if (isNamedLegit && !isKnownLegitClass)
			{
				// Проверим, не легитимный ли это лаунчер
				if (!lowMc.Contains("mojang") && !lowMc.Contains("optifine") && !lowMc.Contains("feather") && !lowMc.Contains("lunar") && !lowMc.Contains("badlion"))
				{
					return true;
				}
			}
			return false;
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
			if (e.Result is List<LauncherForensicItem>)
			{
				allItems = (List<LauncherForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int violations = 0;
			int profiles = 0;
			int accounts = 0;
			int safe = 0;

			foreach (LauncherForensicItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.Category == LauncherFilterCategory.VersionProfiles) profiles++;
				if (it.IsAltAccount) accounts++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = "Объектов: " + total;
			lblKpiViolations.Text = "Нарушений: " + violations;
			lblKpiProfiles.Text = "Версий: " + profiles;
			lblKpiAccounts.Text = "Твинков: " + accounts;
			lblKpiSafe.Text = "Белый список: " + safe;

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} НАРУШЕНИЙ В ЛАУНЧЕРАХ (5.01 / 5.08)", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Лаунчеры и профили чисты: подозрительных mainClass и аргументов -javaagent не обнаружено";
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

			foreach (LauncherForensicItem it in allItems)
			{
				if (currentFilter == LauncherFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == LauncherFilterCategory.VersionProfiles && it.Category != LauncherFilterCategory.VersionProfiles) continue;
				if (currentFilter == LauncherFilterCategory.AccountsAndTwinks && !it.IsAltAccount && it.Category != LauncherFilterCategory.AccountsAndTwinks) continue;
				if (currentFilter == LauncherFilterCategory.Whitelisted && !it.IsAllowed) continue;
				if (currentFilter == LauncherFilterCategory.WipedLogsAndAntiForensics && !it.IsWiped) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.LauncherName != null && it.LauncherName.ToLowerInvariant().Contains(search))
						|| (it.ProfileOrAccountName != null && it.ProfileOrAccountName.ToLowerInvariant().Contains(search))
						|| (it.MainClassOrArgs != null && it.MainClassOrArgs.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search))
						|| (it.ConfigPath != null && it.ConfigPath.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.LauncherName);
				lvi.SubItems.Add(it.ProfileOrAccountName);
				lvi.SubItems.Add(it.Category.ToString());
				lvi.SubItems.Add(it.MainClassOrArgs);
				lvi.SubItems.Add(it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.ConfigPath);

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
			LauncherForensicItem it = listView.SelectedItems[0].Tag as LauncherForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛИЗИРОВАННЫЙ АНАЛИЗ КОНФИГУРАЦИИ ЛАУНЧЕРА MINECRAFT ===");
			sb.AppendLine("Лаунчер / Экосистема: " + it.LauncherName);
			sb.AppendLine("Имя профиля / Ник: " + it.ProfileOrAccountName);
			sb.AppendLine("Категория: " + it.Category);
			sb.AppendLine("mainClass / JVM Аргументы: " + it.MainClassOrArgs);
			sb.AppendLine("Дата изменения конфигурации: " + it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Путь к файлу: " + it.ConfigPath);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт правил сервера: " + it.TargetRule);
			sb.AppendLine();
			sb.AppendLine("--- Криминалистическое резюме ---");
			sb.AppendLine(it.Details);
			sb.AppendLine();
			if (!string.IsNullOrEmpty(it.RawSnippet))
			{
				sb.AppendLine("--- Исходный фрагмент конфигурации / JSON ---");
				sb.AppendLine(it.RawSnippet);
			}

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void OpenConfigFileFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			LauncherForensicItem it = listView.SelectedItems[0].Tag as LauncherForensicItem;
			if (it == null || string.IsNullOrEmpty(it.ConfigPath)) return;

			try
			{
				string folder = Path.GetDirectoryName(it.ConfigPath);
				if (Directory.Exists(folder))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", it.ConfigPath));
				}
				else if (File.Exists(it.ConfigPath))
				{
					Process.Start("explorer.exe", it.ConfigPath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Ошибка открытия папки", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyFilePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			LauncherForensicItem it = listView.SelectedItems[0].Tag as LauncherForensicItem;
			if (it == null || string.IsNullOrEmpty(it.ConfigPath)) return;

			Clipboard.SetText(it.ConfigPath);
			MessageBox.Show("Путь к файлу конфигурации скопирован:\n" + it.ConfigPath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			LauncherForensicItem it = listView.SelectedItems[0].Tag as LauncherForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ ЛАУНЧЕРА MINECRAFT ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Лаунчер: " + it.LauncherName);
			sb.AppendLine("Профиль / Ник: " + it.ProfileOrAccountName);
			sb.AppendLine("mainClass / Аргументы: " + it.MainClassOrArgs);
			sb.AppendLine("Дата изменения: " + it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Конфигурация: " + it.ConfigPath);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Discord-отчет скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			LauncherForensicItem it = listView.SelectedItems[0].Tag as LauncherForensicItem;
			if (it == null) return;

			string cmd;
			if (it.IsWiped)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Зачистка логов лаунчера {1})", targetPlayerName, it.LauncherName);
			}
			else if (it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Чит-профиль {1} в лаунчере {2})", targetPlayerName, it.ProfileOrAccountName, it.LauncherName);
			}
			else if (it.IsAltAccount)
			{
				cmd = string.Format("/ban {0} 30d 5.02 Мультиаккаунт (Твинк {1} в лаунчере {2})", targetPlayerName, it.ProfileOrAccountName, it.LauncherName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Чит-профиль в лаунчере {1}", targetPlayerName, it.LauncherName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
