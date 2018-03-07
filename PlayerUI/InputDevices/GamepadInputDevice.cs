using Bivrost.Log;
using PlayerUI.Tools;
using SharpDX.XInput;

namespace PlayerUI.InputDevices
{
    public class GamepadInputDevice : InputDevice
    {

		Logger logger = new Logger("gamepad");

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
				LoggerManager.Publish(
					"gamepad", 
					string.Format("buttons={6} LT={0:00000} {1:00000} {2:00000} RT={3:00000} {4:00000} {5:00000}",
						gamepad.LeftThumbX, gamepad.LeftThumbY, gamepad.LeftTrigger,
						gamepad.RightThumbX, gamepad.RightThumbY, gamepad.RightTrigger,
						gamepad.Buttons
					)
				);

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

				// left thumb not used, try the right one
				if(dvYaw == 0 && dvPitch == 0)
				{
					if (gamepad.RightThumbX < -Gamepad.RightThumbDeadZone)
						dvYaw = (gamepad.RightThumbX + Gamepad.RightThumbDeadZone) / (32768f - Gamepad.RightThumbDeadZone);
					else if (gamepad.RightThumbX > Gamepad.RightThumbDeadZone)
						dvYaw = (gamepad.RightThumbX - Gamepad.RightThumbDeadZone) / (32767f - Gamepad.RightThumbDeadZone);

					if (gamepad.RightThumbY < -Gamepad.RightThumbDeadZone)
						dvPitch = (gamepad.RightThumbY + Gamepad.RightThumbDeadZone) / (32768f - Gamepad.RightThumbDeadZone);
					else if (gamepad.RightThumbY > Gamepad.RightThumbDeadZone)
						dvPitch = (gamepad.RightThumbY - Gamepad.RightThumbDeadZone) / (32767f - Gamepad.RightThumbDeadZone);
				}

				dvYaw = -dvYaw;
			}
			else
			{
				LoggerManager.Publish("gamepad", "not connected");
			}
			

			vPitch = vPitch.LerpInPlace(dvPitch, deltaTime * 4);
            vYaw = vYaw.LerpInPlace(dvYaw, deltaTime * 4);
        }
    }

}
