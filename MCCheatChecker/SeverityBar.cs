using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class SeverityBar : Control
	{
		private int critical;

		private int needsCheck;

		private int warning;

		private int info;

		public SeverityBar()
		{
			DoubleBuffered = true;
			base.Height = 10;
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Ui.Surface;
		}

		public void SetCounts(int criticalCount, int needsCount, int warningCount, int infoCount)
		{
			critical = criticalCount;
			needsCheck = needsCount;
			warning = warningCount;
			info = infoCount;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = critical + needsCheck + warning + info;
			Rectangle bounds = new Rectangle(0, 0, base.Width, base.Height);
			using (GraphicsPath path = Ui.RoundedRect(bounds, base.Height / 2))
			{
				using (SolidBrush brush = new SolidBrush(Ui.SurfaceAlt))
				{
					e.Graphics.FillPath(brush, path);
				}
			}
			if (num <= 0)
			{
				return;
			}
			float num2 = 0f;
			Color[] array = new Color[4]
			{
				Ui.AccentRed,
				Ui.AccentPurple,
				Ui.AccentOrange,
				Ui.AccentBlue
			};
			int[] array2 = new int[4] { critical, needsCheck, warning, info };
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i] > 0)
				{
					float num3 = (float)base.Width * (float)array2[i] / (float)num;
					using (SolidBrush brush2 = new SolidBrush(array[i]))
					{
						e.Graphics.FillRectangle(brush2, num2, 0f, Math.Max(2f, num3), base.Height);
					}
					num2 += num3;
				}
			}
		}
	}
}
