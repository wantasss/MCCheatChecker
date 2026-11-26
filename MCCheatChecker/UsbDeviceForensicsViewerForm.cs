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
	public class UsbDeviceForensicsViewerForm : Form
	{
		#region Models

		public enum UsbFilterMode
		{
			All,
			ThreatsOnly,
			DisconnectedOnly,
			ConnectedNow,
			ExecutionsOnly
		}

		public class UsbDeviceItem
		{
			public string DeviceName;
			public string Vendor;
			public string Product;
			public string SerialNumber;
			public string DriveLetter;
			public string VolumeGuid;
			public bool IsCurrentlyConnected;
			public bool HasCheatExecutions;
			public string Status;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public List<string> ExecutedFiles = new List<string>();
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private UsbFilterMode currentFilter = UsbFilterMode.All;
		private List<UsbDeviceItem> allEntries = new List<UsbDeviceItem>();
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
		private DarkButton btnFilterDisconnected;
		private DarkButton btnFilterConnected;
		private DarkButton btnFilterExecutions;

		private DarkButton btnCopyBan;
		private DarkButton btnOpenDrive;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger|cleaner|wiper|reghax|selfdelete)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public UsbDeviceForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Инспектор USB-накопителей и запусков со съемных носителей (USBSTOR) — MCCheatChecker";
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
			lblHeaderTitle.Text = "💾 Криминалистический аудит USB-накопителей: USBSTOR, MountPoints & Запуски с флешек";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Аудит подключений съемных USB-накопителей, серийных номеров и выявление запусков читов с извлеченных флешек (5.08 / 5.01).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование USBSTOR...";
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
			btnFilterAll = CreateFilterButton("Все накопители", UsbFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Запуски читов (5.08/5.01)", UsbFilterMode.ThreatsOnly, ref bx, filterBar);
			btnFilterDisconnected = CreateFilterButton("🔌 Извлеченные флешки", UsbFilterMode.DisconnectedOnly, ref bx, filterBar);
			btnFilterConnected = CreateFilterButton("💾 Подключенные сейчас", UsbFilterMode.ConnectedNow, ref bx, filterBar);
			btnFilterExecutions = CreateFilterButton("⚡ Файлы с флешек", UsbFilterMode.ExecutionsOnly, ref bx, filterBar);

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

			btnOpenDrive = new DarkButton();
			btnOpenDrive.Text = "📁 Открыть накопитель в Проводнике";
			btnOpenDrive.Font = Ui.FontBody(9f, false);
			btnOpenDrive.Location = new Point(242, 10);
			btnOpenDrive.Size = new Size(245, 30);
			btnOpenDrive.Click += BtnOpenDrive_Click;
			bottomBar.Controls.Add(btnOpenDrive);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику (SHA-256)";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(495, 10);
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
			btnExportReport.Location = new Point(733, 10);
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

			listView.Columns.Add("Устройство / Модель", 260);
			listView.Columns.Add("Буква диска", 100);
			listView.Columns.Add("Статус подключения", 180);
			listView.Columns.Add("Серийный номер / ID", 240);
			listView.Columns.Add("Вердикт / Улики", 280);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ О НАКОПИТЕЛЕ И ЗАПУСКАХ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите USB-накопитель из таблицы для подробностей.";
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

		private DarkButton CreateFilterButton(string text, UsbFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == UsbFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(150, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == UsbFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == UsbFilterMode.ThreatsOnly);
			btnFilterDisconnected.AccentStyle = (currentFilter == UsbFilterMode.DisconnectedOnly);
			btnFilterConnected.AccentStyle = (currentFilter == UsbFilterMode.ConnectedNow);
			btnFilterExecutions.AccentStyle = (currentFilter == UsbFilterMode.ExecutionsOnly);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование USB...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<UsbDeviceItem> results = new List<UsbDeviceItem>();

				// 1. Опрос активных дисков через DriveInfo
				Dictionary<string, DriveInfo> activeRemovable = new Dictionary<string, DriveInfo>(StringComparer.OrdinalIgnoreCase);
				try
				{
					foreach (DriveInfo di in DriveInfo.GetDrives())
					{
						if (di.DriveType == DriveType.Removable || (di.DriveType == DriveType.Fixed && !di.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)))
						{
							string letter = di.Name.TrimEnd('\\').ToUpperInvariant();
							activeRemovable[letter] = di;
						}
					}
				}
				catch { }

				// 2. Сбор истории запусков с внешних дисков (D:, E:, F:, G: и т.д.)
				Dictionary<string, List<string>> externalExecutions = CollectExternalDriveExecutions();

				// 3. Чтение сопоставлений MountedDevices (Буквы дисков <-> Серийные номера)
				Dictionary<string, string> driveLetterToSerial = GetMountedDevicesMapping();

				// 4. Сканирование реестра USBSTOR
				try
				{
					using (RegistryKey usbstor = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR"))
					{
						if (usbstor != null)
						{
							string[] devTypes = usbstor.GetSubKeyNames();
							foreach (string devType in devTypes)
							{
								using (RegistryKey typeKey = usbstor.OpenSubKey(devType))
								{
									if (typeKey == null) continue;
									string[] serials = typeKey.GetSubKeyNames();

									foreach (string serial in serials)
									{
										using (RegistryKey instKey = typeKey.OpenSubKey(serial))
										{
											if (instKey == null) continue;

											object friendlyObj = instKey.GetValue("FriendlyName");
											string friendly = (friendlyObj != null) ? friendlyObj.ToString() : devType;
											object hwidObj = instKey.GetValue("HardwareID");
											string hwid = (hwidObj != null) ? string.Join(" ", (string[])hwidObj) : "";

											// Ищем назначенную букву диска
											string assignedLetter = "";
											foreach (KeyValuePair<string, string> kvp in driveLetterToSerial)
											{
												if (kvp.Value.IndexOf(serial, StringComparison.OrdinalIgnoreCase) >= 0)
												{
													assignedLetter = kvp.Key;
													break;
												}
											}

											bool isConnected = false;
											if (!string.IsNullOrEmpty(assignedLetter) && activeRemovable.ContainsKey(assignedLetter))
											{
												isConnected = true;
											}

											UsbDeviceItem item = new UsbDeviceItem();
											item.DeviceName = friendly;
											item.Product = devType;
											item.SerialNumber = serial;
											item.DriveLetter = string.IsNullOrEmpty(assignedLetter) ? "Не назначена" : assignedLetter;
											item.IsCurrentlyConnected = isConnected;

											// Проверяем, запускались ли файлы с этой буквы диска
											if (!string.IsNullOrEmpty(assignedLetter) && externalExecutions.ContainsKey(assignedLetter))
											{
												item.ExecutedFiles = externalExecutions[assignedLetter];
											}

											// Проверка наличия читов в запусках
											bool cheatFound = false;
											string matchedCheat = "";
											foreach (string file in item.ExecutedFiles)
											{
												if (CheatRegex.IsMatch(file))
												{
													cheatFound = true;
													matchedCheat = Path.GetFileName(file);
													break;
												}
											}

											item.HasCheatExecutions = cheatFound;

											if (cheatFound && !isConnected)
											{
												item.Status = "🚨 ЧИТ ЗАПУЩЕН С ИЗВЛЕЧЕННОЙ ФЛЕШКИ (5.08 / 5.01)";
												item.RuleViolation = "5.08 / 5.01";
												item.BanReason = string.Format("Запуск чита {0} с флешки {1}, накопитель извлечен перед проверкой", matchedCheat, assignedLetter);
												item.ForeColor = Ui.AccentRed;
												item.Details = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА 5.08 / 5.01!\nУстройство: {0}\nБуква диска: {1} (ИЗВЛЕЧЕНО ПЕРЕД ПРОВЕРКОЙ)\nСерийный номер: {2}\nЗапущенный чит: {3}\nИстория запусков с этого накопителя:\n{4}\nВердикт: Игрок запускал чит со съемной флешки и отключил её перед проверкой.",
													friendly, assignedLetter, serial, matchedCheat, string.Join("\n", item.ExecutedFiles.ToArray()));
											}
											else if (cheatFound && isConnected)
											{
												item.Status = "🚨 НАЙДЕН ЧИТ НА ПОДКЛЮЧЕННОЙ ФЛЕШКЕ (5.01)";
												item.RuleViolation = "5.01";
												item.BanReason = string.Format("Использование читов: запуск {0} с флешки {1}", matchedCheat, assignedLetter);
												item.ForeColor = Ui.AccentRed;
												item.Details = string.Format("ОБНАРУЖЕН ЗАПУСК ЧИТА СО СЪЕМНОГО ДИСКА!\nУстройство: {0} ({1})\nЗапущенный софт: {2}\nИстория запусков:\n{3}",
													friendly, assignedLetter, matchedCheat, string.Join("\n", item.ExecutedFiles.ToArray()));
											}
											else if (!isConnected)
											{
												item.Status = "🔌 Флешка извлечена (в истории USBSTOR)";
												item.RuleViolation = "-";
												item.BanReason = "-";
												item.ForeColor = Ui.AccentOrange;
												item.Details = string.Format("Устройство: {0}\nБуква диска в истории: {1}\nСерийный номер: {2}\nСтатус: Накопитель отключен от ПК.\nЗапусков подозрительных файлов не зафиксировано.",
													friendly, assignedLetter, serial);
											}
											else
											{
												item.Status = "💾 Подключенный съемный диск (Чист)";
												item.RuleViolation = "-";
												item.BanReason = "-";
												item.ForeColor = Ui.AccentGreen;
												item.Details = string.Format("Устройство: {0}\nАктивная буква: {1}\nСерийный номер: {2}\nСтатус: Накопитель подключен к системе.\nПодозрительных запусков не обнаружено.",
													friendly, assignedLetter, serial);
											}

											results.Add(item);
										}
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
					int threatCount = results.Count(delegate(UsbDeviceItem x) { return x.HasCheatExecutions; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ ЧИТЫ НА USB: {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Накопителей в истории: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private Dictionary<string, string> GetMountedDevicesMapping()
		{
			Dictionary<string, string> mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\MountedDevices"))
				{
					if (key != null)
					{
						string[] valNames = key.GetValueNames();
						foreach (string vName in valNames)
						{
							if (vName.StartsWith(@"\DosDevices\", StringComparison.OrdinalIgnoreCase))
							{
								string letter = vName.Substring(12).ToUpperInvariant();
								object val = key.GetValue(vName);
								byte[] bytes = val as byte[];
								if (bytes != null)
								{
									string str = Encoding.Unicode.GetString(bytes);
									mapping[letter] = str;
								}
							}
						}
					}
				}
			}
			catch { }
			return mapping;
		}

		private Dictionary<string, List<string>> CollectExternalDriveExecutions()
		{
			Dictionary<string, List<string>> driveToFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			// 1. Поиск запусков в BAM / DAM
			string[] bamKeys = new string[]
			{
				@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\dam\State\UserSettings"
			};

			foreach (string bk in bamKeys)
			{
				try
				{
					using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(bk))
					{
						if (baseKey == null) continue;
						foreach (string sid in baseKey.GetSubKeyNames())
						{
							using (RegistryKey sidKey = baseKey.OpenSubKey(sid))
							{
								if (sidKey == null) continue;
								foreach (string rawPath in sidKey.GetValueNames())
								{
									AddPathIfExternal(rawPath, driveToFiles);
								}
							}
						}
					}
				}
				catch { }
			}

			// 2. Поиск в UserAssist
			try
			{
				using (RegistryKey uaRoot = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist"))
				{
					if (uaRoot != null)
					{
						foreach (string guid in uaRoot.GetSubKeyNames())
						{
							using (RegistryKey countKey = uaRoot.OpenSubKey(guid + @"\Count"))
							{
								if (countKey == null) continue;
								foreach (string encName in countKey.GetValueNames())
								{
									string decName = DecodeRot13(encName);
									AddPathIfExternal(decName, driveToFiles);
								}
							}
						}
					}
				}
			}
			catch { }

			// 3. Поиск в MuiCache
			try
			{
				using (RegistryKey mui = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"))
				{
					if (mui != null)
					{
						foreach (string p in mui.GetValueNames())
						{
							AddPathIfExternal(p, driveToFiles);
						}
					}
				}
			}
			catch { }

			return driveToFiles;
		}

		private void AddPathIfExternal(string path, Dictionary<string, List<string>> driveToFiles)
		{
			if (string.IsNullOrEmpty(path) || path.Length < 3) return;
			char driveChar = char.ToUpperInvariant(path[0]);
			if (driveChar >= 'D' && driveChar <= 'Z' && path[1] == ':')
			{
				string letter = driveChar + ":";
				if (!driveToFiles.ContainsKey(letter))
				{
					driveToFiles[letter] = new List<string>();
				}

				if (!driveToFiles[letter].Contains(path))
				{
					driveToFiles[letter].Add(path);
				}
			}
		}

		private string DecodeRot13(string input)
		{
			if (string.IsNullOrEmpty(input)) return string.Empty;
			char[] array = input.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				char c = array[i];
				if (c >= 'a' && c <= 'z') array[i] = (char)((c - 'a' + 13) % 26 + 'a');
				else if (c >= 'A' && c <= 'Z') array[i] = (char)((c - 'A' + 13) % 26 + 'A');
			}
			return new string(array);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (UsbDeviceItem item in allEntries)
			{
				if (currentFilter == UsbFilterMode.ThreatsOnly && !item.HasCheatExecutions) continue;
				if (currentFilter == UsbFilterMode.DisconnectedOnly && item.IsCurrentlyConnected) continue;
				if (currentFilter == UsbFilterMode.ConnectedNow && !item.IsCurrentlyConnected) continue;
				if (currentFilter == UsbFilterMode.ExecutionsOnly && item.ExecutedFiles.Count == 0) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.DeviceName != null && item.DeviceName.ToLowerInvariant().Contains(search)) ||
								   (item.Product != null && item.Product.ToLowerInvariant().Contains(search)) ||
								   (item.SerialNumber != null && item.SerialNumber.ToLowerInvariant().Contains(search)) ||
								   (item.DriveLetter != null && item.DriveLetter.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.DeviceName);
				lvi.SubItems.Add(item.DriveLetter);
				lvi.SubItems.Add(item.IsCurrentlyConnected ? "✅ Подключен сейчас" : "🔌 Извлечен из ПК");
				lvi.SubItems.Add(item.SerialNumber);
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

			UsbDeviceItem item = listView.SelectedItems[0].Tag as UsbDeviceItem;
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
				lblDetailRule.Text = "🛡️ Нарушений регламента не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details ?? "Нет сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Накопитель не выбран";
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
				MessageBox.Show("Выберите накопитель с нарушением из таблицы для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			UsbDeviceItem item = listView.SelectedItems[0].Tag as UsbDeviceItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранного накопителя нарушений не зафиксировано.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnOpenDrive_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			UsbDeviceItem item = listView.SelectedItems[0].Tag as UsbDeviceItem;
			if (item == null) return;

			if (item.IsCurrentlyConnected && item.DriveLetter.Length >= 2)
			{
				try
				{
					Process.Start("explorer.exe", item.DriveLetter + "\\");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Не удалось открыть диск: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				MessageBox.Show("Накопитель сейчас отключен (извлечен из ПК). Физический доступ к содержимому невозможен.", "Диск отключен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("USB_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА USBSTOR И СЪЕМНЫХ НОСИТЕЛЕЙ");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (UsbDeviceItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1} (Буква: {2}, Статус: {3})",
							item.Product, item.DeviceName, item.DriveLetter, item.IsCurrentlyConnected ? "ПОДКЛЮЧЕН" : "ИЗВЛЕЧЕН"));
						sb.AppendLine(string.Format("Серийный номер: {0}", item.SerialNumber));
						sb.AppendLine(string.Format("Вердикт:        {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:      Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:        /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						if (item.ExecutedFiles.Count > 0)
						{
							sb.AppendLine("Запущенные файлы с этого накопителя:");
							foreach (string f in item.ExecutedFiles)
							{
								sb.AppendLine(string.Format("  - {0}", f));
							}
						}
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
