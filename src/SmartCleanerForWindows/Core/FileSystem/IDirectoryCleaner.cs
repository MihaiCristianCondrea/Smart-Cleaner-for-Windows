using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectoryCleaner
{
    DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default);

    Task<DirectoryCleanResult> CleanAsync(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default);
}
