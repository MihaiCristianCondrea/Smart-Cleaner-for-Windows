using System;

namespace SmartCleanerForWindows.Core.LargeFiles;

public sealed class LargeFileEntry
{
    public LargeFileEntry()
        : this(string.Empty, 0, string.Empty)
    {
    }

    public LargeFileEntry(string path, long size, string type)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Size = size;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        System.IO.Path.GetFileName(path);
        Extension = System.IO.Path.GetExtension(path);
    }

    public string Path { get; }

    public string Extension { get; }

    public long Size { get; }

    public string Type { get; }

    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}
