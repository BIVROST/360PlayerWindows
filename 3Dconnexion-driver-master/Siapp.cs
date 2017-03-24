///////////////////////////////////////////////////////////////////////////////////
// Copyright notice:
//   Copyright (c) 3Dconnexion. All rights reserved. 
// 
// This file and source code are an integral part of the "3Dconnexion
// Software Development Kit", including all accompanying documentation,
// and is protected by intellectual property laws. All use of the
// 3Dconnexion Software Development Kit is subject to the License
// Agreement found in the "LicenseAgreementSDK.txt" file.
// All rights not expressly granted by 3Dconnexion are reserved.
//

using System;
using System.Runtime.InteropServices;

namespace _3Dconnexion
{
    class SiApp
    {
        const string SI_DLL = "siappdll";

        #region Enums

        public enum SpwRetVal
        {
            SPW_NO_ERROR,
            SPW_ERROR,
            SI_BAD_HANDLE,
            SI_BAD_ID,
            SI_BAD_VALUE,
            SI_IS_EVENT,
            SI_SKIP_EVENT,
            SI_NOT_EVENT,
            SI_NO_DRIVER,
            SI_NO_RESPONSE,
            SI_UNSUPPORTED,
            SI_UNINITIALIZED,
            SI_WRONG_DRIVER,
            SI_INTERNAL_ERROR,
            SI_BAD_PROTOCOL,
            SI_OUT_OF_MEMORY,
            SPW_DLL_LOAD_ERROR,
            SI_NOT_OPEN,
            SI_ITEM_NOT_FOUND,
            SI_UNSUPPORTED_DEVICE
        }

        public enum SiEventType : int
        {
            SI_BUTTON_EVENT = 1,
            SI_MOTION_EVENT,
            SI_COMBO_EVENT,
            SI_ZERO_EVENT,
            SI_EXCEPTION_EVENT,
            SI_OUT_OF_BAND,
            SI_ORIENTATION_EVENT,
            SI_KEYBOARD_EVENT,
            SI_LPFK_EVENT,
            SI_APP_EVENT,
            SI_SYNC_EVENT,
            SI_BUTTON_PRESS_EVENT,
            SI_BUTTON_RELEASE_EVENT,
            SI_DEVICE_CHANGE_EVENT,
            SI_MOUSE_EVENT,
            SI_JOYSTICK_EVENT
        }

        public enum SiDevType
        {
            SI_UNKNOWN_DEVICE = 0,
            SI_SPACEBALL_2003 = 1,
            SI_SPACEBALL_3003 = 2,
            SI_SPACE_CONTROLLER = 3,
            SI_SPACEEXPLORER = 4,
            SI_SPACENAVIGATOR_FOR_NOTEBOOKS = 5,
            SI_SPACENAVIGATOR = 6,
            SI_SPACEBALL_2003A = 7,
            SI_SPACEBALL_2003B = 8,
            SI_SPACEBALL_2003C = 9,
            SI_SPACEBALL_3003A = 10,
            SI_SPACEBALL_3003B = 11,
            SI_SPACEBALL_3003C = 12,
            SI_SPACEBALL_4000 = 13,
            SI_SPACEMOUSE_CLASSIC = 14,
            SI_SPACEMOUSE_PLUS = 15,
            SI_SPACEMOUSE_XT = 16,
            SI_CYBERMAN = 17,
            SI_CADMAN = 18,
            SI_SPACEMOUSE_CLASSIC_PROMO = 19,
            SI_SERIAL_CADMAN = 20,
            SI_SPACEBALL_5000 = 21,
            SI_TEST_NO_DEVICE = 22,
            SI_3DX_KEYBOARD_BLACK = 23,
            SI_3DX_KEYBOARD_WHITE = 24,
            SI_TRAVELER = 25,
            SI_TRAVELER1 = 26,
            SI_SPACEBALL_5000A = 27,
            SI_SPACEDRAGON = 28,
            SI_SPACEPILOT = 29,
            SI_MB = 30,
            SI_SPACEPILOT_PRO = 0xc629,
            SI_SPACEMOUSE_PRO = 0xc62b,
            SI_SPACEMOUSE_TOUCH = 0xc62c,
            SI_SPACEMOUSE_WIRELESS = 0xc62e
        }

        public enum SiDeviceChangeType
        {
            SI_DEVICE_CHANGE_CONNECT = 0,
            SI_DEVICE_CHANGE_DISCONNECT = 1
        }

        public enum SiOrientation
        {
            SI_LEFT = 0,
            SI_RIGHT
        }

        #endregion 

        #region Const Variables

