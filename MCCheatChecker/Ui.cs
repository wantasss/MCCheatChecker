using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public static class Ui
	{
		private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

		private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

		public static readonly Color Backdrop = Color.FromArgb(15, 17, 21);

		public static readonly Color Surface = Color.FromArgb(23, 26, 31);

		public static readonly Color SurfaceAlt = Color.FromArgb(28, 32, 38);

		public static readonly Color Border = Color.FromArgb(38, 43, 51);

		public static readonly Color Sidebar = Color.FromArgb(18, 20, 24);

		public static readonly Color TextPrimary = Color.FromArgb(236, 240, 245);

		public static readonly Color TextSecondary = Color.FromArgb(140, 150, 163);

		public static readonly Color TextMuted = Color.FromArgb(96, 105, 117);

		public static readonly Color AccentTeal = Color.FromArgb(46, 230, 214);

		public static readonly Color AccentRed = Color.FromArgb(239, 83, 80);

		public static readonly Color AccentOrange = Color.FromArgb(255, 167, 38);

		public static readonly Color AccentPurple = Color.FromArgb(171, 71, 188);

		public static readonly Color AccentBlue = Color.FromArgb(79, 195, 247);

		public static readonly Color AccentGreen = Color.FromArgb(102, 187, 106);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

		public static void ApplyDarkScrollbars(Control control)
		{
			if (control == null || !control.IsHandleCreated)
			{
				return;
			}
			try
			{
				SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
			}
			catch
			{
			}
		}

		public static Font FontTitle(float size, bool bold)
		{
			return new Font("Segoe UI Semibold", size, bold ? FontStyle.Bold : FontStyle.Regular);
		}

		public static Font FontBody(float size)
		{
			return new Font("Segoe UI", size, FontStyle.Regular);
		}

		public static Font FontBody(float size, bool bold)
		{
			return new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
		}

		public static Font FontPx(string family, float pixels, bool bold)
		{
			return new Font(family, pixels, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
		}

		public static Font FontMono(float size)
		{
			return new Font("Consolas", size, FontStyle.Regular);
		}

		public static Font FontMono(float size, bool bold)
		{
			return new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular);
		}

		public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
		{
			GraphicsPath graphicsPath = new GraphicsPath();
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return graphicsPath;
			}
			int maxRadius = Math.Min(bounds.Width, bounds.Height) / 2;
			if (radius > maxRadius)
			{
				radius = maxRadius;
			}
			int num = radius * 2;
			if (num <= 0)
			{
				graphicsPath.AddRectangle(bounds);
				return graphicsPath;
			}
			graphicsPath.AddArc(bounds.X, bounds.Y, num, num, 180f, 90f);
			graphicsPath.AddArc(bounds.Right - num, bounds.Y, num, num, 270f, 90f);
			graphicsPath.AddArc(bounds.Right - num, bounds.Bottom - num, num, num, 0f, 90f);
			graphicsPath.AddArc(bounds.X, bounds.Bottom - num, num, num, 90f, 90f);
			graphicsPath.CloseFigure();
			return graphicsPath;
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

		public static void ApplyDarkTitleBar(Form form)
		{
			if (form == null || !form.IsHandleCreated)
			{
				return;
			}
			int value = 1;
			if (DwmSetWindowAttribute(form.Handle, 20, ref value, 4) == 0)
			{
				return;
			}
			try
			{
				DwmSetWindowAttribute(form.Handle, 19, ref value, 4);
			}
			catch
			{
			}
		}
	}
}
