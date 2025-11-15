using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Smart_Cleaner_for_Windows.Diagnostics;

namespace Smart_Cleaner_for_Windows.Shell;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private void OnUnhandledException(object? sender, global::System.UnhandledExceptionEventArgs e)
        => LogException(e.ExceptionObject as Exception, "UnhandledException");

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        => LogException(e.Exception, "UnobservedTaskException");

    private static void LogException(Exception? exception, string source)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            var logPath = AppDataPaths.GetCrashLogPath();
            File.AppendAllText(logPath,
                $"{DateTime.Now:u} [{source}]\r\n{exception}\r\n\r\n");
        }
        catch
        {
            // Intentionally swallow all exceptions from logging to avoid masking the original failure.
        }
    }
}
