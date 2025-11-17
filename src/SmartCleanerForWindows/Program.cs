using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using DynamicDependencyPackageVersion = Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion;
using SmartCleanerForWindows.Diagnostics;
using SmartCleanerForWindows.Logging;
using SmartCleanerForWindows.Shell;
using Serilog;
using WinRT;

namespace SmartCleanerForWindows;

public abstract class Program
{
    private const uint WindowsAppSdkMajorMinor = 0x00010008;
    private static readonly WindowsAppSdkConfiguration WindowsAppSdk = WindowsAppSdkConfiguration.Load();

    [STAThread]
    public static void Main(string[]? args)
    {
        LoggingConfiguration.Initialize(args);
        Log.Information("Program.Main reached. DISABLE_XAML_GENERATED_MAIN entrypoint active.");
        StartupDiagnostics.Initialize();
        ComWrappersSupport.InitializeComWrappers();

        var argsLength = args?.Length ?? 0;
        var argsDescription = args is { Length: > 0 } ? string.Join(" ", args) : "(none)";

        Log.Information("Command line arguments: {ArgsCount} ({Args})", argsLength, argsDescription);

        var isPackaged = IsRunningPackaged();
        Log.Information("Packaged detection result: {IsPackaged}", isPackaged);

        var bootstrapInitialized = false;

        if (isPackaged)
        {
            Log.Information("Running inside packaged deployment; bootstrap not required.");
        }
        else
        {
            Log.Information("Unpackaged execution detected; attempting Windows App SDK bootstrap.");
            bootstrapInitialized = TryInitializeBootstrap();
            if (!bootstrapInitialized)
            {
                Log.Error("Bootstrap initialization failed; exiting before application start.");
                Log.CloseAndFlush();
                return;
            }
        }

        if (!EnsureSingleInstance())
        {
            if (bootstrapInitialized)
            {
                Log.Information("Shutting down bootstrap after failed single-instance check.");
                Bootstrap.Shutdown();
            }

            Log.Error("Another instance is running or single-instance enforcement failed; application will exit.");
            Log.CloseAndFlush();
            return;
        }

        Log.Information("Launching Smart Cleaner for Windows (packaged: {IsPackaged}).", isPackaged);
        StartupDiagnostics.LogMessage("Startup", $"Main invoked (packaged: {isPackaged}, args: {argsLength}).");

        try
        {
            Log.Information("Starting WinUI application host.");
            Application.Start(p =>
            {
                Log.Information("Application.Start delegate executing on UI thread.");
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            CrashHandler.HandleFatalException("application startup", ex, terminateProcess: true);
        }
        finally
        {
            if (!App.OnLaunchedInvoked)
            {
                Log.Error("Program.Main completed without App.OnLaunched being invoked. This may indicate a manifest entrypoint misconfiguration.");
            }

            if (bootstrapInitialized)
            {
                Log.Information("Shutting down Windows App SDK bootstrap.");
                Bootstrap.Shutdown();
            }

            Log.CloseAndFlush();
        }
    }

    private static bool EnsureSingleInstance()
    {
        try
        {
            var instance = AppInstance.FindOrRegisterForKey("SmartCleanerForWindows");
            if (instance.IsCurrent)
            {
                Log.Information("Current process owns the SmartCleanerForWindows AppInstance.");
                return true;
            }

            Log.Warning("Another Smart Cleaner for Windows instance is already running. Exiting.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to enforce single-instance policy; continuing to run current process.");
            return true;
        }
    }

    private static bool TryInitializeBootstrap()
    {
        var configuration = WindowsAppSdk;
        var failures = new List<(string Stage, Exception? Failure)>();

        if (configuration.ShouldPreferBundledRuntime())
        {
            Log.Information("Attempting bootstrap using bundled runtime.");
            if (TryInitializeSelfContained(out var bundledFailure))
            {
                return true;
            }

            failures.Add(("bundled runtime", bundledFailure));
        }

        Log.Information("Attempting bootstrap using configured channel.");
        if (TryInitializeConfiguredChannel(configuration, out var channelFailure))
        {
            return true;
        }

        failures.Add(("configured channel", channelFailure));

        try
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            Log.Information("Initialized Windows App SDK using machine-wide runtime.");
            return true;
        }
        catch (Exception fallbackFailure)
        {
            failures.Add(("machine-wide runtime", fallbackFailure));
            ReportBootstrapFailure(failures.ToArray());
            return false;
        }
    }

    private static bool TryInitializeSelfContained(out Exception? failure)
    {
        try
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            Log.Information("Initialized Windows App SDK using app-bundled runtime.");
            failure = null;
            return true;
        }
        catch (Exception ex)
        {
            failure = ex;
            Log.Error(ex, "Failed to initialize Windows App SDK using the app-bundled runtime. Attempting configured channel.");
            return false;
        }
    }

    private static bool TryInitializeConfiguredChannel(WindowsAppSdkConfiguration configuration, out Exception? failure)
    {
        try
        {
            InitializeBootstrap(configuration);
            Log.Information(configuration.BuildInitializationMessage());
            failure = null;
            return true;
        }
        catch (Exception ex)
        {
            failure = ex;
            Log.Error(ex, configuration.BuildFailureMessage(ex));
            return false;
        }
    }

