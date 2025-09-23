namespace Smart_Cleaner_for_Windows.Core;

public interface IDirectoryDeleter
{
    void Delete(string path, DirectoryDeletionMode mode);
}
