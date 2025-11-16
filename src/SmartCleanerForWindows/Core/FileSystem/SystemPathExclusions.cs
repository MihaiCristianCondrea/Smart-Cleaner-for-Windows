using System;
using System.Collections.Generic;
using System.IO;

namespace SmartCleanerForWindows.Core.FileSystem;

/// <summary>
/// Provides helpers that return directories which should be skipped automatically when
/// scanning the file system. These paths typically map to system-managed locations such
/// as the SRU database or <c>System Volume Information</c> that reject read/write access
/// for standard users and generate noisy audit events.
/// </summary>
internal static class SystemPathExclusions
{
    private static readonly string[] WindowsRelativePaths =
    {
        Path.Combine("System32", "SRU"),
        Path.Combine("System32", "LogFiles", "SRU")
    };

    public static IReadOnlyList<string> GetRestrictedDirectories(string root)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<string>();
        }

        var exclusions = new HashSet<string>(FileSystemPathComparer.PathComparer);

        AddDriveRelativePath(exclusions, root, "$Recycle.Bin");
        AddDriveRelativePath(exclusions, root, "System Volume Information");
        AddWindowsScopedPaths(exclusions);

        return exclusions.Count == 0
            ? Array.Empty<string>()
            : [.. exclusions];
    }

    private static void AddDriveRelativePath(HashSet<string> target, string root, string child)
    {
        if (string.IsNullOrWhiteSpace(child))
        {
            return;
        }

        var drive = GetDriveRoot(root);
        if (string.IsNullOrEmpty(drive) || drive.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return;
        }

        AddNormalized(target, Path.Combine(drive, child));
    }

    private static string? GetDriveRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var normalized = PathUtilities.NormalizeDirectoryPath(path);
            return Path.GetPathRoot(normalized);
        }
        catch
        {
            return null;
        }
    }

    private static void AddWindowsScopedPaths(HashSet<string> target)
    {
        string windowsPath;
        try
        {
            windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }
        catch
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(windowsPath))
        {
            return;
        }

        foreach (var relative in WindowsRelativePaths)
        {
            AddNormalized(target, Path.Combine(windowsPath, relative));
        }
    }

    private static void AddNormalized(HashSet<string> target, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var normalized = PathUtilities.NormalizeDirectoryPath(path);
            target.Add(normalized);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            // Ignore paths that cannot be normalized on the current machine.
        }
    }
}
