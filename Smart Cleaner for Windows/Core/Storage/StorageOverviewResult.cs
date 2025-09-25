using System;
using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core.Storage;

/// <summary>
/// Represents the result of retrieving the current drive usage information.
/// </summary>
public sealed class StorageOverviewResult
{
    public StorageOverviewResult()
        : this(0, Array.Empty<StorageDriveInfo>())
    {
    }

    public StorageOverviewResult(int readyDriveCount, IReadOnlyList<StorageDriveInfo> drives)
    {
        ReadyDriveCount = readyDriveCount;
        Drives = drives;
    }

    /// <summary>
    /// Gets the number of drives that are reported as ready.
    /// </summary>
    public int ReadyDriveCount { get; }

    /// <summary>
    /// Gets the collection of drives that were successfully inspected.
    /// </summary>
    public IReadOnlyList<StorageDriveInfo> Drives { get; }
}
