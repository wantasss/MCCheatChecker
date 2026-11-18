using System;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkTextBoxMultiline : TextBox
	{
		public DarkTextBoxMultiline()
		{
			Multiline = true;
			base.ReadOnly = true;
			base.ScrollBars = ScrollBars.Both;
			base.BorderStyle = BorderStyle.None;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontMono(9f);
			base.WordWrap = false;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Ui.ApplyDarkScrollbars(this);
		}
	}
}
