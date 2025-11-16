using System;

namespace SmartCleanerForWindows.Core.FileSystem;

internal static class FileSystemPathComparer
{
    public static bool IgnoreCase { get; } = OperatingSystem.IsWindows();

    public static StringComparer PathComparer { get; } = IgnoreCase
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
