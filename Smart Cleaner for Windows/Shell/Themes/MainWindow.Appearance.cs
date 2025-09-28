using System;
using System.IO;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.Interop;
using GdiImageType = Windows.Win32.UI.WindowsAndMessaging.GDI_IMAGE_TYPE;
using LoadImageFlags = Windows.Win32.UI.WindowsAndMessaging.IMAGE_FLAGS;
using XamlSystemBackdrop = Microsoft.UI.Xaml.Media.SystemBackdrop;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow
{
    private void TryEnableMica()
    {
        DisposeBackdropController();

        if (!OperatingSystem.IsWindows())
        {
            SystemBackdrop = null;
            return;
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) &&
            MicaController.IsSupported() &&
            TrySetSystemBackdropSafe(new MicaBackdrop()))
        {
            return;
        }

        if (TryInitializeLegacyMicaController())
        {
            return;
        }

        TryEnableDesktopAcrylic();
    }

    private void DisposeBackdropController()
    {
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
            SystemBackdrop = null;
            return false;
        }
    }

    private bool TryInitializeLegacyMicaController()
    {
        if (!MicaController.IsSupported())
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
            DisposeBackdropController();
            SystemBackdrop = null;
            return false;
        }
    }

    private void TryEnableDesktopAcrylic()
    {
        if (!DesktopAcrylicController.IsSupported())
        {
            SystemBackdrop = null;
            return;
        }

        _ = TrySetSystemBackdropSafe(new DesktopAcrylicBackdrop());
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_backdropConfig is not null)
        {
            _backdropConfig.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private AppWindow? TryGetAppWindow()
    {
        try
        {
            return this.AppWindow;
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

            var tempPath = Path.Combine(Path.GetTempPath(), TitleBarIconTempFileName);
            if (!File.Exists(tempPath))
            {
                var iconBytes = Convert.FromBase64String(TitleBarIconBase64);
                File.WriteAllBytes(tempPath, iconBytes);
            }

            return tempPath;
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
        var hwndValue = WindowNative.GetWindowHandle(this);
        if (hwndValue == IntPtr.Zero)
        {
            return false;
        }

        var hwnd = new HWND(hwndValue);

        var iconPath = ResolveTitleBarIconPath();
        if (!string.IsNullOrEmpty(iconPath))
        {
            var iconHandle = PInvoke.LoadImage(
                default,
                iconPath,
                GdiImageType.IMAGE_ICON,
                0,
                0,
                LoadImageFlags.LR_DEFAULTSIZE | LoadImageFlags.LR_LOADFROMFILE);

            if (iconHandle is not null && !iconHandle.IsInvalid)
            {
                var iconValue = iconHandle.DangerousGetHandle();
                var iconParam = new LPARAM(iconValue);
                var hIcon = new HICON(iconValue);

                try
                {
                    _ = PInvoke.SendMessage(
                        hwnd,
                        PInvoke.WM_SETICON,
                        new WPARAM(PInvoke.ICON_BIG),
                        iconParam);

                    _ = PInvoke.SendMessage(
                        hwnd,
                        PInvoke.WM_SETICON,
                        new WPARAM(PInvoke.ICON_SMALL),
                        iconParam);
                }
                finally
                {
                    _ = PInvoke.DestroyIcon(hIcon);
                    iconHandle.SetHandleAsInvalid();
                    iconHandle.Dispose();
                }
            }
        }

        _ = PInvoke.SetWindowText(hwnd, Title ?? string.Empty);
        return true;
    }

}

