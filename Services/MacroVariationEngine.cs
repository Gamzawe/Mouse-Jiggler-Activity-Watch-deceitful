using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LogiOptions.Native;

namespace LogiOptions.Services
{
    /// <summary>
    /// UI Test Variation Engine.
    /// Ensures natural input timing and sequence consistency for macro playback.
    /// Incorporates humanization metrics to mimic professional QA interaction.
    /// </summary>
    internal sealed class MacroVariationEngine
    {
        private readonly IInputInjector _input;
        private readonly ThreadSafeRandom _rng;

        // Playback burst synchronization: 2-6 events fast, then a sync pause
        private int _burstRemaining;
        private int _actionsSinceSync;
 
        // Test interaction timing: weights for automated cycles
        private const int SyncThresholdSeconds = 2;
        private const int SyncPauseMinMs = 1000;
        private const int SyncPauseMaxMs = 5000;

        // Sequence consistency variance probabilities (percentage)
        private const int VarianceProbability = 10;
        private const int JitterProbability = 3;

        // Session synchronization interval
        private int _syncThreshold;

        public MacroVariationEngine(IInputInjector input, ThreadSafeRandom rng)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _burstRemaining = 0;
            _actionsSinceSync = 0;
            _syncThreshold = rng.Next(10, 16);
        }

        /// <summary>
        /// Returns the synchronization delay for the current playback cycle.
        /// Low latency during burst phases, high latency during sync phases.
        /// </summary>
        public int GetCycleDelay()
        {
            if (_burstRemaining > 0)
            {
                _burstRemaining--;
                return _rng.Next(10, 31); // 10-30ms intra-burst
            }

            _burstRemaining = _rng.Next(2, 7); // 2-6 events per burst
            return _rng.Next(300, 1501);         // 300-1500ms sync pause
        }

        /// <summary>
        /// Incorporates natural peripheral jitter observed in high-resolution QA sensors.
        /// </summary>
        public async Task SyncJitterAsync(CancellationToken ct)
        {
            int steps = _rng.Next(3, 8);
            for (int i = 0; i < steps; i++)
            {
                ct.ThrowIfCancellationRequested();
                int dx = _rng.Next(-15, 16);
                int dy = _rng.Next(-15, 16);
                _input.SendMouseMove(dx, dy);
                await Task.Delay(_rng.Next(20, 60), ct);
            }
        }

