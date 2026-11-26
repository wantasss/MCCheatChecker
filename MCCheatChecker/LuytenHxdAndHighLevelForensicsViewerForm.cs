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
	public class LuytenHxdAndHighLevelForensicsViewerForm : Form
	{
		private string targetPlayer;
		private HighLevelScanSummary lastSummary;
		private List<HighLevelEvidenceItem> allItems = new List<HighLevelEvidenceItem>();
		private List<HighLevelEvidenceItem> filteredItems = new List<HighLevelEvidenceItem>();
		private HighLevelCheckCategory currentFilter = HighLevelCheckCategory.All;
		private int scanGeneration = 0;
		private bool isScanning = false;

		private Label lblTitle;
		private Label lblSubtitle;
		private Label lblScanStatus;
		private DarkTextBox txtSearch;
		private DarkListView lvArtifacts;

		private KpiCard kpiTotal;
		private KpiCard kpiViolations;
		private KpiCard kpiLuytenMods;
		private KpiCard kpiHxdDll;
		private KpiCard kpiBamSystemDlc;
		private KpiCard kpiEventLogs;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterLuytenMods;
		private DarkButton btnFilterHxdDll;
		private DarkButton btnFilterBamUnsigned;
		private DarkButton btnFilterSystemDlc;
		private DarkButton btnFilterEventsReg;

		private DarkTabView tabDetails;
		private DarkTextBoxMultiline txtDetailProtocol;
		private DarkTextBoxMultiline txtRawDumpTab;
		private DarkTextBoxMultiline txtBanAndGuideTab;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		public LuytenHxdAndHighLevelForensicsViewerForm() : this("")
		{
		}

		public LuytenHxdAndHighLevelForensicsViewerForm(string player)
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
			this.Text = "🧬 Инспектор Luyten REGEX MODS, HxD Hex DLL, BAM & SystemDLC (Tool 89 / Step 86) — MCCheatChecker";
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
			lblTitle.Text = "ИНСПЕКТОР HIGH-LEVEL: LUYTEN REGEX MODS, HXD HEX DLL, BAM NOT SIGNED & SYSTEMDLC";
			lblTitle.Font = Ui.FontTitle(12f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.AutoSize = true;
			lblTitle.Location = new Point(16, 10);
			pnlHeader.Controls.Add(lblTitle);

			lblSubtitle = new Label();
			lblSubtitle.Text = string.Format("Игрок: {0} | Декомпиляция байткода модов (Forge/Fabric/LabyMod), Hex-аудит DLL, BAM неподписанные, Журналы событий и SystemDLC", targetPlayer);
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

			kpiTotal = new KpiCard("ВСЕГО АРТЕФАКТОВ", Ui.AccentTeal, "Полная сводка High-Level");
			kpiTotal.Size = new Size(215, 68);
			kpiTotal.Location = new Point(12, 8);
			kpiTotal.Click += delegate { SetFilter(HighLevelCheckCategory.All); };

			kpiViolations = new KpiCard("НАРУШЕНИЯ (5.01/5.08)", Ui.AccentRed, "Подтверждённые читы / DLC");
			kpiViolations.Size = new Size(215, 68);
			kpiViolations.Location = new Point(235, 8);
			kpiViolations.Click += delegate { SetFilter(HighLevelCheckCategory.Violations); };

			kpiLuytenMods = new KpiCard("LUYTEN REGEX MODS", Ui.AccentPurple, "Forge / Fabric / LabyMod");
			kpiLuytenMods.Size = new Size(215, 68);
			kpiLuytenMods.Location = new Point(458, 8);
			kpiLuytenMods.Click += delegate { SetFilter(HighLevelCheckCategory.LuytenMods); };

			kpiHxdDll = new KpiCard("HXD HEX DLL АУДИТ", Ui.AccentOrange, "Bytecode / JNI / AABB");
			kpiHxdDll.Size = new Size(215, 68);
			kpiHxdDll.Location = new Point(681, 8);
			kpiHxdDll.Click += delegate { SetFilter(HighLevelCheckCategory.HxdDllHex); };

			kpiBamSystemDlc = new KpiCard("BAM / SYSTEMDLC", Ui.AccentBlue, "Not Signed EXE + SystemDLC");
			kpiBamSystemDlc.Size = new Size(215, 68);
			kpiBamSystemDlc.Location = new Point(904, 8);
			kpiBamSystemDlc.Click += delegate { SetFilter(HighLevelCheckCategory.SystemDlc); };

			kpiEventLogs = new KpiCard("ЖУРНАЛЫ И РЕЕСТР", Ui.AccentGreen, "Event 3079/1102/4616/MRU");
			kpiEventLogs.Size = new Size(220, 68);
			kpiEventLogs.Location = new Point(1127, 8);
			kpiEventLogs.Click += delegate { SetFilter(HighLevelCheckCategory.EventLogsAndRegistry); };

			pnlKpis.Controls.Add(kpiTotal);
			pnlKpis.Controls.Add(kpiViolations);
			pnlKpis.Controls.Add(kpiLuytenMods);
			pnlKpis.Controls.Add(kpiHxdDll);
			pnlKpis.Controls.Add(kpiBamSystemDlc);
			pnlKpis.Controls.Add(kpiEventLogs);

			// Filter & Search Panel
			Panel pnlFilters = new Panel();
			pnlFilters.Dock = DockStyle.Top;
			pnlFilters.Height = 82;
			pnlFilters.BackColor = Ui.Surface;
			pnlFilters.Padding = new Padding(12, 8, 12, 8);

			btnFilterAll = CreateFilterButton("Все (0)", 12, 8, 118, HighLevelCheckCategory.All);
			btnFilterAll.AccentStyle = true;
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (0)", 136, 8, 152, HighLevelCheckCategory.Violations);
			btnFilterLuytenMods = CreateFilterButton("🧩 Luyten Моды (0)", 294, 8, 162, HighLevelCheckCategory.LuytenMods);
			btnFilterHxdDll = CreateFilterButton("🔬 HxD Hex DLL (0)", 462, 8, 158, HighLevelCheckCategory.HxdDllHex);
			btnFilterBamUnsigned = CreateFilterButton("📝 BAM Not Signed (0)", 626, 8, 174, HighLevelCheckCategory.BamUnsigned);
			btnFilterSystemDlc = CreateFilterButton("💉 SystemDLC (0)", 806, 8, 150, HighLevelCheckCategory.SystemDlc);
			btnFilterEventsReg = CreateFilterButton("📜 Журналы/Реестр (0)", 962, 8, 176, HighLevelCheckCategory.EventLogsAndRegistry);

			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск (мод, DLL, сигнатура, вердикт):";
			lblSearch.AutoSize = true;
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(12, 50);

			txtSearch = new DarkTextBox();
			txtSearch.Size = new Size(340, 24);
			txtSearch.Location = new Point(285, 47);
			txtSearch.TextChanged += delegate { ApplyFilters(); };

			lblScanStatus = new Label();
			lblScanStatus.Text = "Ожидание запуска High-Level проверки...";
			lblScanStatus.AutoSize = true;
			lblScanStatus.ForeColor = Ui.AccentTeal;
			lblScanStatus.Location = new Point(645, 50);

			pnlFilters.Controls.Add(btnFilterAll);
			pnlFilters.Controls.Add(btnFilterViolations);
			pnlFilters.Controls.Add(btnFilterLuytenMods);
			pnlFilters.Controls.Add(btnFilterHxdDll);
			pnlFilters.Controls.Add(btnFilterBamUnsigned);
			pnlFilters.Controls.Add(btnFilterSystemDlc);
			pnlFilters.Controls.Add(btnFilterEventsReg);
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
			lvArtifacts.Columns.Add("Модуль", 140);
			lvArtifacts.Columns.Add("Категория", 135);
			lvArtifacts.Columns.Add("Цель / Файл / Событие", 260);
			lvArtifacts.Columns.Add("Ядро / Тип", 125);
			lvArtifacts.Columns.Add("Совпадения сигнатур", 275);
			lvArtifacts.Columns.Add("Время / Размер", 125);
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

			txtBanAndGuideTab = new DarkTextBoxMultiline();
			txtBanAndGuideTab.Dock = DockStyle.Fill;
			txtBanAndGuideTab.ReadOnly = true;

			tabDetails.AddPage("Криминалистический протокол артефакта", txtDetailProtocol);
			tabDetails.AddPage("Технический дамп и метаданные", txtRawDumpTab);
			tabDetails.AddPage("Справочник REGEX MODS, HxD, SystemDLC и Команда бана", txtBanAndGuideTab);

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

		private DarkButton CreateFilterButton(string text, int x, int y, int width, HighLevelCheckCategory category)
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
			txtDetailProtocol.Text = "Запуск нативного анализа Luyten REGEX MODS, HxD Hex DLL, BAM Not Signed, Event Log и SystemDLC...";
			txtRawDumpTab.Text = "Подготовка технического дампа и телеметрии...";
			txtBanAndGuideTab.Text = BuildBanAndManualGuide(null);
		}

		private void SetFilter(HighLevelCheckCategory category)
		{
			currentFilter = category;
			UpdateFilterButtonHighlights();
			ApplyFilters();
		}

		private void UpdateFilterButtonHighlights()
		{
			btnFilterAll.AccentStyle = (currentFilter == HighLevelCheckCategory.All);
			btnFilterViolations.AccentStyle = (currentFilter == HighLevelCheckCategory.Violations);
			btnFilterLuytenMods.AccentStyle = (currentFilter == HighLevelCheckCategory.LuytenMods);
			btnFilterHxdDll.AccentStyle = (currentFilter == HighLevelCheckCategory.HxdDllHex);
			btnFilterBamUnsigned.AccentStyle = (currentFilter == HighLevelCheckCategory.BamUnsigned);
			btnFilterSystemDlc.AccentStyle = (currentFilter == HighLevelCheckCategory.SystemDlc);
			btnFilterEventsReg.AccentStyle = (currentFilter == HighLevelCheckCategory.EventLogsAndRegistry);
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
				lblScanStatus.Text = "⏳ Сканирование Luyten (.jar), HxD Hex (.dll), BAM, Event Logs, Registry и SystemDLC...";
			}

			ThreadPool.QueueUserWorkItem(delegate
			{
				HighLevelScanSummary summary = null;
				Exception scanError = null;
				try
				{
					summary = HighLevelLuytenHxdEngine.RunScan(true);
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
							txtDetailProtocol.Text = "Ошибка при выполнении HighLevelLuytenHxdEngine.RunScan:\r\n" + scanError.ToString();
							return;
						}
						ApplyScanSummary(summary);
					});
				}
				catch { }
			});
		}

		private void ApplyScanSummary(HighLevelScanSummary summary)
		{
			lastSummary = summary ?? new HighLevelScanSummary();
			allItems = lastSummary.Items != null ? new List<HighLevelEvidenceItem>(lastSummary.Items) : new List<HighLevelEvidenceItem>();

			allItems.Sort(delegate(HighLevelEvidenceItem a, HighLevelEvidenceItem b)
			{
				if (a.IsViolation != b.IsViolation) return b.IsViolation.CompareTo(a.IsViolation);
				int sevCmp = b.Severity.CompareTo(a.Severity);
				if (sevCmp != 0) return sevCmp;
				return string.Compare(a.TargetName ?? "", b.TargetName ?? "", StringComparison.OrdinalIgnoreCase);
			});

			kpiTotal.SetValue(lastSummary.TotalScanned);
			kpiViolations.SetValue(lastSummary.ViolationCount);
			kpiLuytenMods.SetValue(lastSummary.LuytenModHits);
			kpiHxdDll.SetValue(lastSummary.HxdDllHits);
			kpiBamSystemDlc.SetValue(lastSummary.BamUnsignedHits + lastSummary.SystemDlcHits);
			kpiEventLogs.SetValue(lastSummary.EventLogHits);

			UpdateFilterButtonCounts();

			string dur = string.IsNullOrEmpty(lastSummary.ScanDurationMs) ? "0" : lastSummary.ScanDurationMs;
			if (lastSummary.SystemDlcHits > 0)
			{
				lblScanStatus.ForeColor = Ui.AccentRed;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🚨 ОБНАРУЖЕН SYSTEMDLC ({1}) и нарушения: {2}", dur, lastSummary.SystemDlcHits, lastSummary.ViolationCount);
			}
			else if (lastSummary.ViolationCount > 0)
			{
				lblScanStatus.ForeColor = Ui.AccentRed;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🚨 Найдено нарушений: {1}", dur, lastSummary.ViolationCount);
			}
			else
			{
				lblScanStatus.ForeColor = Ui.AccentGreen;
				lblScanStatus.Text = string.Format("Готово ({0} мс) | 🟢 Нарушений Luyten / HxD / BAM / SystemDLC не выявлено", dur);
			}

			ApplyFilters();
		}

		private void UpdateFilterButtonCounts()
		{
			int cAll = allItems.Count;
			int cViol = 0;
			int cLuyten = 0;
			int cHxd = 0;
			int cBam = 0;
			int cSysDlc = 0;
			int cEvReg = 0;

			foreach (HighLevelEvidenceItem it in allItems)
			{
				if (it.IsViolation || it.Category == HighLevelCheckCategory.Violations) cViol++;
				if (it.Category == HighLevelCheckCategory.LuytenMods) cLuyten++;
				else if (it.Category == HighLevelCheckCategory.HxdDllHex) cHxd++;
				else if (it.Category == HighLevelCheckCategory.BamUnsigned) cBam++;
				else if (it.Category == HighLevelCheckCategory.SystemDlc) cSysDlc++;
				else if (it.Category == HighLevelCheckCategory.EventLogsAndRegistry) cEvReg++;
			}

			btnFilterAll.Text = string.Format("Все ({0})", cAll);
			btnFilterViolations.Text = string.Format("🚨 Нарушения ({0})", cViol);
			btnFilterLuytenMods.Text = string.Format("🧩 Luyten Моды ({0})", cLuyten);
			btnFilterHxdDll.Text = string.Format("🔬 HxD Hex DLL ({0})", cHxd);
			btnFilterBamUnsigned.Text = string.Format("📝 BAM Not Signed ({0})", cBam);
			btnFilterSystemDlc.Text = string.Format("💉 SystemDLC ({0})", cSysDlc);
			btnFilterEventsReg.Text = string.Format("📜 Журналы/Реестр ({0})", cEvReg);
		}

		private void ApplyFilters()
		{
			string search = (txtSearch != null && txtSearch.Text != null) ? txtSearch.Text.ToLowerInvariant().Trim() : "";
			filteredItems.Clear();

			foreach (HighLevelEvidenceItem it in allItems)
			{
				bool matchCat = true;
				switch (currentFilter)
				{
					case HighLevelCheckCategory.Violations:
						matchCat = it.IsViolation || it.Category == HighLevelCheckCategory.Violations;
						break;
					case HighLevelCheckCategory.All:
						matchCat = true;
						break;
					default:
						matchCat = (it.Category == currentFilter);
						break;
				}
				if (!matchCat) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matchSearch = ContainsIgnoreCase(it.SourceModule, search) ||
									   ContainsIgnoreCase(it.TargetName, search) ||
									   ContainsIgnoreCase(it.CoreOrType, search) ||
									   ContainsIgnoreCase(it.MatchedSignatures, search) ||
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
			foreach (HighLevelEvidenceItem it in filteredItems)
			{
				ListViewItem lvi = new ListViewItem(idx.ToString());
				lvi.SubItems.Add(string.IsNullOrEmpty(it.SourceModule) ? "—" : it.SourceModule);
				lvi.SubItems.Add(FormatCategoryName(it.Category));
				lvi.SubItems.Add(string.IsNullOrEmpty(it.TargetName) ? "—" : it.TargetName);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.CoreOrType) ? "—" : it.CoreOrType);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.MatchedSignatures) ? "—" : it.MatchedSignatures);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.TimeOrSize) ? "—" : it.TimeOrSize);
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
					? "⏳ Сканирование High-Level ещё выполняется..."
					: "Записей не найдено по выбранному фильтру или поисковому запросу.";
				txtRawDumpTab.Text = BuildSummaryTelemetryText();
				txtBanAndGuideTab.Text = BuildBanAndManualGuide(null);
			}
		}

		private static Color GetRowColor(HighLevelEvidenceItem it)
		{
			if (it == null) return Ui.TextPrimary;
			if (it.Severity == CheckSeverity.Critical) return Ui.AccentRed;
			if (it.Severity == CheckSeverity.NeedsCheck) return Ui.AccentPurple;
			if (it.Severity == CheckSeverity.Warning) return Ui.AccentOrange;
			if (it.IsViolation) return Ui.AccentRed;
			return Ui.AccentGreen;
		}

		private static string FormatCategoryName(HighLevelCheckCategory cat)
		{
			switch (cat)
			{
				case HighLevelCheckCategory.LuytenMods: return "🧩 Luyten Моды";
				case HighLevelCheckCategory.HxdDllHex: return "🔬 HxD Hex DLL";
				case HighLevelCheckCategory.BamUnsigned: return "📝 BAM Unsigned";
				case HighLevelCheckCategory.SystemDlc: return "💉 SystemDLC";
				case HighLevelCheckCategory.EventLogsAndRegistry: return "📜 Журналы/Реестр";
				case HighLevelCheckCategory.Violations: return "🚨 Нарушение";
				default: return "📋 Все";
			}
		}

		private void UpdateSelectedDetail()
		{
			if (lvArtifacts.SelectedItems.Count == 0) return;
			HighLevelEvidenceItem it = lvArtifacts.SelectedItems[0].Tag as HighLevelEvidenceItem;
			if (it == null) return;

			// Tab 1: Протокол
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНО-КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ HIGH-LEVEL (LUYTEN / HXD / SYSTEMDLC) ===");
			sb.AppendLine(string.Format("Модуль анализа:      {0}", string.IsNullOrEmpty(it.SourceModule) ? "—" : it.SourceModule));
			sb.AppendLine(string.Format("Категория:           {0}", FormatCategoryName(it.Category)));
			sb.AppendLine(string.Format("Объект / Путь:       {0}", string.IsNullOrEmpty(it.TargetName) ? "—" : it.TargetName));
			sb.AppendLine(string.Format("Ядро / Тип:          {0}", string.IsNullOrEmpty(it.CoreOrType) ? "—" : it.CoreOrType));
			sb.AppendLine(string.Format("Сигнатуры REGEX/HEX: {0}", string.IsNullOrEmpty(it.MatchedSignatures) ? "—" : it.MatchedSignatures));
			sb.AppendLine(string.Format("Время / Размер:      {0}", string.IsNullOrEmpty(it.TimeOrSize) ? "—" : it.TimeOrSize));
			sb.AppendLine(string.Format("Уровень угрозы:      {0}", it.Severity));
			sb.AppendLine(string.Format("Статья регламента:   {0}", string.IsNullOrEmpty(it.RuleViolation) ? "—" : it.RuleViolation));
			sb.AppendLine(string.Format("Вердикт:             {0}", string.IsNullOrEmpty(it.Verdict) ? "—" : it.Verdict));
			sb.AppendLine();
			sb.AppendLine("Описание:");
			sb.AppendLine(string.IsNullOrEmpty(it.Description) ? "Без дополнительного описания." : it.Description);
			sb.AppendLine();
			sb.AppendLine("Подробная расшифровка:");
			sb.AppendLine(string.IsNullOrEmpty(it.FullDetails) ? "Нет расширенных данных." : it.FullDetails);
			txtDetailProtocol.Text = sb.ToString();

			// Tab 2: Технический дамп и метаданные
			StringBuilder sbRaw = new StringBuilder();
			sbRaw.AppendLine("=== ПОЛНЫЕ ТЕХНИЧЕСКИЕ ДАННЫЕ И МЕТАДАННЫЕ ДИСКА ===");
			if (!string.IsNullOrEmpty(it.FullDetails))
			{
				sbRaw.AppendLine(it.FullDetails);
				sbRaw.AppendLine();
			}
			AppendFileMetadataIfApplicable(sbRaw, it.TargetName);
			sbRaw.AppendLine();
			sbRaw.AppendLine(BuildSummaryTelemetryText());
			txtRawDumpTab.Text = sbRaw.ToString();

			// Tab 3: Справочник + команда бана
			txtBanAndGuideTab.Text = BuildBanAndManualGuide(it);
		}

		private static void AppendFileMetadataIfApplicable(StringBuilder sb, string rawPath)
		{
			if (string.IsNullOrEmpty(rawPath)) return;
			try
			{
				string clean = rawPath.Trim().Trim('"', '\'');
				if (clean.StartsWith(@"\\?\")) clean = clean.Substring(4);
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
			sb.AppendLine("--- ТЕЛЕМЕТРИЯ HIGH-LEVEL ПРОВЕРКИ (LUYTEN / HXD / BAM / SYSTEMDLC) ---");
			if (lastSummary == null)
			{
				sb.AppendLine("Данные сканирования отсутствуют.");
				return sb.ToString();
			}
			sb.AppendLine(string.Format("Время сканирования:       {0} мс", string.IsNullOrEmpty(lastSummary.ScanDurationMs) ? "—" : lastSummary.ScanDurationMs));
			sb.AppendLine(string.Format("Всего объектов / записей: {0}", lastSummary.TotalScanned));
			sb.AppendLine(string.Format("Подтверждено нарушений:   {0}", lastSummary.ViolationCount));
			sb.AppendLine(string.Format("Luyten REGEX MODS хитов:  {0}", lastSummary.LuytenModHits));
			sb.AppendLine(string.Format("HxD Hex DLL аномалий:     {0}", lastSummary.HxdDllHits));
			sb.AppendLine(string.Format("BAM Not Signed находок:   {0}", lastSummary.BamUnsignedHits));
			sb.AppendLine(string.Format("SystemDLC следов:         {0}", lastSummary.SystemDlcHits));
			sb.AppendLine(string.Format("Событий EventLog/Реестра: {0}", lastSummary.EventLogHits));
			return sb.ToString();
		}

		private string BuildBanAndManualGuide(HighLevelEvidenceItem it)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ВЕРДИКТ ПО ВЫБРАННОМУ АРТЕФАКТУ И КОМАНДА БАНА ===");
			if (it != null && it.IsViolation)
			{
				string rule = string.IsNullOrEmpty(it.RuleViolation) ? "5.01" : it.RuleViolation;
				sb.AppendLine(string.Format("🚨 ОБНАРУЖЕНО НАРУШЕНИЕ РЕГЛАМЕНТА {0}!", rule));
				sb.AppendLine(string.Format("Модуль: {0} | Объект: {1}", it.SourceModule, it.TargetName));
				sb.AppendLine(string.Format("Улики:  {0}", it.MatchedSignatures));
				sb.AppendLine();
				sb.AppendLine("Рекомендуемая команда бана:");
				sb.AppendLine(BuildBanCommandForItem(it));
			}
			else if (it != null)
			{
				sb.AppendLine("🟢 Выбранный объект чист или носит информационный характер (1 одиночный метод или WhiteList не считается баном).");
			}
			else
			{
				sb.AppendLine("Выберите строку в таблице сверху для генерации прицельной команды бана.");
			}

			sb.AppendLine();
			sb.AppendLine("============================================================================");
			sb.AppendLine("=== СЛОВАРЬ «REGEX MODS» (LUYTEN), HXD HEX-EDITOR, BAM И SYSTEMDLC      ===");
			sb.AppendLine("============================================================================");
			sb.AppendLine();
			sb.AppendLine("1. LUYTEN DECOMPILER & «REGEX MODS» ПО ЯДРАМ МОДОВ (Исключение: >= 2 совпадений!):");
			sb.AppendLine("   Если в классе мода встречается ТОЛЬКО 1 метод из шаблона — ЭТО НЕ БАН.");
			sb.AppendLine("   Исключения (WhiteList Атлантис): Marlow, Haram, End Crystal, Walksy Anchor, Hero's, CuteBow.");
			sb.AppendLine();
			sb.AppendLine("   [FORGE] (определение ядра: net/minecraft/entity/player, net/minecraft/util/math, AxisAlignedBB, Vec3d):");
			sb.AppendLine("     * func_174826_a               -> получить позицию сущности (Vec3d)");
			sb.AppendLine("     * func_226277_ct_             -> получить позицию сущности (Vector3d, Mojang)");
			sb.AppendLine("     * func_174813_aQ()            -> получить bounding box сущности (AxisAlignedBB)");
			sb.AppendLine("     * .field_72338_b              -> minY bounding box");
			sb.AppendLine("     * func_226281_cx_func_226277_ct_ -> minY bounding box (Mojang)");
			sb.AppendLine("     * .field_72337_e              -> maxY bounding box");
			sb.AppendLine("     * func_226281_cx_             -> maxY bounding box (Mojang)");
			sb.AppendLine("     * AxisAlignedBB               -> класс bounding box (AABB)");
			sb.AppendLine();
			sb.AppendLine("   [FABRIC] (определение ядра: org/spongepowered/asm/mixin/injection):");
			sb.AppendLine("     * method_5829                 -> getBoundingBox()");
			sb.AppendLine("     * class_238                   -> класс bounding box (Box)");
			sb.AppendLine("     * method_23317                -> minY");
			sb.AppendLine("     * method_23321                -> maxY");
			sb.AppendLine("     * method_5857                 -> getPos()");
			sb.AppendLine();
			sb.AppendLine("   [LABYMOD] (определение ядра: net/labymod/api/event/event/client/renderer):");
			sb.AppendLine("     * dci                         -> класс Entity");
			sb.AppendLine("     * .cD()                       -> получить bounding box");
			sb.AppendLine("     * .cc()                       -> bounding box (AABB)");
			sb.AppendLine("     * .cc().b                     -> minY");
			sb.AppendLine("     * .cH()                       -> текущая Y-позиция сущности");
			sb.AppendLine("     * .cc().e                     -> maxY");
			sb.AppendLine();
			sb.AppendLine("2. HEX-АУДИТ БИБЛИОТЕК (Анализ неподписанных/сторонних .DLL):");
			sb.AppendLine("   - Сигнатуры хитбоксов/дальности в модулях: bounding box и классы сущностей Minecraft.");
			sb.AppendLine("   - Встроенный Java-байткод внутри DLL: заголовок скомпилированного .class-файла (магик Java-класса).");
			sb.AppendLine("   - Системные вызовы работы с памятью, потоками, снимками процессов и вводом + ссылки на JNI/JVM и модули SystemDLC/Doomsday.");
			sb.AppendLine();
			sb.AppendLine("3. BAM «NOT SIGNED ONLY»:");
			sb.AppendLine("   - Проверка системного журнала запуска приложений (BAM UserSettings) на неподписанные или удалённые исполняемые файлы в пользовательских папках.");
			sb.AppendLine();
			sb.AppendLine("4. EVENT LOGS (11 ID мануала) И РЕЕСТР:");
			sb.AppendLine("   - События журналов Windows: 3079 (очистка USN), 1102 / 104 (очистка журналов), 4616 (смена времени), 7, 7036, 1001,");
			sb.AppendLine("     1002, 1074, 4663, 4688, 4660. Кэши программ и диалогов проводника в системном реестре.");
			sb.AppendLine();
			sb.AppendLine("5. SYSTEMDLC DEEP DETECTOR:");
			sb.AppendLine("   - Проверка системного диска, пользовательских каталогов, кэша запуска, реестра, памяти и модов по профилю SystemDLC.");
			return sb.ToString();
		}

		private string BuildBanCommandForItem(HighLevelEvidenceItem it)
		{
			if (it == null)
			{
				return string.Format("/ban {0} 30d 5.01 - Обнаружен чит-мод / DLL / SystemDLC (High-Level)", targetPlayer);
			}
			string rule = string.IsNullOrEmpty(it.RuleViolation) ? "5.01" : it.RuleViolation;
			string target = string.IsNullOrEmpty(it.TargetName) ? "объект" : it.TargetName;
			if (target.Length > 45) target = target.Substring(0, 42) + "...";

			if (it.Category == HighLevelCheckCategory.SystemDlc)
			{
				return string.Format("/ban {0} 30d {1} - Обнаружен чит SystemDLC ({2})", targetPlayer, rule, target);
			}
			if (rule.Contains("5.08"))
			{
				return string.Format("/ban {0} 30d 5.08 - Очистка / сокрытие улик ({1}: {2})", targetPlayer, it.SourceModule, target);
			}
			return string.Format("/ban {0} 30d {1} - Чит-модификация / DLL ({2}: {3})", targetPlayer, rule, it.SourceModule, target);
		}

		private void CopyBanCommand()
		{
			HighLevelEvidenceItem selected = null;
			if (lvArtifacts.SelectedItems.Count > 0)
			{
				selected = lvArtifacts.SelectedItems[0].Tag as HighLevelEvidenceItem;
			}
			else
			{
				foreach (HighLevelEvidenceItem it in allItems)
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
			int total = lastSummary != null ? lastSummary.TotalScanned : allItems.Count;
			int viol = lastSummary != null ? lastSummary.ViolationCount : 0;
			int luyten = lastSummary != null ? lastSummary.LuytenModHits : 0;
			int hxd = lastSummary != null ? lastSummary.HxdDllHits : 0;
			int bam = lastSummary != null ? lastSummary.BamUnsignedHits : 0;
			int sysDlc = lastSummary != null ? lastSummary.SystemDlcHits : 0;
			int ev = lastSummary != null ? lastSummary.EventLogHits : 0;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine(string.Format("ОТЧЕТ HIGH-LEVEL (LUYTEN / HXD / BAM / SYSTEMDLC) — ИГРОК: {0}", targetPlayer));
			sb.AppendLine(string.Format("Всего проверено:         {0}", total));
			sb.AppendLine(string.Format("Нарушения (5.01 / 5.08): {0}", viol));
			sb.AppendLine(string.Format("Luyten REGEX MODS:       {0}", luyten));
			sb.AppendLine(string.Format("HxD Hex DLL аномалии:     {0}", hxd));
			sb.AppendLine(string.Format("BAM Not Signed:          {0}", bam));
			sb.AppendLine(string.Format("SystemDLC следы:         {0}", sysDlc));
			sb.AppendLine(string.Format("Журналы/Реестр:          {0}", ev));
			if (viol > 0)
			{
				sb.AppendLine("Ключевые улики:");
				int added = 0;
				foreach (HighLevelEvidenceItem it in allItems)
				{
					if (!it.IsViolation) continue;
					sb.AppendLine(string.Format(" - [{0}] {1} -> {2} ({3})", it.RuleViolation, it.SourceModule, it.TargetName, it.MatchedSignatures));
					added++;
					if (added >= 8) break;
				}
			}
			else
			{
				sb.AppendLine("Вердикт: ЧИСТО (нарушений Luyten / HxD / BAM / SystemDLC не выявлено)");
			}
			sb.AppendLine("```");

			try
			{
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Discord отчёт High-Level скопирован в буфер обмена!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
			sfd.FileName = string.Format("HighLevelAudit_{0}_{1:yyyyMMdd_HHmm}.html", targetPlayer, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Отчёт High-Level: Luyten, HxD Hex, BAM и SystemDLC</title>");
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
					sb.AppendLine(string.Format("<h1>🧬 Отчёт High-Level (Luyten REGEX MODS, HxD DLL, BAM, SystemDLC): {0}</h1>", HtmlEsc(targetPlayer)));
					sb.AppendLine(string.Format("<div class='meta'>Дата проверки: {0:dd.MM.yyyy HH:mm:ss} | Регламент «Атлантис» 5.01–5.08 | Нарушений: {1} | Проверено: {2}</div>",
						DateTime.Now, lastSummary != null ? lastSummary.ViolationCount : 0, allItems.Count));
					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Модуль</th><th>Категория</th><th>Объект</th><th>Ядро / Тип</th><th>Сигнатуры</th><th>Время/Размер</th><th>Правило</th><th>Вердикт</th></tr>");

					int rowIdx = 1;
					foreach (HighLevelEvidenceItem it in allItems)
					{
						string cls = (it.Severity == CheckSeverity.Critical || it.IsViolation) ? "crit" : ((it.Severity == CheckSeverity.Warning) ? "warn" : "ok");
						sb.AppendLine(string.Format("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9}</td></tr>",
							cls,
							rowIdx++,
							HtmlEsc(it.SourceModule ?? "—"),
							HtmlEsc(FormatCategoryName(it.Category)),
							HtmlEsc(it.TargetName ?? "—"),
							HtmlEsc(it.CoreOrType ?? "—"),
							HtmlEsc(it.MatchedSignatures ?? "—"),
							HtmlEsc(it.TimeOrSize ?? "—"),
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
