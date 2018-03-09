using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.WPF
{
	[System.Security.SuppressUnmanagedCodeSecurity()]
	public class NativeApiWrapper
	{

		[PreserveSig]
		[DllImport("dwmapi.dll", EntryPoint = "#101")]
		public static extern int UpdateWindowShared(IntPtr hWnd, int one, int two, int three, IntPtr hMonitor, IntPtr unknown);

		[PreserveSig]
		[DllImport("dwmapi.dll", EntryPoint = "#100")]
		public static extern int GetSharedSurface(IntPtr hWnd, Int64 adapterLuid, uint one, uint two, [In, Out]ref uint pD3DFormat, [Out]out IntPtr pSharedHandle, UInt64 unknown);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT(int left, int top, int right, int bottom)
		{
			this.Left = left;
			this.Top = top;
			this.Right = right;
			this.Bottom = bottom;
		}

		public Rectangle AsRectangle
		{
			get
			{
				return new Rectangle(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
			}
		}

		public static RECT FromXYWH(int x, int y, int width, int height)
		{
			return new RECT(x, y, x + width, y + height);
		}

		public static RECT FromRectangle(Rectangle rect)
		{
			return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
		}
	}
}
