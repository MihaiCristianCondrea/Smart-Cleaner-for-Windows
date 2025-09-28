using Smart_Cleaner_for_Windows.Core.LargeFiles;
using Smart_Cleaner_for_Windows.Core.Storage;

namespace Smart_Cleaner_for_Windows.Modules.LargeFiles.ViewModels;

public sealed class LargeFileItemViewModel
{
    public LargeFileItemViewModel(LargeFileEntry entry, string extensionDisplay)
    {
        Entry = entry;
        Path = entry.Path;
        Name = entry.Name;
        Directory = string.IsNullOrEmpty(entry.Directory) ? entry.Path : entry.Directory;
        Size = entry.Size;
        SizeDisplay = ValueFormatting.FormatBytes(entry.Size);
        TypeName = entry.Type;
        ExtensionDisplay = extensionDisplay;
    }

    public LargeFileEntry Entry { get; }

    public string Path { get; }

    public string Name { get; }

    public string Directory { get; }

    public long Size { get; }

    public string SizeDisplay { get; }

    public string TypeName { get; }

    public string ExtensionDisplay { get; }
}
