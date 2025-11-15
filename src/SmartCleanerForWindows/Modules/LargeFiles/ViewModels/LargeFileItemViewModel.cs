using System;
using Smart_Cleaner_for_Windows.Core.Storage;
using IOPath = System.IO.Path;

namespace Smart_Cleaner_for_Windows.Modules.LargeFiles.ViewModels;

public sealed class LargeFileItemViewModel
{
    public LargeFileItemViewModel(
        LargeFileEntry entry, // FIXME: Cannot resolve symbol 'LargeFileEntry'
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
            : IOPath.GetFileName(entry.Path); // FIXME: <html>Ambiguous invocation.<br/>Candidates are:<br/>GetFileName(ReadOnlySpan&lt;char&gt;) : ReadOnlySpan&lt;char&gt;<br/>GetFileName(string?) : string?<br/>all from class Path
        Directory = entry.Directory;
        ExtensionDisplay = string.IsNullOrWhiteSpace(extensionDisplay)
            ? string.Empty
            : extensionDisplay;
        TypeName = entry.Type;
        SizeDisplay = sizeFormatter(Math.Max(0L, entry.Size)); // FIXME: <html>Ambiguous invocation.<br/>Candidates are:<br/>Max(decimal, decimal) : decimal<br/>Max(double, double) : double<br/>Max(float, float) : float<br/>Max(long, long) : long<br/>Max(ulong, ulong) : ulong<br/>all from class Math
    }

    public string Path { get; }

    public long Size { get; }

    public string Name { get; }

    public string Directory { get; }

    public string ExtensionDisplay { get; }

    public string TypeName { get; }

    public string SizeDisplay { get; }
}