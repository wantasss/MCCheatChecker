using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkSplitContainer : SplitContainer
	{
		public DarkSplitContainer()
		{
			BackColor = Ui.Border;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Rectangle splitterRectangle = base.SplitterRectangle;
			if (splitterRectangle.Width <= 0 || splitterRectangle.Height <= 0)
			{
				return;
			}
			using (SolidBrush brush = new SolidBrush(Ui.Border))
			{
				e.Graphics.FillRectangle(brush, splitterRectangle);
			}
		}
	}
}
