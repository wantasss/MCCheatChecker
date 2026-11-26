using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class EvidenceCaptureForm : Form
	{
		#region Fields & Controls

		private string targetPlayerName;
		private string targetModeratorName;
		private Bitmap currentScreenshot;
		private Bitmap watermarkedScreenshot;
		private string currentSha256 = string.Empty;

		private PictureBox picPreview;
		private DarkTextBox txtPlayer;
		private DarkTextBox txtModerator;
		private ComboBox cmbRule;
		private DarkTextBox txtReason;
		private Label lblStatus;
		private Label lblHash;

		private DarkButton btnCaptureScreen;
		private DarkButton btnCaptureWindow;
		private DarkButton btnCopyImage;
		private DarkButton btnSaveDesktop;
		private DarkButton btnCopyForumText;
		private DarkButton btnOpenFolder;

		#endregion

		#region UI Initialization

		public EvidenceCaptureForm(string player = "Player", string moderator = "")
		{
			targetPlayerName = string.IsNullOrEmpty(player) ? "Player" : player;
			targetModeratorName = string.IsNullOrEmpty(moderator) ? Environment.UserName : moderator;

			InitializeComponent();
			CaptureFullScreen();
		}

		private void InitializeComponent()
		{
			this.Text = "Фиксация доказательств и цифровой отпечаток (Evidence Capture) — MCCheatChecker";
			this.Size = new Size(1150, 740);
			this.MinimumSize = new Size(950, 600);
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
					CaptureFullScreen();
					e.Handled = true;
				}
			};

			// Верхняя панель
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 70;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			Label lblTitle = new Label();
			lblTitle.Text = "📸 Цифровая фиксация доказательств с криптографическим штампом времени";
			lblTitle.Font = Ui.FontTitle(13.5f, true);
			lblTitle.ForeColor = Ui.TextPrimary;
			lblTitle.Location = new Point(16, 12);
			lblTitle.AutoSize = true;
			headerPanel.Controls.Add(lblTitle);

			Label lblSub = new Label();
			lblSub.Text = "Создание заверенных снимков экрана с водяным знаком сервера, временем UTC, никами и SHA-256 для тем на форуме / апелляций.";
			lblSub.Font = Ui.FontBody(9f, false);
			lblSub.ForeColor = Ui.TextSecondary;
			lblSub.Location = new Point(18, 40);
			lblSub.AutoSize = true;
			headerPanel.Controls.Add(lblSub);

			// Левая панель параметров
			Panel pnlSidebar = new Panel();
			pnlSidebar.Dock = DockStyle.Left;
			pnlSidebar.Width = 320;
			pnlSidebar.BackColor = Ui.SurfaceAlt;
			pnlSidebar.Padding = new Padding(14);
			this.Controls.Add(pnlSidebar);

			int curY = 14;

			Label lblParamHeading = new Label();
			lblParamHeading.Text = "ПАРАМЕТРЫ ФИКСАЦИИ:";
			lblParamHeading.Font = Ui.FontTitle(8.5f, true);
			lblParamHeading.ForeColor = Ui.TextMuted;
			lblParamHeading.Location = new Point(14, curY);
			lblParamHeading.AutoSize = true;
			pnlSidebar.Controls.Add(lblParamHeading);
			curY += 24;

			Label lblPl = new Label();
			lblPl.Text = "Никнейм игрока:";
			lblPl.Font = Ui.FontBody(9f, true);
			lblPl.ForeColor = Ui.TextPrimary;
			lblPl.Location = new Point(14, curY);
			lblPl.AutoSize = true;
			pnlSidebar.Controls.Add(lblPl);
			curY += 20;

			txtPlayer = new DarkTextBox();
			txtPlayer.Text = targetPlayerName;
			txtPlayer.Location = new Point(14, curY);
			txtPlayer.Size = new Size(285, 26);
			txtPlayer.Font = Ui.FontBody(9.5f, false);
			txtPlayer.TextChanged += delegate { ApplyWatermark(); };
			pnlSidebar.Controls.Add(txtPlayer);
			curY += 34;

			Label lblMod = new Label();
			lblMod.Text = "Никнейм модератора:";
			lblMod.Font = Ui.FontBody(9f, true);
			lblMod.ForeColor = Ui.TextPrimary;
			lblMod.Location = new Point(14, curY);
			lblMod.AutoSize = true;
			pnlSidebar.Controls.Add(lblMod);
			curY += 20;

			txtModerator = new DarkTextBox();
			txtModerator.Text = targetModeratorName;
			txtModerator.Location = new Point(14, curY);
			txtModerator.Size = new Size(285, 26);
			txtModerator.Font = Ui.FontBody(9.5f, false);
			txtModerator.TextChanged += delegate { ApplyWatermark(); };
			pnlSidebar.Controls.Add(txtModerator);
			curY += 34;

			Label lblR = new Label();
			lblR.Text = "Пункт регламента:";
			lblR.Font = Ui.FontBody(9f, true);
			lblR.ForeColor = Ui.TextPrimary;
			lblR.Location = new Point(14, curY);
			lblR.AutoSize = true;
			pnlSidebar.Controls.Add(lblR);
			curY += 20;

			cmbRule = new ComboBox();
			cmbRule.DropDownStyle = ComboBoxStyle.DropDownList;
			cmbRule.BackColor = Ui.Surface;
			cmbRule.ForeColor = Ui.TextPrimary;
			cmbRule.Font = Ui.FontBody(9f, false);
			cmbRule.Location = new Point(14, curY);
			cmbRule.Size = new Size(285, 26);
			cmbRule.Items.Add("5.01 — Использование читов");
			cmbRule.Items.Add("5.08 — Очистка / сокрытие улик / отказ");
			cmbRule.Items.Add("2.06 — Багоюз / вредоносные действия");
			cmbRule.Items.Add("5.05 — Запрещенные миникарты");
			cmbRule.Items.Add("5.02 — Неразрешенные модификации");
			cmbRule.SelectedIndex = 0;
			cmbRule.SelectedIndexChanged += delegate { ApplyWatermark(); };
			pnlSidebar.Controls.Add(cmbRule);
			curY += 34;

			Label lblRs = new Label();
			lblRs.Text = "Детали / Найденный софт:";
			lblRs.Font = Ui.FontBody(9f, true);
			lblRs.ForeColor = Ui.TextPrimary;
			lblRs.Location = new Point(14, curY);
			lblRs.AutoSize = true;
			pnlSidebar.Controls.Add(lblRs);
			curY += 20;

			txtReason = new DarkTextBox();
			txtReason.Text = "Обнаружены следы чит-клиента в памяти / RecentDocs";
			txtReason.Location = new Point(14, curY);
			txtReason.Size = new Size(285, 26);
			txtReason.Font = Ui.FontBody(9.5f, false);
			txtReason.TextChanged += delegate { ApplyWatermark(); };
			pnlSidebar.Controls.Add(txtReason);
			curY += 40;

			btnCaptureScreen = new DarkButton();
			btnCaptureScreen.Text = "📸 Захватить экран (F5)";
			btnCaptureScreen.Font = Ui.FontTitle(9f, true);
			btnCaptureScreen.Location = new Point(14, curY);
			btnCaptureScreen.Size = new Size(285, 32);
			btnCaptureScreen.Click += delegate { CaptureFullScreen(); };
			pnlSidebar.Controls.Add(btnCaptureScreen);
			curY += 38;

			btnCaptureWindow = new DarkButton();
			btnCaptureWindow.Text = "🪟 Захватить только окно утилиты";
			btnCaptureWindow.Font = Ui.FontBody(9f, false);
			btnCaptureWindow.Location = new Point(14, curY);
			btnCaptureWindow.Size = new Size(285, 30);
			btnCaptureWindow.Click += delegate { CaptureOwnerWindow(); };
			pnlSidebar.Controls.Add(btnCaptureWindow);
			curY += 44;

			Label lblHashHeading = new Label();
			lblHashHeading.Text = "ОТПЕЧАТОК ХЭША (SHA-256):";
			lblHashHeading.Font = Ui.FontTitle(8f, true);
			lblHashHeading.ForeColor = Ui.TextMuted;
			lblHashHeading.Location = new Point(14, curY);
			lblHashHeading.AutoSize = true;
			pnlSidebar.Controls.Add(lblHashHeading);
			curY += 18;

			lblHash = new Label();
			lblHash.Text = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
			lblHash.Font = Ui.FontMono(7.5f, false);
			lblHash.ForeColor = Ui.AccentTeal;
			lblHash.Location = new Point(14, curY);
			lblHash.Size = new Size(285, 36);
			pnlSidebar.Controls.Add(lblHash);

			// Нижняя панель действий
			Panel bottomBar = new Panel();
			bottomBar.Dock = DockStyle.Bottom;
			bottomBar.Height = 50;
			bottomBar.BackColor = Ui.Surface;
			bottomBar.Padding = new Padding(14, 8, 14, 8);
			this.Controls.Add(bottomBar);

			btnCopyImage = new DarkButton();
			btnCopyImage.Text = "📋 Скопировать снимок в буфер";
			btnCopyImage.Font = Ui.FontTitle(9f, true);
			btnCopyImage.Location = new Point(14, 10);
			btnCopyImage.Size = new Size(230, 30);
			btnCopyImage.Click += BtnCopyImage_Click;
			bottomBar.Controls.Add(btnCopyImage);

			btnSaveDesktop = new DarkButton();
			btnSaveDesktop.Text = "💾 Сохранить на Рабочий стол";
			btnSaveDesktop.Font = Ui.FontBody(9f, false);
			btnSaveDesktop.Location = new Point(252, 10);
			btnSaveDesktop.Size = new Size(220, 30);
			btnSaveDesktop.Click += BtnSaveDesktop_Click;
			bottomBar.Controls.Add(btnSaveDesktop);

			btnCopyForumText = new DarkButton();
			btnCopyForumText.Text = "📝 Текст апелляционного протокола";
			btnCopyForumText.Font = Ui.FontBody(9f, false);
			btnCopyForumText.Location = new Point(480, 10);
			btnCopyForumText.Size = new Size(245, 30);
			btnCopyForumText.Click += BtnCopyForumText_Click;
			bottomBar.Controls.Add(btnCopyForumText);

			btnOpenFolder = new DarkButton();
			btnOpenFolder.Text = "📁 Открыть Рабочий стол";
			btnOpenFolder.Font = Ui.FontBody(9f, false);
			btnOpenFolder.Location = new Point(733, 10);
			btnOpenFolder.Size = new Size(180, 30);
			btnOpenFolder.Click += delegate
			{
				try
				{
					Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
				}
				catch { }
			};
			bottomBar.Controls.Add(btnOpenFolder);

			lblStatus = new Label();
			lblStatus.Text = "Снимок готов к экспорту";
			lblStatus.Font = Ui.FontBody(9f, true);
			lblStatus.ForeColor = Ui.AccentGreen;
			lblStatus.Location = new Point(bottomBar.Width - 210, 15);
			lblStatus.AutoSize = true;
			lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			bottomBar.Controls.Add(lblStatus);

			// Панель предпросмотра
			Panel pnlPreview = new Panel();
			pnlPreview.Dock = DockStyle.Fill;
			pnlPreview.BackColor = Ui.Backdrop;
			pnlPreview.Padding = new Padding(12);
			this.Controls.Add(pnlPreview);

			picPreview = new PictureBox();
			picPreview.Dock = DockStyle.Fill;
			picPreview.SizeMode = PictureBoxSizeMode.Zoom;
			picPreview.BackColor = Color.FromArgb(10, 12, 15);
			pnlPreview.Controls.Add(picPreview);
		}

		#endregion

		#region Capture & Watermarking Engine

		private void CaptureFullScreen()
		{
			try
			{
				// Скрываем форму на мгновение для захвата чистого экрана
				this.Opacity = 0.0;
				Application.DoEvents();
				System.Threading.Thread.Sleep(120);

				Rectangle bounds = Screen.PrimaryScreen.Bounds;
				Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
				}

				this.Opacity = 1.0;

				if (currentScreenshot != null) currentScreenshot.Dispose();
				currentScreenshot = bmp;

				ApplyWatermark();
			}
			catch (Exception ex)
			{
				this.Opacity = 1.0;
				MessageBox.Show("Не удалось выполнить захват экрана: " + ex.Message, "Ошибка захвата", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CaptureOwnerWindow()
		{
			try
			{
				Form target = this.Owner ?? this;
				Rectangle bounds = target.Bounds;
				if (bounds.Width <= 0 || bounds.Height <= 0) return;

				Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
				}

				if (currentScreenshot != null) currentScreenshot.Dispose();
				currentScreenshot = bmp;

				ApplyWatermark();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось захватить окно: " + ex.Message, "Ошибка захвата", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ApplyWatermark()
		{
			if (currentScreenshot == null) return;

			try
			{
				Bitmap result = new Bitmap(currentScreenshot.Width, currentScreenshot.Height, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(result))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;

					// Отрисовка базового снимка
					g.DrawImage(currentScreenshot, 0, 0, currentScreenshot.Width, currentScreenshot.Height);

					// Вычисление SHA-256 исходного снимка
					currentSha256 = ComputeImageHash(currentScreenshot);
					lblHash.Text = currentSha256;

					// Размеры плашки водяного знака
					int badgeW = Math.Min(680, currentScreenshot.Width - 40);
					int badgeH = 140;
					int badgeX = currentScreenshot.Width - badgeW - 24;
					int badgeY = currentScreenshot.Height - badgeH - 24;

					Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);

					// Полупрозрачный темный фон плашки
					using (GraphicsPath path = Ui.RoundedRect(badgeRect, 10))
					{
						using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(220, 15, 18, 24)))
						{
							g.FillPath(bgBrush, path);
						}
						using (Pen borderPen = new Pen(Color.FromArgb(180, 46, 230, 214), 1.5f))
						{
							g.DrawPath(borderPen, path);
						}
					}

					// Тексты в плашке
					string pName = string.IsNullOrEmpty(txtPlayer.Text) ? targetPlayerName : txtPlayer.Text.Trim();
					string mName = string.IsNullOrEmpty(txtModerator.Text) ? targetModeratorName : txtModerator.Text.Trim();
					string rText = cmbRule.SelectedItem != null ? cmbRule.SelectedItem.ToString() : "5.01 — Использование читов";
					string reason = string.IsNullOrEmpty(txtReason.Text) ? "Нарушение регламента сервера" : txtReason.Text.Trim();
					DateTime now = DateTime.Now;

					using (Font fHead = Ui.FontTitle(10.5f, true))
					using (Font fBody = Ui.FontBody(9f, false))
					using (Font fBold = Ui.FontBody(9f, true))
					using (Font fMono = Ui.FontMono(8f, false))
					using (SolidBrush bWhite = new SolidBrush(Color.FromArgb(245, 248, 252)))
					using (SolidBrush bTeal = new SolidBrush(Color.FromArgb(46, 230, 214)))
					using (SolidBrush bRed = new SolidBrush(Color.FromArgb(244, 67, 54)))
					using (SolidBrush bMuted = new SolidBrush(Color.FromArgb(160, 170, 185)))
					{
						int tx = badgeX + 16;
						int ty = badgeY + 12;

						// Заголовок
						g.DrawString("🛡️ ОФИЦИАЛЬНАЯ ФИКСАЦИЯ SCREENSHARE | MCCheatChecker Forensic", fHead, bTeal, tx, ty);
						ty += 24;

						// Игрок и модератор
						g.DrawString(string.Format("👤 Проверяемый: {0}   |   👮 Модератор: {1}", pName, mName), fBold, bWhite, tx, ty);
						ty += 20;

						// Пункт правил
						g.DrawString(string.Format("⚖️ Пункт регламента: {0} ({1})", rText, reason), fBold, bRed, tx, ty);
						ty += 20;

						// Время UTC и локальное
						string timeStr = string.Format("🕒 Время фиксации: {0:yyyy-MM-dd HH:mm:ss} (Local) | {1:yyyy-MM-dd HH:mm:ss} (UTC)", now, now.ToUniversalTime());
						g.DrawString(timeStr, fBody, bMuted, tx, ty);
						ty += 20;

						// SHA-256
						string hashStr = string.Format("🔐 SHA-256: {0}", currentSha256);
						g.DrawString(hashStr, fMono, bTeal, tx, ty);
					}
				}

				if (watermarkedScreenshot != null) watermarkedScreenshot.Dispose();
				watermarkedScreenshot = result;
				picPreview.Image = watermarkedScreenshot;

				lblStatus.Text = "Снимок заверен и готов";
				lblStatus.ForeColor = Ui.AccentGreen;
			}
			catch
			{
				lblStatus.Text = "Ошибка водяного знака";
				lblStatus.ForeColor = Ui.AccentRed;
			}
		}

		private string ComputeImageHash(Bitmap bmp)
		{
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					bmp.Save(ms, ImageFormat.Png);
					using (SHA256 sha = SHA256.Create())
					{
						byte[] hash = sha.ComputeHash(ms.ToArray());
						StringBuilder sb = new StringBuilder(64);
						foreach (byte b in hash) sb.Append(b.ToString("x2"));
						return sb.ToString();
					}
				}
			}
			catch
			{
				return "hash_computation_failed";
			}
		}

		#endregion

		#region Actions

		private void BtnCopyImage_Click(object sender, EventArgs e)
		{
			if (watermarkedScreenshot == null) return;
			try
			{
				Clipboard.SetImage(watermarkedScreenshot);
				lblStatus.Text = "Снимок скопирован в буфер обмена";
				lblStatus.ForeColor = Ui.AccentGreen;
				MessageBox.Show("Снимок доказательств с водяным знаком скопирован в буфер обмена!\nВы можете вставить его напрямую в Discord, форум или чат (Ctrl+V).", "Снимок скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось скопировать снимок: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSaveDesktop_Click(object sender, EventArgs e)
		{
			if (watermarkedScreenshot == null) return;
			try
			{
				string pName = string.IsNullOrEmpty(txtPlayer.Text) ? targetPlayerName : txtPlayer.Text.Trim();
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				string fileName = string.Format("Доказательство_{0}_{1:yyyyMMdd_HHmmss}.png", pName, DateTime.Now);
				string fullPath = Path.Combine(desktop, fileName);

				watermarkedScreenshot.Save(fullPath, ImageFormat.Png);

				lblStatus.Text = "Сохранено на Рабочий стол";
				lblStatus.ForeColor = Ui.AccentGreen;
				MessageBox.Show("Снимок доказательств сохранен на Рабочий стол:\n" + fullPath, "Файл сохранен", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось сохранить файл: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnCopyForumText_Click(object sender, EventArgs e)
		{
			string pName = string.IsNullOrEmpty(txtPlayer.Text) ? targetPlayerName : txtPlayer.Text.Trim();
			string mName = string.IsNullOrEmpty(txtModerator.Text) ? targetModeratorName : txtModerator.Text.Trim();
			string rText = cmbRule.SelectedItem != null ? cmbRule.SelectedItem.ToString() : "5.01";
			string reason = string.IsNullOrEmpty(txtReason.Text) ? "Использование стороннего ПО" : txtReason.Text.Trim();
			DateTime now = DateTime.Now;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```");
			sb.AppendLine("=== ОФИЦИАЛЬНЫЙ ПРОТОКОЛ ФИКСАЦИИ ДОКАЗАТЕЛЬСТВ SCREENSHARE ===");
			sb.AppendLine(string.Format("Никнейм нарушителя: {0}", pName));
			sb.AppendLine(string.Format("Проверяющий модератор: {0}", mName));
			sb.AppendLine(string.Format("Пункт регламента: {0}", rText));
			sb.AppendLine(string.Format("Причина / Найденные улики: {0}", reason));
			sb.AppendLine(string.Format("Дата и время (Локальное): {0:yyyy-MM-dd HH:mm:ss}", now));
			sb.AppendLine(string.Format("Дата и время (UTC): {0:yyyy-MM-dd HH:mm:ss} UTC", now.ToUniversalTime()));
			sb.AppendLine(string.Format("Цифровой отпечаток снимка (SHA-256): {0}", currentSha256));
			sb.AppendLine(string.Format("Команда выданного бана: /ban {0} 30d {1} ({2})", pName, rText.Split('—')[0].Trim(), reason));
			sb.AppendLine("================================================================");
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			lblStatus.Text = "Текст протокола скопирован";
			lblStatus.ForeColor = Ui.AccentTeal;
			MessageBox.Show("Текст официального протокола проверки скопирован в буфер обмена!\nИспользуйте его при ответе в жалобах / тикетах апелляции.\n\n" + sb.ToString(), "Протокол скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
