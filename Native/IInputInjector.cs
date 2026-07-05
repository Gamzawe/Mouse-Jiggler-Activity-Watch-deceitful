using System;
using System.Drawing;

namespace LogiOptions.Native
{
    /// <summary>
    /// Abstracts low-level input injection so production uses Win32 SendInput
    /// and tests can substitute a recording mock.
    /// </summary>
    public interface IInputInjector
    {
        void SendMouseMove(int dx, int dy);
        void SendMouseWheel(int delta);
        void SendKeyDown(ushort virtualKeyCode);
        void SendKeyUp(ushort virtualKeyCode);
        Point GetCursorPosition();
        Size GetScreenSize();
    }
}
