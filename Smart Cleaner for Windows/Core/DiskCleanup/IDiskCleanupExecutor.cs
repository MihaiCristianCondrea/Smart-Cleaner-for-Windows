using System.Collections.Generic;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core;

public interface IDiskCleanupExecutor
{
    DiskCleanupCleanResult Clean(string drive, IEnumerable<DiskCleanupItem> items, CancellationToken cancellationToken);
}
