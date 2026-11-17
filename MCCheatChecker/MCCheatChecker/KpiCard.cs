using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class KpiCard : Panel
	{
		private readonly string title;

		private readonly string caption;

		private readonly Color accent;

		private int value;

		private Bitmap valueCache;

		private int cacheValue = -1;

		private Color cacheColor = Color.Empty;

		private bool isHovered;

		public KpiCard(string title, Color accentColor, string caption)
		{
			this.title = title;
			this.caption = caption;
			accent = accentColor;
			BackColor = Ui.Surface;
			DoubleBuffered = true;
			Cursor = Cursors.Hand;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			isHovered = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			isHovered = false;
			Invalidate();
		}

		public void SetValue(int newValue, string newCaption)
		{
			value = newValue;
			Invalidate();
		}

		public void SetValue(int newValue)
		{
			value = newValue;
			Invalidate();
		}

		private Bitmap RenderValue(Color color)
		{
			if (valueCache != null && cacheValue == value && cacheColor == color)
			{
				return valueCache;
			}
			if (valueCache != null)
			{
				valueCache.Dispose();
				valueCache = null;
			}
			using (Font font = Ui.FontBody(20f, true))
			{
				using (Bitmap image = new Bitmap(1, 1))
				{
					using (Graphics graphics = Graphics.FromImage(image))
					{
						SizeF sizeF = graphics.MeasureString(value.ToString(), font);
						int num = Math.Max(1, (int)Math.Ceiling(sizeF.Width) + 2);
						int num2 = Math.Max(1, (int)Math.Ceiling(sizeF.Height) + 2);
						Bitmap bitmap = new Bitmap(num, num2);
						using (Graphics graphics2 = Graphics.FromImage(bitmap))
						{
							graphics2.Clear(Ui.Surface);
							graphics2.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
							using (SolidBrush brush = new SolidBrush(color))
							{
								graphics2.DrawString(value.ToString(), font, brush, 0f, 0f);
							}
						}
						valueCache = bitmap;
						cacheValue = value;
						cacheColor = color;
						return bitmap;
					}
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = Ui.RoundedRect(new Rectangle(0, 0, base.Width - 1, base.Height - 1), 8))
			{
				if (isHovered)
				{
					using (SolidBrush brush = new SolidBrush(Color.FromArgb(32, 42, 54)))
					{
						graphics.FillPath(brush, path);
					}
					using (Pen pen = new Pen(accent, 1.5f))
					{
						graphics.DrawPath(pen, path);
					}
				}
				else
				{
					using (Pen pen2 = new Pen(Ui.Border))
					{
						graphics.DrawPath(pen2, path);
					}
				}
			}
			using (SolidBrush brush2 = new SolidBrush(accent))
			{
				graphics.FillRectangle(brush2, 0, 10, 3, Math.Max(0, base.Height - 20));
			}
			int num = 16;
			int num2 = Math.Max(10, base.Width - num - 12);
			TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
			using (Font font = Ui.FontBody(8.5f))
			{
				TextRenderer.DrawText(graphics, title, font, new Rectangle(num, 7, num2, 16), Ui.TextSecondary, textFormatFlags | TextFormatFlags.VerticalCenter);
			}
			Bitmap image = RenderValue((value > 0) ? accent : Ui.TextPrimary);
			graphics.DrawImageUnscaled(image, num - 1, 23);
			using (Font font2 = Ui.FontBody(8f))
			{
				TextRenderer.DrawText(graphics, caption, font2, new Rectangle(num, base.Height - 24, num2, 16), Ui.TextMuted, textFormatFlags | TextFormatFlags.VerticalCenter);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && valueCache != null)
			{
				valueCache.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
