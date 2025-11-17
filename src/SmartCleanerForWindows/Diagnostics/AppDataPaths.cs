using System;
using System.IO;

namespace SmartCleanerForWindows.Diagnostics;

internal static class AppDataPaths
{
    private const string RootDirectoryName = "SmartCleanerForWindows";
    private const string LogsDirectoryName = "Logs";

    private static string EnsureBaseDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            RootDirectoryName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetCrashLogPath()
        => Path.Combine(EnsureLogsDirectory(), "crash.log");

    public static string GetEventLogPath()
        => Path.Combine(EnsureLogsDirectory(), "events.log");

    public static string GetStartupLogPath()
        => Path.Combine(EnsureLogsDirectory(), "startup.log");

    public static string GetLogsDirectory()
        => EnsureLogsDirectory();

    public static string GetAppLogPath()
        => Path.Combine(EnsureLogsDirectory(), "app.log");

    private static string EnsureLogsDirectory()
    {
        var logsDirectory = Path.Combine(EnsureBaseDirectory(), LogsDirectoryName);
        Directory.CreateDirectory(logsDirectory);
        return logsDirectory;
    }
}
