using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class PcaAppLaunchForensicsViewerForm : Form
	{
		#region Data Models

		public enum PcaCategory
		{
			All,
			ViolationsOnly,
			DeletedFiles,
			TempAndDownloads,
			Whitelisted
		}

		public class PcaLaunchItem
		{
			public int Index;
			public string FileName;
			public string FullPath;
			public DateTime LaunchTime;
			public string SourceDb;
			public string ExitStatus;
			public string CompanyName;
			public string FileHash;
			public bool FileExistsOnDisk;
			public bool IsViolation;
			public bool IsDeleted;
			public bool IsTempOrDownload;
			public bool IsAllowed;
			public string Verdict;
			public string TargetRule;
			public PcaCategory Category;
			public Color RowColor;
			public string RawLine;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private PcaCategory currentFilter = PcaCategory.All;
		private List<PcaLaunchItem> allItems = new List<PcaLaunchItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiDeleted;
		private Label lblKpiTemp;
		private Label lblKpiAllowed;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterTemp;
		private DarkButton btnFilterAllowed;

		private DarkButton btnOpenFile;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyPath;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public PcaAppLaunchForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "🚀 Форензика базы запусков PCA (Program Compatibility Assistant) — MCCheatChecker";
			Size = new Size(1240, 760);
			MinimumSize = new Size(1000, 580);
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
			lblHeaderTitle.Text = "🚀 Форензика базы запусков PCA (PcaAppLaunchDic.txt, PcaGeneralDb0 & AppCompatFlags)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Аудит исполняемых файлов: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1200, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotal = CreateKpiLabel("Записей PCA: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Нарушений (5.01): 0", Ui.AccentRed, 170, kpiPanel);
			lblKpiDeleted = CreateKpiLabel("Запущены и удалены (5.08): 0", Ui.AccentOrange, 370, kpiPanel);
			lblKpiTemp = CreateKpiLabel("Из Temp / Downloads: 0", Ui.AccentTeal, 620, kpiPanel);
			lblKpiAllowed = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 840, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все запуски", PcaCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01 / 5.08)", PcaCategory.ViolationsOnly, 126, filterPanel);
			btnFilterDeleted = CreateFilterButton("🗑️ Удаленные файлы (5.08)", PcaCategory.DeletedFiles, 320, filterPanel);
			btnFilterTemp = CreateFilterButton("⚠️ Из Temp / Загрузок", PcaCategory.TempAndDownloads, 514, filterPanel);
			btnFilterAllowed = CreateFilterButton("✅ Белый список (Allowed)", PcaCategory.Whitelisted, 708, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(940, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(995, 9);
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
			listView.Columns.Add("Исполняемый файл", 190);
			listView.Columns.Add("Время запуска (PCA)", 140);
			listView.Columns.Add("Статус файла", 120);
			listView.Columns.Add("Вердикт", 180);
			listView.Columns.Add("Пункт правил", 120);
			listView.Columns.Add("Источник данных", 130);
			listView.Columns.Add("Полный путь", 360);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenFileLocation(); };
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Метаданные запуска PCA, код завершения и цифровой отпечаток:";
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

		private DarkButton CreateFilterButton(string text, PcaCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == PcaCategory.ViolationsOnly ? 186 : (cat == PcaCategory.DeletedFiles ? 186 : (cat == PcaCategory.TempAndDownloads ? 186 : (cat == PcaCategory.Whitelisted ? 190 : 106))), 28);
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
			if (cat == PcaCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterDeleted, btnFilterTemp, btnFilterAllowed };
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

			lblStatusPill.Text = "⏳ Сканирование базы Program Compatibility Assistant (PCA)...";
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
			List<PcaLaunchItem> list = new List<PcaLaunchItem>();
			int idx = 1;
			HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string pcaDir = Path.Combine(winDir, "appcompat\\pca");

			// 1. Parse PcaAppLaunchDic.txt
			try
			{
				string dicPath = Path.Combine(pcaDir, "PcaAppLaunchDic.txt");
				if (File.Exists(dicPath))
				{
					string[] lines = File.ReadAllLines(dicPath);
					foreach (string line in lines)
					{
						if (string.IsNullOrWhiteSpace(line)) continue;
						string[] parts = line.Split('|');
						if (parts.Length >= 2)
						{
							string rawPath = parts[0].Trim();
							DateTime dt;
							if (!DateTime.TryParse(parts[1].Trim(), out dt)) dt = DateTime.MinValue;

							PcaLaunchItem it = BuildLaunchItem(rawPath, dt, "PcaAppLaunchDic.txt", "", "", "", ref idx);
							list.Add(it);
							seenPaths.Add(it.FullPath);
						}
					}
				}
			}
			catch { }

			// 2. Parse PcaGeneralDb0.txt and PcaGeneralDb1.txt (Unicode UTF-16 LE)
			string[] dbFiles = new string[] { "PcaGeneralDb0.txt", "PcaGeneralDb1.txt", "PcaTrackDb.txt" };
			foreach (string dbf in dbFiles)
			{
				try
				{
					string p = Path.Combine(pcaDir, dbf);
					if (File.Exists(p))
					{
						string[] lines = File.ReadAllLines(p, Encoding.Unicode);
						foreach (string line in lines)
						{
							if (string.IsNullOrWhiteSpace(line)) continue;
							string[] parts = line.Split('|');
							if (parts.Length >= 3)
							{
								DateTime dt;
								if (!DateTime.TryParse(parts[0].Trim(), out dt)) dt = DateTime.MinValue;
								string rawPath = parts[2].Trim();
								string company = parts.Length > 4 ? parts[4].Trim() : "";
								string hash = parts.Length > 6 ? parts[6].Trim() : "";
								string exitDetails = parts.Length > 7 ? parts[7].Trim() : "";

								if (!string.IsNullOrEmpty(rawPath))
								{
									PcaLaunchItem it = BuildLaunchItem(rawPath, dt, dbf, exitDetails, company, hash, ref idx);
									list.Add(it);
									seenPaths.Add(it.FullPath);
								}
							}
						}
					}
				}
				catch { }
			}

			// 3. Parse Registry: HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store
			try
			{
				using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"))
				{
					if (rk != null)
					{
						string[] valNames = rk.GetValueNames();
						foreach (string vname in valNames)
						{
							if (!string.IsNullOrEmpty(vname) && !seenPaths.Contains(vname))
							{
								PcaLaunchItem it = BuildLaunchItem(vname, DateTime.MinValue, "AppCompatFlags\\Store", "Registry Stored", "", "", ref idx);
								list.Add(it);
								seenPaths.Add(it.FullPath);
							}
						}
					}
				}
			}
			catch { }

			// Sort by LaunchTime descending
			list.Sort(delegate(PcaLaunchItem a, PcaLaunchItem b)
			{
				return b.LaunchTime.CompareTo(a.LaunchTime);
			});

			// Reindex
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Index = i + 1;
			}

			e.Result = list;
		}

		private PcaLaunchItem BuildLaunchItem(string rawPath, DateTime dt, string source, string exitCode, string company, string hash, ref int idx)
		{
			string expandedPath = Environment.ExpandEnvironmentVariables(rawPath);
			string fileName = Path.GetFileName(expandedPath);
			bool exists = File.Exists(expandedPath);

			PcaLaunchItem it = new PcaLaunchItem();
			it.Index = idx++;
			it.FileName = string.IsNullOrEmpty(fileName) ? expandedPath : fileName;
			it.FullPath = expandedPath;
			it.LaunchTime = dt;
			it.SourceDb = source;
			it.ExitStatus = string.IsNullOrEmpty(exitCode) ? "Успешный запуск" : exitCode;
			it.CompanyName = company;
			it.FileHash = hash;
			it.FileExistsOnDisk = exists;
			it.IsDeleted = !exists;
			it.RowColor = Ui.TextPrimary;

			string lower = (expandedPath + " " + company).ToLowerInvariant();

			// Whitelist check: AnyDesk, RuDesktop, OP Auto Clicker, SpeedAutoClicker
			bool isAllowed = lower.Contains("anydesk") || lower.Contains("rudesktop") ||
				lower.Contains("opautoclicker") || lower.Contains("speedautoclicker") ||
				lower.Contains("tinytask") || lower.Contains("x-mouse") ||
				lower.Contains("autohotkey") || lower.Contains("clickermann") ||
				lower.Contains("keyran") || lower.Contains("logitech") || lower.Contains("razer");

			if (isAllowed)
			{
				it.IsAllowed = true;
				it.Category = PcaCategory.Whitelisted;
				it.Verdict = "✅ БЕЛЫЙ СПИСОК (РАЗРЕШЕНО)";
				it.TargetRule = "Разрешено регламентом";
				it.RowColor = Ui.AccentGreen;
				return it;
			}

			// Temp or Downloads check
			bool isTemp = lower.Contains("\\temp\\") || lower.Contains("\\downloads\\") || lower.Contains("\\desktop\\");
			it.IsTempOrDownload = isTemp;

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
			string matchedKw = string.Empty;

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
				it.Category = PcaCategory.ViolationsOnly;
				if (!exists)
				{
					it.Verdict = "🚨 ЗАПУЩЕН И УДАЛЕН ЧИТ / КЛИНЕР (5.08 / 5.01)";
					it.TargetRule = "5.08 (Удаление улик)";
					it.RowColor = Ui.AccentRed;
				}
				else
				{
					it.Verdict = "🚨 ЗАПУСК ЧИТ-ПРОГРАММЫ (5.01)";
					it.TargetRule = "5.01 (Чит-клиенты)";
					it.RowColor = Ui.AccentRed;
				}
			}
			else if (!exists)
			{
				it.IsDeleted = true;
				it.Category = PcaCategory.DeletedFiles;
				it.Verdict = "⚠️ Исполняемый файл удален с диска";
				it.TargetRule = "5.08 (Проверка удаления)";
				it.RowColor = Ui.AccentOrange;
			}
			else if (isTemp)
			{
				it.Category = PcaCategory.TempAndDownloads;
				it.Verdict = "⚠️ Запуск из Temp / Загрузок";
				it.TargetRule = "Аудит источника";
				it.RowColor = Ui.AccentTeal;
			}
			else
			{
				it.Category = PcaCategory.All;
				it.Verdict = "Обычный запуск приложения";
				it.TargetRule = "Штатный процесс";
				it.RowColor = Ui.TextSecondary;
			}

			return it;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования PCA: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<PcaLaunchItem> res = e.Result as List<PcaLaunchItem>;
			if (res != null)
			{
				allItems = res;

				int violations = 0;
				int deleted = 0;
				int temp = 0;
				int allowed = 0;

				foreach (PcaLaunchItem it in allItems)
				{
					if (it.IsViolation) violations++;
					if (it.IsDeleted) deleted++;
					if (it.IsTempOrDownload) temp++;
					if (it.IsAllowed) allowed++;
				}

				lblKpiTotal.Text = "Записей PCA: " + allItems.Count;
				lblKpiViolations.Text = "Нарушений (5.01): " + violations;
				lblKpiDeleted.Text = "Запущены и удалены (5.08): " + deleted;
				lblKpiTemp.Text = "Из Temp / Downloads: " + temp;
				lblKpiAllowed.Text = "Белый список: " + allowed;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Найдено {0} запусков читов и {1} удаленных исполняемых файлов!", violations, deleted);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else if (deleted > 0)
				{
					lblStatusPill.Text = string.Format("⚠️ Зафиксировано {0} удаленных исполняемых файлов в базе PCA.", deleted);
					lblStatusPill.ForeColor = Ui.AccentOrange;
				}
				else
				{
					lblStatusPill.Text = "✅ Сканирование PCA завершено: запусков софта сокрытия не обнаружено.";
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

			foreach (PcaLaunchItem it in allItems)
			{
				if (currentFilter == PcaCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == PcaCategory.DeletedFiles && !it.IsDeleted) continue;
				if (currentFilter == PcaCategory.TempAndDownloads && !it.IsTempOrDownload) continue;
				if (currentFilter == PcaCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.FileName.ToLowerInvariant().Contains(search) ||
						it.FullPath.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						it.SourceDb.ToLowerInvariant().Contains(search) ||
						(it.CompanyName != null && it.CompanyName.ToLowerInvariant().Contains(search));

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.FileName);
				lvi.SubItems.Add(it.LaunchTime == DateTime.MinValue ? "Не указано (Реестр)" : it.LaunchTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.FileExistsOnDisk ? "Существует" : "УДАЛЕН (5.08)");
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.SourceDb);
				lvi.SubItems.Add(it.FullPath);

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
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ ЗАПУСКА ПРОГРАММЫ В БАЗЕ PCA ===");
			sb.AppendLine("Исполняемый файл: " + it.FileName);
			sb.AppendLine("Полный путь: " + it.FullPath);
			sb.AppendLine("Точное время запуска: " + (it.LaunchTime == DateTime.MinValue ? "Не указано (AppCompatFlags\\Store)" : it.LaunchTime.ToString("yyyy-MM-dd HH:mm:ss.fff (МСК)")));
			sb.AppendLine("Статус наличия на диске: " + (it.FileExistsOnDisk ? "Файл присутствует на диске" : "🚨 ФАЙЛ БЫЛ УДАЛЕН ПОСЛЕ ЗАПУСКА (5.08)"));
			sb.AppendLine("Вердикт проверки: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Источник данных PCA: " + it.SourceDb);
			if (!string.IsNullOrEmpty(it.CompanyName)) sb.AppendLine("Издатель / Компания: " + it.CompanyName);
			if (!string.IsNullOrEmpty(it.ExitStatus)) sb.AppendLine("Код выхода / Событие: " + it.ExitStatus);
			if (!string.IsNullOrEmpty(it.FileHash)) sb.AppendLine("Хэш PCA: " + it.FileHash);
			sb.AppendLine();
			sb.AppendLine("Справка криминалистики:");
			sb.AppendLine("База PCA (Program Compatibility Assistant) ведет постоянный журнал запуска бинарных файлов.");
			sb.AppendLine("Записи сохраняются даже после очистки корзины, удаления файлов и использования сторонних клинеров.");

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions

		private void OpenFileLocation()
		{
			if (listView.SelectedItems.Count == 0) return;
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null) return;

			if (File.Exists(it.FullPath))
			{
				try
				{
					Process.Start("explorer.exe", "/select,\"" + it.FullPath + "\"");
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				string dir = Path.GetDirectoryName(it.FullPath);
				if (Directory.Exists(dir))
				{
					Process.Start("explorer.exe", dir);
				}
				else
				{
					MessageBox.Show("Файл и родительский каталог были удалены:\n" + it.FullPath, "Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		private void OpenSelectedFile()
		{
			if (listView.SelectedItems.Count == 0) return;
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null || !File.Exists(it.FullPath)) return;

			try
			{
				Process.Start(it.FullPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие файла", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyFilePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null) return;

			Clipboard.SetText(it.FullPath);
			MessageBox.Show("Путь к файлу скопирован в буфер:\n" + it.FullPath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ ЗАПУСКА PCA ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Файл: " + it.FileName);
			sb.AppendLine("Путь: " + it.FullPath);
			sb.AppendLine("Время запуска: " + (it.LaunchTime == DateTime.MinValue ? "AppCompatFlags Store" : it.LaunchTime.ToString("yyyy-MM-dd HH:mm:ss")));
			sb.AppendLine("Наличие на диске: " + (it.FileExistsOnDisk ? "Существует" : "УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)"));
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт правил: " + it.TargetRule);
			sb.AppendLine("Источник: " + it.SourceDb);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			PcaLaunchItem it = listView.SelectedItems[0].Tag as PcaLaunchItem;
			if (it == null) return;

			string cmd;
			if (!it.FileExistsOnDisk && it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Запуск чита {1} с последующим удалением)", targetPlayerName, it.FileName);
			}
			else if (it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Запуск {1} зафиксирован в PCA)", targetPlayerName, it.FileName);
			}
			else if (!it.FileExistsOnDisk)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Удаление исполняемого файла {1})", targetPlayerName, it.FileName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование стороннего ПО", targetPlayerName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
