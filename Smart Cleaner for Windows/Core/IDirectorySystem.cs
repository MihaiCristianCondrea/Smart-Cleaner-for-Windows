using System.Collections.Generic;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core;

public interface IDirectorySystem
{
    bool Exists(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFileSystemEntries(string path);

    FileAttributes GetAttributes(string path);
}
