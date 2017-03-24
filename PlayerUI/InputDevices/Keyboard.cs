using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Input;

namespace PlayerUI.InputDevices
{
    public class Keyboard
    {
        enum KeyState { up, pressed, down, released }

        ConcurrentDictionary<Key, KeyState> keys { get; set; } = new ConcurrentDictionary<Key, KeyState>();


        //public Keyboard()
        //{
        //      TODO: currently global per app
        //    EventManager.RegisterClassHandler(typeof(Window), UIElement.KeyDownEvent, new KeyEventHandler((s,e) => Console.WriteLine("DOWN: " + e.Key)));
        //}

        public void UpdateKeyState()
        {
            foreach (KeyValuePair<Key, KeyState> kvp in keys)
            {
                bool isDown = System.Windows.Input.Keyboard.IsKeyDown(kvp.Key);
                switch(kvp.Value)
                {
                    case KeyState.up:
                        if (isDown)
                            keys[kvp.Key] = KeyState.pressed;
                        break;

                    case KeyState.pressed:
                        if (isDown)
                            keys[kvp.Key] = KeyState.down;
                        else
                            keys[kvp.Key] = KeyState.released;
                        break;

                    case KeyState.down:
                        if (!isDown)
                            keys[kvp.Key] = KeyState.released;
                        break;

                    case KeyState.released:
                        if (isDown)
                            keys[kvp.Key] = KeyState.pressed;
                        else
                            keys[kvp.Key] = KeyState.up;
                        break;
                }
            }
        }


        public void RegisterTrackedKey(Key key) {
            if (!keys.ContainsKey(key)) // mark so it will be polled during next UpdateKeyState
                keys[key] = KeyState.up;
            //System.Windows.Input.Keyboard.IsKeyDown(key) ? KeyState.pressed : KeyState.up;
        }


        /// <summary>
        /// The key was pressed in this frame, active only on one frame per key press
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>was the key just pressed</returns>
        public bool KeyPressed(Key key)
        {
            RegisterTrackedKey(key);
            return keys[key] == KeyState.pressed;
        }

        /// <summary>
        /// The key was released in this frame, active only on one frame per key press
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>was the key just released</returns>
        public bool KeyReleased(Key key)
        {
            RegisterTrackedKey(key);
            return keys[key] == KeyState.released;
        }

        /// <summary>
        /// The key is down
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>is the key currently down, also returns true in the pressed state</returns>
        public bool KeyDown(Key key)
        {
            RegisterTrackedKey(key);
            return keys[key] == KeyState.down || keys[key] == KeyState.pressed;
        }

        /// <summary>
        /// The key is not down
        /// </summary>
        /// <param name="key">the key in question</param>
        /// <returns>is the key currently not pressed, also returns true in the released state</returns>
        public bool KeyUp(Key key)
        {
            RegisterTrackedKey(key);
            return keys[key] == KeyState.up || keys[key] == KeyState.released;
        }

    }

}
