using System;
using System.Collections.Generic;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class DirectoryDeletionService(IDirectoryDeleter directoryDeleter) : IDirectoryDeletionService // FIXME: Cannot resolve symbol 'IDirectoryDeleter' && Cannot resolve symbol 'IDirectoryDeletionService'
{
    private readonly IDirectoryDeleter _directoryDeleter = directoryDeleter ?? throw new ArgumentNullException(nameof(directoryDeleter)); // FIXME: Cannot resolve symbol 'IDirectoryDeleter'

    public bool TryDelete(string directory, DirectoryDeletionMode mode, ICollection<DirectoryCleanFailure> failures) // FIXME: Cannot resolve symbol 'DirectoryDeletionMode'
    {
        try
        {
            _directoryDeleter.Delete(directory, mode);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(directory, ex));
            return false;
        }
        catch (Exception ex)
        {
            failures.Add(new DirectoryCleanFailure(directory, ex));
            return false;
        }
    }
}
