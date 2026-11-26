using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DirectXShaderCacheViewerForm : Form
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

		private List<ShaderCacheForensicItem> allItems = new List<ShaderCacheForensicItem>();
		private BackgroundWorker worker;
		private bool isScanning = false;

		private static readonly string[] ImguiSignatures = new string[]
		{
			"imgui", "dear imgui", "imdrawvert", "imdrawcmd", "sv_position", "sv_target",
			"position0", "color0", "texcoord0", "texture2d", "samplerstate"
		};

		private static readonly string[] CheatSignatures = new string[]
		{
			"celestial", "expensive", "nursultan", "akrien", "vape", "wild",
			"midnight", "exptent", "renderesp", "chams", "tracers", "targetesp",
			"glowesp", "customfont", "blurshader", "killaura", "freecam", "nametags"
		};

		public DirectXShaderCacheViewerForm(string playerName)
		{
			this.targetPlayerName = string.IsNullOrEmpty(playerName) ? "Подозрительный игрок" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			this.Text = "Форензика кэша шейдеров DirectX, NVidia и AMD (Shader Cache) - " + this.targetPlayerName;
			this.Size = new Size(1180, 720);
			this.MinimumSize = new Size(950, 580);
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
			titleLabel.Text = "🎮 АУДИТ КЭША ШЕЙДЕРОВ DIRECTX / GPU (D3DSCACHE & DXCACHE)";
			titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
			titleLabel.ForeColor = Ui.AccentTeal;
			titleLabel.AutoSize = true;
			titleLabel.Location = new Point(15, 10);
			topPanel.Controls.Add(titleLabel);

			summaryLabel = new Label();
			summaryLabel.Text = "Сканирование %LOCALAPPDATA%\\D3DSCache, NVidia DXCache, AMD DxCache...";
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
			lblFilter.Text = "Категория:";
			lblFilter.AutoSize = true;
			lblFilter.Location = new Point(345, 75);
			lblFilter.ForeColor = Ui.TextMuted;
			topPanel.Controls.Add(lblFilter);

			filterCombo = new ComboBox();
			filterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			filterCombo.BackColor = Ui.SurfaceAlt;
			filterCombo.ForeColor = Ui.TextPrimary;
			filterCombo.FlatStyle = FlatStyle.Flat;
			filterCombo.Location = new Point(420, 72);
			filterCombo.Size = new Size(210, 25);
			filterCombo.Items.AddRange(new object[] { "Все файлы шейдеров", "🚨 Чит-сигнатуры / ImGui", "⚠️ Свежие компиляции (< 1 ч)", "🧹 Следы зачистки (5.08)" });
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
			split.SplitterDistance = 380;
			split.Panel1MinSize = 200;
			split.Panel2MinSize = 100;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;

			listView.Columns.Add("Статус вердикта", 190);
			listView.Columns.Add("Каталог / GPU провайдер", 150);
			listView.Columns.Add("Имя файла кэша", 210);
			listView.Columns.Add("Размер", 85);
			listView.Columns.Add("Время компиляции (LastWrite)", 150);
			listView.Columns.Add("Время создания (Creation)", 150);
			listView.Columns.Add("Обнаруженные артефакты и маркеры", 280);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenSelectedFileFolder(); };

			split.Panel1.Controls.Add(listView);

			// Нижняя панель деталей
			Panel detailContainer = new Panel();
			detailContainer.Dock = DockStyle.Fill;
			detailContainer.BackColor = Ui.SurfaceAlt;
			detailContainer.Padding = new Padding(10, 8, 10, 8);

			Label detailHeader = new Label();
			detailHeader.Text = "🔍 ИНСПЕКТОР БИНАРНОГО СОДЕРЖИМОГО И СТРОК ВЫБРАННОГО ШЕЙДЕРА:";
			detailHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			detailHeader.ForeColor = Ui.AccentTeal;
			detailHeader.Dock = DockStyle.Top;
			detailHeader.Height = 22;
			detailContainer.Controls.Add(detailHeader);

			detailBox = new DarkTextBox();
			detailBox.Dock = DockStyle.Fill;
			detailBox.Multiline = true;
			detailBox.ScrollBars = ScrollBars.Both;
			detailBox.Font = new Font("Consolas", 8.5f);
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
			statusLabel.Text = "Готов к сканированию кэшей DirectX.";
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
			statusLabel.Text = "Сбор файлов DirectX / NVidia / AMD Shader Cache...";
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
			List<ShaderCacheForensicItem> results = new List<ShaderCacheForensicItem>();
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			// Целевые пути кэшей GPU
			List<KeyValuePair<string, string>> targetDirs = new List<KeyValuePair<string, string>>();
			targetDirs.Add(new KeyValuePair<string, string>("Direct3D D3DSCache", Path.Combine(localAppData, "D3DSCache")));
			targetDirs.Add(new KeyValuePair<string, string>("NVidia DXCache", Path.Combine(localAppData, @"NVIDIA\DXCache")));
			targetDirs.Add(new KeyValuePair<string, string>("NVidia GLCache", Path.Combine(localAppData, @"NVIDIA\GLCache")));
			targetDirs.Add(new KeyValuePair<string, string>("AMD DxCache", Path.Combine(localAppData, @"AMD\DxCache")));
			targetDirs.Add(new KeyValuePair<string, string>("AMD DxcCache", Path.Combine(localAppData, @"AMD\DxcCache")));
			targetDirs.Add(new KeyValuePair<string, string>("Intel ShaderCache", Path.Combine(localAppData, @"Intel\ShaderCache")));

			DateTime now = DateTime.Now;
			TimeSpan systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount);

			foreach (KeyValuePair<string, string> kvp in targetDirs)
			{
				string providerName = kvp.Key;
				string dirPath = kvp.Value;

				if (!Directory.Exists(dirPath))
				{
					continue;
				}

				try
				{
					string[] files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);

					// Проверка на тотальную зачистку (если папка есть, а файлов 0 при аптайме > 30 минут)
					if (files.Length == 0 && systemUptime.TotalMinutes > 30)
					{
						DirectoryInfo di = new DirectoryInfo(dirPath);
						ShaderCacheForensicItem wipeItem = new ShaderCacheForensicItem();
						wipeItem.Provider = providerName;
						wipeItem.FileName = "[КАТАЛОГ ОЧИЩЕН]";
						wipeItem.FullPath = dirPath;
						wipeItem.FileSize = 0;
						wipeItem.CreationTime = di.CreationTime;
						wipeItem.LastWriteTime = di.LastWriteTime;
						wipeItem.VerdictStatus = "🚨 ОЧИСТКА ПЕРЕД СС (5.08)";
						wipeItem.DetectedArtifacts = "Папка кэша шейдеров пуста! Кэш удален перед проверкой при аптайме " + (int)systemUptime.TotalMinutes + " мин";
						wipeItem.ExtractedText = "Каталог: " + dirPath + "\r\nСтатус: Все файлы шейдеров были удалены пользователем перед проверкой.";
						results.Add(wipeItem);
						continue;
					}

					foreach (string file in files)
					{
						try
						{
							FileInfo fi = new FileInfo(file);
							ShaderCacheForensicItem item = new ShaderCacheForensicItem();
							item.Provider = providerName;
							item.FileName = fi.Name;
							item.FullPath = fi.FullName;
							item.FileSize = fi.Length;
							item.CreationTime = fi.CreationTime;
							item.LastWriteTime = fi.LastWriteTime;

							// Анализ строк и байтов (первые 64 КБ)
							AnalyzeShaderFile(fi, item, now);
							results.Add(item);
						}
						catch { }
					}
				}
				catch { }
			}

			// Сортировка: критические вердикты первыми, затем по времени компиляции (новые выше)
			results.Sort(delegate (ShaderCacheForensicItem a, ShaderCacheForensicItem b)
			{
				bool aCrit = a.VerdictStatus.Contains("5.01") || a.VerdictStatus.Contains("5.08");
				bool bCrit = b.VerdictStatus.Contains("5.01") || b.VerdictStatus.Contains("5.08");
				if (aCrit && !bCrit) return -1;
				if (!aCrit && bCrit) return 1;
				return b.LastWriteTime.CompareTo(a.LastWriteTime);
			});

			e.Result = results;
		}

		private void AnalyzeShaderFile(FileInfo fi, ShaderCacheForensicItem item, DateTime now)
		{
			bool hasCheatMatch = false;
			bool hasImguiMatch = false;
			List<string> foundSignatures = new List<string>();

			StringBuilder textCollector = new StringBuilder();
			textCollector.AppendLine("Файл: " + fi.FullName);
			textCollector.AppendLine("Размер: " + (fi.Length / 1024.0).ToString("0.0") + " КБ (" + fi.Length + " байт)");
			textCollector.AppendLine("Создан: " + fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
			textCollector.AppendLine("Изменён: " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
			textCollector.AppendLine("--------------------------------------------------------------------------------");

			try
			{
				int bytesToRead = (int)Math.Min(fi.Length, 65536);
				byte[] buffer = new byte[bytesToRead];
				using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					int read = fs.Read(buffer, 0, bytesToRead);
					string ascii = Encoding.ASCII.GetString(buffer, 0, read).ToLowerInvariant();
					string unicode = Encoding.Unicode.GetString(buffer, 0, read).ToLowerInvariant();

					// 1. Поиск чит-сигнатур
					foreach (string sig in CheatSignatures)
					{
						if (ascii.Contains(sig) || unicode.Contains(sig))
						{
							hasCheatMatch = true;
							if (!foundSignatures.Contains(sig)) foundSignatures.Add(sig);
						}
					}

					// 2. Поиск ImGui сигнатур (характерных для оверлеев читов)
					int imguiHitCount = 0;
					foreach (string img in ImguiSignatures)
					{
						if (ascii.Contains(img) || unicode.Contains(img))
						{
							imguiHitCount++;
							if (!foundSignatures.Contains(img)) foundSignatures.Add(img);
						}
					}
					if (imguiHitCount >= 2)
					{
						hasImguiMatch = true;
					}

					// Извлечение читаемых строк для превью
					textCollector.AppendLine("ИЗВЛЕЧЁННЫЕ СТРОКИ ИЗ КЭША ШЕЙДЕРА (ПЕРВЫЕ 64 КБ):");
					MatchCollection matches = Regex.Matches(Encoding.ASCII.GetString(buffer, 0, read), @"[\x20-\x7E]{4,}");
					int lineCount = 0;
					foreach (Match m in matches)
					{
						textCollector.AppendLine("  > " + m.Value);
						lineCount++;
						if (lineCount > 100)
						{
							textCollector.AppendLine("  ... [остальные строки усечены для читаемости]");
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				textCollector.AppendLine("Ошибка чтения содержимого файла: " + ex.Message);
			}

			item.ExtractedText = textCollector.ToString();

			TimeSpan age = now - item.LastWriteTime;

			if (hasCheatMatch)
			{
				item.VerdictStatus = "⛔ ЧИТ В КЭШЕ ШЕЙДЕРОВ (5.01)";
				item.DetectedArtifacts = "Сигнатуры чита: " + string.Join(", ", foundSignatures.ToArray());
			}
			else if (hasImguiMatch)
			{
				item.VerdictStatus = "🟣 ОВЕРЛЕЙ IMGUI В ШЕЙДЕРЕ (Проверить)";
				item.DetectedArtifacts = "Рендер-пайплайн ImGui: " + string.Join(", ", foundSignatures.ToArray());
			}
			else if (age.TotalMinutes <= 60)
			{
				item.VerdictStatus = "🟡 СВЕЖИЙ ШЕЙДЕР (" + (int)age.TotalMinutes + " мин назад)";
				item.DetectedArtifacts = (foundSignatures.Count > 0) ? string.Join(", ", foundSignatures.ToArray()) : "Стандартный шейдер GPU";
			}
			else
			{
				item.VerdictStatus = "🟢 Обычный кэш шейдера";
				item.DetectedArtifacts = (foundSignatures.Count > 0) ? string.Join(", ", foundSignatures.ToArray()) : "Шейдер приложения";
			}
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			isScanning = false;
			btnRefresh.Enabled = true;
			progressBar.Visible = false;

			if (e.Result is List<ShaderCacheForensicItem>)
			{
				allItems = (List<ShaderCacheForensicItem>)e.Result;
			}

			ApplyFilter();

			int cheatCount = 0;
			int freshCount = 0;
			int wipeCount = 0;
			foreach (ShaderCacheForensicItem it in allItems)
			{
				if (it.VerdictStatus.Contains("5.01")) cheatCount++;
				else if (it.VerdictStatus.Contains("5.08")) wipeCount++;
				else if (it.VerdictStatus.Contains("СВЕЖИЙ")) freshCount++;
			}

			summaryLabel.Text = string.Format("Найдено файлов кэша GPU: {0} | Чит-сигнатур: {1} | Зачисток (5.08): {2} | Свежих (<1ч): {3}",
				allItems.Count, cheatCount, wipeCount, freshCount);

			if (cheatCount > 0 || wipeCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentRed;
			}
			else if (freshCount > 0)
			{
				summaryLabel.ForeColor = Ui.AccentOrange;
			}
			else
			{
				summaryLabel.ForeColor = Ui.AccentGreen;
			}

			statusLabel.Text = "Аудит завершен. Выберите элемент для просмотра содержимого шейдера.";
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();
			int filterIndex = filterCombo.SelectedIndex;

			foreach (ShaderCacheForensicItem item in allItems)
			{
				if (filterIndex == 1 && !item.VerdictStatus.Contains("5.01") && !item.VerdictStatus.Contains("IMGUI"))
				{
					continue;
				}
				if (filterIndex == 2 && !item.VerdictStatus.Contains("СВЕЖИЙ") && !item.VerdictStatus.Contains("5.01"))
				{
					continue;
				}
				if (filterIndex == 3 && !item.VerdictStatus.Contains("5.08"))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(search))
				{
					string combined = (item.FileName + " " + item.Provider + " " + item.DetectedArtifacts + " " + item.VerdictStatus).ToLowerInvariant();
					if (!combined.Contains(search))
					{
						continue;
					}
				}

				ListViewItem lvi = new ListViewItem(item.VerdictStatus);
				lvi.SubItems.Add(item.Provider);
				lvi.SubItems.Add(item.FileName);
				lvi.SubItems.Add(FormatSize(item.FileSize));
				lvi.SubItems.Add(item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
				lvi.SubItems.Add(item.DetectedArtifacts);
				lvi.Tag = item;

				if (item.VerdictStatus.Contains("5.01") || item.VerdictStatus.Contains("5.08"))
				{
					lvi.ForeColor = Ui.AccentRed;
					lvi.BackColor = Color.FromArgb(45, 20, 20);
				}
				else if (item.VerdictStatus.Contains("IMGUI"))
				{
					lvi.ForeColor = Ui.AccentPurple;
					lvi.BackColor = Color.FromArgb(35, 25, 45);
				}
				else if (item.VerdictStatus.Contains("СВЕЖИЙ"))
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

			ShaderCacheForensicItem item = listView.SelectedItems[0].Tag as ShaderCacheForensicItem;
			if (item != null)
			{
				detailBox.Text = item.ExtractedText;
			}
		}

		private void OpenSelectedFileFolder()
		{
			if (listView.SelectedItems.Count == 0) return;
			ShaderCacheForensicItem item = listView.SelectedItems[0].Tag as ShaderCacheForensicItem;
			if (item == null) return;

			try
			{
				if (File.Exists(item.FullPath))
				{
					System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + item.FullPath + "\"");
				}
				else if (Directory.Exists(item.FullPath))
				{
					System.Diagnostics.Process.Start("explorer.exe", "\"" + item.FullPath + "\"");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть каталог: " + ex.Message, "Ошибка Проводника", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyBanVerdict()
		{
			if (listView.SelectedItems.Count == 0)
			{
				// Если ничего не выбрано, ищем первый критический элемент
				ShaderCacheForensicItem crit = allItems.Find(delegate (ShaderCacheForensicItem i) { return i.VerdictStatus.Contains("5.01") || i.VerdictStatus.Contains("5.08"); });
				if (crit != null)
				{
					CopyVerdictForItem(crit);
					return;
				}
				MessageBox.Show("Выберите строку с обнаруженным нарушением для копирования вердикта.", "Выбор строки", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ShaderCacheForensicItem selected = listView.SelectedItems[0].Tag as ShaderCacheForensicItem;
			if (selected != null)
			{
				CopyVerdictForItem(selected);
			}
		}

		private void CopyVerdictForItem(ShaderCacheForensicItem item)
		{
			string verdict;
			if (item.VerdictStatus.Contains("5.08"))
			{
				verdict = string.Format("/ban {0} 30d 5.08 Очистка следов перед проверкой (Удален кэш шейдеров GPU {1})", targetPlayerName, item.Provider);
			}
			else if (item.VerdictStatus.Contains("5.01"))
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (Шейдер чита {1} в кэше {2})", targetPlayerName, item.DetectedArtifacts, item.Provider);
			}
			else
			{
				verdict = string.Format("/ban {0} 30d 5.01 Использование читов (Подозрительный ImGui шейдер: {1})", targetPlayerName, item.FileName);
			}

			try
			{
				Clipboard.SetText(verdict);
				MessageBox.Show("Вердикт успешно скопирован в буфер обмена:\r\n\r\n" + verdict, "Бан-вердикт скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка буфера обмена: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportEvidence()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите файл кэша шейдера для экспорта улики.", "Экспорт доказательства", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ShaderCacheForensicItem item = listView.SelectedItems[0].Tag as ShaderCacheForensicItem;
			if (item == null) return;

			string sha256 = "Н/Д";
			if (File.Exists(item.FullPath))
			{
				try
				{
					using (SHA256 sha = SHA256.Create())
					using (FileStream fs = File.OpenRead(item.FullPath))
					{
						byte[] hash = sha.ComputeHash(fs);
						StringBuilder sb = new StringBuilder();
						foreach (byte b in hash) sb.Append(b.ToString("x2"));
						sha256 = sb.ToString();
					}
				}
				catch { }
			}

			StringBuilder evidence = new StringBuilder();
			evidence.AppendLine("=== СУДЕБНАЯ УЛИКА SCREENSHARE: GPU SHADER CACHE FORENSICS ===");
			evidence.AppendLine("Игрок: " + targetPlayerName);
			evidence.AppendLine("Дата и время фиксации: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Провайдер / API: " + item.Provider);
			evidence.AppendLine("Файл: " + item.FullPath);
			evidence.AppendLine("Размер: " + item.FileSize + " байт");
			evidence.AppendLine("SHA-256 хэш: " + sha256);
			evidence.AppendLine("Время компиляции (LastWrite): " + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Время создания: " + item.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
			evidence.AppendLine("Вердикт: " + item.VerdictStatus);
			evidence.AppendLine("Обнаруженные артефакты: " + item.DetectedArtifacts);
			evidence.AppendLine("----------------------------------------------------------------");
			evidence.AppendLine(item.ExtractedText);

			try
			{
				Clipboard.SetText(evidence.ToString());
				MessageBox.Show("Криминалистическая улика (включая SHA-256) успешно скопирована в буфер обмена!", "Улика зафиксирована", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка копирования улики: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static string FormatSize(long bytes)
		{
			if (bytes == 0) return "0 Б";
			if (bytes < 1024) return bytes + " Б";
			if (bytes < 1048576) return (bytes / 1024.0).ToString("0.0") + " КБ";
			return (bytes / 1048576.0).ToString("0.0") + " МБ";
		}
	}

	public class ShaderCacheForensicItem
	{
		public string Provider;
		public string FileName;
		public string FullPath;
		public long FileSize;
		public DateTime CreationTime;
		public DateTime LastWriteTime;
		public string VerdictStatus;
		public string DetectedArtifacts;
		public string ExtractedText;
	}
}
