using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkTabHeader : Control
	{
		private bool active;

		public bool Active
		{
			get
			{
				return active;
			}
			set
			{
				active = value;
				ForeColor = (active ? Ui.TextPrimary : Ui.TextSecondary);
				Invalidate();
			}
		}

		public DarkTabHeader()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextSecondary;
			Cursor = Cursors.Hand;
			base.Height = 30;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			using (SolidBrush brush = new SolidBrush(active ? Ui.Surface : Ui.Backdrop))
			{
				graphics.FillRectangle(brush, base.ClientRectangle);
			}
			if (active)
			{
				using (SolidBrush brush2 = new SolidBrush(Ui.AccentTeal))
				{
					graphics.FillRectangle(brush2, 0, 0, base.Width, 2);
				}
			}
			using (Pen pen = new Pen(Ui.Border))
			{
				graphics.DrawLine(pen, base.Width - 1, 6, base.Width - 1, base.Height - 1);
				graphics.DrawLine(pen, 0, base.Height - 1, base.Width, base.Height - 1);
			}
			TextRenderer.DrawText(graphics, Text, Font, new Rectangle(12, 0, base.Width - 20, base.Height), ForeColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
		}
	}
}
