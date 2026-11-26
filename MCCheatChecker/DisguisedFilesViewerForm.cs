using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DisguisedFilesViewerForm : Form
	{
		private enum DisguiseCategory
		{
			All,
			FakeMods,
			HiddenExecutable,
			HiddenJar,
			SizeMatch,
			DoubleExtension
		}

		private class DisguisedItem
		{
			public int Index;
			public string FileName;
			public string FullPath;
			public long FileSizeBytes;
			public string DetectedMagic;
			public string InternalModId;
			public string InternalModName;
			public DisguiseCategory Category;
			public string CategoryString;
			public bool IsCheat;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private DisguiseCategory currentFilter = DisguiseCategory.All;
		private List<DisguisedItem> allItems = new List<DisguisedItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailFile;
		private Label lblDetailPath;
		private Label lblDetailMagic;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterFakeMods;
		private DarkButton btnFilterHiddenExe;
		private DarkButton btnFilterHiddenJar;
		private DarkButton btnFilterSizeMatch;
		private DarkButton btnFilterDoubleExt;

		private static readonly Regex CheatKeywordsPattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|boze|abyss|killaura|velocity|aimbot|autoclicker)",
			RegexOptions.Compiled);

		private static readonly HashSet<string> LegitimateModNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"sodium", "iris", "lithium", "indium", "ferritecore", "entityculling", "immediatelyfast",
			"embeddium", "modernfix", "krypton", "starlight", "fabric-api", "forge-api", "modmenu",
			"cloth-config", "appleskin", "optifine", "replaymod", "essential", "zoomify", "logicalzoom",
			"continuity", "citresewn", "notenoughanimations", "3dskinlayers", "waveycapes", "physicsmod",
			"xaerominimap", "xaeroworldmap", "journeymap", "voxelmap", "litematica", "malilib"
		};

		public DisguisedFilesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			ScanDisguisedFilesAsync();
		}

		private void InitializeComponent()
		{
			Text = "🎭 Инспектор замаскированных читов и поддельных модов (Fake Mods & Masquerade Hunter)";
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
			lblHeaderTitle.Text = "🎭  ИНСПЕКТОР ЗАМАСКИРОВАННЫХ ЧИТОВ (Fake Mods, Header Mismatch & Stealth JARs)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Глубокая проверка: выявление поддельных модов (Fake Sodium/Iris), PE-бинарников под видом картинок (MZ) и скрытых JAR...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование директорий на маскировки...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(1015, 8);
			btnRefresh.Size = new Size(150, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { ScanDisguisedFilesAsync(); };
			topPanel.Controls.Add(btnRefresh);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все находки (0)", DisguiseCategory.All, 130);
			btnFilterFakeMods = CreateFilterButton("🚨 Поддельные моды (0)", DisguiseCategory.FakeMods, 175);
			btnFilterHiddenExe = CreateFilterButton("💉 Скрытые PE/MZ (0)", DisguiseCategory.HiddenExecutable, 160);
			btnFilterHiddenJar = CreateFilterButton("📦 Скрытые JAR (0)", DisguiseCategory.HiddenJar, 140);
			btnFilterSizeMatch = CreateFilterButton("🎯 Размер чита (0)", DisguiseCategory.SizeMatch, 140);
			btnFilterDoubleExt = CreateFilterButton("⚠️ Двойные расш. (0)", DisguiseCategory.DoubleExtension, 150);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterFakeMods, btnFilterHiddenExe, btnFilterHiddenJar, btnFilterSizeMatch, btnFilterDoubleExt };
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
			lblHint.Text = "💡 Подсказка: Кликните дважды для копирования команды бана. Модуль проверяет несоответствие заголовков (Magic Bytes).";
			lblHint.Font = Ui.FontBody(8f);
			lblHint.ForeColor = Ui.TextMuted;
			lblHint.Location = new Point(570, 4);
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
			listView.Columns.Add("Категория маскировки", 175);
			listView.Columns.Add("Магические байты", 130);
			listView.Columns.Add("Внутренний ID / Чит", 170);
			listView.Columns.Add("Размер файла", 110);
			listView.Columns.Add("Полный путь", 350);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details & Actions
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 10, 12, 10);

			lblDetailFile = new Label();
			lblDetailFile.Text = "Выберите подозрительный файл для анализа...";
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

			lblDetailMagic = new Label();
			lblDetailMagic.Text = "Заголовок / Сигнатура: —";
			lblDetailMagic.Font = Ui.FontBody(8.5f);
			lblDetailMagic.ForeColor = Ui.TextSecondary;
			lblDetailMagic.Location = new Point(12, 50);
			lblDetailMagic.AutoSize = true;
			detailContainer.Controls.Add(lblDetailMagic);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Вердикт: —";
			lblDetailStatus.Font = Ui.FontBody(8.5f, true);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Location = new Point(12, 70);
			lblDetailStatus.AutoSize = true;
			detailContainer.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Пункт регламента: —";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.AccentTeal;
			lblDetailRule.Location = new Point(12, 90);
			lblDetailRule.AutoSize = true;
			detailContainer.Controls.Add(lblDetailRule);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Location = new Point(12, 114);
			txtDetailDescription.Size = new Size(820, 85);
			txtDetailDescription.Font = Ui.FontMono(8f);
			txtDetailDescription.ReadOnly = true;
			detailContainer.Controls.Add(txtDetailDescription);

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать /ban";
			btnCopyBan.Location = new Point(850, 114);
			btnCopyBan.Size = new Size(160, 27);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			detailContainer.Controls.Add(btnCopyBan);

			DarkButton btnOpenJar = new DarkButton();
			btnOpenJar.Text = "📦 Инспектор JAR";
			btnOpenJar.Location = new Point(850, 145);
			btnOpenJar.Size = new Size(160, 27);
			btnOpenJar.BackColor = Color.FromArgb(94, 53, 177);
			btnOpenJar.ForeColor = Color.White;
			btnOpenJar.Font = Ui.FontBody(8.5f, true);
			btnOpenJar.Cursor = Cursors.Hand;
			btnOpenJar.Click += delegate { OpenInJarInspector(); };
			detailContainer.Controls.Add(btnOpenJar);

			DarkButton btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Показать файл";
			btnOpenFolder.Location = new Point(1020, 114);
			btnOpenFolder.Size = new Size(160, 27);
			btnOpenFolder.BackColor = Ui.SurfaceAlt;
			btnOpenFolder.Font = Ui.FontBody(8.5f, true);
			btnOpenFolder.Cursor = Cursors.Hand;
			btnOpenFolder.Click += delegate { OpenFileLocation(); };
			detailContainer.Controls.Add(btnOpenFolder);

			DarkButton btnVt = new DarkButton();
			btnVt.Text = "🔍 VirusTotal";
			btnVt.Location = new Point(1020, 145);
			btnVt.Size = new Size(160, 27);
			btnVt.BackColor = Ui.SurfaceAlt;
			btnVt.Font = Ui.FontBody(8.5f, true);
			btnVt.Cursor = Cursors.Hand;
			btnVt.Click += delegate
			{
				DisguisedItem sel = GetSelectedItem();
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
					btnCopyBan.Location = new Point(descW + 24, 114);
					btnOpenJar.Location = new Point(descW + 24, 145);
					btnOpenFolder.Location = new Point(descW + 190, 114);
					btnVt.Location = new Point(descW + 190, 145);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);
		}

		private DarkButton CreateFilterButton(string text, DisguiseCategory filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentRed : Ui.SurfaceAlt;
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
			DarkButton[] btns = { btnFilterAll, btnFilterFakeMods, btnFilterHiddenExe, btnFilterHiddenJar, btnFilterSizeMatch, btnFilterDoubleExt };
			DisguiseCategory[] filters = { DisguiseCategory.All, DisguiseCategory.FakeMods, DisguiseCategory.HiddenExecutable, DisguiseCategory.HiddenJar, DisguiseCategory.SizeMatch, DisguiseCategory.DoubleExtension };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentRed : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void ScanDisguisedFilesAsync()
		{
			lblStatusPill.Text = "⏳ Глубокий поиск маскировок и проверка сигнатур...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<DisguisedItem> items = PerformDisguiseAudit();
				e.Result = items;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<DisguisedItem>)
				{
					allItems = (List<DisguisedItem>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static List<DisguisedItem> PerformDisguiseAudit()
		{
			List<DisguisedItem> results = new List<DisguisedItem>();
			int idx = 1;

			// Gather critical paths
			List<string> searchDirs = new List<string>();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			string mcMods = Path.Combine(appData, ".minecraft", "mods");
			if (Directory.Exists(mcMods)) searchDirs.Add(mcMods);

			string mcRoot = Path.Combine(appData, ".minecraft");
			if (Directory.Exists(mcRoot)) searchDirs.Add(mcRoot);

			string downloads = Path.Combine(userProfile, "Downloads");
			if (Directory.Exists(downloads)) searchDirs.Add(downloads);

			string tempDir = Path.GetTempPath();
			if (Directory.Exists(tempDir)) searchDirs.Add(tempDir);

			string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			if (Directory.Exists(desktop)) searchDirs.Add(desktop);

			// 1. Audit .minecraft\mods for Fake Mod Masquerades
			if (Directory.Exists(mcMods))
			{
				try
				{
					string[] jarFiles = Directory.GetFiles(mcMods, "*.jar", SearchOption.AllDirectories);
					foreach (string jarPath in jarFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(jarPath);
							long size = fi.Length;
							string fn = Path.GetFileName(jarPath);

							// Check known cheat size match
							if (CheatDatabases.KnownCheatJarSizes.Contains(size))
							{
								DisguisedItem item = new DisguisedItem();
								item.Index = idx++;
								item.FileName = fn;
								item.FullPath = jarPath;
								item.FileSizeBytes = size;
								item.DetectedMagic = "PK (ZIP/JAR)";
								item.Category = DisguiseCategory.SizeMatch;
								item.CategoryString = "🎯 Точный размер чита";
								item.IsCheat = true;
								item.Verdict = "🚨 Совпадение размера чита (5.01)";
								item.ThreatReason = string.Format("Точный размер файла ({0} байт) совпадает с базой утёкших чит-клиентов!", size);
								item.ForeColor = Ui.AccentRed;
								results.Add(item);
							}

							// Inspect inside jar
							DisguisedItem fakeMod = InspectJarForMasquerade(jarPath, ref idx);
							if (fakeMod != null)
							{
								results.Add(fakeMod);
							}
						}
						catch { }
					}
				}
				catch { }
			}

			// 2. Audit all target folders for Magic Byte Mismatch (MZ in media/txt, PK in media/txt)
			foreach (string dir in searchDirs)
			{
				try
				{
					// Scan non-executable extensions
					string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string fp in files)
					{
						try
						{
							string ext = Path.GetExtension(fp).ToLowerInvariant();
							string fn = Path.GetFileName(fp);

							// Double extension check (e.g. optifine.jar.exe, test.png.bat)
							if (fn.Contains(".jar.") || fn.Contains(".png.") || fn.Contains(".jpg."))
							{
								if (ext == ".exe" || ext == ".cmd" || ext == ".bat" || ext == ".ps1" || ext == ".vbs")
								{
									DisguisedItem dExt = new DisguisedItem();
									dExt.Index = idx++;
									dExt.FileName = fn;
									dExt.FullPath = fp;
									dExt.FileSizeBytes = new FileInfo(fp).Length;
									dExt.DetectedMagic = "Двойное расширение";
									dExt.Category = DisguiseCategory.DoubleExtension;
									dExt.CategoryString = "⚠️ Двойное расширение";
									dExt.IsCheat = true;
									dExt.Verdict = "🚨 Двойное расширение (5.01 / 5.08)";
									dExt.ThreatReason = "Опасный исполняемый файл маскируется двойным расширением: " + fn;
									dExt.ForeColor = Ui.AccentRed;
									results.Add(dExt);
									continue;
								}
							}

							if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" ||
								ext == ".txt" || ext == ".ogg" || ext == ".dat" || ext == ".bin" || ext == ".log")
							{
								FileInfo fi = new FileInfo(fp);
								if (fi.Length < 16 || fi.Length > 200 * 1024 * 1024) continue;

								byte[] header = new byte[16];
								using (FileStream fs = new FileStream(fp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
								{
									fs.Read(header, 0, header.Length);
								}

								// Check MZ Header (PE executable disguised as image/text)
								if (header[0] == 0x4D && header[1] == 0x5A) // 'M', 'Z'
								{
									DisguisedItem peItem = new DisguisedItem();
									peItem.Index = idx++;
									peItem.FileName = fn;
									peItem.FullPath = fp;
									peItem.FileSizeBytes = fi.Length;
									peItem.DetectedMagic = "MZ (PE Executable)";
									peItem.Category = DisguiseCategory.HiddenExecutable;
									peItem.CategoryString = "💉 Скрытый PE/MZ файл";
									peItem.IsCheat = true;
									peItem.Verdict = "🚨 Замаскированный .EXE/.DLL (5.01)";
									peItem.ThreatReason = string.Format("Файл с расширением {0} на самом деле является исполняемым бинарником Windows (сигнатура MZ)!", ext);
									peItem.ForeColor = Ui.AccentRed;
									results.Add(peItem);
									continue;
								}

								// Check PK Header (ZIP/JAR disguised as image/text)
								if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
								{
									// Verify if it contains .class files
									bool isJar = false;
									try
									{
										using (ZipArchive zip = ZipFile.OpenRead(fp))
										{
											foreach (ZipArchiveEntry entry in zip.Entries)
											{
												if (entry.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase) ||
													entry.FullName.Equals("META-INF/MANIFEST.MF", StringComparison.OrdinalIgnoreCase) ||
													entry.FullName.Equals("fabric.mod.json", StringComparison.OrdinalIgnoreCase))
												{
													isJar = true;
													break;
												}
											}
										}
									}
									catch { }

									if (isJar)
									{
										DisguisedItem jarItem = new DisguisedItem();
										jarItem.Index = idx++;
										jarItem.FileName = fn;
										jarItem.FullPath = fp;
										jarItem.FileSizeBytes = fi.Length;
										jarItem.DetectedMagic = "PK (Java ZIP/JAR)";
										jarItem.Category = DisguiseCategory.HiddenJar;
										jarItem.CategoryString = "📦 Скрытый JAR архив";
										jarItem.IsCheat = true;
										jarItem.Verdict = "🚨 Скрытый Java мод (5.01)";
										jarItem.ThreatReason = string.Format("Файл с расширением {0} является замаскированным Java JAR архивом с байткодом классов!", ext);
										jarItem.ForeColor = Ui.AccentRed;
										results.Add(jarItem);
									}
								}
							}
						}
						catch { }
					}
				}
				catch { }
			}

			return results;
		}

		private static DisguisedItem InspectJarForMasquerade(string jarPath, ref int idx)
		{
			try
			{
				string fileName = Path.GetFileName(jarPath);
				string fnLower = fileName.ToLowerInvariant();

				// Check if the filename claims to be a legitimate mod
				bool claimsLegit = false;
				string claimedLegitName = "";
				foreach (string legit in LegitimateModNames)
				{
					if (fnLower.Contains(legit))
					{
						claimsLegit = true;
						claimedLegitName = legit;
						break;
					}
				}

				if (!claimsLegit) return null;

				// Open and inspect internal manifest and classes
				using (ZipArchive zip = ZipFile.OpenRead(jarPath))
				{
					string internalId = "";
					string internalName = "";
					bool internalCheatFound = false;
					string cheatMatchDetails = "";

					// 1. Check fabric.mod.json
					ZipArchiveEntry fabricEntry = zip.GetEntry("fabric.mod.json");
					if (fabricEntry != null)
					{
						using (StreamReader sr = new StreamReader(fabricEntry.Open(), Encoding.UTF8))
						{
							string json = sr.ReadToEnd();
							Match mId = Regex.Match(json, @"""id""\s*:\s*""([^""]+)""");
							if (mId.Success) internalId = mId.Groups[1].Value;

							Match mName = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
							if (mName.Success) internalName = mName.Groups[1].Value;

							if (CheatKeywordsPattern.IsMatch(json))
							{
								internalCheatFound = true;
								cheatMatchDetails = "В fabric.mod.json обнаружены чит-ключи: " + internalId;
							}
						}
					}

					// 2. Check mcmod.info
					ZipArchiveEntry mcmodEntry = zip.GetEntry("mcmod.info");
					if (mcmodEntry != null && !internalCheatFound)
					{
						using (StreamReader sr = new StreamReader(mcmodEntry.Open(), Encoding.UTF8))
						{
							string info = sr.ReadToEnd();
							if (CheatKeywordsPattern.IsMatch(info))
							{
								internalCheatFound = true;
								cheatMatchDetails = "В mcmod.info обнаружены чит-ключи";
							}
						}
					}

					// 3. Scan entry names for cheat packages (e.g. ru/expensive, celestial/client, org/meteor)
					if (!internalCheatFound)
					{
						foreach (ZipArchiveEntry entry in zip.Entries)
						{
							string enLower = entry.FullName.ToLowerInvariant();
							if (CheatKeywordsPattern.IsMatch(enLower))
							{
								internalCheatFound = true;
								cheatMatchDetails = "Внутренний класс/пакет чита: " + entry.FullName;
								break;
							}
						}
					}

					if (internalCheatFound)
					{
						DisguisedItem fakeMod = new DisguisedItem();
						fakeMod.Index = idx++;
						fakeMod.FileName = fileName;
						fakeMod.FullPath = jarPath;
						fakeMod.FileSizeBytes = new FileInfo(jarPath).Length;
						fakeMod.DetectedMagic = "PK (JAR)";
						fakeMod.InternalModId = string.IsNullOrEmpty(internalId) ? "Чит-мод" : internalId;
						fakeMod.InternalModName = internalName;
						fakeMod.Category = DisguiseCategory.FakeMods;
						fakeMod.CategoryString = "🚨 Поддельный мод (Fake Mod)";
						fakeMod.IsCheat = true;
						fakeMod.Verdict = "🚨 ПОДМЕНА МОДА (5.01)";
						fakeMod.ThreatReason = string.Format("Файл называется как легитимный мод [{0}], но внутри скрывает чит-клиент [{1}]! {2}",
							claimedLegitName, internalId, cheatMatchDetails);
						fakeMod.ForeColor = Ui.AccentRed;
						return fakeMod;
					}
				}
			}
			catch { }

			return null;
		}

		private void UpdateSummaryStats()
		{
			int total = allItems.Count;
			int fakeMods = allItems.Count(i => i.Category == DisguiseCategory.FakeMods);
			int hiddenExe = allItems.Count(i => i.Category == DisguiseCategory.HiddenExecutable);
			int hiddenJar = allItems.Count(i => i.Category == DisguiseCategory.HiddenJar);
			int sizeMatch = allItems.Count(i => i.Category == DisguiseCategory.SizeMatch);
			int doubleExt = allItems.Count(i => i.Category == DisguiseCategory.DoubleExtension);

			btnFilterAll.Text = string.Format("🔍 Все ({0})", total);
			btnFilterFakeMods.Text = string.Format("🚨 Поддельные ({0})", fakeMods);
			btnFilterHiddenExe.Text = string.Format("💉 Скрытые PE ({0})", hiddenExe);
			btnFilterHiddenJar.Text = string.Format("📦 Скрытые JAR ({0})", hiddenJar);
			btnFilterSizeMatch.Text = string.Format("🎯 Размер чита ({0})", sizeMatch);
			btnFilterDoubleExt.Text = string.Format("⚠️ Двойные ({0})", doubleExt);

			if (total > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Найдено {0} замаскированных читов/поддельных файлов!", total);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✔ Замаскированных читов и поддельных модов не обнаружено.";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (DisguisedItem item in allItems)
			{
				if (currentFilter != DisguiseCategory.All && item.Category != currentFilter)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = item.FileName.ToLowerInvariant().Contains(query) ||
								 item.Verdict.ToLowerInvariant().Contains(query) ||
								 item.FullPath.ToLowerInvariant().Contains(query) ||
								 (item.InternalModId ?? "").ToLowerInvariant().Contains(query);
					if (!match) continue;
				}

				string sizeStr = item.FileSizeBytes < 1024 * 1024 ?
					string.Format("{0:F1} КБ", item.FileSizeBytes / 1024.0) :
					string.Format("{0:F2} МБ", item.FileSizeBytes / (1024.0 * 1024.0));

				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(item.CategoryString);
				lvi.SubItems.Add(item.DetectedMagic);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.InternalModId) ? "—" : item.InternalModId);
				lvi.SubItems.Add(sizeStr);
				lvi.SubItems.Add(item.FullPath);

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

		private DisguisedItem GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as DisguisedItem;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			DisguisedItem sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailFile.Text = sel.FileName;
			lblDetailPath.Text = "Полный путь: " + sel.FullPath;
			lblDetailMagic.Text = "Сигнатура / Заголовок: " + sel.DetectedMagic + " | Размер: " + sel.FileSizeBytes + " байт";
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;
			lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-программы / Маскировка софта) — Бан 30 дней";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Имя файла на диске: " + sel.FileName);
			sb.AppendLine("Категория маскировки: " + sel.CategoryString);
			sb.AppendLine("Сигнатура: " + sel.DetectedMagic);
			if (!string.IsNullOrEmpty(sel.InternalModId))
			{
				sb.AppendLine("Истинный внутренний ID чита: " + sel.InternalModId);
			}
			sb.AppendLine("Обоснование вердикта: " + sel.ThreatReason);
			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDetailPane()
		{
			lblDetailFile.Text = "Файлы не выбраны";
			lblDetailPath.Text = "Полный путь: —";
			lblDetailMagic.Text = "Заголовок / Сигнатура: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Пункт регламента: —";
			txtDetailDescription.Text = "";
		}

		private void CopyBanCommand()
		{
			DisguisedItem sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.Category == DisguiseCategory.FakeMods)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подмена мода: {1} на самом деле чит {2})",
					targetPlayerName, sel.FileName, sel.InternalModId);
			}
			else if (sel.Category == DisguiseCategory.HiddenExecutable)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Маскировка чита: бинарник MZ спрятан под {1})",
					targetPlayerName, sel.FileName);
			}
			else if (sel.Category == DisguiseCategory.SizeMatch)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Чит-клиент по совпадению размера: {1} [{2} байт])",
					targetPlayerName, sel.FileName, sel.FileSizeBytes);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Замаскированный чит-файл: {1})",
					targetPlayerName, sel.FileName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан за маскировку чита", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void OpenInJarInspector()
		{
			DisguisedItem sel = GetSelectedItem();
			if (sel == null || string.IsNullOrEmpty(sel.FullPath) || !File.Exists(sel.FullPath)) return;

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
			DisguisedItem sel = GetSelectedItem();
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
	}
}
