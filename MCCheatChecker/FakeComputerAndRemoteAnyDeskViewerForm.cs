using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum RemoteAnyDeskFilterMode
	{
		All,
		FakePcAndEvasion,
		ThirdPartyRemote,
		VmAndHypervisor,
		MinecraftStatus,
		WhitelistedAnyDesk
	}

	public class RemoteAnyDeskEvidenceItem
	{
		public string Category;
		public string SourceArtifact;
		public string ValueOrId;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string DetailDescription;
		public string TargetRule; // "5.08", "5.01", "РАЗРЕШЕНО"
		public bool IsWhitelisted;
		public DateTime Timestamp;
	}

	public class FakeComputerAndRemoteAnyDeskViewerForm : Form
	{
		private const int SM_REMOTESESSION = 0x1000;

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);

		private readonly string targetPlayerName;
		private List<RemoteAnyDeskEvidenceItem> allItems = new List<RemoteAnyDeskEvidenceItem>();
		private RemoteAnyDeskFilterMode currentFilter = RemoteAnyDeskFilterMode.All;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblKpiAnydeskProfiles;
		private Label lblKpiRemoteSessions;
		private Label lblKpiMinecraftStatus;
		private Label lblKpiFakePcAlerts;
		private Label lblKpiThirdPartyRemote;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterFakePc;
		private DarkButton btnFilterThirdParty;
		private DarkButton btnFilterVm;
		private DarkButton btnFilterMinecraft;
		private DarkButton btnFilterWhitelisted;

		private DarkTextBox searchBox;
		private DarkListView listView;
		private TabControl detailTabs;
		private DarkTextBoxMultiline txtAnalysis;
		private DarkTextBoxMultiline txtTimeline;
		private DarkTextBoxMultiline txtBanVerdict;

		private DarkButton btnCopyBanCmd;
		private DarkButton btnCopyDiscord;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;

		private static readonly string[] ThirdPartyRemoteToolNames = new string[]
		{
			"rustdesk", "teamviewer", "parsec", "moonlight", "sunshine", "vnc", "realvnc", "tightvnc", "ultravnc",
			"radmin", "ammyy", "supremo", "splashtop", "chrome remote desktop", "litemanager", "ultraviewer", "hopetodesk", "dwservice"
		};

		private static readonly string[] VmOuis = new string[]
		{
			"00:05:69", "00:0C:29", "00:50:56", "08:00:27", "00:15:5D", "52:54:00"
		};

		public FakeComputerAndRemoteAnyDeskViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			ScanRemoteAndComputerIntegrity();
		}

		private void InitializeComponent()
		{
			this.Text = "🖥️ Инспектор подмены ПК, удаленных сессий и AnyDesk профилей (ScreenShare Verification) — MCCheatChecker";
			this.Size = new Size(1320, 800);
			this.MinimumSize = new Size(1020, 620);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			// Панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🖥️ ИНСПЕКТОР ПОДМЕНЫ ПК, УДАЛЕННЫХ СЕССИЙ И ANYDESK ПРОФИЛЕЙ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Криминалистическая проверка на подставной компьютер/чистый ноутбук, параллельные удаленные сессии чистильщика (AnyDesk/RDP), подмену AnyDesk ID и виртуальные машины (5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			// Карточки KPI
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int kx = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ANYDESK ПРОФИЛЕЙ", "0", Ui.TextPrimary, ref kx, out lblKpiAnydeskProfiles));
			kpiPanel.Controls.Add(CreateKpiBadge("УДАЛЕННЫХ ПОДКЛЮЧЕНИЙ", "0", Ui.AccentBlue, ref kx, out lblKpiRemoteSessions));
			kpiPanel.Controls.Add(CreateKpiBadge("MINECRAFT НА ЭТОМ ПК", "Проверка...", Ui.AccentOrange, ref kx, out lblKpiMinecraftStatus));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 ПОДМЕНА ПК / ANYDESK", "0", Ui.AccentRed, ref kx, out lblKpiFakePcAlerts));
			kpiPanel.Controls.Add(CreateKpiBadge("⚠️ СТОРОННИЙ ДОСТУП", "0", Ui.AccentOrange, ref kx, out lblKpiThirdPartyRemote));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ СЕССИЯ МОДЕРАТОРА", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все проверки", RemoteAnyDeskFilterMode.All, ref bx, filterBar);
			btnFilterFakePc = CreateFilterButton("🚨 Подмена ПК / AnyDesk (5.08)", RemoteAnyDeskFilterMode.FakePcAndEvasion, ref bx, filterBar);
			btnFilterThirdParty = CreateFilterButton("⚠️ Сторонний удалённый доступ", RemoteAnyDeskFilterMode.ThirdPartyRemote, ref bx, filterBar);
			btnFilterVm = CreateFilterButton("🖥️ Виртуальные машины (VM)", RemoteAnyDeskFilterMode.VmAndHypervisor, ref bx, filterBar);
			btnFilterMinecraft = CreateFilterButton("☕ Статус игры и логов", RemoteAnyDeskFilterMode.MinecraftStatus, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Разрешённый AnyDesk", RemoteAnyDeskFilterMode.WhitelistedAnyDesk, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "Поиск по источнику, ID, артефакту или вердикту...";
			searchBox.Size = new Size(260, 26);
			searchBox.Location = new Point(filterBar.Width - 280, 8);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			// Панель нижних действий
			Panel bottomActions = new Panel();
			bottomActions.Dock = DockStyle.Bottom;
			bottomActions.Height = 46;
			bottomActions.BackColor = Ui.Surface;
			bottomActions.Padding = new Padding(12, 8, 12, 8);
			this.Controls.Add(bottomActions);

			int ax = 12;
			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана", ref ax, bottomActions, delegate { CopyBanCommand(); });
			btnCopyDiscord = CreateActionButton("📋 Сводка для Discord", ref ax, bottomActions, delegate { CopyDiscordSummary(); });
			btnExportHtml = CreateActionButton("🌐 Экспорт HTML-отчёта", ref ax, bottomActions, delegate { ExportHtmlReport(); });
			btnRescan = CreateActionButton("🔄 Перепроверить AnyDesk и ПК", ref ax, bottomActions, delegate { ScanRemoteAndComputerIntegrity(); });

			// Разделитель
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 330;
			split.BackColor = Ui.Border;
			this.Controls.Add(split);

			// Список ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Columns.Add("Категория", 190);
			listView.Columns.Add("Источник / Артефакт", 220);
			listView.Columns.Add("Значение / ID / Сессия", 200);
			listView.Columns.Add("Статус", 120);
			listView.Columns.Add("Угроза", 100);
			listView.Columns.Add("Вердикт детектора", 300);
			listView.Columns.Add("Регламент", 110);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			split.Panel2.Controls.Add(detailTabs);

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ подмены ПК");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTimeline = new TabPage("📜 Таймлайн подключений AnyDesk и RDP");
			txtTimeline = new DarkTextBoxMultiline();
			txtTimeline.Dock = DockStyle.Fill;
			txtTimeline.ReadOnly = true;
			txtTimeline.Font = Ui.FontMono(8.5f);
			tabTimeline.Controls.Add(txtTimeline);
			detailTabs.TabPages.Add(tabTimeline);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 и Команда наказания");
			txtBanVerdict = new DarkTextBoxMultiline();
			txtBanVerdict.Dock = DockStyle.Fill;
			txtBanVerdict.ReadOnly = true;
			txtBanVerdict.Font = Ui.FontMono(9f);
			tabBan.Controls.Add(txtBanVerdict);
			detailTabs.TabPages.Add(tabBan);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(195, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.Surface;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(188, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(188, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 202;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, RemoteAnyDeskFilterMode mode, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8f, true);
			btn.Size = new Size(195, 28);
			btn.Location = new Point(x, 8);
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtonStyles();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			x += btn.Width + 6;
			return btn;
		}

		private DarkButton CreateActionButton(string text, ref int x, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8.5f, true);
			btn.Size = new Size(220, 30);
			btn.Location = new Point(x, 8);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			x += btn.Width + 10;
			return btn;
		}

		private void UpdateFilterButtonStyles()
		{
			btnFilterAll.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.All);
			btnFilterFakePc.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.FakePcAndEvasion);
			btnFilterThirdParty.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.ThirdPartyRemote);
			btnFilterVm.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.VmAndHypervisor);
			btnFilterMinecraft.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.MinecraftStatus);
			btnFilterWhitelisted.AccentStyle = (currentFilter == RemoteAnyDeskFilterMode.WhitelistedAnyDesk);
		}

		public void ScanRemoteAndComputerIntegrity()
		{
			allItems.Clear();
			listView.Items.Clear();

			int anydeskProfilesCount = 0;
			int remoteSessionsCount = 0;
			int fakePcAlertsCount = 0;
			int thirdPartyRemoteCount = 0;
			int whitelistedCount = 0;
			bool minecraftRunning = false;

			// 1. Проверка активного процесса Minecraft (javaw.exe)
			Process[] javawProcs = Process.GetProcessesByName("javaw");
			Process[] javaProcs = Process.GetProcessesByName("java");
			if (javawProcs.Length > 0 || javaProcs.Length > 0)
			{
				minecraftRunning = true;
				int pid = javawProcs.Length > 0 ? javawProcs[0].Id : javaProcs[0].Id;
				string pName = javawProcs.Length > 0 ? "javaw.exe" : "java.exe";
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Процесс Minecraft",
					SourceArtifact = "Активный процесс ОС",
					ValueOrId = pName + " (PID " + pid + ")",
					Status = "АКТИВЕН",
					ThreatLevel = "SAFE",
					DetailDescription = "Minecraft запущен и активен в памяти на данном компьютере.",
					TargetRule = "РАЗРЕШЕНО",
					IsWhitelisted = true,
					Timestamp = DateTime.Now
				});
			}
			else
			{
				// Если Minecraft не запущен — проверяем дату последнего latest.log!
				DateTime lastLogTime = DateTime.MinValue;
				string lastLogPath = "";
				foreach (string root in MinecraftRoots())
				{
					string logFile = Path.Combine(root, "logs", "latest.log");
					if (File.Exists(logFile))
					{
						FileInfo fi = new FileInfo(logFile);
						if (fi.LastWriteTime > lastLogTime)
						{
							lastLogTime = fi.LastWriteTime;
							lastLogPath = logFile;
						}
					}
				}

				if (lastLogTime != DateTime.MinValue)
				{
					TimeSpan diff = DateTime.Now - lastLogTime;
					if (diff.TotalMinutes > 30)
					{
						fakePcAlertsCount++;
						allItems.Add(new RemoteAnyDeskEvidenceItem
						{
							Category = "Подмена компьютера",
							SourceArtifact = "logs\\latest.log",
							ValueOrId = "Последняя игра: " + lastLogTime.ToString("dd.MM.yyyy HH:mm"),
							Status = "УСТАРЕЛ (" + (int)diff.TotalMinutes + " мин назад)",
							ThreatLevel = "CRITICAL",
							DetailDescription = "🚨 Игрок вызван на проверку, но на этом ПК игра закрыта более " + (int)diff.TotalMinutes + " минут назад! Высокий риск того, что модератору предоставлен другой чистый ПК/ноутбук.",
							TargetRule = "5.08",
							IsWhitelisted = false,
							Timestamp = lastLogTime
						});
					}
					else
					{
						allItems.Add(new RemoteAnyDeskEvidenceItem
						{
							Category = "Процесс Minecraft",
							SourceArtifact = "logs\\latest.log",
							ValueOrId = "Игра закрыта " + (int)diff.TotalMinutes + " мин назад",
							Status = "НЕДАВНО ЗАКРЫТ",
							ThreatLevel = "WARNING",
							DetailDescription = "Minecraft был закрыт " + (int)diff.TotalMinutes + " мин назад (" + lastLogTime.ToString("HH:mm:ss") + "). Проверьте причину закрытия игры перед проверкой.",
							TargetRule = "5.08",
							IsWhitelisted = false,
							Timestamp = lastLogTime
						});
					}
				}
				else
				{
					fakePcAlertsCount++;
					allItems.Add(new RemoteAnyDeskEvidenceItem
					{
						Category = "Подмена компьютера",
						SourceArtifact = ".minecraft\\logs",
						ValueOrId = "Логи отсутствуют",
						Status = "НЕТ СЛЕДОВ ИГРЫ",
						ThreatLevel = "CRITICAL",
						DetailDescription = "🚨 На данном компьютере вообще не обнаружено игровых логов Minecraft! Модератор подключен к чужому или чистому устройству.",
						TargetRule = "5.08",
						IsWhitelisted = false,
						Timestamp = DateTime.Now
					});
				}
			}

			lblKpiMinecraftStatus.Text = minecraftRunning ? "✅ Активен (ОК)" : "⚠️ Игра закрыта!";

			// 2. Аудит профилей AnyDesk (поиск подмены ID)
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string userConf = Path.Combine(appData, "AnyDesk", "system.conf");
			string svcConf = Path.Combine(progData, "AnyDesk", "system.conf");

			string userAnyDeskId = ExtractAnyDeskId(userConf);
			string svcAnyDeskId = ExtractAnyDeskId(svcConf);

			if (!string.IsNullOrEmpty(userAnyDeskId))
			{
				anydeskProfilesCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Профиль AnyDesk",
					SourceArtifact = "AppData\\AnyDesk\\system.conf",
					ValueOrId = userAnyDeskId,
					Status = "Пользовательский",
					ThreatLevel = "SAFE",
					DetailDescription = "Основной AnyDesk ID пользователя для подключения модератора.",
					TargetRule = "РАЗРЕШЕНО",
					IsWhitelisted = true,
					Timestamp = File.GetLastWriteTime(userConf)
				});
				whitelistedCount++;
			}

			if (!string.IsNullOrEmpty(svcAnyDeskId))
			{
				anydeskProfilesCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Профиль AnyDesk",
					SourceArtifact = "ProgramData\\AnyDesk\\system.conf",
					ValueOrId = svcAnyDeskId,
					Status = "Системная служба",
					ThreatLevel = "SAFE",
					DetailDescription = "Системный AnyDesk ID службы ad_svc.",
					TargetRule = "РАЗРЕШЕНО",
					IsWhitelisted = true,
					Timestamp = File.GetLastWriteTime(svcConf)
				});
				whitelistedCount++;
			}

			// Проверка рассинхронизации ID AnyDesk
			if (!string.IsNullOrEmpty(userAnyDeskId) && !string.IsNullOrEmpty(svcAnyDeskId) && userAnyDeskId != svcAnyDeskId)
			{
				fakePcAlertsCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Подмена AnyDesk ID",
					SourceArtifact = "system.conf (User vs Service)",
					ValueOrId = "User: " + userAnyDeskId + " != Svc: " + svcAnyDeskId,
					Status = "РАССИНХРОНИЗАЦИЯ",
					ThreatLevel = "CRITICAL",
					DetailDescription = "🚨 На компьютере обнаружено два разных AnyDesk ID! Игрок мог запустить портативный подставной AnyDesk с чистым профилем.",
					TargetRule = "5.08",
					IsWhitelisted = false,
					Timestamp = DateTime.Now
				});
			}

			// 3. Анализ AnyDesk trace файлов (ad.trace, ad_svc.trace)
			string[] traces = new string[]
			{
				Path.Combine(appData, "AnyDesk", "ad.trace"),
				Path.Combine(progData, "AnyDesk", "ad_svc.trace")
			};

			int activeIncomingConnections = 0;
			foreach (string tr in traces)
			{
				if (!File.Exists(tr)) continue;
				try
				{
					FileInfo fi = new FileInfo(tr);
					TimeSpan traceAge = DateTime.Now - fi.CreationTime;
					if (traceAge.TotalMinutes < 20 && fi.Length < 4096)
					{
						fakePcAlertsCount++;
						allItems.Add(new RemoteAnyDeskEvidenceItem
						{
							Category = "Зачистка AnyDesk",
							SourceArtifact = Path.GetFileName(tr),
							ValueOrId = "Создан " + (int)traceAge.TotalMinutes + " мин назад (" + fi.Length + " байт)",
							Status = "СВЕЖИЙ ЛОГ",
							ThreatLevel = "CRITICAL",
							DetailDescription = "🚨 Лог AnyDesk был создан всего " + (int)traceAge.TotalMinutes + " минут назад и имеет подозрительно маленький размер. Признак удаления следов перед проверкой!",
							TargetRule = "5.08",
							IsWhitelisted = false,
							Timestamp = fi.CreationTime
						});
					}

					// Считываем сессии
					string[] traceLines = File.ReadAllLines(tr);
					foreach (string line in traceLines)
					{
						string lLower = line.ToLowerInvariant();
						if (lLower.Contains("logged in from") || lLower.Contains("incoming session request from"))
						{
							remoteSessionsCount++;
							Match idM = Regex.Match(line, "(?:from|peer)\\s*([0-9]{7,11})", RegexOptions.IgnoreCase);
							string peerId = idM.Success ? idM.Groups[1].Value : "Неизвестный ID";

							allItems.Add(new RemoteAnyDeskEvidenceItem
							{
								Category = "Сессия AnyDesk",
								SourceArtifact = Path.GetFileName(tr),
								ValueOrId = "Входящий клиент: " + peerId,
								Status = "ПОДКЛЮЧЕНИЕ",
								ThreatLevel = "INFO",
								DetailDescription = "Зафиксировано подключение удалённого клиента AnyDesk (" + peerId + ").",
								TargetRule = "РАЗРЕШЕНО",
								IsWhitelisted = true,
								Timestamp = fi.LastWriteTime
							});
						}

						if (lLower.Contains("session started"))
						{
							activeIncomingConnections++;
						}
						else if (lLower.Contains("session closed") || lLower.Contains("session ended"))
						{
							activeIncomingConnections = Math.Max(0, activeIncomingConnections - 1);
						}
					}
				}
				catch { }
			}

			// Детекция параллельной сессии в AnyDesk (чистильщик)
			if (activeIncomingConnections > 1)
			{
				fakePcAlertsCount++;
				thirdPartyRemoteCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Параллельная сессия AnyDesk",
					SourceArtifact = "ad.trace (Active Sessions)",
					ValueOrId = "Параллельных сессий: " + activeIncomingConnections,
					Status = "ДВОЙНОЙ ДОСТУП",
					ThreatLevel = "CRITICAL",
					DetailDescription = "🚨 ВНИМАНИЕ: К AnyDesk одновременно подключено более одного удаленного клиента! Возможно параллельное управление ПК третьим лицом для сокрытия улик.",
					TargetRule = "5.08",
					IsWhitelisted = false,
					Timestamp = DateTime.Now
				});
			}

			// 4. Проверка удаленной сессии Windows RDP / Terminal Services
			int isRemote = GetSystemMetrics(SM_REMOTESESSION);
			if (isRemote != 0)
			{
				fakePcAlertsCount++;
				thirdPartyRemoteCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Удаленная сессия RDP",
					SourceArtifact = "GetSystemMetrics(SM_REMOTESESSION)",
					ValueOrId = "SM_REMOTESESSION = 1 (Active)",
					Status = "RDP СЕССИЯ АКТИВНА",
					ThreatLevel = "CRITICAL",
					DetailDescription = "🚨 Текущая пользовательская сессия Windows запущена через Удаленный рабочий стол (RDP/Terminal Server)! Игрок физически находится за другим компьютером.",
					TargetRule = "5.08",
					IsWhitelisted = false,
					Timestamp = DateTime.Now
				});
			}

			// 5. Проверка сторонних программ удаленного доступа (RustDesk, TeamViewer, Parsec и др.)
			Process[] allProcs = Process.GetProcesses();
			foreach (Process p in allProcs)
			{
				try
				{
					string pName = p.ProcessName.ToLowerInvariant();
					foreach (string remoteTool in ThirdPartyRemoteToolNames)
					{
						if (pName.IndexOf(remoteTool, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							thirdPartyRemoteCount++;
							allItems.Add(new RemoteAnyDeskEvidenceItem
							{
								Category = "Стороннее ПО удаленного доступа",
								SourceArtifact = "Процесс: " + p.ProcessName + ".exe",
								ValueOrId = "PID " + p.Id,
								Status = "ЗАПУЩЕН",
								ThreatLevel = "WARNING",
								DetailDescription = "⚠️ Обнаружен работающий инструмент стороннего удаленного доступа: " + p.ProcessName + ". Убедитесь, что третье лицо не управляет ПК во время ScreenShare.",
								TargetRule = "5.08",
								IsWhitelisted = false,
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
				catch { }
			}

			// 6. Проверка на Виртуальную Машину (VM / Hypervisor)
			AuditVirtualMachine(ref fakePcAlertsCount);

			// 7. Проверка системного Uptime (свежая перезагрузка перед проверкой)
			int uptimeMs = Environment.TickCount;
			int uptimeMinutes = (uptimeMs / 1000) / 60;
			if (uptimeMinutes < 15)
			{
				fakePcAlertsCount++;
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Время работы системы (Uptime)",
					SourceArtifact = "Environment.TickCount",
					ValueOrId = uptimeMinutes + " мин (" + (uptimeMs / 1000) + " сек)",
					Status = "СВЕЖИЙ ЗАПУСК",
					ThreatLevel = "WARNING",
					DetailDescription = "⚠️ Компьютер был запущен/перезагружен всего " + uptimeMinutes + " мин назад. Проверьте журнал Windows EventLog на предмет намеренной очистки памяти перед вызовом на проверку.",
					TargetRule = "5.08",
					IsWhitelisted = false,
					Timestamp = DateTime.Now
				});
			}
			else
			{
				allItems.Add(new RemoteAnyDeskEvidenceItem
				{
					Category = "Время работы системы (Uptime)",
					SourceArtifact = "Environment.TickCount",
					ValueOrId = uptimeMinutes + " мин (" + (uptimeMinutes / 60) + " ч " + (uptimeMinutes % 60) + " мин)",
					Status = "СТАБИЛЕН",
					ThreatLevel = "INFO",
					DetailDescription = "Система работает стабильно, признаков экстренной перезагрузки перед ScreenShare нет.",
					TargetRule = "РАЗРЕШЕНО",
					IsWhitelisted = true,
					Timestamp = DateTime.Now
				});
			}

			lblKpiAnydeskProfiles.Text = anydeskProfilesCount.ToString();
			lblKpiRemoteSessions.Text = remoteSessionsCount.ToString();
			lblKpiFakePcAlerts.Text = fakePcAlertsCount.ToString();
			lblKpiThirdPartyRemote.Text = thirdPartyRemoteCount.ToString();
			lblKpiWhitelisted.Text = whitelistedCount.ToString();

			UpdateFilterButtonStyles();
			ApplyFilter();
		}

		private string ExtractAnyDeskId(string configPath)
		{
			if (!File.Exists(configPath)) return "";
			try
			{
				string[] lines = File.ReadAllLines(configPath);
				foreach (string l in lines)
				{
					string trimmed = l.Trim();
					if (trimmed.StartsWith("ad.anynet.id=") && trimmed.Length > 13)
					{
						return trimmed.Substring(13).Trim();
					}
				}
			}
			catch { }
			return "";
		}

		private void AuditVirtualMachine(ref int fakePcAlertsCount)
		{
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						string mfg = (obj["Manufacturer"] ?? "").ToString().ToLowerInvariant();
						string model = (obj["Model"] ?? "").ToString().ToLowerInvariant();

						if (mfg.Contains("vmware") || model.Contains("vmware") ||
						    mfg.Contains("innotek") || model.Contains("virtualbox") ||
						    mfg.Contains("qemu") || model.Contains("qemu") ||
						    model.Contains("virtual machine") || mfg.Contains("xen") || model.Contains("kvm"))
						{
							fakePcAlertsCount++;
							allItems.Add(new RemoteAnyDeskEvidenceItem
							{
								Category = "Виртуальная машина (VM)",
								SourceArtifact = "Win32_ComputerSystem",
								ValueOrId = mfg + " / " + model,
								Status = "ВИРТУАЛИЗАЦИЯ ОБНАРУЖЕНА",
								ThreatLevel = "CRITICAL",
								DetailDescription = "🚨 Система работает внутри гипервизора / виртуальной машины (" + mfg + " / " + model + ")! Риск поддельного одноразового окружения для обхода ScreenShare.",
								TargetRule = "5.08",
								IsWhitelisted = false,
								Timestamp = DateTime.Now
							});
						}
					}
				}
			}
			catch { }

			// Проверка MAC-адресов
			try
			{
				NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in nics)
				{
					string mac = nic.GetPhysicalAddress().ToString();
					if (mac.Length == 12)
					{
						string formattedMac = string.Format("{0}:{1}:{2}", mac.Substring(0, 2), mac.Substring(2, 2), mac.Substring(4, 2)).ToUpperInvariant();
						foreach (string oui in VmOuis)
						{
							if (formattedMac.Equals(oui, StringComparison.OrdinalIgnoreCase))
							{
								fakePcAlertsCount++;
								allItems.Add(new RemoteAnyDeskEvidenceItem
								{
									Category = "Виртуальный MAC-адрес",
									SourceArtifact = nic.Name + " (" + nic.Description + ")",
									ValueOrId = "MAC: " + mac,
									Status = "OUI ГИПЕРВИЗОРА (" + oui + ")",
									ThreatLevel = "CRITICAL",
									DetailDescription = "🚨 Сетевой адаптер использует диапазон MAC-адресов виртуальных машин (" + oui + ").",
									TargetRule = "5.08",
									IsWhitelisted = false,
									Timestamp = DateTime.Now
								});
								break;
							}
						}
					}
				}
			}
			catch { }
		}

		private static IEnumerable<string> MinecraftRoots()
		{
			List<string> roots = new List<string>();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			roots.Add(Path.Combine(appData, ".minecraft"));
			roots.Add(Path.Combine(appData, "PrismLauncher"));
			roots.Add(Path.Combine(appData, "PolyMC"));
			roots.Add(Path.Combine(appData, "MultiMC"));
			roots.Add(Path.Combine(appData, ".feather"));
			roots.Add(Path.Combine(userProf, ".lunarclient", "offline", "multiver"));
			roots.Add(Path.Combine(appData, ".tlauncher", "legacy", "Minecraft", "game"));
			return roots;
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (RemoteAnyDeskEvidenceItem item in allItems)
			{
				if (currentFilter == RemoteAnyDeskFilterMode.FakePcAndEvasion && item.ThreatLevel != "CRITICAL") continue;
				if (currentFilter == RemoteAnyDeskFilterMode.ThirdPartyRemote && !item.Category.Contains("Удален") && !item.Category.Contains("Стороннее") && !item.Category.Contains("Параллельная")) continue;
				if (currentFilter == RemoteAnyDeskFilterMode.VmAndHypervisor && !item.Category.Contains("Виртуальн")) continue;
				if (currentFilter == RemoteAnyDeskFilterMode.MinecraftStatus && !item.Category.Contains("Minecraft") && !item.Category.Contains("Подмена компьютера")) continue;
				if (currentFilter == RemoteAnyDeskFilterMode.WhitelistedAnyDesk && !item.IsWhitelisted) continue;

				if (!string.IsNullOrEmpty(search))
				{
					string matchStr = (item.Category + " " + item.SourceArtifact + " " + item.ValueOrId + " " + item.DetailDescription).ToLowerInvariant();
					if (!matchStr.Contains(search)) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Category);
				lvi.SubItems.Add(item.SourceArtifact);
				lvi.SubItems.Add(item.ValueOrId);
				lvi.SubItems.Add(item.Status);
				lvi.SubItems.Add(item.ThreatLevel);
				lvi.SubItems.Add(item.DetailDescription);
				lvi.SubItems.Add(item.TargetRule);
				lvi.Tag = item;

				if (item.ThreatLevel == "CRITICAL")
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 15, 15);
				}
				else if (item.ThreatLevel == "WARNING")
				{
					lvi.ForeColor = Ui.AccentOrange;
				}
				else if (item.IsWhitelisted)
				{
					lvi.ForeColor = Ui.AccentGreen;
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
				ShowEmptyDetails();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			RemoteAnyDeskEvidenceItem item = listView.SelectedItems[0].Tag as RemoteAnyDeskEvidenceItem;
			if (item == null) return;

			// Анализ
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ АНАЛИЗ ПОДМЕНЫ ПК И СЕССИЙ УДАЛЕННОГО ДОСТУПА ===");
			sb.AppendLine("Категория проверки: " + item.Category);
			sb.AppendLine("Источник артефакта: " + item.SourceArtifact);
			sb.AppendLine("Значение / Сессия / Идентификатор: " + item.ValueOrId);
			sb.AppendLine("Статус детектора: " + item.Status);
			sb.AppendLine("Уровень угрозы: " + item.ThreatLevel);
			sb.AppendLine("Правило регламента: " + item.TargetRule);
			sb.AppendLine("Штамп времени: " + item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine();
			sb.AppendLine("Детальное криминалистическое заключение:");
			sb.AppendLine(item.DetailDescription);
			txtAnalysis.Text = sb.ToString();

			// Таймлайн
			StringBuilder tb = new StringBuilder();
			tb.AppendLine("=== ТАЙМЛАЙН ПОДКЛЮЧЕНИЙ И СОБЫТИЙ СИСТЕМЫ ===");
			foreach (RemoteAnyDeskEvidenceItem it in allItems)
			{
				tb.AppendLine("[" + it.Timestamp.ToString("HH:mm:ss") + "] [" + it.ThreatLevel + "] " + it.Category + " -> " + it.ValueOrId + " (" + it.Status + ")");
			}
			txtTimeline.Text = tb.ToString();

			// Вердикт
			StringBuilder vb = new StringBuilder();
			vb.AppendLine("=== КРИМИНАЛИСТИЧЕСКИЙ ВЕРДИКТ И ПРАВИЛА РЕГЛАМЕНТА ===");
			vb.AppendLine("Игрок: " + targetPlayerName);
			vb.AppendLine("Дата и время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			vb.AppendLine();
			if (item.ThreatLevel == "CRITICAL")
			{
				vb.AppendLine("НАРУШЕНИЕ: ПРАВИЛО 5.08 (СОКРЫТИЕ УЛИК / ПОДМЕНА ПК ИЛИ ПРОФИЛЯ ANYDESK)");
				vb.AppendLine("Улика: " + item.DetailDescription);
				vb.AppendLine("Наказание: Бан на 30 дней.");
				vb.AppendLine();
				vb.AppendLine("Команда бана для чата сервера:");
				vb.AppendLine("/ban " + targetPlayerName + " 30d 5.08 (" + item.Category + ": " + item.ValueOrId + ")");
			}
			else if (item.IsWhitelisted)
			{
				vb.AppendLine("СТАТУС: БЕЛЫЙ СПИСОК (РАЗРЕШЕНО)");
				vb.AppendLine("Сессия AnyDesk или RuDesktop принадлежит модератору проверки либо является штатной конфигурацией.");
				vb.AppendLine("Наказание не применяется.");
			}
			else
			{
				vb.AppendLine("СТАТУС: ТРЕБУЕТ ВНИМАНИЯ (ПРОВЕРКА СОПУТСТВУЮЩИХ УЛИК)");
				vb.AppendLine("Зафиксировано подозрительное состояние системы. Сравните время закрытия игры с логами сервера.");
			}
			txtBanVerdict.Text = vb.ToString();
		}

		private void ShowEmptyDetails()
		{
			txtAnalysis.Text = "Артефакты не выбраны.";
			txtTimeline.Text = "";
			txtBanVerdict.Text = "";
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count > 0)
			{
				RemoteAnyDeskEvidenceItem item = listView.SelectedItems[0].Tag as RemoteAnyDeskEvidenceItem;
				if (item != null && item.ThreatLevel == "CRITICAL")
				{
					string cmd = "/ban " + targetPlayerName + " 30d 5.08 (" + item.Category + ": " + item.ValueOrId + ")";
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			string defCmd = "/ban " + targetPlayerName + " 30d 5.08 (Подмена компьютера / AnyDesk Evasion)";
			Clipboard.SetText(defCmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n\n" + defCmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine("- [MCCheatChecker] СУДЕБНЫЙ АУДИТ ПОДМЕНЫ ПК И СЕССИЙ ANYDESK/RDP");
			sb.AppendLine("+ Игрок: " + targetPlayerName);
			sb.AppendLine("+ Время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("+ Статус Minecraft: " + lblKpiMinecraftStatus.Text);
			sb.AppendLine("+ Профилей AnyDesk: " + lblKpiAnydeskProfiles.Text);
			sb.AppendLine("+ Сессий удаленного доступа: " + lblKpiRemoteSessions.Text);
			sb.AppendLine("+ Критических алертов (5.08): " + lblKpiFakePcAlerts.Text);
			sb.AppendLine();
			int critCount = 0;
			foreach (RemoteAnyDeskEvidenceItem it in allItems)
			{
				if (it.ThreatLevel == "CRITICAL" && critCount < 5)
				{
					sb.AppendLine("! [5.08] " + it.Category + " -> " + it.ValueOrId + " : " + it.DetailDescription);
					critCount++;
				}
			}
			if (critCount == 0)
			{
				sb.AppendLine("+ Подмены компьютера, сторонних параллельных сессий чистильщика и поддельных AnyDesk ID не выявлено.");
			}
			sb.AppendLine("```");
			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord скопирована в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), "mccheatchecker_remote_anydesk_report_" + DateTime.Now.Ticks + ".html");
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Remote & AnyDesk Integrity Report</title>");
				html.AppendLine("<style>body{background:#0e1117;color:#c9d1d9;font-family:Segoe UI,sans-serif;padding:24px;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #30363d;padding:8px 12px;font-size:12px;text-align:left;} th{background:#161b22;color:#58a6ff;} tr:nth-child(even){background:#12161f;} .crit{color:#f85149;font-weight:bold;} .warn{color:#d29922;} .safe{color:#3fb950;}</style></head><body>");
				html.AppendLine("<h2>🖥️ Криминалистический отчёт по подмене ПК, AnyDesk и удаленным сессиям</h2>");
				html.AppendLine("<p><b>Игрок:</b> " + targetPlayerName + " | <b>Дата:</b> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");
				html.AppendLine("<p><b>Статус игры:</b> " + lblKpiMinecraftStatus.Text + " | <b>AnyDesk ID:</b> " + lblKpiAnydeskProfiles.Text + " | <b>Критических алертов:</b> " + lblKpiFakePcAlerts.Text + "</p>");
				html.AppendLine("<table><tr><th>Категория</th><th>Источник</th><th>Значение</th><th>Статус</th><th>Угроза</th><th>Описание</th><th>Правило</th></tr>");
				foreach (RemoteAnyDeskEvidenceItem it in allItems)
				{
					string cls = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
					html.AppendLine("<tr><td>" + it.Category + "</td><td>" + it.SourceArtifact + "</td><td>" + it.ValueOrId + "</td><td>" + it.Status + "</td><td class='" + cls + "'>" + it.ThreatLevel + "</td><td>" + it.DetailDescription + "</td><td>" + it.TargetRule + "</td></tr>");
				}
				html.AppendLine("</table></body></html>");
				File.WriteAllText(tempPath, html.ToString(), Encoding.UTF8);
				Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка экспорта HTML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
