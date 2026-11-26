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

namespace MCCheatChecker
{
	public class RecycleBinForensicsViewerForm : Form
	{
		#region Models

		public enum BinFilterMode
		{
			All,
			CheatsOnly,
			RecentDeletions,
			PayloadsPresent,
			CleanFiles
		}

		public class RecycleBinItem
		{
			public string DriveLetter;
			public string OriginalPath;
			public string FileName;
			public string Extension;
			public long FileSize;
			public DateTime DeletionTime;
			public string IndexFilePath;   // Путь к $I файлу
			public string PayloadFilePath; // Путь к $R файлу
			public bool PayloadExists;     // Присутствует ли физический $R файл в корзине
			public bool IsCheat;
			public bool IsRecent;          // Удалено менее 24 часов назад
			public bool IsEmergency;       // Удалено менее 15 минут назад (при вызове)
			public string Status;
			public string RuleViolation;
			public string BanReason;
			public Color ForeColor;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private BinFilterMode currentFilter = BinFilterMode.All;
		private List<RecycleBinItem> allEntries = new List<RecycleBinItem>();
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
		private DarkButton btnFilterCheats;
		private DarkButton btnFilterRecent;
		private DarkButton btnFilterPayloads;
		private DarkButton btnFilterClean;

		private DarkButton btnCopyBan;
		private DarkButton btnExtractPayload;
		private DarkButton btnOpenBinFolder;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex CheatRegex = new Regex(
			@"(?i)(vape|celestial|expensive|richclient|richpremium|akrien|nurik|nursultan|wexside|deadcode|thunderhack|meteor|wurst|liquidbounce|aristois|driplite|slinky|itami|koid|archware|kura|flint|cellent|sunset|exloader|processhacker|xenos|cheatengine|extremeinjector|killaura|velocity|aimbot|bape|neverhook|exptv|xray|catlavan|catlean|wildgrief|boze|zeroday|doomsday|rasty|dreampool|norules|neverware|zamorozka|fluger|cleaner|wiper|reghax|selfdelete)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public RecycleBinForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Аудит корзины $Recycle.Bin и детекция удалений ($I / $R) — MCCheatChecker";
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
			lblHeaderTitle.Text = "🗑️ Криминалистический анализ $Recycle.Bin: парсинг индексов $I и тел файлов $R";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Декодирование оригинальных путей удаленных файлов, точного времени отправки в корзину и выявление удалений читов (5.08).";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 40);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование корзины...";
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
			btnFilterAll = CreateFilterButton("Все файлы", BinFilterMode.All, ref bx, filterBar);
			btnFilterCheats = CreateFilterButton("🚨 Читы в корзине (5.08)", BinFilterMode.CheatsOnly, ref bx, filterBar);
			btnFilterRecent = CreateFilterButton("⏰ Свежие (до 24 ч)", BinFilterMode.RecentDeletions, ref bx, filterBar);
			btnFilterPayloads = CreateFilterButton("📦 Содержимое $R живо", BinFilterMode.PayloadsPresent, ref bx, filterBar);
			btnFilterClean = CreateFilterButton("Обычные файлы", BinFilterMode.CleanFiles, ref bx, filterBar);

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

			btnExtractPayload = new DarkButton();
			btnExtractPayload.Text = "💾 Извлечь файл $R на Рабочий стол";
			btnExtractPayload.Font = Ui.FontBody(9f, false);
			btnExtractPayload.Location = new Point(242, 10);
			btnExtractPayload.Size = new Size(245, 30);
			btnExtractPayload.Click += BtnExtractPayload_Click;
			bottomBar.Controls.Add(btnExtractPayload);

			btnOpenBinFolder = new DarkButton();
			btnOpenBinFolder.Text = "📁 Открыть папку $Recycle.Bin";
			btnOpenBinFolder.Font = Ui.FontBody(9f, false);
			btnOpenBinFolder.Location = new Point(495, 10);
			btnOpenBinFolder.Size = new Size(205, 30);
			btnOpenBinFolder.Click += BtnOpenBinFolder_Click;
			bottomBar.Controls.Add(btnOpenBinFolder);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(708, 10);
			btnEvidenceCapture.Size = new Size(185, 30);
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
			btnExportReport.Text = "📄 Экспорт отчета";
			btnExportReport.Font = Ui.FontBody(9f, false);
			btnExportReport.Location = new Point(901, 10);
			btnExportReport.Size = new Size(140, 30);
			btnExportReport.Click += BtnExportReport_Click;
			bottomBar.Controls.Add(btnExportReport);

			btnRefreshNow = new DarkButton();
			btnRefreshNow.Text = "🔄 F5";
			btnRefreshNow.Font = Ui.FontBody(9f, false);
			btnRefreshNow.Location = new Point(bottomBar.Width - 90, 10);
			btnRefreshNow.Size = new Size(75, 30);
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

			listView.Columns.Add("Имя файла", 200);
			listView.Columns.Add("Время удаления", 150);
			listView.Columns.Add("Размер", 100);
			listView.Columns.Add("Оригинальный путь до удаления", 340);
			listView.Columns.Add("Вердикт / Состояние в корзине", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ СВЕДЕНИЯ ОБ УДАЛЕННОМ ФАЙЛЕ ИЗ ИНДЕКСА $I:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из списка для детального анализа.";
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

		private DarkButton CreateFilterButton(string text, BinFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == BinFilterMode.All);
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
			btnFilterAll.AccentStyle = (currentFilter == BinFilterMode.All);
			btnFilterCheats.AccentStyle = (currentFilter == BinFilterMode.CheatsOnly);
			btnFilterRecent.AccentStyle = (currentFilter == BinFilterMode.RecentDeletions);
			btnFilterPayloads.AccentStyle = (currentFilter == BinFilterMode.PayloadsPresent);
			btnFilterClean.AccentStyle = (currentFilter == BinFilterMode.CleanFiles);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Сканирование корзины...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<RecycleBinItem> results = new List<RecycleBinItem>();

				DriveInfo[] drives = new DriveInfo[0];
				try { drives = DriveInfo.GetDrives(); } catch { }

				DateTime now = DateTime.Now;

				foreach (DriveInfo drive in drives)
				{
					if (!drive.IsReady) continue;
					string binPath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
					if (!Directory.Exists(binPath)) continue;

					try
					{
						string[] userDirs = Directory.GetDirectories(binPath);
						foreach (string userDir in userDirs)
						{
							try
							{
								string[] iFiles = Directory.GetFiles(userDir, "$I*.*", SearchOption.TopDirectoryOnly);
								foreach (string iFile in iFiles)
								{
									RecycleBinItem item = ParseIndexFile(iFile, drive.Name.TrimEnd('\\'), now);
									if (item != null)
									{
										results.Add(item);
									}
								}
							}
							catch { }
						}
					}
					catch { }
				}

				// Сортировка по времени удаления (сначала самые свежие)
				results.Sort(delegate(RecycleBinItem a, RecycleBinItem b)
				{
					return b.DeletionTime.CompareTo(a.DeletionTime);
				});

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int cheatCount = results.Count(delegate(RecycleBinItem x) { return x.IsCheat; });

					if (cheatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 ЧИТОВ В КОРЗИНЕ: {0}", cheatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Записей корзины: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private RecycleBinItem ParseIndexFile(string iFilePath, string driveLetter, DateTime now)
		{
			try
			{
				byte[] bytes = File.ReadAllBytes(iFilePath);
				if (bytes.Length < 28) return null;

				long headerVersion = BitConverter.ToInt64(bytes, 0);
				long fileSize = 0;
				long fileTime = 0;
				string origPath = string.Empty;

				if (headerVersion == 1) // Windows Vista / 7 / 8
				{
					fileSize = BitConverter.ToInt64(bytes, 8);
					fileTime = BitConverter.ToInt64(bytes, 16);
					origPath = Encoding.Unicode.GetString(bytes, 24, bytes.Length - 24).TrimEnd('\0');
				}
				else if (headerVersion == 2) // Windows 10 / 11
				{
					fileSize = BitConverter.ToInt64(bytes, 8);
					fileTime = BitConverter.ToInt64(bytes, 16);
					int pathLenChars = BitConverter.ToInt32(bytes, 24);
					int pathByteLen = pathLenChars * 2;
					if (pathByteLen > 0 && 28 + pathByteLen <= bytes.Length)
					{
						origPath = Encoding.Unicode.GetString(bytes, 28, pathByteLen).TrimEnd('\0');
					}
					else
					{
						origPath = Encoding.Unicode.GetString(bytes, 28, bytes.Length - 28).TrimEnd('\0');
					}
				}
				else
				{
					// Неизвестный заголовок, пробуем найти строку
					fileSize = BitConverter.ToInt64(bytes, 8);
					fileTime = BitConverter.ToInt64(bytes, 16);
					origPath = Encoding.Unicode.GetString(bytes, 24, bytes.Length - 24).TrimEnd('\0');
				}

				DateTime delTime = DateTime.MinValue;
				if (fileTime > 0)
				{
					try { delTime = DateTime.FromFileTimeUtc(fileTime).ToLocalTime(); } catch { }
				}

				string iFileName = Path.GetFileName(iFilePath);
				string rFileName = "$R" + iFileName.Substring(2);
				string rFilePath = Path.Combine(Path.GetDirectoryName(iFilePath), rFileName);
				bool rExists = File.Exists(rFilePath);

				string fileName = Path.GetFileName(origPath);
				if (string.IsNullOrEmpty(fileName)) fileName = origPath;

				bool isCheat = CheatRegex.IsMatch(fileName) || CheatRegex.IsMatch(origPath);
				bool isRecent = delTime != DateTime.MinValue && (now - delTime).TotalHours <= 24;
				bool isEmergency = delTime != DateTime.MinValue && (now - delTime).TotalMinutes <= 15;

				RecycleBinItem item = new RecycleBinItem();
				item.DriveLetter = driveLetter;
				item.OriginalPath = origPath;
				item.FileName = fileName;
				item.Extension = Path.GetExtension(fileName).ToLowerInvariant();
				item.FileSize = fileSize;
				item.DeletionTime = delTime;
				item.IndexFilePath = iFilePath;
				item.PayloadFilePath = rFilePath;
				item.PayloadExists = rExists;
				item.IsCheat = isCheat;
				item.IsRecent = isRecent;
				item.IsEmergency = isEmergency;

				if (isCheat && isEmergency)
				{
					item.Status = "🚨 ЭКСТРЕННОЕ УДАЛЕНИЕ ЧИТА ПРИ ВЫЗОВЕ (5.08)";
					item.RuleViolation = "5.08";
					item.BanReason = string.Format("Удаление чита {0} в корзину при вызове на проверку (удалено {1} мин. назад)", fileName, (int)(now - delTime).TotalMinutes);
					item.ForeColor = Ui.AccentRed;
					item.Details = string.Format("КРИТИЧЕСКОЕ НАРУШЕНИЕ РЕГЛАМЕНТА 5.08!\nИгрок выбросил чит в корзину ПРЯМО ПРИ ВЫЗОВЕ НА ПРОВЕРКУ!\nФайл: {0}\nОригинальный путь: {1}\nВремя удаления: {2:yyyy-MM-dd HH:mm:ss} ({3} минут назад)\nРазмер: {4} байт\nТело файла $R: {5}\nЭто прямое доказательство сокрытия улик.",
						fileName, origPath, delTime, (int)(now - delTime).TotalMinutes, fileSize, rExists ? "СОХРАНИЛОСЬ В КОРЗИНЕ (можно извлечь)" : "Очищено");
				}
				else if (isCheat)
				{
					item.Status = "🚨 ЧИТ УДАЛЕН В КОРЗИНУ (5.08 / 5.01)";
					item.RuleViolation = "5.08";
					item.BanReason = string.Format("Удаление чита {0} перед проверкой (найдено в $Recycle.Bin)", fileName);
					item.ForeColor = Ui.AccentRed;
					item.Details = string.Format("НАЙДЕНЫ СЛЕДЫ УДАЛЕНИЯ ЧИТА В $RECYCLE.BIN!\nФайл: {0}\nПуть: {1}\nВремя удаления: {2:yyyy-MM-dd HH:mm:ss}\nРазмер: {3} байт\nТело файла $R: {4}\nНарушение: Пункт 5.08 (Удаление софта/улик перед проверкой).",
						fileName, origPath, delTime, fileSize, rExists ? "Присутствует" : "Очищено из корзины");
				}
				else if (isRecent)
				{
					item.Status = rExists ? "Свежеудаленный файл (в корзине)" : "Удаленная запись индекса $I";
					item.RuleViolation = "-";
					item.BanReason = "-";
					item.ForeColor = Ui.AccentOrange;
					item.Details = string.Format("Файл: {0}\nОригинальный путь: {1}\nВремя: {2:yyyy-MM-dd HH:mm:ss}\nРазмер: {3} байт\nСтатус тела $R: {4}",
						fileName, origPath, delTime, fileSize, rExists ? "Доступно" : "Очищено");
				}
				else
				{
					item.Status = rExists ? "Файл в корзине (Чист)" : "Индекс удаленного файла";
					item.RuleViolation = "-";
					item.BanReason = "-";
					item.ForeColor = Ui.TextSecondary;
					item.Details = string.Format("Файл: {0}\nПуть: {1}\nВремя удаления: {2:yyyy-MM-dd HH:mm:ss}\nРазмер: {3} байт", fileName, origPath, delTime, fileSize);
				}

				return item;
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (RecycleBinItem item in allEntries)
			{
				if (currentFilter == BinFilterMode.CheatsOnly && !item.IsCheat) continue;
				if (currentFilter == BinFilterMode.RecentDeletions && !item.IsRecent) continue;
				if (currentFilter == BinFilterMode.PayloadsPresent && !item.PayloadExists) continue;
				if (currentFilter == BinFilterMode.CleanFiles && item.IsCheat) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.FileName != null && item.FileName.ToLowerInvariant().Contains(search)) ||
								   (item.OriginalPath != null && item.OriginalPath.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.FileName);
				lvi.SubItems.Add(item.DeletionTime == DateTime.MinValue ? "—" : item.DeletionTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(FormatFileSize(item.FileSize));
				lvi.SubItems.Add(item.OriginalPath);
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

		private string FormatFileSize(long bytes)
		{
			if (bytes >= 1024 * 1024) return string.Format("{0:F2} МБ", (double)bytes / (1024 * 1024));
			if (bytes >= 1024) return string.Format("{0:F1} КБ", (double)bytes / 1024);
			return bytes + " Б";
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetails();
				return;
			}

			RecycleBinItem item = listView.SelectedItems[0].Tag as RecycleBinItem;
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
			lblDetailVerdict.Text = "Файл не выбран";
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
				MessageBox.Show("Выберите удаленный файл с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			RecycleBinItem item = listView.SelectedItems[0].Tag as RecycleBinItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранного файла нарушений не зафиксировано.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExtractPayload_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			RecycleBinItem item = listView.SelectedItems[0].Tag as RecycleBinItem;
			if (item == null) return;

			if (!item.PayloadExists || !File.Exists(item.PayloadFilePath))
			{
				MessageBox.Show("Тело файла ($R) уже очищено из корзины. Сохранилась только индексная запись ($I).", "Файл очищен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				string dest = Path.Combine(desktop, "Восстановлено_" + item.FileName);
				File.Copy(item.PayloadFilePath, dest, true);
				MessageBox.Show("Файл успешно извлечен из корзины на Рабочий стол:\n" + dest, "Файл извлечен", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось извлечь файл: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnOpenBinFolder_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			RecycleBinItem item = listView.SelectedItems[0].Tag as RecycleBinItem;
			if (item == null) return;

			try
			{
				string dir = Path.GetDirectoryName(item.IndexFilePath);
				if (Directory.Exists(dir))
				{
					Process.Start("explorer.exe", string.Format("\"{0}\"", dir));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть каталог корзины: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Корзина_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА $RECYCLE.BIN ($I / $R)");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (RecycleBinItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1} (Удалено: {2:yyyy-MM-dd HH:mm:ss}, Размер: {3} Б)",
							item.DriveLetter, item.FileName, item.DeletionTime, item.FileSize));
						sb.AppendLine(string.Format("Оригинальный путь: {0}", item.OriginalPath));
						sb.AppendLine(string.Format("Тело файла ($R):   {0}", item.PayloadExists ? "СОХРАНИЛОСЬ В КОРЗИНЕ" : "Очищено"));
						sb.AppendLine(string.Format("Вердикт:           {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:         Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:           /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности:       {0}", item.Details));
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
