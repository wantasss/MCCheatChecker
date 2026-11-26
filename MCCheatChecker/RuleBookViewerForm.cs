using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class RuleBookViewerForm : Form
	{
		private class RuleDisplayItem
		{
			public string RuleTag;
			public string Title;
			public string Duration;
			public string Reason;
			public string Description;
			public bool IsAllowed;
			public Color AccentColor;
		}

		private List<RuleDisplayItem> allRules = new List<RuleDisplayItem>();
		private DarkListView listView;
		private DarkTextBox searchBox;
		private string targetPlayerName;

		public RuleBookViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeRules();
			InitializeComponent();
		}

		private void InitializeRules()
		{
			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.01",
				Title = "Чит-клиенты и модификации преимущества",
				Duration = "30d",
				Reason = "5.01 (Чит-клиенты / сторонний софт)",
				Description = "Использование чит-клиентов (Celestial, Expensive, Wild, Vape, Doomsday, Meteor, LiquidBounce, Wurst и др.), дающих преимущество в PvP/PvE.",
				IsAllowed = false,
				AccentColor = Ui.AccentRed
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.02",
				Title = "Хранение читов и стороннего ПО",
				Duration = "15d",
				Reason = "5.02 (Хранение читов)",
				Description = "Хранение файлов читов, инжекторов или архивов на диске (в папках Загрузки, Рабочий стол, Корзина, Prefetch), даже если они не запущены прямо сейчас.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.03",
				Title = "Рентген / X-Ray модификации и пакеты",
				Duration = "14d",
				Reason = "5.03 (Рентген / X-Ray)",
				Description = "Использование модов X-Ray, ресурспаков с прозрачными блоками, Freecam/NetheriteESP для поиска руд и баз игроков.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.04",
				Title = "Боты, Baritone и автоматизация",
				Duration = "14d",
				Reason = "5.04 (Боты / Baritone / автоматизация)",
				Description = "Использование Baritone, скриптов автоматического копания, фарм-ботов, макросов на авто-зелья/свап брони.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.05",
				Title = "Распространение и реклама читов",
				Duration = "30d",
				Reason = "5.05 (Распространение читов)",
				Description = "Отправка ссылок на скачивание читов, продажа кфг/читов в чате сервера или Discord.",
				IsAllowed = false,
				AccentColor = Ui.AccentRed
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.06",
				Title = "Подозрительное ПО / Недопуск к проверке",
				Duration = "15d",
				Reason = "5.06 (Подозрительное ПО)",
				Description = "Наличие нераспознанных инжекторов, драйверов WinDivert, перехватчиков трафика, виртуальных машин.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.07",
				Title = "Помеха проведению проверки",
				Duration = "15d",
				Reason = "5.07 (Помеха проверке)",
				Description = "Флуд во время проверки, попытки запутать модератора, отказ следовать инструкциям.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "5.08",
				Title = "Отказ от проверки / Лив / Очистка улик",
				Duration = "30d",
				Reason = "5.08 (Отказ от проверки / Лив / Очистка улик)",
				Description = "Выход из игры при вызове на проверку, очистка корзины/Prefetch/USN перед вызовом, истечение 5-минутного таймера.",
				IsAllowed = false,
				AccentColor = Ui.AccentRed
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "Признание",
				Title = "Признание в использовании софта",
				Duration = "15d",
				Reason = "Признание в софте (5.01 / 5.08)",
				Description = "Игрок добровольно сознался в использовании читов ДО начала сканирования. Наказание снижается с 30 до 15 дней.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "2.06",
				Title = "Багоюз и использование недоработок",
				Duration = "7d",
				Reason = "2.06 (Багоюз)",
				Description = "Использование багов сервера/игры для дюпа предметов, застревания в блоках, прохода сквозь стены.",
				IsAllowed = false,
				AccentColor = Ui.AccentOrange
			});

			allRules.Add(new RuleDisplayItem
			{
				RuleTag = "Белый список",
				Title = "РАЗРЕШЁННЫЙ СОФТ (БАН СТРОГО ЗАПРЕЩЁН!)",
				Duration = "0d (БЕЗ БАНА)",
				Reason = "Белый список / Разрешено",
				Description = "Автокликеры (OP Auto Clicker, SpeedAutoClicker, TinyTask, X-Mouse Button Control, AutoHotkey, Clickermann, Keyran) и утилиты ScreenShare (AnyDesk). БАН ЗАПРЕЩЁН!",
				IsAllowed = true,
				AccentColor = Ui.AccentGreen
			});
		}

		private void InitializeComponent()
		{
			Text = "📖 Справочник регламента проверок (Правила 5.01 - 5.08 & Белый список)";
			Size = new Size(920, 620);
			MinimumSize = new Size(750, 450);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 70;
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
			lblTitle.Text = "📖  СПРАВОЧНИК РЕГЛАМЕНТА ПРОВЕРОК НА ЧИТЫ (Раздел 5 & Статья 2.06)";
			lblTitle.Font = Ui.FontTitle(10f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.Location = new Point(12, 10);
			lblTitle.AutoSize = true;
			topPanel.Controls.Add(lblTitle);

			Label lblPlayerInfo = new Label();
			lblPlayerInfo.Text = "Целевой игрок для команды /ban: " + targetPlayerName;
			lblPlayerInfo.Font = Ui.FontBody(8.5f, true);
			lblPlayerInfo.ForeColor = Ui.AccentBlue;
			lblPlayerInfo.Location = new Point(12, 38);
			lblPlayerInfo.AutoSize = true;
			topPanel.Controls.Add(lblPlayerInfo);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Location = new Point(560, 40);
			lblSearch.AutoSize = true;
			topPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(610, 36);
			searchBox.Size = new Size(240, 22);
			searchBox.TextChanged += delegate
			{
				ApplyFilter();
			};
			topPanel.Controls.Add(searchBox);

			DarkButton btnClear = new DarkButton();
			btnClear.Text = "✕";
			btnClear.Location = new Point(854, 36);
			btnClear.Size = new Size(26, 22);
			btnClear.BackColor = Ui.SurfaceAlt;
			btnClear.ForeColor = Ui.TextMuted;
			btnClear.Click += delegate
			{
				searchBox.Text = string.Empty;
			};
			topPanel.Controls.Add(btnClear);

			Controls.Add(topPanel);

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = false;
			listView.Font = Ui.FontBody(8.5f);
			listView.Columns.Add("Статья", 95, HorizontalAlignment.Left);
			listView.Columns.Add("Срок", 115, HorizontalAlignment.Left);
			listView.Columns.Add("Наименование нарушения", 230, HorizontalAlignment.Left);
			listView.Columns.Add("Описание и примеры ПО", 450, HorizontalAlignment.Left);

			ContextMenuStrip ctx = new ContextMenuStrip();
			ctx.Renderer = new DarkMenuRenderer();
			ToolStripMenuItem copyBanItem = new ToolStripMenuItem("🔨  Скопировать команду /ban по этой статье");
			copyBanItem.Click += delegate
			{
				CopyBanCommandForSelected();
			};
			ToolStripMenuItem copyDescItem = new ToolStripMenuItem("📋  Скопировать описание статьи");
			copyDescItem.Click += delegate
			{
				if (listView.SelectedItems.Count > 0)
				{
					RuleDisplayItem r = listView.SelectedItems[0].Tag as RuleDisplayItem;
					if (r != null)
					{
						Clipboard.SetText(string.Format("Статья {0}: {1} | Срок: {2}\n{3}", r.RuleTag, r.Title, r.Duration, r.Description));
					}
				}
			};
			ctx.Items.Add(copyBanItem);
			ctx.Items.Add(copyDescItem);
			listView.ContextMenuStrip = ctx;

			listView.DoubleClick += delegate
			{
				CopyBanCommandForSelected();
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

			DarkButton btnCopyBan = new DarkButton();
			btnCopyBan.Text = "🔨 Скопировать /ban";
			btnCopyBan.Size = new Size(200, 28);
			btnCopyBan.Location = new Point(14, 8);
			btnCopyBan.BackColor = Ui.AccentRed;
			btnCopyBan.ForeColor = Color.White;
			btnCopyBan.Font = Ui.FontBody(8.5f, true);
			btnCopyBan.Cursor = Cursors.Hand;
			btnCopyBan.Click += delegate
			{
				CopyBanCommandForSelected();
			};
			bottomPanel.Controls.Add(btnCopyBan);

			DarkButton btnCopyAll = new DarkButton();
			btnCopyAll.Text = "📋 Скопировать весь регламент";
			btnCopyAll.Size = new Size(240, 28);
			btnCopyAll.Location = new Point(222, 8);
			btnCopyAll.BackColor = Ui.SurfaceAlt;
			btnCopyAll.ForeColor = Ui.TextPrimary;
			btnCopyAll.Cursor = Cursors.Hand;
			btnCopyAll.Click += delegate
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("=== РЕГЛАМЕНТ ПРОВЕРОК НА ЧИТЫ И БАГОЮЗ (РАЗДЕЛ 5 & 2.06) ===");
				sb.AppendLine();
				foreach (var r in allRules)
				{
					sb.AppendLine(string.Format("[{0}] {1} (Срок: {2})", r.RuleTag, r.Title, r.Duration));
					sb.AppendLine("  Причина: " + r.Reason);
					sb.AppendLine("  Описание: " + r.Description);
					sb.AppendLine();
				}
				Clipboard.SetText(sb.ToString());
				MessageBox.Show("Весь регламент проверок скопирован в буфер обмена!", "Регламент", MessageBoxButtons.OK, MessageBoxIcon.Information);
			};
			bottomPanel.Controls.Add(btnCopyAll);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(110, 28);
			btnClose.Location = new Point(780, 8);
			btnClose.BackColor = Ui.SurfaceAlt;
			btnClose.ForeColor = Ui.TextPrimary;
			btnClose.DialogResult = DialogResult.OK;
			bottomPanel.Controls.Add(btnClose);

			Controls.Add(bottomPanel);
			bottomPanel.BringToFront();

			ApplyFilter();
		}

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			try
			{
				listView.Items.Clear();
				string query = searchBox.Text.Trim();

				foreach (var r in allRules)
				{
					if (!string.IsNullOrEmpty(query))
					{
						if (r.RuleTag.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							r.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							r.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
							r.Duration.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
					}

					ListViewItem lvi = new ListViewItem(r.RuleTag);
					lvi.SubItems.Add(r.Duration);
					lvi.SubItems.Add(r.Title);
					lvi.SubItems.Add(r.Description);
					lvi.ForeColor = r.AccentColor;
					lvi.Tag = r;

					listView.Items.Add(lvi);
				}
			}
			finally
			{
				listView.EndUpdate();
			}
		}

		private void CopyBanCommandForSelected()
		{
			if (listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("Выберите статью из списка для формирования команды бана.", "Справочник регламента", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			RuleDisplayItem r = listView.SelectedItems[0].Tag as RuleDisplayItem;
			if (r == null) return;

			if (r.IsAllowed)
			{
				MessageBox.Show("По статье «Белый список» бан строго запрещён!\nКликеры и AnyDesk разрешены.", "Белый список", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} {1} {2}", targetPlayerName, r.Duration, r.Reason);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер обмена:\n\n" + cmd, "MC Cheat Checker — Регламент", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
