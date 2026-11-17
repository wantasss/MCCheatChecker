using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkMenuRenderer : ToolStripProfessionalRenderer
	{
		public DarkMenuRenderer()
			: base(new DarkColorTable())
		{
			base.RoundedEdges = false;
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			e.TextColor = ((!e.Item.Enabled) ? Ui.TextMuted : (e.Item.Selected ? Ui.TextPrimary : Ui.TextSecondary));
			base.OnRenderItemText(e);
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			using (SolidBrush brush = new SolidBrush(Ui.Surface))
			{
				e.Graphics.FillRectangle(brush, e.Item.Bounds);
			}
			using (Pen pen = new Pen(Ui.Border))
			{
				e.Graphics.DrawLine(pen, 8, e.Item.Bounds.Height / 2, e.Item.Width - 8, e.Item.Bounds.Height / 2);
			}
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			e.ArrowColor = (e.Item.Selected ? Ui.AccentTeal : Ui.TextSecondary);
			base.OnRenderArrow(e);
		}
	}
}
