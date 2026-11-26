using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class JavawRamViewerForm : Form
	{
		private enum RamFilterMode
		{
			All,
			CriticalOnly,
			MzInjections,
			CheatSignatures,
			DllModules
		}

		private string targetPlayerName;
		private RamFilterMode currentFilter = RamFilterMode.All;
		private List<MemoryRegionHit> allHits = new List<MemoryRegionHit>();
		private Process targetProcess = null;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtHitDetails;
		private Label lblHeaderTitle;
		private Label lblProcessInfo;
		private Label lblStatusPill;
		private Label lblDetailAddress;
		private Label lblDetailProtection;
		private Label lblDetailRule;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterCritical;
		private DarkButton btnFilterMz;
		private DarkButton btnFilterSigs;
		private DarkButton btnFilterDlls;

		public JavawRamViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			ScanRamAsync();
		}

		private void InitializeComponent()
		{
			Text = "⚡ Инспектор оперативной памяти Minecraft (javaw.exe RAM Forensics)";
			Size = new Size(1160, 720);
			MinimumSize = new Size(900, 540);
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
			lblHeaderTitle.Text = "⚡  ИНСПЕКТОР ОПЕРАТИВНОЙ ПАМЯТИ JAVAW.EXE (Process RAM Forensics & DLL Modules)";
			lblHeaderTitle.Font = Ui.FontTitle(10f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(12, 8);
			lblHeaderTitle.AutoSize = true;
			topPanel.Controls.Add(lblHeaderTitle);

			lblProcessInfo = new Label();
			lblProcessInfo.Text = "Поиск активного процесса javaw.exe / java.exe...";
			lblProcessInfo.Font = Ui.FontBody(8.5f);
			lblProcessInfo.ForeColor = Ui.TextSecondary;
			lblProcessInfo.Location = new Point(12, 30);
			lblProcessInfo.AutoSize = true;
			topPanel.Controls.Add(lblProcessInfo);

			lblStatusPill = new Label();
			lblStatusPill.Text = "⏳ Сканирование RAM...";
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.Location = new Point(620, 10);
			lblStatusPill.AutoSize = true;
			topPanel.Controls.Add(lblStatusPill);

			DarkButton btnRefresh = new DarkButton();
			btnRefresh.Text = "🔄 Пересканировать";
			btnRefresh.Location = new Point(880, 8);
			btnRefresh.Size = new Size(135, 26);
			btnRefresh.BackColor = Ui.SurfaceAlt;
			btnRefresh.ForeColor = Ui.TextPrimary;
			btnRefresh.Cursor = Cursors.Hand;
			btnRefresh.Click += delegate
			{
				ScanRamAsync();
			};
			topPanel.Controls.Add(btnRefresh);

			DarkButton btnBan = new DarkButton();
			btnBan.Text = "🔨 Скопировать /ban 5.01";
			btnBan.Location = new Point(1022, 8);
			btnBan.Size = new Size(170, 26);
			btnBan.BackColor = Color.FromArgb(183, 28, 28);
			btnBan.ForeColor = Color.White;
			btnBan.Cursor = Cursors.Hand;
			btnBan.Click += delegate
			{
				CopyBanCommand();
			};
			topPanel.Controls.Add(btnBan);

			Panel navRow = new Panel();
			navRow.Location = new Point(12, 54);
			navRow.Size = new Size(1130, 50);
			navRow.BackColor = Color.Transparent;

			btnFilterAll = CreateFilterButton("🔍 Всё (0)", RamFilterMode.All, 95);
			btnFilterCritical = CreateFilterButton("🚨 Критические (0)", RamFilterMode.CriticalOnly, 130);
			btnFilterMz = CreateFilterButton("💉 MZ Инжекции (0)", RamFilterMode.MzInjections, 135);
			btnFilterSigs = CreateFilterButton("🎯 Чит-сигнатуры (0)", RamFilterMode.CheatSignatures, 145);
			btnFilterDlls = CreateFilterButton("📦 DLL Модули (0)", RamFilterMode.DllModules, 125);

			int curX = 0;
			DarkButton[] filterBtns = { btnFilterAll, btnFilterCritical, btnFilterMz, btnFilterSigs, btnFilterDlls };
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
			searchBox.Size = new Size(200, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilterAndSearch();
			};
			navRow.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(curX + 258, 2);
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
			split.SplitterDistance = 700;
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
			listView.Columns.Add("Адрес региона", 130, HorizontalAlignment.Left);
			listView.Columns.Add("Размер", 80, HorizontalAlignment.Left);
			listView.Columns.Add("Защита памяти", 150, HorizontalAlignment.Left);
			listView.Columns.Add("Классификация угрозы", 180, HorizontalAlignment.Left);
			listView.Columns.Add("Подробности", 240, HorizontalAlignment.Left);

			listView.SelectedIndexChanged += delegate
			{
				UpdateHitDetails();
			};

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();

			ToolStripMenuItem copyAddressItem = new ToolStripMenuItem("📋  Скопировать адрес памяти");
			copyAddressItem.Click += delegate
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

			ToolStripMenuItem copyAllReportItem = new ToolStripMenuItem("📋  Скопировать полный протокол сканирования RAM");
			copyAllReportItem.Click += delegate
			{
				CopyRamReport();
			};

			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Сгенерировать команду /ban за софт в RAM (5.01)");
			copyBanItem.Click += delegate
			{
				CopyBanCommand();
			};

			ctx.Items.Add(copyAddressItem);
			ctx.Items.Add(copyRowItem);
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
			lblDetHeader.Text = "🔍 Детальный анализ найденной улики в RAM";
			rightPanel.Controls.Add(lblDetHeader);

			lblDetailAddress = new Label();
			lblDetailAddress.Dock = DockStyle.Top;
			lblDetailAddress.Height = 22;
			lblDetailAddress.Font = Ui.FontBody(8.5f, true);
			lblDetailAddress.ForeColor = Ui.TextPrimary;
			lblDetailAddress.Text = "Адрес: —";
			rightPanel.Controls.Add(lblDetailAddress);

			lblDetailProtection = new Label();
			lblDetailProtection.Dock = DockStyle.Top;
			lblDetailProtection.Height = 22;
			lblDetailProtection.Font = Ui.FontBody(8.5f);
			lblDetailProtection.ForeColor = Ui.TextSecondary;
			lblDetailProtection.Text = "Защита памяти: —";
			rightPanel.Controls.Add(lblDetailProtection);

			lblDetailRule = new Label();
			lblDetailRule.Dock = DockStyle.Top;
			lblDetailRule.Height = 38;
			lblDetailRule.Font = Ui.FontBody(8.5f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Text = "Регламент: —";
			rightPanel.Controls.Add(lblDetailRule);

			Label lblDumpTitle = new Label();
			lblDumpTitle.Dock = DockStyle.Top;
			lblDumpTitle.Height = 22;
			lblDumpTitle.Font = Ui.FontBody(8f, true);
			lblDumpTitle.ForeColor = Ui.TextMuted;
			lblDumpTitle.Text = "📄 Текст детекта / Фрагмент памяти:";
			rightPanel.Controls.Add(lblDumpTitle);

			txtHitDetails = new DarkTextBoxMultiline();
			txtHitDetails.Dock = DockStyle.Fill;
			txtHitDetails.Font = Ui.FontMono(8.5f);
			txtHitDetails.ReadOnly = true;
			txtHitDetails.WordWrap = true;
			txtHitDetails.ScrollBars = ScrollBars.Vertical;
			rightPanel.Controls.Add(txtHitDetails);
			txtHitDetails.BringToFront();

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

			Label lblRuleHint = new Label();
			lblRuleHint.Text = "⚖️ Регламент проверок: Пункт 5.01 (Инжекция/читы в памяти) = Блокировка от 30 дней. Автокликеры (Whitelist) РАЗРЕШЕНЫ.";
			lblRuleHint.Font = Ui.FontBody(8f);
			lblRuleHint.ForeColor = Ui.AccentGreen;
			lblRuleHint.Location = new Point(12, 10);
			lblRuleHint.AutoSize = true;
			bottomPanel.Controls.Add(lblRuleHint);

			DarkButton btnCopyReport = new DarkButton();
			btnCopyReport.Text = "📋 Скопировать протокол RAM";
			btnCopyReport.Size = new Size(200, 24);
			btnCopyReport.Location = new Point(940, 6);
			btnCopyReport.BackColor = Color.FromArgb(0, 105, 92);
			btnCopyReport.ForeColor = Color.White;
			btnCopyReport.Cursor = Cursors.Hand;
			btnCopyReport.Click += delegate
			{
				CopyRamReport();
			};
			bottomPanel.Controls.Add(btnCopyReport);

			Controls.Add(bottomPanel);
		}

		private DarkButton CreateFilterButton(string text, RamFilterMode mode, int width)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Size = new Size(width, 24);
			btn.Font = Ui.FontBody(8f);
			btn.Cursor = Cursors.Hand;
			btn.BackColor = (mode == currentFilter) ? Ui.AccentTeal : Ui.SurfaceAlt;
			btn.ForeColor = (mode == currentFilter) ? Color.Black : Ui.TextPrimary;
			btn.Click += delegate
			{
				currentFilter = mode;
				UpdateFilterButtons();
				ApplyFilterAndSearch();
			};
			return btn;
		}

		private void UpdateFilterButtons()
		{
			DarkButton[] btns = { btnFilterAll, btnFilterCritical, btnFilterMz, btnFilterSigs, btnFilterDlls };
			RamFilterMode[] modes = { RamFilterMode.All, RamFilterMode.CriticalOnly, RamFilterMode.MzInjections, RamFilterMode.CheatSignatures, RamFilterMode.DllModules };

			for (int i = 0; i < btns.Length; i++)
			{
				bool active = (modes[i] == currentFilter);
				btns[i].BackColor = active ? Ui.AccentTeal : Ui.SurfaceAlt;
				btns[i].ForeColor = active ? Color.Black : Ui.TextPrimary;
			}
		}

		private void ScanRamAsync()
		{
			lblStatusPill.Text = "⏳ Сканирование памяти javaw.exe...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblProcessInfo.Text = "Чтение виртуальных регионов javaw.exe...";

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += delegate(object s, DoWorkEventArgs e)
			{
				Process p = null;
				Process[] javaws = Process.GetProcessesByName("javaw");
				if (javaws.Length > 0) p = javaws[0];
				else
				{
					Process[] javas = Process.GetProcessesByName("java");
					if (javas.Length > 0) p = javas[0];
				}

				List<MemoryRegionHit> hits = ProcessMemoryScanner.ScanMinecraftRam(p != null ? p.Id : 0);
				e.Result = new Tuple<Process, List<MemoryRegionHit>>(p, hits);
			};

			bw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
			{
				if (e.Error != null)
				{
					lblStatusPill.Text = "❌ Ошибка сканирования: " + e.Error.Message;
					lblStatusPill.ForeColor = Ui.AccentRed;
					return;
				}

				var res = e.Result as Tuple<Process, List<MemoryRegionHit>>;
				targetProcess = res != null ? res.Item1 : null;
				allHits = res != null ? res.Item2 : new List<MemoryRegionHit>();

				if (targetProcess != null)
				{
					long ramMb = targetProcess.WorkingSet64 / 1048576;
					lblProcessInfo.Text = string.Format("Процесс: {0}.exe (PID: {1}) | RAM: {2:N0} МБ | Потоков: {3} | Запущен: {4}",
						targetProcess.ProcessName, targetProcess.Id, ramMb, targetProcess.Threads.Count, targetProcess.StartTime.ToString("HH:mm:ss"));
				}
				else
				{
					lblProcessInfo.Text = "Процесс javaw.exe / java.exe не найден в системе.";
				}

				int total = allHits.Count;
				int crit = allHits.Count(h => h.Severity == CheckSeverity.Critical);
				int mz = allHits.Count(h => h.Type.IndexOf("MANUAL-MAP", StringComparison.OrdinalIgnoreCase) >= 0 || h.Type.IndexOf("MZ", StringComparison.OrdinalIgnoreCase) >= 0);
				int sigs = allHits.Count(h => h.Type.IndexOf("ЧИТ В ПАМЯТИ", StringComparison.OrdinalIgnoreCase) >= 0);
				int dlls = allHits.Count(h => h.Type.IndexOf("DLL", StringComparison.OrdinalIgnoreCase) >= 0 || h.Type.IndexOf("МОДУЛЬ", StringComparison.OrdinalIgnoreCase) >= 0);

				btnFilterAll.Text = "🔍 Всё (" + total + ")";
				btnFilterCritical.Text = "🚨 Критические (" + crit + ")";
				btnFilterMz.Text = "💉 MZ Инжекции (" + mz + ")";
				btnFilterSigs.Text = "🎯 Чит-сигнатуры (" + sigs + ")";
				btnFilterDlls.Text = "📦 DLL Модули (" + dlls + ")";

				if (crit > 0)
				{
					lblStatusPill.Text = string.Format("🚨 НАЙДЕНЫ ЧИТЫ В ПАМЯТИ ({0} находок)", crit);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ ПАМЯТЬ ЧИСТА: Сторонних инжекций не обнаружено";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}

				ApplyFilterAndSearch();
			};

			bw.RunWorkerAsync();
		}

		private void ApplyFilterAndSearch()
		{
			string query = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();

			listView.BeginUpdate();
			listView.Items.Clear();

			var filtered = allHits.Where(h =>
			{
				switch (currentFilter)
				{
					case RamFilterMode.CriticalOnly:
						if (h.Severity != CheckSeverity.Critical) return false;
						break;
					case RamFilterMode.MzInjections:
						if (h.Type.IndexOf("MANUAL-MAP", StringComparison.OrdinalIgnoreCase) < 0 &&
							h.Type.IndexOf("MZ", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case RamFilterMode.CheatSignatures:
						if (h.Type.IndexOf("ЧИТ В ПАМЯТИ", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
					case RamFilterMode.DllModules:
						if (h.Type.IndexOf("DLL", StringComparison.OrdinalIgnoreCase) < 0 &&
							h.Type.IndexOf("МОДУЛЬ", StringComparison.OrdinalIgnoreCase) < 0) return false;
						break;
				}

				if (!string.IsNullOrEmpty(query))
				{
					bool match = (h.Address != null && h.Address.ToLowerInvariant().Contains(query)) ||
						(h.Type != null && h.Type.ToLowerInvariant().Contains(query)) ||
						(h.Protection != null && h.Protection.ToLowerInvariant().Contains(query)) ||
						(h.Detail != null && h.Detail.ToLowerInvariant().Contains(query));
					if (!match) return false;
				}

				return true;
			}).ToList();

			foreach (MemoryRegionHit hit in filtered)
			{
				string icon = (hit.Severity == CheckSeverity.Critical) ? "🚨" :
					(hit.Severity == CheckSeverity.Warning || hit.Severity == CheckSeverity.NeedsCheck) ? "⚠️" : "ℹ️";

				ListViewItem lvi = new ListViewItem(icon);
				lvi.SubItems.Add(hit.Address ?? "—");
				lvi.SubItems.Add(hit.Size ?? "—");
				lvi.SubItems.Add(hit.Protection ?? "—");
				lvi.SubItems.Add(hit.Type ?? "—");
				lvi.SubItems.Add(hit.Detail ?? "—");
				lvi.Tag = hit;

				if (hit.Severity == CheckSeverity.Critical) lvi.ForeColor = Ui.AccentRed;
				else if (hit.Severity == CheckSeverity.Warning || hit.Severity == CheckSeverity.NeedsCheck) lvi.ForeColor = Ui.AccentOrange;
				else lvi.ForeColor = Ui.TextPrimary;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearHitDetails();
			}
		}

		private void UpdateHitDetails()
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearHitDetails();
				return;
			}

			MemoryRegionHit hit = listView.SelectedItems[0].Tag as MemoryRegionHit;
			if (hit == null) return;

			lblDetailAddress.Text = "Адрес: " + (hit.Address ?? "—") + "  |  Размер: " + (hit.Size ?? "—");
			lblDetailProtection.Text = "Защита региона: " + (hit.Protection ?? "—") + "  |  Тип: " + (hit.Type ?? "—");

			if (hit.Severity == CheckSeverity.Critical)
			{
				lblDetailRule.Text = "⚖️ Нарушение: Регламент 5.01 (Чит-модификация / инжекция в память javaw)\nРекомендуемое наказание: Блокировка от 30 дней.";
				lblDetailRule.ForeColor = Ui.AccentRed;
			}
			else if (hit.Severity == CheckSeverity.Warning || hit.Severity == CheckSeverity.NeedsCheck)
			{
				lblDetailRule.Text = "⚠️ Внимание: Требуется проверка источника библиотеки / региона.";
				lblDetailRule.ForeColor = Ui.AccentOrange;
			}
			else
			{
				lblDetailRule.Text = "ℹ️ Информация: Штатный модуль процесса или системный регион.";
				lblDetailRule.ForeColor = Ui.AccentTeal;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ДЕТАЛИ НАЙДЕННОГО РЕГИОНА ПАМЯТИ ===");
			sb.AppendLine("Базовый адрес: " + hit.Address);
			sb.AppendLine("Размер региона: " + hit.Size);
			sb.AppendLine("Флаги защиты: " + hit.Protection);
			sb.AppendLine("Классификация детекта: " + hit.Type);
			sb.AppendLine("Уровень угрозы: " + hit.Severity);
			sb.AppendLine();
			sb.AppendLine("--- ТЕКСТОВОЕ ОПИСАНИЕ И УЛИКА ---");
			sb.AppendLine(hit.Detail ?? "—");

			if (targetProcess != null)
			{
				sb.AppendLine();
				sb.AppendLine("--- КОНТЕКСТ ПРОЦЕССА ---");
				sb.AppendLine("Процесс: " + targetProcess.ProcessName + " (PID: " + targetProcess.Id + ")");
				try { sb.AppendLine("Исполняемый файл: " + targetProcess.MainModule.FileName); } catch { }
			}

			txtHitDetails.Text = sb.ToString();
		}

		private void ClearHitDetails()
		{
			lblDetailAddress.Text = "Адрес: —";
			lblDetailProtection.Text = "Защита памяти: —";
			lblDetailRule.Text = "Регламент: —";
			txtHitDetails.Text = "Выберите запись в таблице слева для отображения подробностей.";
		}

		private void CopyBanCommand()
		{
			var critHits = allHits.Where(h => h.Severity == CheckSeverity.Critical).ToList();
			string cheatName = "Инжекция в RAM javaw";
			if (critHits.Count > 0)
			{
				string t = critHits[0].Type ?? "";
				if (t.StartsWith("ЧИТ В ПАМЯТИ: ")) cheatName = t.Substring("ЧИТ В ПАМЯТИ: ".Length).Trim();
				else if (t.IndexOf("MANUAL-MAP", StringComparison.OrdinalIgnoreCase) >= 0) cheatName = "Manual-Map DLL Injection";
			}

			string cmd = string.Format("/ban {0} 30d 5.01 ({1})", targetPlayerName, cheatName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена!\n\n" + cmd, "Генератор наказания", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyRamReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("           ПРОТОКОЛ АНАЛИЗА ОПЕРАТИВНОЙ ПАМЯТИ JAVAW.EXE (SCREENSHARE)          ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Дата и время: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
			if (targetProcess != null)
			{
				sb.AppendLine(string.Format("Целевой процесс: {0} (PID: {1})", targetProcess.ProcessName, targetProcess.Id));
			}
			sb.AppendLine("Всего записей памяти: " + allHits.Count);
			sb.AppendLine("Критических находок: " + allHits.Count(h => h.Severity == CheckSeverity.Critical));
			sb.AppendLine();

			sb.AppendLine("--- КРИТИЧЕСКИЕ ИНЖЕКЦИИ И ЧИТЫ В ПАМЯТИ ---");
			var critList = allHits.Where(h => h.Severity == CheckSeverity.Critical).ToList();
			if (critList.Count == 0)
			{
				sb.AppendLine("Чит-модификаций и скрытых PE-заголовков (MZ) в памяти javaw не найдено.");
			}
			else
			{
				foreach (var it in critList)
				{
					sb.AppendLine(string.Format("[{0}] Адрес: {1} | Защита: {2} | Тип: {3}", it.Severity, it.Address, it.Protection, it.Type));
					sb.AppendLine("   Детали: " + it.Detail);
					sb.AppendLine();
				}
			}

			sb.AppendLine("--- ВСЕ ПРОВЕРЕННЫЕ МОДУЛИ И РЕГИОНЫ ---");
			foreach (var it in allHits.Where(h => h.Severity != CheckSeverity.Critical))
			{
				sb.AppendLine(string.Format("Адрес: {0} | Размер: {1} | Защита: {2} | {3} | {4}", it.Address, it.Size, it.Protection, it.Type, it.Detail));
			}

			sb.AppendLine();
			sb.AppendLine("Регламент: Наличие сторонних читов в памяти наказывается по пункту 5.01 (30 дней).");
			sb.AppendLine("================================================================================");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол сканирования памяти успешно скопирован в буфер обмена!", "Анализ RAM", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
