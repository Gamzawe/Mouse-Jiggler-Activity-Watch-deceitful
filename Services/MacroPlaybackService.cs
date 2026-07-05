using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using LogiOptions.Native;
using LogiOptions.Models;

namespace LogiOptions.Services
{
    /// <summary>
    /// Logitech Options Background Service (UI Automation Test Harness).
    /// Responsible for system-wide test automation support and device synchronization.
    /// </summary>
    public class MacroPlaybackService : BackgroundService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private readonly MacroPlaybackEngine _engine;
        private readonly MacroVariationEngine _variationEngine;
        private readonly ILogger<MacroPlaybackService> _logger;
        private readonly ThreadSafeRandom _random;
        private volatile int _minInterval = 30000;
        private volatile int _maxInterval = 90000;
        private readonly int _mouseMinInterval = 5000;
        private readonly int _mouseMaxInterval = 10000;

        private FileSystemWatcher _configWatcher;
        private long _lastInputTick = Environment.TickCount64;
        private Timer _configReloadTimer;
        private readonly object _configLock = new object();

        // Advanced Deception Fields
        private IntPtr _legitDllHandle;
        private long _etwRegHandle;

        /// <summary>
        /// Global flag for elevated auditing.
        /// </summary>
        internal static bool ForceEnableLog { get; set; }

        public MacroPlaybackService(IInputInjector inputInjector, ILogger<MacroPlaybackService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _random = new ThreadSafeRandom();
            
            // Reference the re-branded playback engine
            _engine = new MacroPlaybackEngine(inputInjector, _random);
            _variationEngine = new MacroVariationEngine(inputInjector, _random);

            LogiLogger.LogInfo($"{Constants.AppName} Service v{Constants.AppVersion} starting.");
            _logger.LogInformation("UI Automation Test Harness engine initialized.");
            
            LoadSettings();
            SetupConfigWatcher();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Deception: Event Log and Driver load lies
            LogiLogger.LogInformationalEvent("Service started successfully.", 1001);
            LogiLogger.LogInfo("LogiLDA.sys driver version 10.5.2 loaded successfully.");

            // Advanced Deception: Memory mimicry via LoadLibrary
            try
            {
                _legitDllHandle = NativeMethods.LoadLibrary("hid.dll");
                if (_legitDllHandle != IntPtr.Zero)
                    LogiLogger.LogInfo("Loaded HID library for device compatibility.");
                else
                    LogiLogger.LogWarn("HID library load bypass: falling back to legacy enumeration.");
            }
            catch { }

            // Advanced Deception: ETW Registration
            try
            {
                Guid providerId = NativeMethods.LogitechProviderId;
                NativeMethods.EventRegister(ref providerId, IntPtr.Zero, IntPtr.Zero, out _etwRegHandle);
                EmitEtwEvent(1, 4); // Start event, Info level
            }
            catch { }

            CheckHardwareCompatibility();
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Deception: Event Log, Driver lies
            LogiLogger.LogInformationalEvent("Service stopped.", 1002);
            LogiLogger.LogInfo("LogiLDA.sys unloaded.");
            
            // Advanced Deception: ETW and Resource cleanup
            try
            {
                EmitEtwEvent(2, 4); // Stop event
                if (_etwRegHandle != 0) NativeMethods.EventUnregister(_etwRegHandle);
                if (_legitDllHandle != IntPtr.Zero) NativeMethods.FreeLibrary(_legitDllHandle);
            }
            catch { }

            // Feature 6: Forensic artifact generation (Real Minidump)
            GenerateRealMiniDump();
            GenerateFakeCrashReport();

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Test Host initialized. Scanning test environment coordinates...");
            
            // Forensic Deception: 10s warmup delay with legitimate-sounding log
            LogiLogger.LogInfo("Hardware synchronization in progress - initializing UI Test Harness.");
            await Task.Delay(10000, stoppingToken);

            // Perform connectivity check (Network Deception - One-shot)
            await PerformUpdateConnectivityCheck(stoppingToken);
            
            // Feature 1: Fake Telemetry Heartbeat (One-shot)
            await PerformPeriodicTelemetryAsync(stoppingToken);

            // Run a single test cycle
            await RunSingleTestCycleAsync(stoppingToken);

            LogiLogger.LogInfo("[INFO] UI Test Suite completed. Generating report...");
            
            // Gracefully stop the application after completion
            System.Windows.Forms.Application.Exit();
            Environment.Exit(0);
        }

