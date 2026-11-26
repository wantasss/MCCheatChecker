using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ActiveConnectionsViewerForm : Form
	{
		private enum SocketFilter
		{
			All,
			SuspiciousOnly,
			LocalhostIpcOnly,
			MinecraftOnly,
			RemoteServersOnly,
			WhitelistOnly
		}

		private class DetailedSocketItem
		{
			public int Index;
			public int Pid;
			public string ProcessName;
			public string ExePath;
			public string Protocol;
			public string LocalAddress;
			public int LocalPort;
			public string RemoteAddress;
			public int RemotePort;
			public string State;
			public bool IsSuspicious;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsMinecraft;
			public bool IsLocalhostIpc;
			public bool IsWhitelisted;
			public bool HasDetectedWebGui;
			public string WebGuiSnippet;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private SocketFilter currentFilter = SocketFilter.All;
		private List<DetailedSocketItem> allSockets = new List<DetailedSocketItem>();
		private Timer autoRefreshTimer;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailProcess;
		private Label lblDetailEndpoints;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;
		private DarkCheckBox chkAutoRefresh;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterSuspicious;
		private DarkButton btnFilterLocalhost;
		private DarkButton btnFilterMinecraft;
		private DarkButton btnFilterRemote;
		private DarkButton btnFilterWhitelist;
		private DarkButton btnProbeWebGui;

		private static readonly Regex CheatProcessPattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject)",
			RegexOptions.Compiled);

		public ActiveConnectionsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadSocketsAsync();
		}

		private void InitializeComponent()
		{
			Text = "🔌 Сетевые сокеты и Web-интерфейсы читов (TCP/UDP & IPC Sockets Inspector)";
			Size = new Size(1220, 760);
			MinimumSize = new Size(960, 560);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 114;
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
			lblHeaderTitle.Text = "🔌  СЕТЕВЫЕ СОКЕТЫ И WEB-ИНТЕРФЕЙСЫ ЧИТОВ (IPC & Remote Connections Forensics)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Мониторинг активных TCP/UDP соединений: выявление скрытых Web-GUI читов (Vape/Drip/Slinky), IPC портов и серверов авторизации...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование открытых сокетов...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Обновить";
			btnRefresh.Location = new Point(880, 8);
			btnRefresh.Size = new Size(110, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { LoadSocketsAsync(); };
			topPanel.Controls.Add(btnRefresh);

			btnProbeWebGui = new DarkButton();
			btnProbeWebGui.Text = "📡 Проверить Web-GUI чита";
			btnProbeWebGui.Location = new Point(998, 8);
			btnProbeWebGui.Size = new Size(195, 26);
			btnProbeWebGui.BackColor = Ui.AccentRed;
			btnProbeWebGui.ForeColor = Color.White;
			btnProbeWebGui.Font = Ui.FontBody(8.5f, true);
			btnProbeWebGui.Cursor = Cursors.Hand;
			btnProbeWebGui.Click += delegate { ProbeAllLocalhostWebGuis(); };
			topPanel.Controls.Add(btnProbeWebGui);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все сокеты (0)", SocketFilter.All, 130);
			btnFilterSuspicious = CreateFilterButton("🚨 Подозрительные & Читы (0)", SocketFilter.SuspiciousOnly, 200);
			btnFilterLocalhost = CreateFilterButton("🌐 Localhost IPC (0)", SocketFilter.LocalhostIpcOnly, 150);
			btnFilterMinecraft = CreateFilterButton("☕ Minecraft (javaw) (0)", SocketFilter.MinecraftOnly, 160);
			btnFilterRemote = CreateFilterButton("📡 Внешние серверы (0)", SocketFilter.RemoteServersOnly, 155);
			btnFilterWhitelist = CreateFilterButton("✔ Whitelist (0)", SocketFilter.WhitelistOnly, 120);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterSuspicious, btnFilterLocalhost, btnFilterMinecraft, btnFilterRemote, btnFilterWhitelist };
			foreach (DarkButton b in filterButtons)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 6;
			}

			chkAutoRefresh = new DarkCheckBox();
			chkAutoRefresh.Text = "⏱️ Авто (4 сек)";
			chkAutoRefresh.Location = new Point(curX + 15, 4);
			chkAutoRefresh.Size = new Size(125, 20);
			chkAutoRefresh.Font = Ui.FontBody(8f);
			chkAutoRefresh.CheckedChanged += delegate
			{
				if (chkAutoRefresh.Checked) autoRefreshTimer.Start();
				else autoRefreshTimer.Stop();
			};
			navRow.Controls.Add(chkAutoRefresh);

			topPanel.Controls.Add(navRow);

			Panel searchRow = new Panel();
			searchRow.Location = new Point(12, 85);
			searchRow.Size = new Size(1180, 24);
			searchRow.BackColor = Color.Transparent;

			Label lblSearch = new Label();
			lblSearch.Text = "Быстрый поиск:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(0, 3);
			lblSearch.AutoSize = true;
			searchRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(105, 0);
			searchBox.Size = new Size(360, 23);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			searchRow.Controls.Add(searchBox);

			DarkButton btnClearSearch = new DarkButton();
			btnClearSearch.Text = "✕ Очистить";
			btnClearSearch.Location = new Point(472, 0);
			btnClearSearch.Size = new Size(85, 23);
			btnClearSearch.BackColor = Ui.SurfaceAlt;
			btnClearSearch.Font = Ui.FontBody(8f);
			btnClearSearch.Cursor = Cursors.Hand;
			btnClearSearch.Click += delegate { searchBox.Text = ""; };
			searchRow.Controls.Add(btnClearSearch);

			Label lblHint = new Label();
			lblHint.Text = "💡 Подсказка: Кликните дважды для копирования команды бана. Нажмите «Проверить Web-GUI чита» для детекта скрытых веб-панелей.";
			lblHint.Font = Ui.FontBody(8f);
			lblHint.ForeColor = Ui.TextMuted;
			lblHint.Location = new Point(570, 4);
			lblHint.AutoSize = true;
			searchRow.Controls.Add(lblHint);

			topPanel.Controls.Add(searchRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail pane
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1180, 600);
			try { split.SplitterDistance = 420; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 210);
			listView.Columns.Add("Процесс", 145);
			listView.Columns.Add("PID", 65);
			listView.Columns.Add("Протокол", 75);
			listView.Columns.Add("Локальный адрес : Порт", 185);
			listView.Columns.Add("Удалённый адрес : Порт", 185);
			listView.Columns.Add("Состояние", 110);
			listView.Columns.Add("Исполняемый файл", 240);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Detail pane
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 10, 12, 10);

			lblDetailProcess = new Label();
			lblDetailProcess.Text = "Выберите сетевое подключение для анализа...";
			lblDetailProcess.Font = Ui.FontTitle(10f, true);
			lblDetailProcess.ForeColor = Ui.TextPrimary;
			lblDetailProcess.Location = new Point(12, 8);
			lblDetailProcess.AutoSize = true;
			detailContainer.Controls.Add(lblDetailProcess);

			lblDetailEndpoints = new Label();
			lblDetailEndpoints.Text = "Маршрут соединения: —";
			lblDetailEndpoints.Font = Ui.FontBody(8.5f);
			lblDetailEndpoints.ForeColor = Ui.TextSecondary;
			lblDetailEndpoints.Location = new Point(12, 30);
			lblDetailEndpoints.AutoSize = true;
			detailContainer.Controls.Add(lblDetailEndpoints);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Статус: —";
			lblDetailStatus.Font = Ui.FontBody(8.5f, true);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Location = new Point(12, 50);
			lblDetailStatus.AutoSize = true;
			detailContainer.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Пункт регламента: —";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.AccentTeal;
			lblDetailRule.Location = new Point(12, 70);
			lblDetailRule.AutoSize = true;
			detailContainer.Controls.Add(lblDetailRule);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Location = new Point(12, 94);
			txtDetailDescription.Size = new Size(820, 85);
			txtDetailDescription.Font = Ui.FontMono(8f);
			txtDetailDescription.ReadOnly = true;
			detailContainer.Controls.Add(txtDetailDescription);

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать /ban";
			btnCopyBan.Location = new Point(850, 94);
			btnCopyBan.Size = new Size(160, 27);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			detailContainer.Controls.Add(btnCopyBan);

			DarkButton btnOpenBrowser = new DarkButton();
			btnOpenBrowser.Text = "🌐 Открыть в браузере";
			btnOpenBrowser.Location = new Point(850, 125);
			btnOpenBrowser.Size = new Size(160, 27);
			btnOpenBrowser.BackColor = Ui.SurfaceAlt;
			btnOpenBrowser.Font = Ui.FontBody(8.5f, true);
			btnOpenBrowser.Cursor = Cursors.Hand;
			btnOpenBrowser.Click += delegate { OpenLocalhostInBrowser(); };
			detailContainer.Controls.Add(btnOpenBrowser);

			DarkButton btnKillProc = new DarkButton();
			btnKillProc.Text = "🛑 Завершить процесс";
			btnKillProc.Location = new Point(1020, 94);
			btnKillProc.Size = new Size(160, 27);
			btnKillProc.BackColor = Color.FromArgb(140, 20, 20);
			btnKillProc.ForeColor = Color.White;
			btnKillProc.Font = Ui.FontBody(8.5f, true);
			btnKillProc.Cursor = Cursors.Hand;
			btnKillProc.Click += delegate { KillSelectedProcess(); };
			detailContainer.Controls.Add(btnKillProc);

			DarkButton btnOpenFileLocation = new DarkButton();
			btnOpenFileLocation.Text = "📁 Показать .exe";
			btnOpenFileLocation.Location = new Point(1020, 125);
			btnOpenFileLocation.Size = new Size(160, 27);
			btnOpenFileLocation.BackColor = Ui.SurfaceAlt;
			btnOpenFileLocation.Font = Ui.FontBody(8.5f, true);
			btnOpenFileLocation.Cursor = Cursors.Hand;
			btnOpenFileLocation.Click += delegate { OpenFileLocation(); };
			detailContainer.Controls.Add(btnOpenFileLocation);

			detailContainer.Resize += delegate
			{
				int w = detailContainer.ClientSize.Width - 24;
				if (w > 600)
				{
					int descW = Math.Max(300, w - 360);
					txtDetailDescription.Width = descW;
					btnCopyBan.Location = new Point(descW + 24, 94);
					btnOpenBrowser.Location = new Point(descW + 24, 125);
					btnKillProc.Location = new Point(descW + 190, 94);
					btnOpenFileLocation.Location = new Point(descW + 190, 125);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);

			// Auto refresh timer
			autoRefreshTimer = new Timer();
			autoRefreshTimer.Interval = 4000;
			autoRefreshTimer.Tick += delegate { LoadSocketsAsync(); };
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (autoRefreshTimer != null)
			{
				autoRefreshTimer.Stop();
				autoRefreshTimer.Dispose();
				autoRefreshTimer = null;
			}
			base.OnFormClosing(e);
		}

		private DarkButton CreateFilterButton(string text, SocketFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.Font = Ui.FontBody(8f, filter == currentFilter);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtonsVisual();
				ApplyFilters();
			};
			return btn;
		}

		private void UpdateFilterButtonsVisual()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterSuspicious, btnFilterLocalhost, btnFilterMinecraft, btnFilterRemote, btnFilterWhitelist };
			SocketFilter[] filters = { SocketFilter.All, SocketFilter.SuspiciousOnly, SocketFilter.LocalhostIpcOnly, SocketFilter.MinecraftOnly, SocketFilter.RemoteServersOnly, SocketFilter.WhitelistOnly };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void LoadSocketsAsync()
		{
			lblStatusPill.Text = "⏳ Опрос сетевых сокетов...";
			lblStatusPill.ForeColor = Ui.AccentOrange;

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<DetailedSocketItem> list = QueryAllSystemSockets();
				e.Result = list;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<DetailedSocketItem>)
				{
					allSockets = (List<DetailedSocketItem>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static List<DetailedSocketItem> QueryAllSystemSockets()
		{
			List<DetailedSocketItem> list = new List<DetailedSocketItem>();
			Dictionary<int, Tuple<string, string>> procCache = new Dictionary<int, Tuple<string, string>>();

			try
			{
				foreach (Process p in Process.GetProcesses())
				{
					try
					{
						string path = "";
						try { path = p.MainModule.FileName; } catch { }
						procCache[p.Id] = Tuple.Create(p.ProcessName + ".exe", path);
					}
					catch { }
				}
			}
			catch { }

			// Query TCP sockets
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano -p tcp");
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute = false;
				psi.CreateNoWindow = true;

				using (Process pNet = Process.Start(psi))
				{
					string output = pNet.StandardOutput.ReadToEnd();
					if (!pNet.WaitForExit(3500)) { try { pNet.Kill(); } catch { } }

					string[] lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					int idx = 1;

					foreach (string line in lines)
					{
						string tr = line.Trim();
						if (!tr.StartsWith("TCP", StringComparison.OrdinalIgnoreCase)) continue;

						string[] parts = Regex.Split(tr, @"\s+");
						if (parts.Length < 5) continue;

						int pid = 0;
						if (!int.TryParse(parts[parts.Length - 1], out pid)) continue;

						DetailedSocketItem item = new DetailedSocketItem();
						item.Index = idx++;
						item.Protocol = "TCP";
						item.LocalAddress = parts[1];
						item.RemoteAddress = parts[2];
						item.State = parts[3];
						item.Pid = pid;

						// Split ports
						item.LocalPort = ExtractPort(item.LocalAddress);
						item.RemotePort = ExtractPort(item.RemoteAddress);

						// Resolve process info
						Tuple<string, string> procInfo;
						if (procCache.TryGetValue(pid, out procInfo))
						{
							item.ProcessName = procInfo.Item1;
							item.ExePath = procInfo.Item2;
						}
						else
						{
							item.ProcessName = pid == 0 ? "System Idle" : (pid == 4 ? "System Kernel" : "PID:" + pid);
							item.ExePath = "";
						}

						ClassifySocket(item);
						list.Add(item);
					}
				}
			}
			catch { }

			// Query UDP sockets
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano -p udp");
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute = false;
				psi.CreateNoWindow = true;

				using (Process pNet = Process.Start(psi))
				{
					string output = pNet.StandardOutput.ReadToEnd();
					if (!pNet.WaitForExit(3500)) { try { pNet.Kill(); } catch { } }

					string[] lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					int idx = list.Count + 1;

					foreach (string line in lines)
					{
						string tr = line.Trim();
						if (!tr.StartsWith("UDP", StringComparison.OrdinalIgnoreCase)) continue;

						string[] parts = Regex.Split(tr, @"\s+");
						if (parts.Length < 4) continue;

						int pid = 0;
						if (!int.TryParse(parts[parts.Length - 1], out pid)) continue;

						DetailedSocketItem item = new DetailedSocketItem();
						item.Index = idx++;
						item.Protocol = "UDP";
						item.LocalAddress = parts[1];
						item.RemoteAddress = parts[2];
						item.State = "—";
						item.Pid = pid;

						item.LocalPort = ExtractPort(item.LocalAddress);
						item.RemotePort = ExtractPort(item.RemoteAddress);

						Tuple<string, string> procInfo;
						if (procCache.TryGetValue(pid, out procInfo))
						{
							item.ProcessName = procInfo.Item1;
							item.ExePath = procInfo.Item2;
						}
						else
						{
							item.ProcessName = pid == 0 ? "System Idle" : "PID:" + pid;
							item.ExePath = "";
						}

						ClassifySocket(item);
						list.Add(item);
					}
				}
			}
			catch { }

			// Sort: suspicious & cheats first
			list.Sort(delegate(DetailedSocketItem a, DetailedSocketItem b)
			{
				if (a.IsCheat && !b.IsCheat) return -1;
				if (!a.IsCheat && b.IsCheat) return 1;
				if (a.IsSuspicious && !b.IsSuspicious) return -1;
				if (!a.IsSuspicious && b.IsSuspicious) return 1;
				if (a.IsMinecraft && !b.IsMinecraft) return -1;
				if (!a.IsMinecraft && b.IsMinecraft) return 1;
				return a.ProcessName.CompareTo(b.ProcessName);
			});

			return list;
		}

		private static int ExtractPort(string endpoint)
		{
			if (string.IsNullOrEmpty(endpoint)) return 0;
			int lastColon = endpoint.LastIndexOf(':');
			if (lastColon >= 0 && lastColon < endpoint.Length - 1)
			{
				int port;
				if (int.TryParse(endpoint.Substring(lastColon + 1), out port)) return port;
			}
			return 0;
		}

		private static void ClassifySocket(DetailedSocketItem item)
		{
			string nameLower = (item.ProcessName ?? "").ToLowerInvariant();
			string local = (item.LocalAddress ?? "").ToLowerInvariant();
			string remote = (item.RemoteAddress ?? "").ToLowerInvariant();

			// 1. Whitelist protection (AnyDesk, RuDesktop, Discord, System)
			if (nameLower.Contains("anydesk") || nameLower.Contains("rudesktop") || nameLower.Contains("discord") ||
				nameLower.Contains("system") || nameLower.Contains("svchost") || nameLower.Contains("spoolsv") ||
				nameLower.Contains("explorer") || nameLower.Contains("chrome") || nameLower.Contains("opera") ||
				nameLower.Contains("firefox") || nameLower.Contains("msedge"))
			{
				item.IsWhitelisted = true;
				if (nameLower.Contains("anydesk") || nameLower.Contains("rudesktop"))
				{
					item.Verdict = "✔ ScreenShare (Разрешено)";
					item.ThreatReason = "Легитимный сеанс удалённого подключения ScreenShare";
					item.ForeColor = Ui.AccentGreen;
				}
				else
				{
					item.Verdict = "✔ Штатное приложение";
					item.ThreatReason = "Доверенное системное или браузерное приложение Windows";
					item.ForeColor = Ui.TextSecondary;
				}
				return;
			}

			// 2. Minecraft / Java Check
			if (nameLower.Contains("javaw") || nameLower.Contains("java") || nameLower.Contains("minecraft") || nameLower.Contains("bedrock"))
			{
				item.IsMinecraft = true;

				// Localhost listener or IPC on unexpected port
				if (item.State == "LISTENING" || remote.StartsWith("127.0.0.1:") || remote.StartsWith("[::1]:"))
				{
					if (item.LocalPort == 25565 || item.LocalPort == 25577 || item.LocalPort == 19132)
					{
						item.Verdict = "🟢 Локальный сервер MC (LAN)";
						item.ThreatReason = "Встроенный сервер одиночной игры или LAN мир";
						item.ForeColor = Ui.AccentTeal;
						return;
					}

					item.IsSuspicious = true;
					item.IsLocalhostIpc = true;
					item.Verdict = "🚨 javaw слушает порт / Web-GUI (5.01)";
					item.ThreatReason = string.Format("javaw.exe слушает локальный порт {0} или ведёт IPC обмен (характерно для Vape/Slinky/Drip)", item.LocalPort);
					item.ForeColor = Ui.AccentRed;
					return;
				}

				// Remote endpoints for Minecraft
				if (item.RemotePort == 25565 || item.RemotePort == 25577 || item.RemotePort == 19132)
				{
					item.Verdict = "🟢 Игровой сервер Minecraft";
					item.ThreatReason = "Подключение к игровому серверу Minecraft по штатному порту";
					item.ForeColor = Ui.AccentGreen;
					return;
				}
				else if (item.RemotePort == 443 || item.RemotePort == 80)
				{
					item.Verdict = "Аутентификация Mojang / Скины";
					item.ThreatReason = "Штатный веб-трафик игрового клиента (авторизация, загрузка ресурсов)";
					item.ForeColor = Ui.TextSecondary;
					return;
				}
				else
				{
					item.IsSuspicious = true;
					item.Verdict = "⚠️ Нестандартное соединение javaw";
					item.ThreatReason = "Нестандартный удалённый порт Minecraft: " + item.RemoteAddress;
					item.ForeColor = Ui.AccentOrange;
					return;
				}
			}

			// 3. Known Cleaners Check (Rule 5.08)
			bool isCleaner = false;
			foreach (string cl in CheatDatabases.KnownCleanerSignatures)
			{
				if (cl.Length >= 4 && nameLower.Contains(cl)) { isCleaner = true; break; }
			}
			if (isCleaner)
			{
				item.IsCleaner = true;
				item.IsSuspicious = true;
				item.Verdict = "🚨 Сокет клинера (5.08)";
				item.ThreatReason = "Активное соединение утилиты очистки или сокрытия улик (Пункт 5.08)";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 4. Cheat Process Check (Rule 5.01)
			bool isCheat = CheatProcessPattern.IsMatch(nameLower);
			if (!isCheat)
			{
				foreach (string ec in CheatDatabases.ExtraCheatClients)
				{
					if (ec.Length >= 4 && nameLower.Contains(ec.ToLowerInvariant())) { isCheat = true; break; }
				}
			}
			if (!isCheat)
			{
				foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
				{
					if (inj.Length >= 4 && nameLower.Contains(inj.ToLowerInvariant())) { isCheat = true; break; }
				}
			}

			if (isCheat)
			{
				item.IsCheat = true;
				item.IsSuspicious = true;
				item.Verdict = "🚨 Сокет чит-клиента (5.01)";
				item.ThreatReason = "Сетевая активность запрещённого чит-процесса (Пункт 5.01)";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 5. Localhost IPC Check for unknown apps
			if (remote.StartsWith("127.0.0.1:") || remote.StartsWith("[::1]:") || local.StartsWith("127.0.0.1:") || local.StartsWith("0.0.0.0:"))
			{
				item.IsLocalhostIpc = true;
				if (item.State == "LISTENING" && (item.LocalPort == 1337 || item.LocalPort == 4444 || item.LocalPort == 8088 || item.LocalPort == 31337))
				{
					item.IsSuspicious = true;
					item.Verdict = "🚨 Подозрительный Localhost порт (5.01)";
					item.ThreatReason = "Прослушивание известного хакерского / чит-порта: " + item.LocalPort;
					item.ForeColor = Ui.AccentRed;
					return;
				}
			}

			// Normal connection
			item.Verdict = "Активное соединение";
			item.ThreatReason = "Стандартное сетевое соединение приложения Windows";
			item.ForeColor = Ui.TextPrimary;
		}

		private void UpdateSummaryStats()
		{
			int total = allSockets.Count;
			int susp = allSockets.Count(s => s.IsSuspicious || s.IsCheat || s.IsCleaner);
			int ipc = allSockets.Count(s => s.IsLocalhostIpc);
			int mc = allSockets.Count(s => s.IsMinecraft);
			int remote = allSockets.Count(s => s.RemotePort != 0 && !s.IsLocalhostIpc);
			int white = allSockets.Count(s => s.IsWhitelisted);

			btnFilterAll.Text = string.Format("🔍 Все сокеты ({0})", total);
			btnFilterSuspicious.Text = string.Format("🚨 Подозрительные ({0})", susp);
			btnFilterLocalhost.Text = string.Format("🌐 Localhost IPC ({0})", ipc);
			btnFilterMinecraft.Text = string.Format("☕ Minecraft ({0})", mc);
			btnFilterRemote.Text = string.Format("📡 Внешние ({0})", remote);
			btnFilterWhitelist.Text = string.Format("✔ Whitelist ({0})", white);

			if (susp > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Найдено {0} подозрительных сетевых сокетов!", susp);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔ Активно сокетов: {0}. Подозрительных подключений не найдено.", total);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (DetailedSocketItem item in allSockets)
			{
				switch (currentFilter)
				{
					case SocketFilter.SuspiciousOnly:
						if (!item.IsSuspicious && !item.IsCheat && !item.IsCleaner) continue;
						break;
					case SocketFilter.LocalhostIpcOnly:
						if (!item.IsLocalhostIpc) continue;
						break;
					case SocketFilter.MinecraftOnly:
						if (!item.IsMinecraft) continue;
						break;
					case SocketFilter.RemoteServersOnly:
						if (item.RemotePort == 0 || item.IsLocalhostIpc) continue;
						break;
					case SocketFilter.WhitelistOnly:
						if (!item.IsWhitelisted) continue;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = item.ProcessName.ToLowerInvariant().Contains(query) ||
								 item.Pid.ToString().Contains(query) ||
								 item.LocalAddress.ToLowerInvariant().Contains(query) ||
								 item.RemoteAddress.ToLowerInvariant().Contains(query) ||
								 item.Verdict.ToLowerInvariant().Contains(query) ||
								 item.ExePath.ToLowerInvariant().Contains(query);
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.SubItems.Add(item.ProcessName);
				lvi.SubItems.Add(item.Pid.ToString());
				lvi.SubItems.Add(item.Protocol);
				lvi.SubItems.Add(item.LocalAddress);
				lvi.SubItems.Add(item.RemoteAddress);
				lvi.SubItems.Add(item.State);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.ExePath) ? "—" : item.ExePath);

				lvi.ForeColor = item.ForeColor;
				lvi.Tag = item;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearDetailPane();
			}
		}

		private DetailedSocketItem GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as DetailedSocketItem;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			DetailedSocketItem sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailProcess.Text = string.Format("{0} (PID: {1})", sel.ProcessName, sel.Pid);
			lblDetailEndpoints.Text = string.Format("{0}  ➔  Локальный: {1}  |  Удалённый: {2}  |  Состояние: {3}",
				sel.Protocol, sel.LocalAddress, sel.RemoteAddress, sel.State);
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;

			if (sel.IsCheat || sel.HasDetectedWebGui) lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-клиент / Injected Web-GUI) — Бан 30 дней";
			else if (sel.IsCleaner) lblDetailRule.Text = "Пункт регламента: 5.08 (Сокрытие улик / Клинер) — Бан 30 дней";
			else if (sel.IsWhitelisted) lblDetailRule.Text = "Пункт регламента: 5.01 Белый список (Разрешено)";
			else lblDetailRule.Text = "Пункт регламента: Допустимая сетевая активность";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Исполняемый файл: " + (string.IsNullOrEmpty(sel.ExePath) ? "Не определён (системный процесс или нет доступа)" : sel.ExePath));
			sb.AppendLine("Обоснование: " + sel.ThreatReason);
			if (sel.HasDetectedWebGui)
			{
				sb.AppendLine("🚨 ВНИМАНИЕ: На этом порту откликнулся HTTP интерфейс чита!");
				sb.AppendLine("Фрагмент ответа: " + sel.WebGuiSnippet);
			}
			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDetailPane()
		{
			lblDetailProcess.Text = "Соединения не выбраны";
			lblDetailEndpoints.Text = "Маршрут соединения: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Пункт регламента: —";
			txtDetailDescription.Text = "";
		}

		private void CopyBanCommand()
		{
			DetailedSocketItem sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.HasDetectedWebGui)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Активный Web-GUI чита на порту {1}: {2})",
					targetPlayerName, sel.LocalPort, sel.ProcessName);
			}
			else if (sel.IsCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Активный сокет чита: {1} -> {2})",
					targetPlayerName, sel.ProcessName, sel.RemoteAddress);
			}
			else if (sel.IsCleaner)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Сетевая активность клинера: {1})",
					targetPlayerName, sel.ProcessName);
			}
			else if (sel.IsMinecraft && sel.IsLocalhostIpc)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Инжекция/Localhost сокет в javaw: порт {1})",
					targetPlayerName, sel.LocalPort);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подозрительное сетевое соединение: {1} -> {2})",
					targetPlayerName, sel.ProcessName, sel.RemoteAddress);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан по сетевым сокетам", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void OpenLocalhostInBrowser()
		{
			DetailedSocketItem sel = GetSelectedItem();
			if (sel == null) return;

			int port = sel.LocalPort > 0 ? sel.LocalPort : sel.RemotePort;
			if (port <= 0)
			{
				MessageBox.Show("У выбранного подключения отсутствует локальный порт.", "Web-GUI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				string url = string.Format("http://127.0.0.1:{0}/", port);
				Process.Start(url);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия браузера: " + ex.Message, "Web-GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void KillSelectedProcess()
		{
			DetailedSocketItem sel = GetSelectedItem();
			if (sel == null || sel.Pid <= 4)
			{
				MessageBox.Show("Невозможно завершить данный процесс.", "Завершение процесса", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			DialogResult dr = MessageBox.Show(string.Format("Вы действительно хотите принудительно завершить процесс {0} (PID: {1})?", sel.ProcessName, sel.Pid),
				"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

			if (dr == DialogResult.Yes)
			{
				try
				{
					Process p = Process.GetProcessById(sel.Pid);
					p.Kill();
					MessageBox.Show("Процесс успешно завершён.", "Завершение процесса", MessageBoxButtons.OK, MessageBoxIcon.Information);
					LoadSocketsAsync();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка завершения процесса: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void OpenFileLocation()
		{
			DetailedSocketItem sel = GetSelectedItem();
			if (sel == null || string.IsNullOrEmpty(sel.ExePath) || !File.Exists(sel.ExePath))
			{
				MessageBox.Show("Файл процесса не найден на диске или путь недоступен.", "Файл", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				Process.Start("explorer.exe", "/select,\"" + sel.ExePath + "\"");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка открытия Проводника: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ProbeAllLocalhostWebGuis()
		{
			lblStatusPill.Text = "📡 Отправка HTTP-зондов на все слушающие порты 127.0.0.1...";
			lblStatusPill.ForeColor = Ui.AccentRed;

			List<int> listeningPorts = new List<int>();
			foreach (DetailedSocketItem item in allSockets)
			{
				if (item.State == "LISTENING" || item.LocalAddress.StartsWith("127.0.0.1:") || item.LocalAddress.StartsWith("0.0.0.0:"))
				{
					if (item.LocalPort > 0 && !listeningPorts.Contains(item.LocalPort) && item.LocalPort != 135 && item.LocalPort != 445)
					{
						listeningPorts.Add(item.LocalPort);
					}
				}
			}

			if (listeningPorts.Count == 0)
			{
				MessageBox.Show("Не найдено открытых локальных портов для проверки Web-интерфейсов.", "Зондирование Web-GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
				lblStatusPill.Text = "✔ Открытых localhost портов не обнаружено.";
				lblStatusPill.ForeColor = Ui.AccentGreen;
				return;
			}

			BackgroundWorker probeWorker = new BackgroundWorker();
			probeWorker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<Tuple<int, string>> hits = new List<Tuple<int, string>>();

				foreach (int port in listeningPorts)
				{
					try
					{
						HttpWebRequest req = (HttpWebRequest)WebRequest.Create(string.Format("http://127.0.0.1:{0}/", port));
						req.Timeout = 1200;
						req.ReadWriteTimeout = 1200;
						req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

						using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
						{
							using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
							{
								char[] buf = new char[2048];
								int read = sr.Read(buf, 0, buf.Length);
								string body = new string(buf, 0, read).ToLowerInvariant();

								if (body.Contains("vape") || body.Contains("slinky") || body.Contains("drip") ||
									body.Contains("aimassist") || body.Contains("reach") || body.Contains("velocity") ||
									body.Contains("killaura") || body.Contains("autoclicker") || body.Contains("cheat"))
								{
									hits.Add(Tuple.Create(port, body.Substring(0, Math.Min(120, body.Length))));
								}
							}
						}
					}
					catch { }
				}

				e.Result = hits;
			};

			probeWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				List<Tuple<int, string>> hits = e.Result as List<Tuple<int, string>>;
				if (hits != null && hits.Count > 0)
				{
					foreach (Tuple<int, string> hit in hits)
					{
						foreach (DetailedSocketItem item in allSockets)
						{
							if (item.LocalPort == hit.Item1)
							{
								item.HasDetectedWebGui = true;
								item.IsSuspicious = true;
								item.IsCheat = true;
								item.WebGuiSnippet = hit.Item2;
								item.Verdict = "🚨 ОБНАРУЖЕН WEB-GUI ЧИТА (5.01)";
								item.ThreatReason = string.Format("Порт {0} ответил активным веб-интерфейсом чита: {1}", hit.Item1, hit.Item2);
								item.ForeColor = Ui.AccentRed;
							}
						}
					}

					ApplyFilters();
					string portsStr = string.Join(", ", hits.Select(h => h.Item1.ToString()).ToArray());
					MessageBox.Show("🚨 ВНИМАНИЕ! На локальных портах [" + portsStr + "] ОБНАРУЖЕН АКТИВНЫЙ WEB-ИНТЕРФЕЙС ЧИТА (Vape/Slinky/Drip)!\n\nИгрок использует чит с управлением через браузер!\nНарушение регламента: 5.01 (Бан 30 дней).",
						"ДЕТЕКТ WEB-GUI ЧИТА", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					MessageBox.Show(string.Format("Проверено {0} локальных портов.\nАктивных веб-интерфейсов читов (Vape/Slinky/Drip) не зафиксировано.", listeningPorts.Count),
						"Результат проверки Web-GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
					lblStatusPill.Text = "✔ Проверено " + listeningPorts.Count + " портов: Web-GUI чита не найден.";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			};

			probeWorker.RunWorkerAsync();
		}
	}
}
