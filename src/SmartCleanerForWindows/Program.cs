using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using DynamicDependencyPackageVersion = Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion;
using Smart_Cleaner_for_Windows.Diagnostics;
using WinRT;

namespace Smart_Cleaner_for_Windows;

public abstract class Program
{
    private const uint WindowsAppSdkMajorMinor = 0x00010008;
    private const string WindowsAppSdkChannel = "stable";
    private static readonly string WindowsAppSdkVersion = GetWindowsAppSdkVersion();

    [STAThread]
    public static void Main(string[] args)
    {
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
        Exception? channelFailure = null;

        try
        {
            if (TryGetPackageVersion(WindowsAppSdkVersion, out var packageVersion))
            {
                Bootstrap.Initialize(WindowsAppSdkMajorMinor, WindowsAppSdkChannel, packageVersion);
                LogEvent("Bootstrap", $"Initialized Windows App SDK using packaged runtime {WindowsAppSdkVersion}.");
            }
            else
            {
                Bootstrap.Initialize(WindowsAppSdkMajorMinor, WindowsAppSdkChannel);
                LogEvent("Bootstrap", $"Initialized Windows App SDK using channel '{WindowsAppSdkChannel}'.");
            }

            return true;
        }
        catch (Exception ex)
        {
            channelFailure = ex;
            LogEvent(
                "Bootstrap",
                $"Failed to initialize Windows App SDK from channel '{WindowsAppSdkChannel}' ({ex.GetType().Name}, 0x{ex.HResult:X8}). Attempting fallback.");
        }

        try
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            LogEvent("Bootstrap", "Initialized Windows App SDK using machine-wide runtime.");
            return true;
        }
        catch (Exception fallbackFailure)
        {
            ReportBootstrapFailure(channelFailure, fallbackFailure);
            return false;
        }
    }

    private static void ReportBootstrapFailure(Exception? channelFailure, Exception fallbackFailure)
    {
        var primaryMessage = FormatBootstrapError("channel", channelFailure);
        var fallbackMessage = FormatBootstrapError("fallback", fallbackFailure);
        var message = string.Join(Environment.NewLine, primaryMessage, fallbackMessage);

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

    private static string GetWindowsAppSdkVersion()
    {
        try
        {
            var attribute = typeof(Bootstrap).Assembly
                .GetCustomAttribute<AssemblyFileVersionAttribute>();
            return attribute?.Version ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryGetPackageVersion(
        string version,
        out DynamicDependencyPackageVersion packageVersion)
    {
        packageVersion = new DynamicDependencyPackageVersion();

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Length > 4)
        {
            return false;
        }

        if (!TryReadComponent(parts, 0, out packageVersion.Major))
        {
            return false;
        }

        TryReadComponent(parts, 1, out packageVersion.Minor);
        TryReadComponent(parts, 2, out packageVersion.Build);
        TryReadComponent(parts, 3, out packageVersion.Revision);

        return true;
    }

    private static bool TryReadComponent(string[] parts, int index, out ushort value)
    {
        value = 0;

        if (index >= parts.Length)
        {
            return true;
        }

        if (!uint.TryParse(parts[index], out var parsed) || parsed > ushort.MaxValue)
        {
            return false;
        }

        value = (ushort)parsed;
        return true;
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
}
