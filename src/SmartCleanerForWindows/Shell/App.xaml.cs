using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.Shell;

/// <summary>
/// Represents the WinUI application.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InitializeComponent"/> method is generated from <c>App.xaml</c> at build time.
/// </para>
/// <para>
/// This type registers global exception handlers and creates the main <see cref="Window"/> in
/// <see cref="OnLaunched(LaunchActivatedEventArgs)"/>.
/// </para>
/// </remarks>
public sealed partial class App
{
    private Window? _window;

    internal static bool OnLaunchedInvoked { get; private set; }

    public App()
    {
        InitializeComponent();
        Trace.AutoFlush = true;
        TraceInformation("App constructor invoked. Registering global exception handlers.");
        RegisterGlobalExceptionHandlers();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            OnLaunchedInvoked = true;
            TraceInformation($"App.OnLaunched entered. Launch arguments: \"{args.Arguments}\"");

            ProbeAppEnvironment();
            ProbeResources(
                "BooleanToVisibilityConverter",
                "ApplicationPageBackgroundThemeBrush",
                "SystemControlForegroundBaseMediumBrush");

            ProbePackagedAsset("Assets/Square44x44Logo.scale-200.png");

            _window ??= CreateMainWindow();
            TraceInformation("Main window created. Activating window.");
            _window.Activate();
            TraceInformation("Main window activation requested.");
        }
        catch (Exception ex)
        {
            TraceError("Fatal error during application launch.", ex);
            ShowFatalErrorWindow(
                "Smart Cleaner for Windows encountered a fatal error during app launch.",
                ex);
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Current.UnhandledException += OnApplicationUnhandledException;
    }

    private static void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            TraceError($"Unhandled exception from AppDomain.UnhandledException (non-Exception object: {e.ExceptionObject}).", null);
            Environment.FailFast("Unhandled exception from AppDomain.UnhandledException (non-Exception object).");
            return;
        }

        TraceError("Unhandled exception from AppDomain.UnhandledException.", exception);
        Environment.FailFast("Unhandled exception from AppDomain.UnhandledException.", exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        TraceError("Unobserved task exception from TaskScheduler.UnobservedTaskException.", e.Exception);
        e.SetObserved();
    }

    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        TraceError("Unhandled exception from Application.UnhandledException.", e.Exception);
        e.Handled = true;
    }

    private static void ProbeAppEnvironment()
    {
        var baseDirectory = AppContext.BaseDirectory;
        TraceInformation($"Resolved AppContext.BaseDirectory=\"{baseDirectory}\"");

        var anyPri = false;
        try
        {
            anyPri = Directory.EnumerateFiles(baseDirectory, "*.pri", SearchOption.TopDirectoryOnly).Any();
        }
        catch (Exception ex)
        {
            TraceError("Failed to enumerate PRI files in the base directory.", ex);
        }

        TraceInformation($"PRI present: {anyPri}");
    }

    private static void ProbeResources(params string[] keysToCheck)
    {
        var resources = Current.Resources;

        foreach (var key in keysToCheck)
        {
            var found = resources.TryGetValue(key, out var value);
            TraceInformation(
                found
                    ? $"Startup resource probe found: \"{key}\" (Type={value?.GetType().FullName ?? "<null>"})"
                    : $"Startup resource probe missing: \"{key}\"");
        }
    }

    private static void ProbePackagedAsset(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(AppContext.BaseDirectory, normalizedPath);

        TraceInformation(
            $"Startup asset probe: \"{relativePath}\" exists={File.Exists(absolutePath)} absolute=\"{absolutePath}\"");
    }

    private static Window CreateMainWindow()
    {
        var mainWindow = new MainWindow();
        if (mainWindow.IsFallbackShellActive)
        {
            TraceError("MainWindow initialized in fallback mode.", mainWindow.InitializationFailure);
        }

        return mainWindow;
    }

    private void ShowFatalErrorWindow(string friendlyMessage, Exception exception)
    {
        _window = new Window
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
                        Text = "Details:",
                        FontWeight = FontWeights.SemiBold
                    },
                    new TextBox
                    {
                        Text = FormatException(exception),
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    },
                    new TextBlock
                    {
                        Text = "The app will stay open so you can read the error details above. You can close it after reviewing the logs.",
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        _window.Activate();
    }

    private static string FormatException(Exception exception)
    {
        var sb = new StringBuilder();

        sb.AppendLine(exception.ToString());

        if (exception is not FileNotFoundException fnf || string.IsNullOrWhiteSpace(fnf.FileName))
        {
            return sb.ToString();
        }

        sb.AppendLine();
        sb.Append("FileName: ");
        sb.AppendLine(fnf.FileName);

        return sb.ToString();
    }

    private static void TraceInformation(string message) => Trace.TraceInformation(message);

    private static void TraceError(string message, Exception? exception)
    {
        if (exception is null)
        {
            Trace.TraceError(message);
            return;
        }

        Trace.TraceError($"{message}{Environment.NewLine}{FormatException(exception)}");
    }
}
