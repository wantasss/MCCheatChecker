using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkTabView : Panel
	{
		private readonly Panel header;

		private readonly Panel body;

		private readonly List<DarkTabHeader> headers = new List<DarkTabHeader>();

		private readonly List<Control> pages = new List<Control>();

		private int selectedIndex = -1;

		public int SelectedIndex
		{
			get
			{
				return selectedIndex;
			}
			set
			{
				if (value < 0 || value >= pages.Count)
				{
					return;
				}
				bool flag = selectedIndex != value;
				selectedIndex = value;
				for (int i = 0; i < pages.Count; i++)
				{
					pages[i].Visible = i == selectedIndex;
					headers[i].Active = i == selectedIndex;
				}
				if (pages[selectedIndex] != null)
				{
					pages[selectedIndex].BringToFront();
				}
				if (!flag || this.SelectedIndexChanged == null)
				{
					return;
				}
				try
				{
					this.SelectedIndexChanged(this, EventArgs.Empty);
				}
				catch
				{
				}
			}
		}

		public event EventHandler SelectedIndexChanged;

		public DarkTabView()
		{
			BackColor = Ui.Backdrop;
			header = new Panel();
			header.Dock = DockStyle.Top;
			header.Height = 30;
			header.BackColor = Ui.Backdrop;
			body = new Panel();
			body.Dock = DockStyle.Fill;
			body.BackColor = Ui.Backdrop;
			base.Controls.Add(body);
			base.Controls.Add(header);
		}

		public void AddPage(string title, Control content)
		{
			content.Dock = DockStyle.Fill;
			content.Visible = false;
			content.BackColor = Ui.Backdrop;
			body.Controls.Add(content);
			DarkTabHeader darkTabHeader = new DarkTabHeader();
			darkTabHeader.Text = title;
			darkTabHeader.Font = Font;
			darkTabHeader.Width = MeasureTabWidth(title);
			darkTabHeader.Click += delegate(object s, EventArgs e)
			{
				SelectedIndex = headers.IndexOf((DarkTabHeader)s);
			};
			headers.Add(darkTabHeader);
			pages.Add(content);
			header.Controls.Add(darkTabHeader);
			LayoutHeaders();
			if (selectedIndex < 0)
			{
				SelectedIndex = 0;
			}
		}

		private int MeasureTabWidth(string title)
		{
			using (Graphics dc = CreateGraphics())
			{
				return Math.Max(120, (int)((SizeF)TextRenderer.MeasureText(dc, title, Font)).Width + 34);
			}
		}

		private void LayoutHeaders()
		{
			int num = 0;
			for (int i = 0; i < headers.Count; i++)
			{
				headers[i].Location = new Point(num, 0);
				num += headers[i].Width;
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			for (int i = 0; i < headers.Count; i++)
			{
				headers[i].Font = Font;
				headers[i].Width = MeasureTabWidth(headers[i].Text);
			}
			LayoutHeaders();
		}
	}
}
