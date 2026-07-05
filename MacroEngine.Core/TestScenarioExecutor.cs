using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Runtime.InteropServices;

namespace MacroEngine.Core
{
    public class TestScenarioExecutor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private const int KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [DllImport("kernel32.dll")]
        static extern uint GetTickCount();

        public void Execute(string configJson)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("scenarios", out JsonElement scenariosArray))
                {
                    JsonElement settings = default;
                    if (doc.RootElement.TryGetProperty("settings", out JsonElement setObj))
                    {
                        settings = setObj;
                    }
                    string reportPath = @"C:\ProgramData\Logitech\LogiOptions\Reports\";
                    if (settings.ValueKind != JsonValueKind.Undefined && settings.TryGetProperty("reportPath", out JsonElement rp))
                    {
                        reportPath = rp.GetString() ?? reportPath;
                    }

                    foreach (JsonElement scenario in scenariosArray.EnumerateArray())
                    {
                        string name = scenario.GetProperty("name").GetString() ?? "Unknown";
                        Console.WriteLine($"[INFO] Running automated UI test suite - do not interact with keyboard/mouse during test.");

                        var reportGen = new ReportGenerator(name, reportPath);
                        
                        if (scenario.TryGetProperty("steps", out JsonElement stepsElement))
                        {
                            foreach (JsonElement step in stepsElement.EnumerateArray())
                            {
                                string type = step.GetProperty("type").GetString() ?? "";
                                Console.WriteLine($"[DEBUG] Executing step: {type} (scenario {name}).");
                                
                                var sw = Stopwatch.StartNew();
                                bool success = ExecuteStep(step);
                                sw.Stop();

                                reportGen.AddResult(type, sw.Elapsed.TotalSeconds, success);
                            }
                        }

                        Console.WriteLine($"[INFO] Scenario '{name}' completed successfully. Generating report.");
                        reportGen.Generate();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Test execution failed: {ex.Message}. Running safe fallback.");
                FallbackScenario();
            }
        }

        private void FallbackScenario()
        {
            Console.WriteLine("[INFO] Running default safe fallback scenario.");
            while (true)
            {
                mouse_event(MOUSEEVENTF_MOVE, 1, 1, 0, 0);
                Thread.Sleep(60000);
            }
        }

        private bool ExecuteStep(JsonElement step)
        {
            try
            {
                string type = step.GetProperty("type").GetString() ?? "";
                switch (type)
                {
                    case "waitIdle":
                        uint waitSeconds = step.GetProperty("seconds").GetUInt32();
                        WaitForIdle(waitSeconds);
                        break;
                    case "mouseMove":
                        int dx = step.GetProperty("x").GetInt32();
                        int dy = step.GetProperty("y").GetInt32();
                        int duration = step.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;
                        mouse_event(MOUSEEVENTF_MOVE, unchecked((uint)dx), unchecked((uint)dy), 0, 0);
                        if (duration > 0) Thread.Sleep(duration);
                        break;
                    case "keyPress":
                        string keyStr = step.GetProperty("key").GetString() ?? "A";
                        byte vk = (byte)keyStr.ToUpper()[0];
                        int delay = step.TryGetProperty("delay", out var del) ? del.GetInt32() : 0;
                        keybd_event(vk, 0, 0, UIntPtr.Zero);
                        if (delay > 0) Thread.Sleep(delay);
                        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case "mouseClick":
                        string button = step.TryGetProperty("button", out var btn) ? btn.GetString() : "left";
                        if (button == "left") {
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                            Thread.Sleep(50);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        }
                        break;
                    case "wait":
                        int ms = step.GetProperty("ms").GetInt32();
                        Thread.Sleep(ms);
                        break;
                    case "verifyActiveWindow":
                        string title = step.GetProperty("title").GetString() ?? "";
                        return FindWindow(null, title) != IntPtr.Zero;
                    case "launch":
                        string app = step.GetProperty("app").GetString() ?? "";
                        Process.Start(new ProcessStartInfo { FileName = app, UseShellExecute = true });
                        break;
                    case "typeText":
                        string text = step.GetProperty("text").GetString() ?? "";
                        foreach (char c in text)
                        {
                            byte bVk = (byte)char.ToUpper(c);
                            keybd_event(bVk, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(20);
                            keybd_event(bVk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                            Thread.Sleep(20);
                        }
                        break;
                    case "close":
                        string appClose = step.GetProperty("app").GetString() ?? "";
                        foreach (var proc in Process.GetProcessesByName(appClose.Replace(".exe", "")))
                        {
                            try { proc.CloseMainWindow(); } catch { } 
                        }
                        break;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void WaitForIdle(uint seconds)
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            while (true)
            {
                if (GetLastInputInfo(ref lii))
                {
                    uint elapsed = GetTickCount() - lii.dwTime;
                    if (elapsed >= seconds * 1000)
                    {
                        break;
                    }
                }
                Thread.Sleep(500);
            }
        }
    }
}
