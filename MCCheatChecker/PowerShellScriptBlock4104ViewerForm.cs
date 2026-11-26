using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class PowerShellScriptBlock4104ViewerForm : Form
	{
		#region Data Models

		public enum ScriptBlockFilterCategory
		{
			All,
			ViolationsWiping508,
			CheatDownloads501,
			Base64Obfuscation,
			Recent24Hours,
			Whitelisted
		}

		public class ScriptBlockForensicItem
		{
			public int Index;
			public DateTime ExecutionTime;
			public string DetectedThreat;
			public ScriptBlockFilterCategory Category;
			public string CodeSnippet;
			public string Verdict;
			public string TargetRule;
			public string UserSid;
			public bool IsViolation;
			public bool IsBase64;
			public bool IsWiping;
			public bool IsDownload;
			public bool IsAllowed;
			public Color RowColor;
			public string FullCode;
			public string DecodedBase64Payload;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ScriptBlockFilterCategory currentFilter = ScriptBlockFilterCategory.All;
		private List<ScriptBlockForensicItem> allItems = new List<ScriptBlockForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiWiping;
		private Label lblKpiDownloads;
		private Label lblKpiBase64;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterWiping;
		private DarkButton btnFilterDownloads;
		private DarkButton btnFilterBase64;
		private DarkButton btnFilterRecent;
		private DarkButton btnFilterSafe;

		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public PowerShellScriptBlock4104ViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "📜 Инспектор блоков кода PowerShell ScriptBlock (Event ID 4104) — MCCheatChecker";
			Size = new Size(1300, 780);
			MinimumSize = new Size(1060, 600);
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
			lblHeaderTitle.Text = "📜 Инспектор выполненных блоков кода PowerShell ScriptBlock (Журнал Event ID 4104)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Глубокий аудит Microsoft-Windows-PowerShell/Operational (Event 4104): фиксация скрытых батников зачистки (wevtutil, fsutil), загрузчиков читов (WebClient, BitsTransfer) и авто-декодирование Base64 (5.01 / 5.08).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация чтения журнала событий PowerShell 4104...";
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

			lblKpiTotal = CreateKpiLabel("Блоков: 0", Ui.TextPrimary, 0, kpiPanel);
			lblKpiWiping = CreateKpiLabel("Вайперы (5.08): 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiDownloads = CreateKpiLabel("Загрузки (5.01): 0", Ui.AccentOrange, 272, kpiPanel);
			lblKpiBase64 = CreateKpiLabel("Base64 код: 0", Ui.AccentPurple, 408, kpiPanel);
			lblKpiSafe = CreateKpiLabel("Штатные: 0", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Filter bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все блоки кода", ScriptBlockFilterCategory.All, ref bx, filterBar);
			btnFilterWiping = CreateFilterButton("🚨 Сокрытие улик (5.08)", ScriptBlockFilterCategory.ViolationsWiping508, ref bx, filterBar);
			btnFilterDownloads = CreateFilterButton("⚡ Загрузки читов (5.01)", ScriptBlockFilterCategory.CheatDownloads501, ref bx, filterBar);
			btnFilterBase64 = CreateFilterButton("🔐 Base64 обфускация", ScriptBlockFilterCategory.Base64Obfuscation, ref bx, filterBar);
			btnFilterRecent = CreateFilterButton("🕒 За 24 часа", ScriptBlockFilterCategory.Recent24Hours, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Штатные скрипты", ScriptBlockFilterCategory.Whitelisted, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(200, 26);
			searchBox.Location = new Point(filterBar.Width - 216, 9);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 280, 13);
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
			listView.Columns.Add("Время выполнения", 135, HorizontalAlignment.Center);
			listView.Columns.Add("Обнаруженная угроза / Маркер", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Фрагмент кода ScriptBlock (PowerShell)", 340, HorizontalAlignment.Left);
			listView.Columns.Add("Вердикт криминалиста", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Пользователь / SID", 180, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📋 Скопировать полный код ScriptBlock", null, delegate { CopyFullScript(); });
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
			lblDetailHeader.Text = "📄 Полный листинг кода PowerShell ScriptBlock (включая деобфусцированный Base64):";
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

			DarkButton btnCopyCode = new DarkButton();
			btnCopyCode.Text = "📋 Скопировать код";
			btnCopyCode.Size = new Size(160, 32);
			btnCopyCode.Location = new Point(448, 6);
			btnCopyCode.Click += delegate { CopyFullScript(); };
			btnBar.Controls.Add(btnCopyCode);

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Text = "🔄 Пересканировать 4104";
			btnRescan.Size = new Size(180, 32);
			btnRescan.Location = new Point(btnBar.Width - 192, 6);
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

		private DarkButton CreateFilterButton(string text, ScriptBlockFilterCategory cat, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(165, 30);
			btn.Location = new Point(x, 7);
			x += 171;
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
			lblStatusPill.Text = "⏳ Чтение событий Microsoft-Windows-PowerShell/Operational (EventID 4104)...";
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
			List<ScriptBlockForensicItem> list = new List<ScriptBlockForensicItem>();
			int idx = 1;

			try
			{
				EventLogQuery query = new EventLogQuery("Microsoft-Windows-PowerShell/Operational", PathType.LogName, "*[System[(EventID=4104)]]");
				using (EventLogReader reader = new EventLogReader(query))
				{
					EventRecord record;
					while ((record = reader.ReadEvent()) != null)
					{
						using (record)
						{
							try
							{
								DateTime time = record.TimeCreated ?? DateTime.MinValue;
								string userId = record.UserId != null ? record.UserId.Value : "SYSTEM";
								string scriptText = "";

								// Извлечение ScriptBlockText из свойств события
								if (record.Properties != null && record.Properties.Count > 2)
								{
									object val = record.Properties[2].Value;
									if (val != null) scriptText = val.ToString();
								}

								if (string.IsNullOrEmpty(scriptText))
								{
									scriptText = record.FormatDescription();
								}

								if (string.IsNullOrEmpty(scriptText)) continue;

								// Анализ содержимого скрипта
								AnalyzeScriptBlock(scriptText, time, userId, list, ref idx);
							}
							catch { }
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("EventLogReader 4104 error: " + ex.Message);
			}

			// Сортировка по времени (сначала свежие)
			list.Sort(delegate(ScriptBlockForensicItem a, ScriptBlockForensicItem b) { return b.ExecutionTime.CompareTo(a.ExecutionTime); });
			for (int i = 0; i < list.Count; i++) list[i].Index = i + 1;

			e.Result = list;
		}

		private void AnalyzeScriptBlock(string script, DateTime time, string userId, List<ScriptBlockForensicItem> list, ref int idx)
		{
			string lower = script.ToLowerInvariant();

			bool isWiping = false;
			bool isDownload = false;
			bool isBase64 = false;
			string detectedThreat = "Штатный блок кода PowerShell";
			string verdict = "Легитимный системный скрипт";
			string rule = "Разрешено (5.01)";
			Color color = Ui.AccentGreen;
			ScriptBlockFilterCategory cat = ScriptBlockFilterCategory.Whitelisted;

			// 1. Поиск команд зачистки (5.08)
			if (lower.Contains("wevtutil") || lower.Contains("fsutil usn deletejournal") || lower.Contains("clear-eventlog") 
				|| lower.Contains("cipher /w") || lower.Contains("vssadmin delete shadows") || lower.Contains("deletejournal") 
				|| (lower.Contains("del ") && lower.Contains("prefetch")) || lower.Contains("srm ") || lower.Contains("bleachbit") 
				|| lower.Contains("privazer") || lower.Contains("journaltrace") || lower.Contains("slapper"))
			{
				isWiping = true;
				detectedThreat = "🚨 КОМАНДЫ ЗАЧИСТКИ ЖУРНАЛОВ / СОКРЫТИЯ (5.08)";
				verdict = "🚨 ПОПЫТКА СОКРЫТИЯ УЛИК ПЕРЕД SCREENSHARE";
				rule = "5.08 (Сокрытие улик)";
				color = Ui.AccentRed;
				cat = ScriptBlockFilterCategory.ViolationsWiping508;
			}
			// 2. Поиск скачивания читов / обращений к чит-доменам (5.01)
			else if (lower.Contains("downloadfile") || lower.Contains("downloadstring") || lower.Contains("invoke-webrequest") 
				|| lower.Contains("bitstransfer") || lower.Contains("start-bitstransfer") || lower.Contains("celestial.lol") 
				|| lower.Contains("expensive.fun") || lower.Contains("deadcode.fun") || lower.Contains("vape.gg") 
				|| lower.Contains("driplite.cc") || lower.Contains("slinky.gg") || lower.Contains("akrien.wtf") 
				|| lower.Contains("minced.cc") || lower.Contains("zamorozka.fun") || lower.Contains("nursultan.fun"))
			{
				isDownload = true;
				detectedThreat = "⚡ СКАЧИВАНИЕ ЧИТ-КЛИЕНТА / ПЕЙЛОАДА (5.01)";
				verdict = "🚨 ВЫПОЛНЕНИЕ ЗАГРУЗКИ ЧИТА ИЗ СЕТИ";
				rule = "5.01 (Чит-клиент/Загрузка)";
				color = Ui.AccentRed;
				cat = ScriptBlockFilterCategory.CheatDownloads501;
			}
			// 3. Поиск Base64 обфускации
			string decodedBase64 = TryDecodeBase64Payload(script);
			if (!string.IsNullOrEmpty(decodedBase64))
			{
				isBase64 = true;
				if (!isWiping && !isDownload)
				{
					detectedThreat = "🔐 ОБНАРУЖЕН ОБФУСЦИРОВАННЫЙ BASE64 БЛОК";
					verdict = "⚠️ Скрытый закодированный Base64 код";
					rule = "Внимание (5.01/5.08)";
					color = Ui.AccentPurple;
					cat = ScriptBlockFilterCategory.Base64Obfuscation;
				}
			}

			// Однострочный сниппет для списка
			string singleLine = script.Replace("\r", " ").Replace("\n", " ").Trim();
			if (singleLine.Length > 120) singleLine = singleLine.Substring(0, 120) + "...";

			bool isViolation = isWiping || isDownload;

			list.Add(new ScriptBlockForensicItem
			{
				Index = idx++,
				ExecutionTime = time,
				DetectedThreat = detectedThreat,
				Category = cat,
				CodeSnippet = singleLine,
				Verdict = verdict,
				TargetRule = rule,
				UserSid = userId,
				IsViolation = isViolation,
				IsWiping = isWiping,
				IsDownload = isDownload,
				IsBase64 = isBase64,
				IsAllowed = !isViolation,
				RowColor = color,
				FullCode = script,
				DecodedBase64Payload = decodedBase64,
				Details = string.Format("Время выполнения: {0}\nПользователь: {1}\nВердикт: {2}\nПункт правил: {3}\nУгроза: {4}", time.ToString("yyyy-MM-dd HH:mm:ss"), userId, verdict, rule, detectedThreat)
			});
		}

		private static string TryDecodeBase64Payload(string script)
		{
			try
			{
				MatchCollection matches = Regex.Matches(script, @"[A-Za-z0-9+/=]{40,}");
				StringBuilder sb = new StringBuilder();
				foreach (Match m in matches)
				{
					try
					{
						byte[] b = Convert.FromBase64String(m.Value);
						string dec = Encoding.UTF8.GetString(b);
						if (!string.IsNullOrEmpty(dec) && dec.Length > 8)
						{
							// Проверка на читаемый текст
							int printable = 0;
							for (int i = 0; i < dec.Length; i++)
							{
								if (char.IsLetterOrDigit(dec[i]) || char.IsPunctuation(dec[i]) || char.IsWhiteSpace(dec[i])) printable++;
							}
							if (printable > dec.Length * 0.8)
							{
								sb.AppendLine("[Декодированный Base64 фрагмент]:");
								sb.AppendLine(dec);
								sb.AppendLine();
							}
						}
					}
					catch { }
				}
				return sb.Length > 0 ? sb.ToString() : null;
			}
			catch { return null; }
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result is List<ScriptBlockForensicItem>)
			{
				allItems = (List<ScriptBlockForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int wiping = 0;
			int downloads = 0;
			int b64 = 0;
			int safe = 0;

			foreach (ScriptBlockForensicItem it in allItems)
			{
				if (it.IsWiping) wiping++;
				if (it.IsDownload) downloads++;
				if (it.IsBase64) b64++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = "Блоков: " + total;
			lblKpiWiping.Text = "Вайперы (5.08): " + wiping;
			lblKpiDownloads.Text = "Загрузки (5.01): " + downloads;
			lblKpiBase64.Text = "Base64 код: " + b64;
			lblKpiSafe.Text = "Штатные: " + safe;

			if (wiping > 0 || downloads > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ ОПАСНЫЕ КОМАНДЫ В POWERSHELL (Вайперы: {0}, Загрузки: {1})", wiping, downloads);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Журнал PowerShell 4104 чист: команд сокрытия и загрузчиков читов не обнаружено";
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

			foreach (ScriptBlockForensicItem it in allItems)
			{
				if (currentFilter == ScriptBlockFilterCategory.ViolationsWiping508 && !it.IsWiping) continue;
				if (currentFilter == ScriptBlockFilterCategory.CheatDownloads501 && !it.IsDownload) continue;
				if (currentFilter == ScriptBlockFilterCategory.Base64Obfuscation && !it.IsBase64) continue;
				if (currentFilter == ScriptBlockFilterCategory.Recent24Hours && (DateTime.Now - it.ExecutionTime).TotalHours > 24) continue;
				if (currentFilter == ScriptBlockFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.DetectedThreat != null && it.DetectedThreat.ToLowerInvariant().Contains(search))
						|| (it.CodeSnippet != null && it.CodeSnippet.ToLowerInvariant().Contains(search))
						|| (it.FullCode != null && it.FullCode.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ExecutionTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.DetectedThreat);
				lvi.SubItems.Add(it.Category.ToString());
				lvi.SubItems.Add(it.CodeSnippet);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.UserSid);

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
			ScriptBlockForensicItem it = listView.SelectedItems[0].Tag as ScriptBlockForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ ВЫПОЛНЕННОГО БЛОКА POWERSHELL (EVENT ID 4104) ===");
			sb.AppendLine("Время события: " + it.ExecutionTime.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Пользователь / SID: " + it.UserSid);
			sb.AppendLine("Обнаруженная угроза: " + it.DetectedThreat);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт регламента сервера: " + it.TargetRule);
			sb.AppendLine();

			if (!string.IsNullOrEmpty(it.DecodedBase64Payload))
			{
				sb.AppendLine("================================================================================");
				sb.AppendLine("🔓 АВТОМАТИЧЕСКИ ДЕКОДИРОВАННЫЙ BASE64 ПЕЙЛОАД:");
				sb.AppendLine("================================================================================");
				sb.AppendLine(it.DecodedBase64Payload);
				sb.AppendLine("================================================================================");
				sb.AppendLine();
			}

			sb.AppendLine("--- ИСХОДНЫЙ КОД SCRIPTBLOCK (КАК ВЫПОЛНЯЛСЯ В СИСТЕМЕ) ---");
			sb.AppendLine(it.FullCode);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void CopyFullScript()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScriptBlockForensicItem it = listView.SelectedItems[0].Tag as ScriptBlockForensicItem;
			if (it == null || string.IsNullOrEmpty(it.FullCode)) return;

			Clipboard.SetText(it.FullCode);
			MessageBox.Show("Полный код ScriptBlock скопирован в буфер обмена!", "Копирование кода", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScriptBlockForensicItem it = listView.SelectedItems[0].Tag as ScriptBlockForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ POWERSHELL SCRIPTBLOCK (4104) ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Время выполнения: " + it.ExecutionTime.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Угроза: " + it.DetectedThreat);
			sb.AppendLine("Код: " + it.CodeSnippet);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Пользователь SID: " + it.UserSid);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Discord-отчет скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScriptBlockForensicItem it = listView.SelectedItems[0].Tag as ScriptBlockForensicItem;
			if (it == null) return;

			string cmd;
			if (it.IsWiping)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Зачистка следов через PowerShell)", targetPlayerName);
			}
			else if (it.IsDownload)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Загрузка чита через PowerShell)", targetPlayerName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Вредоносный скрипт PowerShell)", targetPlayerName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
