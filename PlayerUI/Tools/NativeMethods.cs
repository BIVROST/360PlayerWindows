using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Tools
{
	class NativeMethods
	{
		public const int HWND_BROADCAST = 0xffff;
		public static readonly int WM_SHOWBIVROSTPLAYER = RegisterWindowMessage("WM_SHOWBIVROSTPLAYER");
		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
		[DllImport("user32")]
		public static extern int RegisterWindowMessage(string message);
	}
}
