using System;
using System.IO;
using Serilog;
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

        var message = $"Smart Cleaner for Windows encountered a fatal error during {context} and needs to close.\n\n" +
                      $"Error: {exception.Message}";

        ShowMessageBox("Smart Cleaner for Windows - Fatal error", message);

        if (terminateProcess)
        {
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
    }

    private static void TryWriteCrashLog(string context, Exception exception)
    {
        try
        {
            var logPath = AppDataPaths.GetCrashLogPath();
            File.AppendAllText(logPath,
                $"{DateTime.Now:u} [Fatal:{context}]\r\n{exception}\r\n\r\n");
        }
        catch
        {
            // Swallow logging failures to avoid masking the original crash.
        }
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
