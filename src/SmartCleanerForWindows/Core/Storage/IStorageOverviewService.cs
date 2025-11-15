using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.Storage;

/// <summary>
/// Provides access to drive storage usage information.
/// </summary>
public interface IStorageOverviewService
{
    /// <summary>
    /// Retrieves storage usage information for the available drives.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that produces the overview of the current storage usage.</returns>
    Task<StorageOverviewResult> GetDriveUsageAsync(CancellationToken cancellationToken = default);
}
