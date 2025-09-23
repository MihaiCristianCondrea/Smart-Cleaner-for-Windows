using System.Threading;

namespace Smart_Cleaner_for_Windows.Core;

public interface IDirectoryCleaner
{
    DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default);
}
