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
	public class JvmInjectionAndProxyDllViewerForm : Form
	{
		#region Enums & Data Models

		public enum JvmItemCategory
		{
			ProxyDllHijack,
			EnvironmentVariable,
			LauncherAgentArg,
			RunningProcessModule,
			JreIntegrityInfo,
			DeletedEvidence508
		}

		public enum JvmFilterCategory
		{
			All,
			ViolationsOnly,
			ProxyDlls,
			JavaAgents,
			EnvVars,
			ProcessModules,
			CleanRuntimes
		}

		public class JvmForensicItem
		{
			public int Index;
			public JvmItemCategory Category;
			public string CategoryName;
			public string ObjectName;
			public string PathOrLocation;
			public string DetailsOrValue;
			public string SignatureOrState;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsProxyDll;
			public bool IsJavaAgent;
			public bool IsEnvVar;
			public bool IsProcessModule;
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
		private JvmFilterCategory currentFilter = JvmFilterCategory.All;
		private List<JvmForensicItem> allItems = new List<JvmForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtTechnicalDetails;
		private DarkTextBoxMultiline txtCertificateDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotalJre;
		private Label lblKpiViolations;
		private Label lblKpiProxyDlls;
		private Label lblKpiJavaAgents;
		private Label lblKpiEnvVars;
		private Label lblKpiRunningJava;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterProxyDlls;
		private DarkButton btnFilterJavaAgents;
		private DarkButton btnFilterEnvVars;
		private DarkButton btnFilterProcessModules;
		private DarkButton btnFilterClean;

		private DarkButton btnCopyRow;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		private TabControl detailTabs;

		#endregion

		public JvmInjectionAndProxyDllViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "☕ Инспектор JVM инъекций, прокси DLL (dwrite/version/opengl/dxgi) и переменных окружения — MCCheatChecker";
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
			lblHeaderTitle.Text = "☕ ИНСПЕКТОР JVM ИНЪЕКЦИЙ, ПРОКСИ DLL И ПЕРЕМЕННЫХ ОКРУЖЕНИЯ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит подмены библиотек dwrite/version/opengl32 в JRE bin, скрытых параметров _JAVA_OPTIONS, удаленных JavaAgent и инъекций javaw (5.01 / 5.08)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ JVM...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(230, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// KPI Panel inside top panel
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
			kpiPanel.Controls.Add(CreateKpiBadge("☕ JRE каталогов", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotalJre));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01/5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Прокси DLL (JRE)", "0", Ui.AccentOrange, ref kpiX, out lblKpiProxyDlls));
			kpiPanel.Controls.Add(CreateKpiBadge("☕ JavaAgent & Арг.", "0", Ui.AccentPurple, ref kpiX, out lblKpiJavaAgents));
			kpiPanel.Controls.Add(CreateKpiBadge("⚙️ Переменные JVM", "0", Ui.AccentBlue, ref kpiX, out lblKpiEnvVars));
			kpiPanel.Controls.Add(CreateKpiBadge("⚡ Процессы javaw", "0", Ui.AccentGreen, ref kpiX, out lblKpiRunningJava));
			topPanel.Controls.Add(kpiPanel);

			Controls.Add(topPanel);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все объекты", JvmFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", JvmFilterCategory.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterProxyDlls = CreateFilterButton("💉 Прокси DLL", JvmFilterCategory.ProxyDlls, ref fBtnX, filterBar);
			btnFilterJavaAgents = CreateFilterButton("☕ JavaAgent", JvmFilterCategory.JavaAgents, ref fBtnX, filterBar);
			btnFilterEnvVars = CreateFilterButton("⚙️ Переменные среды", JvmFilterCategory.EnvVars, ref fBtnX, filterBar);
			btnFilterProcessModules = CreateFilterButton("⚡ Процессы javaw", JvmFilterCategory.ProcessModules, ref fBtnX, filterBar);
			btnFilterClean = CreateFilterButton("✔️ Чистые JRE", JvmFilterCategory.CleanRuntimes, ref fBtnX, filterBar);

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

			// SplitContainer for ListView and Bottom Detail Pane
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
			listView.Columns.Add("Категория", 175);
			listView.Columns.Add("Объект / Переменная / Файл", 215);
			listView.Columns.Add("Расположение / Параметры", 345);
			listView.Columns.Add("Состояние / Подпись / Хеш", 210);
			listView.Columns.Add("Регламент", 115);
			listView.Columns.Add("Криминалистический вердикт", 215);

			listView.SelectedIndexChanged += delegate { UpdateSelectedDetails(); };
			listView.DoubleClick += delegate { CopySelectedRowText(); };
			Ui.ApplyDarkScrollbars(listView);
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Panel (Tabs + Actions)
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

			TabPage tabTech = new TabPage("🔍 Технические детали & Аргументы");
			tabTech.BackColor = Ui.Surface;
			txtTechnicalDetails = new DarkTextBoxMultiline();
			txtTechnicalDetails.Dock = DockStyle.Fill;
			txtTechnicalDetails.ReadOnly = true;
			txtTechnicalDetails.Font = Ui.FontMono(9f);
			tabTech.Controls.Add(txtTechnicalDetails);
			detailTabs.TabPages.Add(tabTech);

			TabPage tabCert = new TabPage("🔏 Цифровая подпись & Хеш");
			tabCert.BackColor = Ui.Surface;
			txtCertificateDetails = new DarkTextBoxMultiline();
			txtCertificateDetails.Dock = DockStyle.Fill;
			txtCertificateDetails.ReadOnly = true;
			txtCertificateDetails.Font = Ui.FontMono(9f);
			tabCert.Controls.Add(txtCertificateDetails);
			detailTabs.TabPages.Add(tabCert);

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

		private DarkButton CreateFilterButton(string text, JvmFilterCategory category, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(125, 28);
			btn.Location = new Point(curX, 9);
			btn.Click += delegate
			{
				currentFilter = category;
				UpdateFilterButtonHighlights();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 131;
			return btn;
		}

		private void UpdateFilterButtonHighlights()
		{
			Color normalBg = Ui.Surface;
			Color activeBg = Ui.AccentBlue;
			Color activeText = Color.Black;
			Color normalText = Ui.TextPrimary;

			SetBtnStyle(btnFilterAll, currentFilter == JvmFilterCategory.All, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterViolations, currentFilter == JvmFilterCategory.ViolationsOnly, normalBg, Ui.AccentRed, normalText, Color.White);
			SetBtnStyle(btnFilterProxyDlls, currentFilter == JvmFilterCategory.ProxyDlls, normalBg, Ui.AccentOrange, normalText, Color.Black);
			SetBtnStyle(btnFilterJavaAgents, currentFilter == JvmFilterCategory.JavaAgents, normalBg, Ui.AccentPurple, normalText, Color.White);
			SetBtnStyle(btnFilterEnvVars, currentFilter == JvmFilterCategory.EnvVars, normalBg, activeBg, normalText, activeText);
			SetBtnStyle(btnFilterProcessModules, currentFilter == JvmFilterCategory.ProcessModules, normalBg, Ui.AccentGreen, normalText, Color.Black);
			SetBtnStyle(btnFilterClean, currentFilter == JvmFilterCategory.CleanRuntimes, normalBg, Ui.AccentGreen, normalText, Color.Black);
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

			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ JVM...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			listView.Items.Clear();
			allItems.Clear();
			txtForensicReport.Clear();
			txtTechnicalDetails.Clear();
			txtCertificateDetails.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformJvmScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования JVM: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.BackColor = Ui.AccentRed;
					lblStatusPill.ForeColor = Color.White;
					return;
				}

				List<JvmForensicItem> items = e.Result as List<JvmForensicItem>;
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

		private List<JvmForensicItem> PerformJvmScan()
		{
			List<JvmForensicItem> results = new List<JvmForensicItem>();
			int idx = 1;

			// List of known DLLs targeted by Proxy DLL Hijacking (DLL Sideloading)
			string[] targetProxyDlls = new string[]
			{
				"dwrite.dll",
				"version.dll",
				"opengl32.dll",
				"dxgi.dll",
				"d2d1.dll",
				"d3d11.dll",
				"d3d9.dll",
				"winmm.dll",
				"userenv.dll",
				"dbghelp.dll",
				"cryptsp.dll",
				"wintrust.dll"
			};

			// Step 1: Discover all Java Runtime Environments (JRE/JDK)
			HashSet<string> scannedBinDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> candidateJreRoots = DiscoverCandidateJreDirectories();

			foreach (string jreRoot in candidateJreRoots)
			{
				if (!Directory.Exists(jreRoot)) continue;

				// Check for bin directory inside jreRoot or jreRoot itself
				string binDir = Path.Combine(jreRoot, "bin");
				if (!Directory.Exists(binDir))
				{
					if (File.Exists(Path.Combine(jreRoot, "javaw.exe")) || File.Exists(Path.Combine(jreRoot, "java.exe")))
					{
						binDir = jreRoot;
					}
					else
					{
						continue;
					}
				}

				if (scannedBinDirs.Contains(binDir)) continue;
				scannedBinDirs.Add(binDir);

				bool jreHasProxyDll = false;

				// Inspect binDir for Proxy DLLs
				try
				{
					foreach (string targetDll in targetProxyDlls)
					{
						string dllPath = Path.Combine(binDir, targetDll);
						if (File.Exists(dllPath))
						{
							jreHasProxyDll = true;
							FileInfo fi = new FileInfo(dllPath);
							string sha256 = ComputeSha256(dllPath);
							string certInfo = GetCertificateSummary(dllPath);
							bool isCleanSigned = IsLegitimateVendor(certInfo);

							JvmForensicItem item = new JvmForensicItem
							{
								Index = idx++,
								Category = JvmItemCategory.ProxyDllHijack,
								CategoryName = "💉 Прокси DLL (Sideload)",
								ObjectName = targetDll,
								PathOrLocation = dllPath,
								DetailsOrValue = string.Format("Обнаружен перехватчик в bin\\: {0:N0} байт, Изменен: {1:yyyy-MM-dd HH:mm:ss}", fi.Length, fi.LastWriteTime),
								SignatureOrState = isCleanSigned ? certInfo : "🚨 НЕПОДПИСАН / ЧИТ (Sideload)",
								TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
								Verdict = string.Format("🚨 5.01 ИНЖЕКТОР (Proxy DLL перехват {0})", targetDll),
								IsViolation = true,
								IsProxyDll = true,
								RowColor = Ui.AccentRed,
								Sha256Hash = sha256,
								FileSizeBytes = fi.Length,
								FileCreationTime = fi.CreationTime,
								FileLastWriteTime = fi.LastWriteTime,
								CertificateDetails = certInfo,
								TechnicalDetails = string.Format("Файл: {0}\r\nРазмер: {1} байт\r\nСоздан: {2}\r\nИзменен: {3}\r\nSHA-256: {4}\r\nПодпись: {5}",
									dllPath, fi.Length, fi.CreationTime, fi.LastWriteTime, sha256, certInfo),
								ForensicReport = string.Format(
									"🚨 ОБНАРУЖЕН ПЕРЕХВАТЧИК PROXY DLL В КАТАЛОГЕ СРЕДЫ JAVA!\r\n\r\n" +
									"Библиотека: {0}\r\n" +
									"Каталог JRE: {1}\r\n" +
									"Подлинные дистрибутивы Java (Oracle, Adoptium, BellSoft, Microsoft) НИКОГДА не содержат библиотеку '{0}' в папке bin\\!\r\n" +
									"Данная техника DLL Search Order Hijacking используется читами (Vape v4, Drip Lite, Celestial, Karon) для автоматического внедрения в javaw.exe до загрузки системных модулей.\r\n\r\n" +
									"Нарушение регламента: 5.01 (Использование читов / стороннего софта для инъекции).",
									targetDll, binDir)
							};
							results.Add(item);
						}
					}

					// Also check for msvcp140.dll / vcruntime140.dll / concrt140.dll in binDir
					string[] runtimeLibs = new string[] { "msvcp140.dll", "vcruntime140.dll", "concrt140.dll" };
					foreach (string rlib in runtimeLibs)
					{
						string rPath = Path.Combine(binDir, rlib);
						if (File.Exists(rPath))
						{
							string cert = GetCertificateSummary(rPath);
							if (!cert.Contains("Microsoft") && !cert.Contains("Corporation"))
							{
								FileInfo fi = new FileInfo(rPath);
								string sha = ComputeSha256(rPath);
								JvmForensicItem item = new JvmForensicItem
								{
									Index = idx++,
									Category = JvmItemCategory.ProxyDllHijack,
									CategoryName = "⚠️ Подозрительный Runtime DLL",
									ObjectName = rlib,
									PathOrLocation = rPath,
									DetailsOrValue = string.Format("Сторонняя C++ runtime библиотека без подписи Microsoft ({0:N0} байт)", fi.Length),
									SignatureOrState = "Не подписан Microsoft",
									TargetRule = "5.01 ПРОВЕРКА",
									Verdict = "⚠️ Подозрительная библиотека runtime в JRE",
									IsViolation = false,
									IsProxyDll = true,
									RowColor = Ui.AccentOrange,
									Sha256Hash = sha,
									FileSizeBytes = fi.Length,
									FileCreationTime = fi.CreationTime,
									FileLastWriteTime = fi.LastWriteTime,
									CertificateDetails = cert,
									TechnicalDetails = string.Format("Файл: {0}\r\nSHA-256: {1}\r\nПодпись: {2}", rPath, sha, cert),
									ForensicReport = "В каталоге Java Runtime обнаружена неподписанная библиотека " + rlib + ". Возможна маскировка стороннего загрузчика."
								};
								results.Add(item);
							}
						}
					}
				}
				catch { }

				// If no proxy DLLs found, record genuine JRE
				if (!jreHasProxyDll)
				{
					string jreTitle = Path.GetFileName(Path.GetDirectoryName(binDir.TrimEnd('\\', '/'))) ?? Path.GetFileName(binDir);
					if (string.IsNullOrEmpty(jreTitle)) jreTitle = binDir;

					JvmForensicItem cleanJre = new JvmForensicItem
					{
						Index = idx++,
						Category = JvmItemCategory.JreIntegrityInfo,
						CategoryName = "☕ Среда Java (JRE/JDK)",
						ObjectName = jreTitle,
						PathOrLocation = binDir,
						DetailsOrValue = "Подлинный каталог JRE, прокси-библиотеки отсутствуют",
						SignatureOrState = "✔️ Чисто (Прокси DLL не найдены)",
						TargetRule = "—",
						Verdict = "✔️ Подлинная среда выполнения",
						IsViolation = false,
						IsClean = true,
						RowColor = Ui.AccentGreen,
						TechnicalDetails = "Каталог JRE bin: " + binDir,
						ForensicReport = "Каталог " + binDir + " проверен. Системные перехватчики dwrite.dll, version.dll, opengl32.dll, dxgi.dll отсутствуют. Целостность подтверждена."
					};
					results.Add(cleanJre);
				}
			}

			// Step 2: Check .minecraft root and natives for Proxy DLLs
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string mcDir = Path.Combine(appData, ".minecraft");
			if (Directory.Exists(mcDir))
			{
				try
				{
					// Loose DLLs in .minecraft root
					string[] looseDlls = Directory.GetFiles(mcDir, "*.dll", SearchOption.TopDirectoryOnly);
					foreach (string lDll in looseDlls)
					{
						string fname = Path.GetFileName(lDll).ToLowerInvariant();
						FileInfo fi = new FileInfo(lDll);
						string sha = ComputeSha256(lDll);
						string cert = GetCertificateSummary(lDll);

						bool isProxyName = false;
						foreach (string pName in targetProxyDlls)
						{
							if (fname == pName) { isProxyName = true; break; }
						}

						JvmForensicItem item = new JvmForensicItem
						{
							Index = idx++,
							Category = JvmItemCategory.ProxyDllHijack,
							CategoryName = "💉 DLL в корне .minecraft",
							ObjectName = fname,
							PathOrLocation = lDll,
							DetailsOrValue = string.Format("DLL файл непосредственно в корне игры ({0:N0} байт)", fi.Length),
							SignatureOrState = cert,
							TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
							Verdict = isProxyName ?
								string.Format("🚨 5.01 PROXY DLL В КОРНЕ .MINECRAFT ({0})", fname) :
								string.Format("🚨 5.01 ПОСТОРОННЯЯ DLL В КОРНЕ .MINECRAFT ({0})", fname),
							IsViolation = true,
							IsProxyDll = true,
							RowColor = Ui.AccentRed,
							Sha256Hash = sha,
							FileSizeBytes = fi.Length,
							FileCreationTime = fi.CreationTime,
							FileLastWriteTime = fi.LastWriteTime,
							CertificateDetails = cert,
							TechnicalDetails = string.Format("Файл: {0}\r\nSHA-256: {1}\r\nРазмер: {2} байт\r\nСоздан: {3}\r\nИзменен: {4}",
								lDll, sha, fi.Length, fi.CreationTime, fi.LastWriteTime),
							ForensicReport = string.Format(
								"В папке .minecraft обнаружена библиотека '{0}'.\r\n" +
								"Официальный клиент Minecraft никогда не размещает DLL файлы в корневой директории .minecraft! " +
								"Данное размещение характерно для DLL инжекторов и читерских библиотек.\r\n" +
								"Нарушение пункта 5.01 регламента.", fname)
						};
						results.Add(item);
					}

					// Check .minecraft\bin and .minecraft\natives
					string[] subDirsToCheck = new string[] { Path.Combine(mcDir, "bin"), Path.Combine(mcDir, "natives") };
					foreach (string sDir in subDirsToCheck)
					{
						if (Directory.Exists(sDir))
						{
							foreach (string targetDll in targetProxyDlls)
							{
								string testPath = Path.Combine(sDir, targetDll);
								if (File.Exists(testPath))
								{
									FileInfo fi = new FileInfo(testPath);
									string sha = ComputeSha256(testPath);
									string cert = GetCertificateSummary(testPath);
									results.Add(new JvmForensicItem
									{
										Index = idx++,
										Category = JvmItemCategory.ProxyDllHijack,
										CategoryName = "💉 Прокси DLL в " + Path.GetFileName(sDir),
										ObjectName = targetDll,
										PathOrLocation = testPath,
										DetailsOrValue = string.Format("Перехватчик в директории {0} ({1:N0} байт)", Path.GetFileName(sDir), fi.Length),
										SignatureOrState = cert,
										TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
										Verdict = string.Format("🚨 5.01 PROXY DLL В {0} ({1})", Path.GetFileName(sDir).ToUpperInvariant(), targetDll),
										IsViolation = true,
										IsProxyDll = true,
										RowColor = Ui.AccentRed,
										Sha256Hash = sha,
										FileSizeBytes = fi.Length,
										FileCreationTime = fi.CreationTime,
										FileLastWriteTime = fi.LastWriteTime,
										CertificateDetails = cert,
										TechnicalDetails = string.Format("Файл: {0}\r\nSHA-256: {1}", testPath, sha),
										ForensicReport = string.Format("Обнаружен прокси-перехватчик {0} в {1}. 5.01 Использование читов.", targetDll, testPath)
									});
								}
							}
						}
					}
				}
				catch { }
			}

			// Step 3: Audit System and User Environment Variables
			string[] targetEnvVars = new string[]
			{
				"_JAVA_OPTIONS",
				"JAVA_TOOL_OPTIONS",
				"JDK_JAVA_OPTIONS",
				"JAVA_OPTIONS",
				"CLASSPATH"
			};

			// Scan User Registry: HKCU\Environment
			try
			{
				using (RegistryKey uKey = Registry.CurrentUser.OpenSubKey("Environment", false))
				{
					if (uKey != null)
					{
						foreach (string varName in targetEnvVars)
						{
							object valObj = uKey.GetValue(varName, null);
							if (valObj != null)
							{
								string valStr = valObj.ToString();
								AnalyzeEnvironmentVariable(ref idx, results, varName, valStr, "HKCU\\Environment (Пользователь)");
							}
						}
					}
				}
			}
			catch { }

			// Scan System Registry: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
			try
			{
				using (RegistryKey sKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", false))
				{
					if (sKey != null)
					{
						foreach (string varName in targetEnvVars)
						{
							object valObj = sKey.GetValue(varName, null);
							if (valObj != null)
							{
								string valStr = valObj.ToString();
								AnalyzeEnvironmentVariable(ref idx, results, varName, valStr, "HKLM\\...\\Session Manager\\Environment (Система)");
							}
						}
					}
				}
			}
			catch { }

			// Scan Active Process Environment
			try
			{
				foreach (string varName in targetEnvVars)
				{
					string val = Environment.GetEnvironmentVariable(varName);
					if (!string.IsNullOrEmpty(val))
					{
						// Check if already captured from registry
						bool alreadyCaptured = false;
						foreach (JvmForensicItem it in results)
						{
							if (it.Category == JvmItemCategory.EnvironmentVariable &&
								string.Equals(it.ObjectName, varName, StringComparison.OrdinalIgnoreCase) &&
								string.Equals(it.DetailsOrValue, val, StringComparison.OrdinalIgnoreCase))
							{
								alreadyCaptured = true;
								break;
							}
						}

						if (!alreadyCaptured)
						{
							AnalyzeEnvironmentVariable(ref idx, results, varName, val, "Процесс (Environment.GetEnvironmentVariable)");
						}
					}
				}
			}
			catch { }

			// Step 4: Audit Launcher Profiles for JavaAgent / jvmArgs Injections
			AuditLauncherProfiles(ref idx, results);

			// Step 5: Audit Live Running javaw.exe / java.exe Processes
			AuditRunningJavaProcesses(ref idx, results, targetProxyDlls);

			return results;
		}

		private void AnalyzeEnvironmentVariable(ref int idx, List<JvmForensicItem> results, string varName, string valStr, string location)
		{
			string lowerVal = valStr.ToLowerInvariant();

			// Case A: JavaAgent parameter detected
			if (lowerVal.Contains("-javaagent:") || lowerVal.Contains("-javaagent"))
			{
				string agentPath = ExtractPathFromArg(valStr, "-javaagent:");
				bool exists = !string.IsNullOrEmpty(agentPath) && File.Exists(agentPath);

				if (!exists && !string.IsNullOrEmpty(agentPath))
				{
					// File was deleted! -> 5.08 Tampering
					results.Add(new JvmForensicItem
					{
						Index = idx++,
						Category = JvmItemCategory.DeletedEvidence508,
						CategoryName = "🚨 5.08 Удаленный JavaAgent",
						ObjectName = varName,
						PathOrLocation = location,
						DetailsOrValue = string.Format("Удаленный агент: {0} (Значение: {1})", agentPath, valStr),
						SignatureOrState = "ОТСУТСТВУЕТ НА ДИСКЕ (УДАЛЕН)",
						TargetRule = "5.08 СОКРЫТИЕ УЛИК",
						Verdict = "🚨 5.08 СОКРЫТИЕ (Удален JavaAgent чит)",
						IsViolation = true,
						IsJavaAgent = true,
						IsEnvVar = true,
						RowColor = Ui.AccentRed,
						TechnicalDetails = string.Format("Переменная: {0}\r\nИсточник: {1}\r\nПолное значение: {2}\r\nИзвлеченный путь: {3}\r\nСуществование: FALSE (Файл удален)",
							varName, location, valStr, agentPath),
						ForensicReport = string.Format(
							"🚨 ОБНАРУЖЕНЫ СЛЕДЫ УДАЛЕНИЯ ЧИТА (JAVA AGENT)!\r\n\r\n" +
							"В реестре/переменных среды '{0}' обнаружен параметр внедрения JavaAgent:\r\n" +
							"{1}\r\n\r\n" +
							"Целевой файл: {2}\r\n" +
							"Файл на диске ОТСУТСТВУЕТ (был удален перед проверкой ScreenShare)!\r\n" +
							"Оставшаяся переменная среды является неопровержимым доказательством запуска чита и последующего сокрытия улик.\r\n\r\n" +
							"Нарушение регламента: 5.08 (Сокрытие улик / удаление читов перед проверкой).",
							varName, valStr, agentPath)
					});
					return;
				}
				else
				{
					// File exists -> 5.01 Active Cheat Agent
					string sha = !string.IsNullOrEmpty(agentPath) && File.Exists(agentPath) ? ComputeSha256(agentPath) : "—";
					results.Add(new JvmForensicItem
					{
						Index = idx++,
						Category = JvmItemCategory.EnvironmentVariable,
						CategoryName = "🚨 5.01 JavaAgent в переменных",
						ObjectName = varName,
						PathOrLocation = location,
						DetailsOrValue = string.Format("Активный агент: {0} ({1})", agentPath, valStr),
						SignatureOrState = "АКТИВНЫЙ ИНЖЕКТОР ЧИТА",
						TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
						Verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ (JavaAgent)",
						IsViolation = true,
						IsJavaAgent = true,
						IsEnvVar = true,
						RowColor = Ui.AccentRed,
						Sha256Hash = sha,
						TechnicalDetails = string.Format("Переменная: {0}\r\nИсточник: {1}\r\nЗначение: {2}\r\nSHA-256 агента: {3}",
							varName, location, valStr, sha),
						ForensicReport = string.Format(
							"🚨 ОБНАРУЖЕН АКТИВНЫЙ JAVAAGENT ИНЖЕКТОР В ПЕРЕМЕННЫХ ОКРУЖЕНИЯ!\r\n\r\n" +
							"Переменная: {0}\r\n" +
							"Значение: {1}\r\n" +
							"Агент внедряется в каждый запускаемый процесс Java/Minecraft.\r\n" +
							"Нарушение регламента: 5.01 (Использование читов).",
							varName, valStr)
					});
					return;
				}
			}

			// Case B: -noverify / -Xverify:none
			if (lowerVal.Contains("-noverify") || lowerVal.Contains("-xverify:none"))
			{
				results.Add(new JvmForensicItem
				{
					Index = idx++,
					Category = JvmItemCategory.EnvironmentVariable,
					CategoryName = "⚠️ Параметр -noverify",
					ObjectName = varName,
					PathOrLocation = location,
					DetailsOrValue = valStr,
					SignatureOrState = "Отключение проверки байткода",
					TargetRule = "5.01 ПРОВЕРКА",
					Verdict = "⚠️ Отключение верификации байткода (-noverify)",
					IsViolation = false,
					IsEnvVar = true,
					RowColor = Ui.AccentOrange,
					TechnicalDetails = string.Format("Переменная: {0}\r\nИсточник: {1}\r\nЗначение: {2}", varName, location, valStr),
					ForensicReport = "В переменной " + varName + " задан флаг -noverify. Этот флаг отключает проверку целостности байткода JVM и часто используется читерскими инжекторами."
				});
				return;
			}

			// Case C: -Xbootclasspath
			if (lowerVal.Contains("-xbootclasspath"))
			{
				results.Add(new JvmForensicItem
				{
					Index = idx++,
					Category = JvmItemCategory.EnvironmentVariable,
					CategoryName = "🚨 Подмена BootClassPath",
					ObjectName = varName,
					PathOrLocation = location,
					DetailsOrValue = valStr,
					SignatureOrState = "Инъекция системных классов JVM",
					TargetRule = "5.01 ИНЖЕКТОР",
					Verdict = "🚨 5.01 ИНЖЕКТОР (Xbootclasspath)",
					IsViolation = true,
					IsEnvVar = true,
					RowColor = Ui.AccentRed,
					TechnicalDetails = string.Format("Переменная: {0}\r\nИсточник: {1}\r\nЗначение: {2}", varName, location, valStr),
					ForensicReport = "В переменной " + varName + " обнаружен параметр -Xbootclasspath. Это позволяет переопределять базовые классы виртуальной машины Java."
				});
				return;
			}

			// Normal parameter (e.g. -Xmx4G, -Dfile.encoding=UTF-8)
			results.Add(new JvmForensicItem
			{
				Index = idx++,
				Category = JvmItemCategory.EnvironmentVariable,
				CategoryName = "⚙️ Переменная JVM",
				ObjectName = varName,
				PathOrLocation = location,
				DetailsOrValue = valStr,
				SignatureOrState = "Стандартный параметр среды",
				TargetRule = "—",
				Verdict = "✔️ Обычный параметр JVM",
				IsViolation = false,
				IsClean = true,
				IsEnvVar = true,
				RowColor = Ui.TextPrimary,
				TechnicalDetails = string.Format("Переменная: {0}\r\nИсточник: {1}\r\nЗначение: {2}", varName, location, valStr),
				ForensicReport = "Переменная " + varName + " содержит стандартные настройки JVM (" + valStr + "). Признаков читерских модификаций не обнаружено."
			});
		}

		private void AuditLauncherProfiles(ref int idx, List<JvmForensicItem> results)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string profilesPath = Path.Combine(appData, @".minecraft\launcher_profiles.json");

			if (File.Exists(profilesPath))
			{
				try
				{
					string content = File.ReadAllText(profilesPath);
					MatchCollection matches = Regex.Matches(content, @"""(javaArgs|jvmArgs)""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
					foreach (Match m in matches)
					{
						string argKey = m.Groups[1].Value;
						string argsVal = m.Groups[2].Value;
						string lowerArgs = argsVal.ToLowerInvariant();

						if (lowerArgs.Contains("-javaagent:") || lowerArgs.Contains("-javaagent"))
						{
							string agentPath = ExtractPathFromArg(argsVal, "-javaagent:");
							bool exists = !string.IsNullOrEmpty(agentPath) && File.Exists(agentPath);

							if (!exists && !string.IsNullOrEmpty(agentPath))
							{
								results.Add(new JvmForensicItem
								{
									Index = idx++,
									Category = JvmItemCategory.DeletedEvidence508,
									CategoryName = "🚨 5.08 Удаленный JavaAgent в профиле",
									ObjectName = "launcher_profiles.json",
									PathOrLocation = profilesPath,
									DetailsOrValue = string.Format("Аргумент {0}: удаленный файл {1}", argKey, agentPath),
									SignatureOrState = "ОТСУТСТВУЕТ НА ДИСКЕ (УДАЛЕН)",
									TargetRule = "5.08 СОКРЫТИЕ УЛИК",
									Verdict = "🚨 5.08 СОКРЫТИЕ (Удален JavaAgent в профиле)",
									IsViolation = true,
									IsJavaAgent = true,
									RowColor = Ui.AccentRed,
									TechnicalDetails = string.Format("Файл профилей: {0}\r\nАргументы: {1}\r\nАгент: {2}\r\nНаличие: ФАЙЛ УДАЛЕН", profilesPath, argsVal, agentPath),
									ForensicReport = string.Format(
										"В профиле лаунчера launcher_profiles.json обнаружен параметр -javaagent:\r\n{0}\r\n" +
										"Файл агента {1} был удален игроком перед проверкой.\r\n" +
										"Нарушение пункта 5.08 регламента (Сокрытие улик).", argsVal, agentPath)
								});
							}
							else
							{
								string sha = !string.IsNullOrEmpty(agentPath) && File.Exists(agentPath) ? ComputeSha256(agentPath) : "—";
								results.Add(new JvmForensicItem
								{
									Index = idx++,
									Category = JvmItemCategory.LauncherAgentArg,
									CategoryName = "🚨 5.01 JavaAgent в лаунчере",
									ObjectName = "launcher_profiles.json",
									PathOrLocation = profilesPath,
									DetailsOrValue = string.Format("Аргумент {0}: {1}", argKey, argsVal),
									SignatureOrState = "АКТИВНЫЙ АГЕНТ В ПРОФИЛЕ",
									TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
									Verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ (JavaAgent в профиле)",
									IsViolation = true,
									IsJavaAgent = true,
									RowColor = Ui.AccentRed,
									Sha256Hash = sha,
									TechnicalDetails = string.Format("Файл профилей: {0}\r\nАргументы: {1}\r\nSHA-256 агента: {2}", profilesPath, argsVal, sha),
									ForensicReport = "В профиле launcher_profiles.json обнаружен активный параметр -javaagent. 5.01 Использование читов."
								});
							}
						}
						else if (lowerArgs.Contains("-noverify"))
						{
							results.Add(new JvmForensicItem
							{
								Index = idx++,
								Category = JvmItemCategory.LauncherAgentArg,
								CategoryName = "⚠️ Параметр -noverify в профиле",
								ObjectName = "launcher_profiles.json",
								PathOrLocation = profilesPath,
								DetailsOrValue = argsVal,
								SignatureOrState = "Отключение проверки байткода",
								TargetRule = "5.01 ПРОВЕРКА",
								Verdict = "⚠️ Отключение верификации байткода в лаунчере",
								IsViolation = false,
								RowColor = Ui.AccentOrange,
								TechnicalDetails = "Параметры запуска: " + argsVal,
								ForensicReport = "В профиле Minecraft задан аргумент -noverify."
							});
						}
					}
				}
				catch { }
			}
		}

		private void AuditRunningJavaProcesses(ref int idx, List<JvmForensicItem> results, string[] targetProxyDlls)
		{
			Process[] procs = Process.GetProcesses();
			foreach (Process p in procs)
			{
				try
				{
					string pname = p.ProcessName.ToLowerInvariant();
					if (pname == "javaw" || pname == "java" || pname == "minecraft")
					{
						// Get process command line via WMI
						string cmdLine = "";
						try
						{
							using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
								string.Format("SELECT CommandLine FROM Win32_Process WHERE ProcessId = {0}", p.Id)))
							{
								foreach (ManagementObject mo in searcher.Get())
								{
									cmdLine = (mo["CommandLine"] != null) ? mo["CommandLine"].ToString() : "";
									break;
								}
							}
						}
						catch { }

						// Check command line for JavaAgent
						if (!string.IsNullOrEmpty(cmdLine) && (cmdLine.ToLowerInvariant().Contains("-javaagent:") || cmdLine.ToLowerInvariant().Contains("-javaagent")))
						{
							string agentPath = ExtractPathFromArg(cmdLine, "-javaagent:");
							results.Add(new JvmForensicItem
							{
								Index = idx++,
								Category = JvmItemCategory.RunningProcessModule,
								CategoryName = "🚨 Активный JavaAgent в javaw.exe",
								ObjectName = string.Format("{0}.exe (PID: {1})", p.ProcessName, p.Id),
								PathOrLocation = cmdLine,
								DetailsOrValue = "Запущен с параметром: " + agentPath,
								SignatureOrState = "ЖИВОЙ ПРОЦЕСС ИГРЫ",
								TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
								Verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ (JavaAgent в игре)",
								IsViolation = true,
								IsJavaAgent = true,
								IsProcessModule = true,
								RowColor = Ui.AccentRed,
								TechnicalDetails = string.Format("PID: {0}\r\nПроцесс: {1}\r\nКомандная строка:\r\n{2}", p.Id, p.ProcessName, cmdLine),
								ForensicReport = string.Format(
									"🚨 В ЗАПУЩЕННОМ ПРОЦЕССЕ {0}.EXE ОБНАРУЖЕН ПАРАМЕТР JAVAAGENT!\r\n\r\n" +
									"PID: {1}\r\n" +
									"Агент: {2}\r\n" +
									"Полная командная строка:\r\n{3}\r\n\r\n" +
									"Это подтверждает активное использование чита в запущенной игровой сессии.\r\n" +
									"Нарушение регламента: 5.01 (Использование читов).",
									p.ProcessName, p.Id, agentPath, cmdLine)
							});
						}

						// Inspect loaded modules for Proxy DLL Hijack
						try
						{
							foreach (ProcessModule mod in p.Modules)
							{
								string modName = mod.ModuleName.ToLowerInvariant();
								string modPath = mod.FileName;
								string modDir = Path.GetDirectoryName(modPath) ?? "";

								// Is it one of the proxy DLLs?
								bool isProxyName = false;
								foreach (string tName in targetProxyDlls)
								{
									if (modName == tName) { isProxyName = true; break; }
								}

								if (isProxyName)
								{
									// Check if it's NOT loaded from Windows System32 / SysWOW64
									string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLowerInvariant();
									string sys32 = Path.Combine(windir, "system32").ToLowerInvariant();
									string syswow64 = Path.Combine(windir, "syswow64").ToLowerInvariant();
									string lowerModPath = modPath.ToLowerInvariant();

									if (!lowerModPath.StartsWith(sys32) && !lowerModPath.StartsWith(syswow64))
									{
										// LIVE PROXY DLL HIJACK IN MEMORY!
										string sha = File.Exists(modPath) ? ComputeSha256(modPath) : "—";
										results.Add(new JvmForensicItem
										{
											Index = idx++,
											Category = JvmItemCategory.RunningProcessModule,
											CategoryName = "🚨 ЖИВОЙ PROXY DLL В JAVAW",
											ObjectName = modName,
											PathOrLocation = modPath,
											DetailsOrValue = string.Format("Модуль загружен НЕ из System32! (PID: {0})", p.Id),
											SignatureOrState = "АКТИВНЫЙ ПЕРЕХВАТЧИК В ПАМЯТИ",
											TargetRule = "5.01 ЧИТЫ / ИНЖЕКТОР",
											Verdict = string.Format("🚨 5.01 АКТИВНЫЙ PROXY DLL В ИГРЕ ({0})", modName),
											IsViolation = true,
											IsProxyDll = true,
											IsProcessModule = true,
											RowColor = Ui.AccentRed,
											Sha256Hash = sha,
											TechnicalDetails = string.Format("PID: {0}\r\nМодуль: {1}\r\nПуть: {2}\r\nБазовый адрес: 0x{3:X}\r\nРазмер памяти: {4:N0} байт",
												p.Id, modName, modPath, mod.BaseAddress.ToInt64(), mod.ModuleMemorySize),
											ForensicReport = string.Format(
												"🚨 НЕОПРОВЕРЖИМАЯ УЛИКА: АКТИВНЫЙ PROXY DLL HIJACK В ПАМЯТИ JAVAW.EXE!\r\n\r\n" +
												"В процессе Minecraft (PID: {0}) библиотека '{1}' загружена из каталога:\r\n" +
												"{2}\r\n" +
												"вместо стандартной системной директории Windows System32!\r\n" +
												"Это подтверждает, что чит перехватил вызовы графической/шрифтовой подсистемы и активен в памяти игры прямо сейчас.\r\n\r\n" +
												"Нарушение регламента: 5.01 (Использование читов / инжекция софта).",
												p.Id, modName, modPath)
										});
									}
								}

								// Check if module is loaded from suspicious user directories (Temp, Downloads, Desktop)
								string lowerPath = modPath.ToLowerInvariant();
								if (lowerPath.Contains(@"\appdata\local\temp\") || lowerPath.Contains(@"\downloads\") || lowerPath.Contains(@"\desktop\"))
								{
									string sha = File.Exists(modPath) ? ComputeSha256(modPath) : "—";
									results.Add(new JvmForensicItem
									{
										Index = idx++,
										Category = JvmItemCategory.RunningProcessModule,
										CategoryName = "🚨 DLL из Temp/Downloads в javaw",
										ObjectName = modName,
										PathOrLocation = modPath,
										DetailsOrValue = string.Format("Библиотека инжектирована из временной папки (PID: {0})", p.Id),
										SignatureOrState = "ИНЖЕКТИРОВАННЫЙ МОДУЛЬ",
										TargetRule = "5.01 ИНЖЕКТОР",
										Verdict = string.Format("🚨 5.01 ИНЖЕКТИРОВАННЫЙ МОДУЛЬ ({0})", modName),
										IsViolation = true,
										IsProcessModule = true,
										RowColor = Ui.AccentRed,
										Sha256Hash = sha,
										TechnicalDetails = string.Format("PID: {0}\r\nМодуль: {1}\r\nПуть: {2}\r\nSHA-256: {3}", p.Id, modName, modPath, sha),
										ForensicReport = string.Format(
											"В процессе Minecraft javaw.exe обнаружен модуль, загруженный из каталога пользователя:\r\n{0}\r\n" +
											"Легитимные библиотеки игры не загружаются из папок Temp или Downloads.\r\n" +
											"5.01 Использование читов / инжекция.", modPath)
									});
								}
							}
						}
						catch { }

						// Also record running Java process
						results.Add(new JvmForensicItem
						{
							Index = idx++,
							Category = JvmItemCategory.RunningProcessModule,
							CategoryName = "⚡ Запущенный процесс javaw",
							ObjectName = string.Format("{0}.exe (PID: {1})", p.ProcessName, p.Id),
							PathOrLocation = GetProcessMainModuleSafe(p),
							DetailsOrValue = string.Format("Память: {0:0.0} МБ, Потоков: {1}", p.WorkingSet64 / (1024.0 * 1024.0), p.Threads.Count),
							SignatureOrState = "Активный процесс игры",
							TargetRule = "—",
							Verdict = "✔️ Активный процесс Minecraft",
							IsViolation = false,
							IsProcessModule = true,
							RowColor = Ui.AccentGreen,
							TechnicalDetails = string.Format("PID: {0}\r\nИмя: {1}\r\nКомандная строка: {2}", p.Id, p.ProcessName, cmdLine),
							ForensicReport = "Процесс " + p.ProcessName + " (PID: " + p.Id + ") активен в памяти."
						});
					}
				}
				catch { }
				finally
				{
					try { p.Dispose(); } catch { }
				}
			}
		}

		private string GetProcessMainModuleSafe(Process p)
		{
			try { return p.MainModule != null ? p.MainModule.FileName : "javaw.exe"; }
			catch { return "javaw.exe"; }
		}

		private List<string> DiscoverCandidateJreDirectories()
		{
			List<string> dirs = new List<string>();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

			// .minecraft\runtime
			string mcRuntime = Path.Combine(appData, @".minecraft\runtime");
			if (Directory.Exists(mcRuntime))
			{
				dirs.Add(mcRuntime);
				try
				{
					foreach (string sub in Directory.GetDirectories(mcRuntime, "*", SearchOption.AllDirectories))
					{
						if (Path.GetFileName(sub).Equals("bin", StringComparison.OrdinalIgnoreCase))
						{
							dirs.Add(Path.GetDirectoryName(sub));
						}
					}
				}
				catch { }
			}

			// .tlauncher
			string tlJvms = Path.Combine(appData, @".tlauncher\jvms");
			if (Directory.Exists(tlJvms))
			{
				dirs.Add(tlJvms);
				try
				{
					foreach (string sub in Directory.GetDirectories(tlJvms, "*", SearchOption.AllDirectories))
					{
						if (Path.GetFileName(sub).Equals("bin", StringComparison.OrdinalIgnoreCase))
							dirs.Add(Path.GetDirectoryName(sub));
					}
				}
				catch { }
			}

			// PrismLauncher / MultiMC
			string prismJava = Path.Combine(appData, @"PrismLauncher\java");
			if (Directory.Exists(prismJava)) dirs.Add(prismJava);
			string mmcJava = Path.Combine(appData, @"MultiMC\java");
			if (Directory.Exists(mmcJava)) dirs.Add(mmcJava);

			// Program Files Java / Adoptium / BellSoft / Zulu / Corretto
			string[] baseRoots = new string[]
			{
				Path.Combine(progFiles, "Java"),
				Path.Combine(progFilesX86, "Java"),
				Path.Combine(progFiles, "Eclipse Adoptium"),
				Path.Combine(progFiles, "BellSoft"),
				Path.Combine(progFiles, "Zulu"),
				Path.Combine(progFiles, "Amazon Corretto"),
				Path.Combine(progFiles, "Microsoft"),
				Path.Combine(localAppData, @"Programs\Adoptium"),
				Path.Combine(localAppData, @"Programs\Eclipse Foundation")
			};

			foreach (string bRoot in baseRoots)
			{
				if (Directory.Exists(bRoot))
				{
					dirs.Add(bRoot);
					try
					{
						foreach (string sub in Directory.GetDirectories(bRoot))
						{
							dirs.Add(sub);
						}
					}
					catch { }
				}
			}

			// JAVA_HOME
			string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
			if (!string.IsNullOrEmpty(javaHome) && Directory.Exists(javaHome))
			{
				dirs.Add(javaHome);
			}

			return dirs;
		}

		private string ExtractPathFromArg(string argStr, string prefix)
		{
			if (string.IsNullOrEmpty(argStr)) return "";
			int idx = argStr.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
			if (idx < 0) return "";

			string rest = argStr.Substring(idx + prefix.Length).Trim();
			if (rest.StartsWith("\""))
			{
				int endQ = rest.IndexOf('\"', 1);
				if (endQ > 1) return rest.Substring(1, endQ - 1).Trim();
			}

			int spaceIdx = rest.IndexOf(' ');
			if (spaceIdx > 0)
			{
				return rest.Substring(0, spaceIdx).Trim();
			}

			return rest.Trim();
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
			catch
			{
				return "Ошибка чтения хеша";
			}
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
			return "Не подписан (Unsigned / Сторонний)";
		}

		private bool IsLegitimateVendor(string certSummary)
		{
			if (string.IsNullOrEmpty(certSummary)) return false;
			string s = certSummary.ToLowerInvariant();
			return s.Contains("oracle") || s.Contains("microsoft") || s.Contains("adoptium") ||
				   s.Contains("eclipse") || s.Contains("bellsoft") || s.Contains("azul") ||
				   s.Contains("amazon");
		}

		#endregion

		#region KPI & Filtering

		private void UpdateKpisAndStatus()
		{
			int totalJre = 0;
			int viols = 0;
			int proxyDlls = 0;
			int agents = 0;
			int envs = 0;
			int running = 0;

			foreach (JvmForensicItem it in allItems)
			{
				if (it.Category == JvmItemCategory.JreIntegrityInfo) totalJre++;
				if (it.IsViolation) viols++;
				if (it.IsProxyDll) proxyDlls++;
				if (it.IsJavaAgent) agents++;
				if (it.IsEnvVar) envs++;
				if (it.IsProcessModule) running++;
			}

			lblKpiTotalJre.Text = totalJre.ToString();
			lblKpiViolations.Text = viols.ToString();
			lblKpiProxyDlls.Text = proxyDlls.ToString();
			lblKpiJavaAgents.Text = agents.ToString();
			lblKpiEnvVars.Text = envs.ToString();
			lblKpiRunningJava.Text = running.ToString();

			if (viols > 0)
			{
				lblStatusPill.Text = string.Format("🚨 НАЙДЕНО УЛИК: {0} (5.01/5.08)", viols);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else if (proxyDlls > 0 || agents > 0)
			{
				lblStatusPill.Text = "⚠️ ТРЕБУЕТ ВНИМАНИЯ";
				lblStatusPill.BackColor = Ui.AccentOrange;
				lblStatusPill.ForeColor = Color.Black;
			}
			else
			{
				lblStatusPill.Text = "✔️ ВСЕ JRE И JVM ЧИСТЫ";
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			btnFilterAll.Text = string.Format("Все ({0})", allItems.Count);
			btnFilterViolations.Text = string.Format("🚨 Нарушения ({0})", viols);
			btnFilterProxyDlls.Text = string.Format("💉 Прокси DLL ({0})", proxyDlls);
			btnFilterJavaAgents.Text = string.Format("☕ JavaAgent ({0})", agents);
			btnFilterEnvVars.Text = string.Format("⚙️ Переменные ({0})", envs);
			btnFilterProcessModules.Text = string.Format("⚡ Процессы ({0})", running);
			btnFilterClean.Text = string.Format("✔️ Чистые ({0})", totalJre);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (JvmForensicItem item in allItems)
			{
				// Category Filter
				if (currentFilter == JvmFilterCategory.ViolationsOnly && !item.IsViolation) continue;
				if (currentFilter == JvmFilterCategory.ProxyDlls && !item.IsProxyDll) continue;
				if (currentFilter == JvmFilterCategory.JavaAgents && !item.IsJavaAgent) continue;
				if (currentFilter == JvmFilterCategory.EnvVars && !item.IsEnvVar) continue;
				if (currentFilter == JvmFilterCategory.ProcessModules && !item.IsProcessModule) continue;
				if (currentFilter == JvmFilterCategory.CleanRuntimes && !item.IsClean) continue;

				// Text search
				if (!string.IsNullOrEmpty(search))
				{
					bool match = (item.ObjectName != null && item.ObjectName.ToLowerInvariant().Contains(search)) ||
								 (item.PathOrLocation != null && item.PathOrLocation.ToLowerInvariant().Contains(search)) ||
								 (item.DetailsOrValue != null && item.DetailsOrValue.ToLowerInvariant().Contains(search)) ||
								 (item.CategoryName != null && item.CategoryName.ToLowerInvariant().Contains(search)) ||
								 (item.Verdict != null && item.Verdict.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.ObjectName);
				lvi.SubItems.Add(item.PathOrLocation);
				lvi.SubItems.Add(item.SignatureOrState);
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
				txtForensicReport.Text = "Нет записей, соответствующих выбранному фильтру.";
				txtTechnicalDetails.Clear();
				txtCertificateDetails.Clear();
			}
		}

		private void UpdateSelectedDetails()
		{
			if (listView.SelectedItems.Count == 0) return;
			JvmForensicItem item = listView.SelectedItems[0].Tag as JvmForensicItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport ?? "";
			txtTechnicalDetails.Text = item.TechnicalDetails ?? "";
			txtCertificateDetails.Text = string.Format(
				"Подпись / Состояние: {0}\r\nSHA-256 хеш: {1}\r\nРазмер файла: {2:N0} байт\r\nСоздан: {3}\r\nИзменен: {4}\r\nСертификат:\r\n{5}",
				item.SignatureOrState,
				string.IsNullOrEmpty(item.Sha256Hash) ? "—" : item.Sha256Hash,
				item.FileSizeBytes,
				item.FileCreationTime != DateTime.MinValue ? item.FileCreationTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
				item.FileLastWriteTime != DateTime.MinValue ? item.FileLastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") : "—",
				string.IsNullOrEmpty(item.CertificateDetails) ? "Сертификат отсутствует" : item.CertificateDetails);
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRowText()
		{
			if (listView.SelectedItems.Count == 0) return;
			JvmForensicItem item = listView.SelectedItems[0].Tag as JvmForensicItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1} | {2} | {3} | {4} | {5}",
				item.CategoryName, item.ObjectName, item.PathOrLocation, item.SignatureOrState, item.TargetRule, item.Verdict);
			Clipboard.SetText(text);
			MessageBox.Show("Строка скопирована в буфер обмена:\r\n" + text, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyForDiscord()
		{
			if (listView.SelectedItems.Count == 0) return;
			JvmForensicItem item = listView.SelectedItems[0].Tag as JvmForensicItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine(item.IsViolation ? "- [НАРУШЕНИЕ SCREENSHARE]" : "+ [ПРОВЕРКА JVM / RUNTIME]");
			sb.AppendLine(string.Format("Игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Время проверки: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Объект: {0}", item.ObjectName));
			sb.AppendLine(string.Format("Путь: {0}", item.PathOrLocation));
			if (!string.IsNullOrEmpty(item.Sha256Hash) && item.Sha256Hash != "—")
				sb.AppendLine(string.Format("SHA-256: {0}", item.Sha256Hash));
			sb.AppendLine(string.Format("Подпись/Состояние: {0}", item.SignatureOrState));
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
			JvmForensicItem item = listView.SelectedItems[0].Tag as JvmForensicItem;
			if (item == null || !item.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил 5.01 / 5.08.", "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = item.TargetRule.Contains("5.08") ?
				string.Format("5.08 Сокрытие улик ({0})", item.ObjectName) :
				string.Format("5.01 Использование читов ({0} в JRE)", item.ObjectName);

			string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_JVM_ProxyDLL_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза JVM & Proxy DLL — " + targetPlayerName + "</title>");
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

					sb.AppendLine("<h1>☕ Судебная экспертиза JVM инъекций, прокси DLL и переменных окружения</h1>");
					sb.AppendLine(string.Format("<p>Игрок: <b>{0}</b> | Дата генерации: {1:yyyy-MM-dd HH:mm:ss}</p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>☕ JRE каталогов</div><div class='kpi-val'>{0}</div></div>", lblKpiTotalJre.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>🚨 Нарушений (5.01/5.08)</div><div class='kpi-val crit'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>💉 Прокси DLL</div><div class='kpi-val warn'>{0}</div></div>", lblKpiProxyDlls.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>☕ JavaAgent</div><div class='kpi-val'>{0}</div></div>", lblKpiJavaAgents.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>⚙️ Переменные JVM</div><div class='kpi-val'>{0}</div></div>", lblKpiEnvVars.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>⚡ Процессы javaw</div><div class='kpi-val ok'>{0}</div></div>", lblKpiRunningJava.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table><thead><tr><th>#</th><th>Категория</th><th>Объект</th><th>Путь / Значение</th><th>Состояние / Хеш</th><th>Регламент</th><th>Вердикт</th></tr></thead><tbody>");
					foreach (JvmForensicItem it in allItems)
					{
						string cls = it.IsViolation ? "crit" : (it.IsClean ? "ok" : "warn");
						sb.AppendLine(string.Format(
							"<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td></tr>",
							cls, it.Index, it.CategoryName, it.ObjectName, it.PathOrLocation, it.SignatureOrState, it.TargetRule, it.Verdict));
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
