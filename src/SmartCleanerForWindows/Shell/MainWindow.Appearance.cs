using System;
using System.IO;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT;
using WinRT.Interop;
using XamlSystemBackdrop = Microsoft.UI.Xaml.Media.SystemBackdrop;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    // Windows 11+ (build 22000+) is the primary path for SystemBackdrop-based Mica/Acrylic.
    private static readonly Version Windows11Baseline = new(10, 0, 22000);

    // Legacy MicaController fallback is intentionally restricted to Windows 10 builds where
    // SystemBackdrop Mica isn't expected to be consistently available.
    private static readonly Version Windows10LegacyMin = new(10, 0, 19041);

    private void TryEnableMica()
    {
        ResetBackdropState();

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Primary path: modern WinUI 3 SystemBackdrop (Windows 11+ with Windows App SDK runtime support).
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) &&
            IsMicaSupported() &&
            TryCreateSystemBackdrop(static () => new MicaBackdrop()))
        {
            return;
        }

        // Secondary path: keep legacy controller only on constrained compatibility range.
        if (TryInitializeLegacyMicaController())
        {
            return;
        }

        // Final fallback: DesktopAcrylic SystemBackdrop when available.
        TryEnableDesktopAcrylic();
    }

    private void ResetBackdropState()
    {
        SystemBackdrop = null;
        _mica?.Dispose();
        _mica = null;
        _backdropConfig = null;
    }

    private bool TrySetSystemBackdropSafe(XamlSystemBackdrop backdrop)
    {
        try
        {
            SystemBackdrop = backdrop;
            return true;
        }
        catch
        {
            ResetBackdropState();
            return false;
        }
    }

    private bool TryInitializeLegacyMicaController()
    {
        // Strict compatibility gate for legacy controller fallback.
        if (!ShouldUseLegacyMicaController() || !IsMicaSupported())
        {
            return false;
        }

        try
        {
            SystemBackdrop = null;
            _backdropConfig = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = Application.Current.RequestedTheme switch
                {
                    ApplicationTheme.Dark => SystemBackdropTheme.Dark,
                    ApplicationTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                }
            };

            _mica = new MicaController { Kind = MicaKind.Base };
            _mica.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _mica.SetSystemBackdropConfiguration(_backdropConfig);
            return true;
        }
        catch
        {
            ResetBackdropState();
            return false;
        }
    }

    private void TryEnableDesktopAcrylic()
    {
        // SystemBackdrop-based DesktopAcrylic is preferred over legacy controller fallback.
        if (!IsDesktopAcrylicSupported())
        {
            ResetBackdropState();
            return;
        }

        if (!TryCreateSystemBackdrop(static () => new DesktopAcrylicBackdrop()))
        {
            ResetBackdropState();
        }
    }

    private static bool ShouldUseLegacyMicaController()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        // Keep fallback for Windows 10 (19041+) only; avoid dual-path complexity on Windows 11+.
        return OperatingSystem.IsWindowsVersionAtLeast(
                   Windows10LegacyMin.Major,
                   Windows10LegacyMin.Minor,
                   Windows10LegacyMin.Build)
               && !OperatingSystem.IsWindowsVersionAtLeast(
                   Windows11Baseline.Major,
                   Windows11Baseline.Minor,
                   Windows11Baseline.Build);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_backdropConfig is not null)
        {
            _backdropConfig.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private static bool IsMicaSupported()
    {
        try
        {
            return MicaController.IsSupported();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDesktopAcrylicSupported()
    {
        try
        {
            return DesktopAcrylicController.IsSupported();
        }
        catch
        {
            return false;
        }
    }

    private bool TryCreateSystemBackdrop(Func<XamlSystemBackdrop> factory)
    {
        try
        {
            var backdrop = factory();
            return TrySetSystemBackdropSafe(backdrop);
        }
        catch
        {
            ResetBackdropState();
            return false;
        }
    }

    private AppWindow? TryGetAppWindow()
    {
        try
        {
            return AppWindow;
        }
        catch
        {
            return null;
        }
    }

    private void TryConfigureAppWindow()
    {
        var appWindow = TryGetAppWindow();
        if (appWindow is null)
        {
            return;
        }

        try
        {
            appWindow.Resize(new SizeInt32(900, 620));
        }
        catch
        {
            // Ignore sizing failures on unsupported systems.
        }

        TryApplyIcon(appWindow);
    }

    private void TryApplyIcon(AppWindow? appWindow = null)
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (!File.Exists(iconPath))
            {
                return;
            }

            appWindow ??= TryGetAppWindow();
            appWindow?.SetIcon(iconPath);
        }
        catch
        {
            // Ignore icon failures on unsupported systems.
        }
    }

    private static string? ResolveTitleBarIconPath()
    {
        try
        {
            var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
            var appIconPath = Path.Combine(assetsDirectory, "AppIcon.ico");
            if (File.Exists(appIconPath))
            {
                return appIconPath;
            }

            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
            {
                return executablePath;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void InitializeSystemTitleBar()
    {
        if (_isSystemTitleBarInitialized)
        {
            return;
        }

        var appWindow = TryGetAppWindow();
        if (appWindow is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = false;
        }

        SetTitleBar(null);

        if (TryApplySystemTitleBarIcon())
        {
            _isSystemTitleBarInitialized = true;
        }
        else
        {
            _ = DispatcherQueue?.TryEnqueue(InitializeSystemTitleBar);
        }
    }

    private bool TryApplySystemTitleBarIcon()
    {
        if (WindowNative.GetWindowHandle(this) == IntPtr.Zero)
        {
            return false;
        }

        var iconPath = ResolveTitleBarIconPath();
        if (string.IsNullOrEmpty(iconPath))
        {
            return false;
        }

        var appWindow = TryGetAppWindow();
        if (appWindow is null)
        {
            return false;
        }

        try
        {
            appWindow.SetIcon(iconPath);
            appWindow.Title = Title ?? string.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }

}
