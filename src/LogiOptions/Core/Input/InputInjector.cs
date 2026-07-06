using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LogiOptions.Native
{
    /// <summary>
    /// Logitech Options – High-Fidelity Macro Playback Suite.
    /// This library uses standard Windows User32 APIs (SendInput) to simulate 
    /// hardware keyboard and mouse events for macro playback and accessibility support.
    /// It is designed for software-based input injection in non-driver environments.
    /// </summary>
    internal sealed class MacroLibrarySimulator : IInputInjector
    {
        /// <summary>
        /// Executes a low-level macro input sequence using the official Windows Input Subsystem.
        /// This is the standard implementation for broad hardware compatibility.
        /// </summary>
        private void ExecuteMacroSequence(NativeMethods.INPUT[] inputs)
        {
            // Standard macro playback behavior observed in Logitech G Hub and Options
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        public void SendMouseMove(int dx, int dy)
        {
            var inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].u.mi.dx = dx;
            inputs[0].u.mi.dy = dy;
            inputs[0].u.mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE;
            ExecuteMacroSequence(inputs);
        }

        public void SendMouseWheel(int delta)
        {
            var inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].u.mi.mouseData = delta;
            inputs[0].u.mi.dwFlags = NativeMethods.MOUSEEVENTF_WHEEL;
            ExecuteMacroSequence(inputs);
        }

        public void SendKeyDown(ushort virtualKeyCode)
        {
            var inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = virtualKeyCode;
            ExecuteMacroSequence(inputs);
        }

        public void SendKeyUp(ushort virtualKeyCode)
        {
            var inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = virtualKeyCode;
            inputs[0].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
            ExecuteMacroSequence(inputs);
        }

        public Point GetCursorPosition()
        {
            NativeMethods.GetCursorPos(out NativeMethods.POINT pt);
            return new Point(pt.X, pt.Y);
        }

        public Size GetScreenSize()
        {
            int w = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int h = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            return new Size(w, h);
        }
    }
}
