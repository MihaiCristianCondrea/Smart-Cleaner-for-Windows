using System;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileEntry
{
    public LargeFileEntry(string path, long size, string type)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Size = size;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = System.IO.Path.GetFileName(path);
        Extension = System.IO.Path.GetExtension(path);
    }

    public string Path { get; }

    public string Name { get; }

    public string Extension { get; }

    public long Size { get; }

    public string Type { get; }

    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}
