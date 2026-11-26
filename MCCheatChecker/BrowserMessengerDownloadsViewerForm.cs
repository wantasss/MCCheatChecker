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
	public class BrowserMessengerDownloadsViewerForm : Form
	{
		#region Models

		public enum DownloadFilterMode
		{
			All,
			CheatsOnly,
			DeletedOnly,
			DiscordTelegram,
			Whitelist
		}

		public class DownloadItem
		{
			public string SourceApp; // "Google Chrome", "Opera GX", "Telegram Desktop", "Discord", etc.
			public string FileName;
			public string TargetPath;
			public string DownloadUrl;
			public long FileSize;
			public DateTime DownloadTime;
			public bool ExistsOnDisk;
			public bool IsCheat;
			public bool IsMessenger;
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
		private DownloadFilterMode currentFilter = DownloadFilterMode.All;
		private List<DownloadItem> allEntries = new List<DownloadItem>();
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
		private DarkButton btnFilterMessengers;
		private DarkButton btnFilterWhitelist;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenFileFolder;
		private DarkButton btnOpenUrl;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger|cleaner|wiper|reghax|selfdelete)",
			RegexOptions.Compiled);

		private static readonly string[] WhitelistTerms = new string[]
		{
			"anydesk", "rudesktop", "discord", "telegram", "obs64", "steam",
			"opautoclicker", "op auto clicker", "speedautoclicker", "tinytask", "x-mouse", "xmouse", "autohotkey", "clickermann", "keyran",
			"lunarclient", "badlion", "feather", "tlauncher", "curseforge", "prism", "modrinth", "optifine", "sodium", "iris", "fabric", "forge"
		};

		#endregion

		#region UI Initialization

		public BrowserMessengerDownloadsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит загрузок браузеров и мессенджеров (Downloads Forensic) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🌐 Криминалистический аудит загрузок: Браузеры, Discord и Telegram";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Извлечение истории загрузок, оригинальных URL-адресов читов, файлов из Telegram/Discord и выявление удалений скачанного софта (5.08 / 5.01).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование истории загрузок...";
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
			btnFilterAll = CreateFilterButton("Все загрузки", DownloadFilterMode.All, ref bx, filterBar);
			btnFilterCheats = CreateFilterButton("🚨 Читы (5.01 / 5.08)", DownloadFilterMode.CheatsOnly, ref bx, filterBar);
			btnFilterDeleted = CreateFilterButton("🗑️ Удаленные с диска (5.08)", DownloadFilterMode.DeletedOnly, ref bx, filterBar);
			btnFilterMessengers = CreateFilterButton("💬 Discord / Telegram", DownloadFilterMode.DiscordTelegram, ref bx, filterBar);
			btnFilterWhitelist = CreateFilterButton("🛡️ Белый список", DownloadFilterMode.Whitelist, ref bx, filterBar);

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

			btnOpenFileFolder = new DarkButton();
			btnOpenFileFolder.Text = "📁 Открыть папку файла";
			btnOpenFileFolder.Font = Ui.FontBody(9f, false);
			btnOpenFileFolder.Location = new Point(242, 10);
			btnOpenFileFolder.Size = new Size(185, 30);
			btnOpenFileFolder.Click += BtnOpenFileFolder_Click;
			bottomBar.Controls.Add(btnOpenFileFolder);

			btnOpenUrl = new DarkButton();
			btnOpenUrl.Text = "🌐 Открыть URL в браузере";
			btnOpenUrl.Font = Ui.FontBody(9f, false);
			btnOpenUrl.Location = new Point(435, 10);
			btnOpenUrl.Size = new Size(195, 30);
			btnOpenUrl.Click += BtnOpenUrl_Click;
			bottomBar.Controls.Add(btnOpenUrl);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(638, 10);
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
			btnExportReport.Location = new Point(831, 10);
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
			listView.Columns.Add("Имя файла", 220);
			listView.Columns.Add("Время загрузки", 150);
			listView.Columns.Add("Статус на диске", 160);
			listView.Columns.Add("URL-источник / Путь", 340);
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
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ О ЗАГРУЗКЕ И ИСТОЧНИКЕ ФАЙЛА:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите загрузку из таблицы для анализа.";
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

		private DarkButton CreateFilterButton(string text, DownloadFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == DownloadFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(155, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 161;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == DownloadFilterMode.All);
			btnFilterCheats.AccentStyle = (currentFilter == DownloadFilterMode.CheatsOnly);
			btnFilterDeleted.AccentStyle = (currentFilter == DownloadFilterMode.DeletedOnly);
			btnFilterMessengers.AccentStyle = (currentFilter == DownloadFilterMode.DiscordTelegram);
			btnFilterWhitelist.AccentStyle = (currentFilter == DownloadFilterMode.Whitelist);
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
				List<DownloadItem> results = new List<DownloadItem>();

				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

				// 1. Сканирование Chromium и Firefox браузеров (без временного копирования)
				foreach (KeyValuePair<string, string> kvp in WorldSeedAndChunkbaseForensicsViewerForm.GetBrowserHistoryTargets())
				{
					ScanChromiumHistory(kvp.Key, kvp.Value, results);
				}

				// 2. Сканирование Telegram Desktop папки загрузок
				string tgDownloads = Path.Combine(userProfile, @"Downloads\Telegram Desktop");
				ScanFolderForDownloads("Telegram Desktop", tgDownloads, results);

				// 3. Сканирование Discord Cache & Downloads
				string discordDownloads = Path.Combine(userProfile, @"Downloads");
				ScanDiscordDownloads("Discord / Web", discordDownloads, results);

				// Сортировка по времени загрузки (сначала самые свежие)
				results.Sort(delegate(DownloadItem a, DownloadItem b)
				{
					return b.DownloadTime.CompareTo(a.DownloadTime);
				});

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int cheatCount = results.Count(delegate(DownloadItem x) { return x.IsCheat; });
					int deletedCount = results.Count(delegate(DownloadItem x) { return x.IsCheat && !x.ExistsOnDisk; });

					if (deletedCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УДАЛЕННЫХ ЧИТОВ (5.08): {0}", deletedCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else if (cheatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 СКАЧАННЫХ ЧИТОВ (5.01): {0}", cheatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Загрузок проверено: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanChromiumHistory(string browserName, string historyPath, List<DownloadItem> results)
		{
			if (!File.Exists(historyPath)) return;

			try
			{
				// Поиск в файле SQLite структуры цепочек URL и целевых путей
				string text = WorldSeedAndChunkbaseForensicsViewerForm.ReadHistoryBounded(historyPath, 16777216, Encoding.UTF8);
				if (string.IsNullOrEmpty(text)) return;

				// Поиск путей загрузки в SQLite (.jar, .exe, .zip, .rar, .bat)
				MatchCollection pathMatches = Regex.Matches(text, @"([A-Za-z]:\\[\w\s\.\-\\\(\)\[\]]+\.(?:jar|exe|zip|rar|7z|bat|cfg|json))", RegexOptions.IgnoreCase);

				HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (Match m in pathMatches)
				{
					string targetPath = m.Groups[1].Value.Trim();
					if (seen.Contains(targetPath)) continue;
					seen.Add(targetPath);

					string fileName = Path.GetFileName(targetPath);
					if (string.IsNullOrEmpty(fileName)) continue;

					// Ищем ближайший URL к этому пути в сырых данных
					string matchedUrl = "Не определен в кэше";
					int pIdx = text.IndexOf(targetPath, StringComparison.OrdinalIgnoreCase);
					if (pIdx > 0)
					{
						int startSearch = Math.Max(0, pIdx - 1500);
						int lenSearch = Math.Min(3000, text.Length - startSearch);
						string window = text.Substring(startSearch, lenSearch);
						Match urlMatch = Regex.Match(window, @"(https?://[^\s""'<>\x00-\x1f]{4,200})", RegexOptions.IgnoreCase);
						if (urlMatch.Success)
						{
							matchedUrl = urlMatch.Groups[1].Value;
						}
					}

					DateTime dlTime = DateTime.MinValue;
					try
					{
						if (File.Exists(targetPath))
						{
							dlTime = File.GetCreationTime(targetPath);
						}
						else
						{
							dlTime = File.GetLastWriteTime(historyPath);
						}
					}
					catch { }

					AddDownloadRecord(browserName, fileName, targetPath, matchedUrl, dlTime, results);
				}
			}
			catch { }
		}

		private void ScanFolderForDownloads(string sourceName, string folderPath, List<DownloadItem> results)
		{
			if (!Directory.Exists(folderPath)) return;
			try
			{
				string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
				foreach (string file in files)
				{
					try
					{
						FileInfo fi = new FileInfo(file);
						string ext = fi.Extension.ToLowerInvariant();
						if (ext == ".jar" || ext == ".exe" || ext == ".zip" || ext == ".rar" || ext == ".bat" || ext == ".cfg")
						{
							AddDownloadRecord(sourceName, fi.Name, fi.FullName, "Telegram Канал / Группа", fi.CreationTime, results);
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void ScanDiscordDownloads(string sourceName, string downloadsDir, List<DownloadItem> results)
		{
			if (!Directory.Exists(downloadsDir)) return;
			try
			{
				string[] files = Directory.GetFiles(downloadsDir, "*.*", SearchOption.TopDirectoryOnly);
				foreach (string file in files)
				{
					try
					{
						FileInfo fi = new FileInfo(file);
						string ext = fi.Extension.ToLowerInvariant();
						if (ext == ".jar" || ext == ".exe" || ext == ".zip" || ext == ".bat")
						{
							// Проверяем ADS (Zone.Identifier) на наличие HostUrl=cdn.discordapp.com
							string zoneFile = file + ":Zone.Identifier";
							string url = "Локальная папка Загрузки";
							bool isDiscord = false;

							if (File.Exists(zoneFile))
							{
								string zoneContent = File.ReadAllText(zoneFile);
								if (zoneContent.IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0)
								{
									url = "Discord CDN (cdn.discordapp.com)";
									isDiscord = true;
								}
								else
								{
									Match m = Regex.Match(zoneContent, @"HostUrl=(.+)");
									if (m.Success) url = m.Groups[1].Value.Trim();
								}
							}

							if (isDiscord || CheatRegex.IsMatch(fi.Name))
							{
								AddDownloadRecord(isDiscord ? "Discord Вложения" : sourceName, fi.Name, fi.FullName, url, fi.CreationTime, results);
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void AddDownloadRecord(string sourceApp, string fileName, string targetPath, string downloadUrl, DateTime dlTime, List<DownloadItem> results)
		{
			string lowerName = fileName.ToLowerInvariant();
			string lowerPath = targetPath.ToLowerInvariant();
			string lowerUrl = (downloadUrl ?? "").ToLowerInvariant();

			bool isWhitelisted = false;
			foreach (string w in WhitelistTerms)
			{
				if (lowerName.Contains(w) || lowerPath.Contains(w))
				{
					isWhitelisted = true;
					break;
				}
			}

			bool isCheat = !isWhitelisted && (CheatRegex.IsMatch(lowerName) || CheatRegex.IsMatch(lowerPath) || CheatRegex.IsMatch(lowerUrl));
			bool isMessenger = sourceApp.Contains("Telegram") || sourceApp.Contains("Discord") || lowerUrl.Contains("discord") || lowerUrl.Contains("t.me");

			bool exists = false;
			long fSize = 0;
			try
			{
				if (File.Exists(targetPath))
				{
					exists = true;
					fSize = new FileInfo(targetPath).Length;
				}
			}
			catch { }

			DownloadItem item = new DownloadItem();
			item.SourceApp = sourceApp;
			item.FileName = fileName;
			item.TargetPath = targetPath;
			item.DownloadUrl = downloadUrl;
			item.FileSize = fSize;
			item.DownloadTime = dlTime;
			item.ExistsOnDisk = exists;
			item.IsCheat = isCheat;
			item.IsMessenger = isMessenger;
			item.IsWhitelisted = isWhitelisted;

			if (isWhitelisted)
			{
				item.Status = "🛡️ Разрешенная загрузка (Белый список)";
				item.RuleViolation = "-";
				item.BanReason = "-";
				item.ForeColor = Ui.AccentGreen;
				item.Details = string.Format("Файл: {0}\nИсточник: {1}\nСтатус: Входит в белый список сервера.", fileName, sourceApp);
			}
			else if (isCheat && !exists)
			{
				item.Status = "🚨 ЧИТ СКАЧАН И УДАЛЕН С ДИСКА (5.08)";
				item.RuleViolation = "5.08";
				item.BanReason = string.Format("Скачивание чита {0} и удаление перед проверкой (источник: {1})", fileName, sourceApp);
				item.ForeColor = Ui.AccentRed;
				item.Details = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА 5.08!\nИгрок скачал запрещенный софт через {0}, после чего удалил его с диска!\nФайл: {1}\nОригинальный путь: {2}\nURL-источник: {3}\nВремя фиксации загрузки: {4:yyyy-MM-dd HH:mm:ss}\nВердикт: Умышленное скачивание чита и сокрытие улик перед ScreenShare.",
					sourceApp, fileName, targetPath, downloadUrl, dlTime);
			}
			else if (isCheat && exists)
			{
				item.Status = "🚨 СКАЧАННЫЙ ЧИТ НАЙДЕН НА ДИСКЕ (5.01)";
				item.RuleViolation = "5.01";
				item.BanReason = string.Format("Использование читов: скачан софт {0}", fileName);
				item.ForeColor = Ui.AccentRed;
				item.Details = string.Format("ОБНАРУЖЕН СКАЧАННЫЙ ЧИТ НА ДИСКЕ!\nФайл: {0}\nПуть: {1}\nИсточник: {2}\nURL: {3}\nВремя загрузки: {4:yyyy-MM-dd HH:mm:ss}\nНарушение: Пункт 5.01 (Использование стороннего запрещенного ПО / читов).",
					fileName, targetPath, sourceApp, downloadUrl, dlTime);
			}
			else if (isMessenger)
			{
				item.Status = exists ? "Файл из мессенджера (на диске)" : "Файл из мессенджера (удален)";
				item.RuleViolation = "-";
				item.BanReason = "-";
				item.ForeColor = Ui.AccentBlue;
				item.Details = string.Format("Файл: {0}\nИсточник: {1}\nПуть: {2}\nURL: {3}\nВремя: {4:yyyy-MM-dd HH:mm:ss}", fileName, sourceApp, targetPath, downloadUrl, dlTime);
			}
			else
			{
				item.Status = exists ? "Загруженный файл (на диске)" : "Загруженный файл (удален)";
				item.RuleViolation = "-";
				item.BanReason = "-";
				item.ForeColor = Ui.TextSecondary;
				item.Details = string.Format("Файл: {0}\nИсточник: {1}\nПуть: {2}\nURL: {3}\nВремя: {4:yyyy-MM-dd HH:mm:ss}", fileName, sourceApp, targetPath, downloadUrl, dlTime);
			}

			results.Add(item);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (DownloadItem item in allEntries)
			{
				if (currentFilter == DownloadFilterMode.CheatsOnly && !item.IsCheat) continue;
				if (currentFilter == DownloadFilterMode.DeletedOnly && item.ExistsOnDisk) continue;
				if (currentFilter == DownloadFilterMode.DiscordTelegram && !item.IsMessenger) continue;
				if (currentFilter == DownloadFilterMode.Whitelist && !item.IsWhitelisted) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.SourceApp != null && item.SourceApp.ToLowerInvariant().Contains(search)) ||
								   (item.FileName != null && item.FileName.ToLowerInvariant().Contains(search)) ||
								   (item.TargetPath != null && item.TargetPath.ToLowerInvariant().Contains(search)) ||
								   (item.DownloadUrl != null && item.DownloadUrl.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.SourceApp);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(item.DownloadTime == DateTime.MinValue ? "—" : item.DownloadTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.ExistsOnDisk ? "✅ Присутствует" : "❌ Удален");
				lvi.SubItems.Add(item.DownloadUrl ?? item.TargetPath);
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

			DownloadItem item = listView.SelectedItems[0].Tag as DownloadItem;
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

			txtDetails.Text = item.Details ?? "Нет дополнительных сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Загрузка не выбрана";
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
				MessageBox.Show("Выберите загрузку с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			DownloadItem item = listView.SelectedItems[0].Tag as DownloadItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenFileFolder_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			DownloadItem item = listView.SelectedItems[0].Tag as DownloadItem;
			if (item == null) return;

			try
			{
				if (File.Exists(item.TargetPath))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", item.TargetPath));
				}
				else
				{
					string dir = Path.GetDirectoryName(item.TargetPath);
					if (Directory.Exists(dir))
					{
						Process.Start("explorer.exe", string.Format("\"{0}\"", dir));
					}
					else
					{
						MessageBox.Show("Файл или директория отсутствует на диске:\n" + item.TargetPath, "Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть каталог: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnOpenUrl_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			DownloadItem item = listView.SelectedItems[0].Tag as DownloadItem;
			if (item == null || string.IsNullOrEmpty(item.DownloadUrl) || !item.DownloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				MessageBox.Show("Для этой записи прямой HTTP/HTTPS адрес не определен.", "URL не найден", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			try
			{
				Process.Start(item.DownloadUrl);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть ссылку: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Загрузки_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА ЗАГРУЗОК (БРАУЗЕРЫ И МЕССЕНДЖЕРЫ)");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (DownloadItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1} (Время: {2:yyyy-MM-dd HH:mm:ss}, Диск: {3})",
							item.SourceApp, item.FileName, item.DownloadTime, item.ExistsOnDisk ? "Присутствует" : "УДАЛЕН"));
						sb.AppendLine(string.Format("Путь:        {0}", item.TargetPath));
						sb.AppendLine(string.Format("URL:         {0}", item.DownloadUrl));
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
