// TODO: support UI background checks
//       ShellViewModel.uiVisibilityBackgrundChecker


using System;

namespace Bivrost.Bivrost360Player.InputDevices
{
    abstract public class InputDevice
    {

        // TODO: play, pause, rewind, stop

        //public abstract SharpDX.Quaternion velocity { get; }

        public float vYaw = 0;
        public float vPitch = 0;
        public float vRoll = 0;
		public float vPush = 0;


		public virtual void Update(float deltaTime) { }
		public virtual void LateUpdate(float deltaTime) { }

		public abstract bool Active { get; }

        public void Reset() {
            vYaw = vPitch = vRoll = vPush = 0;
        }
    }

    //abstract public class OculusRemoteInputDevice : InputDevice { }
    //abstract public class HTCViveWandInputDevice : InputDevice { }
    //abstract public class MouseInputDevice : InputDevice { }



}
