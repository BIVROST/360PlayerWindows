using PlayerUI.Tools;
using SharpDX.XInput;

namespace PlayerUI.InputDevices
{
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
			float dvPitch = 0;
			float dvYaw = 0;

			if (Active)
			{
				prevGamepad = gamepad;
				gamepad = xpad.GetState().Gamepad;

				
				if (gamepad.LeftThumbX < -Gamepad.LeftThumbDeadZone)
					dvYaw = (gamepad.LeftThumbX + Gamepad.LeftThumbDeadZone) / (32768f - Gamepad.LeftThumbDeadZone);
				else if (gamepad.LeftThumbX > Gamepad.LeftThumbDeadZone)
					dvYaw = (gamepad.LeftThumbX - Gamepad.LeftThumbDeadZone) / (32767f - Gamepad.LeftThumbDeadZone);

				
				if (gamepad.LeftThumbY < -Gamepad.LeftThumbDeadZone)
					dvPitch = (gamepad.LeftThumbY + Gamepad.LeftThumbDeadZone) / (32768f - Gamepad.LeftThumbDeadZone);
				else if (gamepad.LeftThumbY > Gamepad.LeftThumbDeadZone)
					dvPitch = (gamepad.LeftThumbY - Gamepad.LeftThumbDeadZone) / (32767f - Gamepad.LeftThumbDeadZone);

				dvYaw = -dvYaw;
			}
			

			vPitch = vPitch.LerpInPlace(dvPitch, deltaTime * 4);
            vYaw = vYaw.LerpInPlace(dvYaw, deltaTime * 4);
        }
    }

}
