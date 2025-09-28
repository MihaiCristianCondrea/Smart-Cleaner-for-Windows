using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

public interface IEmptyDirectoryDetector
{
    bool IsEmpty(string directory, ICollection<DirectoryCleanFailure> failures);
}
