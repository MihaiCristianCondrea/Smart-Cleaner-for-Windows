using System;
using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.DiskCleanup;

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

public sealed record DiskCleanupFailure(string Name, string Message)
{
    public DiskCleanupFailure()
        : this(string.Empty, string.Empty)
    {
    }
}
