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
		public struct COPYDATASTRUCT
		{
			public IntPtr dwData;
			public int cbData;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpData;
		}

		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		public const Int32 WM_COPYDATA = 0x4A;
		public const int HWND_BROADCAST = 0xffff;
		public static readonly int WM_SHOWBIVROSTPLAYER = RegisterWindowMessage("WM_SHOWBIVROSTPLAYER");
		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
		[DllImport("user32")]
		public static extern int RegisterWindowMessage(string message);
		[DllImport("User32.dll", EntryPoint = "FindWindow")]
		public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("WmCpyDta.dll", EntryPoint = "WmCpyDta_MaxTagLen")]
		public static extern int WmCpyDta_MaxTagLen();

		[DllImport("WmCpyDta.dll", EntryPoint = "WmCpyDta_MaxDataLen")]
		public static extern int WmCpyDta_MaxDataLen();

		[DllImport("WmCpyDta.dll", EntryPoint = "WmCpyDta_GetMessage_sTagData")]
		public static extern bool WmCpyDta_GetMessage_sTagData(int hReceiver, int hSender, int lParam, StringBuilder lpTag, StringBuilder lpData);
	}
}
