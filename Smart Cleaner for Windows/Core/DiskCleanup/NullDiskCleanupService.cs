using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core;

internal sealed class NullDiskCleanupService : IDiskCleanupService
{
    private static readonly IReadOnlyList<DiskCleanupItem> EmptyItems = Array.Empty<DiskCleanupItem>();
    private static readonly DiskCleanupCleanResult EmptyResult = new(0, 0, Array.Empty<DiskCleanupFailure>());
    private readonly IDiskCleanupVolumeService _volumeService;

    public NullDiskCleanupService(IDiskCleanupVolumeService volumeService)
    {
        _volumeService = volumeService ?? throw new ArgumentNullException(nameof(volumeService));
    }

    public bool IsSupported => false;

    public string GetDefaultVolume() => _volumeService.GetDefaultVolume();

    public Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<DiskCleanupItem>>(cancellationToken);
        }

        return Task.FromResult(EmptyItems);
    }

    public Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DiskCleanupCleanResult>(cancellationToken);
        }

        return Task.FromResult(EmptyResult);
    }
}
