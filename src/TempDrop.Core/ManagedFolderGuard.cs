using System;
using System.Collections.Generic;
using System.IO;

namespace TempDrop.Core
{
    public static class ManagedFolderGuard
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A folder path is required.", "path");
            }

            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool IsSafeManagedFolder(string path)
        {
            try
            {
                string fullPath = Normalize(path);
                if (new Uri(fullPath).IsUnc)
                {
                    return false;
                }

                string root = Path.GetPathRoot(fullPath);
                if (string.IsNullOrWhiteSpace(root) ||
                    fullPath.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                foreach (string blockedPath in GetBlockedFolders())
                {
                    if (!string.IsNullOrWhiteSpace(blockedPath) &&
                        fullPath.Equals(Normalize(blockedPath), StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                foreach (string protectedTree in GetProtectedTrees())
                {
                    if (!string.IsNullOrWhiteSpace(protectedTree) && IsAtOrInside(protectedTree, fullPath))
                    {
                        return false;
                    }
                }

                string relative = fullPath.Substring(root.Length);
                string[] segments = relative.Split(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries);
                return segments.Length >= 2;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsInsideManagedFolder(string managedFolder, string candidatePath)
        {
            try
            {
                string managed = Normalize(managedFolder) + Path.DirectorySeparatorChar;
                string candidate = Normalize(candidatePath) + Path.DirectorySeparatorChar;
                return candidate.StartsWith(managed, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAtOrInside(string rootPath, string candidatePath)
        {
            string root = Normalize(rootPath);
            string candidate = Normalize(candidatePath);
            return candidate.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                   (candidate + Path.DirectorySeparatorChar).StartsWith(
                       root + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> GetBlockedFolders()
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return profile;
            yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            yield return Path.Combine(profile, "Downloads");
        }

        private static IEnumerable<string> GetProtectedTrees()
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            yield return Environment.GetEnvironmentVariable("WINDIR");
            yield return Environment.SystemDirectory;
            yield return Environment.GetEnvironmentVariable("OneDrive");
            yield return Environment.GetEnvironmentVariable("OneDriveCommercial");
            yield return Environment.GetEnvironmentVariable("OneDriveConsumer");
        }
    }
}
