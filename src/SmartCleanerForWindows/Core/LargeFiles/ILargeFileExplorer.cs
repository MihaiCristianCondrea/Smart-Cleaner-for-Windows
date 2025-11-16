using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Core.LargeFiles;

public interface ILargeFileExplorer
{
    LargeFileScanResult Scan(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default);

    Task<LargeFileScanResult> ScanAsync(string root, LargeFileScanOptions? options = null, CancellationToken cancellationToken = default);
}
