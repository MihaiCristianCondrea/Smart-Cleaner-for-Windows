namespace SmartCleanerForWindows.Core.FileSystem;

/// <summary>
/// Provides helpers for constructing <see cref="DirectoryCleaner"/> instances with the built-in file system services.
/// </summary>
public static class DirectoryCleanerFactory
{
    /// <summary>
    /// Creates an <see cref="IDirectoryCleaner"/> wired up with the default Windows file system abstractions.
    /// </summary>
    public static IDirectoryCleaner CreateDefault()
    {
        var directorySystem = new FileSystemDirectorySystem();
        var traversalService = new DirectoryTraversalService(directorySystem);
        var emptyDirectoryDetector = new EmptyDirectoryDetector(directorySystem);
        var directoryDeleter = new FileSystemDirectoryDeleter();
        var directoryDeletionService = new DirectoryDeletionService(directoryDeleter);

        return new DirectoryCleaner(
            directorySystem,
            traversalService,
            emptyDirectoryDetector,
            directoryDeletionService);
    }
}
