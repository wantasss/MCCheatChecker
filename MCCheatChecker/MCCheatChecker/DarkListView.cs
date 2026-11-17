using System;
using System.Drawing;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class DarkListView : ListView
	{
		private const int WM_ERASEBKGND = 20;

		private bool rightAlignLastColumn;

		private Color itemBackColor = Color.Empty;

		public Color ItemBackColor
		{
			get
			{
				return itemBackColor;
			}
			set
			{
				itemBackColor = value;
				Invalidate();
			}
		}

		public bool RightAlignLastColumn
		{
			get
			{
				return rightAlignLastColumn;
			}
			set
			{
				rightAlignLastColumn = value;
				Invalidate();
			}
		}

		public DarkListView()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			base.OwnerDraw = true;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			base.BorderStyle = BorderStyle.None;
			DrawListViewColumnHeaderEventHandler value = delegate(object s, DrawListViewColumnHeaderEventArgs e)
			{
				using (SolidBrush brush = new SolidBrush(Ui.SurfaceAlt))
				{
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
				using (SolidBrush brush2 = new SolidBrush(Ui.Border))
				{
					e.Graphics.FillRectangle(brush2, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Width, 1);
				}
				string text = ((e.ColumnIndex >= 0 && e.ColumnIndex < base.Columns.Count) ? base.Columns[e.ColumnIndex].Text : string.Empty);
				TextRenderer.DrawText(e.Graphics, text, Font, e.Bounds, Ui.TextSecondary, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			};
			base.DrawColumnHeader += value;
			base.DrawItem += delegate
			{
			};
			base.DrawSubItem += delegate(object s, DrawListViewSubItemEventArgs e)
			{
				Color color = (e.Item.Selected ? Ui.SurfaceAlt : ((!itemBackColor.IsEmpty) ? itemBackColor : (e.Item.BackColor.IsEmpty ? BackColor : e.Item.BackColor)));
				using (SolidBrush brush = new SolidBrush(color))
				{
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
				if (e.Item.Selected)
				{
					using (SolidBrush brush2 = new SolidBrush(Ui.AccentTeal))
					{
						e.Graphics.FillRectangle(brush2, e.Bounds.X, e.Bounds.Y + 2, 2, e.Bounds.Height - 4);
					}
				}
				TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
				if (rightAlignLastColumn && e.ColumnIndex == base.Columns.Count - 1)
				{
					flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.VerticalCenter;
				}
				Color foreColor = (e.Item.Selected ? Ui.TextPrimary : e.Item.ForeColor);
				TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), foreColor, flags);
			};
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Ui.ApplyDarkScrollbars(this);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 20)
			{
				Graphics graphics = Graphics.FromHdc(m.WParam);
				using (SolidBrush brush = new SolidBrush(BackColor))
				{
					graphics.FillRectangle(brush, base.ClientRectangle);
				}
				graphics.Dispose();
			}
			else
			{
				base.WndProc(ref m);
			}
		}
	}
}
