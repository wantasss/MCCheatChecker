using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class ExplorerSearchesTypedPathsViewerForm : Form
	{
		#region Models

		public enum ExplorerFilterMode
		{
			All,
			ThreatsAndCleaners,
			WordWheelQueryOnly,
			TypedPathsOnly,
			RecentAppsOnly
		}

		public class ExplorerHistoryItem
		{
			public string Source;        // "Поиски WordWheelQuery", "Адресная строка TypedPaths", "Запуски RecentApps", "Диалог OpenSavePidlMRU"
			public string QueryOrPath;   // Текст поискового запроса или путь
			public string TargetStatus;  // "Присутствует на диске", "🚨 УДАЛЕН ПЕРЕД ПРОВЕРКОЙ (5.08)", "Поисковый запрос"
			public string Category;      // "Поиск чит-клиента", "Поиск скрипта очистки", "Переход по стороннему пути"
			public string Status;        // Вердикт
			public DateTime Timestamp;   // Время (если доступно в RecentApps)
			public bool IsThreat;
			public string RuleViolation; // "5.01", "5.02", "5.08", "-"
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ExplorerFilterMode currentFilter = ExplorerFilterMode.All;
		private List<ExplorerHistoryItem> allEntries = new List<ExplorerHistoryItem>();
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
		private DarkButton btnFilterSearches;
		private DarkButton btnFilterPaths;
		private DarkButton btnFilterRecentApps;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyText;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatKeywordsRegex = new Regex(
			@"(?i)(vape|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|minced|wexside|flint|killaura|freecam|xray|x-ray|reach|hitbox|aimbot|speedhack|bhop|fly|spider|nofall|autototem|autoclicker|stealer|injector|cheat|hvh)",
			RegexOptions.Compiled);

		private static readonly Regex CleanerKeywordsRegex = new Regex(
			@"(?i)(clean|cleaner|wevtutil|fsutil|usn|journal|vanish|wipe|selfdelete|regedit|batch|\.bat|delete|tracks)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ExplorerSearchesTypedPathsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Инспектор поисков Проводника WordWheelQuery, адресной строки TypedPaths и RecentApps — MCCheatChecker";
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 580);
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
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔍 Инспектор поисков Проводника (WordWheelQuery), адресной строки (TypedPaths) и RecentApps";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Поиск запросов читов и клинеров в поиске Windows, введенных в адресную строку путей к удаленным папкам и истории RecentApps.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 42);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(260, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 280, 22);
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
			btnFilterAll = CreateFilterButton("Все записи", ExplorerFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Запросы читов / клинеров", ExplorerFilterMode.ThreatsAndCleaners, ref bx, filterBar);
			btnFilterSearches = CreateFilterButton("🔍 Поиски WordWheelQuery", ExplorerFilterMode.WordWheelQueryOnly, ref bx, filterBar);
			btnFilterPaths = CreateFilterButton("📂 Адресная строка (TypedPaths)", ExplorerFilterMode.TypedPathsOnly, ref bx, filterBar);
			btnFilterRecentApps = CreateFilterButton("⚡ Приложения (RecentApps)", ExplorerFilterMode.RecentAppsOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 250, 8);
			searchBox.Size = new Size(235, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 305, 12);
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
			btnCopyBan.Size = new Size(230, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnCopyText = new DarkButton();
			btnCopyText.Text = "📄 Скопировать поисковый запрос / путь";
			btnCopyText.Font = Ui.FontBody(9f, false);
			btnCopyText.Location = new Point(252, 10);
			btnCopyText.Size = new Size(280, 30);
			btnCopyText.Click += BtnCopyText_Click;
			bottomBar.Controls.Add(btnCopyText);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(540, 10);
			btnEvidenceCapture.Size = new Size(185, 30);
			btnEvidenceCapture.Click += delegate
			{
				try
				{
					EvidenceCaptureForm ev = new EvidenceCaptureForm(targetPlayerName, Environment.UserName);
					ev.Show(this);
				}
				catch { }
			};
			bottomBar.Controls.Add(btnEvidenceCapture);

			btnExportReport = new DarkButton();
			btnExportReport.Text = "📄 Экспорт отчета";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(733, 10);
			btnExportReport.Size = new Size(150, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 F5";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 90, 10);
			btnRefreshNow.Size = new Size(75, 30);
			btnRefreshNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefreshNow.Click += delegate { StartScanAsync(); };
			bottomBar.Controls.Add(btnRefreshNow);

			// SplitContainer
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 380;
			split.SplitterWidth = 6;
			this.Controls.Add(split);

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Font = Ui.FontBody(9.5f, false);

			listView.Columns.Add("Источник артефакта", 220);
			listView.Columns.Add("Поисковый запрос / Введенный путь / Программа", 430);
			listView.Columns.Add("Статус целевого пути на диске", 200);
			listView.Columns.Add("Время (RecentApps)", 160);
			listView.Columns.Add("Вердикт / Регламент", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ О ПОИСКЕ ИЛИ АДРЕСНОЙ СТРОКЕ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из таблицы.";
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

		private DarkButton CreateFilterButton(string text, ExplorerFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == ExplorerFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(160, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 166;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == ExplorerFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == ExplorerFilterMode.ThreatsAndCleaners);
			btnFilterSearches.AccentStyle = (currentFilter == ExplorerFilterMode.WordWheelQueryOnly);
			btnFilterPaths.AccentStyle = (currentFilter == ExplorerFilterMode.TypedPathsOnly);
			btnFilterRecentApps.AccentStyle = (currentFilter == ExplorerFilterMode.RecentAppsOnly);
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
				List<ExplorerHistoryItem> results = new List<ExplorerHistoryItem>();

				// 1. Поисковые запросы Проводника WordWheelQuery
				ScanWordWheelQuery(results);

				// 2. Введенные пути в адресной строке TypedPaths
				ScanTypedPaths(results);

				// 3. Запуски приложений через поиск Windows RecentApps
				ScanRecentApps(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(ExplorerHistoryItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УГРОЗ В ИСТОРИИ ПРОИЗВОДНЫХ (5.08/5.01): {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Записей проверено: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanWordWheelQuery(List<ExplorerHistoryItem> results)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery"))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string name in valNames)
					{
						if (name.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;

						byte[] bytes = key.GetValue(name) as byte[];
						if (bytes == null || bytes.Length < 2) continue;

						string query = Encoding.Unicode.GetString(bytes).TrimEnd('\0').Trim();
						if (string.IsNullOrEmpty(query)) continue;

						bool isCheat = CheatKeywordsRegex.IsMatch(query);
						bool isCleaner = CleanerKeywordsRegex.IsMatch(query);

						ExplorerHistoryItem item = new ExplorerHistoryItem();
						item.Source = "Поиски WordWheelQuery";
						item.QueryOrPath = query;
						item.TargetStatus = "Поисковый запрос Проводника";
						item.Timestamp = DateTime.MinValue;

						if (isCheat)
						{
							item.Category = "Поиск чит-клиента в Проводнике";
							item.Status = "🚨 ПОИСК ЧИТА В ПРОВОДНИКЕ (5.01/5.02)";
							item.IsThreat = true;
							item.RuleViolation = "5.02";
							item.BanReason = string.Format("Поиск чит-клиента в Проводнике Windows: '{0}'", query);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("ИГРОК ИСКАЛ ЧИТ-КЛИЕНТ В ПРОВОДНИКЕ WINDOWS!\nЗапрос: \"{0}\"\nКлюч реестра: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\WordWheelQuery\\{1}\nНарушение: Пункт 5.02 (Хранение читов) / 5.01 (Чит-клиенты).", query, name);
							results.Add(item);
						}
						else if (isCleaner)
						{
							item.Category = "Поиск скрипта очистки следов";
							item.Status = "🚨 ПОИСК СКРИПТА ОЧИСТКИ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = string.Format("Поиск клинера / скрипта затирки следов: '{0}'", query);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("ИГРОК ИСКАЛ КЛИНЕР ИЛИ СКРИПТ ОЧИСТКИ В ПРОВОДНИКЕ!\nЗапрос: \"{0}\"\nКлюч реестра: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\WordWheelQuery\\{1}\nНарушение: Пункт регламента 5.08.", query, name);
							results.Add(item);
						}
						else
						{
							item.Category = "Поисковый запрос пользователя";
							item.Status = "Обычный поиск";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Поисковый запрос: \"{0}\"\nКлюч: {1}", query, name);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanTypedPaths(List<ExplorerHistoryItem> results)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths"))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();
					foreach (string name in valNames)
					{
						object val = key.GetValue(name);
						if (val == null) continue;

						string pathStr = val.ToString().TrimEnd('\0', ' ');
						if (string.IsNullOrEmpty(pathStr)) continue;

						bool isCheat = CheatKeywordsRegex.IsMatch(pathStr);
						bool isCleaner = CleanerKeywordsRegex.IsMatch(pathStr);

						bool exists = false;
						bool isDeleted = false;
						bool isUsb = false;

						try
						{
							if (Directory.Exists(pathStr) || File.Exists(pathStr))
							{
								exists = true;
							}
							else if (pathStr.Contains(":") || pathStr.StartsWith("\\\\"))
							{
								isDeleted = true;
								if (pathStr.Length >= 2 && pathStr[1] == ':' && !"cC".Contains(pathStr[0].ToString()))
								{
									isUsb = true;
								}
							}
						}
						catch { }

						string targetStatus = exists ? "Присутствует на диске" : (isDeleted ? "🚨 ПАПКА УДАЛЕНА С ДИСКА (5.08)" : "—");
						if (isUsb && !exists) targetStatus = "🚨 НОСИТЕЛЬ / ФЛЕШКА ИЗВЛЕЧЕНА (5.08)";

						ExplorerHistoryItem item = new ExplorerHistoryItem();
						item.Source = "Адресная строка (TypedPaths)";
						item.QueryOrPath = pathStr;
						item.TargetStatus = targetStatus;
						item.Timestamp = DateTime.MinValue;

						if (isCheat)
						{
							item.Category = "Переход в папку чита через адресную строку";
							item.Status = "🚨 ПУТЬ К ЧИТУ В АДРЕСНОЙ СТРОКЕ (5.01/5.02)";
							item.IsThreat = true;
							item.RuleViolation = "5.02";
							item.BanReason = string.Format("Ручной переход в директорию чита: '{0}'", pathStr);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("В АДРЕСНОЙ СТРОКЕ ПРОВОДНИКА ВВЕДЕН ПУТЬ К ЧИТУ!\nПуть: {0}\nСтатус диска: {1}\nРеестр: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\TypedPaths\\{2}\nНарушение: Пункт 5.02 (Хранение читов).", pathStr, targetStatus, name);
							results.Add(item);
						}
						else if (isDeleted && isCleaner)
						{
							item.Category = "Удаленная папка скрипта очистки";
							item.Status = "🚨 ПУТЬ К УДАЛЕННОМУ КЛИНЕРУ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = string.Format("Переход к скрипту очистки, удаленному перед проверкой: '{0}'", pathStr);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Игрок переходил в папку клинера, удаленную перед проверкой!\nПуть: {0}\nРеестр: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\TypedPaths\\{1}\nНарушение: Пункт регламента 5.08.", pathStr, name);
							results.Add(item);
						}
						else if (isUsb)
						{
							item.Category = "Переход на съемный диск / флешку";
							item.Status = exists ? "Съемный носитель" : "🚨 ИЗВЛЕЧЕННАЯ ФЛЕШКА (5.08)";
							item.IsThreat = !exists;
							item.RuleViolation = !exists ? "5.08" : "-";
							item.BanReason = !exists ? "Переход на флешку, извлеченную перед проверкой" : "-";
							item.ForeColor = !exists ? Ui.AccentRed : Ui.AccentTeal;
							item.Details = string.Format("Введен переход на съемный носитель: {0}\nСтатус: {1}", pathStr, targetStatus);
							results.Add(item);
						}
						else
						{
							item.Category = "Введенный путь в Проводнике";
							item.Status = "Введенный путь";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Путь: {0}\nСтатус: {1}\nКлюч: {2}", pathStr, targetStatus, name);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanRecentApps(List<ExplorerHistoryItem> results)
		{
			try
			{
				using (RegistryKey searchKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search\RecentApps"))
				{
					if (searchKey == null) return;
					string[] subKeyNames = searchKey.GetSubKeyNames();

					foreach (string subName in subKeyNames)
					{
						using (RegistryKey appKey = searchKey.OpenSubKey(subName))
						{
							if (appKey == null) continue;

							object appPathObj = appKey.GetValue("AppPath");
							if (appPathObj == null) continue;

							string appPath = appPathObj.ToString().Trim();
							if (string.IsNullOrEmpty(appPath)) continue;

							DateTime lastAccess = DateTime.MinValue;
							object timeObj = appKey.GetValue("LastAccessedTime");
							if (timeObj is long)
							{
								try
								{
									lastAccess = DateTime.FromFileTimeUtc((long)timeObj).ToLocalTime();
								}
								catch { }
							}

							int launchCount = 0;
							object countObj = appKey.GetValue("LaunchCount");
							if (countObj is int)
							{
								launchCount = (int)countObj;
							}

							bool isCheat = CheatKeywordsRegex.IsMatch(appPath);
							bool isCleaner = CleanerKeywordsRegex.IsMatch(appPath);

							bool exists = File.Exists(appPath) || Directory.Exists(appPath);
							string targetStatus = exists ? "Присутствует на диске" : "🚨 ФАЙЛ УДАЛЕН С ДИСКА (5.08)";

							ExplorerHistoryItem item = new ExplorerHistoryItem();
							item.Source = "Запуски RecentApps";
							item.QueryOrPath = appPath;
							item.TargetStatus = targetStatus;
							item.Timestamp = lastAccess;

							if (isCheat)
							{
								item.Category = "Запуск чита через Windows Search";
								item.Status = "🚨 ЗАПУСК ЧИТА В RECENTAPPS (5.01)";
								item.IsThreat = true;
								item.RuleViolation = "5.01";
								item.BanReason = string.Format("Запуск чита, зафиксированный в RecentApps: '{0}'", Path.GetFileName(appPath));
								item.ForeColor = Ui.AccentRed;
								item.Details = string.Format("В БАЗЕ RECENTAPPS ЗАФИКСИРОВАН ЗАПУСК ЧИТА!\nПриложение: {0}\nПоследний запуск: {1:yyyy-MM-dd HH:mm:ss}\nКоличество запусков: {2}\nСтатус диска: {3}\nНарушение: Пункт 5.01 (Чит-клиенты).",
									appPath, lastAccess, launchCount, targetStatus);
								results.Add(item);
							}
							else if (isCleaner)
							{
								item.Category = "Запуск клинера через Windows Search";
								item.Status = "🚨 ЗАПУСК КЛИНЕРА В RECENTAPPS (5.08)";
								item.IsThreat = true;
								item.RuleViolation = "5.08";
								item.BanReason = string.Format("Запуск клинера улик в RecentApps: '{0}'", Path.GetFileName(appPath));
								item.ForeColor = Ui.AccentRed;
								item.Details = string.Format("В БАЗЕ RECENTAPPS ЗАФИКСИРОВАН ЗАПУСК КЛИНЕРА СЛЕДОВ!\nПриложение: {0}\nПоследний запуск: {1:yyyy-MM-dd HH:mm:ss}\nКоличество запусков: {2}\nСтатус диска: {3}\nНарушение: Пункт регламента 5.08.",
									appPath, lastAccess, launchCount, targetStatus);
								results.Add(item);
							}
							else if (!exists && (appPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || appPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || appPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)))
							{
								item.Category = "Удаленное приложение RecentApps";
								item.Status = "🚨 ПРИЛОЖЕНИЕ УДАЛЕНО С ДИСКА (5.08)";
								item.IsThreat = true;
								item.RuleViolation = "5.08";
								item.BanReason = string.Format("Удаление исполняемого файла из RecentApps: '{0}'", Path.GetFileName(appPath));
								item.ForeColor = Ui.AccentRed;
								item.Details = string.Format("Исполняемый файл из базы RecentApps был удален перед проверкой!\nПриложение: {0}\nПоследний запуск: {1:yyyy-MM-dd HH:mm:ss}\nНарушение: Пункт 5.08.", appPath, lastAccess);
								results.Add(item);
							}
							else
							{
								item.Category = "Запуск приложения RecentApps";
								item.Status = "Штатный запуск";
								item.RuleViolation = "-";
								item.BanReason = "-";
								item.ForeColor = Ui.TextSecondary;
								item.Details = string.Format("Приложение: {0}\nПоследний запуск: {1:yyyy-MM-dd HH:mm:ss}\nКоличество запусков: {2}\nСтатус: {3}", appPath, lastAccess, launchCount, targetStatus);
								results.Add(item);
							}
						}
					}
				}
			}
			catch { }
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (ExplorerHistoryItem item in allEntries)
			{
				if (currentFilter == ExplorerFilterMode.ThreatsAndCleaners && !item.IsThreat) continue;
				if (currentFilter == ExplorerFilterMode.WordWheelQueryOnly && !item.Source.Contains("WordWheelQuery")) continue;
				if (currentFilter == ExplorerFilterMode.TypedPathsOnly && !item.Source.Contains("TypedPaths")) continue;
				if (currentFilter == ExplorerFilterMode.RecentAppsOnly && !item.Source.Contains("RecentApps")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.Source != null && item.Source.ToLowerInvariant().Contains(search)) ||
								   (item.QueryOrPath != null && item.QueryOrPath.ToLowerInvariant().Contains(search)) ||
								   (item.TargetStatus != null && item.TargetStatus.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Source);
				lvi.SubItems.Add(item.QueryOrPath);
				lvi.SubItems.Add(item.TargetStatus);
				lvi.SubItems.Add(item.Timestamp == DateTime.MinValue ? "—" : item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.Status);
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

			ExplorerHistoryItem item = listView.SelectedItems[0].Tag as ExplorerHistoryItem;
			if (item == null) return;

			lblDetailVerdict.Text = item.Status;
			lblDetailVerdict.ForeColor = item.ForeColor;

			if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
			{
				lblDetailRule.Text = string.Format("⚖️ Пункт регламента: {0} ({1})", item.RuleViolation, item.BanReason);
				lblDetailBanCommand.Text = string.Format("📋 Команда: /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			}
			else
			{
				lblDetailRule.Text = "🛡️ Нарушений не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details ?? "Нет сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Запись не выбрана";
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
				MessageBox.Show("Выберите запись с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ExplorerHistoryItem item = listView.SelectedItems[0].Tag as ExplorerHistoryItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnCopyText_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ExplorerHistoryItem item = listView.SelectedItems[0].Tag as ExplorerHistoryItem;
			if (item == null || string.IsNullOrEmpty(item.QueryOrPath)) return;

			Clipboard.SetText(item.QueryOrPath);
			MessageBox.Show("Текст запроса/пути скопирован в буфер обмена:\n\n" + item.QueryOrPath, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Поиски_Проводник_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА ПОИСКОВ WORDWHEELQUERY И TYPEDPATHS");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (ExplorerHistoryItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.Source, item.Category));
						sb.AppendLine(string.Format("Запрос / Путь: {0}", item.QueryOrPath));
						sb.AppendLine(string.Format("Статус диска:  {0}", item.TargetStatus));
						sb.AppendLine(string.Format("Вердикт:       {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:     Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:       /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности:   {0}", item.Details));
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
