using System.Collections.Generic;
using Smart_Cleaner_for_Windows.Core;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal interface IEmptyDirectoryDetector
{
    bool IsEmpty(string directory, ICollection<DirectoryCleanFailure> failures);
}
