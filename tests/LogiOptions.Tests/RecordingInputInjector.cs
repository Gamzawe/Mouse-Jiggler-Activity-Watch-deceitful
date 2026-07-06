using System.Collections.Generic;
using System.Drawing;
using LogiOptions.Native;

namespace LogiOptions.Tests
{
    /// <summary>
    /// Records every input call for assertion. Thread-safe enough for single-threaded test runs.
    /// </summary>
    public sealed class RecordingInputInjector : IInputInjector
    {
        public List<InputEvent> Events { get; } = new();
        public Point CursorPosition { get; set; } = new Point(500, 500);
        public Size ScreenSize { get; set; } = new Size(1920, 1080);

        public void SendMouseMove(int dx, int dy)
        {
            CursorPosition = new Point(CursorPosition.X + dx, CursorPosition.Y + dy);
            Events.Add(new InputEvent(InputEventType.MouseMove, dx: dx, dy: dy));
        }

        public void SendMouseWheel(int delta)
            => Events.Add(new InputEvent(InputEventType.MouseWheel, wheelDelta: delta));

        public void SendKeyDown(ushort virtualKeyCode)
            => Events.Add(new InputEvent(InputEventType.KeyDown, vk: virtualKeyCode));

        public void SendKeyUp(ushort virtualKeyCode)
            => Events.Add(new InputEvent(InputEventType.KeyUp, vk: virtualKeyCode));

        public Point GetCursorPosition() => CursorPosition;

        public Size GetScreenSize() => ScreenSize;

        public void Clear() => Events.Clear();
    }

    public enum InputEventType { MouseMove, MouseWheel, KeyDown, KeyUp }

    public record InputEvent(
        InputEventType Type,
        int dx = 0,
        int dy = 0,
        int wheelDelta = 0,
        ushort vk = 0);
}
