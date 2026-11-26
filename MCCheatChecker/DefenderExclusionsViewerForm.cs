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
	public class DefenderExclusionsViewerForm : Form
	{
		#region Models

		public enum DefenderFilterMode
		{
			All,
			ThreatsAndTampering,
			PathsOnly,
			ProcessesOnly,
			PoliciesOnly
		}

		public class DefenderExclusionItem
		{
			public string ExclusionType; // "Исключение папки (Paths)", "Исключение процесса", "Политика отключения Defender"
			public string TargetValue;   // Путь, имя процесса или параметр политики
			public string DiskStatus;    // "Присутствует на диске", "🚨 ПАПКА УДАЛЕНА С ДИСКА (5.08)", "Параметр реестра"
			public string Category;      // "Исключение папки чита", "Отключение защиты", "Штатное исключение"
			public string Status;        // Вердикт
			public bool IsThreat;
			public string RuleViolation; // "5.08", "5.01", "-"
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private DefenderFilterMode currentFilter = DefenderFilterMode.All;
		private List<DefenderExclusionItem> allEntries = new List<DefenderExclusionItem>();
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
		private DarkButton btnFilterPaths;
		private DarkButton btnFilterProcesses;
		private DarkButton btnFilterPolicies;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyPath;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatKeywordsRegex = new Regex(
			@"(?i)(vape|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|minced|wexside|cheat|injector|\.minecraft\\mods|temp|appdata\\roaming\\\.minecraft)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public DefenderExclusionsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит исключений и политик безопасности Windows Defender — MCCheatChecker";
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
			lblHeaderTitle.Text = "🛡️ Аудит исключений, политик и темперинга Windows Defender (Защитника)";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Выявление папок читов, добавленных в исключения антивируса (Exclusions), отключения Real-Time Monitoring и удаления исключенных каталогов.";
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
			lblStatusPill.Size = new Size(270, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 290, 22);
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
			btnFilterAll = CreateFilterButton("Все записи", DefenderFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Сокрытие читов (5.08)", DefenderFilterMode.ThreatsAndTampering, ref bx, filterBar);
			btnFilterPaths = CreateFilterButton("📂 Исключения путей (Paths)", DefenderFilterMode.PathsOnly, ref bx, filterBar);
			btnFilterProcesses = CreateFilterButton("⚙️ Исключения процессов", DefenderFilterMode.ProcessesOnly, ref bx, filterBar);
			btnFilterPolicies = CreateFilterButton("🛡️ Политики отключения", DefenderFilterMode.PoliciesOnly, ref bx, filterBar);

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

			btnCopyPath = new DarkButton();
			btnCopyPath.Text = "📄 Скопировать исключенный путь";
			btnCopyPath.Font = Ui.FontBody(9f, false);
			btnCopyPath.Location = new Point(252, 10);
			btnCopyPath.Size = new Size(260, 30);
			btnCopyPath.Click += BtnCopyPath_Click;
			bottomBar.Controls.Add(btnCopyPath);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(520, 10);
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
			btnExportReport.Location = new Point(713, 10);
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

			listView.Columns.Add("Тип исключения / Источник", 230);
			listView.Columns.Add("Исключенный путь / Параметр", 420);
			listView.Columns.Add("Статус целевого пути на диске", 200);
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
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ ДАННЫЕ ОБ ИСКЛЮЧЕНИИ ИЛИ ПОЛИТИКЕ БЕЗОПАСНОСТИ:";
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

		private DarkButton CreateFilterButton(string text, DefenderFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == DefenderFilterMode.All);
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
			btnFilterAll.AccentStyle = (currentFilter == DefenderFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == DefenderFilterMode.ThreatsAndTampering);
			btnFilterPaths.AccentStyle = (currentFilter == DefenderFilterMode.PathsOnly);
			btnFilterProcesses.AccentStyle = (currentFilter == DefenderFilterMode.ProcessesOnly);
			btnFilterPolicies.AccentStyle = (currentFilter == DefenderFilterMode.PoliciesOnly);
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
				List<DefenderExclusionItem> results = new List<DefenderExclusionItem>();

				// 1. Исключения путей (Paths)
				ScanRegistryExclusions(results, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths", "Исключение папки (Paths)");
				ScanRegistryExclusions(results, @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths", "Политика исключения (Policy Paths)");

				// 2. Исключения процессов (Processes)
				ScanRegistryExclusions(results, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes", "Исключение процесса (Processes)");
				ScanRegistryExclusions(results, @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Processes", "Политика процесса (Policy Processes)");

				// 3. Исключения расширений (Extensions)
				ScanRegistryExclusions(results, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Extensions", "Исключение расширения (Extensions)");

				// 4. Политики отключения Защитника (Tampering)
				ScanDefenderPolicies(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(DefenderExclusionItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 СОКРЫТИЕ В DEFENDER (5.08): {0}", threatCount);
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

		private void ScanRegistryExclusions(List<DefenderExclusionItem> results, string subKeyPath, string typeName)
		{
			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] valNames = key.GetValueNames();

					foreach (string valName in valNames)
					{
						string path = valName.Trim();
						if (string.IsNullOrEmpty(path)) continue;

						bool isCheat = CheatKeywordsRegex.IsMatch(path);
						bool exists = false;
						bool isDeleted = false;

						try
						{
							if (Directory.Exists(path) || File.Exists(path))
							{
								exists = true;
							}
							else if (path.Contains(":") || path.StartsWith("\\\\"))
							{
								isDeleted = true;
							}
						}
						catch { }

						string diskStatus = exists ? "Присутствует на диске" : (isDeleted ? "🚨 ПАПКА УДАЛЕНА С ДИСКА (5.08)" : "Параметр реестра");

						DefenderExclusionItem item = new DefenderExclusionItem();
						item.ExclusionType = typeName;
						item.TargetValue = path;
						item.DiskStatus = diskStatus;

						if (isCheat)
						{
							item.Category = "Исключение папки чит-клиента в Defender";
							item.Status = "🚨 ИСКЛЮЧЕНИЕ ЧИТА В DEFENDER (5.08/5.01)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = string.Format("Добавление папки чита в исключения Windows Defender: '{0}'", path);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("ПАПКА ЧИТА ИЛИ МОДОВ ДОБАВЛЕНА В ИСКЛЮЧЕНИЯ ЗАЩИТНИКА!\nПуть: {0}\nСтатус диска: {1}\nРеестр: HKLM\\{2}\\{3}\nНарушение: Пункт регламента 5.08 (Сокрытие следов стороннего софта от антивируса) / 5.01.", path, diskStatus, subKeyPath, valName);
							results.Add(item);
						}
						else if (isDeleted && (path.IndexOf("AppData", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("Users", StringComparison.OrdinalIgnoreCase) >= 0))
						{
							item.Category = "Удаленная исключенная папка";
							item.Status = "🚨 ИСКЛЮЧЕННАЯ ПАПКА УДАЛЕНА (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = string.Format("Удаление исключенной папки перед проверкой: '{0}'", path);
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("Папка из белого списка Defender была удалена перед вызовом на проверку!\nПуть: {0}\nРеестр: HKLM\\{1}\nНарушение: Пункт регламента 5.08.", path, subKeyPath);
							results.Add(item);
						}
						else
						{
							item.Category = "Исключение антивируса";
							item.Status = "Штатное исключение";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Путь: {0}\nСтатус: {1}\nВетка реестра: HKLM\\{2}", path, diskStatus, subKeyPath);
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanDefenderPolicies(List<DefenderExclusionItem> results)
		{
			try
			{
				// 1. DisableAntiSpyware
				using (RegistryKey defPolicy = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
				{
					if (defPolicy != null)
					{
						object val = defPolicy.GetValue("DisableAntiSpyware");
						if (val != null && Convert.ToInt32(val) == 1)
						{
							DefenderExclusionItem item = new DefenderExclusionItem();
							item.ExclusionType = "Групповая политика (Policy)";
							item.TargetValue = "DisableAntiSpyware = 1";
							item.DiskStatus = "Параметр реестра";
							item.Category = "Полное отключение Windows Defender";
							item.Status = "🚨 ЗАЩИТНИК ОТКЛЮЧЕН В ПОЛИТИКАХ (5.08)";
							item.IsThreat = true;
							item.RuleViolation = "5.08";
							item.BanReason = "Отключение Windows Defender через групповые политики (DisableAntiSpyware = 1)";
							item.ForeColor = Ui.AccentRed;
							item.Details = "Антивирус Windows Defender был принудительно отключен через политики реестра!\nКлюч: HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\DisableAntiSpyware = 1\nНарушение: Пункт регламента 5.08.";
							results.Add(item);
						}
					}
				}

				// 2. Real-Time Protection policies
				using (RegistryKey rtPolicy = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"))
				{
					if (rtPolicy != null)
					{
						string[] polNames = new string[]
						{
							"DisableRealtimeMonitoring",
							"DisableBehaviorMonitoring",
							"DisableOnAccessProtection",
							"DisableIOAVProtection"
						};

						foreach (string pn in polNames)
						{
							object val = rtPolicy.GetValue(pn);
							if (val != null && Convert.ToInt32(val) == 1)
							{
								DefenderExclusionItem item = new DefenderExclusionItem();
								item.ExclusionType = "Политика Real-Time Protection";
								item.TargetValue = string.Format("{0} = 1", pn);
								item.DiskStatus = "Параметр реестра";
								item.Category = "Отключение мониторинга реального времени";
								item.Status = "🚨 РЕАЛТАЙМ-ЗАЩИТА ОТКЛЮЧЕНА (5.08)";
								item.IsThreat = true;
								item.RuleViolation = "5.08";
								item.BanReason = string.Format("Принудительное отключение {0} в реестре", pn);
								item.ForeColor = Ui.AccentRed;
								item.Details = string.Format("Мониторинг угроз в реальном времени отключен через политики!\nПараметр: {0} = 1\nВетка: HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\nНарушение: Пункт регламента 5.08.", pn);
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

			foreach (DefenderExclusionItem item in allEntries)
			{
				if (currentFilter == DefenderFilterMode.ThreatsAndTampering && !item.IsThreat) continue;
				if (currentFilter == DefenderFilterMode.PathsOnly && !item.ExclusionType.Contains("Paths")) continue;
				if (currentFilter == DefenderFilterMode.ProcessesOnly && !item.ExclusionType.Contains("Processes")) continue;
				if (currentFilter == DefenderFilterMode.PoliciesOnly && !item.ExclusionType.Contains("Policy") && !item.ExclusionType.Contains("Политика")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.ExclusionType != null && item.ExclusionType.ToLowerInvariant().Contains(search)) ||
								   (item.TargetValue != null && item.TargetValue.ToLowerInvariant().Contains(search)) ||
								   (item.DiskStatus != null && item.DiskStatus.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.ExclusionType);
				lvi.SubItems.Add(item.TargetValue);
				lvi.SubItems.Add(item.DiskStatus);
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

			DefenderExclusionItem item = listView.SelectedItems[0].Tag as DefenderExclusionItem;
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
				MessageBox.Show("Выберите исключение с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			DefenderExclusionItem item = listView.SelectedItems[0].Tag as DefenderExclusionItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnCopyPath_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			DefenderExclusionItem item = listView.SelectedItems[0].Tag as DefenderExclusionItem;
			if (item == null || string.IsNullOrEmpty(item.TargetValue)) return;

			Clipboard.SetText(item.TargetValue);
			MessageBox.Show("Исключенный путь скопирован в буфер обмена:\n\n" + item.TargetValue, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Исключения_Defender_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА ИСКЛЮЧЕНИЙ И ПОЛИТИК DEFENDER");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (DefenderExclusionItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.ExclusionType, item.Category));
						sb.AppendLine(string.Format("Параметр/Путь: {0}", item.TargetValue));
						sb.AppendLine(string.Format("Статус диска:  {0}", item.DiskStatus));
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
