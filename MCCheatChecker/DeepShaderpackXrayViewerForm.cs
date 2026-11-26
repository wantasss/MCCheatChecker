using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DeepShaderpackXrayViewerForm : Form
	{
		#region Enums & Models

		public enum ShaderXrayCategory
		{
			GlslDiscardBlockXray501,
			XrayMarkerDetected501,
			SelectiveAlphaTerrain501,
			SuspiciousCustomTerrainShader,
			CleanShaderpack
		}

		public enum ShaderXrayFilter
		{
			All,
			ViolationsOnly,
			GlslDiscard,
			XrayMarkers,
			SelectiveAlpha,
			CleanShaderpacks
		}

		public class ShaderXrayItem
		{
			public int Index;
			public ShaderXrayCategory Category;
			public string CategoryName;
			public string ShaderpackName;
			public string ShaderpackPath;
			public string EntryFileName;
			public string TriggerReason;
			public string DetectedCodeSnippet;
			public int LineNumberInShader;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsDiscard;
			public bool IsMarker;
			public Color RowColor;
			public string ForensicReport;
			public string GlslCodeContext;
			public string FileStructureDetails;
			public DateTime LastModified;
			public long FileSizeBytes;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ShaderXrayFilter currentFilter = ShaderXrayFilter.All;
		private List<ShaderXrayItem> allItems = new List<ShaderXrayItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtGlslContext;
		private DarkTextBoxMultiline txtFileStructure;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiDiscard;
		private Label lblKpiMarkers;
		private Label lblKpiSelectiveAlpha;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterDiscard;
		private DarkButton btnFilterMarkers;
		private DarkButton btnFilterAlpha;
		private DarkButton btnFilterClean;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public DeepShaderpackXrayViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🔮 Инспектор шейдеров на скрытый X-Ray (GLSL Discard и подсветку руд) — MCCheatChecker";
			Size = new Size(1340, 820);
			MinimumSize = new Size(1100, 640);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;

			// Header Panel
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔮 ИНСПЕКТОР ШЕЙДЕРОВ НА СКРЫТЫЙ X-RAY (GLSL DISCARD)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит GLSL шейдеров (.fsh/.vsh/.glsl): выявление скрытого отбрасывания блоков (discard;), подсветки руд и маркеров X-Ray (5.01)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ШЕЙДЕРОВ...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(250, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Container
			Panel topContainer = new Panel();
			topContainer.Dock = DockStyle.Top;
			topContainer.Height = 138;
			topContainer.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("🔮 Всего шейдеров", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01 X-Ray)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💎 GLSL Discard блоков", "0", Ui.AccentRed, ref kpiX, out lblKpiDiscard));
			kpiPanel.Controls.Add(CreateKpiBadge("🏷️ Маркеров X-Ray / Cave", "0", Ui.AccentOrange, ref kpiX, out lblKpiMarkers));
			kpiPanel.Controls.Add(CreateKpiBadge("⚠️ Избирательной прозрачности", "0", Ui.AccentOrange, ref kpiX, out lblKpiSelectiveAlpha));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Чистых шейдерпаков", "0", Ui.AccentGreen, ref kpiX, out lblKpiClean));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все шейдеры", ShaderXrayFilter.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01)", ShaderXrayFilter.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterDiscard = CreateFilterButton("💎 GLSL Discard", ShaderXrayFilter.GlslDiscard, ref fBtnX, filterBar);
			btnFilterMarkers = CreateFilterButton("🏷️ Маркеры X-Ray", ShaderXrayFilter.XrayMarkers, ref fBtnX, filterBar);
			btnFilterAlpha = CreateFilterButton("⚠️ Прозрачность", ShaderXrayFilter.SelectiveAlpha, ref fBtnX, filterBar);
			btnFilterClean = CreateFilterButton("✔️ Чистые шейдеры", ShaderXrayFilter.CleanShaderpacks, ref fBtnX, filterBar);

			// Search Box
			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(fBtnX + 16, 13);
			filterBar.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(fBtnX + 78, 10);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Controls.Add(filterBar);

			// SplitContainer
			SplitContainer splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 5;
			splitContainer.BackColor = Ui.Border;

			// DarkListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;

			listView.Columns.Add("№", 45, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 155, HorizontalAlignment.Left);
			listView.Columns.Add("Имя шейдерпака", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Файл шейдера (.fsh/.vsh)", 210, HorizontalAlignment.Left);
			listView.Columns.Add("Причина срабатывания", 180, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 280, HorizontalAlignment.Left);
			listView.Columns.Add("Время изменения", 140, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate { UpdateDetailViews(); };
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Container
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(12, 7, 12, 7);

			btnCopySelected = new DarkButton();
			btnCopySelected.Text = "📋 Копировать запись";
			btnCopySelected.Size = new Size(160, 28);
			btnCopySelected.Location = new Point(12, 8);
			btnCopySelected.Click += delegate { CopySelectedRow(); };
			actionBar.Controls.Add(btnCopySelected);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Команда бана (/ban)";
			btnCopyBanCmd.Size = new Size(185, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(566, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(724, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			btnClose = new DarkButton();
			btnClose.Text = "✖️ Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(887, 8);
			btnClose.Click += delegate { Close(); };
			actionBar.Controls.Add(btnClose);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Регламент");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabCode = new TabPage("🔍 Исходный GLSL фрагмент с нарушением");
			tabCode.BackColor = Ui.Surface;
			txtGlslContext = new DarkTextBoxMultiline();
			txtGlslContext.Dock = DockStyle.Fill;
			txtGlslContext.ReadOnly = true;
			txtGlslContext.Font = Ui.FontMono(9f);
			tabCode.Controls.Add(txtGlslContext);
			detailTabs.TabPages.Add(tabCode);

			TabPage tabStruct = new TabPage("📁 Структура и параметры шейдерпака");
			tabStruct.BackColor = Ui.Surface;
			txtFileStructure = new DarkTextBoxMultiline();
			txtFileStructure.Dock = DockStyle.Fill;
			txtFileStructure.ReadOnly = true;
			txtFileStructure.Font = Ui.FontMono(9f);
			tabStruct.Controls.Add(txtFileStructure);
			detailTabs.TabPages.Add(tabStruct);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(170, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.SurfaceAlt;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(164, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(164, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 178;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, ShaderXrayFilter cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8f);
			btn.Size = new Size(150, 28);
			btn.Location = new Point(curX, 8);
			btn.Click += delegate
			{
				currentFilter = cat;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.BackColor = (currentFilter == ShaderXrayFilter.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == ShaderXrayFilter.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == ShaderXrayFilter.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == ShaderXrayFilter.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterDiscard.BackColor = (currentFilter == ShaderXrayFilter.GlslDiscard) ? Ui.AccentRed : Ui.Surface;
			btnFilterDiscard.ForeColor = (currentFilter == ShaderXrayFilter.GlslDiscard) ? Color.White : Ui.TextPrimary;

			btnFilterMarkers.BackColor = (currentFilter == ShaderXrayFilter.XrayMarkers) ? Ui.AccentOrange : Ui.Surface;
			btnFilterMarkers.ForeColor = (currentFilter == ShaderXrayFilter.XrayMarkers) ? Color.Black : Ui.TextPrimary;

			btnFilterAlpha.BackColor = (currentFilter == ShaderXrayFilter.SelectiveAlpha) ? Ui.AccentOrange : Ui.Surface;
			btnFilterAlpha.ForeColor = (currentFilter == ShaderXrayFilter.SelectiveAlpha) ? Color.Black : Ui.TextPrimary;

			btnFilterClean.BackColor = (currentFilter == ShaderXrayFilter.CleanShaderpacks) ? Ui.AccentGreen : Ui.Surface;
			btnFilterClean.ForeColor = (currentFilter == ShaderXrayFilter.CleanShaderpacks) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ШЕЙДЕРОВ...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ShaderXrayItem> results = new List<ShaderXrayItem>();
			int itemIndex = 1;

			List<string> shaderFolders = new List<string>();

			try
			{
				foreach (string root in ManualChecks.MinecraftRoots())
				{
					try
					{
						string sp = Path.Combine(root, "shaderpacks");
						if (Directory.Exists(sp) && !shaderFolders.Contains(sp)) shaderFolders.Add(sp);

						string instances = Path.Combine(root, "instances");
						if (Directory.Exists(instances))
						{
							try
							{
								string[] instDirs = Directory.GetDirectories(instances);
								foreach (string id in instDirs)
								{
									string isp1 = Path.Combine(id, "shaderpacks");
									if (Directory.Exists(isp1) && !shaderFolders.Contains(isp1)) shaderFolders.Add(isp1);
									string isp2 = Path.Combine(id, ".minecraft", "shaderpacks");
									if (Directory.Exists(isp2) && !shaderFolders.Contains(isp2)) shaderFolders.Add(isp2);
								}
							}
							catch { }
						}
					}
					catch { }
				}
			}
			catch { }

			// Analyze each shaderpack directory
			foreach (string spFolder in shaderFolders)
			{
				try
				{
					string[] zipFiles = Directory.GetFiles(spFolder, "*.zip");
					foreach (string zipPath in zipFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(zipPath);
							bool packHasViolation = false;

							using (ZipArchive archive = ZipFile.OpenRead(zipPath))
							{
								foreach (ZipArchiveEntry entry in archive.Entries)
								{
									string entryName = entry.FullName.ToLowerInvariant();
									if (!entryName.EndsWith(".fsh") && !entryName.EndsWith(".vsh") && !entryName.EndsWith(".glsl"))
									{
										continue;
									}

									// Target terrain, textures, entities, or composite
									if (!entryName.Contains("terrain") && !entryName.Contains("textured") &&
										!entryName.Contains("solid") && !entryName.Contains("composite") &&
										!entryName.Contains("deferred") && !entryName.Contains("entities"))
									{
										continue;
									}

									using (Stream s = entry.Open())
									using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
									{
										string code = sr.ReadToEnd();
										string lowerCode = code.ToLowerInvariant();

										bool hasDiscard = lowerCode.Contains("discard;");
										bool hasXrayMarker = lowerCode.Contains("xray") || lowerCode.Contains("x-ray") ||
															 lowerCode.Contains("ore_glow") || lowerCode.Contains("cavefinder") ||
															 lowerCode.Contains("outline_ore") || lowerCode.Contains("wallhack");

										bool isSelectiveBlockDiscard = hasDiscard &&
											(entryName.Contains("gbuffers_terrain") || entryName.Contains("gbuffers_textured")) &&
											(lowerCode.Contains("blockid") || lowerCode.Contains("mc_entity") ||
											 lowerCode.Contains("stone") || lowerCode.Contains("dirt") || lowerCode.Contains("deepslate"));

										if (hasXrayMarker && hasDiscard)
										{
											packHasViolation = true;
											results.Add(CreateItem(ref itemIndex, ShaderXrayCategory.GlslDiscardBlockXray501,
												fi, entry.FullName, "X-Ray логика с отбрасыванием блоков (discard;)",
												ExtractSnippet(code, "discard;"), "5.01 Использование читов",
												"🚨 5.01 X-RAY ШЕЙДЕР: Обнаружен полнофункциональный X-Ray с инструкцией discard!"));
											break;
										}
										else if (hasXrayMarker)
										{
											packHasViolation = true;
											results.Add(CreateItem(ref itemIndex, ShaderXrayCategory.XrayMarkerDetected501,
												fi, entry.FullName, "Обнаружены маркеры X-Ray / Cavefinder в коде",
												ExtractSnippet(code, "xray"), "5.01 Использование читов",
												"🚨 5.01 X-RAY МАРКЕР: В коде шейдера найдены маркеры подсветки руд / рентгена!"));
											break;
										}
										else if (isSelectiveBlockDiscard)
										{
											packHasViolation = true;
											results.Add(CreateItem(ref itemIndex, ShaderXrayCategory.SelectiveAlphaTerrain501,
												fi, entry.FullName, "Избирательный discard блоков ландшафта (Камень/Земля)",
												ExtractSnippet(code, "discard;"), "5.01 Использование читов",
												"⚠️ 5.01 ПОДОЗРИТЕЛЬНО: Избирательное отбрасывание камня/земли в gbuffers_terrain!"));
											break;
										}
									}
								}
							}

							if (!packHasViolation)
							{
								results.Add(CreateItem(ref itemIndex, ShaderXrayCategory.CleanShaderpack,
									fi, "shaders/", "Все GLSL модули чисты", "—", "Allowed",
									"✔️ Чистый шейдерпак (X-Ray не обнаружен)"));
							}
						}
						catch { }
					}
				}
				catch { }
			}

			e.Result = results;
		}

		private static ShaderXrayItem CreateItem(ref int idx, ShaderXrayCategory cat, FileInfo fi, string entry, string trigger, string snippet, string rule, string verdict)
		{
			bool isV = (cat == ShaderXrayCategory.GlslDiscardBlockXray501 ||
						cat == ShaderXrayCategory.XrayMarkerDetected501 ||
						cat == ShaderXrayCategory.SelectiveAlphaTerrain501);

			Color rowCol;
			if (cat == ShaderXrayCategory.GlslDiscardBlockXray501) rowCol = Ui.AccentRed;
			else if (cat == ShaderXrayCategory.XrayMarkerDetected501) rowCol = Ui.AccentRed;
			else if (cat == ShaderXrayCategory.SelectiveAlphaTerrain501) rowCol = Ui.AccentOrange;
			else rowCol = Ui.AccentGreen;

			ShaderXrayItem item = new ShaderXrayItem
			{
				Index = idx++,
				Category = cat,
				CategoryName = GetCategoryName(cat),
				ShaderpackName = fi.Name,
				ShaderpackPath = fi.FullName,
				EntryFileName = entry,
				TriggerReason = trigger,
				DetectedCodeSnippet = snippet,
				LineNumberInShader = 1,
				TargetRule = rule,
				Verdict = verdict,
				IsViolation = isV,
				IsDiscard = (cat == ShaderXrayCategory.GlslDiscardBlockXray501 || cat == ShaderXrayCategory.SelectiveAlphaTerrain501),
				IsMarker = (cat == ShaderXrayCategory.XrayMarkerDetected501),
				RowColor = rowCol,
				LastModified = fi.LastWriteTime,
				FileSizeBytes = fi.Length
			};

			item.ForensicReport = BuildForensicReport(item);
			item.GlslCodeContext = BuildGlslContext(item);
			item.FileStructureDetails = BuildFileStructure(item);

			return item;
		}

		private static string GetCategoryName(ShaderXrayCategory cat)
		{
			switch (cat)
			{
				case ShaderXrayCategory.GlslDiscardBlockXray501: return "GLSL Discard X-Ray";
				case ShaderXrayCategory.XrayMarkerDetected501: return "Маркер X-Ray в коде";
				case ShaderXrayCategory.SelectiveAlphaTerrain501: return "Избирательный Discard";
				default: return "Чистый шейдерпак";
			}
		}

		private static string ExtractSnippet(string fullCode, string keyword)
		{
			if (string.IsNullOrEmpty(fullCode)) return "Код недоступен";
			int idx = fullCode.ToLowerInvariant().IndexOf(keyword.ToLowerInvariant());
			if (idx < 0) return fullCode.Length > 200 ? fullCode.Substring(0, 200) + "..." : fullCode;

			int start = Math.Max(0, idx - 100);
			int length = Math.Min(fullCode.Length - start, 300);
			return fullCode.Substring(start, length).Trim();
		}

		private static string BuildForensicReport(ShaderXrayItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: АУДИТ ШЕЙДЕРОВ НА СКРЫТЫЙ X-RAY (GLSL DISCARD) ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Шейдерпак: {0}", item.ShaderpackName));
			sb.AppendLine(string.Format("Полный путь: {0}", item.ShaderpackPath));
			sb.AppendLine(string.Format("Файл шейдера: {0}", item.EntryFileName));
			sb.AppendLine(string.Format("Причина срабатывания: {0}", item.TriggerReason));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsViolation)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 (ИСПОЛЬЗОВАНИЕ ЧИТОВ):");
				sb.AppendLine("В исходном коде шейдера ландшафта (GLSL) обнаружена инструкция discard;");
				sb.AppendLine("или маркеры рентгеновского зрения (X-Ray / Cavefinder).");
				sb.AppendLine("Инструкция discard отбрасывает пиксели камня, земли и окружающих блоков,");
				sb.AppendLine("делая мир полностью прозрачным и обнажая руды, сундуки и пещеры.");
				sb.AppendLine("Использование читерских шейдеров для поиска ресурсов запрещено правилами 5.01.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование X-Ray шейдеров ({1})", "Player", item.ShaderpackName));
			}
			else
			{
				sb.AppendLine("Шейдерпак полностью проверен. Инструкций отбрасывания блоков не обнаружено.");
			}

			return sb.ToString();
		}

		private static string BuildGlslContext(ShaderXrayItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ФРАГМЕНТ ИСХОДНОГО GLSL КОДА ШЕЙДЕРА ===");
			sb.AppendLine(string.Format("Шейдерпак: {0}", item.ShaderpackName));
			sb.AppendLine(string.Format("Файл:      {0}", item.EntryFileName));
			sb.AppendLine();
			sb.AppendLine("ОБНАРУЖЕННЫЙ ФРАГМЕНТ:");
			sb.AppendLine(item.DetectedCodeSnippet);
			sb.AppendLine();
			sb.AppendLine("ПОЯСНЕНИЕ УЛИКИ:");
			if (item.IsDiscard)
			{
				sb.AppendLine("Инструкция discard прерывает обработку текущего пикселя (фрагмента) на GPU.");
				sb.AppendLine("В результате текстура блока не рендерится, и сквозь него видны все руды и структуры.");
			}
			else if (item.IsMarker)
			{
				sb.AppendLine("Маркеры X-Ray прямо свидетельствуют о том, что шейдерпак спроектирован для подсветки руд.");
			}
			else
			{
				sb.AppendLine("Штатный шейдер рендеринга мира Minecraft.");
			}

			return sb.ToString();
		}

		private static string BuildFileStructure(ShaderXrayItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПАРАМЕТРЫ ФАЙЛА И СТРУКТУРА ШЕЙДЕРПАКА ===");
			sb.AppendLine(string.Format("Имя архива:   {0}", item.ShaderpackName));
			sb.AppendLine(string.Format("Полный путь:  {0}", item.ShaderpackPath));
			sb.AppendLine(string.Format("Размер файла: {0:N0} байт", item.FileSizeBytes));
			sb.AppendLine(string.Format("Время изменения: {0:dd.MM.yyyy HH:mm:ss}", item.LastModified));

			return sb.ToString();
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<ShaderXrayItem>;
			if (allItems == null)
			{
				allItems = new List<ShaderXrayItem>();
			}

			int total = allItems.Count;
			int violations = 0;
			int discardCount = 0;
			int markerCount = 0;
			int alphaCount = 0;
			int clean = 0;

			foreach (ShaderXrayItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.Category == ShaderXrayCategory.GlslDiscardBlockXray501) discardCount++;
				if (item.Category == ShaderXrayCategory.XrayMarkerDetected501) markerCount++;
				if (item.Category == ShaderXrayCategory.SelectiveAlphaTerrain501) alphaCount++;
				if (item.Category == ShaderXrayCategory.CleanShaderpack) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiDiscard.Text = discardCount.ToString();
			lblKpiMarkers.Text = markerCount.ToString();
			lblKpiSelectiveAlpha.Text = alphaCount.ToString();
			lblKpiClean.Text = clean.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО X-RAY ШЕЙДЕРОВ: {0}", violations);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО ШЕЙДЕРОВ: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (ShaderXrayItem item in allItems)
			{
				if (currentFilter == ShaderXrayFilter.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == ShaderXrayFilter.GlslDiscard && !item.IsDiscard)
				{
					continue;
				}
				if (currentFilter == ShaderXrayFilter.XrayMarkers && !item.IsMarker)
				{
					continue;
				}
				if (currentFilter == ShaderXrayFilter.SelectiveAlpha && item.Category != ShaderXrayCategory.SelectiveAlphaTerrain501)
				{
					continue;
				}
				if (currentFilter == ShaderXrayFilter.CleanShaderpacks && item.IsViolation)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.ShaderpackName.ToLowerInvariant().Contains(search) ||
								 item.EntryFileName.ToLowerInvariant().Contains(search) ||
								 item.TriggerReason.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.ShaderpackName);
				lvi.SubItems.Add(item.EntryFileName);
				lvi.SubItems.Add(item.TriggerReason);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.LastModified.ToString("dd.MM.yyyy HH:mm:ss"));

				lvi.Tag = item;
				lvi.ForeColor = item.RowColor;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtForensicReport.Clear();
				txtGlslContext.Clear();
				txtFileStructure.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			ShaderXrayItem item = listView.SelectedItems[0].Tag as ShaderXrayItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtGlslContext.Text = item.GlslCodeContext;
			txtFileStructure.Text = item.FileStructureDetails;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите шейдер для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ShaderXrayItem item = listView.SelectedItems[0].Tag as ShaderXrayItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} ({2}) | Причина: {3} | {4}",
				item.CategoryName, item.ShaderpackName, item.EntryFileName, item.TriggerReason, item.Verdict);

			Clipboard.SetText(text);
			MessageBox.Show("Запись успешно скопирована в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования отчета Discord.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ShaderXrayItem item = listView.SelectedItems[0].Tag as ShaderXrayItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [ШЕЙДЕРПАК: ЛЕГИТИМНО]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Шейдерпак: {0}", item.ShaderpackName));
			sb.AppendLine(string.Format("Модуль: {0}", item.EntryFileName));
			sb.AppendLine(string.Format("Причина: {0}", item.TriggerReason));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (X-Ray Шейдеры)"));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord успешно скопирован в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением для генерации команды бана.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ShaderXrayItem item = listView.SelectedItems[0].Tag as ShaderXrayItem;
			if (item == null) return;

			string cmd = string.Format("/ban {0} 30d 5.01 Использование читов (X-Ray шейдерпак {1})", targetPlayerName, item.ShaderpackName);

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_ShaderXray_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза X-Ray шейдеров — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background:#0F1115; color:#ECF0F5; font-family:'Segoe UI',Arial,sans-serif; margin:20px; }");
					sb.AppendLine("h1 { color:#2EE6D6; font-size:20px; border-bottom:1px solid #262B33; padding-bottom:10px; }");
					sb.AppendLine(".kpi-container { display:flex; gap:12px; margin:15px 0; }");
					sb.AppendLine(".kpi-card { background:#171A1F; border:1px solid #262B33; padding:10px 14px; border-radius:6px; min-width:140px; }");
					sb.AppendLine(".kpi-title { font-size:11px; color:#8C96A3; }");
					sb.AppendLine(".kpi-val { font-size:18px; font-weight:bold; margin-top:4px; }");
					sb.AppendLine("table { width:100%; border-collapse:collapse; margin-top:15px; font-size:12px; }");
					sb.AppendLine("th, td { border:1px solid #262B33; padding:8px 10px; text-align:left; }");
					sb.AppendLine("th { background:#171A1F; color:#4FC3F7; }");
					sb.AppendLine("tr:nth-child(even) { background:#121418; }");
					sb.AppendLine(".crit { color:#EF5350; font-weight:bold; }");
					sb.AppendLine(".warn { color:#FFA726; font-weight:bold; }");
					sb.AppendLine(".ok { color:#66BB6A; }");
					sb.AppendLine("</style></head><body>");

					sb.AppendLine("<h1>🔮 Судебный протокол аудита шейдеров на скрытый X-Ray (GLSL Discard)</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего шейдеров</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.01)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>GLSL Discard блоков</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiDiscard.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Маркеров X-Ray / Cave</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiMarkers.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Прозрачность</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiSelectiveAlpha.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Чистых шейдерпаков</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiClean.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Шейдерпак</th><th>Файл</th><th>Причина</th><th>Регламент</th><th>Вердикт</th><th>Время изменения</th></tr>");

					foreach (ShaderXrayItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : "";
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td class='{8}'>{6}</td><td>{7:dd.MM.yyyy HH:mm}</td></tr>",
							item.Index, item.CategoryName, item.ShaderpackName, item.EntryFileName, item.TriggerReason,
							item.TargetRule, item.Verdict, item.LastModified, cssClass));
					}

					sb.AppendLine("</table>");
					sb.AppendLine("</body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					Process.Start(sfd.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при сохранении HTML отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
