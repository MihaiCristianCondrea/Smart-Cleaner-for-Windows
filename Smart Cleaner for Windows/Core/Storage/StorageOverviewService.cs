using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.Storage;

/// <summary>
/// Retrieves drive usage information using <see cref="DriveInfo"/>.
/// </summary>
public sealed class StorageOverviewService : IStorageOverviewService
{
    public Task<StorageOverviewResult> GetDriveUsageAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetDriveUsageInternal(cancellationToken), cancellationToken);
    }

    private static StorageOverviewResult GetDriveUsageInternal(CancellationToken cancellationToken)
    {
        var readyDrives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.TotalSize > 0)
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var accessible = new List<StorageDriveInfo>(readyDrives.Count);

        foreach (var drive in readyDrives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var totalSize = drive.TotalSize;
                if (totalSize <= 0)
                {
                    continue;
                }

                var freeSpace = drive.TotalFreeSpace;
                var totalValue = (ulong)totalSize;
                var freeValue = freeSpace <= 0 ? 0UL : (ulong)freeSpace;
                if (freeValue > totalValue)
                {
                    freeValue = totalValue;
                }

                accessible.Add(new StorageDriveInfo(drive.Name, drive.VolumeLabel, totalValue, freeValue));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip drives that cannot be inspected.
            }
        }

        return new StorageOverviewResult(readyDrives.Count, accessible);
    }
}
