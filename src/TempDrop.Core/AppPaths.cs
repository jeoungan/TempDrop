using System;
using System.IO;

namespace TempDrop.Core
{
    public static class AppPaths
    {
        public static string GetDataRoot()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TempDrop");
        }

        public static string GetDefaultManagedFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "TempDrop");
        }

        public static string GetConfigPath(string dataRoot)
        {
            return Path.Combine(dataRoot, "config.json");
        }

        public static string GetStatePath(string dataRoot)
        {
            return Path.Combine(dataRoot, "state.json");
        }

        public static string GetLogPath(string dataRoot)
        {
            return Path.Combine(dataRoot, "cleanup.log");
        }
    }
}
