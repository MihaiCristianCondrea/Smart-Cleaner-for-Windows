using System;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Provides helpers for constructing disk cleanup services with the built-in Windows implementations.
/// </summary>
public static class DiskCleanupServiceFactory
{
    /// <summary>
    /// Creates an <see cref="IDiskCleanupService"/> that integrates with the Windows Disk Cleanup handlers.
    /// </summary>
    /// <returns>The service configured with the default platform implementations.</returns>
    public static IDiskCleanupService CreateDefault()
    {
        var volumeService = new DiskCleanupVolumeService();

        if (!OperatingSystem.IsWindows())
        {
            return new NullDiskCleanupService(volumeService);
        }

        return new DiskCleanupService(
            volumeService,
            new StaTaskScheduler(),
            new RegistryDiskCleanupAnalyzer(),
            new RegistryDiskCleanupExecutor());
    }
}
