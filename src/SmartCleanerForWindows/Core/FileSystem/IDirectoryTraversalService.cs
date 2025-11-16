using System.Collections.Generic;
using System.Threading;

namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectoryTraversalService
{
    IEnumerable<string> Enumerate(DirectoryTraversalRequest request, CancellationToken cancellationToken);
}
