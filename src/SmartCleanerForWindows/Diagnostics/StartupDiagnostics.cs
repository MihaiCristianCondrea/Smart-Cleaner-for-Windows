using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UnhandledExceptionEventArgs = System.UnhandledExceptionEventArgs;

namespace SmartCleanerForWindows.Diagnostics;

internal static class StartupDiagnostics
{
    private const int FirstChanceSampleLimit = 20;
    private static int s_initialized; // FIXME: Name 's_initialized' does not match rule 'Static fields (private)'. Suggested name is '_sInitialized'.
    private static int s_firstChanceCount; // FIXME: Name 's_firstChanceCount' does not match rule 'Static fields (private)'. Suggested name is '_sFirstChanceCount'.
    private static readonly Lock SyncRoot = new();

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref s_initialized, 1) == 1)
        {
            return;
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        WriteLine($"[Info] Startup diagnostics initialized (ProcessId={Environment.ProcessId}, Version={Environment.Version}).");
    }

    public static void AttachToApplication(Application? application)
    {
        if (application is null)
        {
            return;
        }

        application.UnhandledException += OnApplicationUnhandledException;
    }

    public static void LogMessage(string category, string message)
    {
        WriteLine($"[{category}] {message}");
    }

    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs? e)
    {
        if (e?.Exception is null)
        {
            return;
        }

        WriteException("App.UnhandledException", e.Exception);
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs? e)
    {
        if (e?.ExceptionObject is Exception exception)
        {
            WriteException("AppDomain.UnhandledException", exception);
        }
        else
        {
            WriteLine("[AppDomain.UnhandledException] Non-exception object raised unhandled exception event.");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
    }

    private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        var count = Interlocked.Increment(ref s_firstChanceCount);
        if (count > FirstChanceSampleLimit)
        {
            return;
        }

        WriteException($"FirstChance#{count}", e.Exception);
    }

    private static void WriteException(string category, Exception exception)
    {
        var builder = new StringBuilder();
        builder.Append('[').Append(category).Append("] ");
        builder.Append(exception.GetType().FullName);
        builder.Append(':').Append(' ').Append(exception.Message);
        builder.AppendLine();
        builder.AppendLine(exception.StackTrace ?? "(no stack trace)");
        WriteLine(builder.ToString().TrimEnd());
    }

    private static void WriteLine(string message)
    {
        try
        {
            var logPath = AppDataPaths.GetStartupLogPath();
            var entry = $"{DateTime.Now:u} {message}{Environment.NewLine}";

            lock (SyncRoot)
            {
                File.AppendAllText(logPath, entry, Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow logging failures so diagnostics never interfere with startup.
        }
    }
}
