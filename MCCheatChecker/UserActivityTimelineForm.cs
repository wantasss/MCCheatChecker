using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class UserActivityTimelineForm : Form
	{
		private enum ActivityCategory
		{
			All,
			Browser,
			Messengers,
			PowerShell,
			RecentFiles,
			RecycleBin,
			UsbStorage,
			Bam
		}

		private class ActivityItem
		{
			public int Index;
			public DateTime Timestamp;
			public string TimeString;
			public string CategoryString;
			public ActivityCategory Category;
			public string Title;
			public string Detail;
			public bool IsSuspicious;
			public Color ForeColor;
		}

		private static readonly Regex CheatKeywordRegex = new Regex(
			@"(?i)(vape|celestial|expensive|wild|doomsday|rich|akrien|killaura|velocity|aimbot|speed|meteor|liquidbounce|wurst|inertia|aristois|future|rusherhack|konas|kami|sigma|bape|neverhook|exptv|xray|fly|reach|scaffold|esp|autoclicker|macros|clicker|\.jar)",
			RegexOptions.Compiled);

		private static readonly Regex SuspiciousPsRegex = new Regex(
			@"(?i)(iwr|invoke-webrequest|downloadstring|curl|certutil|bitsadmin|powershell\s+-enc|bypass|rmdir|remove-item|del\s|celestial|expensive|wild|vape|doomsday|rich|akrien|\.ps1|clean|wevtutil|clear-history)",
			RegexOptions.Compiled);

		private List<ActivityItem> allItems = new List<ActivityItem>();
		private ActivityCategory currentCategory = ActivityCategory.All;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkCheckBox chkOnlySuspicious;
		private Label lblHeaderStatus;
		private Label lblStatsTotal;
		private Label lblStatsSuspicious;
		private Label lblStatsCategory;

		private DarkButton btnCatAll;
		private DarkButton btnCatBrowser;
		private DarkButton btnCatMessengers;
		private DarkButton btnCatPs;
		private DarkButton btnCatRecent;
		private DarkButton btnCatRecycle;
		private DarkButton btnCatUsb;
		private DarkButton btnCatBam;

		public UserActivityTimelineForm()
		{
			InitializeComponent();
			LoadAllActivityDataAsync();
		}

		private void InitializeComponent()
		{
			Text = "🕒 Хронология активности игрока (Screenshare Timeline & Загрузки)";
			Size = new Size(1160, 740);
			MinimumSize = new Size(880, 520);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 112;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			Label lblTitle = new Label();
			lblTitle.Text = "🕒  ХРОНОЛОГИЯ АКТИВНОСТИ И ИСТОРИЯ ЗАГРУЗОК (Screenshare Timeline)";
			lblTitle.Font = Ui.FontTitle(10f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.Location = new Point(12, 8);
			lblTitle.AutoSize = true;
			topPanel.Controls.Add(lblTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Сбор данных браузеров, команд PowerShell, корзины, .lnk и подключений USB...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Обновить";
			btnRefresh.Location = new Point(910, 8);
			btnRefresh.Size = new Size(105, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				LoadAllActivityDataAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			chkOnlySuspicious = new DarkCheckBox();
			chkOnlySuspicious.Text = "🚨 Только подозрительное";
			chkOnlySuspicious.Location = new Point(1025, 11);
			chkOnlySuspicious.Size = new Size(185, 20);
			chkOnlySuspicious.ForeColor = Ui.AccentRed;
			chkOnlySuspicious.Checked = false;
			chkOnlySuspicious.CheckedChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			topPanel.Controls.Add(chkOnlySuspicious);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1130, 52);
			navRow.BackColor = Color.Transparent;

			btnCatAll = CreateCategoryButton("🔍 Всё (0)", ActivityCategory.All, 85);
			btnCatBrowser = CreateCategoryButton("🌐 Браузер (0)", ActivityCategory.Browser, 105);
			btnCatMessengers = CreateCategoryButton("💬 Мессенджеры (0)", ActivityCategory.Messengers, 120);
			btnCatPs = CreateCategoryButton("💻 PowerShell (0)", ActivityCategory.PowerShell, 115);
			btnCatRecent = CreateCategoryButton("🕒 Недавние (0)", ActivityCategory.RecentFiles, 105);
			btnCatRecycle = CreateCategoryButton("🗑️ Корзина (0)", ActivityCategory.RecycleBin, 100);
			btnCatUsb = CreateCategoryButton("🔌 USB (0)", ActivityCategory.UsbStorage, 90);
			btnCatBam = CreateCategoryButton("⚡ Запуски (0)", ActivityCategory.Bam, 95);

			int curX = 0;
			DarkButton[] catBtns = { btnCatAll, btnCatBrowser, btnCatMessengers, btnCatPs, btnCatRecent, btnCatRecycle, btnCatUsb, btnCatBam };
			foreach (DarkButton b in catBtns)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 6;
			}

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(curX + 8, 5);
			lblSearch.AutoSize = true;
			navRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(curX + 56, 2);
			searchBox.Size = new Size(240, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 300, 2);
			btnClear.Size = new Size(26, 22);
			btnClear.BackColor = Ui.SurfaceAlt;
			btnClear.ForeColor = Ui.TextMuted;
			btnClear.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			navRow.Controls.Add(btnClear);

			topPanel.Controls.Add(navRow);
			Controls.Add(topPanel);

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontMono(8.5f);
			listView.Columns.Add("№", 55, HorizontalAlignment.Right);
			listView.Columns.Add("Время / Дата", 145, HorizontalAlignment.Left);
			listView.Columns.Add("Источник", 145, HorizontalAlignment.Left);
			listView.Columns.Add("Объект / Команда / Файл", 280, HorizontalAlignment.Left);
			listView.Columns.Add("Детали / Полный путь / URL", 510, HorizontalAlignment.Left);

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();
			ToolStripMenuItem copyItem = new ToolStripMenuItem("📋  Скопировать строку");
			copyItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (ListViewItem it in listView.SelectedItems)
					{
						sb.AppendLine(it.SubItems[1].Text + "\t" + it.SubItems[2].Text + "\t" + it.SubItems[3].Text + "\t" + it.SubItems[4].Text);
					}
					Clipboard.SetText(sb.ToString().TrimEnd());
				}
			};
			ToolStripMenuItem copyEvidenceItem = new ToolStripMenuItem("🚨  Скопировать все подозрительные улики (отчёт)");
			copyEvidenceItem.Click += delegate
			{
				CopySuspiciousReport();
			};
			ToolStripMenuItem openExplorerItem = new ToolStripMenuItem("📁  Открыть расположение объекта");
			openExplorerItem.Click += delegate
			{
				OpenSelectedInExplorer();
			};
			ctx.Items.Add(copyItem);
			ctx.Items.Add(copyEvidenceItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(openExplorerItem);
			listView.ContextMenuStrip = ctx;

			listView.DoubleClick += delegate
			{
				OpenSelectedInExplorer();
			};

			Controls.Add(listView);
			listView.BringToFront();

			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 44;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			lblStatsTotal = new Label() { Text = "Всего: 0", Location = new Point(14, 12), AutoSize = true, ForeColor = Ui.TextSecondary, Font = Ui.FontBody(8.5f, true) };
			lblStatsSuspicious = new Label() { Text = "🚨 Подозрительных: 0", Location = new Point(130, 12), AutoSize = true, ForeColor = Ui.AccentRed, Font = Ui.FontBody(8.5f, true) };
			lblStatsCategory = new Label() { Text = "Категория: Всё", Location = new Point(310, 12), AutoSize = true, ForeColor = Ui.AccentTeal, Font = Ui.FontBody(8.5f, true) };

			bottomPanel.Controls.Add(lblStatsTotal);
			bottomPanel.Controls.Add(lblStatsSuspicious);
			bottomPanel.Controls.Add(lblStatsCategory);

			DarkButton btnCopySusp = new DarkButton();
			btnCopySusp.Text = "📋 Скопировать улики";
			btnCopySusp.Size = new Size(190, 28);
			btnCopySusp.Location = new Point(590, 8);
			btnCopySusp.BackColor = Color.FromArgb(180, 40, 50);
			btnCopySusp.ForeColor = Color.White;
			btnCopySusp.Font = Ui.FontBody(8.5f, true);
			btnCopySusp.Cursor = Cursors.Hand;
			btnCopySusp.Click += delegate
			{
				CopySuspiciousReport();
			};
			bottomPanel.Controls.Add(btnCopySusp);

			DarkButton btnCopyVis = new DarkButton();
			btnCopyVis.Text = "📋 Копировать видимые";
			btnCopyVis.Size = new Size(180, 28);
			btnCopyVis.Location = new Point(790, 8);
			btnCopyVis.BackColor = Ui.SurfaceAlt;
			btnCopyVis.ForeColor = Ui.TextPrimary;
			btnCopyVis.Cursor = Cursors.Hand;
			btnCopyVis.Click += delegate
			{
				StringBuilder sb = new StringBuilder();
				foreach (ListViewItem item in listView.Items)
				{
					sb.AppendLine(item.SubItems[0].Text + "\t" + item.SubItems[1].Text + "\t" + item.SubItems[2].Text + "\t" + item.SubItems[3].Text + "\t" + item.SubItems[4].Text);
				}
				if (sb.Length > 0)
				{
					Clipboard.SetText(sb.ToString());
					MessageBox.Show("Видимые записи (" + listView.Items.Count + " шт.) скопированы в буфер обмена!", "Хронология активности", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			bottomPanel.Controls.Add(btnCopyVis);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(110, 28);
			btnClose.Location = new Point(980, 8);
			btnClose.BackColor = Ui.SurfaceAlt;
			btnClose.ForeColor = Ui.TextPrimary;
			btnClose.DialogResult = DialogResult.OK;
			bottomPanel.Controls.Add(btnClose);

			Controls.Add(bottomPanel);
			bottomPanel.BringToFront();

			UpdateCategoryButtons();
		}

		private DarkButton CreateCategoryButton(string text, ActivityCategory cat, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentCategory = cat;
				UpdateCategoryButtons();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateCategoryButtons()
		{
			DarkButton[] btns = { btnCatAll, btnCatBrowser, btnCatMessengers, btnCatPs, btnCatRecent, btnCatRecycle, btnCatUsb, btnCatBam };
			ActivityCategory[] cats = { ActivityCategory.All, ActivityCategory.Browser, ActivityCategory.Messengers, ActivityCategory.PowerShell, ActivityCategory.RecentFiles, ActivityCategory.RecycleBin, ActivityCategory.UsbStorage, ActivityCategory.Bam };

			for (int i = 0; i < btns.Length; i++)
			{
				if (currentCategory == cats[i])
				{
					btns[i].BackColor = Ui.AccentTeal;
					btns[i].ForeColor = Color.Black;
					btns[i].Font = Ui.FontBody(8.5f, true);
				}
				else
				{
					btns[i].BackColor = Ui.SurfaceAlt;
					btns[i].ForeColor = Ui.TextPrimary;
					btns[i].Font = Ui.FontBody(8.5f, false);
				}
			}
		}

		private void LoadAllActivityDataAsync()
		{
			lblHeaderStatus.Text = "Сбор данных браузеров, PowerShell, корзины, .lnk и USB...";
			lblHeaderStatus.ForeColor = Ui.AccentOrange;

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<ActivityItem> items = new List<ActivityItem>();

				CollectPowerShellHistory(items);
				CollectRunMru(items);
				CollectRecycleBin(items);
				CollectRecentLnkFiles(items);
				CollectUsbStorage(items);
				CollectBrowserActivity(items);
				CollectMessengerActivity(items);
				CollectBamActivity(items);
				CollectUserAssistActivity(items);
				CollectMuiCacheActivity(items);
				CollectPcaStoreActivity(items);

				// Sort items descending by timestamp when available
				items.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

				// Assign serial numbers
				for (int i = 0; i < items.Count; i++)
				{
					items[i].Index = i + 1;
				}

				e.Result = items;
			};

			bw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					lblHeaderStatus.Text = "Ошибка сбора данных: " + e.Error.Message;
					lblHeaderStatus.ForeColor = Ui.AccentRed;
					return;
				}

				allItems = e.Result as List<ActivityItem> ?? new List<ActivityItem>();

				int total = allItems.Count;
				int susp = allItems.Count(i => i.IsSuspicious);
				int browserC = allItems.Count(i => i.Category == ActivityCategory.Browser);
				int msgC = allItems.Count(i => i.Category == ActivityCategory.Messengers);
				int psC = allItems.Count(i => i.Category == ActivityCategory.PowerShell);
				int recentC = allItems.Count(i => i.Category == ActivityCategory.RecentFiles);
				int recycleC = allItems.Count(i => i.Category == ActivityCategory.RecycleBin);
				int usbC = allItems.Count(i => i.Category == ActivityCategory.UsbStorage);
				int bamC = allItems.Count(i => i.Category == ActivityCategory.Bam);

				btnCatAll.Text = "🔍 Всё (" + total + ")";
				btnCatBrowser.Text = "🌐 Браузер (" + browserC + ")";
				btnCatMessengers.Text = "💬 Мессенджеры (" + msgC + ")";
				btnCatPs.Text = "💻 PowerShell (" + psC + ")";
				btnCatRecent.Text = "🕒 Недавние (" + recentC + ")";
				btnCatRecycle.Text = "🗑️ Корзина (" + recycleC + ")";
				btnCatUsb.Text = "🔌 USB (" + usbC + ")";
				btnCatBam.Text = "⚡ Запуски (" + bamC + ")";

				lblHeaderStatus.Text = string.Format("Сбор завершён: найдено {0:N0} событий (подозрительных: {1:N0})", total, susp);
				lblHeaderStatus.ForeColor = (susp > 0) ? Ui.AccentRed : Ui.AccentGreen;

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private static void CollectPowerShellHistory(List<ActivityItem> list)
		{
			try
			{
				string psPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					@"Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");
				if (File.Exists(psPath))
				{
					DateTime dt = File.GetLastWriteTime(psPath);
					string[] lines = File.ReadAllLines(psPath);
					int lIndex = 1;
					foreach (string line in lines)
					{
						if (string.IsNullOrWhiteSpace(line)) { lIndex++; continue; }
						bool isSusp = SuspiciousPsRegex.IsMatch(line);
						list.Add(new ActivityItem
						{
							Timestamp = dt,
							TimeString = dt.ToString("dd.MM.yyyy HH:mm"),
							Category = ActivityCategory.PowerShell,
							CategoryString = "💻 PowerShell",
							Title = "Команда #" + lIndex,
							Detail = line.Trim(),
							IsSuspicious = isSusp,
							ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
						});
						lIndex++;
					}
				}
			}
			catch { }
		}

		private static void CollectRunMru(List<ActivityItem> list)
		{
			try
			{
				using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"))
				{
					if (rk != null)
					{
						DateTime now = DateTime.Now;
						foreach (string valName in rk.GetValueNames())
						{
							if (valName == "MRUList") continue;
							string cmd = rk.GetValue(valName) as string;
							if (!string.IsNullOrEmpty(cmd))
							{
								cmd = cmd.TrimEnd('\\', '1');
								bool isSusp = SuspiciousPsRegex.IsMatch(cmd) || CheatKeywordRegex.IsMatch(cmd);
								list.Add(new ActivityItem
								{
									Timestamp = now,
									TimeString = "Реестр RunMRU",
									Category = ActivityCategory.PowerShell,
									CategoryString = "⌨️ Win+R (Run)",
									Title = "Запуск: " + cmd,
									Detail = "HKCU\\...\\Explorer\\RunMRU [" + valName + "]",
									IsSuspicious = isSusp,
									ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private static void CollectRecycleBin(List<ActivityItem> list)
		{
			try
			{
				foreach (DriveInfo d in DriveInfo.GetDrives())
				{
					if (!d.IsReady) continue;
					string rbPath = Path.Combine(d.RootDirectory.FullName, "$Recycle.Bin");
					if (!Directory.Exists(rbPath)) continue;
					try
					{
						foreach (string sidDir in Directory.GetDirectories(rbPath))
						{
							foreach (string iFile in Directory.GetFiles(sidDir, "$I*"))
							{
								try
								{
									byte[] bytes = File.ReadAllBytes(iFile);
									if (bytes.Length >= 28)
									{
										long ft = BitConverter.ToInt64(bytes, 16);
										DateTime dt = DateTime.FromFileTime(ft);
										string origPath = Encoding.Unicode.GetString(bytes, 28, bytes.Length - 28).TrimEnd('\0');
										long size = BitConverter.ToInt64(bytes, 8);
										string fileName = Path.GetFileName(origPath);
										bool isSusp = CheatKeywordRegex.IsMatch(origPath) ||
													  origPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ||
													  origPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
													  origPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

										list.Add(new ActivityItem
										{
											Timestamp = dt,
											TimeString = dt.ToString("dd.MM.yyyy HH:mm"),
											Category = ActivityCategory.RecycleBin,
											CategoryString = "🗑️ Корзина (" + d.Name.TrimEnd('\\') + ")",
											Title = string.IsNullOrEmpty(fileName) ? origPath : fileName,
											Detail = origPath + " (" + (size / 1024.0).ToString("F1") + " КБ)",
											IsSuspicious = isSusp,
											ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
										});
									}
								}
								catch { }
							}
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private static void CollectRecentLnkFiles(List<ActivityItem> list)
		{
			try
			{
				string recentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent");
				if (Directory.Exists(recentFolder))
				{
					foreach (string lnk in Directory.GetFiles(recentFolder, "*.lnk"))
					{
						try
						{
							FileInfo fi = new FileInfo(lnk);
							string baseName = Path.GetFileNameWithoutExtension(lnk);
							string targetPath = ExtractPathFromLnk(lnk);
							string displayPath = string.IsNullOrEmpty(targetPath) ? lnk : targetPath;

							bool isSusp = CheatKeywordRegex.IsMatch(baseName + " " + displayPath);

							list.Add(new ActivityItem
							{
								Timestamp = fi.LastWriteTime,
								TimeString = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
								Category = ActivityCategory.RecentFiles,
								CategoryString = "🕒 Недавний .lnk",
								Title = baseName,
								Detail = displayPath,
								IsSuspicious = isSusp,
								ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
							});
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private static string ExtractPathFromLnk(string lnkPath)
		{
			try
			{
				byte[] bytes = File.ReadAllBytes(lnkPath);
				string textAscii = Encoding.ASCII.GetString(bytes);
				Match m = Regex.Match(textAscii, @"[A-Za-z]:\\(?:[^\\\/:*?""<>|\r\n]+\\)*[^\\\/:*?""<>|\r\n]+");
				if (m.Success)
				{
					string p = m.Value;
					if (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
						p.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ||
						p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
						p.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) ||
						p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
						Directory.Exists(p) || File.Exists(p))
					{
						return p;
					}
				}
				string textUni = Encoding.Unicode.GetString(bytes);
				Match mUni = Regex.Match(textUni, @"[A-Za-z]:\\(?:[^\\\/:*?""<>|\r\n]+\\)*[^\\\/:*?""<>|\r\n]+");
				if (mUni.Success)
				{
					return mUni.Value;
				}
			}
			catch { }
			return "";
		}

		private static void CollectUsbStorage(List<ActivityItem> list)
		{
			try
			{
				using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR"))
				{
					if (rk != null)
					{
						DateTime now = DateTime.Now;
						foreach (string devSub in rk.GetSubKeyNames())
						{
							using (RegistryKey devKey = rk.OpenSubKey(devSub))
							{
								if (devKey != null)
								{
									foreach (string inst in devKey.GetSubKeyNames())
									{
										using (RegistryKey instKey = devKey.OpenSubKey(inst))
										{
											if (instKey != null)
											{
												string friendly = instKey.GetValue("FriendlyName") as string ?? devSub;
												string mfg = instKey.GetValue("Mfg") as string ?? "";

												list.Add(new ActivityItem
												{
													Timestamp = now,
													TimeString = "Реестр USBSTOR",
													Category = ActivityCategory.UsbStorage,
													CategoryString = "🔌 USB накопитель",
													Title = friendly,
													Detail = "Серийный номер / ID: " + inst + " [" + mfg + "]",
													IsSuspicious = false,
													ForeColor = Ui.AccentTeal
												});
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

		private static void CollectBamActivity(List<ActivityItem> list)
		{
			try
			{
				string[] bamPaths = new string[]
				{
					@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
					@"SYSTEM\CurrentControlSet\Services\bam\UserSettings"
				};
				foreach (string basePath in bamPaths)
				{
					using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(basePath))
					{
						if (baseKey == null) continue;
						foreach (string sid in baseKey.GetSubKeyNames())
						{
							using (RegistryKey sidKey = baseKey.OpenSubKey(sid))
							{
								if (sidKey == null) continue;
								foreach (string valName in sidKey.GetValueNames())
								{
									if (string.IsNullOrEmpty(valName) || valName.Length < 3) continue;
									byte[] valBytes = sidKey.GetValue(valName) as byte[];
									DateTime dt = DateTime.MinValue;
									if (valBytes != null && valBytes.Length >= 8)
									{
										long ft = BitConverter.ToInt64(valBytes, 0);
										try { dt = DateTime.FromFileTime(ft); } catch { }
									}

									string cleanPath = Regex.Replace(valName, @"^\\Device\\HarddiskVolume\d+\\", @"C:\");
									string fileName = Path.GetFileName(cleanPath);
									bool isSusp = CheatKeywordRegex.IsMatch(cleanPath);

									list.Add(new ActivityItem
									{
										Timestamp = (dt > DateTime.MinValue) ? dt : DateTime.Now,
										TimeString = (dt > DateTime.MinValue) ? dt.ToString("dd.MM.yyyy HH:mm") : "Реестр BAM",
										Category = ActivityCategory.Bam,
										CategoryString = "⚡ Запуск BAM",
										Title = string.IsNullOrEmpty(fileName) ? cleanPath : fileName,
										Detail = cleanPath,
										IsSuspicious = isSusp,
										ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
									});
								}
							}
						}
					}
				}
			}
			catch { }
		}

		
		private static void CollectUserAssistActivity(List<ActivityItem> list)
		{
			try
			{
				using (RegistryKey uaKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist"))
				{
					if (uaKey == null) return;
					foreach (string subGuid in uaKey.GetSubKeyNames())
					{
						using (RegistryKey countKey = uaKey.OpenSubKey(subGuid + @"\Count"))
						{
							if (countKey == null) continue;
							foreach (string valName in countKey.GetValueNames())
							{
								if (string.IsNullOrEmpty(valName)) continue;
								string decoded = DecodeRot13(valName);
								if (decoded.StartsWith("UEME_RUNPATH:")) decoded = decoded.Substring("UEME_RUNPATH:".Length).Trim();
								else if (decoded.StartsWith("UEME_RUNPIDL:")) decoded = decoded.Substring("UEME_RUNPIDL:".Length).Trim();

								if (decoded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) < 0 &&
									decoded.IndexOf(".jar", StringComparison.OrdinalIgnoreCase) < 0 &&
									decoded.IndexOf(".bat", StringComparison.OrdinalIgnoreCase) < 0)
								{
									continue;
								}

								byte[] valBytes = countKey.GetValue(valName) as byte[];
								int runCount = 0;
								DateTime dt = DateTime.MinValue;
								if (valBytes != null && valBytes.Length >= 68)
								{
									runCount = BitConverter.ToInt32(valBytes, 4);
									long ft = BitConverter.ToInt64(valBytes, 60);
									try { dt = DateTime.FromFileTime(ft); } catch { }
								}

								string fileName = Path.GetFileName(decoded);
								bool isSusp = CheatKeywordRegex.IsMatch(decoded);

								list.Add(new ActivityItem
								{
									Timestamp = (dt > DateTime.MinValue) ? dt : DateTime.Now,
									TimeString = (dt > DateTime.MinValue) ? dt.ToString("dd.MM.yyyy HH:mm") : "UserAssist",
									Category = ActivityCategory.Bam,
									CategoryString = "⚡ UserAssist (Explorer)",
									Title = string.IsNullOrEmpty(fileName) ? decoded : fileName,
									Detail = decoded + string.Format(" [Запусков: {0}]", runCount),
									IsSuspicious = isSusp,
									ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private static void CollectMuiCacheActivity(List<ActivityItem> list)
		{
			try
			{
				string[] muiPaths = {
					@"Local Settings\Software\Microsoft\Windows\Shell\MuiCache",
					@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"
				};

				foreach (string mPath in muiPaths)
				{
					using (RegistryKey mKey = Registry.CurrentUser.OpenSubKey(mPath))
					{
						if (mKey == null) continue;
						foreach (string valName in mKey.GetValueNames())
						{
							if (string.IsNullOrEmpty(valName)) continue;
							if (valName.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) < 0 &&
								valName.IndexOf(".jar", StringComparison.OrdinalIgnoreCase) < 0 &&
								valName.IndexOf(".bat", StringComparison.OrdinalIgnoreCase) < 0)
							{
								continue;
							}

							string cleanPath = valName;
							if (cleanPath.EndsWith(".FriendlyAppName", StringComparison.OrdinalIgnoreCase))
								cleanPath = cleanPath.Substring(0, cleanPath.Length - ".FriendlyAppName".Length);
							else if (cleanPath.EndsWith(".ApplicationCompany", StringComparison.OrdinalIgnoreCase))
								cleanPath = cleanPath.Substring(0, cleanPath.Length - ".ApplicationCompany".Length);

							string appTitle = mKey.GetValue(valName) as string ?? "";
							bool isSusp = CheatKeywordRegex.IsMatch(cleanPath) || CheatKeywordRegex.IsMatch(appTitle);
							string fileName = Path.GetFileName(cleanPath);

							list.Add(new ActivityItem
							{
								Timestamp = DateTime.MinValue,
								TimeString = "MuiCache",
								Category = ActivityCategory.Bam,
								CategoryString = "⚡ MuiCache",
								Title = string.IsNullOrEmpty(fileName) ? cleanPath : fileName,
								Detail = cleanPath + (!string.IsNullOrEmpty(appTitle) ? " (" + appTitle + ")" : ""),
								IsSuspicious = isSusp,
								ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
							});
						}
					}
				}
			}
			catch { }
		}

		private static void CollectPcaStoreActivity(List<ActivityItem> list)
		{
			try
			{
				using (RegistryKey pcaKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Compatibility Assistant\\Store"))
				{
					if (pcaKey == null) return;
					foreach (string valName in pcaKey.GetValueNames())
					{
						if (string.IsNullOrEmpty(valName)) continue;
						bool isSusp = CheatKeywordRegex.IsMatch(valName);
						string fileName = Path.GetFileName(valName);

						list.Add(new ActivityItem
						{
							Timestamp = DateTime.MinValue,
							TimeString = "PCA Store",
							Category = ActivityCategory.Bam,
							CategoryString = "⚡ PCA Store",
							Title = string.IsNullOrEmpty(fileName) ? valName : fileName,
							Detail = valName,
							IsSuspicious = isSusp,
							ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
						});
					}
				}
			}
			catch { }
		}

		private static string DecodeRot13(string input)
		{
			if (string.IsNullOrEmpty(input)) return input;
			char[] array = input.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				char c = array[i];
				if (c >= 'a' && c <= 'z')
				{
					array[i] = (char)('a' + (c - 'a' + 13) % 26);
				}
				else if (c >= 'A' && c <= 'Z')
				{
					array[i] = (char)('A' + (c - 'A' + 13) % 26);
				}
			}
			return new string(array);
		}
		private static void CollectBrowserActivity(List<ActivityItem> list)
		{
			try
			{
				foreach (KeyValuePair<string, string> item in WorldSeedAndChunkbaseForensicsViewerForm.GetBrowserHistoryTargets())
				{
					string bName = item.Key;
					string dbPath = item.Value;
					try
					{
						FileInfo fi = new FileInfo(dbPath);
						string text = WorldSeedAndChunkbaseForensicsViewerForm.ReadHistoryBounded(dbPath, 16777216, Encoding.UTF8);
						if (text.Length < 100) continue;

						// Scan for file downloads and cheat URLs
						MatchCollection matches = Regex.Matches(text, @"https?:\/\/[a-zA-Z0-9_\-\.\/]+(?:\.jar|\.zip|\.exe|\.rar|\.dll|celestial|expensive|wild|vape|doomsday|rich|akrien|blast\.hk|funpay)[a-zA-Z0-9_\-\.\/&=?%]*", RegexOptions.IgnoreCase);

						HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						foreach (Match m in matches)
						{
							string url = m.Value;
							if (seen.Contains(url) || url.Length > 250) continue;
							seen.Add(url);

							bool isSusp = CheatKeywordRegex.IsMatch(url);
							string fileName = Path.GetFileName(url.Split('?')[0]);

							list.Add(new ActivityItem
							{
								Timestamp = fi.LastWriteTime,
								TimeString = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
								Category = ActivityCategory.Browser,
								CategoryString = "🌐 " + bName,
								Title = string.IsNullOrEmpty(fileName) ? "Загрузка/URL" : fileName,
								Detail = url,
								IsSuspicious = isSusp,
								ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
							});
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private void ApplyFilterAndSearch()
		{
			listView.BeginUpdate();
			try
			{
				listView.Items.Clear();
				string query = searchBox.Text.Trim();
				bool onlySusp = chkOnlySuspicious.Checked;

				List<ListViewItem> listItems = new List<ListViewItem>();

				foreach (ActivityItem item in allItems)
				{
					if (currentCategory != ActivityCategory.All && item.Category != currentCategory) continue;
					if (onlySusp && !item.IsSuspicious) continue;

					if (!string.IsNullOrEmpty(query))
					{
						if (item.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							item.Detail.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							item.CategoryString.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							item.TimeString.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}

					ListViewItem lvi = new ListViewItem(item.Index.ToString());
					lvi.SubItems.Add(item.TimeString);
					lvi.SubItems.Add(item.CategoryString);
					lvi.SubItems.Add(item.Title);
					lvi.SubItems.Add(item.Detail);
					lvi.ForeColor = item.ForeColor;
					lvi.Tag = item;

					listItems.Add(lvi);
				}

				listView.Items.AddRange(listItems.ToArray());

				lblStatsTotal.Text = "Отобрано: " + listItems.Count + " из " + allItems.Count;
				lblStatsSuspicious.Text = "🚨 Подозрительных: " + allItems.Count(i => i.IsSuspicious);
				lblStatsCategory.Text = "Фильтр: " + GetCategoryName(currentCategory);
			}
			finally
			{
				listView.EndUpdate();
			}
		}

		private static string GetCategoryName(ActivityCategory cat)
		{
			switch (cat)
			{
				case ActivityCategory.Browser: return "Браузеры (Загрузки)";
				case ActivityCategory.Messengers: return "Мессенджеры (Telegram & Discord)";
				case ActivityCategory.PowerShell: return "PowerShell & RunMRU";
				case ActivityCategory.RecentFiles: return "Недавние файлы (.lnk)";
				case ActivityCategory.RecycleBin: return "Корзина ($Recycle.Bin)";
				case ActivityCategory.UsbStorage: return "USB накопители";
				case ActivityCategory.Bam: return "Запуски программ (BAM, UserAssist, MuiCache, PCA)";
				default: return "Все категории";
			}
		}

		private void OpenSelectedInExplorer()
		{
			if (listView.SelectedItems.Count == 0) return;
			ActivityItem item = listView.SelectedItems[0].Tag as ActivityItem;
			if (item == null) return;

			string path = item.Detail;
			if (path.Contains(" (") && path.EndsWith(" КБ)"))
			{
				path = path.Substring(0, path.LastIndexOf(" (")).Trim();
			}

			if (File.Exists(path))
			{
				PlatformHelper.SelectFileInFileManager(path);
			}
			else if (Directory.Exists(path))
			{
				PlatformHelper.OpenFolder(path);
			}
			else
			{
				// Try directory of path
				try
				{
					string dir = Path.GetDirectoryName(path);
					if (Directory.Exists(dir))
					{
						PlatformHelper.OpenFolder(dir);
						return;
					}
				}
				catch { }

				MessageBox.Show("Файл или путь не существует на диске (возможно, был удалён или перемещён):\n" + path, "Хронология активности", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private static void CollectMessengerActivity(List<ActivityItem> list)
		{
			try
			{
				// 1. Telegram Desktop Downloads
				string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string tgDownloads = Path.Combine(userProf, "Downloads", "Telegram Desktop");
				if (Directory.Exists(tgDownloads))
				{
					string[] tgFiles = Directory.GetFiles(tgDownloads, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string f in tgFiles)
					{
						try
						{
							FileInfo fi = new FileInfo(f);
							string fName = fi.Name;
							string ext = fi.Extension.ToLowerInvariant();
							bool isCheatExt = (ext == ".jar" || ext == ".exe" || ext == ".bat" || ext == ".dll" || ext == ".zip" || ext == ".rar");
							bool isCheatKw = CheatKeywordRegex.IsMatch(fName);
							bool isSusp = isCheatKw || (ext == ".jar" || ext == ".dll");

							list.Add(new ActivityItem
							{
								Timestamp = fi.LastWriteTime,
								TimeString = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
								Category = ActivityCategory.Messengers,
								CategoryString = "💬 Telegram Загрузка",
								Title = "Telegram: " + fName,
								Detail = f + string.Format(" ({0:N0} КБ)", fi.Length / 1024),
								IsSuspicious = isSusp,
								ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
							});
						}
						catch { }
					}
				}

				// 2. Discord Forensics delegation (reuses centralized scanner)
				try
				{
					foreach (DiscordForensicEvidenceItem df in DiscordRichPresenceAndRpcForensicsViewerForm.PerformDiscordForensicScan())
					{
						if (df == null) continue;
						list.Add(new ActivityItem
						{
							Timestamp = df.Timestamp,
							TimeString = df.Timestamp.ToString("dd.MM.yyyy HH:mm"),
							Category = ActivityCategory.Messengers,
							CategoryString = "💬 Discord: " + df.Category,
							Title = df.DetectedArtifact ?? "Discord",
							Detail = df.SourcePath ?? df.Verdict ?? "",
							IsSuspicious = df.IsViolation,
							ForeColor = df.IsViolation ? Ui.AccentRed : Ui.TextPrimary
						});
					}
				}
				catch { }
			}
			catch { }
		}

		private void CopySuspiciousReport()
		{
			var suspicious = allItems.Where(i => i.IsSuspicious).ToList();
			if (suspicious.Count == 0)
			{
				MessageBox.Show("В хронологии активности не найдено подозрительных улик.", "Хронология активности", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ХРОНОЛОГИЯ АКТИВНОСТИ И ЗАГРУЗОК — ПОДОЗРИТЕЛЬНЫЕ УЛИКИ ===");
			sb.AppendLine("Время формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего подозрительных записей: " + suspicious.Count);
			sb.AppendLine(new string('-', 75));

			foreach (var s in suspicious)
			{
				sb.AppendLine(string.Format("[{0}] {1,-18} | {2} -> {3}", s.TimeString, s.CategoryString, s.Title, s.Detail));
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Найдено " + suspicious.Count + " подозрительных записей! Отчёт скопирован в буфер обмена.", "MC Cheat Checker", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
