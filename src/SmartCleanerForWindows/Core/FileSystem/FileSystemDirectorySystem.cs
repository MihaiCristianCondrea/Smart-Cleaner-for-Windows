using System.Collections.Generic;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

public sealed class FileSystemDirectorySystem : IDirectorySystem // FIXME: Cannot resolve symbol 'IDirectorySystem'
{
    public bool Exists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFileSystemEntries(string path) => Directory.EnumerateFileSystemEntries(path);

    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);
}
