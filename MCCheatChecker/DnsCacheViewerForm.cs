using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DnsCacheViewerForm : Form
	{
		private enum DnsFilterMode
		{
			All,
			CheatsOnly,
			CleanersOnly,
			PasteAndFiles,
			TunnelsAndRats,
			Messengers
		}

		private string targetPlayerName;
		private DnsFilterMode currentFilter = DnsFilterMode.All;
		private List<DnsCacheEntry> allEntries = new List<DnsCacheEntry>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailDomain;
		private Label lblDetailIp;
		private Label lblDetailTtl;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterPasteFiles;
		private DarkButton btnFilterTunnels;
		private DarkButton btnFilterMessengers;

		public DnsCacheViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadDnsCacheAsync();
		}

		private void InitializeComponent()
		{
			Text = "🌐 Инспектор DNS-кэша и доменов читов (DNS Cache Forensics)";
			Size = new Size(1160, 720);
			MinimumSize = new Size(900, 540);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🌐  ИНСПЕКТОР DNS-КЭША WINDOWS (Cheat Domains, Loaders & Remote APIs)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Опрос кэша сопоставления DNS-клиента Windows (ipconfig /displaydns)...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Чтение DNS...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Обновить кэш";
			btnRefresh.Location = new Point(880, 8);
			btnRefresh.Size = new Size(130, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				LoadDnsCacheAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnBan = new DarkButton();
			btnBan.Text = "🔨 Скопировать /ban";
			btnBan.Location = new Point(1018, 8);
			btnBan.Size = new Size(160, 26);
			btnBan.BackColor = Color.FromArgb(183, 28, 28);
			btnBan.ForeColor = Color.White;
			btnBan.Cursor = Cursors.Hand;
			btnBan.Click += delegate
			{
				CopyBanCommand();
			};
			topPanel.Controls.Add(btnBan);

			// Filter Row
			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1130, 50);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все (0)", DnsFilterMode.All, 90);
			btnFilterCheats = CreateFilterButton("🚨 Чит-домены (0)", DnsFilterMode.CheatsOnly, 130);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры (0)", DnsFilterMode.CleanersOnly, 115);
			btnFilterPasteFiles = CreateFilterButton("📁 Файлообменники (0)", DnsFilterMode.PasteAndFiles, 150);
			btnFilterTunnels = CreateFilterButton("🌐 Туннели/RAT (0)", DnsFilterMode.TunnelsAndRats, 125);
			btnFilterMessengers = CreateFilterButton("💬 Мессенджеры (0)", DnsFilterMode.Messengers, 130);

			int curX = 0;
			DarkButton[] filterBtns = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterPasteFiles, btnFilterTunnels, btnFilterMessengers };
			foreach (DarkButton b in filterBtns)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 4;
			}

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(curX + 6, 5);
			lblSearch.AutoSize = true;
			navRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(curX + 50, 2);
			searchBox.Size = new Size(165, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 218, 2);
			btnClear.Size = new Size(24, 22);
			btnClear.BackColor = Ui.SurfaceAlt;
			btnClear.ForeColor = Ui.TextMuted;
			btnClear.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			navRow.Controls.Add(btnClear);

			topPanel.Controls.Add(navRow);
			Controls.Add(topPanel);

			// SplitContainer: Left = ListView, Right = Details
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Vertical;
			split.SplitterDistance = 680;
			split.SplitterWidth = 5;
			split.BackColor = Ui.Border;

			// Left ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontMono(8.5f);
			listView.Columns.Add("!", 35, HorizontalAlignment.Center);
			listView.Columns.Add("Домен / Хост", 230, HorizontalAlignment.Left);
			listView.Columns.Add("Тип", 55, HorizontalAlignment.Center);
			listView.Columns.Add("TTL (Срок)", 80, HorizontalAlignment.Left);
			listView.Columns.Add("IP-адрес / Ответ", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Категория / Вердикт", 220, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdateDnsDetails();
			};

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();

			ToolStripMenuItem copyDomainItem = new ToolStripMenuItem("📋  Скопировать домен");
			copyDomainItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					Clipboard.SetText(listView.SelectedItems[0].SubItems[1].Text);
				}
			};

			ToolStripMenuItem copyRowItem = new ToolStripMenuItem("📋  Скопировать строку улик");
			copyRowItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					ListViewItem it = listView.SelectedItems[0];
					Clipboard.SetText(string.Format("{0} | {1} | {2} | {3} | {4}",
						it.SubItems[1].Text, it.SubItems[2].Text, it.SubItems[3].Text, it.SubItems[4].Text, it.SubItems[5].Text));
				}
			};

			ToolStripMenuItem openVtItem = new ToolStripMenuItem("🌐  Проверить домен на VirusTotal");
			openVtItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					string d = listView.SelectedItems[0].SubItems[1].Text.Trim();
					if (!string.IsNullOrEmpty(d))
					{
						try { Process.Start(string.Format("https://www.virustotal.com/gui/domain/{0}", Uri.EscapeDataString(d))); }
						catch { }
					}
				}
			};

			ToolStripMenuItem copyAllReportItem = new ToolStripMenuItem("📋  Скопировать полный протокол DNS");
			copyAllReportItem.Click += delegate
			{
				CopyDnsReport();
			};

			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Сгенерировать /ban за чит-домен (5.01 / 5.08)");
			copyBanItem.Click += delegate
			{
				CopyBanCommand();
			};

			ctx.Items.Add(copyDomainItem);
			ctx.Items.Add(copyRowItem);
			ctx.Items.Add(openVtItem);
			ctx.Items.Add(copyAllReportItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(copyBanItem);
			listView.ContextMenuStrip = ctx;

			split.Panel1.Controls.Add(listView);

			// Right Details Panel
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;
			rightPanel.Padding = new Padding(12);

			Label lblDetHeader = new Label();
			lblDetHeader.Dock = DockStyle.Top;
			lblDetHeader.Height = 24;
			lblDetHeader.Font = Ui.FontTitle(9.5f, true);
			lblDetHeader.ForeColor = Ui.AccentTeal;
			lblDetHeader.Text = "🔍 Детальный анализ DNS-записи";
			rightPanel.Controls.Add(lblDetHeader);

			lblDetailDomain = new Label();
			lblDetailDomain.Dock = DockStyle.Top;
			lblDetailDomain.Height = 22;
			lblDetailDomain.Font = Ui.FontBody(8.5f, true);
			lblDetailDomain.ForeColor = Ui.TextPrimary;
			lblDetailDomain.Text = "Домен: —";
			rightPanel.Controls.Add(lblDetailDomain);

			lblDetailIp = new Label();
			lblDetailIp.Dock = DockStyle.Top;
			lblDetailIp.Height = 22;
			lblDetailIp.Font = Ui.FontBody(8.5f);
			lblDetailIp.ForeColor = Ui.TextSecondary;
			lblDetailIp.Text = "IP-адрес / Данные: —";
			rightPanel.Controls.Add(lblDetailIp);

			lblDetailTtl = new Label();
			lblDetailTtl.Dock = DockStyle.Top;
			lblDetailTtl.Height = 22;
			lblDetailTtl.Font = Ui.FontBody(8.5f);
			lblDetailTtl.ForeColor = Ui.TextMuted;
			lblDetailTtl.Text = "Срок жизни (TTL): —";
			rightPanel.Controls.Add(lblDetailTtl);

			lblDetailRule = new Label();
			lblDetailRule.Dock = DockStyle.Top;
			lblDetailRule.Height = 40;
			lblDetailRule.Font = Ui.FontBody(8.5f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Text = "Регламент: —";
			rightPanel.Controls.Add(lblDetailRule);

			Label lblDescHeader = new Label();
			lblDescHeader.Dock = DockStyle.Top;
			lblDescHeader.Height = 22;
			lblDescHeader.Font = Ui.FontBody(8f, true);
			lblDescHeader.ForeColor = Ui.TextMuted;
			lblDescHeader.Text = "📄 Разъяснение и криминалистический контекст:";
			rightPanel.Controls.Add(lblDescHeader);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Dock = DockStyle.Fill;
			txtDetailDescription.Font = Ui.FontMono(8.5f);
			txtDetailDescription.ReadOnly = true;
			txtDetailDescription.WordWrap = true;
			txtDetailDescription.ScrollBars = ScrollBars.Vertical;
			rightPanel.Controls.Add(txtDetailDescription);
			txtDetailDescription.BringToFront();

			split.Panel2.Controls.Add(rightPanel);
			Controls.Add(split);

			// Bottom Status Strip
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 36;
			bottomPanel.BackColor = Ui.SurfaceAlt;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			Label lblDnsInfo = new Label();
			lblDnsInfo.Text = "ℹ️ DNS-кэш Windows хранит результаты разрешения имён запущенных программ, браузеров и фоновых чит-лоадеров до перезагрузки.";
			lblDnsInfo.Font = Ui.FontBody(8f);
			lblDnsInfo.ForeColor = Ui.AccentGreen;
			lblDnsInfo.Location = new Point(12, 10);
			lblDnsInfo.AutoSize = true;
			bottomPanel.Controls.Add(lblDnsInfo);

			DarkButton btnCopyReport = new DarkButton();
			btnCopyReport.Text = "📋 Скопировать протокол DNS";
			btnCopyReport.Size = new Size(210, 24);
			btnCopyReport.Location = new Point(930, 6);
			btnCopyReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyReport.ForeColor = Color.White;
			btnCopyReport.Cursor = Cursors.Hand;
			btnCopyReport.Click += delegate
			{
				CopyDnsReport();
			};
			bottomPanel.Controls.Add(btnCopyReport);

			Controls.Add(bottomPanel);
		}

		private DarkButton CreateFilterButton(string text, DnsFilterMode mode, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 24);
			btn.Font = Ui.FontBody(8f);
			btn.Cursor = Cursors.Hand;
			btn.BackColor = (mode == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.ForeColor = (mode == currentFilter) ? Color.Black : Ui.TextPrimary;
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtons();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateFilterButtons()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterPasteFiles, btnFilterTunnels, btnFilterMessengers };
			DnsFilterMode[] modes = { DnsFilterMode.All, DnsFilterMode.CheatsOnly, DnsFilterMode.CleanersOnly, DnsFilterMode.PasteAndFiles, DnsFilterMode.TunnelsAndRats, DnsFilterMode.Messengers };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (modes[i] == currentFilter);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].ForeColor = active ? Color.Black : Ui.TextPrimary;
			}
		}

		private void LoadDnsCacheAsync()
		{
			lblStatusPill.Text = "⏳ Опрос кэша DNS...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblHeaderStatus.Text = "Сбор записей DNS из ipconfig /displaydns...";

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<DnsCacheEntry> entries = NetworkAndRemoteForensics.GetDnsCacheEntries();
				e.Result = entries;
			};

			bw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					lblStatusPill.Text = "❌ Ошибка сбора DNS: " + e.Error.Message;
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				allEntries = e.Result as List<DnsCacheEntry> ?? new List<DnsCacheEntry>();

				// Sort suspicious first, then by domain
				allEntries.Sort((a, b) =>
				{
					if (a.IsSuspicious != b.IsSuspicious) return b.IsSuspicious.CompareTo(a.IsSuspicious);
					return string.Compare(a.Domain, b.Domain, StringComparison.OrdinalIgnoreCase);
				});

				int total = allEntries.Count;
				int cheats = allEntries.Count(i => i.Category.IndexOf("Чит-", StringComparison.OrdinalIgnoreCase) >= 0 || i.Category.IndexOf("5.01", StringComparison.OrdinalIgnoreCase) >= 0);
				int cleaners = allEntries.Count(i => i.Category.IndexOf("клинер", StringComparison.OrdinalIgnoreCase) >= 0 || i.Category.IndexOf("5.08", StringComparison.OrdinalIgnoreCase) >= 0);
				int pastes = allEntries.Count(i => i.Category.IndexOf("Paste", StringComparison.OrdinalIgnoreCase) >= 0 || i.Category.IndexOf("Webhook", StringComparison.OrdinalIgnoreCase) >= 0 ||
					i.Domain.Contains("gofile") || i.Domain.Contains("workupload") || i.Domain.Contains("mega.nz") || i.Domain.Contains("mediafire") || i.Domain.Contains("catbox"));
				int tunnels = allEntries.Count(i => i.Category.IndexOf("Туннель", StringComparison.OrdinalIgnoreCase) >= 0 || i.Category.IndexOf("RAT", StringComparison.OrdinalIgnoreCase) >= 0);
				int msgs = allEntries.Count(i => i.Domain.Contains("discord") || i.Domain.Contains("telegram") || i.Domain.Contains("t.me"));

				btnFilterAll.Text = "🔍 Все (" + total + ")";
				btnFilterCheats.Text = "🚨 Чит-домены (" + cheats + ")";
				btnFilterCleaners.Text = "🧹 Клинеры (" + cleaners + ")";
				btnFilterPasteFiles.Text = "📁 Файлы/Пасты (" + pastes + ")";
				btnFilterTunnels.Text = "🌐 Туннели/RAT (" + tunnels + ")";
				btnFilterMessengers.Text = "💬 Мессенджеры (" + msgs + ")";

				if (cheats > 0 || cleaners > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ ЧИТ-ДОМЕНЫ В КЭШЕ ({0} находок)", cheats + cleaners);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ В кэше нет известных чит-доменов";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}

				lblHeaderStatus.Text = string.Format("Кэш DNS получен: {0:N0} записей (чит-доменов 5.01: {1}, клинеров 5.08: {2})", total, cheats, cleaners);

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = allEntries.Where(e =>
			{
				switch (currentFilter)
				{
					case DnsFilterMode.CheatsOnly:
						if (e.Category.IndexOf("Чит-", StringComparison.OrdinalIgnoreCase) < 0 && e.Category.IndexOf("5.01", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case DnsFilterMode.CleanersOnly:
						if (e.Category.IndexOf("клинер", StringComparison.OrdinalIgnoreCase) < 0 && e.Category.IndexOf("5.08", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case DnsFilterMode.PasteAndFiles:
						if (e.Category.IndexOf("Paste", StringComparison.OrdinalIgnoreCase) < 0 && e.Category.IndexOf("Webhook", StringComparison.OrdinalIgnoreCase) < 0 &&
							!e.Domain.Contains("gofile") && !e.Domain.Contains("workupload") && !e.Domain.Contains("mega.nz") && !e.Domain.Contains("mediafire") && !e.Domain.Contains("catbox")) return false;
						break;
					case DnsFilterMode.TunnelsAndRats:
						if (e.Category.IndexOf("Туннель", StringComparison.OrdinalIgnoreCase) < 0 && e.Category.IndexOf("RAT", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case DnsFilterMode.Messengers:
						if (!e.Domain.Contains("discord") && !e.Domain.Contains("telegram") && !e.Domain.Contains("t.me")) return false;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = (e.Domain != null && e.Domain.ToLowerInvariant().Contains(query)) ||
						(e.Data != null && e.Data.ToLowerInvariant().Contains(query)) ||
						(e.Category != null && e.Category.ToLowerInvariant().Contains(query)) ||
						(e.RecordType != null && e.RecordType.ToLowerInvariant().Contains(query)) ||
						(e.Ttl != null && e.Ttl.ToLowerInvariant().Contains(query));
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (DnsCacheEntry entry in filtered)
			{
				string icon = entry.IsSuspicious ? (entry.Category.Contains("5.01") || entry.Category.Contains("5.08") ? "🚨" : "⚠️") : "ℹ️";

				ListViewItem lvi = new ListViewItem(icon);
				lvi.SubItems.Add(entry.Domain ?? "—");
				lvi.SubItems.Add(entry.RecordType ?? "A");
				lvi.SubItems.Add(entry.Ttl ?? "—");
				lvi.SubItems.Add(entry.Data ?? "—");
				lvi.SubItems.Add(entry.Category ?? "Штатный узел");
				lvi.Tag = entry;

				if (entry.IsSuspicious)
				{
					if (entry.Category.Contains("5.01") || entry.Category.Contains("5.08")) lvi.ForeColor = Ui.AccentRed;
					else lvi.ForeColor = Ui.AccentOrange;
				}
				else
				{
					lvi.ForeColor = Ui.TextPrimary;
				}

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearDnsDetails();
			}
		}

		private void UpdateDnsDetails()
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDnsDetails();
				return;
			}

			DnsCacheEntry entry = listView.SelectedItems[0].Tag as DnsCacheEntry;
			if (entry == null) return;

			lblDetailDomain.Text = "Домен: " + (entry.Domain ?? "—");
			lblDetailIp.Text = "IP-адрес / Ответ DNS: " + (entry.Data ?? "—") + "  |  Тип: " + (entry.RecordType ?? "A");
			lblDetailTtl.Text = "Срок жизни в кэше (TTL): " + (entry.Ttl ?? "—");

			if (entry.Category.Contains("5.01"))
			{
				lblDetailRule.Text = "⚖️ Нарушение: Пункт 5.01 (Обращение к серверу чит-клиента/лоадера)\nРекомендуемое наказание: Блокировка от 30 дней.";
				lblDetailRule.ForeColor = Ui.AccentRed;
			}
			else if (entry.Category.Contains("5.08"))
			{
				lblDetailRule.Text = "⚖️ Нарушение: Пункт 5.08 (Использование софта для сокрытия улик / клинера)\nРекомендуемое наказание: Блокировка от 30 дней.";
				lblDetailRule.ForeColor = Ui.AccentRed;
			}
			else if (entry.IsSuspicious)
			{
				lblDetailRule.Text = "⚠️ Внимание: Подозрительный ресурс (вебхук, файлообменник или туннель).";
				lblDetailRule.ForeColor = Ui.AccentOrange;
			}
			else
			{
				lblDetailRule.Text = "ℹ️ Информация: Легитимный системный домен или сетевой сервис.";
				lblDetailRule.ForeColor = Ui.AccentTeal;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АНАЛИЗ DNS ЗАПИСИ ===");
			sb.AppendLine("Доменное имя: " + entry.Domain);
			sb.AppendLine("Тип RR-записи: " + entry.RecordType);
			sb.AppendLine("TTL (Time To Live): " + entry.Ttl);
			sb.AppendLine("Разрешённые адреса / CNAME: " + entry.Data);
			sb.AppendLine("Классификация: " + entry.Category);
			sb.AppendLine();
			sb.AppendLine("--- КРИМИНАЛИСТИЧЕСКАЯ СПРАВКА ---");

			if (entry.Category.Contains("5.01"))
			{
				sb.AppendLine("Домен принадлежит серверной инфраструктуре чит-модификаций для Minecraft (авторизация по HWID, проверка лицензии лоадера, скачивание jar/dll модулей).");
				sb.AppendLine("Факт наличия домена в кэше DNS доказывает отправку сетевых пакетов с данного компьютера к серверу чита.");
			}
			else if (entry.Category.Contains("5.08"))
			{
				sb.AppendLine("Домен связан с утилитами очистки следов и сокрытия читов (клинеры USN журнала, реестра, Prefetch). Присутствие записи указывает на подготовку к сокрытию улик.");
			}
			else if (entry.Category.Contains("Webhook") || entry.Category.Contains("Pastebin"))
			{
				sb.AppendLine("Сервис обмена текстовыми данными или хостинг файлов. Часто используется лоадерами для получения актуального адреса сервера или скрипта внедрения.");
			}
			else if (entry.Category.Contains("Туннель"))
			{
				sb.AppendLine("Инструмент туннелирования трафика или удалённого доступа (RAT).");
			}
			else
			{
				sb.AppendLine("Стандартная запись разрешения имён операционной системы Windows.");
			}

			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDnsDetails()
		{
			lblDetailDomain.Text = "Домен: —";
			lblDetailIp.Text = "IP-адрес / Ответ: —";
			lblDetailTtl.Text = "Срок жизни (TTL): —";
			lblDetailRule.Text = "Регламент: —";
			txtDetailDescription.Text = "Выберите DNS-запись в списке слева для детального криминалистического анализа.";
		}

		private void CopyBanCommand()
		{
			var crit = allEntries.Where(e => e.Category.Contains("5.01") || e.Category.Contains("5.08")).ToList();
			string reason = "DNS кэш чита";
			string rule = "5.01";
			if (crit.Count > 0)
			{
				DnsCacheEntry first = crit[0];
				if (first.Category.Contains("5.08"))
				{
					rule = "5.08";
					reason = "DNS кэш сокрытия улик: " + first.Domain;
				}
				else
				{
					rule = "5.01";
					reason = "DNS кэш чита: " + first.Domain;
				}
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, rule, reason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена!\n\n" + cmd, "Генератор наказания", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDnsReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("            ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АНАЛИЗА DNS-КЭША (SCREENSHARE)         ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Дата и время: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего записей в кэше: " + allEntries.Count);
			sb.AppendLine("Подозрительных / чит-доменов: " + allEntries.Count(e => e.IsSuspicious));
			sb.AppendLine();

			sb.AppendLine("--- НАЙДЕННЫЕ ЧИТ-ДОМЕНЫ И СЕРВЕРЫ СОКРЫТИЯ УЛИК ---");
			var suspList = allEntries.Where(e => e.IsSuspicious).ToList();
			if (suspList.Count == 0)
			{
				sb.AppendLine("Чит-доменов и сервисов сокрытия улик в DNS кэше НЕ обнаружено.");
			}
			else
			{
				foreach (var e in suspList)
				{
					sb.AppendLine(string.Format("[{0}] Домен: {1,-35} | Ответ: {2,-20} | TTL: {3}", e.Category, e.Domain, e.Data, e.Ttl));
				}
			}

			sb.AppendLine();
			sb.AppendLine("--- ВСЕ ОСТАЛЬНЫЕ ЗАПИСИ DNS-КЭША ---");
			foreach (var e in allEntries.Where(e => !e.IsSuspicious))
			{
				sb.AppendLine(string.Format("{0,-40} | Тип: {1,-5} | TTL: {2,-8} | Данные: {3}", e.Domain, e.RecordType, e.Ttl, e.Data));
			}

			sb.AppendLine();
			sb.AppendLine("Регламент: Обращение к доменам читов наказывается по п. 5.01, к клинерам — по п. 5.08.");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол DNS-кэша успешно скопирован в буфер обмена!", "DNS Кэш", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