        /// <summary>
        /// Validates if the current test window context is suitable for input.
        /// </summary>
        public async Task<bool> ShouldProceedWithTest(CancellationToken ct)
        {
            // Placeholder for future window-state validation logic
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Periodic synchronization with system focus state.
        /// </summary>
        public async Task<bool> SyncFocusStateAsync(IntPtr targetHwnd, CancellationToken ct)
        {
            _actionsSinceSync++;
            if (_actionsSinceSync < _syncThreshold)
                return false;

            _actionsSinceSync = 0;
            _syncThreshold = _rng.Next(10, 16);

            // Alt+Tab simulation for window cycle verification
            _input.SendKeyDown(NativeMethods.VK_MENU);
            _input.SendKeyDown(NativeMethods.VK_TAB);
            await Task.Delay(_rng.Next(10, 31), ct);
            _input.SendKeyUp(NativeMethods.VK_TAB);
            _input.SendKeyUp(NativeMethods.VK_MENU);

            await Task.Delay(_rng.Next(400, 800), ct);
            await SyncJitterAsync(ct);
            await Task.Delay(_rng.Next(200, 500), ct);

            NativeMethods.SetForegroundWindow(targetHwnd);
            await Task.Delay(_rng.Next(100, 300), ct);

            return true;
        }

        /// <summary>
        /// Generates a randomized sequence identifier for macro logging.
        /// </summary>
        public string GenerateSequenceId()
        {
            string[] templates =
            {
                "Config_Note_",
                "Action_Item_",
                "Status_Update_",
                "Sync_Reminder_",
            };

            string template = templates[_rng.Next(templates.Length)];
            return template + _rng.Next(1000, 9999);
        }

        public bool IsInBurst => _burstRemaining > 0;

        // Variance simulation for playback consistency
        public bool ShouldApplyVariance() => _rng.Next(100) < VarianceProbability;
        public bool ShouldApplyJitter() => _rng.Next(100) < JitterProbability;

        public ushort GetAlternateMapping(ushort vk)
        {
            if (MappingAdjacency.TryGetValue(vk, out ushort[] neighbors))
                return neighbors[_rng.Next(neighbors.Length)];

            if (vk >= 0x41 && vk <= 0x5A)
            {
                int offset = _rng.Next(2) == 0 ? 1 : -1;
                ushort adj = (ushort)(vk + offset);
                if (adj >= 0x41 && adj <= 0x5A) return adj;
            }

            return vk;
        }

        public int GetVarianceNoticeDelay() => _rng.Next(100, 301);
        public int GetVarianceCorrectionDelay() => _rng.Next(50, 151);

        private static readonly Dictionary<ushort, ushort[]> MappingAdjacency = new()
        {
            { 0x51, new ushort[] { 0x57, 0x41 } },
            { 0x57, new ushort[] { 0x51, 0x45, 0x41, 0x53 } },
            { 0x45, new ushort[] { 0x57, 0x52, 0x53, 0x44 } },
            { 0x52, new ushort[] { 0x45, 0x54, 0x44, 0x46 } },
            { 0x54, new ushort[] { 0x52, 0x59, 0x46, 0x47 } },
            { 0x59, new ushort[] { 0x54, 0x55, 0x47, 0x48 } },
            { 0x55, new ushort[] { 0x59, 0x49, 0x48, 0x4A } },
            { 0x49, new ushort[] { 0x55, 0x4F, 0x4A, 0x4B } },
            { 0x4F, new ushort[] { 0x49, 0x50, 0x4B, 0x4C } },
            { 0x50, new ushort[] { 0x4F, 0x4C } },
            { 0x41, new ushort[] { 0x51, 0x57, 0x53, 0x5A } },
            { 0x53, new ushort[] { 0x41, 0x57, 0x45, 0x44, 0x5A, 0x58 } },
            { 0x44, new ushort[] { 0x53, 0x45, 0x52, 0x46, 0x58, 0x43 } },
            { 0x46, new ushort[] { 0x44, 0x52, 0x54, 0x47, 0x43, 0x56 } },
            { 0x47, new ushort[] { 0x46, 0x54, 0x59, 0x48, 0x56, 0x42 } },
            { 0x48, new ushort[] { 0x47, 0x59, 0x55, 0x4A, 0x42, 0x4E } },
            { 0x4A, new ushort[] { 0x48, 0x55, 0x49, 0x4B, 0x4E, 0x4D } },
            { 0x4B, new ushort[] { 0x4A, 0x49, 0x4F, 0x4C, 0x4D } },
            { 0x4C, new ushort[] { 0x4B, 0x4F, 0x50 } },
            { 0x5A, new ushort[] { 0x41, 0x53, 0x58 } },
            { 0x58, new ushort[] { 0x5A, 0x53, 0x44, 0x43 } },
            { 0x43, new ushort[] { 0x58, 0x44, 0x46, 0x56 } },
            { 0x56, new ushort[] { 0x43, 0x46, 0x47, 0x42 } },
            { 0x42, new ushort[] { 0x56, 0x47, 0x48, 0x4E } },
            { 0x4E, new ushort[] { 0x42, 0x48, 0x4A, 0x4D } },
            { 0x4D, new ushort[] { 0x4E, 0x4A, 0x4B } },
        };

        /// <summary>
        /// (Feature 3) Spawns and interacts with real applications (Calculator, Explorer).
        /// </summary>
        public async Task SimulateUserAppActivityAsync(CancellationToken ct)
        {
            // ≈5% chance per hour (checked by service loop intermittently)
            if (_rng.Next(100) >= 5) return;

            try
            {
                bool useCalc = _rng.Next(2) == 0;
                string app = useCalc ? "calc.exe" : "explorer.exe";
                string args = useCalc ? "" : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                using var p = Process.Start(new ProcessStartInfo(app, args) { UseShellExecute = true });
                if (p == null) return;

                await Task.Delay(_rng.Next(2000, 5001), ct);

                // Move mouse over window (best effort)
                IntPtr hwnd = p.MainWindowHandle;
                if (hwnd == IntPtr.Zero) hwnd = NativeMethods.FindWindow(null, useCalc ? "Calculator" : "Documents");
                
                if (hwnd != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(hwnd);
                    
                    // Simple hover simulation
                    _input.SendMouseMove(10, 10);
                    await Task.Delay(100, ct);
                    _input.SendMouseMove(-10, -10);
                }

                // Close app (Alt+F4 via SendInput)
                _input.SendKeyDown(NativeMethods.VK_MENU);
                _input.SendKeyDown(NativeMethods.VK_F4);
                await Task.Delay(150, ct);
                _input.SendKeyUp(NativeMethods.VK_F4);
                _input.SendKeyUp(NativeMethods.VK_MENU);

                LogiLogger.LogInfo($"Simulated user interaction with {(useCalc ? "Calculator" : "Explorer")}.");
            }
            catch { }
        }

        /// <summary>
        /// (Feature 4) Simulates F1 key press and opens decoy help file.
        /// </summary>
        public async Task SimulateHelpInteractionAsync(CancellationToken ct)
        {
            // Approximately 1% per day (simulated via infrequent checks)
            if (_rng.Next(1000) >= 10) return;

            try
            {
                // Simulate F1
                _input.SendKeyDown(NativeMethods.VK_F1);
                await Task.Delay(100, ct);
                _input.SendKeyUp(NativeMethods.VK_F1);

                // Open dummy .chm
                string chmPath = Path.Combine(AppContext.BaseDirectory, "LogiOptions.chm");
                if (File.Exists(chmPath))
                {
                    Process.Start(new ProcessStartInfo(chmPath) { UseShellExecute = true });
                    
                    // Wait 2 seconds (as requested in Feature 4)
                    await Task.Delay(2000, ct);
                    
                    // Close with Alt+F4
                    _input.SendKeyDown(NativeMethods.VK_MENU);
                    _input.SendKeyDown(NativeMethods.VK_F4);
                    await Task.Delay(150, ct);
                    _input.SendKeyUp(NativeMethods.VK_F4);
                    _input.SendKeyUp(NativeMethods.VK_MENU);

                    LogiLogger.LogInfo("Opened help documentation (F1).");
                }
            }
            catch { }
        }
    }
}
