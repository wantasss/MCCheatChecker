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
	public class HwidAndHardwareSpoofingViewerForm : Form
	{
		#region Data Models

		public enum HwidFilterCategory
		{
			All,
			AnomaliesAndSpoofers,
			BiosAndMotherboard,
			NetworkMacs,
			DrivesAndVolumes,
			RegistryCrypto
		}

		public class HwidForensicItem
		{
			public int Index;
			public string ComponentCategory;
			public string ParameterName;
			public string HardwareValue;
			public HwidFilterCategory Category;
			public string Verdict;
			public string TargetRule;
			public string DataSource;
			public bool IsAnomaly;
			public bool IsSpooferDetected;
			public Color RowColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private HwidFilterCategory currentFilter = HwidFilterCategory.All;
		private List<HwidForensicItem> allItems = new List<HwidForensicItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiAnomalies;
		private Label lblKpiMacs;
		private Label lblKpiDrives;
		private Label lblKpiStatus;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterAnomalies;
		private DarkButton btnFilterBios;
		private DarkButton btnFilterMacs;
		private DarkButton btnFilterDrives;
		private DarkButton btnFilterRegistry;

		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public HwidAndHardwareSpoofingViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		#region UI Initialization

		private void InitializeComponent()
		{
			Text = "🔍 Инспектор аппаратных идентификаторов (HWID), детекции спуферов и MAC-адресов — MCCheatChecker";
			Size = new Size(1280, 780);
			MinimumSize = new Size(1040, 600);
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
			lblHeaderTitle.Text = "🔍 Инспектор аппаратных идентификаторов (HWID), детекции спуферов и подмены MAC-адресов";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			Label lblSubtitle = new Label();
			lblSubtitle.Text = "Аудит MachineGuid, SMBIOS UUID, серийных номеров дисков/BIOS, детекция Locally Administered MAC (TMAC), утилит VolumeID и спуферов обхода бана (5.02 / 5.08).";
			lblSubtitle.Font = Ui.FontBody(8.5f);
			lblSubtitle.ForeColor = Ui.TextSecondary;
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(17, 36);
			topPanel.Controls.Add(lblSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Инициализация сканирования оборудования...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(35, 45, 55);
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.AutoSize = true;
			lblStatusPill.Location = new Point(18, 62);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			kpiPanel.Size = new Size(680, 50);
			kpiPanel.Location = new Point(topPanel.Width - 696, 50);
			kpiPanel.BackColor = Color.Transparent;

			lblKpiTotal = CreateKpiLabel("Параметров: 0", Ui.TextPrimary, 0, kpiPanel);
			lblKpiAnomalies = CreateKpiLabel("Аномалий: 0", Ui.AccentRed, 136, kpiPanel);
			lblKpiMacs = CreateKpiLabel("MAC-адаптеров: 0", Ui.AccentBlue, 272, kpiPanel);
			lblKpiDrives = CreateKpiLabel("Дисков/Томов: 0", Ui.AccentTeal, 408, kpiPanel);
			lblKpiStatus = CreateKpiLabel("Статус: Проверка", Ui.AccentGreen, 544, kpiPanel);
			topPanel.Controls.Add(kpiPanel);

			// Filter bar
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.SurfaceAlt;
			filterBar.Padding = new Padding(12, 6, 12, 6);

			int bx = 12;
			btnFilterAll = CreateFilterButton("Все параметры", HwidFilterCategory.All, ref bx, filterBar);
			btnFilterAnomalies = CreateFilterButton("🚨 Спуферы и Аномалии", HwidFilterCategory.AnomaliesAndSpoofers, ref bx, filterBar);
			btnFilterBios = CreateFilterButton("💻 BIOS & UUID", HwidFilterCategory.BiosAndMotherboard, ref bx, filterBar);
			btnFilterMacs = CreateFilterButton("🌐 MAC-адреса", HwidFilterCategory.NetworkMacs, ref bx, filterBar);
			btnFilterDrives = CreateFilterButton("💾 Диски & VolumeID", HwidFilterCategory.DrivesAndVolumes, ref bx, filterBar);
			btnFilterRegistry = CreateFilterButton("🛡️ Реестр Crypto", HwidFilterCategory.RegistryCrypto, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Size = new Size(220, 26);
			searchBox.Location = new Point(filterBar.Width - 236, 9);
			searchBox.Text = "";
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblSearch.Text = "🔍 Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(filterBar.Width - 305, 13);
			filterBar.Controls.Add(lblSearch);

			// Split container
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 420;
			split.BackColor = Ui.Border;

			// ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.GridLines = true;

			listView.Columns.Add("#", 45, HorizontalAlignment.Center);
			listView.Columns.Add("Категория оборудования", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Параметр / Компонент", 200, HorizontalAlignment.Left);
			listView.Columns.Add("Значение идентификатора (HWID/MAC/Serial)", 300, HorizontalAlignment.Left);
			listView.Columns.Add("Статус подлинности / Вердикт", 220, HorizontalAlignment.Left);
			listView.Columns.Add("Пункт регламента", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Источник данных", 220, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

			ContextMenuStrip contextMenu = new ContextMenuStrip();
			contextMenu.BackColor = Ui.Surface;
			contextMenu.ForeColor = Ui.TextPrimary;
			contextMenu.Items.Add("📋 Скопировать значение идентификатора", null, delegate { CopySelectedValue(); });
			contextMenu.Items.Add("📑 Скопировать Discord-досье на аппаратную улику", null, delegate { CopyDiscordReport(); });
			contextMenu.Items.Add("🚨 Скопировать команду бана", null, delegate { CopyBanCommand(); });
			listView.ContextMenuStrip = contextMenu;

			split.Panel1.Controls.Add(listView);

			// Bottom Panel
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Fill;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12);

			Label lblDetailHeader = new Label();
			lblDetailHeader.Text = "📄 Детализированный криминалистический анализ аппаратного компонента:";
			lblDetailHeader.Font = Ui.FontBody(8.5f, true);
			lblDetailHeader.ForeColor = Ui.TextSecondary;
			lblDetailHeader.Dock = DockStyle.Top;
			lblDetailHeader.Height = 22;
			bottomPanel.Controls.Add(lblDetailHeader);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Dock = DockStyle.Fill;
			txtDetails.Font = Ui.FontMono(9f);
			txtDetails.ReadOnly = true;
			bottomPanel.Controls.Add(txtDetails);

			// Button Bar
			Panel btnBar = new Panel();
			btnBar.Dock = DockStyle.Bottom;
			btnBar.Height = 44;
			btnBar.BackColor = Ui.SurfaceAlt;
			btnBar.Padding = new Padding(12, 6, 12, 6);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "📋 Скопировать Discord-отчет";
			btnCopyDiscord.Size = new Size(210, 32);
			btnCopyDiscord.Location = new Point(12, 6);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };
			btnBar.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "🚨 Скопировать команду бана";
			btnCopyBanCmd.Size = new Size(210, 32);
			btnCopyBanCmd.Location = new Point(230, 6);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			btnBar.Controls.Add(btnCopyBanCmd);

			btnRescan = new DarkButton();
			btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescan.Text = "🔄 Пересканировать HWID";
			btnRescan.Size = new Size(180, 32);
			btnRescan.Location = new Point(btnBar.Width - 192, 6);
			btnRescan.Click += delegate { StartScan(); };
			btnBar.Controls.Add(btnRescan);

			bottomPanel.Controls.Add(btnBar);
			split.Panel2.Controls.Add(bottomPanel);

			Controls.Add(split);
			Controls.Add(filterBar);
			Controls.Add(topPanel);

			Ui.ApplyDarkScrollbars(this);
			Ui.ApplyDarkTitleBar(this);
		}

		private Label CreateKpiLabel(string text, Color foreColor, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.ForeColor = foreColor;
			lbl.Font = Ui.FontBody(8.5f, true);
			lbl.BackColor = Color.FromArgb(28, 32, 38);
			lbl.TextAlign = ContentAlignment.MiddleCenter;
			lbl.Size = new Size(128, 44);
			lbl.Location = new Point(x, 2);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, HwidFilterCategory cat, ref int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(160, 30);
			btn.Location = new Point(x, 7);
			x += 166;
			btn.Click += delegate
			{
				currentFilter = cat;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			return btn;
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScan()
		{
			lblStatusPill.Text = "⏳ Сканирование аппаратных профилей HWID, WMI и сетевых интерфейсов...";
			lblStatusPill.ForeColor = Ui.AccentTeal;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "";

			if (scanWorker != null && scanWorker.IsBusy) return;

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<HwidForensicItem> list = new List<HwidForensicItem>();
			int idx = 1;

			try
			{
				// 1. Реестр Cryptography (MachineGuid, ProductId, InstallDate)
				ScanRegistryCryptography(list, ref idx);

				// 2. SMBIOS & Материнская плата (WMI)
				ScanSmbiosAndMotherboard(list, ref idx);

				// 3. Сетевые адаптеры и детекция подмены MAC (LAA / TMAC)
				ScanNetworkMacAddresses(list, ref idx);

				// 4. Физические диски и серийные номера томов
				ScanPhysicalDisksAndVolumes(list, ref idx);

				// 5. Детекция активных процессов и драйверов спуферов
				ScanActiveSpooferProcessesAndDrivers(list, ref idx);

				// 6. Следы утилит смены HWID в Prefetch и Temp
				ScanSpooferTracesOnDisk(list, ref idx);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("HWID scan error: " + ex.Message);
			}

			e.Result = list;
		}

		private void ScanRegistryCryptography(List<HwidForensicItem> list, ref int idx)
		{
			try
			{
				// MachineGuid
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
				{
					if (key != null)
					{
						object val = key.GetValue("MachineGuid");
						string guid = val != null ? val.ToString() : "(Отсутствует)";
						bool isAnomaly = IsSuspiciousGuid(guid);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Реестр Cryptography",
							ParameterName = "MachineGuid",
							HardwareValue = guid,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.RegistryCrypto,
							Verdict = isAnomaly ? "🚨 ПОДОЗРИТЕЛЬНЫЙ MACHINEGUID (СПУФЕР)" : "Подлинный MachineGuid системы",
							TargetRule = isAnomaly ? "5.02 / 5.08 (Спуфинг)" : "Разрешено (5.01)",
							DataSource = @"HKLM\SOFTWARE\Microsoft\Cryptography",
							IsAnomaly = isAnomaly,
							IsSpooferDetected = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentRed : Ui.AccentGreen,
							Details = string.Format("Системный MachineGuid: {0}\nРасположение: HKLM\\SOFTWARE\\Microsoft\\Cryptography\\MachineGuid\nСтатус: {1}", guid, isAnomaly ? "Обнаружена маска спуфера" : "Корректный формат UUID")
						});
					}
				}

				// HwProfileGuid
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001"))
				{
					if (key != null)
					{
						object val = key.GetValue("HwProfileGuid");
						string guid = val != null ? val.ToString() : "(Отсутствует)";
						bool isAnomaly = IsSuspiciousGuid(guid);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Реестр IDConfigDB",
							ParameterName = "HwProfileGuid",
							HardwareValue = guid,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.RegistryCrypto,
							Verdict = isAnomaly ? "🚨 АНОМАЛЬНЫЙ HWPROFILEGUID" : "Подлинный аппаратный профиль",
							TargetRule = isAnomaly ? "5.02 (Спуфинг)" : "Разрешено (5.01)",
							DataSource = @"HKLM\SYSTEM\CurrentControlSet\Control\IDConfigDB\...",
							IsAnomaly = isAnomaly,
							IsSpooferDetected = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentRed : Ui.AccentGreen,
							Details = string.Format("HwProfileGuid: {0}\nСтатус: {1}", guid, isAnomaly ? "Аномальное значение" : "Штатный профиль")
						});
					}
				}

				// ProductId
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
				{
					if (key != null)
					{
						object val = key.GetValue("ProductId");
						string pid = val != null ? val.ToString() : "(Не определен)";

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Windows NT CurrentVersion",
							ParameterName = "ProductId",
							HardwareValue = pid,
							Category = HwidFilterCategory.RegistryCrypto,
							Verdict = "Идентификатор сборки ОС",
							TargetRule = "Легитимный параметр",
							DataSource = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
							IsAnomaly = false,
							RowColor = Ui.AccentGreen,
							Details = "ProductId: " + pid
						});
					}
				}
			}
			catch { }
		}

		private void ScanSmbiosAndMotherboard(List<HwidForensicItem> list, ref int idx)
		{
			// Win32_ComputerSystemProduct (SMBIOS UUID)
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UUID, IdentifyingNumber, Name, Vendor FROM Win32_ComputerSystemProduct"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string uuid = mo["UUID"] != null ? mo["UUID"].ToString() : "";
						string vendor = mo["Vendor"] != null ? mo["Vendor"].ToString() : "";
						string name = mo["Name"] != null ? mo["Name"].ToString() : "";

						bool isAnomaly = IsSuspiciousSmbiosString(uuid) || IsSuspiciousGuid(uuid);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "SMBIOS Продукт",
							ParameterName = "SMBIOS UUID",
							HardwareValue = string.IsNullOrEmpty(uuid) ? "(Пусто)" : uuid,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.BiosAndMotherboard,
							Verdict = isAnomaly ? "🚨 ФАЛЬСИФИКАЦИЯ SMBIOS UUID (СПУФЕР)" : "Подлинный аппаратный UUID материнской платы",
							TargetRule = isAnomaly ? "5.02 / 5.08 (Спуфинг)" : "Разрешено (5.01)",
							DataSource = "WMI Win32_ComputerSystemProduct",
							IsAnomaly = isAnomaly,
							IsSpooferDetected = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentRed : Ui.AccentGreen,
							Details = string.Format("SMBIOS UUID: {0}\nВендор: {1}\nМодель: {2}\nВердикт: {3}", uuid, vendor, name, isAnomaly ? "Обнаружены признаки спуфера (дефолтные/фиктивные байты)" : "Подлинный UUID платы")
						});
					}
				}
			}
			catch { }

			// Win32_BaseBoard (Motherboard Serial)
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber, Manufacturer, Product FROM Win32_BaseBoard"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string serial = mo["SerialNumber"] != null ? mo["SerialNumber"].ToString() : "";
						string manuf = mo["Manufacturer"] != null ? mo["Manufacturer"].ToString() : "";
						string prod = mo["Product"] != null ? mo["Product"].ToString() : "";

						bool isAnomaly = IsSuspiciousSmbiosString(serial);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Материнская плата",
							ParameterName = "BaseBoard SerialNumber",
							HardwareValue = string.IsNullOrEmpty(serial) ? "(Не определен)" : serial,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.BiosAndMotherboard,
							Verdict = isAnomaly ? "⚠️ ПОДОЗРИТЕЛЬНЫЙ СЕРИЙНЫЙ НОМЕР ПЛАТЫ" : "Серийный номер материнской платы",
							TargetRule = isAnomaly ? "5.02 (Обход бана)" : "Разрешено (5.01)",
							DataSource = "WMI Win32_BaseBoard",
							IsAnomaly = isAnomaly,
							IsSpooferDetected = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentOrange : Ui.AccentGreen,
							Details = string.Format("Плата: {0} {1}\nСерийный номер: {2}\nПризнак спуфера: {3}", manuf, prod, serial, isAnomaly ? "Да (фиктивная строка)" : "Нет")
						});
					}
				}
			}
			catch { }

			// Win32_BIOS (BIOS Serial)
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber, SMBIOSBIOSVersion, ReleaseDate, Manufacturer FROM Win32_BIOS"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string serial = mo["SerialNumber"] != null ? mo["SerialNumber"].ToString() : "";
						string ver = mo["SMBIOSBIOSVersion"] != null ? mo["SMBIOSBIOSVersion"].ToString() : "";
						string manuf = mo["Manufacturer"] != null ? mo["Manufacturer"].ToString() : "";

						bool isAnomaly = IsSuspiciousSmbiosString(serial);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "BIOS / Прошивка",
							ParameterName = "BIOS SerialNumber",
							HardwareValue = string.IsNullOrEmpty(serial) ? "(Не определен)" : serial,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.BiosAndMotherboard,
							Verdict = isAnomaly ? "⚠️ ПОДОЗРИТЕЛЬНЫЙ СЕРИЙНИК BIOS" : "Подлинный серийный номер BIOS",
							TargetRule = isAnomaly ? "5.02 (Обход бана)" : "Разрешено (5.01)",
							DataSource = "WMI Win32_BIOS",
							IsAnomaly = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentOrange : Ui.AccentGreen,
							Details = string.Format("Вендор BIOS: {0}\nВерсия: {1}\nСерийный номер: {2}", manuf, ver, serial)
						});
					}
				}
			}
			catch { }
		}

		private void ScanNetworkMacAddresses(List<HwidForensicItem> list, ref int idx)
		{
			try
			{
				NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface nic in adapters)
				{
					if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

					PhysicalAddress pa = nic.GetPhysicalAddress();
					byte[] bytes = pa.GetAddressBytes();
					if (bytes == null || bytes.Length == 0) continue;

					string mac = BitConverter.ToString(bytes);

					// Проверка Locally Administered Address (LAA):
					// Бит 1 первого байта указывает на локально заданный MAC (признак TMAC, SMAC или спуфера)
					bool isLocallyAdministered = (bytes[0] & 0x02) != 0;
					bool isVirtual = nic.Description.ToLowerInvariant().Contains("virtual") 
						|| nic.Description.ToLowerInvariant().Contains("wintun") 
						|| nic.Description.ToLowerInvariant().Contains("wireguard") 
						|| nic.Description.ToLowerInvariant().Contains("tap-") 
						|| nic.Description.ToLowerInvariant().Contains("npcap")
						|| nic.Description.ToLowerInvariant().Contains("hyper-v")
						|| nic.Description.ToLowerInvariant().Contains("vmware");

					bool isSpooferMac = isLocallyAdministered && !isVirtual;

					string verdict = "Подлинный заводской MAC-адрес (OUI)";
					string rule = "Разрешено (5.01)";
					Color color = Ui.AccentGreen;

					if (isSpooferMac)
					{
						verdict = "🚨 ПОДМЕНА MAC-АДРЕСА (Locally Administered / TMAC)";
						rule = "5.02 (Обход бана по MAC)";
						color = Ui.AccentRed;
					}
					else if (isLocallyAdministered)
					{
						verdict = "⚠️ Локально администрируемый MAC (Виртуальный адаптер)";
						rule = "Внимание (5.02)";
						color = Ui.AccentOrange;
					}

					list.Add(new HwidForensicItem
					{
						Index = idx++,
						ComponentCategory = "Сетевой адаптер",
						ParameterName = nic.Name + " (" + nic.NetworkInterfaceType + ")",
						HardwareValue = mac,
						Category = isSpooferMac ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.NetworkMacs,
						Verdict = verdict,
						TargetRule = rule,
						DataSource = nic.Description,
						IsAnomaly = isSpooferMac,
						IsSpooferDetected = isSpooferMac,
						RowColor = color,
						Details = string.Format("Имя интерфейса: {0}\nОписание: {1}\nMAC-адрес: {2}\nТип: {3}\nСтатус: {4}\nLocally Administered Bit (LAA): {5} ({6})", 
							nic.Name, nic.Description, mac, nic.NetworkInterfaceType, nic.OperationalStatus, isLocallyAdministered ? "УСТАНОВЛЕН (1)" : "Сброшен (0, Заводской OUI)", isSpooferMac ? "ЯВНЫЙ ПРИЗНАК TMAC / СПУФЕРА!" : "Штатно")
					});
				}

				// Проверка реестра на явные переопределения NetworkAddress
				using (RegistryKey netClass = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}"))
				{
					if (netClass != null)
					{
						string[] sub = netClass.GetSubKeyNames();
						foreach (string sk in sub)
						{
							if (sk.Length != 4) continue;
							using (RegistryKey devKey = netClass.OpenSubKey(sk))
							{
								if (devKey != null)
								{
									object netAddr = devKey.GetValue("NetworkAddress");
									if (netAddr != null && !string.IsNullOrEmpty(netAddr.ToString()))
									{
										string customMac = netAddr.ToString();
										string desc = devKey.GetValue("DriverDesc") != null ? devKey.GetValue("DriverDesc").ToString() : sk;

										list.Add(new HwidForensicItem
										{
											Index = idx++,
											ComponentCategory = "Реестр NetworkAddress",
											ParameterName = "Ручная подмена MAC (" + desc + ")",
											HardwareValue = customMac,
											Category = HwidFilterCategory.AnomaliesAndSpoofers,
											Verdict = "🚨 ПЕРЕОПРЕДЕЛЕНИЕ MAC В РЕЕСТРЕ (TMAC/SMAC)",
											TargetRule = "5.02 / 5.08 (Подмена MAC)",
											DataSource = @"HKLM\SYSTEM\CurrentControlSet\Control\Class\...\NetworkAddress",
											IsAnomaly = true,
											IsSpooferDetected = true,
											RowColor = Ui.AccentRed,
											Details = string.Format("В реестре обнаружено статическое переопределение аппаратного MAC-адреса для сетевой карты:\nДрайвер: {0}\nЗаданный MAC: {1}\nПараметр: NetworkAddress", desc, customMac)
										});
									}
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanPhysicalDisksAndVolumes(List<HwidForensicItem> list, ref int idx)
		{
			// Win32_DiskDrive
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Model, SerialNumber, InterfaceType, Size FROM Win32_DiskDrive"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string model = mo["Model"] != null ? mo["Model"].ToString() : "";
						string serial = mo["SerialNumber"] != null ? mo["SerialNumber"].ToString().Trim() : "";

						bool isAnomaly = IsSuspiciousDiskSerial(serial);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Физический накопитель",
							ParameterName = model,
							HardwareValue = string.IsNullOrEmpty(serial) ? "(Не определен)" : serial,
							Category = isAnomaly ? HwidFilterCategory.AnomaliesAndSpoofers : HwidFilterCategory.DrivesAndVolumes,
							Verdict = isAnomaly ? "🚨 ПОДОЗРИТЕЛЬНЫЙ СЕРИЙНИК ДИСКА (СПУФЕР)" : "Подлинный серийный номер накопителя",
							TargetRule = isAnomaly ? "5.02 (Спуфинг диска)" : "Разрешено (5.01)",
							DataSource = "WMI Win32_DiskDrive",
							IsAnomaly = isAnomaly,
							IsSpooferDetected = isAnomaly,
							RowColor = isAnomaly ? Ui.AccentRed : Ui.AccentGreen,
							Details = string.Format("Модель диска: {0}\nСерийный номер: {1}\nПризнак спуфера: {2}", model, serial, isAnomaly ? "Да (фиктивный шаблон спуфера)" : "Нет")
						});
					}
				}
			}
			catch { }

			// NTFS Volume Serial Numbers
			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo di in drives)
				{
					if (!di.IsReady || di.DriveType != DriveType.Fixed) continue;

					uint serialNum = 0;
					uint maxCompLen = 0;
					uint flags = 0;
					StringBuilder fsName = new StringBuilder(256);
					StringBuilder volName = new StringBuilder(256);

					if (GetVolumeInformation(di.Name, volName, (uint)volName.Capacity, out serialNum, out maxCompLen, out flags, fsName, (uint)fsName.Capacity))
					{
						string volSerialStr = string.Format("{0:X4}-{1:X4}", (serialNum >> 16) & 0xFFFF, serialNum & 0xFFFF);

						list.Add(new HwidForensicItem
						{
							Index = idx++,
							ComponentCategory = "Логический том",
							ParameterName = string.Format("Том {0} ({1})", di.Name, fsName.ToString()),
							HardwareValue = volSerialStr,
							Category = HwidFilterCategory.DrivesAndVolumes,
							Verdict = "Серийный номер тома NTFS (VolumeID)",
							TargetRule = "Легитимный том",
							DataSource = "kernel32!GetVolumeInformationW",
							IsAnomaly = false,
							RowColor = Ui.AccentGreen,
							Details = string.Format("Том: {0}\nМетка: {1}\nФайловая система: {2}\nСерийный номер: {3}", di.Name, volName, fsName, volSerialStr)
						});
					}
				}
			}
			catch { }
		}

		private void ScanActiveSpooferProcessesAndDrivers(List<HwidForensicItem> list, ref int idx)
		{
			string[] spooferNames = new string[]
			{
				"tmac", "smac", "volumeid", "hwidspoofer", "spoofer", "kdmapper", "macchanger",
				"anti-ban", "madspoofer", "monolithspoofer", "btbd", "efiguard", "raider", "titanspoofer",
				"diskid", "serialchanger", "hidhide", "hidremapper"
			};

			Process[] procs = Process.GetProcesses();
			foreach (Process p in procs)
			{
				try
				{
					string pName = p.ProcessName.ToLowerInvariant();
					foreach (string sp in spooferNames)
					{
						if (pName.Contains(sp))
						{
							list.Add(new HwidForensicItem
							{
								Index = idx++,
								ComponentCategory = "Активный процесс",
								ParameterName = p.ProcessName + ".exe (PID: " + p.Id + ")",
								HardwareValue = "Процесс активен в памяти",
								Category = HwidFilterCategory.AnomaliesAndSpoofers,
								Verdict = "🚨 АКТИВНЫЙ ПРОЦЕСС HWID СПУФЕРА (5.02 / 5.08)",
								TargetRule = "5.02 / 5.08 (Спуфер)",
								DataSource = "Process Enumeration",
								IsAnomaly = true,
								IsSpooferDetected = true,
								RowColor = Ui.AccentRed,
								Details = string.Format("В оперативной памяти обнаружен активный процесс утилиты подмены оборудования:\nИмя: {0}\nPID: {1}", p.ProcessName, p.Id)
							});
							break;
						}
					}
				}
				catch { }
			}

			// Проверка служб спуферов
			try
			{
				using (RegistryKey servKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
				{
					if (servKey != null)
					{
						string[] sNames = servKey.GetSubKeyNames();
						foreach (string sn in sNames)
						{
							string lowSn = sn.ToLowerInvariant();
							if (lowSn.Contains("spoofer") || lowSn.Contains("devimp") || lowSn.Contains("deviceimpersonate") || lowSn.Contains("serialspoof") || lowSn.Contains("macspoofer"))
							{
								list.Add(new HwidForensicItem
								{
									Index = idx++,
									ComponentCategory = "Служба / Драйвер ядра",
									ParameterName = sn,
									HardwareValue = "Служба зарегистрирована в реестре",
									Category = HwidFilterCategory.AnomaliesAndSpoofers,
									Verdict = "🚨 ОБНАРУЖЕН ДРАЙВЕР HWID СПУФЕРА В СЛУЖБАХ (5.08)",
									TargetRule = "5.08 (Драйвер спуфера)",
									DataSource = @"HKLM\SYSTEM\CurrentControlSet\Services\" + sn,
									IsAnomaly = true,
									IsSpooferDetected = true,
									RowColor = Ui.AccentRed,
									Details = "В реестре Windows зарегистрирована служба драйвера спуфера оборудования: " + sn
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private void ScanSpooferTracesOnDisk(List<HwidForensicItem> list, ref int idx)
		{
			// Prefetch traces
			string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string pfDir = Path.Combine(winDir, "Prefetch");
			if (Directory.Exists(pfDir))
			{
				try
				{
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					string[] spooferPatterns = new string[] { "TMAC", "VOLUMEID", "MACCHANGER", "SPOOFER", "KDMAPPER", "DISKID", "SERIALCHANGER" };
					foreach (string pf in pfFiles)
					{
						string fileName = Path.GetFileName(pf).ToUpperInvariant();
						foreach (string pat in spooferPatterns)
						{
							if (fileName.StartsWith(pat))
							{
								FileInfo fi = new FileInfo(pf);
								list.Add(new HwidForensicItem
								{
									Index = idx++,
									ComponentCategory = "След Prefetch",
									ParameterName = fileName,
									HardwareValue = "Дата запуска: " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
									Category = HwidFilterCategory.AnomaliesAndSpoofers,
									Verdict = "🚨 ЗАФИКСИРОВАН ЗАПУСК СПУФЕРА В PREFETCH",
									TargetRule = "5.02 / 5.08 (Спуфер)",
									DataSource = pf,
									IsAnomaly = true,
									IsSpooferDetected = true,
									RowColor = Ui.AccentRed,
									Details = string.Format("В журнале Windows Prefetch зафиксирован артефакт выполнения спуфера оборудования:\nФайл: {0}\nВремя: {1}", fileName, fi.LastWriteTime)
								});
								break;
							}
						}
					}
				}
				catch { }
			}
		}

		private static bool IsSuspiciousGuid(string guid)
		{
			if (string.IsNullOrEmpty(guid)) return true;
			string g = guid.ToLowerInvariant().Replace("{", "").Replace("}", "").Trim();

			if (g == "00000000-0000-0000-0000-000000000000" || g == "ffffffff-ffff-ffff-ffff-ffffffffffff" || g == "11111111-1111-1111-1111-111111111111")
			{
				return true;
			}

			// Проверка на некорректную длину или формат
			if (g.Length != 36) return true;
			return false;
		}

		private static bool IsSuspiciousSmbiosString(string val)
		{
			if (string.IsNullOrEmpty(val)) return true;
			string s = val.ToLowerInvariant().Trim();

			string[] dummyPatterns = new string[]
			{
				"to be filled by o.e.m.", "default string", "none", "system serial number",
				"system product name", "all series", "chassis serial number", "not specified",
				"00000000", "ffffffff", "12345678"
			};

			foreach (string d in dummyPatterns)
			{
				if (s.Contains(d)) return true;
			}
			return false;
		}

		private static bool IsSuspiciousDiskSerial(string serial)
		{
			if (string.IsNullOrEmpty(serial)) return false;
			string s = serial.ToLowerInvariant().Trim();

			if (s.StartsWith("0000") || s == "1234567890" || s.Contains("spoofer") || s == "none" || s == "default")
			{
				return true;
			}
			return false;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GetVolumeInformation(
			string rootPathName,
			StringBuilder volumeNameBuffer,
			uint volumeNameSize,
			out uint volumeSerialNumber,
			out uint maximumComponentLength,
			out uint fileSystemFlags,
			StringBuilder fileSystemNameBuffer,
			uint fileSystemNameSize);

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result is List<HwidForensicItem>)
			{
				allItems = (List<HwidForensicItem>)e.Result;
			}

			int total = allItems.Count;
			int anomalies = 0;
			int macs = 0;
			int drives = 0;

			foreach (HwidForensicItem it in allItems)
			{
				if (it.IsAnomaly) anomalies++;
				if (it.Category == HwidFilterCategory.NetworkMacs) macs++;
				if (it.Category == HwidFilterCategory.DrivesAndVolumes) drives++;
			}

			lblKpiTotal.Text = "Параметров: " + total;
			lblKpiAnomalies.Text = "Аномалий: " + anomalies;
			lblKpiMacs.Text = "MAC-адаптеров: " + macs;
			lblKpiDrives.Text = "Дисков/Томов: " + drives;

			if (anomalies > 0)
			{
				lblKpiStatus.Text = "Статус: 🚨 СПУФЕР";
				lblKpiStatus.ForeColor = Ui.AccentRed;
				lblStatusPill.Text = string.Format("🚨 ОБНАРУЖЕНО {0} АНОМАЛИЙ HWID / СЛЕДОВ СПУФЕРА (5.02 / 5.08)", anomalies);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblKpiStatus.Text = "Статус: ✅ ЧИСТО";
				lblKpiStatus.ForeColor = Ui.AccentGreen;
				lblStatusPill.Text = "✅ Аппаратные идентификаторы подлинные, признаков спуфинга не обнаружено";
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : "";

			foreach (HwidForensicItem it in allItems)
			{
				if (currentFilter == HwidFilterCategory.AnomaliesAndSpoofers && !it.IsAnomaly) continue;
				if (currentFilter == HwidFilterCategory.BiosAndMotherboard && it.Category != HwidFilterCategory.BiosAndMotherboard && !it.IsAnomaly) continue;
				if (currentFilter == HwidFilterCategory.NetworkMacs && it.Category != HwidFilterCategory.NetworkMacs && !it.IsAnomaly) continue;
				if (currentFilter == HwidFilterCategory.DrivesAndVolumes && it.Category != HwidFilterCategory.DrivesAndVolumes && !it.IsAnomaly) continue;
				if (currentFilter == HwidFilterCategory.RegistryCrypto && it.Category != HwidFilterCategory.RegistryCrypto && !it.IsAnomaly) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = (it.ComponentCategory != null && it.ComponentCategory.ToLowerInvariant().Contains(search))
						|| (it.ParameterName != null && it.ParameterName.ToLowerInvariant().Contains(search))
						|| (it.HardwareValue != null && it.HardwareValue.ToLowerInvariant().Contains(search))
						|| (it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(search))
						|| (it.DataSource != null && it.DataSource.ToLowerInvariant().Contains(search));
					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ComponentCategory);
				lvi.SubItems.Add(it.ParameterName);
				lvi.SubItems.Add(it.HardwareValue);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.DataSource);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();
			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			HwidForensicItem it = listView.SelectedItems[0].Tag as HwidForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АУДИТ АППАРАТНОГО ИДЕНТИФИКАТОРА (HWID FORENSICS) ===");
			sb.AppendLine("Категория оборудования: " + it.ComponentCategory);
			sb.AppendLine("Компонент / Параметр: " + it.ParameterName);
			sb.AppendLine("Идентификатор (Серийный номер / GUID / MAC): " + it.HardwareValue);
			sb.AppendLine("Вердикт криминалиста: " + it.Verdict);
			sb.AppendLine("Пункт регламента сервера: " + it.TargetRule);
			sb.AppendLine("Источник данных: " + it.DataSource);
			sb.AppendLine();
			sb.AppendLine("--- Подробный криминалистический протокол ---");
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions & Ban Execution

		private void CopySelectedValue()
		{
			if (listView.SelectedItems.Count == 0) return;
			HwidForensicItem it = listView.SelectedItems[0].Tag as HwidForensicItem;
			if (it == null || string.IsNullOrEmpty(it.HardwareValue)) return;

			Clipboard.SetText(it.HardwareValue);
			MessageBox.Show("Значение идентификатора скопировано в буфер обмена:\n" + it.HardwareValue, "Копирование HWID", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			HwidForensicItem it = listView.SelectedItems[0].Tag as HwidForensicItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ АППАРАТНОЙ УЛИКИ HWID / СПУФЕРА ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Компонент: " + it.ComponentCategory + " (" + it.ParameterName + ")");
			sb.AppendLine("Значение: " + it.HardwareValue);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Источник: " + it.DataSource);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Discord-отчет скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			HwidForensicItem it = listView.SelectedItems[0].Tag as HwidForensicItem;
			if (it == null) return;

			string cmd;
			if (it.IsSpooferDetected)
			{
				cmd = string.Format("/ban {0} 30d 5.02 Обход бана (Использование HWID/MAC спуфера: {1})", targetPlayerName, it.ParameterName);
			}
			else if (it.IsAnomaly)
			{
				cmd = string.Format("/ban {0} 30d 5.08 Сокрытие улик (Фальсификация аппаратных данных {1})", targetPlayerName, it.ParameterName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.02 Обход бана по железу", targetPlayerName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
