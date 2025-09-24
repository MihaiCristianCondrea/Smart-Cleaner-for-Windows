using System.Collections.Generic;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core;

public interface IDiskCleanupAnalyzer
{
    IReadOnlyList<DiskCleanupItem> Analyze(string drive, CancellationToken cancellationToken);
}
