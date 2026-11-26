using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class OpenSaveAndLastVisitedPidlViewerForm : Form
	{
		#region Data Models

		public enum DialogCategory
		{
			OpenSaveMRU,
			LastVisitedMRU,
			CIDSizeMRU,
			FirstFolder
		}

		public enum ComDlgFilterCategory
		{
			All,
			Deleted508,
			CheatsAndInjectors501,
			LastVisitedApps,
			OpenSaveFiles,
			Whitelisted
		}

		public class ComDlgForensicItem
		{
			public int Index;
			public DialogCategory Category;
			public string SubkeyName;
			public int MruOrder;
			public string MruOrderDisplay;
			public string CallingApp;
			public string ItemName;
			public string FullPath;
			public string Extension;
			public bool? FileExists;
			public string ExistenceStatusText;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsDeleted508;
			public bool IsCheat501;
			public bool IsAllowed;
			public Color RowColor;
			public byte[] RawBytes;
			public string HexDump;
			public List<string> ExtractedStrings;
			public string ForensicReport;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ComDlgFilterCategory currentFilter = ComDlgFilterCategory.All;
		private List<ComDlgForensicItem> allItems = new List<ComDlgForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtHexDump;
		private DarkTextBoxMultiline txtExtractedStrings;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiDeleted508;
		private Label lblKpiCheat501;
		private Label lblKpiLastVisited;
		private Label lblKpiSafe;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterLastVisited;
		private DarkButton btnFilterOpenSave;
		private DarkButton btnFilterSafe;

		private DarkButton btnCopyPath;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		private TabControl detailTabs;

		#endregion

		public OpenSaveAndLastVisitedPidlViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Initialization

		private void InitializeComponent()
		{
			Text = "📁 Инспектор диалогов ComDlg32 (OpenSavePidlMRU и LastVisitedPidlMRU) — MCCheatChecker";
			Size = new Size(1300, 780);
			MinimumSize = new Size(1060, 600);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			// Top Header Panel
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "📁 Инспектор диалогов ComDlg32 (OpenSavePidlMRU & LastVisitedPidlMRU)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Глубокая реконструкция путей из бинарных структур PIDL (Shell Item IDList): фиксация открытий читов инжекторами (LastVisited), проверка наличия файлов на диске и выявление зачистки улик (5.08 / 5.01).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования диалогов ComDlg32...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(35, 45, 55);
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(17, 68);
			topPanel.Controls.Add(lblStatusPill);

			// KPI Panel inside Header
			Panel kpiPanel = new Panel();
			kpiPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			kpiPanel.Size = new Size(620, 60);
			kpiPanel.Location = new Point(topPanel.Width - 636, 14);
			topPanel.Resize += delegate
			{
				kpiPanel.Location = new Point(topPanel.Width - 636, 14);
			};

			int kpiX = 0;
			kpiPanel.Controls.Add(CreateKpiBadge("Всего записей", "—", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Удалено (5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiDeleted508));
			kpiPanel.Controls.Add(CreateKpiBadge("🔴 Читы (5.01)", "0", Ui.AccentOrange, ref kpiX, out lblKpiCheat501));
			kpiPanel.Controls.Add(CreateKpiBadge("📂 LastVisited", "0", Ui.AccentBlue, ref kpiX, out lblKpiLastVisited));
			kpiPanel.Controls.Add(CreateKpiBadge("⚪ Белый список", "0", Ui.AccentGreen, ref kpiX, out lblKpiSafe));
			topPanel.Controls.Add(kpiPanel);

			Controls.Add(topPanel);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все записи", ComDlgFilterCategory.All, ref fBtnX, filterBar);
			btnFilterDeleted = CreateFilterButton("🚨 Удаленные (5.08)", ComDlgFilterCategory.Deleted508, ref fBtnX, filterBar);
			btnFilterCheats = CreateFilterButton("🔴 Читы / Инжекторы (5.01)", ComDlgFilterCategory.CheatsAndInjectors501, ref fBtnX, filterBar);
			btnFilterLastVisited = CreateFilterButton("📂 LastVisited (Процессы)", ComDlgFilterCategory.LastVisitedApps, ref fBtnX, filterBar);
			btnFilterOpenSave = CreateFilterButton("💾 OpenSave (Файлы)", ComDlgFilterCategory.OpenSaveFiles, ref fBtnX, filterBar);
			btnFilterSafe = CreateFilterButton("⚪ Белый список", ComDlgFilterCategory.Whitelisted, ref fBtnX, filterBar);

			// Search Box
			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(260, 26);
			searchBox.Location = new Point(filterBar.Width - 274, 10);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Resize += delegate
			{
				searchBox.Location = new Point(filterBar.Width - 274, 10);
			};
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextMuted;
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 340, 14);
			filterBar.Resize += delegate
			{
				lblSearch.Location = new Point(filterBar.Width - 340, 14);
			};
			filterBar.Controls.Add(lblSearch);

			Controls.Add(filterBar);

			// Main Split Container
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 420;
			split.SplitterWidth = 6;
			split.BackColor = Ui.Border;

			// Top Panel: ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("#", 45);
			listView.Columns.Add("MRU", 75);
			listView.Columns.Add("Тип / Ключ", 110);
			listView.Columns.Add("Вызывающий процесс", 150);
			listView.Columns.Add("Имя файла / Папка", 200);
			listView.Columns.Add("Полный путь", 360);
			listView.Columns.Add("Статус на диске", 130);
			listView.Columns.Add("Вердикт", 220);
			listView.Columns.Add("Регламент", 110);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			// Context Menu
			ContextMenuStrip cms = new ContextMenuStrip();
			cms.Renderer = new DarkMenuRenderer();
			ToolStripMenuItem miCopyPath = new ToolStripMenuItem("📋 Скопировать путь", null, delegate { CopySelectedPath(); });
			ToolStripMenuItem miOpenDir = new ToolStripMenuItem("📂 Открыть папку в Проводнике", null, delegate { OpenSelectedFolder(); });
			ToolStripMenuItem miCopyDiscord = new ToolStripMenuItem("💬 Скопировать строку для отчета Discord / VK", null, delegate { CopySelectedDiscordRow(); });
			ToolStripMenuItem miCopyBan = new ToolStripMenuItem("🔨 Скопировать команду бана", null, delegate { CopySelectedBanCommand(); });
			ToolStripMenuItem miCopyHex = new ToolStripMenuItem("📋 Скопировать Hex-дамп PIDL", null, delegate { CopySelectedHexDump(); });
			cms.Items.AddRange(new ToolStripItem[] { miCopyPath, miOpenDir, new ToolStripSeparator(), miCopyDiscord, miCopyBan, miCopyHex });
			listView.ContextMenuStrip = cms;

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details with TabControl
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 8, 12, 8);

			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Font = Ui.FontBody(8.5f);

			// Tab 1: Forensic Report
			TabPage tabReport = new TabPage("📄 Криминалистическое заключение");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.Font = Ui.FontMono(8.5f);
			txtForensicReport.ReadOnly = true;
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			// Tab 2: Binary Hex Dump
			TabPage tabHex = new TabPage("🔍 Бинарный дамп PIDL (Hex & ASCII View)");
			tabHex.BackColor = Ui.Surface;
			txtHexDump = new DarkTextBoxMultiline();
			txtHexDump.Dock = DockStyle.Fill;
			txtHexDump.Font = Ui.FontMono(8.5f);
			txtHexDump.ReadOnly = true;
			tabHex.Controls.Add(txtHexDump);
			detailTabs.TabPages.Add(tabHex);

			// Tab 3: Extracted Strings
			TabPage tabStrings = new TabPage("🔤 Извлеченные строки и структура IDList");
			tabStrings.BackColor = Ui.Surface;
			txtExtractedStrings = new DarkTextBoxMultiline();
			txtExtractedStrings.Dock = DockStyle.Fill;
			txtExtractedStrings.Font = Ui.FontMono(8.5f);
			txtExtractedStrings.ReadOnly = true;
			tabStrings.Controls.Add(txtExtractedStrings);
			detailTabs.TabPages.Add(tabStrings);

			detailContainer.Controls.Add(detailTabs);
			split.Panel2.Controls.Add(detailContainer);

			Controls.Add(split);

			// Bottom Action Bar
			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 48;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(14, 8, 14, 8);
			actionBar.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen p = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(p, 0, 0, actionBar.Width, 0);
				}
			};

			int aX = 14;
			btnCopyPath = CreateActionButton("📋 Скопировать путь", ref aX, actionBar, delegate { CopySelectedPath(); });
			btnOpenFolder = CreateActionButton("📂 Открыть папку", ref aX, actionBar, delegate { OpenSelectedFolder(); });
			btnCopyDiscord = CreateActionButton("💬 Строка отчета", ref aX, actionBar, delegate { CopySelectedDiscordRow(); });
			btnCopyBanCmd = CreateActionButton("🔨 Команда бана", ref aX, actionBar, delegate { CopySelectedBanCommand(); });

			DarkButton btnExportHtml = CreateActionButton("📑 Экспорт в HTML", ref aX, actionBar, delegate { ExportToHtml(); });

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Size = new Size(160, 32);
			btnRescan.Location = new Point(actionBar.Width - 174, 8);
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Font = Ui.FontBody(8.5f, true);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Resize += delegate
			{
				btnRescan.Location = new Point(actionBar.Width - 174, 8);
			};
			actionBar.Controls.Add(btnRescan);

			Controls.Add(actionBar);
			split.BringToFront();

			KeyPreview = true;
			KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					Close();
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartScan();
				}
			};
		}

		private Panel CreateKpiBadge(string caption, string initialVal, Color valColor, ref int curX, out Label valLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(116, 52);
			p.Location = new Point(curX, 4);
			p.BackColor = Ui.SurfaceAlt;
			p.Padding = new Padding(6, 4, 6, 4);

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
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = valColor;
			valLabel.Dock = DockStyle.Fill;
			valLabel.TextAlign = ContentAlignment.MiddleCenter;
			p.Controls.Add(valLabel);
			valLabel.BringToFront();

			curX += 122;
			return p;
		}

		private DarkButton CreateFilterButton(string text, ComDlgFilterCategory cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(150, 30);
			btn.Location = new Point(curX, 8);
			btn.Font = Ui.FontBody(8.5f, cat == ComDlgFilterCategory.All);
			btn.Click += delegate
			{
				currentFilter = cat;
				UpdateFilterButtonStates();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int curX, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(145, 32);
			btn.Location = new Point(curX, 8);
			btn.Font = Ui.FontBody(8.5f);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 151;
			return btn;
		}

		private void UpdateFilterButtonStates()
		{
			btnFilterAll.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.All);
			btnFilterDeleted.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.Deleted508);
			btnFilterCheats.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.CheatsAndInjectors501);
			btnFilterLastVisited.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.LastVisitedApps);
			btnFilterOpenSave.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.OpenSaveFiles);
			btnFilterSafe.Font = Ui.FontBody(8.5f, currentFilter == ComDlgFilterCategory.Whitelisted);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			lblStatusPill.Text = "⏳ Сканирование реестра ComDlg32 и дешифрование PIDL...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			listView.Items.Clear();
			allItems.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<ComDlgForensicItem> items = new List<ComDlgForensicItem>();
			int globalIdx = 1;

			// 1. Scan LastVisitedPidlMRU
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU"))
				{
					if (key != null)
					{
						byte[] mruBytes = key.GetValue("MRUListEx") as byte[];
						List<int> mruOrder = ParseMruList(mruBytes);
						HashSet<int> processed = new HashSet<int>();

						for (int rank = 0; rank < mruOrder.Count; rank++)
						{
							int valIdx = mruOrder[rank];
							processed.Add(valIdx);
							byte[] val = key.GetValue(valIdx.ToString()) as byte[];
							if (val == null) continue;

							ComDlgForensicItem item = ProcessLastVisitedEntry(val, valIdx, rank, globalIdx++);
							if (item != null) items.Add(item);
						}

						// Also scan any values not in MRUListEx
						string[] valNames = key.GetValueNames();
						foreach (string vn in valNames)
						{
							if (vn.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
							int parsed;
							if (int.TryParse(vn, out parsed) && processed.Contains(parsed)) continue;

							byte[] val = key.GetValue(vn) as byte[];
							if (val == null) continue;
							ComDlgForensicItem item = ProcessLastVisitedEntry(val, parsed, 999, globalIdx++);
							if (item != null) items.Add(item);
						}
					}
				}
			}
			catch { }

			// 2. Scan OpenSavePidlMRU
			try
			{
				using (RegistryKey root = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU"))
				{
					if (root != null)
					{
						string[] subNames = root.GetSubKeyNames();
						foreach (string sub in subNames)
						{
							try
							{
								using (RegistryKey subKey = root.OpenSubKey(sub))
								{
									if (subKey == null) continue;
									byte[] mruBytes = subKey.GetValue("MRUListEx") as byte[];
									List<int> mruOrder = ParseMruList(mruBytes);
									HashSet<int> processed = new HashSet<int>();

									for (int rank = 0; rank < mruOrder.Count; rank++)
									{
										int valIdx = mruOrder[rank];
										processed.Add(valIdx);
										byte[] val = subKey.GetValue(valIdx.ToString()) as byte[];
										if (val == null) continue;

										ComDlgForensicItem item = ProcessOpenSaveEntry(val, sub, valIdx, rank, globalIdx++);
										if (item != null) items.Add(item);
									}

									string[] valNames = subKey.GetValueNames();
									foreach (string vn in valNames)
									{
										if (vn.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
										int parsed;
										if (int.TryParse(vn, out parsed) && processed.Contains(parsed)) continue;

										byte[] val = subKey.GetValue(vn) as byte[];
										if (val == null) continue;
										ComDlgForensicItem item = ProcessOpenSaveEntry(val, sub, parsed, 999, globalIdx++);
										if (item != null) items.Add(item);
									}
								}
							}
							catch { }
						}
					}
				}
			}
			catch { }

			// 3. Scan CIDSizeMRU for executables
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\CIDSizeMRU"))
				{
					if (key != null)
					{
						byte[] mruBytes = key.GetValue("MRUListEx") as byte[];
						List<int> mruOrder = ParseMruList(mruBytes);
						string[] valNames = key.GetValueNames();
						foreach (string vn in valNames)
						{
							if (vn.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
							byte[] val = key.GetValue(vn) as byte[];
							if (val == null) continue;

							ComDlgForensicItem item = ProcessCIDSizeEntry(val, vn, globalIdx++);
							if (item != null) items.Add(item);
						}
					}
				}
			}
			catch { }

			e.Result = items;
		}

		private ComDlgForensicItem ProcessLastVisitedEntry(byte[] bytes, int valIndex, int mruRank, int globalIndex)
		{
			if (bytes == null || bytes.Length < 4) return null;

			// Extract calling executable (Unicode string terminated by \0\0)
			int nullIdx = -1;
			for (int i = 0; i < bytes.Length - 1; i += 2)
			{
				if (bytes[i] == 0 && bytes[i + 1] == 0)
				{
					nullIdx = i;
					break;
				}
			}

			string callingExe = nullIdx > 0 ? Encoding.Unicode.GetString(bytes, 0, nullIdx).Trim() : "UnknownApp";
			int pidlStart = nullIdx > 0 ? nullIdx + 2 : 0;
			string folderPath = ParsePidlPath(bytes, pidlStart);

			ComDlgForensicItem item = new ComDlgForensicItem();
			item.Index = globalIndex;
			item.Category = DialogCategory.LastVisitedMRU;
			item.SubkeyName = "LastVisitedPidlMRU";
			item.MruOrder = mruRank;
			item.MruOrderDisplay = mruRank == 999 ? "—" : string.Format("#{0}{1}", mruRank, mruRank == 0 ? " (Свежий)" : "");
			item.CallingApp = callingExe;
			item.ItemName = string.IsNullOrEmpty(folderPath) ? callingExe : Path.GetFileName(folderPath.TrimEnd('\\'));
			item.FullPath = folderPath;
			item.Extension = "Папка";
			item.RawBytes = bytes;
			item.HexDump = GenerateHexDump(bytes);
			item.ExtractedStrings = ExtractAllStrings(bytes);

			// Analysis
			string exeLower = callingExe.ToLowerInvariant();
			string pathLower = (folderPath ?? "").ToLowerInvariant();

			bool isKnownInjector = IsCheatOrInjector(exeLower);
			bool isAllowedApp = IsWhitelistedApp(exeLower);

			bool dirExists = !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
			item.FileExists = dirExists;
			item.ExistenceStatusText = dirExists ? "Папка существует" : (string.IsNullOrEmpty(folderPath) ? "Путь не распознан" : "🚨 ПАПКА УДАЛЕНА");

			if (isKnownInjector)
			{
				item.IsViolation = true;
				item.IsCheat501 = true;
				item.TargetRule = "5.01 Использование читов";
				item.Verdict = string.Format("🚨 5.01 ИНЖЕКТОР / ЧИТ: {0}", callingExe);
				item.RowColor = Color.FromArgb(255, 110, 110);
			}
			else if (!dirExists && !string.IsNullOrEmpty(folderPath) && IsSensitiveFolder(pathLower))
			{
				item.IsViolation = true;
				item.IsDeleted508 = true;
				item.TargetRule = "5.08 Сокрытие улик";
				item.Verdict = string.Format("⚠️ 5.08 СОКРЫТИЕ: Удалена папка {0}", item.ItemName);
				item.RowColor = Color.FromArgb(255, 160, 100);
			}
			else if (isAllowedApp)
			{
				item.IsAllowed = true;
				item.TargetRule = "Белый список ПО";
				item.Verdict = "⚪ Легитимное приложение (Белый список)";
				item.RowColor = Color.FromArgb(180, 225, 180);
			}
			else
			{
				item.TargetRule = "Чисто / Аудит";
				item.Verdict = "Диалог выбора папки приложением";
				item.RowColor = Ui.TextPrimary;
			}

			item.ForensicReport = BuildLastVisitedReport(item, callingExe, folderPath, dirExists, isKnownInjector, isAllowedApp);
			return item;
		}

		private ComDlgForensicItem ProcessOpenSaveEntry(byte[] bytes, string subkey, int valIndex, int mruRank, int globalIndex)
		{
			if (bytes == null || bytes.Length < 4) return null;

			string resolvedPath = ParsePidlPath(bytes, 0);

			// Fallback: If standard Shell Item path reconstruction didn't yield an absolute path, try regex
			if (string.IsNullOrEmpty(resolvedPath) || resolvedPath.Length < 3 || resolvedPath.IndexOf('\\') < 0)
			{
				string uni = Encoding.Unicode.GetString(bytes);
				string asc = Encoding.Default.GetString(bytes);

				Match m1 = Regex.Match(uni, @"[a-zA-Z]:\\[^<>" + "\"" + @"/|?*\x00-\x1F\r\n\t]+");
				if (m1.Success) resolvedPath = m1.Value.Trim();
				else
				{
					Match m2 = Regex.Match(asc, @"[a-zA-Z]:\\[^<>" + "\"" + @"/|?*\x00-\x1F\r\n\t]+");
					if (m2.Success) resolvedPath = m2.Value.Trim();
				}
			}

			string fName = "";
			if (!string.IsNullOrEmpty(resolvedPath))
			{
				try { fName = Path.GetFileName(resolvedPath); } catch { fName = resolvedPath; }
			}
			if (string.IsNullOrEmpty(fName))
			{
				fName = ExtractBestFilenameFallback(bytes, subkey);
			}

			ComDlgForensicItem item = new ComDlgForensicItem();
			item.Index = globalIndex;
			item.Category = DialogCategory.OpenSaveMRU;
			item.SubkeyName = "OpenSavePidlMRU\\" + subkey;
			item.MruOrder = mruRank;
			item.MruOrderDisplay = mruRank == 999 ? "—" : string.Format("#{0}{1}", mruRank, mruRank == 0 ? " (Свежий)" : "");
			item.CallingApp = "OpenSaveDialog (." + subkey + ")";
			item.ItemName = fName;
			item.FullPath = resolvedPath;
			item.Extension = string.IsNullOrEmpty(subkey) ? "*" : "." + subkey;
			item.RawBytes = bytes;
			item.HexDump = GenerateHexDump(bytes);
			item.ExtractedStrings = ExtractAllStrings(bytes);

			// Disk audit
			bool pathIsCheckable = !string.IsNullOrEmpty(resolvedPath) && (resolvedPath.Contains(":\\") || resolvedPath.StartsWith("\\\\"));
			bool fileExists = false;
			bool dirExists = false;

			if (pathIsCheckable)
			{
				try
				{
					fileExists = File.Exists(resolvedPath);
					dirExists = !fileExists && Directory.Exists(resolvedPath);
				}
				catch { }
			}

			item.FileExists = fileExists || dirExists;

			string checkTarget = (fName + " " + (resolvedPath ?? "")).ToLowerInvariant();
			bool isCheat = IsCheatOrInjector(checkTarget);
			bool isWhitelisted = IsWhitelistedFileOrPath(checkTarget);

			if (pathIsCheckable)
			{
				item.ExistenceStatusText = fileExists ? "Файл существует" : (dirExists ? "Папка существует" : "🚨 ФАЙЛ УДАЛЕН С ДИСКА");
			}
			else
			{
				item.ExistenceStatusText = "Путь неполный";
			}

			// Verdict assignment
			if (isCheat)
			{
				item.IsViolation = true;
				if (!fileExists && pathIsCheckable)
				{
					item.IsDeleted508 = true;
					item.TargetRule = "5.08 Сокрытие улик";
					item.Verdict = string.Format("🚨 5.08 СОКРЫТИЕ: Удален чит [{0}]", fName);
					item.RowColor = Color.FromArgb(255, 100, 100);
				}
				else
				{
					item.IsCheat501 = true;
					item.TargetRule = "5.01 Использование читов";
					item.Verdict = string.Format("🔴 5.01 ЧИТ В ДИАЛОГЕ: [{0}]", fName);
					item.RowColor = Color.FromArgb(255, 120, 120);
				}
			}
			else if (isWhitelisted)
			{
				item.IsAllowed = true;
				item.TargetRule = "Белый список ПО";
				item.Verdict = "⚪ Разрешенное ПО (Белый список)";
				item.RowColor = Color.FromArgb(180, 225, 180);
			}
			else if (pathIsCheckable && !fileExists && !dirExists)
			{
				string extLower = (Path.GetExtension(resolvedPath) ?? "").ToLowerInvariant();
				bool isExecutable = extLower == ".exe" || extLower == ".dll" || extLower == ".jar" || extLower == ".bat" || extLower == ".cmd";
				bool isInSensitiveDir = IsSensitiveFolder(resolvedPath.ToLowerInvariant());

				if (isExecutable && isInSensitiveDir)
				{
					item.IsViolation = true;
					item.IsDeleted508 = true;
					item.TargetRule = "5.08 Сокрытие улик";
					item.Verdict = string.Format("⚠️ 5.08 СОКРЫТИЕ: Удален бинарный файл [{0}]", fName);
					item.RowColor = Color.FromArgb(255, 170, 90);
				}
				else
				{
					item.TargetRule = "Аудит диска";
					item.Verdict = "Файл удален или перемещен";
					item.RowColor = Color.FromArgb(210, 190, 150);
				}
			}
			else
			{
				item.TargetRule = "Чисто";
				item.Verdict = "Легитимный выбор файла в диалоге";
				item.RowColor = Ui.TextPrimary;
			}

			item.ForensicReport = BuildOpenSaveReport(item, subkey, resolvedPath, fName, fileExists, isCheat, isWhitelisted);
			return item;
		}

		private ComDlgForensicItem ProcessCIDSizeEntry(byte[] bytes, string valueName, int globalIndex)
		{
			if (bytes == null || bytes.Length < 4) return null;

			string appName = valueName;
			bool isCheat = IsCheatOrInjector(appName.ToLowerInvariant());

			ComDlgForensicItem item = new ComDlgForensicItem();
			item.Index = globalIndex;
			item.Category = DialogCategory.CIDSizeMRU;
			item.SubkeyName = "CIDSizeMRU";
			item.MruOrder = 999;
			item.MruOrderDisplay = "—";
			item.CallingApp = appName;
			item.ItemName = appName;
			item.FullPath = "CIDSizeMRU\\" + valueName;
			item.Extension = "CID Dialog";
			item.RawBytes = bytes;
			item.HexDump = GenerateHexDump(bytes);
			item.ExtractedStrings = ExtractAllStrings(bytes);
			item.FileExists = null;
			item.ExistenceStatusText = "Параметры окна";

			if (isCheat)
			{
				item.IsViolation = true;
				item.IsCheat501 = true;
				item.TargetRule = "5.01 Использование читов";
				item.Verdict = string.Format("🚨 5.01 ЧИТ / ИНЖЕКТОР (CID): {0}", appName);
				item.RowColor = Color.FromArgb(255, 120, 120);
			}
			else
			{
				item.TargetRule = "Чисто";
				item.Verdict = "Параметры диалогового окна процесса";
				item.RowColor = Ui.TextMuted;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ РАЗМЕРА И ПОЗИЦИИ ДИАЛОГОВ (CIDSizeMRU) ===");
			sb.AppendLine(string.Format("Исполняемый файл / Идентификатор: {0}", appName));
			sb.AppendLine(string.Format("Статус угрозы: {0}", isCheat ? "🚨 ОБНАРУЖЕН ЧИТ / ИНЖЕКТОР!" : "Легитимное приложение Windows"));
			sb.AppendLine();
			sb.AppendLine("Данный ключ реестра фиксирует координаты и размеры окна диалогов открытия файлов, использованные программой.");
			item.ForensicReport = sb.ToString();

			return item;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			allItems = e.Result as List<ComDlgForensicItem> ?? new List<ComDlgForensicItem>();

			// Calculate KPI
			int total = allItems.Count;
			int del508 = 0;
			int cheat501 = 0;
			int lastVis = 0;
			int safe = 0;

			foreach (ComDlgForensicItem it in allItems)
			{
				if (it.IsDeleted508) del508++;
				if (it.IsCheat501) cheat501++;
				if (it.Category == DialogCategory.LastVisitedMRU) lastVis++;
				if (it.IsAllowed) safe++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiDeleted508.Text = del508.ToString();
			lblKpiCheat501.Text = cheat501.ToString();
			lblKpiLastVisited.Text = lastVis.ToString();
			lblKpiSafe.Text = safe.ToString();

			if (del508 > 0 || cheat501 > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ УЛИКИ: {0} наруш. 5.08 (Удалено), {1} читов 5.01", del508, cheat501);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✅ Проверено {0} записей ComDlg32. Критических нарушений не выявлено.", total);
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

			string search = (searchBox != null ? searchBox.Text : "").Trim().ToLowerInvariant();

			int rowNumber = 1;
			foreach (ComDlgForensicItem item in allItems)
			{
				// Filter logic
				if (currentFilter == ComDlgFilterCategory.Deleted508 && !item.IsDeleted508) continue;
				if (currentFilter == ComDlgFilterCategory.CheatsAndInjectors501 && !item.IsCheat501) continue;
				if (currentFilter == ComDlgFilterCategory.LastVisitedApps && item.Category != DialogCategory.LastVisitedMRU) continue;
				if (currentFilter == ComDlgFilterCategory.OpenSaveFiles && item.Category != DialogCategory.OpenSaveMRU) continue;
				if (currentFilter == ComDlgFilterCategory.Whitelisted && !item.IsAllowed) continue;

				// Search filter
				if (!string.IsNullOrEmpty(search))
				{
					string matchTarget = string.Format("{0} {1} {2} {3} {4} {5} {6}",
						item.CallingApp, item.ItemName, item.FullPath, item.Extension, item.Verdict, item.TargetRule, item.SubkeyName).ToLowerInvariant();
					if (!matchTarget.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(rowNumber.ToString());
				lvi.SubItems.Add(item.MruOrderDisplay);
				lvi.SubItems.Add(item.Extension);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.CallingApp) ? "—" : item.CallingApp);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.ItemName) ? "—" : item.ItemName);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.FullPath) ? "—" : item.FullPath);
				lvi.SubItems.Add(item.ExistenceStatusText);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.TargetRule);

				lvi.ForeColor = item.RowColor;
				lvi.Tag = item;

				listView.Items.Add(lvi);
				rowNumber++;
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

			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null)
			{
				ClearDetails();
				return;
			}

			txtForensicReport.Text = item.ForensicReport ?? "Нет данных отчета.";
			txtHexDump.Text = item.HexDump ?? "Hex-дамп отсутствует.";

			StringBuilder sbStrings = new StringBuilder();
			sbStrings.AppendLine(string.Format("=== ИЗВЛЕЧЕННЫЕ СТРОКИ ИЗ БИНАРНОЙ СТРУКТУРЫ PIDL ({0} байт) ===", item.RawBytes != null ? item.RawBytes.Length : 0));
			sbStrings.AppendLine(string.Format("Реестровый путь: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\{0}", item.SubkeyName));
			sbStrings.AppendLine(string.Format("Порядок в MRUListEx: {0}", item.MruOrderDisplay));
			sbStrings.AppendLine();
			if (item.ExtractedStrings != null && item.ExtractedStrings.Count > 0)
			{
				for (int i = 0; i < item.ExtractedStrings.Count; i++)
				{
					sbStrings.AppendLine(string.Format("[Строка #{0:D2}]: {1}", i + 1, item.ExtractedStrings[i]));
				}
			}
			else
			{
				sbStrings.AppendLine("(Читаемые текстовые строки внутри структуры не обнаружены)");
			}
			txtExtractedStrings.Text = sbStrings.ToString();
		}

		private void ClearDetails()
		{
			txtForensicReport.Text = "Выберите запись из списка выше для подробного криминалистического анализа.";
			txtHexDump.Text = "";
			txtExtractedStrings.Text = "";
		}

		#endregion

		#region Binary PIDL Parsing Helpers

		private static List<int> ParseMruList(byte[] mru)
		{
			List<int> list = new List<int>();
			if (mru == null) return list;
			for (int i = 0; i <= mru.Length - 4; i += 4)
			{
				int val = BitConverter.ToInt32(mru, i);
				if (val == -1) break;
				list.Add(val);
			}
			return list;
		}

		private static string ParsePidlPath(byte[] bytes, int startOffset)
		{
			if (bytes == null || startOffset >= bytes.Length) return "";
			List<string> parts = new List<string>();
			int cur = startOffset;
			while (cur <= bytes.Length - 2)
			{
				ushort cb = BitConverter.ToUInt16(bytes, cur);
				if (cb == 0) break;
				if (cur + cb > bytes.Length) break;

				byte[] item = new byte[cb];
				Array.Copy(bytes, cur, item, 0, cb);

				string part = ExtractPart(item);
				if (!string.IsNullOrEmpty(part))
				{
					parts.Add(part);
				}
				cur += cb;
			}

			string full = "";
			foreach (string p in parts)
			{
				if (string.IsNullOrEmpty(full)) full = p;
				else if (full.EndsWith("\\")) full += p;
				else full += "\\" + p;
			}
			return full;
		}

		private static string ExtractPart(byte[] item)
		{
			if (item.Length < 3) return null;
			byte type = item[2];

			// Drive letter: type 0x2F (e.g. "C:\")
			if (type == 0x2F && item.Length >= 6)
			{
				if (char.IsLetter((char)item[3]) && item[4] == ':' && item[5] == '\\')
				{
					return ((char)item[3]).ToString().ToUpperInvariant() + ":\\";
				}
			}

			// Root CLSID (type 0x1F) -> ignore
			if (type == 0x1F) return null;

			// Try BEEF extension block (0xBEEF / 0xEFBE) for Unicode Long Name
			for (int i = 0; i < item.Length - 8; i++)
			{
				if (item[i] == 0xEF && item[i + 1] == 0xBE)
				{
					// In Windows Shell Item, Unicode name starts at i + 40
					int uStart = i + 40;
					if (uStart < item.Length - 2)
					{
						int end = uStart;
						while (end < item.Length - 1 && !(item[end] == 0 && item[end + 1] == 0)) end += 2;
						int len = end - uStart;
						if (len >= 2)
						{
							string s = Encoding.Unicode.GetString(item, uStart, len).Trim();
							if (IsValidPart(s)) return s;
						}
					}
				}
			}

			// Fallback: ANSI short name at offset 14 (standard for 0x31 / 0x32 items)
			if (item.Length > 14)
			{
				int end = 14;
				while (end < item.Length && item[end] != 0) end++;
				int len = end - 14;
				if (len >= 1 && len < 260)
				{
					string s = Encoding.Default.GetString(item, 14, len).Trim();
					if (IsValidPart(s)) return s;
				}
			}

			return null;
		}

		private static bool IsValidPart(string s)
		{
			if (string.IsNullOrEmpty(s) || s == "." || s == "..") return false;
			if (s.StartsWith("@shell32", StringComparison.OrdinalIgnoreCase)) return false;
			return true;
		}

		private static string ExtractBestFilenameFallback(byte[] bytes, string expectedExt)
		{
			if (bytes == null || bytes.Length < 6) return "Item";

			// Scan Unicode strings
			string best = null;
			for (int i = 0; i < bytes.Length - 4; i += 2)
			{
				if (bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] == 0 &&
					bytes[i + 2] >= 0x20 && bytes[i + 2] <= 0x7E && bytes[i + 3] == 0)
				{
					int end = i;
					while (end < bytes.Length - 1 && !(bytes[end] == 0 && bytes[end + 1] == 0)) end += 2;
					int len = end - i;
					if (len >= 4 && len < 260)
					{
						string s = Encoding.Unicode.GetString(bytes, i, len).Trim();
						if (IsValidPart(s))
						{
							if (!string.IsNullOrEmpty(expectedExt) && s.EndsWith("." + expectedExt, StringComparison.OrdinalIgnoreCase))
							{
								return s;
							}
							best = s;
						}
						i = end;
					}
				}
			}

			if (!string.IsNullOrEmpty(best)) return best;

			// Scan ASCII strings
			for (int i = 0; i < bytes.Length - 4; i++)
			{
				if (bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] >= 0x20 && bytes[i + 1] <= 0x7E)
				{
					int end = i;
					while (end < bytes.Length && bytes[end] >= 0x20 && bytes[end] <= 0x7E) end++;
					int len = end - i;
					if (len >= 4 && len < 260)
					{
						string s = Encoding.Default.GetString(bytes, i, len).Trim();
						if (IsValidPart(s))
						{
							if (!string.IsNullOrEmpty(expectedExt) && s.EndsWith("." + expectedExt, StringComparison.OrdinalIgnoreCase))
							{
								return s;
							}
							best = s;
						}
						i = end;
					}
				}
			}

			return best ?? "Item";
		}

		private static List<string> ExtractAllStrings(byte[] bytes)
		{
			List<string> list = new List<string>();
			if (bytes == null || bytes.Length == 0) return list;
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Unicode
			for (int i = 0; i < bytes.Length - 4; i += 2)
			{
				if (bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] == 0 &&
					bytes[i + 2] >= 0x20 && bytes[i + 2] <= 0x7E && bytes[i + 3] == 0)
				{
					int end = i;
					while (end < bytes.Length - 1 && !(bytes[end] == 0 && bytes[end + 1] == 0)) end += 2;
					int len = end - i;
					if (len >= 4 && len < 500)
					{
						string s = Encoding.Unicode.GetString(bytes, i, len).Trim();
						if (s.Length >= 2 && !seen.Contains(s))
						{
							seen.Add(s);
							list.Add(string.Format("[UTF-16 @ 0x{0:X4}]: {1}", i, s));
						}
						i = end;
					}
				}
			}

			// ASCII
			for (int i = 0; i < bytes.Length - 3; i++)
			{
				if (bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] >= 0x20 && bytes[i + 1] <= 0x7E && bytes[i + 2] >= 0x20 && bytes[i + 2] <= 0x7E)
				{
					int end = i;
					while (end < bytes.Length && bytes[end] >= 0x20 && bytes[end] <= 0x7E) end++;
					int len = end - i;
					if (len >= 3 && len < 300)
					{
						string s = Encoding.Default.GetString(bytes, i, len).Trim();
						if (s.Length >= 3 && !seen.Contains(s))
						{
							seen.Add(s);
							list.Add(string.Format("[ASCII  @ 0x{0:X4}]: {1}", i, s));
						}
						i = end;
					}
				}
			}

			return list;
		}

		private static string GenerateHexDump(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0) return "(Пустой массив байт)";
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Format("СМЕЩЕНИЕ  00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F   ASCII"));
			sb.AppendLine("----------------------------------------------------------------------");

			int len = bytes.Length;
			for (int i = 0; i < len; i += 16)
			{
				int chunk = Math.Min(16, len - i);
				StringBuilder hex = new StringBuilder();
				StringBuilder asc = new StringBuilder();

				for (int j = 0; j < 16; j++)
				{
					if (j < chunk)
					{
						byte b = bytes[i + j];
						hex.Append(b.ToString("X2") + " ");
						if (j == 7) hex.Append(" ");
						asc.Append(b >= 32 && b <= 126 ? (char)b : '.');
					}
					else
					{
						hex.Append("   ");
						if (j == 7) hex.Append(" ");
					}
				}

				sb.AppendLine(string.Format("{0:X8}  {1}  {2}", i, hex.ToString(), asc.ToString()));
			}
			return sb.ToString();
		}

		#endregion

		#region Threat Intelligence & Whitelist

		private static bool IsCheatOrInjector(string target)
		{
			if (string.IsNullOrEmpty(target)) return false;
			string t = target.ToLowerInvariant();

			string[] cheatSignatures = new string[]
			{
				"vape", "celestial", "expensive", "deadcode", "drip", "driplite", "slinky", "koid",
				"wild", "akrien", "minced", "neverhook", "karon", "monolith", "florian", "nurik",
				"nur", "wurst", "liquidbounce", "sigma", "impact", "inertia", "ares", "future",
				"rusherhack", "kamiblue", "meteor", "bleachhack", "aristois", "xenos", "extremeinjector",
				"cheatengine", "processhacker", "slapper", "journaltrace", "usndelete", "spoofer", "tmac",
				"smac", "r-wipe", "bleachbit", "cleaner", "bypasser", "nullclient", "freecam"
			};

			foreach (string sig in cheatSignatures)
			{
				if (t.Contains(sig)) return true;
			}

			if (CheatDatabases.ExtraCheatClients != null)
			{
				foreach (string cl in CheatDatabases.ExtraCheatClients)
				{
					if (cl.Length >= 4 && t.Contains(cl.ToLowerInvariant())) return true;
				}
			}

			return false;
		}

		private static bool IsWhitelistedApp(string appName)
		{
			if (string.IsNullOrEmpty(appName)) return false;
			string a = appName.ToLowerInvariant();

			string[] safeApps = new string[]
			{
				"explorer.exe", "javaw.exe", "java.exe", "chrome.exe", "firefox.exe", "opera.exe",
				"msedge.exe", "browser.exe", "discord.exe", "telegram.exe", "obs64.exe", "obs32.exe",
				"code.exe", "devenv.exe", "notepad.exe", "mspaint.exe", "anydesk.exe", "rudesktop.exe",
				"cmd.exe", "powershell.exe", "taskmgr.exe", "mmc.exe"
			};

			foreach (string sa in safeApps)
			{
				if (a == sa || a.EndsWith("\\" + sa)) return true;
			}

			return false;
		}

		private static bool IsWhitelistedFileOrPath(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			string p = path.ToLowerInvariant();

			if (CheatDatabases.IsWhitelistedPath(p)) return true;

			// Allowed autoclickers & ScreenShare tools
			string[] allowed = new string[]
			{
				"opautoclicker", "speedautoclicker", "tinytask", "x-mouse", "autohotkey",
				"clickermann", "keyran", "anydesk", "rudesktop"
			};

			foreach (string al in allowed)
			{
				if (p.Contains(al)) return true;
			}

			return false;
		}

		private static bool IsSensitiveFolder(string pathLower)
		{
			if (string.IsNullOrEmpty(pathLower)) return false;
			return pathLower.Contains("\\temp\\") ||
				   pathLower.Contains("\\tmp\\") ||
				   pathLower.Contains("\\downloads\\") ||
				   pathLower.Contains("\\desktop\\") ||
				   pathLower.Contains("\\appdata\\local\\temp") ||
				   pathLower.Contains("\\users\\");
		}

		#endregion

		#region Forensic Report Construction

		private string BuildLastVisitedReport(ComDlgForensicItem item, string callingExe, string folderPath, bool dirExists, bool isInjector, bool isAllowed)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("==========================================================================");
			sb.AppendLine("    СУДЕБНЫЙ ОТЧЕТ: АУДИТ ДИАЛОГОВ LASTVISITEDPIDLMRU (COMDLG32)");
			sb.AppendLine("==========================================================================");
			sb.AppendLine(string.Format("Целевой игрок проверки : {0}", targetPlayerName));
			sb.AppendLine(string.Format("Вызывающий процесс     : {0}", callingExe));
			sb.AppendLine(string.Format("Хронологический ранг   : {0} в MRUListEx", item.MruOrderDisplay));
			sb.AppendLine(string.Format("Выбранный каталог      : {0}", string.IsNullOrEmpty(folderPath) ? "—" : folderPath));
			sb.AppendLine(string.Format("Статус папки на диске  : {0}", dirExists ? "✅ Присутствует на накопителе" : "🚨 УДАЛЕНА ИЛИ НЕДОСТУПНА"));
			sb.AppendLine(string.Format("Криминалистический вердикт: {0}", item.Verdict));
			sb.AppendLine(string.Format("Применимый пункт правил: {0}", item.TargetRule));
			sb.AppendLine("--------------------------------------------------------------------------");
			sb.AppendLine("ФОРЕНЗИЧЕСКАЯ СПРАВКА:");
			sb.AppendLine("Ключ LastVisitedPidlMRU связывает исполняемый файл приложения с последней");
			sb.AppendLine("папкой, в которой данный процесс вызывал системный диалог выбора файлов");
			sb.AppendLine("(GetOpenFileNameW / IFileDialog).");
			sb.AppendLine();

			if (isInjector)
			{
				sb.AppendLine("🚨 ВНИМАНИЕ: Процесс-инжектор читов открывал диалог выбора файлов!");
				sb.AppendLine("Это прямое неопровержимое свидетельство попытки инжекта DLL или выбора чита.");
				sb.AppendLine(string.Format("Рекомендуемое действие: Выдача бана по правилу 5.01 ({0}).", item.TargetRule));
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.01 Использование читов (Инжектор {1} в LastVisitedPidlMRU)", targetPlayerName, callingExe));
			}
			else if (!dirExists && !string.IsNullOrEmpty(folderPath) && IsSensitiveFolder(folderPath.ToLowerInvariant()))
			{
				sb.AppendLine("⚠️ ВНИМАНИЕ: Папка, которую открывал данный процесс, была удалена с накопителя!");
				sb.AppendLine("Если в этой папке находился софт или читы перед вызовом на ScreenShare —");
				sb.AppendLine("это квалифицируется как сокрытие следов использования стороннего ПО (5.08).");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.08 Сокрытие улик (Удалена папка {1})", targetPlayerName, item.ItemName));
			}
			else if (isAllowed)
			{
				sb.AppendLine("⚪ Данный процесс входит в белый список разрешенных системных и прикладных программ.");
			}
			else
			{
				sb.AppendLine("Запись отражает штатную работу системного Проводника или прикладного софта.");
			}

			sb.AppendLine("==========================================================================");
			return sb.ToString();
		}

		private string BuildOpenSaveReport(ComDlgForensicItem item, string subkey, string resolvedPath, string fName, bool fileExists, bool isCheat, bool isWhitelisted)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("==========================================================================");
			sb.AppendLine("    СУДЕБНЫЙ ОТЧЕТ: АУДИТ ДИАЛОГОВ OPENSAVEPIDLMRU (COMDLG32)");
			sb.AppendLine("==========================================================================");
			sb.AppendLine(string.Format("Целевой игрок проверки : {0}", targetPlayerName));
			sb.AppendLine(string.Format("Подключ расширения     : OpenSavePidlMRU\\{0}", subkey));
			sb.AppendLine(string.Format("Хронологический ранг   : {0} в MRUListEx", item.MruOrderDisplay));
			sb.AppendLine(string.Format("Имя файла              : {0}", fName));
			sb.AppendLine(string.Format("Реконструированный путь: {0}", string.IsNullOrEmpty(resolvedPath) ? "(Не полный путь)" : resolvedPath));
			sb.AppendLine(string.Format("Физический статус файла: {0}", fileExists ? "✅ Присутствует на накопителе" : "🚨 ФАЙЛ УДАЛЕН С ДИСКА"));
			sb.AppendLine(string.Format("Криминалистический вердикт: {0}", item.Verdict));
			sb.AppendLine(string.Format("Применимый пункт правил: {0}", item.TargetRule));
			sb.AppendLine("--------------------------------------------------------------------------");
			sb.AppendLine("ФОРЕНЗИЧЕСКАЯ СПРАВКА:");
			sb.AppendLine("OpenSavePidlMRU сохраняет бинарные потоки PIDL (Shell Item IDList) для каждого");
			sb.AppendLine("файла, выбранного или сохраненного пользователем в системных диалоговых окнах.");
			sb.AppendLine("Данный реестровый след ПЕРЕЖИВАЕТ удаление самого файла с диска, очистку Prefetch");
			sb.AppendLine("и большинство примитивных клинеров, фиксируя точное историческое присутствие файла.");
			sb.AppendLine();

			if (isCheat)
			{
				if (!fileExists)
				{
					sb.AppendLine("🚨 КРИТИЧЕСКИЙ ВЕРДИКТ: ЧИТ-ПРОГРАММА БЫЛА УДАЛЕНА С НАКОПИТЕЛЯ!");
					sb.AppendLine("Игрок выбирал чит через диалог открытия/сохранения, а затем удалил его");
					sb.AppendLine("для сокрытия улик перед ScreenShare проверкой (Пункт 5.08 регламента).");
					sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.08 Сокрытие улик (Удален выбранный в диалоге чит: {1})", targetPlayerName, fName));
				}
				else
				{
					sb.AppendLine("🔴 КРИТИЧЕСКИЙ ВЕРДИКТ: ОБНАРУЖЕН ДЕЙСТВУЮЩИЙ ЧИТ НА ДИСКЕ!");
					sb.AppendLine("Чит-программа присутствует в файловой системе и была активирована через диалог.");
					sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.01 Использование читов ({1})", targetPlayerName, fName));
				}
			}
			else if (isWhitelisted)
			{
				sb.AppendLine("⚪ Данный файл относится к белому списку разрешенного ПО (автокликеры/ScreenShare софт).");
				sb.AppendLine("Наказание по пунктам 5.01 и 5.08 НЕ применимо.");
			}
			else if (!fileExists && item.IsDeleted508)
			{
				sb.AppendLine("⚠️ ВНИМАНИЕ: Исполняемый файл/библиотека (.exe, .dll, .jar) был удален из системной");
				sb.AppendLine("папки Temp/Downloads/Desktop после обращения к нему через системный диалог.");
				sb.AppendLine("Если файл удален за несколько минут до проверки, это указывает на зачистку улик.");
				sb.AppendLine(string.Format("Команда бана: /ban {0} 30d 5.08 Сокрытие улик (Удален исполняемый файл: {1})", targetPlayerName, fName));
			}
			else
			{
				sb.AppendLine("Запись отражает легитимную операцию открытия или сохранения файла пользователем.");
			}

			sb.AppendLine("==========================================================================");
			return sb.ToString();
		}

		#endregion

		#region Context Actions

		private void CopySelectedPath()
		{
			if (listView.SelectedItems.Count == 0) return;
			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null) return;

			string text = !string.IsNullOrEmpty(item.FullPath) ? item.FullPath : item.ItemName;
			if (!string.IsNullOrEmpty(text))
			{
				Clipboard.SetText(text);
				MessageBox.Show("Путь скопирован в буфер обмена:\n" + text, "Копирование пути", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void OpenSelectedFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null) return;

			string p = item.FullPath;
			if (string.IsNullOrEmpty(p)) return;

			try
			{
				if (File.Exists(p))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", p));
				}
				else if (Directory.Exists(p))
				{
					Process.Start("explorer.exe", string.Format("\"{0}\"", p));
				}
				else
				{
					string dir = Path.GetDirectoryName(p);
					if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					{
						Process.Start("explorer.exe", string.Format("\"{0}\"", dir));
					}
					else
					{
						MessageBox.Show("Файл или каталог не найден на диске (возможно, был удален):\n" + p, "Каталог отсутствует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия проводника: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopySelectedDiscordRow()
		{
			if (listView.SelectedItems.Count == 0) return;
			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null) return;

			string row = string.Format("📁 [ComDlg32 {0}] {1} | MRU: {2} | Путь: {3} | Статус: {4} | Вердикт: {5}",
				item.Category == DialogCategory.LastVisitedMRU ? "LastVisited" : "OpenSave",
				item.CallingApp,
				item.MruOrderDisplay,
				string.IsNullOrEmpty(item.FullPath) ? item.ItemName : item.FullPath,
				item.ExistenceStatusText,
				item.Verdict);

			Clipboard.SetText(row);
			MessageBox.Show("Строка скопирована в буфер обмена для Discord/VK:\n" + row, "Строка улик", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopySelectedBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null) return;

			string cmd;
			if (item.IsCheat501)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов ({1})", targetPlayerName, item.ItemName);
			}
			else if (item.IsDeleted508)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Удален выбранный в диалоге чит: {1})", targetPlayerName, item.ItemName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 Стороннее ПО ({1})", targetPlayerName, item.ItemName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopySelectedHexDump()
		{
			if (listView.SelectedItems.Count == 0) return;
			ComDlgForensicItem item = listView.SelectedItems[0].Tag as ComDlgForensicItem;
			if (item == null || string.IsNullOrEmpty(item.HexDump)) return;

			Clipboard.SetText(item.HexDump);
			MessageBox.Show("Hex-дамп PIDL скопирован в буфер обмена (" + (item.RawBytes != null ? item.RawBytes.Length : 0) + " байт).", "Hex-дамп", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "HTML Report (*.html)|*.html|Текстовый отчет (*.txt)|*.txt";
				sfd.FileName = string.Format("ComDlg32_ForensicReport_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

				if (sfd.ShowDialog() != DialogResult.OK) return;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("<!DOCTYPE html>");
				sb.AppendLine("<html lang=\"ru\">");
				sb.AppendLine("<head>");
				sb.AppendLine("<meta charset=\"utf-8\" />");
				sb.AppendLine("<title>Судебная экспертиза диалогов ComDlg32 — " + targetPlayerName + "</title>");
				sb.AppendLine("<style>");
				sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #121417; color: #E1E3E6; margin: 20px; }");
				sb.AppendLine("h1 { color: #4FA3E3; margin-bottom: 4px; }");
				sb.AppendLine(".sub { color: #8C939D; font-size: 13px; margin-bottom: 20px; }");
				sb.AppendLine(".kpi-container { display: flex; gap: 12px; margin-bottom: 24px; }");
				sb.AppendLine(".kpi-card { background: #1A1D21; border: 1px solid #282C34; border-radius: 6px; padding: 12px 16px; min-width: 130px; text-align: center; }");
				sb.AppendLine(".kpi-title { font-size: 11px; color: #8C939D; text-transform: uppercase; margin-bottom: 6px; }");
				sb.AppendLine(".kpi-val { font-size: 20px; font-weight: bold; }");
				sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 12px; }");
				sb.AppendLine("th, td { border: 1px solid #282C34; padding: 8px 10px; text-align: left; }");
				sb.AppendLine("th { background-color: #1E2228; color: #A0A8B4; }");
				sb.AppendLine("tr:nth-child(even) { background-color: #16191D; }");
				sb.AppendLine(".bad { color: #FF6E6E; font-weight: bold; }");
				sb.AppendLine(".warn { color: #FFB347; font-weight: bold; }");
				sb.AppendLine(".good { color: #78D98C; }");
				sb.AppendLine(".footer { margin-top: 30px; font-size: 11px; color: #5C6370; }");
				sb.AppendLine("</style>");
				sb.AppendLine("</head>");
				sb.AppendLine("<body>");
				sb.AppendLine("<h1>📁 Судебная экспертиза диалогов ComDlg32 (OpenSavePidlMRU / LastVisitedPidlMRU)</h1>");
				sb.AppendLine(string.Format("<div class=\"sub\">Игрок: <b>{0}</b> | Дата генерации: {1:dd.MM.yyyy HH:mm:ss} | Экспертный модуль MCCheatChecker</div>", targetPlayerName, DateTime.Now));

				sb.AppendLine("<div class=\"kpi-container\">");
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">Всего записей</div><div class=\"kpi-val\" style=\"color:#E1E3E6;\">{0}</div></div>", lblKpiTotal.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">🚨 Удалено (5.08)</div><div class=\"kpi-val\" style=\"color:#FF6E6E;\">{0}</div></div>", lblKpiDeleted508.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">🔴 Читы (5.01)</div><div class=\"kpi-val\" style=\"color:#FFB347;\">{0}</div></div>", lblKpiCheat501.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">📂 LastVisited</div><div class=\"kpi-val\" style=\"color:#4FA3E3;\">{0}</div></div>", lblKpiLastVisited.Text));
				sb.AppendLine(string.Format("<div class=\"kpi-card\"><div class=\"kpi-title\">⚪ Белый список</div><div class=\"kpi-val\" style=\"color:#78D98C;\">{0}</div></div>", lblKpiSafe.Text));
				sb.AppendLine("</div>");

				sb.AppendLine("<table>");
				sb.AppendLine("<thead><tr><th>#</th><th>MRU</th><th>Тип / Ключ</th><th>Процесс</th><th>Имя файла / Папка</th><th>Полный путь</th><th>Статус</th><th>Вердикт</th><th>Регламент</th></tr></thead>");
				sb.AppendLine("<tbody>");

				int r = 1;
				foreach (ComDlgForensicItem it in allItems)
				{
					string css = it.IsDeleted508 ? "bad" : (it.IsCheat501 ? "warn" : (it.IsAllowed ? "good" : ""));
					sb.AppendLine("<tr>");
					sb.AppendLine(string.Format("<td>{0}</td>", r++));
					sb.AppendLine(string.Format("<td>{0}</td>", it.MruOrderDisplay));
					sb.AppendLine(string.Format("<td>{0}</td>", it.Extension));
					sb.AppendLine(string.Format("<td>{0}</td>", it.CallingApp));
					sb.AppendLine(string.Format("<td><b>{0}</b></td>", it.ItemName));
					sb.AppendLine(string.Format("<td><code>{0}</code></td>", it.FullPath));
					sb.AppendLine(string.Format("<td class=\"{0}\">{1}</td>", css, it.ExistenceStatusText));
					sb.AppendLine(string.Format("<td class=\"{0}\">{1}</td>", css, it.Verdict));
					sb.AppendLine(string.Format("<td>{0}</td>", it.TargetRule));
					sb.AppendLine("</tr>");
				}

				sb.AppendLine("</tbody></table>");
				sb.AppendLine("<div class=\"footer\">Отчет сформирован программным комплексом MCCheatChecker. Все права защищены.</div>");
				sb.AppendLine("</body></html>");

				File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
				MessageBox.Show("Судебный отчет успешно сохранен в файл:\n" + sfd.FileName, "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка сохранения отчета: " + ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
