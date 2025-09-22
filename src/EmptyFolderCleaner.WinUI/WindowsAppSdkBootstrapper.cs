using System;
using System.IO;
using System.Reflection;
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

        var releaseInfo = GetReleaseInfo();

        if (Bootstrap.TryInitialize(
                releaseInfo.MajorMinor,
                releaseInfo.VersionTag,
                releaseInfo.MinVersion,
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

    private static (string MajorMinor, string VersionTag, PackageVersion MinVersion) GetReleaseInfo()
    {
        Assembly assembly = typeof(Bootstrap).Assembly;
        Version? assemblyVersion = assembly.GetName().Version;
        string majorMinor = assemblyVersion is not null
            ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}"
            : "";
        string versionTag = string.Empty;
        PackageVersion minVersion = default;

        try
        {
            Type? releaseType = assembly.GetType("Microsoft.WindowsAppSDK.Release");
            if (releaseType is not null)
            {
                majorMinor = releaseType
                                 .GetProperty("MajorMinor", BindingFlags.Public | BindingFlags.Static)?
                                 .GetValue(null) as string
                             ?? majorMinor;
                versionTag = releaseType
                                 .GetProperty("VersionTag", BindingFlags.Public | BindingFlags.Static)?
                                 .GetValue(null) as string
                             ?? versionTag;
            }

            Type? runtimeVersionType = assembly.GetType("Microsoft.WindowsAppSDK.Runtime+Version");
            if (runtimeVersionType?
                    .GetProperty("UInt64", BindingFlags.Public | BindingFlags.Static)?
                    .GetValue(null) is ulong rawVersion && rawVersion != 0)
            {
                minVersion = new PackageVersion(rawVersion);
            }
        }
        catch (Exception)
        {
        }

        if (string.IsNullOrWhiteSpace(majorMinor))
        {
            string? informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrEmpty(informationalVersion))
            {
                int prereleaseSeparator = informationalVersion.IndexOf('-');
                if (prereleaseSeparator >= 0)
                {
                    informationalVersion = informationalVersion.Substring(0, prereleaseSeparator);
                }

                string[] parts = informationalVersion.Split('.');
                if (parts.Length >= 2)
                {
                    majorMinor = $"{parts[0]}.{parts[1]}";
                }
            }

            if (string.IsNullOrWhiteSpace(majorMinor))
            {
                majorMinor = "1.6";
            }
        }

        return (majorMinor, versionTag, minVersion);
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
