using System;
using System.Collections.Generic;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class EmptyDirectoryDetector(IDirectorySystem directorySystem) : IEmptyDirectoryDetector // FIXME: Cannot resolve symbol 'IDirectorySystem' && Cannot resolve symbol 'IEmptyDirectoryDetector'
{
    private readonly IDirectorySystem _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem)); // FIXME: Cannot resolve symbol 'IDirectorySystem'

    public bool IsEmpty(string directory, ICollection<DirectoryCleanFailure> failures)
    {
        try
        {
            using var enumerator = _directorySystem.EnumerateFileSystemEntries(directory).GetEnumerator();
            return !enumerator.MoveNext();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(directory, ex));
            return false;
        }
    }
}
