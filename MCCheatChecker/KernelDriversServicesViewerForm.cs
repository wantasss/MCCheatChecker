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
	public class KernelDriversServicesViewerForm : Form
	{
		#region Models

		public enum DriverFilterMode
		{
			All,
			CheatDriversOnly,
			DisabledForensicsOnly,
			ThirdPartyDrivers,
			CleanServices
		}

		public class DriverServiceItem
		{
			public string ServiceName;
			public string DisplayName;
			public string ImagePath;
			public int ServiceType; // 1 = Kernel, 2 = FileSystem, 16 = Win32Own, 32 = Win32Share
			public int StartType;   // 0 = Boot, 1 = System, 2 = Auto, 3 = Demand, 4 = Disabled
			public string Status;
			public bool ExistsOnDisk;
			public bool IsCheatDriver;
			public bool IsForensicService;
			public bool IsTampered;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private DriverFilterMode currentFilter = DriverFilterMode.All;
		private List<DriverServiceItem> allEntries = new List<DriverServiceItem>();
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
		private DarkButton btnFilterCheatDrivers;
		private DarkButton btnFilterDisabledForensics;
		private DarkButton btnFilterThirdParty;
		private DarkButton btnFilterClean;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenFolder;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatDriverRegex = new Regex(
			@"(?i)(dbk64|dbk32|dbk|kprocesshacker|kmod|systeminformer|xenos|xenos64|capcom|gdrv|gigabyte|mhyprot|winring0|ols|speedhack|timerdriver|rtcore64|blackbone|cpuz14|cpuz15)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public KernelDriversServicesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Инспектор ядерных драйверов и системных служб (Services / Drivers) — MCCheatChecker";
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
			lblHeaderTitle.Text = "⚙️ Аудит драйверов ядра и служб Windows (Services / Drivers Forensic)";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Обнаружение ядерных инжекторов (Cheat Engine, Process Hacker, Xenos), уязвимых DSE-драйверов и отключения служб DPS/SysMain/EventLog (5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование служб и драйверов...";
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
			btnFilterAll = CreateFilterButton("Все службы и драйверы", DriverFilterMode.All, ref bx, filterBar);
			btnFilterCheatDrivers = CreateFilterButton("🚨 Чит-драйверы (5.01/5.08)", DriverFilterMode.CheatDriversOnly, ref bx, filterBar);
			btnFilterDisabledForensics = CreateFilterButton("🛡️ Отключение служб (5.08)", DriverFilterMode.DisabledForensicsOnly, ref bx, filterBar);
			btnFilterThirdParty = CreateFilterButton("Сторонние драйверы", DriverFilterMode.ThirdPartyDrivers, ref bx, filterBar);
			btnFilterClean = CreateFilterButton("Легитимные службы", DriverFilterMode.CleanServices, ref bx, filterBar);

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

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Открыть папку с драйвером";
			btnOpenFolder.Font = Ui.FontBody(9f, false);
			btnOpenFolder.Location = new Point(242, 10);
			btnOpenFolder.Size = new Size(220, 30);
			btnOpenFolder.Click += BtnOpenFolder_Click;
			bottomBar.Controls.Add(btnOpenFolder);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику (SHA-256)";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(470, 10);
			btnEvidenceCapture.Size = new Size(230, 30);
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
			btnExportReport.Text = "📄 Экспорт отчета в TXT";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(708, 10);
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

			listView.Columns.Add("Имя службы / Драйвера", 220);
			listView.Columns.Add("Тип запуска", 140);
			listView.Columns.Add("Файл на диске", 140);
			listView.Columns.Add("Путь к образу (ImagePath)", 300);
			listView.Columns.Add("Вердикт / Статус", 280);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЙ АНАЛИЗ ДРАЙВЕРА ИЛИ СЛУЖБЫ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите службу или драйвер из таблицы.";
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

		private DarkButton CreateFilterButton(string text, DriverFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == DriverFilterMode.All);
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
			btnFilterAll.AccentStyle = (currentFilter == DriverFilterMode.All);
			btnFilterCheatDrivers.AccentStyle = (currentFilter == DriverFilterMode.CheatDriversOnly);
			btnFilterDisabledForensics.AccentStyle = (currentFilter == DriverFilterMode.DisabledForensicsOnly);
			btnFilterThirdParty.AccentStyle = (currentFilter == DriverFilterMode.ThirdPartyDrivers);
			btnFilterClean.AccentStyle = (currentFilter == DriverFilterMode.CleanServices);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование служб...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<DriverServiceItem> results = new List<DriverServiceItem>();

				// Ключевые службы криминалистики Windows
				HashSet<string> forensicServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
				{
					"DPS", "SysMain", "EventLog", "PcaSvc", "DiagTrack", "WSearch"
				};

				try
				{
					using (RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
					{
						if (servicesKey != null)
						{
							string[] names = servicesKey.GetSubKeyNames();
							foreach (string sName in names)
							{
								using (RegistryKey key = servicesKey.OpenSubKey(sName))
								{
									if (key == null) continue;

									object typeObj = key.GetValue("Type");
									int sType = (typeObj != null) ? Convert.ToInt32(typeObj) : 0;

									object startObj = key.GetValue("Start");
									int sStart = (startObj != null) ? Convert.ToInt32(startObj) : 3;

									object dispObj = key.GetValue("DisplayName");
									string dispName = (dispObj != null) ? dispObj.ToString() : sName;

									object imgObj = key.GetValue("ImagePath");
									string imgPath = (imgObj != null) ? imgObj.ToString() : string.Empty;

									bool isCheatDriver = CheatDriverRegex.IsMatch(sName) || CheatDriverRegex.IsMatch(imgPath) || CheatDriverRegex.IsMatch(dispName);
									bool isForensic = forensicServices.Contains(sName);

									// Проверяем статус физического файла
									string cleanPath = CleanImagePath(imgPath);
									bool exists = false;
									if (!string.IsNullOrEmpty(cleanPath))
									{
										try { exists = File.Exists(cleanPath); } catch { }
									}

									DriverServiceItem item = new DriverServiceItem();
									item.ServiceName = sName;
									item.DisplayName = dispName;
									item.ImagePath = imgPath;
									item.ServiceType = sType;
									item.StartType = sStart;
									item.ExistsOnDisk = exists;
									item.IsCheatDriver = isCheatDriver;
									item.IsForensicService = isForensic;

									// Анализ нарушений
									if (isForensic)
									{
										// Проверка отключения службы (Start = 4 - Disabled)
										if (sStart == 4)
										{
											item.IsTampered = true;
											item.Status = "🚨 СЛУЖБА ОТКЛЮЧЕНА В РЕЕСТРЕ (5.08)";
											item.RuleViolation = "5.08";
											item.BanReason = string.Format("Отключение критической службы криминалистики Windows ({0})", sName);
											item.ForeColor = Ui.AccentRed;
											item.Details = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА 5.08!\nСлужба: {0} ({1})\nЗначение Start в реестре: 4 (Отключена / Disabled)\nПоследствия: Сокрытие истории Prefetch, Shimcache или системных журналов EventLog.\nЭто умышленное противодействие расследованию.",
												sName, dispName);
										}
										else
										{
											item.Status = "🛡️ Криминалистическая служба активна";
											item.RuleViolation = "-";
											item.BanReason = "-";
											item.ForeColor = Ui.AccentGreen;
											item.Details = string.Format("Служба: {0} ({1})\nТип запуска: {2}\nСтатус: Служба настроена штатно.", sName, dispName, FormatStartType(sStart));
										}
										results.Add(item);
									}
									else if (isCheatDriver)
									{
										item.IsTampered = true;
										if (!exists && !string.IsNullOrEmpty(cleanPath))
										{
											item.Status = "🚨 ДРАЙВЕР ИНЖЕКТОРА БЫЛ УДАЛЕН С ДИСКА (5.08)";
											item.RuleViolation = "5.08";
											item.BanReason = string.Format("Удаление драйвера чит-инжектора {0} с диска", sName);
											item.ForeColor = Ui.AccentRed;
											item.Details = string.Format("ЯДЕРНЫЙ ДРАЙВЕР ЧИТА БЫЛ ЗАГРУЖЕН И УДАЛЕН!\nИмя службы: {0}\nПуть: {1}\nСтатус файла: Отсутствует на диске (удален игроком)\nНазначение драйвера: Кольцо 0 (Ring0), чтение/запись виртуальной памяти javaw в обход проверок Process Hacker / Cheat Engine.",
												sName, imgPath);
										}
										else
										{
											item.Status = "🚨 НАЙДЕН ДРАЙВЕР ИНЖЕКТОРА / ЧИТА (5.01)";
											item.RuleViolation = "5.01";
											item.BanReason = string.Format("Использование чит-драйвера ядра {0}", sName);
											item.ForeColor = Ui.AccentRed;
											item.Details = string.Format("ОБНАРУЖЕН ДРАЙВЕР ИНЖЕКЦИИ ЧИТОВ В ПАМЯТЬ!\nИмя: {0}\nПуть: {1}\nТип: Ядерный драйвер (Kernel Driver)\nНазначение: Манипуляция памятью Minecraft на уровне ядра Windows.",
												sName, imgPath);
										}
										results.Add(item);
									}
									else if (sType == 1 || sType == 2)
									{
										// Прочие сторонние драйверы
										bool isMicrosoft = imgPath.IndexOf("system32\\drivers", StringComparison.OrdinalIgnoreCase) >= 0 &&
														   !imgPath.Contains("Temp") && !imgPath.Contains("Users");

										item.Status = isMicrosoft ? "Системный драйвер Windows" : "Сторонний драйвер ядра";
										item.RuleViolation = "-";
										item.BanReason = "-";
										item.ForeColor = isMicrosoft ? Ui.TextSecondary : Ui.AccentBlue;
										item.Details = string.Format("Драйвер: {0} ({1})\nПуть: {2}\nТип запуска: {3}", sName, dispName, imgPath, FormatStartType(sStart));
										results.Add(item);
									}
								}
							}
						}
					}
				}
				catch { }

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(DriverServiceItem x) { return x.IsTampered; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УГРОЗ ДРАЙВЕРОВ / СЛУЖБ: {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Службы и драйверы чисты ({0})", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private string CleanImagePath(string rawPath)
		{
			if (string.IsNullOrEmpty(rawPath)) return string.Empty;
			string path = rawPath.Trim();
			if (path.StartsWith(@"\??\")) path = path.Substring(4);
			if (path.StartsWith("\"") && path.Length > 2)
			{
				int endQuote = path.IndexOf('"', 1);
				if (endQuote > 1) path = path.Substring(1, endQuote - 1);
				else path = path.Trim('"');
			}

			if (path.StartsWith(@"system32\", StringComparison.OrdinalIgnoreCase))
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				path = Path.Combine(winDir, path);
			}

			int argIdx = path.IndexOf(" -", StringComparison.Ordinal);
			if (argIdx > 0) path = path.Substring(0, argIdx).Trim();
			int slashArg = path.IndexOf(" /", StringComparison.Ordinal);
			if (slashArg > 0) path = path.Substring(0, slashArg).Trim();

			return path;
		}

		private string FormatStartType(int start)
		{
			switch (start)
			{
				case 0: return "Boot (Загрузка)";
				case 1: return "System (Система)";
				case 2: return "Auto (Автоматически)";
				case 3: return "Demand (Вручную)";
				case 4: return "Disabled (ОТКЛЮЧЕНО)";
				default: return start.ToString();
			}
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (DriverServiceItem item in allEntries)
			{
				if (currentFilter == DriverFilterMode.CheatDriversOnly && !item.IsCheatDriver) continue;
				if (currentFilter == DriverFilterMode.DisabledForensicsOnly && (!item.IsForensicService || item.StartType != 4)) continue;
				if (currentFilter == DriverFilterMode.ThirdPartyDrivers && item.ServiceType != 1 && item.ServiceType != 2) continue;
				if (currentFilter == DriverFilterMode.CleanServices && item.IsTampered) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.ServiceName != null && item.ServiceName.ToLowerInvariant().Contains(search)) ||
								   (item.DisplayName != null && item.DisplayName.ToLowerInvariant().Contains(search)) ||
								   (item.ImagePath != null && item.ImagePath.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.ServiceName);
				lvi.SubItems.Add(FormatStartType(item.StartType));
				lvi.SubItems.Add(item.ExistsOnDisk ? "✅ На диске" : "❌ Отсутствует");
				lvi.SubItems.Add(item.ImagePath);
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

			DriverServiceItem item = listView.SelectedItems[0].Tag as DriverServiceItem;
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
			lblDetailVerdict.Text = "Служба не выбрана";
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
				MessageBox.Show("Выберите службу или драйвер с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			DriverServiceItem item = listView.SelectedItems[0].Tag as DriverServiceItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenFolder_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			DriverServiceItem item = listView.SelectedItems[0].Tag as DriverServiceItem;
			if (item == null) return;

			string clean = CleanImagePath(item.ImagePath);
			try
			{
				if (File.Exists(clean))
				{
					Process.Start("explorer.exe", string.Format("/select,\"{0}\"", clean));
				}
				else
				{
					string dir = Path.GetDirectoryName(clean);
					if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					{
						Process.Start("explorer.exe", string.Format("\"{0}\"", dir));
					}
					else
					{
						MessageBox.Show("Файл драйвера отсутствует на диске:\n" + clean, "Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть каталог: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Драйверы_Службы_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА ДРАЙВЕРОВ И СЛУЖБ WINDOWS");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (DriverServiceItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1} (Запуск: {2}, Диск: {3})",
							item.ServiceName, item.DisplayName, FormatStartType(item.StartType), item.ExistsOnDisk ? "На диске" : "ОТСУТСТВУЕТ"));
						sb.AppendLine(string.Format("Путь:        {0}", item.ImagePath));
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
