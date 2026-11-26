using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class NetworkTunnelsAndCertificatesViewerForm : Form
	{
		#region Data Models

		public enum NetFilterCategory
		{
			All,
			ViolationsOnly,
			RootCertificates,
			NetworkAdapters,
			Whitelisted
		}

		public class NetAuditItem
		{
			public int Index;
			public string TargetName;
			public string ArtifactType; // "Корневой сертификат (Root CA)", "Сетевой адаптер (TUN/TAP)", "WinINet Прокси"
			public NetFilterCategory Category;
			public string Status; // "Активен", "Установлен", "Перехват"
			public string Verdict;
			public string TargetRule;
			public string Details;
			public string ThumbprintOrMac;
			public bool IsViolation;
			public bool IsAllowed;
			public Color RowColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private NetFilterCategory currentFilter = NetFilterCategory.All;
		private List<NetAuditItem> allItems = new List<NetAuditItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiCerts;
		private Label lblKpiViolations;
		private Label lblKpiAdapters;
		private Label lblKpiAllowed;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCerts;
		private DarkButton btnFilterAdapters;
		private DarkButton btnFilterAllowed;

		private DarkButton btnCopyDetails;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public NetworkTunnelsAndCertificatesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "🌐 Аудит сетевых туннелей, VPN/TUN адаптеров и корневых сертификатов перехвата — MCCheatChecker";
			Size = new Size(1240, 760);
			MinimumSize = new Size(1000, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🌐 Аудит сетевых туннелей (Wintun/WireGuard), прокси и корневых сертификатов перехвата (MitM)";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сетевая криминалистика: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1200, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotal = CreateKpiLabel("Всего объектов: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiCerts = CreateKpiLabel("Корневых сертификатов: 0", Ui.AccentTeal, 180, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик перехвата (5.01): 0", Ui.AccentRed, 410, kpiPanel);
			lblKpiAdapters = CreateKpiLabel("Адаптеров TUN/TAP: 0", Ui.AccentOrange, 620, kpiPanel);
			lblKpiAllowed = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 840, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все объекты", NetFilterCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Только нарушения (5.01)", NetFilterCategory.ViolationsOnly, 126, filterPanel);
			btnFilterCerts = CreateFilterButton("🔏 Корневые сертификаты CA", NetFilterCategory.RootCertificates, 330, filterPanel);
			btnFilterAdapters = CreateFilterButton("🔌 Сетевые адаптеры (TUN/TAP)", NetFilterCategory.NetworkAdapters, 544, filterPanel);
			btnFilterAllowed = CreateFilterButton("✅ Белый список (Allowed)", NetFilterCategory.Whitelisted, 768, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(960, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(1015, 9);
			searchBox.Size = new Size(180, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			Controls.Add(filterPanel);

			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
			split.BackColor = Ui.Backdrop;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Объект / Сертификат / Адаптер", 240);
			listView.Columns.Add("Тип артефакта", 160);
			listView.Columns.Add("Статус", 110);
			listView.Columns.Add("Вердикт", 220);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Отпечаток / MAC адрес", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Детали сертификата перехвата трафика или конфигурация адаптера:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(12, 30);
			txtDetails.Size = new Size(860, 130);
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			bottomContainer.Controls.Add(txtDetails);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(885, 20);
			actionsPanel.Size = new Size(330, 145);

			btnCopyDetails = CreateActionButton("📋 Копировать детали доказательства", 0, actionsPanel);
			btnCopyDetails.Click += delegate { CopyDetailsText(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 32, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана (5.01)", 64, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(0, 106);
			btnRescan.Size = new Size(320, 26);
			btnRescan.Click += delegate { StartScan(); };
			actionsPanel.Controls.Add(btnRescan);

			bottomContainer.Controls.Add(actionsPanel);

			split.Panel2.Controls.Add(bottomContainer);
			Controls.Add(split);

			filterPanel.BringToFront();
			split.BringToFront();
		}

		private Label CreateKpiLabel(string text, Color color, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.Font = Ui.FontBody(9f, true);
			lbl.ForeColor = color;
			lbl.BackColor = Ui.SurfaceAlt;
			lbl.AutoSize = true;
			lbl.Padding = new Padding(8, 4, 8, 4);
			lbl.Location = new Point(x, 4);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, NetFilterCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == NetFilterCategory.ViolationsOnly ? 198 : (cat == NetFilterCategory.RootCertificates ? 208 : (cat == NetFilterCategory.NetworkAdapters ? 218 : (cat == NetFilterCategory.Whitelisted ? 190 : 108))), 28);
			btn.Font = Ui.FontBody(8.5f, true);
			btn.Click += delegate
			{
				currentFilter = cat;
				ResetFilterButtonColors();
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			if (cat == NetFilterCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterCerts, btnFilterAdapters, btnFilterAllowed };
			foreach (DarkButton b in btns)
			{
				b.BackColor = Ui.Surface;
				b.ForeColor = Ui.TextPrimary;
			}
		}

		private DarkButton CreateActionButton(string text, int y, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(0, y);
			btn.Size = new Size(320, 26);
			btn.Font = Ui.FontBody(8f, true);
			parent.Controls.Add(btn);
			return btn;
		}

		#region Scan Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Сканирование сетевых туннелей, VPN/TUN адаптеров и сертификатов Root CA...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<NetAuditItem> list = new List<NetAuditItem>();
			int idx = 1;

			// 1. Audit Trusted Root Certification Authorities (CurrentUser & LocalMachine)
			ScanRootCertificates(list, StoreLocation.CurrentUser, ref idx);
			ScanRootCertificates(list, StoreLocation.LocalMachine, ref idx);

			// 2. Audit Network Interfaces & Virtual TUN/TAP Adapters
			ScanNetworkInterfaces(list, ref idx);

			// 3. Audit WinINet Proxy & Environment Variables
			ScanProxySettings(list, ref idx);

			e.Result = list;
		}

		private void ScanRootCertificates(List<NetAuditItem> list, StoreLocation loc, ref int idx)
		{
			try
			{
				using (X509Store store = new X509Store(StoreName.Root, loc))
				{
					store.Open(OpenFlags.ReadOnly);
					foreach (X509Certificate2 cert in store.Certificates)
					{
						string subj = cert.Subject.ToLowerInvariant();
						string issuer = cert.Issuer.ToLowerInvariant();

						// Check for rogue sniffer / MitM certificates
						string[] snifferNames = new string[]
						{
							"fiddler", "charles", "mitmproxy", "burp", "portswigger",
							"do_not_trust", "httpcanary", "proxyman", "wiremock", "cheat",
							"packetfly", "desync"
						};

						bool isSniffer = false;
						string matchedName = "";

						foreach (string sn in snifferNames)
						{
							if (subj.Contains(sn) || issuer.Contains(sn))
							{
								isSniffer = true;
								matchedName = sn;
								break;
							}
						}

						if (isSniffer)
						{
							NetAuditItem it = new NetAuditItem();
							it.Index = idx++;
							it.TargetName = cert.GetNameInfo(X509NameType.SimpleName, false);
							it.ArtifactType = "Корневой сертификат (Root CA)";
							it.Category = NetFilterCategory.ViolationsOnly;
							it.Status = "Установлен в Root CA (" + loc + ")";
							it.IsViolation = true;
							it.Verdict = "🚨 СЕРТИФИКАТ ПЕРЕХВАТА ТРАФИКА / SNIFFER (5.01)";
							it.TargetRule = "5.01 (Перехват сессий)";
							it.ThumbprintOrMac = cert.Thumbprint;
							it.RowColor = Ui.AccentRed;

							StringBuilder sb = new StringBuilder();
							sb.AppendLine("=== ВНИМАНИЕ: ОБНАРУЖЕН КОРНЕВОЙ СЕРТИФИКАТ СНИФФЕРА ===");
							sb.AppendLine("Имя: " + it.TargetName);
							sb.AppendLine("Хранилище: " + loc + "\\Root (Доверенные корневые центры)");
							sb.AppendLine("Владелец: " + cert.Subject);
							sb.AppendLine("Издатель: " + cert.Issuer);
							sb.AppendLine("Отпечаток: " + cert.Thumbprint);
							sb.AppendLine("Срок действия: с " + cert.NotBefore.ToShortDateString() + " по " + cert.NotAfter.ToShortDateString());
							sb.AppendLine();
							sb.AppendLine("Опасность криминалистики:");
							sb.AppendLine("Данный сертификат позволяет снифферу (" + matchedName + ") расшифровывать HTTPS соединения Minecraft, подменять пакеты или перехватывать сессионные токены игроков.");
							it.Details = sb.ToString();

							list.Add(it);
						}
					}
				}
			}
			catch { }
		}

		private void ScanNetworkInterfaces(List<NetAuditItem> list, ref int idx)
		{
			try
			{
				NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in adapters)
				{
					string name = nic.Name;
					string desc = nic.Description.ToLowerInvariant();
					string lowName = name.ToLowerInvariant();

					NetAuditItem it = new NetAuditItem();
					it.Index = idx++;
					it.TargetName = name + " (" + nic.Description + ")";
					it.ThumbprintOrMac = nic.GetPhysicalAddress().ToString();
					it.Status = nic.OperationalStatus.ToString();

					// Whitelist physical adapters: Intel, Realtek, MediaTek, Killer, Broadcom
					bool isPhysical = desc.Contains("intel") || desc.Contains("realtek") || desc.Contains("mediatek") ||
						desc.Contains("killer") || desc.Contains("broadcom") || desc.Contains("qualcomm") ||
						desc.Contains("wi-fi direct") || desc.Contains("bluetooth");

					// Virtual tunnels: Wintun, WireGuard, TAP, OpenVPN, Npcap, Proxifier
					string[] tunnelKeywords = new string[]
					{
						"wintun", "wireguard", "tap-windows", "openvpn", "npcap", "proxifier",
						"clarent", "zerotier", "tailscale", "hamachi", "radmin", "tun/tap"
					};

					bool isTunnel = false;
					string tunnelMatch = "";

					foreach (string tk in tunnelKeywords)
					{
						if (desc.Contains(tk) || lowName.Contains(tk))
						{
							isTunnel = true;
							tunnelMatch = tk;
							break;
						}
					}

					if (isTunnel)
					{
						it.ArtifactType = "Сетевой адаптер (TUN/TAP)";
						it.Category = NetFilterCategory.NetworkAdapters;
						it.IsViolation = false;
						it.Verdict = "⚠️ Виртуальный туннель / TAP (" + tunnelMatch + ")";
						it.TargetRule = "Аудит туннелей (5.02/5.07)";
						it.RowColor = Ui.AccentOrange;

						StringBuilder sb = new StringBuilder();
						sb.AppendLine("=== АКТИВНЫЙ ВИРТУАЛЬНЫЙ СЕТЕВОЙ АДАПТЕР ===");
						sb.AppendLine("Адаптер: " + it.TargetName);
						sb.AppendLine("Тип интерфейса: " + nic.NetworkInterfaceType);
						sb.AppendLine("Состояние: " + nic.OperationalStatus);
						sb.AppendLine("MAC-адрес: " + it.ThumbprintOrMac);
						sb.AppendLine();
						sb.AppendLine("Виртуальные адаптеры TUN/TAP могут использоваться для маршрутизации трафика чита (PacketFly, Desync, PingSpoof) или обхода банов по IP.");
						it.Details = sb.ToString();

						list.Add(it);
					}
					else if (isPhysical)
					{
						it.ArtifactType = "Физический адаптер";
						it.Category = NetFilterCategory.Whitelisted;
						it.IsAllowed = true;
						it.Verdict = "✅ Легитимный сетевой адаптер";
						it.TargetRule = "Штатное оборудование";
						it.RowColor = Ui.AccentGreen;
						it.Details = string.Format("Физический сетевой адаптер {0}.\nСтатус: {1}.\nMAC: {2}.", it.TargetName, it.Status, it.ThumbprintOrMac);
						list.Add(it);
					}
				}
			}
			catch { }
		}

		private void ScanProxySettings(List<NetAuditItem> list, ref int idx)
		{
			try
			{
				using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
				{
					if (rk != null)
					{
						object pEnable = rk.GetValue("ProxyEnable");
						object pServer = rk.GetValue("ProxyServer");

						if (pEnable != null && Convert.ToInt32(pEnable) == 1 && pServer != null)
						{
							NetAuditItem it = new NetAuditItem();
							it.Index = idx++;
							it.TargetName = "Системный WinINet Прокси: " + pServer;
							it.ArtifactType = "WinINet Прокси";
							it.Category = NetFilterCategory.ViolationsOnly;
							it.Status = "Включен (ProxyEnable=1)";
							it.IsViolation = true;
							it.Verdict = "⚠️ АКТИВНЫЙ СИСТЕМНЫЙ ПРОКСИ (Аудит 5.02)";
							it.TargetRule = "5.02 (Обход блокировок)";
							it.ThumbprintOrMac = pServer.ToString();
							it.RowColor = Ui.AccentOrange;
							it.Details = string.Format("В системе активирован постоянный прокси-сервер WinINet: {0}.\nВесь веб-трафик и запросы лаунчера перенаправляются через данный узел.", pServer);
							list.Add(it);
						}
					}
				}
			}
			catch { }
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сетевого аудита: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<NetAuditItem> res = e.Result as List<NetAuditItem>;
			if (res != null)
			{
				allItems = res;

				int certs = 0;
				int violations = 0;
				int adapters = 0;
				int allowed = 0;

				foreach (NetAuditItem it in allItems)
				{
					if (it.Category == NetFilterCategory.RootCertificates || it.IsViolation) certs++;
					if (it.IsViolation) violations++;
					if (it.Category == NetFilterCategory.NetworkAdapters) adapters++;
					if (it.IsAllowed) allowed++;
				}

				lblKpiTotal.Text = "Всего объектов: " + allItems.Count;
				lblKpiCerts.Text = "Корневых сертификатов: " + certs;
				lblKpiViolations.Text = "Улик перехвата (5.01): " + violations;
				lblKpiAdapters.Text = "Адаптеров TUN/TAP: " + adapters;
				lblKpiAllowed.Text = "Белый список: " + allowed;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Обнаружен корневой сертификат сниффера перехвата трафика (5.01)!");
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else if (adapters > 0)
				{
					lblStatusPill.Text = string.Format("⚠️ Зафиксировано {0} виртуальных туннельных адаптеров (TUN/TAP).", adapters);
					lblStatusPill.ForeColor = Ui.AccentOrange;
				}
				else
				{
					lblStatusPill.Text = "✅ Сетевой аудит завершен: сертификатов перехвата и подозрительных туннелей не обнаружено.";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & UI Updates

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : string.Empty;

			foreach (NetAuditItem it in allItems)
			{
				if (currentFilter == NetFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == NetFilterCategory.RootCertificates && it.Category != NetFilterCategory.RootCertificates && !it.IsViolation) continue;
				if (currentFilter == NetFilterCategory.NetworkAdapters && it.Category != NetFilterCategory.NetworkAdapters) continue;
				if (currentFilter == NetFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.TargetName.ToLowerInvariant().Contains(search) ||
						it.ArtifactType.ToLowerInvariant().Contains(search) ||
						it.Status.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.ThumbprintOrMac.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.TargetName);
				lvi.SubItems.Add(it.ArtifactType);
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.ThumbprintOrMac);

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
				txtDetails.Text = "Нет записей, соответствующих выбранному фильтру.";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			NetAuditItem it = listView.SelectedItems[0].Tag as NetAuditItem;
			if (it == null) return;

			txtDetails.Text = it.Details;
		}

		#endregion

		#region Actions

		private void CopyDetailsText()
		{
			if (listView.SelectedItems.Count == 0) return;
			NetAuditItem it = listView.SelectedItems[0].Tag as NetAuditItem;
			if (it == null) return;

			Clipboard.SetText(it.Details);
			MessageBox.Show("Детали скопированы в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			NetAuditItem it = listView.SelectedItems[0].Tag as NetAuditItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ СЕТЕВОЙ УЛИКИ / СЕРТИФИКАТА СНИФФЕРА ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Объект: " + it.TargetName);
			sb.AppendLine("Тип: " + it.ArtifactType);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт правил: " + it.TargetRule);
			sb.AppendLine("Отпечаток / MAC: " + it.ThumbprintOrMac);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			NetAuditItem it = listView.SelectedItems[0].Tag as NetAuditItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.01 Использование читов / Сниффер ({1})", targetPlayerName, it.TargetName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
