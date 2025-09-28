using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

public interface IDirectoryDeletionService
{
    bool TryDelete(string directory, DirectoryDeletionMode mode, ICollection<DirectoryCleanFailure> failures);
}