        private const int SI_STRSIZE = 128;
        private const int MAX_PATH = 260;
        private const int SI_MAXPATH = 512;
        private const int SI_MAXPORTNAME = 260;
        private const int SI_MAXBUF = 128;
        private const int SI_KEY_MAXBUF = 5120;

        public const int SI_ANY_DEVICE = -1;
        public const int SI_NO_BUTTON = -1;
        public const int SI_EVENT = 0x0001;
        public const int SI_AVERAGE_EVENTS = 1;

		public const UInt32 SI_UI_ALL_CONTROLS = 0xffffffff;
		public const UInt32 SI_UI_NO_CONTROLS = 0x00000000;

		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
        public struct SiDevInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_STRSIZE)]
            public string firmware;
            public int devType;
            public int numButtons;
            public int numDegrees;
            public bool canBeep;
            public int majorVersion;
            public int minorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiDevPort
        {
            public int devID;
            public int devType;
            public int devClass;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_STRSIZE)]
            public string devName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_MAXPORTNAME)]
            public string portName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiDeviceName
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_STRSIZE)]
            public String name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiButtonName
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_STRSIZE)]
            public String name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiPortName
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_MAXPATH)]
            public string name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiOpenData
        {
            public int hWnd;
            public IntPtr transCtl;
            public int processID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public String exeFile;
            public int libFlag;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiTypeMask
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public byte[] mask;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiGetEventData
        {
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiButtonData
        {
            public uint last;
            public uint current;
            public uint pressed;
            public uint released;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiSpwData
        {
            public SiButtonData bData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public int[] mData;
            public int period;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SI_MAXBUF)]
            public byte[] exData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiSpwOOB
        {
            public byte code;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_MAXBUF - 1)]
            public byte[] message;
        }

		[StructLayout(LayoutKind.Explicit, Size = 4)]
		public struct SiHWButtonData
		{
			[FieldOffset(0)]
			public uint buttonNumber;
		}

        [StructLayout(LayoutKind.Sequential)]
        public struct SiOrientation1
        {
            public int orientation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiKeyboardData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_KEY_MAXBUF)]
            public byte[] kstring;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiDeviceChangeEventData
        {
            public int type;
            public int devID;
            public SiPortName portName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiSpwEvent_JustType
        {
            public SiEventType type; // int
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiSpwEvent_SpwData
        {
            public SiEventType type; // int
			public SiSpwData spwData;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SiSpwEvent_HWButtonData
		{
			public SiEventType type; // int
			public SiHWButtonData hwButtonEvent;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SiSpwEvent_SpwUnion
		{
			[FieldOffset(0)]
			public SiSpwData spwData;
			[FieldOffset(0)]
			public SiHWButtonData hwButtonEvent;
		}

        [StructLayout(LayoutKind.Sequential)]
        public struct SiSpwEvent_DeviceChange
        {
            public SiEventType type; // int
            public SiDeviceChangeEventData deviceChangeEventData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SiDeviceIconPath
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SI_STRSIZE)]
            public String path;
        }

        #endregion

        #region DLL Imports

        [DllImport(SI_DLL, EntryPoint = "SiGetButtonName")]
        private static extern SpwRetVal pfnSiGetButtonName(IntPtr hdl, uint buttonNumber, ref SiButtonName name);
        [DllImport(SI_DLL, EntryPoint = "SiGetDeviceName")]
        private static extern SpwRetVal pfnSiGetDeviceName(IntPtr hdl, ref SiDeviceName name);

        [DllImport(SI_DLL, EntryPoint = "SiInitialize")]
        public static extern SpwRetVal SiInitialize();
        [DllImport(SI_DLL, EntryPoint = "SiTerminate")]
        public static extern int SiTerminate();
        [DllImport(SI_DLL, EntryPoint = "SiClose")]
        public static extern SpwRetVal SiClose(IntPtr hdl);
        [DllImport(SI_DLL, EntryPoint = "SiOpenWinInit")]
        public static extern int SiOpenWinInit(ref SiOpenData o, IntPtr hwnd);
        [DllImport(SI_DLL, EntryPoint = "SiOpen", CharSet = CharSet.Ansi)]
        public static extern IntPtr SiOpen(string pAppName, int devID, IntPtr pTMask, int mode, ref SiOpenData pData);
        [DllImport(SI_DLL, EntryPoint = "SiOpenPort", CharSet = CharSet.Ansi)]
        public static extern IntPtr SiOpenPort(string pAppName, ref SiDevPort pPort, int mode, ref SiOpenData pData);
        [DllImport(SI_DLL, EntryPoint = "SiGetDeviceInfo")]
        public static extern SpwRetVal SiGetDeviceInfo(IntPtr hdl, ref SiDevInfo pInfo);
        [DllImport(SI_DLL, EntryPoint = "SiGetDevicePort")]
        public static extern SpwRetVal SiGetDevicePort(IntPtr hdl, ref SiDevPort pPort);
        [DllImport(SI_DLL, EntryPoint = "SiGetEventWinInit")]
        public static extern void SiGetEventWinInit(ref SiGetEventData pData, int msg, IntPtr wParam, IntPtr lParam);



		[DllImport(SI_DLL, EntryPoint = "SiGetEvent")]
        public static extern SpwRetVal SiGetEvent(IntPtr hdl, int flags, ref SiGetEventData pData, ref SiSpwEvent_SpwData pEvent);

		[DllImport(SI_DLL, EntryPoint = "SiGetEvent")]
		public static extern SpwRetVal SiGetEvent(IntPtr hdl, int flags, ref SiGetEventData pData, ref SiSpwEvent_HWButtonData pEvent);



		[DllImport(SI_DLL, EntryPoint = "SiButtonPressed")]
        public static extern int SiButtonPressed(ref SiSpwEvent_SpwData pEvent);
		[DllImport(SI_DLL, EntryPoint = "SiButtonPressed")]
		public static extern int SiButtonPressed(ref SiSpwEvent_HWButtonData pEvent);


		[DllImport(SI_DLL, EntryPoint = "SiButtonReleased")]
		public static extern int SiButtonReleased(ref SiSpwEvent_SpwData pEvent);

		[DllImport(SI_DLL, EntryPoint = "SiRezero")]
        public static extern SpwRetVal SiRezero(IntPtr hdl);
        [DllImport(SI_DLL, EntryPoint = "SiSetLEDs")]
        public static extern SpwRetVal SiSetLEDs(IntPtr hdl, int mask);
        [DllImport(SI_DLL, EntryPoint = "SiGetNumDevices")]
        public static extern int SiGetNumDevices();
        [DllImport(SI_DLL, EntryPoint = "SiGetDeviceID")]
        public static extern int SiGetDeviceID(IntPtr hdl);
        [DllImport(SI_DLL, EntryPoint = "SiGetDeviceImageFileName")]
        private static extern SpwRetVal pfnSiGetDeviceImageFileName(IntPtr hdl, ref SiDeviceIconPath path, ref int len);

		[DllImport(SI_DLL, EntryPoint = "SiSetUiMode")]
		public static extern SpwRetVal SiSetUiMode(IntPtr hdl, UInt32 mode);

		[DllImport(SI_DLL, EntryPoint = "SiBeep")]
		public static extern SpwRetVal SiBeep(IntPtr hdl, [MarshalAs(UnmanagedType.LPStr)] string note);

		#endregion

		#region DLL Imports Sync Functions
		[DllImport(SI_DLL, EntryPoint = "SiSyncSendQuery")]
		public static extern SpwRetVal SiSyncSendQuery(IntPtr hdl);

		[DllImport(SI_DLL, EntryPoint = "SiSyncSetButtonAssignment")]
		public static extern SpwRetVal SiSyncSetButtonAssignment(IntPtr hdl, UInt32 buttonNumber, UInt32 assignedFunctionIndex);



		#endregion


		#region Functions

		public static SpwRetVal SiGetDeviceImageFileName(IntPtr hdl, out string path)
        {
            SpwRetVal tmpRetVal;
            SiDeviceIconPath devicePathStruct = new SiDeviceIconPath();
            int len = SI_STRSIZE;
            tmpRetVal = pfnSiGetDeviceImageFileName(hdl, ref devicePathStruct, ref len);
            path = devicePathStruct.path;
            return tmpRetVal;
        }

        public static SpwRetVal SiGetDeviceName(IntPtr hdl, out string name)
        {
            SpwRetVal tmpRetVal;
            SiDeviceName deviceNameStruct = new SiDeviceName();
            tmpRetVal = pfnSiGetDeviceName(hdl, ref deviceNameStruct);
            name = deviceNameStruct.name;
            return tmpRetVal;
        }

        public static SpwRetVal SiGetButtonName(IntPtr hdl, uint buttonNumber, out string name)
        {
            SpwRetVal tmpRetVal;
            SiButtonName buttonNameStruct = new SiButtonName();
            tmpRetVal = pfnSiGetButtonName(hdl, buttonNumber, ref buttonNameStruct);
            name = buttonNameStruct.name;
            return tmpRetVal;
        }

        #endregion
    }
}
