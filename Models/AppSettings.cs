using System.Collections.Generic;

namespace LogiOptions.Models
{
    public static class Constants
    {
        public const string AppName = "Logitech Options";
        public const string AppVersion = "10.5.2";
        public const string Manufacturer = "Logitech";
        public const string ProjectName = "LogiOptions";
        public const string ServiceName = "LogiOptionsSvc";
        public const string LogSource = "Logitech Options";
        
        public static string GetBaseAppDataPath() => 
            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), Manufacturer, ProjectName);
    }

    public class PlainAppSettings { public Settings Settings { get; set; } = new Settings(); }
    public class AppSettings { public string EncryptedData { get; set; } }

    public class Settings
    {
        public int MinIntervalMs { get; set; } = 30000;
        public int MaxIntervalMs { get; set; } = 90000;

        /// <summary>
        /// Weights controlling how often each action fires.
        /// Default: scroll 45%, key press 27%, page scroll 27% (normalized from 25/15/15).
        /// Mouse movement runs on an independent timer and is not weighted.
        /// </summary>
        public ActionWeights Weights { get; set; } = new ActionWeights();

        /// <summary>
        /// Hex VK codes (e.g. "0x91") for random single key-press simulation.
        /// Defaults to Scroll Lock if empty.
        /// </summary>
        public List<string> VirtualKeyPool { get; set; } = new List<string>();

        /// <summary>
        /// When false (default), CSV audit log is not written to disk.
        /// Events still accumulate in the in-memory ring buffer.
        /// Can be overridden via --enable-log CLI flag.
        /// </summary>
        public bool EnableCsvLogging { get; set; } = false;
    }

    public class ActionWeights
    {

        public int MouseScroll { get; set; } = 25;
        public int KeyPress { get; set; } = 15;
        public int PageScroll { get; set; } = 15;
    }
}
