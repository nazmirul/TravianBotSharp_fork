namespace MainCore.Models
{
    public class ChromeSetting
    {
        public required string ProfilePath { get; set; }
        public string? ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string? ProxyUsername { get; set; }
        public string? ProxyPassword { get; set; }
        public string? UserAgent { get; set; }
        public bool IsHeadless { get; set; }

        /// <summary>
        /// When true, launch a real chrome.exe process with a persistent user-data-dir and
        /// remote-debugging-port, then attach via DebuggerAddress (mirrors the Python sniper flow).
        /// Lets a manually-established Google/Gmail login persist across runs and reduces bot detection.
        /// </summary>
        public bool AttachChrome { get; set; }

        /// <summary>
        /// Remote debugging port used in attach mode. 0 = auto-pick a free port (safe for multi-account).
        /// </summary>
        public int DebugPort { get; set; }
    }
}