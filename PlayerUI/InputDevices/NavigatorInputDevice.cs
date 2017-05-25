using _3DconnexionDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bivrost.Log;

namespace PlayerUI.InputDevices
{
    public class NavigatorInputDevice : InputDevice, IDisposable
    {


        public bool leftPressed;
        public bool rightPressed;

        static _3DconnexionDevice connex;

		private static Logger log = new Logger("3dConnexion Navigator");

        public override bool Active
        {
            get { return connex != null; }
        }

        public override void Update(float deltaTime)
        {
            leftPressed = rightPressed = false;
        }

        public NavigatorInputDevice()
        {
            if (!Active)
                return;
            connex.Motion += Connex_Motion;
            connex.ZeroPoint += Connex_ZeroPoint;
            connex.ButtonPressed += Connex_ButtonPressed;
        }

        private void Connex_ButtonPressed(object sender, ButtonEventArgs e)
        {
            switch(e.Key)
            {
                case V3dKey.V3DK_MENU_1:
                    leftPressed = true;
                    break;
                case V3dKey.V3DK_MENU_2:
                    rightPressed = true;
                    break;
            }
        }

        public void Dispose()
        {
            connex.Motion -= Connex_Motion;
            connex.ZeroPoint -= Connex_ZeroPoint;
        }


        private void Connex_Motion(object sender, MotionEventArgs e)
        {
            vYaw = e.RY / 2048f;
            vPitch = -e.RX / 2048f;
            vRoll = -e.RZ / 2048f;
            if (Logic.Instance.settings.SpaceNavigatorInvertPitch)
                vPitch = -vPitch;
        }

        private void Connex_ZeroPoint(object sender, EventArgs e)
        {
            Reset();
        }

        internal static void TryInit(IntPtr windowHandle)
        {
            try
            {
                connex = new _3DconnexionDevice("BIVROST 360Player");
                connex.InitDevice(windowHandle);
                connex.UiMode = false;
                connex.ResetAllButtonBindings();
                connex.LEDs = 10;
				log.Info($"Device initiated");
			}
			catch (DllNotFoundException)
            {
                connex = null;
            } catch(_3DxException e)
			{
				log.Error($"Could not connect: {e.Message}");
				connex = null;
			}
        }

        internal static void WndProc(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (connex != null)
                connex.ProcessWindowMessage(msg, wParam, lParam);
        }
    }
}