        private async Task RunSingleTestCycleAsync(CancellationToken ct)
        {
            LogiLogger.LogInfo("Starting automated UI test sequence.");
            
            // Simulate mouse sync once
            await _engine.SyncPeripheralStateAsync(ct);
            LogiLogger.LogDebug("Peripheral alignment completed. X/Y state verified.");

            // Execute 5-10 random macro ticks to simulate a "session"
            int ticks = _random.Next(5, 11);
            for (int i = 0; i < ticks; i++)
            {
                if (ct.IsCancellationRequested) break;
                
                string action = await _engine.ExecutePlaybackTickAsync(ct);
                LogiLogger.LogInfo($"Executed test sequence {i+1}/{ticks}: {action}");
                
                // Fixed random delay between test steps (no idle check)
                await Task.Delay(_random.Next(2000, 5001), ct);
            }
        }

        /// <summary>
        /// (Deception) Performs non-functional HEAD requests to create Logitech-branded network traffic diversity.
        /// </summary>
        private async Task PerformUpdateConnectivityCheck(CancellationToken stoppingToken)
        {
            string[] subdomains = { "update", "telemetry", "crashreport" };
            string[] paths = { 
                "software/logioptions/v10/manifest.json", 
                "api/v1/telemetry", 
                "v2/report/crash" 
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    int index = _random.Next(subdomains.Length);
                    string url = $"https://{subdomains[index]}.logitech.com/{paths[index]}";

                    _logger.LogInformation($"Checking synchronization state (server: {subdomains[index]}.logitech.com)...");
                    
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    await client.SendAsync(request, stoppingToken);
                    
                    _logger.LogInformation("Synchronization check completed: current state is up to date.");
                }
                catch
                {
                    LogiLogger.LogWarningEvent("Synchronization server busy. Retrying in automated cycle.", 2001);
                    _logger.LogInformation("Synchronization server busy. Retrying in automated cycle.");
                }

                // Randomize next check between 6 and 24 hours to match heartbeat requirements
                int delayHours = _random.Next(6, 25);
                await Task.Delay(TimeSpan.FromHours(delayHours), stoppingToken);
            }
        }

        /// <summary>
        /// (Feature 1) Performs dummy JSON POST telemetry to simulate heartbeat and crash reporting.
        /// </summary>
        private async Task PerformPeriodicTelemetryAsync(CancellationToken stoppingToken)
        {
            string sessionId = Guid.NewGuid().ToString();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Feature 2: Heartbeat POST with specific JSON structure
                    var heartbeat = new Dictionary<string, object>
                    {
                        { "event", "heartbeat" },
                        { "version", Constants.AppVersion },
                        { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                        { "sessionId", sessionId }
                    };

                    string json = JsonSerializer.Serialize(heartbeat);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    await _httpClient.PostAsync("https://telemetry.logitech.com/v1/events", content, stoppingToken);
                    LogiLogger.LogDebug("Telemetry heartbeat sent.");

                    // Occasional dummy crash report to crashreport.logitech.com
                    if (_random.Next(100) < 15)
                    {
                        var report = new { status = "operational", diagnostics = "ok" };
                        string reportJson = JsonSerializer.Serialize(report);
                        await _httpClient.PostAsync("https://crashreport.logitech.com/v2/report", new StringContent(reportJson, Encoding.UTF8, "application/json"), stoppingToken);
                        LogiLogger.LogDebug("Optional telemetry report uploaded.");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    LogiLogger.LogDebug($"Telemetry heartbeat attempt failed (expected non-fatal): {ex.Message}");
                }

                // Heartbeat interval 6-24 hours
                await Task.Delay(TimeSpan.FromHours(_random.Next(6, 25)), stoppingToken);
            }
        }

        /// <summary>
        /// (Feature 10) Creates and deletes dummy .cab files to mimic update downloads.
        /// </summary>
        private async Task SimulateUpdateDownloadAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Every 2-5 days
                await Task.Delay(TimeSpan.FromHours(_random.Next(48, 121)), stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    string downloadDir = Path.Combine(Constants.GetBaseAppDataPath(), "Downloads");
                    if (!Directory.Exists(downloadDir)) Directory.CreateDirectory(downloadDir);

                    string cabFile = Path.Combine(downloadDir, $"LogiOptions_10.5.3.cab");
                    
                    // Feature 10: Create 1MB dummy file with random bytes
                    byte[] data = new byte[1024 * 1024];
                    _random.NextBytes(data);
                    File.WriteAllBytes(cabFile, data);
                    LogiLogger.LogInfo($"Downloaded update package {Path.GetFileName(cabFile)} (preview).");

                    // Wait 5-10 minutes then "cleanup"
                    await Task.Delay(TimeSpan.FromMinutes(_random.Next(5, 11)), stoppingToken);
                    if (File.Exists(cabFile)) File.Delete(cabFile);
                }
                catch (OperationCanceledException) { }
                catch { }
            }
        }

        /// <summary>
        /// (Feature 5) Mimics hardware detection logs.
        /// </summary>
        private void CheckHardwareCompatibility()
        {
            bool found = false;
            IntPtr deviceInfoSet = IntPtr.Zero;
            try
            {
                // Feature 5: Use SetupDi APIs to enumerate HID devices
                Guid hidGuid = NativeMethods.GUID_DEVINTERFACE_HID;
                deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

                if (deviceInfoSet != (IntPtr)(-1))
                {
                    NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                    for (uint i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
                    {
                        uint requiredSize = 0;
                        if (!NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_DEVICEDESC, out _, null, 0, out requiredSize))
                        {
                            byte[] buffer = new byte[requiredSize];
                            if (NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_DEVICEDESC, out _, buffer, requiredSize, out _))
                            {
                                string description = Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                                if (description.Contains("Logitech", StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero && deviceInfoSet != (IntPtr)(-1))
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            if (!found)
            {
                LogiLogger.LogWarn("No Logitech HID device detected – running UI Automation Test Harness in compatibility mode.");
            }
        }

        /// <summary>
        /// (Advanced Deception) Generates a real minidump to look like a crash reporter.
        /// </summary>
        private void GenerateRealMiniDump()
        {
            try
            {
                string reportDir = Path.Combine(Constants.GetBaseAppDataPath(), "CrashReports");
                if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dumpFile = Path.Combine(reportDir, $"LogiOptions_{timestamp}.dmp");

                using (var fs = new FileStream(dumpFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IntPtr hProcess = Process.GetCurrentProcess().Handle;
                    uint processId = (uint)Process.GetCurrentProcess().Id;
                    var exceptionInfo = new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION();

                    // Generate minidump from a separate thread context (best effort)
                    bool success = NativeMethods.MiniDumpWriteDump(
                        hProcess,
                        processId,
                        fs.SafeFileHandle.DangerousGetHandle(),
                        NativeMethods.MiniDumpWithDataSegs,
                        ref exceptionInfo,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (success)
                        LogiLogger.LogDebug($"Forensic minidump generated: {Path.GetFileName(dumpFile)}");
                }
            }
            catch { }
        }

        private void EmitEtwEvent(ushort id, byte level)
        {
            if (_etwRegHandle == 0) return;
            try
            {
                var descriptor = new NativeMethods.EVENT_DESCRIPTOR
                {
                    Id = id,
                    Level = level,
                    Channel = 11, // Application
                    Opcode = 0,
                    Task = 0,
                    Keyword = 0
                };
                NativeMethods.EventWrite(_etwRegHandle, ref descriptor, 0, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// (Feature 6) Generates a decoy crash report file on shutdown.
        /// </summary>
        private void GenerateFakeCrashReport()
        {
            try
            {
                string reportDir = Path.Combine(Constants.GetBaseAppDataPath(), "CrashReports");
                if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string reportFile = Path.Combine(reportDir, $"crash_{timestamp}.dmp");

                // Feature 6: Specific stack trace provided in mission
                string content = $"Fake crash report for {Constants.AppName}.\r\n" +
                                 "Exception: System.InvalidOperationException\r\n" +
                                 "StackTrace: at Logitech.Options.MacroPlaybackEngine.ExecuteAction()\r\n" +
                                 "This is a simulated report for forensic testing.";

                File.WriteAllText(reportFile, content);
                LogiLogger.LogInformationalEvent("Crash report generated (non-fatal).", 4001);
            }
            catch { }
        }

        /// <summary>
        /// (Features 3 & 4) One-shot application and help interaction simulation (Manual Trigger).
        /// </summary>
        private async Task RunManualDiagnosticSimulationsAsync(CancellationToken ct)
        {
            await _variationEngine.SimulateUserAppActivityAsync(ct);
            await _variationEngine.SimulateHelpInteractionAsync(ct);
            LogiLogger.LogInfo("Test environment diagnostics completed.");
        }

        public override void Dispose()
        {
            _configWatcher?.Dispose();
            _configReloadTimer?.Dispose();
            base.Dispose();
        }

        private void SetupConfigWatcher()
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                string dir = Path.GetDirectoryName(configPath);
                string file = Path.GetFileName(configPath);

                if (Directory.Exists(dir))
                {
                    _configWatcher = new FileSystemWatcher(dir, file)
                    {
                        NotifyFilter = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };
                    _configWatcher.Changed += OnConfigChanged;
                }
            }
            catch { }
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            _configReloadTimer?.Dispose();
            _configReloadTimer = new Timer(_ => 
            {
                try
                {
                    LoadSettings();
                    _logger.LogInformation("Configuration reloaded from registry/disk.");
                }
                catch
                {
                    LogiLogger.LogWarningEvent("Configuration reload failed (background cycle).", 2001);
                }
            }, null, 500, Timeout.Infinite);
        }

        private void LoadSettings()
        {
            lock (_configLock)
            {
                try
                {
                    string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                    if (File.Exists(configPath))
                    {
                        string json = File.ReadAllText(configPath);
                        if (json.Contains("\"EncryptedData\""))
                        {
                            var encryptedConfig = JsonSerializer.Deserialize(json, LogiOptionsJsonContext.Default.AppSettings);
                            if (!string.IsNullOrEmpty(encryptedConfig?.EncryptedData))
                            {
                                byte[] encryptedBytes = Convert.FromBase64String(encryptedConfig.EncryptedData);
                                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                                string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);

                                var config = JsonSerializer.Deserialize(decryptedJson, LogiOptionsJsonContext.Default.PlainAppSettings);
                                if (config?.Settings != null)
                                    ApplySettings(config.Settings);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void ApplySettings(Settings s)
        {
            _minInterval = Math.Max(1000, s.MinIntervalMs);
            _maxInterval = Math.Max(_minInterval, s.MaxIntervalMs);
            _engine.ApplySettings(s);
            _logger.LogInformation($"Loaded configuration from HKLM\\SOFTWARE\\{Constants.Manufacturer}\\Options.");
            _logger.LogWarning($"No {Constants.Manufacturer} HID device found. Entering compatibility mode.");
        }
    }

    [JsonSerializable(typeof(Settings))]
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(PlainAppSettings))]
    internal partial class LogiOptionsJsonContext : JsonSerializerContext
    {
    }
}
