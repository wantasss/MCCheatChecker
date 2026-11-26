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
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ResourcePacksXrayViewerForm : Form
	{
		#region Models

		public enum PackFilterMode
		{
			All,
			ThreatsAndXray,
			ResourcePacksOnly,
			ShaderpacksOnly,
			SilentStepOnly
		}

		public class ResourcePackForensicItem
		{
			public string Name;
			public string PackType;      // "Ресурспак (.zip)", "Папка ресурспака", "Шейдерпак (.zip)", "Серверный кэш"
			public string FilePath;
			public long FileSize;
			public DateTime LastModified;
			public string Category;      // "X-Ray текстурпак", "Silent-Step звуки", "Штатный ресурспак"
			public string Status;        // Вердикт
			public bool IsThreat;
			public string RuleViolation; // "5.06", "5.01", "5.08", "-"
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private PackFilterMode currentFilter = PackFilterMode.All;
		private List<ResourcePackForensicItem> allEntries = new List<ResourcePackForensicItem>();
		private bool isScanning = false;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;
		private Label lblDetailVerdict;
		private Label lblDetailRule;
		private Label lblDetailBanCommand;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterThreats;
		private DarkButton btnFilterPacks;
		private DarkButton btnFilterShaders;
		private DarkButton btnFilterSilentStep;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenFolder;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex XrayKeywordsRegex = new Regex(
			@"(?i)(xray|x-ray|ore\s*finder|diamond\s*finder|wireframe|transparent\s*blocks|see\s*through)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ResourcePacksXrayViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Инспектор ресурспаков, шейдерпаков и скрытого X-Ray — MCCheatChecker";
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 580);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.KeyPreview = true;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartScanAsync();
					e.Handled = true;
				}
			};

			// Верхняя панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "💎 Инспектор ресурспаков, шейдерпаков и скрытых X-Ray модификаций";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Аудит .zip архивов на прозрачные модели камня/руды (stone.json), X-Ray шейдеры и эксплойт заглушения шагов в sounds.json (Silent-Step).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 42);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(260, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 280, 22);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Панель фильтров и поиска
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все паки", PackFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 X-Ray и эксплойты (5.06)", PackFilterMode.ThreatsAndXray, ref bx, filterBar);
			btnFilterPacks = CreateFilterButton("💎 Ресурспаки (.zip)", PackFilterMode.ResourcePacksOnly, ref bx, filterBar);
			btnFilterShaders = CreateFilterButton("🖼️ Шейдерпаки", PackFilterMode.ShaderpacksOnly, ref bx, filterBar);
			btnFilterSilentStep = CreateFilterButton("🔇 Silent-Step звуки", PackFilterMode.SilentStepOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 250, 8);
			searchBox.Size = new Size(235, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 305, 12);
			lblSearch.AutoSize = true;
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			filterBar.Controls.Add(lblSearch);

			// Нижняя панель действий
			Panel bottomBar = new Panel();
			bottomBar.Dock = DockStyle.Bottom;
			bottomBar.Height = 48;
			bottomBar.BackColor = Ui.Surface;
			bottomBar.Padding = new Padding(14, 8, 14, 8);
			this.Controls.Add(bottomBar);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Font = Ui.FontTitle(9f, true);
			btnCopyBan.Location = new Point(14, 10);
			btnCopyBan.Size = new Size(230, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📂 Открыть папку с паком";
			btnOpenFolder.Font = Ui.FontBody(9f, false);
			btnOpenFolder.Location = new Point(252, 10);
			btnOpenFolder.Size = new Size(210, 30);
			btnOpenFolder.Click += BtnOpenFolder_Click;
			bottomBar.Controls.Add(btnOpenFolder);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(470, 10);
			btnEvidenceCapture.Size = new Size(185, 30);
			btnEvidenceCapture.Click += delegate
			{
				try
				{
					EvidenceCaptureForm ev = new EvidenceCaptureForm(targetPlayerName, Environment.UserName);
					ev.Show(this);
				}
				catch { }
			};
			bottomBar.Controls.Add(btnEvidenceCapture);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📄 Экспорт отчета";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(663, 10);
			btnExportReport.Size = new Size(150, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 F5";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 90, 10);
			btnRefreshNow.Size = new Size(75, 30);
			btnRefreshNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			bottomBar.Controls.Add(btnRefreshNow);

			// SplitContainer
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 380;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Font = Ui.FontBody(9.5f, false);

			listView.Columns.Add("Имя ресурспака / шейдерпака", 260);
			listView.Columns.Add("Тип / Формат", 160);
			listView.Columns.Add("Каталог расположения", 380);
			listView.Columns.Add("Размер архива", 120);
			listView.Columns.Add("Вердикт / Регламент", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ ДАННЫЕ О РЕСУРСПАКЕ / ШЕЙДЕРАХ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из таблицы.";
			lblDetailVerdict.Font = Ui.FontTitle(10.5f, true);
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailVerdict.Location = new Point(16, 32);
			lblDetailVerdict.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailVerdict);

			lblDetailRule = new Label();
			lblDetailRule.Text = "";
			lblDetailRule.Font = Ui.FontTitle(9f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Location = new Point(16, 56);
			lblDetailRule.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailRule);

			lblDetailBanCommand = new Label();
			lblDetailBanCommand.Text = "";
			lblDetailBanCommand.Font = Ui.FontMono(9f, true);
			lblDetailBanCommand.ForeColor = Ui.AccentTeal;
			lblDetailBanCommand.Location = new Point(16, 78);
			lblDetailBanCommand.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailBanCommand);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(16, 104);
			txtDetails.Size = new Size(pnlDetailsContainer.Width - 32, pnlDetailsContainer.Height - 114);
			txtDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			txtDetails.Font = Ui.FontMono(9f, false);
			txtDetails.ReadOnly = true;
			pnlDetailsContainer.Controls.Add(txtDetails);
		}

		private DarkButton CreateFilterButton(string text, PackFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == PackFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(160, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 166;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == PackFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == PackFilterMode.ThreatsAndXray);
			btnFilterPacks.AccentStyle = (currentFilter == PackFilterMode.ResourcePacksOnly);
			btnFilterShaders.AccentStyle = (currentFilter == PackFilterMode.ShaderpacksOnly);
			btnFilterSilentStep.AccentStyle = (currentFilter == PackFilterMode.SilentStepOnly);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование паков...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<ResourcePackForensicItem> results = new List<ResourcePackForensicItem>();
				List<string> mcRoots = FindMinecraftRoots();

				foreach (string mcDir in mcRoots)
				{
					// 1. Папка resourcepacks
					string rpPath = Path.Combine(mcDir, "resourcepacks");
					if (Directory.Exists(rpPath))
					{
						ScanResourcePacksFolder(rpPath, results);
					}

					// 2. Папка shaderpacks
					string spPath = Path.Combine(mcDir, "shaderpacks");
					if (Directory.Exists(spPath))
					{
						ScanShaderPacksFolder(spPath, results);
					}

					// 3. Серверные кэши ресурспаков
					string srpPath = Path.Combine(mcDir, "server-resource-packs");
					if (Directory.Exists(srpPath))
					{
						ScanServerResourcePacksFolder(srpPath, results);
					}
				}

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(ResourcePackForensicItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 X-RAY И ЭКСПЛОЙТОВ: {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Паков проверено: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private List<string> FindMinecraftRoots()
		{
			List<string> dirs = new List<string>();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			string def = Path.Combine(appData, ".minecraft");
			if (Directory.Exists(def)) dirs.Add(def);

			string tl = Path.Combine(appData, @".tlauncher\legacy\Minecraft\game");
			if (Directory.Exists(tl)) dirs.Add(tl);

			string prism = Path.Combine(appData, "PrismLauncher");
			if (Directory.Exists(prism))
			{
				try
				{
					string[] insts = Directory.GetDirectories(Path.Combine(prism, "instances"));
					foreach (string inst in insts)
					{
						string instMc = Path.Combine(inst, ".minecraft");
						if (Directory.Exists(instMc)) dirs.Add(instMc);
					}
				}
				catch { }
			}

			return dirs;
		}

		private void ScanResourcePacksFolder(string folderPath, List<ResourcePackForensicItem> results)
		{
			try
			{
				string[] files = Directory.GetFiles(folderPath, "*.zip");
				foreach (string file in files)
				{
					ScanSingleZipResourcePack(file, results);
				}

				string[] subDirs = Directory.GetDirectories(folderPath);
				foreach (string subDir in subDirs)
				{
					ScanSingleFolderResourcePack(subDir, results);
				}
			}
			catch { }
		}

		private void ScanSingleZipResourcePack(string zipPath, List<ResourcePackForensicItem> results)
		{
			try
			{
				FileInfo fi = new FileInfo(zipPath);
				string fname = fi.Name;

				bool isXrayName = XrayKeywordsRegex.IsMatch(fname);
				bool hasTransparentStoneModel = false;
				bool hasSilentStep = false;
				string mcmetaDesc = "";

				List<string> transparentModels = new List<string>();

				using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						foreach (ZipArchiveEntry entry in zip.Entries)
						{
							string entryName = entry.FullName.ToLowerInvariant();

							// 1. Проверка pack.mcmeta
							if (entryName.Equals("pack.mcmeta", StringComparison.OrdinalIgnoreCase))
							{
								try
								{
									using (StreamReader sr = new StreamReader(entry.Open(), Encoding.UTF8))
									{
										mcmetaDesc = sr.ReadToEnd();
										if (XrayKeywordsRegex.IsMatch(mcmetaDesc))
										{
											isXrayName = true;
										}
									}
								}
								catch { }
							}

							// 2. Проверка моделей блоков на прозрачность (stone, deepslate, netherrack)
							if (entryName.Contains("models/block/") && (entryName.EndsWith("stone.json") || entryName.EndsWith("netherrack.json") || entryName.EndsWith("deepslate.json") || entryName.EndsWith("dirt.json")))
							{
								try
								{
									using (StreamReader sr = new StreamReader(entry.Open(), Encoding.UTF8))
									{
										string json = sr.ReadToEnd();
										if (json.Length < 120 || json.IndexOf("\"elements\"", StringComparison.OrdinalIgnoreCase) < 0 || json.IndexOf("transparent", StringComparison.OrdinalIgnoreCase) >= 0)
										{
											hasTransparentStoneModel = true;
											transparentModels.Add(Path.GetFileName(entryName));
										}
									}
								}
								catch { }
							}

							// 3. Проверка sounds.json на Silent-Step
							if (entryName.EndsWith("sounds.json"))
							{
								try
								{
									using (StreamReader sr = new StreamReader(entry.Open(), Encoding.UTF8))
									{
										string sJson = sr.ReadToEnd();
										if (sJson.Contains("step.stone") && (sJson.Contains("\"sounds\": []") || sJson.Contains("\"volume\": 0")))
										{
											hasSilentStep = true;
										}
									}
								}
								catch { }
							}
						}
					}
				}

				ResourcePackForensicItem item = new ResourcePackForensicItem();
				item.Name = fname;
				item.PackType = "Ресурспак (.zip)";
				item.FilePath = zipPath;
				item.FileSize = fi.Length;
				item.LastModified = fi.LastWriteTime;

				if (isXrayName || hasTransparentStoneModel)
				{
					item.Category = "X-Ray текстурпак (Прозрачные блоки)";
					item.Status = "🚨 X-RAY РЕСУРСПАК (5.06 / 5.03)";
					item.IsThreat = true;
					item.RuleViolation = "5.06";
					item.BanReason = string.Format("Использование X-Ray текстурпака: '{0}'", fname);
					item.ForeColor = Ui.AccentRed;
					item.Details = string.Format("ОБНАРУЖЕН X-RAY РЕСУРСПАК С ПРОЗРАЧНЫМИ БЛОКАМИ!\nФайл: {0}\nПуть: {1}\nРазмер: {2:N0} КБ\nМодели X-Ray: {3}\nОписание pack.mcmeta:\n{4}\nНарушение: Пункт регламента 5.06 (Поиск руд, X-Ray) / 5.03.",
						fname, zipPath, fi.Length / 1024, string.Join(", ", transparentModels.ToArray()), mcmetaDesc);
					results.Add(item);
				}
				else if (hasSilentStep)
				{
					item.Category = "Эксплойт заглушения шагов (Silent-Step)";
					item.Status = "🚨 SILENT-STEP ЭКСПЛОЙТ В SOUNDS.JSON (5.01)";
					item.IsThreat = true;
					item.RuleViolation = "5.01";
					item.BanReason = "Заглушение звуков шагов в sounds.json (Silent-Step)";
					item.ForeColor = Ui.AccentRed;
					item.Details = string.Format("В sounds.json обнаружена модификация звуков шагов (бесшумный бег без Shift)!\nФайл: {0}\nНарушение: Пункт регламента 5.01 (Модификации преимущества).", fname);
					results.Add(item);
				}
				else
				{
					item.Category = "Легитимный ресурспак";
					item.Status = "Штатный ресурспак (Чист)";
					item.RuleViolation = "-";
					item.BanReason = "-";
					item.ForeColor = Ui.TextSecondary;
					item.Details = string.Format("Файл: {0}\nПуть: {1}\nРазмер: {2:N0} КБ\nМодели блоков соответствуют стандарту.", fname, zipPath, fi.Length / 1024);
					results.Add(item);
				}
			}
			catch { }
		}

		private void ScanSingleFolderResourcePack(string dirPath, List<ResourcePackForensicItem> results)
		{
			try
			{
				DirectoryInfo di = new DirectoryInfo(dirPath);
				string dname = di.Name;

				bool isXray = XrayKeywordsRegex.IsMatch(dname);
				string mcmetaPath = Path.Combine(dirPath, "pack.mcmeta");
				string mcmetaContent = "";

				if (File.Exists(mcmetaPath))
				{
					try
					{
						mcmetaContent = File.ReadAllText(mcmetaPath, Encoding.UTF8);
						if (XrayKeywordsRegex.IsMatch(mcmetaContent)) isXray = true;
					}
					catch { }
				}

				ResourcePackForensicItem item = new ResourcePackForensicItem();
				item.Name = dname;
				item.PackType = "Папка ресурспака";
				item.FilePath = dirPath;
				item.FileSize = 0;
				item.LastModified = di.LastWriteTime;

				if (isXray)
				{
					item.Category = "X-Ray ресурспак в папке";
					item.Status = "🚨 X-RAY РЕСУРСПАК (5.06 / 5.03)";
					item.IsThreat = true;
					item.RuleViolation = "5.06";
					item.BanReason = string.Format("X-Ray текстурпак: '{0}'", dname);
					item.ForeColor = Ui.AccentRed;
					item.Details = string.Format("ОБНАРУЖЕН X-RAY ТЕКСТУРПАК В ПАПКЕ RESOURCEPACKS!\nПапка: {0}\nПуть: {1}\nОписание mcmeta: {2}\nНарушение: Пункт регламента 5.06.", dname, dirPath, mcmetaContent);
					results.Add(item);
				}
				else
				{
					item.Category = "Папка ресурспака";
					item.Status = "Штатный ресурспак (Чист)";
					item.RuleViolation = "-";
					item.BanReason = "-";
					item.ForeColor = Ui.TextSecondary;
					item.Details = string.Format("Папка: {0}\nПуть: {1}", dname, dirPath);
					results.Add(item);
				}
			}
			catch { }
		}

		private void ScanShaderPacksFolder(string spPath, List<ResourcePackForensicItem> results)
		{
			try
			{
				string[] files = Directory.GetFiles(spPath, "*.zip");
				foreach (string file in files)
				{
					FileInfo fi = new FileInfo(file);
					bool isXray = XrayKeywordsRegex.IsMatch(fi.Name);

					ResourcePackForensicItem item = new ResourcePackForensicItem();
					item.Name = fi.Name;
					item.PackType = "Шейдерпак (.zip)";
					item.FilePath = file;
					item.FileSize = fi.Length;
					item.LastModified = fi.LastWriteTime;

					if (isXray)
					{
						item.Category = "X-Ray шейдерпак";
						item.Status = "🚨 X-RAY ШЕЙДЕРЫ (5.06)";
						item.IsThreat = true;
						item.RuleViolation = "5.06";
						item.BanReason = string.Format("Использование X-Ray шейдерпака: '{0}'", fi.Name);
						item.ForeColor = Ui.AccentRed;
						item.Details = string.Format("ОБНАРУЖЕН X-RAY ШЕЙДЕРПАК!\nФайл: {0}\nПуть: {1}\nНарушение: Пункт регламента 5.06.", fi.Name, file);
						results.Add(item);
					}
					else
					{
						item.Category = "Шейдерпак";
						item.Status = "Легитимные шейдеры (Чисто)";
						item.RuleViolation = "-";
						item.BanReason = "-";
						item.ForeColor = Ui.TextSecondary;
						item.Details = string.Format("Шейдерпак: {0}\nРазмер: {1:N0} КБ", fi.Name, fi.Length / 1024);
						results.Add(item);
					}
				}
			}
			catch { }
		}

		private void ScanServerResourcePacksFolder(string srpPath, List<ResourcePackForensicItem> results)
		{
			try
			{
				string[] files = Directory.GetFiles(srpPath);
				foreach (string file in files)
				{
					FileInfo fi = new FileInfo(file);
					ResourcePackForensicItem item = new ResourcePackForensicItem();
					item.Name = fi.Name;
					item.PackType = "Серверный ресурспак";
					item.FilePath = file;
					item.FileSize = fi.Length;
					item.LastModified = fi.LastWriteTime;
					item.Category = "Кэш серверного ресурспака";
					item.Status = "Серверный кэш (Чист)";
					item.RuleViolation = "-";
					item.BanReason = "-";
					item.ForeColor = Ui.TextSecondary;
					item.Details = string.Format("Хэш серверного пака: {0}\nРазмер: {1:N0} КБ", fi.Name, fi.Length / 1024);
					results.Add(item);
				}
			}
			catch { }
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (ResourcePackForensicItem item in allEntries)
			{
				if (currentFilter == PackFilterMode.ThreatsAndXray && !item.IsThreat) continue;
				if (currentFilter == PackFilterMode.ResourcePacksOnly && !item.PackType.Contains("Ресурспак")) continue;
				if (currentFilter == PackFilterMode.ShaderpacksOnly && !item.PackType.Contains("Шейдерпак")) continue;
				if (currentFilter == PackFilterMode.SilentStepOnly && !item.Category.Contains("Silent-Step")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.Name != null && item.Name.ToLowerInvariant().Contains(search)) ||
								   (item.PackType != null && item.PackType.ToLowerInvariant().Contains(search)) ||
								   (item.FilePath != null && item.FilePath.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Name);
				lvi.SubItems.Add(item.PackType);
				lvi.SubItems.Add(item.FilePath);
				lvi.SubItems.Add(item.FileSize > 0 ? string.Format("{0:N0} КБ", item.FileSize / 1024) : "—");
				lvi.SubItems.Add(item.Status);
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
				ClearDetails();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetails();
				return;
			}

			ResourcePackForensicItem item = listView.SelectedItems[0].Tag as ResourcePackForensicItem;
			if (item == null) return;

			lblDetailVerdict.Text = item.Status;
			lblDetailVerdict.ForeColor = item.ForeColor;

			if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
			{
				lblDetailRule.Text = string.Format("⚖️ Пункт регламента: {0} ({1})", item.RuleViolation, item.BanReason);
				lblDetailBanCommand.Text = string.Format("📋 Команда: /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			}
			else
			{
				lblDetailRule.Text = "🛡️ Нарушений не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details ?? "Нет сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Запись не выбрана";
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailRule.Text = "";
			lblDetailBanCommand.Text = "";
			txtDetails.Text = "";
		}

		#endregion

		#region Actions

		private void BtnCopyBan_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите ресурспак с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ResourcePackForensicItem item = listView.SelectedItems[0].Tag as ResourcePackForensicItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранного пака нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenFolder_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ResourcePackForensicItem item = listView.SelectedItems[0].Tag as ResourcePackForensicItem;
			if (item == null || string.IsNullOrEmpty(item.FilePath)) return;

			try
			{
				string folder = Directory.Exists(item.FilePath) ? item.FilePath : Path.GetDirectoryName(item.FilePath);
				if (Directory.Exists(folder))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", item.FilePath));
				}
			}
			catch { }
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Ресурспаки_XRay_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА РЕСУРСПАКОВ И X-RAY");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (ResourcePackForensicItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.PackType, item.Name));
						sb.AppendLine(string.Format("Путь:        {0}", item.FilePath));
						sb.AppendLine(string.Format("Вердикт:     {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", item.Details));
						sb.AppendLine(new string('-', 80));
					}

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("Отчет успешно сохранен:\n" + sfd.FileName, "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при экспорте отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
