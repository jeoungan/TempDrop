using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TempDrop.Core;

namespace TempDrop
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            TempDropService service = new TempDropService(AppPaths.GetDataRoot());
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

            if (command == "--cleanup")
            {
                return RunCleanup(service);
            }
            if (command == "--remove-schedule")
            {
                return ScheduledTaskService.IsInstalled() ? ScheduledTaskService.Remove().ExitCode : 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs eventArgs)
            {
                MessageBox.Show(eventArgs.Exception.Message, "TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                AppConfiguration config = null;
                if (service.HasConfiguration)
                {
                    try
                    {
                        config = service.LoadConfiguration();
                    }
                    catch
                    {
                        config = null;
                    }
                }

                if (config == null || !config.ConsentAccepted)
                {
                    using (SetupForm setup = new SetupForm(service, config))
                    {
                        if (setup.ShowDialog() != DialogResult.OK)
                        {
                            return 0;
                        }
                    }
                }

                config = service.LoadConfiguration();
                if (config.AutomaticCleanupEnabled)
                {
                    try
                    {
                        service.CleanupExpired(DateTime.UtcNow, false);
                    }
                    catch (Exception ex)
                    {
                        service.WriteDiagnosticLog("StartupCleanupError", ex.Message);
                    }
                }

                Application.Run(new MainForm(service));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TempDrop could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static int RunCleanup(TempDropService service)
        {
            try
            {
                if (!service.HasConfiguration)
                {
                    return 0;
                }
                AppConfiguration config = service.LoadConfiguration();
                if (!config.AutomaticCleanupEnabled || !config.ConsentAccepted)
                {
                    return 0;
                }
                CleanupResult result = service.CleanupExpired(DateTime.UtcNow, false);
                return result.Failed.Count == 0 ? 0 : 2;
            }
            catch (Exception ex)
            {
                try
                {
                    service.WriteDiagnosticLog("ScheduledCleanupError", ex.ToString());
                }
                catch
                {
                }
                return 1;
            }
        }
    }
}
