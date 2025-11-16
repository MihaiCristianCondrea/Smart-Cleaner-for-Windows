using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using SmartCleanerForWindows.Diagnostics;
using Serilog;

namespace SmartCleanerForWindows.Shell;

public partial class App : Application
{
    private Window? _window;

    internal static bool OnLaunchedInvoked { get; private set; }

    public App()
    {
        Log.Information("App constructor invoked. Setting up global exception handlers.");
        StartupDiagnostics.AttachToApplication(this);
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        UnhandledException += OnApplicationUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        OnLaunchedInvoked = true;
        Log.Information("App.OnLaunched entered. Launch arguments: {Arguments}", args?.Arguments ?? "(none)");
        _window = new MainWindow();
        Log.Information("MainWindow instance created. Activating window.");
        _window.Activate();
        Log.Information("MainWindow activation requested.");
    }

    private void OnUnhandledException(object? sender, global::System.UnhandledExceptionEventArgs e)
        => LogException(e.ExceptionObject as Exception, "UnhandledException");

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }

    private void OnApplicationUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Application.UnhandledException");
        e.Handled = true;
    }

    private static void LogException(Exception? exception, string source)
    {
        if (exception is null)
        {
            return;
        }

        Log.Error(exception, "{Source} captured an unhandled exception.", source);

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
