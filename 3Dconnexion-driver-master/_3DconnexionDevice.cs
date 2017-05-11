using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _3Dconnexion;
using System.Threading;
using System.Runtime.InteropServices;

namespace _3DconnexionDriver
{
    public class _3DconnexionDevice : IDisposable
    {
        IntPtr _deviceHandle = IntPtr.Zero;
        int _leds = -1;
		UInt32 _uiMode = SiApp.SI_UI_NO_CONTROLS;
		
        /// <summary>
        /// Event when Device is at 0 Point
        /// </summary>
        public event EventHandler ZeroPoint;
        
        /// <summary>
        /// Event when Device is moved
        /// </summary>
        public event EventHandler<MotionEventArgs> Motion;

		/// <summary>
		/// Event when button is pressed
		/// </summary>
		public event EventHandler<ButtonEventArgs> ButtonPressed;

		/// <summary>
		/// Event when button is released
		/// </summary>
		public event EventHandler<ButtonEventArgs> ButtonReleased;

		/// <summary>
		/// Event when Device changes. Doesn't work yet
		/// </summary>
		//public event EventHandler<DeviceChangeEventArgs> DeviceChange;

		/// <summary>
		/// Dispatching Thread
		/// </summary>
		private readonly System.Threading.Thread eventThread;
        
        /// <summary>
        /// Buffer for Events
        /// </summary>
        private readonly Dictionary<SiApp.SiEventType, EventArgs> eventBuffer = new Dictionary<SiApp.SiEventType, EventArgs>();

        public _3DconnexionDevice(string appName)
        {
            this.AppName = appName;
            eventThread = new System.Threading.Thread(EventThreadLoop);
            eventThread.IsBackground = true;
            eventThread.Name = "3Dconnexion-Event-Dispatcher";
            eventThread.Start();
        }

        ~_3DconnexionDevice()
        {
            Dispose();
        }

        /// <summary>
        /// Device is available (initialized)
        /// </summary>
        public bool IsAvailable
        {
            get { return _deviceHandle != IntPtr.Zero; }
        }

        /// <summary>
        /// Device is disposed
        /// </summary>
        public bool IsDisposed
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the app using this device
        /// </summary>
        public string AppName
        {
            get; 
            private set;
        }

        /// <summary>
        /// The Name of the Device (e.g. Space Pilot)
        /// </summary>
        public string DeviceName
        {
            get;
            private set;
        }

        /// <summary>
        /// The DeviceID of the device
        /// </summary>
        public int DeviceID
        {
            get;
            private set;
        }

        /// <summary>
        /// The Firmware version of the device
        /// </summary>
        public string FirmwareVersion
        {
            get;
            private set;
        }

		public int NumButtons
		{
			get;
			private set;
		}

        /// <summary>
        /// Bitwise Flags for the LEDs of the Device
        /// </summary>
        public int LEDs
        {
            get { return _leds; }
            set
            {
                _leds = value;
                if (IsAvailable)
                {
                    SiApp.SiSetLEDs(_deviceHandle, _leds);
                }
            }
        }

		public bool UiMode
		{
			get
			{
				return _uiMode == SiApp.SI_UI_ALL_CONTROLS ? true : false;
			}
			set
			{
				_uiMode = value ? SiApp.SI_UI_ALL_CONTROLS : SiApp.SI_UI_NO_CONTROLS; 
				SiApp.SiSetUiMode(_deviceHandle, _uiMode);
			}
		}

        /// <summary>
        /// Path to an icon for that device, provided by the 3Dx Ware Driver
        /// </summary>
        public string IconPath
        {
            get
            {
                if (IsAvailable)
                {
                    string path;
                    var ret = SiApp.SiGetDeviceImageFileName(_deviceHandle, out path);
                    return path;
                }
                return null;
            }
        }

        /// <summary>
        /// Initialize the Device
        /// </summary>
        /// <param name="windowHandle">Handle to window using the device</param>
        public void InitDevice(IntPtr windowHandle)
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException("");
            if (IsAvailable)
                return; //Init already done.

            var v = SiApp.SiInitialize();
            if (v == SiApp.SpwRetVal.SPW_DLL_LOAD_ERROR)
                throw new _3DxException("Unable to load SiApp DLL");

            SiApp.SiOpenData o = default(SiApp.SiOpenData);
            SiApp.SiOpenWinInit(ref o, windowHandle);

            _deviceHandle = SiApp.SiOpen(this.AppName, SiApp.SI_ANY_DEVICE, IntPtr.Zero, SiApp.SI_EVENT, ref o);
            if (_deviceHandle == IntPtr.Zero)
            {
                SiApp.SiTerminate();
                throw new _3DxException("Unable to open device");
            }
            
            string devName;
            SiApp.SiGetDeviceName(_deviceHandle, out devName);

            this.DeviceName = devName;

            this.DeviceID = SiApp.SiGetDeviceID(_deviceHandle);

            SiApp.SiDevInfo info = default(SiApp.SiDevInfo);
            SiApp.SiGetDeviceInfo(_deviceHandle, ref info);

