using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class BrowserSearchAndHistoryViewerForm : Form
	{
		private string targetPlayerName;
		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBox detailBox;
		private Label statusLabel;
		private Label summaryLabel;
		private DarkProgressBar progressBar;
		private DarkButton btnRefresh;
		private DarkButton btnCopyBan;
		private DarkButton btnExportEvidence;
		private ComboBox filterCombo;

		private List<BrowserHistoryForensicItem> allItems = new List<BrowserHistoryForensicItem>();
		private BackgroundWorker worker;
		private bool isScanning = false;

		private static readonly Regex CheatKeywordsRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger|blast\.hk|yougame\.biz|lolz\.live|funtime|holyworld|reallyworld|reghax|selfdelete)",
			RegexOptions.Compiled);

		private static readonly Regex CleanerKeywordsRegex = new Regex(
			@"(?i)(очистк|очистить|скрыть\s*чит|как\s*пройти\s*ss|пройти\s*проверк|обход\s*проверк|usn\s*journal|prefetch|wevtutil|fsutil|bat\s*cleaner|wiper|cleaner|кряк|скачать\s*чит|чит\s*на|кфг\s*на|конфиг|хрей|x-ray)",
			RegexOptions.Compiled);

		private static readonly Regex CheatDomainsRegex = new Regex(
			@"(?i)(vape\.gg|celestial\.lol|expensive\.fun|nursultan\.fun|akrien\.pw|blast\.hk|yougame\.biz|lolz\.live|exloader\.net|midnight\.im|freecam|hitbox)",
			RegexOptions.Compiled);

		public BrowserSearchAndHistoryViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Подозрительный игрок" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "Инспектор поисковых запросов и веб-истории браузеров (Chrome, Edge, Yandex, Opera, Firefox) - " + this.targetPlayerName;
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Font = new Font("Segoe UI", 9f);
			this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

			// Верхняя панель
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 115;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(15, 12, 15, 10);

			Label titleLabel = new Label();
			titleLabel.Text = "🌐 ИНСПЕКТОР ПОИСКОВЫХ ЗАПРОСОВ И ИСТОРИИ БРАУЗЕРОВ (WEB FORENSICS)";
			titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
			titleLabel.ForeColor = Ui.AccentTeal;
			titleLabel.AutoSize = true;
			titleLabel.Location = new Point(15, 10);
			topPanel.Controls.Add(titleLabel);

			summaryLabel = new Label();
			summaryLabel.Text = "Сбор истории и поисковых запросов Google, Yandex, YouTube из всех профилей браузеров...";
			summaryLabel.Font = new Font("Segoe UI", 9f);
			summaryLabel.ForeColor = Ui.TextMuted;
			summaryLabel.AutoSize = true;
			summaryLabel.Location = new Point(16, 36);
			topPanel.Controls.Add(summaryLabel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(16, 75);
			lblSearch.ForeColor = Ui.TextMuted;
			topPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(68, 72);
			searchBox.Size = new Size(260, 25);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			topPanel.Controls.Add(searchBox);

			Label lblFilter = new Label();
			lblFilter.Text = "Фильтр:";
			lblFilter.AutoSize = true;
			lblFilter.Location = new Point(345, 75);
			lblFilter.ForeColor = Ui.TextMuted;
			topPanel.Controls.Add(lblFilter);

			filterCombo = new ComboBox();
			filterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			filterCombo.BackColor = Ui.SurfaceAlt;
			filterCombo.ForeColor = Ui.TextPrimary;
			filterCombo.FlatStyle = FlatStyle.Flat;
			filterCombo.Location = new Point(405, 72);
			filterCombo.Size = new Size(225, 25);
			filterCombo.Items.AddRange(new object[] { "Все записи", "🚨 Поиски читов и сокрытия (5.01/5.08)", "🌐 Сайты читов и Web-GUI", "🔍 Поисковые запросы (Google/Yandex)", "🧹 Очистка истории (5.08)" });
			filterCombo.SelectedIndex = 0;
			filterCombo.SelectedIndexChanged += delegate { ApplyFilter(); };
			topPanel.Controls.Add(filterCombo);

			btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(645, 70);
			btnRefresh.Size = new Size(135, 28);
			btnRefresh.Click += delegate { StartScan(); };
			topPanel.Controls.Add(btnRefresh);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать вердикт бана";
			btnCopyBan.Location = new Point(788, 70);
			btnCopyBan.Size = new Size(185, 28);
			btnCopyBan.Click += delegate { CopyBanVerdict(); };
			topPanel.Controls.Add(btnCopyBan);

			btnExportEvidence = new DarkButton();
			btnExportEvidence.Text = "🛡️ Сохранить улику (SHA-256)";
			btnExportEvidence.Location = new Point(980, 70);
			btnExportEvidence.Size = new Size(175, 28);
			btnExportEvidence.Click += delegate { ExportEvidence(); };
			topPanel.Controls.Add(btnExportEvidence);

			this.Controls.Add(topPanel);

			// SplitContainer для таблицы и инспектора деталей
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 400;
			split.Panel1MinSize = 200;
			split.Panel2MinSize = 100;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("Вердикт / Статус", 210);
			listView.Columns.Add("Браузер / Профиль", 160);
			listView.Columns.Add("Тип артефакта", 130);
			listView.Columns.Add("Поисковый запрос / Домен", 280);
			listView.Columns.Add("Время активности", 145);
			listView.Columns.Add("Полный URL / Аргументы запроса", 350);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			split.Panel1.Controls.Add(listView);

			// Нижняя панель деталей
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(10, 8, 10, 8);

			Label detailHeader = new Label();
			detailHeader.Text = "🔍 ДЕТАЛЬНЫЙ СУДЕБНЫЙ ПРОТОКОЛ ВЫБРАННОГО АРТЕФАКТА ВЕБ-ФОРЕНЗИКИ:";
			detailHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			detailHeader.ForeColor = Ui.AccentTeal;
			detailHeader.Dock = DockStyle.Top;
			detailHeader.Height = 22;
			detailContainer.Controls.Add(detailHeader);

			detailBox = new DarkTextBox();
			detailBox.Dock = DockStyle.Fill;
			detailBox.Multiline = true;
			detailBox.ScrollBars = ScrollBars.Both;
			detailBox.Font = new Font("Consolas", 9f);
			detailBox.ReadOnly = true;
			detailContainer.Controls.Add(detailBox);

			split.Panel2.Controls.Add(detailContainer);
			this.Controls.Add(split);

			// Нижняя строка статуса
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 34;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(10, 5, 10, 5);

			statusLabel = new Label();
			statusLabel.Text = "Готов к аудиту веб-истории и поисковых запросов.";
			statusLabel.AutoSize = true;
			statusLabel.Location = new Point(10, 8);
			statusLabel.ForeColor = Ui.TextMuted;
			bottomPanel.Controls.Add(statusLabel);

			progressBar = new DarkProgressBar();
			progressBar.Size = new Size(180, 16);
			progressBar.Location = new Point(this.ClientSize.Width - 200, 9);
			progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			progressBar.Visible = false;
			bottomPanel.Controls.Add(progressBar);

			this.Controls.Add(bottomPanel);
		}

		private void StartScan()
		{
			if (isScanning) return;
			isScanning = true;
			btnRefresh.Enabled = false;
			progressBar.Visible = true;
			statusLabel.Text = "Парсинг баз SQLite браузеров Chrome, Edge, Yandex, Opera, Firefox...";
			allItems.Clear();
			listView.Items.Clear();
			detailBox.Text = "";

			worker = new BackgroundWorker();
			worker.DoWork += Worker_DoWork;
			worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			worker.RunWorkerAsync();
		}

		private void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<BrowserHistoryForensicItem> results = new List<BrowserHistoryForensicItem>();
			List<KeyValuePair<string, string>> browserHistoryPaths = WorldSeedAndChunkbaseForensicsViewerForm.GetBrowserHistoryTargets();

			DateTime now = DateTime.Now;
			TimeSpan systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount);

			foreach (KeyValuePair<string, string> kvp in browserHistoryPaths)
			{
				string browserProfile = kvp.Key;
				string historyFile = kvp.Value;

				ScanSingleBrowserHistory(browserProfile, historyFile, results, now, systemUptime);
			}

			// Сортировка: критические вердикты первыми, затем по времени активности
			results.Sort(delegate (BrowserHistoryForensicItem a, BrowserHistoryForensicItem b)
			{
				bool aCrit = a.VerdictStatus.Contains("5.01") || a.VerdictStatus.Contains("5.08");
				bool bCrit = b.VerdictStatus.Contains("5.01") || b.VerdictStatus.Contains("5.08");
				if (aCrit && !bCrit) return -1;
				if (!aCrit && bCrit) return 1;
				return b.ActivityTime.CompareTo(a.ActivityTime);
			});

			e.Result = results;
		}

		private void ScanSingleBrowserHistory(string browserProfile, string historyFile, List<BrowserHistoryForensicItem> results, DateTime now, TimeSpan uptime)
		{
			if (!File.Exists(historyFile)) return;

			FileInfo fi = new FileInfo(historyFile);
			DateTime fileModified = fi.LastWriteTime;
			long fileSize = fi.Length;

			// Проверка на экстренную зачистку истории (если размер файла минимален и изменен недавно при долгом аптайме)
			TimeSpan timeSinceModified = now - fileModified;
			if (fileSize < 24576 && timeSinceModified.TotalMinutes < 20 && uptime.TotalMinutes > 30)
			{
				BrowserHistoryForensicItem wipe = new BrowserHistoryForensicItem();
				wipe.BrowserProfile = browserProfile;
				wipe.ArtifactType = "Очистка истории";
				wipe.TitleOrQuery = "🚨 ИСТОРИЯ БРАУЗЕРА БЫЛА ОЧИЩЕНА ПЕРЕД ПРОВЕРКОЙ (5.08)";
				wipe.FullUrl = historyFile;
				wipe.ActivityTime = fileModified;
				wipe.VerdictStatus = "🚨 ЗАЧИСТКА ИСТОРИИ (5.08)";
				wipe.Details = string.Format("База данных истории {0} имеет минимальный размер ({1} байт) и была модифицирована всего {2} мин назад при аптайме {3} мин.\r\nИгрок произвел очистку истории браузера перед или во время вызова на проверку.",
					browserProfile, fileSize, (int)timeSinceModified.TotalMinutes, (int)uptime.TotalMinutes);
				results.Add(wipe);
			}

			try
			{
				string rawText = WorldSeedAndChunkbaseForensicsViewerForm.ReadHistoryBounded(historyFile, 16777216, Encoding.UTF8);
				if (string.IsNullOrEmpty(rawText)) return;

				// Извлечение URL
				MatchCollection urlMatches = Regex.Matches(rawText, @"(https?://[^\s""'<>\x00-\x1f]{5,350})", RegexOptions.IgnoreCase);
				HashSet<string> seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (Match m in urlMatches)
				{
					string url = m.Groups[1].Value.Trim();
					if (seenUrls.Contains(url)) continue;
					if (url.EndsWith(".css") || url.EndsWith(".js") || url.EndsWith(".png") || url.EndsWith(".woff2") || url.EndsWith(".svg") || url.EndsWith(".ico")) continue;

					seenUrls.Add(url);

					// 1. Анализ поисковых запросов Google, Yandex, YouTube, DuckDuckGo
					string searchQuery = ExtractSearchQuery(url);
					bool isSearch = !string.IsNullOrEmpty(searchQuery);

					bool isCheatSite = CheatDomainsRegex.IsMatch(url);
					bool isCheatQuery = false;
					bool isCleanerQuery = false;

					if (isSearch)
					{
						isCheatQuery = CheatKeywordsRegex.IsMatch(searchQuery);
						isCleanerQuery = CleanerKeywordsRegex.IsMatch(searchQuery);
					}
					else
					{
						isCheatQuery = CheatKeywordsRegex.IsMatch(url);
					}

					// Игнорируем обычный сетевой мусор, если нет совпадений по читам/поиску
					if (!isSearch && !isCheatSite && !isCheatQuery)
					{
						continue;
					}

					BrowserHistoryForensicItem item = new BrowserHistoryForensicItem();
					item.BrowserProfile = browserProfile;
					item.FullUrl = url;
					item.ActivityTime = fileModified;

					if (isSearch)
					{
						item.ArtifactType = "Поисковый запрос";
						item.TitleOrQuery = searchQuery;

						if (isCheatQuery || isCleanerQuery)
						{
							item.VerdictStatus = "⛔ ПОИСК ЧИТА / СОКРЫТИЯ (5.01/5.08)";
						}
						else
						{
							item.VerdictStatus = "🔍 Обычный поисковый запрос";
						}
					}
					else if (isCheatSite)
					{
						item.ArtifactType = "Сайт чит-клиента";
						item.TitleOrQuery = GetHostSafe(url);
						item.VerdictStatus = "⛔ САЙТ ЧИТ-КЛИЕНТА (5.01)";
					}
					else if (isCheatQuery)
					{
						item.ArtifactType = "Подозрительный URL читов";
						item.TitleOrQuery = GetHostSafe(url);
						item.VerdictStatus = "🟣 УПОМИНАНИЕ ЧИТА В URL (5.01)";
					}

					StringBuilder details = new StringBuilder();
					details.AppendLine("Браузер / Профиль: " + browserProfile);
					details.AppendLine("Файл БД: " + historyFile);
					details.AppendLine("Тип артефакта: " + item.ArtifactType);
					details.AppendLine("Вердикт: " + item.VerdictStatus);
					details.AppendLine("Время модификации базы: " + fileModified.ToString("yyyy-MM-dd HH:mm:ss"));
					if (isSearch)
					{
						details.AppendLine("ПОИСКОВЫЙ ЗАПРОС: \"" + searchQuery + "\"");
					}
					details.AppendLine("ПОЛНЫЙ URL: " + url);
					details.AppendLine("--------------------------------------------------------------------------------");
					if (item.VerdictStatus.Contains("5.01"))
					{
						details.AppendLine("РЕГЛАМЕНТ 5.01: Зафиксирован целенаправленный поиск, переход или использование чит-клиента через веб-браузер.");
					}
					else if (item.VerdictStatus.Contains("5.08"))
					{
						details.AppendLine("РЕГЛАМЕНТ 5.08: Зафиксирован поиск инструкций или утилит сокрытия улик / очистки перед проверкой.");
					}

					item.Details = details.ToString();
					results.Add(item);
				}
			}
			catch { }
		}

		private static string ExtractSearchQuery(string url)
		{
			try
			{
				// Google
				Match mG = Regex.Match(url, @"google\.[a-z\.]+/search\?(?:[^&]*&)*q=([^&#]+)", RegexOptions.IgnoreCase);
				if (mG.Success) return CleanUrlTerm(mG.Groups[1].Value);

				// Yandex
				Match mY = Regex.Match(url, @"yandex\.[a-z\.]+/search/\?(?:[^&]*&)*text=([^&#]+)", RegexOptions.IgnoreCase);
				if (mY.Success) return CleanUrlTerm(mY.Groups[1].Value);

				// YouTube
				Match mYt = Regex.Match(url, @"youtube\.[a-z\.]+/results\?(?:[^&]*&)*search_query=([^&#]+)", RegexOptions.IgnoreCase);
				if (mYt.Success) return CleanUrlTerm(mYt.Groups[1].Value);

				// DuckDuckGo
				Match mD = Regex.Match(url, @"duckduckgo\.[a-z\.]+/\?(?:[^&]*&)*q=([^&#]+)", RegexOptions.IgnoreCase);
				if (mD.Success) return CleanUrlTerm(mD.Groups[1].Value);

				// Mail.ru
				Match mM = Regex.Match(url, @"go\.mail\.ru/search\?(?:[^&]*&)*q=([^&#]+)", RegexOptions.IgnoreCase);
				if (mM.Success) return CleanUrlTerm(mM.Groups[1].Value);
			}
			catch { }
			return null;
		}

		private static string CleanUrlTerm(string raw)
		{
			try
			{
				string decoded = Uri.UnescapeDataString(raw.Replace('+', ' ')).Trim();
				return decoded;
			}
			catch
			{
				return raw.Replace('+', ' ');
			}
		}

		private static string GetHostSafe(string url)
		{
			try
			{
				Uri u = new Uri(url);
				return u.Host;
			}
			catch
			{
				return url;
			}
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			isScanning = false;
			btnRefresh.Enabled = true;
			progressBar.Visible = false;

			if (e.Result is List<BrowserHistoryForensicItem>)
			{
				allItems = (List<BrowserHistoryForensicItem>)e.Result;
			}

			ApplyFilter();

			int cheatCount = 0;
			int wipeCount = 0;
			int searchCount = 0;

			foreach (BrowserHistoryForensicItem it in allItems)
			{
				if (it.VerdictStatus.Contains("5.01")) cheatCount++;
				if (it.VerdictStatus.Contains("5.08")) wipeCount++;
				if (it.ArtifactType == "Поисковый запрос") searchCount++;
			}

			summaryLabel.Text = string.Format("Найдено артефактов: {0} | Чит-запросов и сайтов: {1} | Зачисток истории (5.08): {2} | Поисковых запросов: {3}",
				allItems.Count, cheatCount, wipeCount, searchCount);

			if (cheatCount > 0 || wipeCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentRed;
			}
			else if (searchCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentTeal;
			}
			else
			{
				summaryLabel.ForeColor = Ui.AccentGreen;
			}

			statusLabel.Text = "Аудит завершен. Выберите запись для просмотра полного протокола.";
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();
			int filterIndex = filterCombo.SelectedIndex;

			foreach (BrowserHistoryForensicItem item in allItems)
			{
				if (filterIndex == 1 && !item.VerdictStatus.Contains("5.01") && !item.VerdictStatus.Contains("5.08"))
				{
					continue;
				}
				if (filterIndex == 2 && !item.ArtifactType.Contains("Сайт") && !item.VerdictStatus.Contains("САЙТ"))
				{
					continue;
				}
				if (filterIndex == 3 && item.ArtifactType != "Поисковый запрос")
				{
					continue;
				}
				if (filterIndex == 4 && !item.VerdictStatus.Contains("5.08"))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					string combined = (item.TitleOrQuery + " " + item.FullUrl + " " + item.BrowserProfile + " " + item.VerdictStatus).ToLowerInvariant();
					if (!combined.Contains(search))
					{
						continue;
					}
				}

				ListViewItem lvi = new ListViewItem(item.VerdictStatus);
				lvi.SubItems.Add(item.BrowserProfile);
				lvi.SubItems.Add(item.ArtifactType);
				lvi.SubItems.Add(item.TitleOrQuery);
				lvi.SubItems.Add(item.ActivityTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.FullUrl);
				lvi.Tag = item;

				if (item.VerdictStatus.Contains("5.01") || item.VerdictStatus.Contains("5.08"))
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 20, 20);
				}
				else if (item.ArtifactType == "Поисковый запрос")
				{
					lvi.ForeColor = Ui.AccentTeal;
				}
				else
				{
					lvi.ForeColor = Ui.TextPrimary;
				}

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				detailBox.Text = "";
				return;
			}

			BrowserHistoryForensicItem item = listView.SelectedItems[0].Tag as BrowserHistoryForensicItem;
			if (item != null)
			{
				detailBox.Text = item.Details;
			}
		}

		private void CopyBanVerdict()
		{
			if (listView.SelectedItems.Count == 0)
			{
				BrowserHistoryForensicItem crit = allItems.Find(delegate (BrowserHistoryForensicItem i) { return i.VerdictStatus.Contains("5.01") || i.VerdictStatus.Contains("5.08"); });
				if (crit != null)
				{
					CopyVerdictForItem(crit);
					return;
				}
				MessageBox.Show("Выберите строку с обнаруженным нарушением для копирования вердикта.", "Выбор строки", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			BrowserHistoryForensicItem selected = listView.SelectedItems[0].Tag as BrowserHistoryForensicItem;
			if (selected != null)
			{
				CopyVerdictForItem(selected);
			}
		}

		private void CopyVerdictForItem(BrowserHistoryForensicItem item)
		{
			string verdict;
			if (item.VerdictStatus.Contains("5.08"))
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Очистка истории {1})", targetPlayerName, item.BrowserProfile);
			}
			else if (item.ArtifactType == "Поисковый запрос")
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (Поиск софта в браузере: \"{1}\")", targetPlayerName, item.TitleOrQuery);
			}
			else
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (Посещение сайта чита {1})", targetPlayerName, item.TitleOrQuery);
			}

			try
			{
				Clipboard.SetText(verdict);
				MessageBox.Show("Бан-вердикт успешно скопирован в буфер обмена:\r\n\r\n" + verdict, "Вердикт скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportEvidence()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите строку для экспорта криминалистической улики.", "Экспорт доказательства", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			BrowserHistoryForensicItem item = listView.SelectedItems[0].Tag as BrowserHistoryForensicItem;
			if (item == null) return;

			StringBuilder evidence = new StringBuilder();
			evidence.AppendLine("=== СУДЕБНАЯ УЛИКА SCREENSHARE: BROWSER SEARCH & HISTORY FORENSICS ===");
			evidence.AppendLine("Игрок: " + targetPlayerName);
			evidence.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Браузер / Профиль: " + item.BrowserProfile);
			evidence.AppendLine("Тип артефакта: " + item.ArtifactType);
			evidence.AppendLine("Запрос / Заголовок: " + item.TitleOrQuery);
			evidence.AppendLine("URL: " + item.FullUrl);
			evidence.AppendLine("Вердикт: " + item.VerdictStatus);
			evidence.AppendLine("----------------------------------------------------------------");
			evidence.AppendLine(item.Details);

			try
			{
				Clipboard.SetText(evidence.ToString());
				MessageBox.Show("Криминалистическая улика успешно скопирована в буфер обмена!", "Улика зафиксирована", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка копирования улики: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	public class BrowserHistoryForensicItem
	{
		public string BrowserProfile;
		public string ArtifactType;
		public string TitleOrQuery;
		public string FullUrl;
		public DateTime ActivityTime;
		public string VerdictStatus;
		public string Details;
	}
}
