using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Management;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class VolumeShadowCopiesViewerForm : Form
	{
		#region Data Models

		public enum VssItemCategory
		{
			All,
			ViolationsOnly,
			ShadowCopies,
			RestorePoints,
			EventLogRecords,
			SystemAnomalies
		}

		public class VssEvidenceItem
		{
			public int Index;
			public string ItemName;
			public string ItemType;
			public VssItemCategory Category;
			public string Verdict;
			public string TargetRule;
			public string Details;
			public DateTime Timestamp;
			public string VolumePathOrSource;
			public bool IsViolation;
			public Color RowColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private VssItemCategory currentFilter = VssItemCategory.All;
		private List<VssEvidenceItem> allItems = new List<VssEvidenceItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiShadows;
		private Label lblKpiViolations;
		private Label lblKpiPoints;
		private Label lblKpiEvents;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterShadows;
		private DarkButton btnFilterPoints;
		private DarkButton btnFilterEvents;

		private DarkButton btnCopyPath;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public VolumeShadowCopiesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "💾 Форензика теневых копий тома (VSS), точек восстановления и улик зачистки (5.08) — MCCheatChecker";
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
			lblHeaderTitle.Text = "💾 Форензика теневых копий тома (Volume Shadow Copies - VSS) и точек восстановления";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Аудит снимков файловой системы и сокрытия улик: " + targetPlayerName;
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
			lblKpiShadows = CreateKpiLabel("Снимков VSS: 0", Ui.AccentTeal, 180, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Улик зачистки (5.08): 0", Ui.AccentRed, 350, kpiPanel);
			lblKpiPoints = CreateKpiLabel("Точек восстановления: 0", Ui.AccentOrange, 570, kpiPanel);
			lblKpiEvents = CreateKpiLabel("Событий VSS: 0", Ui.AccentPurple, 810, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все объекты", VssItemCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Нарушения сокрытия (5.08)", VssItemCategory.ViolationsOnly, 126, filterPanel);
			btnFilterShadows = CreateFilterButton("💾 Снимки томов (VSS)", VssItemCategory.ShadowCopies, 340, filterPanel);
			btnFilterPoints = CreateFilterButton("🛡️ Точки восстановления", VssItemCategory.RestorePoints, 534, filterPanel);
			btnFilterEvents = CreateFilterButton("📜 События VSS в журнале", VssItemCategory.EventLogRecords, 738, filterPanel);

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
			listView.Columns.Add("Объект / Снимок", 220);
			listView.Columns.Add("Тип артефакта", 150);
			listView.Columns.Add("Вердикт", 220);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Время создания / события", 160);
			listView.Columns.Add("Путь тома / Источник", 330);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Детали снимка тома VSS, точка монтирования и журнал событий:";
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

			btnCopyPath = CreateActionButton("📋 Копировать путь снимка тома", 0, actionsPanel);
			btnCopyPath.Click += delegate { CopyVolumePath(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 32, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана (5.08)", 64, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			DarkButton btnCheckSystemRestore = CreateActionButton("🛡️ Проверить статус SystemRestore", 96, actionsPanel);
			btnCheckSystemRestore.Click += delegate { ShowSystemRestoreInfo(); };

			bottomContainer.Controls.Add(actionsPanel);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(12, 166);
			btnRescan.Size = new Size(160, 26);
			btnRescan.Click += delegate { StartScan(); };
			bottomContainer.Controls.Add(btnRescan);

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

		private DarkButton CreateFilterButton(string text, VssItemCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == VssItemCategory.ViolationsOnly ? 198 : (cat == VssItemCategory.ShadowCopies ? 188 : (cat == VssItemCategory.RestorePoints ? 198 : (cat == VssItemCategory.EventLogRecords ? 198 : 108))), 28);
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
			if (cat == VssItemCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterShadows, btnFilterPoints, btnFilterEvents };
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

			lblStatusPill.Text = "⏳ Сканирование теневых копий VSS и точек восстановления...";
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
			List<VssEvidenceItem> list = new List<VssEvidenceItem>();
			int idx = 1;

			// 1. Query WMI for Win32_ShadowCopy
			int shadowCount = 0;
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ShadowCopy"))
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						shadowCount++;
						string id = obj["ID"] != null ? obj["ID"].ToString() : "VSS-Snapshot";
						string volumeName = obj["DeviceObject"] != null ? obj["DeviceObject"].ToString() : "";
						string originVol = obj["VolumeName"] != null ? obj["VolumeName"].ToString() : "C:\\";

						DateTime dt = DateTime.Now;
						try
						{
							if (obj["InstallDate"] != null)
							{
								dt = ManagementDateTimeConverter.ToDateTime(obj["InstallDate"].ToString());
							}
						}
						catch { }

						VssEvidenceItem it = new VssEvidenceItem();
						it.Index = idx++;
						it.ItemName = "Теневой снимок " + id;
						it.ItemType = "Снимок тома (Shadow Copy)";
						it.Category = VssItemCategory.ShadowCopies;
						it.Verdict = "Доступный снимок тома VSS";
						it.TargetRule = "Резервная копия";
						it.Timestamp = dt;
						it.VolumePathOrSource = volumeName;
						it.RowColor = Ui.AccentTeal;

						StringBuilder sb = new StringBuilder();
						sb.AppendLine("=== СНИМОК ТОМА VOLUME SHADOW COPY (VSS) ===");
						sb.AppendLine("ID снимка: " + id);
						sb.AppendLine("Устройство снимка: " + volumeName);
						sb.AppendLine("Исходный том: " + originVol);
						sb.AppendLine("Время создания: " + dt.ToString("yyyy-MM-dd HH:mm:ss"));
						sb.AppendLine();
						sb.AppendLine("Подсказка модератору:");
						sb.AppendLine("Данный снимок содержит состояние файлов до их возможного удаления.");
						sb.AppendLine("Для проверки удаленных модов создайте символическую ссылку:");
						sb.AppendLine("mklink /d C:\\vss_mount " + volumeName + "\\");
						it.Details = sb.ToString();

						list.Add(it);
					}
				}
			}
			catch { }

			// 2. Query System Restore settings in Registry
			try
			{
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore"))
				{
					if (rk != null)
					{
						object disSR = rk.GetValue("DisableSR");
						object disCfg = rk.GetValue("DisableConfig");

						bool isDisabled = (disSR != null && Convert.ToInt32(disSR) == 1) || (disCfg != null && Convert.ToInt32(disCfg) == 1);

						if (isDisabled)
						{
							VssEvidenceItem it = new VssEvidenceItem();
							it.Index = idx++;
							it.ItemName = "Служба System Restore (Реестр)";
							it.ItemType = "Конфигурация системы";
							it.Category = VssItemCategory.RestorePoints;
							it.IsViolation = true;
							it.Verdict = "🚨 ОТКЛЮЧЕНИЕ ТОЧЕК ВОССТАНОВЛЕНИЯ (5.08)";
							it.TargetRule = "5.08 (Сокрытие улик)";
							it.Timestamp = DateTime.Now;
							it.VolumePathOrSource = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore";
							it.RowColor = Ui.AccentRed;
							it.Details = "В системном реестре обнаружен параметр DisableSR=1 или DisableConfig=1.\nТочки восстановления Windows принудительно отключены, что часто применяется для сокрытия улик запуска читов.";
							list.Add(it);
						}
					}
				}
			}
			catch { }

			// 3. Query Windows Event Log for VSS operations and deletions
			try
			{
				// System Log Event ID 7036 (VSS Service), Event ID 22 (Shadow copy deleted)
				string queryStr = "*[System[(EventID=22 or EventID=7036 or EventID=14)] and System/Provider[@Name='Microsoft-Windows-VolumeSnapshot-Driver' or @Name='VSS' or @Name='Service Control Manager']]";
				EventLogQuery query = new EventLogQuery("System", PathType.LogName, queryStr);

				using (EventLogReader reader = new EventLogReader(query))
				{
					EventRecord rec;
					int eventCount = 0;
					while ((rec = reader.ReadEvent()) != null && eventCount < 20)
					{
						using (rec)
						{
							eventCount++;
							int eventId = rec.Id;
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;

							if (eventId == 22) // Shadow copy was deleted!
							{
								VssEvidenceItem it = new VssEvidenceItem();
								it.Index = idx++;
								it.ItemName = "Microsoft-Windows-VolumeSnapshot-Driver (ID 22)";
								it.ItemType = "Удаление снимка тома";
								it.Category = VssItemCategory.ViolationsOnly;
								it.IsViolation = true;
								it.Verdict = "🚨 УДАЛЕНИЕ ТЕНЕВЫХ КОПИЙ VSS ПЕРЕД СС (5.08)";
								it.TargetRule = "5.08 (Сокрытие улик)";
								it.Timestamp = dt;
								it.VolumePathOrSource = "System / Event ID 22";
								it.RowColor = Ui.AccentRed;
								it.Details = string.Format("Зафиксирован факт принудительного удаления теневой копии тома (Volume Shadow Copy).\nВремя удаления: {0}.\nВыполнение команд 'vssadmin delete shadows' или 'wmic shadowcopy delete' является нарушением регламента 5.08.", dt);
								list.Add(it);
							}
							else if (eventId == 14 || (eventId == 7036 && rec.FormatDescription() != null && rec.FormatDescription().Contains("Volume Shadow Copy")))
							{
								VssEvidenceItem it = new VssEvidenceItem();
								it.Index = idx++;
								it.ItemName = "Служба VSS (Event ID " + eventId + ")";
								it.ItemType = "Событие службы VSS";
								it.Category = VssItemCategory.EventLogRecords;
								it.Verdict = "Активность службы теневого копирования";
								it.TargetRule = "Аудит VSS";
								it.Timestamp = dt;
								it.VolumePathOrSource = "System Log";
								it.RowColor = Ui.AccentPurple;
								it.Details = string.Format("Зафиксировано изменение состояния службы Volume Shadow Copy: {0}\nВремя: {1}.", rec.FormatDescription(), dt);
								list.Add(it);
							}
						}
					}
				}
			}
			catch { }

			// 4. Anomaly check: High uptime but zero shadow copies
			long uptimeHours = Environment.TickCount / (1000 * 60 * 60);
			if (shadowCount == 0 && uptimeHours > 48)
			{
				VssEvidenceItem it = new VssEvidenceItem();
				it.Index = idx++;
				it.ItemName = "Состояние теневых копий VSS";
				it.ItemType = "Аномалия системы";
				it.Category = VssItemCategory.SystemAnomalies;
				it.IsViolation = false;
				it.Verdict = "⚠️ Теневые копии отсутствуют при высоком аптайме";
				it.TargetRule = "Аудит сокрытия";
				it.Timestamp = DateTime.Now;
				it.VolumePathOrSource = "Windows VSS Engine";
				it.RowColor = Ui.AccentOrange;
				it.Details = string.Format("Аптайм системы составляет {0} ч., однако активных теневых копий тома не обнаружено.\nВозможна зачистка снимков тома перед проверкой или отключение точек восстановления.", uptimeHours);
				list.Add(it);
			}

			e.Result = list;
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования VSS: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<VssEvidenceItem> res = e.Result as List<VssEvidenceItem>;
			if (res != null)
			{
				allItems = res;

				int violations = 0;
				int shadows = 0;
				int points = 0;
				int events = 0;

				foreach (VssEvidenceItem it in allItems)
				{
					if (it.IsViolation) violations++;
					if (it.Category == VssItemCategory.ShadowCopies) shadows++;
					if (it.Category == VssItemCategory.RestorePoints) points++;
					if (it.Category == VssItemCategory.EventLogRecords) events++;
				}

				lblKpiTotal.Text = "Всего объектов: " + allItems.Count;
				lblKpiShadows.Text = "Снимков VSS: " + shadows;
				lblKpiViolations.Text = "Улик зачистки (5.08): " + violations;
				lblKpiPoints.Text = "Точек восстановления: " + points;
				lblKpiEvents.Text = "Событий VSS: " + events;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Обнаружено {0} улик зачистки теневых копий или отключения точек восстановления (5.08)!", violations);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ Аудит VSS завершен: признаков уничтожения теневых копий не обнаружено.";
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

			foreach (VssEvidenceItem it in allItems)
			{
				if (currentFilter == VssItemCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == VssItemCategory.ShadowCopies && it.Category != VssItemCategory.ShadowCopies) continue;
				if (currentFilter == VssItemCategory.RestorePoints && it.Category != VssItemCategory.RestorePoints) continue;
				if (currentFilter == VssItemCategory.EventLogRecords && it.Category != VssItemCategory.EventLogRecords) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.ItemName.ToLowerInvariant().Contains(search) ||
						it.ItemType.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						it.VolumePathOrSource.ToLowerInvariant().Contains(search) ||
						(it.Details != null && it.Details.ToLowerInvariant().Contains(search));

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ItemName);
				lvi.SubItems.Add(it.ItemType);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(it.VolumePathOrSource);

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
			VssEvidenceItem it = listView.SelectedItems[0].Tag as VssEvidenceItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ ТЕНЕВЫХ КОПИЙ И ТОЧЕК ВОССТАНОВЛЕНИЯ ===");
			sb.AppendLine("Объект: " + it.ItemName);
			sb.AppendLine("Тип артефакта: " + it.ItemType);
			sb.AppendLine("Вердикт проверки: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Время: " + it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss (МСК)"));
			sb.AppendLine("Путь / Источник: " + it.VolumePathOrSource);
			sb.AppendLine();
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions

		private void CopyVolumePath()
		{
			if (listView.SelectedItems.Count == 0) return;
			VssEvidenceItem it = listView.SelectedItems[0].Tag as VssEvidenceItem;
			if (it == null) return;

			Clipboard.SetText(it.VolumePathOrSource);
			MessageBox.Show("Путь снимка тома скопирован в буфер:\n" + it.VolumePathOrSource, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			VssEvidenceItem it = listView.SelectedItems[0].Tag as VssEvidenceItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ УЛИКИ VSS / ТОЧЕК ВОССТАНОВЛЕНИЯ ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Объект: " + it.ItemName);
			sb.AppendLine("Тип: " + it.ItemType);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт: " + it.TargetRule);
			sb.AppendLine("Источник: " + it.VolumePathOrSource);
			sb.AppendLine("Время: " + it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			VssEvidenceItem it = listView.SelectedItems[0].Tag as VssEvidenceItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Уничтожение теневых копий VSS: {1})", targetPlayerName, it.ItemName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ShowSystemRestoreInfo()
		{
			try
			{
				Process.Start("SystemPropertiesProtection.exe");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "SystemPropertiesProtection", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
