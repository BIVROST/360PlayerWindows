using Bivrost.Bivrost360Player.Tools;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace Bivrost.Bivrost360Player.InputDevices
{
    public class KeyboardInputDevice : InputDevice
    {

        #region static global state
        private static BitArray state;
        private static object locker = new object();

        static KeyboardInputDevice()
        {
            lock (locker)
            {
                const int maxKey = 255;
                state = new BitArray(maxKey, false);
                EventManager.RegisterClassHandler(typeof(Window), UIElement.KeyDownEvent, new KeyEventHandler(HandleKeyDown));
                EventManager.RegisterClassHandler(typeof(Window), UIElement.KeyUpEvent, new KeyEventHandler(HandleKeyUp));
            }
        }


        private static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            lock (locker)
                state.Set((byte)e.Key, true);
        }

        private static void HandleKeyUp(object sender, KeyEventArgs e)
        {
            lock (locker)
                state.Set((byte)e.Key, false);
        }
        #endregion

        private BitArray currentState;
        private BitArray prevState;

        public KeyboardInputDevice()
        {
            prevState = new BitArray(state.Length);
            currentState = new BitArray(state.Length);
            for (int i = state.Length - 1; i >= 0; i--)
            {
                prevState[i] = currentState[i] = state[i];
            }
        }


        /// <summary>
        /// The key was pressed in this frame, active only on one frame per key press
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>was the key just pressed</returns>
        public bool KeyPressed(Key key)
        {
            byte k = (byte)key;
            return currentState[k] && !prevState[k];
        }

        /// <summary>
        /// The key was released in this frame, active only on one frame per key press
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>was the key just released</returns>
        public bool KeyReleased(Key key)
        {
            byte k = (byte)key;
            return !currentState[k] && prevState[k];
        }

        /// <summary>
        /// The key is down
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>is the key currently down, also returns true in the pressed state</returns>
        public bool KeyDown(Key key)
        {
            byte k = (byte)key;
            return currentState[k];
        }

        /// <summary>
        /// The key is not down
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>is the key currently not pressed, also returns true in the released state</returns>
        public bool KeyUp(Key key)
        {
            byte k = (byte)key;
            return !currentState[k];
        }


        public override bool Active
        {
            get { return true; }
        }

        public override void Update(float deltaTime)
        {
            lock (locker)
            {
                for (int i = state.Length - 1; i >= 0; i--)
                {
                    prevState[i] = currentState[i];
                    currentState[i] = state[i];
                }
            }

            float dvYaw = 0;
            float dvPitch = 0;
            if (KeyDown(Key.Left))
                dvYaw += 1;
            if (KeyDown(Key.Right))
                dvYaw -= 1;
            if (KeyDown(Key.Up))
                dvPitch += 1;
            if (KeyDown(Key.Down))
                dvPitch -= 1;
            vPitch = vPitch.LerpInPlace(dvPitch, deltaTime * 4);
            vYaw = vYaw.LerpInPlace(dvYaw, deltaTime * 4);
        }

    }

}
