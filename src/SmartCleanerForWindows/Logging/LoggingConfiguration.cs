using System;
using System.IO;
using System.Linq;
using Serilog;
using SmartCleanerForWindows.Diagnostics;

namespace SmartCleanerForWindows.Logging;

internal static class LoggingConfiguration
{
    public static void Initialize(string[]? args)
    {
        var logDirectory = AppDataPaths.GetLogsDirectory();
        var logPath = AppDataPaths.GetAppLogPath();

        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("ThreadId", Environment.CurrentManagedThreadId)
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Logging initialized. LogDirectory={LogDirectory}, LogFile={LogFile}, Args={Args}", logDirectory, logPath,
            args?.Any() == true ? string.Join(' ', args) : "(none)");
    }
}
