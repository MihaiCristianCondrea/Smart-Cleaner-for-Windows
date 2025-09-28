using System;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal static class FileSystemPathComparer
{
    public static bool IgnoreCase { get; } = OperatingSystem.IsWindows();

    public static StringComparer PathComparer { get; } = IgnoreCase
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
