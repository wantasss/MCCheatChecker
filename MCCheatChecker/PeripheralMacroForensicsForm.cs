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
	public class PeripheralMacroForensicsForm : Form
	{
		private enum MacroFilterCategory
		{
			All,
			SuspiciousOnly,
			Logitech,
			Bloody,
			Razer,
			Glorious,
			Corsair,
			Other,
			Whitelist
		}

		private class MacroItem
		{
			public int Index;
			public string Vendor;
			public string ItemType;
			public string Name;
			public string Path;
			public DateTime Timestamp;
			public string TimeString;
			public bool IsSuspicious;
			public bool IsWhitelisted;
			public string Verdict;
			public string Content;
			public string Analysis;
			public Color ForeColor;
		}

		private MacroFilterCategory currentCategory = MacroFilterCategory.All;
		private List<MacroItem> allItems = new List<MacroItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkCheckBox chkOnlySuspicious;
		private DarkTextBoxMultiline txtPreview;
		private Label lblHeaderStatus;
		private Label lblPreviewTitle;
		private Label lblPreviewVerdict;
		private Label lblPreviewDetails;

		private DarkButton btnCatAll;
		private DarkButton btnCatSuspicious;
		private DarkButton btnCatLogitech;
		private DarkButton btnCatBloody;
		private DarkButton btnCatRazer;
		private DarkButton btnCatGlorious;
		private DarkButton btnCatCorsair;
		private DarkButton btnCatOther;
		private DarkButton btnCatWhitelist;

		public PeripheralMacroForensicsForm()
		{
			InitializeComponent();
			LoadPeripheralDataAsync();
		}

		private void InitializeComponent()
		{
			Text = "🖱️ Аудит ПО периферии и макросов мыши (Logitech / Bloody / Razer / Glorious / iCUE)";
			Size = new Size(1180, 740);
			MinimumSize = new Size(920, 560);
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

			Label lblTitle = new Label();
			lblTitle.Text = "🖱️  АУДИТ ПЕРИФЕРИИ И МАКРОСОВ МЫШИ (Logitech Lua / Bloody Oscar / Razer / Glorious Дебаунс)";
			lblTitle.Font = Ui.FontTitle(10f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.Location = new Point(12, 8);
			lblTitle.AutoSize = true;
			topPanel.Controls.Add(lblTitle);

			lblHeaderStatus = new Label();
			lblHeaderStatus.Text = "Сканирование драйверов, макро-профилей, Lua-скриптов и настроек дебаунса кликов...";
			lblHeaderStatus.Font = Ui.FontBody(8.5f);
			lblHeaderStatus.ForeColor = Ui.TextSecondary;
			lblHeaderStatus.Location = new Point(12, 30);
			lblHeaderStatus.AutoSize = true;
			topPanel.Controls.Add(lblHeaderStatus);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Обновить";
			btnRefresh.Location = new Point(915, 8);
			btnRefresh.Size = new Size(105, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				LoadPeripheralDataAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			chkOnlySuspicious = new DarkCheckBox();
			chkOnlySuspicious.Text = "🚨 Только абузы/скрипты";
			chkOnlySuspicious.Location = new Point(1030, 11);
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
			navRow.Size = new Size(1150, 54);
			navRow.BackColor = Color.Transparent;

			btnCatAll = CreateCategoryButton("🔍 Всё (0)", MacroFilterCategory.All, 90);
			btnCatSuspicious = CreateCategoryButton("🚨 Абузы (0)", MacroFilterCategory.SuspiciousOnly, 105);
			btnCatLogitech = CreateCategoryButton("🖱️ Logitech (0)", MacroFilterCategory.Logitech, 115);
			btnCatBloody = CreateCategoryButton("🩸 Bloody (0)", MacroFilterCategory.Bloody, 105);
			btnCatRazer = CreateCategoryButton("🐍 Razer (0)", MacroFilterCategory.Razer, 100);
			btnCatGlorious = CreateCategoryButton("⚡ Glorious (0)", MacroFilterCategory.Glorious, 110);
			btnCatCorsair = CreateCategoryButton("⚙️ iCUE (0)", MacroFilterCategory.Corsair, 95);
			btnCatOther = CreateCategoryButton("🎮 Прочие (0)", MacroFilterCategory.Other, 95);
			btnCatWhitelist = CreateCategoryButton("✔ Whitelist (0)", MacroFilterCategory.Whitelist, 115);

			int curX = 0;
			DarkButton[] catBtns = { btnCatAll, btnCatSuspicious, btnCatLogitech, btnCatBloody, btnCatRazer, btnCatGlorious, btnCatCorsair, btnCatOther, btnCatWhitelist };
			foreach (DarkButton b in catBtns)
			{
				b.Location = new Point(curX, 0);
				navRow.Controls.Add(b);
				curX += b.Width + 4;
			}

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(curX + 6, 5);
			lblSearch.AutoSize = true;
			navRow.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(curX + 50, 2);
			searchBox.Size = new Size(150, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 204, 2);
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

			// SplitContainer: Left = ListView, Right = Preview Panel
			SplitContainer split = new SplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Vertical;
			split.SplitterDistance = 670;
			split.SplitterWidth = 5;
			split.BackColor = Ui.Border;

			// Left ListView
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontMono(8.5f);
			listView.Columns.Add("№", 45, HorizontalAlignment.Right);
			listView.Columns.Add("Производитель / ПО", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Тип объекта", 130, HorizontalAlignment.Left);
			listView.Columns.Add("Профиль / Файл / Имя", 160, HorizontalAlignment.Left);
			listView.Columns.Add("Вердикт / Результат", 240, HorizontalAlignment.Left);
			listView.Columns.Add("Дата изменения", 130, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdatePreviewDetails();
			};

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

			ToolStripMenuItem copyCodeItem = new ToolStripMenuItem("📄  Скопировать текст скрипта / настроек");
			copyCodeItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					MacroItem item = listView.SelectedItems[0].Tag as MacroItem;
					if (item != null && !string.IsNullOrEmpty(item.Content))
					{
						Clipboard.SetText(item.Content);
						MessageBox.Show("Содержимое скрипта/настроек скопировано в буфер!", "Макросы", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			};

			ToolStripMenuItem openExplorerItem = new ToolStripMenuItem("📁  Открыть расположение файла в Проводнике");
			openExplorerItem.Click += delegate
			{
				OpenSelectedInExplorer();
			};

			ToolStripMenuItem copyReportItem = new ToolStripMenuItem("📋  Скопировать полный протокол аудита периферии");
			copyReportItem.Click += delegate
			{
				CopyAuditReport();
			};

			ctx.Items.Add(copyItem);
			ctx.Items.Add(copyCodeItem);
			ctx.Items.Add(openExplorerItem);
			ctx.Items.Add(new ToolStripSeparator());
			ctx.Items.Add(copyReportItem);
			listView.ContextMenuStrip = ctx;

			split.Panel1.Controls.Add(listView);

			// Right Preview Panel
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;
			rightPanel.Padding = new Padding(10);

			lblPreviewTitle = new Label();
			lblPreviewTitle.Dock = DockStyle.Top;
			lblPreviewTitle.Height = 26;
			lblPreviewTitle.Font = Ui.FontTitle(9.5f, true);
			lblPreviewTitle.ForeColor = Ui.AccentTeal;
			lblPreviewTitle.Text = "Выберите объект для анализа";
			rightPanel.Controls.Add(lblPreviewTitle);

			lblPreviewVerdict = new Label();
			lblPreviewVerdict.Dock = DockStyle.Top;
			lblPreviewVerdict.Height = 32;
			lblPreviewVerdict.Font = Ui.FontBody(8.5f, true);
			lblPreviewVerdict.ForeColor = Ui.TextSecondary;
			lblPreviewVerdict.Text = "—";
			rightPanel.Controls.Add(lblPreviewVerdict);

			lblPreviewDetails = new Label();
			lblPreviewDetails.Dock = DockStyle.Top;
			lblPreviewDetails.Height = 52;
			lblPreviewDetails.Font = Ui.FontBody(8f);
			lblPreviewDetails.ForeColor = Ui.TextMuted;
			lblPreviewDetails.Text = "Здесь отображается детальный анализ макроса, Lua-скрипта или аппаратной задержки.";
			rightPanel.Controls.Add(lblPreviewDetails);

			Panel previewHeader = new Panel();
			previewHeader.Dock = DockStyle.Top;
			previewHeader.Height = 26;
			previewHeader.BackColor = Ui.SurfaceAlt;

			Label lblCodeHeader = new Label();
			lblCodeHeader.Text = "📄 Исходный код / Конфигурация:";
			lblCodeHeader.Font = Ui.FontBody(8f, true);
			lblCodeHeader.ForeColor = Ui.TextSecondary;
			lblCodeHeader.Location = new Point(6, 5);
			lblCodeHeader.AutoSize = true;
			previewHeader.Controls.Add(lblCodeHeader);

			DarkButton btnCopyScript = new DarkButton();
			btnCopyScript.Text = "📋 Скопировать";
			btnCopyScript.Size = new Size(100, 22);
			btnCopyScript.Location = new Point(360, 2);
			btnCopyScript.BackColor = Ui.Surface;
			btnCopyScript.ForeColor = Ui.TextPrimary;
			btnCopyScript.Cursor = Cursors.Hand;
			btnCopyScript.Click += delegate
			{
				if (!string.IsNullOrEmpty(txtPreview.Text))
				{
					Clipboard.SetText(txtPreview.Text);
					MessageBox.Show("Текст скопирован в буфер обмена!", "Периферия", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};
			previewHeader.Controls.Add(btnCopyScript);

			rightPanel.Controls.Add(previewHeader);

			txtPreview = new DarkTextBoxMultiline();
			txtPreview.Dock = DockStyle.Fill;
			txtPreview.Font = Ui.FontMono(8.5f);
			txtPreview.ReadOnly = true;
			txtPreview.WordWrap = false;
			txtPreview.ScrollBars = ScrollBars.Both;
			rightPanel.Controls.Add(txtPreview);
			txtPreview.BringToFront();

			split.Panel2.Controls.Add(rightPanel);
			Controls.Add(split);

			// Bottom Status Strip
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 36;
			bottomPanel.BackColor = Ui.SurfaceAlt;
			bottomPanel.Padding = new Padding(10, 6, 10, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			Label lblWhitelistInfo = new Label();
			lblWhitelistInfo.Text = "ℹ️ Whitelist 5.01: Автокликеры (OP Auto Clicker, SpeedAutoClicker, TinyTask, X-Mouse, AHK, Keyran) РАЗРЕШЕНЫ. Аудит выявляет скрытые Lua-скрипты и 0-2ms дебаунс.";
			lblWhitelistInfo.Font = Ui.FontBody(8f);
			lblWhitelistInfo.ForeColor = Ui.AccentGreen;
			lblWhitelistInfo.Location = new Point(12, 10);
			lblWhitelistInfo.AutoSize = true;
			bottomPanel.Controls.Add(lblWhitelistInfo);

			DarkButton btnCopyAllReport = new DarkButton();
			btnCopyAllReport.Text = "📋 Скопировать протокол аудита";
			btnCopyAllReport.Size = new Size(220, 24);
			btnCopyAllReport.Location = new Point(940, 6);
			btnCopyAllReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyAllReport.ForeColor = Color.White;
			btnCopyAllReport.Cursor = Cursors.Hand;
			btnCopyAllReport.Click += delegate
			{
				CopyAuditReport();
			};
			bottomPanel.Controls.Add(btnCopyAllReport);

			Controls.Add(bottomPanel);
		}

		private DarkButton CreateCategoryButton(string text, MacroFilterCategory cat, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 24);
			btn.Font = Ui.FontBody(8f);
			btn.Cursor = Cursors.Hand;
			btn.BackColor = (cat == currentCategory) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.ForeColor = (cat == currentCategory) ? Color.Black : Ui.TextPrimary;
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
			DarkButton[] btns = { btnCatAll, btnCatSuspicious, btnCatLogitech, btnCatBloody, btnCatRazer, btnCatGlorious, btnCatCorsair, btnCatOther, btnCatWhitelist };
			MacroFilterCategory[] cats = {
				MacroFilterCategory.All, MacroFilterCategory.SuspiciousOnly, MacroFilterCategory.Logitech,
				MacroFilterCategory.Bloody, MacroFilterCategory.Razer, MacroFilterCategory.Glorious,
				MacroFilterCategory.Corsair, MacroFilterCategory.Other, MacroFilterCategory.Whitelist
			};

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (cats[i] == currentCategory);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].ForeColor = active ? Color.Black : Ui.TextPrimary;
			}
		}

		private void LoadPeripheralDataAsync()
		{
			lblHeaderStatus.Text = "Сбор данных о драйверах мышей, профилях макросов и Lua-скриптах...";
			lblHeaderStatus.ForeColor = Ui.AccentOrange;

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				List<MacroItem> items = new List<MacroItem>();

				CollectLogitechForensics(items);
				CollectBloodyForensics(items);
				CollectRazerForensics(items);
				CollectGloriousForensics(items);
				CollectCorsairForensics(items);
				CollectOtherMouseForensics(items);
				CollectWhitelistedClickers(items);

				// Sort items: suspicious first, then by vendor
				items.Sort((a, b) =>
				{
					if (a.IsSuspicious != b.IsSuspicious) return b.IsSuspicious.CompareTo(a.IsSuspicious);
					return string.Compare(a.Vendor, b.Vendor, StringComparison.OrdinalIgnoreCase);
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
					lblHeaderStatus.Text = "Ошибка сбора данных: " + e.Error.Message;
					lblHeaderStatus.ForeColor = Ui.AccentRed;
					return;
				}

				allItems = e.Result as List<MacroItem> ?? new List<MacroItem>();

				int total = allItems.Count;
				int susp = allItems.Count(i => i.IsSuspicious);
				int logi = allItems.Count(i => i.Vendor.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) >= 0);
				int bloody = allItems.Count(i => i.Vendor.IndexOf("Bloody", StringComparison.OrdinalIgnoreCase) >= 0);
				int razer = allItems.Count(i => i.Vendor.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) >= 0);
				int glor = allItems.Count(i => i.Vendor.IndexOf("Glorious", StringComparison.OrdinalIgnoreCase) >= 0);
				int cors = allItems.Count(i => i.Vendor.IndexOf("Corsair", StringComparison.OrdinalIgnoreCase) >= 0);
				int other = allItems.Count(i => !i.IsWhitelisted && i.Vendor.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) < 0 &&
					i.Vendor.IndexOf("Bloody", StringComparison.OrdinalIgnoreCase) < 0 &&
					i.Vendor.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) < 0 &&
					i.Vendor.IndexOf("Glorious", StringComparison.OrdinalIgnoreCase) < 0 &&
					i.Vendor.IndexOf("Corsair", StringComparison.OrdinalIgnoreCase) < 0);
				int white = allItems.Count(i => i.IsWhitelisted);

				btnCatAll.Text = "🔍 Всё (" + total + ")";
				btnCatSuspicious.Text = "🚨 Абузы (" + susp + ")";
				btnCatLogitech.Text = "🖱️ Logitech (" + logi + ")";
				btnCatBloody.Text = "🩸 Bloody (" + bloody + ")";
				btnCatRazer.Text = "🐍 Razer (" + razer + ")";
				btnCatGlorious.Text = "⚡ Glorious (" + glor + ")";
				btnCatCorsair.Text = "⚙️ iCUE (" + cors + ")";
				btnCatOther.Text = "🎮 Прочие (" + other + ")";
				btnCatWhitelist.Text = "✔ Whitelist (" + white + ")";

				lblHeaderStatus.Text = string.Format("Аудит завершён: найдено {0} объектов периферии (подозрительных/абузов: {1}, разрешённых: {2})", total, susp, white);
				lblHeaderStatus.ForeColor = (susp > 0) ? Ui.AccentRed : Ui.AccentGreen;

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private static void CollectLogitechForensics(List<MacroItem> list)
		{
			try
			{
				// 1. Process check
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					if (pName == "lghub" || pName == "lghub_agent" || pName == "lcore")
					{
						list.Add(new MacroItem
						{
							Vendor = "Logitech G HUB",
							ItemType = "Активный процесс",
							Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
							Path = SafeGetProcessPath(p),
							Timestamp = DateTime.UtcNow,
							TimeString = "Работает сейчас",
							IsSuspicious = false,
							Verdict = "ℹ️ Активное ПО Logitech",
							Content = "Процесс ПО Logitech активен в операционной системе.\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
							Analysis = "Драйверное ПО Logitech G HUB / Gaming Software запущено.",
							ForeColor = Ui.AccentTeal
						});
					}
				}

				// 2. Scan %LOCALAPPDATA%\LGHUB\settings.db for Lua scripts and macros
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

				string ghubDb = Path.Combine(localApp, @"LGHUB\settings.db");
				if (File.Exists(ghubDb))
				{
					DateTime dt = File.GetLastWriteTime(ghubDb);
					string dbText = "";
					try
					{
						using (FileStream fs = new FileStream(ghubDb, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
						{
							dbText = sr.ReadToEnd();
						}
					}
					catch { }

					if (!string.IsNullOrEmpty(dbText))
					{
						// Detect Lua scripts in settings.db
						bool hasLuaClicker = (dbText.Contains("PressMouseButton") || dbText.Contains("ReleaseMouseButton")) &&
							(dbText.Contains("Sleep") || dbText.Contains("while"));
						bool hasJitter = dbText.Contains("math.random");

						if (hasLuaClicker)
						{
							list.Add(new MacroItem
							{
								Vendor = "Logitech G HUB",
								ItemType = "Встроенный Lua-скрипт",
								Name = "Lua Autoclicker в settings.db",
								Path = ghubDb,
								Timestamp = dt,
								TimeString = dt.ToString("dd.MM.yyyy HH:mm"),
								IsSuspicious = true,
								Verdict = "🚨 ПОДОЗРИТЕЛЬНО: Lua-кликер в профиле Logitech",
								Content = ExtractLuaSnippet(dbText),
								Analysis = "В базе данных профилей Logitech G HUB обнаружен встроенный скрипт автоматизации кликов (PressMouseButton + Sleep" + (hasJitter ? " + math.random jitter" : "") + ")!",
								ForeColor = Ui.AccentRed
							});
						}
						else
						{
							list.Add(new MacroItem
							{
								Vendor = "Logitech G HUB",
								ItemType = "База настроек",
								Name = "settings.db",
								Path = ghubDb,
								Timestamp = dt,
								TimeString = dt.ToString("dd.MM.yyyy HH:mm"),
								IsSuspicious = false,
								Verdict = "ℹ️ ЧИСТО: Стандартные профили без Lua-кликеров",
								Content = "Файл настроек Logitech G HUB найден.\nРазмер: " + (dbText.Length / 1024) + " КБ.\nВстроенных скриптов автоматизации клика не обнаружено.",
								Analysis = "База настроек G HUB проверена. Подозрительных функций PressMouseButton не найдено.",
								ForeColor = Ui.TextPrimary
							});
						}
					}
				}

				// 3. Scan scripts directory
				string[] scriptDirs = {
					Path.Combine(appData, @"Logitech\G HUB\scripts"),
					Path.Combine(localApp, @"LGHUB\scripts"),
					Path.Combine(appData, @"Logitech\Gaming Software\Profiles")
				};

				foreach (string sDir in scriptDirs)
				{
					if (Directory.Exists(sDir))
					{
						string[] files = Directory.GetFiles(sDir, "*.*", SearchOption.AllDirectories);
						foreach (string f in files)
						{
							string ext = Path.GetExtension(f).ToLowerInvariant();
							if (ext == ".lua" || ext == ".xml")
							{
								string content = "";
								try { content = File.ReadAllText(f); } catch { }
								bool hasClick = content.IndexOf("PressMouseButton", StringComparison.OrdinalIgnoreCase) >= 0;
								bool hasSleep = content.IndexOf("Sleep", StringComparison.OrdinalIgnoreCase) >= 0;
								bool isSusp = hasClick && hasSleep;

								DateTime fDt = File.GetLastWriteTime(f);
								list.Add(new MacroItem
								{
									Vendor = "Logitech G HUB",
									ItemType = (ext == ".lua") ? "Скрипт Lua" : "Профиль XML",
									Name = Path.GetFileName(f),
									Path = f,
									Timestamp = fDt,
									TimeString = fDt.ToString("dd.MM.yyyy HH:mm"),
									IsSuspicious = isSusp,
									Verdict = isSusp ? "🚨 ПОДОЗРИТЕЛЬНО: Скрипт кликера" : "ℹ️ Профиль без кликера",
									Content = (content.Length > 4000) ? content.Substring(0, 4000) + "\n...[Обрезано]" : content,
									Analysis = isSusp ? "Обнаружен файл скрипта с вызовами PressMouseButton и Sleep (автокликер/макрос на мышь)." : "Стандартный файл настроек профиля Logitech.",
									ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private static void CollectBloodyForensics(List<MacroItem> list)
		{
			try
			{
				// 1. Process check
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					if (pName.StartsWith("bloody") || pName.Contains("bloody7") || pName.Contains("bloody6") || pName.Contains("bloody5"))
					{
						list.Add(new MacroItem
						{
							Vendor = "Bloody (A4Tech)",
							ItemType = "Активный процесс",
							Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
							Path = SafeGetProcessPath(p),
							Timestamp = DateTime.UtcNow,
							TimeString = "Работает сейчас",
							IsSuspicious = true,
							Verdict = "⚠️ ВНИМАНИЕ: Активно ПО Bloody",
							Content = "Запущен процесс Bloody.\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
							Analysis = "Мыши Bloody поддерживают встроенную память и аппаратные макросы Oscar (Core 3/4), способные исполнять спам ЛКМ в обход детектов.",
							ForeColor = Ui.AccentOrange
						});
					}
				}

				// 2. Directory checks
				string[] bloodyDirs = {
					@"C:\Program Files (x86)\Bloody7",
					@"C:\Program Files\Bloody7",
					@"C:\Program Files (x86)\Bloody6",
					@"C:\Program Files (x86)\Bloody5"
				};

				foreach (string bDir in bloodyDirs)
				{
					if (Directory.Exists(bDir))
					{
						list.Add(new MacroItem
						{
							Vendor = "Bloody (A4Tech)",
							ItemType = "Установленное ПО",
							Name = Path.GetFileName(bDir),
							Path = bDir,
							Timestamp = Directory.GetLastWriteTime(bDir),
							TimeString = Directory.GetLastWriteTime(bDir).ToString("dd.MM.yyyy HH:mm"),
							IsSuspicious = false,
							Verdict = "ℹ️ Установлен пакет Bloody",
							Content = "Обнаружена директория установки: " + bDir,
							Analysis = "Пакет Bloody установлен в системе. Проверьте папки Data\\RES\\English\\Scripts на наличие макросов .bmg/.amc.",
							ForeColor = Ui.TextPrimary
						});

						// Scan for macro files .bmg, .amc, .mgn
						try
						{
							string[] macroFiles = Directory.GetFiles(bDir, "*.*", SearchOption.AllDirectories);
							foreach (string mf in macroFiles)
							{
								string ext = Path.GetExtension(mf).ToLowerInvariant();
								if (ext == ".bmg" || ext == ".amc" || ext == ".mgn")
								{
									DateTime mDt = File.GetLastWriteTime(mf);
									list.Add(new MacroItem
									{
										Vendor = "Bloody (A4Tech)",
										ItemType = "Макрос Oscar (" + ext + ")",
										Name = Path.GetFileName(mf),
										Path = mf,
										Timestamp = mDt,
										TimeString = mDt.ToString("dd.MM.yyyy HH:mm"),
										IsSuspicious = true,
										Verdict = "🚨 ПОДОЗРИТЕЛЬНО: Файл макроса Bloody",
										Content = "Файл аппаратного макроса мыши Bloody: " + mf + "\nРазмер: " + (new FileInfo(mf).Length) + " байт.",
										Analysis = "Файл скрипта Oscar Macro. Может содержать аппаратный кликер или компенсатор отдачи.",
										ForeColor = Ui.AccentRed
									});
								}
							}
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private static void CollectRazerForensics(List<MacroItem> list)
		{
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					if (pName.Contains("razer") && (pName.Contains("synapse") || pName.Contains("central")))
					{
						list.Add(new MacroItem
						{
							Vendor = "Razer Synapse",
							ItemType = "Активный процесс",
							Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
							Path = SafeGetProcessPath(p),
							Timestamp = DateTime.UtcNow,
							TimeString = "Работает сейчас",
							IsSuspicious = false,
							Verdict = "ℹ️ Активно ПО Razer",
							Content = "Запущен процесс Razer Synapse.\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
							Analysis = "Драйверное ПО Razer Synapse активно.",
							ForeColor = Ui.TextPrimary
						});
					}
				}

				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

				string[] razerMacroDirs = {
					Path.Combine(localApp, @"Razer\Synapse3\Macros"),
					Path.Combine(progData, @"Razer\Synapse\Profiles")
				};

				foreach (string rDir in razerMacroDirs)
				{
					if (Directory.Exists(rDir))
					{
						string[] xmls = Directory.GetFiles(rDir, "*.xml", SearchOption.AllDirectories);
						foreach (string xf in xmls)
						{
							string txt = "";
							try { txt = File.ReadAllText(xf); } catch { }
							bool hasMouseClick = txt.IndexOf("Button=\"Left\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
								txt.IndexOf("MouseButton", StringComparison.OrdinalIgnoreCase) >= 0;
							bool hasFastDelay = txt.IndexOf("Delay=\"", StringComparison.OrdinalIgnoreCase) >= 0;

							DateTime xDt = File.GetLastWriteTime(xf);
							bool isSusp = hasMouseClick && hasFastDelay;
							list.Add(new MacroItem
							{
								Vendor = "Razer Synapse",
								ItemType = "Макрос XML",
								Name = Path.GetFileName(xf),
								Path = xf,
								Timestamp = xDt,
								TimeString = xDt.ToString("dd.MM.yyyy HH:mm"),
								IsSuspicious = isSusp,
								Verdict = isSusp ? "🚨 ПОДОЗРИТЕЛЬНО: Макрос ЛКМ Razer" : "ℹ️ Макрос Razer",
								Content = (txt.Length > 3000) ? txt.Substring(0, 3000) + "\n...[Обрезано]" : txt,
								Analysis = isSusp ? "Макрос содержит циклическое нажатие левой кнопки мыши с заданной задержкой (автокликер)." : "Макрос Razer без спама ЛКМ.",
								ForeColor = isSusp ? Ui.AccentRed : Ui.TextPrimary
							});
						}
					}
				}
			}
			catch { }
		}

		private static void CollectGloriousForensics(List<MacroItem> list)
		{
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					if (pName.Contains("glorious"))
					{
						list.Add(new MacroItem
						{
							Vendor = "Glorious Core",
							ItemType = "Активный процесс",
							Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
							Path = SafeGetProcessPath(p),
							Timestamp = DateTime.UtcNow,
							TimeString = "Работает сейчас",
							IsSuspicious = false,
							Verdict = "ℹ️ Активно ПО Glorious",
							Content = "Процесс Glorious Core активен.\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
							Analysis = "ПО Glorious Core используется для мышей Model O/D/I. Важно проверить задержку дебаунса (Debounce time).",
							ForeColor = Ui.TextPrimary
						});
					}
				}

				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

				string[] glorDirs = {
					Path.Combine(appData, @"Glorious Core"),
					Path.Combine(localApp, @"Glorious Core")
				};

				foreach (string gDir in glorDirs)
				{
					if (Directory.Exists(gDir))
					{
						string[] jsonFiles = Directory.GetFiles(gDir, "*.json", SearchOption.AllDirectories);
						foreach (string jf in jsonFiles)
						{
							string jText = "";
							try { jText = File.ReadAllText(jf); } catch { }
							if (jText.IndexOf("debounce", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								// Match debounce value: "debounce": 2 or debounceTime: 0
								Match m = Regex.Match(jText, @"(?i)""debounce(?:Time)?""\s*:\s*(\d+)");
								int debVal = m.Success ? int.Parse(m.Groups[1].Value) : 10;
								bool isAbuse = debVal <= 4;

								DateTime jDt = File.GetLastWriteTime(jf);
								list.Add(new MacroItem
								{
									Vendor = "Glorious Core",
									ItemType = "Конфигурация дебаунса",
									Name = Path.GetFileName(jf) + " (Debounce: " + debVal + "ms)",
									Path = jf,
									Timestamp = jDt,
									TimeString = jDt.ToString("dd.MM.yyyy HH:mm"),
									IsSuspicious = isAbuse,
									Verdict = isAbuse ? "⚠️ ВНИМАНИЕ: Дебаунс " + debVal + "ms (Абуз дабл-клика)" : "ℹ️ Дебаунс " + debVal + "ms (Норма)",
									Content = "Параметр Debounce: " + debVal + " мс.\nФайл: " + jf + "\n\n" + (jText.Length > 2000 ? jText.Substring(0, 2000) : jText),
									Analysis = isAbuse ?
										string.Format("Задержка дебаунса установлена в {0} мс (ниже 4 мс). При такой задержке механический микрик регистрирует двойной клик на одно физическое нажатие (Butterfly/Drag click абуз).", debVal) :
										string.Format("Дебаунс {0} мс находится в пределах заводской нормы (10-12 мс). Абуза двойного клика нет.", debVal),
									ForeColor = isAbuse ? Ui.AccentOrange : Ui.AccentGreen
								});
							}
						}
					}
				}
			}
			catch { }
		}

		private static void CollectCorsairForensics(List<MacroItem> list)
		{
			try
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					if (pName == "icue" || pName.Contains("corsair"))
					{
						list.Add(new MacroItem
						{
							Vendor = "Corsair iCUE",
							ItemType = "Активный процесс",
							Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
							Path = SafeGetProcessPath(p),
							Timestamp = DateTime.UtcNow,
							TimeString = "Работает сейчас",
							IsSuspicious = false,
							Verdict = "ℹ️ Активно ПО Corsair iCUE",
							Content = "Запущен процесс Corsair iCUE.\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
							Analysis = "ПО Corsair iCUE активно.",
							ForeColor = Ui.TextPrimary
						});
					}
				}

				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string[] icueDirs = {
					Path.Combine(appData, @"Corsair\CUE5\actions"),
					Path.Combine(appData, @"Corsair\CUE4\actions")
				};

				foreach (string cDir in icueDirs)
				{
					if (Directory.Exists(cDir))
					{
						string[] files = Directory.GetFiles(cDir, "*.*", SearchOption.AllDirectories);
						foreach (string f in files)
						{
							DateTime fDt = File.GetLastWriteTime(f);
							list.Add(new MacroItem
							{
								Vendor = "Corsair iCUE",
								ItemType = "Файл действия iCUE",
								Name = Path.GetFileName(f),
								Path = f,
								Timestamp = fDt,
								TimeString = fDt.ToString("dd.MM.yyyy HH:mm"),
								IsSuspicious = false,
								Verdict = "ℹ️ Профиль действий iCUE",
								Content = "Файл действия Corsair iCUE: " + f,
								Analysis = "Конфигурация действий Corsair.",
								ForeColor = Ui.TextPrimary
							});
						}
					}
				}
			}
			catch { }
		}

		private static void CollectOtherMouseForensics(List<MacroItem> list)
		{
			try
			{
				// Check known OEM gaming software: Redragon, Defender, Ardor/Zet, SteelSeries, etc.
				string[] otherKeywords = {
					"steelseries", "redragon", "defender", "ardor", "zet", "fantech", "motospeed", "aula", "havit", "marvo", "hyperx"
				};

				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					foreach (string kw in otherKeywords)
					{
						if (pName.Contains(kw))
						{
							list.Add(new MacroItem
							{
								Vendor = Capitalize(kw) + " Gaming",
								ItemType = "Активный процесс",
								Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
								Path = SafeGetProcessPath(p),
								Timestamp = DateTime.UtcNow,
								TimeString = "Работает сейчас",
								IsSuspicious = false,
								Verdict = "ℹ️ Активно ПО " + Capitalize(kw),
								Content = "Запущен процесс: " + p.ProcessName + "\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p),
								Analysis = "ПО периферии " + Capitalize(kw) + " активно в операционной системе.",
								ForeColor = Ui.TextPrimary
							});
							break;
						}
					}
				}
			}
			catch { }
		}

		private static void CollectWhitelistedClickers(List<MacroItem> list)
		{
			try
			{
				// OP Auto Clicker, SpeedAutoClicker, TinyTask, X-Mouse, AutoHotkey, Clickermann, Keyran
				string[] whiteProcesses = {
					"opautoclicker", "autoclicker", "speedautoclicker", "tinytask", "xmouse", "x-mouse", "autohotkey", "clickermann", "keyran"
				};

				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					string pName = p.ProcessName.ToLowerInvariant();
					foreach (string wp in whiteProcesses)
					{
						if (pName.Contains(wp))
						{
							list.Add(new MacroItem
							{
								Vendor = "Разрешённый кликер (Whitelist)",
								ItemType = "Активный процесс",
								Name = p.ProcessName + ".exe (PID: " + p.Id + ")",
								Path = SafeGetProcessPath(p),
								Timestamp = DateTime.UtcNow,
								TimeString = "Работает сейчас",
								IsSuspicious = false,
								IsWhitelisted = true,
								Verdict = "✔ РАЗРЕШЕНО (Whitelist 5.01)",
								Content = "Утилита: " + p.ProcessName + "\nPID: " + p.Id + "\nПуть: " + SafeGetProcessPath(p) + "\n\nРегламент: Данный кликер внесён в Белый Список (Whitelist) правил сервера и не подлежит бану.",
								Analysis = "Утилита разрешена правилами сервера (Whitelist 5.01). Наказание НЕ применяется.",
								ForeColor = Ui.AccentGreen
							});
							break;
						}
					}
				}
			}
			catch { }
		}

		private static string ExtractLuaSnippet(string text)
		{
			int idx = text.IndexOf("PressMouseButton", StringComparison.OrdinalIgnoreCase);
			if (idx < 0) idx = text.IndexOf("function OnEvent", StringComparison.OrdinalIgnoreCase);
			if (idx >= 0)
			{
				int start = Math.Max(0, idx - 200);
				int len = Math.Min(1200, text.Length - start);
				return text.Substring(start, len);
			}
			return text.Length > 800 ? text.Substring(0, 800) : text;
		}

		private static string SafeGetProcessPath(Process p)
		{
			try { return p.MainModule.FileName; } catch { return "—"; }
		}

		private static string Capitalize(string s)
		{
			if (string.IsNullOrEmpty(s)) return s;
			return char.ToUpperInvariant(s[0]) + s.Substring(1);
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
			bool onlySusp = chkOnlySuspicious.Checked;

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = allItems.Where(it =>
			{
				if (onlySusp && !it.IsSuspicious) return false;

				switch (currentCategory)
				{
					case MacroFilterCategory.SuspiciousOnly:
						if (!it.IsSuspicious) return false;
						break;
					case MacroFilterCategory.Logitech:
						if (it.Vendor.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case MacroFilterCategory.Bloody:
						if (it.Vendor.IndexOf("Bloody", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case MacroFilterCategory.Razer:
						if (it.Vendor.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case MacroFilterCategory.Glorious:
						if (it.Vendor.IndexOf("Glorious", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case MacroFilterCategory.Corsair:
						if (it.Vendor.IndexOf("Corsair", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case MacroFilterCategory.Other:
						if (it.IsWhitelisted || it.Vendor.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) >= 0 ||
							it.Vendor.IndexOf("Bloody", StringComparison.OrdinalIgnoreCase) >= 0 ||
							it.Vendor.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) >= 0 ||
							it.Vendor.IndexOf("Glorious", StringComparison.OrdinalIgnoreCase) >= 0 ||
							it.Vendor.IndexOf("Corsair", StringComparison.OrdinalIgnoreCase) >= 0) return false;
						break;
					case MacroFilterCategory.Whitelist:
						if (!it.IsWhitelisted) return false;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = (it.Vendor != null && it.Vendor.ToLowerInvariant().Contains(query)) ||
						(it.ItemType != null && it.ItemType.ToLowerInvariant().Contains(query)) ||
						(it.Name != null && it.Name.ToLowerInvariant().Contains(query)) ||
						(it.Verdict != null && it.Verdict.ToLowerInvariant().Contains(query)) ||
						(it.Path != null && it.Path.ToLowerInvariant().Contains(query)) ||
						(it.Content != null && it.Content.ToLowerInvariant().Contains(query));
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (MacroItem item in filtered)
			{
				ListViewItem lvi = new ListViewItem(item.Index.ToString());
				lvi.SubItems.Add(item.Vendor);
				lvi.SubItems.Add(item.ItemType);
				lvi.SubItems.Add(item.Name);
				lvi.SubItems.Add(item.Verdict);
				lvi.SubItems.Add(item.TimeString);
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
				ClearPreview();
			}
		}

		private void UpdatePreviewDetails()
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearPreview();
				return;
			}

			MacroItem item = listView.SelectedItems[0].Tag as MacroItem;
			if (item == null) return;

			lblPreviewTitle.Text = item.Vendor + " — " + item.Name;
			lblPreviewTitle.ForeColor = item.IsSuspicious ? Ui.AccentRed : (item.IsWhitelisted ? Ui.AccentGreen : Ui.AccentTeal);

			lblPreviewVerdict.Text = item.Verdict;
			lblPreviewVerdict.ForeColor = item.ForeColor;

			lblPreviewDetails.Text = item.Analysis + "\nПуть: " + (string.IsNullOrEmpty(item.Path) ? "—" : item.Path);

			txtPreview.Text = string.IsNullOrEmpty(item.Content) ? "Содержимое недоступно." : item.Content;
		}

		private void ClearPreview()
		{
			lblPreviewTitle.Text = "Выберите объект для анализа";
			lblPreviewTitle.ForeColor = Ui.TextSecondary;
			lblPreviewVerdict.Text = "—";
			lblPreviewDetails.Text = "Здесь отображается детальный анализ макроса, Lua-скрипта или аппаратной задержки.";
			txtPreview.Text = "";
		}

		private void OpenSelectedInExplorer()
		{
			if (listView.SelectedItems.Count == 0) return;
			MacroItem item = listView.SelectedItems[0].Tag as MacroItem;
			if (item == null || string.IsNullOrEmpty(item.Path) || item.Path == "—") return;

			try
			{
				if (File.Exists(item.Path))
				{
					PlatformHelper.SelectFileInFileManager(item.Path);
				}
				else if (Directory.Exists(item.Path))
				{
					PlatformHelper.OpenFolder(item.Path);
				}
				else
				{
					string dir = Path.GetDirectoryName(item.Path);
					if (Directory.Exists(dir)) PlatformHelper.OpenFolder(dir);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть расположение: " + ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyAuditReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("          ПРОТОКОЛ АУДИТА ПЕРИФЕРИИ И МАКРОСОВ МЫШИ (SCREENSHARE)               ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Дата и время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("Всего проверено объектов: " + allItems.Count);
			sb.AppendLine("Подозрительных / абузов клика: " + allItems.Count(i => i.IsSuspicious));
			sb.AppendLine("Разрешённых инструментов (Whitelist): " + allItems.Count(i => i.IsWhitelisted));
			sb.AppendLine();

			sb.AppendLine("--- НАЙДЕННЫЕ УЛИКИ И АБУЗЫ ---");
			var suspList = allItems.Where(i => i.IsSuspicious).ToList();
			if (suspList.Count == 0)
			{
				sb.AppendLine("Подозрительных Lua-скриптов, макросов Bloody и абузов дебаунса 0-4ms НЕ обнаружено.");
			}
			else
			{
				foreach (var it in suspList)
				{
					sb.AppendLine(string.Format("[{0}] {1} | {2}", it.Vendor, it.Name, it.Verdict));
					sb.AppendLine("   Путь: " + it.Path);
					sb.AppendLine("   Анализ: " + it.Analysis);
					sb.AppendLine();
				}
			}

			sb.AppendLine("--- АКТИВНОЕ ПО ПЕРИФЕРИИ И БЕЛЫЙ СПИСОК ---");
			foreach (var it in allItems.Where(i => !i.IsSuspicious))
			{
				sb.AppendLine(string.Format("[{0}] {1} ({2}) -> {3}", it.Vendor, it.Name, it.ItemType, it.Verdict));
			}

			sb.AppendLine();
			sb.AppendLine("Регламент: Автокликеры из Whitelist разрешены правилом 5.01. Скрытые Lua-скрипты наказываются по 5.01.");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол аудита периферии успешно скопирован в буфер обмена!", "Аудит мыши и макросов", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
