using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkButton : Button
	{
		private bool accentStyle;

		private bool pressedVisual;

		public bool AccentStyle
		{
			get
			{
				return accentStyle;
			}
			set
			{
				accentStyle = value;
				Invalidate();
			}
		}

		public DarkButton()
		{
			base.FlatStyle = FlatStyle.Flat;
			base.FlatAppearance.BorderSize = 0;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			Font = Ui.FontBody(9f);
			base.Height = 30;
			Cursor = Cursors.Hand;
			BackColor = Ui.SurfaceAlt;
			ForeColor = Ui.TextPrimary;
			base.UseVisualStyleBackColor = false;
		}

		protected override void OnMouseDown(MouseEventArgs mevent)
		{
			base.OnMouseDown(mevent);
			pressedVisual = true;
			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs mevent)
		{
			base.OnMouseUp(mevent);
			pressedVisual = false;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			pressedVisual = false;
			Invalidate();
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			Invalidate();
		}

		private static Color Lighten(Color c, float amount)
		{
			return Color.FromArgb(c.A, (int)Math.Min(255f, (float)(int)c.R + 255f * amount), (int)Math.Min(255f, (float)(int)c.G + 255f * amount), (int)Math.Min(255f, (float)(int)c.B + 255f * amount));
		}

		private static Color Darken(Color c, float amount)
		{
			return Color.FromArgb(c.A, (int)Math.Max(0f, (float)(int)c.R - 255f * amount), (int)Math.Max(0f, (float)(int)c.G - 255f * amount), (int)Math.Max(0f, (float)(int)c.B - 255f * amount));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Color color = BackColor;
			Color foreColor = ForeColor;
			if (!base.Enabled)
			{
				color = Color.FromArgb(26, 29, 34);
				foreColor = Color.FromArgb(84, 92, 103);
			}
			else if (pressedVisual)
			{
				color = Darken(BackColor, accentStyle ? 0.1f : 0.06f);
			}
			else if (Focused)
			{
				color = Lighten(BackColor, 0.04f);
			}
			Rectangle bounds = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
			using (GraphicsPath path = Ui.RoundedRect(bounds, 6))
			{
				using (SolidBrush brush = new SolidBrush(color))
				{
					graphics.FillPath(brush, path);
				}
				if (!base.Enabled)
				{
					using (Pen pen = new Pen(Color.FromArgb(34, 39, 46)))
					{
						graphics.DrawPath(pen, path);
					}
				}
				else if (!accentStyle)
				{
					using (Pen pen2 = new Pen(Lighten(Ui.Border, 0.06f)))
					{
						graphics.DrawPath(pen2, path);
					}
				}
			}
			TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
			switch (TextAlign)
			{
			case ContentAlignment.TopLeft:
			case ContentAlignment.MiddleLeft:
			case ContentAlignment.BottomLeft:
				// Left aligned by default
				break;
			case ContentAlignment.TopRight:
			case ContentAlignment.MiddleRight:
			case ContentAlignment.BottomRight:
				textFormatFlags |= TextFormatFlags.Right;
				break;
			default:
				textFormatFlags |= TextFormatFlags.HorizontalCenter;
				break;
			}
			textFormatFlags |= TextFormatFlags.VerticalCenter;
			Rectangle clientRectangle = base.ClientRectangle;
			if (textFormatFlags.HasFlag(TextFormatFlags.Default))
			{
				clientRectangle.X += base.Padding.Left + 6;
			}
			if (textFormatFlags.HasFlag(TextFormatFlags.Right))
			{
				clientRectangle.Width -= base.Padding.Right + 6;
			}
			TextRenderer.DrawText(graphics, Text, Font, clientRectangle, foreColor, textFormatFlags);
		}
	}
}
