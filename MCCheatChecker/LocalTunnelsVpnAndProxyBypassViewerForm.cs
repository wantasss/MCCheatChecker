using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum TunnelForensicFilter
	{
		All,
		ViolationsOnly,
		ReverseTunnels,
		VpnAdapters,
		ProxySettings,
		HostsFile,
		CleanBaseline
	}

	public class TunnelEvidenceItem
	{
		public int Index;
		public string ArtifactName;
		public string Category;
		public string Verdict;
		public string Severity; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public int RiskScore; // 0..100
		public string SettingsOrPath;
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

	public class LocalTunnelsVpnAndProxyBypassViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<TunnelEvidenceItem> allItems = new List<TunnelEvidenceItem>();
		private TunnelForensicFilter currentFilter = TunnelForensicFilter.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiTunnels;
		private Label lblKpiVpn;
		private Label lblKpiProxy;
		private Label lblKpiHosts;
		private Label lblKpiClean;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterTunnels;
		private DarkButton btnFilterVpn;
		private DarkButton btnFilterProxy;
		private DarkButton btnFilterHosts;
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

		public LocalTunnelsVpnAndProxyBypassViewerForm() : this("Player")
		{
		}

		public LocalTunnelsVpnAndProxyBypassViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponentCustom();
			StartScan();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "🌐 Инспектор сетевых туннелей, VPN и прокси (Tool 82 / Step 79) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🌐 Инспектор сетевых туннелей, VPN и прокси (Tool 82 / Step 79)";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 10);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Судебная экспертиза сетевого трафика и уклонений: аудит туннелей Ngrok/Cloudflared/Frp, VPN адаптеров (Wintun/WireGuard/TAP), локальных прокси (V2Ray/Tor) и подмены hosts (5.08).";
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
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ПРОВЕРОК", "0", Ui.TextPrimary, ref xOff, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("ТУННЕЛИ NGROK (5.08)", "0", Ui.AccentRed, ref xOff, out lblKpiTunnels));
			kpiPanel.Controls.Add(CreateKpiBadge("АКТИВНЫЕ VPN (5.08)", "0", Ui.AccentOrange, ref xOff, out lblKpiVpn));
			kpiPanel.Controls.Add(CreateKpiBadge("СИСТЕМНЫЙ ПРОКСИ (5.08)", "0", Ui.AccentPurple, ref xOff, out lblKpiProxy));
			kpiPanel.Controls.Add(CreateKpiBadge("ФАЙЛ HOSTS (5.08)", "0", Ui.AccentRed, ref xOff, out lblKpiHosts));
			kpiPanel.Controls.Add(CreateKpiBadge("ПРЯМОЕ СОЕДИНЕНИЕ", "0", Ui.AccentGreen, ref xOff, out lblKpiClean));

			// 3. Панель фильтров и поиска
			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.Surface;
			filterPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterPanel);

			int fX = 12;
			btnFilterAll = CreateFilterButton("Все (0)", TunnelForensicFilter.All, ref fX, filterPanel);
			btnFilterViolations = CreateFilterButton("Нарушения 5.08 (0)", TunnelForensicFilter.ViolationsOnly, ref fX, filterPanel);
			btnFilterTunnels = CreateFilterButton("Туннели Ngrok (0)", TunnelForensicFilter.ReverseTunnels, ref fX, filterPanel);
			btnFilterVpn = CreateFilterButton("VPN адаптеры (0)", TunnelForensicFilter.VpnAdapters, ref fX, filterPanel);
			btnFilterProxy = CreateFilterButton("Прокси (0)", TunnelForensicFilter.ProxySettings, ref fX, filterPanel);
			btnFilterHosts = CreateFilterButton("Файл hosts (0)", TunnelForensicFilter.HostsFile, ref fX, filterPanel);
			btnFilterClean = CreateFilterButton("Чистые (0)", TunnelForensicFilter.CleanBaseline, ref fX, filterPanel);

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
			listView.Columns.Add("Объект / Адаптер", 260);
			listView.Columns.Add("Категория", 190);
			listView.Columns.Add("Вердикт", 140);
			listView.Columns.Add("Риск", 70);
			listView.Columns.Add("Настройки / Путь", 200);
			listView.Columns.Add("ID / PID", 140);
			listView.Columns.Add("Правило", 85);
			listView.Columns.Add("Источник данных", 160);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;

			TabPage tabAnalysis = new TabPage("📋 Судебный анализ сетевого туннелирования");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTechnical = new TabPage("⚙️ Адаптеры, прокси и файл hosts");
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

		private DarkButton CreateFilterButton(string text, TunnelForensicFilter mode, ref int xOffset, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(140, 28);
			btn.Location = new Point(xOffset, 8);
			btn.Tag = mode;
			btn.Click += delegate
			{
				currentFilter = (TunnelForensicFilter)btn.Tag;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			xOffset += 145;
			return btn;
		}

		public void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование туннелей, VPN и прокси...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = PerformTunnelAndProxyAudit();
			};
			scanWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					MessageBox.Show("Ошибка сетевого аудита: " + e.Error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatusPill.Text = "❌ ОШИБКА";
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				List<TunnelEvidenceItem> results = e.Result as List<TunnelEvidenceItem>;
				if (results != null)
				{
					allItems = results;
				}

				UpdateKpiAndCounts();
				ApplyFilter();

				int violCount = 0;
				foreach (TunnelEvidenceItem it in allItems)
				{
					if (it.IsViolation) violCount++;
				}

				if (violCount > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНЫ ТУННЕЛИ 5.08 ({0})", violCount);
					lblStatusPill.ForeColor = Ui.AccentRed;
					lblStatusPill.BackColor = Color.FromArgb(50, 20, 20);
				}
				else
				{
					lblStatusPill.Text = "🟢 СЕТЬ БЕЗОПАСНА";
					lblStatusPill.ForeColor = Ui.AccentGreen;
					lblStatusPill.BackColor = Color.FromArgb(20, 45, 20);
				}
			};
			scanWorker.RunWorkerAsync();
		}

		private void UpdateKpiAndCounts()
		{
			int total = allItems.Count;
			int tunnels = 0;
			int vpn = 0;
			int proxy = 0;
			int hosts = 0;
			int clean = 0;

			foreach (TunnelEvidenceItem it in allItems)
			{
				if (it.Category.Contains("Туннель") || it.Category.Contains("Ngrok") || it.Category.Contains("Cloudflared") || it.Category.Contains("Frp")) tunnels++;
				if (it.Category.Contains("VPN") || it.Category.Contains("Wintun") || it.Category.Contains("WireGuard")) vpn++;
				if (it.Category.Contains("Прокси") || it.Category.Contains("Proxy")) proxy++;
				if (it.Category.Contains("hosts")) hosts++;
				if (!it.IsViolation) clean++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiTunnels.Text = tunnels.ToString();
			lblKpiVpn.Text = vpn.ToString();
			lblKpiProxy.Text = proxy.ToString();
			lblKpiHosts.Text = hosts.ToString();
			lblKpiClean.Text = clean.ToString();

			int viol = tunnels + vpn + proxy + hosts;
			btnFilterAll.Text = string.Format("Все ({0})", total);
			btnFilterViolations.Text = string.Format("Нарушения 5.08 ({0})", viol);
			btnFilterTunnels.Text = string.Format("Туннели ({0})", tunnels);
			btnFilterVpn.Text = string.Format("VPN ({0})", vpn);
			btnFilterProxy.Text = string.Format("Прокси ({0})", proxy);
			btnFilterHosts.Text = string.Format("Hosts ({0})", hosts);
			btnFilterClean.Text = string.Format("Чистые ({0})", clean);
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (TunnelEvidenceItem it in allItems)
			{
				bool matchFilter = false;
				switch (currentFilter)
				{
					case TunnelForensicFilter.All:
						matchFilter = true;
						break;
					case TunnelForensicFilter.ViolationsOnly:
						matchFilter = it.IsViolation;
						break;
					case TunnelForensicFilter.ReverseTunnels:
						matchFilter = it.Category.Contains("Туннель") || it.Category.Contains("Ngrok") || it.Category.Contains("Cloudflared") || it.Category.Contains("Frp");
						break;
					case TunnelForensicFilter.VpnAdapters:
						matchFilter = it.Category.Contains("VPN") || it.Category.Contains("Wintun") || it.Category.Contains("WireGuard");
						break;
					case TunnelForensicFilter.ProxySettings:
						matchFilter = it.Category.Contains("Прокси") || it.Category.Contains("Proxy");
						break;
					case TunnelForensicFilter.HostsFile:
						matchFilter = it.Category.Contains("hosts");
						break;
					case TunnelForensicFilter.CleanBaseline:
						matchFilter = !it.IsViolation;
						break;
				}

				if (!matchFilter) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string haystack = (it.ArtifactName + " " + it.Category + " " + it.SettingsOrPath + " " + it.HardwareIdOrPid + " " + it.DetailedForensics).ToLowerInvariant();
					if (!haystack.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ArtifactName);
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.RiskScore.ToString() + "%");
				lvi.SubItems.Add(it.SettingsOrPath);
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

			TunnelEvidenceItem it = listView.SelectedItems[0].Tag as TunnelEvidenceItem;
			if (it == null) return;

			txtAnalysis.Text = it.DetailedForensics;
			txtTechnical.Text = it.TechnicalAnalysis;
			txtBanVerdict.Text = it.BanVerdictGuide;
		}

		private void ClearDetailTabs()
		{
			txtAnalysis.Text = "Выберите запись в таблице для криминалистического анализа сетевого туннелирования.";
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

			TunnelEvidenceItem it = listView.SelectedItems[0].Tag as TunnelEvidenceItem;
			if (it == null || !it.IsViolation)
			{
				MessageBox.Show("Выбранный элемент не является нарушением правил!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string reason = "5.08 Уклонение от проверки (Сетевой обход / Туннель / Прокси: " + it.ArtifactName + ")";
			string cmd = "/ban " + targetPlayerName + " 30d " + reason;
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**🌐 MCCheatChecker — Отчёт инспектора сетевых туннелей, VPN и прокси (Tool 82)**");
			sb.AppendLine("**Игрок:** `" + targetPlayerName + "` | **Дата проверки:** `" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			sb.AppendLine();

			int violations = 0;
			foreach (TunnelEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
			}

			if (violations > 0)
			{
				sb.AppendLine("🚨 **СТАТУС:** `ОБНАРУЖЕНЫ НАРУШЕНИЯ (" + violations + " шт.)`");
				sb.AppendLine();
				sb.AppendLine("**Выявленные сетевые туннели, VPN и подмены hosts (5.08):**");
				foreach (TunnelEvidenceItem it in allItems)
				{
					if (it.IsViolation)
					{
						sb.AppendLine("- ⛔ `[" + it.RuleArticle + "]` **" + it.Category + "**: `" + it.ArtifactName + "` (" + it.SettingsOrPath + ") — Риск: `" + it.RiskScore + "%`");
					}
				}
				sb.AppendLine();
				sb.AppendLine("**Команда бана:** `/ban " + targetPlayerName + " 30d 5.08 Использование сетевых туннелей и прокси для обхода проверки`");
			}
			else
			{
				sb.AppendLine("✅ **СТАТУС:** `ЧИСТО (Обратных туннелей Ngrok/Cloudflared, VPN Wintun/WireGuard, системных прокси и подмен hosts не обнаружено)`");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована в буфер обмена!", "Экспорт в Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "HTML Files (*.html)|*.html";
				sfd.FileName = "MCCheatChecker_NetworkTunnelReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html>");
					sb.AppendLine("<html lang='ru'>");
					sb.AppendLine("<head><meta charset='utf-8'><title>Отчёт сетевого аудита — " + targetPlayerName + "</title>");
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
					sb.AppendLine("<h1>🌐 Отчёт инспектора сетевых туннелей, VPN и прокси (Tool 82)</h1>");
					sb.AppendLine("<p>Игрок: <b>" + targetPlayerName + "</b> | Время проверки: <b>" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</b></p>");

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ВСЕГО ПРОВЕРОК</div><div class='kpi-value' style='color:#E0E0E0'>" + lblKpiTotal.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ТУННЕЛИ NGROK</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiTunnels.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>АКТИВНЫЕ VPN</div><div class='kpi-value' style='color:#FFA726'>" + lblKpiVpn.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>СИСТЕМНЫЙ ПРОКСИ</div><div class='kpi-value' style='color:#AB47BC'>" + lblKpiProxy.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ФАЙЛ HOSTS</div><div class='kpi-value' style='color:#FF5252'>" + lblKpiHosts.Text + "</div></div>");
					sb.AppendLine("<div class='kpi'><div class='kpi-title'>ПРЯМОЕ СОЕДИНЕНИЕ</div><div class='kpi-value' style='color:#4CAF50'>" + lblKpiClean.Text + "</div></div>");
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>#</th><th>Объект / Адаптер</th><th>Категория</th><th>Вердикт</th><th>Риск</th><th>Настройки / Путь</th><th>ID / PID</th><th>Правило</th><th>Источник</th></tr>");

					foreach (TunnelEvidenceItem it in allItems)
					{
						string css = it.IsViolation ? "crit" : "safe";
						sb.AppendLine("<tr>");
						sb.AppendLine("<td>" + it.Index + "</td>");
						sb.AppendLine("<td><b>" + it.ArtifactName + "</b></td>");
						sb.AppendLine("<td>" + it.Category + "</td>");
						sb.AppendLine("<td class='" + css + "'>" + it.Verdict + "</td>");
						sb.AppendLine("<td>" + it.RiskScore + "%</td>");
						sb.AppendLine("<td><code>" + it.SettingsOrPath + "</code></td>");
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

		public static List<TunnelEvidenceItem> PerformTunnelAndProxyAudit()
		{
			List<TunnelEvidenceItem> list = new List<TunnelEvidenceItem>();
			int idx = 1;

			// 1. Проверка активных процессов сетевых туннелей и прокси
			try
			{
				string[] tunnelExeNames = new string[]
				{
					"ngrok", "cloudflared", "localxpose", "frpc", "frps",
					"v2ray", "xray", "sing-box", "tor", "proxifier",
					"zerotier-one", "tailscale", "wireguard", "openvpn"
				};

				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					try
					{
						string pn = p.ProcessName.ToLowerInvariant();
						foreach (string targetName in tunnelExeNames)
						{
							if (pn == targetName || pn.StartsWith(targetName + "_"))
							{
								TunnelEvidenceItem item = new TunnelEvidenceItem();
								item.Index = idx++;
								item.ArtifactName = p.ProcessName + ".exe";
								item.Category = (pn == "ngrok" || pn == "cloudflared" || pn == "localxpose" || pn.StartsWith("frp"))
									? "Обратный сетевой туннель (Ngrok 5.08)"
									: "Локальный прокси / VPN процесс (5.08)";
								item.Verdict = "🚨 BAN 5.08";
								item.Severity = "CRITICAL";
								item.RiskScore = 95;
								item.SettingsOrPath = SafeGetProcessPath(p);
								item.HardwareIdOrPid = "PID: " + p.Id;
								item.RuleArticle = "5.08";
								item.DataSource = "Активный процесс ОС";
								item.IsViolation = true;
								item.Timestamp = SafeGetStartTime(p);
								item.RowColor = Ui.AccentRed;
								item.DetailedForensics = "ОБНАРУЖЕН АКТИВНЫЙ ПРОЦЕСС СЕТЕВОГО ТУННЕЛИРОВАНИЯ / ПРОКСИ ВО ВРЕМЯ ПРОВЕРКИ!\n\n" +
									"Имя процесса: " + p.ProcessName + ".exe\n" +
									"PID: " + p.Id + "\n" +
									"Время запуска: " + item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss") + "\n" +
									"Исполняемый файл: " + item.SettingsOrPath + "\n\n" +
									"Криминалистическое значение:\n" +
									"Инструменты сетевого туннелирования (Ngrok, Cloudflared, Frp, Xray, Proxifier) используются нечестными игроками для:\n" +
									"1) Скрытой маршрутизации команд чита мимо античита сервера.\n" +
									"2) Обхода сетевых ограничений и сокрытия реальных исходящих сокетов.\n" +
									"3) Создания обратного соединения к серверу чистильщика/консультанта.\n\n" +
									"Использование таких утилит при проверке является уклонением (Регламент 5.08).";
								item.TechnicalAnalysis = "ProcessName: " + p.ProcessName + "\nPID: " + p.Id + "\nPath: " + item.SettingsOrPath;
								item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Использование сетевых туннелей/прокси при проверке)\nСрок: 30 дней\n\nКоманда:\n/ban <player> 30d 5.08 Использование сетевого туннеля " + p.ProcessName;
								list.Add(item);
								break;
							}
						}
					}
					catch { }
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Process tunnel scan error: " + ex.Message);
			}

			// 2. Аудит Windows Prefetch (запуск туннелей перед проверкой <= 60 минут)
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string prefetchDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(prefetchDir))
				{
					string[] pfFiles = Directory.GetFiles(prefetchDir, "*.pf");
					foreach (string pf in pfFiles)
					{
						string fileName = Path.GetFileName(pf).ToUpperInvariant();
						if (fileName.StartsWith("NGROK.EXE-") || fileName.StartsWith("CLOUDFLARED.EXE-") ||
							fileName.StartsWith("LOCALXPOSE.EXE-") || fileName.StartsWith("FRPC.EXE-") ||
							fileName.StartsWith("V2RAY.EXE-") || fileName.StartsWith("XRAY.EXE-") ||
							fileName.StartsWith("SING-BOX.EXE-") || fileName.StartsWith("PROXIFIER.EXE-"))
						{
							FileInfo fi = new FileInfo(pf);
							TimeSpan diff = DateTime.Now - fi.LastWriteTime;

							// Если запуск был менее 60 минут назад
							if (diff.TotalMinutes <= 60)
							{
								TunnelEvidenceItem item = new TunnelEvidenceItem();
								item.Index = idx++;
								item.ArtifactName = fileName;
								item.Category = "Prefetch: Туннель запущен перед проверкой (5.08)";
								item.Verdict = "🚨 BAN 5.08";
								item.Severity = "CRITICAL";
								item.RiskScore = 95;
								item.SettingsOrPath = pf;
								item.HardwareIdOrPid = Math.Round(diff.TotalMinutes, 1) + " мин назад";
								item.RuleArticle = "5.08";
								item.DataSource = "Windows Prefetch";
								item.IsViolation = true;
								item.Timestamp = fi.LastWriteTime;
								item.RowColor = Ui.AccentRed;
								item.DetailedForensics = "СЛЕД ЗАПУСКА СЕТЕВОГО ТУННЕЛЯ В PREFETCH НЕПОСРЕДСТВЕННО ПЕРЕД ПРОВЕРКОЙ!\n\n" +
									"Prefetch файл: " + fileName + "\n" +
									"Время последнего запуска: " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + " (" + Math.Round(diff.TotalMinutes, 1) + " мин назад)\n\n" +
									"Криминалистический вывод:\n" +
									"Игрок запускал туннелирование или проксирование непосредственно перед вызовом на проверку ScreenShare (менее 1 часа). " +
									"Это свидетельствует о сокрытии сетевого трафика чита или подготовке к уклонению.";
								item.TechnicalAnalysis = "File: " + pf + "\nLastWriteTime: " + fi.LastWriteTime.ToString("o") + "\nSize: " + fi.Length;
								item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Запуск сетевого туннеля/прокси перед проверкой)\nКоманда:\n/ban <player> 30d 5.08 Запуск сетевого туннеля " + fileName + " перед проверкой";
								list.Add(item);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Prefetch tunnel scan error: " + ex.Message);
			}

			// 3. Аудит виртуальных сетевых адаптеров (Wintun, WireGuard, OpenVPN TAP, Cloudflare WARP, Tailscale)
			try
			{
				NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in interfaces)
				{
					string nicName = nic.Name;
					string nicDesc = nic.Description;
					string lowerDesc = (nicName + " " + nicDesc).ToLowerInvariant();

					bool isVpnOrTunnel = false;
					string vpnType = "";

					if (lowerDesc.Contains("wintun")) { isVpnOrTunnel = true; vpnType = "Wintun Userspace Tunnel"; }
					else if (lowerDesc.Contains("wireguard")) { isVpnOrTunnel = true; vpnType = "WireGuard Tunnel"; }
					else if (lowerDesc.Contains("tap-windows") || lowerDesc.Contains("tap0901") || lowerDesc.Contains("openvpn")) { isVpnOrTunnel = true; vpnType = "OpenVPN TAP Adapter"; }
					else if (lowerDesc.Contains("cloudflare warp") || lowerDesc.Contains("warp")) { isVpnOrTunnel = true; vpnType = "Cloudflare WARP Tunnel"; }
					else if (lowerDesc.Contains("tailscale")) { isVpnOrTunnel = true; vpnType = "Tailscale Tunnel"; }
					else if (lowerDesc.Contains("zerotier")) { isVpnOrTunnel = true; vpnType = "ZeroTier Virtual Port"; }

					if (isVpnOrTunnel)
					{
						bool isUp = (nic.OperationalStatus == OperationalStatus.Up);
						TunnelEvidenceItem item = new TunnelEvidenceItem();
						item.Index = idx++;
						item.ArtifactName = nicName + " (" + vpnType + ")";
						item.Category = isUp ? "Активный VPN / Wintun адаптер (5.08)" : "Установленный VPN адаптер";
						item.Verdict = isUp ? "🚨 BAN 5.08" : "⚠️ ПОДОЗРЕНИЕ";
						item.Severity = isUp ? "CRITICAL" : "WARNING";
						item.RiskScore = isUp ? 90 : 40;
						item.SettingsOrPath = nicDesc;
						item.HardwareIdOrPid = "Статус: " + nic.OperationalStatus.ToString();
						item.RuleArticle = isUp ? "5.08" : "ИНФО";
						item.DataSource = "NetworkInterface / NDIS";
						item.IsViolation = isUp;
						item.Timestamp = DateTime.Now;
						item.RowColor = isUp ? Ui.AccentRed : Ui.AccentOrange;
						item.DetailedForensics = "ОБНАРУЖЕН ВИРТУАЛЬНЫЙ VPN / WINTUN АДАПТЕР!\n\n" +
							"Имя адаптера: " + nicName + "\n" +
							"Описание: " + nicDesc + "\n" +
							"Тип туннеля: " + vpnType + "\n" +
							"Текущий статус: " + nic.OperationalStatus.ToString() + "\n\n" +
							(isUp
								? "Адаптер находится в АКТИВНОМ состоянии (UP) прямо во время проверки! Трафик перенаправляется через виртуальный интерфейс, маскируя игровой IP адрес и сокеты подключения к серверу (Регламент 5.08)."
								: "Адаптер установлен, но в данный момент отключен (Down). Требуется убедиться, что игрок не переключал интерфейс перед проверкой.");
						item.TechnicalAnalysis = "NIC ID: " + nic.Id + "\nDesc: " + nicDesc + "\nSpeed: " + nic.Speed + "\nStatus: " + nic.OperationalStatus;
						item.BanVerdictGuide = isUp
							? "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Активный VPN адаптер при проверке)\nКоманда:\n/ban <player> 30d 5.08 Активный VPN/Wintun туннель при проверке"
							: "Проверить историю подключений и маршруты route print.";
						list.Add(item);
					}
					else if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet || nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
					{
						// Легитимный физический адаптер
						if (!lowerDesc.Contains("virtual") && !lowerDesc.Contains("vmware") && !lowerDesc.Contains("hyper-v") && !lowerDesc.Contains("vbox"))
						{
							TunnelEvidenceItem item = new TunnelEvidenceItem();
							item.Index = idx++;
							item.ArtifactName = nicName;
							item.Category = "Физический сетевой адаптер";
							item.Verdict = "🟢 БЕЗОПАСНО";
							item.Severity = "SAFE";
							item.RiskScore = 0;
							item.SettingsOrPath = nicDesc;
							item.HardwareIdOrPid = "Статус: " + nic.OperationalStatus.ToString();
							item.RuleArticle = "ЧИСТО";
							item.DataSource = "NetworkInterface / NDIS";
							item.IsViolation = false;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentGreen;
							item.DetailedForensics = "Штатный физический адаптер локальной сети (" + nicDesc + "). Прямое соединение без признаков туннелирования.";
							item.TechnicalAnalysis = "NIC: " + nicName + "\nType: " + nic.NetworkInterfaceType + "\nStatus: " + nic.OperationalStatus;
							item.BanVerdictGuide = "Прямое подключение разрешено. Нарушений нет.";
							list.Add(item);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Network interface scan error: " + ex.Message);
			}

			// 4. Аудит системного прокси (HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings)
			try
			{
				string inetKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
				using (RegistryKey inetKey = Registry.CurrentUser.OpenSubKey(inetKeyPath))
				{
					if (inetKey != null)
					{
						object proxyEnableObj = inetKey.GetValue("ProxyEnable");
						int proxyEnable = (proxyEnableObj is int) ? (int)proxyEnableObj : 0;
						string proxyServer = (inetKey.GetValue("ProxyServer") as string) ?? "";
						string autoConfigUrl = (inetKey.GetValue("AutoConfigURL") as string) ?? "";

						if (proxyEnable == 1 || !string.IsNullOrEmpty(proxyServer))
						{
							TunnelEvidenceItem item = new TunnelEvidenceItem();
							item.Index = idx++;
							item.ArtifactName = "Системный прокси: " + proxyServer;
							item.Category = "Системный прокси-сервер (5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 95;
							item.SettingsOrPath = "ProxyServer=" + proxyServer + "; ProxyEnable=" + proxyEnable;
							item.HardwareIdOrPid = "HKCU\\Internet Settings";
							item.RuleArticle = "5.08";
							item.DataSource = "Реестр Internet Settings";
							item.IsViolation = true;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "В СИСТЕМЕ АКТИВИРОВАН ЛОКАЛЬНЫЙ / ВНЕШНИЙ ПРОКСИ-СЕРВЕР!\n\n" +
								"ProxyEnable: " + proxyEnable + "\n" +
								"ProxyServer: " + proxyServer + "\n\n" +
								"Криминалистическое значение:\n" +
								"Настройка ProxyServer в реестре Windows перенаправляет HTTP/SOCKS трафик через промежуточный хост (часто 127.0.0.1:8080 или SOCKS5). " +
								"Используется читерами для фильтрации и перехвата пакетов, инъекции чит-пакетов через прокси, а также подмены IP при проверке (Регламент 5.08).";
							item.TechnicalAnalysis = "Registry: HKCU\\" + inetKeyPath + "\nProxyEnable: " + proxyEnable + "\nProxyServer: " + proxyServer;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Использование локального прокси при проверке)\nКоманда:\n/ban <player> 30d 5.08 Использование прокси-сервера " + proxyServer;
							list.Add(item);
						}

						if (!string.IsNullOrEmpty(autoConfigUrl))
						{
							TunnelEvidenceItem item = new TunnelEvidenceItem();
							item.Index = idx++;
							item.ArtifactName = "PAC скрипт: " + autoConfigUrl;
							item.Category = "Автонастройка прокси (PAC 5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 90;
							item.SettingsOrPath = "AutoConfigURL=" + autoConfigUrl;
							item.HardwareIdOrPid = "HKCU\\Internet Settings";
							item.RuleArticle = "5.08";
							item.DataSource = "Реестр Internet Settings";
							item.IsViolation = true;
							item.Timestamp = DateTime.Now;
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "ОБНАРУЖЕН СКРИПТ АВТОМАТИЧЕСКОЙ НАСТРОЙКИ ПРОКСИ (PAC)!\n\n" +
								"AutoConfigURL: " + autoConfigUrl + "\n\n" +
								"PAC скрипт позволяет избирательно направлять трафик определенных доменов (например, серверов майнкрафта или античита) через скрытый прокси.";
							item.TechnicalAnalysis = "AutoConfigURL: " + autoConfigUrl;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (PAC скрипт автонастройки прокси)\nКоманда:\n/ban <player> 30d 5.08 PAC скрипт скрытого проксирования трафика";
							list.Add(item);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Proxy registry scan error: " + ex.Message);
			}

			// 5. Аудит файла hosts на перенаправление серверов Mojang и античитов
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string hostsPath = Path.Combine(winDir, @"System32\drivers\etc\hosts");
				if (File.Exists(hostsPath))
				{
					string[] lines = File.ReadAllLines(hostsPath);
					foreach (string line in lines)
					{
						string trimmed = line.Trim();
						if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

						string lowerLine = trimmed.ToLowerInvariant();
						bool isMojangTampered = lowerLine.Contains("mojang") || lowerLine.Contains("minecraft.net");
						bool isAnticheatTampered = lowerLine.Contains("anticheat") || lowerLine.Contains("intave") || lowerLine.Contains("matrix") || lowerLine.Contains("grim");

						if (isMojangTampered || isAnticheatTampered)
						{
							TunnelEvidenceItem item = new TunnelEvidenceItem();
							item.Index = idx++;
							item.ArtifactName = "Подмена hosts: " + trimmed;
							item.Category = "Модификация файла hosts (5.08)";
							item.Verdict = "🚨 BAN 5.08";
							item.Severity = "CRITICAL";
							item.RiskScore = 100;
							item.SettingsOrPath = hostsPath;
							item.HardwareIdOrPid = "hosts line";
							item.RuleArticle = "5.08";
							item.DataSource = "Файл hosts";
							item.IsViolation = true;
							item.Timestamp = File.GetLastWriteTime(hostsPath);
							item.RowColor = Ui.AccentRed;
							item.DetailedForensics = "КРИТИЧЕСКАЯ ПОДМЕНА В ФАЙЛЕ HOSTS!\n\n" +
								"Строка: " + trimmed + "\n" +
								"Файл: " + hostsPath + "\n\n" +
								"Криминалистическое значение:\n" +
								"В файле hosts обнаружена прямая переадресация серверов авторизации Mojang или доменов античита! " +
								"Это популярный трюк читеров для перенаправления запросов клиента на локальный эмулятор сервера лицензий (TheAltening, EasyMC, кряки) " +
								"или глушения обратной связи античита.";
							item.TechnicalAnalysis = "Hosts path: " + hostsPath + "\nLine: " + trimmed;
							item.BanVerdictGuide = "РЕКОМЕНДАЦИЯ К БАНУ:\nПункт 5.08 (Подмена файла hosts для обхода авторизации / античита)\nСрок: 30 дней\n\nКоманда:\n/ban <player> 30d 5.08 Подмена доменов в файле hosts";
							list.Add(item);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Hosts scan error: " + ex.Message);
			}

			// Если ничего подозрительного не найдено и устройств нет, добавляем физическую чистую запись
			if (list.Count == 0)
			{
				TunnelEvidenceItem cleanItem = new TunnelEvidenceItem();
				cleanItem.Index = idx++;
				cleanItem.ArtifactName = "Прямое сетевое соединение Windows";
				cleanItem.Category = "Чистая сеть";
				cleanItem.Verdict = "🟢 БЕЗОПАСНО";
				cleanItem.Severity = "SAFE";
				cleanItem.RiskScore = 0;
				cleanItem.SettingsOrPath = "Direct Connection (No Tunnels / Proxy)";
				cleanItem.HardwareIdOrPid = "OK";
				cleanItem.RuleArticle = "ЧИСТО";
				cleanItem.DataSource = "Системный аудит";
				cleanItem.IsViolation = false;
				cleanItem.Timestamp = DateTime.Now;
				cleanItem.RowColor = Ui.AccentGreen;
				cleanItem.DetailedForensics = "Обратных сетевых туннелей (Ngrok, Cloudflared, Frp), активных VPN адаптеров (Wintun/WireGuard), системных прокси и подмен в файле hosts не обнаружено.";
				cleanItem.TechnicalAnalysis = "All network interfaces and proxy settings are clean.";
				cleanItem.BanVerdictGuide = "Сетевая конфигурация соответствует регламенту. Нарушений нет.";
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
