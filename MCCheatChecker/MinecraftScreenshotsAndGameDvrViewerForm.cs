using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class MinecraftScreenshotsAndGameDvrViewerForm : Form
	{
		#region Data Models

		public enum ScreenshotFilterCategory
		{
			All,
			ViolationsOnly,
			TodaySession,
			GameDvrVideos,
			DisguisedFiles,
			Whitelisted
		}

		public class ScreenshotForensicItem
		{
			public int Index;
			public string ClientSource;
			public string FileName;
			public ScreenshotFilterCategory Category;
			public string ResolutionOrType;
			public long SizeBytes;
			public DateTime Timestamp;
			public string Verdict;
			public string TargetRule;
			public string FullFilePath;
			public bool IsViolation;
			public bool IsDisguised;
			public bool IsVideo;
			public bool IsAllowed;
			public Color RowColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ScreenshotFilterCategory currentFilter = ScreenshotFilterCategory.All;
		private List<ScreenshotForensicItem> allItems = new List<ScreenshotForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private PictureBox picPreview;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiToday;
		private Label lblKpiVideos;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterToday;
		private DarkButton btnFilterVideos;
		private DarkButton btnFilterDisguised;
		private DarkButton btnFilterSafe;

		private DarkButton btnOpenFile;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public MinecraftScreenshotsAndGameDvrViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "📸 Инспектор скриншотов Minecraft, записей GameDVR и скрытых улик — MCCheatChecker";
			Size = new Size(1300, 800);
			MinimumSize = new Size(1060, 620);
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
			lblHeaderTitle.Text = "📸 Инспектор скриншотов Minecraft (F2), записей GameDVR и скрытых графических улик";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Аудит скриншотов .minecraft, Lunar, Feather, TLauncher, Prism и видеозаписей GameDVR/ShadowPlay: встроенная галерея предпросмотра, детекция маскированных JAR/EXE под PNG и зачисток улик (5.01 / 5.08).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования каталогов скриншотов...";
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

			lblKpiTotal = CreateKpiLabel("Файлов: 0", Ui.TextPrimary, 0, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Нарушений: 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiToday = CreateKpiLabel("За сегодня: 0", Ui.AccentBlue, 272, kpiPanel);
			lblKpiVideos = CreateKpiLabel("Видеозаписей: 0", Ui.AccentOrange, 408, kpiPanel);
			lblKpiSafe = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Filter bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все медиа", ScreenshotFilterCategory.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", ScreenshotFilterCategory.ViolationsOnly, ref bx, filterBar);
			btnFilterToday = CreateFilterButton("📸 За сегодня (<2ч)", ScreenshotFilterCategory.TodaySession, ref bx, filterBar);
			btnFilterVideos = CreateFilterButton("🎮 Видео GameDVR", ScreenshotFilterCategory.GameDvrVideos, ref bx, filterBar);
			btnFilterDisguised = CreateFilterButton("🎭 Поддельные PNG", ScreenshotFilterCategory.DisguisedFiles, ref bx, filterBar);
			btnFilterSafe = CreateFilterButton("🛡️ Белый список", ScreenshotFilterCategory.Whitelisted, ref bx, filterBar);

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

			// Split Container (List on top, Preview + Details on bottom)
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 380;
			split.BackColor = Ui.Border;

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.GridLines = true;

			listView.Columns.Add("#", 45, HorizontalAlignment.Center);
			listView.Columns.Add("Клиент / Источник", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Имя файла", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Разрешение / Тип", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Размер", 90, HorizontalAlignment.Right);
			listView.Columns.Add("Дата создания", 130, HorizontalAlignment.Center);
			listView.Columns.Add("Вердикт", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Путь к файлу", 260, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.MouseDoubleClick += delegate { OpenFileOrFolder(); };

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📁 Открыть файл / папку", null, delegate { OpenFileOrFolder(); });
			contextMenu.Items.Add("📋 Скопировать путь к файлу", null, delegate { CopyFilePath(); });
			contextMenu.Items.Add("📑 Скопировать Discord-досье на улитку", null, delegate { CopyDiscordReport(); });
			contextMenu.Items.Add("🚨 Скопировать команду бана", null, delegate { CopyBanCommand(); });
			listView.ContextMenuStrip = contextMenu;

			split.Panel1.Controls.Add(listView);

			// Bottom Area: Split into Left Preview & Right Details
			DarkSplitContainer bottomSplit = new DarkSplitContainer();
			bottomSplit.Dock = DockStyle.Fill;
			bottomSplit.Orientation = Orientation.Vertical;
			bottomSplit.SplitterDistance = 420;
			bottomSplit.BackColor = Ui.Border;

			// Left: PictureBox Preview
			Panel previewPanel = new Panel();
			previewPanel.Dock = DockStyle.Fill;
			previewPanel.BackColor = Ui.Surface;
			previewPanel.Padding = new Padding(12);

			Label lblPreviewHeader = new Label();
			lblPreviewHeader.Text = "🖼️ Предпросмотр изображения (Живая галерея):";
			lblPreviewHeader.Font = Ui.FontBody(8.5f, true);
			lblPreviewHeader.ForeColor = Ui.TextSecondary;
			lblPreviewHeader.Dock = DockStyle.Top;
			lblPreviewHeader.Height = 22;
			previewPanel.Controls.Add(lblPreviewHeader);

			picPreview = new PictureBox();
			picPreview.Dock = DockStyle.Fill;
			picPreview.BackColor = Color.FromArgb(18, 20, 24);
			picPreview.SizeMode = PictureBoxSizeMode.Zoom;
			picPreview.BorderStyle = BorderStyle.FixedSingle;
			previewPanel.Controls.Add(picPreview);

			bottomSplit.Panel1.Controls.Add(previewPanel);

			// Right: Text Details + Buttons
			Panel detailsPanel = new Panel();
			detailsPanel.Dock = DockStyle.Fill;
			detailsPanel.BackColor = Ui.Surface;
			detailsPanel.Padding = new Padding(12);

			Label lblDetailHeader = new Label();
			lblDetailHeader.Text = "📄 Криминалистический протокол медиафайла:";
			lblDetailHeader.Font = Ui.FontBody(8.5f, true);
			lblDetailHeader.ForeColor = Ui.TextSecondary;
			lblDetailHeader.Dock = DockStyle.Top;
			lblDetailHeader.Height = 22;
			detailsPanel.Controls.Add(lblDetailHeader);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Dock = DockStyle.Fill;
			txtDetails.Font = Ui.FontMono(9f);
			txtDetails.ReadOnly = true;
			detailsPanel.Controls.Add(txtDetails);

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

			btnOpenFile = new DarkButton();
			btnOpenFile.Text = "📁 Открыть файл";
			btnOpenFile.Size = new Size(140, 32);
			btnOpenFile.Location = new Point(448, 6);
			btnOpenFile.Click += delegate { OpenFileOrFolder(); };
			btnBar.Controls.Add(btnOpenFile);

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(160, 32);
			btnRescan.Location = new Point(btnBar.Width - 172, 6);
			btnRescan.Click += delegate { StartScan(); };
			btnBar.Controls.Add(btnRescan);

			detailsPanel.Controls.Add(btnBar);
			bottomSplit.Panel2.Controls.Add(detailsPanel);

			split.Panel2.Controls.Add(bottomSplit);

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

		private DarkButton CreateFilterButton(string text, ScreenshotFilterCategory cat, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(160, 30);
			btn.Location = new Point(x, 7);
			x += 166;
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
			lblStatusPill.Text = "⏳ Сканирование скриншотов Minecraft и видеозаписей GameDVR...";
			lblStatusPill.ForeColor = Ui.AccentTeal;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "";
			if (picPreview.Image != null)
			{
				picPreview.Image.Dispose();
				picPreview.Image = null;
			}

			if (scanWorker != null && scanWorker.IsBusy) return;

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ScreenshotForensicItem> list = new List<ScreenshotForensicItem>();
			int idx = 1;

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			// 1. Официальный .minecraft\screenshots
			string dotMcScreens = Path.Combine(appData, @".minecraft\screenshots");
			ScanFolder(".minecraft", dotMcScreens, list, ref idx);

			// 2. Lunar Client
			string lunarScreens = Path.Combine(userProfile, @".lunarclient\offline\multiver\screenshots");
			ScanFolder("Lunar Client", lunarScreens, list, ref idx);

			// 3. Feather Client
			string featherScreens = Path.Combine(appData, @".feather\screenshots");
			ScanFolder("Feather Client", featherScreens, list, ref idx);
			string featherUserScreens = Path.Combine(appData, @".feather\user-gamedir\screenshots");
			ScanFolder("Feather GameDir", featherUserScreens, list, ref idx);

			// 4. TLauncher
			string tlScreens = Path.Combine(appData, @".tlauncher\Minecraft\game\screenshots");
			ScanFolder("TLauncher", tlScreens, list, ref idx);

			// 5. Badlion Client
			string blScreens = Path.Combine(appData, @"Badlion Client\screenshots");
			ScanFolder("Badlion Client", blScreens, list, ref idx);

			// 6. Prism Launcher instances
			string prismDir = Path.Combine(appData, "PrismLauncher\\instances");
			if (Directory.Exists(prismDir))
			{
				ScanLauncherInstances("Prism Launcher", prismDir, list, ref idx);
			}

			// 7. MultiMC instances
			string mmcDir = Path.Combine(appData, "MultiMC\\instances");
			if (Directory.Exists(mmcDir))
			{
				ScanLauncherInstances("MultiMC", mmcDir, list, ref idx);
			}

			// 8. Видеозаписи Xbox Game Bar (Captures)
			string capturesDir = Path.Combine(userProfile, @"Videos\Captures");
			ScanFolder("Xbox Game Bar (Captures)", capturesDir, list, ref idx, isVideoFolder: true);

			// 9. Видеозаписи OBS / ShadowPlay
			string videosDir = Path.Combine(userProfile, "Videos");
			ScanFolder("OBS / ShadowPlay", videosDir, list, ref idx, isVideoFolder: true, topLevelOnly: true);

			e.Result = list;
		}

		private void ScanFolder(string clientName, string folderPath, List<ScreenshotForensicItem> list, ref int idx, bool isVideoFolder = false, bool topLevelOnly = false)
		{
			if (!Directory.Exists(folderPath)) return;

			try
			{
				string[] files = Directory.GetFiles(folderPath, "*.*", topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
				foreach (string filePath in files)
				{
					string fName = Path.GetFileName(filePath);
					string ext = Path.GetExtension(filePath).ToLowerInvariant();

					// Фильтр по расширениям
					bool isImg = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp";
					bool isVid = ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov";

					if (!isImg && !isVid)
					{
						// Неграфический файл в папке скриншотов! Проверим заголовок
						CheckDisguisedFileInScreenshots(clientName, filePath, list, ref idx);
						continue;
					}

					try
					{
						FileInfo fi = new FileInfo(filePath);

						// Проверка на фальшивый PNG (заголовок PK или MZ)
						if (isImg && ext == ".png")
						{
							byte[] header = ReadHeaderBytes(filePath, 4);
							if (header != null && header.Length >= 2)
							{
								if (header[0] == 0x50 && header[1] == 0x4B) // PK (ZIP / JAR)
								{
									list.Add(new ScreenshotForensicItem
									{
										Index = idx++,
										ClientSource = clientName,
										FileName = fName,
										Category = ScreenshotFilterCategory.DisguisedFiles,
										ResolutionOrType = "Фальшивый PNG (ZIP/JAR)",
										SizeBytes = fi.Length,
										Timestamp = fi.LastWriteTime,
										Verdict = "🚨 МАСКИРОВКА ЧИТ-АРХИВА ПОД СКРИНШОТ .PNG (5.08)",
										TargetRule = "5.08 / 5.01 (Маскировка)",
										FullFilePath = filePath,
										IsViolation = true,
										IsDisguised = true,
										RowColor = Ui.AccentRed,
										Details = string.Format("Файл имеет расширение .png, но заголовок PK (0x50 0x4B), что свидетельствует о маскировке ZIP/JAR чита внутри папки скриншотов!\nПуть: {0}\nРазмер: {1} байт", filePath, fi.Length)
									});
									continue;
								}
								else if (header[0] == 0x4D && header[1] == 0x5A) // MZ (EXE / DLL)
								{
									list.Add(new ScreenshotForensicItem
									{
										Index = idx++,
										ClientSource = clientName,
										FileName = fName,
										Category = ScreenshotFilterCategory.DisguisedFiles,
										ResolutionOrType = "Фальшивый PNG (EXE/DLL)",
										SizeBytes = fi.Length,
										Timestamp = fi.LastWriteTime,
										Verdict = "🚨 МАСКИРОВКА ИНЖЕКТОРА ПОД СКРИНШОТ .PNG (5.01)",
										TargetRule = "5.01 / 5.08 (Маскировка)",
										FullFilePath = filePath,
										IsViolation = true,
										IsDisguised = true,
										RowColor = Ui.AccentRed,
										Details = string.Format("Файл имеет расширение .png, но сигнатуру исполняемого файла Windows MZ (0x4D 0x5A)!\nПуть: {0}\nРазмер: {1} байт", filePath, fi.Length)
									});
									continue;
								}
							}
						}

						bool isToday = (DateTime.Now - fi.LastWriteTime).TotalHours < 2.5;
						string resStr = isVid ? "Видеоклип " + ext.ToUpper() : "Изображение " + ext.ToUpper();

						ScreenshotFilterCategory cat = ScreenshotFilterCategory.Whitelisted;
						if (isVid) cat = ScreenshotFilterCategory.GameDvrVideos;
						else if (isToday) cat = ScreenshotFilterCategory.TodaySession;

						string verdict = isToday 
							? "📸 Снимок сделан во время текущей игровой сессии (<2.5ч)" 
							: (isVid ? "Видеозапись экрана GameDVR / ShadowPlay" : "Стандартный скриншот Minecraft");

						string rule = isToday ? "Внимание (Аудит экрана)" : "Разрешено (5.01)";
						Color color = isToday ? Ui.AccentBlue : (isVid ? Ui.AccentOrange : Ui.AccentGreen);

						list.Add(new ScreenshotForensicItem
						{
							Index = idx++,
							ClientSource = clientName,
							FileName = fName,
							Category = cat,
							ResolutionOrType = resStr,
							SizeBytes = fi.Length,
							Timestamp = fi.LastWriteTime,
							Verdict = verdict,
							TargetRule = rule,
							FullFilePath = filePath,
							IsViolation = false,
							IsVideo = isVid,
							IsAllowed = true,
							RowColor = color,
							Details = string.Format("Медиафайл: {0}\nИсточник: {1}\nРазмер: {2:N0} байт ({3:0.0} КБ)\nДата создания: {4}\nПуть: {5}\nСтатус: {6}", fName, clientName, fi.Length, fi.Length / 1024.0, fi.LastWriteTime, filePath, verdict)
						});
					}
					catch { }
				}
			}
			catch { }
		}

		private void CheckDisguisedFileInScreenshots(string clientName, string filePath, List<ScreenshotForensicItem> list, ref int idx)
		{
			try
			{
				FileInfo fi = new FileInfo(filePath);
				byte[] header = ReadHeaderBytes(filePath, 4);
				if (header != null && header.Length >= 2)
				{
					if (header[0] == 0x50 && header[1] == 0x4B)
					{
						list.Add(new ScreenshotForensicItem
						{
							Index = idx++,
							ClientSource = clientName,
							FileName = fi.Name,
							Category = ScreenshotFilterCategory.DisguisedFiles,
							ResolutionOrType = "Скрытый ZIP/JAR архив",
							SizeBytes = fi.Length,
							Timestamp = fi.LastWriteTime,
							Verdict = "🚨 ЧИТ-АРХИВ В ПАПКЕ СКРИНШОТОВ (5.08)",
							TargetRule = "5.08 (Сокрытие улик)",
							FullFilePath = filePath,
							IsViolation = true,
							IsDisguised = true,
							RowColor = Ui.AccentRed,
							Details = string.Format("В папке скриншотов обнаружен скрытый архив чита: {0}\nПуть: {1}", fi.Name, filePath)
						});
					}
					else if (header[0] == 0x4D && header[1] == 0x5A)
					{
						list.Add(new ScreenshotForensicItem
						{
							Index = idx++,
							ClientSource = clientName,
							FileName = fi.Name,
							Category = ScreenshotFilterCategory.DisguisedFiles,
							ResolutionOrType = "Скрытый EXE/DLL инжектор",
							SizeBytes = fi.Length,
							Timestamp = fi.LastWriteTime,
							Verdict = "🚨 ИСПОЛНЯЕМЫЙ ФАЙЛ В СКРИНШОТАХ (5.01)",
							TargetRule = "5.01 (Инжектор в скриншотах)",
							FullFilePath = filePath,
							IsViolation = true,
							IsDisguised = true,
							RowColor = Ui.AccentRed,
							Details = string.Format("В папке скриншотов спрятан исполняемый файл чита: {0}\nПуть: {1}", fi.Name, filePath)
						});
					}
				}
			}
			catch { }
		}

		private void ScanLauncherInstances(string launcherName, string instancesDir, List<ScreenshotForensicItem> list, ref int idx)
		{
			try
			{
				string[] subDirs = Directory.GetDirectories(instancesDir);
				foreach (string sd in subDirs)
				{
					string instName = Path.GetFileName(sd);
					string sc1 = Path.Combine(sd, ".minecraft\\screenshots");
					string sc2 = Path.Combine(sd, "minecraft\\screenshots");
					string sc3 = Path.Combine(sd, "screenshots");

					if (Directory.Exists(sc1)) ScanFolder(launcherName + " (" + instName + ")", sc1, list, ref idx);
					if (Directory.Exists(sc2)) ScanFolder(launcherName + " (" + instName + ")", sc2, list, ref idx);
					if (Directory.Exists(sc3)) ScanFolder(launcherName + " (" + instName + ")", sc3, list, ref idx);
				}
			}
			catch { }
		}

		private static byte[] ReadHeaderBytes(string path, int count)
		{
			try
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] buf = new byte[count];
					int read = fs.Read(buf, 0, count);
					if (read < count)
					{
						byte[] trimmed = new byte[read];
						Array.Copy(buf, trimmed, read);
						return trimmed;
					}
					return buf;
				}
			}
			catch { return null; }
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result is List<ScreenshotForensicItem>)
			{
				allItems = (List<ScreenshotForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int violations = 0;
			int today = 0;
			int videos = 0;
			int safe = 0;

			foreach (ScreenshotForensicItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.Category == ScreenshotFilterCategory.TodaySession) today++;
				if (it.IsVideo) videos++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = "Файлов: " + total;
			lblKpiViolations.Text = "Нарушений: " + violations;
			lblKpiToday.Text = "За сегодня: " + today;
			lblKpiVideos.Text = "Видеозаписей: " + videos;
			lblKpiSafe.Text = "Белый список: " + safe;

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} ПОДДЕЛЬНЫХ PNG / МАСКИРОВАННЫХ ЧИТОВ В СКРИНШОТАХ (5.08)", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Скриншоты чисты: поддельных PNG-заголовков и скрытых JAR/EXE не обнаружено";
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

			foreach (ScreenshotForensicItem it in allItems)
			{
				if (currentFilter == ScreenshotFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == ScreenshotFilterCategory.TodaySession && it.Category != ScreenshotFilterCategory.TodaySession) continue;
				if (currentFilter == ScreenshotFilterCategory.GameDvrVideos && !it.IsVideo) continue;
				if (currentFilter == ScreenshotFilterCategory.DisguisedFiles && !it.IsDisguised) continue;
				if (currentFilter == ScreenshotFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.ClientSource != null && it.ClientSource.ToLowerInvariant().Contains(search))
						|| (it.FileName != null && it.FileName.ToLowerInvariant().Contains(search))
						|| (it.ResolutionOrType != null && it.ResolutionOrType.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search))
						|| (it.FullFilePath != null && it.FullFilePath.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ClientSource);
				lvi.SubItems.Add(it.FileName);
				lvi.SubItems.Add(it.Category.ToString());
				lvi.SubItems.Add(it.ResolutionOrType);
				lvi.SubItems.Add(string.Format("{0:0.0} КБ", it.SizeBytes / 1024.0));
				lvi.SubItems.Add(it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
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
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotForensicItem it = listView.SelectedItems[0].Tag as ScreenshotForensicItem;
			if (it == null) return;

			// Preview image if not video and exists
			if (!it.IsVideo && File.Exists(it.FullFilePath) && !it.IsDisguised)
			{
				try
				{
					if (picPreview.Image != null)
					{
						picPreview.Image.Dispose();
						picPreview.Image = null;
					}

					using (FileStream fs = new FileStream(it.FullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					{
						Image original = Image.FromStream(fs);
						picPreview.Image = new Bitmap(original);
						it.ResolutionOrType = string.Format("{0}x{1} PNG", original.Width, original.Height);
					}
				}
				catch
				{
					picPreview.Image = null;
				}
			}
			else
			{
				if (picPreview.Image != null)
				{
					picPreview.Image.Dispose();
					picPreview.Image = null;
				}
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ СКРИНШОТА / МЕДИАФАЙЛА ===");
			sb.AppendLine("Клиент / Источник: " + it.ClientSource);
			sb.AppendLine("Имя файла: " + it.FileName);
			sb.AppendLine("Категория: " + it.Category);
			sb.AppendLine("Формат / Разрешение: " + it.ResolutionOrType);
			sb.AppendLine("Размер: " + it.SizeBytes + " байт");
			sb.AppendLine("Дата создания файла: " + it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Полный путь: " + it.FullFilePath);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт регламента сервера: " + it.TargetRule);
			sb.AppendLine();
			sb.AppendLine("--- Подробный протокол проверки ---");
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void OpenFileOrFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotForensicItem it = listView.SelectedItems[0].Tag as ScreenshotForensicItem;
			if (it == null || string.IsNullOrEmpty(it.FullFilePath)) return;

			try
			{
				if (File.Exists(it.FullFilePath))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", it.FullFilePath));
				}
				else if (Directory.Exists(it.FullFilePath))
				{
					Process.Start("explorer.exe", it.FullFilePath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Ошибка открытия", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyFilePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotForensicItem it = listView.SelectedItems[0].Tag as ScreenshotForensicItem;
			if (it == null || string.IsNullOrEmpty(it.FullFilePath)) return;

			Clipboard.SetText(it.FullFilePath);
			MessageBox.Show("Путь скопирован в буфер:\n" + it.FullFilePath, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotForensicItem it = listView.SelectedItems[0].Tag as ScreenshotForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ ГРАФИЧЕСКОЙ УЛИКИ SCREENSHARE ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Клиент: " + it.ClientSource);
			sb.AppendLine("Файл: " + it.FileName);
			sb.AppendLine("Разрешение: " + it.ResolutionOrType);
			sb.AppendLine("Дата создания: " + it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Путь: " + it.FullFilePath);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Discord-отчет скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ScreenshotForensicItem it = listView.SelectedItems[0].Tag as ScreenshotForensicItem;
			if (it == null) return;

			string cmd;
			if (it.IsDisguised)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Маскировка чита под скриншот {1})", targetPlayerName, it.FileName);
			}
			else if (it.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Улика на скриншоте {1})", targetPlayerName, it.FileName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Медиаулика {1})", targetPlayerName, it.FileName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
