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
	public class ShimcacheViewerForm : Form
	{
		private enum ShimcacheFilter
		{
			All,
			CheatsOnly,
			CleanersOnly,
			RemovableDrives,
			DeletedFilesOnly,
			WhitelistOnly
		}

		private class ShimcacheEntryItem
		{
			public int Index;
			public string ExeName;
			public string FullPath;
			public string DriveLetter;
			public bool ExistsOnDisk;
			public bool IsRemovable;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsWhitelisted;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		private string targetPlayerName;
		private ShimcacheFilter currentFilter = ShimcacheFilter.All;
		private List<ShimcacheEntryItem> allEntries = new List<ShimcacheEntryItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailExe;
		private Label lblDetailPath;
		private Label lblDetailDiskStatus;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterCleaners;
		private DarkButton btnFilterRemovable;
		private DarkButton btnFilterDeleted;
		private DarkButton btnFilterWhitelist;

		private static readonly Regex CheatExePattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject|killaura|velocity|aimbot|autoclicker|cleaner|wiper|reghax)",
			RegexOptions.Compiled);

		public ShimcacheViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadShimcacheAsync();
		}

		private void InitializeComponent()
		{
			Text = "🗄️ Инспектор кэша совместимости ядра AppCompatCache (ShimCache Forensics)";
			Size = new Size(1220, 780);
			MinimumSize = new Size(960, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 114;
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
			lblHeaderTitle.Text = "🗄️  ИНСПЕКТОР ЯДРА APPCOMPATCACHE (ShimCache Execution Artifacts)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Разбор реестра Session Manager\\AppCompatCache: история до 1024 запущенных файлов, в т.ч. удалённых и с флешек...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Чтение данных AppCompatCache...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(1015, 8);
			btnRefresh.Size = new Size(150, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { LoadShimcacheAsync(); };
			topPanel.Controls.Add(btnRefresh);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все записи (0)", ShimcacheFilter.All, 125);
			btnFilterCheats = CreateFilterButton("🚨 Читы & Инжекторы (0)", ShimcacheFilter.CheatsOnly, 180);
			btnFilterCleaners = CreateFilterButton("🧹 Клинеры 5.08 (0)", ShimcacheFilter.CleanersOnly, 140);
			btnFilterRemovable = CreateFilterButton("🔌 USB / Съемные (0)", ShimcacheFilter.RemovableDrives, 150);
			btnFilterDeleted = CreateFilterButton("🗑️ Удалённые с диска (0)", ShimcacheFilter.DeletedFilesOnly, 165);
			btnFilterWhitelist = CreateFilterButton("✔ Whitelist (0)", ShimcacheFilter.WhitelistOnly, 120);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterRemovable, btnFilterDeleted, btnFilterWhitelist };
			foreach (DarkButton b in filterButtons)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 6;
			}
			topPanel.Controls.Add(navRow);

			Panel searchRow = new Panel();
			searchRow.Location = new Point(12, 85);
			searchRow.Size = new Size(1180, 24);
			searchRow.BackColor = Color.Transparent;

			Label lblSearch = new Label();
			lblSearch.Text = "Быстрый поиск:";
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(0, 3);
			lblSearch.AutoSize = true;
			searchRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(105, 0);
			searchBox.Size = new Size(360, 23);
			searchBox.Font = Ui.FontBody(8.5f);
			searchBox.TextChanged += delegate { ApplyFilters(); };
			searchRow.Controls.Add(searchBox);

			DarkButton btnClearSearch = new DarkButton();
			btnClearSearch.Text = "✕ Очистить";
			btnClearSearch.Location = new Point(472, 0);
			btnClearSearch.Size = new Size(85, 23);
			btnClearSearch.BackColor = Ui.SurfaceAlt;
			btnClearSearch.Font = Ui.FontBody(8f);
			btnClearSearch.Cursor = Cursors.Hand;
			btnClearSearch.Click += delegate { searchBox.Text = ""; };
			searchRow.Controls.Add(btnClearSearch);

			Label lblHint = new Label();
			lblHint.Text = "💡 Подсказка: Записи ShimCache сохраняются даже после удаления чита или извлечения флешки до перезагрузки ПК.";
			lblHint.Font = Ui.FontBody(8f);
			lblHint.ForeColor = Ui.TextMuted;
			lblHint.Location = new Point(570, 4);
			lblHint.AutoSize = true;
			searchRow.Controls.Add(lblHint);

			topPanel.Controls.Add(searchRow);
			Controls.Add(topPanel);

			// SplitContainer for ListView and Detail pane
			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.Size = new Size(1180, 600);
			try { split.SplitterDistance = 420; } catch { }

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.MultiSelect = false;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

			listView.Columns.Add("Вердикт / Статус", 185);
			listView.Columns.Add("Исполняемый файл (.exe)", 210);
			listView.Columns.Add("Статус на диске", 165);
			listView.Columns.Add("Полный путь к файлу", 430);
			listView.Columns.Add("Диск", 65);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details & Ban Action
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 10, 12, 10);

			lblDetailExe = new Label();
			lblDetailExe.Text = "Выберите запись для анализа...";
			lblDetailExe.Font = Ui.FontTitle(10f, true);
			lblDetailExe.ForeColor = Ui.TextPrimary;
			lblDetailExe.Location = new Point(12, 8);
			lblDetailExe.AutoSize = true;
			detailContainer.Controls.Add(lblDetailExe);

			lblDetailPath = new Label();
			lblDetailPath.Text = "Полный путь: —";
			lblDetailPath.Font = Ui.FontBody(8.5f);
			lblDetailPath.ForeColor = Ui.TextSecondary;
			lblDetailPath.Location = new Point(12, 30);
			lblDetailPath.AutoSize = true;
			detailContainer.Controls.Add(lblDetailPath);

			lblDetailDiskStatus = new Label();
			lblDetailDiskStatus.Text = "Состояние файла: —";
			lblDetailDiskStatus.Font = Ui.FontBody(8.5f);
			lblDetailDiskStatus.ForeColor = Ui.TextSecondary;
			lblDetailDiskStatus.Location = new Point(12, 50);
			lblDetailDiskStatus.AutoSize = true;
			detailContainer.Controls.Add(lblDetailDiskStatus);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Вердикт: —";
			lblDetailStatus.Font = Ui.FontBody(8.5f, true);
			lblDetailStatus.ForeColor = Ui.TextSecondary;
			lblDetailStatus.Location = new Point(12, 70);
			lblDetailStatus.AutoSize = true;
			detailContainer.Controls.Add(lblDetailStatus);

			lblDetailRule = new Label();
			lblDetailRule.Text = "Пункт регламента: —";
			lblDetailRule.Font = Ui.FontBody(8.5f);
			lblDetailRule.ForeColor = Ui.AccentTeal;
			lblDetailRule.Location = new Point(12, 90);
			lblDetailRule.AutoSize = true;
			detailContainer.Controls.Add(lblDetailRule);

			txtDetailDescription = new DarkTextBoxMultiline();
			txtDetailDescription.Location = new Point(12, 114);
			txtDetailDescription.Size = new Size(820, 85);
			txtDetailDescription.Font = Ui.FontMono(8f);
			txtDetailDescription.ReadOnly = true;
			detailContainer.Controls.Add(txtDetailDescription);

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "📋 Скопировать /ban";
			btnCopyBan.Location = new Point(850, 114);
			btnCopyBan.Size = new Size(160, 27);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate { CopyBanCommand(); };
			detailContainer.Controls.Add(btnCopyBan);

			DarkButton btnCopyPath = new DarkButton();
			btnCopyPath.Text = "📄 Скопировать путь";
			btnCopyPath.Location = new Point(850, 145);
			btnCopyPath.Size = new Size(160, 27);
			btnCopyPath.BackColor = Ui.SurfaceAlt;
			btnCopyPath.Font = Ui.FontBody(8.5f, true);
			btnCopyPath.Cursor = Cursors.Hand;
			btnCopyPath.Click += delegate
			{
				ShimcacheEntryItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FullPath))
				{
					Clipboard.SetText(sel.FullPath);
					MessageBox.Show("Путь скопирован:\n" + sel.FullPath, "ShimCache", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			detailContainer.Controls.Add(btnCopyPath);

			DarkButton btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Показать в папке";
			btnOpenFolder.Location = new Point(1020, 114);
			btnOpenFolder.Size = new Size(160, 27);
			btnOpenFolder.BackColor = Ui.SurfaceAlt;
			btnOpenFolder.Font = Ui.FontBody(8.5f, true);
			btnOpenFolder.Cursor = Cursors.Hand;
			btnOpenFolder.Click += delegate
			{
				ShimcacheEntryItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FullPath))
				{
					if (File.Exists(sel.FullPath))
					{
						try { Process.Start("explorer.exe", "/select,\"" + sel.FullPath + "\""); } catch { }
					}
					else
					{
						string dir = Path.GetDirectoryName(sel.FullPath);
						if (Directory.Exists(dir))
						{
							try { Process.Start("explorer.exe", dir); } catch { }
						}
						else
						{
							MessageBox.Show("Файл или содержащая папка не существует на диске (файл удалён или USB отключен).", "ShimCache", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					}
				}
			};
			detailContainer.Controls.Add(btnOpenFolder);

			DarkButton btnVt = new DarkButton();
			btnVt.Text = "🔍 VirusTotal";
			btnVt.Location = new Point(1020, 145);
			btnVt.Size = new Size(160, 27);
			btnVt.BackColor = Ui.SurfaceAlt;
			btnVt.Font = Ui.FontBody(8.5f, true);
			btnVt.Cursor = Cursors.Hand;
			btnVt.Click += delegate
			{
				ShimcacheEntryItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.ExeName))
				{
					try { Process.Start("https://www.virustotal.com/gui/search/" + Uri.EscapeDataString(sel.ExeName)); } catch { }
				}
			};
			detailContainer.Controls.Add(btnVt);

			detailContainer.Resize += delegate
			{
				int w = detailContainer.ClientSize.Width - 24;
				if (w > 600)
				{
					int descW = Math.Max(300, w - 360);
					txtDetailDescription.Width = descW;
					btnCopyBan.Location = new Point(descW + 24, 114);
					btnCopyPath.Location = new Point(descW + 24, 145);
					btnOpenFolder.Location = new Point(descW + 190, 114);
					btnVt.Location = new Point(descW + 190, 145);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);
		}

		private DarkButton CreateFilterButton(string text, ShimcacheFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.Font = Ui.FontBody(8f, filter == currentFilter);
			btn.Cursor = Cursors.Hand;
			btn.Click += delegate
			{
				currentFilter = filter;
				UpdateFilterButtonsVisual();
				ApplyFilters();
			};
			return btn;
		}

		private void UpdateFilterButtonsVisual()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCheats, btnFilterCleaners, btnFilterRemovable, btnFilterDeleted, btnFilterWhitelist };
			ShimcacheFilter[] filters = { ShimcacheFilter.All, ShimcacheFilter.CheatsOnly, ShimcacheFilter.CleanersOnly, ShimcacheFilter.RemovableDrives, ShimcacheFilter.DeletedFilesOnly, ShimcacheFilter.WhitelistOnly };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void LoadShimcacheAsync()
		{
			lblStatusPill.Text = "⏳ Чтение реестра AppCompatCache...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allEntries.Clear();
			listView.Items.Clear();

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<ShimcacheEntryItem> items = ParseSystemAppCompatCache();
				e.Result = items;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<ShimcacheEntryItem>)
				{
					allEntries = (List<ShimcacheEntryItem>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static List<ShimcacheEntryItem> ParseSystemAppCompatCache()
		{
			List<ShimcacheEntryItem> list = new List<ShimcacheEntryItem>();
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache"))
				{
					if (key == null) return list;

					byte[] data = key.GetValue("AppCompatCache") as byte[];
					if (data == null || data.Length < 128) return list;

					// Extract Unicode paths from binary stream
					StringBuilder sb = new StringBuilder();
					int index = 1;

					for (int i = 0; i < data.Length - 1; i += 2)
					{
						char c = (char)BitConverter.ToUInt16(data, i);
						if (c >= 32 && c <= 126 || c >= 0x0400 && c <= 0x04FF)
						{
							sb.Append(c);
						}
						else
						{
							if (sb.Length >= 4)
							{
								string raw = sb.ToString().Trim();
								if (raw.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || raw.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || raw.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
								{
									string clean = CleanShimPath(raw);
									if (!string.IsNullOrEmpty(clean) && !seen.Contains(clean))
									{
										seen.Add(clean);
										ShimcacheEntryItem item = new ShimcacheEntryItem();
										item.Index = index++;
										item.FullPath = clean;
										item.ExeName = Path.GetFileName(clean);

										string drive = "";
										if (clean.Length >= 2 && clean[1] == ':') drive = clean.Substring(0, 2).ToUpperInvariant();
										item.DriveLetter = string.IsNullOrEmpty(drive) ? "—" : drive;
										item.IsRemovable = !string.IsNullOrEmpty(drive) && drive != "C:";

										try { item.ExistsOnDisk = File.Exists(clean); } catch { item.ExistsOnDisk = false; }

										ClassifyShimItem(item);
										list.Add(item);
									}
								}
							}
							sb.Length = 0;
						}
					}
				}
			}
			catch { }

			// Sort: Cheats first, then cleaners, then removable/deleted
			list.Sort(delegate(ShimcacheEntryItem a, ShimcacheEntryItem b)
			{
				if (a.IsCheat && !b.IsCheat) return -1;
				if (!a.IsCheat && b.IsCheat) return 1;
				if (a.IsCleaner && !b.IsCleaner) return -1;
				if (!a.IsCleaner && b.IsCleaner) return 1;
				if (!a.ExistsOnDisk && b.ExistsOnDisk) return -1;
				if (a.ExistsOnDisk && !b.ExistsOnDisk) return 1;
				return a.ExeName.CompareTo(b.ExeName);
			});

			return list;
		}

		private static string CleanShimPath(string path)
		{
			if (string.IsNullOrEmpty(path)) return "";
			string clean = path;

			// Strip NT namespace prefixes
			if (clean.StartsWith(@"\??\")) clean = clean.Substring(4);
			if (clean.StartsWith(@"\\?\")) clean = clean.Substring(4);

			// Strip leading garbage or control characters
			while (clean.Length > 0 && (clean[0] == '@' || clean[0] == '!' || clean[0] == '?' || clean[0] == ' '))
			{
				clean = clean.Substring(1);
			}

			// Validate path format
			if (clean.Length >= 3 && clean[1] == ':' && (clean[2] == '\\' || clean[2] == '/'))
			{
				return clean;
			}
			if (clean.StartsWith(@"\"))
			{
				return clean;
			}

			return clean;
		}

		private static void ClassifyShimItem(ShimcacheEntryItem item)
		{
			string fnLower = (item.ExeName ?? "").ToLowerInvariant();
			string pathLower = (item.FullPath ?? "").ToLowerInvariant();

			// 1. Whitelist Check
			bool isWhite = false;
			foreach (string w in CheatDatabases.WhitelistProcessNames)
			{
				if (fnLower.Contains(w.ToLowerInvariant())) { isWhite = true; break; }
			}
			if (fnLower.Contains("anydesk") || fnLower.Contains("rudesktop") || fnLower.Contains("autohotkey") ||
				fnLower.Contains("opautoclicker") || fnLower.Contains("speedautoclicker") || fnLower.Contains("tinytask") ||
				fnLower.Contains("clickermann") || fnLower.Contains("keyran") || fnLower.Contains("x-mouse"))
			{
				isWhite = true;
			}

			if (isWhite)
			{
				item.IsWhitelisted = true;
				item.Verdict = "✔ Whitelist (5.01)";
				item.ThreatReason = "Разрешённая утилита ScreenShare или разрешённый автокликер (белый список)";
				item.ForeColor = Ui.AccentGreen;
				return;
			}

			// 2. Cleaners Check (Rule 5.08)
			bool isCleaner = false;
			foreach (string cl in CheatDatabases.KnownCleanerSignatures)
			{
				if (cl.Length >= 4 && (fnLower.Contains(cl) || pathLower.Contains(cl))) { isCleaner = true; break; }
			}
			if (isCleaner || fnLower.Contains("journaltrace") || fnLower.Contains("cleandetecting") || fnLower.Contains("sdelete"))
			{
				item.IsCleaner = true;
				item.Verdict = "🚨 Клинер сокрытия (5.08)";
				item.ThreatReason = "Зафиксирован запуск утилиты очистки или сокрытия улик в системном кэше AppCompatCache!";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 3. Cheats & Injectors Check (Rule 5.01)
			bool isCheat = CheatExePattern.IsMatch(fnLower) || CheatExePattern.IsMatch(pathLower);
			if (!isCheat)
			{
				foreach (string ec in CheatDatabases.ExtraCheatClients)
				{
					if (ec.Length >= 4 && (fnLower.Contains(ec.ToLowerInvariant()) || pathLower.Contains(ec.ToLowerInvariant())))
					{
						isCheat = true;
						break;
					}
				}
			}
			if (!isCheat)
			{
				foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
				{
					if (inj.Length >= 4 && (fnLower.Contains(inj.ToLowerInvariant()) || pathLower.Contains(inj.ToLowerInvariant())))
					{
						isCheat = true;
						break;
					}
				}
			}

			if (isCheat)
			{
				item.IsCheat = true;
				item.Verdict = "🚨 Запуск чита (5.01)";
				item.ThreatReason = "В кэше AppCompatCache зафиксирован запуск запрещённого чита/инжектора: " + item.FullPath;
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 4. Removable / USB drive
			if (item.IsRemovable)
			{
				item.Verdict = "🔌 Запуск с USB / Съемного диска";
				item.ThreatReason = "Программа запускалась со съемного диска или флешки (" + item.DriveLetter + ")";
				item.ForeColor = Ui.AccentOrange;
				return;
			}

			// 5. Deleted file
			if (!item.ExistsOnDisk)
			{
				item.Verdict = "🗑️ Файл удалён с диска";
				item.ThreatReason = "Исполняемый файл запускался ранее, но в настоящее время отсутствует на диске";
				item.ForeColor = Ui.TextSecondary;
				return;
			}

			// Normal
			item.Verdict = "Обычный процесс";
			item.ThreatReason = "Стандартное системное или пользовательское приложение Windows";
			item.ForeColor = Ui.TextPrimary;
		}

		private void UpdateSummaryStats()
		{
			int total = allEntries.Count;
			int cheats = allEntries.Count(e => e.IsCheat);
			int cleaners = allEntries.Count(e => e.IsCleaner);
			int removable = allEntries.Count(e => e.IsRemovable);
			int deleted = allEntries.Count(e => !e.ExistsOnDisk);
			int white = allEntries.Count(e => e.IsWhitelisted);

			btnFilterAll.Text = string.Format("🔍 Все ({0})", total);
			btnFilterCheats.Text = string.Format("🚨 Читы ({0})", cheats);
			btnFilterCleaners.Text = string.Format("🧹 Клинеры ({0})", cleaners);
			btnFilterRemovable.Text = string.Format("🔌 USB ({0})", removable);
			btnFilterDeleted.Text = string.Format("🗑️ Удалены ({0})", deleted);
			btnFilterWhitelist.Text = string.Format("✔ Whitelist ({0})", white);

			if (cheats > 0 || cleaners > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: В кэше ядра обнаружено {0} читов, {1} клинеров!", cheats, cleaners);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔ Проверено {0} записей AppCompatCache. Читов не обнаружено.", total);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();

			foreach (ShimcacheEntryItem item in allEntries)
			{
				switch (currentFilter)
				{
					case ShimcacheFilter.CheatsOnly:
						if (!item.IsCheat) continue;
						break;
					case ShimcacheFilter.CleanersOnly:
						if (!item.IsCleaner) continue;
						break;
					case ShimcacheFilter.RemovableDrives:
						if (!item.IsRemovable) continue;
						break;
					case ShimcacheFilter.DeletedFilesOnly:
						if (item.ExistsOnDisk) continue;
						break;
					case ShimcacheFilter.WhitelistOnly:
						if (!item.IsWhitelisted) continue;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = item.ExeName.ToLowerInvariant().Contains(query) ||
								 item.FullPath.ToLowerInvariant().Contains(query) ||
								 item.Verdict.ToLowerInvariant().Contains(query) ||
								 item.DriveLetter.ToLowerInvariant().Contains(query);
					if (!match) continue;
				}

				string diskStatus = item.ExistsOnDisk ? "✔ Существует на диске" : (item.IsRemovable ? "🔌 USB отключен" : "🗑️ Удалён с диска");

				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.SubItems.Add(item.ExeName);
				lvi.SubItems.Add(diskStatus);
				lvi.SubItems.Add(item.FullPath);
				lvi.SubItems.Add(item.DriveLetter);

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
				ClearDetailPane();
			}
		}

		private ShimcacheEntryItem GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as ShimcacheEntryItem;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			ShimcacheEntryItem sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailExe.Text = sel.ExeName;
			lblDetailPath.Text = "Полный путь: " + sel.FullPath;
			lblDetailDiskStatus.Text = "Состояние на диске: " + (sel.ExistsOnDisk ? "Файл найден на диске" : (sel.IsRemovable ? "Съемный диск отключен" : "Файл удалён с диска"));
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;

			if (sel.IsCheat) lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-программы / Стороннее ПО) — Бан 30 дней";
			else if (sel.IsCleaner) lblDetailRule.Text = "Пункт регламента: 5.08 (Сокрытие улик / Клинеры) — Бан 30 дней";
			else if (sel.IsWhitelisted) lblDetailRule.Text = "Пункт регламента: 5.01 Белый список (Разрешено)";
			else lblDetailRule.Text = "Пункт регламента: Допустимая активность приложений";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Исполняемый файл: " + sel.ExeName);
			sb.AppendLine("Полный путь: " + sel.FullPath);
			sb.AppendLine("Диск: " + sel.DriveLetter + (sel.IsRemovable ? " (Съемный накопитель / USB флешка)" : " (Локальный диск)"));
			sb.AppendLine("Статус на диске: " + (sel.ExistsOnDisk ? "Существует" : "Удалён или накопитель извлечен"));
			sb.AppendLine("Обоснование: " + sel.ThreatReason);
			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDetailPane()
		{
			lblDetailExe.Text = "Записи не выбраны";
			lblDetailPath.Text = "Полный путь: —";
			lblDetailDiskStatus.Text = "Состояние файла: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Пункт регламента: —";
			txtDetailDescription.Text = "";
		}

		private void CopyBanCommand()
		{
			ShimcacheEntryItem sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.IsCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Запуск чита по AppCompatCache: {1})",
					targetPlayerName, sel.ExeName);
			}
			else if (sel.IsCleaner)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Запуск клинера по AppCompatCache: {1})",
					targetPlayerName, sel.ExeName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подозрительный запуск по AppCompatCache: {1})",
					targetPlayerName, sel.ExeName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан по AppCompatCache", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
