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
using WinRT;

namespace SmartCleanerForWindows;

public abstract class Program
{
    private const uint WindowsAppSdkMajorMinor = 0x00010008;
    private static readonly WindowsAppSdkConfiguration WindowsAppSdk = WindowsAppSdkConfiguration.Load();

    [STAThread]
    public static void Main(string[] args)
    {
        StartupDiagnostics.Initialize();
        ComWrappersSupport.InitializeComWrappers();

        var isPackaged = IsRunningPackaged();
        var bootstrapInitialized = false;

        if (isPackaged)
        {
            LogEvent("Bootstrap", "Running inside packaged deployment; bootstrap not required.");
        }
        else
        {
            bootstrapInitialized = TryInitializeBootstrap();
            if (!bootstrapInitialized)
            {
                return;
            }
        }

        if (!EnsureSingleInstance())
        {
            if (bootstrapInitialized)
            {
                Bootstrap.Shutdown();
            }

            return;
        }

        LogEvent("Startup", $"Launching Smart Cleaner for Windows (packaged: {isPackaged})");
        StartupDiagnostics.LogMessage("Startup", $"Main invoked (packaged: {isPackaged}, args: {args.Length}).");

        try
        {
            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new Shell.App();
            });
        }
        finally
        {
            if (bootstrapInitialized)
            {
                Bootstrap.Shutdown();
            }
        }
    }

    private static bool EnsureSingleInstance()
    {
        try
        {
            var instance = AppInstance.FindOrRegisterForKey("SmartCleanerForWindows");
            if (instance.IsCurrent)
            {
                return true;
            }

            LogEvent("Startup", "Another Smart Cleaner for Windows instance is already running. Exiting.");
            return false;
        }
        catch (Exception ex)
        {
            LogEvent("Startup", $"Failed to enforce single-instance policy ({ex.GetType().Name}): {ex.Message}");
            return true;
        }
    }

    private static bool TryInitializeBootstrap()
    {
        var configuration = WindowsAppSdk;
        var failures = new List<(string Stage, Exception? Failure)>();

        if (configuration.ShouldPreferBundledRuntime())
        {
            if (TryInitializeSelfContained(out var bundledFailure))
            {
                return true;
            }

            failures.Add(("bundled runtime", bundledFailure));
        }

        if (TryInitializeConfiguredChannel(configuration, out var channelFailure))
        {
            return true;
        }

        failures.Add(("configured channel", channelFailure));

        try
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            LogEvent("Bootstrap", "Initialized Windows App SDK using machine-wide runtime.");
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
            LogEvent("Bootstrap", "Initialized Windows App SDK using app-bundled runtime.");
            failure = null;
            return true;
        }
        catch (Exception ex)
        {
            failure = ex;
            LogEvent("Bootstrap", $"Failed to initialize Windows App SDK using the app-bundled runtime ({ex.GetType().Name}, 0x{ex.HResult:X8}). Attempting configured channel.");
            return false;
        }
    }

    private static bool TryInitializeConfiguredChannel(WindowsAppSdkConfiguration configuration, out Exception? failure)
    {
        try
        {
            InitializeBootstrap(configuration);
            LogEvent("Bootstrap", configuration.BuildInitializationMessage());
            failure = null;
            return true;
        }
        catch (Exception ex)
        {
            failure = ex;
            LogEvent("Bootstrap", configuration.BuildFailureMessage(ex));
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
        LogEvent("Bootstrap", message);
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

    private static void LogEvent(string category, string message)
    {
        try
        {
            var logPath = AppDataPaths.GetEventLogPath();
            File.AppendAllText(logPath, $"{DateTime.Now:u} [{category}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures to avoid impacting startup.
        }
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

        public string DisplayChannel => string.IsNullOrEmpty(Channel) ? "stable" : Channel;

        public string Version { get; }

        public static WindowsAppSdkConfiguration Load()
        {
            var metadata = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
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

            selectedVersion ??= string.Empty;

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