            this.FirmwareVersion = info.firmware;
			this.NumButtons = info.numButtons;
        }


		public void ResetAllButtonBindings()
		{
			if (this.IsDisposed)
				throw new ObjectDisposedException("");

			SiApp.SiSyncSendQuery(_deviceHandle);
			for(uint it = 1; it<=NumButtons; it++)
				SiApp.SiSyncSetButtonAssignment(_deviceHandle, it, 0);
		}

		public void Beep(string tone)
		{
			var retVal = SiApp.SiBeep(_deviceHandle, tone);
		}

        /// <summary>
        /// Close the Device
        /// </summary>
        public void CloseDevice()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                SiApp.SiClose(_deviceHandle);
                _deviceHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Called in WndPrc to process the Window Messages
        /// </summary>
        /// <param name="msg">Message Number</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        public void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (this.IsDisposed) //We don't throw an Exception in the Message Loop, just return
                return;

            SiApp.SiGetEventData evd = default(SiApp.SiGetEventData);
            SiApp.SiGetEventWinInit(ref evd, msg, wParam, lParam);
            SiApp.SiSpwEvent_SpwData edata = default(SiApp.SiSpwEvent_SpwData);
            var t = SiApp.SiGetEvent(_deviceHandle, SiApp.SI_AVERAGE_EVENTS, ref evd, ref edata);
            if (t == SiApp.SpwRetVal.SI_IS_EVENT)
            {
                switch (edata.type)
                {
                    case SiApp.SiEventType.SI_ZERO_EVENT:
                        lock (eventBuffer)
                        {
                            eventBuffer[SiApp.SiEventType.SI_ZERO_EVENT] = EventArgs.Empty;
                            Monitor.Pulse(eventBuffer);
                        }
                        break;
                    case SiApp.SiEventType.SI_MOTION_EVENT:
                        lock (eventBuffer)
                        {
                            eventBuffer[SiApp.SiEventType.SI_MOTION_EVENT] = MotionEventArgs.FromEventArray(edata.spwData.mData);
                            Monitor.Pulse(eventBuffer);
                        }
                        break;
					case SiApp.SiEventType.SI_BUTTON_PRESS_EVENT:
						lock (eventBuffer)
						{
							eventBuffer[SiApp.SiEventType.SI_BUTTON_PRESS_EVENT] = ButtonEventArgs.FromKeyCode((int)edata.spwData.bData.last, true);
							Monitor.Pulse(eventBuffer);
						}
						break;
					case SiApp.SiEventType.SI_BUTTON_RELEASE_EVENT:
						lock (eventBuffer)
						{
							eventBuffer[SiApp.SiEventType.SI_BUTTON_RELEASE_EVENT] = ButtonEventArgs.FromKeyCode((int)edata.spwData.bData.last, true);
							Monitor.Pulse(eventBuffer);
						}
						break;
					default:
						lock(eventBuffer) {
							//Console.WriteLine("event type: " + edata.type);
						}
						break;
                }
            }
        }

        protected virtual void OnZeroPoint()
        {
            if (ZeroPoint != null)
                ZeroPoint(this, EventArgs.Empty);
        }

        protected virtual void OnMotion(MotionEventArgs args)
        {
            if (Motion != null)
                Motion(this, args);
        }

		protected virtual void OnButtonPress(ButtonEventArgs args)
		{
			if (ButtonPressed != null)
				ButtonPressed(this, args);
		}

		protected virtual void OnButtonRelease(ButtonEventArgs args)
		{
			if (ButtonReleased != null)
				ButtonReleased(this, args);
		}

		//protected virtual void OnDeviceChange(DeviceChangeEventArgs args)
		//{
		//    if (DeviceChange != null)
		//        DeviceChange(this, args);
		//}

		#region IDisposable Member

		public void Dispose()
        {
            if (!this.IsDisposed)
            {
                CloseDevice();
                ZeroPoint = null;
                Motion = null;
            }
            this.IsDisposed = true;
        }

        #endregion

        private void EventThreadLoop()
        {
            while (!this.IsDisposed)
            {
                lock (eventBuffer)
                {
                    while (eventBuffer.Count == 0)
                        Monitor.Wait(eventBuffer);
                    foreach (var c in eventBuffer)
                    {
                        switch (c.Key)
                        {
                            case SiApp.SiEventType.SI_MOTION_EVENT:
                                OnMotion(c.Value as MotionEventArgs); break;
							case SiApp.SiEventType.SI_BUTTON_PRESS_EVENT:
								OnButtonPress(c.Value as ButtonEventArgs); break;
							case SiApp.SiEventType.SI_BUTTON_RELEASE_EVENT:
								OnButtonRelease(c.Value as ButtonEventArgs); break;
							case SiApp.SiEventType.SI_ZERO_EVENT:
                                OnZeroPoint(); break;
                        }
                    }
                    eventBuffer.Clear();
                }
                Thread.Sleep(50);
            }
        }
    }
}
