using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkCheckBox : CheckBox
	{
		public DarkCheckBox()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			ForeColor = Ui.TextSecondary;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (SolidBrush brush = new SolidBrush(Ui.Surface))
			{
				graphics.FillRectangle(brush, base.ClientRectangle);
			}
			Rectangle bounds = new Rectangle(1, (base.Height - 14) / 2, 14, 14);
			using (GraphicsPath path = Ui.RoundedRect(bounds, 3))
			{
				using (SolidBrush brush2 = new SolidBrush(base.Checked ? Ui.AccentTeal : Ui.SurfaceAlt))
				{
					graphics.FillPath(brush2, path);
				}
				using (Pen pen = new Pen(base.Checked ? Ui.AccentTeal : Ui.Border))
				{
					graphics.DrawPath(pen, path);
				}
			}
			if (base.Checked)
			{
				using (Pen pen2 = new Pen(Ui.Backdrop, 2f))
				{
					pen2.StartCap = LineCap.Round;
					pen2.EndCap = LineCap.Round;
					graphics.DrawLines(pen2, new Point[3]
					{
						new Point(bounds.X + 3, bounds.Y + 7),
						new Point(bounds.X + 6, bounds.Y + 10),
						new Point(bounds.X + 11, bounds.Y + 4)
					});
				}
			}
			TextRenderer.DrawText(graphics, Text, Font, new Rectangle(22, 0, base.Width - 24, base.Height), ForeColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
		}
	}
}
