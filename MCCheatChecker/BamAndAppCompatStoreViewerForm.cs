using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class BamAndAppCompatStoreViewerForm : Form
	{
		#region Enums & Data Models

		public enum BamStoreCategory
		{
			DeletedExecutable508,
			KnownCheatExecution501,
			SuspiciousTempExecution,
			CompatibilityLayerOverride,
			FeatureUsageExecution,
			LegitimateExecutable
		}

		public enum BamStoreFilterCategory
		{
			All,
			ViolationsOnly,
			DeletedFiles508,
			Cheats501,
			SacpStore,
			FeatureUsageAndBam,
			ExistingClean
		}

		public class BamStoreItem
		{
			public int Index;
			public BamStoreCategory Category;
			public string CategoryName;
			public string SourceDatabase;
			public string FileName;
			public string FullPath;
			public bool FileExistsOnDisk;
			public string DiskStatus;
			public DateTime ExecutionTime;
			public string ExecutionTimeDisplay;
			public string ExecutionDetails;
			public string SignatureOrVendor;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsDeleted;
			public bool IsCheat;
			public bool IsClean;
			public Color RowColor;
			public string ForensicReport;
			public string TechnicalDetails;
			public string CertificateDetails;
			public string Sha256Hash;
			public long FileSizeBytes;
			public DateTime FileCreationTime;
			public DateTime FileLastWriteTime;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private BamStoreFilterCategory currentFilter = BamStoreFilterCategory.All;
		private List<BamStoreItem> allItems = new List<BamStoreItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtTechnicalDetails;
		private DarkTextBoxMultiline txtSignatureDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiDeletedFiles;
		private Label lblKpiCheatExecutions;
		private Label lblKpiSacpStore;
		private Label lblKpiFeatureUsage;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterSacp;
		private DarkButton btnFilterFeatureUsage;
		private DarkButton btnFilterClean;

		private DarkButton btnCopyRow;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		private TabControl detailTabs;

		#endregion

		public BamAndAppCompatStoreViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "🛡️ Инспектор следов запуска SACP, BAM и FeatureUsage (Артефакты выполнения) — MCCheatChecker";
			Size = new Size(1320, 800);
			MinimumSize = new Size(1080, 620);
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
			lblHeaderTitle.Text = "🛡️ ИНСПЕКТОР СЛЕДОВ ЗАПУСКА SACP, BAM И FEATUREUSAGE (АРТЕФАКТЫ ВЫПОЛНЕНИЯ)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит баз Compatibility Assistant (SACP Store), Background Activity Moderator (BAM), FeatureUsage и фиксация удаленных файлов (5.01 / 5.08)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ БАЗ ЗАПУСКА...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(240, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1050, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Panel with Header + KPI
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 138;
			topPanel.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ Всего в базах", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01/5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("🗑️ Удалено файлов (5.08)", "0", Ui.AccentOrange, ref kpiX, out lblKpiDeletedFiles));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Запусков читов (5.01)", "0", Ui.AccentRed, ref kpiX, out lblKpiCheatExecutions));
			kpiPanel.Controls.Add(CreateKpiBadge("🏬 Записей SACP Store", "0", Ui.AccentBlue, ref kpiX, out lblKpiSacpStore));
			kpiPanel.Controls.Add(CreateKpiBadge("🔄 FeatureUsage / BAM", "0", Ui.AccentGreen, ref kpiX, out lblKpiFeatureUsage));
			topPanel.Controls.Add(kpiPanel);

			Controls.Add(topPanel);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все записи", BamStoreFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", BamStoreFilterCategory.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterDeleted = CreateFilterButton("🗑️ Удаленные (5.08)", BamStoreFilterCategory.DeletedFiles508, ref fBtnX, filterBar);
			btnFilterCheats = CreateFilterButton("💉 Читы (5.01)", BamStoreFilterCategory.Cheats501, ref fBtnX, filterBar);
			btnFilterSacp = CreateFilterButton("🏬 SACP Store", BamStoreFilterCategory.SacpStore, ref fBtnX, filterBar);
			btnFilterFeatureUsage = CreateFilterButton("🔄 FeatureUsage/BAM", BamStoreFilterCategory.FeatureUsageAndBam, ref fBtnX, filterBar);
			btnFilterClean = CreateFilterButton("✔️ Существующие", BamStoreFilterCategory.ExistingClean, ref fBtnX, filterBar);

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
			searchBox.Size = new Size(190, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Controls.Add(filterBar);

			// SplitContainer for ListView and Bottom Details
			DarkSplitContainer splitContainer = new DarkSplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 6;
			splitContainer.BackColor = Ui.Border;

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("#", 45);
			listView.Columns.Add("Источник базы", 175);
			listView.Columns.Add("Исполняемый файл", 210);
			listView.Columns.Add("Полный путь", 330);
			listView.Columns.Add("Наличие на диске / Время", 200);
			listView.Columns.Add("Регламент", 115);
			listView.Columns.Add("Криминалистический вердикт", 245);

			listView.SelectedIndexChanged += delegate { UpdateSelectedDetails(); };
			listView.DoubleClick += delegate { CopySelectedRowText(); };
			Ui.ApplyDarkScrollbars(listView);
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Panel
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			// Action Bar
			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.SurfaceAlt;
			actionBar.Padding = new Padding(12, 6, 12, 6);

			btnCopyRow = new DarkButton();
			btnCopyRow.Text = "📋 Копировать строку";
			btnCopyRow.Size = new Size(160, 28);
			btnCopyRow.Location = new Point(12, 8);
			btnCopyRow.Click += delegate { CopySelectedRowText(); };
			actionBar.Controls.Add(btnCopyRow);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyForDiscord(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🔨 Копировать команду бана";
			btnCopyBanCmd.Size = new Size(205, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(586, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(744, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Аналитика");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabTech = new TabPage("🔍 Технические детали & Данные реестра");
			tabTech.BackColor = Ui.Surface;
			txtTechnicalDetails = new DarkTextBoxMultiline();
			txtTechnicalDetails.Dock = DockStyle.Fill;
			txtTechnicalDetails.ReadOnly = true;
			txtTechnicalDetails.Font = Ui.FontMono(9f);
			tabTech.Controls.Add(txtTechnicalDetails);
			detailTabs.TabPages.Add(tabTech);

			TabPage tabSig = new TabPage("🔏 Цифровая подпись & Хеш");
			tabSig.BackColor = Ui.Surface;
			txtSignatureDetails = new DarkTextBoxMultiline();
			txtSignatureDetails.Dock = DockStyle.Fill;
			txtSignatureDetails.ReadOnly = true;
			txtSignatureDetails.Font = Ui.FontMono(9f);
			tabSig.Controls.Add(txtSignatureDetails);
			detailTabs.TabPages.Add(tabSig);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string caption, string initialVal, Color valColor, ref int curX, out Label valLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(140, 52);
			p.Location = new Point(curX, 4);
			p.BackColor = Ui.SurfaceAlt;
			p.Padding = new Padding(4, 4, 4, 4);

			Label lblCaption = new Label();
			lblCaption.Text = caption;
			lblCaption.Font = Ui.FontBody(7.5f);
			lblCaption.ForeColor = Ui.TextMuted;
			lblCaption.Dock = DockStyle.Top;
			lblCaption.Height = 16;
			lblCaption.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(lblCaption);

			valLabel = new Label();
			valLabel.Text = initialVal;
			valLabel.Font = Ui.FontTitle(11.5f, true);
			valLabel.ForeColor = valColor;
			valLabel.Dock = DockStyle.Fill;
			valLabel.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(valLabel);

			curX += 146;
			return p;
		}

		private DarkButton CreateFilterButton(string text, BamStoreFilterCategory category, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(130, 28);
			btn.Location = new Point(curX, 9);
			btn.Click += delegate
			{
				currentFilter = category;
				UpdateFilterButtonHighlights();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 136;
			return btn;
		}

		private void UpdateFilterButtonHighlights()
		{
			Color normalBg = Ui.Surface;
			Color activeBg = Ui.AccentBlue;
			Color activeText = Color.Black;
			Color normalText = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == BamStoreFilterCategory.All, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterViolations, currentFilter == BamStoreFilterCategory.ViolationsOnly, normalBg, Ui.AccentRed, normalText, Color.White);
			SetBtnStyle(btnFilterDeleted, currentFilter == BamStoreFilterCategory.DeletedFiles508, normalBg, Ui.AccentOrange, normalText, Color.Black);
			SetBtnStyle(btnFilterCheats, currentFilter == BamStoreFilterCategory.Cheats501, normalBg, Ui.AccentRed, normalText, Color.White);
			SetBtnStyle(btnFilterSacp, currentFilter == BamStoreFilterCategory.SacpStore, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterFeatureUsage, currentFilter == BamStoreFilterCategory.FeatureUsageAndBam, normalBg, Ui.AccentGreen, normalText, Color.Black);
			SetBtnStyle(btnFilterClean, currentFilter == BamStoreFilterCategory.ExistingClean, normalBg, Ui.AccentGreen, normalText, Color.Black);
		}

		private void SetBtnStyle(DarkButton btn, bool isActive, Color normBg, Color actBg, Color normFg, Color actFg)
		{
			if (btn == null) return;
			btn.BackColor = isActive ? actBg : normBg;
			btn.ForeColor = isActive ? actFg : normFg;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
				return;

			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ БАЗ ЗАПУСКА...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			listView.Items.Clear();
			allItems.Clear();
			txtForensicReport.Clear();
			txtTechnicalDetails.Clear();
			txtSignatureDetails.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformBamAndStoreScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования артефактов запуска: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.BackColor = Ui.AccentRed;
					lblStatusPill.ForeColor = Color.White;
					return;
				}

				List<BamStoreItem> items = e.Result as List<BamStoreItem>;
				if (items != null)
				{
					allItems = items;
				}

				UpdateKpisAndStatus();
				UpdateFilterButtonHighlights();
				ApplyFilter();
			};
			scanWorker.RunWorkerAsync();
		}

		private List<BamStoreItem> PerformBamAndStoreScan()
		{
			List<BamStoreItem> results = new List<BamStoreItem>();
			int idx = 1;
			HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Step 1: Parse HKCU Compatibility Assistant Store (SACP)
			ScanCompatibilityAssistantStore(ref idx, results, seenPaths, Registry.CurrentUser, @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", "🏬 SACP Store (HKCU)");

			// Step 2: Parse HKLM Compatibility Assistant Store (SACP)
			ScanCompatibilityAssistantStore(ref idx, results, seenPaths, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", "🏬 SACP Store (HKLM)");

			// Step 3: Parse AppCompatFlags Layers
			ScanAppCompatFlagsLayers(ref idx, results, seenPaths, Registry.CurrentUser, @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", "⚙️ Layers (HKCU)");
			ScanAppCompatFlagsLayers(ref idx, results, seenPaths, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", "⚙️ Layers (HKLM)");

			// Step 4: Parse Explorer FeatureUsage AppSwitched & ShowJumpView
			ScanFeatureUsage(ref idx, results, seenPaths, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", "🔄 FeatureUsage (AppSwitched)");
			ScanFeatureUsage(ref idx, results, seenPaths, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", "🔄 FeatureUsage (ShowJumpView)");

			// Step 5: Parse BAM / DAM if accessible
			ScanBamAndDam(ref idx, results, seenPaths);

			return results;
		}

		private void ScanCompatibilityAssistantStore(ref int idx, List<BamStoreItem> results, HashSet<string> seenPaths, RegistryKey rootKey, string subKeyPath, string sourceName)
		{
			try
			{
				using (RegistryKey key = rootKey.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string valName in valNames)
					{
						if (string.IsNullOrEmpty(valName) || valName.Length < 3) continue;

						byte[] rawData = key.GetValue(valName) as byte[];
						DateTime execTime = ExtractTimestampFromSacp(rawData);

						ProcessFileEntry(ref idx, results, seenPaths, valName, sourceName, execTime,
							string.Format("SACP запись: {0} байт", rawData != null ? rawData.Length : 0));
					}
				}
			}
			catch { }
		}

		private void ScanAppCompatFlagsLayers(ref int idx, List<BamStoreItem> results, HashSet<string> seenPaths, RegistryKey rootKey, string subKeyPath, string sourceName)
		{
			try
			{
				using (RegistryKey key = rootKey.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string valName in valNames)
					{
						if (string.IsNullOrEmpty(valName) || valName.Length < 3) continue;

						object valObj = key.GetValue(valName);
						string layerStr = (valObj != null) ? valObj.ToString() : "";

						ProcessFileEntry(ref idx, results, seenPaths, valName, sourceName, DateTime.MinValue,
							"Режим совместимости: " + layerStr);
					}
				}
			}
			catch { }
		}

		private void ScanFeatureUsage(ref int idx, List<BamStoreItem> results, HashSet<string> seenPaths, string subKeyPath, string sourceName)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string valName in valNames)
					{
						if (string.IsNullOrEmpty(valName) || valName.Length < 3) continue;

						// Filter out GUIDs or PID tags like *PID00003394 unless they are file paths
						string normalizedPath = NormalizeFeatureUsagePath(valName);
						if (string.IsNullOrEmpty(normalizedPath)) continue;

						object valObj = key.GetValue(valName);
						int switchCount = (valObj is int) ? (int)valObj : 0;

						ProcessFileEntry(ref idx, results, seenPaths, normalizedPath, sourceName, DateTime.MinValue,
							string.Format("Счетчик переключений фокуса: {0}", switchCount));
					}
				}
			}
			catch { }
		}

		private void ScanBamAndDam(ref int idx, List<BamStoreItem> results, HashSet<string> seenPaths)
		{
			string[] bamRoots = new string[]
			{
				@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\bam\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\dam\State\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\dam\UserSettings"
			};

			foreach (string bRoot in bamRoots)
			{
				try
				{
					using (RegistryKey root = Registry.LocalMachine.OpenSubKey(bRoot))
					{
						if (root == null) continue;
						foreach (string sid in root.GetSubKeyNames())
						{
							try
							{
								using (RegistryKey sidKey = root.OpenSubKey(sid))
								{
									if (sidKey == null) continue;
									foreach (string valName in sidKey.GetValueNames())
									{
										if (string.IsNullOrEmpty(valName) || valName.Length < 4) continue;

										string dosPath = NormalizeNtDevicePath(valName);
										byte[] rawBytes = sidKey.GetValue(valName) as byte[];
										DateTime execTime = DateTime.MinValue;
										if (rawBytes != null && rawBytes.Length >= 8)
										{
											long ft = BitConverter.ToInt64(rawBytes, 0);
											try { if (ft > 0) execTime = DateTime.FromFileTimeUtc(ft).ToLocalTime(); } catch { }
										}

										ProcessFileEntry(ref idx, results, seenPaths, dosPath, "🛡️ BAM/DAM (" + sid + ")", execTime, "Зафиксировано службой Background Activity Moderator");
									}
								}
							}
							catch { }
						}
					}
				}
				catch { }
			}
		}

		private void ProcessFileEntry(ref int idx, List<BamStoreItem> results, HashSet<string> seenPaths, string filePath, string sourceDb, DateTime execTime, string extraDetails)
		{
			string key = filePath.ToLowerInvariant();
			if (seenPaths.Contains(key)) return;
			seenPaths.Add(key);

			string fname = Path.GetFileName(filePath);
			string ext = Path.GetExtension(filePath).ToLowerInvariant();

			bool isExecutableType = (ext == ".exe" || ext == ".dll" || ext == ".jar" || ext == ".bat" || ext == ".cmd" || ext == ".ps1");
			if (!isExecutableType) return;

			bool exists = File.Exists(filePath);
			string lowerPath = filePath.ToLowerInvariant();

			// Whitelist check
			bool isAllowed = IsWhitelistedTool(fname, filePath);

			// Cheat check
			bool isKnownCheat = IsCheatMatch(fname, lowerPath);

			bool isSuspiciousDir = (lowerPath.Contains(@"\downloads\") || lowerPath.Contains(@"\appdata\local\temp\") ||
									lowerPath.Contains(@"\windows\temp\") || lowerPath.Contains(@"\desktop\"));

			string sha = "—";
			long fsize = 0;
			DateTime fCreate = DateTime.MinValue;
			DateTime fWrite = DateTime.MinValue;
			string cert = "—";

			if (exists)
			{
				try
				{
					FileInfo fi = new FileInfo(filePath);
					fsize = fi.Length;
					fCreate = fi.CreationTime;
					fWrite = fi.LastWriteTime;
					sha = ComputeSha256(filePath);
					cert = GetCertificateSummary(filePath);
				}
				catch { }
			}

			// Case 1: Whitelisted Tool (AnyDesk, RuDesktop, AutoClicker, etc.)
			if (isAllowed)
			{
				results.Add(new BamStoreItem
				{
					Index = idx++,
					Category = BamStoreCategory.LegitimateExecutable,
					CategoryName = "🟢 Белый список (Разрешено)",
					SourceDatabase = sourceDb,
					FileName = fname,
					FullPath = filePath,
					FileExistsOnDisk = exists,
					DiskStatus = exists ? "✔️ На диске" : "⚠️ Удален (Белый список)",
					ExecutionTime = execTime,
					ExecutionTimeDisplay = (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
					ExecutionDetails = extraDetails,
					SignatureOrVendor = cert,
					TargetRule = "Белый список",
					Verdict = "🟢 5.01 / 5.08 РАЗРЕШЕНО (ПО из белого списка)",
					IsViolation = false,
					IsClean = true,
					RowColor = Ui.AccentGreen,
					Sha256Hash = sha,
					FileSizeBytes = fsize,
					FileCreationTime = fCreate,
					FileLastWriteTime = fWrite,
					CertificateDetails = cert,
					TechnicalDetails = string.Format("Путь: {0}\r\nБаза: {1}\r\nПараметры: {2}\r\nПодпись: {3}", filePath, sourceDb, extraDetails, cert),
					ForensicReport = string.Format("Утилита {0} входит в официальный белый список проверок ScreenShare. Нарушений нет.", fname)
				});
				return;
			}

			// Case 2: Deleted Executable (Physical Disk Check = False)
			if (!exists)
			{
				// If deleted from Temp, Downloads, Desktop or is a known cheat -> 5.08 Tampering!
				bool isCritical508 = isKnownCheat || isSuspiciousDir;

				results.Add(new BamStoreItem
				{
					Index = idx++,
					Category = BamStoreCategory.DeletedExecutable508,
					CategoryName = isCritical508 ? "🚨 5.08 Удаленный файл" : "⚠️ Отсутствующий файл",
					SourceDatabase = sourceDb,
					FileName = fname,
					FullPath = filePath,
					FileExistsOnDisk = false,
					DiskStatus = "🚨 УДАЛЕН С ДИСКА (5.08)",
					ExecutionTime = execTime,
					ExecutionTimeDisplay = (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
					ExecutionDetails = extraDetails,
					SignatureOrVendor = "ФАЙЛ УДАЛЕН",
					TargetRule = isCritical508 ? "5.08 СОКРЫТИЕ УЛИК" : "5.08 ПРОВЕРКА",
					Verdict = isKnownCheat ?
						string.Format("🚨 5.01 / 5.08 УДАЛЕН ЧИТ-КЛИЕНТ ({0})", fname) :
						string.Format("🚨 5.08 СОКРЫТИЕ УЛИК (Удален {0})", fname),
					IsViolation = isCritical508,
					IsDeleted = true,
					IsCheat = isKnownCheat,
					RowColor = isCritical508 ? Ui.AccentRed : Ui.AccentOrange,
					TechnicalDetails = string.Format("Файл: {0}\r\nИсточник следа: {1}\r\nСтатус на диске: FALSE (УДАЛЕН)\r\nВремя запуска: {2}\r\nДетали: {3}",
						filePath, sourceDb, (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "—", extraDetails),
					ForensicReport = string.Format(
						"🚨 ОБНАРУЖЕНЫ СЛЕДЫ УДАЛЕНИЯ ИСПОЛНЯЕМОГО ФАЙЛА (5.08)!\r\n\r\n" +
						"Исполняемый файл: {0}\r\n" +
						"Полный путь запуска: {1}\r\n" +
						"База фиксации: {2}\r\n" +
						"Время запуска: {3}\r\n\r\n" +
						"Операционная система Windows сохранила профиль запуска (Compatibility Assistant / BAM / FeatureUsage), однако на момент проверки ScreenShare данный файл был удален игроком с диска!\r\n" +
						"Удаление исполняемых файлов из папок Загрузок/Temp/Рабочего стола перед проверкой квалифицируется как сокрытие улик.\r\n\r\n" +
						"Нарушение регламента: 5.08 (Сокрытие улик).",
						fname, filePath, sourceDb, (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "Не зафиксировано")
				});
				return;
			}

			// Case 3: Known Cheat Present on Disk
			if (isKnownCheat)
			{
				results.Add(new BamStoreItem
				{
					Index = idx++,
					Category = BamStoreCategory.KnownCheatExecution501,
					CategoryName = "🚨 5.01 Чит-клиент на диске",
					SourceDatabase = sourceDb,
					FileName = fname,
					FullPath = filePath,
					FileExistsOnDisk = true,
					DiskStatus = "🚨 СУЩЕСТВУЕТ (ЧИТ)",
					ExecutionTime = execTime,
					ExecutionTimeDisplay = (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
					ExecutionDetails = extraDetails,
					SignatureOrVendor = cert,
					TargetRule = "5.01 ЧИТЫ",
					Verdict = string.Format("🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ ({0})", fname),
					IsViolation = true,
					IsCheat = true,
					RowColor = Ui.AccentRed,
					Sha256Hash = sha,
					FileSizeBytes = fsize,
					FileCreationTime = fCreate,
					FileLastWriteTime = fWrite,
					CertificateDetails = cert,
					TechnicalDetails = string.Format("Файл: {0}\r\nSHA-256: {1}\r\nРазмер: {2:N0} байт\r\nСоздан: {3}\r\nИзменен: {4}",
						filePath, sha, fsize, fCreate, fWrite),
					ForensicReport = string.Format("В базах запуска зафиксирован старт известного чит-клиента '{0}'. 5.01 Использование читов.", fname)
				});
				return;
			}

			// Case 4: Normal Existing File
			results.Add(new BamStoreItem
			{
				Index = idx++,
				Category = BamStoreCategory.LegitimateExecutable,
				CategoryName = "✔️ Легитимное приложение",
				SourceDatabase = sourceDb,
				FileName = fname,
				FullPath = filePath,
				FileExistsOnDisk = true,
				DiskStatus = "✔️ На диске",
				ExecutionTime = execTime,
				ExecutionTimeDisplay = (execTime != DateTime.MinValue) ? execTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
				ExecutionDetails = extraDetails,
				SignatureOrVendor = cert,
				TargetRule = "—",
				Verdict = "✔️ Штатный запуск ПО",
				IsViolation = false,
				IsClean = true,
				RowColor = Ui.AccentGreen,
				Sha256Hash = sha,
				FileSizeBytes = fsize,
				FileCreationTime = fCreate,
				FileLastWriteTime = fWrite,
				CertificateDetails = cert,
				TechnicalDetails = string.Format("Файл: {0}\r\nРазмер: {1:N0} байт\r\nПодпись: {2}", filePath, fsize, cert),
				ForensicReport = "Файл " + filePath + " присутствует на диске. Целостность подтверждена."
			});
		}

		private DateTime ExtractTimestampFromSacp(byte[] data)
		{
			if (data == null || data.Length < 52) return DateTime.MinValue;
			// Scan known offsets in SACP structure (40, 44, 48) for FILETIME
			int[] offsets = new int[] { 44, 40, 48, 52, 36 };
			foreach (int off in offsets)
			{
				if (off + 8 <= data.Length)
				{
					long ft = BitConverter.ToInt64(data, off);
					try
					{
						if (ft > 0)
						{
							DateTime dt = DateTime.FromFileTimeUtc(ft).ToLocalTime();
							if (dt.Year >= 2020 && dt.Year <= 2030)
							{
								return dt;
							}
						}
					}
					catch { }
				}
			}
			return DateTime.MinValue;
		}

		private string NormalizeNtDevicePath(string ntPath)
		{
			if (string.IsNullOrEmpty(ntPath)) return "";
			// Match \Device\HarddiskVolumeX\
			Match m = Regex.Match(ntPath, @"^\\Device\\HarddiskVolume(\d+)\\(.*)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				string volNum = m.Groups[1].Value;
				string rest = m.Groups[2].Value;

				// Map common volume numbers to drives
				string drive = (volNum == "3") ? "C:" : (volNum == "2" ? "C:" : (volNum == "4" ? "D:" : "C:"));
				return drive + "\\" + rest;
			}
			return ntPath;
		}

		private string NormalizeFeatureUsagePath(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return null;
			if (raw.StartsWith("*PID", StringComparison.OrdinalIgnoreCase)) return null;

			// Replace known folder GUIDs
			string res = raw;
			if (res.StartsWith("{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", StringComparison.OrdinalIgnoreCase))
			{
				res = res.Replace("{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", Environment.GetFolderPath(Environment.SpecialFolder.System));
			}
			if (res.StartsWith("{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}", StringComparison.OrdinalIgnoreCase))
			{
				res = res.Replace("{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			}

			// If it contains a colon or backslash, it's a file path
			if (res.Contains(":\\") || res.Contains(@"\"))
			{
				return res;
			}
			return null;
		}

		private bool IsWhitelistedTool(string fname, string fullPath)
		{
			string lower = (fname + " " + fullPath).ToLowerInvariant();
			return lower.Contains("anydesk") || lower.Contains("rudesktop") ||
				   lower.Contains("opautoclicker") || lower.Contains("speedautoclicker") ||
				   lower.Contains("tinytask") || lower.Contains("x-mouse") ||
				   lower.Contains("autohotkey") || lower.Contains("clickermann") ||
				   lower.Contains("keyran") || lower.Contains("mccheatchecker");
		}

		private bool IsCheatMatch(string fname, string fullPath)
		{
			string combined = (fname + " " + fullPath).ToLowerInvariant();
			string[] cheats = new string[]
			{
				"vape", "celestial", "expensive", "nursultan", "nurik", "kura", "minced", "slinky", "drip",
				"doomsday", "wexside", "richpremium", "flint", "rockstar", "boberware", "tenacity", "novoline",
				"riseclient", "thunderhack", "liquidbounce", "kmbox", "spoofer", "akrien", "exloader",
				"killaura", "hitbox", "reach", "velocity", "midnight", "neverlose", "gamesense", "fatality",
				"weave", "wurst", "meteor", "future", "rusherhack", "konas", "phobos", "fanta", "exhibition",
				"entropy", "whiteout", "koid", "phantom", "dream", "wild", "exire"
			};

			foreach (string ch in cheats)
			{
				if (combined.Contains(ch)) return true;
			}
			return false;
		}

		private string ComputeSha256(string filePath)
		{
			try
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (SHA256 sha = SHA256.Create())
				{
					byte[] hash = sha.ComputeHash(fs);
					StringBuilder sb = new StringBuilder();
					foreach (byte b in hash) sb.Append(b.ToString("x2"));
					return sb.ToString();
				}
			}
			catch { return "Ошибка чтения хеша"; }
		}

		private string GetCertificateSummary(string filePath)
		{
			try
			{
				X509Certificate rawCert = X509Certificate.CreateFromSignedFile(filePath);
				if (rawCert != null)
				{
					using (X509Certificate2 cert = new X509Certificate2(rawCert))
					{
						string subj = cert.Subject;
						int cnIdx = subj.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
						if (cnIdx >= 0)
						{
							int endIdx = subj.IndexOf(',', cnIdx);
							string cn = (endIdx > cnIdx) ? subj.Substring(cnIdx + 3, endIdx - cnIdx - 3) : subj.Substring(cnIdx + 3);
							return "Подписан: " + cn.Trim('\"', ' ');
						}
						return "Подписан: " + subj;
					}
				}
			}
			catch { }
			return "Не подписан (Unsigned)";
		}

		#endregion

		#region KPI & Filtering

		private void UpdateKpisAndStatus()
		{
			int total = allItems.Count;
			int viols = 0;
			int deleted = 0;
			int cheats = 0;
			int sacp = 0;
			int feat = 0;

			foreach (BamStoreItem it in allItems)
			{
				if (it.IsViolation) viols++;
				if (it.IsDeleted) deleted++;
				if (it.IsCheat) cheats++;
				if (it.SourceDatabase.Contains("SACP")) sacp++;
				if (it.SourceDatabase.Contains("FeatureUsage") || it.SourceDatabase.Contains("BAM")) feat++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = viols.ToString();
			lblKpiDeletedFiles.Text = deleted.ToString();
			lblKpiCheatExecutions.Text = cheats.ToString();
			lblKpiSacpStore.Text = sacp.ToString();
			lblKpiFeatureUsage.Text = feat.ToString();

			if (viols > 0)
			{
				lblStatusPill.Text = string.Format("🚨 НАЙДЕНО УЛИК: {0} (5.01/5.08)", viols);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else if (deleted > 0)
			{
				lblStatusPill.Text = string.Format("⚠️ УДАЛЕННЫХ ФАЙЛОВ: {0}", deleted);
				lblStatusPill.BackColor = Ui.AccentOrange;
				lblStatusPill.ForeColor = Color.Black;
			}
			else
			{
				lblStatusPill.Text = "✔️ БАЗЫ ЗАПУСКА ЧИСТЫ";
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("🚨 Нарушения ({0})", viols);
			btnFilterDeleted.Text = string.Format("🗑️ Удаленные ({0})", deleted);
			btnFilterCheats.Text = string.Format("💉 Читы ({0})", cheats);
			btnFilterSacp.Text = string.Format("🏬 SACP ({0})", sacp);
			btnFilterFeatureUsage.Text = string.Format("🔄 FeatureUsage ({0})", feat);
			btnFilterClean.Text = string.Format("✔️ На диске ({0})", total - deleted);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (BamStoreItem item in allItems)
			{
				// Category Filter
				if (currentFilter == BamStoreFilterCategory.ViolationsOnly && !item.IsViolation) continue;
				if (currentFilter == BamStoreFilterCategory.DeletedFiles508 && !item.IsDeleted) continue;
				if (currentFilter == BamStoreFilterCategory.Cheats501 && !item.IsCheat) continue;
				if (currentFilter == BamStoreFilterCategory.SacpStore && !item.SourceDatabase.Contains("SACP")) continue;
				if (currentFilter == BamStoreFilterCategory.FeatureUsageAndBam && !(item.SourceDatabase.Contains("FeatureUsage") || item.SourceDatabase.Contains("BAM"))) continue;
				if (currentFilter == BamStoreFilterCategory.ExistingClean && !item.FileExistsOnDisk) continue;

				// Text search
				if (!string.IsNullOrEmpty(search))
				{
					bool match = (item.FileName != null && item.FileName.ToLowerInvariant().Contains(search)) ||
								 (item.FullPath != null && item.FullPath.ToLowerInvariant().Contains(search)) ||
								 (item.SourceDatabase != null && item.SourceDatabase.ToLowerInvariant().Contains(search)) ||
								 (item.DiskStatus != null && item.DiskStatus.ToLowerInvariant().Contains(search)) ||
								 (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.SourceDatabase);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(item.FullPath);
				lvi.SubItems.Add(item.FileExistsOnDisk ? "✔️ " + item.ExecutionTimeDisplay : item.DiskStatus);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);

				lvi.ForeColor = item.RowColor;
				lvi.Tag = item;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
				UpdateSelectedDetails();
			}
			else
			{
				txtForensicReport.Text = "Нет записей в базах запуска, соответствующих выбранному фильтру.";
				txtTechnicalDetails.Clear();
				txtSignatureDetails.Clear();
			}
		}

		private void UpdateSelectedDetails()
		{
			if (listView.SelectedItems.Count == 0) return;
			BamStoreItem item = listView.SelectedItems[0].Tag as BamStoreItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport ?? "";
			txtTechnicalDetails.Text = item.TechnicalDetails ?? "";
			txtSignatureDetails.Text = string.Format(
				"Файл: {0}\r\nНаличие: {1}\r\nРазмер: {2:N0} байт\r\nSHA-256: {3}\r\nСоздан: {4}\r\nИзменен: {5}\r\nСертификат:\r\n{6}",
				item.FullPath,
				item.FileExistsOnDisk ? "СУЩЕСТВУЕТ НА ДИСКЕ" : "УДАЛЕН С ДИСКА (5.08)",
				item.FileSizeBytes,
				item.Sha256Hash,
				item.FileCreationTime != DateTime.MinValue ? item.FileCreationTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
				item.FileLastWriteTime != DateTime.MinValue ? item.FileLastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
				item.CertificateDetails ?? "—");
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRowText()
		{
			if (listView.SelectedItems.Count == 0) return;
			BamStoreItem item = listView.SelectedItems[0].Tag as BamStoreItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | {2} | {3} | {4} | {5}",
				item.SourceDatabase, item.FileName, item.FullPath, item.DiskStatus, item.TargetRule, item.Verdict);
			Clipboard.SetText(text);
			MessageBox.Show("Строка скопирована в буфер обмена:\r\n" + text, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyForDiscord()
		{
			if (listView.SelectedItems.Count == 0) return;
			BamStoreItem item = listView.SelectedItems[0].Tag as BamStoreItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine(item.IsViolation ? "- [НАРУШЕНИЕ 5.01 / 5.08 - СЛЕД ЗАПУСКА]" : "+ [СЛЕД ЗАПУСКА ИСПОЛНЯЕМОГО ФАЙЛА]");
			sb.AppendLine(string.Format("Игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Время проверки: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Источник базы: {0}", item.SourceDatabase));
			sb.AppendLine(string.Format("Исполняемый файл: {0}", item.FileName));
			sb.AppendLine(string.Format("Полный путь: {0}", item.FullPath));
			sb.AppendLine(string.Format("Статус на диске: {0}", item.FileExistsOnDisk ? "Существует" : "УДАЛЕН (5.08)"));
			if (item.ExecutionTime != DateTime.MinValue)
				sb.AppendLine(string.Format("Время запуска: {0:yyyy-MM-dd HH:mm:ss}", item.ExecutionTime));
			if (!string.IsNullOrEmpty(item.Sha256Hash) && item.Sha256Hash != "—")
				sb.AppendLine(string.Format("SHA-256: {0}", item.Sha256Hash));
			sb.AppendLine(string.Format("Регламент: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Форматированный отчет для Discord скопирован!", "Discord Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите строку с нарушением регламента для генерации команды бана.", "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			BamStoreItem item = listView.SelectedItems[0].Tag as BamStoreItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранная запись не является нарушением правил 5.01 / 5.08.", "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = item.TargetRule.Contains("5.08") ?
				string.Format("5.08 Сокрытие улик (Удален исполняемый файл {0})", item.FileName) :
				string.Format("5.01 Использование читов ({0})", item.FileName);

			string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_Execution_SACP_BAM_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза следов запуска SACP / BAM — " + targetPlayerName + "</title>");
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

					sb.AppendLine("<h1>🛡️ Судебная экспертиза следов запуска SACP, BAM и FeatureUsage</h1>");
					sb.AppendLine(string.Format("<p>Игрок: <b>{0}</b> | Дата генерации: {1:yyyy-MM-dd HH:mm:ss}</p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🛡️ Всего в базах</div><div class='kpi-val'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🚨 Нарушений (5.01/5.08)</div><div class='kpi-val crit'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🗑️ Удалено файлов (5.08)</div><div class='kpi-val warn'>{0}</div></div>", lblKpiDeletedFiles.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>💉 Запусков читов (5.01)</div><div class='kpi-val crit'>{0}</div></div>", lblKpiCheatExecutions.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🏬 SACP Store</div><div class='kpi-val'>{0}</div></div>", lblKpiSacpStore.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🔄 FeatureUsage/BAM</div><div class='kpi-val ok'>{0}</div></div>", lblKpiFeatureUsage.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table><thead><tr><th>#</th><th>Источник базы</th><th>Исполняемый файл</th><th>Полный путь</th><th>Статус / Время</th><th>Регламент</th><th>Вердикт</th></tr></thead><tbody>");
					foreach (BamStoreItem it in allItems)
					{
						string cls = it.IsViolation ? "crit" : (it.IsClean ? "ok" : "warn");
						sb.AppendLine(string.Format(
							"<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td></tr>",
							cls, it.Index, it.SourceDatabase, it.FileName, it.FullPath, it.FileExistsOnDisk ? it.ExecutionTimeDisplay : it.DiskStatus, it.TargetRule, it.Verdict));
					}
					sb.AppendLine("</tbody></table></body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("HTML отчет успешно экспортирован:\r\n" + sfd.FileName, "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка сохранения HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
