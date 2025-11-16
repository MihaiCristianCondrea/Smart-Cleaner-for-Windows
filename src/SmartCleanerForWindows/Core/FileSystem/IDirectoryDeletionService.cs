using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectoryDeletionService
{
    bool TryDelete(string directory, DirectoryDeletionMode mode, ICollection<DirectoryCleanFailure> failures);
}
