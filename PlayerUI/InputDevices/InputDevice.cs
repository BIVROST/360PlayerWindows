using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using System.Windows.Input;
using PlayerUI.Tools;
using SharpDX.XInput;

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

    abstract public class OculusRemoteInputDevice : InputDevice { }
    abstract public class HTCViveWandInputDevice : InputDevice { }

    abstract public class MouseInputDevice : InputDevice { }
    public class GamepadInputDevice : InputDevice
    {
        public override bool Active
        {
            get { return xpad != null && xpad.IsConnected; }
        }

        private Controller xpad;

        public GamepadInputDevice(Controller xpad)
        {
            this.xpad = xpad;
        }


        public GamepadInputDevice() : this(new Controller(UserIndex.One))
        {
        }

        public bool ButtonDown(GamepadButtonFlags button)
        {
            return gamepad.Buttons.HasFlag(button);
        }

        public bool ButtonPressed(GamepadButtonFlags button)
        {
            return gamepad.Buttons.HasFlag(button) && !prevGamepad.Buttons.HasFlag(button);
        }

        public bool ButtonReleased(GamepadButtonFlags button)
        {
            return !gamepad.Buttons.HasFlag(button) && prevGamepad.Buttons.HasFlag(button);
        }

        public bool ButtonUp(GamepadButtonFlags button)
        {
            return !ButtonDown(button);
        }

        Gamepad gamepad;
        Gamepad prevGamepad;
        public override void Update(float deltaTime)
        {
            const float velocity = 90;
            prevGamepad = gamepad;
            gamepad = xpad.GetState().Gamepad;

            float dvYaw = 0;
            if (gamepad.LeftThumbX < -Gamepad.LeftThumbDeadZone)
                dvYaw = (gamepad.LeftThumbX + Gamepad.LeftThumbDeadZone) / (32768f - Gamepad.LeftThumbDeadZone);
            else if (gamepad.LeftThumbX > Gamepad.LeftThumbDeadZone)
                dvYaw = (gamepad.LeftThumbX - Gamepad.LeftThumbDeadZone) / (32767f - Gamepad.LeftThumbDeadZone);

            float dvPitch = 0;
            if (gamepad.LeftThumbY < -Gamepad.LeftThumbDeadZone)
                dvPitch = (gamepad.LeftThumbY + Gamepad.LeftThumbDeadZone) / (32768f - Gamepad.LeftThumbDeadZone);
            else if (gamepad.LeftThumbY > Gamepad.LeftThumbDeadZone)
                dvPitch = (gamepad.LeftThumbY - Gamepad.LeftThumbDeadZone) / (32767f - Gamepad.LeftThumbDeadZone);

            dvPitch *= velocity;
            dvYaw *= -velocity;

            vPitch = vPitch.LerpInPlace(dvPitch, deltaTime * 4);
            vYaw = vYaw.LerpInPlace(dvYaw, deltaTime * 4);
        }
    }



    abstract public class NavigatorInputDevice : InputDevice { }


    public class KeyboardInputDevice : InputDevice {
        Keyboard keyboard;

        public const float velocity = 90;

        public KeyboardInputDevice(Keyboard keyboard)
        {
            this.keyboard = keyboard;
        }
        
        public override bool Active
        {
            get { return true; }
        }

        public override void Update(float deltaTime)
        {
            float dvYaw = 0;
            float dvPitch = 0;
            if (keyboard.KeyDown(Key.Left))
                dvYaw += velocity;
            if (keyboard.KeyDown(Key.Right))
                dvYaw -= velocity;
            if (keyboard.KeyDown(Key.Up))
                dvPitch += velocity;
            if (keyboard.KeyDown(Key.Down))
                dvPitch -= velocity;
            vPitch = vPitch.LerpInPlace(dvPitch, deltaTime * 4);
            vYaw = vYaw.LerpInPlace(dvYaw, deltaTime * 4);
        }

    }
}
