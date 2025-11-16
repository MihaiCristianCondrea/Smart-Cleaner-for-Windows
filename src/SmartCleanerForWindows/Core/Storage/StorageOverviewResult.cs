using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.Storage;

/// <summary>
/// Represents the result of retrieving the current drive usage information.
/// </summary>
public sealed class StorageOverviewResult(int readyDriveCount, IReadOnlyList<StorageDriveInfo> drives)
{
    /// <summary>
    /// Gets the number of drives that are reported as ready.
    /// </summary>
    public int ReadyDriveCount { get; } = readyDriveCount;

    /// <summary>
    /// Gets the collection of drives that were successfully inspected.
    /// </summary>
    public IReadOnlyList<StorageDriveInfo> Drives { get; } = drives;
}
