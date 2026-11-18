using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkColorTable : ProfessionalColorTable
	{
		public override Color MenuItemSelected
		{
			get
			{
				return Ui.SurfaceAlt;
			}
		}

		public override Color MenuItemSelectedGradientBegin
		{
			get
			{
				return Ui.SurfaceAlt;
			}
		}

		public override Color MenuItemSelectedGradientEnd
		{
			get
			{
				return Ui.SurfaceAlt;
			}
		}

		public override Color MenuItemBorder
		{
			get
			{
				return Ui.AccentTeal;
			}
		}

		public override Color MenuBorder
		{
			get
			{
				return Ui.Border;
			}
		}

		public override Color MenuItemPressedGradientBegin
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color MenuItemPressedGradientEnd
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color MenuStripGradientBegin
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color MenuStripGradientEnd
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color ToolStripDropDownBackground
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color ImageMarginGradientBegin
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color ImageMarginGradientMiddle
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color ImageMarginGradientEnd
		{
			get
			{
				return Ui.Surface;
			}
		}

		public override Color SeparatorDark
		{
			get
			{
				return Ui.Border;
			}
		}

		public override Color SeparatorLight
		{
			get
			{
				return Ui.Border;
			}
		}

		public override Color StatusStripGradientBegin
		{
			get
			{
				return Ui.Sidebar;
			}
		}

		public override Color StatusStripGradientEnd
		{
			get
			{
				return Ui.Sidebar;
			}
		}
	}
}
