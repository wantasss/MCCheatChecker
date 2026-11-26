using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class JarStructureViewerForm : Form
	{
		private class JarClassEntry
		{
			public int Index;
			public string FullPath;
			public long UncompressedSize;
			public long CompressedSize;
			public bool IsSuspicious;
			public string DetectionReason;
			public List<string> ExtractedStrings = new List<string>();
		}

		private string targetPlayerName;
		private string currentJarPath;
		private List<JarClassEntry> classEntries = new List<JarClassEntry>();
		private string modMetadataText = "";

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkCheckBox chkOnlySuspicious;
		private Label lblHeaderTitle;
		private Label lblJarInfo;
		private Label lblStatusPill;
		private Label lblSelectedEntryTitle;
		private DarkTextBoxMultiline txtModMeta;
		private DarkTextBoxMultiline txtStringsPreview;

		private static readonly Regex CheatClassPattern = new Regex(
			@"(?i)(aura|aimbot|killaura|hitbox|velocity|reach|nofall|jesus|speed|fly|timer|scaffold|esp|tracer|chestesp|xray|freecam|autototem|totem|middleclick|fastplace|autobuy|auction|crystal|anchor|bedfucker|baritone|meteor|wurst|liquidbounce|celestial|expensive|richclient|akrien|nurik|nursultan|thunderhack|vape|driplite|boze|abyss|slinky)",
			RegexOptions.Compiled);

		public JarStructureViewerForm(string jarPath = null, string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			currentJarPath = jarPath;
			InitializeComponent();

			if (!string.IsNullOrEmpty(currentJarPath) && File.Exists(currentJarPath))
			{
				InspectJarFileAsync(currentJarPath);
			}
			else
			{
				PromptSelectJar();
			}
		}

		private void InitializeComponent()
		{
			Text = "📦 Инспектор структуры JAR-архивов и байткода классов (Mod & Bytecode Forensics)";
			Size = new Size(1180, 740);
			MinimumSize = new Size(920, 560);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 100;
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
			lblHeaderTitle.Text = "📦  ИНСПЕКТОР СТРУКТУРЫ JAR И БАЙТКОДА КЛАССОВ (In-Memory Constant Pool Inspector)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblJarInfo = new Label();
			lblJarInfo.Text = "Выберите JAR-файл мода для анализа внутренней структуры классов...";
			lblJarInfo.Font = Ui.FontBody(8.5f);
			lblJarInfo.ForeColor = Ui.TextSecondary;
			lblJarInfo.Location = new Point(12, 30);
			lblJarInfo.AutoSize = true;
			topPanel.Controls.Add(lblJarInfo);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Ожидание выбора файла...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(600, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnBrowse = new DarkButton();
			btnBrowse.Text = "📁 Выбрать JAR...";
			btnBrowse.Location = new Point(885, 8);
			btnBrowse.Size = new Size(135, 26);
			btnBrowse.BackColor = Ui.SurfaceAlt;
			btnBrowse.ForeColor = Ui.TextPrimary;
			btnBrowse.Cursor = Cursors.Hand;
			btnBrowse.Click += delegate
			{
				PromptSelectJar();
			};
			topPanel.Controls.Add(btnBrowse);

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

			// Sub Toolbar
			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1140, 40);
			navRow.BackColor = Color.Transparent;

			chkOnlySuspicious = new DarkCheckBox();
			chkOnlySuspicious.Text = "🚨 Только подозрительные классы читов";
			chkOnlySuspicious.Location = new Point(0, 5);
			chkOnlySuspicious.Size = new Size(270, 20);
			chkOnlySuspicious.ForeColor = Ui.AccentRed;
			chkOnlySuspicious.Checked = false;
			chkOnlySuspicious.CheckedChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(chkOnlySuspicious);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск класса / строки:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(280, 7);
			lblSearch.AutoSize = true;
			navRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(430, 4);
			searchBox.Size = new Size(240, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(676, 4);
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

			// SplitContainer
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Vertical;
			split.SplitterDistance = 620;
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
			listView.Columns.Add("Путь к классу / Ресурсу в JAR", 350, HorizontalAlignment.Left);
			listView.Columns.Add("Размер", 80, HorizontalAlignment.Right);
			listView.Columns.Add("Статус / Детект", 140, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdateEntryPreview();
			};

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();

			ToolStripMenuItem copyClassNameItem = new ToolStripMenuItem("📋  Скопировать имя класса");
			copyClassNameItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					Clipboard.SetText(listView.SelectedItems[0].SubItems[1].Text);
				}
			};

			ToolStripMenuItem copyStringsItem = new ToolStripMenuItem("📄  Скопировать строковые константы байткода");
			copyStringsItem.Click += delegate
			{
				if (!string.IsNullOrEmpty(txtStringsPreview.Text))
				{
					Clipboard.SetText(txtStringsPreview.Text);
					MessageBox.Show("Строковые константы скопированы в буфер обмена!", "Байткод", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};

			ToolStripMenuItem copyReportItem = new ToolStripMenuItem("📋  Скопировать отчёт инспекции JAR");
			copyReportItem.Click += delegate
			{
				CopyJarReport();
			};

			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Сгенерировать /ban 5.01 за чит в JAR");
			copyBanItem.Click += delegate
			{
				CopyBanCommand();
			};

			ctx.Items.Add(copyClassNameItem);
			ctx.Items.Add(copyStringsItem);
			ctx.Items.Add(copyReportItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(copyBanItem);
			listView.ContextMenuStrip = ctx;

			split.Panel1.Controls.Add(listView);

			// Right Details Panel
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;
			rightPanel.Padding = new Padding(10);

			Label lblMetaHeader = new Label();
			lblMetaHeader.Dock = DockStyle.Top;
			lblMetaHeader.Height = 22;
			lblMetaHeader.Font = Ui.FontTitle(9f, true);
			lblMetaHeader.ForeColor = Ui.AccentTeal;
			lblMetaHeader.Text = "📄 Метаданные мода (fabric.mod.json / mcmod.info / MANIFEST):";
			rightPanel.Controls.Add(lblMetaHeader);

			txtModMeta = new DarkTextBoxMultiline();
			txtModMeta.Dock = DockStyle.Top;
			txtModMeta.Height = 110;
			txtModMeta.Font = Ui.FontMono(8f);
			txtModMeta.ReadOnly = true;
			txtModMeta.WordWrap = true;
			txtModMeta.ScrollBars = ScrollBars.Vertical;
			rightPanel.Controls.Add(txtModMeta);

			lblSelectedEntryTitle = new Label();
			lblSelectedEntryTitle.Dock = DockStyle.Top;
			lblSelectedEntryTitle.Height = 30;
			lblSelectedEntryTitle.Font = Ui.FontTitle(9f, true);
			lblSelectedEntryTitle.ForeColor = Ui.AccentOrange;
			lblSelectedEntryTitle.Text = "Строковые константы выбранного класса:";
			rightPanel.Controls.Add(lblSelectedEntryTitle);

			txtStringsPreview = new DarkTextBoxMultiline();
			txtStringsPreview.Dock = DockStyle.Fill;
			txtStringsPreview.Font = Ui.FontMono(8.5f);
			txtStringsPreview.ReadOnly = true;
			txtStringsPreview.WordWrap = false;
			txtStringsPreview.ScrollBars = ScrollBars.Both;
			rightPanel.Controls.Add(txtStringsPreview);
			txtStringsPreview.BringToFront();

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

			Label lblHelp = new Label();
			lblHelp.Text = "ℹ️ Декодирование пула констант (CONSTANT_Utf8) Java-классов в памяти без сохранения файлов на диске.";
			lblHelp.Font = Ui.FontBody(8f);
			lblHelp.ForeColor = Ui.AccentGreen;
			lblHelp.Location = new Point(12, 10);
			lblHelp.AutoSize = true;
			bottomPanel.Controls.Add(lblHelp);

			DarkButton btnCopyReport = new DarkButton();
			btnCopyReport.Text = "📋 Скопировать протокол JAR";
			btnCopyReport.Size = new Size(200, 24);
			btnCopyReport.Location = new Point(940, 6);
			btnCopyReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyReport.ForeColor = Color.White;
			btnCopyReport.Cursor = Cursors.Hand;
			btnCopyReport.Click += delegate
			{
				CopyJarReport();
			};
			bottomPanel.Controls.Add(btnCopyReport);

			Controls.Add(bottomPanel);
		}

		public void PromptSelectJar()
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Title = "Выберите JAR или ZIP файл мода для инспекции";
				ofd.Filter = "Java архивы (*.jar;*.zip)|*.jar;*.zip|Все файлы (*.*)|*.*";
				string modsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods");
				if (Directory.Exists(modsDir)) ofd.InitialDirectory = modsDir;

				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					currentJarPath = ofd.FileName;
					InspectJarFileAsync(currentJarPath);
				}
			}
		}

		public void InspectJarFileAsync(string jarPath)
		{
			if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath)) return;

			lblStatusPill.Text = "⏳ Чтение структуры JAR...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblJarInfo.Text = "Файл: " + Path.GetFileName(jarPath) + " (" + (new FileInfo(jarPath).Length / 1024) + " КБ) | Путь: " + jarPath;

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<JarClassEntry> entries = new List<JarClassEntry>();
				StringBuilder metaSb = new StringBuilder();

				try
				{
					using (FileStream fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
					{
						int idx = 1;
						foreach (ZipArchiveEntry entry in zip.Entries)
						{
							string name = entry.FullName;
							string lower = name.ToLowerInvariant();

							// Metadata check
							if (lower.EndsWith("fabric.mod.json") || lower.EndsWith("mcmod.info") || lower.EndsWith("manifest.mf"))
							{
								try
								{
									using (Stream es = entry.Open())
									using (StreamReader sr = new StreamReader(es, Encoding.UTF8))
									{
										string txt = sr.ReadToEnd();
										metaSb.AppendLine("=== " + name + " ===");
										metaSb.AppendLine(txt.Length > 2000 ? txt.Substring(0, 2000) + "\n...[Обрезано]" : txt);
										metaSb.AppendLine();
									}
								}
								catch { }
							}

							// Class check
							if (lower.EndsWith(".class"))
							{
								bool isSuspName = CheatClassPattern.IsMatch(name);
								JarClassEntry cEntry = new JarClassEntry
								{
									Index = idx++,
									FullPath = name,
									UncompressedSize = entry.Length,
									CompressedSize = entry.CompressedLength,
									IsSuspicious = isSuspName,
									DetectionReason = isSuspName ? "🚨 Чит-имя класса" : "Штатный класс"
								};

								// Parse constant pool strings in-memory
								try
								{
									using (Stream es = entry.Open())
									using (MemoryStream ms = new MemoryStream())
									{
										es.CopyTo(ms);
										byte[] bytes = ms.ToArray();
										cEntry.ExtractedStrings = ExtractConstantPoolStrings(bytes);

										// Check extracted strings for cheat commands or markers
										foreach (string str in cEntry.ExtractedStrings)
										{
											if (str.StartsWith(".") || str.StartsWith("#") || CheatClassPattern.IsMatch(str))
											{
												if (str.Contains("killaura") || str.Contains("velocity") || str.Contains("vape") || str.Contains("celestial"))
												{
													cEntry.IsSuspicious = true;
													cEntry.DetectionReason = "🚨 Строка чита: " + str;
													break;
												}
											}
										}
									}
								}
								catch { }

								entries.Add(cEntry);
							}
						}
					}
				}
				catch (Exception exZip)
				{
					metaSb.AppendLine("Ошибка чтения ZIP-архива: " + exZip.Message);
				}

				e.Result = new Tuple<List<JarClassEntry>, string>(entries, metaSb.ToString());
			};

			bw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					lblStatusPill.Text = "❌ Ошибка: " + e.Error.Message;
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				var res = e.Result as Tuple<List<JarClassEntry>, string>;
				classEntries = res != null ? res.Item1 : new List<JarClassEntry>();
				modMetadataText = res != null ? res.Item2 : "";

				txtModMeta.Text = string.IsNullOrEmpty(modMetadataText) ? "Метаданные (fabric.mod.json / mcmod.info) не найдены." : modMetadataText;

				int totalClasses = classEntries.Count;
				int suspClasses = classEntries.Count(c => c.IsSuspicious);

				if (suspClasses > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ ЧИТ-КЛАССЫ ({0} из {1})", suspClasses, totalClasses);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = string.Format("✅ Проверено {0} классов (Чисто)", totalClasses);
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private static List<string> ExtractConstantPoolStrings(byte[] bytes)
		{
			List<string> strings = new List<string>();
			if (bytes == null || bytes.Length < 10) return strings;

			// Verify CAFEBABE
			if (bytes[0] != 0xCA || bytes[1] != 0xFE || bytes[2] != 0xBA || bytes[3] != 0xBE) return strings;

			int offset = 8;
			int cpCount = (bytes[offset] << 8) | bytes[offset + 1];
			offset += 2;

			for (int i = 1; i < cpCount && offset < bytes.Length; i++)
			{
				byte tag = bytes[offset++];
				switch (tag)
				{
					case 1: // CONSTANT_Utf8
						if (offset + 2 > bytes.Length) return strings;
						int len = (bytes[offset] << 8) | bytes[offset + 1];
						offset += 2;
						if (offset + len <= bytes.Length && len > 0)
						{
							try
							{
								string str = Encoding.UTF8.GetString(bytes, offset, len);
								if (!string.IsNullOrWhiteSpace(str) && str.Length >= 2)
								{
									strings.Add(str);
								}
							}
							catch { }
						}
						offset += len;
						break;
					case 3: // Integer
					case 4: // Float
						offset += 4;
						break;
					case 5: // Long
					case 6: // Double
						offset += 8;
						i++; // Long and Double take two entries in constant pool
						break;
					case 7: // Class
					case 8: // String
					case 16: // MethodType
						offset += 2;
						break;
					case 9: // Fieldref
					case 10: // Methodref
					case 11: // InterfaceMethodref
					case 12: // NameAndType
					case 18: // InvokeDynamic
						offset += 4;
						break;
					case 15: // MethodHandle
						offset += 3;
						break;
					default:
						return strings; // Unknown tag or reached end of pool
				}
			}

			return strings;
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
			bool onlySusp = chkOnlySuspicious.Checked;

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = classEntries.Where(c =>
			{
				if (onlySusp && !c.IsSuspicious) return false;

				if (!string.IsNullOrEmpty(query))
				{
					bool match = c.FullPath.ToLowerInvariant().Contains(query) ||
						c.DetectionReason.ToLowerInvariant().Contains(query) ||
						c.ExtractedStrings.Any(s => s.ToLowerInvariant().Contains(query));
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (JarClassEntry item in filtered)
			{
				string icon = item.IsSuspicious ? "🚨" : "✔";
				ListViewItem lvi = new ListViewItem(icon);
				lvi.SubItems.Add(item.FullPath);
				lvi.SubItems.Add(item.UncompressedSize + " B");
				lvi.SubItems.Add(item.DetectionReason);
				lvi.Tag = item;
				lvi.ForeColor = item.IsSuspicious ? Ui.AccentRed : Ui.TextPrimary;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtStringsPreview.Text = "Нет классов, удовлетворяющих фильтру.";
			}
		}

		private void UpdateEntryPreview()
		{
			if (listView.SelectedItems.Count == 0)
			{
				txtStringsPreview.Text = "";
				return;
			}

			JarClassEntry entry = listView.SelectedItems[0].Tag as JarClassEntry;
			if (entry == null) return;

			lblSelectedEntryTitle.Text = "Строки класса: " + Path.GetFileName(entry.FullPath) +
				string.Format(" ({0} констант, {1} байт)", entry.ExtractedStrings.Count, entry.UncompressedSize);
			lblSelectedEntryTitle.ForeColor = entry.IsSuspicious ? Ui.AccentRed : Ui.AccentTeal;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== СТРОКОВЫЙ ПУЛ КОНСТАНТ БАЙТКОДА ===");
			sb.AppendLine("Класс: " + entry.FullPath);
			sb.AppendLine("Статус: " + entry.DetectionReason);
			sb.AppendLine("Всего UTF-8 констант: " + entry.ExtractedStrings.Count);
			sb.AppendLine();

			foreach (string s in entry.ExtractedStrings)
			{
				bool isSusp = CheatClassPattern.IsMatch(s) || s.StartsWith(".") || s.StartsWith("#");
				if (isSusp) sb.AppendLine(">> [МАРКЕР]: " + s);
				else sb.AppendLine(s);
			}

			txtStringsPreview.Text = sb.ToString();
		}

		private void CopyBanCommand()
		{
			string jarName = Path.GetFileName(currentJarPath ?? "cheat.jar");
			string cmd = string.Format("/ban {0} 30d 5.01 (Чит в JAR: {1})", targetPlayerName, jarName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена!\n\n" + cmd, "Генератор бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyJarReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("           ПРОТОКОЛ ИНСПЕКЦИИ JAR-АРХИВА И БАЙТКОДА КЛАССОВ (SCREENSHARE)       ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Дата и время: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Файл JAR: " + currentJarPath);
			sb.AppendLine("Всего классов в архиве: " + classEntries.Count);
			sb.AppendLine("Подозрительных чит-классов: " + classEntries.Count(c => c.IsSuspicious));
			sb.AppendLine();

			sb.AppendLine("--- НАЙДЕННЫЕ ПОДОЗРИТЕЛЬНЫЕ КЛАССЫ И МАРКЕРЫ ---");
			var suspList = classEntries.Where(c => c.IsSuspicious).ToList();
			if (suspList.Count == 0)
			{
				sb.AppendLine("Чит-классов и вредоносных маркеров в структуре JAR не обнаружено.");
			}
			else
			{
				foreach (var it in suspList)
				{
					sb.AppendLine(string.Format("[{0}] {1} ({2} B)", it.DetectionReason, it.FullPath, it.UncompressedSize));
				}
			}

			sb.AppendLine();
			sb.AppendLine("--- МЕТАДАННЫЕ МОДА ---");
			sb.AppendLine(modMetadataText);

			sb.AppendLine();
			sb.AppendLine("Регламент: Использование читов в модификациях наказывается по пункту 5.01 (30 дней).");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол инспекции JAR скопирован в буфер обмена!", "Инспектор JAR", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
