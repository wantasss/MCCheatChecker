using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class FastCheatHunterForm : Form
	{
		private enum HunterFilter
		{
			All,
			CheatsOnly,
			CleanersOnly,
			SizeMatchesOnly,
			MinecraftModsOnly,
			Fresh24HoursOnly,
			WhitelistOnly
		}

		private class CheatFileHit
		{
			public int Index;
			public string FileName;
			public string DirectoryPath;
			public string FullPath;
			public long FileSizeBytes;
			public DateTime LastModified;
			public DateTime Created;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsSizeMatch;
			public bool IsMinecraftMod;
			public bool IsWhitelisted;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private HunterFilter currentFilter = HunterFilter.All;
		private List<CheatFileHit> allHits = new List<CheatFileHit>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailFile;
		private Label lblDetailPath;
		private Label lblDetailSize;
		private Label lblDetailDates;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkCheckBox chkScopeMc;
		private DarkCheckBox chkScopeDownloads;
		private DarkCheckBox chkScopeTemp;
		private DarkCheckBox chkScopeDesktop;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterSizes;
		private DarkButton btnFilterMods;
		private DarkButton btnFilterFresh;
		private DarkButton btnFilterWhitelist;

		private static readonly Regex CheatKeywordsPattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject|boze|abyss|killaura|velocity|aimbot|autoclicker|triggerbot|reghax)",
			RegexOptions.Compiled);

		public FastCheatHunterForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartSearchAsync();
		}

		private void InitializeComponent()
		{
			Text = "🔍 Быстрый охотник за файлами читов и модов (Fast Cheat & Mod Hunter)";
			Size = new Size(1220, 780);
			MinimumSize = new Size(960, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 118;
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
			lblHeaderTitle.Text = "🔍  БЫСТРЫЙ ОХОТНИК ЗА ФАЙЛАМИ ЧИТОВ (Fast Multi-threaded Search & Everything)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Многопоточный поиск по 350+ именам софтов, 140+ точным размерам JAR, батникам клинеров и свежим файлам...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Поиск файлов...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnEverythingMenu = new DarkButton();
			btnEverythingMenu.Text = "⚡ Меню Everything ▾";
			btnEverythingMenu.Location = new Point(870, 8);
			btnEverythingMenu.Size = new Size(160, 26);
			btnEverythingMenu.BackColor = Color.FromArgb(74, 20, 140);
			btnEverythingMenu.ForeColor = Color.White;
			btnEverythingMenu.Font = Ui.FontBody(8.5f, true);
			btnEverythingMenu.Cursor = Cursors.Hand;
			btnEverythingMenu.Click += delegate { ShowEverythingMenu(btnEverythingMenu); };
			topPanel.Controls.Add(btnEverythingMenu);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(1036, 8);
			btnRefresh.Size = new Size(145, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { StartSearchAsync(); };
			topPanel.Controls.Add(btnRefresh);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все (0)", HunterFilter.All, 100);
			btnFilterCheats = CreateFilterButton("🚨 Читы & Инжекторы (0)", HunterFilter.CheatsOnly, 185);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры 5.08 (0)", HunterFilter.CleanersOnly, 140);
			btnFilterSizes = CreateFilterButton("🎯 Размер чита (0)", HunterFilter.SizeMatchesOnly, 140);
			btnFilterMods = CreateFilterButton("☕ Моды .jar (0)", HunterFilter.MinecraftModsOnly, 125);
			btnFilterFresh = CreateFilterButton("⏱️ Свежие (24ч) (0)", HunterFilter.Fresh24HoursOnly, 140);
			btnFilterWhitelist = CreateFilterButton("✔ Whitelist (0)", HunterFilter.WhitelistOnly, 120);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterSizes, btnFilterMods, btnFilterFresh, btnFilterWhitelist };
			foreach (DarkButton b in filterButtons)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 6;
			}
			topPanel.Controls.Add(navRow);

			Panel searchRow = new Panel();
			searchRow.Location = new Point(12, 85);
			searchRow.Size = new Size(1180, 26);
			searchRow.BackColor = Color.Transparent;

			Label lblSearch = new Label();
			lblSearch.Text = "Фильтр:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(0, 4);
			lblSearch.AutoSize = true;
			searchRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(60, 1);
			searchBox.Size = new Size(300, 23);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			searchRow.Controls.Add(searchBox);

			DarkButton btnClearSearch = new DarkButton();
			btnClearSearch.Text = "✕ Очистить";
			btnClearSearch.Location = new Point(366, 1);
			btnClearSearch.Size = new Size(80, 23);
			btnClearSearch.BackColor = Ui.SurfaceAlt;
			btnClearSearch.Font = Ui.FontBody(8f);
			btnClearSearch.Cursor = Cursors.Hand;
			btnClearSearch.Click += delegate { searchBox.Text = ""; };
			searchRow.Controls.Add(btnClearSearch);

			chkScopeMc = CreateScopeCheckbox(".minecraft", true, 460, searchRow);
			chkScopeDownloads = CreateScopeCheckbox("Downloads", true, 570, searchRow);
			chkScopeTemp = CreateScopeCheckbox("Temp", true, 675, searchRow);
			chkScopeDesktop = CreateScopeCheckbox("Desktop", true, 750, searchRow);

			Label lblHint = new Label();
			lblHint.Text = "💡 Кликните дважды по строке для копирования /ban. Нажмите «Меню Everything» для глобального мгновенного MFT-поиска.";
			lblHint.Font = Ui.FontBody(8f);
			lblHint.ForeColor = Ui.TextMuted;
			lblHint.Location = new Point(835, 5);
			lblHint.AutoSize = true;
			searchRow.Controls.Add(lblHint);

			topPanel.Controls.Add(searchRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail pane
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1180, 600);
			try { split.SplitterDistance = 420; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 210);
			listView.Columns.Add("Имя файла", 230);
			listView.Columns.Add("Размер", 100);
			listView.Columns.Add("Дата изменения", 155);
			listView.Columns.Add("Каталог", 180);
			listView.Columns.Add("Полный путь", 380);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details & Actions
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 10, 12, 10);

			lblDetailFile = new Label();
			lblDetailFile.Text = "Выберите найденный файл для анализа...";
			lblDetailFile.Font = Ui.FontTitle(10f, true);
			lblDetailFile.ForeColor = Ui.TextPrimary;
			lblDetailFile.Location = new Point(12, 8);
			lblDetailFile.AutoSize = true;
			detailContainer.Controls.Add(lblDetailFile);

			lblDetailPath = new Label();
			lblDetailPath.Text = "Полный путь: —";
			lblDetailPath.Font = Ui.FontBody(8.5f);
			lblDetailPath.ForeColor = Ui.TextSecondary;
			lblDetailPath.Location = new Point(12, 30);
			lblDetailPath.AutoSize = true;
			detailContainer.Controls.Add(lblDetailPath);

			lblDetailSize = new Label();
			lblDetailSize.Text = "Размер файла: —";
			lblDetailSize.Font = Ui.FontBody(8.5f);
			lblDetailSize.ForeColor = Ui.TextSecondary;
			lblDetailSize.Location = new Point(12, 50);
			lblDetailSize.AutoSize = true;
			detailContainer.Controls.Add(lblDetailSize);

			lblDetailDates = new Label();
			lblDetailDates.Text = "Даты: —";
			lblDetailDates.Font = Ui.FontBody(8.5f);
			lblDetailDates.ForeColor = Ui.TextSecondary;
			lblDetailDates.Location = new Point(12, 70);
			lblDetailDates.AutoSize = true;
			detailContainer.Controls.Add(lblDetailDates);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Вердикт: —";
			lblDetailStatus.Font = Ui.FontBody(8.5f, true);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Location = new Point(12, 90);
			lblDetailStatus.AutoSize = true;
			detailContainer.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Пункт регламента: —";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.AccentTeal;
			lblDetailRule.Location = new Point(12, 110);
			lblDetailRule.AutoSize = true;
			detailContainer.Controls.Add(lblDetailRule);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Location = new Point(12, 134);
			txtDetailDescription.Size = new Size(820, 75);
			txtDetailDescription.Font = Ui.FontMono(8f);
			txtDetailDescription.ReadOnly = true;
			detailContainer.Controls.Add(txtDetailDescription);

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать /ban";
			btnCopyBan.Location = new Point(850, 134);
			btnCopyBan.Size = new Size(160, 27);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			detailContainer.Controls.Add(btnCopyBan);

			DarkButton btnOpenJar = new DarkButton();
			btnOpenJar.Text = "📦 Инспектор JAR";
			btnOpenJar.Location = new Point(850, 165);
			btnOpenJar.Size = new Size(160, 27);
			btnOpenJar.BackColor = Color.FromArgb(94, 53, 177);
			btnOpenJar.ForeColor = Color.White;
			btnOpenJar.Font = Ui.FontBody(8.5f, true);
			btnOpenJar.Cursor = Cursors.Hand;
			btnOpenJar.Click += delegate { OpenInJarInspector(); };
			detailContainer.Controls.Add(btnOpenJar);

			DarkButton btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Показать файл";
			btnOpenFolder.Location = new Point(1020, 134);
			btnOpenFolder.Size = new Size(160, 27);
			btnOpenFolder.BackColor = Ui.SurfaceAlt;
			btnOpenFolder.Font = Ui.FontBody(8.5f, true);
			btnOpenFolder.Cursor = Cursors.Hand;
			btnOpenFolder.Click += delegate { OpenFileLocation(); };
			detailContainer.Controls.Add(btnOpenFolder);

			DarkButton btnVt = new DarkButton();
			btnVt.Text = "🔍 VirusTotal";
			btnVt.Location = new Point(1020, 165);
			btnVt.Size = new Size(160, 27);
			btnVt.BackColor = Ui.SurfaceAlt;
			btnVt.Font = Ui.FontBody(8.5f, true);
			btnVt.Cursor = Cursors.Hand;
			btnVt.Click += delegate
			{
				CheatFileHit sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FileName))
				{
					try { Process.Start("https://www.virustotal.com/gui/search/" + Uri.EscapeDataString(sel.FileName)); } catch { }
				}
			};
			detailContainer.Controls.Add(btnVt);

			detailContainer.Resize += delegate
			{
				int w = detailContainer.ClientSize.Width - 24;
				if (w > 600)
				{
					int descW = Math.Max(300, w - 360);
					txtDetailDescription.Width = descW;
					btnCopyBan.Location = new Point(descW + 24, 134);
					btnOpenJar.Location = new Point(descW + 24, 165);
					btnOpenFolder.Location = new Point(descW + 190, 134);
					btnVt.Location = new Point(descW + 190, 165);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);
		}

		private DarkCheckBox CreateScopeCheckbox(string text, bool check, int x, Panel parent)
		{
			DarkCheckBox cb = new DarkCheckBox();
			cb.Text = text;
			cb.Checked = check;
			cb.Location = new Point(x, 4);
			cb.Size = new Size(95, 20);
			cb.Font = Ui.FontBody(8f);
			cb.CheckedChanged += delegate { StartSearchAsync(); };
			parent.Controls.Add(cb);
			return cb;
		}

		private DarkButton CreateFilterButton(string text, HunterFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
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
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterSizes, btnFilterMods, btnFilterFresh, btnFilterWhitelist };
			HunterFilter[] filters = { HunterFilter.All, HunterFilter.CheatsOnly, HunterFilter.CleanersOnly, HunterFilter.SizeMatchesOnly, HunterFilter.MinecraftModsOnly, HunterFilter.Fresh24HoursOnly, HunterFilter.WhitelistOnly };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void StartSearchAsync()
		{
			lblStatusPill.Text = "⏳ Поиск файлов читов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allHits.Clear();
			listView.Items.Clear();

			bool scMc = chkScopeMc.Checked;
			bool scDl = chkScopeDownloads.Checked;
			bool scTmp = chkScopeTemp.Checked;
			bool scDt = chkScopeDesktop.Checked;

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<CheatFileHit> hits = PerformDeepFileHunt(scMc, scDl, scTmp, scDt);
				e.Result = hits;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<CheatFileHit>)
				{
					allHits = (List<CheatFileHit>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static List<CheatFileHit> PerformDeepFileHunt(bool scMc, bool scDl, bool scTmp, bool scDt)
		{
			List<CheatFileHit> results = new List<CheatFileHit>();
			HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int idx = 1;

			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			List<string> targetRoots = new List<string>();
			if (scMc)
			{
				string mc = Path.Combine(appData, ".minecraft");
				if (Directory.Exists(mc)) targetRoots.Add(mc);
			}
			if (scDl)
			{
				string dl = Path.Combine(userProf, "Downloads");
				if (Directory.Exists(dl)) targetRoots.Add(dl);
			}
			if (scTmp)
			{
				string tmp = Path.GetTempPath();
				if (Directory.Exists(tmp)) targetRoots.Add(tmp);
			}
			if (scDt)
			{
				string dt = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				if (Directory.Exists(dt)) targetRoots.Add(dt);
			}

			foreach (string root in targetRoots)
			{
				try
				{
					ScanDirectoryRecursive(root, results, seenPaths, ref idx, 0);
				}
				catch { }
			}

			// Sort hits: Cheats first, then cleaners, then sizes, then newest
			results.Sort(delegate(CheatFileHit a, CheatFileHit b)
			{
				if (a.IsCheat && !b.IsCheat) return -1;
				if (!a.IsCheat && b.IsCheat) return 1;
				if (a.IsCleaner && !b.IsCleaner) return -1;
				if (!a.IsCleaner && b.IsCleaner) return 1;
				if (a.IsSizeMatch && !b.IsSizeMatch) return -1;
				if (!a.IsSizeMatch && b.IsSizeMatch) return 1;
				return b.LastModified.CompareTo(a.LastModified);
			});

			return results;
		}

		private static void ScanDirectoryRecursive(string dir, List<CheatFileHit> results, HashSet<string> seenPaths, ref int idx, int depth)
		{
			if (depth > 5 || results.Count > 10000) return;

			try
			{
				string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
				foreach (string fp in files)
				{
					if (seenPaths.Contains(fp)) continue;
					string fn = Path.GetFileName(fp);
					string ext = Path.GetExtension(fp).ToLowerInvariant();

					// Only inspect executable, archive, mod, script, or known data files
					if (ext != ".jar" && ext != ".exe" && ext != ".dll" && ext != ".bat" && ext != ".cmd" &&
						ext != ".ps1" && ext != ".vbs" && ext != ".zip" && ext != ".rar" && ext != ".7z" &&
						ext != ".ahk" && ext != ".litematic" && ext != ".json" && ext != ".txt")
					{
						continue;
					}

					try
					{
						FileInfo fi = new FileInfo(fp);
						long size = fi.Length;
						string fnLower = fn.ToLowerInvariant();

						bool isCheat = false;
						bool isCleaner = false;
						bool isSizeMatch = false;
						bool isMcMod = ext == ".jar";
						string threatReason = "";
						string verdict = "";
						Color col = Ui.TextPrimary;

						// 1. Whitelist Clickers Check
						bool isWhite = false;
						foreach (string w in CheatDatabases.WhitelistProcessNames)
						{
							if (fnLower.Contains(w.ToLowerInvariant())) { isWhite = true; break; }
						}
						if (fnLower.Contains("anydesk") || fnLower.Contains("rudesktop") || fnLower.Contains("autohotkey") ||
							fnLower.Contains("opautoclicker") || fnLower.Contains("speedautoclicker") || fnLower.Contains("tinytask") ||
							fnLower.Contains("clickermann") || fnLower.Contains("keyran") || fnLower.Contains("x-mouse"))
						{
							isWhite = true;
						}

						if (isWhite)
						{
							seenPaths.Add(fp);
							CheatFileHit hit = new CheatFileHit();
							hit.Index = idx++;
							hit.FileName = fn;
							hit.DirectoryPath = dir;
							hit.FullPath = fp;
							hit.FileSizeBytes = size;
							hit.LastModified = fi.LastWriteTime;
							hit.Created = fi.CreationTime;
							hit.IsWhitelisted = true;
							hit.Verdict = "✔ Whitelist (5.01)";
							hit.ThreatReason = "Разрешённое ПО / Автокликер / Утилита ScreenShare (белый список)";
							hit.ForeColor = Ui.AccentGreen;
							results.Add(hit);
							continue;
						}

						// 2. Size Match Check (140+ unique leaked cheat JAR sizes)
						if (ext == ".jar" && CheatDatabases.KnownCheatJarSizes.Contains(size))
						{
							isSizeMatch = true;
							isCheat = true;
							verdict = "🚨 Совпадение размера чита (5.01)";
							threatReason = string.Format("Точный размер файла ({0} байт) совпадает с базой утёкших чит-клиентов!", size);
							col = Ui.AccentRed;
						}

						// 3. Cleaners Check (Rule 5.08)
						if (!isCheat)
						{
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && fnLower.Contains(cl)) { isCleaner = true; break; }
							}
							if (isCleaner || (ext == ".bat" || ext == ".cmd" || ext == ".ps1") && (fnLower.Contains("clean") || fnLower.Contains("wipe") || fnLower.Contains("sdelete") || fnLower.Contains("journaltrace")))
							{
								isCleaner = true;
								verdict = "🚨 Клинер / Батник сокрытия (5.08)";
								threatReason = "Обнаружена утилита очистки логов, реестра, USN журнала или Prefetch (Пункт 5.08)";
								col = Ui.AccentRed;
							}
						}

						// 4. Cheats & Injectors Check (Rule 5.01)
						if (!isCheat && !isCleaner)
						{
							if (CheatKeywordsPattern.IsMatch(fnLower))
							{
								isCheat = true;
							}
							if (!isCheat)
							{
								foreach (string ec in CheatDatabases.ExtraCheatClients)
								{
									if (ec.Length >= 4 && fnLower.Contains(ec.ToLowerInvariant())) { isCheat = true; break; }
								}
							}
							if (!isCheat)
							{
								foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
								{
									if (inj.Length >= 4 && fnLower.Contains(inj.ToLowerInvariant())) { isCheat = true; break; }
								}
							}

							if (isCheat)
							{
								verdict = "🚨 Чит-программа / Инжектор (5.01)";
								threatReason = "Обнаружен файл запрещённого чит-клиента или инжектора (Пункт 5.01 регламента)";
								col = Ui.AccentRed;
							}
						}

						// If match found or is a fresh/suspicious mod
						if (isCheat || isCleaner || isSizeMatch || (isMcMod && fi.LastWriteTime >= DateTime.Now.AddHours(-24)))
						{
							seenPaths.Add(fp);
							CheatFileHit hit = new CheatFileHit();
							hit.Index = idx++;
							hit.FileName = fn;
							hit.DirectoryPath = dir;
							hit.FullPath = fp;
							hit.FileSizeBytes = size;
							hit.LastModified = fi.LastWriteTime;
							hit.Created = fi.CreationTime;
							hit.IsCheat = isCheat;
							hit.IsCleaner = isCleaner;
							hit.IsSizeMatch = isSizeMatch;
							hit.IsMinecraftMod = isMcMod;
							hit.ThreatReason = threatReason;

							if (string.IsNullOrEmpty(verdict))
							{
								if (isMcMod)
								{
									hit.Verdict = "☕ Свежий мод .jar (24ч)";
									hit.ThreatReason = "Модификация игры, изменённая за последние 24 часа";
									hit.ForeColor = Ui.AccentTeal;
								}
								else
								{
									hit.Verdict = "Штатный файл";
									hit.ThreatReason = "Пользовательский файл в системе";
									hit.ForeColor = Ui.TextPrimary;
								}
							}
							else
							{
								hit.Verdict = verdict;
								hit.ForeColor = col;
							}

							results.Add(hit);
						}
					}
					catch { }
				}

				// Recurse subdirectories
				string[] subDirs = Directory.GetDirectories(dir);
				foreach (string sd in subDirs)
				{
					string sdn = Path.GetFileName(sd);
					if (sdn.StartsWith("$") || sdn.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || sdn.Equals(".git", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					ScanDirectoryRecursive(sd, results, seenPaths, ref idx, depth + 1);
				}
			}
			catch { }
		}

		private void UpdateSummaryStats()
		{
			int total = allHits.Count;
			int cheats = allHits.Count(h => h.IsCheat);
			int cleaners = allHits.Count(h => h.IsCleaner);
			int sizes = allHits.Count(h => h.IsSizeMatch);
			int mods = allHits.Count(h => h.IsMinecraftMod);
			DateTime past24 = DateTime.Now.AddHours(-24);
			int fresh = allHits.Count(h => h.LastModified >= past24);
			int white = allHits.Count(h => h.IsWhitelisted);

			btnFilterAll.Text = string.Format("🔍 Все ({0})", total);
			btnFilterCheats.Text = string.Format("🚨 Читы ({0})", cheats);
			btnFilterCleaners.Text = string.Format("🧹 Клинеры ({0})", cleaners);
			btnFilterSizes.Text = string.Format("🎯 Размер ({0})", sizes);
			btnFilterMods.Text = string.Format("☕ Моды ({0})", mods);
			btnFilterFresh.Text = string.Format("⏱️ Свежие ({0})", fresh);
			btnFilterWhitelist.Text = string.Format("✔ Whitelist ({0})", white);

			if (cheats > 0 || cleaners > 0)
			{
				lblStatusPill.Text = string.Format("🚨 НАЙДЕНО УЛИК: Читов {0}, Клинеров {1}!", cheats, cleaners);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔ Найдено {0} файлов. Явных читов не обнаружено.", total);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();
			DateTime past24 = DateTime.Now.AddHours(-24);

			foreach (CheatFileHit hit in allHits)
			{
				switch (currentFilter)
				{
					case HunterFilter.CheatsOnly:
						if (!hit.IsCheat) continue;
						break;
					case HunterFilter.CleanersOnly:
						if (!hit.IsCleaner) continue;
						break;
					case HunterFilter.SizeMatchesOnly:
						if (!hit.IsSizeMatch) continue;
						break;
					case HunterFilter.MinecraftModsOnly:
						if (!hit.IsMinecraftMod) continue;
						break;
					case HunterFilter.Fresh24HoursOnly:
						if (hit.LastModified < past24) continue;
						break;
					case HunterFilter.WhitelistOnly:
						if (!hit.IsWhitelisted) continue;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = hit.FileName.ToLowerInvariant().Contains(query) ||
								 hit.FullPath.ToLowerInvariant().Contains(query) ||
								 hit.Verdict.ToLowerInvariant().Contains(query);
					if (!match) continue;
				}

				string sizeStr = hit.FileSizeBytes < 1024 * 1024 ?
					string.Format("{0:F1} КБ", hit.FileSizeBytes / 1024.0) :
					string.Format("{0:F2} МБ", hit.FileSizeBytes / (1024.0 * 1024.0));

				ListViewItem lvi = new ListViewItem(hit.Verdict);
				lvi.SubItems.Add(hit.FileName);
				lvi.SubItems.Add(sizeStr);
				lvi.SubItems.Add(hit.LastModified.ToString("dd.MM.yyyy HH:mm:ss"));
				lvi.SubItems.Add(Path.GetFileName(hit.DirectoryPath));
				lvi.SubItems.Add(hit.FullPath);

				lvi.ForeColor = hit.ForeColor;
				lvi.Tag = hit;
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

		private CheatFileHit GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as CheatFileHit;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			CheatFileHit sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailFile.Text = sel.FileName;
			lblDetailPath.Text = "Полный путь: " + sel.FullPath;
			lblDetailSize.Text = string.Format("Точный размер: {0:N0} байт ({1})", sel.FileSizeBytes,
				sel.FileSizeBytes < 1024 * 1024 ? string.Format("{0:F1} КБ", sel.FileSizeBytes / 1024.0) : string.Format("{0:F2} МБ", sel.FileSizeBytes / (1024.0 * 1024.0)));
			lblDetailDates.Text = string.Format("Изменён: {0:dd.MM.yyyy HH:mm:ss} | Создан: {1:dd.MM.yyyy HH:mm:ss}", sel.LastModified, sel.Created);
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;

			if (sel.IsCheat || sel.IsSizeMatch) lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-программы / Стороннее ПО) — Бан 30 дней";
			else if (sel.IsCleaner) lblDetailRule.Text = "Пункт регламента: 5.08 (Удаление логов / Сокрытие улик) — Бан 30 дней";
			else if (sel.IsWhitelisted) lblDetailRule.Text = "Пункт регламента: 5.01 Белый список (Разрешено)";
			else lblDetailRule.Text = "Пункт регламента: Допустимый файл";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Имя файла: " + sel.FileName);
			sb.AppendLine("Расположение: " + sel.DirectoryPath);
			sb.AppendLine("Обоснование: " + sel.ThreatReason);
			if (sel.IsSizeMatch)
			{
				sb.AppendLine("🎯 ВНИМАНИЕ: Размер в байтах строго совпадает со слитым читом из базы 140+!");
			}
			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDetailPane()
		{
			lblDetailFile.Text = "Файлы не выбраны";
			lblDetailPath.Text = "Полный путь: —";
			lblDetailSize.Text = "Размер файла: —";
			lblDetailDates.Text = "Даты: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Пункт регламента: —";
			txtDetailDescription.Text = "";
		}

		private void CopyBanCommand()
		{
			CheatFileHit sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.IsSizeMatch)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Чит-клиент по размеру {1} байт: {2})",
					targetPlayerName, sel.FileSizeBytes, sel.FileName);
			}
			else if (sel.IsCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Найден файл чита: {1})",
					targetPlayerName, sel.FileName);
			}
			else if (sel.IsCleaner)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Найден клинер сокрытия: {1})",
					targetPlayerName, sel.FileName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подозрительный файл: {1})",
					targetPlayerName, sel.FileName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан по файлу", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void OpenInJarInspector()
		{
			CheatFileHit sel = GetSelectedItem();
			if (sel == null || !File.Exists(sel.FullPath)) return;

			if (!sel.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
			{
				MessageBox.Show("Выбранный файл не является Java JAR архивом.", "Инспектор JAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				JarStructureViewerForm jarForm = new JarStructureViewerForm(sel.FullPath, targetPlayerName);
				jarForm.Show(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия инспектора JAR: " + ex.Message, "Инспектор JAR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenFileLocation()
		{
			CheatFileHit sel = GetSelectedItem();
			if (sel == null || string.IsNullOrEmpty(sel.FullPath) || !File.Exists(sel.FullPath))
			{
				MessageBox.Show("Файл не найден на диске.", "Файл", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				Process.Start("explorer.exe", "/select,\"" + sel.FullPath + "\"");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия Проводника: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ShowEverythingMenu(Control anchor)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Renderer = new DarkMenuRenderer();

			menu.Items.Add("🔥  Запустить Everything: 350+ читов", null, delegate
			{
				LaunchEverythingQuery(CheatDatabases.EverythingNamesQuery);
			});

			menu.Items.Add("🎯  Запустить Everything: 140+ точных размеров (*.jar)", null, delegate
			{
				LaunchEverythingQuery(CheatDatabases.EverythingSizesQuery);
			});

			menu.Items.Add("🧹  Запустить Everything: Клинеры и батники (5.08)", null, delegate
			{
				LaunchEverythingQuery("*.bat|*.cmd|*.ps1|clean*|wipe*|sdelete*|journaltrace*|cleandetecting*");
			});

			menu.Items.Add("☕  Запустить Everything: Свежие моды .jar за сегодня", null, delegate
			{
				LaunchEverythingQuery("ext:jar dm:today");
			});

			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add("📋  Скопировать запрос 350+ читов в буфер", null, delegate
			{
				Clipboard.SetText(CheatDatabases.EverythingNamesQuery);
				MessageBox.Show("Запрос 350+ софтов скопирован!", "Everything", MessageBoxButtons.OK, MessageBoxIcon.Information);
			});

			menu.Items.Add("📋  Скопировать запрос 140 размеров *.jar в буфер", null, delegate
			{
				Clipboard.SetText(CheatDatabases.EverythingSizesQuery);
				MessageBox.Show("Запрос 140 размеров скопирован!", "Everything", MessageBoxButtons.OK, MessageBoxIcon.Information);
			});

			menu.Show(anchor, 0, anchor.Height);
		}

		private void LaunchEverythingQuery(string query)
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("everything.exe", "-s \"" + query + "\"");
				Process.Start(psi);
			}
			catch
			{
				try
				{
					string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
					string everythingPath = Path.Combine(progFiles, "Everything", "Everything.exe");
					if (File.Exists(everythingPath))
					{
						Process.Start(everythingPath, "-s \"" + query + "\"");
						return;
					}
				}
				catch { }

				Clipboard.SetText(query);
				MessageBox.Show("Everything.exe не найден в PATH или Program Files.\nЗапрос скопирован в буфер обмена:\n\n" + query, "Everything", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	}
}
