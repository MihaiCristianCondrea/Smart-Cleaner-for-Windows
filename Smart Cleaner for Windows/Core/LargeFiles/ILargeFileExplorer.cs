using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public interface ILargeFileExplorer
{
    LargeFileScanResult Scan(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default);

    Task<LargeFileScanResult> ScanAsync(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default);
}
