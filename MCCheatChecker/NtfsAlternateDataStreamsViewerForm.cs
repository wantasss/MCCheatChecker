using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class NtfsAlternateDataStreamsViewerForm : Form
	{
		#region Win32 P/Invoke for NTFS Streams

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct WIN32_FIND_STREAM_DATA
		{
			public long StreamSize;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
			public string cStreamName;
		}

		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr FindFirstStreamW(
			string lpFileName,
			int InfoLevel,
			ref WIN32_FIND_STREAM_DATA lpFindStreamData,
			int dwFlags);

		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FindNextStreamW(
			IntPtr hFindStream,
			ref WIN32_FIND_STREAM_DATA lpFindStreamData);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FindClose(IntPtr hFindFile);

		private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		#endregion

		#region Data Models

		public enum AdsItemCategory
		{
			All,
			ViolationsOnly,
			HiddenStreams,
			ZoneIdentifier,
			Whitelisted
		}

		public class AdsStreamItem
		{
			public int Index;
			public string FileName;
			public string StreamName;
			public long StreamSize;
			public string HostUrl;
			public string ReferrerUrl;
			public int ZoneId;
			public string Verdict;
			public string TargetRule;
			public string FullFilePath;
			public string RawDetails;
			public bool IsViolation;
			public bool IsHiddenStream;
			public bool IsAllowed;
			public AdsItemCategory Category;
			public Color RowColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private AdsItemCategory currentFilter = AdsItemCategory.All;
		private List<AdsStreamItem> allItems = new List<AdsStreamItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtStreamContent;
		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotalFiles;
		private Label lblKpiTotalStreams;
		private Label lblKpiViolations;
		private Label lblKpiHiddenStreams;
		private Label lblKpiAllowed;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterHidden;
		private DarkButton btnFilterZone;
		private DarkButton btnFilterAllowed;

		private DarkButton btnOpenFile;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyUrl;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public NtfsAlternateDataStreamsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "📎 Форензика альтернативных потоков NTFS (ADS) и меток Zone.Identifier — MCCheatChecker";
			Size = new Size(1220, 760);
			MinimumSize = new Size(980, 580);
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
			lblHeaderTitle.Text = "📎 Форензика альтернативных потоков NTFS (ADS) и меток безопасности Zone.Identifier";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Проверка подозрительного: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1180, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotalFiles = CreateKpiLabel("Файлов: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiTotalStreams = CreateKpiLabel("Потоков ADS: 0", Ui.AccentTeal, 140, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Нарушений (5.01): 0", Ui.AccentRed, 310, kpiPanel);
			lblKpiHiddenStreams = CreateKpiLabel("Скрытых потоков (5.08): 0", Ui.AccentOrange, 510, kpiPanel);
			lblKpiAllowed = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 730, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все потоки", AdsItemCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01 / 5.08)", AdsItemCategory.ViolationsOnly, 126, filterPanel);
			btnFilterHidden = CreateFilterButton("⚠️ Скрытые потоки (5.08)", AdsItemCategory.HiddenStreams, 320, filterPanel);
			btnFilterZone = CreateFilterButton("🌐 Zone.Identifier (Интернет)", AdsItemCategory.ZoneIdentifier, 514, filterPanel);
			btnFilterAllowed = CreateFilterButton("✅ Белый список (Allowed)", AdsItemCategory.Whitelisted, 728, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(940, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(995, 9);
			searchBox.Size = new Size(190, 24);
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
			listView.Columns.Add("Файл", 190);
			listView.Columns.Add("Имя потока", 130);
			listView.Columns.Add("Размер", 80);
			listView.Columns.Add("Вердикт", 180);
			listView.Columns.Add("Пункт правил", 130);
			listView.Columns.Add("Host / Referrer URL", 280);
			listView.Columns.Add("Полный путь", 350);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenFileLocation(); };
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Содержимое потока NTFS и параметры загрузки Zone.Identifier:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtStreamContent = new DarkTextBoxMultiline();
			txtStreamContent.Location = new Point(12, 30);
			txtStreamContent.Size = new Size(840, 130);
			txtStreamContent.Font = Ui.FontMono(8.5f);
			txtStreamContent.ReadOnly = true;
			bottomContainer.Controls.Add(txtStreamContent);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(865, 20);
			actionsPanel.Size = new Size(325, 145);

			btnOpenFile = CreateActionButton("📄 Открыть файл", 0, actionsPanel);
			btnOpenFile.Click += delegate { OpenSelectedFile(); };

			btnOpenFolder = CreateActionButton("📂 Открыть папку в Проводнике", 32, actionsPanel);
			btnOpenFolder.Click += delegate { OpenFileLocation(); };

			btnCopyUrl = CreateActionButton("🌐 Копировать URL источника", 64, actionsPanel);
			btnCopyUrl.Click += delegate { CopySourceUrl(); };

			btnCopyDiscord = CreateActionButton("📋 Копировать отчет для Discord", 96, actionsPanel);
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

		private DarkButton CreateFilterButton(string text, AdsItemCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == AdsItemCategory.ViolationsOnly ? 186 : (cat == AdsItemCategory.HiddenStreams ? 186 : (cat == AdsItemCategory.ZoneIdentifier ? 206 : (cat == AdsItemCategory.Whitelisted ? 190 : 106))), 28);
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
			if (cat == AdsItemCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterHidden, btnFilterZone, btnFilterAllowed };
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
			btn.Size = new Size(310, 26);
			btn.Font = Ui.FontBody(8f, true);
			parent.Controls.Add(btn);
			return btn;
		}

		#region Scan Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Сканирование файловой системы NTFS и поиск скрытых потоков ADS...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			txtStreamContent.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<AdsStreamItem> list = new List<AdsStreamItem>();
			int idx = 1;

			// List of target directories
			List<string> targetFolders = new List<string>();

			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcFolder = Path.Combine(appData, ".minecraft");
				if (Directory.Exists(mcFolder))
				{
					targetFolders.Add(mcFolder);
					string modsFolder = Path.Combine(mcFolder, "mods");
					if (Directory.Exists(modsFolder)) targetFolders.Add(modsFolder);
					string versionsFolder = Path.Combine(mcFolder, "versions");
					if (Directory.Exists(versionsFolder)) targetFolders.Add(versionsFolder);
					string configFolder = Path.Combine(mcFolder, "config");
					if (Directory.Exists(configFolder)) targetFolders.Add(configFolder);
				}
			}
			catch { }

			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string downloads = Path.Combine(userProfile, "Downloads");
				if (Directory.Exists(downloads)) targetFolders.Add(downloads);

				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				if (Directory.Exists(desktop)) targetFolders.Add(desktop);

				string temp = Path.GetTempPath();
				if (Directory.Exists(temp)) targetFolders.Add(temp);
			}
			catch { }

			int totalFilesScanned = 0;

			foreach (string folder in targetFolders)
			{
				try
				{
					IEnumerable<string> files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string file in files)
					{
						totalFilesScanned++;
						try
						{
							List<AdsStreamItem> fileStreams = EnumerateStreamsForFile(file, ref idx);
							if (fileStreams.Count > 0)
							{
								list.AddRange(fileStreams);
							}
						}
						catch { }
					}
				}
				catch { }
			}

			e.Result = new Tuple<List<AdsStreamItem>, int>(list, totalFilesScanned);
		}

		private List<AdsStreamItem> EnumerateStreamsForFile(string filePath, ref int idx)
		{
			List<AdsStreamItem> results = new List<AdsStreamItem>();
			WIN32_FIND_STREAM_DATA findData = new WIN32_FIND_STREAM_DATA();
			IntPtr hFind = FindFirstStreamW(filePath, 0, ref findData, 0);

			if (hFind == INVALID_HANDLE_VALUE)
			{
				return results;
			}

			string fileName = Path.GetFileName(filePath);
			string ext = Path.GetExtension(filePath).ToLowerInvariant();

			try
			{
				do
				{
					string sName = findData.cStreamName;
					if (string.IsNullOrEmpty(sName) || sName == "::$DATA")
					{
						continue; // Standard default data stream
					}

					AdsStreamItem item = new AdsStreamItem();
					item.Index = idx++;
					item.FileName = fileName;
					item.StreamName = sName;
					item.StreamSize = findData.StreamSize;
					item.FullFilePath = filePath;
					item.RowColor = Ui.TextPrimary;

					string cleanStreamName = sName;
					if (cleanStreamName.EndsWith(":$DATA", StringComparison.OrdinalIgnoreCase))
					{
						cleanStreamName = cleanStreamName.Substring(0, cleanStreamName.Length - 6);
					}
					if (cleanStreamName.StartsWith(":"))
					{
						cleanStreamName = cleanStreamName.Substring(1);
					}

					// Process Zone.Identifier
					if (cleanStreamName.Equals("Zone.Identifier", StringComparison.OrdinalIgnoreCase))
					{
						item.Category = AdsItemCategory.ZoneIdentifier;
						ParseZoneIdentifierStream(filePath, item);
					}
					else
					{
						// Hidden alternate data stream!
						item.IsHiddenStream = true;
						item.Category = AdsItemCategory.HiddenStreams;
						item.IsViolation = true;
						item.Verdict = "🚨 СКРЫТЫЙ ПОТОК NTFS (5.08 / 5.01)";
						item.TargetRule = "5.08 (Сокрытие улик)";
						item.RowColor = Ui.AccentRed;
						item.RawDetails = "Обнаружен скрытый альтернативный поток данных: [" + cleanStreamName + "], размер: " + FormatBytes(item.StreamSize) + ".\nИсполняемые и конфигурационные файлы могут маскироваться внутри скрытых потоков NTFS.";
					}

					results.Add(item);

				} while (FindNextStreamW(hFind, ref findData));
			}
			finally
			{
				FindClose(hFind);
			}

			return results;
		}

		private void ParseZoneIdentifierStream(string filePath, AdsStreamItem item)
		{
			string fullStreamPath = filePath + ":Zone.Identifier";
			StringBuilder sb = new StringBuilder();
			string hostUrl = string.Empty;
			string referrerUrl = string.Empty;
			int zoneId = -1;

			try
			{
				if (File.Exists(fullStreamPath))
				{
					string[] lines = File.ReadAllLines(fullStreamPath);
					foreach (string rawLine in lines)
					{
						string line = rawLine.Trim();
						sb.AppendLine(line);

						if (line.StartsWith("ZoneId=", StringComparison.OrdinalIgnoreCase))
						{
							int.TryParse(line.Substring(7), out zoneId);
						}
						else if (line.StartsWith("HostUrl=", StringComparison.OrdinalIgnoreCase))
						{
							hostUrl = line.Substring(8).Trim();
						}
						else if (line.StartsWith("ReferrerUrl=", StringComparison.OrdinalIgnoreCase))
						{
							referrerUrl = line.Substring(12).Trim();
						}
					}
				}
			}
			catch (Exception ex)
			{
				sb.AppendLine("[Ошибка чтения потока: " + ex.Message + "]");
			}

			item.ZoneId = zoneId;
			item.HostUrl = hostUrl;
			item.ReferrerUrl = referrerUrl;
			item.RawDetails = sb.ToString();

			// Whitelist check: AnyDesk, RuDesktop, OP Auto Clicker, SpeedAutoClicker
			string lowerName = item.FileName.ToLowerInvariant();
			string lowerHost = (hostUrl + " " + referrerUrl).ToLowerInvariant();

			bool isWhitelisted = lowerName.Contains("anydesk") || lowerName.Contains("rudesktop") ||
				lowerName.Contains("opautoclicker") || lowerName.Contains("speedautoclicker") ||
				lowerName.Contains("tinytask") || lowerName.Contains("x-mouse") ||
				lowerName.Contains("autohotkey") || lowerName.Contains("clickermann") ||
				lowerName.Contains("keyran");

			if (isWhitelisted)
			{
				item.IsAllowed = true;
				item.Category = AdsItemCategory.Whitelisted;
				item.Verdict = "✅ БЕЛЫЙ СПИСОК (РАЗРЕШЕНО)";
				item.TargetRule = "Разрешено регламентом";
				item.RowColor = Ui.AccentGreen;
				return;
			}

			// Violation check against cheat domains and keywords
			string[] cheatKeywords = new string[]
			{
				"celestial", "expensive", "nursultan", "akrien", "wildclient", "nurik",
				"wexside", "deadcode", "minced", "vape", "drip", "slinky", "catlavan",
				"boberware", "neverhook", "fluger", "koid", "injector", "xenos",
				"blast.hk", "yougame.biz", "lolsteam", "zelenka", "exloader",
				"vegaline", "liquidbounce", "meteorclient", "wurstclient", "aristois",
				"thunderhack", "rusherhack", "futureclient", "whiteout", "spirthack"
			};

			bool hasCheatMatch = false;
			string matchedWord = string.Empty;

			foreach (string kw in cheatKeywords)
			{
				if (lowerHost.Contains(kw) || lowerName.Contains(kw))
				{
					hasCheatMatch = true;
					matchedWord = kw;
					break;
				}
			}

			if (hasCheatMatch)
			{
				item.IsViolation = true;
				item.Category = AdsItemCategory.ViolationsOnly;
				item.Verdict = "🚨 ЗАГРУЗКА ЧИТА ИЗ ИНТЕРНЕТА (5.01)";
				item.TargetRule = "5.01 (Чит-клиенты)";
				item.RowColor = Ui.AccentRed;
			}
			else if (zoneId == 3) // Internet Zone
			{
				item.Verdict = "🌐 Загружено из Интернета (Zone 3)";
				item.TargetRule = "Аудит источника";
				item.RowColor = Ui.AccentTeal;
			}
			else
			{
				item.Verdict = "ℹ️ Метка безопасности Zone.Identifier";
				item.TargetRule = "Аудит";
				item.RowColor = Ui.TextSecondary;
			}
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			Tuple<List<AdsStreamItem>, int> res = e.Result as Tuple<List<AdsStreamItem>, int>;
			if (res != null)
			{
				allItems = res.Item1;
				int totalFiles = res.Item2;

				int violations = 0;
				int hidden = 0;
				int allowed = 0;

				foreach (AdsStreamItem it in allItems)
				{
					if (it.IsViolation && !it.IsHiddenStream) violations++;
					if (it.IsHiddenStream) hidden++;
					if (it.IsAllowed) allowed++;
				}

				lblKpiTotalFiles.Text = "Файлов проверено: " + totalFiles;
				lblKpiTotalStreams.Text = "Потоков ADS: " + allItems.Count;
				lblKpiViolations.Text = "Нарушений (5.01): " + violations;
				lblKpiHiddenStreams.Text = "Скрытых потоков (5.08): " + hidden;
				lblKpiAllowed.Text = "Белый список: " + allowed;

				if (violations > 0 || hidden > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Найдено {0} нарушений Zone.Identifier и {1} скрытых потоков ADS!", violations, hidden);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ Сканирование ADS завершено: скрытых потоков и загрузок читов не обнаружено.";
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

			foreach (AdsStreamItem it in allItems)
			{
				if (currentFilter == AdsItemCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == AdsItemCategory.HiddenStreams && !it.IsHiddenStream) continue;
				if (currentFilter == AdsItemCategory.ZoneIdentifier && it.Category != AdsItemCategory.ZoneIdentifier) continue;
				if (currentFilter == AdsItemCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.FileName.ToLowerInvariant().Contains(search) ||
						it.StreamName.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						(it.HostUrl != null && it.HostUrl.ToLowerInvariant().Contains(search)) ||
						(it.ReferrerUrl != null && it.ReferrerUrl.ToLowerInvariant().Contains(search)) ||
						it.FullFilePath.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.FileName);
				lvi.SubItems.Add(it.StreamName);
				lvi.SubItems.Add(FormatBytes(it.StreamSize));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.HostUrl) ? (string.IsNullOrEmpty(it.ReferrerUrl) ? "-" : it.ReferrerUrl) : it.HostUrl);
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
			else
			{
				txtStreamContent.Text = "Нет записей, соответствующих выбранному фильтру.";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АЛЬТЕРНАТИВНЫЙ ПОТОК ДАННЫХ NTFS (ADS) ===");
			sb.AppendLine("Файл: " + it.FullFilePath);
			sb.AppendLine("Поток: " + it.StreamName);
			sb.AppendLine("Размер: " + FormatBytes(it.StreamSize) + " (" + it.StreamSize + " байт)");
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine();

			if (!string.IsNullOrEmpty(it.HostUrl))
			{
				sb.AppendLine("Host URL: " + it.HostUrl);
			}
			if (!string.IsNullOrEmpty(it.ReferrerUrl))
			{
				sb.AppendLine("Referrer URL: " + it.ReferrerUrl);
			}
			if (it.ZoneId >= 0)
			{
				sb.AppendLine("Zone ID: " + it.ZoneId + " (" + GetZoneDescription(it.ZoneId) + ")");
			}

			sb.AppendLine();
			sb.AppendLine("=== СЫРЫЕ ДАННЫЕ ПОТОКА ===");
			sb.AppendLine(it.RawDetails);

			txtStreamContent.Text = sb.ToString();
		}

		private string GetZoneDescription(int zoneId)
		{
			switch (zoneId)
			{
				case 0: return "Локальный компьютер (My Computer)";
				case 1: return "Местная интрасеть (Local Intranet)";
				case 2: return "Надежные узлы (Trusted Sites)";
				case 3: return "Интернет (Internet - скачано из сети)";
				case 4: return "Ограниченные узлы (Restricted Sites)";
				default: return "Пользовательская зона";
			}
		}

		private string FormatBytes(long bytes)
		{
			if (bytes < 1024) return bytes + " B";
			if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
			return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
		}

		#endregion

		#region Actions

		private void OpenFileLocation()
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null || !File.Exists(it.FullFilePath)) return;

			try
			{
				Process.Start("explorer.exe", "/select,\"" + it.FullFilePath + "\"");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenSelectedFile()
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null || !File.Exists(it.FullFilePath)) return;

			try
			{
				Process.Start(it.FullFilePath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие файла", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopySourceUrl()
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null) return;

			string url = !string.IsNullOrEmpty(it.HostUrl) ? it.HostUrl : it.ReferrerUrl;
			if (!string.IsNullOrEmpty(url))
			{
				Clipboard.SetText(url);
				MessageBox.Show("URL скопирован в буфер:\n" + url, "Копирование URL", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
			{
				MessageBox.Show("У выбранного элемента отсутствует URL источника.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ NTFS ADS / ZONE.IDENTIFIER ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Файл: " + it.FileName);
			sb.AppendLine("Поток: " + it.StreamName);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			if (!string.IsNullOrEmpty(it.HostUrl)) sb.AppendLine("Host URL: " + it.HostUrl);
			if (!string.IsNullOrEmpty(it.ReferrerUrl)) sb.AppendLine("Referrer URL: " + it.ReferrerUrl);
			sb.AppendLine("Путь: " + it.FullFilePath);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			AdsStreamItem it = listView.SelectedItems[0].Tag as AdsStreamItem;
			if (it == null) return;

			string cmd;
			if (it.IsHiddenStream)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Скрытый поток NTFS ADS {1})", targetPlayerName, it.StreamName);
			}
			else if (it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Загрузка чита из сети: {1})", targetPlayerName, it.FileName);
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
