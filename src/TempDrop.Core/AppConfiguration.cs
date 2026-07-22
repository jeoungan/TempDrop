using System;

namespace TempDrop.Core
{
    public sealed class AppConfiguration
    {
        public int Version { get; set; }
        public string ManagedFolder { get; set; }
        public int RetentionDays { get; set; }
        public string CleanupMode { get; set; }
        public string DailyRunTime { get; set; }
        public bool AutomaticCleanupEnabled { get; set; }
        public bool ConsentAccepted { get; set; }
        public string ConsentAcceptedAtUtc { get; set; }
        public string[] SkipExtensions { get; set; }

        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration
            {
                Version = 1,
                ManagedFolder = AppPaths.GetDefaultManagedFolder(),
                RetentionDays = 7,
                CleanupMode = "Permanent",
                DailyRunTime = "03:00",
                AutomaticCleanupEnabled = true,
                ConsentAccepted = false,
                ConsentAcceptedAtUtc = null,
                SkipExtensions = new[] { ".crdownload", ".download", ".part", ".partial", ".tmp" }
            };
        }
    }
}
