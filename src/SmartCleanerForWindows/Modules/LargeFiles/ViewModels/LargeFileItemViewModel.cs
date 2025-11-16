using System;
using SmartCleanerForWindows.Core.LargeFiles;
using SmartCleanerForWindows.Core.Storage;
using IOPath = System.IO.Path;

namespace SmartCleanerForWindows.Modules.LargeFiles.ViewModels;

public sealed class LargeFileItemViewModel
{
    public LargeFileItemViewModel(
        LargeFileEntry entry,
        string extensionDisplay,
        Func<ulong, string>? formatBytes = null)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var sizeFormatter = formatBytes ?? ValueFormatting.FormatBytes;

        Path = entry.Path;
        Size = entry.Size;
        Name = string.IsNullOrWhiteSpace(entry.Path)
            ? string.Empty
            : IOPath.GetFileName(entry.Path);
        Directory = entry.Directory;
        ExtensionDisplay = string.IsNullOrWhiteSpace(extensionDisplay)
            ? string.Empty
            : extensionDisplay;
        TypeName = entry.Type;
        SizeDisplay = sizeFormatter((ulong)Math.Max(0L, entry.Size));
    }

    public string Path { get; }

    public long Size { get; }

    public string Name { get; }

    public string Directory { get; }

    public string ExtensionDisplay { get; }

    public string TypeName { get; }

    public string SizeDisplay { get; }
}