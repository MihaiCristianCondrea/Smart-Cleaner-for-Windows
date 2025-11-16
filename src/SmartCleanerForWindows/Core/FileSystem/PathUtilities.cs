using System.IO;

namespace SmartCleanerForWindows.Core.FileSystem;

internal static class PathUtilities
{
    public static string NormalizeDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
}
