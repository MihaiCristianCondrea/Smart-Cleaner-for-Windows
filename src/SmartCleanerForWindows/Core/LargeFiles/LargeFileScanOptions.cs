using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.LargeFiles;

public sealed class LargeFileScanOptions
{
    public bool IncludeSubdirectories { get; init; } = true;

    public bool SkipReparsePoints { get; init; } = true;

    public int MaxResults { get; init; } = 100;

    public IReadOnlyCollection<string>? ExcludedFullPaths { get; init; }

    public IReadOnlyCollection<string>? ExcludedNamePatterns { get; init; }
}
