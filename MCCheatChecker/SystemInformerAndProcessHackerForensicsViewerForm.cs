using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class SystemInformerAndProcessHackerForensicsViewerForm : Form
	{
		private string targetPlayer;
		private SystemInformerScanSummary lastSummary;
		private List<SystemInformerEvidenceItem> allItems = new List<SystemInformerEvidenceItem>();
		private List<SystemInformerEvidenceItem> filteredItems = new List<SystemInformerEvidenceItem>();
		private SystemInformerCategory currentFilter = SystemInformerCategory.All;
		private int scanGeneration = 0;
		private bool isScanning = false;

		private Label lblTitle;
		private Label lblSubtitle;
		private Label lblScanStatus;
		private DarkTextBox txtSearch;
		private DarkListView lvArtifacts;

		private KpiCard kpiTotal;
		private KpiCard kpiViolations;
		private KpiCard kpiJavawStrings;
		private KpiCard kpiSuspiciousDlls;
		private KpiCard kpiServicesExplorer;
		private KpiCard kpiDpsCsrss;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterJavawStrings;
		private DarkButton btnFilterJavawModules;
		private DarkButton btnFilterExplorer;
		private DarkButton btnFilterDcomLaunch;
		private DarkButton btnFilterPcaSvc;
		private DarkButton btnFilterDps;
		private DarkButton btnFilterCsrss;

		private DarkTabView tabDetails;
		private DarkTextBoxMultiline txtDetailProtocol;
		private DarkTextBoxMultiline txtRawDumpTab;
		private DarkTextBoxMultiline txtBanAndManualTab;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		public SystemInformerAndProcessHackerForensicsViewerForm() : this("")
		{
		}

		public SystemInformerAndProcessHackerForensicsViewerForm(string player)
		{
			this.targetPlayer = string.IsNullOrEmpty(player) ? "DefaultPlayer" : player;
			InitializeComponentCustom();
			PopulateInitialPlaceholders();
			this.Load += delegate
			{
				StartAsyncScan();
			};
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🔬 Инспектор памяти System Informer / Process Hacker 2 (Tool 88 / Step 85) — MCCheatChecker";
			this.Size = new Size(1380, 860);
			this.MinimumSize = new Size(1160, 680);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Font = Ui.FontBody(9f);
			this.Icon = SystemIcons.Shield;
			Ui.ApplyDarkScrollbars(this);
			this.KeyPreview = true;
			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartAsyncScan();
				}
			};

			// Header Panel
			Panel pnlHeader = new Panel();
			pnlHeader.Dock = DockStyle.Top;
			pnlHeader.Height = 72;
			pnlHeader.BackColor = Ui.Surface;
			pnlHeader.Padding = new Padding(16, 10, 16, 6);

			lblTitle = new Label();
			lblTitle.Text = "ИНСПЕКТОР ПАМЯТИ SYSTEM INFORMER / PROCESS HACKER 2 (НАТИВНОЕ ЯДРО)";
			lblTitle.Font = Ui.FontTitle(12f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.AutoSize = true;
			lblTitle.Location = new Point(16, 10);
			pnlHeader.Controls.Add(lblTitle);

			lblSubtitle = new Label();
			lblSubtitle.Text = string.Format("Игрок: {0} | Автоматический нативный аудит памяти javaw.exe, explorer.exe, DcomLaunch, PcaSvc, DPS и csrss.exe без запуска внешних утилит", targetPlayer);
			lblSubtitle.Font = Ui.FontBody(8.8f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 38);
			pnlHeader.Controls.Add(lblSubtitle);

			// KPI Panel
			Panel pnlKpis = new Panel();
			pnlKpis.Dock = DockStyle.Top;
			pnlKpis.Height = 86;
			pnlKpis.BackColor = Ui.Backdrop;
			pnlKpis.Padding = new Padding(10, 8, 10, 8);

			kpiTotal = new KpiCard("ВСЕГО АРТЕФАКТОВ", Ui.AccentTeal, "Память, DLL и службы");
			kpiTotal.Size = new Size(215, 68);
			kpiTotal.Location = new Point(12, 8);
			kpiTotal.Click += delegate { SetFilter(SystemInformerCategory.All); };

			kpiViolations = new KpiCard("НАРУШЕНИЯ (5.01/5.08)", Ui.AccentRed, "Читы / Инжекты / Обход");
			kpiViolations.Size = new Size(215, 68);
			kpiViolations.Location = new Point(235, 8);
			kpiViolations.Click += delegate { SetFilter(SystemInformerCategory.Violations); };

			kpiJavawStrings = new KpiCard("СТРОКИ JAVAW.EXE", Ui.AccentPurple, "Сигнатуры в ОЗУ игры");
			kpiJavawStrings.Size = new Size(215, 68);
			kpiJavawStrings.Location = new Point(458, 8);
			kpiJavawStrings.Click += delegate { SetFilter(SystemInformerCategory.JavawStrings); };

			kpiSuspiciousDlls = new KpiCard("МОДУЛИ DLL (JAVAW)", Ui.AccentOrange, "Без Description / 1.42-1.89 МБ");
			kpiSuspiciousDlls.Size = new Size(215, 68);
			kpiSuspiciousDlls.Location = new Point(681, 8);
			kpiSuspiciousDlls.Click += delegate { SetFilter(SystemInformerCategory.JavawModules); };

			kpiServicesExplorer = new KpiCard("EXPLORER / DCOM / PCA", Ui.AccentBlue, "PcaClient, .jar, PlugPlay");
			kpiServicesExplorer.Size = new Size(215, 68);
			kpiServicesExplorer.Location = new Point(904, 8);
			kpiServicesExplorer.Click += delegate { SetFilter(SystemInformerCategory.Explorer); };

			kpiDpsCsrss = new KpiCard("DPS / CSRSS АНОМАЛИИ", Ui.AccentGreen, "Без .exe / Replace / DLL");
			kpiDpsCsrss.Size = new Size(220, 68);
			kpiDpsCsrss.Location = new Point(1127, 8);
			kpiDpsCsrss.Click += delegate { SetFilter(SystemInformerCategory.Dps); };

			pnlKpis.Controls.Add(kpiTotal);
			pnlKpis.Controls.Add(kpiViolations);
			pnlKpis.Controls.Add(kpiJavawStrings);
			pnlKpis.Controls.Add(kpiSuspiciousDlls);
			pnlKpis.Controls.Add(kpiServicesExplorer);
			pnlKpis.Controls.Add(kpiDpsCsrss);

			// Filter & Search Panel (2 neat rows)
			Panel pnlFilters = new Panel();
			pnlFilters.Dock = DockStyle.Top;
			pnlFilters.Height = 82;
			pnlFilters.BackColor = Ui.Surface;
			pnlFilters.Padding = new Padding(12, 8, 12, 8);

			btnFilterAll = CreateFilterButton("Все (0)", 12, 8, 108, SystemInformerCategory.All);
			btnFilterAll.AccentStyle = true;
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (0)", 126, 8, 146, SystemInformerCategory.Violations);
			btnFilterJavawStrings = CreateFilterButton("☕ Строки javaw (0)", 278, 8, 152, SystemInformerCategory.JavawStrings);
			btnFilterJavawModules = CreateFilterButton("🧩 Модули DLL (0)", 436, 8, 142, SystemInformerCategory.JavawModules);
			btnFilterExplorer = CreateFilterButton("📁 Explorer (0)", 584, 8, 132, SystemInformerCategory.Explorer);
			btnFilterDcomLaunch = CreateFilterButton("🔌 DcomLaunch (0)", 722, 8, 148, SystemInformerCategory.DcomLaunch);
			btnFilterPcaSvc = CreateFilterButton("🚀 PcaSvc (0)", 876, 8, 128, SystemInformerCategory.PcaSvc);
			btnFilterDps = CreateFilterButton("⚡ DPS (0)", 1010, 8, 118, SystemInformerCategory.Dps);
			btnFilterCsrss = CreateFilterButton("🛡️ Csrss (0)", 1134, 8, 126, SystemInformerCategory.Csrss);

			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск (строка, процесс, путь, вердикт):";
			lblSearch.AutoSize = true;
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(12, 50);

			txtSearch = new DarkTextBox();
			txtSearch.Size = new Size(340, 24);
			txtSearch.Location = new Point(290, 47);
			txtSearch.TextChanged += delegate { ApplyFilters(); };

			lblScanStatus = new Label();
			lblScanStatus.Text = "Ожидание запуска сканирования памяти...";
			lblScanStatus.AutoSize = true;
			lblScanStatus.ForeColor = Ui.AccentTeal;
			lblScanStatus.Location = new Point(650, 50);

			pnlFilters.Controls.Add(btnFilterAll);
			pnlFilters.Controls.Add(btnFilterViolations);
			pnlFilters.Controls.Add(btnFilterJavawStrings);
			pnlFilters.Controls.Add(btnFilterJavawModules);
			pnlFilters.Controls.Add(btnFilterExplorer);
			pnlFilters.Controls.Add(btnFilterDcomLaunch);
			pnlFilters.Controls.Add(btnFilterPcaSvc);
			pnlFilters.Controls.Add(btnFilterDps);
			pnlFilters.Controls.Add(btnFilterCsrss);
			pnlFilters.Controls.Add(lblSearch);
			pnlFilters.Controls.Add(txtSearch);
			pnlFilters.Controls.Add(lblScanStatus);

			// SplitContainer: ListView on top, Detail Tabs on bottom
			DarkSplitContainer splitMain = new DarkSplitContainer();
			splitMain.Dock = DockStyle.Fill;
			splitMain.Orientation = Orientation.Horizontal;
			splitMain.SplitterDistance = 360;
			splitMain.SplitterWidth = 6;
			splitMain.BackColor = Ui.Border;

			// Top Panel: ListView
			lvArtifacts = new DarkListView();
			lvArtifacts.Dock = DockStyle.Fill;
			lvArtifacts.View = View.Details;
			lvArtifacts.FullRowSelect = true;
			lvArtifacts.GridLines = true;
			lvArtifacts.MultiSelect = false;

			lvArtifacts.Columns.Add("#", 44);
			lvArtifacts.Columns.Add("Процесс", 130);
			lvArtifacts.Columns.Add("PID", 62);
			lvArtifacts.Columns.Add("Категория", 125);
			lvArtifacts.Columns.Add("Метод / Источник", 165);
			lvArtifacts.Columns.Add("Артефакт / Строка / Путь", 330);
			lvArtifacts.Columns.Add("Вес / Размер", 105);
			lvArtifacts.Columns.Add("Время", 130);
			lvArtifacts.Columns.Add("Правило", 70);
			lvArtifacts.Columns.Add("Вердикт", 185);

			lvArtifacts.SelectedIndexChanged += delegate { UpdateSelectedDetail(); };
			splitMain.Panel1.Controls.Add(lvArtifacts);

			// Bottom Panel: Details TabControl
			tabDetails = new DarkTabView();
			tabDetails.Dock = DockStyle.Fill;

			txtDetailProtocol = new DarkTextBoxMultiline();
			txtDetailProtocol.Dock = DockStyle.Fill;
			txtDetailProtocol.ReadOnly = true;

			txtRawDumpTab = new DarkTextBoxMultiline();
			txtRawDumpTab.Dock = DockStyle.Fill;
			txtRawDumpTab.ReadOnly = true;

			txtBanAndManualTab = new DarkTextBoxMultiline();
			txtBanAndManualTab.Dock = DockStyle.Fill;
			txtBanAndManualTab.ReadOnly = true;

			tabDetails.AddPage("Криминалистический протокол артефакта", txtDetailProtocol);
			tabDetails.AddPage("Технический дамп и метаданные файла", txtRawDumpTab);
			tabDetails.AddPage("Справочник System Informer и Команда бана", txtBanAndManualTab);

			splitMain.Panel2.Controls.Add(tabDetails);

			// Bottom Button Bar
			Panel pnlBottom = new Panel();
			pnlBottom.Dock = DockStyle.Bottom;
			pnlBottom.Height = 52;
			pnlBottom.BackColor = Ui.Surface;
			pnlBottom.Padding = new Padding(12, 10, 12, 10);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Size = new Size(215, 32);
			btnCopyBan.Location = new Point(12, 10);
			btnCopyBan.Click += delegate { CopyBanCommand(); };

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Discord отчет";
			btnCopyDiscord.Size = new Size(155, 32);
			btnCopyDiscord.Location = new Point(235, 10);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "📄 Экспорт HTML";
			btnExportHtml.Size = new Size(145, 32);
			btnExportHtml.Location = new Point(398, 10);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать (F5)";
			btnRescan.Size = new Size(180, 32);
			btnRescan.Location = new Point(551, 10);
			btnRescan.Click += delegate { StartAsyncScan(); };

			btnClose = new DarkButton();
			btnClose.Text = "❌ Закрыть";
			btnClose.Size = new Size(105, 32);
			btnClose.Location = new Point(739, 10);
			btnClose.Click += delegate { this.Close(); };

			pnlBottom.Controls.Add(btnCopyBan);
			pnlBottom.Controls.Add(btnCopyDiscord);
			pnlBottom.Controls.Add(btnExportHtml);
			pnlBottom.Controls.Add(btnRescan);
			pnlBottom.Controls.Add(btnClose);

			// Add controls to form in proper Dock Z-order
			this.Controls.Add(splitMain);
			this.Controls.Add(pnlFilters);
			this.Controls.Add(pnlKpis);
			this.Controls.Add(pnlHeader);
			this.Controls.Add(pnlBottom);
		}

		private DarkButton CreateFilterButton(string text, int x, int y, int width, SystemInformerCategory category)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 30);
			btn.Location = new Point(x, y);
			btn.Click += delegate { SetFilter(category); };
			return btn;
		}

		private void PopulateInitialPlaceholders()
		{
			txtDetailProtocol.Text = "Запуск нативного анализа памяти процессов System Informer / Process Hacker 2...";
			txtRawDumpTab.Text = "Подготовка дампа памяти и модулей...";
			txtBanAndManualTab.Text = BuildBanAndManualReference(null);
		}

		private void SetFilter(SystemInformerCategory category)
		{
			currentFilter = category;
			UpdateFilterButtonHighlights();
			ApplyFilters();
		}

		private void UpdateFilterButtonHighlights()
		{
			btnFilterAll.AccentStyle = (currentFilter == SystemInformerCategory.All);
			btnFilterViolations.AccentStyle = (currentFilter == SystemInformerCategory.Violations);
			btnFilterJavawStrings.AccentStyle = (currentFilter == SystemInformerCategory.JavawStrings);
			btnFilterJavawModules.AccentStyle = (currentFilter == SystemInformerCategory.JavawModules);
			btnFilterExplorer.AccentStyle = (currentFilter == SystemInformerCategory.Explorer);
			btnFilterDcomLaunch.AccentStyle = (currentFilter == SystemInformerCategory.DcomLaunch);
			btnFilterPcaSvc.AccentStyle = (currentFilter == SystemInformerCategory.PcaSvc);
			btnFilterDps.AccentStyle = (currentFilter == SystemInformerCategory.Dps);
			btnFilterCsrss.AccentStyle = (currentFilter == SystemInformerCategory.Csrss);
		}

		public void StartAsyncScan()
		{
			scanGeneration++;
			int activeGen = scanGeneration;
			isScanning = true;
			if (btnRescan != null)
			{
				btnRescan.Enabled = false;
				btnRescan.Text = "⏳ Сканирование...";
			}
			if (lblScanStatus != null)
			{
				lblScanStatus.ForeColor = Ui.AccentTeal;
				lblScanStatus.Text = "⏳ Нативное сканирование памяти (javaw, explorer, DcomLaunch, PcaSvc, DPS, csrss)...";
			}

			ThreadPool.QueueUserWorkItem(delegate
			{
				SystemInformerScanSummary summary = null;
				Exception scanError = null;
				try
				{
					summary = SystemInformerForensicsEngine.RunScan(true);
				}
				catch (Exception ex)
				{
					scanError = ex;
				}

				if (this.IsDisposed || !this.IsHandleCreated) return;
				try
				{
					this.BeginInvoke((MethodInvoker)delegate
					{
						if (this.IsDisposed || activeGen != scanGeneration) return;
						isScanning = false;
						if (btnRescan != null)
						{
							btnRescan.Enabled = true;
							btnRescan.Text = "🔄 Пересканировать (F5)";
						}
						if (scanError != null)
						{
							lblScanStatus.ForeColor = Ui.AccentRed;
							lblScanStatus.Text = "Ошибка сканирования: " + scanError.Message;
							txtDetailProtocol.Text = "Ошибка при выполнении SystemInformerForensicsEngine.RunScan:\r\n" + scanError.ToString();
							return;
						}
						ApplyScanSummary(summary);
					});
				}
				catch { }
			});
		}

		private void ApplyScanSummary(SystemInformerScanSummary summary)
		{
			lastSummary = summary ?? new SystemInformerScanSummary();
			allItems = lastSummary.Items != null ? new List<SystemInformerEvidenceItem>(lastSummary.Items) : new List<SystemInformerEvidenceItem>();

			// Sort violations first so critical evidence immediately stands out
			allItems.Sort(delegate(SystemInformerEvidenceItem a, SystemInformerEvidenceItem b)
			{
				if (a.IsViolation != b.IsViolation) return b.IsViolation.CompareTo(a.IsViolation);
				int sevCmp = b.Severity.CompareTo(a.Severity);
				if (sevCmp != 0) return sevCmp;
				return string.Compare(a.ProcessName ?? "", b.ProcessName ?? "", StringComparison.OrdinalIgnoreCase);
			});

			kpiTotal.SetValue(lastSummary.TotalArtifacts);
			kpiViolations.SetValue(lastSummary.ViolationCount);
			kpiJavawStrings.SetValue(lastSummary.JavawStringHits);
			kpiSuspiciousDlls.SetValue(lastSummary.SuspiciousDllCount);
			kpiServicesExplorer.SetValue(lastSummary.ServiceAndExplorerHits);
			kpiDpsCsrss.SetValue(lastSummary.DpsAnomalyCount + lastSummary.CsrssInjectionCount);

			UpdateFilterButtonCounts();

			string dur = string.IsNullOrEmpty(lastSummary.ScanDurationMs) ? "0" : lastSummary.ScanDurationMs;
			if (lastSummary.CsrssAccessDenied)
			{
				lblScanStatus.ForeColor = Ui.AccentOrange;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🔒 csrss.exe защищён (Kernel PPL / Античит — оставлен по правилам безопасности)", dur);
			}
			else if (lastSummary.ViolationCount > 0)
			{
				lblScanStatus.ForeColor = Ui.AccentRed;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🚨 Найдено нарушений: {1}", dur, lastSummary.ViolationCount);
			}
			else
			{
				lblScanStatus.ForeColor = Ui.AccentGreen;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🟢 Нарушений в памяти и службах не обнаружено", dur);
			}

			ApplyFilters();
		}

		private void UpdateFilterButtonCounts()
		{
			int cAll = allItems.Count;
			int cViol = 0;
			int cJavawStr = 0;
			int cJavawMod = 0;
			int cExp = 0;
			int cDcom = 0;
			int cPca = 0;
			int cDps = 0;
			int cCsrss = 0;

			foreach (SystemInformerEvidenceItem it in allItems)
			{
				if (it.IsViolation || it.Category == SystemInformerCategory.Violations) cViol++;
				if (it.Category == SystemInformerCategory.JavawStrings) cJavawStr++;
				else if (it.Category == SystemInformerCategory.JavawModules) cJavawMod++;
				else if (it.Category == SystemInformerCategory.Explorer) cExp++;
				else if (it.Category == SystemInformerCategory.DcomLaunch) cDcom++;
				else if (it.Category == SystemInformerCategory.PcaSvc) cPca++;
				else if (it.Category == SystemInformerCategory.Dps) cDps++;
				else if (it.Category == SystemInformerCategory.Csrss) cCsrss++;
			}

			btnFilterAll.Text = string.Format("Все ({0})", cAll);
			btnFilterViolations.Text = string.Format("🚨 Нарушения ({0})", cViol);
			btnFilterJavawStrings.Text = string.Format("☕ Строки javaw ({0})", cJavawStr);
			btnFilterJavawModules.Text = string.Format("🧩 Модули DLL ({0})", cJavawMod);
			btnFilterExplorer.Text = string.Format("📁 Explorer ({0})", cExp);
			btnFilterDcomLaunch.Text = string.Format("🔌 DcomLaunch ({0})", cDcom);
			btnFilterPcaSvc.Text = string.Format("🚀 PcaSvc ({0})", cPca);
			btnFilterDps.Text = string.Format("⚡ DPS ({0})", cDps);
			btnFilterCsrss.Text = string.Format("🛡️ Csrss ({0})", cCsrss);
		}

		private void ApplyFilters()
		{
			string search = (txtSearch != null && txtSearch.Text != null) ? txtSearch.Text.ToLowerInvariant().Trim() : "";
			filteredItems.Clear();

			foreach (SystemInformerEvidenceItem it in allItems)
			{
				bool matchCat = true;
				switch (currentFilter)
				{
					case SystemInformerCategory.Violations:
						matchCat = it.IsViolation || it.Category == SystemInformerCategory.Violations;
						break;
					case SystemInformerCategory.All:
						matchCat = true;
						break;
					default:
						matchCat = (it.Category == currentFilter);
						break;
				}
				if (!matchCat) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = ContainsIgnoreCase(it.ProcessName, search) ||
									   ContainsIgnoreCase(it.SourceMethod, search) ||
									   ContainsIgnoreCase(it.DetectedStringOrPath, search) ||
									   ContainsIgnoreCase(it.Verdict, search) ||
									   ContainsIgnoreCase(it.RuleViolation, search) ||
									   ContainsIgnoreCase(it.Description, search) ||
									   ContainsIgnoreCase(it.FullDetails, search);
					if (!matchSearch) continue;
				}

				filteredItems.Add(it);
			}

			RenderListView();
		}

		private static bool ContainsIgnoreCase(string source, string termLower)
		{
			return !string.IsNullOrEmpty(source) && source.ToLowerInvariant().Contains(termLower);
		}

		private void RenderListView()
		{
			lvArtifacts.BeginUpdate();
			lvArtifacts.Items.Clear();

			int idx = 1;
			foreach (SystemInformerEvidenceItem it in filteredItems)
			{
				ListViewItem lvi = new ListViewItem(idx.ToString());
				lvi.SubItems.Add(string.IsNullOrEmpty(it.ProcessName) ? "—" : it.ProcessName);
				lvi.SubItems.Add(it.ProcessId > 0 ? it.ProcessId.ToString() : "—");
				lvi.SubItems.Add(FormatCategoryName(it.Category));
				lvi.SubItems.Add(string.IsNullOrEmpty(it.SourceMethod) ? "—" : it.SourceMethod);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.DetectedStringOrPath) ? "—" : it.DetectedStringOrPath);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.FileSizeOrWeight) ? "—" : it.FileSizeOrWeight);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.TimeInfo) ? "—" : it.TimeInfo);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.RuleViolation) ? (it.IsViolation ? "5.01" : "ЧИСТО") : it.RuleViolation);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.Verdict) ? "—" : it.Verdict);

				lvi.ForeColor = GetRowColor(it);
				lvi.Tag = it;
				lvArtifacts.Items.Add(lvi);
				idx++;
			}

			lvArtifacts.EndUpdate();
			if (lvArtifacts.Items.Count > 0)
			{
				lvArtifacts.Items[0].Selected = true;
			}
			else
			{
				txtDetailProtocol.Text = isScanning
					? "⏳ Сканирование ещё выполняется..."
					: "Записей не найдено по выбранному фильтру или поисковому запросу.";
				txtRawDumpTab.Text = BuildSummaryTelemetryText();
				txtBanAndManualTab.Text = BuildBanAndManualReference(null);
			}
		}

		private static Color GetRowColor(SystemInformerEvidenceItem it)
		{
			if (it == null) return Ui.TextPrimary;
			if (it.Severity == CheckSeverity.Critical) return Ui.AccentRed;
			if (it.Severity == CheckSeverity.NeedsCheck) return Ui.AccentPurple;
			if (it.Severity == CheckSeverity.Warning) return Ui.AccentOrange;
			if (it.IsViolation) return Ui.AccentRed;
			return Ui.AccentGreen;
		}

		private static string FormatCategoryName(SystemInformerCategory cat)
		{
			switch (cat)
			{
				case SystemInformerCategory.JavawStrings: return "☕ Строки javaw";
				case SystemInformerCategory.JavawModules: return "🧩 Модули DLL";
				case SystemInformerCategory.Explorer: return "📁 Explorer";
				case SystemInformerCategory.DcomLaunch: return "🔌 DcomLaunch";
				case SystemInformerCategory.PcaSvc: return "🚀 PcaSvc";
				case SystemInformerCategory.Dps: return "⚡ Служба DPS";
				case SystemInformerCategory.Csrss: return "🛡️ Процесс Csrss";
				case SystemInformerCategory.Violations: return "🚨 Нарушение";
				default: return "📋 Все";
			}
		}

		private void UpdateSelectedDetail()
		{
			if (lvArtifacts.SelectedItems.Count == 0) return;
			SystemInformerEvidenceItem it = lvArtifacts.SelectedItems[0].Tag as SystemInformerEvidenceItem;
			if (it == null) return;

			// Tab 1: Криминалистический протокол артефакта
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНО-КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ SYSTEM INFORMER ===");
			sb.AppendLine(string.Format("Процесс:            {0} (PID: {1})", string.IsNullOrEmpty(it.ProcessName) ? "—" : it.ProcessName, it.ProcessId > 0 ? it.ProcessId.ToString() : "N/A"));
			sb.AppendLine(string.Format("Категория:          {0}", FormatCategoryName(it.Category)));
			sb.AppendLine(string.Format("Метод / Источник:   {0}", string.IsNullOrEmpty(it.SourceMethod) ? "—" : it.SourceMethod));
			sb.AppendLine(string.Format("Найденный артефакт: {0}", string.IsNullOrEmpty(it.DetectedStringOrPath) ? "—" : it.DetectedStringOrPath));
			sb.AppendLine(string.Format("Вес / Размер:       {0}", string.IsNullOrEmpty(it.FileSizeOrWeight) ? "—" : it.FileSizeOrWeight));
			sb.AppendLine(string.Format("Метка времени:      {0}", string.IsNullOrEmpty(it.TimeInfo) ? "—" : it.TimeInfo));
			sb.AppendLine(string.Format("Уровень угрозы:     {0}", it.Severity));
			sb.AppendLine(string.Format("Статья регламента:  {0}", string.IsNullOrEmpty(it.RuleViolation) ? "—" : it.RuleViolation));
			sb.AppendLine(string.Format("Вердикт:            {0}", string.IsNullOrEmpty(it.Verdict) ? "—" : it.Verdict));
			sb.AppendLine();
			sb.AppendLine("Описание и контекст:");
			sb.AppendLine(string.IsNullOrEmpty(it.Description) ? "Без дополнительного описания." : it.Description);
			sb.AppendLine();
			sb.AppendLine("Подробная расшифровка:");
			sb.AppendLine(string.IsNullOrEmpty(it.FullDetails) ? "Нет расширенных данных." : it.FullDetails);
			txtDetailProtocol.Text = sb.ToString();

			// Tab 2: Технический дамп и метаданные файла
			StringBuilder sbRaw = new StringBuilder();
			sbRaw.AppendLine("=== ПОЛНЫЕ ТЕХНИЧЕСКИЕ ДАННЫЕ И МЕТАДАННЫЕ ДИСКА ===");
			if (!string.IsNullOrEmpty(it.FullDetails))
			{
				sbRaw.AppendLine(it.FullDetails);
				sbRaw.AppendLine();
			}
			AppendFileMetadataIfApplicable(sbRaw, it.DetectedStringOrPath);
			sbRaw.AppendLine();
			sbRaw.AppendLine(BuildSummaryTelemetryText());
			txtRawDumpTab.Text = sbRaw.ToString();

			// Tab 3: Справочник System Informer и Команда бана
			txtBanAndManualTab.Text = BuildBanAndManualReference(it);
		}

		private static void AppendFileMetadataIfApplicable(StringBuilder sb, string rawPath)
		{
			if (string.IsNullOrEmpty(rawPath)) return;
			try
			{
				string clean = rawPath.Trim().Trim('"', '\'');
				if (clean.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
				{
					clean = Uri.UnescapeDataString(clean.Substring(8)).Replace('/', '\\');
				}
				if (clean.StartsWith(@"\\?\")) clean = clean.Substring(4);
				if (clean.StartsWith("!!")) return;

				if (clean.Length >= 4 && clean[1] == ':' && File.Exists(clean))
				{
					FileInfo fi = new FileInfo(clean);
					sb.AppendLine("Свойства файла на диске (без запуска внешних утилит):");
					sb.AppendLine(string.Format("  Полный путь:  {0}", fi.FullName));
					sb.AppendLine(string.Format("  Размер:       {0:N0} байт ({1:F2} МБ)", fi.Length, fi.Length / (1024.0 * 1024.0)));
					sb.AppendLine(string.Format("  Создан:       {0:dd.MM.yyyy HH:mm:ss}", fi.CreationTime));
					sb.AppendLine(string.Format("  Изменён:      {0:dd.MM.yyyy HH:mm:ss}", fi.LastWriteTime));
					try
					{
						FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fi.FullName);
						sb.AppendLine(string.Format("  Description:  {0}", string.IsNullOrEmpty(fvi.FileDescription) ? "<ПУСТО>" : fvi.FileDescription));
						sb.AppendLine(string.Format("  Company:      {0}", string.IsNullOrEmpty(fvi.CompanyName) ? "<ПУСТО>" : fvi.CompanyName));
						sb.AppendLine(string.Format("  Product:      {0}", string.IsNullOrEmpty(fvi.ProductName) ? "<ПУСТО>" : fvi.ProductName));
					}
					catch { }
				}
			}
			catch { }
		}

		private string BuildSummaryTelemetryText()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("--- ТЕЛЕМЕТРИЯ СКАНИРОВАНИЯ SYSTEM INFORMER ---");
			if (lastSummary == null)
			{
				sb.AppendLine("Данные сканирования отсутствуют.");
				return sb.ToString();
			}
			sb.AppendLine(string.Format("Время выполнения:        {0} мс", string.IsNullOrEmpty(lastSummary.ScanDurationMs) ? "—" : lastSummary.ScanDurationMs));
			sb.AppendLine(string.Format("Всего артефактов:        {0}", lastSummary.TotalArtifacts));
			sb.AppendLine(string.Format("Нарушений (Violations):  {0}", lastSummary.ViolationCount));
			sb.AppendLine(string.Format("Строк памяти javaw.exe:  {0}", lastSummary.JavawStringHits));
			sb.AppendLine(string.Format("Подозрительных DLL:      {0}", lastSummary.SuspiciousDllCount));
			sb.AppendLine(string.Format("Explorer/DcomLaunch/PCA: {0}", lastSummary.ServiceAndExplorerHits));
			sb.AppendLine(string.Format("Аномалий службы DPS:    {0}", lastSummary.DpsAnomalyCount));
			sb.AppendLine(string.Format("Инжекций csrss.exe:      {0}", lastSummary.CsrssInjectionCount));
			sb.AppendLine(string.Format("Статус защиты csrss.exe: {0}", lastSummary.CsrssAccessDenied ? "🔒 Заблокирован Kernel PPL / Античитом (переключено на безопасный режим)" : "Доступен для анализа"));
			return sb.ToString();
		}

		private string BuildBanAndManualReference(SystemInformerEvidenceItem it)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ВЕРДИКТ ПО ВЫБРАННОМУ АРТЕФАКТУ И КОМАНДА БАНА ===");
			if (it != null && it.IsViolation)
			{
				string rule = string.IsNullOrEmpty(it.RuleViolation) ? "5.01" : it.RuleViolation;
				sb.AppendLine(string.Format("🚨 ОБНАРУЖЕНО НАРУШЕНИЕ РЕГЛАМЕНТА {0}!", rule));
				sb.AppendLine(string.Format("Процесс: {0} | Метод: {1}", it.ProcessName, it.SourceMethod));
				sb.AppendLine(string.Format("Улика:   {0}", it.DetectedStringOrPath));
				sb.AppendLine();
				sb.AppendLine("Рекомендуемая команда бана:");
				sb.AppendLine(BuildBanCommandForItem(it));
			}
			else if (it != null)
			{
				sb.AppendLine("🟢 Выбранный артефакт носит информационный характер или не подтверждает факт нарушения.");
			}
			else
			{
				sb.AppendLine("Выберите строку в таблице сверху для формирования прицельной команды бана.");
			}

			sb.AppendLine();
			sb.AppendLine("=========================================================================");
			sb.AppendLine("=== СПРАВОЧНИК МЕТОДИКИ МАНУАЛА «SYSTEM INFORMER / PROCESS HACKER 2» ===");
			sb.AppendLine("=========================================================================");
			sb.AppendLine();
			sb.AppendLine("1. АНАЛИЗ ПРОЦЕССА JAVAW.EXE (Memory -> Strings: Min Len = 4, все галочки):");
			sb.AppendLine("   - Каталоги игры: \\mods, \\versions, \\libraries, \\logs, \\config, \\resourcepacks, \\shaderpacks.");
			sb.AppendLine("   - Сигнатуры читов в ОЗУ: hitbox:, stubborn.website, reach:, bushroot, ASM: (>100 ClownClient / 10 DeadCode),");
			sb.AppendLine("     <-8dd / :79Q (Doomsday), exitsave (Troxill), Triggerbot, baobab, killaura, expensive, celka, 71KD, HNtXxl,");
			sb.AppendLine("     SCHEDULED_EXECUTABLES, lookAheadStep, .]+5+]5, 5+]5]]q, 10^]^]]]!!46, Walvbt#:, #Hit, Zero/Time, #THV,");
			sb.AppendLine("     BreakHitsOn, BeackGa:, net.minecraftforge.ASMEventHandler.31.wait(long, int), clowdy, Derick1337, okuma:,");
			sb.AppendLine("     sfxcmd, Az85', 71L;, VAPE4DLL, InvisibleHitbox, hLit/unimi/dsi/fastutil/objects/Object2DoubleMap;>;, baritone.");
			sb.AppendLine();
			sb.AppendLine("2. АУДИТ МОДУЛЕЙ .DLL В JAVAW.EXE И ИСТОРИЯ INJECT DLL:");
			sb.AppendLine("   - Проверка колонок Modules: Description (пустое), Size (веса 1.42 МБ, 1.43 МБ, 1.56 МБ, 1.89 МБ), LoadTime.");
			sb.AppendLine("   - Критерий бана: пустой Description + совпадение веса (1.42/1.43/1.56/1.89 МБ) + разрыв LoadTime >= 2 мин");
			sb.AppendLine("     при отсутствии DLL на диске (удалена после инжекта).");
			sb.AppendLine("   - Аудит списка InjectDllHistory в реестре и настроек Process Hacker 2 / System Informer (settings.xml).");
			sb.AppendLine();
			sb.AppendLine("3. EXPLORER.EXE:");
			sb.AppendLine("   - Шаблоны `pcaclient` (кэш Помощника по совместимости программ) и `file:///` (.exe, .dll, .jar).");
			sb.AppendLine();
			sb.AppendLine("4. DCOMLAUNCH / PLUGPLAY (svchost.exe):");
			sb.AppendLine("   - Поиск полных путей `.jar` файлов (запущенные чит-моды, инжекторы и автокликеры).");
			sb.AppendLine();
			sb.AppendLine("5. PCASVC (svchost.exe):");
			sb.AppendLine("   - Поиск `jar` без точки (выявляет запуск `java -jar` с поддельными расширениями) и имён читов.");
			sb.AppendLine();
			sb.AppendLine("6. СЛУЖБА DPS (Diagnostic Policy Service, svchost.exe):");
			sb.AppendLine("   - Регулярные выражения `^([a-zA-Z]:\\\\.+)\\\\?$` и `^!![A-Z]((?!Exe).)*$` (файлы без .exe и переименования).");
			sb.AppendLine("   - Метки времени `!YYYY/MM/DD:HH:MM:SS!` и выявление подмены исполняемых файлов (DPS Replace).");
			sb.AppendLine();
			sb.AppendLine("7. ПРОЦЕСС CSRSS.EXE (с защитой от BSOD / KernelDrive):");
			sb.AppendLine("   - Регулярные выражения `^[A-Z]:\\\\.+.exe$`, `^[A-Z]:\\\\.+.dll$`, `^(?:\\\\\\\\\\?\\\\)?[A-Za-z]:\\.+$`.");
			sb.AppendLine("   - Выявление неподписанных DLL и замаскированных исполняемых файлов (.tmp, .png и т.д.).");
			sb.AppendLine("   - ВАЖНО: Если csrss.exe защищён KernelDrive или античитом (Vanguard, Faceit), НЕ включайте драйвер во избежание BSOD!");
			return sb.ToString();
		}

		private string BuildBanCommandForItem(SystemInformerEvidenceItem it)
		{
			if (it == null)
			{
				return string.Format("/ban {0} 30d 5.01 - Чит в памяти процесса (System Informer)", targetPlayer);
			}
			string rule = string.IsNullOrEmpty(it.RuleViolation) ? "5.01" : it.RuleViolation;
			string proc = string.IsNullOrEmpty(it.ProcessName) ? "process" : it.ProcessName;
			string art = string.IsNullOrEmpty(it.DetectedStringOrPath) ? "артефакт" : it.DetectedStringOrPath;
			if (art.Length > 48) art = art.Substring(0, 45) + "...";

			if (rule.Contains("5.08"))
			{
				return string.Format("/ban {0} 30d 5.08 - Обход проверки / подмена / сокрытие ({1}: {2})", targetPlayer, proc, art);
			}
			return string.Format("/ban {0} 30d {1} - Запрещённое ПО в памяти ({2}: {3})", targetPlayer, rule, proc, art);
		}

		private void CopyBanCommand()
		{
			SystemInformerEvidenceItem selected = null;
			if (lvArtifacts.SelectedItems.Count > 0)
			{
				selected = lvArtifacts.SelectedItems[0].Tag as SystemInformerEvidenceItem;
			}
			else
			{
				foreach (SystemInformerEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						selected = it;
						break;
					}
				}
			}

			if (selected == null)
			{
				MessageBox.Show("Нет нарушений или выбранного артефакта для формирования команды бана.", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = BuildBanCommandForItem(selected);
			try
			{
				Clipboard.SetText(cmd);
				MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось скопировать в буфер: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyDiscordSummary()
		{
			int total = lastSummary != null ? lastSummary.TotalArtifacts : allItems.Count;
			int viol = lastSummary != null ? lastSummary.ViolationCount : 0;
			int jStr = lastSummary != null ? lastSummary.JavawStringHits : 0;
			int jDll = lastSummary != null ? lastSummary.SuspiciousDllCount : 0;
			int svc = lastSummary != null ? lastSummary.ServiceAndExplorerHits : 0;
			int dps = lastSummary != null ? lastSummary.DpsAnomalyCount : 0;
			int csrss = lastSummary != null ? lastSummary.CsrssInjectionCount : 0;
			bool csrssDenied = lastSummary != null && lastSummary.CsrssAccessDenied;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine(string.Format("ОТЧЕТ SYSTEM INFORMER / PROCESS HACKER 2 — ИГРОК: {0}", targetPlayer));
			sb.AppendLine(string.Format("Всего артефактов:          {0}", total));
			sb.AppendLine(string.Format("Нарушения (5.01 / 5.08):   {0}", viol));
			sb.AppendLine(string.Format("Строки памяти javaw.exe:   {0}", jStr));
			sb.AppendLine(string.Format("Подозрительные DLL javaw:  {0}", jDll));
			sb.AppendLine(string.Format("Explorer / Dcom / PcaSvc:  {0}", svc));
			sb.AppendLine(string.Format("Аномалии службы DPS:       {0}", dps));
			sb.AppendLine(string.Format("Инжекции csrss.exe:        {0} {1}", csrss, csrssDenied ? "(Access Denied / Kernel PPL)" : ""));
			if (viol > 0)
			{
				sb.AppendLine("Ключевые улики:");
				int added = 0;
				foreach (SystemInformerEvidenceItem it in allItems)
				{
					if (!it.IsViolation) continue;
					sb.AppendLine(string.Format(" - [{0}] {1} ({2}): {3}", it.RuleViolation, it.ProcessName, it.SourceMethod, it.DetectedStringOrPath));
					added++;
					if (added >= 8) break;
				}
			}
			else
			{
				sb.AppendLine("Вердикт: ЧИСТО (нарушений в памяти процессов не выявлено)");
			}
			sb.AppendLine("```");

			try
			{
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Discord отчёт System Informer скопирован в буфер обмена!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера обмена: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportHtmlReport()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML отчет (*.html)|*.html";
			sfd.FileName = string.Format("SystemInformerAudit_{0}_{1:yyyyMMdd_HHmm}.html", targetPlayer, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Криминалистический отчёт System Informer / Process Hacker 2</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background: #0c1017; color: #c9d1d9; font-family: 'Segoe UI', Tahoma, sans-serif; padding: 22px; }");
					sb.AppendLine("h1 { color: #2ee6d6; margin-bottom: 6px; }");
					sb.AppendLine(".meta { color: #8b949e; margin-bottom: 18px; }");
					sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px; }");
					sb.AppendLine("th, td { border: 1px solid #30363d; padding: 8px 10px; text-align: left; word-break: break-word; }");
					sb.AppendLine("th { background: #161b22; color: #8b949e; }");
					sb.AppendLine("tr:nth-child(even) { background: #111620; }");
					sb.AppendLine(".crit { color: #f85149; font-weight: bold; }");
					sb.AppendLine(".warn { color: #ffa726; }");
					sb.AppendLine(".ok { color: #66bb6a; }");
					sb.AppendLine("</style></head><body>");
					sb.AppendLine(string.Format("<h1>🔬 Криминалистический отчёт System Informer / Process Hacker 2: {0}</h1>", HtmlEsc(targetPlayer)));
					sb.AppendLine(string.Format("<div class='meta'>Дата проверки: {0:dd.MM.yyyy HH:mm:ss} | Регламент «Атлантис» 5.01–5.08 | Нарушений: {1} | Всего артефактов: {2}</div>",
						DateTime.Now, lastSummary != null ? lastSummary.ViolationCount : 0, allItems.Count));
					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Процесс</th><th>PID</th><th>Категория</th><th>Метод</th><th>Артефакт / Путь / Строка</th><th>Вес</th><th>Время</th><th>Правило</th><th>Вердикт</th></tr>");

					int rowIdx = 1;
					foreach (SystemInformerEvidenceItem it in allItems)
					{
						string cls = (it.Severity == CheckSeverity.Critical || it.IsViolation) ? "crit" : ((it.Severity == CheckSeverity.Warning) ? "warn" : "ok");
						sb.AppendLine(string.Format("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9}</td><td>{10}</td></tr>",
							cls,
							rowIdx++,
							HtmlEsc(it.ProcessName ?? "—"),
							it.ProcessId > 0 ? it.ProcessId.ToString() : "—",
							HtmlEsc(FormatCategoryName(it.Category)),
							HtmlEsc(it.SourceMethod ?? "—"),
							HtmlEsc(it.DetectedStringOrPath ?? "—"),
							HtmlEsc(it.FileSizeOrWeight ?? "—"),
							HtmlEsc(it.TimeInfo ?? "—"),
							HtmlEsc(it.RuleViolation ?? "—"),
							HtmlEsc(it.Verdict ?? "—")));
					}

					sb.AppendLine("</table></body></html>");
					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("HTML отчёт успешно сохранён:\n" + sfd.FileName, "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка экспорта HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private static string HtmlEsc(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
		}
	}
}
