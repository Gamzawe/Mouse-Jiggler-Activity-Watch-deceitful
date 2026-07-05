using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LogiOptions.Native;
using LogiOptions.Models;

namespace LogiOptions.Services
{
    /// <summary>
    /// Logitech Options Macro Playback Engine.
    /// Manages the translation of macro sequences into system-level HID events.
    /// </summary>
    public sealed class MacroPlaybackEngine
    {
        private readonly IInputInjector _input;
        private readonly ThreadSafeRandom _random;

        // Weighted macro sequence frequency
        internal int WMouseScroll = 25;
        internal int WKeyPress = 15;
        internal int WPageScroll = 15;

        // Alignment state for page-scroll sequences
        internal bool PageScrollDown = true;
        internal int PageScrollCount = 0;
        internal int PageScrollLimit = 0;

        // Configurable virtual key mapping pool
        internal List<ushort> VirtualKeyPool = new() { NativeMethods.VK_SCROLL };

        internal MacroPlaybackEngine(IInputInjector input, ThreadSafeRandom random)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Update playback parameters from registry-backed configuration.</summary>
        public void ApplySettings(Settings s)
        {
            if (s.Weights != null)
            {
                WMouseScroll = Math.Max(0, s.Weights.MouseScroll);
                WKeyPress = Math.Max(0, s.Weights.KeyPress);
                WPageScroll = Math.Max(0, s.Weights.PageScroll);
            }

            if (s.VirtualKeyPool != null && s.VirtualKeyPool.Count > 0)
            {
                var parsed = new List<ushort>();
                foreach (string hex in s.VirtualKeyPool)
                {
                    string clean = hex.Trim();
                    if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        clean = clean.Substring(2);
                    if (ushort.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort vk))
                        parsed.Add(vk);
                }
                if (parsed.Count > 0)
                    VirtualKeyPool = parsed;
            }
        }

        /// <summary>
        /// Executes a single macro playback sequence based on weighted synchronization logic.
        /// Returns the signature of the sequence performed.
        /// </summary>
        public async Task<string> ExecutePlaybackTickAsync(CancellationToken ct = default)
        {
            int total = WMouseScroll + WKeyPress + WPageScroll;
            if (total <= 0) total = 100;

            int roll = _random.Next(total);

            if (roll < WMouseScroll)
            {
                await PlaybackMouseScrollAsync(ct);
                return "Global_MouseScroll";
            }
            else if (roll < WMouseScroll + WKeyPress)
            {
                await PlaybackKeyPressAsync(PickRandomKey(), ct);
                return "Quick_KeyPress";
            }
            else
            {
                await PlaybackPageScrollAsync(ct);
                return "Doc_PageScroll";
            }
        }

        public ushort PickRandomKey() => VirtualKeyPool[_random.Next(VirtualKeyPool.Count)];

