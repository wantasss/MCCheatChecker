using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class NetworkBypassViewerForm : Form
	{
		#region Models

		public enum BypassFilterMode
		{
			All,
			ThreatsOnly,
			HostsOnly,
			DoHOnly,
			ProxiesAndVpn,
			CleanOnly
		}

		public class NetworkBypassEntry
		{
			public string Category; // "Файл Hosts", "DNS-over-HTTPS (DoH)", "Системный Прокси", "VPN / Сетевой туннель"
			public string Target;
			public string Value;
			public string Status;
			public bool IsThreat;
			public bool IsWarning;
			public bool IsClean;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private BypassFilterMode currentFilter = BypassFilterMode.All;
		private List<NetworkBypassEntry> allEntries = new List<NetworkBypassEntry>();
		private bool isScanning = false;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;
		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;
		private Label lblDetailVerdict;
		private Label lblDetailRule;
		private Label lblDetailBanCommand;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterThreats;
		private DarkButton btnFilterHosts;
		private DarkButton btnFilterDoH;
		private DarkButton btnFilterProxyVpn;
		private DarkButton btnFilterClean;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenHosts;
		private DarkButton btnOpenDnsCache;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly string[] SuspiciousHostsDomains = new string[]
		{
			// Домены чит-клиентов и сервисов
			"vape.gg", "celestial.lol", "nursultan.fun", "expensive.fun", "deadcode.fun",
			"wildclient.ru", "extremecheat.org", "liquidbounce.net", "akrien.club", "wurstclient.net",
			"sigma-client.com", "moonclient.xyz", "riseclient.com", "intent.store", "novoline.lol",
			"tenacity.dev", "kura.pw", "drip.gg", "slinky.gg", "entropy.club", "dreamclient.fun",
			"zerodayclient.com", "exloader.net", "boze.dev", "meteorclient.com", "rusohack.ru",
			// Серверы авторизации Mojang / Microsoft (спуфинг аккаунтов / альт-генераторы)
			"authserver.mojang.com", "sessionserver.mojang.com", "api.mojang.com", "textures.minecraft.net",
			"thealtening.com", "easymc.io", "ely.by", "mcleaks.net", "altening.com",
			// Серверы для обхода античитов и телеметрии
			"funtime.su", "holyworld.ru", "reallyworld.ru", "spmc.su", "mstnw.net"
		};

		private static readonly string[] SuspiciousAdapterKeywords = new string[]
		{
			"tap", "tun", "wintun", "wireguard", "openvpn", "openconnect", "npcap", "winpcap",
			"warp", "cloudflare", "tailscale", "zerotier", "proxifier", "clash", "fiddler",
			"charles", "softether", "nordvpn", "surfshark", "mullvad", "windscribe", "hidemy",
			"adguard", "packet", "pcap", "sock"
		};

		#endregion

		#region UI Initialization

		public NetworkBypassViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Сетевые обходы, Hosts, DNS-over-HTTPS и Прокси/VPN — MCCheatChecker";
			this.Size = new Size(1160, 720);
			this.MinimumSize = new Size(950, 580);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.KeyPreview = true;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartScanAsync();
					e.Handled = true;
				}
			};

			// Верхняя панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🛡️ Сетевой аудит: Hosts, DNS-over-HTTPS (DoH), Прокси и Туннели";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Обнаружение подмены hosts, обхода DNS-кэша через DoH в браузерах, системных SOCKS/HTTP прокси и скрытых VPN-адаптеров.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование сетевых настроек...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(240, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 260, 22);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Панель фильтров и поиска
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все записи", BypassFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Угрозы (5.08 / 5.01)", BypassFilterMode.ThreatsOnly, ref bx, filterBar);
			btnFilterHosts = CreateFilterButton("📝 Файл Hosts", BypassFilterMode.HostsOnly, ref bx, filterBar);
			btnFilterDoH = CreateFilterButton("🔒 DNS-over-HTTPS", BypassFilterMode.DoHOnly, ref bx, filterBar);
			btnFilterProxyVpn = CreateFilterButton("🌐 Прокси и VPN", BypassFilterMode.ProxiesAndVpn, ref bx, filterBar);
			btnFilterClean = CreateFilterButton("🛡️ Чистые / Без угрозы", BypassFilterMode.CleanOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 230, 8);
			searchBox.Size = new Size(215, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 285, 12);
			lblSearch.AutoSize = true;
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			filterBar.Controls.Add(lblSearch);

			// Нижняя панель действий
			Panel bottomBar = new Panel();
			bottomBar.Dock = DockStyle.Bottom;
			bottomBar.Height = 48;
			bottomBar.BackColor = Ui.Surface;
			bottomBar.Padding = new Padding(14, 8, 14, 8);
			this.Controls.Add(bottomBar);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать команду бана";
			btnCopyBan.Font = Ui.FontTitle(9f, true);
			btnCopyBan.Location = new Point(14, 10);
			btnCopyBan.Size = new Size(220, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnOpenHosts = new DarkButton();
			btnOpenHosts.Text = "📝 Открыть hosts в Блокноте";
			btnOpenHosts.Font = Ui.FontBody(9f, false);
			btnOpenHosts.Location = new Point(242, 10);
			btnOpenHosts.Size = new Size(205, 30);
			btnOpenHosts.Click += BtnOpenHosts_Click;
			bottomBar.Controls.Add(btnOpenHosts);

			btnOpenDnsCache = new DarkButton();
			btnOpenDnsCache.Text = "⚡ DNS-кэш (Ctrl+E)";
			btnOpenDnsCache.Font = Ui.FontBody(9f, false);
			btnOpenDnsCache.Location = new Point(455, 10);
			btnOpenDnsCache.Size = new Size(160, 30);
			btnOpenDnsCache.Click += delegate
			{
				try
				{
					DnsCacheViewerForm dnsForm = new DnsCacheViewerForm(targetPlayerName);
					dnsForm.Show(this);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Не удалось открыть DNS кэш: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};
			bottomBar.Controls.Add(btnOpenDnsCache);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📄 Экспорт отчета в TXT";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(623, 10);
			btnExportReport.Size = new Size(175, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 Обновить (F5)";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 145, 10);
			btnRefreshNow.Size = new Size(130, 30);
			btnRefreshNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			bottomBar.Controls.Add(btnRefreshNow);

			// SplitContainer для таблицы и карточки подробностей
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// Таблица ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Font = Ui.FontBody(9.5f, false);

			listView.Columns.Add("Категория", 150);
			listView.Columns.Add("Параметр / Домен", 240);
			listView.Columns.Add("Значение / Перенаправление", 220);
			listView.Columns.Add("Статус / Вердикт", 260);
			listView.Columns.Add("Пункт правил", 120);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей внизу
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ ДЕТАЛИ И ВЕРДИКТ РЕГЛАМЕНТА:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из списка выше для просмотра сведений.";
			lblDetailVerdict.Font = Ui.FontTitle(10.5f, true);
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailVerdict.Location = new Point(16, 32);
			lblDetailVerdict.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailVerdict);

			lblDetailRule = new Label();
			lblDetailRule.Text = "";
			lblDetailRule.Font = Ui.FontTitle(9f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Location = new Point(16, 56);
			lblDetailRule.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailRule);

			lblDetailBanCommand = new Label();
			lblDetailBanCommand.Text = "";
			lblDetailBanCommand.Font = Ui.FontMono(9f, true);
			lblDetailBanCommand.ForeColor = Ui.AccentTeal;
			lblDetailBanCommand.Location = new Point(16, 78);
			lblDetailBanCommand.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailBanCommand);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(16, 104);
			txtDetails.Size = new Size(pnlDetailsContainer.Width - 32, pnlDetailsContainer.Height - 114);
			txtDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			txtDetails.Font = Ui.FontMono(9f, false);
			txtDetails.ReadOnly = true;
			pnlDetailsContainer.Controls.Add(txtDetails);
		}

		private DarkButton CreateFilterButton(string text, BypassFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == BypassFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(140, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 146;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == BypassFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == BypassFilterMode.ThreatsOnly);
			btnFilterHosts.AccentStyle = (currentFilter == BypassFilterMode.HostsOnly);
			btnFilterDoH.AccentStyle = (currentFilter == BypassFilterMode.DoHOnly);
			btnFilterProxyVpn.AccentStyle = (currentFilter == BypassFilterMode.ProxiesAndVpn);
			btnFilterClean.AccentStyle = (currentFilter == BypassFilterMode.CleanOnly);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<NetworkBypassEntry> results = new List<NetworkBypassEntry>();

				// 1. Аудит файла Hosts
				ScanHostsFile(results);

				// 2. Аудит DNS-over-HTTPS (DoH) в Windows и браузерах
				ScanDohSettings(results);

				// 3. Аудит системных прокси (WinINET & WinHTTP)
				ScanProxySettings(results);

				// 4. Аудит активных сетевых адаптеров (VPN / Tun / Tap / Npcap)
				ScanNetworkAdapters(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(NetworkBypassEntry x) { return x.IsThreat; });
					int warningCount = results.Count(delegate(NetworkBypassEntry x) { return x.IsWarning; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УГРОЗ: {0} | ПРЕДУПРЕЖДЕНИЙ: {1}", threatCount, warningCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else if (warningCount > 0)
					{
						lblStatusPill.Text = string.Format("⚠️ ПРЕДУПРЕЖДЕНИЙ: {0}", warningCount);
						lblStatusPill.ForeColor = Ui.AccentOrange;
						lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);
					}
					else
					{
						lblStatusPill.Text = "🛡️ Сеть и Hosts чисты";
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanHostsFile(List<NetworkBypassEntry> results)
		{
			try
			{
				string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string hostsPath = Path.Combine(windir, @"System32\drivers\etc\hosts");

				// Проверка редиректа пути к hosts в реестре
				try
				{
					using (RegistryKey tcpKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
					{
						if (tcpKey != null)
						{
							object dbPathObj = tcpKey.GetValue("DataBasePath");
							if (dbPathObj != null)
							{
								string dbPath = dbPathObj.ToString();
								string expected = @"%SystemRoot%\System32\drivers\etc";
								if (!dbPath.Equals(expected, StringComparison.OrdinalIgnoreCase) &&
									!dbPath.Equals(Path.Combine(windir, @"System32\drivers\etc"), StringComparison.OrdinalIgnoreCase))
								{
									results.Add(new NetworkBypassEntry
									{
										Category = "Файл Hosts",
										Target = "DataBasePath в реестре",
										Value = dbPath,
										Status = "🚨 ПОДМЕНА ПУТИ К HOSTS В РЕЕСТРЕ",
										IsThreat = true,
										RuleViolation = "5.08",
										BanReason = "Подмена системного пути к файлу hosts для сокрытия перенаправлений",
										ForeColor = Ui.AccentRed,
										Details = string.Format("Ключ реестра HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\DataBasePath указывает на поддельную директорию: {0}.\nЭто грубое нарушение регламента 5.08 (сокрытие улик и обход проверок).", dbPath)
									});
								}
							}
						}
					}
				}
				catch { }

				if (!File.Exists(hostsPath))
				{
					results.Add(new NetworkBypassEntry
					{
						Category = "Файл Hosts",
						Target = hostsPath,
						Value = "Файл отсутствует",
						Status = "⚠️ HOSTS ОТСУТСТВУЕТ",
						IsWarning = true,
						RuleViolation = "5.08 (Подозрение)",
						BanReason = "Удаление файла hosts",
						ForeColor = Ui.AccentOrange,
						Details = "Системный файл hosts не найден на диске. Возможно, файл был удален перед проверкой для очистки следов."
					});
					return;
				}

				FileInfo fi = new FileInfo(hostsPath);
				string hostsMeta = string.Format("Размер: {0} байт, Изменен: {1:yyyy-MM-dd HH:mm:ss}", fi.Length, fi.LastWriteTime);

				string[] lines = File.ReadAllLines(hostsPath);
				int hostsEntriesFound = 0;

				for (int i = 0; i < lines.Length; i++)
				{
					string rawLine = lines[i].Trim();
					if (string.IsNullOrEmpty(rawLine)) continue;

					bool isCommented = rawLine.StartsWith("#");
					string line = isCommented ? rawLine.TrimStart('#').Trim() : rawLine;

					// Парсинг IP и имени хоста
					string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length >= 2)
					{
						string ip = tokens[0].Trim();
						for (int t = 1; t < tokens.Length; t++)
						{
							string domain = tokens[t].Trim().ToLowerInvariant();
							if (domain.StartsWith("#")) break;

							// Игнорируем стандартные записи localhost
							if ((domain == "localhost" || domain == "localhost.localdomain" || domain == "broadcasthost") &&
								(ip == "127.0.0.1" || ip == "::1"))
							{
								continue;
							}

							hostsEntriesFound++;
							bool isSuspiciousCheat = false;
							bool isMojangAuth = false;
							bool isServerDomain = false;

							foreach (string cheatDomain in SuspiciousHostsDomains)
							{
								if (domain.Contains(cheatDomain))
								{
									if (cheatDomain.Contains("mojang") || cheatDomain.Contains("altening") || cheatDomain.Contains("easymc") || cheatDomain.Contains("ely.by"))
										isMojangAuth = true;
									else if (cheatDomain.Contains("funtime") || cheatDomain.Contains("reallyworld") || cheatDomain.Contains("holyworld") || cheatDomain.Contains("mstnw"))
										isServerDomain = true;
									else
										isSuspiciousCheat = true;
									break;
								}
							}

							if (isSuspiciousCheat)
							{
								results.Add(new NetworkBypassEntry
								{
									Category = "Файл Hosts",
									Target = domain,
									Value = string.Format("{0} -> {1}{2}", domain, ip, isCommented ? " (Закомментировано #)" : ""),
									Status = isCommented ? "⚠️ ЧИТ В HOSTS (ЗАКОММЕНТИРОВАН #)" : "🚨 БЛОКИРОВКА / ПОДМЕНА ЧИТА В HOSTS (5.08 / 5.01)",
									IsThreat = !isCommented,
									IsWarning = isCommented,
									RuleViolation = isCommented ? "5.08 (Улика)" : "5.08 / 5.01",
									BanReason = string.Format("Модификация hosts: перенаправление чита {0} на {1}", domain, ip),
									ForeColor = isCommented ? Ui.AccentOrange : Ui.AccentRed,
									Details = string.Format("Строка #{0}: {1}\nДомен чита: {2}\nПеренаправлен на: {3}\nСтатус комментария: {4}\n{5}\nНазначение: Блокировка лицензионных проверок чита, кряк или скрытие обращений к чит-серверам.",
										i + 1, rawLine, domain, ip, isCommented ? "Закомментирован игроком" : "Активен", hostsMeta)
								});
							}
							else if (isMojangAuth)
							{
								results.Add(new NetworkBypassEntry
								{
									Category = "Файл Hosts",
									Target = domain,
									Value = string.Format("{0} -> {1}{2}", domain, ip, isCommented ? " (Закомментировано #)" : ""),
									Status = "🚨 ПОДМЕНА СЕРВЕРА АВТОРИЗАЦИИ MINECRAFT (5.08 / 5.01)",
									IsThreat = true,
									RuleViolation = "5.08 / 5.01",
									BanReason = string.Format("Модификация hosts: подмена авторизации {0} на {1}", domain, ip),
									ForeColor = Ui.AccentRed,
									Details = string.Format("Строка #{0}: {1}\nСлужба авторизации Mojang: {2}\nПодменена на: {3}\n{4}\nНазначение: Использование генераторов пиратских/ворованных аккаунтов (TheAltening/EasyMC) для обхода банов на сервере.",
										i + 1, rawLine, domain, ip, hostsMeta)
								});
							}
							else if (isServerDomain)
							{
								results.Add(new NetworkBypassEntry
								{
									Category = "Файл Hosts",
									Target = domain,
									Value = string.Format("{0} -> {1}", domain, ip),
									Status = "🚨 ПОДМЕНА ДОМЕНА ИГРОВОГО СЕРВЕРА В HOSTS",
									IsThreat = true,
									RuleViolation = "5.08 / 2.06",
									BanReason = string.Format("Модификация hosts: перенаправление сервера {0} на {1}", domain, ip),
									ForeColor = Ui.AccentRed,
									Details = string.Format("Строка #{0}: {1}\nДомен сервера: {2} перенаправлен на {3}.\nНазначение: Перехват игровых пакетов или поддельный прокси-сервер.", i + 1, rawLine, domain, ip)
								});
							}
							else if (ip == "127.0.0.1" || ip == "0.0.0.0")
							{
								// Прочая блокировка домена
								results.Add(new NetworkBypassEntry
								{
									Category = "Файл Hosts",
									Target = domain,
									Value = string.Format("{0} -> {1}{2}", domain, ip, isCommented ? " (#)" : ""),
									Status = isCommented ? "Закомментированная запись hosts" : "Пользовательская запись hosts",
									IsClean = true,
									RuleViolation = "-",
									BanReason = "-",
									ForeColor = Ui.TextSecondary,
									Details = string.Format("Строка #{0}: {1}\n{2}", i + 1, rawLine, hostsMeta)
								});
							}
						}
					}
				}

				if (hostsEntriesFound == 0)
				{
					results.Add(new NetworkBypassEntry
					{
						Category = "Файл Hosts",
						Target = "C:\\Windows\\System32\\drivers\\etc\\hosts",
						Value = "Стандартный системный hosts",
						Status = "🛡️ Файл hosts чист (без сторонних редиректов)",
						IsClean = true,
						RuleViolation = "-",
						BanReason = "-",
						ForeColor = Ui.AccentGreen,
						Details = string.Format("Файл hosts полностью соответствует стандартному виду Windows.\n{0}", hostsMeta)
					});
				}
			}
			catch (Exception ex)
			{
				results.Add(new NetworkBypassEntry
				{
					Category = "Файл Hosts",
					Target = "Ошибка чтения",
					Value = ex.Message,
					Status = "⚠️ Ошибка доступа к hosts",
					IsWarning = true,
					RuleViolation = "-",
					BanReason = "-",
					ForeColor = Ui.AccentOrange,
					Details = ex.ToString()
				});
			}
		}

		private void ScanDohSettings(List<NetworkBypassEntry> results)
		{
			// 1. Проверка системного DNS-over-HTTPS в Windows 11
			try
			{
				using (RegistryKey dohKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters"))
				{
					if (dohKey != null)
					{
						object autoDohObj = dohKey.GetValue("EnableAutoDoh");
						if (autoDohObj != null)
						{
							int autoDoh = Convert.ToInt32(autoDohObj);
							if (autoDoh > 0)
							{
								results.Add(new NetworkBypassEntry
								{
									Category = "DNS-over-HTTPS (DoH)",
									Target = "Windows 11 Системный DoH",
									Value = string.Format("EnableAutoDoh = {0}", autoDoh),
									Status = "⚠️ СИСТЕМНЫЙ DOH ВКЛЮЧЕН (DNS-кэш обходится)",
									IsWarning = true,
									RuleViolation = "5.08 (Внимание)",
									BanReason = "-",
									ForeColor = Ui.AccentOrange,
									Details = "В операционной системе Windows включен автоматический системный DNS-over-HTTPS (DoH).\nВсе DNS-запросы шифруются на уровне ОС и не попадают в стандартный кэш ipconfig /displaydns!\nМодератор должен вручную проверить историю браузеров и файлы RecentDocs."
								});
							}
						}
					}
				}
			}
			catch { }

			// 2. Проверка DoH в Google Chrome, Microsoft Edge, Opera, Yandex, Brave
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			CheckChromiumBrowserDoh("Google Chrome", Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Preferences"), results);
			CheckChromiumBrowserDoh("Microsoft Edge", Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Preferences"), results);
			CheckChromiumBrowserDoh("Opera GX", Path.Combine(appData, @"Opera Software\Opera GX Stable\Preferences"), results);
			CheckChromiumBrowserDoh("Opera Stable", Path.Combine(appData, @"Opera Software\Opera Stable\Preferences"), results);
			CheckChromiumBrowserDoh("Yandex Browser", Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data\Default\Preferences"), results);
			CheckChromiumBrowserDoh("Brave Browser", Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Preferences"), results);

			// 3. Проверка Mozilla Firefox
			CheckFirefoxDoh(Path.Combine(appData, @"Mozilla\Firefox\Profiles"), results);
		}

		private void CheckChromiumBrowserDoh(string browserName, string prefsPath, List<NetworkBypassEntry> results)
		{
			try
			{
				if (!File.Exists(prefsPath)) return;
				string content = File.ReadAllText(prefsPath);

				if (content.IndexOf("\"dns_over_https\"", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					bool isSecure = content.IndexOf("\"secure\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
									content.IndexOf("\"automatic\"", StringComparison.OrdinalIgnoreCase) >= 0;

					results.Add(new NetworkBypassEntry
					{
						Category = "DNS-over-HTTPS (DoH)",
						Target = browserName,
						Value = prefsPath,
						Status = isSecure ? "⚠️ DOH ВКЛЮЧЕН В БРАУЗЕРЕ (Кэш скрыт)" : "DOH настроен в профиле браузера",
						IsWarning = isSecure,
						IsClean = !isSecure,
						RuleViolation = isSecure ? "5.08 (Внимание)" : "-",
						BanReason = "-",
						ForeColor = isSecure ? Ui.AccentOrange : Ui.TextSecondary,
						Details = string.Format("Браузер: {0}\nФайл настроек: {1}\nВ настройках браузера активирован защищенный протокол DNS-over-HTTPS (DoH).\nЭто означает, что запросы на скачивание читов и открытие чит-сайтов в этом браузере НЕ СОХРАНЯЮТСЯ в системном DNS-кэше Windows!\nРекомендуется проверить историю загрузок браузера напрямую через инструмент 'Браузер & PS' (Ctrl+T).",
							browserName, prefsPath)
					});
				}
			}
			catch { }
		}

		private void CheckFirefoxDoh(string profilesDir, List<NetworkBypassEntry> results)
		{
			try
			{
				if (!Directory.Exists(profilesDir)) return;
				string[] prefFiles = Directory.GetFiles(profilesDir, "prefs.js", SearchOption.AllDirectories);

				foreach (string file in prefFiles)
				{
					string content = File.ReadAllText(file);
					if (content.Contains("network.trr.mode"))
					{
						// Mode 2 или 3 означает включенный DoH (TRR)
						Match m = Regex.Match(content, @"user_pref\(""network\.trr\.mode"",\s*([1-5])\);");
						if (m.Success)
						{
							int mode = int.Parse(m.Groups[1].Value);
							if (mode == 2 || mode == 3)
							{
								results.Add(new NetworkBypassEntry
								{
									Category = "DNS-over-HTTPS (DoH)",
									Target = "Mozilla Firefox",
									Value = string.Format("network.trr.mode = {0}", mode),
									Status = "⚠️ DOH ВКЛЮЧЕН В FIREFOX (Кэш скрыт)",
									IsWarning = true,
									RuleViolation = "5.08 (Внимание)",
									BanReason = "-",
									ForeColor = Ui.AccentOrange,
									Details = string.Format("Профиль Firefox: {0}\nПараметр network.trr.mode установлен в {1} (DNS-over-HTTPS активен).\nЗапросы на загрузку читов идут через зашифрованный туннель Cloudflare/NextDNS и скрыты от ipconfig /displaydns.",
										file, mode)
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanProxySettings(List<NetworkBypassEntry> results)
		{
			// WinINET Системный прокси
			try
			{
				using (RegistryKey inetKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
				{
					if (inetKey != null)
					{
						object proxyEnableObj = inetKey.GetValue("ProxyEnable");
						int proxyEnable = (proxyEnableObj != null) ? Convert.ToInt32(proxyEnableObj) : 0;
						object proxyServerObj = inetKey.GetValue("ProxyServer");
						string proxyServer = (proxyServerObj != null) ? proxyServerObj.ToString() : string.Empty;
						object autoConfigUrlObj = inetKey.GetValue("AutoConfigURL");
						string autoConfigUrl = (autoConfigUrlObj != null) ? autoConfigUrlObj.ToString() : string.Empty;

						if (proxyEnable > 0 && !string.IsNullOrEmpty(proxyServer))
						{
							bool isLocalProxy = proxyServer.Contains("127.0.0.1") || proxyServer.Contains("localhost");
							results.Add(new NetworkBypassEntry
							{
								Category = "Системный Прокси",
								Target = "WinINET Proxy",
								Value = proxyServer,
								Status = isLocalProxy ? "🚨 АКТИВНЫЙ ЛОКАЛЬНЫЙ ПРОКСИ (ПЕРЕХВАТ ТРАФИКА / SOCKS)" : "⚠️ АКТИВНЫЙ СИСТЕМНЫЙ ПРОКСИ",
								IsThreat = isLocalProxy,
								IsWarning = !isLocalProxy,
								RuleViolation = isLocalProxy ? "5.08 / 5.01" : "5.08",
								BanReason = string.Format("Активный локальный прокси-сервер {0} для перехвата сетевого трафика / спуфинга", proxyServer),
								ForeColor = isLocalProxy ? Ui.AccentRed : Ui.AccentOrange,
								Details = string.Format("В реестре HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings включен системный прокси: {0}.\nЕсли прокси слушает 127.0.0.1 или порт 1080/8080/8888, это признак инжекции трафика через Fiddler, Charles, BurpSuite, Proxifier или кастомный прокси-чит.", proxyServer)
							});
						}

						if (!string.IsNullOrEmpty(autoConfigUrl))
						{
							results.Add(new NetworkBypassEntry
							{
								Category = "Системный Прокси",
								Target = "PAC Script (AutoConfigURL)",
								Value = autoConfigUrl,
								Status = "⚠️ АВТОМАТИЧЕСКИЙ ПРОКСИ-СКРИПТ (PAC)",
								IsWarning = true,
								RuleViolation = "5.08",
								BanReason = "Использование PAC-скрипта автоконфигурации прокси",
								ForeColor = Ui.AccentOrange,
								Details = string.Format("Установлен скрипт автоконфигурации прокси: {0}.\nПозволяет динамически маршрутизировать домены читов и серверов в обход проверок.", autoConfigUrl)
							});
						}
					}
				}
			}
			catch { }
		}

		private void ScanNetworkAdapters(List<NetworkBypassEntry> results)
		{
			try
			{
				NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in interfaces)
				{
					string name = nic.Name.ToLowerInvariant();
					string desc = nic.Description.ToLowerInvariant();
					bool isSuspicious = false;
					string matchedKeyword = string.Empty;

					foreach (string kw in SuspiciousAdapterKeywords)
					{
						if (name.Contains(kw) || desc.Contains(kw))
						{
							isSuspicious = true;
							matchedKeyword = kw;
							break;
						}
					}

					if (isSuspicious)
					{
						bool isUp = (nic.OperationalStatus == OperationalStatus.Up);
						results.Add(new NetworkBypassEntry
						{
							Category = "VPN / Сетевой туннель",
							Target = nic.Name,
							Value = nic.Description,
							Status = isUp ? "🚨 АКТИВНЫЙ СЕТЕВОЙ ТУННЕЛЬ / ПАКЕТНЫЙ ПЕРЕХВАТЧИК" : "Отключенный виртуальный сетевой адаптер",
							IsThreat = isUp,
							IsWarning = !isUp,
							IsClean = false,
							RuleViolation = isUp ? "5.08 / 2.06" : "-",
							BanReason = string.Format("Использование активного сетевого туннеля / перехватчика {0} ({1})", nic.Name, matchedKeyword),
							ForeColor = isUp ? Ui.AccentRed : Ui.TextSecondary,
							Details = string.Format("Сетевой адаптер: {0}\nОписание: {1}\nТип: {2}\nСтатус работы: {3}\nСигнатура: {4}\nНазначение: Перенаправление трафика через виртуальный туннель, Npcap перехват пакетов или VPN для сокрытия сетевого следа чита.",
								nic.Name, nic.Description, nic.NetworkInterfaceType, nic.OperationalStatus, matchedKeyword)
						});
					}
				}
			}
			catch (Exception ex)
			{
				results.Add(new NetworkBypassEntry
				{
					Category = "VPN / Сетевой туннель",
					Target = "Ошибка опроса адаптеров",
					Value = ex.Message,
					Status = "⚠️ Ошибка WMI/Network",
					IsWarning = true,
					RuleViolation = "-",
					BanReason = "-",
					ForeColor = Ui.AccentOrange,
					Details = ex.ToString()
				});
			}
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (NetworkBypassEntry entry in allEntries)
			{
				// Фильтр по режимам
				if (currentFilter == BypassFilterMode.ThreatsOnly && !entry.IsThreat) continue;
				if (currentFilter == BypassFilterMode.HostsOnly && entry.Category != "Файл Hosts") continue;
				if (currentFilter == BypassFilterMode.DoHOnly && entry.Category != "DNS-over-HTTPS (DoH)") continue;
				if (currentFilter == BypassFilterMode.ProxiesAndVpn && entry.Category != "Системный Прокси" && entry.Category != "VPN / Сетевой туннель") continue;
				if (currentFilter == BypassFilterMode.CleanOnly && !entry.IsClean) continue;

				// Поиск
				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (entry.Category != null && entry.Category.ToLowerInvariant().Contains(search)) ||
								   (entry.Target != null && entry.Target.ToLowerInvariant().Contains(search)) ||
								   (entry.Value != null && entry.Value.ToLowerInvariant().Contains(search)) ||
								   (entry.Status != null && entry.Status.ToLowerInvariant().Contains(search)) ||
								   (entry.Details != null && entry.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem item = new ListViewItem(entry.Category);
				item.SubItems.Add(entry.Target);
				item.SubItems.Add(entry.Value);
				item.SubItems.Add(entry.Status);
				item.SubItems.Add(entry.RuleViolation ?? "-");
				item.ForeColor = entry.ForeColor;
				item.Tag = entry;

				listView.Items.Add(item);
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

			NetworkBypassEntry entry = listView.SelectedItems[0].Tag as NetworkBypassEntry;
			if (entry == null) return;

			lblDetailVerdict.Text = entry.Status;
			lblDetailVerdict.ForeColor = entry.ForeColor;

			if (!string.IsNullOrEmpty(entry.RuleViolation) && entry.RuleViolation != "-")
			{
				lblDetailRule.Text = string.Format("⚖️ Пункт регламента: {0} ({1})", entry.RuleViolation, entry.BanReason);
				lblDetailBanCommand.Text = string.Format("📋 Команда: /ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason);
			}
			else
			{
				lblDetailRule.Text = "🛡️ Нарушений не выявлено (Параметр легитимен)";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = entry.Details ?? "Нет дополнительных сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Записи не выбраны";
			lblDetailVerdict.ForeColor = Ui.TextPrimary;
			lblDetailRule.Text = "";
			lblDetailBanCommand.Text = "";
			txtDetails.Text = "";
		}

		#endregion

		#region Actions

		private void BtnCopyBan_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением из таблицы для копирования команды бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			NetworkBypassEntry entry = listView.SelectedItems[0].Tag as NetworkBypassEntry;
			if (entry == null || string.IsNullOrEmpty(entry.RuleViolation) || entry.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи не предусмотрено наказание (нарушений нет).", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана успешно скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenHosts_Click(object sender, EventArgs e)
		{
			try
			{
				string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string hostsPath = Path.Combine(windir, @"System32\drivers\etc\hosts");
				if (File.Exists(hostsPath))
				{
					Process.Start("notepad.exe", hostsPath);
				}
				else
				{
					MessageBox.Show("Файл hosts не найден по стандартному пути: " + hostsPath, "Hosts не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть файл hosts: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Сетевой_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ СЕТЕВОГО АУДИТА: HOSTS, DOH И ПРОКСИ");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (NetworkBypassEntry entry in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", entry.Category, entry.Status));
						sb.AppendLine(string.Format("Цель/Домен:  {0}", entry.Target));
						sb.AppendLine(string.Format("Значение:    {0}", entry.Value));
						if (!string.IsNullOrEmpty(entry.RuleViolation) && entry.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", entry.RuleViolation, entry.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, entry.RuleViolation, entry.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", entry.Details));
						sb.AppendLine(new string('-', 80));
					}

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					MessageBox.Show("Отчет успешно сохранен:\n" + sfd.FileName, "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при экспорте отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
