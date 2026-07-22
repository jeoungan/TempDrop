using System;
using System.IO;
using System.Windows.Forms;
using TempDrop.Core;

namespace TempDrop
{
    internal static class GuiTestProgram
    {
        [STAThread]
        private static int Main()
        {
            string testParent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TempDropTestData");
            string testRoot = Path.Combine(testParent, "Gui." + Guid.NewGuid().ToString("N"));
            try
            {
                string dataRoot = Path.Combine(testRoot, "Data");
                string managedFolder = Path.Combine(testRoot, "Managed");
                AppConfiguration config = AppConfiguration.CreateDefault();
                config.ManagedFolder = managedFolder;
                config.ConsentAccepted = true;
                config.ConsentAcceptedAtUtc = DateTime.UtcNow.ToString("o");
                config.AutomaticCleanupEnabled = false;

                TempDropService service = new TempDropService(dataRoot);
                service.SaveConfiguration(config);
                File.WriteAllText(Path.Combine(managedFolder, "sample-presentation.pptx"), "sample");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (MainForm form = new MainForm(service))
                using (Timer timer = new Timer())
                {
                    timer.Interval = 500;
                    timer.Tick += delegate
                    {
                        timer.Stop();
                        form.Close();
                    };
                    timer.Start();
                    Application.Run(form);
                }

                Console.WriteLine("TempDrop GUI smoke test passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
                if (Directory.Exists(testParent) && Directory.GetFileSystemEntries(testParent).Length == 0)
                {
                    Directory.Delete(testParent, false);
                }
            }
        }
    }
}
