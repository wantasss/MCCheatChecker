using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ZipSteganographyAndModPayloadViewerForm : Form
	{
		#region Enums & Models

		public enum ZipPayloadCategory
		{
			EmbeddedPeExecutable501,
			EmbeddedHiddenZipJar501,
			EmbeddedClassBytecode501,
			CheatSignatureInTail501,
			SuspiciousTrailingData508,
			CleanValidArchive,
			CorruptedArchive
		}

		public enum ZipPayloadFilter
		{
			All,
			ViolationsOnly,
			EmbeddedPe,
			EmbeddedZip,
			EmbeddedZipJar,
			SuspiciousTails,
			CleanArchives
		}

		public class ZipSteganographyItem
		{
			public int Index;
			public ZipPayloadCategory Category;
			public string CategoryName;
			public string FileName;
			public string FilePath;
			public long TotalFileSize;
			public long LogicalZipSize;
			public long TrailingBytesCount;
			public string PayloadType;
			public string PayloadHexDump;
			public string PayloadAsciiPreview;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsEmbeddedPe;
			public bool IsEmbeddedZip;
			public Color RowColor;
			public string ForensicReport;
			public string BinaryDetails;
			public string FileAttributesInfo;
			public DateTime LastModified;
			public string Sha256Hash;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ZipPayloadFilter currentFilter = ZipPayloadFilter.All;
		private List<ZipSteganographyItem> allItems = new List<ZipSteganographyItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtBinaryDetails;
		private DarkTextBoxMultiline txtFileAttributes;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiEmbeddedPe;
		private Label lblKpiEmbeddedZip;
		private Label lblKpiSuspicious;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterPe;
		private DarkButton btnFilterZip;
		private DarkButton btnFilterSuspicious;
		private DarkButton btnFilterClean;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public ZipSteganographyAndModPayloadViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "📦 Инспектор стеганографии в модах, ZIP EOCD и скрытых нагрузок — MCCheatChecker";
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
			lblHeaderTitle.Text = "📦 ИНСПЕКТОР СТЕГАНОГРАФИИ В МОДАХ, ZIP EOCD И СКРЫТЫХ НАГРУЗОК";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит структуры ZIP-архивов .jar/.zip, выявление полезной нагрузки за пределами End of Central Directory (EOCD) и внедренных PE/DLL (5.01 / 5.08)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ СТРУКТУРЫ МОДОВ...";
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
			kpiPanel.Controls.Add(CreateKpiBadge("📦 Всего архивов (.jar/.zip)", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01/5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Внедренных PE (.exe/.dll)", "0", Ui.AccentRed, ref kpiX, out lblKpiEmbeddedPe));
			kpiPanel.Controls.Add(CreateKpiBadge("🗜️ Скрытых ZIP/JAR в хвосте", "0", Ui.AccentOrange, ref kpiX, out lblKpiEmbeddedZip));
			kpiPanel.Controls.Add(CreateKpiBadge("⚠️ Хвостов > 16 байт", "0", Ui.AccentOrange, ref kpiX, out lblKpiSuspicious));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Чистых валидных модов", "0", Ui.AccentGreen, ref kpiX, out lblKpiClean));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все архивы", ZipPayloadFilter.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", ZipPayloadFilter.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterPe = CreateFilterButton("💉 Внедренные PE (.dll/.exe)", ZipPayloadFilter.EmbeddedPe, ref fBtnX, filterBar);
			btnFilterZip = CreateFilterButton("🗜️ Скрытые ZIP/JAR", ZipPayloadFilter.EmbeddedZip, ref fBtnX, filterBar);
			btnFilterSuspicious = CreateFilterButton("⚠️ Хвосты > 16 байт", ZipPayloadFilter.SuspiciousTails, ref fBtnX, filterBar);
			btnFilterClean = CreateFilterButton("✔️ Чистые моды", ZipPayloadFilter.CleanArchives, ref fBtnX, filterBar);

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
			listView.Columns.Add("Имя файла мода / Архива", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Размер файла", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Скрытый хвост (байт)", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Тип полезной нагрузки", 210, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 280, HorizontalAlignment.Left);

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

			TabPage tabBinary = new TabPage("🔍 Дамп скрытой нагрузки (Hex / ASCII)");
			tabBinary.BackColor = Ui.Surface;
			txtBinaryDetails = new DarkTextBoxMultiline();
			txtBinaryDetails.Dock = DockStyle.Fill;
			txtBinaryDetails.ReadOnly = true;
			txtBinaryDetails.Font = Ui.FontMono(9f);
			tabBinary.Controls.Add(txtBinaryDetails);
			detailTabs.TabPages.Add(tabBinary);

			TabPage tabAttrs = new TabPage("📁 Файловая система и хеш SHA-256");
			tabAttrs.BackColor = Ui.Surface;
			txtFileAttributes = new DarkTextBoxMultiline();
			txtFileAttributes.Dock = DockStyle.Fill;
			txtFileAttributes.ReadOnly = true;
			txtFileAttributes.Font = Ui.FontMono(9f);
			tabAttrs.Controls.Add(txtFileAttributes);
			detailTabs.TabPages.Add(tabAttrs);

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

		private DarkButton CreateFilterButton(string text, ZipPayloadFilter cat, ref int curX, Panel parent)
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
			btnFilterAll.BackColor = (currentFilter == ZipPayloadFilter.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == ZipPayloadFilter.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == ZipPayloadFilter.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == ZipPayloadFilter.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterPe.BackColor = (currentFilter == ZipPayloadFilter.EmbeddedPe) ? Ui.AccentRed : Ui.Surface;
			btnFilterPe.ForeColor = (currentFilter == ZipPayloadFilter.EmbeddedPe) ? Color.White : Ui.TextPrimary;

			btnFilterZip.BackColor = (currentFilter == ZipPayloadFilter.EmbeddedZip) ? Ui.AccentOrange : Ui.Surface;
			btnFilterZip.ForeColor = (currentFilter == ZipPayloadFilter.EmbeddedZip) ? Color.Black : Ui.TextPrimary;

			btnFilterSuspicious.BackColor = (currentFilter == ZipPayloadFilter.SuspiciousTails) ? Ui.AccentOrange : Ui.Surface;
			btnFilterSuspicious.ForeColor = (currentFilter == ZipPayloadFilter.SuspiciousTails) ? Color.Black : Ui.TextPrimary;

			btnFilterClean.BackColor = (currentFilter == ZipPayloadFilter.CleanArchives) ? Ui.AccentGreen : Ui.Surface;
			btnFilterClean.ForeColor = (currentFilter == ZipPayloadFilter.CleanArchives) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ СТРУКТУРЫ МОДОВ...";
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
			List<ZipSteganographyItem> results = new List<ZipSteganographyItem>();
			int itemIndex = 1;

			List<string> candidateFiles = new List<string>();

			try
			{
				foreach (string root in ManualChecks.MinecraftRoots())
				{
					try
					{
						// Check mods folder
						string modsDir = Path.Combine(root, "mods");
						if (Directory.Exists(modsDir))
						{
							string[] jarFiles = Directory.GetFiles(modsDir, "*.jar", SearchOption.AllDirectories);
							foreach (string f in jarFiles) { if (!candidateFiles.Contains(f)) candidateFiles.Add(f); }
							string[] zipFiles = Directory.GetFiles(modsDir, "*.zip", SearchOption.AllDirectories);
							foreach (string f in zipFiles) { if (!candidateFiles.Contains(f)) candidateFiles.Add(f); }
						}

						// Check resourcepacks folder
						string rpDir = Path.Combine(root, "resourcepacks");
						if (Directory.Exists(rpDir))
						{
							string[] zipFiles = Directory.GetFiles(rpDir, "*.zip", SearchOption.TopDirectoryOnly);
							foreach (string f in zipFiles) { if (!candidateFiles.Contains(f)) candidateFiles.Add(f); }
						}

						// Check shaderpacks folder
						string spDir = Path.Combine(root, "shaderpacks");
						if (Directory.Exists(spDir))
						{
							string[] zipFiles = Directory.GetFiles(spDir, "*.zip", SearchOption.TopDirectoryOnly);
							foreach (string f in zipFiles) { if (!candidateFiles.Contains(f)) candidateFiles.Add(f); }
						}

						// Check instances (MultiMC, PrismLauncher)
						string instances = Path.Combine(root, "instances");
						if (Directory.Exists(instances))
						{
							try
							{
								string[] instDirs = Directory.GetDirectories(instances);
								foreach (string inst in instDirs)
								{
									string instMods = Path.Combine(inst, ".minecraft", "mods");
									if (!Directory.Exists(instMods)) instMods = Path.Combine(inst, "mods");
									if (Directory.Exists(instMods))
									{
										string[] jars = Directory.GetFiles(instMods, "*.jar", SearchOption.AllDirectories);
										foreach (string f in jars) { if (!candidateFiles.Contains(f)) candidateFiles.Add(f); }
									}
								}
							}
							catch { }
						}
					}
					catch { }
				}
			}
			catch { }

			// Analyze each candidate archive
			foreach (string filePath in candidateFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(filePath);
					if (!fi.Exists || fi.Length < 22 || fi.Length > 300 * 1024 * 1024)
					{
						continue;
					}

					long fileLen = fi.Length;
					long logicalZipEnd = -1;
					long trailingBytes = 0;
					byte[] trailingSample = null;
					string payloadType = "Отсутствует (Чистый архив)";
					ZipPayloadCategory cat = ZipPayloadCategory.CleanValidArchive;

					using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						int searchLen = (int)Math.Min(65558L, fileLen);
						byte[] buf = new byte[searchLen];
						fs.Seek(fileLen - searchLen, SeekOrigin.Begin);
						fs.Read(buf, 0, searchLen);

						long eocdOffsetInFile = -1;
						int commentLen = 0;

						// Search backwards for EOCD signature PK\x05\x06 (0x50, 0x4B, 0x05, 0x06)
						for (int i = searchLen - 22; i >= 0; i--)
						{
							if (buf[i] == 0x50 && buf[i + 1] == 0x4B && buf[i + 2] == 0x05 && buf[i + 3] == 0x06)
							{
								int cLen = buf[i + 20] | (buf[i + 21] << 8);
								eocdOffsetInFile = (fileLen - searchLen) + i;
								commentLen = cLen;
								break;
							}
						}

						if (eocdOffsetInFile >= 0)
						{
							logicalZipEnd = eocdOffsetInFile + 22 + commentLen;
							trailingBytes = fileLen - logicalZipEnd;

							if (trailingBytes > 16)
							{
								fs.Seek(logicalZipEnd, SeekOrigin.Begin);
								int sampleLen = (int)Math.Min(512L, trailingBytes);
								trailingSample = new byte[sampleLen];
								fs.Read(trailingSample, 0, sampleLen);

								string asciiText = Encoding.ASCII.GetString(trailingSample);
								string lowerAscii = asciiText.ToLowerInvariant();

								// Check Magic Bytes
								if (sampleLen >= 2 && trailingSample[0] == 0x4D && trailingSample[1] == 0x5A)
								{
									// 'MZ' header
									cat = ZipPayloadCategory.EmbeddedPeExecutable501;
									payloadType = "Внедрённый PE исполняемый файл (.EXE / .DLL)";
								}
								else if (sampleLen >= 4 && trailingSample[0] == 0x50 && trailingSample[1] == 0x4B && trailingSample[2] == 0x03 && trailingSample[3] == 0x04)
								{
									// 'PK\x03\x04' header
									cat = ZipPayloadCategory.EmbeddedHiddenZipJar501;
									payloadType = "Внедрённый скрытый вторичный ZIP/JAR архив";
								}
								else if (sampleLen >= 4 && trailingSample[0] == 0xCA && trailingSample[1] == 0xFE && trailingSample[2] == 0xBA && trailingSample[3] == 0xBE)
								{
									// 0xCAFEBABE Java bytecode
									cat = ZipPayloadCategory.EmbeddedClassBytecode501;
									payloadType = "Скомпилированный байт-код класса Java (.class)";
								}
								else if (lowerAscii.Contains("celestial") || lowerAscii.Contains("expensive") ||
										 lowerAscii.Contains("nursultan") || lowerAscii.Contains("vape") ||
										 lowerAscii.Contains("slinky") || lowerAscii.Contains("drip") ||
										 lowerAscii.Contains("akrien") || lowerAscii.Contains("kura") ||
										 lowerAscii.Contains("flint") || lowerAscii.Contains("boberware"))
								{
									cat = ZipPayloadCategory.CheatSignatureInTail501;
									payloadType = "Обнаружена строковая сигнатура чита в скрытом хвосте";
								}
								else
								{
									cat = ZipPayloadCategory.SuspiciousTrailingData508;
									payloadType = string.Format("Неизвестные бинарные данные ({0} байт)", trailingBytes);
								}
							}
						}
						else
						{
							cat = ZipPayloadCategory.CorruptedArchive;
							payloadType = "Сигнатура EOCD не найдена (Поврежден или зашифрован)";
						}
					}

					bool isV = (cat == ZipPayloadCategory.EmbeddedPeExecutable501 ||
								cat == ZipPayloadCategory.EmbeddedHiddenZipJar501 ||
								cat == ZipPayloadCategory.EmbeddedClassBytecode501 ||
								cat == ZipPayloadCategory.CheatSignatureInTail501 ||
								cat == ZipPayloadCategory.SuspiciousTrailingData508);

					string rule = isV ? (cat == ZipPayloadCategory.SuspiciousTrailingData508 ? "5.08 Сокрытие читов" : "5.01 Использование читов") : "Allowed";

					string verdict;
					Color rowCol;

					if (cat == ZipPayloadCategory.EmbeddedPeExecutable501)
					{
						verdict = "🚨 5.01 / 5.08 КРИТИЧЕСКОЕ: Внедрен PE-исполняемый файл (.EXE/.DLL) за пределами ZIP!";
						rowCol = Ui.AccentRed;
					}
					else if (cat == ZipPayloadCategory.EmbeddedHiddenZipJar501)
					{
						verdict = "🚨 5.01 / 5.08 КРИТИЧЕСКОЕ: Внедрен вторичный скрытый JAR/ZIP архив в хвосте мода!";
						rowCol = Ui.AccentRed;
					}
					else if (cat == ZipPayloadCategory.EmbeddedClassBytecode501)
					{
						verdict = "🚨 5.01 КРИТИЧЕСКОЕ: Внедрен Java-байткод (.class) за границей ZIP-каталога!";
						rowCol = Ui.AccentRed;
					}
					else if (cat == ZipPayloadCategory.CheatSignatureInTail501)
					{
						verdict = "🚨 5.01 КРИТИЧЕСКОЕ: Сигнатура чита обнаружена в скрытых данных после EOCD!";
						rowCol = Ui.AccentRed;
					}
					else if (cat == ZipPayloadCategory.SuspiciousTrailingData508)
					{
						verdict = string.Format("⚠️ 5.08 ПОДОЗРИТЕЛЬНО: Мод содержит {0} байт скрытых данных после конца архива", trailingBytes);
						rowCol = Ui.AccentOrange;
					}
					else if (cat == ZipPayloadCategory.CorruptedArchive)
					{
						verdict = "⚠️ Нестандартный заголовок архива";
						rowCol = Ui.TextSecondary;
					}
					else
					{
						verdict = "✔️ Валидная структура ZIP, скрытых данных нет";
						rowCol = Ui.AccentGreen;
					}

					ZipSteganographyItem item = new ZipSteganographyItem
					{
						Index = itemIndex++,
						Category = cat,
						CategoryName = GetCategoryName(cat),
						FileName = fi.Name,
						FilePath = fi.FullName,
						TotalFileSize = fileLen,
						LogicalZipSize = logicalZipEnd > 0 ? logicalZipEnd : fileLen,
						TrailingBytesCount = trailingBytes,
						PayloadType = payloadType,
						PayloadHexDump = FormatHexDump(trailingSample),
						PayloadAsciiPreview = FormatAsciiPreview(trailingSample),
						TargetRule = rule,
						Verdict = verdict,
						IsViolation = isV,
						IsEmbeddedPe = (cat == ZipPayloadCategory.EmbeddedPeExecutable501),
						IsEmbeddedZip = (cat == ZipPayloadCategory.EmbeddedHiddenZipJar501),
						RowColor = rowCol,
						LastModified = fi.LastWriteTime,
						Sha256Hash = isV ? ComputeSha256(fi.FullName) : "—"
					};

					item.ForensicReport = BuildForensicReport(item);
					item.BinaryDetails = BuildBinaryDetails(item);
					item.FileAttributesInfo = BuildFileAttributes(item, fi);

					results.Add(item);
				}
				catch { }
			}

			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<ZipSteganographyItem>;
			if (allItems == null)
			{
				allItems = new List<ZipSteganographyItem>();
			}

			int total = allItems.Count;
			int violations = 0;
			int embeddedPe = 0;
			int embeddedZip = 0;
			int suspicious = 0;
			int clean = 0;

			foreach (ZipSteganographyItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.IsEmbeddedPe) embeddedPe++;
				if (item.IsEmbeddedZip) embeddedZip++;
				if (item.Category == ZipPayloadCategory.SuspiciousTrailingData508) suspicious++;
				if (item.Category == ZipPayloadCategory.CleanValidArchive) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiEmbeddedPe.Text = embeddedPe.ToString();
			lblKpiEmbeddedZip.Text = embeddedZip.ToString();
			lblKpiSuspicious.Text = suspicious.ToString();
			lblKpiClean.Text = clean.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО НАРУШЕНИЙ: {0}", violations);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО МОДОВ: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Helpers & Formats

		private static string GetCategoryName(ZipPayloadCategory cat)
		{
			switch (cat)
			{
				case ZipPayloadCategory.EmbeddedPeExecutable501: return "Внедрен PE (.exe/.dll)";
				case ZipPayloadCategory.EmbeddedHiddenZipJar501: return "Скрытый ZIP/JAR";
				case ZipPayloadCategory.EmbeddedClassBytecode501: return "Java Байт-код (.class)";
				case ZipPayloadCategory.CheatSignatureInTail501: return "Сигнатура чита в хвосте";
				case ZipPayloadCategory.SuspiciousTrailingData508: return "Подозрительный хвост";
				case ZipPayloadCategory.CorruptedArchive: return "Поврежденный архив";
				default: return "Чистый мод";
			}
		}

		private static string FormatHexDump(byte[] data)
		{
			if (data == null || data.Length == 0) return "Данные отсутствуют";
			StringBuilder sb = new StringBuilder();
			int len = Math.Min(data.Length, 256);
			for (int i = 0; i < len; i += 16)
			{
				sb.AppendFormat("{0:X4}: ", i);
				for (int j = 0; j < 16; j++)
				{
					if (i + j < len)
						sb.AppendFormat("{0:X2} ", data[i + j]);
					else
						sb.Append("   ");
				}
				sb.Append(" | ");
				for (int j = 0; j < 16; j++)
				{
					if (i + j < len)
					{
						byte b = data[i + j];
						sb.Append((b >= 32 && b <= 126) ? (char)b : '.');
					}
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		private static string FormatAsciiPreview(byte[] data)
		{
			if (data == null || data.Length == 0) return "—";
			StringBuilder sb = new StringBuilder();
			foreach (byte b in data)
			{
				if (b >= 32 && b <= 126) sb.Append((char)b);
				else if (b == '\r' || b == '\n' || b == '\t') sb.Append(' ');
				else sb.Append('.');
			}
			return sb.ToString();
		}

		private static string ComputeSha256(string filePath)
		{
			try
			{
				using (SHA256 sha = SHA256.Create())
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					byte[] hash = sha.ComputeHash(fs);
					StringBuilder sb = new StringBuilder();
					foreach (byte b in hash) sb.Append(b.ToString("x2"));
					return sb.ToString();
				}
			}
			catch { }
			return "Недоступен";
		}

		#endregion

		#region Report Builders

		private static string BuildForensicReport(ZipSteganographyItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: СТЕГАНОГРАФИЯ В МОДАХ MINECRAFT (ZIP EOCD) ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория объекта: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Имя файла мода: {0}", item.FileName));
			sb.AppendLine(string.Format("Полный путь: {0}", item.FilePath));
			sb.AppendLine(string.Format("Общий размер файла: {0:N0} байт", item.TotalFileSize));
			sb.AppendLine(string.Format("Логический размер ZIP архива: {0:N0} байт", item.LogicalZipSize));
			sb.AppendLine(string.Format("Размер скрытого хвоста (Payload): {0:N0} байт", item.TrailingBytesCount));
			sb.AppendLine(string.Format("Тип скрытой полезной нагрузки: {0}", item.PayloadType));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsViolation)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 / 5.08:");
				sb.AppendLine("Обнаружена скрытая полезная нагрузка за пределами служебной записи");
				sb.AppendLine("End of Central Directory (EOCD, сигнатура PK\\x05\\x06).");
				sb.AppendLine("Спецификация ZIP/JAR определяет, что любые данные, расположенные после");
				sb.AppendLine("конца каталога архива, игнорируются стандартными архиваторами (WinRAR/7-Zip)");
				sb.AppendLine("и стандартным загрузчиком Java ClassLoader.");
				sb.AppendLine("Данная техника (ZIP Steganography Overlay) используется разработчиками читов для:");
				sb.AppendLine(" • Скрытного внедрения исполняемых модулей (.DLL / .EXE) под видом легитимного мода");
				sb.AppendLine(" • Упаковки вторичного чит-мода внутрь обычного OptiFine/Sodium");
				sb.AppendLine(" • Обхода статического анализа и экранов ручной проверки модераторами");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d {1} ({2})", "Player", item.TargetRule, item.PayloadType));
			}
			else
			{
				sb.AppendLine("Файл полностью соответствует спецификации ZIP/JAR.");
				sb.AppendLine("Скрытых данных и оверлеев за пределами EOCD не зафиксировано.");
			}

			return sb.ToString();
		}

		private static string BuildBinaryDetails(ZipSteganographyItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СТРУКТУРНЫЙ И БИНАРНЫЙ ДАМП СКРЫТОЙ НАГРУЗКИ ===");
			sb.AppendLine(string.Format("Смещение EOCD:        0x{0:X8} ({0} байт)", item.LogicalZipSize));
			sb.AppendLine(string.Format("Размер оверлея:       {0} байт", item.TrailingBytesCount));
			sb.AppendLine(string.Format("Тип полезной нагрузки: {0}", item.PayloadType));
			sb.AppendLine();
			sb.AppendLine("HEX ДАМП ПЕРВЫХ 256 БАЙТ СКРЫТОГО ХВОСТА:");
			sb.AppendLine(item.PayloadHexDump);
			sb.AppendLine();
			sb.AppendLine("ТЕКСТОВЫЙ ASCII ПРЕДПРОСМОТР:");
			sb.AppendLine(item.PayloadAsciiPreview);

			return sb.ToString();
		}

		private static string BuildFileAttributes(ZipSteganographyItem item, FileInfo fi)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ФАЙЛОВЫЕ АТРИБУТЫ И ХЕШИ ===");
			sb.AppendLine(string.Format("Имя файла:         {0}", item.FileName));
			sb.AppendLine(string.Format("Путь к файлу:      {0}", item.FilePath));
			sb.AppendLine(string.Format("Хеш SHA-256:       {0}", item.Sha256Hash));
			sb.AppendLine();

			if (fi != null && fi.Exists)
			{
				sb.AppendLine(string.Format("Физический размер: {0:N0} байт", fi.Length));
				sb.AppendLine(string.Format("Время создания:    {0:dd.MM.yyyy HH:mm:ss}", fi.CreationTime));
				sb.AppendLine(string.Format("Время изменения:   {0:dd.MM.yyyy HH:mm:ss}", fi.LastWriteTime));
				sb.AppendLine(string.Format("Время доступа:     {0:dd.MM.yyyy HH:mm:ss}", fi.LastAccessTime));
				sb.AppendLine(string.Format("Атрибуты:          {0}", fi.Attributes));
			}

			return sb.ToString();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (ZipSteganographyItem item in allItems)
			{
				if (currentFilter == ZipPayloadFilter.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == ZipPayloadFilter.EmbeddedPe && !item.IsEmbeddedPe)
				{
					continue;
				}
				if (currentFilter == ZipPayloadFilter.EmbeddedZip && !item.IsEmbeddedZip)
				{
					continue;
				}
				if (currentFilter == ZipPayloadFilter.SuspiciousTails && item.TrailingBytesCount <= 16)
				{
					continue;
				}
				if (currentFilter == ZipPayloadFilter.CleanArchives && item.IsViolation)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.FileName.ToLowerInvariant().Contains(search) ||
								 item.PayloadType.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(string.Format("{0:N0} КБ", item.TotalFileSize / 1024));
				lvi.SubItems.Add(item.TrailingBytesCount > 0 ? string.Format("{0:N0} байт", item.TrailingBytesCount) : "0");
				lvi.SubItems.Add(item.PayloadType);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);

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
				txtBinaryDetails.Clear();
				txtFileAttributes.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			ZipSteganographyItem item = listView.SelectedItems[0].Tag as ZipSteganographyItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtBinaryDetails.Text = item.BinaryDetails;
			txtFileAttributes.Text = item.FileAttributesInfo;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите файл для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			ZipSteganographyItem item = listView.SelectedItems[0].Tag as ZipSteganographyItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | Хвост: {2} байт | Нагрузка: {3} | {4}",
				item.CategoryName, item.FileName, item.TrailingBytesCount, item.PayloadType, item.Verdict);

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
			ZipSteganographyItem item = listView.SelectedItems[0].Tag as ZipSteganographyItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [СТЕГАНОГРАФИЯ В МОДАХ: ЛЕГИТИМНО]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Файл: {0}", item.FileName));
			sb.AppendLine(string.Format("Размер: {0:N0} байт | Скрытый хвост: {1:N0} байт", item.TotalFileSize, item.TrailingBytesCount));
			sb.AppendLine(string.Format("Тип нагрузки: {0}", item.PayloadType));
			sb.AppendLine(string.Format("Хеш SHA-256: {0}", item.Sha256Hash));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (ZIP Стеганография)"));
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
			ZipSteganographyItem item = listView.SelectedItems[0].Tag as ZipSteganographyItem;
			if (item == null) return;

			string cmd = "";
			if (item.IsEmbeddedPe)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Внедрен PE файл в мод {1})", targetPlayerName, item.FileName);
			}
			else if (item.IsEmbeddedZip)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Скрытый вторичный архив в моде {1})", targetPlayerName, item.FileName);
			}
			else if (item.IsViolation)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие софта (Скрытый хвост {1} байт в {2})", targetPlayerName, item.TrailingBytesCount, item.FileName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, item.TargetRule);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_ZipSteganography_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза стеганографии в модах — " + targetPlayerName + "</title>");
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

					sb.AppendLine("<h1>📦 Судебный протокол аудита стеганографии в модах Minecraft (ZIP EOCD Trailing Payload)</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего архивов</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.01/5.08)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Внедренных PE (.exe/.dll)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiEmbeddedPe.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Скрытых ZIP/JAR</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiEmbeddedZip.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Хвостов > 16 байт</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiSuspicious.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Чистых валидных модов</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiClean.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Имя файла</th><th>Размер</th><th>Хвост (байт)</th><th>Тип нагрузки</th><th>Регламент</th><th>Вердикт</th></tr>");

					foreach (ZipSteganographyItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : "";
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3:N0} КБ</td><td>{4:N0}</td><td>{5}</td><td>{6}</td><td class='{8}'>{7}</td></tr>",
							item.Index, item.CategoryName, item.FileName, item.TotalFileSize / 1024, item.TrailingBytesCount,
							item.PayloadType, item.TargetRule, item.Verdict, cssClass));
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
