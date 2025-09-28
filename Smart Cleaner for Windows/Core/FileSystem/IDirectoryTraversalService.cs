using System.Collections.Generic;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal interface IDirectoryTraversalService
{
    IEnumerable<string> Enumerate(DirectoryTraversalRequest request, CancellationToken cancellationToken);
}
