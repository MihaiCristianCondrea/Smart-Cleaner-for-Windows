using System.Collections.Generic;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public interface IDiskCleanupAnalyzer
{
    IReadOnlyList<DiskCleanupItem> Analyze(string drive, CancellationToken cancellationToken);
}
