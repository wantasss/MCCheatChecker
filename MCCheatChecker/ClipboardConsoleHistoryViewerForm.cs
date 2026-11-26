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
	public class ClipboardConsoleHistoryViewerForm : Form
	{
		#region Models

		public enum ClipFilterMode
		{
			All,
			CleanersAndThreats,
			PowerShellOnly,
			ClipboardOnly,
			RunMruOnly
		}

		public class ClipboardConsoleItem
		{
			public string Source; // "Буфер обмена (Active)", "PowerShell (PSReadLine)", "Диалог Выполнить (RunMRU)"
			public string Content;
			public string Category;
			public string Status;
			public DateTime Timestamp;
			public bool IsThreat;
			public bool IsSuspicious;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ClipFilterMode currentFilter = ClipFilterMode.All;
		private List<ClipboardConsoleItem> allEntries = new List<ClipboardConsoleItem>();
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
		private DarkButton btnFilterPowerShell;
		private DarkButton btnFilterClipboard;
		private DarkButton btnFilterRunMru;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyContent;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CleanerCmdRegex = new Regex(
			@"(?i)(wevtutil\s+cl|fsutil\s+usn\s+deletejournal|Set-MpPreference\s+-DisableRealtime|remove-item\s+-recurse|rmdir\s+/s\s+/q|del\s+/f\s+/q|del\s+/s|clear-history|clean-traces|vanish|selfdelete|wipe_logs|vape|celestial|expensive|nursultan|wild|deadcode|akrien|karlat)",
			RegexOptions.Compiled);

		private static readonly Regex CheatCommandRegex = new Regex(
			@"(?i)(^\.(?:bind|vape|config|load|nursultan|celestial|rich|catlavan|friend|save|say)|#goto|#mine|#baritone|cdn\.discordapp\.com|vape\.gg|celestial\.lol|nursultan\.fun|expensive\.fun)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ClipboardConsoleHistoryViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит буфера обмена, истории консоли PowerShell и RunMRU — MCCheatChecker";
			this.Size = new Size(1180, 730);
			this.MinimumSize = new Size(960, 580);
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
			lblHeaderTitle.Text = "📋 Аудит буфера обмена, консоли PowerShell и диалога 'Выполнить' (RunMRU)";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Выявление команд очистки логов (wevtutil cl), удаления USN журнала (fsutil), команд читов (.bind/.vape) и скриптов в PSReadLine.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование...";
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
			btnFilterAll = CreateFilterButton("Все записи", ClipFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Скрипты очистки (5.08)", ClipFilterMode.CleanersAndThreats, ref bx, filterBar);
			btnFilterPowerShell = CreateFilterButton("⚡ PowerShell история", ClipFilterMode.PowerShellOnly, ref bx, filterBar);
			btnFilterClipboard = CreateFilterButton("📋 Буфер обмена", ClipFilterMode.ClipboardOnly, ref bx, filterBar);
			btnFilterRunMru = CreateFilterButton("⌨️ RunMRU (Win+R)", ClipFilterMode.RunMruOnly, ref bx, filterBar);

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

			btnCopyContent = new DarkButton();
			btnCopyContent.Text = "📄 Скопировать текст команды";
			btnCopyContent.Font = Ui.FontBody(9f, false);
			btnCopyContent.Location = new Point(242, 10);
			btnCopyContent.Size = new Size(220, 30);
			btnCopyContent.Click += BtnCopyContent_Click;
			bottomBar.Controls.Add(btnCopyContent);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(470, 10);
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
			btnExportReport.Location = new Point(663, 10);
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
			split.SplitterDistance = 370;
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

			listView.Columns.Add("Источник", 160);
			listView.Columns.Add("Категория / Тип", 160);
			listView.Columns.Add("Текст команды / Содержимое", 420);
			listView.Columns.Add("Время фиксации", 150);
			listView.Columns.Add("Вердикт / Регламент", 250);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ О КОМАНДЕ ИЛИ БУФЕРЕ ОБМЕНА:";
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

		private DarkButton CreateFilterButton(string text, ClipFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == ClipFilterMode.All);
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
			btnFilterAll.AccentStyle = (currentFilter == ClipFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == ClipFilterMode.CleanersAndThreats);
			btnFilterPowerShell.AccentStyle = (currentFilter == ClipFilterMode.PowerShellOnly);
			btnFilterClipboard.AccentStyle = (currentFilter == ClipFilterMode.ClipboardOnly);
			btnFilterRunMru.AccentStyle = (currentFilter == ClipFilterMode.RunMruOnly);
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

			// Проверка активного буфера обмена в UI потоке
			string activeClipText = string.Empty;
			try
			{
				if (Clipboard.ContainsText())
				{
					activeClipText = Clipboard.GetText();
				}
			}
			catch { }

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<ClipboardConsoleItem> results = new List<ClipboardConsoleItem>();

				// 1. Анализ активного буфера обмена
				if (!string.IsNullOrEmpty(activeClipText))
				{
					AnalyzeClipboardContent(activeClipText, results);
				}

				// 2. Аудит истории PowerShell PSReadLine
				ScanPowerShellHistory(results);

				// 3. Аудит RunMRU (Win+R)
				ScanRunMru(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(ClipboardConsoleItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 СКРИПТОВ ОЧИСТКИ (5.08): {0}", threatCount);
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

		private void AnalyzeClipboardContent(string text, List<ClipboardConsoleItem> results)
		{
			string trim = text.Trim();
			bool isCleaner = CleanerCmdRegex.IsMatch(trim);
			bool isCheatCmd = CheatCommandRegex.IsMatch(trim);

			ClipboardConsoleItem item = new ClipboardConsoleItem();
			item.Source = "Буфер обмена (Active)";
			item.Content = trim;
			item.Timestamp = DateTime.Now;

			if (isCleaner)
			{
				item.Category = "Скрипт очистки / сокрытия";
				item.Status = "🚨 СКРИПТ ОЧИСТКИ В БУФЕРЕ (5.08)";
				item.IsThreat = true;
				item.RuleViolation = "5.08";
				item.BanReason = "Команда очистки следов в буфере обмена";
				item.ForeColor = Ui.AccentRed;
				item.Details = string.Format("В БУФЕРЕ ОБМЕНА ОБНАРУЖЕНА КОМАНДА ЗАТИРАНИЯ УЛИК!\nТекст:\n{0}\nНарушение: Пункт 5.08 (Использование скриптов/команд для очистки логов, корзины или USN).", trim);
			}
			else if (isCheatCmd)
			{
				item.Category = "Команда / ссылка чита";
				item.Status = "🚨 КОМАНДА ЧИТ-КЛИЕНТА В БУФЕРЕ (5.01)";
				item.IsThreat = true;
				item.RuleViolation = "5.01";
				item.BanReason = "Использование команд чит-клиента (.bind / чит-ссылка)";
				item.ForeColor = Ui.AccentRed;
				item.Details = string.Format("ОБНАРУЖЕНА КОМАНДА ИЛИ ССЫЛКА ЧИТА В БУФЕРЕ!\nТекст:\n{0}\nНарушение: Пункт 5.01 (Использование читов / запрещенных команд).", trim);
			}
			else
			{
				item.Category = "Обычный текст";
				item.Status = "Текст в буфере обмена (Чист)";
				item.RuleViolation = "-";
				item.BanReason = "-";
				item.ForeColor = Ui.TextSecondary;
				item.Details = string.Format("Текст в буфере обмена:\n{0}", trim);
			}

			results.Add(item);
		}

		private void ScanPowerShellHistory(List<ClipboardConsoleItem> results)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string psHistoryPath = Path.Combine(appData, @"Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");

			if (!File.Exists(psHistoryPath)) return;

			try
			{
				DateTime fTime = File.GetLastWriteTime(psHistoryPath);
				string[] lines = File.ReadAllLines(psHistoryPath);

				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i].Trim();
					if (string.IsNullOrEmpty(line)) continue;

					bool isCleaner = CleanerCmdRegex.IsMatch(line);

					ClipboardConsoleItem item = new ClipboardConsoleItem();
					item.Source = "PowerShell (PSReadLine)";
					item.Content = line;
					item.Timestamp = fTime;

					if (isCleaner)
					{
						item.Category = "Команда очистки улик";
						item.Status = "🚨 ОЧИСТКА В POWERSHELL (5.08)";
						item.IsThreat = true;
						item.RuleViolation = "5.08";
						item.BanReason = string.Format("Выполнение команды очистки в PowerShell: {0}", line.Length > 50 ? line.Substring(0, 47) + "..." : line);
						item.ForeColor = Ui.AccentRed;
						item.Details = string.Format("В ИСТОРИИ POWERSHELL ЗАФИКСИРОВАНА КОМАНДА ОЧИСТКИ СЛЕДОВ!\nСтрока #{0}: {1}\nФайл истории: {2}\nДата последнего изменения файла: {3:yyyy-MM-dd HH:mm:ss}\nНарушение: Пункт 5.08 (Удаление логов через wevtutil, fsutil или скрипты).",
							i + 1, line, psHistoryPath, fTime);
						results.Add(item);
					}
					else if (line.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("curl", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("iwr", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						item.Category = "Системный вызов PowerShell";
						item.Status = "Вызов PowerShell";
						item.RuleViolation = "-";
						item.BanReason = "-";
						item.ForeColor = Ui.AccentOrange;
						item.Details = string.Format("Строка #{0}: {1}", i + 1, line);
						results.Add(item);
					}
				}
			}
			catch { }
		}

		private void ScanRunMru(List<ClipboardConsoleItem> results)
		{
			try
			{
				using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"))
				{
					if (runKey == null) return;

					string[] valNames = runKey.GetValueNames();
					foreach (string vName in valNames)
					{
						if (vName.Equals("MRUList", StringComparison.OrdinalIgnoreCase)) continue;

						object val = runKey.GetValue(vName);
						if (val == null) continue;

						string cmd = val.ToString().TrimEnd('\\', '1');
						bool isCleaner = CleanerCmdRegex.IsMatch(cmd);

						ClipboardConsoleItem item = new ClipboardConsoleItem();
						item.Source = "Диалог Выполнить (RunMRU)";
						item.Content = cmd;
						item.Timestamp = DateTime.MinValue;

						if (isCleaner)
						{
							item.Category = "Команда очистки в RunMRU";
							item.Status = "🚨 КОМАНДА ОЧИСТКИ В WIN+R (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = string.Format("Запуск команды очистки через Win+R: {0}", cmd);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Игрок вводил команду сокрытия улик через диалог Выполнить (Win+R)!\nКоманда: {0}\nРеестр: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU\\{1}\nНарушение: Пункт регламента 5.08.", cmd, vName);
						}
						else
						{
							item.Category = "История Win+R";
							item.Status = "Штатная команда Win+R";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Команда: {0}\nКлюч RunMRU: {1}", cmd, vName);
						}

						results.Add(item);
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

			foreach (ClipboardConsoleItem item in allEntries)
			{
				if (currentFilter == ClipFilterMode.CleanersAndThreats && !item.IsThreat) continue;
				if (currentFilter == ClipFilterMode.PowerShellOnly && !item.Source.Contains("PowerShell")) continue;
				if (currentFilter == ClipFilterMode.ClipboardOnly && !item.Source.Contains("Буфер")) continue;
				if (currentFilter == ClipFilterMode.RunMruOnly && !item.Source.Contains("RunMRU")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.Source != null && item.Source.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Content != null && item.Content.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Source);
				lvi.SubItems.Add(item.Category);
				lvi.SubItems.Add(item.Content);
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

			ClipboardConsoleItem item = listView.SelectedItems[0].Tag as ClipboardConsoleItem;
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
				MessageBox.Show("Выберите команду с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ClipboardConsoleItem item = listView.SelectedItems[0].Tag as ClipboardConsoleItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnCopyContent_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ClipboardConsoleItem item = listView.SelectedItems[0].Tag as ClipboardConsoleItem;
			if (item == null || string.IsNullOrEmpty(item.Content)) return;

			Clipboard.SetText(item.Content);
			MessageBox.Show("Текст команды скопирован в буфер обмена:\n\n" + item.Content, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Консоль_Буфер_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА БУФЕРА ОБМЕНА, POWERSHELL И RUNMRU");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (ClipboardConsoleItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.Source, item.Category));
						sb.AppendLine(string.Format("Содержимое:  {0}", item.Content));
						sb.AppendLine(string.Format("Вердикт:     {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", item.Details));
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
