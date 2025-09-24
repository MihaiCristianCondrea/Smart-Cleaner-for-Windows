using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Provides helpers to surface the built-in Windows Disk Cleanup handlers.
/// </summary>
public static class DiskCleanupManager
{
    private static readonly IDiskCleanupService Service = CreateDefaultService();

    /// <summary>
    /// Gets the default volume that should be analyzed (the system drive).
    /// </summary>
    /// <returns>The root path for the system drive.</returns>
    public static string GetDefaultVolume() => Service.GetDefaultVolume();

    /// <summary>
    /// Enumerates the registered Disk Cleanup handlers and returns their estimated savings.
    /// </summary>
    /// <param name="volume">The drive letter (e.g. C:\) that should be analyzed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The list of handlers and their associated metadata.</returns>
    public static Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default)
    {
        return Service.AnalyzeAsync(volume, cancellationToken);
    }

    /// <summary>
    /// Requests the selected Disk Cleanup handlers to delete their unneeded data.
    /// </summary>
    /// <param name="volume">The drive letter (e.g. C:\) that should be cleaned.</param>
    /// <param name="items">The handlers that should be executed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A summary of the clean operation.</returns>
    public static Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default)
    {
        return Service.CleanAsync(volume, items, cancellationToken);
    }

    private static IDiskCleanupService CreateDefaultService()
    {
        var volumeService = new DiskCleanupVolumeService();
        var staTaskScheduler = new StaTaskScheduler();
        var analyzer = new RegistryDiskCleanupAnalyzer();
        var executor = new RegistryDiskCleanupExecutor();
        return new DiskCleanupService(volumeService, staTaskScheduler, analyzer, executor);
    }
}
