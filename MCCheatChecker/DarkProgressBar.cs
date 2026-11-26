using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkProgressBar : Control
	{
		private int value;

		private int maximum = 100;

		public int Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = Math.Max(0, Math.Min(maximum, value));
				Invalidate();
			}
		}

		public int Maximum
		{
			get
			{
				return maximum;
			}
			set
			{
				maximum = Math.Max(1, value);
				if (this.value > maximum)
				{
					this.value = maximum;
				}
				Invalidate();
			}
		}

		public DarkProgressBar()
		{
			DoubleBuffered = true;
			base.Height = 8;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle bounds = new Rectangle(0, 0, base.Width, base.Height);
			using (GraphicsPath path = Ui.RoundedRect(bounds, base.Height / 2))
			{
				using (SolidBrush brush = new SolidBrush(Ui.SurfaceAlt))
				{
					e.Graphics.FillPath(brush, path);
				}
			}
			int num = (int)((float)base.Width * (float)value / (float)maximum);
			if (num <= 1)
			{
				return;
			}
			using (GraphicsPath path2 = Ui.RoundedRect(new Rectangle(0, 0, num, base.Height), base.Height / 2))
			{
				using (SolidBrush brush2 = new SolidBrush(Ui.AccentTeal))
				{
					e.Graphics.FillPath(brush2, path2);
				}
			}
		}
	}
}
