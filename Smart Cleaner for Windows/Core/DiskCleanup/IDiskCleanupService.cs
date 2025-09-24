using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core;

public interface IDiskCleanupService
{
    bool IsSupported { get; }

    string GetDefaultVolume();

    Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default);

    Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default);
}
