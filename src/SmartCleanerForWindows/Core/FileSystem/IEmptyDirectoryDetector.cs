using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.FileSystem;

public interface IEmptyDirectoryDetector
{
    bool IsEmpty(string directory, ICollection<DirectoryCleanFailure> failures);
}
