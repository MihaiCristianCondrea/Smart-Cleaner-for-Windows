using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Serilog;
using SmartCleanerForWindows.Diagnostics;
using SmartCleanerForWindows.Settings;

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

        // AppDomain handler MUST use System.UnhandledExceptionEventArgs + non-null sender.
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // This one is fine as-is.
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // WinUI Application.UnhandledException uses Microsoft.UI.Xaml.UnhandledExceptionEventArgs.
        UnhandledException += OnApplicationUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            OnLaunchedInvoked = true;
            Log.Information("App.OnLaunched entered. Launch arguments: {Arguments}", args.Arguments);

            try
            {
                ToolSettingsBootstrapper.Initialize();
            }
            catch (Exception bootstrapFailure)
            {
                Log.Warning(bootstrapFailure, "Settings bootstrap failed; continuing startup with defaults where possible.");
            }

            var resources = Application.Current.Resources;
            string[] keysToCheck =
            [
                "TextFillColorSecondaryBrush"
            ];

            foreach (var key in keysToCheck)
            {
                Debug.WriteLine(!resources.TryGetValue(key, out var value)
                    ? $"[RESOURCE] MISSING: {key}"
                    : $"[RESOURCE] FOUND: {key} -> {value?.GetType().FullName ?? "<null>"}");
            }

            _window = new MainWindow();
            Log.Information("MainWindow instance created. Activating window.");
            _window.Activate();
            Log.Information("MainWindow activation requested.");
        }
        catch (FileNotFoundException fileEx)
        {
            Log.Error(fileEx, "Fatal XAML load failed: missing file during app launch. Message={Message}", fileEx.Message);
            ShowFatalErrorWindow(
                "A required application file could not be found. Please reinstall Smart Cleaner for Windows.",
                fileEx);
        }
        catch (XamlParseException xamlEx)
        {
            Log.Error(
                xamlEx,
                "Fatal XAML parse during app launch. HResult={HResult}, Inner={InnerMessage}",
                xamlEx.HResult,
                xamlEx.InnerException?.Message);

            ShowFatalErrorWindow(
                "Smart Cleaner for Windows could not build its interface. The packaged resources may be corrupted.",
                xamlEx);
        }
        catch (Exception ex)
        {
            CrashHandler.HandleFatalException("app launch", ex, terminateProcess: true);
        }
    }

    private static void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        => HandleUnhandledException(e.ExceptionObject as Exception, "AppDomain.UnhandledException");

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        HandleUnhandledException(e.Exception, "Application.UnhandledException");
        e.Handled = true;
    }

    private static void HandleUnhandledException(Exception? exception, string source)
    {
        if (exception is null) return;
        CrashHandler.HandleFatalException(source, exception, terminateProcess: true);
    }

    private void ShowFatalErrorWindow(string friendlyMessage, Exception exception)
    {
        CrashHandler.HandleFatalException("app launch (non-fatal)", exception, terminateProcess: false);

        var fallbackWindow = new Window
        {
            Content = new StackPanel
            {
                Padding = new Thickness(32),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = friendlyMessage,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 20,
                        FontWeight = FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "The app will stay open so you can read the error above. You can close it after reviewing the logs.",
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        _window = fallbackWindow;
        _window.Activate();
    }
}
