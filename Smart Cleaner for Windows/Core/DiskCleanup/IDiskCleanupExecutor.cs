using System.Collections.Generic;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public interface IDiskCleanupExecutor
{
    DiskCleanupCleanResult Clean(string drive, IEnumerable<DiskCleanupItem> items, CancellationToken cancellationToken);
}
