using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.DiskCleanup;

public sealed class DiskCleanupService : IDiskCleanupService
{
    private readonly IDiskCleanupVolumeService _volumeService;
    private readonly IStaTaskScheduler _staTaskScheduler;
    private readonly IDiskCleanupAnalyzer _analyzer;
    private readonly IDiskCleanupExecutor _executor;

    public DiskCleanupService(
        IDiskCleanupVolumeService volumeService,
        IStaTaskScheduler staTaskScheduler,
        IDiskCleanupAnalyzer analyzer,
        IDiskCleanupExecutor executor)
    {
        _volumeService = volumeService ?? throw new ArgumentNullException(nameof(volumeService));
        _staTaskScheduler = staTaskScheduler ?? throw new ArgumentNullException(nameof(staTaskScheduler));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public string GetDefaultVolume() => _volumeService.GetDefaultVolume();

    public Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default)
    {
        EnsureWindows();

        var drive = _volumeService.NormalizeVolume(volume);
        return _staTaskScheduler.RunAsync(ct => _analyzer.Analyze(drive, ct), cancellationToken);
    }

    public Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();

        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var drive = _volumeService.NormalizeVolume(volume);
        return _staTaskScheduler.RunAsync(ct => _executor.Clean(drive, items, ct), cancellationToken);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Disk Cleanup integration is only available on Windows.");
        }
    }
}
