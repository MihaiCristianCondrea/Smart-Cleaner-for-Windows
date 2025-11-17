using System;
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
        try
        {
            OnLaunchedInvoked = true;
            Log.Information("App.OnLaunched entered. Launch arguments: {Arguments}", args?.Arguments ?? "(none)");
            _window = new MainWindow();
            Log.Information("MainWindow instance created. Activating window.");
            _window.Activate();
            Log.Information("MainWindow activation requested.");
        }
        catch (Exception ex)
        {
            CrashHandler.HandleFatalException("app launch", ex, terminateProcess: true);
        }
    }

    private void OnUnhandledException(object? sender, global::System.UnhandledExceptionEventArgs e)
    {
        HandleUnhandledException(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private void OnApplicationUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        HandleUnhandledException(e.Exception, "Application.UnhandledException");
        e.Handled = true;
    }

    private static void HandleUnhandledException(Exception? exception, string source)
    {
        if (exception is null)
        {
            return;
        }

        CrashHandler.HandleFatalException(source, exception, terminateProcess: true);
    }
}
