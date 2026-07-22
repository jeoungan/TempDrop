using System;
using System.Collections.Generic;

namespace TempDrop.Core
{
    public sealed class StateDocument
    {
        public int Version { get; set; }
        public List<StateItem> Items { get; set; }

        public StateDocument()
        {
            Version = 1;
            Items = new List<StateItem>();
        }
    }

    public sealed class StateItem
    {
        public string Path { get; set; }
        public string ExpiresAtUtc { get; set; }
        public string UpdatedAtUtc { get; set; }
    }

    public sealed class InventoryItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public long Length { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsPartial { get; set; }
        public bool IsExpired { get; set; }
    }

    public sealed class CleanupFailure
    {
        public string Path { get; set; }
        public string Error { get; set; }
    }

    public sealed class CleanupResult
    {
        public int ScannedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int DeletedCount { get; set; }
        public bool AlreadyRunning { get; set; }
        public List<CleanupFailure> Failed { get; set; }

        public CleanupResult()
        {
            Failed = new List<CleanupFailure>();
        }
    }
}
