using Smart_Cleaner_for_Windows.Core.LargeFiles;
using Smart_Cleaner_for_Windows.Core.Storage;

namespace Smart_Cleaner_for_Windows.Modules.LargeFiles.ViewModels;

public sealed class LargeFileItemViewModel
{
    public LargeFileItemViewModel(LargeFileEntry entry)
    {
        Path = entry.Path;
        Size = entry.Size;
        ValueFormatting.FormatBytes(entry.Size);
    }

    public string Path { get; }

    public long Size { get; }
}
