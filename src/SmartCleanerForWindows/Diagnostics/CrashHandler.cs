using System;
using System.IO;
using Serilog;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartCleanerForWindows.Diagnostics;

internal static class CrashHandler
{
    internal static void HandleFatalException(string context, Exception exception, bool terminateProcess)
    {
        Log.Error(exception, "Fatal exception during {Context}.", context);
        TryWriteCrashLog(context, exception);

        var baseException = exception.GetBaseException();
        var logPath = AppDataPaths.GetCrashLogPath();
        var firstStackLine = baseException.StackTrace?
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        var message = $"Smart Cleaner for Windows encountered a fatal error during {context} and needs to close.\n\n" +
                      $"Error: {baseException.GetType().Name}: {baseException.Message}\n" +
                      (firstStackLine is not null ? $"Location: {firstStackLine}\n" : string.Empty) +
                      $"A detailed crash log was written to:{Environment.NewLine}{logPath}";

        ShowMessageBox("Smart Cleaner for Windows - Fatal error", message);

        if (!terminateProcess) return;
        try
        {
            Log.CloseAndFlush();
        }
        catch
        {
            // Ignore logging failures during shutdown.
        }

        Environment.Exit(1);
    }

    private static void TryWriteCrashLog(string context, Exception exception)
    {
        try
        {
            var logPath = AppDataPaths.GetCrashLogPath();
            File.AppendAllText(logPath,
                $"{DateTime.Now:u} [Fatal:{context}]\r\n{DescribeExceptionTree(exception)}\r\n\r\n");
        }
        catch
        {
            // Swallow logging failures to avoid masking the original crash.
        }
    }

    private static string DescribeExceptionTree(Exception exception)
    {
        var writer = new StringWriter();
        var current = exception;
        var depth = 0;
        while (current is not null && depth < 16)
        {
            var prefix = depth == 0 ? string.Empty : new string('>', depth) + " ";
            writer.WriteLine($"{prefix}{current.GetType().FullName} (0x{current.HResult:X8}): {current.Message}");
            writer.WriteLine(current.StackTrace ?? "(no stack trace)");
            writer.WriteLine();

            current = current.InnerException;
            depth++;
        }

        return writer.ToString();
    }

    private static void ShowMessageBox(string title, string message)
    {
        try
        {
            _ = PInvoke.MessageBox(
                HWND.Null,
                message,
                title,
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST);
        }
        catch
        {
            // If even the MessageBox fails, there's nothing else we can do here.
        }
    }
}
