using System.IO;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal static class PathUtilities
{
    public static string NormalizeDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
}
