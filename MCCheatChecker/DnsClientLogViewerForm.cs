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
	public class DnsClientLogViewerForm : Form
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

		private List<DnsQueryForensicItem> allItems = new List<DnsQueryForensicItem>();
		private BackgroundWorker worker;
		private bool isScanning = false;

		private static readonly Regex CheatDomainsRegex = new Regex(
			@"(?i)(vape\.gg|celestial\.lol|expensive\.fun|nursultan\.fun|akrien\.pw|blast\.hk|yougame\.biz|lolz\.live|exloader\.net|midnight\.im|thunderhack|driplite\.cc|slinky\.gg|catlavan|catlean|wildgrief|boze\.dev|liquidbounce\.net|wurstclient\.net|aristois\.net|meteorclient\.com|richclient|richpremium|neverhook|exptv|flintclient|cellent|kura\.cc)",
			RegexOptions.Compiled);

		private static readonly Regex PasteConfigRegex = new Regex(
			@"(?i)(pastebin\.com|hastebin\.com|rentry\.co|ghostbin|controlc\.com|anonfiles\.com|gofile\.io|mega\.nz|workupload\.com|mediafire\.com|cdn\.discordapp\.com|raw\.githubusercontent\.com)",
			RegexOptions.Compiled);

		public DnsClientLogViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Подозрительный игрок" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "Форензика журнала DNS-запросов и резолвинга чит-доменов (DNS Client Operational) - " + this.targetPlayerName;
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
			titleLabel.Text = "🌐 АУДИТ ЖУРНАЛА DNS-ЗАПРОСОВ (MICROSOFT-WINDOWS-DNS-CLIENT / EVENT 3008)";
			titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
			titleLabel.ForeColor = Ui.AccentTeal;
			titleLabel.AutoSize = true;
			titleLabel.Location = new Point(15, 10);
			topPanel.Controls.Add(titleLabel);

			summaryLabel = new Label();
			summaryLabel.Text = "Чтение журнала событий DNS-клиента Windows и фиксация обращений к серверам авторизации читов...";
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
			filterCombo.Items.AddRange(new object[] { "Все DNS-запросы", "🚨 Домены читов (5.01)", "⚠️ Пасты / Конфиги / CDN", "☕ Запросы javaw.exe", "🧹 Следы зачистки DNS (5.08)" });
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
			listView.Columns.Add("Запрошенный домен (QueryName)", 260);
			listView.Columns.Add("Процесс-источник (PID)", 170);
			listView.Columns.Add("Время запроса (Timestamp)", 150);
			listView.Columns.Add("Результат резолвинга (IP / CNAME)", 230);
			listView.Columns.Add("Тип / Источник", 130);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			split.Panel1.Controls.Add(listView);

			// Нижняя панель деталей
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(10, 8, 10, 8);

			Label detailHeader = new Label();
			detailHeader.Text = "🔍 ДЕТАЛЬНЫЙ СУДЕБНЫЙ ПРОТОКОЛ ВЫБРАННОГО DNS-ЗАПРОСА:";
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
			statusLabel.Text = "Готов к аудиту DNS-запросов и резолвинга.";
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
			statusLabel.Text = "Сбор записей DNS из журнала Microsoft-Windows-DNS-Client/Operational...";
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
			List<DnsQueryForensicItem> results = new List<DnsQueryForensicItem>();
			HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// 1. Попытка чтения журнала событий Microsoft-Windows-DNS-Client/Operational
			try
			{
				string query = "*[System[(EventID=3008 or EventID=3010 or EventID=3018)]]";
				EventLogQuery elq = new EventLogQuery("Microsoft-Windows-DNS-Client/Operational", PathType.LogName, query);
				elq.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord record;
					int count = 0;
					while ((record = reader.ReadEvent()) != null && count < 2000)
					{
						using (record)
						{
							count++;
							ParseDnsEventRecord(record, results, seenKeys);
						}
					}
				}
			}
			catch (Exception exLog)
			{
				// Если журнал отключен или очищен
				DnsQueryForensicItem note = new DnsQueryForensicItem();
				note.Domain = "[ЖУРНАЛ DNS-КЛИЕНТА ОТКЛЮЧЕН ИЛИ ОЧИЩЕН]";
				note.ProcessInfo = "System";
				note.Timestamp = DateTime.Now;
				note.VerdictStatus = "🟡 Журнал DNS не активен";
				note.ResolutionResult = exLog.Message;
				note.SourceType = "DNS Event Log";
				note.Details = "Канал Microsoft-Windows-DNS-Client/Operational не вернул событий.\r\nИнформация: " + exLog.Message + "\r\nБудет выполнен сбор активного кэша DNS.";
				results.Add(note);
			}

			// 2. Дополнительно опрашиваем активный DNS-кэш Windows (ipconfig /displaydns)
			ScanActiveDnsCache(results, seenKeys);

			// Сортировка: критические вердикты первыми, затем по времени запроса
			results.Sort(delegate (DnsQueryForensicItem a, DnsQueryForensicItem b)
			{
				bool aCrit = a.VerdictStatus.Contains("5.01") || a.VerdictStatus.Contains("5.08");
				bool bCrit = b.VerdictStatus.Contains("5.01") || b.VerdictStatus.Contains("5.08");
				if (aCrit && !bCrit) return -1;
				if (!aCrit && bCrit) return 1;
				return b.Timestamp.CompareTo(a.Timestamp);
			});

			e.Result = results;
		}

		private void ParseDnsEventRecord(EventRecord record, List<DnsQueryForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				string queryName = "";
				string queryResults = "";
				int clientPid = 0;

				if (record.Properties != null)
				{
					if (record.Properties.Count > 0 && record.Properties[0].Value != null) queryName = record.Properties[0].Value.ToString().Trim().TrimEnd('.');
					if (record.Properties.Count > 3 && record.Properties[3].Value != null) queryResults = record.Properties[3].Value.ToString().Trim();
					if (record.Properties.Count > 5 && record.Properties[5].Value != null)
					{
						int.TryParse(record.Properties[5].Value.ToString(), out clientPid);
					}
				}

				if (string.IsNullOrEmpty(queryName)) return;

				string dedupeKey = queryName + "_" + clientPid + "_" + (record.TimeCreated.HasValue ? record.TimeCreated.Value.ToString("yyyyMMddHHmm") : "");
				if (seenKeys.Contains(dedupeKey)) return;
				seenKeys.Add(dedupeKey);

				DnsQueryForensicItem item = new DnsQueryForensicItem();
				item.Domain = queryName;
				item.ResolutionResult = string.IsNullOrEmpty(queryResults) ? "Успешно" : queryResults;
				item.Timestamp = record.TimeCreated.HasValue ? record.TimeCreated.Value : DateTime.Now;
				item.SourceType = "Event ID " + record.Id;

				string procName = "Неизвестный процесс";
				if (clientPid > 0)
				{
					try
					{
						Process proc = Process.GetProcessById(clientPid);
						procName = proc.ProcessName + ".exe";
					}
					catch
					{
						procName = "Завершен (PID " + clientPid + ")";
					}
				}
				item.ProcessInfo = procName + " (PID " + clientPid + ")";

				bool isCheatDomain = CheatDomainsRegex.IsMatch(queryName);
				bool isPasteConfig = PasteConfigRegex.IsMatch(queryName);
				bool isJavaw = procName.ToLowerInvariant().Contains("java");

				if (isCheatDomain)
				{
					item.VerdictStatus = "⛔ ЧИТ-ДОМЕН В ЖУРНАЛЕ DNS (5.01)";
				}
				else if (isPasteConfig)
				{
					item.VerdictStatus = "🟣 ХОСТИНГ ПАСТЫ/КОНФИГА (Проверить)";
				}
				else if (isJavaw)
				{
					item.VerdictStatus = "☕ Сетевой запрос Minecraft (javaw)";
				}
				else
				{
					item.VerdictStatus = "🟢 Обычный DNS-запрос";
				}

				StringBuilder details = new StringBuilder();
				details.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ DNS-РЕЗОЛВИНГА ===");
				details.AppendLine("Запрошенный домен: " + queryName);
				details.AppendLine("Время запроса: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				details.AppendLine("Источник: " + item.SourceType + " (Microsoft-Windows-DNS-Client/Operational)");
				details.AppendLine("Процесс-клиент: " + item.ProcessInfo);
				details.AppendLine("Результат DNS-ответа: " + item.ResolutionResult);
				details.AppendLine("Вердикт: " + item.VerdictStatus);
				details.AppendLine("--------------------------------------------------------------------------------");
				if (isCheatDomain)
				{
					details.AppendLine("РЕГЛАМЕНТ 5.01: Зафиксирован сетевой запрос к домену авторизации/обновления чит-клиента!");
					details.AppendLine("Даже если игрок очистил кэш через ipconfig /flushdns, журнал DNS-клиента сохраняет факт обращения.");
				}
				else if (isPasteConfig)
				{
					details.AppendLine("РЕГЛАМЕНТ 5.01/5.02: Обращение к внешнему сервису Pastebin/Rentry/Discord CDN для загрузки конфигурации или кода чита.");
				}

				item.Details = details.ToString();
				results.Add(item);
			}
			catch { }
		}

		private void ScanActiveDnsCache(List<DnsQueryForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("ipconfig.exe", "/displaydns");
				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = true;
				psi.StandardOutputEncoding = Encoding.GetEncoding(866);

				using (Process p = Process.Start(psi))
				{
					string output = p.StandardOutput.ReadToEnd();
					p.WaitForExit(3000);

					string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					string currentRecord = "";

					foreach (string rawLine in lines)
					{
						string line = rawLine.Trim();
						if (line.StartsWith("Имя записи", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Record Name", StringComparison.OrdinalIgnoreCase))
						{
							int colon = line.IndexOf(':');
							if (colon > 0)
							{
								currentRecord = line.Substring(colon + 1).Trim().TrimEnd('.');
							}
						}
						else if (line.StartsWith("Запись", StringComparison.OrdinalIgnoreCase) || line.StartsWith("A (Host) Record", StringComparison.OrdinalIgnoreCase) || line.StartsWith("PTR", StringComparison.OrdinalIgnoreCase))
						{
							if (!string.IsNullOrEmpty(currentRecord))
							{
								int colon = line.IndexOf(':');
								string ip = (colon > 0) ? line.Substring(colon + 1).Trim() : "";

								string dedupeKey = currentRecord + "_active";
								if (!seenKeys.Contains(dedupeKey))
								{
									seenKeys.Add(dedupeKey);

									DnsQueryForensicItem item = new DnsQueryForensicItem();
									item.Domain = currentRecord;
									item.ResolutionResult = string.IsNullOrEmpty(ip) ? "В кэше" : ip;
									item.ProcessInfo = "Активный DNS-кэш Windows";
									item.Timestamp = DateTime.Now;
									item.SourceType = "RAM Cache";

									bool isCheatDomain = CheatDomainsRegex.IsMatch(currentRecord);
									bool isPaste = PasteConfigRegex.IsMatch(currentRecord);

									if (isCheatDomain) item.VerdictStatus = "⛔ ЧИТ-ДОМЕН В КЭШЕ DNS (5.01)";
									else if (isPaste) item.VerdictStatus = "🟣 ХОСТИНГ ПАСТЫ/КОНФИГА (Проверить)";
									else item.VerdictStatus = "🟢 Активная запись DNS-кэша";

									StringBuilder details = new StringBuilder();
									details.AppendLine("Источник: Активный DNS-кэш (ipconfig /displaydns)");
									details.AppendLine("Домен: " + currentRecord);
									details.AppendLine("IP: " + ip);
									details.AppendLine("Вердикт: " + item.VerdictStatus);
									item.Details = details.ToString();

									results.Add(item);
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			isScanning = false;
			btnRefresh.Enabled = true;
			progressBar.Visible = false;

			if (e.Result is List<DnsQueryForensicItem>)
			{
				allItems = (List<DnsQueryForensicItem>)e.Result;
			}

			ApplyFilter();

			int cheatCount = 0;
			int pasteCount = 0;
			int javaCount = 0;

			foreach (DnsQueryForensicItem it in allItems)
			{
				if (it.VerdictStatus.Contains("5.01")) cheatCount++;
				if (it.VerdictStatus.Contains("ХОСТИНГ")) pasteCount++;
				if (it.ProcessInfo.ToLowerInvariant().Contains("java")) javaCount++;
			}

			summaryLabel.Text = string.Format("Всего DNS-запросов: {0} | Чит-доменов: {1} | Паст/Конфигов: {2} | От javaw.exe: {3}",
				allItems.Count, cheatCount, pasteCount, javaCount);

			if (cheatCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentRed;
			}
			else if (pasteCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentOrange;
			}
			else
			{
				summaryLabel.ForeColor = Ui.AccentGreen;
			}

			statusLabel.Text = "Аудит завершен. Выберите запись для просмотра протокола резолвинга.";
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();
			int filterIndex = filterCombo.SelectedIndex;

			foreach (DnsQueryForensicItem item in allItems)
			{
				if (filterIndex == 1 && !item.VerdictStatus.Contains("5.01"))
				{
					continue;
				}
				if (filterIndex == 2 && !item.VerdictStatus.Contains("ХОСТИНГ") && !item.VerdictStatus.Contains("5.01"))
				{
					continue;
				}
				if (filterIndex == 3 && !item.ProcessInfo.ToLowerInvariant().Contains("java"))
				{
					continue;
				}
				if (filterIndex == 4 && !item.VerdictStatus.Contains("5.08") && !item.VerdictStatus.Contains("ОЧИЩЕН"))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					string combined = (item.Domain + " " + item.ProcessInfo + " " + item.ResolutionResult + " " + item.VerdictStatus).ToLowerInvariant();
					if (!combined.Contains(search))
					{
						continue;
					}
				}

				ListViewItem lvi = new ListViewItem(item.VerdictStatus);
				lvi.SubItems.Add(item.Domain);
				lvi.SubItems.Add(item.ProcessInfo);
				lvi.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.ResolutionResult);
				lvi.SubItems.Add(item.SourceType);
				lvi.Tag = item;

				if (item.VerdictStatus.Contains("5.01") || item.VerdictStatus.Contains("5.08"))
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 20, 20);
				}
				else if (item.VerdictStatus.Contains("ХОСТИНГ"))
				{
					lvi.ForeColor = Ui.AccentOrange;
				}
				else if (item.ProcessInfo.ToLowerInvariant().Contains("java"))
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

			DnsQueryForensicItem item = listView.SelectedItems[0].Tag as DnsQueryForensicItem;
			if (item != null)
			{
				detailBox.Text = item.Details;
			}
		}

		private void CopyBanVerdict()
		{
			if (listView.SelectedItems.Count == 0)
			{
				DnsQueryForensicItem crit = allItems.Find(delegate (DnsQueryForensicItem i) { return i.VerdictStatus.Contains("5.01") || i.VerdictStatus.Contains("5.08"); });
				if (crit != null)
				{
					CopyVerdictForItem(crit);
					return;
				}
				MessageBox.Show("Выберите строку с обнаруженным нарушением для копирования вердикта.", "Выбор строки", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			DnsQueryForensicItem selected = listView.SelectedItems[0].Tag as DnsQueryForensicItem;
			if (selected != null)
			{
				CopyVerdictForItem(selected);
			}
		}

		private void CopyVerdictForItem(DnsQueryForensicItem item)
		{
			string verdict;
			if (item.VerdictStatus.Contains("5.08"))
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Очистка журнала DNS-клиента)", targetPlayerName);
			}
			else
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (DNS резолвинг чит-домена: {1} в {2})", targetPlayerName, item.Domain, item.ProcessInfo);
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

			DnsQueryForensicItem item = listView.SelectedItems[0].Tag as DnsQueryForensicItem;
			if (item == null) return;

			StringBuilder evidence = new StringBuilder();
			evidence.AppendLine("=== СУДЕБНАЯ УЛИКА SCREENSHARE: DNS CLIENT RESOLUTION FORENSICS ===");
			evidence.AppendLine("Игрок: " + targetPlayerName);
			evidence.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Домен: " + item.Domain);
			evidence.AppendLine("Процесс-клиент: " + item.ProcessInfo);
			evidence.AppendLine("Время запроса: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			evidence.AppendLine("Результат: " + item.ResolutionResult);
			evidence.AppendLine("Источник: " + item.SourceType);
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

	public class DnsQueryForensicItem
	{
		public string Domain;
		public string ProcessInfo;
		public DateTime Timestamp;
		public string ResolutionResult;
		public string SourceType;
		public string VerdictStatus;
		public string Details;
	}
}
