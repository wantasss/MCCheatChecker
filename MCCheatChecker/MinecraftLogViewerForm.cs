using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class MinecraftLogViewerForm : Form
	{
		private enum FilterMode
		{
			All,
			Cheats,
			Anticheat,
			Chat,
			Mods
		}

		private class LogEntry
		{
			public int LineNumber;
			public string TimeThread;
			public string Message;
			public bool IsCheat;
			public bool IsAnticheat;
			public bool IsChat;
			public bool IsMod;
			public Color TextColor;
		}

		private static readonly Regex CheatCommandRegex = new Regex(
			@"(?i)(?:^|\[chat\]\s*|<[^>]+>\s*|\s)(?:\.|#|@|!)(\s*(?:bind|vape|goto|mine|tunnel|follow|path|baritone|toggle|t\s|set\s|config|friend|c\s|killaura|ka\b|velocity|aimbot|speed|esp|tracers|nofall|step|timer|freecam|reach|flight|fly|derp|autoarmor|fastplace|antiknockback|blink|jesus|scaffold|aura|criticals|wtap|target|macros|fakelag|hitboxes|longjump|antifire|inventorywalk|fastbreak|cheststealer|vclip|hclip|xray|bhop|autototem|triggerbot|panic|selfdestruct))",
			RegexOptions.Compiled);

		private static readonly Regex CheatBrandRegex = new Regex(
			@"(?i)(baritone|meteor-client|liquidbounce|wurstclient|doomsdayclient|celestial|expensive\s*client|rich\s*client|akrien|wild\s*client|thunderhack|inertia\s*client|aristois|future\s*client|rusherhack|konas|kami\s*blue|sigma\s*5|zero\s*day|impact\s*client|bape|vape\s*v4|minced|neverhook|exptv|xray\s*bruteforce|nuker|autotool|seedcracker|seedmapper|boberware|nursultan|weighbar)",
			RegexOptions.Compiled);

		private static readonly Regex AnticheatRegex = new Regex(
			@"(?i)(kicked|disconnect|grim\s*ac|grimac|vulcan|matrix\s*anticheat|polar|karhu|intave|cheat|flying\s*is\s*not\s*enabled|illegal\s*action|speed\s*limit|invalid\s*movement|timed\s*out|connection\s*lost|internal\s*exception)",
			RegexOptions.Compiled);

		private static readonly Regex ChatRegex = new Regex(
			@"(?i)(\[CHAT\]|chatmessage)",
			RegexOptions.Compiled);

		private static readonly Regex ModRegex = new Regex(
			@"(?i)(loading\s*mod|fabric\s*loader|forge\s*mod\s*loader|mixin|spongepowered|tweaker|optifine|sodium|iris|initializing\s*mod)",
			RegexOptions.Compiled);

		private static readonly Regex LogHeaderRegex = new Regex(
			@"^(\[[0-9]{2}:[0-9]{2}:[0-9]{2}\]\s*\[[^\]]+\]:?)\s*(.*)$",
			RegexOptions.Compiled);

		private string logFilePath;
		private long lastFileSize;
		private DateTime lastFileModified;
		private List<LogEntry> allEntries = new List<LogEntry>();
		private FilterMode currentFilter = FilterMode.All;
		private Timer tailTimer;

		private Label lblFileHeader;
		private DarkTextBox searchBox;
		private DarkListView logListView;
		private DarkCheckBox chkAutoTail;
		private Label lblStatsTotal;
		private Label lblStatsCheats;
		private Label lblStatsAc;
		private Label lblStatsChat;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterAc;
		private DarkButton btnFilterChat;
		private DarkButton btnFilterMods;

		public MinecraftLogViewerForm(string initialPath = null)
		{
			InitializeComponent();
			logFilePath = !string.IsNullOrEmpty(initialPath) && File.Exists(initialPath)
				? initialPath
				: AutoDetectLatestLog();

			if (!string.IsNullOrEmpty(logFilePath))
			{
				LoadLogFile(logFilePath);
			}
			else
			{
				lblFileHeader.Text = "⚠️  Файл latest.log не найден автоматически. Нажмите [📂 Выбрать другой лог...] для открытия.";
				lblFileHeader.ForeColor = Ui.AccentOrange;
			}
		}

		private static string AutoDetectLatestLog()
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			List<string> candidates = new List<string>
			{
				Path.Combine(appData, ".minecraft", "logs", "latest.log"),
				Path.Combine(userProf, ".feather", "player-logs", "latest.log"),
				Path.Combine(userProf, ".feather", "logs", "latest.log"),
				Path.Combine(userProf, ".lunarclient", "offline", "multiver", "logs", "latest.log"),
				Path.Combine(appData, ".tlauncher", "legacy", "Minecraft", "game", "logs", "latest.log")
			};

			try
			{
				string prismDir = Path.Combine(appData, "PrismLauncher", "instances");
				if (Directory.Exists(prismDir))
				{
					foreach (string inst in Directory.GetDirectories(prismDir))
					{
						string l = Path.Combine(inst, ".minecraft", "logs", "latest.log");
						if (File.Exists(l)) candidates.Add(l);
					}
				}
			}
			catch { }

			try
			{
				string cfDir = Path.Combine(userProf, "curseforge", "minecraft", "Instances");
				if (Directory.Exists(cfDir))
				{
					foreach (string inst in Directory.GetDirectories(cfDir))
					{
						string l = Path.Combine(inst, "logs", "latest.log");
						if (File.Exists(l)) candidates.Add(l);
					}
				}
			}
			catch { }

			string best = null;
			DateTime bestTime = DateTime.MinValue;
			foreach (string c in candidates)
			{
				if (File.Exists(c))
				{
					try
					{
						DateTime wt = File.GetLastWriteTimeUtc(c);
						if (wt > bestTime)
						{
							bestTime = wt;
							best = c;
						}
					}
					catch { }
				}
			}

			return best;
		}

		private void InitializeComponent()
		{
			Text = "📜 Монитор Minecraft логов — Аудит latest.log & Поиск чит-триггеров";
			Size = new Size(1140, 720);
			MinimumSize = new Size(840, 500);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			lblFileHeader = new Label();
			lblFileHeader.Text = "📁  Загрузка файла лога...";
			lblFileHeader.Font = Ui.FontBody(8.5f, true);
			lblFileHeader.ForeColor = Ui.AccentTeal;
			lblFileHeader.Location = new Point(12, 10);
			lblFileHeader.Size = new Size(460, 22);
			lblFileHeader.AutoEllipsis = true;
			topPanel.Controls.Add(lblFileHeader);

			DarkButton btnBrowse = new DarkButton();
			btnBrowse.Text = "📂 Выбрать лог...";
			btnBrowse.Location = new Point(480, 7);
			btnBrowse.Size = new Size(125, 26);
			btnBrowse.BackColor = Ui.SurfaceAlt;
			btnBrowse.ForeColor = Ui.TextPrimary;
			btnBrowse.Cursor = Cursors.Hand;
			btnBrowse.Click += delegate
			{
				using (OpenFileDialog ofd = new OpenFileDialog())
				{
					ofd.Title = "Выберите файл лога Minecraft";
					ofd.Filter = "Логи Minecraft (*.log;*.txt;*.gz)|*.log;*.txt;*.gz|Все файлы (*.*)|*.*";
					if (ofd.ShowDialog(this) == DialogResult.OK)
					{
						LoadLogFile(ofd.FileName);
					}
				}
			};
			topPanel.Controls.Add(btnBrowse);

			DarkButton btnScanArchives = new DarkButton();
			btnScanArchives.Text = "📦 Архивы (*.log.gz)...";
			btnScanArchives.Location = new Point(610, 7);
			btnScanArchives.Size = new Size(155, 26);
			btnScanArchives.BackColor = Ui.SurfaceAlt;
			btnScanArchives.ForeColor = Ui.AccentTeal;
			btnScanArchives.Font = Ui.FontBody(8.5f, true);
			btnScanArchives.Cursor = Cursors.Hand;
			btnScanArchives.Click += delegate
			{
				ScanArchiveLogs();
			};
			topPanel.Controls.Add(btnScanArchives);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Обновить";
			btnRefresh.Location = new Point(770, 7);
			btnRefresh.Size = new Size(95, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				if (!string.IsNullOrEmpty(logFilePath)) LoadLogFile(logFilePath);
			};
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnExplorer = new DarkButton();
			btnExplorer.Text = "📁 В проводнике";
			btnExplorer.Location = new Point(870, 7);
			btnExplorer.Size = new Size(115, 26);
			btnExplorer.BackColor = Ui.SurfaceAlt;
			btnExplorer.ForeColor = Ui.TextPrimary;
			btnExplorer.Cursor = Cursors.Hand;
			btnExplorer.Click += delegate
			{
				if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
				{
					PlatformHelper.SelectFileInFileManager(logFilePath);
				}
			};
			topPanel.Controls.Add(btnExplorer);

			chkAutoTail = new DarkCheckBox();
			chkAutoTail.Text = "⚡ Live (1.5с)";
			chkAutoTail.Location = new Point(992, 10);
			chkAutoTail.Size = new Size(130, 20);
			chkAutoTail.ForeColor = Ui.AccentGreen;
			chkAutoTail.Checked = true;
			topPanel.Controls.Add(chkAutoTail);

			Panel filterRow = new Panel();
			filterRow.Location = new Point(12, 40);
			filterRow.Size = new Size(1110, 62);
			filterRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все строки", FilterMode.All, 120);
			btnFilterCheats = CreateFilterButton("🚨 Чит-команды", FilterMode.Cheats, 140);
			btnFilterAc = CreateFilterButton("🛡️ Кики / Античит", FilterMode.Anticheat, 150);
			btnFilterChat = CreateFilterButton("💬 Чат игрока", FilterMode.Chat, 120);
			btnFilterMods = CreateFilterButton("🧩 Моды / Старт", FilterMode.Mods, 130);

			int curX = 0;
			DarkButton[] filterBtns = { btnFilterAll, btnFilterCheats, btnFilterAc, btnFilterChat, btnFilterMods };
			foreach (DarkButton b in filterBtns)
			{
				b.Location = new Point(curX, 0);
				filterRow.Controls.Add(b);
				curX += b.Width + 8;
			}

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(curX + 10, 5);
			lblSearch.AutoSize = true;
			filterRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(curX + 60, 2);
			searchBox.Size = new Size(240, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			filterRow.Controls.Add(searchBox);

			DarkButton btnClearSearch = new DarkButton();
			btnClearSearch.Text = "✕";
			btnClearSearch.Location = new Point(curX + 306, 2);
			btnClearSearch.Size = new Size(26, 22);
			btnClearSearch.BackColor = Ui.SurfaceAlt;
			btnClearSearch.ForeColor = Ui.TextMuted;
			btnClearSearch.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			filterRow.Controls.Add(btnClearSearch);

			topPanel.Controls.Add(filterRow);
			Controls.Add(topPanel);

			logListView = new DarkListView();
			logListView.Dock = DockStyle.Fill;
			logListView.View = View.Details;
			logListView.FullRowSelect = true;
			logListView.GridLines = false;
			logListView.Font = Ui.FontMono(8.5f);
			logListView.Columns.Add("№", 65, HorizontalAlignment.Right);
			logListView.Columns.Add("Время / Источник", 240, HorizontalAlignment.Left);
			logListView.Columns.Add("Содержимое строки лога", 810, HorizontalAlignment.Left);

			ContextMenuStrip ctxMenu = new ContextMenuStrip();
			ctxMenu.Renderer = new DarkMenuRenderer();
			ToolStripMenuItem copyLineItem = new ToolStripMenuItem("📋  Скопировать выбранную строку");
			copyLineItem.Click += delegate
			{
				if (logListView.SelectedItems.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (ListViewItem item in logListView.SelectedItems)
					{
						sb.AppendLine(item.SubItems[1].Text + " " + item.SubItems[2].Text);
					}
					Clipboard.SetText(sb.ToString().TrimEnd());
				}
			};
			ToolStripMenuItem copyAllSuspiciousItem = new ToolStripMenuItem("🚨  Скопировать все чит-триггеры и кики (отчёт)");
			copyAllSuspiciousItem.Click += delegate
			{
				CopySuspiciousReport();
			};
			ctxMenu.Items.Add(copyLineItem);
			ctxMenu.Items.Add(copyAllSuspiciousItem);
			logListView.ContextMenuStrip = ctxMenu;

			Controls.Add(logListView);
			logListView.BringToFront();

			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 44;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			lblStatsTotal = new Label() { Text = "Всего: 0", Location = new Point(14, 12), AutoSize = true, ForeColor = Ui.TextSecondary, Font = Ui.FontBody(8.5f, true) };
			lblStatsCheats = new Label() { Text = "🚨 Читы: 0", Location = new Point(130, 12), AutoSize = true, ForeColor = Ui.AccentRed, Font = Ui.FontBody(8.5f, true) };
			lblStatsAc = new Label() { Text = "🛡️ Античит: 0", Location = new Point(230, 12), AutoSize = true, ForeColor = Ui.AccentOrange, Font = Ui.FontBody(8.5f, true) };
			lblStatsChat = new Label() { Text = "💬 Чат: 0", Location = new Point(350, 12), AutoSize = true, ForeColor = Color.FromArgb(240, 220, 130), Font = Ui.FontBody(8.5f, true) };

			bottomPanel.Controls.Add(lblStatsTotal);
			bottomPanel.Controls.Add(lblStatsCheats);
			bottomPanel.Controls.Add(lblStatsAc);
			bottomPanel.Controls.Add(lblStatsChat);

			DarkButton btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Бан (/ban 5.01)";
			btnCopyBanCmd.Size = new Size(160, 28);
			btnCopyBanCmd.Location = new Point(460, 8);
			btnCopyBanCmd.BackColor = Ui.SurfaceAlt;
			btnCopyBanCmd.ForeColor = Ui.AccentRed;
			btnCopyBanCmd.Font = Ui.FontBody(8.5f, true);
			btnCopyBanCmd.Cursor = Cursors.Hand;
			btnCopyBanCmd.Click += delegate
			{
				CopyBanCommand();
			};
			bottomPanel.Controls.Add(btnCopyBanCmd);

			DarkButton btnCopySuspicious = new DarkButton();
			btnCopySuspicious.Text = "📋 Скопировать улики";
			btnCopySuspicious.Size = new Size(180, 28);
			btnCopySuspicious.Location = new Point(630, 8);
			btnCopySuspicious.BackColor = Color.FromArgb(180, 40, 50);
			btnCopySuspicious.ForeColor = Color.White;
			btnCopySuspicious.Font = Ui.FontBody(8.5f, true);
			btnCopySuspicious.Cursor = Cursors.Hand;
			btnCopySuspicious.Click += delegate
			{
				CopySuspiciousReport();
			};
			bottomPanel.Controls.Add(btnCopySuspicious);

			DarkButton btnCopyVisible = new DarkButton();
			btnCopyVisible.Text = "📋 Копировать видимые";
			btnCopyVisible.Size = new Size(180, 28);
			btnCopyVisible.Location = new Point(820, 8);
			btnCopyVisible.BackColor = Ui.SurfaceAlt;
			btnCopyVisible.ForeColor = Ui.TextPrimary;
			btnCopyVisible.Cursor = Cursors.Hand;
			btnCopyVisible.Click += delegate
			{
				StringBuilder sb = new StringBuilder();
				foreach (ListViewItem item in logListView.Items)
				{
					sb.AppendLine(item.SubItems[0].Text + "\t" + item.SubItems[1].Text + "\t" + item.SubItems[2].Text);
				}
				if (sb.Length > 0)
				{
					Clipboard.SetText(sb.ToString());
					MessageBox.Show("Видимые строки (" + logListView.Items.Count + " шт.) скопированы в буфер обмена!", "Монитор логов", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			bottomPanel.Controls.Add(btnCopyVisible);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(1010, 8);
			btnClose.BackColor = Ui.SurfaceAlt;
			btnClose.ForeColor = Ui.TextPrimary;
			btnClose.DialogResult = DialogResult.OK;
			bottomPanel.Controls.Add(btnClose);

			Controls.Add(bottomPanel);
			bottomPanel.BringToFront();

			UpdateFilterButtonStyles();

			tailTimer = new Timer();
			tailTimer.Interval = 1500;
			tailTimer.Tick += delegate
			{
				if (chkAutoTail.Checked && !string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
				{
					try
					{
						FileInfo fi = new FileInfo(logFilePath);
						if (fi.Length != lastFileSize || fi.LastWriteTimeUtc != lastFileModified)
						{
							LoadLogFile(logFilePath, true);
						}
					}
					catch { }
				}
			};
			tailTimer.Start();

			FormClosed += delegate
			{
				if (tailTimer != null)
				{
					tailTimer.Stop();
					tailTimer.Dispose();
				}
			};
		}

		private DarkButton CreateFilterButton(string text, FilterMode mode, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtonStyles();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterAc, btnFilterChat, btnFilterMods };
			FilterMode[] modes = { FilterMode.All, FilterMode.Cheats, FilterMode.Anticheat, FilterMode.Chat, FilterMode.Mods };

			for (int i = 0; i < btns.Length; i++)
			{
				if (currentFilter == modes[i])
				{
					btns[i].BackColor = Ui.AccentTeal;
					btns[i].ForeColor = Color.Black;
					btns[i].Font = Ui.FontBody(8.5f, true);
				}
				else
				{
					btns[i].BackColor = Ui.SurfaceAlt;
					btns[i].ForeColor = Ui.TextPrimary;
					btns[i].Font = Ui.FontBody(8.5f, false);
				}
			}
		}

		public void LoadLogFile(string path, bool isAutoRefresh = false)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

			try
			{
				logFilePath = path;
				FileInfo fi = new FileInfo(path);
				lastFileSize = fi.Length;
				lastFileModified = fi.LastWriteTimeUtc;

				double sizeKb = lastFileSize / 1024.0;
				lblFileHeader.Text = string.Format("📁  {0} ({1:F1} КБ, изм. {2:dd.MM.yyyy HH:mm:ss})",
					path, sizeKb, fi.LastWriteTime);
				lblFileHeader.ForeColor = Ui.AccentTeal;

				List<string> lines = new List<string>();
				if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
				{
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
					using (StreamReader sr = new StreamReader(gz, Encoding.UTF8))
					{
						string l;
						while ((l = sr.ReadLine()) != null) lines.Add(l);
					}
				}
				else
				{
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						string l;
						while ((l = sr.ReadLine()) != null) lines.Add(l);
					}
				}

				allEntries.Clear();
				int lineNum = 1;
				int cheatCount = 0;
				int acCount = 0;
				int chatCount = 0;

				foreach (string rawLine in lines)
				{
					if (string.IsNullOrWhiteSpace(rawLine))
					{
						lineNum++;
						continue;
					}

					string timeThread = "";
					string msg = rawLine;

					Match m = LogHeaderRegex.Match(rawLine);
					if (m.Success)
					{
						timeThread = m.Groups[1].Value.Trim();
						msg = m.Groups[2].Value.Trim();
					}

					bool isCheat = CheatCommandRegex.IsMatch(msg) || CheatBrandRegex.IsMatch(msg) || CheatCommandRegex.IsMatch(rawLine) || CheatBrandRegex.IsMatch(rawLine);
					bool isAc = AnticheatRegex.IsMatch(msg);
					bool isChat = ChatRegex.IsMatch(rawLine);
					bool isMod = ModRegex.IsMatch(rawLine);

					Color color = Ui.TextPrimary;
					if (isCheat)
					{
						color = Ui.AccentRed;
						cheatCount++;
					}
					else if (isAc)
					{
						color = Ui.AccentOrange;
						acCount++;
					}
					else if (isChat)
					{
						color = Color.FromArgb(240, 220, 130);
						chatCount++;
					}
					else if (isMod)
					{
						color = Ui.AccentTeal;
					}

					LogEntry entry = new LogEntry
					{
						LineNumber = lineNum,
						TimeThread = string.IsNullOrEmpty(timeThread) ? "—" : timeThread,
						Message = msg,
						IsCheat = isCheat,
						IsAnticheat = isAc,
						IsChat = isChat,
						IsMod = isMod,
						TextColor = color
					};

					allEntries.Add(entry);
					lineNum++;
				}

				lblStatsTotal.Text = "Всего: " + allEntries.Count.ToString("N0");
				lblStatsCheats.Text = "🚨 Читы: " + cheatCount.ToString("N0");
				lblStatsAc.Text = "🛡️ Античит: " + acCount.ToString("N0");
				lblStatsChat.Text = "💬 Чат: " + chatCount.ToString("N0");

				ApplyFilterAndSearch(isAutoRefresh);
			}
			catch (Exception ex)
			{
				lblFileHeader.Text = "Ошибка чтения лога: " + ex.Message;
				lblFileHeader.ForeColor = Ui.AccentRed;
			}
		}

		private void ApplyFilterAndSearch(bool keepPosition = false)
		{
			logListView.BeginUpdate();
			try
			{
				int topIndex = (keepPosition && logListView.TopItem != null) ? logListView.TopItem.Index : -1;

				logListView.Items.Clear();
				string query = searchBox.Text.Trim();

				List<ListViewItem> listItems = new List<ListViewItem>();

				foreach (LogEntry e in allEntries)
				{
					if (currentFilter == FilterMode.Cheats && !e.IsCheat) continue;
					if (currentFilter == FilterMode.Anticheat && !e.IsAnticheat) continue;
					if (currentFilter == FilterMode.Chat && !e.IsChat) continue;
					if (currentFilter == FilterMode.Mods && !e.IsMod) continue;

					if (!string.IsNullOrEmpty(query))
					{
						if (e.Message.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							e.TimeThread.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							e.LineNumber.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}

					ListViewItem item = new ListViewItem(e.LineNumber.ToString());
					item.SubItems.Add(e.TimeThread);
					item.SubItems.Add(e.Message);
					item.ForeColor = e.TextColor;

					listItems.Add(item);
				}

				logListView.Items.AddRange(listItems.ToArray());

				if (keepPosition && topIndex >= 0 && topIndex < logListView.Items.Count)
				{
					logListView.TopItem = logListView.Items[topIndex];
				}
				else if (!keepPosition && listItems.Count > 0)
				{
					// Scroll to bottom if new log was loaded
					logListView.EnsureVisible(listItems.Count - 1);
				}
			}
			finally
			{
				logListView.EndUpdate();
			}
		}

		private void CopySuspiciousReport()
		{
			var suspicious = allEntries.Where(e => e.IsCheat || e.IsAnticheat).ToList();
			if (suspicious.Count == 0)
			{
				MessageBox.Show("В текущем логе не найдено подозрительных команд читов или флагов античита.", "Монитор логов", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ MINECRAFT ЛОГА — ПОДОЗРИТЕЛЬНЫЕ ЗАПИСИ ===");
			sb.AppendLine("Файл: " + logFilePath);
			sb.AppendLine("Время формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего подозрительных строк: " + suspicious.Count);
			sb.AppendLine(new string('-', 70));

			foreach (var s in suspicious)
			{
				string tag = s.IsCheat ? "[🚨 ЧИТ-ТРИГГЕР]" : "[🛡️ АНТИЧИТ/КИК]";
				sb.AppendLine(string.Format("Стр {0,5} | {1} {2} -> {3}", s.LineNumber, tag, s.TimeThread, s.Message));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Найдено " + suspicious.Count + " подозрительных строк! Отчёт скопирован в буфер обмена.", "MC Cheat Checker", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			var cheats = allEntries.Where(e => e.IsCheat).ToList();
			string detail = cheats.Count > 0 ? cheats[0].Message : "Чит-команды в логах игры";
			if (detail.Length > 40) detail = detail.Substring(0, 40);
			string banCmd = "/ban <player> 30d 5.01 Использование читов (" + detail.Trim() + ")";
			try { Clipboard.SetText(banCmd); } catch { }
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + banCmd, "Регламент 5.01", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ScanArchiveLogs()
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			List<string> logDirs = new List<string>
			{
				Path.Combine(appData, ".minecraft", "logs"),
				Path.Combine(userProf, ".feather", "logs"),
				Path.Combine(userProf, ".feather", "user-gamedir", "logs"),
				Path.Combine(userProf, ".lunarclient", "offline", "multiver", "logs"),
				Path.Combine(appData, ".tlauncher", "legacy", "Minecraft", "game", "logs"),
				Path.Combine(appData, ".tlauncher", "minecraft", "logs")
			};

			try
			{
				string prism = Path.Combine(appData, "PrismLauncher", "instances");
				if (Directory.Exists(prism))
				{
					foreach (string d in Directory.GetDirectories(prism))
					{
						string l = Path.Combine(d, ".minecraft", "logs");
						if (Directory.Exists(l)) logDirs.Add(l);
					}
				}
			} catch { }

			try
			{
				string cf = Path.Combine(userProf, "curseforge", "minecraft", "Instances");
				if (Directory.Exists(cf))
				{
					foreach (string d in Directory.GetDirectories(cf))
					{
						string l = Path.Combine(d, "logs");
						if (Directory.Exists(l)) logDirs.Add(l);
					}
				}
			} catch { }

			List<FileInfo> gzFiles = new List<FileInfo>();
			foreach (string dir in logDirs)
			{
				try
				{
					if (Directory.Exists(dir))
					{
						foreach (string f in Directory.GetFiles(dir, "*.log.gz"))
						{
							gzFiles.Add(new FileInfo(f));
						}
					}
				} catch { }
			}

			if (gzFiles.Count == 0)
			{
				MessageBox.Show("Архивные файлы логов (*.log.gz) не найдены в директориях лаунчеров.", "Аудит архивов логов", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			gzFiles.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
			int maxToScan = Math.Min(gzFiles.Count, 30);

			List<LogEntry> archiveCheats = new List<LogEntry>();
			int scannedCount = 0;

			foreach (FileInfo fi in gzFiles)
			{
				if (scannedCount >= maxToScan) break;
				scannedCount++;

				try
				{
					using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
					using (StreamReader sr = new StreamReader(gz, Encoding.UTF8))
					{
						int lNum = 0;
						string line;
						while ((line = sr.ReadLine()) != null && lNum < 5000)
						{
							lNum++;
							bool isCheat = CheatCommandRegex.IsMatch(line) || CheatBrandRegex.IsMatch(line);
							if (isCheat)
							{
								LogEntry ent = new LogEntry
								{
									LineNumber = lNum,
									TimeThread = "[" + fi.Name + " : " + lNum + "]",
									Message = line.Trim(),
									IsCheat = true,
									TextColor = Ui.AccentRed
								};
								archiveCheats.Add(ent);
								if (archiveCheats.Count >= 100) break;
							}
						}
					}
				}
				catch { }
			}

			if (archiveCheats.Count > 0)
			{
				allEntries.Clear();
				allEntries.AddRange(archiveCheats);
				lblFileHeader.Text = "📦  Результаты аудита архивов (*.log.gz) — обнаружено чит-команд: " + archiveCheats.Count;
				lblFileHeader.ForeColor = Ui.AccentRed;
				currentFilter = FilterMode.Cheats;
				UpdateFilterButtonStyles();
				ApplyFilterAndSearch();
				MessageBox.Show("🚨 В архивных логах найдено " + archiveCheats.Count + " следов чит-команд!\n\nОни загружены в список для детального изучения.", "Аудит архивов логов (Регламент 5.01)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				MessageBox.Show("✅ Проверено архивов: " + scannedCount + " шт. (*.log.gz).\nСледов чит-команд и сокрытия улик не обнаружено!", "Аудит архивов логов", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	}
}
