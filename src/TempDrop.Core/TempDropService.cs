using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace TempDrop.Core
{
    public sealed class TempDropService
    {
        private readonly string dataRoot;

        public TempDropService(string dataRoot)
        {
            this.dataRoot = ManagedFolderGuard.Normalize(dataRoot);
        }

        public string DataRoot
        {
            get { return dataRoot; }
        }

        public bool HasConfiguration
        {
            get { return File.Exists(AppPaths.GetConfigPath(dataRoot)); }
        }

        public AppConfiguration LoadConfiguration()
        {
            AppConfiguration config = JsonStore.Read<AppConfiguration>(AppPaths.GetConfigPath(dataRoot));
            if (config == null)
            {
                throw new InvalidOperationException("TempDrop has not been configured yet.");
            }

            ValidateConfiguration(config);
            return config;
        }

        public void SaveConfiguration(AppConfiguration config)
        {
            ValidateConfiguration(config);
            Directory.CreateDirectory(dataRoot);
            Directory.CreateDirectory(ManagedFolderGuard.Normalize(config.ManagedFolder));
            JsonStore.WriteAtomic(AppPaths.GetConfigPath(dataRoot), config);

            if (!File.Exists(AppPaths.GetStatePath(dataRoot)))
            {
                JsonStore.WriteAtomic(AppPaths.GetStatePath(dataRoot), new StateDocument());
            }
        }

        public IList<InventoryItem> GetInventory(DateTime nowUtc)
        {
            AppConfiguration config = LoadConfiguration();
            string managedFolder = ManagedFolderGuard.Normalize(config.ManagedFolder);
            Directory.CreateDirectory(managedFolder);

            StateDocument state = LoadState();
            Dictionary<string, StateItem> stateMap = state.Items
                .Where(delegate(StateItem item) { return item != null && !string.IsNullOrWhiteSpace(item.Path); })
                .GroupBy(delegate(StateItem item) { return ManagedFolderGuard.Normalize(item.Path); }, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(delegate(IGrouping<string, StateItem> group) { return group.Key; }, delegate(IGrouping<string, StateItem> group) { return group.Last(); }, StringComparer.OrdinalIgnoreCase);

            HashSet<string> partialExtensions = new HashSet<string>(
                config.SkipExtensions ?? new string[0],
                StringComparer.OrdinalIgnoreCase);

            List<InventoryItem> result = new List<InventoryItem>();
            foreach (string path in EnumerateSafeFiles(managedFolder))
            {
                try
                {
                    FileInfo file = new FileInfo(path);
                    if (!file.Exists || IsReparsePoint(file.Attributes))
                    {
                        continue;
                    }

                    StateItem customState;
                    DateTime expiresAtUtc;
                    if (stateMap.TryGetValue(file.FullName, out customState))
                    {
                        expiresAtUtc = ParseUtc(customState.ExpiresAtUtc);
                    }
                    else
                    {
                        expiresAtUtc = file.CreationTimeUtc.AddDays(config.RetentionDays);
                    }

                    bool isPartial = partialExtensions.Contains(file.Extension);
                    result.Add(new InventoryItem
                    {
                        Name = file.Name,
                        FullName = file.FullName,
                        Length = file.Length,
                        CreatedAtUtc = file.CreationTimeUtc,
                        ExpiresAtUtc = expiresAtUtc,
                        IsPartial = isPartial,
                        IsExpired = !isPartial && expiresAtUtc <= nowUtc.ToUniversalTime()
                    });
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return result.OrderBy(delegate(InventoryItem item) { return item.ExpiresAtUtc; }).ToList();
        }

        public string AddFile(string sourcePath, int retentionDays, bool copy)
        {
            if (retentionDays < 1 || retentionDays > 365)
            {
                throw new ArgumentOutOfRangeException("retentionDays", "Retention must be between 1 and 365 days.");
            }

            string source = ManagedFolderGuard.Normalize(sourcePath);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("The selected file does not exist.", source);
            }

            FileInfo sourceInfo = new FileInfo(source);
            if (IsReparsePoint(sourceInfo.Attributes))
            {
                throw new InvalidOperationException("Links and reparse points cannot be imported.");
            }

            AppConfiguration config = LoadConfiguration();
            string managedFolder = ManagedFolderGuard.Normalize(config.ManagedFolder);
            Directory.CreateDirectory(managedFolder);
            string destination = GetUniqueDestination(managedFolder, sourceInfo.Name);

            if (copy)
            {
                File.Copy(source, destination, false);
            }
            else
            {
                File.Move(source, destination);
            }

            SetExpiry(destination, DateTime.UtcNow.AddDays(retentionDays));
            return destination;
        }

        public void SetExpiry(string path, DateTime expiresAtUtc)
        {
            AppConfiguration config = LoadConfiguration();
            string fullPath = ManagedFolderGuard.Normalize(path);
            if (!ManagedFolderGuard.IsInsideManagedFolder(config.ManagedFolder, fullPath))
            {
                throw new InvalidOperationException("Expiry can only be set for files inside the managed folder.");
            }

            StateDocument state = LoadState();
            state.Items.RemoveAll(delegate(StateItem item)
            {
                return item != null && string.Equals(
                    ManagedFolderGuard.Normalize(item.Path),
                    fullPath,
                    StringComparison.OrdinalIgnoreCase);
            });
            state.Items.Add(new StateItem
            {
                Path = fullPath,
                ExpiresAtUtc = expiresAtUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                UpdatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            });
            SaveState(state);
        }

        public CleanupResult CleanupExpired(DateTime nowUtc, bool dryRun)
        {
            CleanupResult result = new CleanupResult();
            bool lockTaken = false;
            using (Mutex cleanupMutex = new Mutex(false, "Local\\TempDrop.Cleanup"))
            {
                try
                {
                    try
                    {
                        lockTaken = cleanupMutex.WaitOne(0, false);
                    }
                    catch (AbandonedMutexException)
                    {
                        lockTaken = true;
                    }

                    if (!lockTaken)
                    {
                        result.AlreadyRunning = true;
                        return result;
                    }

                    AppConfiguration config = LoadConfiguration();
                    IList<InventoryItem> inventory = GetInventory(nowUtc);
                    result.ScannedCount = inventory.Count;
                    result.ExpiredCount = inventory.Count(delegate(InventoryItem item) { return item.IsExpired; });

                    foreach (InventoryItem item in inventory.Where(delegate(InventoryItem value) { return value.IsExpired; }))
                    {
                        if (dryRun)
                        {
                            continue;
                        }

                        try
                        {
                            DeleteManagedFile(config, item.FullName, "PermanentDelete", item.ExpiresAtUtc);
                            result.DeletedCount++;
                        }
                        catch (Exception ex)
                        {
                            result.Failed.Add(new CleanupFailure { Path = item.FullName, Error = ex.Message });
                        }
                    }

                    if (!dryRun)
                    {
                        PurgeMissingStateItems();
                        RemoveEmptyDirectories(config.ManagedFolder);
                    }

                    return result;
                }
                finally
                {
                    if (lockTaken)
                    {
                        cleanupMutex.ReleaseMutex();
                    }
                }
            }
        }

        public void DeleteFileNow(string path)
        {
            AppConfiguration config = LoadConfiguration();
            DeleteManagedFile(config, path, "ManualPermanentDelete", null);
            PurgeMissingStateItems();
        }

        public void WriteDiagnosticLog(string action, string message)
        {
            AppendLog(new
            {
                TimestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Action = action,
                Message = message
            });
        }

        private void ValidateConfiguration(AppConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (!ManagedFolderGuard.IsSafeManagedFolder(config.ManagedFolder))
            {
                throw new InvalidOperationException("The managed folder failed TempDrop's safety checks.");
            }
            string managedFolder = ManagedFolderGuard.Normalize(config.ManagedFolder);
            if (ManagedFolderGuard.IsInsideManagedFolder(dataRoot, managedFolder) ||
                ManagedFolderGuard.IsInsideManagedFolder(managedFolder, dataRoot))
            {
                throw new InvalidOperationException("The managed folder cannot overlap TempDrop's configuration folder.");
            }
            if (config.RetentionDays < 1 || config.RetentionDays > 365)
            {
                throw new InvalidOperationException("Retention must be between 1 and 365 days.");
            }
            if (!string.Equals(config.CleanupMode, "Permanent", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unsupported cleanup mode.");
            }
            if (!config.ConsentAccepted)
            {
                throw new InvalidOperationException("Permanent-deletion consent has not been accepted.");
            }
        }

        private StateDocument LoadState()
        {
            StateDocument state = JsonStore.Read<StateDocument>(AppPaths.GetStatePath(dataRoot));
            if (state == null)
            {
                state = new StateDocument();
            }
            if (state.Items == null)
            {
                state.Items = new List<StateItem>();
            }
            return state;
        }

        private void SaveState(StateDocument state)
        {
            JsonStore.WriteAtomic(AppPaths.GetStatePath(dataRoot), state);
        }

        private void DeleteManagedFile(AppConfiguration config, string path, string action, DateTime? expiresAtUtc)
        {
            string fullPath = ManagedFolderGuard.Normalize(path);
            if (!ManagedFolderGuard.IsInsideManagedFolder(config.ManagedFolder, fullPath))
            {
                throw new InvalidOperationException("Refusing to delete a file outside the managed folder.");
            }
            if (!File.Exists(fullPath))
            {
                return;
            }

            FileInfo file = new FileInfo(fullPath);
            if (IsReparsePoint(file.Attributes))
            {
                throw new InvalidOperationException("Refusing to delete a link or reparse point.");
            }

            long size = file.Length;
            File.Delete(fullPath);
            AppendLog(new
            {
                TimestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Action = action,
                Path = fullPath,
                SizeBytes = size,
                ExpiredAtUtc = expiresAtUtc.HasValue
                    ? expiresAtUtc.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                    : null
            });
        }

        private IEnumerable<string> EnumerateSafeFiles(string managedFolder)
        {
            List<string> files = new List<string>();
            Stack<string> pending = new Stack<string>();
            pending.Push(managedFolder);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                DirectoryInfo directoryInfo;
                try
                {
                    directoryInfo = new DirectoryInfo(directory);
                    if (!directoryInfo.Exists || IsReparsePoint(directoryInfo.Attributes))
                    {
                        continue;
                    }

                    foreach (FileInfo file in directoryInfo.GetFiles())
                    {
                        if (!IsReparsePoint(file.Attributes) &&
                            ManagedFolderGuard.IsInsideManagedFolder(managedFolder, file.FullName))
                        {
                            files.Add(file.FullName);
                        }
                    }

                    foreach (DirectoryInfo child in directoryInfo.GetDirectories())
                    {
                        if (!IsReparsePoint(child.Attributes) &&
                            ManagedFolderGuard.IsInsideManagedFolder(managedFolder, child.FullName))
                        {
                            pending.Push(child.FullName);
                        }
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            return files;
        }

        private void PurgeMissingStateItems()
        {
            StateDocument state = LoadState();
            state.Items.RemoveAll(delegate(StateItem item)
            {
                return item == null || string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path);
            });
            SaveState(state);
        }

        private static void RemoveEmptyDirectories(string managedFolder)
        {
            string root = ManagedFolderGuard.Normalize(managedFolder);
            List<string> directories = new List<string>();
            Stack<string> pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                try
                {
                    DirectoryInfo info = new DirectoryInfo(current);
                    if (!info.Exists || IsReparsePoint(info.Attributes))
                    {
                        continue;
                    }
                    foreach (DirectoryInfo child in info.GetDirectories())
                    {
                        if (!IsReparsePoint(child.Attributes) &&
                            ManagedFolderGuard.IsInsideManagedFolder(root, child.FullName))
                        {
                            pending.Push(child.FullName);
                        }
                    }
                    if (!current.Equals(root, StringComparison.OrdinalIgnoreCase))
                    {
                        directories.Add(current);
                    }
                }
                catch
                {
                }
            }

            foreach (string directory in directories.OrderByDescending(delegate(string value) { return value.Length; }))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory, false);
                    }
                }
                catch
                {
                }
            }
        }

        private static bool IsReparsePoint(FileAttributes attributes)
        {
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        private static string GetUniqueDestination(string folder, string name)
        {
            string destination = Path.Combine(folder, name);
            string baseName = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            int counter = 1;
            while (File.Exists(destination))
            {
                destination = Path.Combine(folder, string.Format(CultureInfo.InvariantCulture, "{0} ({1}){2}", baseName, counter, extension));
                counter++;
            }
            return destination;
        }

        private static DateTime ParseUtc(string value)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
        }

        private void AppendLog(object entry)
        {
            Directory.CreateDirectory(dataRoot);
            File.AppendAllText(
                AppPaths.GetLogPath(dataRoot),
                JsonStore.Serialize(entry) + Environment.NewLine,
                new System.Text.UTF8Encoding(false));
        }
    }
}
