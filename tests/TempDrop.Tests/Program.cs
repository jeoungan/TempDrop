using System;
using System.IO;
using System.Linq;
using TempDrop.Core;

namespace TempDrop.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            string testParent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TempDropTestData");
            string testRoot = Path.Combine(testParent, "Core." + Guid.NewGuid().ToString("N"));
            try
            {
                RunCoreTests(testRoot);
                if (failures == 0)
                {
                    Console.WriteLine("All TempDrop tests passed.");
                    return 0;
                }

                Console.Error.WriteLine("{0} TempDrop test(s) failed.", failures);
                return 1;
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

        private static void RunCoreTests(string testRoot)
        {
            string dataRoot = Path.Combine(testRoot, "Data");
            string managedFolder = Path.Combine(testRoot, "Managed");
            string sourceFolder = Path.Combine(testRoot, "Source");
            Directory.CreateDirectory(sourceFolder);

            Assert(ManagedFolderGuard.IsSafeManagedFolder(managedFolder), "dedicated test folder is accepted");
            Assert(!ManagedFolderGuard.IsSafeManagedFolder(Path.GetPathRoot(testRoot)), "drive root is blocked");
            Assert(!ManagedFolderGuard.IsSafeManagedFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), "profile root is blocked");
            Assert(!ManagedFolderGuard.IsSafeManagedFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TempDropCandidate")), "AppData descendants are blocked");

            AppConfiguration config = AppConfiguration.CreateDefault();
            config.ManagedFolder = managedFolder;
            config.RetentionDays = 7;
            config.ConsentAccepted = true;
            config.ConsentAcceptedAtUtc = DateTime.UtcNow.ToString("o");
            config.AutomaticCleanupEnabled = false;

            bool overlapBlocked = false;
            try
            {
                AppConfiguration overlapConfig = AppConfiguration.CreateDefault();
                overlapConfig.ManagedFolder = Path.Combine(testRoot, "Overlap");
                overlapConfig.ConsentAccepted = true;
                new TempDropService(Path.Combine(overlapConfig.ManagedFolder, "Data")).SaveConfiguration(overlapConfig);
            }
            catch (InvalidOperationException)
            {
                overlapBlocked = true;
            }
            Assert(overlapBlocked, "managed and configuration folders cannot overlap");

            TempDropService service = new TempDropService(dataRoot);
            service.SaveConfiguration(config);
            Assert(service.HasConfiguration, "configuration is saved");

            DateTime now = DateTime.UtcNow;
            string expiredPath = Path.Combine(managedFolder, "expired.pptx");
            string freshPath = Path.Combine(managedFolder, "fresh.pdf");
            string partialPath = Path.Combine(managedFolder, "downloading.crdownload");
            File.WriteAllText(expiredPath, "expired");
            File.WriteAllText(freshPath, "fresh");
            File.WriteAllText(partialPath, "partial");
            File.SetCreationTimeUtc(expiredPath, now.AddDays(-8));
            File.SetCreationTimeUtc(freshPath, now.AddDays(-1));
            File.SetCreationTimeUtc(partialPath, now.AddDays(-30));

            InventoryItem[] inventory = service.GetInventory(now).ToArray();
            Assert(inventory.Length == 3, "inventory finds all managed files");
            Assert(inventory.Count(delegate(InventoryItem item) { return item.IsExpired; }) == 1, "only completed expired file is eligible");
            Assert(inventory.Single(delegate(InventoryItem item) { return item.Name == "downloading.crdownload"; }).IsPartial, "partial download is protected");

            string sourcePath = Path.Combine(sourceFolder, "custom.docx");
            File.WriteAllText(sourcePath, "custom");
            string importedPath = service.AddFile(sourcePath, 2, false);
            Assert(!File.Exists(sourcePath), "import moves the original file");
            Assert(File.Exists(importedPath), "imported file exists in managed folder");
            InventoryItem imported = service.GetInventory(now).Single(delegate(InventoryItem item) { return item.Name == "custom.docx"; });
            Assert(Math.Abs((imported.ExpiresAtUtc - now).TotalDays - 2) < 0.05, "per-file expiry is applied");

            CleanupResult preview = service.CleanupExpired(now, true);
            Assert(preview.ExpiredCount == 1 && preview.DeletedCount == 0, "dry run reports without deleting");
            Assert(File.Exists(expiredPath), "dry run preserves expired file");

            CleanupResult cleanup = service.CleanupExpired(now, false);
            Assert(cleanup.DeletedCount == 1 && cleanup.Failed.Count == 0, "cleanup deletes one expired file");
            Assert(!File.Exists(expiredPath), "expired file is permanently deleted");
            Assert(File.Exists(freshPath), "fresh file is preserved");
            Assert(File.Exists(partialPath), "partial download is preserved");
            Assert(File.Exists(AppPaths.GetLogPath(dataRoot)), "deletion is logged");

            bool outsideDeleteBlocked = false;
            string outsidePath = Path.Combine(sourceFolder, "outside.txt");
            File.WriteAllText(outsidePath, "outside");
            try
            {
                service.DeleteFileNow(outsidePath);
            }
            catch (InvalidOperationException)
            {
                outsideDeleteBlocked = true;
            }
            Assert(outsideDeleteBlocked && File.Exists(outsidePath), "deletion outside managed folder is blocked");
        }

        private static void Assert(bool condition, string name)
        {
            if (condition)
            {
                Console.WriteLine("PASS: " + name);
                return;
            }

            failures++;
            Console.Error.WriteLine("FAIL: " + name);
        }
    }
}
