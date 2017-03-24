using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.InputDevices
{
    abstract public class InputDevice
    {

        // TODO: play, pause, rewind, stop

        public SharpDX.Quaternion velocity;

        public abstract void Update();


        public abstract bool Active { get; }
    }


    abstract public class MouseInputDevice : InputDevice { }
    abstract public class GamepadInputDevice : InputDevice { }
    abstract public class NavigatorInputDevice : InputDevice { }
    abstract public class KeyboardInputDevice : InputDevice {

        //private bool GetKeyState(Key key)
        //{
        //    if (this.Host != null)
        //    {
        //        if (!this.Host.KeyState.ContainsKey(key))
        //        {
        //            this.Host.KeyState.TryAdd(key, false);
        //            return false;
        //        }
        //        return this.Host.KeyState[key];
        //    }
        //    return false;
        //}

        //public override void Update()
        //{
        //    if (IsKeyDown(Key.Left))
        //        MoveDelta(1f, 0f, speed * deltaTime, 4f);
        //    if (IsKeyDown(Key.Right))
        //        MoveDelta(-1.0f, 0f, speed * deltaTime, 4f);
        //    if (IsKeyDown(Key.Up))
        //        MoveDelta(0f, 1f, speed * deltaTime, 4f);
        //    if (IsKeyDown(Key.Down))
        //        MoveDelta(0f, -1f, speed * deltaTime, 4f);
        //}
    }
}
