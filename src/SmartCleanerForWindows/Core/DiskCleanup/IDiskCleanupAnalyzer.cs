using System.Collections.Generic;
using System.Threading;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public interface IDiskCleanupAnalyzer
{
    IReadOnlyList<DiskCleanupItem> Analyze(string drive, CancellationToken cancellationToken);
}
