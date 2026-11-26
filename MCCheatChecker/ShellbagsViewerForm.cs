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
	public class ShellbagsViewerForm : Form
	{
		private enum ShellbagsFilter
		{
			All,
			CheatsOnly,
			RemovableDrives,
			DeletedOrMissing
		}

		private class ShellbagItem
		{
			public int Index;
			public string FullPath;
			public string FolderName;
			public string DriveType;
			public bool ExistsOnDisk;
			public bool IsCheatFolder;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private ShellbagsFilter currentFilter = ShellbagsFilter.All;
		private List<ShellbagItem> allItems = new List<ShellbagItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailFolder;
		private Label lblDetailPath;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterRemovable;
		private DarkButton btnFilterDeleted;

		private static readonly Regex CheatFolderPattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|boze|abyss|killaura|velocity|aimbot|cheat|hacks?|софт|читы|кликер|инжект|loader|exloader|cleaner|wiper|reghax)",
			RegexOptions.Compiled);

		public ShellbagsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadShellbagsAsync();
		}

		private void InitializeComponent()
		{
			Text = "📁 Инспектор истории папок ShellBags (BagMRU Forensics & Deleted USB Folders)";
			Size = new Size(1180, 740);
			MinimumSize = new Size(920, 560);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(12, 10, 12, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "📁  ИНСПЕКТОР ИСТОРИИ ПАПОК SHELLBAGS (BagMRU Forensics & USB Artifacts)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Рекурсивный разбор дерева BagMRU: извлечение истории открытия папок в Проводнике...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование реестра Shellbags...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(885, 8);
			btnRefresh.Size = new Size(135, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				LoadShellbagsAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnBan = new DarkButton();
			btnBan.Text = "🔨 Скопировать /ban 5.01";
			btnBan.Location = new Point(1026, 8);
			btnBan.Size = new Size(165, 26);
			btnBan.BackColor = Color.FromArgb(183, 28, 28);
			btnBan.ForeColor = Color.White;
			btnBan.Cursor = Cursors.Hand;
			btnBan.Click += delegate
			{
				CopyBanCommand();
			};
			topPanel.Controls.Add(btnBan);

			// Filter Row
			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1140, 50);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все папки (0)", ShellbagsFilter.All, 110);
			btnFilterCheats = CreateFilterButton("🚨 Папки читов (0)", ShellbagsFilter.CheatsOnly, 140);
			btnFilterRemovable = CreateFilterButton("🔌 Флешки / USB (0)", ShellbagsFilter.RemovableDrives, 140);
			btnFilterDeleted = CreateFilterButton("🗑️ Удалённые пути (0)", ShellbagsFilter.DeletedOrMissing, 150);

			int curX = 0;
			DarkButton[] filterBtns = { btnFilterAll, btnFilterCheats, btnFilterRemovable, btnFilterDeleted };
			foreach (DarkButton b in filterBtns)
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
			searchBox.Location = new Point(curX + 54, 2);
			searchBox.Size = new Size(220, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 278, 2);
			btnClear.Size = new Size(24, 22);
			btnClear.BackColor = Ui.SurfaceAlt;
			btnClear.ForeColor = Ui.TextMuted;
			btnClear.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			navRow.Controls.Add(btnClear);

			topPanel.Controls.Add(navRow);
			Controls.Add(topPanel);

			// SplitContainer: Left = ListView, Right = Details
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Vertical;
			split.SplitterDistance = 690;
			split.SplitterWidth = 5;
			split.BackColor = Ui.Border;

			// Left ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontMono(8.5f);
			listView.Columns.Add("!", 35, HorizontalAlignment.Center);
			listView.Columns.Add("Восстановленный путь папки", 340, HorizontalAlignment.Left);
			listView.Columns.Add("Имя каталога", 140, HorizontalAlignment.Left);
			listView.Columns.Add("Тип диска", 85, HorizontalAlignment.Center);
			listView.Columns.Add("Наличие на диске", 120, HorizontalAlignment.Left);
			listView.Columns.Add("Вердикт", 180, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdateShellbagDetails();
			};

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();

			ToolStripMenuItem copyPathItem = new ToolStripMenuItem("📋  Скопировать путь к папке");
			copyPathItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					Clipboard.SetText(listView.SelectedItems[0].SubItems[1].Text);
				}
			};

			ToolStripMenuItem copyRowItem = new ToolStripMenuItem("📋  Скопировать строку улик");
			copyRowItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					ListViewItem it = listView.SelectedItems[0];
					Clipboard.SetText(string.Format("{0} | {1} | {2} | {3} | {4}",
						it.SubItems[1].Text, it.SubItems[2].Text, it.SubItems[3].Text, it.SubItems[4].Text, it.SubItems[5].Text));
				}
			};

			ToolStripMenuItem openExplorerItem = new ToolStripMenuItem("📁  Открыть папку в Проводнике (если доступна)");
			openExplorerItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					string path = listView.SelectedItems[0].SubItems[1].Text;
					if (Directory.Exists(path))
					{
						PlatformHelper.OpenFolder(path);
					}
					else
					{
						MessageBox.Show("Каталог не найден на диске (возможно, был удалён или отключена флешка): " + path, "ShellBags", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			};

			ToolStripMenuItem copyAllReportItem = new ToolStripMenuItem("📋  Скопировать полный протокол ShellBags");
			copyAllReportItem.Click += delegate
			{
				CopyShellbagsReport();
			};

			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Сгенерировать /ban за чит-папку (5.01)");
			copyBanItem.Click += delegate
			{
				CopyBanCommand();
			};

			ctx.Items.Add(copyPathItem);
			ctx.Items.Add(copyRowItem);
			ctx.Items.Add(openExplorerItem);
			ctx.Items.Add(copyAllReportItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(copyBanItem);
			listView.ContextMenuStrip = ctx;

			split.Panel1.Controls.Add(listView);

			// Right Details Panel
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;
			rightPanel.Padding = new Padding(12);

			Label lblDetHeader = new Label();
			lblDetHeader.Dock = DockStyle.Top;
			lblDetHeader.Height = 24;
			lblDetHeader.Font = Ui.FontTitle(9.5f, true);
			lblDetHeader.ForeColor = Ui.AccentTeal;
			lblDetHeader.Text = "🔍 Криминалистический анализ папки ShellBags";
			rightPanel.Controls.Add(lblDetHeader);

			lblDetailFolder = new Label();
			lblDetailFolder.Dock = DockStyle.Top;
			lblDetailFolder.Height = 22;
			lblDetailFolder.Font = Ui.FontBody(8.5f, true);
			lblDetailFolder.ForeColor = Ui.TextPrimary;
			lblDetailFolder.Text = "Каталог: —";
			rightPanel.Controls.Add(lblDetailFolder);

			lblDetailPath = new Label();
			lblDetailPath.Dock = DockStyle.Top;
			lblDetailPath.Height = 22;
			lblDetailPath.Font = Ui.FontBody(8.5f);
			lblDetailPath.ForeColor = Ui.TextSecondary;
			lblDetailPath.Text = "Полный путь: —";
			rightPanel.Controls.Add(lblDetailPath);

			lblDetailStatus = new Label();
			lblDetailStatus.Dock = DockStyle.Top;
			lblDetailStatus.Height = 22;
			lblDetailStatus.Font = Ui.FontBody(8.5f);
			lblDetailStatus.ForeColor = Ui.TextMuted;
			lblDetailStatus.Text = "Статус на диске: —";
			rightPanel.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Dock = DockStyle.Top;
			lblDetailRule.Height = 40;
			lblDetailRule.Font = Ui.FontBody(8.5f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Text = "Регламент: —";
			rightPanel.Controls.Add(lblDetailRule);

			Label lblDescHeader = new Label();
			lblDescHeader.Dock = DockStyle.Top;
			lblDescHeader.Height = 22;
			lblDescHeader.Font = Ui.FontBody(8f, true);
			lblDescHeader.ForeColor = Ui.TextMuted;
			lblDescHeader.Text = "📄 Криминалистическое значение улики:";
			rightPanel.Controls.Add(lblDescHeader);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Dock = DockStyle.Fill;
			txtDetailDescription.Font = Ui.FontMono(8.5f);
			txtDetailDescription.ReadOnly = true;
			txtDetailDescription.WordWrap = true;
			txtDetailDescription.ScrollBars = ScrollBars.Vertical;
			rightPanel.Controls.Add(txtDetailDescription);
			txtDetailDescription.BringToFront();

			split.Panel2.Controls.Add(rightPanel);
			Controls.Add(split);

			// Bottom Status Strip
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 36;
			bottomPanel.BackColor = Ui.SurfaceAlt;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			Label lblShellbagsHelp = new Label();
			lblShellbagsHelp.Text = "ℹ️ Артефакты ShellBags (BagMRU) доказывают просмотр папок в Проводнике, даже если файлы удалили или флешку отключили.";
			lblShellbagsHelp.Font = Ui.FontBody(8f);
			lblShellbagsHelp.ForeColor = Ui.AccentGreen;
			lblShellbagsHelp.Location = new Point(12, 10);
			lblShellbagsHelp.AutoSize = true;
			bottomPanel.Controls.Add(lblShellbagsHelp);

			DarkButton btnCopyReport = new DarkButton();
			btnCopyReport.Text = "📋 Скопировать протокол ShellBags";
			btnCopyReport.Size = new Size(220, 24);
			btnCopyReport.Location = new Point(930, 6);
			btnCopyReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyReport.ForeColor = Color.White;
			btnCopyReport.Cursor = Cursors.Hand;
			btnCopyReport.Click += delegate
			{
				CopyShellbagsReport();
			};
			bottomPanel.Controls.Add(btnCopyReport);

			Controls.Add(bottomPanel);
		}

		private DarkButton CreateFilterButton(string text, ShellbagsFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 24);
			btn.Font = Ui.FontBody(8f);
			btn.Cursor = Cursors.Hand;
			btn.BackColor = (filter == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.ForeColor = (filter == currentFilter) ? Color.Black : Ui.TextPrimary;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtons();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateFilterButtons()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterRemovable, btnFilterDeleted };
			ShellbagsFilter[] filters = { ShellbagsFilter.All, ShellbagsFilter.CheatsOnly, ShellbagsFilter.RemovableDrives, ShellbagsFilter.DeletedOrMissing };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (filters[i] == currentFilter);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].ForeColor = active ? Color.Black : Ui.TextPrimary;
			}
		}

		private void LoadShellbagsAsync()
		{
			lblStatusPill.Text = "⏳ Чтение BagMRU...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblHeaderStatus.Text = "Парсинг реестра BagMRU в HKCU...";

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<ShellbagItem> items = new List<ShellbagItem>();
				HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				string[] bagRoots = {
					@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU",
					@"Software\Microsoft\Windows\Shell\BagMRU",
					@"Software\Microsoft\Windows\ShellNoRoam\BagMRU"
				};

				foreach (string rootPath in bagRoots)
				{
					using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(rootPath))
					{
						if (rKey != null)
						{
							TraverseBagMru(rKey, "", items, seenPaths, 0);
						}
					}
				}

				// Sort suspicious first, then by path
				items.Sort((a, b) =>
				{
					if (a.IsCheatFolder != b.IsCheatFolder) return b.IsCheatFolder.CompareTo(a.IsCheatFolder);
					return string.Compare(a.FullPath, b.FullPath, StringComparison.OrdinalIgnoreCase);
				});

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
					lblStatusPill.Text = "❌ Ошибка: " + e.Error.Message;
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				allItems = e.Result as List<ShellbagItem> ?? new List<ShellbagItem>();

				int total = allItems.Count;
				int cheats = allItems.Count(i => i.IsCheatFolder);
				int removable = allItems.Count(i => i.DriveType.Contains("USB") || i.DriveType.Contains("Сменный"));
				int missing = allItems.Count(i => !i.ExistsOnDisk);

				btnFilterAll.Text = "🔍 Все папки (" + total + ")";
				btnFilterCheats.Text = "🚨 Папки читов (" + cheats + ")";
				btnFilterRemovable.Text = "🔌 Флешки / USB (" + removable + ")";
				btnFilterDeleted.Text = "🗑️ Удалённые пути (" + missing + ")";

				if (cheats > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ ПАПКИ ЧИТОВ В ИСТОРИИ ({0} находок)", cheats);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ В истории Проводника чит-папок не обнаружено";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}

				lblHeaderStatus.Text = string.Format("Анализ завершён: найдено {0:N0} открывавшихся папок (папок читов 5.01: {1}, на USB: {2})", total, cheats, removable);

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private static void TraverseBagMru(RegistryKey key, string currentPath, List<ShellbagItem> list, HashSet<string> seen, int depth)
		{
			if (key == null || depth > 8) return;

			try
			{
				foreach (string valName in key.GetValueNames())
				{
					if (valName == "MRUListEx" || valName == "NodeSlot") continue;
					byte[] bytes = key.GetValue(valName) as byte[];
					if (bytes == null || bytes.Length < 6) continue;

					string folderName = ExtractFolderNameFromBytes(bytes);
					if (string.IsNullOrEmpty(folderName) || folderName.Length < 2) continue;

					string combinedPath;
					if (string.IsNullOrEmpty(currentPath))
					{
						combinedPath = folderName;
					}
					else
					{
						combinedPath = currentPath.EndsWith("\\") ? (currentPath + folderName) : (currentPath + "\\" + folderName);
					}

					if (combinedPath.Contains("\\") && seen.Add(combinedPath))
					{
						bool isCheat = CheatFolderPattern.IsMatch(combinedPath) || CheatFolderPattern.IsMatch(folderName);
						bool exists = Directory.Exists(combinedPath);

						string drive = "Локальный (C:)";
						if (combinedPath.Length >= 2 && combinedPath[1] == ':')
						{
							char dLetter = char.ToUpperInvariant(combinedPath[0]);
							if (dLetter != 'C') drive = "USB / Диск " + dLetter + ":";
						}

						list.Add(new ShellbagItem
						{
							FullPath = combinedPath,
							FolderName = folderName,
							DriveType = drive,
							ExistsOnDisk = exists,
							IsCheatFolder = isCheat,
							ThreatReason = isCheat ? "🚨 ЧИТ-КАТАЛОГ: " + folderName : (exists ? "Штатный каталог" : "Удалённый путь"),
							Verdict = isCheat ? "🚨 5.01 Чит-папка" : (exists ? "✔ Существует" : "🗑️ Удалена / Нет флешки"),
							ForeColor = isCheat ? Ui.AccentRed : (drive.Contains("USB") ? Ui.AccentOrange : Ui.TextPrimary)
						});
					}
				}

				foreach (string subName in key.GetSubKeyNames())
				{
					using (RegistryKey subKey = key.OpenSubKey(subName))
					{
						if (subKey != null)
						{
							// Find corresponding folder name from value
							string nextPath = currentPath;
							byte[] bVal = key.GetValue(subName) as byte[];
							if (bVal != null)
							{
								string fName = ExtractFolderNameFromBytes(bVal);
								if (!string.IsNullOrEmpty(fName))
								{
									nextPath = string.IsNullOrEmpty(currentPath) ? fName : (currentPath.EndsWith("\\") ? currentPath + fName : currentPath + "\\" + fName);
								}
							}
							TraverseBagMru(subKey, nextPath, list, seen, depth + 1);
						}
					}
				}
			}
			catch { }
		}

		private static string ExtractFolderNameFromBytes(byte[] bytes)
		{
			if (bytes == null || bytes.Length < 6) return null;

			// Check for drive specification: 0x23 / 0x2F / e.g. "C:\"
			for (int i = 0; i < bytes.Length - 3; i++)
			{
				if (bytes[i + 1] == ':' && bytes[i + 2] == '\\' && char.IsLetter((char)bytes[i]))
				{
					return ((char)bytes[i]).ToString().ToUpperInvariant() + ":\\";
				}
			}

			// Scan for null-terminated UTF-16 string
			for (int i = 0; i < bytes.Length - 4; i += 2)
			{
				if (bytes[i] >= 0x20 && bytes[i] < 0x7F && bytes[i + 1] == 0 &&
					bytes[i + 2] >= 0x20 && bytes[i + 2] < 0x7F && bytes[i + 3] == 0)
				{
					int end = i;
					while (end < bytes.Length - 1 && !(bytes[end] == 0 && bytes[end + 1] == 0))
					{
						end += 2;
					}
					int strLen = end - i;
					if (strLen >= 4 && strLen < 260)
					{
						try
						{
							string s = Encoding.Unicode.GetString(bytes, i, strLen).Trim();
							if (IsValidFolderName(s)) return s;
						}
						catch { }
					}
				}
			}

			// Scan for ASCII string
			for (int i = 0; i < bytes.Length - 3; i++)
			{
				if (bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] >= 0x20 && bytes[i + 1] <= 0x7E && bytes[i + 2] >= 0x20 && bytes[i + 2] <= 0x7E)
				{
					int end = i;
					while (end < bytes.Length && bytes[end] >= 0x20 && bytes[end] <= 0x7E)
					{
						end++;
					}
					int len = end - i;
					if (len >= 3 && len < 128)
					{
						string s = Encoding.ASCII.GetString(bytes, i, len).Trim();
						if (IsValidFolderName(s)) return s;
					}
				}
			}

			return null;
		}

		private static bool IsValidFolderName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return false;
			if (name.Contains("<") || name.Contains(">") || name.Contains("\"") || name.Contains("|") || name.Contains("?")) return false;
			if (name.StartsWith("{") && name.EndsWith("}")) return false; // Skip GUIDs
			return true;
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = allItems.Where(it =>
			{
				switch (currentFilter)
				{
					case ShellbagsFilter.CheatsOnly:
						if (!it.IsCheatFolder) return false;
						break;
					case ShellbagsFilter.RemovableDrives:
						if (!it.DriveType.Contains("USB") && !it.DriveType.Contains("Сменный")) return false;
						break;
					case ShellbagsFilter.DeletedOrMissing:
						if (it.ExistsOnDisk) return false;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = it.FullPath.ToLowerInvariant().Contains(query) ||
						it.FolderName.ToLowerInvariant().Contains(query) ||
						it.DriveType.ToLowerInvariant().Contains(query) ||
						it.ThreatReason.ToLowerInvariant().Contains(query);
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (ShellbagItem item in filtered)
			{
				string icon = item.IsCheatFolder ? "🚨" : (item.DriveType.Contains("USB") ? "🔌" : (item.ExistsOnDisk ? "✔" : "🗑️"));
				ListViewItem lvi = new ListViewItem(icon);
				lvi.SubItems.Add(item.FullPath);
				lvi.SubItems.Add(item.FolderName);
				lvi.SubItems.Add(item.DriveType);
				lvi.SubItems.Add(item.ExistsOnDisk ? "На диске" : "Удалена / Флешка");
				lvi.SubItems.Add(item.Verdict);
				lvi.Tag = item;
				lvi.ForeColor = item.ForeColor;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearShellbagDetails();
			}
		}

		private void UpdateShellbagDetails()
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearShellbagDetails();
				return;
			}

			ShellbagItem item = listView.SelectedItems[0].Tag as ShellbagItem;
			if (item == null) return;

			lblDetailFolder.Text = "Каталог: " + item.FolderName;
			lblDetailFolder.ForeColor = item.IsCheatFolder ? Ui.AccentRed : Ui.AccentTeal;

			lblDetailPath.Text = "Полный путь: " + item.FullPath;
			lblDetailStatus.Text = "Тип накопителя: " + item.DriveType + "  |  Доступность: " + (item.ExistsOnDisk ? "Доступен на диске" : "Удалён или отключена флешка");

			if (item.IsCheatFolder)
			{
				lblDetailRule.Text = "⚖️ Нарушение: Пункт 5.01 (Хранение / запуск чит-модификаций в Проводнике)\nРекомендуемое наказание: Блокировка от 30 дней.";
				lblDetailRule.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblDetailRule.Text = "✔ Нарушений не обнаружено: Обычный каталог файловой системы.";
				lblDetailRule.ForeColor = Ui.AccentGreen;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛИ ЗАПИСИ SHELLBAGS (BAGMRU) ===");
			sb.AppendLine("Восстановленный путь: " + item.FullPath);
			sb.AppendLine("Имя каталога: " + item.FolderName);
			sb.AppendLine("Носитель данных: " + item.DriveType);
			sb.AppendLine("Наличие на файловой системе сейчас: " + (item.ExistsOnDisk ? "ДА (Каталог существует)" : "НЕТ (Удалён перед проверкой или отключена USB-флешка)"));
			sb.AppendLine("Классификация: " + item.ThreatReason);
			sb.AppendLine();
			sb.AppendLine("--- КРИМИНАЛИСТИЧЕСКАЯ СПРАВКА ---");

			if (item.IsCheatFolder)
			{
				sb.AppendLine("Данный каталог был открыт подозреваемым в Проводнике Windows.");
				sb.AppendLine("Запись в реестре ShellBags (BagMRU) не может возникнуть случайно или без ручного входа в папку.");
				sb.AppendLine("Если каталог находился на сменном носителе (D:, E:, F:), это доказывает запуск софта с флешки.");
			}
			else
			{
				sb.AppendLine("Штатный каталог пользователя или программный каталог Windows.");
			}

			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearShellbagDetails()
		{
			lblDetailFolder.Text = "Каталог: —";
			lblDetailPath.Text = "Полный путь: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Регламент: —";
			txtDetailDescription.Text = "Выберите папку в списке слева для детального анализа.";
		}

		private void CopyBanCommand()
		{
			var cheats = allItems.Where(i => i.IsCheatFolder).ToList();
			string fName = "чит-папка";
			if (cheats.Count > 0)
			{
				fName = cheats[0].FolderName;
			}

			string cmd = string.Format("/ban {0} 30d 5.01 (Папка чита в истории ShellBags: {1})", targetPlayerName, fName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена!\n\n" + cmd, "Генератор бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyShellbagsReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("          ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОГО АНАЛИЗА SHELLBAGS (SCREENSHARE)          ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Дата и время: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего записей ShellBags: " + allItems.Count);
			sb.AppendLine("Обнаружено папок читов: " + allItems.Count(i => i.IsCheatFolder));
			sb.AppendLine("Папок на сменных USB-накопителях: " + allItems.Count(i => i.DriveType.Contains("USB")));
			sb.AppendLine();

			sb.AppendLine("--- НАЙДЕННЫЕ ПАПКИ ЧИТОВ В ИСТОРИИ ПРОСМОТРА ---");
			var cheatList = allItems.Where(i => i.IsCheatFolder).ToList();
			if (cheatList.Count == 0)
			{
				sb.AppendLine("Папок читов в истории Проводника (ShellBags) НЕ обнаружено.");
			}
			else
			{
				foreach (var it in cheatList)
				{
					sb.AppendLine(string.Format("[{0}] Путь: {1} | Диск: {2} | Наличие: {3}",
						it.Verdict, it.FullPath, it.DriveType, it.ExistsOnDisk ? "Существует" : "Удалена/USB отключен"));
				}
			}

			sb.AppendLine();
			sb.AppendLine("--- ПАПКИ НА СМЕННЫХ НОСИТЕЛЯХ (USB / ФЛЕШКИ) ---");
			foreach (var it in allItems.Where(i => i.DriveType.Contains("USB") && !i.IsCheatFolder))
			{
				sb.AppendLine(string.Format("Диск {0,-12} | Путь: {1}", it.DriveType, it.FullPath));
			}

			sb.AppendLine();
			sb.AppendLine("Регламент: Факт открытия папок читов доказывает использование стороннего софта (п. 5.01).");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол ShellBags скопирован в буфер обмена!", "ShellBags", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
