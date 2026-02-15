using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartCleanerForWindows.Modules.Dashboard.Views;
using SmartCleanerForWindows.Modules.DiskCleanup.Views;
using SmartCleanerForWindows.Modules.EmptyFolders.Views;
using SmartCleanerForWindows.Modules.InternetRepair.Views;
using SmartCleanerForWindows.Modules.LargeFiles.Views;
using Serilog;
using SmartCleanerForWindows.Diagnostics;
using SmartCleanerForWindows.Settings;
using SmartCleanerForWindows.Shell.Settings;

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

            var baseDirectory = AppContext.BaseDirectory;
            Log.Information(
                "Resolved AppContext.BaseDirectory={BaseDirectory}. PRI present: {PriExists}",
                baseDirectory,
                File.Exists(Path.Combine(baseDirectory, "SmartCleanerForWindows.pri")));

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
                "BooleanToVisibilityConverter",
                "ApplicationPageBackgroundThemeBrush",
                "SystemControlForegroundBaseMediumBrush"
            ];

            foreach (var key in keysToCheck)
            {
                Log.Information(!resources.TryGetValue(key, out var value)
                    ? "Startup resource probe missing: {ResourceKey}"
                    : "Startup resource probe found: {ResourceKey} ({ValueType})",
                    key,
                    value?.GetType().FullName ?? "<null>");
            }

            ProbePackagedAsset("Assets/Square44x44Logo.scale-200.png");
            ProbeViewInitialization();

            _window = UiConstructionLog.Create(() => new MainWindow(), "MainWindow");
            if (_window is MainWindow { IsFallbackShellActive: true } fallbackWindow)
            {
                Log.Warning(
                    "MainWindow started in fallback mode due to XAML initialization failure: {FailureType} {FailureMessage}",
                    fallbackWindow.InitializationFailure?.GetType().Name ?? "Unknown",
                    fallbackWindow.InitializationFailure?.Message ?? "(none)");
            }
            else
            {
                Log.Information("MainWindow started in normal mode (XAML initialization succeeded).");
            }

            Log.Information("MainWindow instance created. Activating window.");
            _window.Activate();
            Log.Information("MainWindow activation requested.");
        }
        catch (FileNotFoundException fileEx)
        {
            Log.Error("MainWindow creation failed.\n{Details}", XamlDiagnostics.Format(fileEx));
            ShowFatalErrorWindow(
                "A required application file could not be found. Please reinstall Smart Cleaner for Windows.",
                fileEx);
        }
        catch (Exception ex)
        {
            Log.Error("MainWindow creation failed.\n{Details}", XamlDiagnostics.Format(ex));
            CrashHandler.HandleFatalException("app launch", ex, terminateProcess: true);
        }
    }


    private static void ProbePackagedAsset(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
        Log.Information(
            "Startup asset probe: {RelativePath} exists={Exists} absolute={AbsolutePath}",
            relativePath,
            File.Exists(absolutePath),
            absolutePath);
    }

    private static void ProbeViewInitialization()
    {
        if (!ShouldRunViewInitializationProbes())
        {
            Log.Information("Startup view probes disabled. Set SMARTCLEANER_PROBE_VIEWS=1 to enable troubleshooting probes.");
            return;
        }

        var probes = new[]
        {
            (Name: "DashboardView", Factory: (Func<FrameworkElement>)(() => new DashboardView())),
            (Name: "EmptyFoldersView", Factory: (Func<FrameworkElement>)(() => new EmptyFoldersView())),
            (Name: "LargeFilesView", Factory: (Func<FrameworkElement>)(() => new LargeFilesView())),
            (Name: "InternetRepairView", Factory: (Func<FrameworkElement>)(() => new InternetRepairView())),
            (Name: "DiskCleanupView", Factory: (Func<FrameworkElement>)(() => new DiskCleanupView())),
            (Name: "SettingsView", Factory: (Func<FrameworkElement>)(() => new SettingsView()))
        };

        foreach (var probe in probes)
        {
            try
            {
                _ = probe.Factory();
                Log.Information("Startup view probe succeeded: {ViewName}", probe.Name);
            }
            catch (Exception ex)
            {
                Log.Error("Startup view probe failed: {ViewName}\n{Details}", probe.Name, XamlDiagnostics.Format(ex));
            }
        }
    }

    private static bool ShouldRunViewInitializationProbes()
    {
#if DEBUG
        const bool debugDefault = true;
#else
        const bool debugDefault = false;
#endif

        var configured = Environment.GetEnvironmentVariable("SMARTCLEANER_PROBE_VIEWS");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return debugDefault;
        }

        return configured.Equals("1", StringComparison.OrdinalIgnoreCase)
               || configured.Equals("true", StringComparison.OrdinalIgnoreCase)
               || configured.Equals("yes", StringComparison.OrdinalIgnoreCase)
               || configured.Equals("on", StringComparison.OrdinalIgnoreCase);
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
