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
	public class BrowserExtensionsAndWebCheatPanelsViewerForm : Form
	{
		#region Data Models

		public enum BrowserArtifactCategory
		{
			All,
			ViolationsOnly,
			BrowserExtensions,
			TampermonkeyScripts,
			LocalStorageAndTokens,
			CheatBookmarks,
			Whitelisted
		}

		public class BrowserForensicItem
		{
			public int Index;
			public string BrowserName;
			public string ItemTitle;
			public BrowserArtifactCategory Category;
			public string IdentifierOrUrl;
			public DateTime LastModified;
			public string Verdict;
			public string TargetRule;
			public string StoragePath;
			public bool IsViolation;
			public bool IsAllowed;
			public Color RowColor;
			public string Details;
			public string RawSnippet;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private BrowserArtifactCategory currentFilter = BrowserArtifactCategory.All;
		private List<BrowserForensicItem> allItems = new List<BrowserForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiExtensions;
		private Label lblKpiTokens;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterExtensions;
		private DarkButton btnFilterScripts;
		private DarkButton btnFilterStorage;
		private DarkButton btnFilterSafe;

		private DarkButton btnOpenFolder;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public BrowserExtensionsAndWebCheatPanelsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🌐 Инспектор браузерных расширений, скриптов Tampermonkey и Web-панелей читов — MCCheatChecker";
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
			lblHeaderTitle.Text = "🌐 Инспектор браузерных расширений, скриптов Tampermonkey и Web-панелей читов";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Аудит Chrome, Edge, Opera, Yandex, Brave, Firefox: выявление юзерскриптов Tampermonkey/Violentmonkey, Web-GUI читов (Vape/Drip/Slinky), сессий LocalStorage и закладок (5.01 / 5.02 / 5.08).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования браузерных хранилищ...";
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
			lblKpiViolations = CreateKpiLabel("Улик читов: 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiExtensions = CreateKpiLabel("Расширений: 0", Ui.AccentBlue, 272, kpiPanel);
			lblKpiTokens = CreateKpiLabel("LocalStorage: 0", Ui.AccentOrange, 408, kpiPanel);
			lblKpiSafe = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Filter bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все объекты", BrowserArtifactCategory.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Только улики (5.01/5.02)", BrowserArtifactCategory.ViolationsOnly, ref bx, filterBar);
			btnFilterExtensions = CreateFilterButton("🧩 Расширения", BrowserArtifactCategory.BrowserExtensions, ref bx, filterBar);
			btnFilterScripts = CreateFilterButton("📜 Tampermonkey", BrowserArtifactCategory.TampermonkeyScripts, ref bx, filterBar);
			btnFilterStorage = CreateFilterButton("🔑 LocalStorage", BrowserArtifactCategory.LocalStorageAndTokens, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Белый список", BrowserArtifactCategory.Whitelisted, ref bx, filterBar);

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
			listView.Columns.Add("Браузер / Профиль", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Название расширения / Скрипт / Сессия", 260, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 150, HorizontalAlignment.Left);
			listView.Columns.Add("ID расширения / URL / Домен", 280, HorizontalAlignment.Left);
			listView.Columns.Add("Дата изменения", 130, HorizontalAlignment.Center);
			listView.Columns.Add("Вердикт", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Путь к хранилищу", 260, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.MouseDoubleClick += delegate { OpenFolder(); };

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📁 Открыть папку с файлом", null, delegate { OpenFolder(); });
			contextMenu.Items.Add("📋 Скопировать путь к файлу", null, delegate { CopyPath(); });
			contextMenu.Items.Add("📑 Скопировать Discord-досье на улику", null, delegate { CopyDiscordReport(); });
			contextMenu.Items.Add("🚨 Скопировать команду бана", null, delegate { CopyBanCommand(); });
			listView.ContextMenuStrip = contextMenu;

			split.Panel1.Controls.Add(listView);

			// Bottom Panel
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Fill;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12);

			Label lblDetailHeader = new Label();
			lblDetailHeader.Text = "📄 Детализированный криминалистический анализ артефакта браузера:";
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

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Открыть папку";
			btnOpenFolder.Size = new Size(150, 32);
			btnOpenFolder.Location = new Point(448, 6);
			btnOpenFolder.Click += delegate { OpenFolder(); };
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

		private DarkButton CreateFilterButton(string text, BrowserArtifactCategory cat, ref int x, Panel parent)
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
			lblStatusPill.Text = "⏳ Сканирование расширений, скриптов Tampermonkey и LocalStorage...";
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
			List<BrowserForensicItem> list = new List<BrowserForensicItem>();
			int idx = 1;

			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			// Браузерные базы
			var browsers = new[]
			{
				new { Name = "Google Chrome", Path = Path.Combine(localAppData, @"Google\Chrome\User Data") },
				new { Name = "Microsoft Edge", Path = Path.Combine(localAppData, @"Microsoft\Edge\User Data") },
				new { Name = "Yandex Browser", Path = Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data") },
				new { Name = "Opera", Path = Path.Combine(appData, @"Opera Software\Opera Stable") },
				new { Name = "Opera GX", Path = Path.Combine(appData, @"Opera Software\Opera GX Stable") },
				new { Name = "Brave Browser", Path = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data") }
			};

			foreach (var b in browsers)
			{
				if (!Directory.Exists(b.Path)) continue;

				// 1. Поиск расширений
				ScanChromiumExtensions(b.Name, b.Path, list, ref idx);

				// 2. Поиск сессий Web-GUI читов в LocalStorage / LevelDB
				ScanChromiumLocalStorage(b.Name, b.Path, list, ref idx);

				// 3. Закладки браузера
				ScanChromiumBookmarks(b.Name, b.Path, list, ref idx);
			}

			// Mozilla Firefox
			string ffPath = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
			if (Directory.Exists(ffPath))
			{
				ScanFirefoxProfiles(ffPath, list, ref idx);
			}

			e.Result = list;
		}

		private void ScanChromiumExtensions(string browserName, string userDataDir, List<BrowserForensicItem> list, ref int idx)
		{
			try
			{
				string[] profileDirs = Directory.GetDirectories(userDataDir, "Profile *");
				List<string> allProfiles = new List<string>(profileDirs);
				string defDir = Path.Combine(userDataDir, "Default");
				if (Directory.Exists(defDir)) allProfiles.Add(defDir);

				foreach (string prof in allProfiles)
				{
					string profName = Path.GetFileName(prof);
					string extDir = Path.Combine(prof, "Extensions");
					if (!Directory.Exists(extDir)) continue;

					string[] extIds = Directory.GetDirectories(extDir);
					foreach (string extIdDir in extIds)
					{
						string extId = Path.GetFileName(extIdDir);
						string[] verDirs = Directory.GetDirectories(extIdDir);
						if (verDirs.Length == 0) continue;

						string verDir = verDirs[0];
						string manifestJson = Path.Combine(verDir, "manifest.json");
						if (!File.Exists(manifestJson)) continue;

						try
						{
							FileInfo fi = new FileInfo(manifestJson);
							string content = ReadFileSafe(manifestJson);

							string name = ExtractRegexValue(content, @"""name""\s*:\s*""([^""]+)""");
							string version = ExtractRegexValue(content, @"""version""\s*:\s*""([^""]+)""");
							string desc = ExtractRegexValue(content, @"""description""\s*:\s*""([^""]+)""");

							// Разрешение локализованного имени
							if (name != null && name.StartsWith("__MSG_"))
							{
								string resolved = ResolveLocaleString(verDir, name);
								if (!string.IsNullOrEmpty(resolved)) name = resolved;
							}

							if (string.IsNullOrEmpty(name)) name = extId;

							// Анализ на Tampermonkey / читерские расширения
							bool isTampermonkey = extId.Equals("dhdgffkkebhmkfjojejmpbldmpobfkfo", StringComparison.OrdinalIgnoreCase)
								|| extId.Equals("gcalmfdmpgfggjjhooghgkgclhmgleip", StringComparison.OrdinalIgnoreCase)
								|| extId.Equals("jinjaccalgkegednnccohejagnlnfdag", StringComparison.OrdinalIgnoreCase)
								|| name.ToLowerInvariant().Contains("tampermonkey")
								|| name.ToLowerInvariant().Contains("violentmonkey");

							string cheat = DetectCheatInString(name) ?? DetectCheatInString(desc);
							bool isCheat = !string.IsNullOrEmpty(cheat);

							string verdict = "Легитимное расширение браузера";
							string rule = "Разрешено (Белый список)";
							Color color = Ui.AccentGreen;
							BrowserArtifactCategory cat = BrowserArtifactCategory.BrowserExtensions;

							if (isCheat)
							{
								verdict = string.Format("🚨 ЧИТЕРСКОЕ РАСШИРЕНИЕ ({0})", cheat.ToUpper());
								rule = "5.01 (Чит-клиент/Скрипт)";
								color = Ui.AccentRed;
								cat = BrowserArtifactCategory.ViolationsOnly;
							}
							else if (isTampermonkey)
							{
								verdict = "⚠️ МЕНЕДЖЕР СКРИПТОВ TAMPERMONKEY (Требует проверки скриптов)";
								rule = "Внимание (5.01 / 5.08)";
								color = Ui.AccentOrange;
								cat = BrowserArtifactCategory.TampermonkeyScripts;

								// Проверим хранилище скриптов Tampermonkey
								ScanTampermonkeyStorage(browserName, prof, extId, list, ref idx);
							}

							list.Add(new BrowserForensicItem
							{
								Index = idx++,
								BrowserName = browserName + " (" + profName + ")",
								ItemTitle = name,
								Category = cat,
								IdentifierOrUrl = extId + " (v" + (version ?? "1.0") + ")",
								LastModified = fi.LastWriteTime,
								Verdict = verdict,
								TargetRule = rule,
								StoragePath = manifestJson,
								IsViolation = isCheat,
								IsAllowed = !isCheat && !isTampermonkey,
								RowColor = color,
								Details = string.Format("Расширение: {0}\nБраузер: {1}\nID: {2}\nВерсия: {3}\nОписание: {4}\nПуть: {5}\nДата: {6}", name, browserName, extId, version, desc, manifestJson, fi.LastWriteTime),
								RawSnippet = content.Length > 800 ? content.Substring(0, 800) + "..." : content
							});
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private void ScanTampermonkeyStorage(string browserName, string profileDir, string extId, List<BrowserForensicItem> list, ref int idx)
		{
			try
			{
				string storageDir = Path.Combine(profileDir, @"Local Extension Settings\" + extId);
				if (!Directory.Exists(storageDir)) return;

				string[] files = Directory.GetFiles(storageDir, "*.*");
				foreach (string f in files)
				{
					if (f.EndsWith(".ldb") || f.EndsWith(".log"))
					{
						string content = ReadFileSafe(f);
						string[] cheatKeywords = new string[] { "minecraft", "killaura", "autoclicker", "vape", "celestial", "expensive", "funtime", "reallyworld", "matrix", "reach", "breeze" };
						foreach (string kw in cheatKeywords)
						{
							if (content.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								FileInfo fi = new FileInfo(f);
								list.Add(new BrowserForensicItem
								{
									Index = idx++,
									BrowserName = browserName,
									ItemTitle = "Скрипт Tampermonkey: обнаружен маркер " + kw.ToUpper(),
									Category = BrowserArtifactCategory.ViolationsOnly,
									IdentifierOrUrl = "Ключевое слово: " + kw,
									LastModified = fi.LastWriteTime,
									Verdict = "🚨 ВРЕДОНОСНЫЙ СКРИПТ В TAMPERMONKEY (5.01)",
									TargetRule = "5.01 (Инъекция скрипта)",
									StoragePath = f,
									IsViolation = true,
									IsAllowed = false,
									RowColor = Ui.AccentRed,
									Details = string.Format("В базе скриптов Tampermonkey ({0}) обнаружено упоминание читерского скрипта по маркеру '{1}'.", f, kw),
									RawSnippet = "Ключевое слово: " + kw + "\nХранилище: " + f
								});
								break;
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanChromiumLocalStorage(string browserName, string userDataDir, List<BrowserForensicItem> list, ref int idx)
		{
			try
			{
				string[] profileDirs = Directory.GetDirectories(userDataDir, "Profile *");
				List<string> allProfiles = new List<string>(profileDirs);
				string defDir = Path.Combine(userDataDir, "Default");
				if (Directory.Exists(defDir)) allProfiles.Add(defDir);

				string[] cheatDomains = new string[]
				{
					"vape.gg", "driplite.cc", "slinky.gg", "celestial.lol", "expensive.fun",
					"deadcode.fun", "neverhook.ru", "akrien.wtf", "minced.cc", "wildclient.ru",
					"zamorozka.fun", "nursultan.fun", "lolz.live", "zelenka.guru", "funpay.com", "cheat-mc.ru"
				};

				foreach (string prof in allProfiles)
				{
					string profName = Path.GetFileName(prof);
					string levelDbDir = Path.Combine(prof, @"Local Storage\leveldb");
					if (!Directory.Exists(levelDbDir)) continue;

					string[] files = Directory.GetFiles(levelDbDir, "*.*");
					foreach (string f in files)
					{
						if (!f.EndsWith(".ldb") && !f.EndsWith(".log")) continue;

						try
						{
							FileInfo fi = new FileInfo(f);
							string text = ReadFileSafe(f);

							foreach (string domain in cheatDomains)
							{
								if (text.IndexOf(domain, StringComparison.OrdinalIgnoreCase) >= 0)
								{
									bool isBuyMarket = domain.Contains("lolz") || domain.Contains("zelenka") || domain.Contains("funpay");

									string verdict = isBuyMarket 
										? "🚨 СЕССИЯ ПОКУПКИ АККАУНТОВ / ЧИТОВ (5.02)" 
										: string.Format("🚨 АКТИВНАЯ СЕССИЯ WEB-ПАНЕЛИ ЧИТА ({0})", domain.ToUpper());

									string rule = isBuyMarket ? "5.02 (Покупка аккаунтов)" : "5.01 (Web-GUI чита)";

									list.Add(new BrowserForensicItem
									{
										Index = idx++,
										BrowserName = browserName + " (" + profName + ")",
										ItemTitle = "Сессия в LocalStorage: " + domain,
										Category = BrowserArtifactCategory.ViolationsOnly,
										IdentifierOrUrl = "Домен: https://" + domain,
										LastModified = fi.LastWriteTime,
										Verdict = verdict,
										TargetRule = rule,
										StoragePath = f,
										IsViolation = true,
										IsAllowed = false,
										RowColor = Ui.AccentRed,
										Details = string.Format("В постоянном хранилище браузера LevelDB обнаружена активная сессия/токен веб-панели чит-сервиса:\nДомен: {0}\nБраузер: {1}\nФайл: {2}\nДата записи: {3}", domain, browserName, f, fi.LastWriteTime),
										RawSnippet = "Домен: " + domain + "\nФайл хранилища: " + f
									});
								}
							}
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private void ScanChromiumBookmarks(string browserName, string userDataDir, List<BrowserForensicItem> list, ref int idx)
		{
			try
			{
				string[] profileDirs = Directory.GetDirectories(userDataDir, "Profile *");
				List<string> allProfiles = new List<string>(profileDirs);
				string defDir = Path.Combine(userDataDir, "Default");
				if (Directory.Exists(defDir)) allProfiles.Add(defDir);

				foreach (string prof in allProfiles)
				{
					string profName = Path.GetFileName(prof);
					string bmPath = Path.Combine(prof, "Bookmarks");
					if (!File.Exists(bmPath)) continue;

					try
					{
						FileInfo fi = new FileInfo(bmPath);
						string content = ReadFileSafe(bmPath);

						MatchCollection matches = Regex.Matches(content, @"""name""\s*:\s*""([^""]+)""\s*,\s*""type""\s*:\s*""url""\s*,\s*""url""\s*:\s*""([^""]+)""");
						foreach (Match m in matches)
						{
							string name = m.Groups[1].Value;
							string url = m.Groups[2].Value;

							string cheat = DetectCheatInString(name) ?? DetectCheatInString(url);
							if (!string.IsNullOrEmpty(cheat))
							{
								list.Add(new BrowserForensicItem
								{
									Index = idx++,
									BrowserName = browserName + " (" + profName + ")",
									ItemTitle = "Закладка: " + name,
									Category = BrowserArtifactCategory.ViolationsOnly,
									IdentifierOrUrl = url,
									LastModified = fi.LastWriteTime,
									Verdict = string.Format("🚨 СОХРАНЕННАЯ ЗАКЛАДКА НА ЧИТ ({0})", cheat.ToUpper()),
									TargetRule = "5.01 / 5.08 (Закладки читов)",
									StoragePath = bmPath,
									IsViolation = true,
									IsAllowed = false,
									RowColor = Ui.AccentRed,
									Details = string.Format("В закладках браузера обнаружена ссылка на чит:\nИмя: {0}\nURL: {1}\nБраузер: {2}\nДата изменения файла: {3}", name, url, browserName, fi.LastWriteTime),
									RawSnippet = m.Value
								});
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanFirefoxProfiles(string profilesDir, List<BrowserForensicItem> list, ref int idx)
		{
			try
			{
				string[] pDirs = Directory.GetDirectories(profilesDir);
				foreach (string pd in pDirs)
				{
					string extJson = Path.Combine(pd, "extensions.json");
					if (File.Exists(extJson))
					{
						FileInfo fi = new FileInfo(extJson);
						string content = ReadFileSafe(extJson);

						MatchCollection matches = Regex.Matches(content, @"""defaultLocale""\s*:\s*\{\s*""name""\s*:\s*""([^""]+)""");
						foreach (Match m in matches)
						{
							string name = m.Groups[1].Value;
							string cheat = DetectCheatInString(name);
							bool isTamper = name.ToLowerInvariant().Contains("tampermonkey") || name.ToLowerInvariant().Contains("violentmonkey");

							list.Add(new BrowserForensicItem
							{
								Index = idx++,
								BrowserName = "Mozilla Firefox",
								ItemTitle = name,
								Category = !string.IsNullOrEmpty(cheat) ? BrowserArtifactCategory.ViolationsOnly : (isTamper ? BrowserArtifactCategory.TampermonkeyScripts : BrowserArtifactCategory.BrowserExtensions),
								IdentifierOrUrl = "Firefox Add-on",
								LastModified = fi.LastWriteTime,
								Verdict = !string.IsNullOrEmpty(cheat) ? "🚨 ЧИТЕРСКОЕ РАСШИРЕНИЕ FIREFOX" : (isTamper ? "⚠️ Менеджер скриптов Tampermonkey" : "Легитимный Add-on"),
								TargetRule = !string.IsNullOrEmpty(cheat) ? "5.01 (Чит-клиент)" : "Разрешено (Белый список)",
								StoragePath = extJson,
								IsViolation = !string.IsNullOrEmpty(cheat),
								IsAllowed = string.IsNullOrEmpty(cheat) && !isTamper,
								RowColor = !string.IsNullOrEmpty(cheat) ? Ui.AccentRed : (isTamper ? Ui.AccentOrange : Ui.AccentGreen),
								Details = string.Format("Firefox дополнение: {0}\nФайл: {1}\nДата: {2}", name, extJson, fi.LastWriteTime),
								RawSnippet = m.Value
							});
						}
					}
				}
			}
			catch { }
		}

		private static string ResolveLocaleString(string verDir, string msgKey)
		{
			string key = msgKey.Replace("__MSG_", "").Replace("__", "").Trim();
			string[] localeDirs = new string[] { Path.Combine(verDir, @"_locales\ru\messages.json"), Path.Combine(verDir, @"_locales\en\messages.json") };

			foreach (string lf in localeDirs)
			{
				if (File.Exists(lf))
				{
					string content = ReadFileSafe(lf);
					string pattern = string.Format(@"""{0}""\s*:\s*\{{\s*""message""\s*:\s*""([^""]+)""", Regex.Escape(key));
					Match m = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
					if (m.Success) return m.Groups[1].Value;
				}
			}
			return null;
		}

		private static string DetectCheatInString(string str)
		{
			if (string.IsNullOrEmpty(str)) return null;
			string s = str.ToLowerInvariant();

			string[] cheatNames = new string[]
			{
				"vape", "celestial", "expensive", "deadcode", "wild", "karon", "akrien", "exclient",
				"florian", "nur", "minced", "delta", "rich", "neverhook", "zamorozka", "wexside",
				"driplite", "slinky", "meteor", "liquidbounce", "autoclicker", "killaura", "xray",
				"breeze", "entropy", "opalclient", "catlavan"
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
			if (e.Result is List<BrowserForensicItem>)
			{
				allItems = (List<BrowserForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int violations = 0;
			int extensions = 0;
			int tokens = 0;
			int safe = 0;

			foreach (BrowserForensicItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.Category == BrowserArtifactCategory.BrowserExtensions) extensions++;
				if (it.Category == BrowserArtifactCategory.LocalStorageAndTokens) tokens++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = "Объектов: " + total;
			lblKpiViolations.Text = "Улик читов: " + violations;
			lblKpiExtensions.Text = "Расширений: " + extensions;
			lblKpiTokens.Text = "LocalStorage: " + tokens;
			lblKpiSafe.Text = "Белый список: " + safe;

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} УЛИК ЧИТОВ / ПАНЕЛЕЙ В БРАУЗЕРЕ (5.01 / 5.02)", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Браузерные расширения чисты: скриптов читов и сессий Web-панелей не обнаружено";
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

			foreach (BrowserForensicItem it in allItems)
			{
				if (currentFilter == BrowserArtifactCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == BrowserArtifactCategory.BrowserExtensions && it.Category != BrowserArtifactCategory.BrowserExtensions && !it.IsViolation) continue;
				if (currentFilter == BrowserArtifactCategory.TampermonkeyScripts && it.Category != BrowserArtifactCategory.TampermonkeyScripts && !it.IsViolation) continue;
				if (currentFilter == BrowserArtifactCategory.LocalStorageAndTokens && it.Category != BrowserArtifactCategory.LocalStorageAndTokens && !it.IsViolation) continue;
				if (currentFilter == BrowserArtifactCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.BrowserName != null && it.BrowserName.ToLowerInvariant().Contains(search))
						|| (it.ItemTitle != null && it.ItemTitle.ToLowerInvariant().Contains(search))
						|| (it.IdentifierOrUrl != null && it.IdentifierOrUrl.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search))
						|| (it.StoragePath != null && it.StoragePath.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.BrowserName);
				lvi.SubItems.Add(it.ItemTitle);
				lvi.SubItems.Add(it.Category.ToString());
				lvi.SubItems.Add(it.IdentifierOrUrl);
				lvi.SubItems.Add(it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.StoragePath);

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
			BrowserForensicItem it = listView.SelectedItems[0].Tag as BrowserForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛЬНЫЙ АНАЛИЗ БРАУЗЕРНОГО АРТЕФАКТА ===");
			sb.AppendLine("Браузер / Профиль: " + it.BrowserName);
			sb.AppendLine("Название / Улика: " + it.ItemTitle);
			sb.AppendLine("Категория: " + it.Category);
			sb.AppendLine("Идентификатор / URL: " + it.IdentifierOrUrl);
			sb.AppendLine("Дата изменения файла: " + it.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Путь к хранилищу: " + it.StoragePath);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт регламента сервера: " + it.TargetRule);
			sb.AppendLine();
			sb.AppendLine("--- Подробный протокол проверки ---");
			sb.AppendLine(it.Details);
			sb.AppendLine();
			if (!string.IsNullOrEmpty(it.RawSnippet))
			{
				sb.AppendLine("--- Фрагмент манифеста / данных ---");
				sb.AppendLine(it.RawSnippet);
			}

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void OpenFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			BrowserForensicItem it = listView.SelectedItems[0].Tag as BrowserForensicItem;
			if (it == null || string.IsNullOrEmpty(it.StoragePath)) return;

			try
			{
				string folder = Path.GetDirectoryName(it.StoragePath);
				if (Directory.Exists(folder))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", it.StoragePath));
				}
				else if (File.Exists(it.StoragePath))
				{
					Process.Start("explorer.exe", it.StoragePath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Ошибка открытия папки", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyPath()
		{
			if (listView.SelectedItems.Count == 0) return;
			BrowserForensicItem it = listView.SelectedItems[0].Tag as BrowserForensicItem;
			if (it == null || string.IsNullOrEmpty(it.StoragePath)) return;

			Clipboard.SetText(it.StoragePath);
			MessageBox.Show("Путь скопирован в буфер:\n" + it.StoragePath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			BrowserForensicItem it = listView.SelectedItems[0].Tag as BrowserForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ БРАУЗЕРНОЙ УЛИКИ SCREENSHARE ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Браузер: " + it.BrowserName);
			sb.AppendLine("Объект / Расширение: " + it.ItemTitle);
			sb.AppendLine("Идентификатор / URL: " + it.IdentifierOrUrl);
			sb.AppendLine("Хранилище: " + it.StoragePath);
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
			BrowserForensicItem it = listView.SelectedItems[0].Tag as BrowserForensicItem;
			if (it == null) return;

			string cmd;
			if (it.Category == BrowserArtifactCategory.TampermonkeyScripts)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Скрипт {1} в Tampermonkey)", targetPlayerName, it.ItemTitle);
			}
			else if (it.TargetRule.Contains("5.02"))
			{
				cmd = string.Format("/ban {0} 30d 5.02 Обход бана (Сессия {1} в браузере)", targetPlayerName, it.ItemTitle);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Улика {1} в {2})", targetPlayerName, it.ItemTitle, it.BrowserName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
