using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using SmartCleanerForWindows.Diagnostics;
using Serilog;

namespace SmartCleanerForWindows.Shell;

/// <summary>
/// App is declared partial because InitializeComponent is generated from App.xaml at build time.
/// </summary>
public sealed partial class App
{
    private Window? _window;

    internal static bool OnLaunchedInvoked { get; private set; }

    public App()
    {
        Log.Information("App constructor invoked. Setting up global exception handlers.");
        InitializeComponent();
        StartupDiagnostics.Initialize();
        StartupDiagnostics.AttachToApplication(this);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        UnhandledException += OnApplicationUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            OnLaunchedInvoked = true;
            Log.Information("App.OnLaunched entered. Launch arguments: {Arguments}", args.Arguments);
            _window = new MainWindow();
            Log.Information("MainWindow instance created. Activating window.");
            _window.Activate();
            Log.Information("MainWindow activation requested.");
        }
        catch (FileNotFoundException fileEx)
        {
            Log.Error(fileEx, "Fatal XAML load failed: missing file during app launch. Message={Message}", fileEx.Message);
            CrashHandler.HandleFatalException("app launch (missing file during XAML load)", fileEx, terminateProcess: true);
        }
        catch (XamlParseException xamlEx)
        {
            Log.Error(xamlEx, "Fatal XAML parse during app launch. HResult={HResult}, Inner={InnerMessage}", xamlEx.HResult, xamlEx.InnerException?.Message);
            CrashHandler.HandleFatalException("app launch (XAML parse)", xamlEx, terminateProcess: true);
        }
        catch (Exception ex)
        {
            CrashHandler.HandleFatalException("app launch", ex, terminateProcess: true);
        }
    }

    private static void OnUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        HandleUnhandledException(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static void OnApplicationUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
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
