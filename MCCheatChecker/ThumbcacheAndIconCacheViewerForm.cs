using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ThumbcacheAndIconCacheViewerForm : Form
	{
		#region Data Models

		public enum ThumbFilterCategory
		{
			All,
			ViolationsOnly,
			ExtractedImages,
			DatabaseFiles,
			IconCaches
		}

		public class ThumbcacheItem
		{
			public int Index;
			public string ItemName;
			public string ItemType; // "Кэш эскизов (.db)", "Извлеченный эскиз", "Кэш иконок"
			public ThumbFilterCategory Category;
			public string Format; // "JPEG", "PNG", "CMMC DB"
			public long SizeBytes;
			public DateTime Timestamp;
			public string Verdict;
			public string TargetRule;
			public string SourcePath;
			public bool IsViolation;
			public Bitmap ThumbnailImage;
			public Color RowColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ThumbFilterCategory currentFilter = ThumbFilterCategory.All;
		private List<ThumbcacheItem> allItems = new List<ThumbcacheItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private PictureBox picPreview;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiImages;
		private Label lblKpiViolations;
		private Label lblKpiDatabases;
		private Label lblKpiIcons;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterImages;
		private DarkButton btnFilterDatabases;
		private DarkButton btnFilterIcons;

		private DarkButton btnSaveImage;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public ThumbcacheAndIconCacheViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "🖼️ Форензика системного кэша эскизов Thumbcache и IconCache — MCCheatChecker";
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
			lblHeaderTitle.Text = "🖼️ Форензика системного кэша эскизов Windows Thumbcache и кэша иконок IconCache";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Анализ эскизов удаленных файлов: " + targetPlayerName;
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

			lblKpiTotal = CreateKpiLabel("Всего объектов: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiImages = CreateKpiLabel("Эскизов извлечено: 0", Ui.AccentTeal, 180, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик зачистки (5.08): 0", Ui.AccentRed, 380, kpiPanel);
			lblKpiDatabases = CreateKpiLabel("Баз Thumbcache: 0", Ui.AccentOrange, 590, kpiPanel);
			lblKpiIcons = CreateKpiLabel("Баз IconCache: 0", Ui.AccentPurple, 790, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все объекты", ThumbFilterCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.08)", ThumbFilterCategory.ViolationsOnly, 126, filterPanel);
			btnFilterImages = CreateFilterButton("🖼️ Извлеченные эскизы", ThumbFilterCategory.ExtractedImages, 290, filterPanel);
			btnFilterDatabases = CreateFilterButton("💾 Базы Thumbcache (.db)", ThumbFilterCategory.DatabaseFiles, 494, filterPanel);
			btnFilterIcons = CreateFilterButton("🎨 Базы иконок IconCache", ThumbFilterCategory.IconCaches, 708, filterPanel);

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
			listView.Columns.Add("Объект / Файл", 220);
			listView.Columns.Add("Тип артефакта", 140);
			listView.Columns.Add("Формат / Разрешение", 140);
			listView.Columns.Add("Размер", 90);
			listView.Columns.Add("Вердикт", 200);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Время создания / изменения", 150);
			listView.Columns.Add("Путь / База-источник", 300);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Детали артефакта Thumbcache / Hex-структура:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(12, 30);
			txtDetails.Size = new Size(650, 140);
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			bottomContainer.Controls.Add(txtDetails);

			// PictureBox for Thumbnail Preview
			Panel previewPanel = new Panel();
			previewPanel.Location = new Point(675, 10);
			previewPanel.Size = new Size(200, 160);
			previewPanel.BackColor = Ui.Backdrop;
			previewPanel.BorderStyle = BorderStyle.FixedSingle;

			picPreview = new PictureBox();
			picPreview.Dock = DockStyle.Fill;
			picPreview.SizeMode = PictureBoxSizeMode.Zoom;
			picPreview.BackColor = Ui.Backdrop;
			previewPanel.Controls.Add(picPreview);
			bottomContainer.Controls.Add(previewPanel);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(890, 15);
			actionsPanel.Size = new Size(330, 155);

			btnSaveImage = CreateActionButton("💾 Сохранить эскиз (.png)", 0, actionsPanel);
			btnSaveImage.Click += delegate { SaveSelectedImage(); };

			btnOpenFolder = CreateActionButton("📂 Открыть папку Explorer в Проводнике", 32, actionsPanel);
			btnOpenFolder.Click += delegate { OpenExplorerFolder(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 64, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана (5.08)", 96, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(0, 128);
			btnRescan.Size = new Size(320, 26);
			btnRescan.Click += delegate { StartScan(); };
			actionsPanel.Controls.Add(btnRescan);

			bottomContainer.Controls.Add(actionsPanel);

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

		private DarkButton CreateFilterButton(string text, ThumbFilterCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == ThumbFilterCategory.ViolationsOnly ? 158 : (cat == ThumbFilterCategory.ExtractedImages ? 198 : (cat == ThumbFilterCategory.DatabaseFiles ? 208 : (cat == ThumbFilterCategory.IconCaches ? 208 : 108))), 28);
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
			if (cat == ThumbFilterCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterImages, btnFilterDatabases, btnFilterIcons };
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

			lblStatusPill.Text = "⏳ Сканирование баз эскизов Thumbcache и извлечение изображений...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			picPreview.Image = null;
			txtDetails.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ThumbcacheItem> list = new List<ThumbcacheItem>();
			int idx = 1;

			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string explorerDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");

			if (!Directory.Exists(explorerDir))
			{
				e.Result = list;
				return;
			}

			long uptimeMinutes = Environment.TickCount / (1000 * 60);

			// 1. Analyze thumbcache_*.db and iconcache_*.db files
			try
			{
				string[] dbFiles = Directory.GetFiles(explorerDir, "*cache_*.db");
				DateTime newestDbTime = DateTime.MinValue;
				DateTime oldestDbTime = DateTime.MaxValue;

				foreach (string dbFile in dbFiles)
				{
					FileInfo fi = new FileInfo(dbFile);
					bool isIcon = fi.Name.ToLowerInvariant().StartsWith("iconcache");

					if (fi.LastWriteTime > newestDbTime) newestDbTime = fi.LastWriteTime;
					if (fi.CreationTime < oldestDbTime) oldestDbTime = fi.CreationTime;

					ThumbcacheItem it = new ThumbcacheItem();
					it.Index = idx++;
					it.ItemName = fi.Name;
					it.ItemType = isIcon ? "Кэш иконок (IconCache)" : "Кэш эскизов (Thumbcache)";
					it.Category = isIcon ? ThumbFilterCategory.IconCaches : ThumbFilterCategory.DatabaseFiles;
					it.Format = "CMMC Database";
					it.SizeBytes = fi.Length;
					it.Timestamp = fi.LastWriteTime;
					it.SourcePath = dbFile;
					it.RowColor = Ui.TextPrimary;
					it.Verdict = "Активная база кэша Explorer";
					it.TargetRule = "Системный кэш";

					StringBuilder sb = new StringBuilder();
					sb.AppendLine("=== СИСТЕМНАЯ БАЗА ЭСКИЗОВ WINDOWS ===");
					sb.AppendLine("Файл: " + fi.FullName);
					sb.AppendLine("Размер: " + FormatBytes(fi.Length) + " (" + fi.Length + " байт)");
					sb.AppendLine("Время создания: " + fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
					sb.AppendLine("Последняя запись: " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
					sb.AppendLine();
					sb.AppendLine("База содержит кэшированные миниатюры файлов (JPEG/PNG/BMP), просмотренных пользователем в Проводнике, включая файлы на флешках и в папках Загрузок.");
					it.Details = sb.ToString();

					list.Add(it);

					// Carve embedded images from smaller databases (thumbcache_96.db, thumbcache_256.db, thumbcache_48.db)
					string lowName = fi.Name.ToLowerInvariant();
					if (lowName.Contains("_96.db") || lowName.Contains("_256.db") || lowName.Contains("_48.db"))
					{
						CarveImagesFromDatabase(dbFile, fi.Name, list, ref idx);
					}
				}

				// Check for Thumbcache Wiping / Cleaners (5.08)
				// If newestDbTime is within last 20 minutes while uptime is > 1 hour and total databases were created recently
				TimeSpan timeSinceRecreation = DateTime.Now - oldestDbTime;
				if (uptimeMinutes > 60 && timeSinceRecreation.TotalMinutes < 25)
				{
					ThumbcacheItem it = new ThumbcacheItem();
					it.Index = idx++;
					it.ItemName = "Системный каталог кэша Explorer";
					it.ItemType = "Аномалия кэша эскизов";
					it.Category = ThumbFilterCategory.ViolationsOnly;
					it.IsViolation = true;
					it.Format = "Аномалия времени";
					it.SizeBytes = 0;
					it.Timestamp = oldestDbTime;
					it.SourcePath = explorerDir;
					it.RowColor = Ui.AccentRed;
					it.Verdict = "🚨 ОЧИСТКА КЭША THUMBCACHE ПЕРЕД СС (5.08)";
					it.TargetRule = "5.08 (Сокрытие улик)";
					it.Details = string.Format("Базы thumbcache_*.db были пересозданы {0:0.0} мин. назад при аптайме системы {1} ч.\nЭто прямое свидетельство использования клинера или батника очистки эскизов перед ScreenShare.", timeSinceRecreation.TotalMinutes, (uptimeMinutes / 60.0));
					list.Add(it);
				}
			}
			catch { }

			e.Result = list;
		}

		private void CarveImagesFromDatabase(string dbPath, string dbName, List<ThumbcacheItem> list, ref int idx)
		{
			try
			{
				using (FileStream fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					if (fs.Length > 20 * 1024 * 1024) return; // Skip very large files to keep fast
					byte[] buffer = new byte[fs.Length];
					fs.Read(buffer, 0, buffer.Length);

					int carvedCount = 0;
					int maxCarve = 15; // Max 15 thumbnails per db for speed and responsiveness

					for (int i = 0; i < buffer.Length - 4 && carvedCount < maxCarve; i++)
					{
						// Check JPEG magic: FF D8 FF
						if (buffer[i] == 0xFF && buffer[i + 1] == 0xD8 && buffer[i + 2] == 0xFF)
						{
							// Find JPEG end: FF D9
							int endIdx = -1;
							for (int j = i + 2; j < Math.Min(buffer.Length - 1, i + 500000); j++)
							{
								if (buffer[j] == 0xFF && buffer[j + 1] == 0xD9)
								{
									endIdx = j + 2;
									break;
								}
							}

							if (endIdx > i)
							{
								int imgLen = endIdx - i;
								byte[] imgBytes = new byte[imgLen];
								Array.Copy(buffer, i, imgBytes, 0, imgLen);

								try
								{
									using (MemoryStream ms = new MemoryStream(imgBytes))
									{
										Bitmap bmp = new Bitmap(ms);
										Bitmap cloned = new Bitmap(bmp); // Detach from stream

										ThumbcacheItem it = new ThumbcacheItem();
										it.Index = idx++;
										it.ItemName = string.Format("Эскиз #{0} ({1}x{2})", carvedCount + 1, cloned.Width, cloned.Height);
										it.ItemType = "Извлеченный эскиз";
										it.Category = ThumbFilterCategory.ExtractedImages;
										it.Format = "JPEG (" + cloned.Width + "x" + cloned.Height + ")";
										it.SizeBytes = imgLen;
										it.Timestamp = DateTime.Now;
										it.SourcePath = dbName + " [Смещение: 0x" + i.ToString("X") + "]";
										it.Verdict = "Кэшированный эскиз файла";
										it.TargetRule = "Криминалистика изображений";
										it.ThumbnailImage = cloned;
										it.RowColor = Ui.AccentTeal;

										StringBuilder sb = new StringBuilder();
										sb.AppendLine("=== ИЗВЛЕЧЕННЫЙ ЭСКИЗ ФАЙЛА (CARVED THUMBNAIL) ===");
										sb.AppendLine("База-источник: " + dbName);
										sb.AppendLine("Смещение в базе: 0x" + i.ToString("X"));
										sb.AppendLine("Формат: JPEG");
										sb.AppendLine("Разрешение: " + cloned.Width + " x " + cloned.Height);
										sb.AppendLine("Размер потока: " + FormatBytes(imgLen));
										sb.AppendLine();
										sb.AppendLine("Эскиз сохраняется в кэше Windows даже после удаления оригинального файла с диска.");
										it.Details = sb.ToString();

										list.Add(it);
										carvedCount++;
										i = endIdx; // Skip past this image
									}
								}
								catch { }
							}
						}
						// Check PNG magic: 89 50 4E 47 0D 0A 1A 0A
						else if (buffer[i] == 0x89 && buffer[i + 1] == 0x50 && buffer[i + 2] == 0x4E && buffer[i + 3] == 0x47)
						{
							// Find PNG IEND: 49 45 4E 44 AE 42 60 82
							int endIdx = -1;
							for (int j = i + 4; j < Math.Min(buffer.Length - 4, i + 500000); j++)
							{
								if (buffer[j] == 0x49 && buffer[j + 1] == 0x45 && buffer[j + 2] == 0x4E && buffer[j + 3] == 0x44)
								{
									endIdx = j + 8;
									break;
								}
							}

							if (endIdx > i)
							{
								int imgLen = endIdx - i;
								byte[] imgBytes = new byte[imgLen];
								Array.Copy(buffer, i, imgBytes, 0, imgLen);

								try
								{
									using (MemoryStream ms = new MemoryStream(imgBytes))
									{
										Bitmap bmp = new Bitmap(ms);
										Bitmap cloned = new Bitmap(bmp);

										ThumbcacheItem it = new ThumbcacheItem();
										it.Index = idx++;
										it.ItemName = string.Format("Эскиз PNG #{0} ({1}x{2})", carvedCount + 1, cloned.Width, cloned.Height);
										it.ItemType = "Извлеченный эскиз";
										it.Category = ThumbFilterCategory.ExtractedImages;
										it.Format = "PNG (" + cloned.Width + "x" + cloned.Height + ")";
										it.SizeBytes = imgLen;
										it.Timestamp = DateTime.Now;
										it.SourcePath = dbName + " [Смещение: 0x" + i.ToString("X") + "]";
										it.Verdict = "Кэшированный эскиз файла";
										it.TargetRule = "Криминалистика изображений";
										it.ThumbnailImage = cloned;
										it.RowColor = Ui.AccentTeal;

										it.Details = string.Format("Извлеченный PNG эскиз из {0}.\nРазрешение: {1}x{2}.\nРазмер: {3}.", dbName, cloned.Width, cloned.Height, FormatBytes(imgLen));
										list.Add(it);
										carvedCount++;
										i = endIdx;
									}
								}
								catch { }
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования Thumbcache: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<ThumbcacheItem> res = e.Result as List<ThumbcacheItem>;
			if (res != null)
			{
				allItems = res;

				int images = 0;
				int violations = 0;
				int dbs = 0;
				int icons = 0;

				foreach (ThumbcacheItem it in allItems)
				{
					if (it.Category == ThumbFilterCategory.ExtractedImages) images++;
					if (it.IsViolation) violations++;
					if (it.Category == ThumbFilterCategory.DatabaseFiles) dbs++;
					if (it.Category == ThumbFilterCategory.IconCaches) icons++;
				}

				lblKpiTotal.Text = "Всего объектов: " + allItems.Count;
				lblKpiImages.Text = "Эскизов извлечено: " + images;
				lblKpiViolations.Text = "Улик зачистки (5.08): " + violations;
				lblKpiDatabases.Text = "Баз Thumbcache: " + dbs;
				lblKpiIcons.Text = "Баз IconCache: " + icons;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Обнаружена аномалия недавней зачистки кэша эскизов перед ScreenShare (5.08)!");
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = string.Format("✅ Анализ кэша эскизов завершен: успешно извлечено {0} графических миниатюр.", images);
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

			foreach (ThumbcacheItem it in allItems)
			{
				if (currentFilter == ThumbFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == ThumbFilterCategory.ExtractedImages && it.Category != ThumbFilterCategory.ExtractedImages) continue;
				if (currentFilter == ThumbFilterCategory.DatabaseFiles && it.Category != ThumbFilterCategory.DatabaseFiles) continue;
				if (currentFilter == ThumbFilterCategory.IconCaches && it.Category != ThumbFilterCategory.IconCaches) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.ItemName.ToLowerInvariant().Contains(search) ||
						it.ItemType.ToLowerInvariant().Contains(search) ||
						it.Format.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.SourcePath.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ItemName);
				lvi.SubItems.Add(it.ItemType);
				lvi.SubItems.Add(it.Format);
				lvi.SubItems.Add(FormatBytes(it.SizeBytes));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.SourcePath);

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
				picPreview.Image = null;
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ThumbcacheItem it = listView.SelectedItems[0].Tag as ThumbcacheItem;
			if (it == null) return;

			txtDetails.Text = it.Details;
			picPreview.Image = it.ThumbnailImage;
		}

		private string FormatBytes(long bytes)
		{
			if (bytes < 1024) return bytes + " B";
			if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
			return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
		}

		#endregion

		#region Actions

		private void SaveSelectedImage()
		{
			if (listView.SelectedItems.Count == 0) return;
			ThumbcacheItem it = listView.SelectedItems[0].Tag as ThumbcacheItem;
			if (it == null || it.ThumbnailImage == null)
			{
				MessageBox.Show("Для сохранения выберите извлеченный эскиз изображения.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Title = "Сохранить извлеченный эскиз";
				sfd.Filter = "PNG Изображение (*.png)|*.png|JPEG Изображение (*.jpg)|*.jpg";
				sfd.FileName = string.Format("Thumb_{0}_{1}.png", targetPlayerName, it.Index);

				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						it.ThumbnailImage.Save(sfd.FileName, ImageFormat.Png);
						MessageBox.Show("Эскиз успешно сохранен:\n" + sfd.FileName, "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void OpenExplorerFolder()
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string explorerDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
			if (Directory.Exists(explorerDir))
			{
				try
				{
					Process.Start("explorer.exe", explorerDir);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			ThumbcacheItem it = listView.SelectedItems[0].Tag as ThumbcacheItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ КЭША ЭСКИЗОВ THUMBCACHE ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Объект: " + it.ItemName);
			sb.AppendLine("Тип: " + it.ItemType);
			sb.AppendLine("Формат: " + it.Format);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт правил: " + it.TargetRule);
			sb.AppendLine("Источник: " + it.SourcePath);
			sb.AppendLine("Время: " + it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ThumbcacheItem it = listView.SelectedItems[0].Tag as ThumbcacheItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Очистка кэша эскизов Thumbcache перед ScreenShare)", targetPlayerName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