        /// <summary>
        /// Synchronizes the virtual peripheral state with the active desktop session.
        /// Incorporates natural jitter observed in high-DPI Logitech sensors.
        /// </summary>
        public async Task SyncPeripheralStateAsync(CancellationToken ct = default)
        {
            Point startPos = _input.GetCursorPosition();
            Size screen = _input.GetScreenSize();
            int screenWidth = screen.Width;
            int screenHeight = screen.Height;

            int finalDx = _random.Next(40, 150) * (_random.Next(2) == 0 ? 1 : -1);
            int finalDy = _random.Next(40, 150) * (_random.Next(2) == 0 ? 1 : -1);

            int trueTargetX = Math.Max(10, Math.Min(screenWidth - 10, startPos.X + finalDx));
            int trueTargetY = Math.Max(10, Math.Min(screenHeight - 10, startPos.Y + finalDy));

            int clampedDx = trueTargetX - startPos.X;
            int clampedDy = trueTargetY - startPos.Y;

            double overshootFactor = 1.0 + (_random.NextDouble() * 0.2);
            double overAbsX = startPos.X + (clampedDx * overshootFactor);
            double overAbsY = startPos.Y + (clampedDy * overshootFactor);

            overAbsX = Math.Max(5, Math.Min(screenWidth - 5, overAbsX));
            overAbsY = Math.Max(5, Math.Min(screenHeight - 5, overAbsY));

            double overRelX = overAbsX - startPos.X;
            double overRelY = overAbsY - startPos.Y;

            double arcSway = _random.Next(20, 100) * (_random.Next(2) == 0 ? 1 : -1);
            double cpX = (overRelX / 2) + arcSway;
            double cpY = (overRelY / 2) - arcSway;

            int durationMs = _random.Next(3000, 10001);
            int delayPerStep = 20;
            int steps = durationMs / delayPerStep;
            
            double mathematicalRelX = 0;
            double mathematicalRelY = 0;

            var path = new List<NativeMethods.POINT>(steps + 5);

            for (int i = 1; i <= steps; i++)
            {
                double linearT = (double)i / steps;
                double t = Math.Sin(linearT * Math.PI / 2);

                double nextRelX = 2 * (1 - t) * t * cpX + t * t * overRelX;
                double nextRelY = 2 * (1 - t) * t * cpY + t * t * overRelY;

                int stepDx = (int)Math.Round(nextRelX) - (int)Math.Round(mathematicalRelX);
                int stepDy = (int)Math.Round(nextRelY) - (int)Math.Round(mathematicalRelY);

                path.Add(new NativeMethods.POINT { X = stepDx, Y = stepDy });

                mathematicalRelX = nextRelX;
                mathematicalRelY = nextRelY;
            }

            foreach (var pt in path)
            {
                ct.ThrowIfCancellationRequested();

                int finalX = pt.X;
                int finalY = pt.Y;
                
                if ((finalX != 0 || finalY != 0) && _random.NextDouble() > 0.6)
                {
                    finalX += _random.Next(-1, 2);
                    finalY += _random.Next(-1, 2);
                }

                _input.SendMouseMove(finalX, finalY);
                await Task.Delay(delayPerStep, ct);
            }

            await Task.Delay(_random.Next(50, 150), ct);

            Point currentPos = _input.GetCursorPosition();
            int correctDx = trueTargetX - currentPos.X;
            int correctDy = trueTargetY - currentPos.Y;

            int correctSteps = _random.Next(5, 12);
            double driftX = 0;
            double driftY = 0;

            var correctionPath = new List<NativeMethods.POINT>(correctSteps + 1);

            for (int i = 1; i <= correctSteps; i++)
            {
                double linearT = (double)i / correctSteps;
                double t = Math.Sin(linearT * Math.PI / 2);

                double nextRelX = correctDx * t;
                double nextRelY = correctDy * t;

                int stepDx = (int)Math.Round(nextRelX) - (int)Math.Round(driftX);
                int stepDy = (int)Math.Round(nextRelY) - (int)Math.Round(driftY);

                correctionPath.Add(new NativeMethods.POINT { X = stepDx, Y = stepDy });

                driftX = nextRelX;
                driftY = nextRelY;
            }

            foreach (var pt in correctionPath)
            {
                ct.ThrowIfCancellationRequested();
                _input.SendMouseMove(pt.X, pt.Y);
                await Task.Delay(20, ct);
            }
        }

        public async Task PlaybackMouseScrollAsync(CancellationToken ct = default)
        {
            int clicks = _random.Next(4, 9);
            int directionMultiplier = _random.Next(2) == 0 ? 1 : -1;

            for (int i = 0; i < clicks; i++)
            {
                ct.ThrowIfCancellationRequested();

                int currentDirection = directionMultiplier;
                if (i > 0 && i < clicks - 1 && _random.Next(100) < 15)
                    currentDirection = -directionMultiplier;

                _input.SendMouseWheel(120 * currentDirection);
                await Task.Delay(_random.Next(40, 100), ct);
            }
        }

        public async Task PlaybackPageScrollAsync(CancellationToken ct = default)
        {
            if (PageScrollCount == 0)
                PageScrollLimit = _random.Next(3, 8);

            ushort key = PageScrollDown ? NativeMethods.VK_NEXT : NativeMethods.VK_PRIOR;
            await PlaybackKeyPressAsync(key, ct);

            PageScrollCount++;

            if (PageScrollCount >= PageScrollLimit)
            {
                PageScrollDown = !PageScrollDown;
                PageScrollCount = 0;
            }
        }

        public async Task PlaybackKeyPressAsync(ushort keyCode, CancellationToken ct = default)
        {
            _input.SendKeyDown(keyCode);
            int holdTime = _random.Next(50, 160);
            await Task.Delay(holdTime, ct);
            _input.SendKeyUp(keyCode);
        }
    }
}