    private static void ReportBootstrapFailure(params (string Stage, Exception? Failure)[] failures)
    {
        var messages = failures
            .Select(failure => FormatBootstrapError(failure.Stage, failure.Failure))
            .ToArray();
        var message = string.Join(Environment.NewLine, messages);

        try
        {
            Console.Error.WriteLine(message);
        }
        catch
        {
            // Ignored: console may be unavailable in packaged apps.
        }

        LogBootstrapFailure(message);
        Log.Error("Bootstrap failure chain:\n{Message}", message);
    }

    private static string FormatBootstrapError(string stage, Exception? exception)
    {
        if (exception is null)
        {
            return $"Bootstrap {stage} initialization was not attempted.";
        }

        return $"Bootstrap {stage} initialization failed with HRESULT 0x{exception.HResult:X8} ({exception.GetType().Name}): {exception.Message}";
    }

    private static void LogBootstrapFailure(string message)
    {
        try
        {
            var logPath = AppDataPaths.GetCrashLogPath();
            File.AppendAllText(logPath, $"{DateTime.Now:u} [Bootstrap]\r\n{message}\r\n\r\n");
        }
        catch
        {
            // Ignore logging failures so bootstrap errors bubble out.
        }
    }

    private static void InitializeBootstrap(WindowsAppSdkConfiguration configuration)
    {
        if (configuration.TryGetPackageVersion(out var packageVersion))
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor, configuration.Channel, packageVersion);
            return;
        }

        Bootstrap.Initialize(WindowsAppSdkMajorMinor, configuration.Channel);
    }

    private static bool IsRunningPackaged()
    {
        try
        {
            return Windows.ApplicationModel.Package.Current is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private sealed class WindowsAppSdkConfiguration
    {
        private WindowsAppSdkConfiguration(string channel, string version)
        {
            Channel = channel;
            Version = version;
        }

        public string Channel { get; }

        private string DisplayChannel => string.IsNullOrEmpty(Channel) ? "stable" : Channel;

        private string Version { get; }

        public static WindowsAppSdkConfiguration Load()
        {
            var metadata = typeof(Program).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToArray();
            var channel = NormalizeChannel(GetMetadata(metadata, "WindowsAppSdkChannel"));
            var stable = GetMetadata(metadata, "WindowsAppSdkStableVersion");
            var preview = GetMetadata(metadata, "WindowsAppSdkPreviewVersion");
            var experimental = GetMetadata(metadata, "WindowsAppSdkExperimentalVersion");

            var selectedVersion = channel switch
            {
                var value when value.StartsWith("preview", StringComparison.OrdinalIgnoreCase) => preview,
                var value when value.StartsWith("experimental", StringComparison.OrdinalIgnoreCase) => experimental,
                _ => stable
            };

            if (string.IsNullOrWhiteSpace(selectedVersion))
            {
                selectedVersion = stable;
            }

            return new WindowsAppSdkConfiguration(channel, selectedVersion);
        }

        public string BuildInitializationMessage()
        {
            if (!string.IsNullOrWhiteSpace(Version))
            {
                return $"Initialized Windows App SDK runtime {Version}{FormatChannelSuffix()}.";
            }

            return $"Initialized Windows App SDK using {DescribeChannel()}.";
        }

        public string BuildFailureMessage(Exception ex)
            => $"Failed to initialize Windows App SDK from {DescribeChannel()} ({ex.GetType().Name}, 0x{ex.HResult:X8}). Attempting fallback.";

        public bool TryGetPackageVersion(out DynamicDependencyPackageVersion packageVersion)
            => TryGetPackageVersion(Version, out packageVersion);

        public bool ShouldPreferBundledRuntime()
            => string.IsNullOrEmpty(Channel) || Channel.Equals("stable", StringComparison.OrdinalIgnoreCase);

        private string DescribeChannel()
            => string.IsNullOrEmpty(DisplayChannel) ? "the configured channel" : $"channel '{DisplayChannel}'";

        private string FormatChannelSuffix()
            => string.IsNullOrEmpty(DisplayChannel) ? string.Empty : $" (channel: {DisplayChannel})";

        private static string GetMetadata(IEnumerable<AssemblyMetadataAttribute> metadata, string key)
            => metadata.FirstOrDefault(attr => string.Equals(attr.Key, key, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;

        private static string NormalizeChannel(string? channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return "stable";
            }

            var trimmed = channel.Trim();
            if (trimmed.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                return "stable";
            }

            return trimmed.ToLowerInvariant();
        }

        private static bool TryGetPackageVersion(string version, out DynamicDependencyPackageVersion packageVersion)
        {
            packageVersion = new DynamicDependencyPackageVersion();

            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            var dashIndex = version.IndexOf('-');
            if (dashIndex >= 0)
            {
                version = version[..dashIndex];
            }

            var components = version.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (components.Length == 0 || components.Length > 4)
            {
                return false;
            }

            if (!TryReadComponent(components, 0, out packageVersion.Major))
            {
                return false;
            }

            TryReadComponent(components, 1, out packageVersion.Minor);
            TryReadComponent(components, 2, out packageVersion.Build);
            TryReadComponent(components, 3, out packageVersion.Revision);

            return true;
        }

        private static bool TryReadComponent(string[] components, int index, out ushort value)
        {
            value = 0;

            if (index >= components.Length)
            {
                return true;
            }

            var segment = components[index];
            if (string.IsNullOrEmpty(segment))
            {
                return false;
            }

            if (!ushort.TryParse(segment, out value))
            {
                return false;
            }

            return true;
        }
    }
}
