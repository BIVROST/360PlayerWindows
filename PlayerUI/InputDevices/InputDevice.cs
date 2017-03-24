// TODO: support UI background checks
//       ShellViewModel.uiVisibilityBackgrundChecker


namespace PlayerUI.InputDevices
{
    abstract public class InputDevice
    {

        // TODO: play, pause, rewind, stop

        //public abstract SharpDX.Quaternion velocity { get; }

        public float vYaw;
        public float vPitch;
        public float vRoll;


        public abstract void Update(float deltaTime);

        public abstract bool Active { get; }

        public void Reset() {
            vYaw = vPitch = vRoll = 0;
        }
    }

    //abstract public class OculusRemoteInputDevice : InputDevice { }
    //abstract public class HTCViveWandInputDevice : InputDevice { }

    //abstract public class MouseInputDevice : InputDevice { }
 
    //abstract public class NavigatorInputDevice : InputDevice { }

}
