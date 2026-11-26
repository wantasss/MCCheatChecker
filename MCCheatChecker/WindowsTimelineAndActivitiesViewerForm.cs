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
	public class WindowsTimelineAndActivitiesViewerForm : Form
	{
		#region Data Models

		public enum ActivityFilterCategory
		{
			All,
			ViolationsOnly,
			DeletedApps,
			TimelineApps,
			Whitelisted
		}

		public class ActivityRecordItem
		{
			public int Index;
			public string AppName;
			public string AppPath;
			public string DisplayText;
			public DateTime StartTime;
			public int DurationSeconds;
			public ActivityFilterCategory Category;
			public string Verdict;
			public string TargetRule;
			public string SourceDatabase;
			public bool FileExistsOnDisk;
			public bool IsViolation;
			public bool IsDeleted;
			public bool IsAllowed;
			public Color RowColor;
			public string RawJson;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ActivityFilterCategory currentFilter = ActivityFilterCategory.All;
		private List<ActivityRecordItem> allItems = new List<ActivityRecordItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiDeleted;
		private Label lblKpiApps;
		private Label lblKpiAllowed;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterApps;
		private DarkButton btnFilterAllowed;

		private DarkButton btnOpenFile;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyPath;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public WindowsTimelineAndActivitiesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "📅 Форензика таймлайна активности Windows и базы ActivitiesCache.db — MCCheatChecker";
			Size = new Size(1260, 780);
			MinimumSize = new Size(1020, 600);
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
			lblHeaderTitle.Text = "📅 Форензика таймлайна активности Windows Timeline (ActivitiesCache.db & ConnectedDevices)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Аудит журнала активности пользователя: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1220, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotal = CreateKpiLabel("Всего активностей: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик читов (5.01): 0", Ui.AccentRed, 190, kpiPanel);
			lblKpiDeleted = CreateKpiLabel("Запущены и удалены (5.08): 0", Ui.AccentOrange, 380, kpiPanel);
			lblKpiApps = CreateKpiLabel("Приложений в базе: 0", Ui.AccentTeal, 630, kpiPanel);
			lblKpiAllowed = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 840, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все события", ActivityFilterCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01 / 5.08)", ActivityFilterCategory.ViolationsOnly, 126, filterPanel);
			btnFilterDeleted = CreateFilterButton("🗑️ Удаленные бинарники (5.08)", ActivityFilterCategory.DeletedApps, 330, filterPanel);
			btnFilterApps = CreateFilterButton("📱 Запуски приложений", ActivityFilterCategory.TimelineApps, 544, filterPanel);
			btnFilterAllowed = CreateFilterButton("✅ Белый список (Allowed)", ActivityFilterCategory.Whitelisted, 738, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(960, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(1015, 9);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			Controls.Add(filterPanel);

			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
			split.BackColor = Ui.Backdrop;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Приложение / Файл", 200);
			listView.Columns.Add("Время старта", 140);
			listView.Columns.Add("Длительность (сек)", 120);
			listView.Columns.Add("Статус файла", 120);
			listView.Columns.Add("Вердикт", 200);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Полный путь", 350);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenFileLocation(); };
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Метаданные таймлайна активности, JSON-структура и хронология:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(12, 30);
			txtDetails.Size = new Size(860, 130);
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			bottomContainer.Controls.Add(txtDetails);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(885, 20);
			actionsPanel.Size = new Size(330, 145);

			btnOpenFile = CreateActionButton("📄 Запустить / открыть файл", 0, actionsPanel);
			btnOpenFile.Click += delegate { OpenSelectedFile(); };

			btnOpenFolder = CreateActionButton("📂 Открыть папку в Проводнике", 32, actionsPanel);
			btnOpenFolder.Click += delegate { OpenFileLocation(); };

			btnCopyPath = CreateActionButton("📋 Копировать полный путь", 64, actionsPanel);
			btnCopyPath.Click += delegate { CopyFilePath(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 96, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана", 128, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			bottomContainer.Controls.Add(actionsPanel);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(12, 166);
			btnRescan.Size = new Size(160, 26);
			btnRescan.Click += delegate { StartScan(); };
			bottomContainer.Controls.Add(btnRescan);

			split.Panel2.Controls.Add(bottomContainer);
			Controls.Add(split);

			filterPanel.BringToFront();
			split.BringToFront();
		}

		private Label CreateKpiLabel(string text, Color color, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.Font = Ui.FontBody(9f, true);
			lbl.ForeColor = color;
			lbl.BackColor = Ui.SurfaceAlt;
			lbl.AutoSize = true;
			lbl.Padding = new Padding(8, 4, 8, 4);
			lbl.Location = new Point(x, 4);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, ActivityFilterCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == ActivityFilterCategory.ViolationsOnly ? 198 : (cat == ActivityFilterCategory.DeletedApps ? 208 : (cat == ActivityFilterCategory.TimelineApps ? 188 : (cat == ActivityFilterCategory.Whitelisted ? 190 : 108))), 28);
			btn.Font = Ui.FontBody(8.5f, true);
			btn.Click += delegate
			{
				currentFilter = cat;
				ResetFilterButtonColors();
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			if (cat == ActivityFilterCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterDeleted, btnFilterApps, btnFilterAllowed };
			foreach (DarkButton b in btns)
			{
				b.BackColor = Ui.Surface;
				b.ForeColor = Ui.TextPrimary;
			}
		}

		private DarkButton CreateActionButton(string text, int y, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(0, y);
			btn.Size = new Size(320, 26);
			btn.Font = Ui.FontBody(8f, true);
			parent.Controls.Add(btn);
			return btn;
		}

		#region Scan Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Сканирование базы Windows Timeline (ActivitiesCache.db)...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ActivityRecordItem> list = new List<ActivityRecordItem>();
			int idx = 1;

			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string cdpDir = Path.Combine(localAppData, "ConnectedDevicesPlatform");

			if (!Directory.Exists(cdpDir))
			{
				e.Result = list;
				return;
			}

			// Find all ActivitiesCache.db files
			try
			{
				string[] dbFiles = Directory.GetFiles(cdpDir, "ActivitiesCache.db", SearchOption.AllDirectories);
				HashSet<string> seenApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string dbFile in dbFiles)
				{
					string tempCopy = Path.Combine(Path.GetTempPath(), "ActivitiesCache_copy_" + Guid.NewGuid().ToString("N") + ".db");
					try
					{
						File.Copy(dbFile, tempCopy, true);
						byte[] rawBytes = File.ReadAllBytes(tempCopy);
						string rawText = Encoding.UTF8.GetString(rawBytes);

						// Extract application paths: {"application":"..."} or "application":"C:\\..."
						MatchCollection appMatches = Regex.Matches(rawText, @"\""application\""\s*:\s*\""([^\""]+)\""", RegexOptions.IgnoreCase);
						foreach (Match m in appMatches)
						{
							string rawApp = m.Groups[1].Value.Replace(@"\\", @"\");
							if (rawApp.Length < 4) continue;
							if (seenApps.Contains(rawApp)) continue;
							seenApps.Add(rawApp);

							ActivityRecordItem it = BuildActivityItem(rawApp, dbFile, rawText, ref idx);
							list.Add(it);
						}

						// Extract file display texts: "displayText":"..."
						MatchCollection displayMatches = Regex.Matches(rawText, @"\""displayText\""\s*:\s*\""([^\""]+)\""", RegexOptions.IgnoreCase);
						foreach (Match dm in displayMatches)
						{
							string dt = dm.Groups[1].Value;
							if (dt.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || dt.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || dt.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
							{
								if (!seenApps.Contains(dt))
								{
									seenApps.Add(dt);
									ActivityRecordItem it = BuildActivityItem(dt, dbFile, rawText, ref idx);
									list.Add(it);
								}
							}
						}
					}
					catch { }
					finally
					{
						try { if (File.Exists(tempCopy)) File.Delete(tempCopy); } catch { }
					}
				}
			}
			catch { }

			e.Result = list;
		}

		private ActivityRecordItem BuildActivityItem(string rawPath, string dbPath, string fullText, ref int idx)
		{
			string expandedPath = Environment.ExpandEnvironmentVariables(rawPath);
			string fName = Path.GetFileName(expandedPath);
			bool exists = File.Exists(expandedPath);

			ActivityRecordItem it = new ActivityRecordItem();
			it.Index = idx++;
			it.AppName = string.IsNullOrEmpty(fName) ? expandedPath : fName;
			it.AppPath = expandedPath;
			it.SourceDatabase = Path.GetFileName(Path.GetDirectoryName(dbPath)) + "\\ActivitiesCache.db";
			it.FileExistsOnDisk = exists;
			it.IsDeleted = !exists;
			it.StartTime = File.Exists(expandedPath) ? File.GetLastWriteTime(expandedPath) : DateTime.Now;
			it.DurationSeconds = 0;
			it.RowColor = Ui.TextPrimary;

			string lower = (expandedPath + " " + it.AppName).ToLowerInvariant();

			// Whitelist check
			bool isAllowed = lower.Contains("anydesk") || lower.Contains("rudesktop") ||
				lower.Contains("opautoclicker") || lower.Contains("speedautoclicker") ||
				lower.Contains("tinytask") || lower.Contains("x-mouse") ||
				lower.Contains("autohotkey") || lower.Contains("clickermann") ||
				lower.Contains("keyran") || lower.Contains("logitech") || lower.Contains("razer");

			if (isAllowed)
			{
				it.IsAllowed = true;
				it.Category = ActivityFilterCategory.Whitelisted;
				it.Verdict = "✅ БЕЛЫЙ СПИСОК (РАЗРЕШЕНО)";
				it.TargetRule = "Разрешено регламентом";
				it.RowColor = Ui.AccentGreen;
				it.Details = "Утилита удаленного доступа или кликер из официального разрешенного перечня правил проекта.\nСанкции по 5.01 не применяются.";
				return it;
			}

			// Cheat keywords check
			string[] cheatKeywords = new string[]
			{
				"celestial", "expensive", "nursultan", "akrien", "wildclient", "nurik",
				"wexside", "deadcode", "minced", "vape", "drip", "slinky", "catlavan",
				"boberware", "neverhook", "fluger", "koid", "injector", "extremeinjector", "xenos",
				"cleaner", "usnjrnl", "bypass", "journaltrace", "blast.hk", "yougame",
				"exloader", "liquidbounce", "meteorclient", "wurstclient", "aristois", "thunderhack",
				"whiteout", "spirthack", "clean.bat", "cleanss", "anti_ss"
			};

			bool hasCheatMatch = false;
			string matchedKw = "";

			foreach (string kw in cheatKeywords)
			{
				if (lower.Contains(kw))
				{
					hasCheatMatch = true;
					matchedKw = kw;
					break;
				}
			}

			if (hasCheatMatch)
			{
				it.IsViolation = true;
				it.Category = ActivityFilterCategory.ViolationsOnly;
				if (!exists)
				{
					it.Verdict = "🚨 ЗАПУЩЕН И УДАЛЕН ЧИТ / КЛИНЕР (5.08 / 5.01)";
					it.TargetRule = "5.08 (Удаление улик)";
					it.RowColor = Ui.AccentRed;
				}
				else
				{
					it.Verdict = "🚨 АКТИВНОСТЬ ЧИТ-ПРОГРАММЫ (5.01)";
					it.TargetRule = "5.01 (Чит-клиенты)";
					it.RowColor = Ui.AccentRed;
				}

				it.Details = string.Format("В системном журнале активности Windows Timeline зафиксирован запуск [{0}].\nПуть: {1}.\nНаличие на диске: {2}.\nПункт правил: {3}.", it.AppName, it.AppPath, (exists ? "Существует" : "УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)"), it.TargetRule);
			}
			else if (!exists)
			{
				it.IsDeleted = true;
				it.Category = ActivityFilterCategory.DeletedApps;
				it.Verdict = "⚠️ Приложение удалено с диска";
				it.TargetRule = "5.08 (Аудит удаления)";
				it.RowColor = Ui.AccentOrange;
				it.Details = string.Format("Приложение зафиксировано в Windows Timeline, однако в данный момент файл отсутствует на диске:\n{0}.", it.AppPath);
			}
			else
			{
				it.Category = ActivityFilterCategory.TimelineApps;
				it.Verdict = "Штатная активность приложения";
				it.TargetRule = "Стандартный процесс";
				it.RowColor = Ui.TextSecondary;
				it.Details = string.Format("Запуск приложения зафиксирован в ActivitiesCache.db.\nПуть: {0}.", it.AppPath);
			}

			return it;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования ActivitiesCache: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<ActivityRecordItem> res = e.Result as List<ActivityRecordItem>;
			if (res != null)
			{
				allItems = res;

				int violations = 0;
				int deleted = 0;
				int apps = 0;
				int allowed = 0;

				foreach (ActivityRecordItem it in allItems)
				{
					if (it.IsViolation) violations++;
					if (it.IsDeleted) deleted++;
					if (it.Category == ActivityFilterCategory.TimelineApps) apps++;
					if (it.IsAllowed) allowed++;
				}

				lblKpiTotal.Text = "Всего активностей: " + allItems.Count;
				lblKpiViolations.Text = "Улик читов (5.01): " + violations;
				lblKpiDeleted.Text = "Запущены и удалены (5.08): " + deleted;
				lblKpiApps.Text = "Приложений в базе: " + apps;
				lblKpiAllowed.Text = "Белый список: " + allowed;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: В Windows Timeline найдено {0} следов читов и {1} удаленных программ!", violations, deleted);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else if (deleted > 0)
				{
					lblStatusPill.Text = string.Format("⚠️ В Windows Timeline зафиксировано {0} удаленных исполняемых файлов.", deleted);
					lblStatusPill.ForeColor = Ui.AccentOrange;
				}
				else
				{
					lblStatusPill.Text = "✅ Сканирование Windows Timeline завершено: подозрительных активностей не обнаружено.";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & UI Updates

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : string.Empty;

			foreach (ActivityRecordItem it in allItems)
			{
				if (currentFilter == ActivityFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == ActivityFilterCategory.DeletedApps && !it.IsDeleted) continue;
				if (currentFilter == ActivityFilterCategory.TimelineApps && it.Category != ActivityFilterCategory.TimelineApps) continue;
				if (currentFilter == ActivityFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.AppName.ToLowerInvariant().Contains(search) ||
						it.AppPath.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						it.SourceDatabase.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.AppName);
				lvi.SubItems.Add(it.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.DurationSeconds > 0 ? it.DurationSeconds.ToString() : "-");
				lvi.SubItems.Add(it.FileExistsOnDisk ? "Существует" : "УДАЛЕН (5.08)");
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.AppPath);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtDetails.Text = "Нет записей, соответствующих выбранному фильтру.";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АКТ АКТИВНОСТИ WINDOWS TIMELINE (ACTIVITIESCACHE.DB) ===");
			sb.AppendLine("Приложение: " + it.AppName);
			sb.AppendLine("Полный путь: " + it.AppPath);
			sb.AppendLine("Время активности: " + it.StartTime.ToString("yyyy-MM-dd HH:mm:ss (МСК)"));
			sb.AppendLine("Наличие на диске: " + (it.FileExistsOnDisk ? "Существует на диске" : "🚨 ФАЙЛ БЫЛ УДАЛЕН ПОСЛЕ АКТИВНОСТИ (5.08)"));
			sb.AppendLine("Вердикт проверки: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("База данных: " + it.SourceDatabase);
			sb.AppendLine();
			sb.AppendLine("=== ДЕТАЛИ И КОНТЕКСТ ===");
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions

		private void OpenFileLocation()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null) return;

			if (File.Exists(it.AppPath))
			{
				try
				{
					Process.Start("explorer.exe", "/select,\"" + it.AppPath + "\"");
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				string dir = Path.GetDirectoryName(it.AppPath);
				if (Directory.Exists(dir))
				{
					Process.Start("explorer.exe", dir);
				}
				else
				{
					MessageBox.Show("Файл и родительский каталог были удалены:\n" + it.AppPath, "Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		private void OpenSelectedFile()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null || !File.Exists(it.AppPath)) return;

			try
			{
				Process.Start(it.AppPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyFilePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null) return;

			Clipboard.SetText(it.AppPath);
			MessageBox.Show("Путь скопирован в буфер:\n" + it.AppPath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ WINDOWS TIMELINE ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Приложение: " + it.AppName);
			sb.AppendLine("Путь: " + it.AppPath);
			sb.AppendLine("Время: " + it.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Статус на диске: " + (it.FileExistsOnDisk ? "Существует" : "УДАЛЕН ПЕРЕД СС (5.08)"));
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт: " + it.TargetRule);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityRecordItem it = listView.SelectedItems[0].Tag as ActivityRecordItem;
			if (it == null) return;

			string cmd;
			if (!it.FileExistsOnDisk && it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Запуск чита {1} с последующим удалением)", targetPlayerName, it.AppName);
			}
			else if (it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Активность {1} зафиксирована в Timeline)", targetPlayerName, it.AppName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Удаление файла {1})", targetPlayerName, it.AppName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
