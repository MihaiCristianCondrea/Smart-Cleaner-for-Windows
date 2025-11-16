using System.Collections.Generic;
using System.IO;

namespace SmartCleanerForWindows.Core.FileSystem;

public sealed class FileSystemDirectorySystem : IDirectorySystem
{
    public bool Exists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFileSystemEntries(string path) => Directory.EnumerateFileSystemEntries(path);

    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);
}
