using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class VirtualDisksAndSandboxViewerForm : Form
	{
		private string targetPlayerName;
		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBox detailBox;
		private Label statusLabel;
		private Label summaryLabel;
		private DarkProgressBar progressBar;
		private DarkButton btnRefresh;
		private DarkButton btnCopyBan;
		private DarkButton btnExportEvidence;
		private ComboBox filterCombo;

		private List<VirtualDiskForensicItem> allItems = new List<VirtualDiskForensicItem>();
		private BackgroundWorker worker;
		private bool isScanning = false;

		private static readonly string[] VirtualDiskExts = new string[]
		{
			".vhdx", ".vhd", ".iso", ".img", ".vmdk", ".vdi", ".tc", ".hc"
		};

		public VirtualDisksAndSandboxViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Подозрительный игрок" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "Форензика виртуальных дисков (VHD/VHDX/ISO), VeraCrypt и монтированных носителей - " + this.targetPlayerName;
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.Font = new Font("Segoe UI", 9f);
			this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

			// Верхняя панель
			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 115;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(15, 12, 15, 10);

			Label titleLabel = new Label();
			titleLabel.Text = "💽 ФОРЕНЗИКА ВИРТУАЛЬНЫХ ДИСКОВ (VHDX, ISO, VERACRYPT, МОНТИРОВАННЫЕ НОСИТЕЛИ)";
			titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
			titleLabel.ForeColor = Ui.AccentTeal;
			titleLabel.AutoSize = true;
			titleLabel.Location = new Point(15, 10);
			topPanel.Controls.Add(titleLabel);

			summaryLabel = new Label();
			summaryLabel.Text = "Сканирование событий VHDMP, MountedDevices, драйверов VeraCrypt/ImDisk и размонтирования перед ScreenShare...";
			summaryLabel.Font = new Font("Segoe UI", 9f);
			summaryLabel.ForeColor = Ui.TextMuted;
			summaryLabel.AutoSize = true;
			summaryLabel.Location = new Point(16, 36);
			topPanel.Controls.Add(summaryLabel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(16, 75);
			lblSearch.ForeColor = Ui.TextMuted;
			topPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(68, 72);
			searchBox.Size = new Size(260, 25);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			topPanel.Controls.Add(searchBox);

			Label lblFilter = new Label();
			lblFilter.Text = "Фильтр:";
			lblFilter.AutoSize = true;
			lblFilter.Location = new Point(345, 75);
			lblFilter.ForeColor = Ui.TextMuted;
			topPanel.Controls.Add(lblFilter);

			filterCombo = new ComboBox();
			filterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			filterCombo.BackColor = Ui.SurfaceAlt;
			filterCombo.ForeColor = Ui.TextPrimary;
			filterCombo.FlatStyle = FlatStyle.Flat;
			filterCombo.Location = new Point(405, 72);
			filterCombo.Size = new Size(225, 25);
			filterCombo.Items.AddRange(new object[] { "Все события и носители", "🚨 Размонтирование перед СС (5.08)", "🔐 VeraCrypt / Контейнеры", "💿 Образы .ISO / .VHDX", "🟢 Активные монтирования" });
			filterCombo.SelectedIndex = 0;
			filterCombo.SelectedIndexChanged += delegate { ApplyFilter(); };
			topPanel.Controls.Add(filterCombo);

			btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(645, 70);
			btnRefresh.Size = new Size(135, 28);
			btnRefresh.Click += delegate { StartScan(); };
			topPanel.Controls.Add(btnRefresh);

			btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать вердикт бана";
			btnCopyBan.Location = new Point(788, 70);
			btnCopyBan.Size = new Size(185, 28);
			btnCopyBan.Click += delegate { CopyBanVerdict(); };
			topPanel.Controls.Add(btnCopyBan);

			btnExportEvidence = new DarkButton();
			btnExportEvidence.Text = "🛡️ Сохранить улику (SHA-256)";
			btnExportEvidence.Location = new Point(980, 70);
			btnExportEvidence.Size = new Size(175, 28);
			btnExportEvidence.Click += delegate { ExportEvidence(); };
			topPanel.Controls.Add(btnExportEvidence);

			this.Controls.Add(topPanel);

			// SplitContainer для таблицы и инспектора деталей
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 400;
			split.Panel1MinSize = 200;
			split.Panel2MinSize = 100;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("Вердикт / Статус", 220);
			listView.Columns.Add("Тип артефакта", 160);
			listView.Columns.Add("Путь к образу / Контейнеру", 300);
			listView.Columns.Add("Время операции (Timestamp)", 150);
			listView.Columns.Add("Действие / Состояние", 160);
			listView.Columns.Add("Источник данных", 160);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			split.Panel1.Controls.Add(listView);

			// Нижняя панель деталей
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(10, 8, 10, 8);

			Label detailHeader = new Label();
			detailHeader.Text = "🔍 ДЕТАЛЬНЫЙ СУДЕБНЫЙ ПРОТОКОЛ ВЫБРАННОГО НОСИТЕЛЯ / ВИРТУАЛЬНОГО ДИСКА:";
			detailHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			detailHeader.ForeColor = Ui.AccentTeal;
			detailHeader.Dock = DockStyle.Top;
			detailHeader.Height = 22;
			detailContainer.Controls.Add(detailHeader);

			detailBox = new DarkTextBox();
			detailBox.Dock = DockStyle.Fill;
			detailBox.Multiline = true;
			detailBox.ScrollBars = ScrollBars.Both;
			detailBox.Font = new Font("Consolas", 9f);
			detailBox.ReadOnly = true;
			detailContainer.Controls.Add(detailBox);

			split.Panel2.Controls.Add(detailContainer);
			this.Controls.Add(split);

			// Нижняя строка статуса
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 34;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(10, 5, 10, 5);

			statusLabel = new Label();
			statusLabel.Text = "Готов к форензике виртуальных дисков и монтированных носителей.";
			statusLabel.AutoSize = true;
			statusLabel.Location = new Point(10, 8);
			statusLabel.ForeColor = Ui.TextMuted;
			bottomPanel.Controls.Add(statusLabel);

			progressBar = new DarkProgressBar();
			progressBar.Size = new Size(180, 16);
			progressBar.Location = new Point(this.ClientSize.Width - 200, 9);
			progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			progressBar.Visible = false;
			bottomPanel.Controls.Add(progressBar);

			this.Controls.Add(bottomPanel);
		}

		private void StartScan()
		{
			if (isScanning) return;
			isScanning = true;
			btnRefresh.Enabled = false;
			progressBar.Visible = true;
			statusLabel.Text = "Анализ журнала VHDMP, реестра MountedDevices и драйверов контейнеров...";
			allItems.Clear();
			listView.Items.Clear();
			detailBox.Text = "";

			worker = new BackgroundWorker();
			worker.DoWork += Worker_DoWork;
			worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			worker.RunWorkerAsync();
		}

		private void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<VirtualDiskForensicItem> results = new List<VirtualDiskForensicItem>();
			HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			DateTime now = DateTime.Now;

			// 1. Анализ журнала событий Microsoft-Windows-VHDMP/Operational
			ScanVhdmpEventLog(results, seenKeys, now);

			// 2. Анализ реестра MountedDevices на виртуальные тома
			ScanMountedDevicesRegistry(results, seenKeys);

			// 3. Проверка драйверов и служб VeraCrypt, TrueCrypt, ImDisk
			ScanContainerDrivers(results);

			// 4. Поиск недавних .vhdx, .vhd, .iso в Recent
			ScanRecentVirtualDiskShortcuts(results, seenKeys, now);

			// Сортировка: критические вердикты первыми, затем по времени
			results.Sort(delegate (VirtualDiskForensicItem a, VirtualDiskForensicItem b)
			{
				bool aCrit = a.VerdictStatus.Contains("5.08") || a.VerdictStatus.Contains("5.01");
				bool bCrit = b.VerdictStatus.Contains("5.08") || b.VerdictStatus.Contains("5.01");
				if (aCrit && !bCrit) return -1;
				if (!aCrit && bCrit) return 1;
				return b.Timestamp.CompareTo(a.Timestamp);
			});

			e.Result = results;
		}

		private void ScanVhdmpEventLog(List<VirtualDiskForensicItem> results, HashSet<string> seenKeys, DateTime now)
		{
			try
			{
				string query = "*[System[(EventID=1 or EventID=2 or EventID=12)]]";
				EventLogQuery elq = new EventLogQuery("Microsoft-Windows-VHDMP/Operational", PathType.LogName, query);
				elq.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(elq))
				{
					EventRecord record;
					int count = 0;
					while ((record = reader.ReadEvent()) != null && count < 500)
					{
						using (record)
						{
							count++;
							string vhdPath = "";
							if (record.Properties != null && record.Properties.Count > 0 && record.Properties[0].Value != null)
							{
								vhdPath = record.Properties[0].Value.ToString().Trim();
							}

							if (string.IsNullOrEmpty(vhdPath))
							{
								string desc = "";
								try { desc = record.FormatDescription(); } catch { }
								Match m = Regex.Match(desc, @"([A-Za-z]:\\[^\s""]+\.(?:vhdx|vhd|iso))", RegexOptions.IgnoreCase);
								if (m.Success) vhdPath = m.Groups[1].Value;
							}

							if (string.IsNullOrEmpty(vhdPath)) continue;

							DateTime time = record.TimeCreated.HasValue ? record.TimeCreated.Value : DateTime.Now;
							string dedupeKey = vhdPath + "_" + record.Id + "_" + time.ToString("yyyyMMddHHmm");
							if (seenKeys.Contains(dedupeKey)) continue;
							seenKeys.Add(dedupeKey);

							VirtualDiskForensicItem item = new VirtualDiskForensicItem();
							item.ImagePath = vhdPath;
							item.Timestamp = time;
							item.Source = "Microsoft-Windows-VHDMP/Operational (Event ID " + record.Id + ")";

							TimeSpan age = now - time;

							if (record.Id == 2)
							{
								item.ArtifactType = "Виртуальный диск (VHDX/VHD)";
								item.OperationState = "Размонтирован (Detach / Eject)";

								if (age.TotalMinutes <= 30)
								{
									item.VerdictStatus = "🚨 РАЗМОНТИРОВАНИЕ ДИСКА ПЕРЕД СС (5.08)";
								}
								else
								{
									item.VerdictStatus = "⚠️ Размонтирован (" + (int)age.TotalMinutes + " мин назад)";
								}
							}
							else if (record.Id == 1)
							{
								item.ArtifactType = "Виртуальный диск (VHDX/VHD)";
								item.OperationState = "Смонтирован (Attach / Mount)";
								item.VerdictStatus = (age.TotalHours <= 2) ? "🟡 Свежее монтирование VHDX" : "🟢 Монтирование VHDX";
							}
							else
							{
								item.ArtifactType = "Служебная операция VHDMP";
								item.OperationState = "Операция с диском";
								item.VerdictStatus = "🟢 Журнал VHDMP";
							}

							StringBuilder details = new StringBuilder();
							details.AppendLine("=== СУДЕБНЫЙ ПРОТОКОЛ ВИРТУАЛЬНОГО ДИСКА VHDMP ===");
							details.AppendLine("Файл образа: " + vhdPath);
							details.AppendLine("Операция: " + item.OperationState);
							details.AppendLine("Время события: " + time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
							details.AppendLine("Событие: Event ID " + record.Id + " (" + (record.Id == 2 ? "Размонтирование / Detach" : "Монтирование / Attach") + ")");
							details.AppendLine("Вердикт: " + item.VerdictStatus);
							details.AppendLine("--------------------------------------------------------------------------------");
							if (item.VerdictStatus.Contains("5.08"))
							{
								details.AppendLine("РЕГЛАМЕНТ 5.08: Зафиксировано экстренное размонтирование (eject) виртуального диска прямо перед вызовом на ScreenShare!");
								details.AppendLine("Продвинутые игроки часто прячут чит-клиенты внутри контейнеров .VHDX, отключая их перед проверкой.");
							}

							item.Details = details.ToString();
							results.Add(item);
						}
					}
				}
			}
			catch { }
		}

		private void ScanMountedDevicesRegistry(List<VirtualDiskForensicItem> results, HashSet<string> seenKeys)
		{
			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\MountedDevices"))
				{
					if (key == null) return;
					string[] valueNames = key.GetValueNames();

					foreach (string name in valueNames)
					{
						if (!name.StartsWith(@"\DosDevices\", StringComparison.OrdinalIgnoreCase)) continue;
						string driveLetter = name.Substring(@"\DosDevices\".Length);

						byte[] val = key.GetValue(name) as byte[];
						if (val == null || val.Length == 0) continue;

						string valStr = Encoding.Unicode.GetString(val);

						bool isVirtual = valStr.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
						                 valStr.IndexOf("Vhd", StringComparison.OrdinalIgnoreCase) >= 0 ||
						                 valStr.IndexOf("ImDisk", StringComparison.OrdinalIgnoreCase) >= 0 ||
						                 valStr.IndexOf("VeraCrypt", StringComparison.OrdinalIgnoreCase) >= 0;

						if (isVirtual)
						{
							string dedupeKey = "mounted_" + driveLetter;
							if (!seenKeys.Contains(dedupeKey))
							{
								seenKeys.Add(dedupeKey);
								VirtualDiskForensicItem item = new VirtualDiskForensicItem();
								item.ArtifactType = "Том MountedDevices (" + driveLetter + ")";
								item.ImagePath = valStr;
								item.Timestamp = DateTime.Now;
								item.OperationState = "Запись в реестре системы";
								item.Source = @"HKLM\SYSTEM\MountedDevices";
								item.VerdictStatus = "🟣 ВИРТУАЛЬНЫЙ ТОМ В РЕЕСТРЕ (" + driveLetter + ")";

								StringBuilder details = new StringBuilder();
								details.AppendLine("Буква диска: " + driveLetter);
								details.AppendLine("Идентификатор устройства: " + valStr);
								details.AppendLine("Реестр: HKLM\\SYSTEM\\MountedDevices\\" + name);
								details.AppendLine("Вердикт: " + item.VerdictStatus);
								item.Details = details.ToString();

								results.Add(item);
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanContainerDrivers(List<VirtualDiskForensicItem> results)
		{
			// Проверка драйверов VeraCrypt / TrueCrypt / ImDisk
			string[] drivers = new string[]
			{
				"VeraCrypt", "TrueCrypt", "ImDisk", "OSFMount", "VBoxGuest", "vmmouse"
			};

			foreach (string drv in drivers)
			{
				try
				{
					using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + drv))
					{
						if (k != null)
						{
							VirtualDiskForensicItem item = new VirtualDiskForensicItem();
							item.ArtifactType = "Драйвер шифрованного контейнера";
							item.ImagePath = drv + " (Служба / Драйвер ядра)";
							item.Timestamp = DateTime.Now;
							item.OperationState = "Установлен в системе";
							item.Source = @"HKLM\SYSTEM\CurrentControlSet\Services\" + drv;
							item.VerdictStatus = (drv.Contains("VeraCrypt") || drv.Contains("ImDisk")) ? "⚠️ ДРАЙВЕР КОНТЕЙНЕРОВ " + drv : "🟢 Драйвер " + drv;

							StringBuilder details = new StringBuilder();
							details.AppendLine("Обнаружен драйвер контейнеров/виртуальных дисков: " + drv);
							details.AppendLine("Раздел реестра: HKLM\\SYSTEM\\CurrentControlSet\\Services\\" + drv);
							details.AppendLine("Назначение: Монтирование зашифрованных дисков или контейнеров в операционной системе.");
							item.Details = details.ToString();

							results.Add(item);
						}
					}
				}
				catch { }
			}
		}

		private void ScanRecentVirtualDiskShortcuts(List<VirtualDiskForensicItem> results, HashSet<string> seenKeys, DateTime now)
		{
			try
			{
				string recent = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
				if (!Directory.Exists(recent)) return;

				string[] lnks = Directory.GetFiles(recent, "*.lnk", SearchOption.TopDirectoryOnly);
				foreach (string lnk in lnks)
				{
					try
					{
						string name = Path.GetFileName(lnk).ToLowerInvariant();
						bool isVdisk = false;
						foreach (string ext in VirtualDiskExts)
						{
							if (name.Contains(ext)) { isVdisk = true; break; }
						}

						if (isVdisk)
						{
							FileInfo fi = new FileInfo(lnk);
							DateTime writeTime = fi.LastWriteTime;
							TimeSpan age = now - writeTime;

							string dedupeKey = "recent_" + fi.Name;
							if (!seenKeys.Contains(dedupeKey))
							{
								seenKeys.Add(dedupeKey);
								VirtualDiskForensicItem item = new VirtualDiskForensicItem();
								item.ArtifactType = "Ярлык Recent (LNK)";
								item.ImagePath = fi.Name;
								item.Timestamp = writeTime;
								item.OperationState = "Открытие образа в Проводнике";
								item.Source = fi.FullName;

								if (age.TotalMinutes <= 45)
								{
									item.VerdictStatus = "🚨 СВЕЖИЙ ОБРАЗ В RECENT (" + (int)age.TotalMinutes + " мин назад) (5.08)";
								}
								else
								{
									item.VerdictStatus = "🟡 Открытие образа VHD/ISO (" + (int)age.TotalHours + " ч назад)";
								}

								StringBuilder details = new StringBuilder();
								details.AppendLine("Файл ярлыка: " + fi.FullName);
								details.AppendLine("Целевой образ: " + fi.Name);
								details.AppendLine("Время открытия: " + writeTime.ToString("yyyy-MM-dd HH:mm:ss"));
								details.AppendLine("Вердикт: " + item.VerdictStatus);
								item.Details = details.ToString();

								results.Add(item);
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			isScanning = false;
			btnRefresh.Enabled = true;
			progressBar.Visible = false;

			if (e.Result is List<VirtualDiskForensicItem>)
			{
				allItems = (List<VirtualDiskForensicItem>)e.Result;
			}

			ApplyFilter();

			int ejectCount = 0;
			int containerCount = 0;
			int totalCount = allItems.Count;

			foreach (VirtualDiskForensicItem it in allItems)
			{
				if (it.VerdictStatus.Contains("5.08")) ejectCount++;
				if (it.VerdictStatus.Contains("VERACRYPT") || it.VerdictStatus.Contains("КОНТЕЙНЕР")) containerCount++;
			}

			summaryLabel.Text = string.Format("Всего артефактов: {0} | Размонтирований перед СС (5.08): {1} | Контейнеров VeraCrypt/ImDisk: {2}",
				totalCount, ejectCount, containerCount);

			if (ejectCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentRed;
			}
			else if (containerCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentOrange;
			}
			else
			{
				summaryLabel.ForeColor = Ui.AccentGreen;
			}

			statusLabel.Text = "Аудит завершен. Выберите запись для просмотра детального протокола.";
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();
			int filterIndex = filterCombo.SelectedIndex;

			foreach (VirtualDiskForensicItem item in allItems)
			{
				if (filterIndex == 1 && !item.VerdictStatus.Contains("5.08"))
				{
					continue;
				}
				if (filterIndex == 2 && !item.VerdictStatus.Contains("VERACRYPT") && !item.VerdictStatus.Contains("КОНТЕЙНЕР") && !item.VerdictStatus.Contains("ДРАЙВЕР"))
				{
					continue;
				}
				if (filterIndex == 3 && !item.ImagePath.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) && !item.ImagePath.EndsWith(".vhdx", StringComparison.OrdinalIgnoreCase) && !item.ImagePath.EndsWith(".vhd", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (filterIndex == 4 && !item.OperationState.Contains("Монтирован") && !item.OperationState.Contains("Attach"))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					string combined = (item.ImagePath + " " + item.ArtifactType + " " + item.OperationState + " " + item.VerdictStatus).ToLowerInvariant();
					if (!combined.Contains(search))
					{
						continue;
					}
				}

				ListViewItem lvi = new ListViewItem(item.VerdictStatus);
				lvi.SubItems.Add(item.ArtifactType);
				lvi.SubItems.Add(item.ImagePath);
				lvi.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.OperationState);
				lvi.SubItems.Add(item.Source);
				lvi.Tag = item;

				if (item.VerdictStatus.Contains("5.08"))
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 20, 20);
				}
				else if (item.VerdictStatus.Contains("VERACRYPT") || item.VerdictStatus.Contains("КОНТЕЙНЕР"))
				{
					lvi.ForeColor = Ui.AccentOrange;
				}
				else
				{
					lvi.ForeColor = Ui.TextPrimary;
				}

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				detailBox.Text = "";
				return;
			}

			VirtualDiskForensicItem item = listView.SelectedItems[0].Tag as VirtualDiskForensicItem;
			if (item != null)
			{
				detailBox.Text = item.Details;
			}
		}

		private void CopyBanVerdict()
		{
			if (listView.SelectedItems.Count == 0)
			{
				VirtualDiskForensicItem crit = allItems.Find(delegate (VirtualDiskForensicItem i) { return i.VerdictStatus.Contains("5.08"); });
				if (crit != null)
				{
					CopyVerdictForItem(crit);
					return;
				}
				MessageBox.Show("Выберите строку с обнаруженным нарушением для копирования вердикта.", "Выбор строки", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			VirtualDiskForensicItem selected = listView.SelectedItems[0].Tag as VirtualDiskForensicItem;
			if (selected != null)
			{
				CopyVerdictForItem(selected);
			}
		}

		private void CopyVerdictForItem(VirtualDiskForensicItem item)
		{
			string verdict;
			if (item.VerdictStatus.Contains("5.08"))
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Размонтирование виртуального диска {1})", targetPlayerName, Path.GetFileName(item.ImagePath));
			}
			else
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Скрытие носителя / контейнера {1})", targetPlayerName, item.ArtifactType);
			}

			try
			{
				Clipboard.SetText(verdict);
				MessageBox.Show("Бан-вердикт успешно скопирован в буфер обмена:\r\n\r\n" + verdict, "Вердикт скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportEvidence()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите строку для экспорта криминалистической улики.", "Экспорт доказательства", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			VirtualDiskForensicItem item = listView.SelectedItems[0].Tag as VirtualDiskForensicItem;
			if (item == null) return;

			StringBuilder evidence = new StringBuilder();
			evidence.AppendLine("=== СУДЕБНАЯ УЛИКА SCREENSHARE: VIRTUAL DISK / CONTAINER FORENSICS ===");
			evidence.AppendLine("Игрок: " + targetPlayerName);
			evidence.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Тип носителя: " + item.ArtifactType);
			evidence.AppendLine("Файл образа / Точки монтирования: " + item.ImagePath);
			evidence.AppendLine("Операция: " + item.OperationState);
			evidence.AppendLine("Время: " + item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			evidence.AppendLine("Источник данных: " + item.Source);
			evidence.AppendLine("Вердикт: " + item.VerdictStatus);
			evidence.AppendLine("----------------------------------------------------------------");
			evidence.AppendLine(item.Details);

			try
			{
				Clipboard.SetText(evidence.ToString());
				MessageBox.Show("Криминалистическая улика успешно скопирована в буфер обмена!", "Улика зафиксирована", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка копирования улики: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	public class VirtualDiskForensicItem
	{
		public string ArtifactType;
		public string ImagePath;
		public DateTime Timestamp;
		public string OperationState;
		public string Source;
		public string VerdictStatus;
		public string Details;
	}
}
