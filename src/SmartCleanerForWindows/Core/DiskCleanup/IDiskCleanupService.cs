using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public interface IDiskCleanupService
{
    string GetDefaultVolume();

    Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default);

    Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default);
}
