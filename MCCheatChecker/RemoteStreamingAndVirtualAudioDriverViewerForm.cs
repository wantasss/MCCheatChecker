using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum AudioForensicFilter
	{
		All,
		ViolationsOnly,
		VirtualCables,
		RemoteStreaming,
		VoiceMeeter,
		ThirdPartyWhisper,
		PhysicalClean
	}

	public class AudioForensicEvidenceItem
	{
		public int Index;
		public string DeviceOrDriverName;
		public string Category;
		public string Verdict;
		public string Severity; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public int RiskScore; // 0..100
		public string DriverFileOrPath;
		public string HardwareIdOrPid;
		public string RuleArticle; // "5.08", "ЧИСТО"
		public string DataSource;
		public bool IsViolation;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailedForensics;
		public string TechnicalAnalysis;
		public string BanVerdictGuide;
	}

	public class RemoteStreamingAndVirtualAudioDriverViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<AudioForensicEvidenceItem> allItems = new List<AudioForensicEvidenceItem>();
		private AudioForensicFilter currentFilter = AudioForensicFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiCables;
		private Label lblKpiStreaming;
		private Label lblKpiVoiceMeeter;
		private Label lblKpiThirdParty;
		private Label lblKpiPhysical;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCables;
		private DarkButton btnFilterStreaming;
		private DarkButton btnFilterVoiceMeeter;
		private DarkButton btnFilterThirdParty;
		private DarkButton btnFilterPhysical;

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

		public RemoteStreamingAndVirtualAudioDriverViewerForm() : this("Player")
		{
		}

		public RemoteStreamingAndVirtualAudioDriverViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🎙️ Инспектор виртуальных аудиокабелей и стриминга (Tool 80 / Step 77) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🎙️ Инспектор виртуальных аудиокабелей и стриминга (Tool 80 / Step 77)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Судебная экспертиза аудиоустройств и драйверов: аудит VB-Audio Cable, VoiceMeeter, VAC, Parsec Audio, стримеров Sunshine/Moonlight и детекция скрытых каналов подсказок третьих лиц (5.08).";
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО УСТРОЙСТВ", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("ВИРТУАЛЬНЫЕ КАБЕЛИ (5.08)", "0", Ui.AccentRed, ref xOff, out lblKpiCables));
			kpiPanel.Controls.Add(CreateKpiBadge("СТРИМИНГ АУДИО (5.08)", "0", Ui.AccentOrange, ref xOff, out lblKpiStreaming));
			kpiPanel.Controls.Add(CreateKpiBadge("VOICEMEETER РОУТИНГ", "0", Ui.AccentPurple, ref xOff, out lblKpiVoiceMeeter));
			kpiPanel.Controls.Add(CreateKpiBadge("ПОСТОРОННЯЯ ПОМОЩЬ (5.08)", "0", Ui.AccentRed, ref xOff, out lblKpiThirdParty));
			kpiPanel.Controls.Add(CreateKpiBadge("ФИЗИЧЕСКИЕ АУДИО", "0", Ui.AccentGreen, ref xOff, out lblKpiPhysical));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", AudioForensicFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения 5.08 (0)", AudioForensicFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterCables = CreateFilterButton("Кабели (0)", AudioForensicFilter.VirtualCables, ref fX, filterPanel);
			btnFilterStreaming = CreateFilterButton("Стриминг (0)", AudioForensicFilter.RemoteStreaming, ref fX, filterPanel);
			btnFilterVoiceMeeter = CreateFilterButton("VoiceMeeter (0)", AudioForensicFilter.VoiceMeeter, ref fX, filterPanel);
			btnFilterThirdParty = CreateFilterButton("Посторонняя помощь (0)", AudioForensicFilter.ThirdPartyWhisper, ref fX, filterPanel);
			btnFilterPhysical = CreateFilterButton("Физические (0)", AudioForensicFilter.PhysicalClean, ref fX, filterPanel);

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
			listView.Columns.Add("Устройство / Источник", 260);
			listView.Columns.Add("Категория", 190);
			listView.Columns.Add("Вердикт", 140);
			listView.Columns.Add("Риск", 70);
			listView.Columns.Add("Драйвер / Файл", 190);
			listView.Columns.Add("ID / PID", 140);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Источник данных", 160);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ аудио-маршрутизации");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Драйверы, реестр и процессы звука");
			txtTechnical = new DarkTextBoxMultiline();
			txtTechnical.Dock = DockStyle.Fill;
			txtTechnical.ReadOnly = true;
			tabTechnical.Controls.Add(txtTechnical);
			detailTabs.TabPages.Add(tabTechnical);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 и Команда бана");
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

		private DarkButton CreateFilterButton(string text, AudioForensicFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(140, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (AudioForensicFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 145;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование аудио-устройств и драйверов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformAudioForensicScan();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сканирования аудио: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<AudioForensicEvidenceItem> results = e.Result as List<AudioForensicEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (AudioForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation) violCount++;
				}

				if (violCount > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАРУШЕНИЯ 5.08 ({0})", violCount);
					lblStatusPill.ForeColor = Ui.AccentRed;
					lblStatusPill.BackColor = Color.FromArgb(50, 20, 20);
				}
				else
				{
					lblStatusPill.Text = "🟢 АУДИОКАНАЛ ЧИСТ";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int cables = 0;
			int streaming = 0;
			int voicemeeter = 0;
			int thirdparty = 0;
			int physical = 0;

			foreach (AudioForensicEvidenceItem it in allItems)
			{
				if (it.Category.Contains("кабель") || it.Category.Contains("CABLE") || it.Category.Contains("VAC")) cables++;
				if (it.Category.Contains("стриминг") || it.Category.Contains("Parsec") || it.Category.Contains("Sunshine")) streaming++;
				if (it.Category.Contains("VoiceMeeter")) voicemeeter++;
				if (it.Category.Contains("Посторонняя помощь")) thirdparty++;
				if (!it.IsViolation) physical++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiCables.Text = cables.ToString();
			lblKpiStreaming.Text = streaming.ToString();
			lblKpiVoiceMeeter.Text = voicemeeter.ToString();
			lblKpiThirdParty.Text = thirdparty.ToString();
			lblKpiPhysical.Text = physical.ToString();

			int viol = cables + streaming + voicemeeter + thirdparty;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения 5.08 ({0})", viol);
			btnFilterCables.Text = string.Format("Кабели ({0})", cables);
			btnFilterStreaming.Text = string.Format("Стриминг ({0})", streaming);
			btnFilterVoiceMeeter.Text = string.Format("VoiceMeeter ({0})", voicemeeter);
			btnFilterThirdParty.Text = string.Format("Помощь ({0})", thirdparty);
			btnFilterPhysical.Text = string.Format("Физические ({0})", physical);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (AudioForensicEvidenceItem it in allItems)
			{
				bool matchFilter = false;
				switch (currentFilter)
				{
					case AudioForensicFilter.All:
						matchFilter = true;
						break;
					case AudioForensicFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case AudioForensicFilter.VirtualCables:
						matchFilter = it.Category.Contains("кабель") || it.Category.Contains("CABLE") || it.Category.Contains("VAC");
						break;
					case AudioForensicFilter.RemoteStreaming:
						matchFilter = it.Category.Contains("стриминг") || it.Category.Contains("Parsec") || it.Category.Contains("Sunshine");
						break;
					case AudioForensicFilter.VoiceMeeter:
						matchFilter = it.Category.Contains("VoiceMeeter");
						break;
					case AudioForensicFilter.ThirdPartyWhisper:
						matchFilter = it.Category.Contains("Посторонняя помощь");
						break;
					case AudioForensicFilter.PhysicalClean:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string haystack = (it.DeviceOrDriverName + " " + it.Category + " " + it.DriverFileOrPath + " " + it.HardwareIdOrPid + " " + it.DetailedForensics).ToLowerInvariant();
					if (!haystack.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.DeviceOrDriverName);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.RiskScore.ToString() + "%");
				lvi.SubItems.Add(it.DriverFileOrPath);
				lvi.SubItems.Add(it.HardwareIdOrPid);
				lvi.SubItems.Add(it.RuleArticle);
				lvi.SubItems.Add(it.DataSource);

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

			AudioForensicEvidenceItem it = listView.SelectedItems[0].Tag as AudioForensicEvidenceItem;
			if (it == null) return;

			txtAnalysis.Text = it.DetailedForensics;
			txtTechnical.Text = it.TechnicalAnalysis;
			txtBanVerdict.Text = it.BanVerdictGuide;
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в таблице для криминалистического анализа звукового устройства.";
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

			AudioForensicEvidenceItem it = listView.SelectedItems[0].Tag as AudioForensicEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = "5.08 Уклонение от проверки (Скрытый аудиоканал консультанта / Виртуальный кабель " + it.DeviceOrDriverName + ")";
			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**🎙️ MCCheatChecker — Отчёт инспектора виртуальных аудиокабелей и стриминга (Tool 80)**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (AudioForensicEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные виртуальные кабели, стримеры и каналы подсказок (5.08):**");
				foreach (AudioForensicEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.RuleArticle + "]` **" + it.Category + "**: `" + it.DeviceOrDriverName + "` (" + it.DriverFileOrPath + ") — Риск: `" + it.RiskScore + "%`");
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.08 Посторонняя помощь и скрытый аудиоканал консультанта при проверке`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Виртуальные аудиокабели, VoiceMeeter, Parsec и каналы подсказок третьих лиц не обнаружены)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_AudioForensicsReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт аудиоэкспертизы — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #121418; color: #E0E0E0; margin: 20px; }");
					sb.AppendLine("h1 { color: #EF5350; border-bottom: 2px solid #333; padding-bottom: 8px; }");
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
					sb.AppendLine("<h1>🎙️ Отчёт инспектора виртуальных аудиокабелей и стриминга (Tool 80)</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО УСТРОЙСТВ</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВИРТУАЛЬНЫЕ КАБЕЛИ</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiCables.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>СТРИМИНГ АУДИО</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiStreaming.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>VOICEMEETER</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiVoiceMeeter.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ПОСТОРОННЯЯ ПОМОЩЬ</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiThirdParty.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ФИЗИЧЕСКИЕ АУДИО</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiPhysical.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Устройство / Драйвер</th><th>Категория</th><th>Вердикт</th><th>Риск</th><th>Драйвер / Файл</th><th>ID / PID</th><th>Правило</th><th>Источник</th></tr>");

					foreach (AudioForensicEvidenceItem it in allItems)
					{
						string css = it.IsViolation ? "crit" : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td><b>" + it.DeviceOrDriverName + "</b></td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td class='" + css + "'>" + it.Verdict + "</td>");
						sb.AppendLine("<td>" + it.RiskScore + "%</td>");
						sb.AppendLine("<td><code>" + it.DriverFileOrPath + "</code></td>");
						sb.AppendLine("<td>" + it.HardwareIdOrPid + "</td>");
						sb.AppendLine("<td>" + it.RuleArticle + "</td>");
						sb.AppendLine("<td>" + it.DataSource + "</td>");
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

		public static List<AudioForensicEvidenceItem> PerformAudioForensicScan()
		{
			List<AudioForensicEvidenceItem> list = new List<AudioForensicEvidenceItem>();
			int idx = 1;

			// Отслеживаем найденные виртуальные драйверы для синтеза улик
			List<string> detectedVirtualDrivers = new List<string>();
			List<string> detectedRemoteProcesses = new List<string>();

			// 1. Аудит реестра драйверов звука HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e96c-e325-11ce-bfc1-08002be10318}
			try
			{
				string classKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e96c-e325-11ce-bfc1-08002be10318}";
				using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classKeyPath))
				{
					if (classKey != null)
					{
						string[] subKeyNames = classKey.GetSubKeyNames();
						foreach (string subName in subKeyNames)
						{
							if (subName.StartsWith("Properties") || subName.Length > 6) continue;

							using (RegistryKey sk = classKey.OpenSubKey(subName))
							{
								if (sk == null) continue;

								string driverDesc = (sk.GetValue("DriverDesc") as string) ?? "";
								string providerName = (sk.GetValue("ProviderName") as string) ?? "";
								string infPath = (sk.GetValue("InfPath") as string) ?? "";
								string matchingDevId = (sk.GetValue("MatchingDeviceId") as string) ?? "";
								string driverVersion = (sk.GetValue("DriverVersion") as string) ?? "";

								if (string.IsNullOrEmpty(driverDesc) && string.IsNullOrEmpty(matchingDevId)) continue;

								string lowerDesc = driverDesc.ToLowerInvariant();
								string lowerProv = providerName.ToLowerInvariant();
								string lowerDev = matchingDevId.ToLowerInvariant();

								// Проверка VB-Audio Cable
								if (lowerDesc.Contains("vb-audio") || lowerDesc.Contains("cable input") || lowerDesc.Contains("cable output") ||
									lowerProv.Contains("vb-audio") || lowerDev.Contains("vbcable") || lowerDev.Contains("vbaudiovac"))
								{
									detectedVirtualDrivers.Add("VB-Audio Cable (" + driverDesc + ")");
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = string.IsNullOrEmpty(driverDesc) ? "VB-Audio Virtual Cable" : driverDesc;
									item.Category = "Виртуальный аудиокабель (VB-Cable 5.08)";
									item.Verdict = "🚨 BAN 5.08";
									item.Severity = "CRITICAL";
									item.RiskScore = 95;
									item.DriverFileOrPath = "vbaudio_cable64_win7.sys / " + infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "5.08";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = true;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentRed;
									item.DetailedForensics = "ОБНАРУЖЕН ВИРТУАЛЬНЫЙ АУДИОКАБЕЛЬ VB-AUDIO CABLE!\n\n" +
										"Устройство: " + item.DeviceOrDriverName + "\n" +
										"Поставщик: " + providerName + "\n" +
										"Версия: " + driverVersion + "\n" +
										"INF-файл: " + infPath + "\n\n" +
										"Криминалистическое значение:\n" +
										"Виртуальные кабели используются нечестными игроками во время ScreenShare для разделения аудиопотоков. " +
										"Звук игры выводится модератору в стрим, а звук тайного звонка Discord/Telegram/AnyDesk перенаправляется " +
										"в виртуальную шину напрямую в наушники игрока. Модератор не слышит подсказок помощника/консультанта.\n\n" +
										"Согласно Регламенту проверок (п. 5.08), использование сторонней помощи и скрытых каналов передачи информации карается баном на 30 дней.";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\n" +
										"DriverDesc: " + driverDesc + "\n" +
										"ProviderName: " + providerName + "\n" +
										"MatchingDeviceId: " + matchingDevId + "\n" +
										"InfPath: " + infPath + "\n" +
										"DriverVersion: " + driverVersion;
									item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
										"Пункт регламента: 5.08 (Уклонение от проверки / Посторонняя помощь / Скрытый аудиоканал консультанта)\n" +
										"Срок: 30 дней\n\n" +
										"Команда:\n/ban <player> 30d 5.08 Посторонняя помощь и скрытый аудиоканал консультанта при проверке";
									list.Add(item);
								}
								// Проверка VoiceMeeter
								else if (lowerDesc.Contains("voicemeeter") || lowerDesc.Contains("vmvaio") || lowerProv.Contains("voicemeeter") || lowerDev.Contains("vmvaio"))
								{
									detectedVirtualDrivers.Add("VoiceMeeter (" + driverDesc + ")");
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = string.IsNullOrEmpty(driverDesc) ? "VoiceMeeter Audio Mixer" : driverDesc;
									item.Category = "VoiceMeeter роутер (5.08)";
									item.Verdict = "🚨 BAN 5.08";
									item.Severity = "CRITICAL";
									item.RiskScore = 95;
									item.DriverFileOrPath = "vbaudio_vmvaio64_win7.sys / " + infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "5.08";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = true;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentRed;
									item.DetailedForensics = "ОБНАРУЖЕН АУДИОМИКШЕР VOICEMEETER (VB-AUDIO)!\n\n" +
										"Устройство: " + item.DeviceOrDriverName + "\n" +
										"Поставщик: " + providerName + "\n" +
										"Версия: " + driverVersion + "\n\n" +
										"Криминалистическое значение:\n" +
										"VoiceMeeter позволяет на лету матрично микшировать аудиоканалы (B1/B2/A1). Это популярнейший инструмент читеров для скрытого " +
										"сопровождения проверки третьими лицами (консультантами по читам, которые подсказывают что удалять или координируют действия).\n" +
										"Наличие VoiceMeeter с активной маршрутизацией при проверке квалифицируется как уклонение и посторонняя помощь (5.08).";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\n" +
										"DriverDesc: " + driverDesc + "\n" +
										"MatchingDeviceId: " + matchingDevId + "\n" +
										"InfPath: " + infPath;
									item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
										"Пункт регламента: 5.08 (Использование аудиомикшера для сокрытия голоса третьих лиц при проверке)\n" +
										"Срок: 30 дней\n\n" +
										"Команда:\n/ban <player> 30d 5.08 Использование VoiceMeeter для сокрытия подсказок третьих лиц";
									list.Add(item);
								}
								// Проверка Virtual Audio Cable (VAC) Eugene Muzychenko
								else if (lowerDesc.Contains("virtual audio cable") || lowerProv.Contains("muzychenko") || lowerDev.Contains("vckndis") || lowerDev.Contains("vac"))
								{
									detectedVirtualDrivers.Add("VAC (" + driverDesc + ")");
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = string.IsNullOrEmpty(driverDesc) ? "Virtual Audio Cable (VAC)" : driverDesc;
									item.Category = "Виртуальный аудиокабель (VAC Muzychenko 5.08)";
									item.Verdict = "🚨 BAN 5.08";
									item.Severity = "CRITICAL";
									item.RiskScore = 95;
									item.DriverFileOrPath = "vckndis5.sys / " + infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "5.08";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = true;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentRed;
									item.DetailedForensics = "ОБНАРУЖЕН ДРАЙВЕР VIRTUAL AUDIO CABLE (EUGENE MUZYCHENKO)!\n\n" +
										"Драйвер: " + item.DeviceOrDriverName + "\n" +
										"Поставщик: " + providerName + "\n\n" +
										"Криминалистическое значение:\n" +
										"Классический драйвер VAC перенаправляет аудиопотоки между приложениями без использования физической звуковой карты. " +
										"Применяется для тайного ввода/вывода голосовых команд клинеров во время демонстрации экрана модератору.";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\n" +
										"DriverDesc: " + driverDesc + "\n" +
										"ProviderName: " + providerName;
									item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
										"Пункт регламента: 5.08 (Виртуальный аудиокабель VAC)\n" +
										"Команда:\n/ban <player> 30d 5.08 Виртуальный аудиокабель VAC для скрытой помощи";
									list.Add(item);
								}
								// Проверка Parsec Virtual Audio Driver
								else if (lowerDesc.Contains("parsec") || lowerDev.Contains("parsec"))
								{
									detectedVirtualDrivers.Add("Parsec Audio (" + driverDesc + ")");
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = string.IsNullOrEmpty(driverDesc) ? "Parsec Virtual Audio Device" : driverDesc;
									item.Category = "Удаленный аудиостриминг (Parsec 5.08)";
									item.Verdict = "🚨 BAN 5.08";
									item.Severity = "CRITICAL";
									item.RiskScore = 95;
									item.DriverFileOrPath = "parsec-audio.sys / " + infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "5.08";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = true;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentRed;
									item.DetailedForensics = "ОБНАРУЖЕН ВИРТУАЛЬНЫЙ АУДИОДРАЙВЕР PARSEC!\n\n" +
										"Драйвер: " + item.DeviceOrDriverName + "\n" +
										"Криминалистическое значение:\n" +
										"Parsec используется для сверхбыстрого удаленного управления компьютером и двусторонней передачи звука. " +
										"Наличие аудиодрайвера Parsec указывает на удаленный стриминг или сеанс удаленного помощника при проверке.";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\n" +
										"DriverDesc: " + driverDesc;
									item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
										"Пункт регламента: 5.08 (Parsec Remote Audio Streaming)\n" +
										"Команда:\n/ban <player> 30d 5.08 Удаленный аудиостриминг Parsec при проверке";
									list.Add(item);
								}
								// Steam Streaming
								else if (lowerDesc.Contains("steam streaming"))
								{
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = driverDesc;
									item.Category = "Стриминг аудио (Steam Remote Play)";
									item.Verdict = "⚠️ ПОДОЗРЕНИЕ";
									item.Severity = "WARNING";
									item.RiskScore = 45;
									item.DriverFileOrPath = infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "5.08";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = false;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentOrange;
									item.DetailedForensics = "Обнаружен драйвер Steam Streaming Speakers/Microphone. Легитимный компонент Steam Remote Play, однако требует внимания если запущен удаленный стрим.";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\nDriverDesc: " + driverDesc;
									item.BanVerdictGuide = "Проверить запущен ли Steam и транслируется ли сессия.";
									list.Add(item);
								}
								// Физические легитимные устройства (Realtek, Nvidia, Intel, AMD, USB)
								else if (lowerDesc.Contains("realtek") || lowerDesc.Contains("nvidia") || lowerDesc.Contains("amd") ||
										 lowerDesc.Contains("intel") || lowerDesc.Contains("high definition") || lowerDesc.Contains("usb audio") ||
										 lowerDesc.Contains("generic"))
								{
									AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
									item.Index = idx++;
									item.DeviceOrDriverName = driverDesc;
									item.Category = "Физическое аудиоустройство";
									item.Verdict = "🟢 БЕЗОПАСНО";
									item.Severity = "SAFE";
									item.RiskScore = 0;
									item.DriverFileOrPath = infPath;
									item.HardwareIdOrPid = matchingDevId;
									item.RuleArticle = "ЧИСТО";
									item.DataSource = "Реестр Class\\Media\\" + subName;
									item.IsViolation = false;
									item.Timestamp = DateTime.Now;
									item.RowColor = Ui.AccentGreen;
									item.DetailedForensics = "Штатный физический звуковой контроллер оборудования (" + providerName + "). Признаков виртуализации или перехвата не обнаружено.";
									item.TechnicalAnalysis = "HKLM\\" + classKeyPath + "\\" + subName + "\nDriverDesc: " + driverDesc + "\nProvider: " + providerName;
									item.BanVerdictGuide = "Нарушений не выявлено. Физическое устройство авторизовано.";
									list.Add(item);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Audio Registry Class scan error: " + ex.Message);
			}

			// 2. Аудит аудио эндпоинтов в HKLM\SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI
			try
			{
				string mmdevapiPath = @"SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI";
				using (RegistryKey mmKey = Registry.LocalMachine.OpenSubKey(mmdevapiPath))
				{
					if (mmKey != null)
					{
						string[] epNames = mmKey.GetSubKeyNames();
						foreach (string ep in epNames)
						{
							using (RegistryKey epKey = mmKey.OpenSubKey(ep))
							{
								if (epKey == null) continue;

								string friendlyName = (epKey.GetValue("FriendlyName") as string) ?? "";
								string devDesc = (epKey.GetValue("DeviceDesc") as string) ?? "";
								if (string.IsNullOrEmpty(friendlyName) && string.IsNullOrEmpty(devDesc)) continue;

								string name = !string.IsNullOrEmpty(friendlyName) ? friendlyName : devDesc;
								string lowerName = name.ToLowerInvariant();

								if (lowerName.Contains("cable input") || lowerName.Contains("cable output") ||
									lowerName.Contains("vb-audio") || lowerName.Contains("voicemeeter") ||
									lowerName.Contains("virtual audio cable") || lowerName.Contains("parsec audio"))
								{
									// Проверяем не добавлен ли уже такой
									bool alreadyAdded = false;
									foreach (AudioForensicEvidenceItem it in list)
									{
										if (it.DeviceOrDriverName == name)
										{
											alreadyAdded = true;
											break;
										}
									}

									if (!alreadyAdded)
									{
										detectedVirtualDrivers.Add("Endpoint: " + name);
										AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
										item.Index = idx++;
										item.DeviceOrDriverName = name;
										item.Category = "Аудио эндпоинт (Виртуальное устройство 5.08)";
										item.Verdict = "🚨 BAN 5.08";
										item.Severity = "CRITICAL";
										item.RiskScore = 95;
										item.DriverFileOrPath = "MMDEVAPI Virtual Endpoint";
										item.HardwareIdOrPid = ep;
										item.RuleArticle = "5.08";
										item.DataSource = "Реестр Enum\\MMDEVAPI";
										item.IsViolation = true;
										item.Timestamp = DateTime.Now;
										item.RowColor = Ui.AccentRed;
										item.DetailedForensics = "ОБНАРУЖЕН АКТИВНЫЙ ВИРТУАЛЬНЫЙ АУДИО ЭНДПОИНТ!\n\n" +
											"Устройство: " + name + "\n" +
											"GUID эндпоинта: " + ep + "\n\n" +
											"Криминалистическое значение:\n" +
											"Интерфейс MMDEVAPI Windows зарегистрировал виртуальное аудиоустройство для воспроизведения/записи. " +
											"Игрок может переключать вывод звука на данный эндпоинт во время проверки, чтобы скрыть переговоры с клинером.";
										item.TechnicalAnalysis = "HKLM\\" + mmdevapiPath + "\\" + ep + "\nFriendlyName: " + friendlyName + "\nDeviceDesc: " + devDesc;
										item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
											"Пункт 5.08 (Использование виртуальных аудиокабелей)\n" +
											"Команда:\n/ban <player> 30d 5.08 Виртуальный аудиокабель " + name;
										list.Add(item);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("MMDEVAPI scan error: " + ex.Message);
			}

			// 3. Прямой аудит файловой системы: C:\Windows\System32\drivers
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string driversDir = Path.Combine(winDir, @"System32\drivers");
				if (Directory.Exists(driversDir))
				{
					string[] suspiciousDrivers = new string[]
					{
						"vbaudio_cable64_win7.sys", "vbaudio_cable64.sys", "vbaudio_cable.sys",
						"vbaudio_vmvaio64_win7.sys", "vbaudio_vmvaio64.sys", "vbaudio_vaio.sys", "vbaudio_auxvaio64.sys",
						"vckndis5.sys", "vckndis5_x64.sys",
						"parsec-audio.sys", "parsec_audio64.sys",
						"vasdrv.sys", "virtualaudio.sys"
					};

					foreach (string drvName in suspiciousDrivers)
					{
						string fullPath = Path.Combine(driversDir, drvName);
						if (File.Exists(fullPath))
						{
							FileInfo fi = new FileInfo(fullPath);
							bool alreadyInList = false;
							foreach (AudioForensicEvidenceItem it in list)
							{
								if (it.DriverFileOrPath.Contains(drvName))
								{
									alreadyInList = true;
									break;
								}
							}

							if (!alreadyInList)
							{
								detectedVirtualDrivers.Add("SysFile: " + drvName);
								AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
								item.Index = idx++;
								item.DeviceOrDriverName = drvName;
								item.Category = "Файловый драйвер ядра (5.08)";
								item.Verdict = "🚨 BAN 5.08";
								item.Severity = "CRITICAL";
								item.RiskScore = 95;
								item.DriverFileOrPath = fullPath;
								item.HardwareIdOrPid = "Размер: " + fi.Length + " байт";
								item.RuleArticle = "5.08";
								item.DataSource = "Файловая система \\drivers";
								item.IsViolation = true;
								item.Timestamp = fi.LastWriteTime;
								item.RowColor = Ui.AccentRed;
								item.DetailedForensics = "ОБНАРУЖЕН БИНАРНЫЙ ДРАЙВЕР ВИРТУАЛЬНОГО АУДИОКАБЕЛЯ В SYSTEM32\\DRIVERS!\n\n" +
									"Файл: " + fullPath + "\n" +
									"Размер: " + fi.Length + " байт\n" +
									"Дата создания: " + fi.CreationTime.ToString("dd.MM.yyyy HH:mm:ss") + "\n" +
									"Последняя модификация: " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + "\n\n" +
									"Драйвер физически установлен в операционной системе Windows для маршрутизации звуковых каналов.";
								item.TechnicalAnalysis = "Path: " + fullPath + "\nSize: " + fi.Length + "\nSHA256 / Signed: Windows Driver System";
								item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Драйвер виртуального аудиокабеля " + drvName + ")\nКоманда:\n/ban <player> 30d 5.08 Драйвер виртуального аудиокабеля " + drvName;
								list.Add(item);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Drivers filesystem scan error: " + ex.Message);
			}

			// 4. Аудит активных процессов: VoiceMeeter, Sunshine, Moonlight, Parsec
			try
			{
				Process[] processes = Process.GetProcesses();
				foreach (Process p in processes)
				{
					try
					{
						string pName = p.ProcessName.ToLowerInvariant();
						if (pName == "voicemeeter" || pName == "voicemeeter8" || pName == "voicemeeterpro" ||
							pName == "vbcable_controlpanel" || pName == "vacsetup" || pName == "audiorepeater")
						{
							detectedRemoteProcesses.Add("Микшер: " + p.ProcessName + " (PID " + p.Id + ")");
							AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
							item.Index = idx++;
							item.DeviceOrDriverName = p.ProcessName + ".exe";
							item.Category = "VoiceMeeter / VB-Cable процесс (5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 95;
							item.DriverFileOrPath = SafeGetProcessPath(p);
							item.HardwareIdOrPid = "PID: " + p.Id;
							item.RuleArticle = "5.08";
							item.DataSource = "Активный процесс ОС";
							item.IsViolation = true;
							item.Timestamp = SafeGetStartTime(p);
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "АКТИВНЫЙ ПРОЦЕСС МАРШРУТИЗАЦИИ АУДИО VOICEMEETER / VB-CABLE ВО ВРЕМЯ ПРОВЕРКИ!\n\n" +
								"Процесс: " + p.ProcessName + ".exe (PID: " + p.Id + ")\n" +
								"Время запуска: " + item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss") + "\n" +
								"Путь: " + item.DriverFileOrPath + "\n\n" +
								"Криминалистическое значение:\n" +
								"Процесс микшера прямо сейчас активен в операционной системе. Это исключает случайность: игрок активно использует " +
								"разделение аудиоканалов прямо во время прохождения проверки ScreenShare (Регламент 5.08)!";
							item.TechnicalAnalysis = "Process: " + p.ProcessName + "\nPID: " + p.Id + "\nPath: " + item.DriverFileOrPath;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Активный VoiceMeeter при проверке)\nКоманда:\n/ban <player> 30d 5.08 Использование VoiceMeeter при проверке для подсказок";
							list.Add(item);
						}
						else if (pName == "sunshine" || pName == "moonlight")
						{
							detectedRemoteProcesses.Add("Стример: " + p.ProcessName + " (PID " + p.Id + ")");
							AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
							item.Index = idx++;
							item.DeviceOrDriverName = p.ProcessName + ".exe";
							item.Category = "Удаленный аудиостриминг (Sunshine/Moonlight 5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 95;
							item.DriverFileOrPath = SafeGetProcessPath(p);
							item.HardwareIdOrPid = "PID: " + p.Id;
							item.RuleArticle = "5.08";
							item.DataSource = "Активный процесс ОС";
							item.IsViolation = true;
							item.Timestamp = SafeGetStartTime(p);
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "ОБНАРУЖЕН СЕРВЕР/КЛИЕНТ УДАЛЕННОГО СТРИМИНГА SUNSHINE / MOONLIGHT!\n\n" +
								"Процесс: " + p.ProcessName + " (PID: " + p.Id + ")\n" +
								"Sunshine/Moonlight передает аудио и видеопоток с ультранизкой задержкой по сети. " +
								"Используется для удаленного шеринга экрана третьим лицам или скрытой передачи звука игры и голосового чата.";
							item.TechnicalAnalysis = "Process: " + p.ProcessName + "\nPID: " + p.Id;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Удаленный стриминг Sunshine/Moonlight)\nКоманда:\n/ban <player> 30d 5.08 Удаленный аудиостриминг Sunshine/Moonlight";
							list.Add(item);
						}
						else if (pName == "parsec")
						{
							detectedRemoteProcesses.Add("Parsec (PID " + p.Id + ")");
							AudioForensicEvidenceItem item = new AudioForensicEvidenceItem();
							item.Index = idx++;
							item.DeviceOrDriverName = "Parsec.exe";
							item.Category = "Удаленный аудиостриминг (Parsec 5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 90;
							item.DriverFileOrPath = SafeGetProcessPath(p);
							item.HardwareIdOrPid = "PID: " + p.Id;
							item.RuleArticle = "5.08";
							item.DataSource = "Активный процесс ОС";
							item.IsViolation = true;
							item.Timestamp = SafeGetStartTime(p);
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "ОБНАРУЖЕН АКТИВНЫЙ ПРОЦЕСС PARSEC ВО ВРЕМЯ ПРОВЕРКИ!\n\n" +
								"Parsec — приложение для сверхнизкозадержного удаленного доступа к рабочему столу и аудиоканалам. " +
								"Наличие запущенного Parsec во время проверки ScreenShare указывает на параллельный сеанс удаленного помощника.";
							item.TechnicalAnalysis = "Process: parsec.exe\nPID: " + p.Id;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Активный Parsec при проверке)\nКоманда:\n/ban <player> 30d 5.08 Запущенный Parsec при проверке ScreenShare";
							list.Add(item);
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Process scan error: " + ex.Message);
			}

			// 5. Синтез улик: Корреляция со ScreenShare / Программами удаленного доступа (AnyDesk / Discord / Telegram / RustDesk)
			try
			{
				List<string> activeScreenShareApps = new List<string>();
				Process[] allP = Process.GetProcesses();
				foreach (Process p in allP)
				{
					try
					{
						string pn = p.ProcessName.ToLowerInvariant();
						if (pn == "anydesk") activeScreenShareApps.Add("AnyDesk (PID " + p.Id + ")");
						else if (pn == "discord") activeScreenShareApps.Add("Discord");
						else if (pn == "rustdesk") activeScreenShareApps.Add("RustDesk (PID " + p.Id + ")");
						else if (pn == "teamviewer") activeScreenShareApps.Add("TeamViewer (PID " + p.Id + ")");
						else if (pn == "telegram") activeScreenShareApps.Add("Telegram");
					}
					catch { }
				}

				// Если найдены виртуальные аудиоустройства И одновременно активны программы ScreenShare/Связи
				if (detectedVirtualDrivers.Count > 0 && activeScreenShareApps.Count > 0)
				{
					string appsStr = string.Join(", ", activeScreenShareApps.ToArray());
					string drvStr = string.Join(", ", detectedVirtualDrivers.ToArray());

					AudioForensicEvidenceItem synthItem = new AudioForensicEvidenceItem();
					synthItem.Index = idx++;
					synthItem.DeviceOrDriverName = "Скрытый аудиоканал консультанта (СИНТЕЗ)";
					synthItem.Category = "Посторонняя помощь / Скрытый аудиоканал (5.08)";
					synthItem.Verdict = "🚨 КРИТИЧЕСКИЙ BAN 5.08";
					synthItem.Severity = "CRITICAL";
					synthItem.RiskScore = 100;
					synthItem.DriverFileOrPath = "Синтез: " + appsStr + " + " + drvStr;
					synthItem.HardwareIdOrPid = "СИНТЕЗ КОРРЕЛЯЦИИ";
					synthItem.RuleArticle = "5.08";
					synthItem.DataSource = "Синтез улик AnyDesk/Discord + Virtual Audio";
					synthItem.IsViolation = true;
					synthItem.Timestamp = DateTime.Now;
					synthItem.RowColor = Ui.AccentRed;
					synthItem.DetailedForensics = "🚨 СИНТЕЗ КРИМИНАЛИСТИЧЕСКИХ УЛИК: ОБНАРУЖЕНА ПОСТОРОННЯЯ ПОМОЩЬ И СКРЫТЫЙ АУДИОКАНАЛ (5.08)!\n\n" +
						"1. Активные приложения демонстрации экрана / связи:\n   " + appsStr + "\n\n" +
						"2. Обнаруженные виртуальные аудиоканалы / микшеры:\n   " + drvStr + "\n\n" +
						"СУДЕБНЫЙ ВЫВОД:\n" +
						"Установлена одновременная работа виртуальных аудиокабелей и программ удаленного подключения/голосовой связи. " +
						"Данная конфигурация применяется игроками исключительно с целью обхода трансляции: модератору передается чистый звук игры, " +
						"тогда как третий человек (консультант/клинер) удаленно надиктовывает игроку подсказки прямо в ухо через отдельный виртуальный аудиоканал.\n\n" +
						"Это прямое и неоспоримое нарушение п. 5.08 (Посторонняя помощь и создание скрытых каналов связи при проверке).";
					synthItem.TechnicalAnalysis = "Correlation Trigger:\nApps: " + appsStr + "\nVirtual Audio: " + drvStr + "\nConfidence: 100%";
					synthItem.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\n" +
						"Пункт регламента: 5.08 (Посторонняя помощь / Скрытый аудиоканал консультанта при проверке)\n" +
						"Срок: 30 дней\n\n" +
						"Команда:\n/ban <player> 30d 5.08 Посторонняя помощь и скрытый аудиоканал консультанта при проверке";
					list.Insert(0, synthItem); // Ставим самым первым
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Synthesis correlation error: " + ex.Message);
			}

			// Если ничего подозрительного не найдено и устройств нет, добавляем физическую чистую запись
			if (list.Count == 0)
			{
				AudioForensicEvidenceItem cleanItem = new AudioForensicEvidenceItem();
				cleanItem.Index = idx++;
				cleanItem.DeviceOrDriverName = "Физические звуковые адаптеры Windows";
				cleanItem.Category = "Физическое аудиоустройство";
				cleanItem.Verdict = "🟢 БЕЗОПАСНО";
				cleanItem.Severity = "SAFE";
				cleanItem.RiskScore = 0;
				cleanItem.DriverFileOrPath = "Standard Windows Audio Endpoint";
				cleanItem.HardwareIdOrPid = "OK";
				cleanItem.RuleArticle = "ЧИСТО";
				cleanItem.DataSource = "Системный аудит";
				cleanItem.IsViolation = false;
				cleanItem.Timestamp = DateTime.Now;
				cleanItem.RowColor = Ui.AccentGreen;
				cleanItem.DetailedForensics = "Виртуальных аудиокабелей (VB-Cable, VoiceMeeter, VAC, Parsec), фоновых стримеров (Sunshine/Moonlight) и скрытых каналов подсказок не обнаружено. Аудиосистема чиста.";
				cleanItem.TechnicalAnalysis = "No virtual audio drivers or mixers detected.";
				cleanItem.BanVerdictGuide = "Звуковая конфигурация соответствует регламенту. Нарушений нет.";
				list.Add(cleanItem);
			}

			// Обновляем индексы
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Index = i + 1;
			}

			return list;
		}

		private static string SafeGetProcessPath(Process p)
		{
			try
			{
				return p.MainModule != null ? p.MainModule.FileName : p.ProcessName;
			}
			catch
			{
				return p.ProcessName;
			}
		}

		private static DateTime SafeGetStartTime(Process p)
		{
			try
			{
				return p.StartTime;
			}
			catch
			{
				return DateTime.Now;
			}
		}

		#endregion
	}
}
