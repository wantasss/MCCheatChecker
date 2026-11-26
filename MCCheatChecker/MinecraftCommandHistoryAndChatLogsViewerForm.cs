using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class MinecraftCommandHistoryAndChatLogsViewerForm : Form
	{
		#region Enums & Models

		public enum CommandItemCategory
		{
			CheatCommand501,
			BaritoneBot501,
			EvasionAndTampering508,
			ServerAuthAndLogin,
			StandardServerCommand,
			ChatLogCommand
		}

		public enum CommandFilterCategory
		{
			All,
			ViolationsOnly,
			CheatCommands,
			BaritoneBots,
			EvasionTampering,
			StandardCommands
		}

		public class CommandForensicItem
		{
			public int Index;
			public CommandItemCategory Category;
			public string CategoryName;
			public string RawCommand;
			public string DisplayCommand;
			public string DetectedPattern;
			public string SourceFilePath;
			public string SourceFileName;
			public int LineNumber;
			public DateTime FileLastModified;
			public long FileSizeBytes;
			public string TargetRule;
			public string Verdict;
			public bool IsViolation;
			public bool IsCheatCommand;
			public bool IsBaritone;
			public bool IsTampering;
			public Color RowColor;
			public string ForensicReport;
			public string SyntaxContext;
			public string FileIntegrityDetails;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private CommandFilterCategory currentFilter = CommandFilterCategory.All;
		private List<CommandForensicItem> allItems = new List<CommandForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtForensicReport;
		private DarkTextBoxMultiline txtSyntaxContext;
		private DarkTextBoxMultiline txtFileIntegrity;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiCheatCommands;
		private Label lblKpiBaritone;
		private Label lblKpiTampering;
		private Label lblKpiStandard;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterBaritone;
		private DarkButton btnFilterTampering;
		private DarkButton btnFilterStandard;

		private DarkButton btnCopySelected;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnExportHtml;
		private DarkButton btnRescan;
		private DarkButton btnClose;

		private TabControl detailTabs;

		#endregion

		public MinecraftCommandHistoryAndChatLogsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Construction

		private void InitializeComponent()
		{
			Text = "📜 Инспектор истории команд Minecraft, чат-логов и чит-префиксов — MCCheatChecker";
			Size = new Size(1340, 820);
			MinimumSize = new Size(1100, 640);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;

			// Header Panel
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 72;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(16, 10, 16, 10);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "📜 ИНСПЕКТОР ИСТОРИИ КОМАНД MINECRAFT, ЧАТ-ЛОГОВ И ЧИТ-ПРЕФИКСОВ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(14, 10);
			headerPanel.Controls.Add(lblHeaderTitle);

			Label lblHeaderSub = new Label();
			lblHeaderSub.Text = "Аудит файлов command_history.txt, префиксов читов (., #, -, !, @), детекция очистки/удаления истории и чат-логов (5.01 / 5.08)";
			lblHeaderSub.Font = Ui.FontBody(8.5f);
			lblHeaderSub.ForeColor = Ui.TextSecondary;
			lblHeaderSub.AutoSize = true;
			lblHeaderSub.Location = new Point(15, 36);
			headerPanel.Controls.Add(lblHeaderSub);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ИСТОРИИ КОМАНД...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Color.Black;
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.AutoSize = false;
			lblStatusPill.Size = new Size(250, 26);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Location = new Point(1060, 12);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblStatusPill);

			// Top Container
			Panel topContainer = new Panel();
			topContainer.Dock = DockStyle.Top;
			topContainer.Height = 138;
			topContainer.Controls.Add(headerPanel);

			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Bottom;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.Surface;
			kpiPanel.Padding = new Padding(14, 4, 14, 6);

			int kpiX = 14;
			kpiPanel.Controls.Add(CreateKpiBadge("📜 Всего команд", "-", Ui.TextPrimary, ref kpiX, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 Нарушений (5.01/5.08)", "0", Ui.AccentRed, ref kpiX, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💉 Чит-команд (5.01)", "0", Ui.AccentRed, ref kpiX, out lblKpiCheatCommands));
			kpiPanel.Controls.Add(CreateKpiBadge("🤖 Baritone бот-команд", "0", Ui.AccentOrange, ref kpiX, out lblKpiBaritone));
			kpiPanel.Controls.Add(CreateKpiBadge("🗑️ Очистка / Сокрытие (5.08)", "0", Ui.AccentOrange, ref kpiX, out lblKpiTampering));
			kpiPanel.Controls.Add(CreateKpiBadge("✔️ Обычных команд", "0", Ui.AccentGreen, ref kpiX, out lblKpiStandard));
			topContainer.Controls.Add(kpiPanel);

			Controls.Add(topContainer);

			// Filter Bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 46;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(14, 7, 14, 7);

			int fBtnX = 14;
			btnFilterAll = CreateFilterButton("Все команды", CommandFilterCategory.All, ref fBtnX, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения (5.01/5.08)", CommandFilterCategory.ViolationsOnly, ref fBtnX, filterBar);
			btnFilterCheats = CreateFilterButton("💉 Чит-команды (5.01)", CommandFilterCategory.CheatCommands, ref fBtnX, filterBar);
			btnFilterBaritone = CreateFilterButton("🤖 Baritone (5.01)", CommandFilterCategory.BaritoneBots, ref fBtnX, filterBar);
			btnFilterTampering = CreateFilterButton("🗑️ Сокрытие (5.08)", CommandFilterCategory.EvasionTampering, ref fBtnX, filterBar);
			btnFilterStandard = CreateFilterButton("✔️ Обычные команды", CommandFilterCategory.StandardCommands, ref fBtnX, filterBar);

			// Search Box
			Label lblSearch = new Label();
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.Font = Ui.FontBody(8.5f);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(fBtnX + 16, 13);
			filterBar.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(fBtnX + 78, 10);
			searchBox.Size = new Size(200, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Controls.Add(filterBar);

			// SplitContainer
			SplitContainer splitContainer = new SplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Horizontal;
			splitContainer.SplitterDistance = 330;
			splitContainer.SplitterWidth = 5;
			splitContainer.BackColor = Ui.Border;

			// DarkListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;

			listView.Columns.Add("№", 45, HorizontalAlignment.Left);
			listView.Columns.Add("Категория", 155, HorizontalAlignment.Left);
			listView.Columns.Add("Введенная команда", 260, HorizontalAlignment.Left);
			listView.Columns.Add("Обнаруженный паттерн", 180, HorizontalAlignment.Left);
			listView.Columns.Add("Источник / Файл", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Регламент", 110, HorizontalAlignment.Left);
			listView.Columns.Add("Судебный вердикт", 260, HorizontalAlignment.Left);
			listView.Columns.Add("Время изменения файла", 140, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate { UpdateDetailViews(); };
			splitContainer.Panel1.Controls.Add(listView);

			// Bottom Container
			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;

			Panel actionBar = new Panel();
			actionBar.Dock = DockStyle.Bottom;
			actionBar.Height = 44;
			actionBar.BackColor = Ui.Surface;
			actionBar.Padding = new Padding(12, 7, 12, 7);

			btnCopySelected = new DarkButton();
			btnCopySelected.Text = "📋 Копировать запись";
			btnCopySelected.Size = new Size(160, 28);
			btnCopySelected.Location = new Point(12, 8);
			btnCopySelected.Click += delegate { CopySelectedRow(); };
			actionBar.Controls.Add(btnCopySelected);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "💬 Копировать для Discord";
			btnCopyDiscord.Size = new Size(185, 28);
			btnCopyDiscord.Location = new Point(180, 8);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			actionBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Команда бана (/ban)";
			btnCopyBanCmd.Size = new Size(185, 28);
			btnCopyBanCmd.Location = new Point(373, 8);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionBar.Controls.Add(btnCopyBanCmd);

			btnExportHtml = new DarkButton();
			btnExportHtml.Text = "🌐 Экспорт в HTML";
			btnExportHtml.Size = new Size(150, 28);
			btnExportHtml.Location = new Point(566, 8);
			btnExportHtml.Click += delegate { ExportToHtml(); };
			actionBar.Controls.Add(btnExportHtml);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Size = new Size(155, 28);
			btnRescan.Location = new Point(724, 8);
			btnRescan.Click += delegate { StartScan(); };
			actionBar.Controls.Add(btnRescan);

			btnClose = new DarkButton();
			btnClose.Text = "✖️ Закрыть";
			btnClose.Size = new Size(100, 28);
			btnClose.Location = new Point(887, 8);
			btnClose.Click += delegate { Close(); };
			actionBar.Controls.Add(btnClose);

			bottomContainer.Controls.Add(actionBar);

			// Detail Tabs
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			detailTabs.Padding = new Point(12, 6);

			TabPage tabReport = new TabPage("📋 Судебный протокол / Регламент");
			tabReport.BackColor = Ui.Surface;
			txtForensicReport = new DarkTextBoxMultiline();
			txtForensicReport.Dock = DockStyle.Fill;
			txtForensicReport.ReadOnly = true;
			txtForensicReport.Font = Ui.FontMono(9f);
			tabReport.Controls.Add(txtForensicReport);
			detailTabs.TabPages.Add(tabReport);

			TabPage tabContext = new TabPage("🔍 Контекст команды и параметры синтаксиса");
			tabContext.BackColor = Ui.Surface;
			txtSyntaxContext = new DarkTextBoxMultiline();
			txtSyntaxContext.Dock = DockStyle.Fill;
			txtSyntaxContext.ReadOnly = true;
			txtSyntaxContext.Font = Ui.FontMono(9f);
			tabContext.Controls.Add(txtSyntaxContext);
			detailTabs.TabPages.Add(tabContext);

			TabPage tabIntegrity = new TabPage("📁 Файловая система и аудит целостности");
			tabIntegrity.BackColor = Ui.Surface;
			txtFileIntegrity = new DarkTextBoxMultiline();
			txtFileIntegrity.Dock = DockStyle.Fill;
			txtFileIntegrity.ReadOnly = true;
			txtFileIntegrity.Font = Ui.FontMono(9f);
			tabIntegrity.Controls.Add(txtFileIntegrity);
			detailTabs.TabPages.Add(tabIntegrity);

			bottomContainer.Controls.Add(detailTabs);
			splitContainer.Panel2.Controls.Add(bottomContainer);

			Controls.Add(splitContainer);
		}

		private Panel CreateKpiBadge(string title, string initialValue, Color accentColor, ref int curX, out Label valLabel)
		{
			Panel pnl = new Panel();
			pnl.Size = new Size(170, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.SurfaceAlt;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(164, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(164, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 178;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, CommandFilterCategory cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8f);
			btn.Size = new Size(150, 28);
			btn.Location = new Point(curX, 8);
			btn.Click += delegate
			{
				currentFilter = cat;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.BackColor = (currentFilter == CommandFilterCategory.All) ? Ui.AccentTeal : Ui.Surface;
			btnFilterAll.ForeColor = (currentFilter == CommandFilterCategory.All) ? Color.Black : Ui.TextPrimary;

			btnFilterViolations.BackColor = (currentFilter == CommandFilterCategory.ViolationsOnly) ? Ui.AccentRed : Ui.Surface;
			btnFilterViolations.ForeColor = (currentFilter == CommandFilterCategory.ViolationsOnly) ? Color.White : Ui.TextPrimary;

			btnFilterCheats.BackColor = (currentFilter == CommandFilterCategory.CheatCommands) ? Ui.AccentRed : Ui.Surface;
			btnFilterCheats.ForeColor = (currentFilter == CommandFilterCategory.CheatCommands) ? Color.White : Ui.TextPrimary;

			btnFilterBaritone.BackColor = (currentFilter == CommandFilterCategory.BaritoneBots) ? Ui.AccentOrange : Ui.Surface;
			btnFilterBaritone.ForeColor = (currentFilter == CommandFilterCategory.BaritoneBots) ? Color.Black : Ui.TextPrimary;

			btnFilterTampering.BackColor = (currentFilter == CommandFilterCategory.EvasionTampering) ? Ui.AccentOrange : Ui.Surface;
			btnFilterTampering.ForeColor = (currentFilter == CommandFilterCategory.EvasionTampering) ? Color.Black : Ui.TextPrimary;

			btnFilterStandard.BackColor = (currentFilter == CommandFilterCategory.StandardCommands) ? Ui.AccentGreen : Ui.Surface;
			btnFilterStandard.ForeColor = (currentFilter == CommandFilterCategory.StandardCommands) ? Color.Black : Ui.TextPrimary;
		}

		#endregion

		#region Scanning Logic

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ СКАНИРОВАНИЕ ИСТОРИИ КОМАНД...";
			lblStatusPill.BackColor = Ui.AccentOrange;
			lblStatusPill.ForeColor = Color.Black;

			if (scanWorker != null && scanWorker.IsBusy)
			{
				return;
			}

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<CommandForensicItem> results = new List<CommandForensicItem>();
			int itemIndex = 1;

			// List of target command files
			List<string> commandFiles = new List<string>();

			try
			{
				foreach (string root in ManualChecks.MinecraftRoots())
				{
					try
					{
						// Check main root command_history.txt
						string ch = Path.Combine(root, "command_history.txt");
						if (File.Exists(ch))
						{
							if (!commandFiles.Contains(ch)) commandFiles.Add(ch);
						}

						// Check instances directory (MultiMC, PrismLauncher, PolyMC, etc.)
						string instances = Path.Combine(root, "instances");
						if (Directory.Exists(instances))
						{
							try
							{
								string[] dirs = Directory.GetDirectories(instances);
								foreach (string instDir in dirs)
								{
									string sub1 = Path.Combine(instDir, ".minecraft", "command_history.txt");
									if (File.Exists(sub1) && !commandFiles.Contains(sub1)) commandFiles.Add(sub1);

									string sub2 = Path.Combine(instDir, "command_history.txt");
									if (File.Exists(sub2) && !commandFiles.Contains(sub2)) commandFiles.Add(sub2);
								}
							}
							catch { }
						}
					}
					catch { }
				}
			}
			catch { }

			// Known Cheat Command Signatures
			string[] cheatCommandsExact = new string[]
			{
				".vape", ".bind", ".unbind", ".damage", ".friend", ".target", ".config", ".cfg",
				".panic", ".selfdestruct", ".irc", ".tp", ".teleport", ".fly", ".speed", ".killaura",
				".aura", ".aimbot", ".reach", ".hitbox", ".freecam", ".esp", ".velocity", ".nofall",
				".jesus", ".step", ".fastplace", ".timer", ".hclip", ".vclip", ".clip", ".say",
				".celestial", ".expensive", ".nursultan", ".akrien", ".kura", ".minced", ".flint",
				".rockstar", ".boberware", ".tenacity", ".novoline", ".riseclient", ".thunderhack",
				".liquidbounce", ".kmbox", ".spoofer", ".zamorozka", ".exloader", ".systemdlc",
				".memesense", ".midnight", ".neverlose", ".gamesense", ".fatality", ".weave",
				".matix", ".wurst", ".meteor", ".future", ".rusherhack", ".kamiblue", ".aristois",
				".pyro", ".konas", ".phobos", ".fanta", ".exhibition", ".intent", ".flux",
				".zeroday", ".hummingbird", ".alacrity", ".entropy", ".whiteout", ".koid",
				".phantom", ".dream", ".wild", ".exire", "-bind", "-config", "-toggle", "-load",
				"-set", "!bind", "!config", "!toggle", "@bind", "@config", "@toggle"
			};

			string[] baritoneCommands = new string[]
			{
				"#goto", "#mine", "#farm", "#follow", "#path", "#tunnel", "#stop", "#explore",
				"#sel", "#schematic", "#build", "#surface", "#cancel", "#pause", "#resume",
				"#click", "#invert", "#bhome", "#sethome", "#waypoints", "#find", "#eta",
				"#proc", "#repackage", "#save", "#load", "#rescan", "#farm", "#clear"
			};

			// Process each found command_history.txt file
			foreach (string filePath in commandFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(filePath);
					DateTime lastMod = fi.LastWriteTime;
					long size = fi.Length;

					// Check for Empty / Cleared File (Tampering 5.08)
					if (size == 0)
					{
						CommandForensicItem emptyItem = new CommandForensicItem
						{
							Index = itemIndex++,
							Category = CommandItemCategory.EvasionAndTampering508,
							CategoryName = "Очистка истории",
							RawCommand = "<ФАЙЛ ПОЛНОСТЬЮ ОЧИЩЕН (0 БАЙТ)>",
							DisplayCommand = "<ФАЙЛ ПОЛНОСТЬЮ ОЧИЩЕН (0 БАЙТ)>",
							DetectedPattern = "Файл истории команд command_history.txt пуст",
							SourceFilePath = filePath,
							SourceFileName = Path.GetFileName(filePath),
							LineNumber = 0,
							FileLastModified = lastMod,
							FileSizeBytes = size,
							TargetRule = "5.08 Сокрытие улик",
							Verdict = "🚨 5.08 СОКРЫТИЕ УЛИК: Файл command_history.txt был очищен перед проверкой!",
							IsViolation = true,
							IsCheatCommand = false,
							IsBaritone = false,
							IsTampering = true,
							RowColor = Ui.AccentRed
						};
						emptyItem.ForensicReport = BuildForensicReport(emptyItem);
						emptyItem.SyntaxContext = "Файл был обнулен (размер: 0 байт).\r\nВремя изменения: " + lastMod.ToString("dd.MM.yyyy HH:mm:ss");
						emptyItem.FileIntegrityDetails = BuildFileIntegrity(emptyItem, fi);
						results.Add(emptyItem);
						continue;
					}

					string[] lines = File.ReadAllLines(filePath);
					int lineNum = 1;

					foreach (string line in lines)
					{
						string cmd = (line ?? "").Trim();
						if (string.IsNullOrEmpty(cmd))
						{
							lineNum++;
							continue;
						}

						string cmdLower = cmd.ToLowerInvariant();

						bool isCheat = false;
						bool isBaritone = false;
						bool isAuth = false;
						string pattern = "";

						// 1. Check Baritone
						foreach (string bCmd in baritoneCommands)
						{
							if (cmdLower == bCmd || cmdLower.StartsWith(bCmd + " "))
							{
								isBaritone = true;
								pattern = "Baritone Bot Command: " + bCmd;
								break;
							}
						}

						// 2. Check Cheat Commands
						if (!isBaritone)
						{
							foreach (string cCmd in cheatCommandsExact)
							{
								if (cmdLower == cCmd || cmdLower.StartsWith(cCmd + " "))
								{
									isCheat = true;
									pattern = "Чит-команда: " + cCmd;
									break;
								}
							}
						}

						// 3. Check for generic cheat dot prefixes (.killaura, .reach, .speed, etc.)
						if (!isCheat && !isBaritone && cmd.StartsWith(".") && cmd.Length > 2 && !cmd.StartsWith("..") && !cmd.StartsWith("./"))
						{
							isCheat = true;
							pattern = "Чит-префикс точки (Dot Prefix): " + cmd.Split(' ')[0];
						}

						// 4. Check for Auth commands (/login, /reg) -> Mask password
						string displayCmd = cmd;
						if (cmdLower.StartsWith("/login ") || cmdLower.StartsWith("/l ") ||
							cmdLower.StartsWith("/register ") || cmdLower.StartsWith("/reg ") ||
							cmdLower.StartsWith("/changepassword ") || cmdLower.StartsWith("/cp "))
						{
							isAuth = true;
							string[] parts = cmd.Split(' ');
							if (parts.Length > 1)
							{
								displayCmd = parts[0] + " " + new string('*', parts[1].Length);
								if (parts.Length > 2) displayCmd += " " + new string('*', parts[2].Length);
							}
							pattern = "Команда авторизации на сервере";
						}

						CommandItemCategory cat;
						string rule;
						string verdict;
						Color rowColor;

						if (isCheat)
						{
							cat = CommandItemCategory.CheatCommand501;
							rule = "5.01 Использование читов";
							verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ (Зафиксирована чит-команда в истории!)";
							rowColor = Ui.AccentRed;
						}
						else if (isBaritone)
						{
							cat = CommandItemCategory.BaritoneBot501;
							rule = "5.01 Использование читов";
							verdict = "🚨 5.01 ИСПОЛЬЗОВАНИЕ ЧИТОВ (Автоматизация Baritone Bot)";
							rowColor = Ui.AccentOrange;
						}
						else if (isAuth)
						{
							cat = CommandItemCategory.ServerAuthAndLogin;
							rule = "Allowed";
							verdict = "🔒 Аутентификация на сервере (Пароль скрыт в целях приватности)";
							rowColor = Ui.TextSecondary;
						}
						else
						{
							cat = CommandItemCategory.StandardServerCommand;
							rule = "Allowed";
							verdict = "✔️ Стандартная игровая команда";
							rowColor = Ui.TextPrimary;
						}

						CommandForensicItem item = new CommandForensicItem
						{
							Index = itemIndex++,
							Category = cat,
							CategoryName = (cat == CommandItemCategory.CheatCommand501) ? "Чит-команда" :
										   ((cat == CommandItemCategory.BaritoneBot501) ? "Baritone Бот" :
										   ((cat == CommandItemCategory.ServerAuthAndLogin) ? "Авторизация" : "Игровая команда")),
							RawCommand = cmd,
							DisplayCommand = displayCmd,
							DetectedPattern = string.IsNullOrEmpty(pattern) ? "Обычная команда" : pattern,
							SourceFilePath = filePath,
							SourceFileName = Path.GetFileName(filePath),
							LineNumber = lineNum,
							FileLastModified = lastMod,
							FileSizeBytes = size,
							TargetRule = rule,
							Verdict = verdict,
							IsViolation = isCheat || isBaritone,
							IsCheatCommand = isCheat,
							IsBaritone = isBaritone,
							IsTampering = false,
							RowColor = rowColor
						};

						item.ForensicReport = BuildForensicReport(item);
						item.SyntaxContext = BuildSyntaxContext(item);
						item.FileIntegrityDetails = BuildFileIntegrity(item, fi);

						results.Add(item);
						lineNum++;
					}
				}
				catch { }
			}

			e.Result = results;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			allItems = e.Result as List<CommandForensicItem>;
			if (allItems == null)
			{
				allItems = new List<CommandForensicItem>();
			}

			// Update KPI counters
			int total = allItems.Count;
			int violations = 0;
			int cheatCmds = 0;
			int baritoneCmds = 0;
			int tamperingCount = 0;
			int standardCmds = 0;

			foreach (CommandForensicItem item in allItems)
			{
				if (item.IsViolation) violations++;
				if (item.IsCheatCommand) cheatCmds++;
				if (item.IsBaritone) baritoneCmds++;
				if (item.IsTampering) tamperingCount++;
				if (!item.IsViolation) standardCmds++;
			}

			lblKpiTotal.Text = total.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiCheatCommands.Text = cheatCmds.ToString();
			lblKpiBaritone.Text = baritoneCmds.ToString();
			lblKpiTampering.Text = tamperingCount.ToString();
			lblKpiStandard.Text = standardCmds.ToString();

			if (violations > 0 || tamperingCount > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВЫЯВЛЕНО НАРУШЕНИЙ: {0}", violations + tamperingCount);
				lblStatusPill.BackColor = Ui.AccentRed;
				lblStatusPill.ForeColor = Color.White;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔️ ВСЕГО КОМАНД: {0} | ЧИСТО", total);
				lblStatusPill.BackColor = Ui.AccentGreen;
				lblStatusPill.ForeColor = Color.Black;
			}

			HighlightFilterButtons();
			ApplyFilter();
		}

		#endregion

		#region Report Builders

		private static string BuildForensicReport(CommandForensicItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ: ИСТОРИЯ КОМАНД MINECRAFT (COMMAND_HISTORY.TXT) ===");
			sb.AppendLine(string.Format("Подозреваемый игрок: {0}", "Player"));
			sb.AppendLine(string.Format("Дата фиксации: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("Категория: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Введенная команда: \"{0}\"", item.DisplayCommand));
			sb.AppendLine(string.Format("Обнаруженный паттерн: {0}", item.DetectedPattern));
			sb.AppendLine(string.Format("Файл-источник: {0}", item.SourceFilePath));
			sb.AppendLine(string.Format("Номер строки: {0}", item.LineNumber));
			sb.AppendLine(string.Format("Время изменения файла: {0:dd.MM.yyyy HH:mm:ss}", item.FileLastModified));
			sb.AppendLine(string.Format("Регламент проверки: {0}", item.TargetRule));
			sb.AppendLine(string.Format("Судебный вердикт: {0}", item.Verdict));
			sb.AppendLine();

			if (item.IsCheatCommand)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 (ИСПОЛЬЗОВАНИЕ ЧИТОВ):");
				sb.AppendLine("В файле command_history.txt обнаружена команда с синтаксисом чит-клиента.");
				sb.AppendLine("Файл command_history.txt автоматически ведется движком Minecraft и хранит");
				sb.AppendLine("команды, введенные игроком в чат. Ввод команд через точку (.) или префиксы читов");
				sb.AppendLine("невозможен на стороне сервера и подтверждает наличие активного чит-мода.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование читов (История команд: {1})", "Player", item.DisplayCommand));
			}
			else if (item.IsBaritone)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.01 (АВТОМАТИЗАЦИЯ / БОТЫ):");
				sb.AppendLine("Зафиксирована команда автоматизации искусственного интеллекта Baritone (#goto, #mine).");
				sb.AppendLine("Использование автономных ботов поиска пути и копки ресурсов запрещено правилами.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.01 Использование ботов (Baritone: {1})", "Player", item.DisplayCommand));
			}
			else if (item.IsTampering)
			{
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine("🚨 ЗАКЛЮЧЕНИЕ СУДЕБНОГО ЭКСПЕРТА ПО СТАТЬЕ 5.08 (СОКРЫТИЕ УЛИК):");
				sb.AppendLine("Файл истории команд был полностью очищен (0 байт) перед вызовом на проверку.");
				sb.AppendLine("Умышленное уничтожение истории ввода команд расценивается как сокрытие софта.");
				sb.AppendLine("----------------------------------------------------------------------");
				sb.AppendLine(string.Format("РЕКОМЕНДУЕМАЯ КОМАНДА БАНА:\r\n/ban {0} 30d 5.08 Сокрытие софта (Очистка файла command_history.txt)", "Player"));
			}
			else
			{
				sb.AppendLine("Команда является стандартной серверной командой или сообщением в чат.");
			}

			return sb.ToString();
		}

		private static string BuildSyntaxContext(CommandForensicItem item)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СИНТАКСИЧЕСКИЙ АНАЛИЗ И КОНТЕКСТ КОМАНДЫ ===");
			sb.AppendLine(string.Format("Команда: \"{0}\"", item.DisplayCommand));
			sb.AppendLine(string.Format("Длина строки: {0} символов", item.RawCommand.Length));
			sb.AppendLine(string.Format("Префикс команды: '{0}'", item.RawCommand.Length > 0 ? item.RawCommand[0].ToString() : ""));
			sb.AppendLine(string.Format("Классификация паттерна: {0}", item.DetectedPattern));
			sb.AppendLine();
			sb.AppendLine("ОБЪЯСНЕНИЕ ПАТТЕРНА:");
			if (item.IsCheatCommand)
			{
				sb.AppendLine("Синтаксис типа .vape, .bind, .killaura, .reach используется чит-клиентами для:");
				sb.AppendLine(" • Настройки параметров модулей (радиус киллауры, хитбоксы, автокликер)");
				sb.AppendLine(" • Привязки клавиш активации (.bind add killaura r)");
				sb.AppendLine(" • Загрузки и сохранения профилей конфигурации (.config load legit)");
				sb.AppendLine(" • Экстренного самоуничтожения (.selfdestruct / .panic)");
			}
			else if (item.IsBaritone)
			{
				sb.AppendLine("Синтаксис типа #goto, #mine, #tunnel используется модом Baritone для:");
				sb.AppendLine(" • Автоматического перемещения к координатам или блокам руды");
				sb.AppendLine(" • Автономного копания шахт, тоннелей и фарма мобов");
			}
			else
			{
				sb.AppendLine("Стандартный синтаксис игрового сервера Minecraft.");
			}

			return sb.ToString();
		}

		private static string BuildFileIntegrity(CommandForensicItem item, FileInfo fi)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ФАЙЛОВАЯ СИСТЕМА И ЦЕЛОСТНОСТЬ АРТЕФАКТА ===");
			sb.AppendLine(string.Format("Полный путь: {0}", item.SourceFilePath));
			sb.AppendLine(string.Format("Имя файла:   {0}", item.SourceFileName));
			sb.AppendLine(string.Format("Номер строки в файле: {0}", item.LineNumber));
			sb.AppendLine();

			if (fi != null && fi.Exists)
			{
				sb.AppendLine(string.Format("Размер файла:      {0:N0} байт", fi.Length));
				sb.AppendLine(string.Format("Время создания:    {0:dd.MM.yyyy HH:mm:ss}", fi.CreationTime));
				sb.AppendLine(string.Format("Время изменения:   {0:dd.MM.yyyy HH:mm:ss}", fi.LastWriteTime));
				sb.AppendLine(string.Format("Время доступа:     {0:dd.MM.yyyy HH:mm:ss}", fi.LastAccessTime));
				sb.AppendLine(string.Format("Атрибуты файла:    {0}", fi.Attributes));
			}
			else
			{
				sb.AppendLine("Файл не найден на диске (возможно, удален во время сканирования).");
			}

			return sb.ToString();
		}

		#endregion

		#region Filter & List Rendering

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (CommandForensicItem item in allItems)
			{
				if (currentFilter == CommandFilterCategory.ViolationsOnly && !item.IsViolation)
				{
					continue;
				}
				if (currentFilter == CommandFilterCategory.CheatCommands && !item.IsCheatCommand)
				{
					continue;
				}
				if (currentFilter == CommandFilterCategory.BaritoneBots && !item.IsBaritone)
				{
					continue;
				}
				if (currentFilter == CommandFilterCategory.EvasionTampering && !item.IsTampering)
				{
					continue;
				}
				if (currentFilter == CommandFilterCategory.StandardCommands && item.IsViolation)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					bool match = item.DisplayCommand.ToLowerInvariant().Contains(search) ||
								 item.DetectedPattern.ToLowerInvariant().Contains(search) ||
								 item.SourceFileName.ToLowerInvariant().Contains(search) ||
								 item.TargetRule.ToLowerInvariant().Contains(search) ||
								 item.Verdict.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.DisplayCommand);
				lvi.SubItems.Add(item.DetectedPattern);
				lvi.SubItems.Add(string.Format("{0}:{1}", item.SourceFileName, item.LineNumber));
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.FileLastModified.ToString("dd.MM.yyyy HH:mm:ss"));

				lvi.Tag = item;
				lvi.ForeColor = item.RowColor;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtForensicReport.Clear();
				txtSyntaxContext.Clear();
				txtFileIntegrity.Clear();
			}
		}

		private void UpdateDetailViews()
		{
			if (listView.SelectedItems.Count == 0) return;
			CommandForensicItem item = listView.SelectedItems[0].Tag as CommandForensicItem;
			if (item == null) return;

			txtForensicReport.Text = item.ForensicReport;
			txtSyntaxContext.Text = item.SyntaxContext;
			txtFileIntegrity.Text = item.FileIntegrityDetails;
		}

		#endregion

		#region Actions & Exports

		private void CopySelectedRow()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите команду для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			CommandForensicItem item = listView.SelectedItems[0].Tag as CommandForensicItem;
			if (item == null) return;

			string text = string.Format("[{0}] \"{1}\" | Паттерн: {2} | Файл: {3}:{4} | Вердикт: {5}",
				item.CategoryName, item.DisplayCommand, item.DetectedPattern, item.SourceFileName, item.LineNumber, item.Verdict);

			Clipboard.SetText(text);
			MessageBox.Show("Запись успешно скопирована в буфер обмена!", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись для копирования отчета Discord.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			CommandForensicItem item = listView.SelectedItems[0].Tag as CommandForensicItem;
			if (item == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			if (item.IsViolation)
			{
				sb.AppendLine("- [НАРУШЕНИЕ РЕГЛАМЕНТА " + item.TargetRule + "]");
			}
			else
			{
				sb.AppendLine("+ [ИСТОРИЯ КОМАНД: ЛЕГИТИМНО]");
			}
			sb.AppendLine(string.Format("Подозреваемый: {0}", targetPlayerName));
			sb.AppendLine(string.Format("Категория: {0}", item.CategoryName));
			sb.AppendLine(string.Format("Команда: \"{0}\"", item.DisplayCommand));
			sb.AppendLine(string.Format("Паттерн: {0}", item.DetectedPattern));
			sb.AppendLine(string.Format("Файл: {0}:{1}", item.SourceFileName, item.LineNumber));
			sb.AppendLine(string.Format("Вердикт: {0}", item.Verdict));
			sb.AppendLine("```");

			if (item.IsViolation)
			{
				sb.AppendLine(string.Format("**Команда наказания:** `/ban {0} 30d {1}`", targetPlayerName, item.TargetRule + " (Чит-команда)"));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord успешно скопирован в буфер обмена!", "Discord Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите запись с нарушением для генерации команды бана.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			CommandForensicItem item = listView.SelectedItems[0].Tag as CommandForensicItem;
			if (item == null) return;

			string cmd = "";
			if (item.IsCheatCommand)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование читов (История команд: {1})", targetPlayerName, item.DisplayCommand);
			}
			else if (item.IsBaritone)
			{
				cmd = string.Format("/ban {0} 30d 5.01 Использование ботов (Baritone: {1})", targetPlayerName, item.DisplayCommand);
			}
			else if (item.IsTampering)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие софта (Очистка файла command_history.txt)", targetPlayerName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, item.TargetRule);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\r\n\r\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportToHtml()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "HTML Files (*.html)|*.html";
			sfd.FileName = string.Format("MCCheatChecker_CommandHistory_{0}_{1:yyyyMMdd_HHmmss}.html", targetPlayerName, DateTime.Now);

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
					sb.AppendLine("<title>Судебная экспертиза истории команд Minecraft — " + targetPlayerName + "</title>");
					sb.AppendLine("<style>");
					sb.AppendLine("body { background:#0F1115; color:#ECF0F5; font-family:'Segoe UI',Arial,sans-serif; margin:20px; }");
					sb.AppendLine("h1 { color:#2EE6D6; font-size:20px; border-bottom:1px solid #262B33; padding-bottom:10px; }");
					sb.AppendLine(".kpi-container { display:flex; gap:12px; margin:15px 0; }");
					sb.AppendLine(".kpi-card { background:#171A1F; border:1px solid #262B33; padding:10px 14px; border-radius:6px; min-width:140px; }");
					sb.AppendLine(".kpi-title { font-size:11px; color:#8C96A3; }");
					sb.AppendLine(".kpi-val { font-size:18px; font-weight:bold; margin-top:4px; }");
					sb.AppendLine("table { width:100%; border-collapse:collapse; margin-top:15px; font-size:12px; }");
					sb.AppendLine("th, td { border:1px solid #262B33; padding:8px 10px; text-align:left; }");
					sb.AppendLine("th { background:#171A1F; color:#4FC3F7; }");
					sb.AppendLine("tr:nth-child(even) { background:#121418; }");
					sb.AppendLine(".crit { color:#EF5350; font-weight:bold; }");
					sb.AppendLine(".warn { color:#FFA726; font-weight:bold; }");
					sb.AppendLine(".ok { color:#66BB6A; }");
					sb.AppendLine("</style></head><body>");

					sb.AppendLine("<h1>📜 Судебный протокол аудита истории команд Minecraft (command_history.txt)</h1>");
					sb.AppendLine(string.Format("<p>Подозреваемый игрок: <b>{0}</b> | Дата проверки: <b>{1:dd.MM.yyyy HH:mm:ss}</b></p>", targetPlayerName, DateTime.Now));

					sb.AppendLine("<div class='kpi-container'>");
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Всего команд</div><div class='kpi-val' style='color:#ECF0F5;'>{0}</div></div>", lblKpiTotal.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Нарушений (5.01/5.08)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiViolations.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Чит-команд (5.01)</div><div class='kpi-val' style='color:#EF5350;'>{0}</div></div>", lblKpiCheatCommands.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Baritone бот-команд</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiBaritone.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Очистка / Сокрытие (5.08)</div><div class='kpi-val' style='color:#FFA726;'>{0}</div></div>", lblKpiTampering.Text));
					sb.AppendLine(string.Format("<div class='kpi-card'><div class='kpi-title'>Обычных команд</div><div class='kpi-val' style='color:#66BB6A;'>{0}</div></div>", lblKpiStandard.Text));
					sb.AppendLine("</div>");

					sb.AppendLine("<table>");
					sb.AppendLine("<tr><th>№</th><th>Категория</th><th>Команда</th><th>Паттерн</th><th>Файл</th><th>Регламент</th><th>Вердикт</th><th>Время изменения</th></tr>");

					foreach (CommandForensicItem item in allItems)
					{
						string cssClass = item.IsViolation ? "crit" : "";
						sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td class='{8}'>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td class='{8}'>{6}</td><td>{7:dd.MM.yyyy HH:mm}</td></tr>",
							item.Index, item.CategoryName, item.DisplayCommand, item.DetectedPattern,
							string.Format("{0}:{1}", item.SourceFileName, item.LineNumber),
							item.TargetRule, item.Verdict, item.FileLastModified, cssClass));
					}

					sb.AppendLine("</table>");
					sb.AppendLine("</body></html>");

					File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
					Process.Start(sfd.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при сохранении HTML отчета: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
	}
}
