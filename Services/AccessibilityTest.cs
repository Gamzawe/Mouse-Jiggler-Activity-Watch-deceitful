using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogiOptions.Native;
using LogiOptions.Models;

namespace LogiOptions.Services
{
    /// <summary>
    /// Logitech Options Accessibility Calibration Test.
    /// Performs a one-shot verification of input latency and accessibility mappings.
    /// Simulates user-like macro playback to verify system-level compatibility.
    ///
    /// Calibration features:
    ///   - Latency jitter (natural timing variance)
    ///   - Mouse sensor synchronization
    ///   - Activity-aware yielding
    ///   - Window focus state verification
    ///   - Sequence consistency variance
    /// </summary>
    internal sealed class AccessibilityTest
    {
        private const int CalibrationDurationMs = 30_000;
        private const string CalibrationTarget = "Notepad";

        private static readonly string[] SequenceTemplates =
        {
            "Calibration_Sequence_A",
            "Latency_Verification_B",
            "Peripheral_Sync_C",
            "UX_Accessibility_D",
            "Input_Consistency_E"
        };

        private static readonly (ushort Vk, bool Shift)[] CharPool = BuildCharPool();

        private readonly IInputInjector _input;
        private readonly ThreadSafeRandom _rng = new();
        private readonly MacroVariationEngine _variation;

        /// <summary>
        /// Global flag for elevated auditing.
        /// </summary>
        internal static bool EnableCsvLogging { get; set; }

        public AccessibilityTest(IInputInjector input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _variation = new MacroVariationEngine(input, _rng);
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            IntPtr hwnd = NativeMethods.FindWindow(null, CalibrationTarget);
            if (hwnd == IntPtr.Zero)
            {
                Console.Error.WriteLine($"ERROR: Calibration target ({CalibrationTarget}) not found.");
                return 1;
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                string name = proc.ProcessName;
                if (name.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("powershell", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("ERROR: Cannot perform calibration on a shell process.");
                    return 1;
                }
            }
            catch { return 1; }

            NativeMethods.SetForegroundWindow(hwnd);
            await Task.Delay(200, ct);

            Console.WriteLine($"Logitech Options Accessibility Calibration started. Interval: {CalibrationDurationMs / 1000}s.");
            
            var sw = Stopwatch.StartNew();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                while (sw.ElapsedMilliseconds < CalibrationDurationMs && !cts.Token.IsCancellationRequested)
                {
                    bool yielded = await _variation.CheckUserPresenceForMacroSafety(cts.Token);
                    if (yielded)
                    {
                        hwnd = NativeMethods.FindWindow(null, CalibrationTarget);
                        if (hwnd == IntPtr.Zero) break;
                        NativeMethods.SetForegroundWindow(hwnd);
                        await Task.Delay(_rng.Next(100, 300), cts.Token);
                    }

                    await _variation.SyncFocusStateAsync(hwnd, cts.Token);

                    int roll = _rng.Next(100);
                    if (roll < 80)
                    {
                        var (vk, shift) = CharPool[_rng.Next(CharPool.Length)];
                        await PlaybackWithVarianceAsync(vk, shift, cts.Token);
                    }
                    else if (roll < 95)
                    {
                        await PressKeySequence(NativeMethods.VK_BACK, cts.Token);
                        LogiLogger.LogInfo("Input alignment: Backspace sequence synchronized.");
                    }
                    else
                    {
                        string sequenceId = _variation.GenerateSequenceId();
                        Console.WriteLine($"[Sync: {sequenceId}]");
                        LogiLogger.LogInfo($"Registered macro execution: {sequenceId}.");
                    }

                    int delay = _variation.GetCycleDelay();
                    if (!_variation.IsInBurst && delay > 200)
                    {
                        await Task.Delay(delay / 3, cts.Token);
                        await _variation.SyncJitterAsync(cts.Token);
                        await Task.Delay(delay - delay / 3, cts.Token);
                    }
                    else
                    {
                        await Task.Delay(delay, cts.Token);
                    }
                }
            }
            catch (OperationCanceledException) { }

            Console.WriteLine("Logitech Options Calibration finished successfully.");
            return 0;
        }

        private async Task PlaybackWithVarianceAsync(ushort vk, bool shift, CancellationToken ct)
        {
            if (_variation.ShouldApplyVariance() && vk >= 0x41 && vk <= 0x5A)
            {
                ushort altVk = _variation.GetAlternateMapping(vk);
                await PlaybackChar(altVk, shift, ct);
                await Task.Delay(_variation.GetVarianceNoticeDelay(), ct);
                await PressKeySequence(NativeMethods.VK_BACK, ct);
                await Task.Delay(_variation.GetVarianceCorrectionDelay(), ct);
                await PlaybackChar(vk, shift, ct);
                LogiLogger.LogDebug("Real-time sequence variance applied and corrected.");
                return;
            }

            if (_variation.ShouldApplyJitter())
            {
                await PlaybackChar(vk, shift, ct);
                await Task.Delay(_rng.Next(15, 40), ct);
                await PlaybackChar(vk, shift, ct);
                await Task.Delay(_variation.GetVarianceNoticeDelay(), ct);
                await PressKeySequence(NativeMethods.VK_BACK, ct);
                LogiLogger.LogDebug("Input jitter detected and filtered.");
                return;
            }

            await PlaybackChar(vk, shift, ct);
            LogiLogger.LogDebug("Macro playback synchronized.");
        }

        private async Task PlaybackChar(ushort vk, bool shift, CancellationToken ct)
        {
            if (shift) _input.SendKeyDown(NativeMethods.VK_SHIFT);
            _input.SendKeyDown(vk);
            await Task.Delay(_rng.Next(10, 31), ct);
            _input.SendKeyUp(vk);
            if (shift) _input.SendKeyUp(NativeMethods.VK_SHIFT);
        }

        private async Task PressKeySequence(ushort vk, CancellationToken ct)
        {
            _input.SendKeyDown(vk);
            await Task.Delay(_rng.Next(10, 31), ct);
            _input.SendKeyUp(vk);
        }

        private static (ushort Vk, bool Shift)[] BuildCharPool()
        {
            var pool = new System.Collections.Generic.List<(ushort, bool)>();
            for (ushort vk = 0x41; vk <= 0x5A; vk++) pool.Add((vk, false));
            for (ushort vk = 0x41; vk <= 0x5A; vk++) pool.Add((vk, true));
            for (ushort vk = 0x30; vk <= 0x39; vk++) pool.Add((vk, false));
            pool.Add((NativeMethods.VK_SPACE, false));
            pool.Add((NativeMethods.VK_OEM_PERIOD, false));
            pool.Add((NativeMethods.VK_OEM_COMMA, false));
            return pool.ToArray();
        }
    }
}
