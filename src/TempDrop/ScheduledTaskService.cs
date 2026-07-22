using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace TempDrop
{
    internal sealed class CommandResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
    }

    internal static class ScheduledTaskService
    {
        private const string TaskName = "\\TempDrop\\Cleanup";

        public static CommandResult Create(string executablePath, string dailyRunTime)
        {
            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException("TempDrop executable was not found.", executablePath);
            }

            DateTime parsed;
            if (!DateTime.TryParseExact(dailyRunTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                throw new ArgumentException("Daily run time must use HH:mm format.", "dailyRunTime");
            }

            string taskCommand = string.Format(CultureInfo.InvariantCulture, "\\\"{0}\\\" --cleanup", executablePath);
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "/Create /TN \"{0}\" /SC DAILY /ST {1} /TR \"{2}\" /F",
                TaskName,
                dailyRunTime,
                taskCommand);
            return Run(arguments);
        }

        public static CommandResult Remove()
        {
            return Run(string.Format(CultureInfo.InvariantCulture, "/Delete /TN \"{0}\" /F", TaskName));
        }

        public static bool IsInstalled()
        {
            return Run(string.Format(CultureInfo.InvariantCulture, "/Query /TN \"{0}\"", TaskName)).ExitCode == 0;
        }

        private static CommandResult Run(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();
                return new CommandResult { ExitCode = process.ExitCode, Output = output.Trim() };
            }
        }
    }
}
