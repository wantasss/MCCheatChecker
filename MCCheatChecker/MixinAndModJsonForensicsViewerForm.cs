using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public enum MixinModFilter
	{
		All,
		ViolationsOnly,
		ProhibitedMixins,
		CpvpWhitelisted,
		DisguisedMods,
		CleanMods
	}

	public class MixinModEvidenceItem
	{
		public int Index;
		public string ModNameOrFile;
		public string ModId;
		public string Category;
		public string Verdict;
		public string Severity; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public int RiskScore; // 0..100
		public string JarPath;
		public string MixinConfigs;
		public string TargetClasses;
		public string RuleArticle; // "5.01", "5.08", "РАЗРЕШЕНО", "ЧИСТО"
		public bool IsViolation;
		public DateTime Timestamp;
		public Color RowColor;
		public int ObfuscationScore; // 0..100
		public string Obfuscator;
		public string ObfuscationSummary;
		public string DetailedForensics;
		public string TechnicalAnalysis;
		public string BanVerdictGuide;
	}

	public class MixinAndModJsonForensicsViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<MixinModEvidenceItem> allItems = new List<MixinModEvidenceItem>();
		private MixinModFilter currentFilter = MixinModFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiMixins;
		private Label lblKpiDisguised;
		private Label lblKpiCpvp;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterMixins;
		private DarkButton btnFilterCpvp;
		private DarkButton btnFilterDisguised;
		private DarkButton btnFilterClean;

		private DarkTextBox searchBox;
		private DarkListView listView;
		private TabControl detailTabs;
		private DarkTextBoxMultiline txtAnalysis;
		private DarkTextBoxMultiline txtTechnical;
		private DarkTextBoxMultiline txtBanVerdict;

		private DarkButton btnCopyBanCmd;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		public MixinAndModJsonForensicsViewerForm() : this("Player")
		{
		}

		public MixinAndModJsonForensicsViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🧬 Инспектор Mixin & mod.json архивов модов (Tool 84 / Step 81) — MCCheatChecker";
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Icon = SystemIcons.Shield;
			Ui.ApplyDarkScrollbars(this);

			// 1. Верхний заголовок
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 68;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🧬 Инспектор Mixin & mod.json архивов модов (Tool 84 / Step 81)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentPurple;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Глубокий парсинг .jar модов, fabric.mod.json, mcmod.info и Mixin JSON: аудит target-классов (ClientPlayerEntity, EntityRenderer), проверка белого списка CPVP «Атлантис» (Marlow/Haram/Walksy) и маскировки модов (5.01/5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 38);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканера...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(40, 30, 20);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(270, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 290, 20);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// 2. Панель KPI бейджей
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int xOff = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО МОДОВ", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("ЧИТ-МОДЫ (5.01)", "0", Ui.AccentRed, ref xOff, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("СОМНИТЕЛЬНЫЕ MIXIN", "0", Ui.AccentOrange, ref xOff, out lblKpiMixins));
			kpiPanel.Controls.Add(CreateKpiBadge("МАСКИРОВКА (5.08)", "0", Ui.AccentPurple, ref xOff, out lblKpiDisguised));
			kpiPanel.Controls.Add(CreateKpiBadge("CPVP БЕЛЫЙ СПИСОК", "0", Ui.AccentTeal, ref xOff, out lblKpiCpvp));
			kpiPanel.Controls.Add(CreateKpiBadge("ЧИСТЫЕ МОДЫ", "0", Ui.AccentGreen, ref xOff, out lblKpiClean));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", MixinModFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения 5.01 (0)", MixinModFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterMixins = CreateFilterButton("Mixins (0)", MixinModFilter.ProhibitedMixins, ref fX, filterPanel);
			btnFilterCpvp = CreateFilterButton("CPVP Атлантис (0)", MixinModFilter.CpvpWhitelisted, ref fX, filterPanel);
			btnFilterDisguised = CreateFilterButton("Маскировка (0)", MixinModFilter.DisguisedMods, ref fX, filterPanel);
			btnFilterClean = CreateFilterButton("Чистые (0)", MixinModFilter.CleanMods, ref fX, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(filterPanel.Width - 250, 12);
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.AutoSize = true;
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterPanel.Width - 180, 9);
			searchBox.Size = new Size(165, 24);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			// 4. Нижняя панель действий (Bottom Action Bar)
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 48;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 8, 12, 8);
			this.Controls.Add(bottomPanel);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🔨 Скопировать команду бана";
			btnCopyBanCmd.Size = new Size(215, 30);
			btnCopyBanCmd.Location = new Point(12, 9);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			bottomPanel.Controls.Add(btnCopyBanCmd);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Сводка для Discord";
			btnCopyDiscord.Size = new Size(165, 30);
			btnCopyDiscord.Location = new Point(235, 9);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };
			bottomPanel.Controls.Add(btnCopyDiscord);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 30);
			btnExportHtml.Location = new Point(408, 9);
			btnExportHtml.Click += delegate { ExportHtmlReport(); };
			bottomPanel.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(150, 30);
			btnRescan.Location = new Point(566, 9);
			btnRescan.Click += delegate { StartScan(); };
			bottomPanel.Controls.Add(btnRescan);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(100, 30);
			btnClose.Location = new Point(bottomPanel.Width - 115, 9);
			btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClose.Click += delegate { this.Close(); };
			bottomPanel.Controls.Add(btnClose);

			// 5. Главный SplitContainer (ListView сверху, вкладки снизу)
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 440;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.View = View.Details;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Мод / JAR файл", 240);
			listView.Columns.Add("Mod ID", 140);
			listView.Columns.Add("Категория", 180);
			listView.Columns.Add("Вердикт", 140);
			listView.Columns.Add("Риск", 70);
			listView.Columns.Add("Mixin конфигурация", 180);
			listView.Columns.Add("Target классы", 200);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Обфускация", 110);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ Mixin & Manifest");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ fabric.mod.json, mcmod.info и сырые данные");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.01/5.08, «Атлантис» и Команда бана");
			txtBanVerdict = new DarkTextBoxMultiline();
			txtBanVerdict.Dock = DockStyle.Fill;
			txtBanVerdict.ReadOnly = true;
			tabBan.Controls.Add(txtBanVerdict);
			detailTabs.TabPages.Add(tabBan);

			split.Panel2.Controls.Add(detailTabs);
		}

		private Panel CreateKpiBadge(string title, string value, Color accent, ref int xOffset, out Label valueLabel)
		{
			Panel p = new Panel();
			p.Size = new Size(185, 52);
			p.Location = new Point(xOffset, 6);
			p.BackColor = Ui.Surface;
			p.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f, true);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.Location = new Point(8, 6);
			lblTitle.AutoSize = true;
			p.Controls.Add(lblTitle);

			valueLabel = new Label();
			valueLabel.Text = value;
			valueLabel.Font = Ui.FontTitle(13f, true);
			valueLabel.ForeColor = accent;
			valueLabel.Location = new Point(8, 24);
			valueLabel.AutoSize = true;
			p.Controls.Add(valueLabel);

			xOffset += 192;
			return p;
		}

		private DarkButton CreateFilterButton(string text, MixinModFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(140, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (MixinModFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 145;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование модов и Mixin дескрипторов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformMixinAndModJsonAudit();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка аудита Mixin: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<MixinModEvidenceItem> results = e.Result as List<MixinModEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (MixinModEvidenceItem it in allItems)
				{
					if (it.IsViolation) violCount++;
				}

				if (violCount > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ НАРУШЕНИЯ ({0})", violCount);
					lblStatusPill.ForeColor = Ui.AccentRed;
					lblStatusPill.BackColor = Color.FromArgb(50, 20, 20);
				}
				else
				{
					lblStatusPill.Text = "🟢 МОДЫ СООТВЕТСТВУЮТ РЕГЛАМЕНТУ";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int viol = 0;
			int mixins = 0;
			int disguised = 0;
			int cpvp = 0;
			int clean = 0;

			foreach (MixinModEvidenceItem it in allItems)
			{
				if (it.Category.Contains("Чит") && it.IsViolation) viol++;
				if (it.Category.Contains("Mixin") && it.IsViolation) mixins++;
				if (it.Category.Contains("Маскировка")) disguised++;
				if (it.Category.Contains("CPVP") || it.Verdict.Contains("Атлантис")) cpvp++;
				if (!it.IsViolation) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = viol.ToString();
			lblKpiMixins.Text = mixins.ToString();
			lblKpiDisguised.Text = disguised.ToString();
			lblKpiCpvp.Text = cpvp.ToString();
			lblKpiClean.Text = clean.ToString();

			int allViolations = viol + mixins + disguised;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения ({0})", allViolations);
			btnFilterMixins.Text = string.Format("Mixins ({0})", mixins);
			btnFilterCpvp.Text = string.Format("CPVP Атлантис ({0})", cpvp);
			btnFilterDisguised.Text = string.Format("Маскировка ({0})", disguised);
			btnFilterClean.Text = string.Format("Чистые ({0})", clean);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (MixinModEvidenceItem it in allItems)
			{
				bool matchFilter = false;
				switch (currentFilter)
				{
					case MixinModFilter.All:
						matchFilter = true;
						break;
					case MixinModFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case MixinModFilter.ProhibitedMixins:
						matchFilter = it.Category.Contains("Mixin");
						break;
					case MixinModFilter.CpvpWhitelisted:
						matchFilter = it.Category.Contains("CPVP") || it.Verdict.Contains("Атлантис");
						break;
					case MixinModFilter.DisguisedMods:
						matchFilter = it.Category.Contains("Маскировка");
						break;
					case MixinModFilter.CleanMods:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string haystack = (it.ModNameOrFile + " " + it.ModId + " " + it.Category + " " + it.TargetClasses + " " + it.DetailedForensics).ToLowerInvariant();
					if (!haystack.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ModNameOrFile);
				lvi.SubItems.Add(it.ModId);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.RiskScore.ToString() + "%");
				lvi.SubItems.Add(it.MixinConfigs);
				lvi.SubItems.Add(it.TargetClasses);
				lvi.SubItems.Add(it.RuleArticle);
				lvi.SubItems.Add(it.ObfuscationScore.ToString() + "%" + (!string.IsNullOrEmpty(it.Obfuscator) && it.Obfuscator != "Отсутствует" ? " (" + it.Obfuscator + ")" : ""));

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
				ClearDetailTabs();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetailTabs();
				return;
			}

			MixinModEvidenceItem it = listView.SelectedItems[0].Tag as MixinModEvidenceItem;
			if (it == null) return;

			txtAnalysis.Text = it.DetailedForensics;
			txtTechnical.Text = it.TechnicalAnalysis;
			txtBanVerdict.Text = it.BanVerdictGuide;
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в таблице для криминалистического анализа Mixin и дескрипторов мода.";
			txtTechnical.Text = "Нет данных технического анализа.";
			txtBanVerdict.Text = "Нет выбранного вердикта.";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Сначала выберите нарушение в списке!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			MixinModEvidenceItem it = listView.SelectedItems[0].Tag as MixinModEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = (it.RuleArticle == "5.08")
				? "5.08 Маскировка чит-мода (" + it.ModNameOrFile + ")"
				: "5.01 Использование запрещенных Mixin/чит-модов (" + it.ModNameOrFile + ")";

			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**🧬 MCCheatChecker — Отчёт инспектора Mixin & mod.json архивов модов (Tool 84)**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine("**Профиль сервера:** `" + ServerRulesProfileManager.CurrentProfileDisplayName + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (MixinModEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные запрещенные моды и сомнительные Mixins:**");
				foreach (MixinModEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.RuleArticle + "]` **" + it.ModNameOrFile + "** (`" + it.ModId + "`): " + it.Category + " — Риск: `" + it.RiskScore + "%`");
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.01 Запрещенные моды / Mixin инъекции`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Все моды соответствуют регламенту и белому списку CPVP «Атлантис»)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_MixinReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт анализа Mixin и модов — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #121418; color: #E0E0E0; margin: 20px; }");
					sb.AppendLine("h1 { color: #AB47BC; border-bottom: 2px solid #333; padding-bottom: 8px; }");
					sb.AppendLine(".kpi-container { display: flex; gap: 15px; margin: 20px 0; flex-wrap: wrap; }");
					sb.AppendLine(".kpi { background: #1E222B; border: 1px solid #2C3240; padding: 12px 18px; border-radius: 6px; }");
					sb.AppendLine(".kpi-title { font-size: 11px; color: #8C9BAE; font-weight: bold; }");
					sb.AppendLine(".kpi-value { font-size: 20px; font-weight: bold; margin-top: 4px; }");
					sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; background: #1E222B; }");
					sb.AppendLine("th, td { border: 1px solid #2C3240; padding: 8px 12px; text-align: left; }");
					sb.AppendLine("th { background: #262C38; color: #8C9BAE; }");
					sb.AppendLine(".crit { color: #FF5252; font-weight: bold; }");
					sb.AppendLine(".warn { color: #FFA726; font-weight: bold; }");
					sb.AppendLine(".safe { color: #4CAF50; }");
					sb.AppendLine("</style></head><body>");
					sb.AppendLine("<h1>🧬 Отчёт инспектора Mixin & mod.json архивов модов (Tool 84)</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Профиль: <b>" + ServerRulesProfileManager.CurrentProfileDisplayName + "</b> | Время: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО МОДОВ</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>НАРУШЕНИЯ 5.01</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiViolations.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>MIXIN ТАРГЕТЫ</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiMixins.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>МАСКИРОВКА</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiDisguised.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>CPVP АТЛАНТИС</div><div class='kpi-value' style='color:#26A69A'>" + lblKpiCpvp.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ЧИСТЫЕ МОДЫ</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiClean.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Мод / Файл</th><th>Mod ID</th><th>Категория</th><th>Вердикт</th><th>Риск</th><th>Mixin конфиг</th><th>Target классы</th><th>Правило</th></tr>");

					foreach (MixinModEvidenceItem it in allItems)
					{
						string css = it.IsViolation ? "crit" : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td><b>" + it.ModNameOrFile + "</b></td>");
						sb.AppendLine("<td>" + it.ModId + "</td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td class='" + css + "'>" + it.Verdict + "</td>");
						sb.AppendLine("<td>" + it.RiskScore + "%</td>");
						sb.AppendLine("<td><code>" + it.MixinConfigs + "</code></td>");
						sb.AppendLine("<td><code>" + it.TargetClasses + "</code></td>");
						sb.AppendLine("<td>" + it.RuleArticle + "</td>");
						sb.AppendLine("</tr>");
					}

					sb.AppendLine("</table></body></html>");

					try
					{
						File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
						MessageBox.Show("HTML отчёт успешно сохранён!", "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка сохранения отчёта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		#region Криминалистическое сканирование (Forensic Scan Engine)

		public static List<MixinModEvidenceItem> PerformMixinAndModJsonAudit()
		{
			List<MixinModEvidenceItem> list = new List<MixinModEvidenceItem>();
			int idx = 1;

			List<string> candidateDirs = new List<string>();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			// Стандартные пути
			candidateDirs.Add(Path.Combine(appData, @".minecraft\mods"));
			candidateDirs.Add(Path.Combine(appData, @".minecraft\versions"));

			// Сторонние лаунчеры
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			candidateDirs.Add(Path.Combine(appData, @"PrismLauncher\instances"));
			candidateDirs.Add(Path.Combine(appData, @"ModrinthApp\profiles"));
			candidateDirs.Add(Path.Combine(userProfile, @"curseforge\minecraft\Instances"));
			candidateDirs.Add(Path.Combine(userProfile, @".feather\user-mods"));

			List<string> jarFiles = new List<string>();
			foreach (string dir in candidateDirs)
			{
				try
				{
					if (Directory.Exists(dir))
					{
						string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
						foreach (string f in files)
						{
							string ext = Path.GetExtension(f).ToLowerInvariant();
							if (ext == ".jar" || ext == ".zip" || ext == ".disabled" || ext == ".tmp" || ext == ".bin" || ext == ".dat")
							{
								jarFiles.Add(f);
								if (jarFiles.Count >= 200) break;
							}
						}
					}
				}
				catch { }
			}

			// Анализ каждого найденного архива
			foreach (string jarPath in jarFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(jarPath);
					string fileName = fi.Name;
					string ext = fi.Extension.ToLowerInvariant();

					// Проверка на ZIP magic bytes (PK 0x03 0x04)
					byte[] header = new byte[4];
					using (FileStream fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						if (fs.Length < 4) continue;
						fs.Read(header, 0, 4);
					}

					bool isZip = (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04);
					if (!isZip) continue;

					// Если расширение не .jar, но это ZIP архив с классами -> Маскировка (5.08)
					bool isDisguised = (ext != ".jar" && ext != ".disabled");

					string modId = "N/A";
					string modName = fileName;
					List<string> mixinConfigs = new List<string>();
					List<string> targetClasses = new List<string>();
					string fabricJsonRaw = "";

					using (FileStream zipStream = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
					{
						// 1. Поиск fabric.mod.json
						ZipArchiveEntry fabricEntry = archive.GetEntry("fabric.mod.json");
						if (fabricEntry != null)
						{
							using (StreamReader sr = new StreamReader(fabricEntry.Open(), Encoding.UTF8))
							{
								fabricJsonRaw = sr.ReadToEnd();
								Match idMatch = Regex.Match(fabricJsonRaw, "\"id\"\\s*:\\s*\"([^\"]+)\"");
								if (idMatch.Success) modId = idMatch.Groups[1].Value;

								Match nameMatch = Regex.Match(fabricJsonRaw, "\"name\"\\s*:\\s*\"([^\"]+)\"");
								if (nameMatch.Success) modName = nameMatch.Groups[1].Value;

								// Поиск mixins в fabric.mod.json
								MatchCollection mixinMatches = Regex.Matches(fabricJsonRaw, "\"([^\"]+\\.mixins?\\.json)\"");
								foreach (Match m in mixinMatches)
								{
									mixinConfigs.Add(m.Groups[1].Value);
								}
							}
						}

						// 2. Поиск mcmod.info (Forge)
						if (modId == "N/A")
						{
							ZipArchiveEntry mcmodEntry = archive.GetEntry("mcmod.info");
							if (mcmodEntry != null)
							{
								using (StreamReader sr = new StreamReader(mcmodEntry.Open(), Encoding.UTF8))
								{
									string mcmodRaw = sr.ReadToEnd();
									Match idMatch = Regex.Match(mcmodRaw, "\"modid\"\\s*:\\s*\"([^\"]+)\"");
									if (idMatch.Success) modId = idMatch.Groups[1].Value;
								}
							}
						}

						// 3. Поиск всех *.mixins.json в архиве
						foreach (ZipArchiveEntry entry in archive.Entries)
						{
							string eName = entry.Name.ToLowerInvariant();
							if (eName.EndsWith(".mixins.json") || eName.EndsWith(".mixin.json"))
							{
								if (!mixinConfigs.Contains(entry.Name)) mixinConfigs.Add(entry.Name);

								try
								{
									using (StreamReader sr = new StreamReader(entry.Open(), Encoding.UTF8))
									{
										string mixinJson = sr.ReadToEnd();
										// Извлекаем имена классов mixin
										MatchCollection classMatches = Regex.Matches(mixinJson, "\"([^\"]+Mixin[^\"]*)\"");
										foreach (Match cm in classMatches)
										{
											string cName = cm.Groups[1].Value;
											// По именам классов выявляем target-классы
											string lowerCName = cName.ToLowerInvariant();
											if (lowerCName.Contains("player") || lowerCName.Contains("clientplayer")) targetClasses.Add("ClientPlayerEntity");
											if (lowerCName.Contains("render") || lowerCName.Contains("entityrender")) targetClasses.Add("EntityRenderer");
											if (lowerCName.Contains("living")) targetClasses.Add("LivingEntityRenderer");
											if (lowerCName.Contains("client") || lowerCName.Contains("minecraftclient")) targetClasses.Add("MinecraftClient");
											if (lowerCName.Contains("world") || lowerCName.Contains("worldrender")) targetClasses.Add("WorldRenderer");
											if (lowerCName.Contains("light") || lowerCName.Contains("lightmap")) targetClasses.Add("LightmapTextureManager");
											if (lowerCName.Contains("hud") || lowerCName.Contains("ingamehud")) targetClasses.Add("InGameHud");
											if (lowerCName.Contains("camera")) targetClasses.Add("Camera");
										}
									}
								}
								catch { }
							}
						}
					}

					// Оценка через профиль правил (Атлантис / Стандарт)
					bool isViolation = false;
					string ruleArticle = "ЧИСТО";
					int riskScore = 0;
					string verdict = ServerRulesProfileManager.EvaluateModVerdict(modId, fileName, targetClasses, out isViolation, out ruleArticle, out riskScore);

					// Криминалистический анализ обфускации JAR-архива
					JarObfuscationResult obf = JarObfuscationAnalyzer.AnalyzeJar(jarPath);
					if (obf != null && obf.IsObfuscated && !verdict.Contains("Атлантис") && !isViolation)
					{
						if (obf.RiskLevel == "CRITICAL")
						{
							isViolation = true;
							ruleArticle = "5.01";
							verdict = "🚨 КРИТИЧЕСКАЯ ОБФУСКАЦИЯ ПАКЕРОМ (" + (obf.DetectedObfuscators.Count > 0 ? string.Join(", ", obf.DetectedObfuscators.ToArray()) : "Односимвольные классы") + ", " + obf.ObfuscationPercentage + "%) [5.01]";
							riskScore = Math.Max(riskScore, 95);
						}
						else if (obf.RiskLevel == "SUSPICIOUS")
						{
							riskScore = Math.Max(riskScore, 65);
						}
					}

					// Проверка маскировки расширения
					if (isDisguised)
					{
						isViolation = true;
						ruleArticle = "5.08";
						riskScore = 100;
						verdict = "🚨 МАСКИРОВКА МОДА ПОД ДРУГОЙ ФОРМАТ (Регламент 5.08)";
					}

					MixinModEvidenceItem item = new MixinModEvidenceItem();
					item.Index = idx++;
					item.ModNameOrFile = modName;
					item.ModId = modId;
					item.Category = isDisguised
						? "Маскировка JAR файла (5.08)"
						: (isViolation ? "Запрещенный чит-мод / Mixin (5.01)" : (verdict.Contains("Атлантис") ? "CPVP Белый список «Атлантис»" : "Легитимный мод"));
					item.Verdict = verdict;
					item.Severity = isViolation ? "CRITICAL" : (verdict.Contains("Атлантис") ? "SAFE" : "INFO");
					item.RiskScore = riskScore;
					item.JarPath = jarPath;
					item.MixinConfigs = (mixinConfigs.Count > 0) ? string.Join(", ", mixinConfigs.ToArray()) : "Нет";
					item.TargetClasses = (targetClasses.Count > 0) ? string.Join(", ", targetClasses.ToArray()) : "Штатные";
					item.RuleArticle = ruleArticle;
					item.IsViolation = isViolation;
					item.Timestamp = fi.LastWriteTime;
					item.RowColor = isViolation ? Ui.AccentRed : (verdict.Contains("Атлантис") ? Ui.AccentTeal : Ui.AccentGreen);
					item.ObfuscationScore = obf != null ? obf.ObfuscationPercentage : 0;
					item.Obfuscator = (obf != null && obf.DetectedObfuscators.Count > 0)
						? string.Join(", ", obf.DetectedObfuscators.ToArray())
						: ((obf != null && obf.ObfuscationPercentage >= 30) ? "Безымянный / ProGuard" : "Отсутствует");
					item.ObfuscationSummary = obf != null ? obf.Summary : "Чистый";

					item.DetailedForensics = "КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ МОДА:\n\n" +
						"Имя мода: " + modName + "\n" +
						"Mod ID: " + modId + "\n" +
						"Файл архива: " + jarPath + "\n" +
						"Размер: " + fi.Length + " байт\n" +
						"Последнее изменение: " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + "\n" +
						"Mixin конфиги: " + item.MixinConfigs + "\n" +
						"Обнаруженные Target-классы: " + item.TargetClasses + "\n" +
						"Обфускация байткода: " + item.ObfuscationScore + "% (Пакер: " + item.Obfuscator + ")\n" +
						"Сводка обфускатора: " + item.ObfuscationSummary + "\n" +
						(obf != null && obf.SampleObfuscatedClasses.Count > 0 ? "Примеры обфусцированных классов: " + string.Join(", ", obf.SampleObfuscatedClasses.ToArray()) + "\n" : "") + "\n" +
						"Судебный вердикт:\n" + verdict + "\n\n" +
						(isDisguised
							? "ВНИМАНИЕ: Файл имеет расширение " + ext + ", но является полноценным ZIP/JAR архивом мода. Это умышленная маскировка софта (п. 5.08)!"
							: (isViolation
								? "Обнаружен запрещенный чит-мод или внедрение Mixin в критические игровые классы (движение, рендер, сущности, HUD). Нарушение п. 5.01."
								: "Мод авторизован или соответствует белому списку CPVP оптимизаторов сервера «Атлантис»."));

					item.TechnicalAnalysis = "JAR: " + jarPath + "\nfabric.mod.json raw:\n" +
						(string.IsNullOrEmpty(fabricJsonRaw) ? "Отсутствует" : (fabricJsonRaw.Length > 200 ? fabricJsonRaw.Substring(0, 200) + "..." : fabricJsonRaw));

					item.BanVerdictGuide = isViolation
						? "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт: " + ruleArticle + " (" + (ruleArticle == "5.08" ? "Маскировка мода" : "Запрещенный чит-мод / Mixin") + ")\nСрок: 30 дней\n\nКоманда:\n/ban <player> 30d " + ruleArticle + " " + item.ModNameOrFile
						: "Мод легитимен. Проверен по базе «Атлантис» 5.01-5.08.";

					list.Add(item);
				}
				catch { }
			}

			// Если ничего не найдено, добавляем пустую чистую запись
			if (list.Count == 0)
			{
				MixinModEvidenceItem clean = new MixinModEvidenceItem();
				clean.Index = idx++;
				clean.ModNameOrFile = "Папка mods пуста / чиста";
				clean.ModId = "none";
				clean.Category = "Чистые моды";
				clean.Verdict = "🟢 БЕЗОПАСНО";
				clean.Severity = "SAFE";
				clean.RiskScore = 0;
				clean.JarPath = @"%AppData%\.minecraft\mods";
				clean.MixinConfigs = "Нет";
				clean.TargetClasses = "Нет";
				clean.RuleArticle = "ЧИСТО";
				clean.IsViolation = false;
				clean.Timestamp = DateTime.Now;
				clean.RowColor = Ui.AccentGreen;
				clean.DetailedForensics = "В каталогах mods и versions подозрительных модов, маскировок и Mixin инъекций не обнаружено.";
				clean.TechnicalAnalysis = "No suspicious mods found.";
				clean.BanVerdictGuide = "Нарушений нет.";
				list.Add(clean);
			}

			for (int i = 0; i < list.Count; i++)
			{
				list[i].Index = i + 1;
			}

			return list;
		}

		#endregion
	}
}
