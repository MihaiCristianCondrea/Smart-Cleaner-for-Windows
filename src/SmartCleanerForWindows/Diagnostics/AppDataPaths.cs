using System;
using System.IO;

namespace Smart_Cleaner_for_Windows.Diagnostics;

internal static class AppDataPaths
{
    private const string RootDirectoryName = "SmartCleanerForWindows";

    public static string EnsureBaseDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            RootDirectoryName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetCrashLogPath()
        => Path.Combine(EnsureBaseDirectory(), "crash.log");

    public static string GetEventLogPath()
        => Path.Combine(EnsureBaseDirectory(), "events.log");
}
