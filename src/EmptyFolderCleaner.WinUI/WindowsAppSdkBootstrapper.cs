using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace EmptyFolderCleaner.WinUI;

internal static class WindowsAppSdkBootstrapper
{
    private const uint MessageBoxOk = 0x00000000;
    private const uint MessageBoxIconError = 0x00000010;
    private static bool _initialized;

    [ModuleInitializer]
    internal static void Initialize()
    {
        string runtimeDirectory = ResolveRuntimeDirectory();
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", runtimeDirectory);

        try
        {
            _ = WindowsAppRuntimeEnsureIsLoaded();
        }
        catch (DllNotFoundException)
        {
            // The bootstrapper call below will surface a clearer error message.
        }

        var minVersion = new PackageVersion(Microsoft.WindowsAppSDK.Runtime.Version.UInt64);

        if (Bootstrap.TryInitialize(
                Microsoft.WindowsAppSDK.Release.MajorMinor,
                Microsoft.WindowsAppSDK.Release.VersionTag,
                minVersion,
                Bootstrap.InitializeOptions.None,
                out int hresult))
        {
            _initialized = true;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }
        else
        {
            ShowInitializationError(hresult);
            Environment.Exit(hresult);
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            Bootstrap.Shutdown();
            _initialized = false;
        }
    }

    private static string ResolveRuntimeDirectory()
    {
        string baseDirectory = AppContext.BaseDirectory;
        if (ContainsRuntimeAssets(baseDirectory))
        {
            return baseDirectory;
        }

        string? processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(processDirectory) && ContainsRuntimeAssets(processDirectory))
        {
            return processDirectory;
        }

        return baseDirectory;
    }

    private static bool ContainsRuntimeAssets(string directory)
    {
        try
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            if (File.Exists(Path.Combine(directory, "Microsoft.WindowsAppRuntime.dll")))
            {
                return true;
            }

            foreach (string _ in Directory.EnumerateDirectories(directory, "Microsoft.WindowsAppRuntime.*"))
            {
                return true;
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    private static void ShowInitializationError(int hresult)
    {
        string message =
            $"Empty Folder Cleaner couldn't load the Windows App SDK runtime (0x{hresult:X8}).{Environment.NewLine}{Environment.NewLine}" +
            "Make sure all files from the publish folder are present.";
        MessageBox(IntPtr.Zero, message, "Empty Folder Cleaner", MessageBoxOk | MessageBoxIconError);
    }

    [DllImport("Microsoft.WindowsAppRuntime.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int WindowsAppRuntimeEnsureIsLoaded();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
