using System;
using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkTextBox : TextBox
	{
		private string placeholder = "";

		public string Placeholder
		{
			get
			{
				return placeholder;
			}
			set
			{
				placeholder = value ?? "";
				Invalidate();
			}
		}

		public DarkTextBox()
		{
			BackColor = Ui.SurfaceAlt;
			ForeColor = Ui.TextPrimary;
			base.BorderStyle = BorderStyle.FixedSingle;
			Font = Ui.FontBody(9.5f);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Ui.ApplyDarkScrollbars(this);
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (placeholder.Length == 0 || TextLength > 0 || Focused)
			{
				return;
			}
			using (SolidBrush solidBrush = new SolidBrush(Ui.TextMuted))
			{
				TextRenderer.DrawText(e.Graphics, placeholder, Font, new Rectangle(2, 2, base.Width - 4, base.Height - 4), solidBrush.Color, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			}
		}
	}
}
