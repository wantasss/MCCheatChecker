using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class UsnJournalViewerForm : Form
	{
		private enum UsnFilter
		{
			All,
			CheatDeletionsOnly,
			AllDeletions,
			RenamesOnly,
			Last60Minutes,
			MinecraftModsOnly
		}

		private class UsnEntryItem
		{
			public int Index;
			public string FileName;
			public string Extension;
			public DateTime Timestamp;
			public string ReasonText;
			public uint ReasonCode;
			public ulong Usn;
			public bool IsDelete;
			public bool IsRename;
			public bool IsCreate;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsMinecraft;
			public bool IsWhitelisted;
			public string ThreatReason;
			public string Verdict;
			public Color ForeColor;
		}

		#region Win32 Native USN Journal Structures & APIs
		private const uint GENERIC_READ = 0x80000000;
		private const uint FILE_SHARE_READ = 0x00000001;
		private const uint FILE_SHARE_WRITE = 0x00000002;
		private const uint OPEN_EXISTING = 3;
		private const uint FSCTL_QUERY_USN_JOURNAL = 0x000900f4;
		private const uint FSCTL_READ_USN_JOURNAL = 0x000900bb;

		private const uint USN_REASON_DATA_OVERWRITE = 0x00000001;
		private const uint USN_REASON_DATA_EXTEND = 0x00000002;
		private const uint USN_REASON_DATA_TRUNCATION = 0x00000004;
		private const uint USN_REASON_FILE_CREATE = 0x00000100;
		private const uint USN_REASON_FILE_DELETE = 0x00000200;
		private const uint USN_REASON_RENAME_OLD_NAME = 0x00001000;
		private const uint USN_REASON_RENAME_NEW_NAME = 0x00002000;
		private const uint USN_REASON_CLOSE = 0x80000000;

		[StructLayout(LayoutKind.Sequential)]
		private struct USN_JOURNAL_DATA_V0
		{
			public ulong UsnJournalID;
			public ulong FirstUsn;
			public ulong NextUsn;
			public ulong LowestValidUsn;
			public ulong MaxUsn;
			public ulong MaximumSize;
			public ulong AllocationDelta;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct READ_USN_JOURNAL_DATA_V0
		{
			public ulong StartUsn;
			public uint ReasonMask;
			public uint ReturnOnlyOnClose;
			public ulong Timeout;
			public ulong BytesToWaitFor;
			public ulong UsnJournalID;
		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			uint nInBufferSize,
			IntPtr lpOutBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);
		#endregion

		private string targetPlayerName;
		private string selectedDrive = "C:";
		private UsnFilter currentFilter = UsnFilter.All;
		private List<UsnEntryItem> allEntries = new List<UsnEntryItem>();
		private bool journalWipedDetected = false;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderStatus;
		private Label lblStatusPill;
		private Label lblDetailFile;
		private Label lblDetailReason;
		private Label lblDetailTime;
		private Label lblDetailStatus;
		private Label lblDetailRule;
		private DarkTextBoxMultiline txtDetailDescription;
		private ComboBox comboDrives;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCheatDeletes;
		private DarkButton btnFilterAllDeletes;
		private DarkButton btnFilterRenames;
		private DarkButton btnFilter60m;
		private DarkButton btnFilterMc;

		private static readonly Regex CheatNamePattern = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|injector|inject|dllinject|killaura|velocity|aimbot|autoclicker|cleaner|wiper|reghax)",
			RegexOptions.Compiled);

		public UsnJournalViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			LoadDrives();
			LoadUsnJournalAsync();
		}

		private void InitializeComponent()
		{
			Text = "🗑️ Инспектор удалений и изменений NTFS USN Journal ($UsnJrnl Forensics)";
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
			lblHeaderTitle.Text = "🗑️  ИНСПЕКТОР УДАЛЕНИЙ NTFS USN JOURNAL ($UsnJrnl File Deletion Forensics)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentOrange;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Чтение системного журнала изменений NTFS: фиксация удалений, переименований и затираний файлов софтов и модов...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Опрос журнала USN...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			Label lblDrive = new Label();
			lblDrive.Text = "Диск:";
			lblDrive.Font = Ui.FontBody(8.5f, true);
			lblDrive.ForeColor = Ui.TextSecondary;
			lblDrive.Location = new Point(880, 12);
			lblDrive.AutoSize = true;
			topPanel.Controls.Add(lblDrive);

			comboDrives = new ComboBox();
			comboDrives.DropDownStyle = ComboBoxStyle.DropDownList;
			comboDrives.BackColor = Ui.SurfaceAlt;
			comboDrives.ForeColor = Ui.TextPrimary;
			comboDrives.Font = Ui.FontBody(8.5f);
			comboDrives.Location = new Point(925, 9);
			comboDrives.Size = new Size(80, 24);
			comboDrives.SelectedIndexChanged += delegate
			{
				if (comboDrives.SelectedItem != null)
				{
					selectedDrive = comboDrives.SelectedItem.ToString();
					LoadUsnJournalAsync();
				}
			};
			topPanel.Controls.Add(comboDrives);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(1015, 8);
			btnRefresh.Size = new Size(150, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.Font = Ui.FontBody(8.5f, true);
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate { LoadUsnJournalAsync(); };
			topPanel.Controls.Add(btnRefresh);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1180, 28);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Все события (0)", UsnFilter.All, 130);
			btnFilterCheatDeletes = CreateFilterButton("🚨 Удаления читов (0)", UsnFilter.CheatDeletionsOnly, 175);
			btnFilterAllDeletes = CreateFilterButton("🗑️ Все удаления (0)", UsnFilter.AllDeletions, 145);
			btnFilterRenames = CreateFilterButton("🔄 Переименования (0)", UsnFilter.RenamesOnly, 160);
			btnFilter60m = CreateFilterButton("⏱️ За 60 минут (0)", UsnFilter.Last60Minutes, 130);
			btnFilterMc = CreateFilterButton("☕ Моды & MC (0)", UsnFilter.MinecraftModsOnly, 135);

			int curX = 0;
			DarkButton[] filterButtons = { btnFilterAll, btnFilterCheatDeletes, btnFilterAllDeletes, btnFilterRenames, btnFilter60m, btnFilterMc };
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
			lblHint.Text = "💡 Подсказка: Кликните дважды по строке для копирования команды бана. Записи USN невозможно удалить без стирания всего журнала (5.08).";
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
			listView.Columns.Add("Имя файла", 230);
			listView.Columns.Add("Действие (USN Reason)", 185);
			listView.Columns.Add("Дата и время события", 160);
			listView.Columns.Add("Давность", 95);
			listView.Columns.Add("Тип", 75);
			listView.Columns.Add("USN ID", 125);

			listView.SelectedIndexChanged += delegate { UpdateDetailPane(); };
			listView.DoubleClick += delegate { CopyBanCommand(); };

			split.Panel1.Controls.Add(listView);

			// Bottom Panel: Details & Ban Action
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.Surface;
			detailContainer.Padding = new Padding(12, 10, 12, 10);

			lblDetailFile = new Label();
			lblDetailFile.Text = "Выберите запись для детального анализа...";
			lblDetailFile.Font = Ui.FontTitle(10f, true);
			lblDetailFile.ForeColor = Ui.TextPrimary;
			lblDetailFile.Location = new Point(12, 8);
			lblDetailFile.AutoSize = true;
			detailContainer.Controls.Add(lblDetailFile);

			lblDetailReason = new Label();
			lblDetailReason.Text = "Событие USN: —";
			lblDetailReason.Font = Ui.FontBody(8.5f);
			lblDetailReason.ForeColor = Ui.TextSecondary;
			lblDetailReason.Location = new Point(12, 30);
			lblDetailReason.AutoSize = true;
			detailContainer.Controls.Add(lblDetailReason);

			lblDetailTime = new Label();
			lblDetailTime.Text = "Точное время: —";
			lblDetailTime.Font = Ui.FontBody(8.5f);
			lblDetailTime.ForeColor = Ui.TextSecondary;
			lblDetailTime.Location = new Point(12, 50);
			lblDetailTime.AutoSize = true;
			detailContainer.Controls.Add(lblDetailTime);

			lblDetailStatus = new Label();
			lblDetailStatus.Text = "Статус: —";
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

			DarkButton btnCopyFileName = new DarkButton();
			btnCopyFileName.Text = "📄 Скопировать имя";
			btnCopyFileName.Location = new Point(850, 145);
			btnCopyFileName.Size = new Size(160, 27);
			btnCopyFileName.BackColor = Ui.SurfaceAlt;
			btnCopyFileName.Font = Ui.FontBody(8.5f, true);
			btnCopyFileName.Cursor = Cursors.Hand;
			btnCopyFileName.Click += delegate
			{
				UsnEntryItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FileName))
				{
					Clipboard.SetText(sel.FileName);
					MessageBox.Show("Имя файла скопировано в буфер: " + sel.FileName, "USN", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			detailContainer.Controls.Add(btnCopyFileName);

			DarkButton btnSearchEverything = new DarkButton();
			btnSearchEverything.Text = "🔍 Поиск в Everything";
			btnSearchEverything.Location = new Point(1020, 114);
			btnSearchEverything.Size = new Size(160, 27);
			btnSearchEverything.BackColor = Ui.SurfaceAlt;
			btnSearchEverything.Font = Ui.FontBody(8.5f, true);
			btnSearchEverything.Cursor = Cursors.Hand;
			btnSearchEverything.Click += delegate
			{
				UsnEntryItem sel = GetSelectedItem();
				if (sel != null && !string.IsNullOrEmpty(sel.FileName))
				{
					try { Process.Start("everything.exe", "-s \"" + sel.FileName + "\""); }
					catch { MessageBox.Show("Everything.exe не найден в PATH.", "Everything", MessageBoxButtons.OK, MessageBoxIcon.Information); }
				}
			};
			detailContainer.Controls.Add(btnSearchEverything);

			detailContainer.Resize += delegate
			{
				int w = detailContainer.ClientSize.Width - 24;
				if (w > 600)
				{
					int descW = Math.Max(300, w - 360);
					txtDetailDescription.Width = descW;
					btnCopyBan.Location = new Point(descW + 24, 114);
					btnCopyFileName.Location = new Point(descW + 24, 145);
					btnSearchEverything.Location = new Point(descW + 190, 114);
				}
			};

			split.Panel2.Controls.Add(detailContainer);
			Controls.Add(split);
		}

		private void LoadDrives()
		{
			comboDrives.Items.Clear();
			foreach (DriveInfo d in DriveInfo.GetDrives())
			{
				if (d.DriveType == DriveType.Fixed)
				{
					comboDrives.Items.Add(d.Name.Substring(0, 2));
				}
			}
			if (comboDrives.Items.Count > 0) comboDrives.SelectedIndex = 0;
			else comboDrives.Items.Add("C:");
		}

		private DarkButton CreateFilterButton(string text, UsnFilter filter, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 26);
			btn.BackColor = (filter == currentFilter) ? Ui.AccentOrange : Ui.SurfaceAlt;
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
			DarkButton[] btns = { btnFilterAll, btnFilterCheatDeletes, btnFilterAllDeletes, btnFilterRenames, btnFilter60m, btnFilterMc };
			UsnFilter[] filters = { UsnFilter.All, UsnFilter.CheatDeletionsOnly, UsnFilter.AllDeletions, UsnFilter.RenamesOnly, UsnFilter.Last60Minutes, UsnFilter.MinecraftModsOnly };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (currentFilter == filters[i]);
				btns[i].BackColor = active ? Ui.AccentOrange : Ui.SurfaceAlt;
				btns[i].Font = Ui.FontBody(8f, active);
			}
		}

		private void LoadUsnJournalAsync()
		{
			lblStatusPill.Text = "⏳ Чтение USN Journal на диске " + selectedDrive + "...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			journalWipedDetected = false;
			allEntries.Clear();
			listView.Items.Clear();

			string drive = selectedDrive;
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<UsnEntryItem> items = ReadVolumeUsnJournal(drive, out journalWipedDetected);
				e.Result = items;
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is List<UsnEntryItem>)
				{
					allEntries = (List<UsnEntryItem>)e.Result;
				}
				UpdateSummaryStats();
				ApplyFilters();
			};

			worker.RunWorkerAsync();
		}

		private static List<UsnEntryItem> ReadVolumeUsnJournal(string drive, out bool wiped)
		{
			List<UsnEntryItem> items = new List<UsnEntryItem>();
			wiped = false;
			string volumePath = @"\\.\" + drive;

			IntPtr hVolume = CreateFile(volumePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
			if (hVolume == new IntPtr(-1))
			{
				// Check if journal was wiped in event log
				wiped = CheckIfJournalWipedEventLogs();
				return items;
			}

			try
			{
				USN_JOURNAL_DATA_V0 journalData = new USN_JOURNAL_DATA_V0();
				int sizeJournalData = Marshal.SizeOf(typeof(USN_JOURNAL_DATA_V0));
				IntPtr pJournalData = Marshal.AllocHGlobal(sizeJournalData);
				uint bytesReturned;

				bool ok = DeviceIoControl(hVolume, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, pJournalData, (uint)sizeJournalData, out bytesReturned, IntPtr.Zero);
				if (ok)
				{
					journalData = (USN_JOURNAL_DATA_V0)Marshal.PtrToStructure(pJournalData, typeof(USN_JOURNAL_DATA_V0));
				}
				Marshal.FreeHGlobal(pJournalData);

				if (!ok || journalData.UsnJournalID == 0)
				{
					wiped = CheckIfJournalWipedEventLogs();
					return items;
				}

				// Read recent USN records (up to 4MB chunk from end of journal)
				READ_USN_JOURNAL_DATA_V0 readData = new READ_USN_JOURNAL_DATA_V0();
				readData.UsnJournalID = journalData.UsnJournalID;
				readData.ReasonMask = 0xFFFFFFFF; // All reasons
				readData.ReturnOnlyOnClose = 0;
				readData.Timeout = 0;
				readData.BytesToWaitFor = 0;

				// Start reading from recent offset
				ulong startUsn = 0;
				if (journalData.NextUsn > 3 * 1024 * 1024)
				{
					startUsn = journalData.NextUsn - 3 * 1024 * 1024;
				}
				readData.StartUsn = startUsn;

				int inSize = Marshal.SizeOf(typeof(READ_USN_JOURNAL_DATA_V0));
				IntPtr pIn = Marshal.AllocHGlobal(inSize);
				Marshal.StructureToPtr(readData, pIn, false);

				int outBufferSize = 2 * 1024 * 1024; // 2 MB buffer
				IntPtr pOut = Marshal.AllocHGlobal(outBufferSize);

				int loopCount = 0;
				int index = 1;

				while (loopCount < 3 && items.Count < 5000)
				{
					loopCount++;
					bool readOk = DeviceIoControl(hVolume, FSCTL_READ_USN_JOURNAL, pIn, (uint)inSize, pOut, (uint)outBufferSize, out bytesReturned, IntPtr.Zero);
					if (!readOk || bytesReturned <= 8) break;

					// First 8 bytes is NextUsn
					ulong nextUsn = (ulong)Marshal.ReadInt64(pOut);
					int offset = 8;

					while (offset < bytesReturned)
					{
						uint recordLen = (uint)Marshal.ReadInt32(pOut, offset);
						if (recordLen == 0 || offset + recordLen > bytesReturned) break;

						ushort major = (ushort)Marshal.ReadInt16(pOut, offset + 4);
						if (major == 2 || major == 3)
						{
							long fileTime = Marshal.ReadInt64(pOut, offset + 32);
							uint reason = (uint)Marshal.ReadInt32(pOut, offset + 40);
							ushort fnLen = (ushort)Marshal.ReadInt16(pOut, offset + 56);
							ushort fnOffset = (ushort)Marshal.ReadInt16(pOut, offset + 58);

							if (fnLen > 0 && offset + fnOffset + fnLen <= bytesReturned)
							{
								byte[] nameBytes = new byte[fnLen];
								Marshal.Copy(new IntPtr(pOut.ToInt64() + offset + fnOffset), nameBytes, 0, fnLen);
								string fileName = Encoding.Unicode.GetString(nameBytes);

								if (!string.IsNullOrEmpty(fileName) && !fileName.StartsWith("$") && fileName.Length >= 2)
								{
									DateTime dt = DateTime.MinValue;
									try { dt = DateTime.FromFileTimeUtc(fileTime).ToLocalTime(); } catch { }

									UsnEntryItem item = new UsnEntryItem();
									item.Index = index++;
									item.FileName = fileName;
									item.Extension = Path.GetExtension(fileName).ToLowerInvariant();
									item.Timestamp = dt;
									item.ReasonCode = reason;
									item.Usn = nextUsn;

									item.IsDelete = (reason & USN_REASON_FILE_DELETE) != 0;
									item.IsRename = (reason & (USN_REASON_RENAME_OLD_NAME | USN_REASON_RENAME_NEW_NAME)) != 0;
									item.IsCreate = (reason & USN_REASON_FILE_CREATE) != 0;

									item.ReasonText = FormatReason(reason);
									ClassifyUsnItem(item);

									items.Add(item);
								}
							}
						}
						offset += (int)recordLen;
					}

					if (nextUsn >= journalData.NextUsn) break;
					Marshal.WriteInt64(pIn, 0, (long)nextUsn);
				}

				Marshal.FreeHGlobal(pIn);
				Marshal.FreeHGlobal(pOut);
			}
			catch { }
			finally
			{
				CloseHandle(hVolume);
			}

			// Sort newest events first, cheats prioritized
			items.Sort(delegate(UsnEntryItem a, UsnEntryItem b)
			{
				if (a.IsCheat && !b.IsCheat) return -1;
				if (!a.IsCheat && b.IsCheat) return 1;
				if (a.IsCleaner && !b.IsCleaner) return -1;
				if (!a.IsCleaner && b.IsCleaner) return 1;
				if (a.IsDelete && !b.IsDelete) return -1;
				if (!a.IsDelete && b.IsDelete) return 1;
				return b.Timestamp.CompareTo(a.Timestamp);
			});

			return items;
		}

		private static bool CheckIfJournalWipedEventLogs()
		{
			try
			{
				using (EventLog el = new EventLog("System"))
				{
					int cnt = el.Entries.Count;
					int scan = Math.Min(cnt, 250);
					for (int i = cnt - 1; i >= cnt - scan; i--)
					{
						EventLogEntry entry = el.Entries[i];
						if (entry.InstanceId == 3079 || entry.InstanceId == 3080)
						{
							return true;
						}
					}
				}
			}
			catch { }
			return false;
		}

		private static string FormatReason(uint r)
		{
			List<string> reasons = new List<string>();
			if ((r & USN_REASON_FILE_DELETE) != 0) reasons.Add("🗑️ Удаление");
			if ((r & USN_REASON_RENAME_OLD_NAME) != 0) reasons.Add("🔄 Переименование (было)");
			if ((r & USN_REASON_RENAME_NEW_NAME) != 0) reasons.Add("🔄 Переименование (стало)");
			if ((r & USN_REASON_FILE_CREATE) != 0) reasons.Add("⚡ Создание");
			if ((r & USN_REASON_DATA_OVERWRITE) != 0) reasons.Add("📝 Запись");
			if ((r & USN_REASON_DATA_TRUNCATION) != 0) reasons.Add("✂️ Усечение");
			if ((r & USN_REASON_DATA_EXTEND) != 0) reasons.Add("Разрастание");
			if ((r & USN_REASON_CLOSE) != 0) reasons.Add("Закрытие");

			return reasons.Count > 0 ? string.Join(" | ", reasons.ToArray()) : "0x" + r.ToString("X8");
		}

		private static void ClassifyUsnItem(UsnEntryItem item)
		{
			string fnLower = item.FileName.ToLowerInvariant();

			// 1. Whitelist Clickers & Tools
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
				item.ThreatReason = "Разрешённая утилита ScreenShare или разрешённый автокликер";
				item.ForeColor = Ui.AccentGreen;
				return;
			}

			// 2. Cleaners & Anti-Forensics (Rule 5.08)
			bool isCleaner = fnLower.Contains("clean") || fnLower.Contains("wipe") || fnLower.Contains("sdelete") ||
							 fnLower.Contains("journaltrace") || fnLower.Contains("cleandetecting") || fnLower.Contains("wevtutil");
			if (!isCleaner)
			{
				foreach (string cl in CheatDatabases.KnownCleanerSignatures)
				{
					if (cl.Length >= 4 && fnLower.Contains(cl)) { isCleaner = true; break; }
				}
			}
			if (isCleaner)
			{
				item.IsCleaner = true;
				item.Verdict = item.IsDelete ? "🚨 УДАЛЕНИЕ КЛИНЕРА (5.08)" : "🚨 Действие клинера (5.08)";
				item.ThreatReason = "Зафиксированы следы очистки улик, батника или клинера (Пункт регламента 5.08)";
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 3. Cheats & Injectors (Rule 5.01)
			bool isCheat = CheatNamePattern.IsMatch(fnLower);
			if (!isCheat)
			{
				foreach (string ec in CheatDatabases.ExtraCheatClients)
				{
					if (ec.Length >= 4 && fnLower.Contains(ec.ToLowerInvariant())) { isCheat = true; break; }
				}
			}
			if (!isCheat)
			{
				foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
				{
					if (inj.Length >= 4 && fnLower.Contains(inj.ToLowerInvariant())) { isCheat = true; break; }
				}
			}

			if (isCheat)
			{
				item.IsCheat = true;
				if (item.IsDelete)
				{
					item.Verdict = "🚨 УДАЛЕНИЕ ЧИТА (5.01)";
					item.ThreatReason = "Зафиксировано удаление файла запрещённого чита/инжектора перед или во время проверки!";
				}
				else if (item.IsRename)
				{
					item.Verdict = "🚨 ПЕРЕИМЕНОВАНИЕ ЧИТА (5.01)";
					item.ThreatReason = "Попытка сокрытия чита путём переименования файла!";
				}
				else
				{
					item.Verdict = "🚨 ЧИТ-АКТИВНОСТЬ (5.01)";
					item.ThreatReason = "Создание или перезапись файла чит-клиента на диске";
				}
				item.ForeColor = Ui.AccentRed;
				return;
			}

			// 4. Minecraft mods / configs
			if (item.Extension == ".jar" || item.Extension == ".litematic" || item.Extension == ".json" ||
				fnLower == "latest.log" || fnLower == "options.txt" || fnLower == "servers.dat")
			{
				item.IsMinecraft = true;
				if (item.IsDelete)
				{
					item.Verdict = "⚠️ Удаление мода / файла MC";
					item.ThreatReason = "Удаление файла модификации или конфига Minecraft";
					item.ForeColor = Ui.AccentOrange;
				}
				else
				{
					item.Verdict = "☕ Файл Minecraft";
					item.ThreatReason = "Стандартное действие с файлами игры";
					item.ForeColor = Ui.AccentTeal;
				}
				return;
			}

			// Normal
			if (item.IsDelete)
			{
				item.Verdict = "🗑️ Удаление файла";
				item.ThreatReason = "Штатное удаление пользовательского или системного файла";
				item.ForeColor = Ui.TextSecondary;
			}
			else
			{
				item.Verdict = "Операция с файлом";
				item.ThreatReason = "Стандартная дисковая активность Windows";
				item.ForeColor = Ui.TextPrimary;
			}
		}

		private void UpdateSummaryStats()
		{
			int total = allEntries.Count;
			int cheatDeletes = allEntries.Count(e => (e.IsCheat || e.IsCleaner) && e.IsDelete);
			int allDeletes = allEntries.Count(e => e.IsDelete);
			int renames = allEntries.Count(e => e.IsRename);
			DateTime past60m = DateTime.Now.AddMinutes(-60);
			int recent = allEntries.Count(e => e.Timestamp >= past60m);
			int mc = allEntries.Count(e => e.IsMinecraft);

			btnFilterAll.Text = string.Format("🔍 Все ({0})", total);
			btnFilterCheatDeletes.Text = string.Format("🚨 Удаления читов ({0})", cheatDeletes);
			btnFilterAllDeletes.Text = string.Format("🗑️ Удаления ({0})", allDeletes);
			btnFilterRenames.Text = string.Format("🔄 Переименования ({0})", renames);
			btnFilter60m.Text = string.Format("⏱️ За 60 мин ({0})", recent);
			btnFilterMc.Text = string.Format("☕ Моды MC ({0})", mc);

			if (journalWipedDetected)
			{
				lblStatusPill.Text = "🚨 ВНИМАНИЕ: Журнал USN был удалён/очищен! Нарушение 5.08!";
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else if (cheatDeletes > 0)
			{
				lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Зафиксировано {0} удалений читов в журнале USN!", cheatDeletes);
				lblStatusPill.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblStatusPill.Text = string.Format("✔ Проанализировано {0} записей USN на {1}. Удалений читов не обнаружено.", total, selectedDrive);
				lblStatusPill.ForeColor = Ui.AccentGreen;
			}
		}

		private void ApplyFilters()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string query = (searchBox.Text ?? "").Trim().ToLowerInvariant();
			DateTime past60m = DateTime.Now.AddMinutes(-60);

			foreach (UsnEntryItem item in allEntries)
			{
				switch (currentFilter)
				{
					case UsnFilter.CheatDeletionsOnly:
						if (!item.IsCheat && !item.IsCleaner) continue;
						break;
					case UsnFilter.AllDeletions:
						if (!item.IsDelete) continue;
						break;
					case UsnFilter.RenamesOnly:
						if (!item.IsRename) continue;
						break;
					case UsnFilter.Last60Minutes:
						if (item.Timestamp < past60m) continue;
						break;
					case UsnFilter.MinecraftModsOnly:
						if (!item.IsMinecraft) continue;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = item.FileName.ToLowerInvariant().Contains(query) ||
								 item.Verdict.ToLowerInvariant().Contains(query) ||
								 item.ReasonText.ToLowerInvariant().Contains(query) ||
								 item.Extension.ToLowerInvariant().Contains(query);
					if (!match) continue;
				}

				TimeSpan diff = DateTime.Now - item.Timestamp;
				string ago = diff.TotalMinutes < 1 ? "< 1 мин" :
							 (diff.TotalHours < 1 ? string.Format("{0} мин назад", (int)diff.TotalMinutes) :
							 (diff.TotalDays < 1 ? string.Format("{0} ч назад", (int)diff.TotalHours) :
							 string.Format("{0} д назад", (int)diff.TotalDays)));

				ListViewItem lvi = new ListViewItem(item.Verdict);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(item.ReasonText);
				lvi.SubItems.Add(item.Timestamp != DateTime.MinValue ? item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss") : "—");
				lvi.SubItems.Add(ago);
				lvi.SubItems.Add(string.IsNullOrEmpty(item.Extension) ? "файл" : item.Extension);
				lvi.SubItems.Add("0x" + item.Usn.ToString("X"));

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

		private UsnEntryItem GetSelectedItem()
		{
			if (listView.SelectedItems.Count > 0)
			{
				return listView.SelectedItems[0].Tag as UsnEntryItem;
			}
			return null;
		}

		private void UpdateDetailPane()
		{
			UsnEntryItem sel = GetSelectedItem();
			if (sel == null)
			{
				ClearDetailPane();
				return;
			}

			lblDetailFile.Text = sel.FileName;
			lblDetailReason.Text = "Действие NTFS: " + sel.ReasonText + " (Код: 0x" + sel.ReasonCode.ToString("X8") + ")";
			lblDetailTime.Text = "Точное время: " + (sel.Timestamp != DateTime.MinValue ? sel.Timestamp.ToString("dd.MM.yyyy HH:mm:ss.fff") : "—");
			lblDetailStatus.Text = "Вердикт: " + sel.Verdict;
			lblDetailStatus.ForeColor = sel.ForeColor;

			if (sel.IsCheat) lblDetailRule.Text = "Пункт регламента: 5.01 (Чит-программы / Стороннее ПО) — Бан 30 дней";
			else if (sel.IsCleaner) lblDetailRule.Text = "Пункт регламента: 5.08 (Удаление логов / Сокрытие улик) — Бан 30 дней";
			else if (sel.IsWhitelisted) lblDetailRule.Text = "Пункт регламента: 5.01 Белый список (Разрешено)";
			else lblDetailRule.Text = "Пункт регламента: Допустимая дисковая активность";

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Диск: " + selectedDrive);
			sb.AppendLine("Имя файла: " + sel.FileName);
			sb.AppendLine("USN смещение: 0x" + sel.Usn.ToString("X"));
			sb.AppendLine("Обоснование: " + sel.ThreatReason);
			if (sel.IsDelete && (sel.IsCheat || sel.IsCleaner))
			{
				sb.AppendLine("🚨 ВНИМАНИЕ: Файл был удалён! Журнал USN зафиксировал точный момент удаления!");
			}
			txtDetailDescription.Text = sb.ToString();
		}

		private void ClearDetailPane()
		{
			lblDetailFile.Text = "Записи не выбраны";
			lblDetailReason.Text = "Событие USN: —";
			lblDetailTime.Text = "Точное время: —";
			lblDetailStatus.Text = "Статус: —";
			lblDetailRule.Text = "Пункт регламента: —";
			txtDetailDescription.Text = "";
		}

		private void CopyBanCommand()
		{
			UsnEntryItem sel = GetSelectedItem();
			if (sel == null) return;

			string cmd = "";
			if (sel.IsCheat)
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Удаление чита по USN Journal: {1} в {2})",
					targetPlayerName, sel.FileName, sel.Timestamp.ToString("dd.MM.yyyy HH:mm"));
			}
			else if (sel.IsCleaner)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Удаление клинера по USN Journal: {1} в {2})",
					targetPlayerName, sel.FileName, sel.Timestamp.ToString("dd.MM.yyyy HH:mm"));
			}
			else if (journalWipedDetected)
			{
				cmd = string.Format("/ban {0} 30d 5.08 (Очистка журнала NTFS USN)", targetPlayerName);
			}
			else
			{
				cmd = string.Format("/ban {0} 30d 5.01 (Подозрительное удаление по USN Journal: {1})",
					targetPlayerName, sel.FileName);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "Бан по USN Journal", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
