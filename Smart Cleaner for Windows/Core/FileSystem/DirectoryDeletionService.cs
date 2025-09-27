using System;
using System.Collections.Generic;
using System.IO;
using Smart_Cleaner_for_Windows.Core;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class DirectoryDeletionService : IDirectoryDeletionService
{
    private readonly IDirectoryDeleter _directoryDeleter;

    public DirectoryDeletionService(IDirectoryDeleter directoryDeleter)
    {
        _directoryDeleter = directoryDeleter ?? throw new ArgumentNullException(nameof(directoryDeleter));
    }

    public bool TryDelete(string directory, DirectoryDeletionMode mode, ICollection<DirectoryCleanFailure> failures)
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
