using System.Collections.Generic;
using System.IO;

namespace SmartCleanerForWindows.Core.FileSystem;

public interface IDirectorySystem
{
    bool Exists(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFileSystemEntries(string path);

    FileAttributes GetAttributes(string path);
}
