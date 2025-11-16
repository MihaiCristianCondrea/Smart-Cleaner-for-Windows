using System.Collections.Generic;
using System.Threading;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public interface IDiskCleanupExecutor
{
    DiskCleanupCleanResult Clean(string drive, IEnumerable<DiskCleanupItem> items, CancellationToken cancellationToken);
}
