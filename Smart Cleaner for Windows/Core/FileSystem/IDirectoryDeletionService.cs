using System.Collections.Generic;
using Smart_Cleaner_for_Windows.Core;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal interface IDirectoryDeletionService
{
    bool TryDelete(string directory, DirectoryDeletionMode mode, ICollection<DirectoryCleanFailure> failures);
}
