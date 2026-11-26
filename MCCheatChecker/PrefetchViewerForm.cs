using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class PrefetchViewerForm : Form
	{
		private enum PrefetchFilter
		{
			All,
			CheatsOnly,
			CleanersOnly,
			MinecraftOnly,
			Last24Hours,
			WhitelistOnly
		}

		private class PrefetchItem
		{
			public int Index;
			public string ExeName;
			public string Hash;
			public string PfFileName;
			public string FullPfPath;
			public uint RunCount;
			public DateTime LastExecution;
			public List<DateTime> AllExecutions = new List<DateTime>();
			public List<string> LoadedModules = new List<string>();
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsMinecraft;
			public bool IsWhitelisted;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		[DllImport("ntdll.dll", SetLastError = false)]
		private static extern uint RtlDecompressBufferEx(
			ushort CompressionFormat,
			byte[] UncompressedBuffer,
			int UncompressedBufferSize,
			byte[] CompressedBuffer,
			int CompressedBufferSize,
			out int FinalUncompressedSize,
			IntPtr WorkSpace);

		[DllImport("ntdll.dll", SetLastError = false)]
		private static extern uint RtlDecompressBuffer(
			ushort CompressionFormat,
			byte[] UncompressedBuffer,
			int UncompressedBufferSize,
			byte[] CompressedBuffer,
			int CompressedBufferSize,
			out int FinalUncompressedSize);

		private const ushort COMPRESSION_FORMAT_XPRESS_HUFF = 0x0004;

		private string targetPlayerName;
		private PrefetchFilter currentFilter = PrefetchFilter.All;
		private List<PrefetchItem> allItems = new List<PrefetchItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailExe;
		private Label lblDetailHash;
		private Label lblDetailRunCount;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;
		private DarkListView listLaunches;
		private DarkListView listModules;
		private DarkTextBox searchModulesBox;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterMinecraft;
		private DarkButton btnFilter24h;
		private DarkButton btnFilterWhitelist;

		private static readonly Regex CheatExePattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject|hacks?|читы|софт|кликер|clicker|macro|trigger|aimbot|killaura|autoclicker)",
			RegexOptions.Compiled);

		private static readonly Regex CleanerExePattern = new Regex(
			@"(?i)(ccleaner|bleachbit|privazer|eraser|sdelete|cleanmgr|journaltrace|prefetchcleaner|logcleaner|winsweeper|bypass-cleaner|bcleaner|systemcleaner|tracecleaner|usncleaner|regwipe|eventcleaner|batcleaner|cleanpre|cleanall|wiper|destroyer|redengine|viper|echo)",
			RegexOptions.Compiled);

		public PrefetchViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadPrefetchDataAsync();
		}

		private void InitializeComponent()
		{
			Text = "⚡ Инспектор истории запусков Windows Prefetch (.pf Forensics & Run History)";
			Size = new Size(1220, 780);
			MinimumSize = new Size(960, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 114;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "⚡  ИНСПЕКТОР ЗАПУСКОВ PREFETCH (MAM / SCCA Forensics & 8-Launch Timeline)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentOrange;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Декодирование C:\\Windows\\Prefetch: учёт количества запусков, точное время последних 8 запусков и загруженные DLL...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование папки Prefetch...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(640, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(910, 8);
			btnRefresh.Size = new Size(140, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { LoadPrefetchDataAsync(); };
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Папка Prefetch";
			btnOpenFolder.Location = new Point(1056, 8);
			btnOpenFolder.Size = new Size(135, 26);
			btnOpenFolder.BackColor = Ui.SurfaceAlt;
			btnOpenFolder.Font = Ui.FontBody(8.5f, true);
			btnOpenFolder.Cursor = Cursors.Hand;
			btnOpenFolder.Click += delegate
			{
				try
				{
					string pfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
					if (Directory.Exists(pfDir)) Process.Start("explorer.exe", pfDir);
				}
				catch { }
			};
			topPanel.Controls.Add(btnOpenFolder);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все (.pf) (0)", PrefetchFilter.All, 115);
			btnFilterCheats = CreateFilterButton("🚨 Читы & Инжекторы (0)", PrefetchFilter.CheatsOnly, 185);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры 5.08 (0)", PrefetchFilter.CleanersOnly, 140);
			btnFilterMinecraft = CreateFilterButton("☕ Minecraft / Java (0)", PrefetchFilter.MinecraftOnly, 160);
			btnFilter24h = CreateFilterButton("⏱️ За 24 часа (0)", PrefetchFilter.Last24Hours, 130);
			btnFilterWhitelist = CreateFilterButton("✔ Whitelist (0)", PrefetchFilter.WhitelistOnly, 120);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterMinecraft, btnFilter24h, btnFilterWhitelist };
			foreach (DarkButton b in filterButtons)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 6;
			}
			topPanel.Controls.Add(navRow);

			Panel searchRow = new Panel();
			searchRow.Location = new Point(12, 85);
			searchRow.Size = new Size(1180, 24);
			searchRow.BackColor = Color.Transparent;

			Label lblSearch = new Label();
			lblSearch.Text = "Быстрый поиск:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(0, 3);
			lblSearch.AutoSize = true;
			searchRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(105, 0);
			searchBox.Size = new Size(360, 23);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			searchRow.Controls.Add(searchBox);

			DarkButton btnClearSearch = new DarkButton();
			btnClearSearch.Text = "✕ Очистить";
			btnClearSearch.Location = new Point(472, 0);
			btnClearSearch.Size = new Size(85, 23);
			btnClearSearch.BackColor = Ui.SurfaceAlt;
			btnClearSearch.Font = Ui.FontBody(8f);
			btnClearSearch.Cursor = Cursors.Hand;
			btnClearSearch.Click += delegate { searchBox.Text = ""; };
			searchRow.Controls.Add(btnClearSearch);

			Label lblHint = new Label();
			lblHint.Text = "💡 Подсказка: Кликните дважды по строке для копирования команды бана. Изучите историю запусков и загруженные DLL справа.";
			lblHint.Font = Ui.FontBody(8f);
			lblHint.ForeColor = Ui.TextMuted;
			lblHint.Location = new Point(570, 4);
			lblHint.AutoSize = true;
			searchRow.Controls.Add(lblHint);

			topPanel.Controls.Add(searchRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail/Modules panes
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1180, 600);
			try { split.SplitterDistance = 390; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 175);
			listView.Columns.Add("Исполняемый файл", 180);
			listView.Columns.Add("Запусков", 85);
			listView.Columns.Add("Последний запуск", 155);
			listView.Columns.Add("Всего запусков в истории", 170);
			listView.Columns.Add("Хеш", 85);
			listView.Columns.Add("Файл Prefetch (.pf)", 260);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details, Launches history, and Loaded DLLs
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 8, 12, 8);

			// Left: Info & Ban
			Panel pnlInfo = new Panel();
			pnlInfo.Location = new Point(12, 8);
			pnlInfo.Size = new Size(390, 240);
			pnlInfo.BackColor = Color.Transparent;

			lblDetailExe = new Label();
			lblDetailExe.Text = "Выберите процесс для анализа...";
			lblDetailExe.Font = Ui.FontTitle(10f, true);
			lblDetailExe.ForeColor = Ui.TextPrimary;
			lblDetailExe.Location = new Point(0, 0);
			lblDetailExe.AutoSize = true;
			pnlInfo.Controls.Add(lblDetailExe);

			lblDetailHash = new Label();
			lblDetailHash.Text = "Хеш: —";
			lblDetailHash.Font = Ui.FontBody(8.5f);
			lblDetailHash.ForeColor = Ui.TextSecondary;
			lblDetailHash.Location = new Point(0, 22);
			lblDetailHash.AutoSize = true;
			pnlInfo.Controls.Add(lblDetailHash);

			lblDetailRunCount = new Label();
			lblDetailRunCount.Text = "Счётчик запусков: —";
			lblDetailRunCount.Font = Ui.FontBody(8.5f);
			lblDetailRunCount.ForeColor = Ui.TextSecondary;
			lblDetailRunCount.Location = new Point(0, 42);
			lblDetailRunCount.AutoSize = true;
			pnlInfo.Controls.Add(lblDetailRunCount);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Статус: —";
			lblDetailStatus.Font = Ui.FontBody(8.5f, true);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Location = new Point(0, 62);
			lblDetailStatus.AutoSize = true;
			pnlInfo.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Правило: —";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.AccentTeal;
			lblDetailRule.Location = new Point(0, 82);
			lblDetailRule.AutoSize = true;
			pnlInfo.Controls.Add(lblDetailRule);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Location = new Point(0, 104);
			txtDetailDescription.Size = new Size(385, 80);
			txtDetailDescription.Font = Ui.FontMono(8f);
			txtDetailDescription.ReadOnly = true;
			pnlInfo.Controls.Add(txtDetailDescription);

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать /ban";
			btnCopyBan.Location = new Point(0, 190);
			btnCopyBan.Size = new Size(160, 27);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			pnlInfo.Controls.Add(btnCopyBan);

			DarkButton btnVt = new DarkButton();
			btnVt.Text = "🔍 VirusTotal";
			btnVt.Location = new Point(166, 190);
			btnVt.Size = new Size(100, 27);
			btnVt.BackColor = Ui.SurfaceAlt;
			btnVt.Font = Ui.FontBody(8.5f, true);
			btnVt.Cursor = Cursors.Hand;
			btnVt.Click += delegate { CheckVirusTotal(); };
			pnlInfo.Controls.Add(btnVt);

			DarkButton btnCopyPath = new DarkButton();
			btnCopyPath.Text = "📄 Скопировать путь";
			btnCopyPath.Location = new Point(272, 190);
			btnCopyPath.Size = new Size(113, 27);
			btnCopyPath.BackColor = Ui.SurfaceAlt;
			btnCopyPath.Font = Ui.FontBody(8f);
			btnCopyPath.Cursor = Cursors.Hand;
			btnCopyPath.Click += delegate
			{
				PrefetchItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FullPfPath))
				{
					Clipboard.SetText(sel.FullPfPath);
					MessageBox.Show("Путь к файлу скопирован:\n" + sel.FullPfPath, "Prefetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			pnlInfo.Controls.Add(btnCopyPath);

			detailContainer.Controls.Add(pnlInfo);

			// Middle: Launch History (up to 8 timestamps)
			Panel pnlHistory = new Panel();
			pnlHistory.Location = new Point(412, 8);
			pnlHistory.Size = new Size(290, 240);
			pnlHistory.BackColor = Color.Transparent;

			Label lblHistoryTitle = new Label();
			lblHistoryTitle.Text = "🕒 История запусков (до 8 запусков):";
			lblHistoryTitle.Font = Ui.FontBody(8.5f, true);
			lblHistoryTitle.ForeColor = Ui.TextSecondary;
			lblHistoryTitle.Location = new Point(0, 0);
			lblHistoryTitle.AutoSize = true;
			pnlHistory.Controls.Add(lblHistoryTitle);

			listLaunches = new DarkListView();
			listLaunches.Location = new Point(0, 22);
			listLaunches.Size = new Size(285, 195);
			listLaunches.View = View.Details;
			listLaunches.FullRowSelect = true;
			listLaunches.GridLines = false;
			listLaunches.HeaderStyle = ColumnHeaderStyle.Nonclickable;
			listLaunches.Columns.Add("#", 35);
			listLaunches.Columns.Add("Дата и время запуска", 155);
			listLaunches.Columns.Add("Давность", 85);
			pnlHistory.Controls.Add(listLaunches);

			detailContainer.Controls.Add(pnlHistory);

			// Right: Loaded DLLs / Modules
			Panel pnlModules = new Panel();
			pnlModules.Location = new Point(712, 8);
			pnlModules.Size = new Size(480, 240);
			pnlModules.BackColor = Color.Transparent;

			Label lblModulesTitle = new Label();
			lblModulesTitle.Text = "📦 Загруженные модули & DLL (первые 10 сек):";
			lblModulesTitle.Font = Ui.FontBody(8.5f, true);
			lblModulesTitle.ForeColor = Ui.TextSecondary;
			lblModulesTitle.Location = new Point(0, 0);
			lblModulesTitle.AutoSize = true;
			pnlModules.Controls.Add(lblModulesTitle);

			searchModulesBox = new DarkTextBox();
			searchModulesBox.Location = new Point(280, 0);
			searchModulesBox.Size = new Size(195, 20);
			searchModulesBox.Font = Ui.FontBody(8f);
			searchModulesBox.TextChanged += delegate { FilterLoadedModules(); };
			pnlModules.Controls.Add(searchModulesBox);

			listModules = new DarkListView();
			listModules.Location = new Point(0, 24);
			listModules.Size = new Size(475, 193);
			listModules.View = View.Details;
			listModules.FullRowSelect = true;
			listModules.GridLines = false;
			listModules.HeaderStyle = ColumnHeaderStyle.Nonclickable;
			listModules.Columns.Add("Модуль / Путь к DLL", 360);
			listModules.Columns.Add("Тип", 105);
			pnlModules.Controls.Add(listModules);

			detailContainer.Controls.Add(pnlModules);

			detailContainer.Resize += delegate
			{
				int w = detailContainer.ClientSize.Width - 24;
				int h = detailContainer.ClientSize.Height - 16;
				if (w < 800) return;

				pnlInfo.Width = 380;
				pnlHistory.Location = new Point(390, 8);
				pnlHistory.Width = 280;
				listLaunches.Width = 275;

				int modX = 680;
				int modW = w - modX;
				if (modW > 200)
				{
					pnlModules.Location = new Point(modX, 8);
					pnlModules.Width = modW;
					listModules.Width = modW - 5;
					searchModulesBox.Location = new Point(Math.Max(180, modW - 200), 0);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);
		}

		private DarkButton CreateFilterButton(string text, PrefetchFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentBlue : Ui.SurfaceAlt;
			btn.Font = Ui.FontBody(8f, filter == currentFilter);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtonsVisual();
				ApplyFilters();
			};
			return btn;
		}

		private void UpdateFilterButtonsVisual()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterMinecraft, btnFilter24h, btnFilterWhitelist };
			PrefetchFilter[] filters = { PrefetchFilter.All, PrefetchFilter.CheatsOnly, PrefetchFilter.CleanersOnly, PrefetchFilter.MinecraftOnly, PrefetchFilter.Last24Hours, PrefetchFilter.WhitelistOnly };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentBlue : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void LoadPrefetchDataAsync()
		{
			lblStatusPill.Text = "⏳ Сканирование папки Prefetch...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<PrefetchItem> items = new List<PrefetchItem>();
				string pfFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

				if (!Directory.Exists(pfFolder))
				{
					e.Result = items;
					return;
				}

				try
				{
					string[] pfFiles = Directory.GetFiles(pfFolder, "*.pf");
					int index = 1;

					foreach (string pfPath in pfFiles)
					{
						try
						{
							PrefetchItem item = ParsePrefetchFile(pfPath);
							if (item != null)
							{
								item.Index = index++;
								items.Add(item);
							}
						}
						catch { }
					}
				}
				catch (UnauthorizedAccessException)
				{
					// Flag lack of admin permissions
				}
				catch { }

				// Sort items: newest executions first, cheats prioritized
				items.Sort(delegate(PrefetchItem a, PrefetchItem b)
				{
					if (a.IsCheat && !b.IsCheat) return -1;
					if (!a.IsCheat && b.IsCheat) return 1;
					if (a.IsCleaner && !b.IsCleaner) return -1;
					if (!a.IsCleaner && b.IsCleaner) return 1;
					return b.LastExecution.CompareTo(a.LastExecution);
				});

				e.Result = items;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<PrefetchItem>)
				{
					allItems = (List<PrefetchItem>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static PrefetchItem ParsePrefetchFile(string pfPath)
		{
			string fileName = Path.GetFileName(pfPath);
			Match fnMatch = Regex.Match(fileName, @"^(?<exe>.+)-(?<hash>[0-9A-Fa-f]{8})\.pf$", RegexOptions.IgnoreCase);
			string exeName = fnMatch.Success ? fnMatch.Groups["exe"].Value : fileName;
			string hash = fnMatch.Success ? fnMatch.Groups["hash"].Value.ToUpperInvariant() : "—";

			PrefetchItem item = new PrefetchItem();
			item.FullPfPath = pfPath;
			item.PfFileName = fileName;
			item.ExeName = exeName.ToUpperInvariant();
			item.Hash = hash;

			// File system timestamps as baseline
			DateTime lastWrite = File.GetLastWriteTime(pfPath);
			DateTime creation = File.GetCreationTime(pfPath);
			item.LastExecution = lastWrite > creation ? lastWrite : creation;

			byte[] rawBytes = null;
			try
			{
				using (FileStream fs = new FileStream(pfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					rawBytes = new byte[fs.Length];
					fs.Read(rawBytes, 0, rawBytes.Length);
				}
			}
			catch { }

			if (rawBytes != null && rawBytes.Length >= 8)
			{
				byte[] decompressed = DecompressPrefetch(rawBytes);
				if (decompressed != null && decompressed.Length >= 0x84)
				{
					int version = BitConverter.ToInt32(decompressed, 0);

					// Parse Unicode exe name if available at 0x10
					try
					{
						string parsedExe = Encoding.Unicode.GetString(decompressed, 0x10, Math.Min(60, decompressed.Length - 0x10));
						int nullIdx = parsedExe.IndexOf('\0');
						if (nullIdx > 0) parsedExe = parsedExe.Substring(0, nullIdx);
						if (!string.IsNullOrEmpty(parsedExe) && parsedExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
						{
							item.ExeName = parsedExe.ToUpperInvariant();
						}
					}
					catch { }

					// Parse Hash at 0x4C
					if (decompressed.Length >= 0x50)
					{
						try
						{
							uint parsedHash = BitConverter.ToUInt32(decompressed, 0x4C);
							if (parsedHash > 0) item.Hash = parsedHash.ToString("X8");
						}
						catch { }
					}

					// Parse Run Count
					try
					{
						int runCountOffset = (version >= 26) ? 0xD0 : ((version == 23) ? 0x98 : 0x90);
						if (decompressed.Length >= runCountOffset + 4)
						{
							uint rc = BitConverter.ToUInt32(decompressed, runCountOffset);
							if (rc > 0 && rc < 1000000) item.RunCount = rc;
						}
					}
					catch { }

					// Parse Launch Timestamps (up to 8 in Win8/10/11)
					try
					{
						int maxTimestamps = (version >= 26) ? 8 : 1;
						int tsBaseOffset = 0x80;

						for (int t = 0; t < maxTimestamps; t++)
						{
							int ftOffset = tsBaseOffset + (t * 8);
							if (decompressed.Length >= ftOffset + 8)
							{
								long ft = BitConverter.ToInt64(decompressed, ftOffset);
								if (ft > 0)
								{
									try
									{
										DateTime dt = DateTime.FromFileTimeUtc(ft).ToLocalTime();
										if (dt.Year >= 2010 && dt.Year <= 2035)
										{
											if (!item.AllExecutions.Contains(dt))
											{
												item.AllExecutions.Add(dt);
											}
										}
									}
									catch { }
								}
							}
						}
						if (item.AllExecutions.Count > 0)
						{
							item.AllExecutions.Sort(delegate(DateTime a, DateTime b) { return b.CompareTo(a); });
							item.LastExecution = item.AllExecutions[0];
						}
					}
					catch { }

					// Extract loaded strings / modules
					try
					{
						List<string> modules = ExtractLoadedModules(decompressed);
						item.LoadedModules = modules;
					}
					catch { }
				}
			}

			if (item.RunCount == 0) item.RunCount = 1;
			if (item.AllExecutions.Count == 0) item.AllExecutions.Add(item.LastExecution);

			// Classify process and determine verdict
			ClassifyItem(item);

			return item;
		}

		private static byte[] DecompressPrefetch(byte[] fileBytes)
		{
			if (fileBytes == null || fileBytes.Length < 8) return null;

			// MAM Header check
			if (fileBytes[0] == 0x4D && fileBytes[1] == 0x41 && fileBytes[2] == 0x4D && fileBytes[3] == 0x04)
			{
				uint uncompressedSize = BitConverter.ToUInt32(fileBytes, 4);
				if (uncompressedSize == 0 || uncompressedSize > 80 * 1024 * 1024) return null;

				byte[] compressed = new byte[fileBytes.Length - 8];
				Buffer.BlockCopy(fileBytes, 8, compressed, 0, compressed.Length);

				byte[] decompressed = new byte[uncompressedSize];
				int finalSize = 0;

				try
				{
					uint status = RtlDecompressBufferEx(COMPRESSION_FORMAT_XPRESS_HUFF, decompressed, decompressed.Length, compressed, compressed.Length, out finalSize, IntPtr.Zero);
					if (status == 0) return decompressed;
				}
				catch { }

				try
				{
					uint status = RtlDecompressBuffer(COMPRESSION_FORMAT_XPRESS_HUFF, decompressed, decompressed.Length, compressed, compressed.Length, out finalSize);
					if (status == 0) return decompressed;
				}
				catch { }

				return null;
			}

			// If uncompressed SCCA
			if (fileBytes.Length >= 8 && fileBytes[4] == 0x53 && fileBytes[5] == 0x43 && fileBytes[6] == 0x43 && fileBytes[7] == 0x41)
			{
				return fileBytes;
			}

			return fileBytes;
		}

		private static List<string> ExtractLoadedModules(byte[] data)
		{
			List<string> modules = new List<string>();
			if (data == null || data.Length < 100) return modules;

			// Scan for null-terminated Unicode strings with paths or DLL extensions
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < data.Length - 1; i += 2)
			{
				char c = (char)BitConverter.ToUInt16(data, i);
				if (c >= 32 && c <= 126 || c >= 0x0400 && c <= 0x04FF)
				{
					sb.Append(c);
				}
				else
				{
					if (sb.Length >= 4)
					{
						string s = sb.ToString();
						if (s.IndexOf('\\') >= 0 || s.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
							s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
						{
							// Clean device prefixes
							string clean = s;
							int devIdx = clean.IndexOf(@"\DEVICE\HARDDISKVOLUME", StringComparison.OrdinalIgnoreCase);
							if (devIdx >= 0)
							{
								int slashAfter = clean.IndexOf('\\', devIdx + 22);
								if (slashAfter >= 0) clean = "C:" + clean.Substring(slashAfter);
							}
							if (!modules.Contains(clean)) modules.Add(clean);
						}
					}
					sb.Length = 0;
				}
			}
			return modules;
		}

		private static void ClassifyItem(PrefetchItem item)
		{
			string nameLower = item.ExeName.ToLowerInvariant();

			// 1. Whitelist Check (Strict 5.01 Whitelist protection)
			bool isWhite = false;
			foreach (string w in CheatDatabases.WhitelistProcessNames)
			{
				if (nameLower.Contains(w.ToLowerInvariant())) { isWhite = true; break; }
			}
			if (nameLower.Contains("anydesk") || nameLower.Contains("rudesktop") || nameLower.Contains("autohotkey") ||
				nameLower.Contains("opautoclicker") || nameLower.Contains("speedautoclicker") || nameLower.Contains("tinytask") ||
				nameLower.Contains("clickermann") || nameLower.Contains("keyran") || nameLower.Contains("x-mouse"))
			{
				isWhite = true;
			}

			if (isWhite)
			{
				item.IsWhitelisted = true;
				item.Verdict = "✔ Whitelist (5.01)";
				item.ThreatReason = "Разрешённое ПО / Автокликер / Утилита ScreenShare (белый список)";
				item.ForeColor = Ui.AccentGreen;
				return;
			}

			// 2. Minecraft / Java Check
			if (nameLower.Contains("javaw") || nameLower.Contains("java") || nameLower.Contains("minecraft") || nameLower.Contains("bedrock"))
			{
				item.IsMinecraft = true;

				// Deep inspect javaw loaded modules for injected cheats
				bool suspiciousDllLoaded = false;
				string foundDll = "";
				foreach (string mod in item.LoadedModules)
				{
					string modLower = mod.ToLowerInvariant();
					if (modLower.Contains("vape") || modLower.Contains("celestial") || modLower.Contains("expensive") ||
						modLower.Contains("rich") || modLower.Contains("akrien") || modLower.Contains("drip") ||
						modLower.Contains("koid") || modLower.Contains("slinky") || modLower.Contains("inject"))
					{
						suspiciousDllLoaded = true;
						foundDll = mod;
						break;
					}
				}

				if (suspiciousDllLoaded)
				{
					item.IsCheat = true;
					item.Verdict = "🚨 Инжекция в javaw (5.01)";
					item.ThreatReason = "В Prefetch javaw.exe обнаружена загрузка стороннего чит-модуля: " + foundDll;
					item.ForeColor = Ui.AccentRed;
					return;
				}

				item.Verdict = "☕ Minecraft / Java";
				item.ThreatReason = "Штатный исполняемый файл среды Java / Minecraft";
				item.ForeColor = Ui.AccentTeal;
				return;
			}

			// 3. Cleaners Check (Rule 5.08)
			bool isCleaner = CleanerExePattern.IsMatch(nameLower);
			if (!isCleaner)
			{
				foreach (string cl in CheatDatabases.KnownCleanerSignatures)
				{
					if (cl.Length >= 4 && nameLower.Contains(cl)) { isCleaner = true; break; }
				}
			}
			if (isCleaner)
			{
				item.IsCleaner = true;
				item.Verdict = "🧹 Клинер сокрытия (5.08)";
				item.ThreatReason = "Запуск утилиты очистки логов, реестра, USN журнала или Prefetch (Пункт 5.08)";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 4. Cheats & Injectors Check (Rule 5.01)
			bool isCheat = CheatExePattern.IsMatch(nameLower);
			if (!isCheat)
			{
				foreach (string ec in CheatDatabases.ExtraCheatClients)
				{
					if (ec.Length >= 4 && nameLower.Contains(ec.ToLowerInvariant())) { isCheat = true; break; }
				}
			}
			if (!isCheat)
			{
				foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
				{
					if (inj.Length >= 4 && nameLower.Contains(inj.ToLowerInvariant())) { isCheat = true; break; }
				}
			}

			if (isCheat)
			{
				item.IsCheat = true;
				item.Verdict = "🚨 Запуск чита (5.01)";
				item.ThreatReason = "Зафиксирован запуск запрещённого чит-клиента или инжектора (Пункт 5.01)";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// Normal process
			item.Verdict = "Обычный процесс";
			item.ThreatReason = "Системное или пользовательское приложение Windows";
			item.ForeColor = Ui.TextPrimary;
		}

		private void UpdateSummaryStats()
		{
			int total = allItems.Count;
			int cheats = allItems.Count(i => i.IsCheat);
			int cleaners = allItems.Count(i => i.IsCleaner);
			int mc = allItems.Count(i => i.IsMinecraft);
			int white = allItems.Count(i => i.IsWhitelisted);
			DateTime past24 = DateTime.Now.AddHours(-24);
			int fresh = allItems.Count(i => i.LastExecution >= past24);

			btnFilterAll.Text = string.Format("🔍 Все ({0})", total);
			btnFilterCheats.Text = string.Format("🚨 Читы & Инжекторы ({0})", cheats);
			btnFilterCleaners.Text = string.Format("🧹 Клинеры 5.08 ({0})", cleaners);
			btnFilterMinecraft.Text = string.Format("☕ Minecraft ({0})", mc);
			btnFilter24h.Text = string.Format("⏱️ За 24ч ({0})", fresh);
			btnFilterWhitelist.Text = string.Format("✔ Whitelist ({0})", white);

			if (total == 0)
			{
				lblStatusPill.Text = "⚠️ Prefetch пуст или служба SysMain отключена (Возможно 5.08)";
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else if (cheats > 0 || cleaners > 0)
			{
				lblStatusPill.Text = string.Format("🚨 НАЙДЕНО УЛИК: Читов {0}, Клинеров {1}", cheats, cleaners);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔ Проверено {0} записей Prefetch. Читов не обнаружено.", total);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();
			DateTime past24 = DateTime.Now.AddHours(-24);

			foreach (PrefetchItem item in allItems)
			{
				switch (currentFilter)
				{
					case PrefetchFilter.CheatsOnly:
						if (!item.IsCheat) continue;
						break;
					case PrefetchFilter.CleanersOnly:
						if (!item.IsCleaner) continue;
						break;
					case PrefetchFilter.MinecraftOnly:
						if (!item.IsMinecraft) continue;
						break;
					case PrefetchFilter.Last24Hours:
						if (item.LastExecution < past24) continue;
						break;
					case PrefetchFilter.WhitelistOnly:
						if (!item.IsWhitelisted) continue;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = item.ExeName.ToLowerInvariant().Contains(query) ||
								 item.Hash.ToLowerInvariant().Contains(query) ||
								 item.Verdict.ToLowerInvariant().Contains(query) ||
								 item.PfFileName.ToLowerInvariant().Contains(query);
					if (!match)
					{
						foreach (string mod in item.LoadedModules)
						{
							if (mod.ToLowerInvariant().Contains(query)) { match = true; break; }
						}
					}
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.SubItems.Add(item.ExeName);
				lvi.SubItems.Add(item.RunCount.ToString() + " раз");
				lvi.SubItems.Add(item.LastExecution.ToString("dd.MM.yyyy HH:mm:ss"));
				lvi.SubItems.Add(item.AllExecutions.Count.ToString() + " записей в истории");
				lvi.SubItems.Add(item.Hash);
				lvi.SubItems.Add(item.PfFileName);

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
				ClearDetailPane();
			}
		}

		private PrefetchItem GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as PrefetchItem;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			PrefetchItem sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailExe.Text = sel.ExeName;
			lblDetailHash.Text = "Хеш Prefetch: " + sel.Hash;
			lblDetailRunCount.Text = string.Format("Счётчик запусков: {0} раз | Записей в заголовке: {1}", sel.RunCount, sel.AllExecutions.Count);
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;

			if (sel.IsCheat) lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-программы / Стороннее ПО) — Бан 30 дней";
			else if (sel.IsCleaner) lblDetailRule.Text = "Пункт регламента: 5.08 (Очистка логов / Сокрытие улик) — Бан 30 дней";
			else if (sel.IsWhitelisted) lblDetailRule.Text = "Пункт регламента: 5.01 Белый список (Разрешено)";
			else lblDetailRule.Text = "Пункт регламента: Нарушений не зафиксировано";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Файл: " + sel.PfFileName);
			sb.AppendLine("Путь: " + sel.FullPfPath);
			sb.AppendLine("Последний запуск: " + sel.LastExecution.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Обоснование: " + sel.ThreatReason);
			txtDetailDescription.Text = sb.ToString();

			// Fill Launches history
			listLaunches.BeginUpdate();
			listLaunches.Items.Clear();
			for (int i = 0; i < sel.AllExecutions.Count; i++)
			{
				DateTime dt = sel.AllExecutions[i];
				TimeSpan diff = DateTime.Now - dt;
				string ago = diff.TotalHours < 1 ? string.Format("{0} мин назад", (int)diff.TotalMinutes) :
							 (diff.TotalDays < 1 ? string.Format("{0} ч назад", (int)diff.TotalHours) :
							 string.Format("{0} д назад", (int)diff.TotalDays));

				ListViewItem lvi = new ListViewItem((i + 1).ToString());
				lvi.SubItems.Add(dt.ToString("dd.MM.yyyy HH:mm:ss"));
				lvi.SubItems.Add(ago);
				lvi.ForeColor = (i == 0 && sel.IsCheat) ? Ui.AccentRed : Ui.TextPrimary;
				listLaunches.Items.Add(lvi);
			}
			listLaunches.EndUpdate();

			// Fill Modules
			FilterLoadedModules();
		}

		private void FilterLoadedModules()
		{
			PrefetchItem sel = GetSelectedItem();
			if (sel == null)
			{
				listModules.Items.Clear();
				return;
			}

			string q = (searchModulesBox.Text ?? "").Trim().ToLowerInvariant();
			listModules.BeginUpdate();
			listModules.Items.Clear();

			foreach (string mod in sel.LoadedModules)
			{
				if (!string.IsNullOrEmpty(q) && !mod.ToLowerInvariant().Contains(q)) continue;

				string modLower = mod.ToLowerInvariant();
				string type = "Системный DLL";
				Color col = Ui.TextSecondary;

				if (modLower.Contains("vape") || modLower.Contains("celestial") || modLower.Contains("expensive") ||
					modLower.Contains("rich") || modLower.Contains("drip") || modLower.Contains("koid") || modLower.Contains("slinky"))
				{
					type = "🚨 ЧИТ-МОДУЛЬ";
					col = Ui.AccentRed;
				}
				else if (modLower.Contains(".minecraft") || modLower.Contains("jvm.dll"))
				{
					type = "Minecraft / Java";
					col = Ui.AccentTeal;
				}
				else if (modLower.Contains(@"\appdata\local\temp") || modLower.Contains(@"\downloads\"))
				{
					type = "⚠️ Temp / Downloads";
					col = Ui.AccentOrange;
				}

				ListViewItem lvi = new ListViewItem(mod);
				lvi.SubItems.Add(type);
				lvi.ForeColor = col;
				listModules.Items.Add(lvi);
			}
			listModules.EndUpdate();
		}

		private void ClearDetailPane()
		{
			lblDetailExe.Text = "Процессы не найдены";
			lblDetailHash.Text = "Хеш: —";
			lblDetailRunCount.Text = "Счётчик запусков: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Правило: —";
			txtDetailDescription.Text = "";
			listLaunches.Items.Clear();
			listModules.Items.Clear();
		}

		private void CopyBanCommand()
		{
			PrefetchItem sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.IsCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Запуск чита по Prefetch: {1} в {2})",
					targetPlayerName, sel.ExeName, sel.LastExecution.ToString("dd.MM.yyyy HH:mm"));
			}
			else if (sel.IsCleaner)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Запуск клинера по Prefetch: {1} в {2})",
					targetPlayerName, sel.ExeName, sel.LastExecution.ToString("dd.MM.yyyy HH:mm"));
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подозрительный запуск по Prefetch: {1})",
					targetPlayerName, sel.ExeName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан по Prefetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CheckVirusTotal()
		{
			PrefetchItem sel = GetSelectedItem();
			if (sel == null) return;

			try
			{
				string query = !string.IsNullOrEmpty(sel.Hash) && sel.Hash != "—" ? sel.Hash : sel.ExeName;
				string url = "https://www.virustotal.com/gui/search/" + Uri.EscapeDataString(query);
				Process.Start(url);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия браузера: " + ex.Message, "VirusTotal", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
