using System;
using System.Diagnostics;
using System.IO;
using LogiOptions.Models;

namespace LogiOptions.Services
{
    /// <summary>
    /// Centralized logging for Logitech Options.
    /// Manages file logs (branding) and Windows Event Log entries.
    /// </summary>
    internal static class LogiLogger
    {
        private static string GetLogDirectory() => 
            Path.Combine(Constants.GetBaseAppDataPath(), "Logs");

        public static void WriteBrandedLog(string message)
        {
            try
            {
                string logDir = GetLogDirectory();
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                
                string logFile = Path.Combine(logDir, $"{Constants.ProjectName}_{DateTime.Now:yyyy-MM-dd}.log");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(logFile, $"{timestamp} {message}{Environment.NewLine}");
            }
            catch { /* Silent failure for forensics */ }
        }

        public static void WriteEvent(string message, EventLogEntryType type, int eventId)
        {
            try
            {
                if (EventLog.SourceExists(Constants.LogSource))
                {
                    EventLog.WriteEntry(Constants.LogSource, message, type, eventId);
                }
            }
            catch { }
        }

        public static void LogInfo(string message) => WriteBrandedLog($"[INFO] {message}");
        public static void LogWarn(string message) => WriteBrandedLog($"[WARN] {message}");
        public static void LogError(string message) => WriteBrandedLog($"[ERROR] {message}");
        public static void LogDebug(string message) => WriteBrandedLog($"[DEBUG] {message}");
        
        public static void LogInformationalEvent(string message, int eventId) 
            => WriteEvent(message, EventLogEntryType.Information, eventId);
            
        public static void LogWarningEvent(string message, int eventId) 
            => WriteEvent(message, EventLogEntryType.Warning, eventId);
    }
}
