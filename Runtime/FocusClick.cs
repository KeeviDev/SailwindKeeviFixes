using System;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Hides the mouse click that brings the game window to the front: a mouse button pressed shortly after the game
    /// gains focus, or already held when it does, reads as up until it is released.
    /// </summary>
    internal static class FocusClick
    {
        /// <summary>Seconds after gaining focus during which a press counts as the focusing click.</summary>
        private const float FocusClickWindow = 0.15f;

        private const int MouseButtonCount = 7;

        private static readonly bool[] Swallowed = new bool[MouseButtonCount];

        private static readonly Func<KeyCode, bool> KeyDown = Input.GetKeyDown;
        private static readonly Func<KeyCode, bool> Key = Input.GetKey;
        private static readonly Func<KeyCode, bool> KeyUp = Input.GetKeyUp;

        private static float focusTime = float.NegativeInfinity;
        private static int refreshedFrame = -1;
        private static int swallowedCount;

        /// <summary>
        /// Starts the focus click window when the game gains focus, hiding the mouse buttons already held.
        /// </summary>
        public static void OnFocusChanged(bool focused)
        {
            if (!focused)
                return;

            focusTime = Time.realtimeSinceStartup;
            refreshedFrame = -1;
            for (int i = 0; i < MouseButtonCount; i++)
            {
                if (Input.GetKey(KeyCode.Mouse0 + i))
                    Swallow(i);
            }
        }

        /// <summary>Whether any mouse button is hidden this frame.</summary>
        public static bool IsSwallowing
        {
            get
            {
                Refresh();
                return swallowedCount > 0;
            }
        }

        /// <summary>Returns the game's input read for a pressed button, without hidden mouse buttons.</summary>
        public static bool GetKeyDown(InputName inputName)
        {
            return Read(inputName, KeyDown);
        }

        /// <summary>Returns the game's input read for a held button, without hidden mouse buttons.</summary>
        public static bool GetKey(InputName inputName)
        {
            return Read(inputName, Key);
        }

        /// <summary>Returns the game's input read for a released button, without hidden mouse buttons.</summary>
        public static bool GetKeyUp(InputName inputName)
        {
            return Read(inputName, KeyUp);
        }

        /// <summary>Returns <see cref="Input.GetMouseButtonDown"/>, or false for a hidden button.</summary>
        public static bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button) && !IsSwallowed(KeyCode.Mouse0 + button);
        }

        /// <summary>Returns <see cref="Input.GetMouseButton"/>, or false for a hidden button.</summary>
        public static bool GetMouseButton(int button)
        {
            return Input.GetMouseButton(button) && !IsSwallowed(KeyCode.Mouse0 + button);
        }

        /// <summary>Returns <see cref="Input.GetMouseButtonUp"/>, or false for a hidden button.</summary>
        public static bool GetMouseButtonUp(int button)
        {
            return Input.GetMouseButtonUp(button) && !IsSwallowed(KeyCode.Mouse0 + button);
        }

        /// <summary>
        /// Returns <paramref name="read"/> for the input's two key bindings and, when it is on, its controller binding,
        /// as <see cref="GameInput"/> reads them.
        /// </summary>
        private static bool Read(InputName inputName, Func<KeyCode, bool> read)
        {
            return ReadKey(GameInput.GetKeyCode(inputName, false, false), read)
                || ReadKey(GameInput.GetKeyCode(inputName, true, false), read)
                || (ReadKey(GameInput.GetKeyCode(inputName, false, true), read) && GameInput.controllerEnabled);
        }

        private static bool ReadKey(KeyCode key, Func<KeyCode, bool> read)
        {
            return read(key) && !IsSwallowed(key);
        }

        private static bool IsSwallowed(KeyCode key)
        {
            Refresh();
            int button = key - KeyCode.Mouse0;
            return button >= 0 && button < MouseButtonCount && Swallowed[button];
        }

        /// <summary>
        /// Hides mouse buttons pressed within the focus click window and shows hidden ones again the frame after their
        /// release, once a frame.
        /// </summary>
        private static void Refresh()
        {
            int frame = Time.frameCount;
            if (frame == refreshedFrame)
                return;

            refreshedFrame = frame;
            bool inWindow = Time.realtimeSinceStartup - focusTime <= FocusClickWindow
                && FixGuard.IsOn(ModConfig.FixFocusClick);
            if (swallowedCount == 0 && !inWindow)
                return;

            for (int i = 0; i < MouseButtonCount; i++)
            {
                KeyCode button = KeyCode.Mouse0 + i;
                if (Swallowed[i])
                {
                    if (!Input.GetKey(button) && !Input.GetKeyUp(button))
                    {
                        Swallowed[i] = false;
                        swallowedCount--;
                    }
                }
                else if (inWindow && Input.GetKeyDown(button))
                {
                    Swallow(i);
                }
            }
        }

        private static void Swallow(int button)
        {
            if (Swallowed[button] || !FixGuard.IsOn(ModConfig.FixFocusClick))
                return;

            Swallowed[button] = true;
            swallowedCount++;
        }
    }
}
