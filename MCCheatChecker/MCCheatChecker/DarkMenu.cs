using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkMenu : ContextMenuStrip
	{
		public DarkMenu()
		{
			base.RenderMode = ToolStripRenderMode.Professional;
			base.Renderer = new DarkMenuRenderer();
			base.BackColor = Ui.Surface;
			base.ForeColor = Ui.TextSecondary;
			Font = Ui.FontBody(9f);
			base.Padding = new Padding(2, 4, 2, 4);
		}
	}
}
