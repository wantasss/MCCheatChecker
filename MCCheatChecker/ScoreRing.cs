using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ScoreRing : Control
	{
		private int score;

		private Color accent = Ui.AccentTeal;

		public int Score
		{
			get
			{
				return score;
			}
			set
			{
				score = Math.Max(0, Math.Min(100, value));
				Invalidate();
			}
		}

		public Color Accent
		{
			get
			{
				return accent;
			}
			set
			{
				accent = value;
				Invalidate();
			}
		}

		public ScoreRing()
		{
			DoubleBuffered = true;
			base.Size = new Size(112, 112);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Ui.Surface;
			Cursor = Cursors.Hand;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = 9;
			Rectangle rect = new Rectangle(num / 2 + 3, num / 2 + 3, base.Width - num - 6, base.Height - num - 6);
			using (Pen pen = new Pen(Ui.Border, num))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Round;
				e.Graphics.DrawArc(pen, rect, -90f, 360f);
			}
			float num2 = 360f * (float)score / 100f;
			if (num2 > 0f)
			{
				using (Pen pen2 = new Pen(accent, num))
				{
					pen2.StartCap = LineCap.Round;
					pen2.EndCap = LineCap.Round;
					e.Graphics.DrawArc(pen2, rect, -90f, num2);
				}
			}
			string s = score.ToString();
			using (Font font = Ui.FontTitle(22f, true))
			{
				StringFormat stringFormat = new StringFormat();
				stringFormat.Alignment = StringAlignment.Center;
				stringFormat.LineAlignment = StringAlignment.Center;
				using (StringFormat format = stringFormat)
				{
					using (SolidBrush brush = new SolidBrush(Ui.TextPrimary))
					{
						Rectangle rectangle = new Rectangle(0, 0, base.Width, base.Height - 6);
						e.Graphics.DrawString(s, font, brush, rectangle, format);
					}
				}
			}
			using (Font font2 = Ui.FontBody(7.5f))
			{
				StringFormat stringFormat2 = new StringFormat();
				stringFormat2.Alignment = StringAlignment.Center;
				using (StringFormat format2 = stringFormat2)
				{
					using (SolidBrush brush2 = new SolidBrush(Ui.TextMuted))
					{
						e.Graphics.DrawString("ОЦЕНКА", font2, brush2, new Rectangle(0, base.Height - 30, base.Width, 16), format2);
					}
				}
			}
		}
	}
}
