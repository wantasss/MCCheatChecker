using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum EvasionFilterMode
	{
		All,
		ViolationsOnly,
		MountPointsAndDisks,
		ClipboardHistory,
		SelfDestructAndWiping,
		WhitelistedAndSafe
	}

	public class EvasionEvidenceItem
	{
		public int Index;
		public string Category; // "MountPoints2 / Диски", "Буфер history.db", "Самоликвидация / Зачистка"
		public string SourceArtifact;
		public string TargetOrToken;
		public string Status;
		public string ThreatLevel; // "CRITICAL", "WARNING", "SAFE", "INFO"
		public string Verdict;
		public string TargetRule; // "5.08", "5.01", "РАЗРЕШЕНО"
		public bool IsViolation;
		public bool IsWhitelisted;
		public DateTime Timestamp;
		public Color RowColor;
		public string DetailDescription;
	}

	public class AntiEvasionAndStealthBypassViewerForm : Form
	{
		private readonly string targetPlayerName;
		private List<EvasionEvidenceItem> allItems = new List<EvasionEvidenceItem>();
		private EvasionFilterMode currentFilter = EvasionFilterMode.All;
		private BackgroundWorker scanWorker;

		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblStatusPill;

		private Label lblKpiTotal;
		private Label lblKpiViolations;
		private Label lblKpiMountPoints;
		private Label lblKpiClipboard;
		private Label lblKpiWiping;
		private Label lblKpiWhitelisted;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterMountPoints;
		private DarkButton btnFilterClipboard;
		private DarkButton btnFilterWiping;
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

		public AntiEvasionAndStealthBypassViewerForm() : this("Player")
		{
		}

		public AntiEvasionAndStealthBypassViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "🥷 Инспектор обхода проверок, сокрытия софта и анти-форензики (Tool 69 / Step 66) — MCCheatChecker";
			this.Size = new Size(1340, 820);
			this.MinimumSize = new Size(1040, 640);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			// Панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🥷 ИНСПЕКТОР ОБХОДА ПРОВЕРОК, СОКРЫТИЯ СОФТА И АНТИ-ФОРЕНЗИКИ";
			lblHeaderTitle.Font = Ui.FontTitle(12.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentRed;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Судебная экспертиза сокрытия софта: извлечение USB/RamDisk (MountPoints2), история буфера обмена history.db (токены Vape/Drip/Slinky), самоликвидаторы (fsutil, wevtutil, vssadmin).";
			lblHeaderSubtitle.Font = Ui.FontBody(8.5f);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования...";
			lblStatusPill.Font = Ui.FontTitle(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(headerPanel.Width - 320, 16);
			lblStatusPill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblStatusPill.AutoSize = true;
			headerPanel.Controls.Add(lblStatusPill);

			// Карточки KPI
			Panel kpiPanel = new Panel();
			kpiPanel.Dock = DockStyle.Top;
			kpiPanel.Height = 64;
			kpiPanel.BackColor = Ui.SurfaceAlt;
			kpiPanel.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(kpiPanel);

			int kx = 12;
			kpiPanel.Controls.Add(CreateKpiBadge("ВСЕГО ОБЪЕКТОВ", "0", Ui.TextPrimary, ref kx, out lblKpiTotal));
			kpiPanel.Controls.Add(CreateKpiBadge("🚨 НАРУШЕНИЯ (5.08/5.01)", "0", Ui.AccentRed, ref kx, out lblKpiViolations));
			kpiPanel.Controls.Add(CreateKpiBadge("💾 MOUNTPOINTS2 / ДИСКИ", "0", Ui.AccentBlue, ref kx, out lblKpiMountPoints));
			kpiPanel.Controls.Add(CreateKpiBadge("📋 БУФЕР HISTORY.DB", "0", Ui.AccentPurple, ref kx, out lblKpiClipboard));
			kpiPanel.Controls.Add(CreateKpiBadge("🧹 ЗАЧИСТКА / САМОЛИКВИДАЦИЯ", "0", Ui.AccentOrange, ref kx, out lblKpiWiping));
			kpiPanel.Controls.Add(CreateKpiBadge("🛡️ БЕЛЫЙ СПИСОК", "0", Ui.AccentGreen, ref kx, out lblKpiWhitelisted));

			// Панель фильтров
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все проверки", EvasionFilterMode.All, ref bx, filterBar);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения 5.08 / 5.01", EvasionFilterMode.ViolationsOnly, ref bx, filterBar);
			btnFilterMountPoints = CreateFilterButton("💾 MountPoints2 и диски", EvasionFilterMode.MountPointsAndDisks, ref bx, filterBar);
			btnFilterClipboard = CreateFilterButton("📋 Буфер history.db", EvasionFilterMode.ClipboardHistory, ref bx, filterBar);
			btnFilterWiping = CreateFilterButton("🧹 Зачистка и клинеры", EvasionFilterMode.SelfDestructAndWiping, ref bx, filterBar);
			btnFilterWhitelisted = CreateFilterButton("🛡️ Белый список", EvasionFilterMode.WhitelistedAndSafe, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Placeholder = "🔍 Поиск по уликам, токенам, путям и командам...";
			searchBox.Size = new Size(240, 26);
			searchBox.Location = new Point(filterBar.Width - 260, 8);
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
			btnRescan = CreateActionButton("🔄 Повторить аудит сокрытия", ref ax, bottomActions, delegate { StartScan(); });

			// Разделитель
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 350;
			split.BackColor = Ui.Border;
			this.Controls.Add(split);

			// Список ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Columns.Add("№", 45);
			listView.Columns.Add("Категория", 185);
			listView.Columns.Add("Источник / Артефакт", 220);
			listView.Columns.Add("Объект / Токен / Команда", 270);
			listView.Columns.Add("Статус", 110);
			listView.Columns.Add("Угроза", 95);
			listView.Columns.Add("Вердикт детектора", 290);
			listView.Columns.Add("Регламент", 95);
			listView.Columns.Add("Штамп времени", 130);
			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Вкладки деталей
			detailTabs = new TabControl();
			detailTabs.Dock = DockStyle.Fill;
			split.Panel2.Controls.Add(detailTabs);

			TabPage tabAnalysis = new TabPage("🔍 Криминалистический анализ сокрытия и обхода");
			txtAnalysis = new DarkTextBoxMultiline();
			txtAnalysis.Dock = DockStyle.Fill;
			txtAnalysis.ReadOnly = true;
			txtAnalysis.Font = Ui.FontMono(9f);
			tabAnalysis.Controls.Add(txtAnalysis);
			detailTabs.TabPages.Add(tabAnalysis);

			TabPage tabTimeline = new TabPage("⏱️ Хронологическая цепочка улик");
			txtTimeline = new DarkTextBoxMultiline();
			txtTimeline.Dock = DockStyle.Fill;
			txtTimeline.ReadOnly = true;
			txtTimeline.Font = Ui.FontMono(9f);
			tabTimeline.Controls.Add(txtTimeline);
			detailTabs.TabPages.Add(tabTimeline);

			TabPage tabBan = new TabPage("⚖️ Регламент 5.08 / 5.01 и Команда бана");
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
			pnl.Size = new Size(198, 52);
			pnl.Location = new Point(curX, 6);
			pnl.BackColor = Ui.Surface;
			pnl.BorderStyle = BorderStyle.FixedSingle;

			Label lblTitle = new Label();
			lblTitle.Text = title;
			lblTitle.Font = Ui.FontBody(7.5f);
			lblTitle.ForeColor = Ui.TextSecondary;
			lblTitle.AutoSize = false;
			lblTitle.Size = new Size(190, 16);
			lblTitle.Location = new Point(4, 4);
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(lblTitle);

			valLabel = new Label();
			valLabel.Text = initialValue;
			valLabel.Font = Ui.FontTitle(12f, true);
			valLabel.ForeColor = accentColor;
			valLabel.AutoSize = false;
			valLabel.Size = new Size(190, 26);
			valLabel.Location = new Point(4, 20);
			valLabel.TextAlign = ContentAlignment.MiddleLeft;
			pnl.Controls.Add(valLabel);

			curX += 204;
			return pnl;
		}

		private DarkButton CreateFilterButton(string text, EvasionFilterMode mode, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8f, true);
			btn.Size = new Size(190, 28);
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
			btnFilterAll.AccentStyle = (currentFilter == EvasionFilterMode.All);
			btnFilterViolations.AccentStyle = (currentFilter == EvasionFilterMode.ViolationsOnly);
			btnFilterMountPoints.AccentStyle = (currentFilter == EvasionFilterMode.MountPointsAndDisks);
			btnFilterClipboard.AccentStyle = (currentFilter == EvasionFilterMode.ClipboardHistory);
			btnFilterWiping.AccentStyle = (currentFilter == EvasionFilterMode.SelfDestructAndWiping);
			btnFilterWhitelisted.AccentStyle = (currentFilter == EvasionFilterMode.WhitelistedAndSafe);
		}

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Идёт глубокий аудит сокрытия и зачистки...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			listView.Items.Clear();
			allItems.Clear();

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<EvasionEvidenceItem> items = new List<EvasionEvidenceItem>();
			int idx = 1;

			// 1. MountPoints2, USB/RamDisk извлечение
			ScanMountPointsAndDisks(items, ref idx);

			// 2. Windows 10/11 Clipboard history.db и активный буфер
			ScanClipboardHistoryDb(items, ref idx);

			// 3. Самоликвидация и зачистка логов (fsutil usn, wevtutil cl, vssadmin)
			ScanSelfDestructAndWiping(items, ref idx);

			e.Result = items;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			allItems = e.Result as List<EvasionEvidenceItem>;
			if (allItems == null) allItems = new List<EvasionEvidenceItem>();

			int violations = 0;
			int mountPoints = 0;
			int clipboard = 0;
			int wiping = 0;
			int whitelisted = 0;

			foreach (EvasionEvidenceItem it in allItems)
			{
				if (it.IsViolation) violations++;
				if (it.IsWhitelisted) whitelisted++;

				if (it.Category.Contains("MountPoints2") || it.Category.Contains("Диски")) mountPoints++;
				else if (it.Category.Contains("Буфер")) clipboard++;
				else if (it.Category.Contains("Зачистка") || it.Category.Contains("Самоликвидация")) wiping++;
			}

			lblKpiTotal.Text = allItems.Count.ToString();
			lblKpiViolations.Text = violations.ToString();
			lblKpiMountPoints.Text = mountPoints.ToString();
			lblKpiClipboard.Text = clipboard.ToString();
			lblKpiWiping.Text = wiping.ToString();
			lblKpiWhitelisted.Text = whitelisted.ToString();

			if (violations > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} НАРУШЕНИЙ (5.08 / 5.01)!", violations);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = "✅ Следов сокрытия и зачистки не обнаружено";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}

			UpdateFilterButtonStyles();
			ApplyFilter();
		}

		#region Detection Engine

		private void ScanMountPointsAndDisks(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				// Получаем текущие смонтированные диски
				DriveInfo[] activeDrives = DriveInfo.GetDrives();
				HashSet<string> activeLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (DriveInfo d in activeDrives)
				{
					activeLetters.Add(d.Name.TrimEnd('\\'));
				}

				// Читаем сопоставление \DosDevices\ с Volume GUID из HKLM\SYSTEM\MountedDevices
				Dictionary<string, string> volumeToDos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				try
				{
					using (RegistryKey mdKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\MountedDevices"))
					{
						if (mdKey != null)
						{
							foreach (string valName in mdKey.GetValueNames())
							{
								if (valName.StartsWith(@"\DosDevices\", StringComparison.OrdinalIgnoreCase))
								{
									string dosDrive = valName.Substring(12);
									byte[] data = mdKey.GetValue(valName) as byte[];
									if (data != null)
									{
										string rawStr = Encoding.Unicode.GetString(data);
										volumeToDos[dosDrive] = rawStr;
									}
								}
							}
						}
					}
				}
				catch { }

				// Сканируем HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2
				using (RegistryKey mpKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2"))
				{
					if (mpKey != null)
					{
						string[] subNames = mpKey.GetSubKeyNames();
						foreach (string sub in subNames)
						{
							if (sub.StartsWith("{", StringComparison.OrdinalIgnoreCase) && sub.EndsWith("}", StringComparison.OrdinalIgnoreCase))
							{
								// Точка монтирования тома GUID
								string volGuid = sub;
								string boundDrive = "";
								foreach (KeyValuePair<string, string> kv in volumeToDos)
								{
									if (kv.Value.IndexOf(volGuid, StringComparison.OrdinalIgnoreCase) >= 0)
									{
										boundDrive = kv.Key;
										break;
									}
								}

								// Проверяем, существует ли сейчас этот диск
								bool isCurrentlyActive = false;
								if (!string.IsNullOrEmpty(boundDrive))
								{
									isCurrentlyActive = activeLetters.Contains(boundDrive);
								}

								// Читаем дополнительные свойства точки монтирования
								string label = "";
								try
								{
									using (RegistryKey volSub = mpKey.OpenSubKey(sub))
									{
										if (volSub != null)
										{
											object val = volSub.GetValue("_LabelFromReg");
											if (val != null) label = val.ToString();
										}
									}
								}
								catch { }

								if (!string.IsNullOrEmpty(boundDrive) && !isCurrentlyActive)
								{
									EvasionEvidenceItem it = new EvasionEvidenceItem();
									it.Index = idx++;
									it.Category = "MountPoints2 / Диски";
									it.SourceArtifact = @"HKCU\...\MountPoints2\" + volGuid;
									it.TargetOrToken = string.Format("Диск {0} ({1})", boundDrive, string.IsNullOrEmpty(label) ? "Съёмный накопитель" : label);
									it.Status = "ИЗВЛЕЧЁН / ОТКЛЮЧЁН";
									it.ThreatLevel = "WARNING";
									it.Verdict = "⚠️ Съёмный том отключён от системы (MountPoints2)";
									it.TargetRule = "5.08";
									it.IsViolation = false; // Предупреждение, станет Critical при корреляции с читами
									it.IsWhitelisted = false;
									it.Timestamp = DateTime.Now;
									it.RowColor = Ui.AccentOrange;
									it.DetailDescription = string.Format("В реестре Windows (MountPoints2) обнаружена запись о монтировании тома {0}, которому был назначен диск {1}.\nВ настоящий момент диск {1} отсутствует в системе (физически извлечён или размонтирован).\nЕсли перед проверкой с данного диска запускался софт — это прямое сокрытие улик по регламенту 5.08.", volGuid, boundDrive);
									items.Add(it);
								}
							}
							else if (sub.StartsWith("##", StringComparison.OrdinalIgnoreCase))
							{
								// Сетевой UNC ресурс
								string uncPath = sub.Replace('#', '\\');
								EvasionEvidenceItem it = new EvasionEvidenceItem();
								it.Index = idx++;
								it.Category = "MountPoints2 / Диски";
								it.SourceArtifact = @"HKCU\...\MountPoints2";
								it.TargetOrToken = uncPath;
								it.Status = "СЕТЕВОЙ UNC РЕСУРС";
								it.ThreatLevel = "INFO";
								it.Verdict = "ℹ️ Подключение к сетевой папке UNC";
								it.TargetRule = "РАЗРЕШЕНО";
								it.IsViolation = false;
								it.IsWhitelisted = true;
								it.Timestamp = DateTime.Now;
								it.RowColor = Ui.TextSecondary;
								it.DetailDescription = string.Format("Зафиксировано подключение к сетевой папке: {0}.\nИспользование сетевых дисков разрешено, если с них не запускались чит-клиенты.", uncPath);
								items.Add(it);
							}
						}
					}
				}

				// Корреляция: проверяем запуск программ с отключённых дисков через ComDlg32 / Recent / Prefetch
				ScanExecutionsFromEjectedDrives(items, ref idx, activeLetters);

				// Сканирование драйверов и служб RamDisk (ImDisk, Primo, OSFMount)
				ScanRamDiskSoftware(items, ref idx);

				// Сканирование журнала событий PnP на недавнее извлечение USB (Event ID 420 / 225)
				ScanUsbEjectionEvents(items, ref idx);
			}
			catch (Exception ex)
			{
				EvasionEvidenceItem err = new EvasionEvidenceItem();
				err.Index = idx++;
				err.Category = "MountPoints2 / Диски";
				err.SourceArtifact = "MountPoints2 Engine";
				err.TargetOrToken = ex.Message;
				err.Status = "ОШИБКА";
				err.ThreatLevel = "INFO";
				err.Verdict = "Ошибка чтения точек монтирования: " + ex.Message;
				err.TargetRule = "ИНФО";
				err.Timestamp = DateTime.Now;
				err.RowColor = Ui.TextMuted;
				items.Add(err);
			}
		}

		private void ScanExecutionsFromEjectedDrives(List<EvasionEvidenceItem> items, ref int idx, HashSet<string> activeLetters)
		{
			try
			{
				// Проверяем OpenSavePidlMRU
				string[] exts = new string[] { "exe", "jar", "bat", "cmd", "dll", "zip" };
				using (RegistryKey osmKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU"))
				{
					if (osmKey != null)
					{
						foreach (string ext in exts)
						{
							using (RegistryKey subKey = osmKey.OpenSubKey(ext))
							{
								if (subKey != null)
								{
									foreach (string valName in subKey.GetValueNames())
									{
										if (valName.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
										byte[] data = subKey.GetValue(valName) as byte[];
										if (data != null && data.Length > 4)
										{
											string raw = Encoding.Unicode.GetString(data);
											Match m = Regex.Match(raw, @"([a-zA-Z]:\\[^\\/:*?""<>|\r\n]+\." + ext + ")", RegexOptions.IgnoreCase);
											if (m.Success)
											{
												string filePath = m.Groups[1].Value;
												string driveLetter = Path.GetPathRoot(filePath).TrimEnd('\\');
												if (!string.IsNullOrEmpty(driveLetter) && !activeLetters.Contains(driveLetter))
												{
													// Запуск исполняемого файла с извлечённого диска!
													EvasionEvidenceItem it = new EvasionEvidenceItem();
													it.Index = idx++;
													it.Category = "MountPoints2 / Диски";
													it.SourceArtifact = @"ComDlg32\OpenSavePidlMRU\" + ext;
													it.TargetOrToken = filePath;
													it.Status = "ЗАПУСК С ИЗВЛЕЧЁННОГО ДИСКА";
													it.ThreatLevel = "CRITICAL";
													it.Verdict = "🚨 ЗАПУСК СОФТА С ИЗВЛЕЧЁННОЙ ФЛЕШКИ (5.08)";
													it.TargetRule = "5.08";
													it.IsViolation = true;
													it.IsWhitelisted = false;
													it.Timestamp = DateTime.Now;
													it.RowColor = Ui.AccentRed;
													it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nЗафиксирован выбор/запуск файла '{0}' с диска '{1}', который в данный момент физически извлечён из компьютера!\nЭто типичный паттерн сокрытия: чит запускается с флешки, после чего накопитель выдёргивается перед проверкой.", filePath, driveLetter);
													items.Add(it);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanRamDiskSoftware(List<EvasionEvidenceItem> items, ref int idx)
		{
			string[] ramDrivers = new string[] { "imdisk", "awealloc", "pramdisk", "osfmount", "sramdisk", "spldisk" };
			foreach (string dName in ramDrivers)
			{
				try
				{
					using (RegistryKey sKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + dName))
					{
						if (sKey != null)
						{
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "MountPoints2 / Диски";
							it.SourceArtifact = @"Services\" + dName;
							it.TargetOrToken = "Драйвер RamDisk: " + dName;
							it.Status = "ОБНАРУЖЕН ДРАЙВЕР RAMDISK";
							it.ThreatLevel = "WARNING";
							it.Verdict = "⚠️ Драйвер виртуального RAM-диска в системе (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = false;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentOrange;
							it.DetailDescription = string.Format("В системе установлен драйвер виртуального диска в оперативной памяти: {0}.\nRAM-диски часто используются читерами для размещения читов в памяти без сохранения на SSD/HDD с мгновенным уничтожением при размонтировании.", dName);
							items.Add(it);
						}
					}
				}
				catch { }
			}

			// Проверка запущенных процессов RamDisk
			string[] ramProcs = new string[] { "imdisk", "imdiskui", "osfmount", "pramdisk", "spldisk" };
			foreach (string pName in ramProcs)
			{
				try
				{
					Process[] procs = Process.GetProcessesByName(pName);
					if (procs.Length > 0)
					{
						EvasionEvidenceItem it = new EvasionEvidenceItem();
						it.Index = idx++;
						it.Category = "MountPoints2 / Диски";
						it.SourceArtifact = "Активный процесс ОС";
						it.TargetOrToken = pName + ".exe (PID: " + procs[0].Id + ")";
						it.Status = "АКТИВЕН RAMDISK МЕНЕДЖЕР";
						it.ThreatLevel = "CRITICAL";
						it.Verdict = "🚨 АКТИВНЫЙ RAMDISK ДИСК В ПАМЯТИ (5.08)";
						it.TargetRule = "5.08";
						it.IsViolation = true;
						it.IsWhitelisted = false;
						it.Timestamp = DateTime.Now;
						it.RowColor = Ui.AccentRed;
						it.DetailDescription = string.Format("Обнаружен работающий процесс менеджера виртуальных RAM-дисков: {0}.exe.\nХранение или запуск клиентов с RAM-диска приравнивается к сокрытию следов по правилу 5.08.", pName);
						items.Add(it);
					}
				}
				catch { }
			}
		}

		private void ScanUsbEjectionEvents(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				string queryStr = "*[System[(EventID=420 or EventID=225)] and System[TimeCreated[timediff(@SystemTime) <= 86400000]]]";
				EventLogQuery q = new EventLogQuery("Microsoft-Windows-Kernel-PnP/Configuration", PathType.LogName, queryStr);
				using (EventLogReader reader = new EventLogReader(q))
				{
					EventRecord rec;
					int count = 0;
					while ((rec = reader.ReadEvent()) != null && count < 10)
					{
						using (rec)
						{
							count++;
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "MountPoints2 / Диски";
							it.SourceArtifact = "Kernel-PnP (Event ID " + rec.Id + ")";
							it.TargetOrToken = "Извлечение устройства USB / Storage";
							it.Status = "ИЗВЛЕЧЕНО " + dt.ToString("HH:mm:ss");
							it.ThreatLevel = "WARNING";
							it.Verdict = "⚠️ Физическое отключение USB-устройства перед проверкой (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = false;
							it.IsWhitelisted = false;
							it.Timestamp = dt;
							it.RowColor = Ui.AccentOrange;
							it.DetailDescription = string.Format("Журнал Kernel-PnP зафиксировал факт отключения/извлечения съёмного устройства в {0}.\nЕсли игрок извлёк флешку после вызова на проверку — это прямое нарушение 5.08.", dt.ToString("dd.MM.yyyy HH:mm:ss"));
							items.Add(it);
						}
					}
				}
			}
			catch { }
		}

		private void ScanClipboardHistoryDb(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string clipDir = Path.Combine(localApp, "Microsoft", "Windows", "Clipboard");
				string dbPath = Path.Combine(clipDir, "history.db");

				// Проверяем включена ли история буфера обмена в реестре
				bool historyEnabled = false;
				try
				{
					using (RegistryKey cKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
					{
						if (cKey != null)
						{
							object val = cKey.GetValue("EnableClipboardHistory");
							if (val is int && (int)val == 1) historyEnabled = true;
						}
					}
				}
				catch { }

				if (!File.Exists(dbPath))
				{
					EvasionEvidenceItem it = new EvasionEvidenceItem();
					it.Index = idx++;
					it.Category = "Буфер history.db";
					it.SourceArtifact = @"%LocalAppData%\Microsoft\Windows\Clipboard";
					it.TargetOrToken = "history.db";
					it.Status = historyEnabled ? "ОТСУТСТВУЕТ (ОЧИЩЕНА?)" : "ОТКЛЮЧЕНА В ОС";
					it.ThreatLevel = historyEnabled ? "WARNING" : "SAFE";
					it.Verdict = historyEnabled ? "⚠️ База history.db отсутствует при включённой истории (Возможна очистка)" : "ℹ️ История буфера обмена (Win+V) не используется";
					it.TargetRule = historyEnabled ? "5.08" : "РАЗРЕШЕНО";
					it.IsViolation = false;
					it.IsWhitelisted = !historyEnabled;
					it.Timestamp = DateTime.Now;
					it.RowColor = historyEnabled ? Ui.AccentOrange : Ui.TextSecondary;
					it.DetailDescription = historyEnabled
						? "Служба истории буфера обмена включена в реестре, однако файл history.db отсутствует или был недавно удалён. Это может указывать на зачистку буфера обмена перед проверкой."
						: "История буфера обмена Windows 10/11 отключена пользователем. Это штатная конфигурация системы.";
					items.Add(it);
				}
				else
				{
					// База history.db существует — анализируем размер и штамп времени
					FileInfo fi = new FileInfo(dbPath);
					if (fi.Length == 0)
					{
						EvasionEvidenceItem it = new EvasionEvidenceItem();
						it.Index = idx++;
						it.Category = "Буфер history.db";
						it.SourceArtifact = dbPath;
						it.TargetOrToken = "history.db (0 байт)";
						it.Status = "ОБНУЛЁН";
						it.ThreatLevel = "CRITICAL";
						it.Verdict = "🚨 ФАЙЛ БУФЕРА ОБМЕНА HISTORY.DB ОБНУЛЁН (5.08)";
						it.TargetRule = "5.08";
						it.IsViolation = true;
						it.IsWhitelisted = false;
						it.Timestamp = fi.LastWriteTime;
						it.RowColor = Ui.AccentRed;
						it.DetailDescription = string.Format("Файл базы буфера обмена history.db имеет нулевой размер (0 байт) при дате изменения {0}.\nЭто прямое свидетельство принудительной зачистки или обнуления буфера скриптом-клинером перед проверкой.", fi.LastWriteTime);
						items.Add(it);
					}
					else
					{
						// Считываем бинарные страницы SQLite и извлекаем текстовые блоки
						byte[] dbBytes = null;
						using (FileStream fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							using (MemoryStream ms = new MemoryStream())
							{
								byte[] buffer = new byte[8192];
								int read;
								while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
								{
									ms.Write(buffer, 0, read);
								}
								dbBytes = ms.ToArray();
							}
						}

						// Проверяем сопутствующий wal журнал, если есть
						string walPath = Path.Combine(clipDir, "history.db-wal");
						byte[] walBytes = null;
						if (File.Exists(walPath))
						{
							try
							{
								using (FileStream fs = new FileStream(walPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									using (MemoryStream ms = new MemoryStream())
									{
										byte[] buffer = new byte[8192];
										int read;
										while ((read = fs.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
										walBytes = ms.ToArray();
									}
								}
							}
							catch { }
						}

						// Извлекаем строки из базы
						List<string> extractedStrings = ExtractStringsFromBinary(dbBytes);
						if (walBytes != null)
						{
							extractedStrings.AddRange(ExtractStringsFromBinary(walBytes));
						}

						ScanExtractedClipboardStrings(items, ref idx, extractedStrings, dbPath);
					}
				}

				// Сканируем текущий активный буфер обмена Windows
				ScanActiveClipboard(items, ref idx);
			}
			catch (Exception ex)
			{
				EvasionEvidenceItem err = new EvasionEvidenceItem();
				err.Index = idx++;
				err.Category = "Буфер history.db";
				err.SourceArtifact = "Clipboard Engine";
				err.TargetOrToken = ex.Message;
				err.Status = "ОШИБКА";
				err.ThreatLevel = "INFO";
				err.Verdict = "Ошибка чтения history.db: " + ex.Message;
				err.TargetRule = "ИНФО";
				err.Timestamp = DateTime.Now;
				err.RowColor = Ui.TextMuted;
				items.Add(err);
			}
		}

		private static List<string> ExtractStringsFromBinary(byte[] bytes)
		{
			List<string> result = new List<string>();
			if (bytes == null || bytes.Length == 0) return result;

			// Извлечение ASCII / UTF-8
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				byte b = bytes[i];
				if (b >= 32 && b <= 126)
				{
					sb.Append((char)b);
				}
				else
				{
					if (sb.Length >= 4)
					{
						result.Add(sb.ToString());
					}
					sb.Length = 0;
				}
			}
			if (sb.Length >= 4) result.Add(sb.ToString());

			// Извлечение UTF-16LE
			sb.Length = 0;
			for (int i = 0; i < bytes.Length - 1; i += 2)
			{
				byte b1 = bytes[i];
				byte b2 = bytes[i + 1];
				if (b2 == 0 && b1 >= 32 && b1 <= 126)
				{
					sb.Append((char)b1);
				}
				else
				{
					if (sb.Length >= 4)
					{
						result.Add(sb.ToString());
					}
					sb.Length = 0;
				}
			}
			if (sb.Length >= 4) result.Add(sb.ToString());

			return result;
		}

		private void ScanExtractedClipboardStrings(List<EvasionEvidenceItem> items, ref int idx, List<string> strings, string dbPath)
		{
			string[] cheatSignatures = new string[]
			{
				"vape.gg", "vape v4", "vape lite", "auth.vape", "vape_token", "vapeclient",
				"drip.gg", "drip lite", "drip client", "driptoken",
				"slinky.gg", "slinky client", "slinky token", "slinky_token",
				"expensive client", "celestial client", "nursultan client", "nurik client",
				"akrien client", "wexside client", "doomsday client", "riseclient", "liquidbounce",
				"alts.top", "thealtening", "easyalts"
			};

			string[] cleanerSignatures = new string[]
			{
				"fsutil usn deletejournal", "wevtutil cl", "vssadmin delete shadows", "clear-eventlog", "sdelete"
			};

			HashSet<string> seenHits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string raw in strings)
			{
				string lower = raw.ToLowerInvariant();

				// Проверка на читы (5.01)
				foreach (string sig in cheatSignatures)
				{
					if (lower.Contains(sig))
					{
						if (seenHits.Add(sig + ":" + raw))
						{
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Буфер history.db";
							it.SourceArtifact = "%LocalAppData%\\...\\history.db";
							it.TargetOrToken = raw.Length > 80 ? raw.Substring(0, 77) + "..." : raw;
							it.Status = "ЧИТ-ТОКЕН В ИСТОРИИ БУФЕРА";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ОБНАРУЖЕН ТОКЕН/КЛЮЧ ЧИТА В HISTORY.DB (5.01)";
							it.TargetRule = "5.01";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.01:\nВ базе данных истории буфера обмена Windows (history.db) обнаружена запись чита!\nСигнатура: {0}\nПолный фрагмент: {1}\nИгрок копировал лицензионный ключ, токен авторизации или URL чита.", sig, raw);
							items.Add(it);
						}
						break;
					}
				}

				// Проверка на команды клинеров (5.08)
				foreach (string csig in cleanerSignatures)
				{
					if (lower.Contains(csig))
					{
						if (seenHits.Add(csig + ":" + raw))
						{
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Буфер history.db";
							it.SourceArtifact = "%LocalAppData%\\...\\history.db";
							it.TargetOrToken = raw.Length > 80 ? raw.Substring(0, 77) + "..." : raw;
							it.Status = "КОМАНДА КЛИНЕРА В БУФЕРЕ";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 КОМАНДА САМОЛИКВИДАЦИИ В HISTORY.DB (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nВ истории буфера обмена обнаружена деструктивная команда зачистки следов: '{0}'.\nИгрок копировал или запускал команду уничтожения журналов/теневых копий.", raw);
							items.Add(it);
						}
						break;
					}
				}
			}
		}

		private void ScanActiveClipboard(List<EvasionEvidenceItem> items, ref int idx)
		{
			string activeText = "";
			Thread staThread = new Thread(delegate()
			{
				try
				{
					if (Clipboard.ContainsText())
					{
						activeText = Clipboard.GetText();
					}
				}
				catch { }
			});
			staThread.SetApartmentState(ApartmentState.STA);
			staThread.Start();
			staThread.Join(1000);

			if (!string.IsNullOrEmpty(activeText))
			{
				string lower = activeText.ToLowerInvariant();
				string[] cheatSigs = new string[] { "vape.gg", "drip.gg", "slinky.gg", "expensive", "celestial", "nursultan", "akrien", "doomsday" };
				bool isCheat = false;
				string matched = "";
				foreach (string cs in cheatSigs)
				{
					if (lower.Contains(cs))
					{
						isCheat = true;
						matched = cs;
						break;
					}
				}

				if (isCheat)
				{
					EvasionEvidenceItem it = new EvasionEvidenceItem();
					it.Index = idx++;
					it.Category = "Буфер history.db";
					it.SourceArtifact = "Активный буфер обмена Windows";
					it.TargetOrToken = activeText.Length > 80 ? activeText.Substring(0, 77) + "..." : activeText;
					it.Status = "АКТИВНЫЙ ЧИТ-ТОКЕН";
					it.ThreatLevel = "CRITICAL";
					it.Verdict = "🚨 В АКТИВНОМ БУФЕРЕ ОБМЕНА ОБНАРУЖЕН ТОКЕН ЧИТА (5.01)";
					it.TargetRule = "5.01";
					it.IsViolation = true;
					it.IsWhitelisted = false;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.AccentRed;
					it.DetailDescription = string.Format("В текущем буфере обмена (Ctrl+C) прямо сейчас находится токен/ссылка на чит ({0})!\nСодержимое: {1}", matched, activeText);
					items.Add(it);
				}
				else
				{
					EvasionEvidenceItem it = new EvasionEvidenceItem();
					it.Index = idx++;
					it.Category = "Буфер history.db";
					it.SourceArtifact = "Активный буфер обмена Windows";
					it.TargetOrToken = activeText.Length > 50 ? activeText.Substring(0, 47) + "..." : activeText;
					it.Status = "ЧИСТО";
					it.ThreatLevel = "SAFE";
					it.Verdict = "ℹ️ Текущий буфер обмена не содержит читов";
					it.TargetRule = "РАЗРЕШЕНО";
					it.IsViolation = false;
					it.IsWhitelisted = true;
					it.Timestamp = DateTime.Now;
					it.RowColor = Ui.TextSecondary;
					it.DetailDescription = "Текущий текст в буфере обмена проверен и не содержит признаков запрещённого ПО.";
					items.Add(it);
				}
			}
		}

		private void ScanSelfDestructAndWiping(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				// 1. Проверка очистки журнала USN через Event Log или статус
				ScanUsnJournalWipe(items, ref idx);

				// 2. Проверка очистки журналов событий wevtutil cl (Event ID 1102 / 104)
				ScanEventLogClearing(items, ref idx);

				// 3. Проверка удаления теневых копий vssadmin delete shadows
				ScanVolumeShadowCopiesWipe(items, ref idx);

				// 4. Проверка консольной истории PowerShell (ConsoleHost_history.txt) и RunMRU
				ScanConsoleAndRunMruWiping(items, ref idx);

				// 5. Проверка скриптов клинеров (.bat / .cmd / .ps1) на диске
				ScanCleanerBatchScripts(items, ref idx);
			}
			catch (Exception ex)
			{
				EvasionEvidenceItem err = new EvasionEvidenceItem();
				err.Index = idx++;
				err.Category = "Самоликвидация / Зачистка";
				err.SourceArtifact = "Wiping Engine";
				err.TargetOrToken = ex.Message;
				err.Status = "ОШИБКА";
				err.ThreatLevel = "INFO";
				err.Verdict = "Ошибка проверки зачистки: " + ex.Message;
				err.TargetRule = "ИНФО";
				err.Timestamp = DateTime.Now;
				err.RowColor = Ui.TextMuted;
				items.Add(err);
			}
		}

		private void ScanUsnJournalWipe(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				// Проверяем System Event Log на Event ID 1205 / fsutil
				string queryStr = "*[System[Provider[@Name='Ntfs' or @Name='Microsoft-Windows-Ntfs'] and (EventID=1205 or EventID=1000)]]";
				EventLogQuery q = new EventLogQuery("System", PathType.LogName, queryStr);
				using (EventLogReader reader = new EventLogReader(q))
				{
					EventRecord rec;
					while ((rec = reader.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Самоликвидация / Зачистка";
							it.SourceArtifact = "System / Event ID " + rec.Id + " (NTFS)";
							it.TargetOrToken = "Журнал изменений USN (C:)";
							it.Status = "УДАЛЕНИЕ ЖУРНАЛА USN";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 УДАЛЕНИЕ ЖУРНАЛА USN FSUTIL (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = dt;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nЗафиксировано удаление журнала изменений USN (fsutil usn deletejournal) в {0}!\nУдаление журнала USN уничтожает доказательства создания, запуска и удаления читов на диске.", dt.ToString("dd.MM.yyyy HH:mm:ss"));
							items.Add(it);
						}
					}
				}
			}
			catch { }
		}

		private void ScanEventLogClearing(List<EvasionEvidenceItem> items, ref int idx)
		{
			// Event ID 1102 в Security (The audit log was cleared)
			try
			{
				string queryStr = "*[System[(EventID=1102)]]";
				EventLogQuery q = new EventLogQuery("Security", PathType.LogName, queryStr);
				using (EventLogReader reader = new EventLogReader(q))
				{
					EventRecord rec;
					while ((rec = reader.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Самоликвидация / Зачистка";
							it.SourceArtifact = "Security (Event ID 1102)";
							it.TargetOrToken = "wevtutil cl Security";
							it.Status = "ОЧИСТКА ЖУРНАЛА СОБЫТИЙ";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ПРИНУДИТЕЛЬНАЯ ОЧИСТКА ЖУРНАЛА СОБЫТИЙ WEVTUTIL (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = dt;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nЖурнал безопасности Windows (Security) был принудительно очищен командой 'wevtutil cl' в {0}.\nОчистка системных журналов является преднамеренным сокрытием доказательств.", dt.ToString("dd.MM.yyyy HH:mm:ss"));
							items.Add(it);
						}
					}
				}
			}
			catch { }

			// Event ID 104 в System (The log file was cleared)
			try
			{
				string queryStr = "*[System[(EventID=104)]]";
				EventLogQuery q = new EventLogQuery("System", PathType.LogName, queryStr);
				using (EventLogReader reader = new EventLogReader(q))
				{
					EventRecord rec;
					while ((rec = reader.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Самоликвидация / Зачистка";
							it.SourceArtifact = "System (Event ID 104)";
							it.TargetOrToken = "wevtutil cl System";
							it.Status = "ОЧИСТКА СИСТЕМНОГО ЖУРНАЛА";
							it.ThreatLevel = "CRITICAL";
							it.Verdict = "🚨 ПРИНУДИТЕЛЬНАЯ ОЧИСТКА ЖУРНАЛА SYSTEM WEVTUTIL (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = true;
							it.IsWhitelisted = false;
							it.Timestamp = dt;
							it.RowColor = Ui.AccentRed;
							it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nСистемный журнал Windows (System) был принудительно очищен командой 'wevtutil cl' в {0}.", dt.ToString("dd.MM.yyyy HH:mm:ss"));
							items.Add(it);
						}
					}
				}
			}
			catch { }

			// Проверка размеров физических файлов логов в winevt\Logs
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string evtxPath = Path.Combine(winDir, "System32", "winevt", "Logs", "Security.evtx");
				if (File.Exists(evtxPath))
				{
					FileInfo fi = new FileInfo(evtxPath);
					long uptimeMinutes = Environment.TickCount / (1000 * 60);
					if (fi.Length < 68 * 1024 && uptimeMinutes > 60)
					{
						EvasionEvidenceItem it = new EvasionEvidenceItem();
						it.Index = idx++;
						it.Category = "Самоликвидация / Зачистка";
						it.SourceArtifact = evtxPath;
						it.TargetOrToken = "Security.evtx (" + (fi.Length / 1024) + " КБ)";
						it.Status = "АНОМАЛЬНЫЙ РАЗМЕР ЛОГА";
						it.ThreatLevel = "CRITICAL";
						it.Verdict = "🚨 АНОМАЛЬНО ПУСТОЙ ЖУРНАЛ SECURITY.EVTX (5.08)";
						it.TargetRule = "5.08";
						it.IsViolation = true;
						it.IsWhitelisted = false;
						it.Timestamp = fi.LastWriteTime;
						it.RowColor = Ui.AccentRed;
						it.DetailDescription = string.Format("Файл журнала Security.evtx имеет аномально малый размер ({0} байт) при времени работы ПК {1} мин.\nЭто признак недавнего выполнения 'wevtutil cl Security'.", fi.Length, uptimeMinutes);
						items.Add(it);
					}
				}
			}
			catch { }
		}

		private void ScanVolumeShadowCopiesWipe(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				// Проверяем отключение службы теневого копирования томов (VSS)
				using (RegistryKey vssKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\VSS"))
				{
					if (vssKey != null)
					{
						object val = vssKey.GetValue("Start");
						if (val is int && (int)val == 4)
						{
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Самоликвидация / Зачистка";
							it.SourceArtifact = @"Services\VSS";
							it.TargetOrToken = "Служба Volume Shadow Copy (VSS) ОТКЛЮЧЕНА (Start=4)";
							it.Status = "СЛУЖБА ОТКЛЮЧЕНА";
							it.ThreatLevel = "WARNING";
							it.Verdict = "⚠️ Служба теневого копирования VSS отключена (5.08)";
							it.TargetRule = "5.08";
							it.IsViolation = false;
							it.IsWhitelisted = false;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.AccentOrange;
							it.DetailDescription = "Служба VSS принудительно отключена в реестре. Это препятствует созданию точек восстановления и теневых копий для восстановления удалённых читов.";
							items.Add(it);
						}
					}
				}

				// Проверка наличия теневых копий через WMI Win32_ShadowCopy
				try
				{
					using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ShadowCopy"))
					{
						int shadowCount = searcher.Get().Count;
						if (shadowCount > 0)
						{
							EvasionEvidenceItem it = new EvasionEvidenceItem();
							it.Index = idx++;
							it.Category = "Самоликвидация / Зачистка";
							it.SourceArtifact = "WMI Win32_ShadowCopy";
							it.TargetOrToken = shadowCount + " теневых копий доступно";
							it.Status = "ТЕНЕВЫЕ КОПИИ ЦЕЛЫ";
							it.ThreatLevel = "SAFE";
							it.Verdict = "ℹ️ Теневые копии томов VSS не уничтожены";
							it.TargetRule = "РАЗРЕШЕНО";
							it.IsViolation = false;
							it.IsWhitelisted = true;
							it.Timestamp = DateTime.Now;
							it.RowColor = Ui.TextSecondary;
							it.DetailDescription = string.Format("В системе обнаружено {0} теневых копий. Команда 'vssadmin delete shadows' не выполнялась.", shadowCount);
							items.Add(it);
						}
					}
				}
				catch { }
			}
			catch { }
		}

		private void ScanConsoleAndRunMruWiping(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				// PowerShell ConsoleHost_history.txt
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string psHistory = Path.Combine(appData, "Microsoft", "Windows", "PowerShell", "PSReadLine", "ConsoleHost_history.txt");
				if (File.Exists(psHistory))
				{
					string[] lines = File.ReadAllLines(psHistory);
					string[] wipeKeywords = new string[] { "fsutil usn", "wevtutil cl", "vssadmin delete", "clear-eventlog", "sdelete", "del /f /q c:\\windows\\prefetch", "rmdir /s /q" };

					foreach (string line in lines)
					{
						string lower = line.ToLowerInvariant();
						foreach (string kw in wipeKeywords)
						{
							if (lower.Contains(kw))
							{
								EvasionEvidenceItem it = new EvasionEvidenceItem();
								it.Index = idx++;
								it.Category = "Самоликвидация / Зачистка";
								it.SourceArtifact = "PowerShell ConsoleHost_history.txt";
								it.TargetOrToken = line.Length > 80 ? line.Substring(0, 77) + "..." : line;
								it.Status = "КОМАНДА КЛИНЕРА В ИСТОРИИ КОНСОЛИ";
								it.ThreatLevel = "CRITICAL";
								it.Verdict = "🚨 КОМАНДА ЗАЧИСТКИ В ИСТОРИИ POWERSHELL (5.08)";
								it.TargetRule = "5.08";
								it.IsViolation = true;
								it.IsWhitelisted = false;
								it.Timestamp = DateTime.Now;
								it.RowColor = Ui.AccentRed;
								it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nВ истории команд PowerShell PSReadLine зафиксирован запуск деструктивной команды зачистки:\n{0}", line);
								items.Add(it);
								break;
							}
						}
					}
				}

				// RunMRU в реестре
				using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"))
				{
					if (runKey != null)
					{
						string[] valNames = runKey.GetValueNames();
						foreach (string vn in valNames)
						{
							if (vn.Equals("MRUList", StringComparison.OrdinalIgnoreCase)) continue;
							string cmd = runKey.GetValue(vn) as string;
							if (!string.IsNullOrEmpty(cmd))
							{
								string lower = cmd.ToLowerInvariant();
								if (lower.Contains("fsutil") || lower.Contains("wevtutil") || lower.Contains("vssadmin") || lower.Contains("sdelete"))
								{
									EvasionEvidenceItem it = new EvasionEvidenceItem();
									it.Index = idx++;
									it.Category = "Самоликвидация / Зачистка";
									it.SourceArtifact = @"RunMRU\" + vn;
									it.TargetOrToken = cmd;
									it.Status = "КОМАНДА В ДИАЛОГЕ 'ВЫПОЛНИТЬ'";
									it.ThreatLevel = "CRITICAL";
									it.Verdict = "🚨 ЗАПУСК КЛИНЕРА ИЗ ДИАЛОГА 'ВЫПОЛНИТЬ' (5.08)";
									it.TargetRule = "5.08";
									it.IsViolation = true;
									it.IsWhitelisted = false;
									it.Timestamp = DateTime.Now;
									it.RowColor = Ui.AccentRed;
									it.DetailDescription = string.Format("В реестре Windows RunMRU (история диалога 'Выполнить') зафиксирована команда клинера:\n{0}", cmd);
									items.Add(it);
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanCleanerBatchScripts(List<EvasionEvidenceItem> items, ref int idx)
		{
			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string[] scanDirs = new string[]
				{
					Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
					Path.Combine(userProfile, "Downloads"),
					Path.GetTempPath(),
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
				};

				string[] dangerousCommands = new string[]
				{
					"fsutil usn deletejournal", "wevtutil cl", "vssadmin delete shadows", "clear-eventlog", "del /f /q %temp%", "del /f /q c:\\windows\\prefetch"
				};

				foreach (string dir in scanDirs)
				{
					if (!Directory.Exists(dir)) continue;
					string[] files;
					try
					{
						files = Directory.GetFiles(dir, "*.bat", SearchOption.TopDirectoryOnly);
					}
					catch { continue; }

					foreach (string file in files)
					{
						try
						{
							FileInfo fi = new FileInfo(file);
							if (fi.Length > 200 * 1024) continue; // Пропускаем слишком большие скрипты

							string content = File.ReadAllText(file).ToLowerInvariant();
							foreach (string dc in dangerousCommands)
							{
								if (content.Contains(dc))
								{
									EvasionEvidenceItem it = new EvasionEvidenceItem();
									it.Index = idx++;
									it.Category = "Самоликвидация / Зачистка";
									it.SourceArtifact = file;
									it.TargetOrToken = fi.Name + " (" + dc + ")";
									it.Status = "СКРИПТ САМОЛИКВИДАТОРА";
									it.ThreatLevel = "CRITICAL";
									it.Verdict = "🚨 СКРИПТ САМОЛИКВИДАЦИИ / ОЧИСТКИ СЛЕДОВ (5.08)";
									it.TargetRule = "5.08";
									it.IsViolation = true;
									it.IsWhitelisted = false;
									it.Timestamp = fi.LastWriteTime;
									it.RowColor = Ui.AccentRed;
									it.DetailDescription = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ 5.08:\nНа диске обнаружен batch-скрипт самоликвидации: '{0}'.\nСодержит деструктивную команду зачистки: '{1}'.\nДата изменения: {2}.", file, dc, fi.LastWriteTime);
									items.Add(it);
									break;
								}
							}
						}
						catch { }
					}
				}
			}
			catch { }
		}

		#endregion

		#region Filtering & ListView

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (EvasionEvidenceItem it in allItems)
			{
				// Фильтр по категории
				if (currentFilter == EvasionFilterMode.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == EvasionFilterMode.MountPointsAndDisks && !it.Category.Contains("MountPoints2") && !it.Category.Contains("Диски")) continue;
				if (currentFilter == EvasionFilterMode.ClipboardHistory && !it.Category.Contains("Буфер")) continue;
				if (currentFilter == EvasionFilterMode.SelfDestructAndWiping && !it.Category.Contains("Самоликвидация") && !it.Category.Contains("Зачистка")) continue;
				if (currentFilter == EvasionFilterMode.WhitelistedAndSafe && !it.IsWhitelisted) continue;

				// Поиск
				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.Category.ToLowerInvariant().Contains(search) ||
								 it.SourceArtifact.ToLowerInvariant().Contains(search) ||
								 it.TargetOrToken.ToLowerInvariant().Contains(search) ||
								 it.Status.ToLowerInvariant().Contains(search) ||
								 it.Verdict.ToLowerInvariant().Contains(search) ||
								 it.TargetRule.ToLowerInvariant().Contains(search) ||
								 it.DetailDescription.ToLowerInvariant().Contains(search);
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.Category);
				lvi.SubItems.Add(it.SourceArtifact);
				lvi.SubItems.Add(it.TargetOrToken);
				lvi.SubItems.Add(it.Status);
				lvi.SubItems.Add(it.ThreatLevel);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Timestamp.ToString("dd.MM.yyyy HH:mm"));
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
				txtAnalysis.Text = "Нет записей, соответствующих выбранному фильтру.";
				txtTimeline.Text = "";
				txtBanVerdict.Text = "";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			EvasionEvidenceItem item = listView.SelectedItems[0].Tag as EvasionEvidenceItem;
			if (item == null) return;

			// Вкладка 1: Анализ
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СУДЕБНЫЙ АНАЛИЗ СОКРЫТИЯ СОФТА И АНТИ-ФОРЕНЗИКИ ===");
			sb.AppendLine("Категория проверки: " + item.Category);
			sb.AppendLine("Источник артефакта: " + item.SourceArtifact);
			sb.AppendLine("Объект / Токен / Команда: " + item.TargetOrToken);
			sb.AppendLine("Статус детектора: " + item.Status);
			sb.AppendLine("Уровень угрозы: " + item.ThreatLevel);
			sb.AppendLine("Правило регламента: " + item.TargetRule);
			sb.AppendLine("Штамп времени: " + item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Вердикт детектора: " + item.Verdict);
			sb.AppendLine();
			sb.AppendLine("--- ПОДРОБНОЕ ОПИСАНИЕ УЛИКИ ---");
			sb.AppendLine(item.DetailDescription);
			txtAnalysis.Text = sb.ToString();

			// Вкладка 2: Хронология
			StringBuilder tb = new StringBuilder();
			tb.AppendLine("=== ХРОНОЛОГИЧЕСКАЯ ЦЕПОЧКА УЛИК И СОБЫТИЙ ===");
			List<EvasionEvidenceItem> sorted = new List<EvasionEvidenceItem>(allItems);
			sorted.Sort(delegate(EvasionEvidenceItem a, EvasionEvidenceItem b) { return a.Timestamp.CompareTo(b.Timestamp); });
			foreach (EvasionEvidenceItem ev in sorted)
			{
				string flag = ev.ThreatLevel == "CRITICAL" ? "[🚨 НАКАЗАНИЕ]" : (ev.ThreatLevel == "WARNING" ? "[⚠️ ВНИМАНИЕ]" : "[ℹ️ ИНФО]");
				tb.AppendLine(string.Format("{0:dd.MM.yyyy HH:mm:ss} | {1} {2} -> {3} ({4})", ev.Timestamp, flag, ev.Category, ev.TargetOrToken, ev.Verdict));
			}
			txtTimeline.Text = tb.ToString();

			// Вкладка 3: Команда бана
			StringBuilder bb = new StringBuilder();
			bb.AppendLine("=== РЕГЛАМЕНТ И ВЕРДИКТ НАКАЗАНИЯ ===");
			if (item.IsViolation)
			{
				string reason = item.TargetRule == "5.01"
					? string.Format("5.01 - Чит-клиенты (Буфер обмена / history.db: {0})", item.TargetOrToken)
					: string.Format("5.08 - Сокрытие софта ({0})", item.Verdict.Replace("🚨 ", ""));
				string banCmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);

				bb.AppendLine("СУДЕБНЫЙ ВЕРДИКТ: ПОДТВЕРЖДЕНО КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА!");
				bb.AppendLine("Нарушенный пункт: " + item.TargetRule);
				bb.AppendLine("Срок блокировки: 30 дней");
				bb.AppendLine();
				bb.AppendLine("ГОТОВАЯ КОМАНДА ДЛЯ МОДЕРАТОРА:");
				bb.AppendLine(banCmd);
				bb.AppendLine();
				bb.AppendLine("Нажмите кнопку '⚖️ Копировать команду бана' внизу формы для быстрой вставки.");
			}
			else
			{
				bb.AppendLine("Критических нарушений для выбранного артефакта не зафиксировано.");
				bb.AppendLine("Объект квалифицирован как: " + item.ThreatLevel + " (" + item.Status + ")");
			}
			txtBanVerdict.Text = bb.ToString();
		}

		#endregion

		#region Action Handlers

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count > 0)
			{
				EvasionEvidenceItem item = listView.SelectedItems[0].Tag as EvasionEvidenceItem;
				if (item != null && item.IsViolation)
				{
					string reason = item.TargetRule == "5.01"
						? string.Format("5.01 - Чит-клиенты (history.db: {0})", item.TargetOrToken)
						: string.Format("5.08 - Сокрытие читов ({0})", item.Verdict.Replace("🚨 ", ""));
					string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.08 / 5.01", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			// Поиск первого нарушения в списке
			foreach (EvasionEvidenceItem it in allItems)
			{
				if (it.IsViolation)
				{
					string reason = it.TargetRule == "5.01"
						? string.Format("5.01 - Чит-клиенты (history.db: {0})", it.TargetOrToken)
						: string.Format("5.08 - Сокрытие читов ({0})", it.Verdict.Replace("🚨 ", ""));
					string cmd = string.Format("/ban {0} 30d {1}", targetPlayerName, reason);
					Clipboard.SetText(cmd);
					MessageBox.Show("Команда бана скопирована в буфер:\n\n" + cmd, "Бан 5.08 / 5.01", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			string defCmd = string.Format("/ban {0} 30d 5.08 - Сокрытие читов и обход проверок", targetPlayerName);
			Clipboard.SetText(defCmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n\n" + defCmd, "Бан 5.08", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```diff");
			sb.AppendLine("- [MCCheatChecker] СУДЕБНЫЙ АУДИТ СОКРЫТИЯ СОФТА И ОБХОДА ПРОВЕРОК");
			sb.AppendLine("+ Игрок: " + targetPlayerName);
			sb.AppendLine("+ Время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("+ Нарушений: " + lblKpiViolations.Text);
			sb.AppendLine("+ MountPoints2 / Диски: " + lblKpiMountPoints.Text);
			sb.AppendLine("+ Буфер history.db: " + lblKpiClipboard.Text);
			sb.AppendLine("+ Зачистка / Самоликвидация: " + lblKpiWiping.Text);
			sb.AppendLine();
			sb.AppendLine("--- НАИБОЛЕЕ ОПАСНЫЕ НАХОДКИ ---");
			int count = 0;
			foreach (EvasionEvidenceItem it in allItems)
			{
				if (it.ThreatLevel == "CRITICAL" || it.ThreatLevel == "WARNING")
				{
					string pfx = it.ThreatLevel == "CRITICAL" ? "- [🚨 НАКАЗАНИЕ]" : "! [⚠️ ВНИМАНИЕ]";
					sb.AppendLine(string.Format("{0} {1} | {2} ({3})", pfx, it.Category, it.TargetOrToken, it.Verdict));
					count++;
					if (count >= 8) break;
				}
			}
			if (count == 0)
			{
				sb.AppendLine("+ Следов сокрытия и зачистки не обнаружено.");
			}
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Сводка для Discord успешно скопирована!", "Discord Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ExportHtmlReport()
		{
			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), "mccheatchecker_evasion_report_" + DateTime.Now.Ticks + ".html");
				StringBuilder html = new StringBuilder();
				html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Anti-Evasion & Stealth Bypass Report</title>");
				html.AppendLine("<style>body{background:#0e1117;color:#c9d1d9;font-family:Segoe UI,sans-serif;padding:24px;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #30363d;padding:8px 12px;font-size:12px;text-align:left;} th{background:#161b22;color:#58a6ff;} tr:nth-child(even){background:#12161f;} .crit{color:#f85149;font-weight:bold;} .warn{color:#d29922;} .safe{color:#3fb950;}</style></head><body>");
				html.AppendLine("<h2>🥷 Судебный отчёт по обходу проверок, сокрытию софта и самоликвидаторам</h2>");
				html.AppendLine("<p><b>Игрок:</b> " + targetPlayerName + " | <b>Дата проверки:</b> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");
				html.AppendLine("<p><b>Нарушений:</b> " + lblKpiViolations.Text + " | <b>MountPoints2:</b> " + lblKpiMountPoints.Text + " | <b>Буфер history.db:</b> " + lblKpiClipboard.Text + " | <b>Зачистка:</b> " + lblKpiWiping.Text + "</p>");
				html.AppendLine("<table><tr><th>№</th><th>Категория</th><th>Источник</th><th>Объект / Токен</th><th>Статус</th><th>Угроза</th><th>Вердикт</th><th>Регламент</th></tr>");

				foreach (EvasionEvidenceItem it in allItems)
				{
					string cls = it.ThreatLevel == "CRITICAL" ? "crit" : (it.ThreatLevel == "WARNING" ? "warn" : "safe");
					html.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td class='{5}'>{6}</td><td>{7}</td><td>{8}</td></tr>",
						it.Index, it.Category, it.SourceArtifact, it.TargetOrToken, it.Status, cls, it.ThreatLevel, it.Verdict, it.TargetRule));
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

		#endregion
	}
}
