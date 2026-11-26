using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class BitsTransferForensicsViewerForm : Form
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

		private List<BitsTransferForensicItem> allItems = new List<BitsTransferForensicItem>();
		private BackgroundWorker worker;
		private bool isScanning = false;

		private static readonly Regex CheatKeywordsRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger|cleaner|wiper|reghax|selfdelete)",
			RegexOptions.Compiled);

		private static readonly Regex SuspiciousUrlRegex = new Regex(
			@"(?i)(discordapp\.com/attachments/|discord\.com/attachments/|pastebin\.com/raw/|rentry\.co/raw|raw\.githubusercontent\.com|anonfiles\.com|gofile\.io|mega\.nz|workupload\.com|mediafire\.com|catbox\.moe)",
			RegexOptions.Compiled);

		public BitsTransferForensicsViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Подозрительный игрок" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "Форензика фоновых загрузок BITS и передачи файлов (Background Intelligent Transfer Service) - " + this.targetPlayerName;
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
			titleLabel.Text = "📥 ФОРЕНЗИКА ФОНОВЫХ ЗАГРУЗОК BITS (MICROSOFT BITS TRANSFER SERVICE)";
			titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
			titleLabel.ForeColor = Ui.AccentTeal;
			titleLabel.AutoSize = true;
			titleLabel.Location = new Point(15, 10);
			topPanel.Controls.Add(titleLabel);

			summaryLabel = new Label();
			summaryLabel.Text = "Аудит скрытых фоновых загрузок через службу BITS, PowerShell Start-BitsTransfer и базы qmgr.db...";
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
			filterCombo.Items.AddRange(new object[] { "Все задания BITS", "🚨 Загрузки читов (5.01)", "⚠️ Discord / Pastebin / CDN", "🗑️ Удаленные с диска файлы (5.08)", "🧹 Очистка BITS (5.08)" });
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
			listView.Columns.Add("Имя задания BITS", 170);
			listView.Columns.Add("Локальный файл (Local Path)", 260);
			listView.Columns.Add("Время передачи (Timestamp)", 145);
			listView.Columns.Add("Удаленный URL (Remote Source)", 320);
			listView.Columns.Add("Статус файла на диске", 160);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			split.Panel1.Controls.Add(listView);

			// Нижняя панель деталей
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(10, 8, 10, 8);

			Label detailHeader = new Label();
			detailHeader.Text = "🔍 ДЕТАЛЬНЫЙ СУДЕБНЫЙ ПРОТОКОЛ ВЫБРАННОГО ЗАДАНИЯ ПЕРЕДАЧИ BITS:";
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
			statusLabel.Text = "Готов к форензике BITS-заданий.";
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
			statusLabel.Text = "Сканирование событий Microsoft-Windows-Bits-Client/Operational и активных заданий...";
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
			List<BitsTransferForensicItem> results = new List<BitsTransferForensicItem>();
			HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// 1. Чтение журнала событий Microsoft-Windows-Bits-Client/Operational
			ScanBitsEventLog(results, seenKeys);

			// 2. Сканирование активных заданий через bitsadmin.exe /list /allusers /verbose
			ScanActiveBitsJobs(results, seenKeys);

			// 3. Низкоуровневый парсинг баз данных qmgr*.dat и qmgr.db
			ScanQmgrDatabases(results, seenKeys);

			// Сортировка: критические вердикты первыми, затем по времени
			results.Sort(delegate (BitsTransferForensicItem a, BitsTransferForensicItem b)
			{
				bool aCrit = a.VerdictStatus.Contains("5.01") || a.VerdictStatus.Contains("5.08");
				bool bCrit = b.VerdictStatus.Contains("5.01") || b.VerdictStatus.Contains("5.08");
				if (aCrit && !bCrit) return -1;
				if (!aCrit && bCrit) return 1;
				return b.Timestamp.CompareTo(a.Timestamp);
			});

			e.Result = results;
		}

		private void ScanBitsEventLog(List<BitsTransferForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				string query = "*[System[(EventID=3 or EventID=4 or EventID=59 or EventID=60 or EventID=16403)]]";
				EventLogQuery elq = new EventLogQuery("Microsoft-Windows-Bits-Client/Operational", PathType.LogName, query);
				elq.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord record;
					int count = 0;
					while ((record = reader.ReadEvent()) != null && count < 1000)
					{
						using (record)
						{
							count++;
							ParseBitsEventRecord(record, results, seenKeys);
						}
					}
				}
			}
			catch (Exception exLog)
			{
				BitsTransferForensicItem note = new BitsTransferForensicItem();
				note.JobName = "[ЖУРНАЛ BITS ОТКЛЮЧЕН ИЛИ ОЧИЩЕН]";
				note.LocalPath = "-";
				note.RemoteUrl = exLog.Message;
				note.Timestamp = DateTime.Now;
				note.DiskStatus = "Журнал недоступен";
				note.VerdictStatus = "🟡 Журнал BITS не активен";
				note.Details = "Канал Microsoft-Windows-Bits-Client/Operational не вернул событий: " + exLog.Message;
				results.Add(note);
			}
		}

		private void ParseBitsEventRecord(EventRecord record, List<BitsTransferForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				string remoteUrl = "";
				string localPath = "";
				string jobTitle = "";

				if (record.Properties != null)
				{
					foreach (EventProperty prop in record.Properties)
					{
						if (prop == null || prop.Value == null) continue;
						string val = prop.Value.ToString().Trim();
						if (val.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || val.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
						{
							remoteUrl = val;
						}
						else if (val.IndexOf(@":\", StringComparison.OrdinalIgnoreCase) > 0 && (val.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || val.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || val.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || val.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || val.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)))
						{
							localPath = val;
						}
						else if (string.IsNullOrEmpty(jobTitle) && val.Length > 2 && val.Length < 100 && !val.Contains("{") && !val.Contains(@"\"))
						{
							jobTitle = val;
						}
					}
				}

				if (string.IsNullOrEmpty(remoteUrl) && string.IsNullOrEmpty(localPath))
				{
					// Пробуем парсинг описания
					string desc = "";
					try { desc = record.FormatDescription(); } catch { }
					if (!string.IsNullOrEmpty(desc))
					{
						Match mUrl = Regex.Match(desc, @"(https?://[^\s""'<>]+)", RegexOptions.IgnoreCase);
						if (mUrl.Success) remoteUrl = mUrl.Groups[1].Value;

						Match mPath = Regex.Match(desc, @"([A-Za-z]:\\[^\s""'<>]+\.[a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
						if (mPath.Success) localPath = mPath.Groups[1].Value;
					}
				}

				if (string.IsNullOrEmpty(remoteUrl) && string.IsNullOrEmpty(localPath)) return;

				string dedupeKey = remoteUrl + "_" + localPath;
				if (seenKeys.Contains(dedupeKey)) return;
				seenKeys.Add(dedupeKey);

				BitsTransferForensicItem item = new BitsTransferForensicItem();
				item.JobName = string.IsNullOrEmpty(jobTitle) ? ("BITS Job (ID " + record.Id + ")") : jobTitle;
				item.RemoteUrl = string.IsNullOrEmpty(remoteUrl) ? "URL не определен" : remoteUrl;
				item.LocalPath = string.IsNullOrEmpty(localPath) ? "Временный файл BITS" : localPath;
				item.Timestamp = record.TimeCreated.HasValue ? record.TimeCreated.Value : DateTime.Now;

				bool exists = !string.IsNullOrEmpty(localPath) && File.Exists(localPath);
				bool isCheatMatch = CheatKeywordsRegex.IsMatch(remoteUrl + " " + localPath + " " + jobTitle);
				bool isSuspiciousUrl = SuspiciousUrlRegex.IsMatch(remoteUrl);

				if (!string.IsNullOrEmpty(localPath) && localPath.Contains(@":\"))
				{
					item.DiskStatus = exists ? "Файл существует на диске" : "🚨 УДАЛЁН С ДИСКА (5.08)";
				}
				else
				{
					item.DiskStatus = "Служебный кэш BITS";
				}

				if (isCheatMatch)
				{
					item.VerdictStatus = "⛔ ЧИТ В ЗАГРУЗКЕ BITS (5.01)";
				}
				else if (isSuspiciousUrl)
				{
					item.VerdictStatus = "🟣 ЗАГРУЗКА DISCORD/PASTEBIN В BITS (5.01/5.02)";
				}
				else if (!exists && !string.IsNullOrEmpty(localPath) && (localPath.Contains("Temp") || localPath.Contains("AppData")) && (localPath.EndsWith(".jar") || localPath.EndsWith(".exe")))
				{
					item.VerdictStatus = "🚨 СКАЧАННЫЙ ФАЙЛ УДАЛЕН ПЕРЕД СС (5.08)";
				}
				else
				{
					item.VerdictStatus = "🟢 Обычное задание BITS (Система/Обновления)";
				}

				StringBuilder details = new StringBuilder();
				details.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ ФОНОВОЙ ПЕРЕДАЧИ BITS ===");
				details.AppendLine("Имя задания: " + item.JobName);
				details.AppendLine("Удаленный URL источника: " + item.RemoteUrl);
				details.AppendLine("Локальный путь сохранения: " + item.LocalPath);
				details.AppendLine("Время события: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				details.AppendLine("Событие журнала: Event ID " + record.Id + " (Microsoft-Windows-Bits-Client/Operational)");
				details.AppendLine("Статус файла на диске: " + item.DiskStatus);
				details.AppendLine("Вердикт: " + item.VerdictStatus);
				details.AppendLine("--------------------------------------------------------------------------------");
				if (isCheatMatch)
				{
					details.AppendLine("РЕГЛАМЕНТ 5.01: Служба BITS зафиксировала фоновое скачивание чит-клиента или модификации.");
					details.AppendLine("Данная загрузка выполняется в обход браузеров (через PowerShell Start-BitsTransfer или лоадер).");
				}
				else if (item.VerdictStatus.Contains("5.08"))
				{
					details.AppendLine("РЕГЛАМЕНТ 5.08: Файл был загружен через BITS в системную папку и удален пользователем до проверки.");
				}

				item.Details = details.ToString();
				results.Add(item);
			}
			catch { }
		}

		private void ScanActiveBitsJobs(List<BitsTransferForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("bitsadmin.exe", "/list /allusers /verbose");
				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = true;
				psi.StandardOutputEncoding = Encoding.GetEncoding(866);

				using (Process p = Process.Start(psi))
				{
					string output = p.StandardOutput.ReadToEnd();
					p.WaitForExit(3000);

					string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					string curJob = "";
					string curUrl = "";
					string curFile = "";

					foreach (string rawLine in lines)
					{
						string line = rawLine.Trim();
						if (line.StartsWith("DISPLAY NAME:", StringComparison.OrdinalIgnoreCase))
						{
							curJob = line.Substring("DISPLAY NAME:".Length).Trim();
						}
						else if (line.Contains("URL:") || line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
						{
							Match m = Regex.Match(line, @"(https?://[^\s]+)", RegexOptions.IgnoreCase);
							if (m.Success) curUrl = m.Groups[1].Value;
						}
						else if (line.Contains(@":\") && (line.EndsWith(".jar") || line.EndsWith(".exe") || line.EndsWith(".dll") || line.EndsWith(".tmp")))
						{
							Match m = Regex.Match(line, @"([A-Za-z]:\\[^\s""]+)", RegexOptions.IgnoreCase);
							if (m.Success) curFile = m.Groups[1].Value;
						}

						if (!string.IsNullOrEmpty(curUrl) && !seenKeys.Contains(curUrl))
						{
							seenKeys.Add(curUrl);
							BitsTransferForensicItem item = new BitsTransferForensicItem();
							item.JobName = string.IsNullOrEmpty(curJob) ? "Active BITS Job" : curJob;
							item.RemoteUrl = curUrl;
							item.LocalPath = string.IsNullOrEmpty(curFile) ? "Активная передача" : curFile;
							item.Timestamp = DateTime.Now;
							item.DiskStatus = "Активное задание в очереди BITS";

							bool isCheat = CheatKeywordsRegex.IsMatch(curUrl + " " + curFile);
							item.VerdictStatus = isCheat ? "⛔ АКТИВНАЯ ЗАГРУЗКА ЧИТА В BITS (5.01)" : "🟢 Активное задание BITS";

							StringBuilder details = new StringBuilder();
							details.AppendLine("Источник: bitsadmin.exe /list /allusers /verbose");
							details.AppendLine("Задание: " + item.JobName);
							details.AppendLine("URL: " + curUrl);
							details.AppendLine("Файл: " + curFile);
							details.AppendLine("Вердикт: " + item.VerdictStatus);
							item.Details = details.ToString();

							results.Add(item);
							curUrl = "";
						}
					}
				}
			}
			catch { }
		}

		private void ScanQmgrDatabases(List<BitsTransferForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				string bDir = Path.Combine(progData, @"Microsoft\Network\Downloader");
				if (!Directory.Exists(bDir)) return;

				string[] qmgrFiles = Directory.GetFiles(bDir, "qmgr*.*", SearchOption.TopDirectoryOnly);
				foreach (string qFile in qmgrFiles)
				{
					try
					{
						byte[] bytes = File.ReadAllBytes(qFile);
						string text = Encoding.UTF8.GetString(bytes);

						MatchCollection urls = Regex.Matches(text, @"(https?://[^\s\x00-\x1f""']{8,250})", RegexOptions.IgnoreCase);
						foreach (Match m in urls)
						{
							string url = m.Groups[1].Value.Trim();
							if (seenKeys.Contains(url)) continue;
							seenKeys.Add(url);

							bool isCheat = CheatKeywordsRegex.IsMatch(url);
							bool isSusp = SuspiciousUrlRegex.IsMatch(url);

							if (!isCheat && !isSusp) continue;

							BitsTransferForensicItem item = new BitsTransferForensicItem();
							item.JobName = "Carved из " + Path.GetFileName(qFile);
							item.RemoteUrl = url;
							item.LocalPath = "База " + Path.GetFileName(qFile);
							item.Timestamp = File.GetLastWriteTime(qFile);
							item.DiskStatus = "Сохранено в системной базе BITS";
							item.VerdictStatus = isCheat ? "⛔ ЧИТ В БАЗЕ QMGR BITS (5.01)" : "🟣 ПОДОЗРИТЕЛЬНЫЙ URL В БАЗЕ BITS (5.01/5.02)";

							StringBuilder details = new StringBuilder();
							details.AppendLine("Источник: Системная база BITS " + qFile);
							details.AppendLine("Извлеченный URL: " + url);
							details.AppendLine("Время базы: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
							details.AppendLine("Вердикт: " + item.VerdictStatus);
							item.Details = details.ToString();

							results.Add(item);
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			isScanning = false;
			btnRefresh.Enabled = true;
			progressBar.Visible = false;

			if (e.Result is List<BitsTransferForensicItem>)
			{
				allItems = (List<BitsTransferForensicItem>)e.Result;
			}

			ApplyFilter();

			int cheatCount = 0;
			int wipeCount = 0;
			int suspCount = 0;

			foreach (BitsTransferForensicItem it in allItems)
			{
				if (it.VerdictStatus.Contains("5.01")) cheatCount++;
				if (it.VerdictStatus.Contains("5.08")) wipeCount++;
				if (it.VerdictStatus.Contains("DISCORD") || it.VerdictStatus.Contains("ПОДОЗРИТЕЛЬНЫЙ")) suspCount++;
			}

			summaryLabel.Text = string.Format("Всего заданий BITS: {0} | Чит-загрузок (5.01): {1} | Удаленных файлов (5.08): {2} | Discord/Pastebin: {3}",
				allItems.Count, cheatCount, wipeCount, suspCount);

			if (cheatCount > 0 || wipeCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentRed;
			}
			else if (suspCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentOrange;
			}
			else
			{
				summaryLabel.ForeColor = Ui.AccentGreen;
			}

			statusLabel.Text = "Аудит завершен. Выберите задание BITS для просмотра полного протокола передачи.";
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();
			int filterIndex = filterCombo.SelectedIndex;

			foreach (BitsTransferForensicItem item in allItems)
			{
				if (filterIndex == 1 && !item.VerdictStatus.Contains("5.01"))
				{
					continue;
				}
				if (filterIndex == 2 && !item.VerdictStatus.Contains("DISCORD") && !item.VerdictStatus.Contains("ПОДОЗРИТЕЛЬНЫЙ") && !item.VerdictStatus.Contains("5.01"))
				{
					continue;
				}
				if (filterIndex == 3 && !item.DiskStatus.Contains("УДАЛЁН") && !item.VerdictStatus.Contains("5.08"))
				{
					continue;
				}
				if (filterIndex == 4 && !item.VerdictStatus.Contains("ОЧИЩЕН"))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					string combined = (item.JobName + " " + item.RemoteUrl + " " + item.LocalPath + " " + item.VerdictStatus).ToLowerInvariant();
					if (!combined.Contains(search))
					{
						continue;
					}
				}

				ListViewItem lvi = new ListViewItem(item.VerdictStatus);
				lvi.SubItems.Add(item.JobName);
				lvi.SubItems.Add(item.LocalPath);
				lvi.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.RemoteUrl);
				lvi.SubItems.Add(item.DiskStatus);
				lvi.Tag = item;

				if (item.VerdictStatus.Contains("5.01") || item.VerdictStatus.Contains("5.08"))
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 20, 20);
				}
				else if (item.VerdictStatus.Contains("DISCORD") || item.VerdictStatus.Contains("ПОДОЗРИТЕЛЬНЫЙ"))
				{
					lvi.ForeColor = Ui.AccentOrange;
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

			BitsTransferForensicItem item = listView.SelectedItems[0].Tag as BitsTransferForensicItem;
			if (item != null)
			{
				detailBox.Text = item.Details;
			}
		}

		private void CopyBanVerdict()
		{
			if (listView.SelectedItems.Count == 0)
			{
				BitsTransferForensicItem crit = allItems.Find(delegate (BitsTransferForensicItem i) { return i.VerdictStatus.Contains("5.01") || i.VerdictStatus.Contains("5.08"); });
				if (crit != null)
				{
					CopyVerdictForItem(crit);
					return;
				}
				MessageBox.Show("Выберите строку с обнаруженным нарушением для копирования вердикта.", "Выбор строки", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			BitsTransferForensicItem selected = listView.SelectedItems[0].Tag as BitsTransferForensicItem;
			if (selected != null)
			{
				CopyVerdictForItem(selected);
			}
		}

		private void CopyVerdictForItem(BitsTransferForensicItem item)
		{
			string verdict;
			if (item.VerdictStatus.Contains("5.08"))
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Удален файл фоновой загрузки BITS: {1})", targetPlayerName, Path.GetFileName(item.LocalPath));
			}
			else
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (Фоновая загрузка софта BITS: {1})", targetPlayerName, item.JobName);
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

			BitsTransferForensicItem item = listView.SelectedItems[0].Tag as BitsTransferForensicItem;
			if (item == null) return;

			StringBuilder evidence = new StringBuilder();
			evidence.AppendLine("=== СУДЕБНАЯ УЛИКА SCREENSHARE: BITS TRANSFER FORENSICS ===");
			evidence.AppendLine("Игрок: " + targetPlayerName);
			evidence.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Задание BITS: " + item.JobName);
			evidence.AppendLine("URL источника: " + item.RemoteUrl);
			evidence.AppendLine("Файл сохранения: " + item.LocalPath);
			evidence.AppendLine("Время передачи: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			evidence.AppendLine("Статус на диске: " + item.DiskStatus);
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

	public class BitsTransferForensicItem
	{
		public string JobName;
		public string RemoteUrl;
		public string LocalPath;
		public DateTime Timestamp;
		public string DiskStatus;
		public string VerdictStatus;
		public string Details;
	}
}
