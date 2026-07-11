// <copyright file="InputHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;
using static BPlusLib.Foundation.Native.User32;

namespace BPlusLib.Foundation.Input
{
    /// <summary>
    /// Provides keyboard and mouse input simulation via SendInput.
    /// All methods are thread-safe and gracefully return false on failure.
    /// </summary>
    /// <remarks>
    /// SendInput is subject to UIPI: a lower-integrity process cannot send input
    /// to a higher-integrity window. Elevation may be required for some targets.
    /// </remarks>
    public static class InputHelper
    {
        private static readonly int InputSize = Marshal.SizeOf<INPUT>();

        /// <summary>
        /// Sends a single key press (down followed by up).
        /// </summary>
        public static bool SendKeyPress(VirtualKeyCode keyCode)
        {
            var inputs = new INPUT[2];
            inputs[0] = CreateKeyInput((ushort)keyCode, KEYEVENTF_KEYDOWN);
            inputs[1] = CreateKeyInput((ushort)keyCode, KEYEVENTF_KEYUP);
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>
        /// Sends a modifier+key combination (e.g., Ctrl+C, Alt+Tab).
        /// </summary>
        public static bool SendModifiedKey(VirtualKeyCode modifier, VirtualKeyCode key)
        {
            var inputs = new INPUT[4];
            inputs[0] = CreateKeyInput((ushort)modifier, KEYEVENTF_KEYDOWN);
            inputs[1] = CreateKeyInput((ushort)key, KEYEVENTF_KEYDOWN);
            inputs[2] = CreateKeyInput((ushort)key, KEYEVENTF_KEYUP);
            inputs[3] = CreateKeyInput((ushort)modifier, KEYEVENTF_KEYUP);
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>
        /// Sends a key-down event for the specified key.
        /// </summary>
        public static bool KeyDown(VirtualKeyCode keyCode)
        {
            var input = CreateKeyInput((ushort)keyCode, KEYEVENTF_KEYDOWN);
            return SendInput(1, new[] { input }, InputSize) == 1;
        }

        /// <summary>
        /// Sends a key-up event for the specified key.
        /// </summary>
        public static bool KeyUp(VirtualKeyCode keyCode)
        {
            var input = CreateKeyInput((ushort)keyCode, KEYEVENTF_KEYUP);
            return SendInput(1, new[] { input }, InputSize) == 1;
        }

        /// <summary>
        /// Types text by sending Unicode key events for each character.
        /// Supports most printable characters.
        /// </summary>
        public static bool SendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var inputs = new INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                inputs[i * 2] = CreateUnicodeInput(text[i], KEYEVENTF_KEYDOWN);
                inputs[i * 2 + 1] = CreateUnicodeInput(text[i], KEYEVENTF_KEYUP);
            }
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>
        /// Moves the mouse cursor. If relative=false, uses absolute screen coordinates.
        /// </summary>
        public static bool MoveMouse(int x, int y, bool relative = false)
        {
            uint flags = MOUSEEVENTF_MOVE;
            if (!relative)
                flags |= MOUSEEVENTF_ABSOLUTE;
            var input = new INPUT { type = INPUT_MOUSE };
            input.union.mi = new MOUSEINPUT
            {
                dx = x,
                dy = y,
                dwFlags = flags,
            };
            return SendInput(1, new[] { input }, InputSize) == 1;
        }

        /// <summary>Simulates a left mouse button click.</summary>
        public static bool LeftClick()
        {
            var inputs = new INPUT[2];
            inputs[0] = CreateMouseInput(MOUSEEVENTF_LEFTDOWN);
            inputs[1] = CreateMouseInput(MOUSEEVENTF_LEFTUP);
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>Simulates a right mouse button click.</summary>
        public static bool RightClick()
        {
            var inputs = new INPUT[2];
            inputs[0] = CreateMouseInput(MOUSEEVENTF_RIGHTDOWN);
            inputs[1] = CreateMouseInput(MOUSEEVENTF_RIGHTUP);
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>Simulates a middle mouse button click.</summary>
        public static bool MiddleClick()
        {
            var inputs = new INPUT[2];
            inputs[0] = CreateMouseInput(MOUSEEVENTF_MIDDLEDOWN);
            inputs[1] = CreateMouseInput(MOUSEEVENTF_MIDDLEUP);
            return SendInput((uint)inputs.Length, inputs, InputSize) == inputs.Length;
        }

        /// <summary>Simulates a mouse wheel scroll. Positive = up, negative = down.</summary>
        public static bool ScrollWheel(int delta)
        {
            var input = new INPUT { type = INPUT_MOUSE };
            input.union.mi = new MOUSEINPUT
            {
                mouseData = (uint)delta,
                dwFlags = MOUSEEVENTF_WHEEL,
            };
            return SendInput(1, new[] { input }, InputSize) == 1;
        }

        // --- Private helpers ---

        private static INPUT CreateKeyInput(ushort keyCode, uint flags)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                union = new INPUT_UNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = keyCode,
                        dwFlags = flags,
                    },
                },
            };
        }

        private static INPUT CreateUnicodeInput(char ch, uint flags)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                union = new INPUT_UNION
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = ch,
                        dwFlags = flags | KEYEVENTF_UNICODE,
                    },
                },
            };
        }

        private static INPUT CreateMouseInput(uint flags)
        {
            return new INPUT
            {
                type = INPUT_MOUSE,
                union = new INPUT_UNION
                {
                    mi = new MOUSEINPUT { dwFlags = flags },
                },
            };
        }
    }
}
