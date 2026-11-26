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
	public class WindowsNotificationForensicsViewerForm : Form
	{
		#region Models

		public enum ToastFilterMode
		{
			All,
			ThreatsAndAntivirus,
			MessengersOnly,
			BrowsersOnly,
			SystemOnly
		}

		public class ToastNotificationItem
		{
			public string AppSource;     // "Windows Защитник / SmartScreen", "Discord", "Telegram Desktop", "Браузер (Chrome/Edge)"
			public string Title;         // Заголовок оповещения
			public string Content;       // Текст уведомления
			public string Category;      // "Алерт антивируса о чите", "Сообщение мессенджера", "Загрузка браузера"
			public string Status;        // Вердикт
			public bool IsThreat;
			public string RuleViolation; // "5.01", "5.02", "5.08", "-"
			public string BanReason;
			public Color ForeColor;
			public string RawXml;
			public string Details;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private ToastFilterMode currentFilter = ToastFilterMode.All;
		private List<ToastNotificationItem> allEntries = new List<ToastNotificationItem>();
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
		private DarkButton btnFilterThreats;
		private DarkButton btnFilterMessengers;
		private DarkButton btnFilterBrowsers;
		private DarkButton btnFilterSystem;

		private DarkButton btnCopyBan;
		private DarkButton btnCopyText;
		private DarkButton btnEvidenceCapture;
		private DarkButton btnExportReport;
		private DarkButton btnRefreshNow;

		private static readonly Regex AntivirusThreatRegex = new Regex(
			@"(?i)(Trojan:Win32|HackTool:Win32|VirTool:Win32|Behavior:Win32|PUA:|PUP:|Backdoor:|vape|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|minced|wexside|killaura|autototem|injector|cheat)",
			RegexOptions.Compiled);

		private static readonly Regex MessengerCheatRegex = new Regex(
			@"(?i)(vape|celestial|expensive|nursultan|wild|akrien|чит|кфг|\.cfg|\.jar|инжект|cheat|config)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public WindowsNotificationForensicsViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScanAsync();
		}

		private void InitializeComponent()
		{
			this.Text = "Форензика центра уведомлений Windows (wpndatabase.db) — MCCheatChecker";
			this.Size = new Size(1220, 750);
			this.MinimumSize = new Size(980, 580);
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
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔔 Форензика базы центра уведомлений Windows Action Center (wpndatabase.db)";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Извлечение тостов всплывающих оповещений: предупреждения Windows Defender о читах, сообщения Discord/Telegram и завершенные загрузки.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 42);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Сканирование...";
			lblStatusPill.Font = Ui.FontTitle(9f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Color.FromArgb(20, 45, 45);
			lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
			lblStatusPill.Size = new Size(260, 28);
			lblStatusPill.Location = new Point(headerPanel.Width - 280, 22);
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
			btnFilterAll = CreateFilterButton("Все уведомления", ToastFilterMode.All, ref bx, filterBar);
			btnFilterThreats = CreateFilterButton("🚨 Алерты антивируса (5.01)", ToastFilterMode.ThreatsAndAntivirus, ref bx, filterBar);
			btnFilterMessengers = CreateFilterButton("💬 Discord и Telegram", ToastFilterMode.MessengersOnly, ref bx, filterBar);
			btnFilterBrowsers = CreateFilterButton("🌐 Загрузки браузеров", ToastFilterMode.BrowsersOnly, ref bx, filterBar);
			btnFilterSystem = CreateFilterButton("⚙️ Системные тосты", ToastFilterMode.SystemOnly, ref bx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 250, 8);
			searchBox.Size = new Size(235, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 305, 12);
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
			btnCopyBan.Size = new Size(230, 30);
			btnCopyBan.Click += BtnCopyBan_Click;
			bottomBar.Controls.Add(btnCopyBan);

			btnCopyText = new DarkButton();
			btnCopyText.Text = "📄 Скопировать текст оповещения";
			btnCopyText.Font = Ui.FontBody(9f, false);
			btnCopyText.Location = new Point(252, 10);
			btnCopyText.Size = new Size(240, 30);
			btnCopyText.Click += BtnCopyText_Click;
			bottomBar.Controls.Add(btnCopyText);

			btnEvidenceCapture = new DarkButton();
			btnEvidenceCapture.Text = "📸 Зафиксировать улику";
			btnEvidenceCapture.Font = Ui.FontBody(9f, false);
			btnEvidenceCapture.Location = new Point(500, 10);
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
			btnExportReport.Location = new Point(693, 10);
			btnExportReport.Size = new Size(150, 30);
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
			split.SplitterDistance = 380;
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

			listView.Columns.Add("Источник оповещения", 200);
			listView.Columns.Add("Заголовок / Приложение", 240);
			listView.Columns.Add("Текст уведомления / Оповещение", 420);
			listView.Columns.Add("Вердикт / Регламент", 260);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			split.Panel1.Controls.Add(listView);

			// Панель подробностей
			Panel pnlDetailsContainer = new Panel();
			pnlDetailsContainer.Dock = DockStyle.Fill;
			pnlDetailsContainer.BackColor = Ui.Surface;
			pnlDetailsContainer.Padding = new Padding(16, 10, 16, 10);
			split.Panel2.Controls.Add(pnlDetailsContainer);

			Label lblDetailsHeading = new Label();
			lblDetailsHeading.Text = "КРИМИНАЛИСТИЧЕСКИЕ ДАННЫЕ ОПОВЕЩЕНИЯ ЦЕНТРА УВЕДОМЛЕНИЙ:";
			lblDetailsHeading.Font = Ui.FontTitle(8.5f, true);
			lblDetailsHeading.ForeColor = Ui.TextMuted;
			lblDetailsHeading.Location = new Point(16, 10);
			lblDetailsHeading.AutoSize = true;
			pnlDetailsContainer.Controls.Add(lblDetailsHeading);

			lblDetailVerdict = new Label();
			lblDetailVerdict.Text = "Выберите запись из таблицы.";
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

		private DarkButton CreateFilterButton(string text, ToastFilterMode mode, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, mode == ToastFilterMode.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(160, 28);
			btn.Click += delegate
			{
				currentFilter = mode;
				HighlightFilterButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 166;
			return btn;
		}

		private void HighlightFilterButtons()
		{
			btnFilterAll.AccentStyle = (currentFilter == ToastFilterMode.All);
			btnFilterThreats.AccentStyle = (currentFilter == ToastFilterMode.ThreatsAndAntivirus);
			btnFilterMessengers.AccentStyle = (currentFilter == ToastFilterMode.MessengersOnly);
			btnFilterBrowsers.AccentStyle = (currentFilter == ToastFilterMode.BrowsersOnly);
			btnFilterSystem.AccentStyle = (currentFilter == ToastFilterMode.SystemOnly);
		}

		#endregion

		#region Forensic Scanner Engine

		private void StartScanAsync()
		{
			if (isScanning) return;
			isScanning = true;
			lblStatusPill.Text = "Чтение wpndatabase.db...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			lblStatusPill.BackColor = Color.FromArgb(45, 35, 15);

			listView.Items.Clear();
			allEntries.Clear();

			ThreadPool.QueueUserWorkItem(delegate
			{
				List<ToastNotificationItem> results = new List<ToastNotificationItem>();

				ScanWpnDatabase(results);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					allEntries = results;
					int threatCount = results.Count(delegate(ToastNotificationItem x) { return x.IsThreat; });

					if (threatCount > 0)
					{
						lblStatusPill.Text = string.Format("🚨 УГРОЗ В БАЗЕ УВЕДОМЛЕНИЙ: {0}", threatCount);
						lblStatusPill.ForeColor = Ui.AccentRed;
						lblStatusPill.BackColor = Color.FromArgb(50, 15, 15);
					}
					else
					{
						lblStatusPill.Text = string.Format("🛡️ Уведомлений проверено: {0}", results.Count);
						lblStatusPill.ForeColor = Ui.AccentGreen;
						lblStatusPill.BackColor = Color.FromArgb(15, 45, 20);
					}

					ApplyFilter();
				});
			});
		}

		private void ScanWpnDatabase(List<ToastNotificationItem> results)
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string wpnPath = Path.Combine(localAppData, @"Microsoft\Windows\Notifications\wpndatabase.db");

			if (!File.Exists(wpnPath))
			{
				return;
			}

			try
			{
				byte[] dbBytes;
				using (FileStream fs = new FileStream(wpnPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (MemoryStream ms = new MemoryStream())
					{
						fs.CopyTo(ms);
						dbBytes = ms.ToArray();
					}
				}

				if (dbBytes.Length == 0) return;

				string fullText = Encoding.UTF8.GetString(dbBytes);
				MatchCollection toasts = Regex.Matches(fullText, @"<toast[^>]*>(.*?)</toast>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

				foreach (Match tm in toasts)
				{
					try
					{
						string toastXml = tm.Value;
						MatchCollection textMatches = Regex.Matches(toastXml, @"<text[^>]*>(.*?)</text>", RegexOptions.IgnoreCase);

						List<string> texts = new List<string>();
						foreach (Match tmInner in textMatches)
						{
							string tVal = tmInner.Groups[1].Value.Trim();
							if (!string.IsNullOrEmpty(tVal))
							{
								texts.Add(tVal);
							}
						}

						if (texts.Count == 0) continue;

						string title = texts.Count > 0 ? texts[0] : "";
						string body = texts.Count > 1 ? string.Join(" | ", texts.Skip(1).ToArray()) : "";
						string combined = (title + " " + body).Trim();

						bool isAntivirus = combined.IndexOf("Защит", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("Defender", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("SmartScreen", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("Вирус", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("Угроза", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("Trojan", StringComparison.OrdinalIgnoreCase) >= 0;

						bool isMessenger = combined.IndexOf("Discord", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   combined.IndexOf("Telegram", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   toastXml.IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   toastXml.IndexOf("telegram", StringComparison.OrdinalIgnoreCase) >= 0;

						bool isBrowser = combined.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0 ||
										 combined.IndexOf("Opera", StringComparison.OrdinalIgnoreCase) >= 0 ||
										 combined.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0 ||
										 combined.IndexOf("Загрузка завершена", StringComparison.OrdinalIgnoreCase) >= 0;

						bool isThreat = AntivirusThreatRegex.IsMatch(combined);
						bool isMessengerCheat = isMessenger && MessengerCheatRegex.IsMatch(combined);

						ToastNotificationItem item = new ToastNotificationItem();
						item.Title = title;
						item.Content = body;
						item.RawXml = toastXml;

						if (isAntivirus && isThreat)
						{
							item.AppSource = "Windows Защитник / SmartScreen";
							item.Category = "Алерт антивируса о запуске чита";
							item.Status = "🚨 СРАБАТЫВАНИЕ ЗАЩИТНИКА НА ЧИТ (5.01)";
							item.IsThreat = true;
							item.RuleViolation = "5.01";
							item.BanReason = "Срабатывание Windows Defender на запуск чит-клиента";
							item.ForeColor = Ui.AccentRed;
							item.Details = string.Format("В БАЗЕ WPNDATABASE ЗАФИКСИРОВАНО ОПОВЕЩЕНИЕ ЗАЩИТНИКА О ЧИТЕ!\nЗаголовок: {0}\nТекст: {1}\nБаза: {2}\nНарушение: Пункт регламента 5.01 (Чит-клиенты) / 5.02 (Хранение читов).", title, body, wpnPath);
							results.Add(item);
						}
						else if (isMessengerCheat)
						{
							item.AppSource = isMessenger ? "Мессенджер (Discord/TG)" : "Уведомление";
							item.Category = "Передача чита / конфига в чате";
							item.Status = "🚨 УПОМИНАНИЕ ЧИТА В СООБЩЕНИИ (5.02)";
							item.IsThreat = true;
							item.RuleViolation = "5.02";
							item.BanReason = "Получение или отправка читов/конфигов через мессенджер";
							item.ForeColor = Ui.AccentOrange;
							item.Details = string.Format("В БАЗЕ УВЕДОМЛЕНИЙ ОБНАРУЖЕНО СООБЩЕНИЕ О ЧИТЕ!\nЗаголовок: {0}\nТекст: {1}\nНарушение: Пункт регламента 5.02 (Хранение/получение софта).", title, body);
							results.Add(item);
						}
						else if (isAntivirus)
						{
							item.AppSource = "Безопасность Windows";
							item.Category = "Системное предупреждение безопасности";
							item.Status = "Оповещение безопасности";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.AccentTeal;
							item.Details = string.Format("Заголовок: {0}\nТекст: {1}", title, body);
							results.Add(item);
						}
						else if (isMessenger)
						{
							item.AppSource = "Мессенджер (Discord/TG)";
							item.Category = "Сообщение мессенджера";
							item.Status = "Сообщение";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Заголовок: {0}\nТекст: {1}", title, body);
							results.Add(item);
						}
						else if (isBrowser)
						{
							item.AppSource = "Браузер";
							item.Category = "Загрузка браузера";
							item.Status = "Загрузка";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextSecondary;
							item.Details = string.Format("Заголовок: {0}\nТекст: {1}", title, body);
							results.Add(item);
						}
						else
						{
							item.AppSource = "Система Windows";
							item.Category = "Системный тост";
							item.Status = "Штатное уведомление";
							item.RuleViolation = "-";
							item.BanReason = "-";
							item.ForeColor = Ui.TextMuted;
							item.Details = string.Format("Заголовок: {0}\nТекст: {1}", title, body);
							results.Add(item);
						}
					}
					catch { }
				}
			}
			catch { }
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (ToastNotificationItem item in allEntries)
			{
				if (currentFilter == ToastFilterMode.ThreatsAndAntivirus && !item.IsThreat) continue;
				if (currentFilter == ToastFilterMode.MessengersOnly && !item.AppSource.Contains("Мессенджер")) continue;
				if (currentFilter == ToastFilterMode.BrowsersOnly && !item.AppSource.Contains("Браузер")) continue;
				if (currentFilter == ToastFilterMode.SystemOnly && !item.AppSource.Contains("Windows") && !item.AppSource.Contains("Система")) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.AppSource != null && item.AppSource.ToLowerInvariant().Contains(search)) ||
								   (item.Title != null && item.Title.ToLowerInvariant().Contains(search)) ||
								   (item.Content != null && item.Content.ToLowerInvariant().Contains(search)) ||
								   (item.Category != null && item.Category.ToLowerInvariant().Contains(search)) ||
								   (item.Status != null && item.Status.ToLowerInvariant().Contains(search)) ||
								   (item.Details != null && item.Details.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.AppSource);
				lvi.SubItems.Add(item.Title);
				lvi.SubItems.Add(item.Content);
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

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetails();
				return;
			}

			ToastNotificationItem item = listView.SelectedItems[0].Tag as ToastNotificationItem;
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
				lblDetailRule.Text = "🛡️ Нарушений не выявлено";
				lblDetailBanCommand.Text = "";
			}

			txtDetails.Text = item.Details ?? "Нет сведений.";
		}

		private void ClearDetails()
		{
			lblDetailVerdict.Text = "Запись не выбрана";
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
				MessageBox.Show("Выберите оповещение с нарушением для копирования бана.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			ToastNotificationItem item = listView.SelectedItems[0].Tag as ToastNotificationItem;
			if (item == null || string.IsNullOrEmpty(item.RuleViolation) || item.RuleViolation == "-")
			{
				MessageBox.Show("Для выбранной записи нарушения не предусмотрены.", "Копирование бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnCopyText_Click(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			ToastNotificationItem item = listView.SelectedItems[0].Tag as ToastNotificationItem;
			if (item == null) return;

			string text = string.Format("[{0}] {1}\n{2}", item.AppSource, item.Title, item.Content);
			Clipboard.SetText(text);
			MessageBox.Show("Текст уведомления скопирован в буфер обмена:\n\n" + text, "Скопировано", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void BtnExportReport_Click(object sender, EventArgs e)
		{
			try
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Уведомления_ActionCenter_Аудит_{0}_{1:yyyyMMdd_HHmmss}.txt", targetPlayerName, DateTime.Now);
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("================================================================================");
					sb.AppendLine("ОФИЦИАЛЬНЫЙ КРИМИНАЛИСТИЧЕСКИЙ ПРОТОКОЛ АУДИТА БАЗЫ УВЕДОМЛЕНИЙ WPNDATABASE.DB");
					sb.AppendLine("Инструмент: MCCheatChecker v3.0 | Проверка игрока ScreenShare");
					sb.AppendLine(string.Format("Игрок: {0} | Дата фиксации: {1:yyyy-MM-dd HH:mm:ss}", targetPlayerName, DateTime.Now));
					sb.AppendLine("================================================================================");
					sb.AppendLine();

					foreach (ToastNotificationItem item in allEntries)
					{
						sb.AppendLine(string.Format("[{0}] {1}", item.AppSource, item.Title));
						sb.AppendLine(string.Format("Содержимое:  {0}", item.Content));
						sb.AppendLine(string.Format("Вердикт:     {0}", item.Status));
						if (!string.IsNullOrEmpty(item.RuleViolation) && item.RuleViolation != "-")
						{
							sb.AppendLine(string.Format("РЕГЛАМЕНТ:   Пункт {0} ({1})", item.RuleViolation, item.BanReason));
							sb.AppendLine(string.Format("КОМАНДА:     /ban {0} 30d {1} ({2})", targetPlayerName, item.RuleViolation, item.BanReason));
						}
						sb.AppendLine(string.Format("Подробности: {0}", item.Details));
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
