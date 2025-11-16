namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectoryDeleter
{
    void Delete(string path, DirectoryDeletionMode mode);
}
