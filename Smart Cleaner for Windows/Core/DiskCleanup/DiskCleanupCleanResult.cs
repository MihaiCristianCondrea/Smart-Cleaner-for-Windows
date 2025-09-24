using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public sealed class DiskCleanupCleanResult
{
    internal DiskCleanupCleanResult(ulong freed, int successCount, IReadOnlyList<DiskCleanupFailure> failures)
    {
        Freed = freed;
        SuccessCount = successCount;
        Failures = failures;
    }

    public ulong Freed { get; }

    public int SuccessCount { get; }

    public IReadOnlyList<DiskCleanupFailure> Failures { get; }

    public bool HasFailures => Failures.Count > 0;
}

public sealed record DiskCleanupFailure(string Name, string Message);
