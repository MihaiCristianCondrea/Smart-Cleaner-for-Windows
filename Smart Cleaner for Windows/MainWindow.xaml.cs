using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.VisualBasic.FileIO;
using Smart_Cleaner_for_Windows.Core;
using Smart_Cleaner_for_Windows.Core.DiskCleanup;
using Smart_Cleaner_for_Windows.Core.Storage;
using Smart_Cleaner_for_Windows.Core.LargeFiles;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using GdiImageType = Windows.Win32.UI.WindowsAndMessaging.GDI_IMAGE_TYPE;
using LoadImageFlags = Windows.Win32.UI.WindowsAndMessaging.IMAGE_FLAGS;
using WinRT;
using WinRT.Interop;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow
{
    private readonly IDirectoryCleaner _directoryCleaner;
    private readonly IDiskCleanupService _diskCleanupService;
    private readonly IStorageOverviewService _storageOverviewService;
    private readonly ILargeFileExplorer _largeFileExplorer;
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private List<string> _previewCandidates = new();
    private bool _isBusy;
    private readonly ObservableCollection<DriveUsageViewModel> _driveUsage = new();
    private readonly ObservableCollection<DiskCleanupItemViewModel> _diskCleanupItems = new();
    private readonly ObservableCollection<LargeFileGroupViewModel> _largeFileGroups = new();
    private readonly ObservableCollection<string> _largeFileExclusions = new();
    private readonly HashSet<string> _largeFileExclusionLookup = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private CancellationTokenSource? _diskCleanupCts;
    private CancellationTokenSource? _largeFilesCts;
    private readonly string _diskCleanupVolume;
    private CancellationTokenSource? _storageOverviewCts;
    private bool _isDiskCleanupOperation;
    private bool _isLargeFilesBusy;
    private readonly Dictionary<string, Color> _defaultAccentColors = new();
    private readonly ResourceLoader? _resources = TryCreateResourceLoader();
    private readonly ApplicationDataContainer? _settings = TryGetLocalSettings();
    private bool _isInitializingSettings;
    private string _themePreference = ThemePreferenceDefault;
    private string _accentPreference = AccentPreferenceDefault;
    private bool _cleanerSendToRecycleBin = true;
    private int _cleanerDepthLimit;
    private string _cleanerExclusions = string.Empty;
    private bool _automationAutoPreview;
    private bool _automationWeeklyReminder;
    private bool _notificationShowCompletion = true;
    private bool _notificationDesktopAlerts;
    private int _historyRetentionDays = HistoryRetentionDefaultDays;

    private const string ThemePreferenceKey = "Settings.ThemePreference";
    private const string AccentPreferenceKey = "Settings.AccentPreference";
    private const string ThemePreferenceLight = "light";
    private const string ThemePreferenceDark = "dark";
    private const string ThemePreferenceDefault = "default";
    private const string AccentPreferenceZest = "zest";
    private const string AccentPreferenceDefault = "default";
    private const string CleanerRecyclePreferenceKey = "Settings.Cleaner.SendToRecycleBin";
    private const string CleanerDepthPreferenceKey = "Settings.Cleaner.DepthLimit";
    private const string CleanerExclusionsPreferenceKey = "Settings.Cleaner.Exclusions";
    private const string AutomationAutoPreviewKey = "Settings.Automation.AutoPreview";
    private const string AutomationReminderKey = "Settings.Automation.Reminder";
    private const string NotificationShowCompletionKey = "Settings.Notifications.ShowCompletion";
    private const string NotificationDesktopAlertsKey = "Settings.Notifications.DesktopAlerts";
    private const string HistoryRetentionKey = "Settings.History.RetentionDays";
    private const int HistoryRetentionDefaultDays = 30;
    private const int HistoryRetentionMinDays = 0;
    private const int HistoryRetentionMaxDays = 365;
    private const string LargeFilesExclusionsKey = "LargeFiles.Exclusions";
    private const string TitleBarIconTempFileName = "SmartCleanerTitleIcon.ico";
    private const string TitleBarIconBase64 = """
AAABAAEAEBAAAAEAIABoBAAAFgAAACgAAAAQAAAAIAAAAAEAIAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAADUeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==
""";
    private static readonly string[] AccentResourceKeys = new[]
    {
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3",
    };

    public MainWindow()
        : this(
            DirectoryCleaner.Default,
            DiskCleanupServiceFactory.CreateDefault(),
            new StorageOverviewService(),
            LargeFileExplorer.Default)
    {
    }

    public MainWindow(IDirectoryCleaner directoryCleaner)
        : this(
            directoryCleaner,
            DiskCleanupServiceFactory.CreateDefault(),
            new StorageOverviewService(),
            LargeFileExplorer.Default)
    {
    }

    public MainWindow(IDirectoryCleaner directoryCleaner, IDiskCleanupService diskCleanupService)
        : this(directoryCleaner, diskCleanupService, new StorageOverviewService(), LargeFileExplorer.Default)
    {
    }

    public MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService)
        : this(directoryCleaner, diskCleanupService, storageOverviewService, LargeFileExplorer.Default)
    {
    }

    public MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService,
        ILargeFileExplorer largeFileExplorer)
    {
        _directoryCleaner = directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner));
        _diskCleanupService = diskCleanupService ?? throw new ArgumentNullException(nameof(diskCleanupService));
        _storageOverviewService = storageOverviewService ?? throw new ArgumentNullException(nameof(storageOverviewService));
        _largeFileExplorer = largeFileExplorer ?? throw new ArgumentNullException(nameof(largeFileExplorer));
        _diskCleanupVolume = _diskCleanupService.GetDefaultVolume();

        InitializeComponent();

        LargeFilesGroupList.ItemsSource = _largeFileGroups;

        CaptureDefaultAccentColors();
        LoadPreferences();

        LargeFilesExclusionsList.ItemsSource = _largeFileExclusions;
        LoadLargeFilePreferences();

        SetLargeFilesStatus(
            Symbol.SaveLocal,
            Localize("LargeFilesStatusReadyTitle", "Ready to explore large files"),
            Localize("LargeFilesStatusReadyDescription", "Choose a location to find the biggest files grouped by type."));
        SetLargeFilesActivity(Localize("ActivityIdle", "Waiting for the next action."));
        UpdateLargeFilesSummary();
        UpdateLargeFilesExclusionState();

        DriveUsageList.ItemsSource = _driveUsage;
        _ = UpdateStorageOverviewAsync();

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyleObj) &&
            accentStyleObj is Style accentStyle)
        {
            DeleteBtn.Style = accentStyle;
            DiskCleanupCleanBtn.Style = accentStyle;
            LargeFilesScanBtn.Style = accentStyle;
        }

        SetStatus(
            Symbol.Folder,
            Localize("StatusReadyTitle", "Ready when you are"),
            Localize("StatusReadyDescription", "Select a folder to begin."));
        SetActivity(Localize("ActivityIdle", "Waiting for the next action."));
        UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));

        DiskCleanupList.ItemsSource = _diskCleanupItems;
        DiskCleanupStatusText.Text = LocalizeFormat(
            "DiskCleanupStatusReady",
            "Ready to analyze disk cleanup handlers for {0}.",
            _diskCleanupVolume);
        if (!IsAdministrator())
        {
            DiskCleanupIntro.Text = Localize(
                "DiskCleanupIntro",
                "Analyze Windows cleanup handlers. Some categories require Administrator privileges.");
        }
        UpdateDiskCleanupActionState();

        TryEnableMica();
        ApplyThemePreference(_themePreference, save: false);
        TryConfigureAppWindow();
        Activated += OnWindowActivated;
        Closed += OnClosed;

        NavigateTo(DashboardItem);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Activated -= OnWindowActivated;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _mica?.Dispose();
        _mica = null;
        _backdropConfig = null;
        _diskCleanupCts?.Cancel();
        _diskCleanupCts?.Dispose();
        _diskCleanupCts = null;
        _largeFilesCts?.Cancel();
        _largeFilesCts?.Dispose();
        _largeFilesCts = null;
        _storageOverviewCts?.Cancel();
        _storageOverviewCts?.Dispose();
        _storageOverviewCts = null;
    }

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

    private bool TrySetSystemBackdropSafe(SystemBackdrop backdrop)
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

    private void basicButton_Click(object sender, RoutedEventArgs e)
    {
        CustomTitleBarPanel.Visibility = Visibility.Collapsed;

        var appWindow = TryGetAppWindow();
        if (appWindow is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = false;
        }

        SetTitleBar(null);

        var hwndValue = WindowNative.GetWindowHandle(this);
        if (hwndValue == IntPtr.Zero)
        {
            return;
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
                        new WPARAM((nuint)PInvoke.ICON_BIG),
                        iconParam);

                    _ = PInvoke.SendMessage(
                        hwnd,
                        PInvoke.WM_SETICON,
                        new WPARAM((nuint)PInvoke.ICON_SMALL),
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

    private void customButton_Click(object sender, RoutedEventArgs e)
    {
        CustomTitleBarPanel.Visibility = Visibility.Visible;

        SetTitleBar(CustomTitleBarPanel);

        var appWindow = TryGetAppWindow();
        if (appWindow is null)
        {
            return;
        }

        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
    }

    private void OnNavigationLoaded(object sender, RoutedEventArgs e)
    {
        if (RootNavigation.SelectedItem is null)
        {
            NavigateTo(DashboardItem);
            return;
        }

        ShowPage(RootNavigation.SelectedItem);
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var target = args.IsSettingsSelected ? sender.SettingsItem : args.SelectedItem;
        ShowPage(target);
    }

    private void OnNavigateToEmptyFolders(object sender, RoutedEventArgs e) => NavigateTo(EmptyFoldersItem);

    private void OnNavigateToLargeFiles(object sender, RoutedEventArgs e) => NavigateTo(LargeFilesItem);

    private void OnNavigateToDiskCleanup(object sender, RoutedEventArgs e) => NavigateTo(DiskCleanupItem);

    private void NavigateTo(object? target)
    {
        if (target is null)
        {
            return;
        }

        if (!Equals(RootNavigation.SelectedItem, target))
        {
            RootNavigation.SelectedItem = target;
        }

        ShowPage(target);
    }

    private void ShowPage(object? item)
    {
        var target = item ?? DashboardItem;
        var settingsItem = RootNavigation.SettingsItem;

        var isDashboard = Equals(target, DashboardItem);
        var isEmptyFolders = Equals(target, EmptyFoldersItem);
        var isLargeFiles = Equals(target, LargeFilesItem);
        var isDiskCleanup = Equals(target, DiskCleanupItem);
        var isSettings = settingsItem is not null && Equals(target, settingsItem);

        SetViewVisibility(DashboardView, isDashboard);
        SetViewVisibility(EmptyFoldersView, isEmptyFolders);
        SetViewVisibility(LargeFilesView, isLargeFiles);
        SetViewVisibility(DiskCleanupView, isDiskCleanup);
        SetViewVisibility(SettingsView, isSettings);

        UIElement? activeView = isDashboard
            ? DashboardView
            : isEmptyFolders
                ? EmptyFoldersView
                : isLargeFiles
                    ? LargeFilesView
                    : isDiskCleanup
                        ? DiskCleanupView
                        : isSettings
                            ? SettingsView
                            : null;

        if (activeView is not null)
        {
            PlayEntranceTransition(activeView);
        }

        if (Equals(target, DashboardItem))
        {
            _ = UpdateStorageOverviewAsync();
        }
    }

    private static void SetViewVisibility(UIElement view, bool shouldBeVisible)
    {
        var desired = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
        if (view.Visibility != desired)
        {
            view.Visibility = desired;
        }
    }

    private void PlayEntranceTransition(UIElement view)
    {
        if (view.XamlRoot is null)
        {
            return;
        }

        ElementCompositionPreview.SetIsTranslationEnabled(view, true);

        var visual = ElementCompositionPreview.GetElementVisual(view);
        var compositor = visual.Compositor;

        visual.StopAnimation(nameof(visual.Opacity));
        visual.StopAnimation("Translation");

        visual.Opacity = 0f;

        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
        opacityAnimation.InsertKeyFrame(0f, 0f);
        opacityAnimation.InsertKeyFrame(1f, 1f, easing);
        visual.StartAnimation(nameof(visual.Opacity), opacityAnimation);

        var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
        translationAnimation.Duration = TimeSpan.FromMilliseconds(300);
        translationAnimation.InsertKeyFrame(0f, new Vector3(0f, 16f, 0f));
        translationAnimation.InsertKeyFrame(1f, Vector3.Zero, easing);
        visual.StartAnimation("Translation", translationAnimation);
    }

    private async Task UpdateStorageOverviewAsync()
    {
        _storageOverviewCts?.Cancel();
        _storageOverviewCts?.Dispose();

        var cts = new CancellationTokenSource();
        _storageOverviewCts = cts;

        try
        {
            var result = await _storageOverviewService.GetDriveUsageAsync(cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            _driveUsage.Clear();

            if (result.ReadyDriveCount == 0)
            {
                StorageSummaryText.Text = "No ready drives detected.";
                StorageTipText.Text = "Connect or unlock a drive to view usage details.";
                return;
            }

            if (result.Drives.Count == 0)
            {
                StorageSummaryText.Text = "No accessible drives detected.";
                StorageTipText.Text = "We couldn't read your drive information. Try running the app with higher permissions.";
                return;
            }

            ulong totalCapacity = 0;
            ulong totalFree = 0;
            DriveUsageViewModel? busiestDrive = null;

            foreach (var drive in result.Drives)
            {
                var capacityValue = drive.TotalSize;
                var freeValue = drive.FreeSpace;
                if (freeValue > capacityValue)
                {
                    freeValue = capacityValue;
                }

                totalCapacity += capacityValue;
                totalFree += freeValue;

                var usedValue = capacityValue - freeValue;
                var usedPercentage = capacityValue == 0
                    ? 0
                    : (double)usedValue / capacityValue * 100;

                var displayName = GetDriveDisplayName(drive);
                var details = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} free of {1}",
                    FormatBytes(freeValue),
                    FormatBytes(capacityValue));
                var usageSummary = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:0}% used ({1})",
                    usedPercentage,
                    FormatBytes(usedValue));

                var viewModel = new DriveUsageViewModel(displayName, details, usedPercentage, usageSummary);
                _driveUsage.Add(viewModel);

                if (busiestDrive is null || viewModel.UsedPercentage > busiestDrive.UsedPercentage)
                {
                    busiestDrive = viewModel;
                }
            }

            var driveLabel = _driveUsage.Count == 1 ? "drive" : "drives";
            StorageSummaryText.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Monitoring {0} {1}. {2} free of {3}.",
                _driveUsage.Count,
                driveLabel,
                FormatBytes(totalFree),
                FormatBytes(totalCapacity));

            if (busiestDrive is not null)
            {
                StorageTipText.Text = GetStorageTip(busiestDrive);
            }
            else
            {
                StorageTipText.Text = "Storage tips will appear once drives are detected.";
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellations when refreshing the storage overview.
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _driveUsage.Clear();
            StorageSummaryText.Text = "Storage overview is unavailable.";
            StorageTipText.Text = "We couldn't access drive information. Try again later or adjust your permissions.";
        }
        finally
        {
            if (ReferenceEquals(_storageOverviewCts, cts))
            {
                _storageOverviewCts = null;
            }

            cts.Dispose();
        }
    }

    private static string GetDriveDisplayName(StorageDriveInfo drive)
    {
        var name = drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var label = drive.VolumeLabel;
        if (string.IsNullOrWhiteSpace(label))
        {
            return name;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, label);
    }

    private static string GetStorageTip(DriveUsageViewModel drive)
    {
        var usageDetail = drive.UsageSummary;

        if (drive.UsedPercentage >= 90)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} is running low on space ({1}). Remove large files or move content to free up storage.",
                drive.Name,
                usageDetail);
        }

        if (drive.UsedPercentage >= 75)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} is getting crowded ({1}). Consider cleaning temporary files or uninstalling unused apps.",
                drive.Name,
                usageDetail);
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            "{0} has plenty of room ({1}). Keep performing periodic cleanups to stay optimized.",
            drive.Name,
            usageDetail);
    }


    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            RootPathBox.Text = folder.Path;
            DeleteBtn.IsEnabled = !_isBusy && _previewCandidates.Count > 0;
            SetStatus(
                Symbol.Folder,
                Localize("StatusFolderSelectedTitle", "Folder selected"),
                Localize("StatusFolderSelectedDescription", "Run Preview to identify empty directories."));
            SetActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
            UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));
        }
    }

    private void RootPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        _previewCandidates.Clear();
        Candidates.ItemsSource = null;
        DeleteBtn.IsEnabled = false;
        UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));
    }

    private async void OnPreview(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        if (!TryGetRootPath(out var root))
        {
            ShowInfo(Localize("InfoSelectValidFolder", "Select a valid folder."), InfoBarSeverity.Warning);
            SetStatus(
                Symbol.Important,
                Localize("StatusSelectValidFolderTitle", "Select a valid folder"),
                Localize("StatusSelectValidFolderDescription", "Choose a folder before scanning."));
            UpdateResultsSummary(0, Localize("ResultsNeedValidFolder", "Select a valid folder to run a scan."));
            SetActivity(Localize("ActivityWaitingForValidFolder", "Waiting for a valid folder."));
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        _previewCandidates = new List<string>();
        Candidates.ItemsSource = null;
        DeleteBtn.IsEnabled = false;

        SetBusy(true);
        SetActivity(Localize("ActivityScanning", "Scanning for empty folders…"));
        SetStatus(
            Symbol.Sync,
            Localize("StatusScanningTitle", "Scanning in progress…"),
            Localize("StatusScanningDescription", "Looking for empty folders. You can cancel the scan if needed."));
        UpdateResultsSummary(0, Localize("ResultsScanning", "Scanning for empty folders…"));

        try
        {
            var options = CreateOptions(dryRun: true);
            var result = await _directoryCleaner.CleanAsync(root, options, _cts.Token);

            _previewCandidates = new List<string>(result.EmptyDirectories);
            Candidates.ItemsSource = _previewCandidates;
            DeleteBtn.IsEnabled = !_isBusy && _previewCandidates.Count > 0;

            var hasResults = result.EmptyFound > 0;
            var resultsMessage = result.HasFailures
                ? Localize("ResultsMissingDueToAccess", "Some folders might be missing from the preview due to access issues.")
                : hasResults
                    ? Localize("ResultsReadyToReview", "Review the folders below before cleaning.")
                    : Localize("ResultsNoneDetected", "No empty folders were detected for this location.");
            UpdateResultsSummary(result.EmptyFound, resultsMessage);

            var message = LocalizeFormat("InfoFoundEmptyFolders", "Found {0} empty folder(s).", result.EmptyFound);
            var severity = InfoBarSeverity.Informational;
            if (result.HasFailures)
            {
                message += " " + LocalizeFormat("InfoEncounteredIssues", "Encountered {0} issue(s).", result.Failures.Count);
                var failureSummaries = result.Failures
                    .Take(3)
                    .Select(f => $"• {f.Path}: {f.Exception.Message}");

                message += Environment.NewLine + string.Join(Environment.NewLine, failureSummaries);

                if (result.Failures.Count > 3)
                {
                    message += Environment.NewLine + LocalizeFormat(
                        "InfoAdditionalIssues",
                        "…and {0} more.",
                        result.Failures.Count - 3);
                }

                severity = InfoBarSeverity.Warning;
            }

            var statusTitle = result.HasFailures
                ? Localize("StatusScanWarningsTitle", "Scan completed with warnings")
                : hasResults
                    ? LocalizeFormat("StatusFoundEmptyFoldersTitle", "Found {0} empty folder(s)", result.EmptyFound)
                    : Localize("StatusNoEmptyFoldersTitle", "No empty folders detected");
            var statusDescription = result.HasFailures
                ? Localize("StatusScanWarningsDescription", "Some items could not be analyzed. Review the message below.")
                : hasResults
                    ? Localize("StatusScanHasResultsDescription", "Review the folders list below before cleaning.")
                    : Localize("StatusScanCleanDescription", "Everything looks tidy. Try adjusting filters if you expected more.");
            var statusSymbol = result.HasFailures
                ? Symbol.Important
                : hasResults
                    ? Symbol.View
                    : Symbol.Accept;
            int? badgeValue = hasResults ? result.EmptyFound : null;

            SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
            SetActivity(Localize("ActivityScanComplete", "Scan complete."));

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            SetActivity(Localize("ActivityScanCancelled", "Scan cancelled."));
            SetStatus(
                Symbol.Cancel,
                Localize("StatusScanCancelledTitle", "Scan cancelled"),
                Localize("StatusScanCancelledDescription", "Preview was cancelled. Adjust settings or try again."));
            UpdateResultsSummary(0, Localize("ResultsScanCancelled", "Preview was cancelled. Run Preview to refresh the list."));
            ShowInfo(Localize("InfoPreviewCancelled", "Preview cancelled."), InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            SetActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
            SetStatus(
                Symbol.Important,
                Localize("StatusScanFailedTitle", "Scan failed"),
                Localize("StatusScanFailedDescription", "An unexpected error occurred. Review the message below."));
            UpdateResultsSummary(0, Localize("ResultsScanFailed", "The scan failed. Review the message above and try again."));
            ShowInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        if (!TryGetRootPath(out var root))
        {
            ShowInfo(Localize("InfoSelectValidFolder", "Select a valid folder."), InfoBarSeverity.Warning);
            SetStatus(
                Symbol.Important,
                Localize("StatusSelectValidFolderTitle", "Select a valid folder"),
                Localize("StatusSelectValidFolderForCleaningDescription", "Choose a folder before cleaning."));
            UpdateResultsSummary(0, Localize("ResultsNeedValidFolderForCleaning", "Select a valid folder before cleaning."));
            SetActivity(Localize("ActivityWaitingForValidFolder", "Waiting for a valid folder."));
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        SetBusy(true);
        SetActivity(Localize("ActivityCleaning", "Cleaning empty folders…"));
        var pendingCount = _previewCandidates.Count;
        int? pendingBadge = pendingCount > 0 ? pendingCount : null;
        SetStatus(
            Symbol.Delete,
            Localize("StatusCleaningTitle", "Cleaning in progress…"),
            Localize("StatusCleaningDescription", "Removing empty folders safely. You can cancel the operation if needed."),
            pendingBadge);
        UpdateResultsSummary(pendingCount, pendingCount > 0
            ? Localize("ResultsCleaningProgressWithPreview", "Cleaning in progress. We'll refresh the preview afterwards.")
            : Localize("ResultsCleaningProgress", "Cleaning in progress…"));

        try
        {
            var options = CreateOptions(dryRun: false);
            var result = await _directoryCleaner.CleanAsync(root, options, _cts.Token);

            _previewCandidates.Clear();
            Candidates.ItemsSource = null;
            DeleteBtn.IsEnabled = false;

            var message = result.EmptyFound == 0
                ? Localize("InfoNoEmptyFoldersDetected", "No empty folders detected.")
                : LocalizeFormat("InfoDeletedFolders", "Deleted {0} folder(s).", result.DeletedCount);
            var severity = result.EmptyFound == 0 ? InfoBarSeverity.Informational : InfoBarSeverity.Success;

            if (result.EmptyFound > result.DeletedCount)
            {
                var remaining = result.EmptyFound - result.DeletedCount;
                message += " " + LocalizeFormat("InfoItemsNotRemoved", "{0} item(s) could not be removed.", remaining);
            }

            if (result.HasFailures)
            {
                message += " " + LocalizeFormat("InfoEncounteredIssues", "Encountered {0} issue(s).", result.Failures.Count);
                severity = InfoBarSeverity.Warning;
            }

            int? badgeValue = result.DeletedCount > 0 ? result.DeletedCount : null;
            var statusSymbol = result.HasFailures || result.EmptyFound > result.DeletedCount
                ? Symbol.Important
                : result.DeletedCount > 0
                    ? Symbol.Accept
                    : Symbol.Message;
            var statusTitle = result.HasFailures
                ? Localize("StatusCleanWarningsTitle", "Clean completed with warnings")
                : result.EmptyFound == 0
                    ? Localize("StatusNoEmptyFoldersTitle", "No empty folders detected")
                    : result.EmptyFound > result.DeletedCount
                        ? Localize("StatusCleanPartialTitle", "Some folders could not be removed")
                        : LocalizeFormat("StatusCleanRemovedTitle", "Removed {0} folder(s)", result.DeletedCount);
            var statusDescription = result.HasFailures
                ? Localize("StatusCleanWarningsDescription", "Some folders could not be removed. Review the message below.")
                : result.EmptyFound == 0
                    ? Localize("StatusCleanNoResultsDescription", "Run Preview to check another location.")
                    : result.EmptyFound > result.DeletedCount
                        ? Localize("StatusCleanPartialDescription", "Some items remain because they could not be deleted.")
                        : Localize("StatusCleanSuccessDescription", "Your workspace is tidier. Run Preview again to double-check.");

            SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
            SetActivity(Localize("ActivityCleanComplete", "Clean complete."));
            UpdateResultsSummary(0, result.DeletedCount > 0
                ? Localize("ResultsCleanCompleted", "Clean completed. Run Preview again to scan another location.")
                : Localize("ResultsCleanNoRemovals", "No empty folders were removed. Run Preview to check again."));

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            SetActivity(Localize("ActivityCleanCancelled", "Clean cancelled."));
            SetStatus(
                Symbol.Cancel,
                Localize("StatusCleanCancelledTitle", "Clean cancelled"),
                Localize("StatusCleanCancelledDescription", "Deletion was cancelled. Preview again when you're ready."));
            UpdateResultsSummary(0, Localize("ResultsCleanCancelled", "Clean cancelled. Run Preview to refresh the list."));
            ShowInfo(Localize("InfoDeletionCancelled", "Deletion cancelled."), InfoBarSeverity.Informational);
        }
        catch (UnauthorizedAccessException)
        {
            SetActivity(Localize("ActivityPermissionRequired", "Permission required."));
            SetStatus(
                Symbol.Important,
                Localize("StatusAccessDeniedTitle", "Access denied"),
                Localize("StatusAccessDeniedDescription", "Run the app as Administrator to remove protected folders."));
            UpdateResultsSummary(0, Localize("ResultsAccessDenied", "Some folders could not be removed due to permissions."));
            ShowInfo(Localize("InfoAccessDenied", "Access denied. Try running as Administrator."), InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            SetActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
            SetStatus(
                Symbol.Important,
                Localize("StatusCleanFailedTitle", "Clean failed"),
                Localize("StatusCleanFailedDescription", "An unexpected error occurred. Review the message below."));
            UpdateResultsSummary(0, Localize("ResultsCleanFailed", "Cleaning failed. Review the details and try again."));
            ShowInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => CancelActiveOperation();

    private void CancelActiveOperation()
    {
        var cancelled = false;

        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            cancelled = true;
        }

        if (_diskCleanupCts is { IsCancellationRequested: false })
        {
            _diskCleanupCts.Cancel();
            cancelled = true;
        }

        if (_largeFilesCts is { IsCancellationRequested: false })
        {
            _largeFilesCts.Cancel();
            cancelled = true;
        }

        if (cancelled && (_isBusy || _isDiskCleanupOperation))
        {
            SetActivity(Localize("ActivityCancelling", "Cancelling current operation…"));
        }

        if (cancelled && _isLargeFilesBusy)
        {
            SetLargeFilesActivity(Localize("ActivityCancelling", "Cancelling current operation…"));
        }
    }

    private bool TryGetRootPath(out string root)
    {
        root = RootPathBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    private DirectoryCleanOptions CreateOptions(bool dryRun)
    {
        var depthValue = DepthBox.Value;
        int? maxDepth = null;
        if (!double.IsNaN(depthValue))
        {
            var depth = (int)Math.Max(0, Math.Round(depthValue));
            if (depth > 0)
            {
                maxDepth = depth;
            }
        }

        return new DirectoryCleanOptions
        {
            DryRun = dryRun,
            SendToRecycleBin = RecycleChk.IsChecked == true,
            SkipReparsePoints = true,
            DeleteRootWhenEmpty = false,
            MaxDepth = maxDepth,
            ExcludedNamePatterns = ParseExclusions(ExcludeBox.Text),
        };
    }

    private static IReadOnlyCollection<string> ParseExclusions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool ParseBoolSetting(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric != 0;
        }

        return defaultValue;
    }

    private static int ParseIntSetting(string? value, int defaultValue, int min, int max)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            defaultValue = parsed;
        }

        return Math.Clamp(defaultValue, min, max);
    }

    private void ApplyCleanerDefaultsToSession()
    {
        if (RecycleChk is not null)
        {
            RecycleChk.IsChecked = _cleanerSendToRecycleBin;
        }

        if (DepthBox is not null)
        {
            DepthBox.Value = _cleanerDepthLimit;
        }

        if (ExcludeBox is not null)
        {
            ExcludeBox.Text = _cleanerExclusions;
        }
    }

    private void UpdateCleanerDefaultsSummary()
    {
        if (CleanerDefaultsSummaryText is null)
        {
            return;
        }

        var recycleText = _cleanerSendToRecycleBin
            ? Localize("SettingsCleanerDefaultsRecycle", "Recycle Bin")
            : Localize("SettingsCleanerDefaultsPermanent", "Permanent delete");
        var depthText = _cleanerDepthLimit > 0
            ? string.Format(
                CultureInfo.CurrentCulture,
                Localize("SettingsCleanerDefaultsDepth", "Depth limit {0}"),
                _cleanerDepthLimit)
            : Localize("SettingsCleanerDefaultsNoDepth", "No depth limit");

        CleanerDefaultsSummaryText.Text = string.Format(
            CultureInfo.CurrentCulture,
            Localize("SettingsCleanerDefaultsSummary", "{0} • {1}"),
            recycleText,
            depthText);
    }

    private void UpdateAutomationSummary()
    {
        if (AutomationSummaryText is null)
        {
            return;
        }

        var descriptors = new List<string>();
        if (_automationAutoPreview)
        {
            descriptors.Add(Localize("SettingsAutomationAutoPreview", "Auto preview"));
        }

        if (_automationWeeklyReminder)
        {
            descriptors.Add(Localize("SettingsAutomationWeeklyReminder", "Weekly reminders"));
        }

        AutomationSummaryText.Text = descriptors.Count > 0
            ? string.Join(" • ", descriptors)
            : Localize("SettingsAutomationDisabled", "Automation disabled");
    }

    private void UpdateNotificationSummary()
    {
        if (NotificationSummaryText is null)
        {
            return;
        }

        string summary;
        if (_notificationShowCompletion && _notificationDesktopAlerts)
        {
            summary = Localize("SettingsNotificationsAll", "All notifications enabled");
        }
        else if (_notificationShowCompletion)
        {
            summary = Localize("SettingsNotificationsCompletionOnly", "Completion summary only");
        }
        else if (_notificationDesktopAlerts)
        {
            summary = Localize("SettingsNotificationsDesktopOnly", "Desktop alerts only");
        }
        else
        {
            summary = Localize("SettingsNotificationsMuted", "Notifications muted");
        }

        NotificationSummaryText.Text = summary;
    }

    private void UpdateHistoryRetentionSummary()
    {
        if (HistoryRetentionSummaryText is null)
        {
            return;
        }

        var summary = _historyRetentionDays > 0
            ? string.Format(
                CultureInfo.CurrentCulture,
                Localize("SettingsHistoryRetentionSummary", "Keep history for {0} day(s)"),
                _historyRetentionDays)
            : Localize("SettingsHistoryRetentionOff", "Do not keep history");

        HistoryRetentionSummaryText.Text = summary;
    }

    private void ShowCleanerDefaultsInfo(string message, InfoBarSeverity severity)
    {
        if (CleanerDefaultsInfoBar is null)
        {
            return;
        }

        CleanerDefaultsInfoBar.Message = message;
        CleanerDefaultsInfoBar.Severity = severity;
        CleanerDefaultsInfoBar.IsOpen = true;
    }

    private async void OnLargeFilesBrowse(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            LargeFilesRootPathBox.Text = folder.Path;
            ClearLargeFilesResults();
            SetLargeFilesStatus(
                Symbol.SaveLocal,
                Localize("LargeFilesStatusFolderSelectedTitle", "Folder selected"),
                Localize("LargeFilesStatusFolderSelectedDescription", "Run Scan to find the largest files in this location."));
            SetLargeFilesActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
        }
    }

    private void OnLargeFilesRootPathChanged(object sender, TextChangedEventArgs e)
    {
        LargeFilesInfoBar.IsOpen = false;

        if (_isLargeFilesBusy)
        {
            return;
        }

        ClearLargeFilesResults();
        if (!string.IsNullOrWhiteSpace(LargeFilesRootPathBox.Text))
        {
            SetLargeFilesStatus(
                Symbol.SaveLocal,
                Localize("LargeFilesStatusFolderSelectedTitle", "Folder selected"),
                Localize("LargeFilesStatusFolderSelectedDescription", "Run Scan to find the largest files in this location."));
        }
        else
        {
            SetLargeFilesStatus(
                Symbol.SaveLocal,
                Localize("LargeFilesStatusReadyTitle", "Ready to explore large files"),
                Localize("LargeFilesStatusReadyDescription", "Choose a location to find the biggest files grouped by type."));
        }
    }

    private async void OnLargeFilesScan(object sender, RoutedEventArgs e)
    {
        if (_isLargeFilesBusy)
        {
            ShowLargeFilesInfo(Localize("LargeFilesInfoScanInProgress", "Finish the current scan before starting a new one."), InfoBarSeverity.Warning);
            return;
        }

        LargeFilesInfoBar.IsOpen = false;

        if (!TryGetLargeFilesRoot(out var root))
        {
            ShowLargeFilesInfo(Localize("LargeFilesInfoSelectValidFolder", "Select a valid folder to scan."), InfoBarSeverity.Warning);
            SetLargeFilesStatus(
                Symbol.Important,
                Localize("LargeFilesStatusSelectValidFolderTitle", "Select a valid folder"),
                Localize("LargeFilesStatusSelectValidFolderDescription", "Choose a folder before running the scan."));
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsNeedValidFolder", "Select a valid folder to run a scan."));
            return;
        }

        _largeFilesCts?.Cancel();
        _largeFilesCts?.Dispose();
        _largeFilesCts = new CancellationTokenSource();

        ClearLargeFilesResults();

        SetLargeFilesBusy(true);
        SetLargeFilesActivity(Localize("LargeFilesActivityScanning", "Scanning for large files…"));
        SetLargeFilesResultsCaption(Localize("LargeFilesResultsScanning", "Scanning for large files…"));
        SetLargeFilesStatus(
            Symbol.Sync,
            Localize("LargeFilesStatusScanningTitle", "Scanning for large files…"),
            Localize("LargeFilesStatusScanningDescription", "Looking for the largest files. You can cancel the scan if needed."));

        try
        {
            var options = CreateLargeFileOptions();
            var result = await _largeFileExplorer.ScanAsync(root, options, _largeFilesCts.Token);

            ApplyLargeFileScanResult(result);

            var rootLabel = Path.GetFileName(root);
            if (string.IsNullOrEmpty(rootLabel))
            {
                rootLabel = root;
            }

            if (result.FileCount > 0)
            {
                SetLargeFilesStatus(
                    Symbol.Accept,
                    Localize("LargeFilesStatusResultsTitle", "Review the largest files"),
                    LocalizeFormat("LargeFilesStatusResultsDescription", "Top {0} files found in {1}.", FormatFileCount(result.FileCount), rootLabel),
                    result.FileCount);
                UpdateLargeFilesResultsCaption(result.FileCount, result.HasFailures);
            }
            else
            {
                SetLargeFilesStatus(
                    Symbol.Library,
                    Localize("LargeFilesStatusNoResultsTitle", "No large files detected"),
                    Localize("LargeFilesStatusNoResultsDescription", "Try adjusting the filters or scanning another location."));
                SetLargeFilesResultsCaption(Localize("LargeFilesResultsNone", "No large files were detected for this location."));
            }

            if (result.HasFailures)
            {
                var message = LocalizeFormat("LargeFilesInfoFailures", "Encountered {0} issue(s) while scanning.", result.Failures.Count);
                var failureSummaries = result.Failures
                    .Take(3)
                    .Select(failure => string.Format(CultureInfo.CurrentCulture, "• {0}: {1}", failure.Path, failure.Exception.Message));
                var details = string.Join(Environment.NewLine, failureSummaries);
                if (!string.IsNullOrEmpty(details))
                {
                    message += Environment.NewLine + details;
                }
                ShowLargeFilesInfo(message, InfoBarSeverity.Warning);
            }

            SetLargeFilesActivity(Localize("LargeFilesActivityScanComplete", "Large file scan complete."));
        }
        catch (OperationCanceledException)
        {
            SetLargeFilesActivity(Localize("ActivityScanCancelled", "Scan cancelled."));
            SetLargeFilesStatus(
                Symbol.Cancel,
                Localize("LargeFilesStatusCancelledTitle", "Scan cancelled"),
                Localize("LargeFilesStatusCancelledDescription", "The large files scan was cancelled. Run it again when you're ready."));
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsCancelled", "Scan cancelled. Run Scan again to refresh the list."));
        }
        catch (Exception ex)
        {
            SetLargeFilesActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
            SetLargeFilesStatus(
                Symbol.Important,
                Localize("LargeFilesStatusErrorTitle", "Scan failed"),
                Localize("LargeFilesStatusErrorDescription", "Something went wrong. Review the details below and try again."));
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsError", "Scan failed. Review the details below."));
            ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoScanFailed", "Scan failed: {0}"), ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            SetLargeFilesBusy(false);
            _largeFilesCts?.Dispose();
            _largeFilesCts = null;
        }
    }

    private void OnLargeFilesCancel(object sender, RoutedEventArgs e)
    {
        if (_largeFilesCts is { IsCancellationRequested: false })
        {
            _largeFilesCts.Cancel();
            SetLargeFilesActivity(Localize("ActivityCancelling", "Cancelling current operation…"));
        }
    }

    private void OnLargeFileOpen(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not LargeFileItemViewModel item)
        {
            return;
        }

        try
        {
            if (!File.Exists(item.Path))
            {
                RemoveLargeFileItem(item);
                ShowLargeFilesInfo(Localize("LargeFilesInfoFileMissing", "The file is no longer available."), InfoBarSeverity.Warning);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoOpenFailed", "Couldn't open the file: {0}"), ex.Message), InfoBarSeverity.Error);
        }
    }

    private void OnLargeFileDelete(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not LargeFileItemViewModel item)
        {
            return;
        }

        try
        {
            if (!File.Exists(item.Path))
            {
                RemoveLargeFileItem(item);
                ShowLargeFilesInfo(Localize("LargeFilesInfoFileMissing", "The file is no longer available."), InfoBarSeverity.Warning);
                return;
            }

            var recycleMode = LargeFilesRecycleChk.IsChecked == true
                ? RecycleOption.SendToRecycleBin
                : RecycleOption.DeletePermanently;
            FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, recycleMode);
            RemoveLargeFileItem(item);
            ShowLargeFilesInfo(Localize("LargeFilesInfoDeleted", "File deleted successfully."), InfoBarSeverity.Success);
            SetLargeFilesActivity(Localize("LargeFilesActivityFileDeleted", "File removed."));
        }
        catch (Exception ex)
        {
            ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoDeleteFailed", "Couldn't delete the file: {0}"), ex.Message), InfoBarSeverity.Error);
        }
    }

    private void OnLargeFileExclude(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not LargeFileItemViewModel item)
        {
            return;
        }

        if (AddLargeFileExclusion(item.Path))
        {
            RemoveLargeFileItem(item);
            ShowLargeFilesInfo(Localize("LargeFilesInfoExcluded", "Excluded from future scans."), InfoBarSeverity.Success);
            SetLargeFilesActivity(Localize("LargeFilesActivityFileExcluded", "File excluded from future scans."));
        }
    }

    private void OnLargeFilesRemoveExclusion(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string path)
        {
            return;
        }

        RemoveLargeFileExclusion(path);
        ShowLargeFilesInfo(Localize("LargeFilesInfoRemovedExclusion", "Removed from exclusions."), InfoBarSeverity.Informational);
        SetLargeFilesActivity(Localize("LargeFilesActivityRemovedExclusion", "Exclusion removed."));
    }

    private void OnLargeFilesClearExclusions(object sender, RoutedEventArgs e)
    {
        if (_largeFileExclusions.Count == 0)
        {
            return;
        }

        _largeFileExclusions.Clear();
        _largeFileExclusionLookup.Clear();
        PersistLargeFileExclusions();
        UpdateLargeFilesExclusionState();
        ShowLargeFilesInfo(Localize("LargeFilesInfoClearedExclusions", "Cleared all exclusions."), InfoBarSeverity.Success);
        SetLargeFilesActivity(Localize("LargeFilesActivityExclusionsCleared", "Exclusion list cleared."));
    }

    private bool TryGetLargeFilesRoot(out string root)
    {
        root = LargeFilesRootPathBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    private LargeFileScanOptions CreateLargeFileOptions()
    {
        var includeSubfolders = LargeFilesIncludeSubfoldersCheck.IsChecked != false;
        var maxItemsValue = LargeFilesMaxItemsBox.Value;
        var maxItems = 100;
        if (!double.IsNaN(maxItemsValue))
        {
            maxItems = (int)Math.Max(1, Math.Round(maxItemsValue));
        }

        return new LargeFileScanOptions
        {
            IncludeSubdirectories = includeSubfolders,
            SkipReparsePoints = true,
            MaxResults = maxItems,
            ExcludedNamePatterns = ParseExclusions(LargeFilesExclusionsBox.Text),
            ExcludedFullPaths = _largeFileExclusions.ToList(),
        };
    }

    private void ClearLargeFilesResults()
    {
        _largeFileGroups.Clear();
        LargeFilesInfoBar.IsOpen = false;
        SetLargeFilesResultsCaption(Localize("LargeFilesResultsPlaceholder", "Scan results will appear here after you run a scan."));
        LargeFilesResultBadge.ClearValue(InfoBadge.ValueProperty);
        LargeFilesResultBadge.Visibility = Visibility.Collapsed;
        UpdateLargeFilesSummary();
    }

    private void ApplyLargeFileScanResult(LargeFileScanResult result)
    {
        _largeFileGroups.Clear();

        var groups = result.Files
            .GroupBy(file => file.Type)
            .Select(group => new
            {
                Name = group.Key,
                Entries = group.OrderByDescending(entry => entry.Size).ToList(),
                Total = group.Aggregate(0L, (current, entry) => current + Math.Max(0L, entry.Size))
            })
            .OrderByDescending(group => group.Total)
            .ThenBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase);

        foreach (var group in groups)
        {
            if (group.Entries.Count == 0)
            {
                continue;
            }

            var viewModel = new LargeFileGroupViewModel(this, group.Name);
            foreach (var entry in group.Entries)
            {
                var extensionLabel = string.IsNullOrEmpty(entry.Extension)
                    ? Localize("LargeFilesNoExtensionLabel", "No extension")
                    : entry.Extension.ToUpperInvariant();
                var item = new LargeFileItemViewModel(entry, extensionLabel);
                viewModel.AddItem(item);
            }

            if (viewModel.ItemCount > 0)
            {
                _largeFileGroups.Add(viewModel);
            }
        }

        UpdateLargeFilesSummary();
    }

    private void UpdateLargeFilesResultsCaption(int count, bool hasFailures)
    {
        if (count == 0)
        {
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsNone", "No large files were detected for this location."));
            return;
        }

        if (hasFailures)
        {
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsWithIssues", "Some results may be missing due to access issues. Review the largest files below."));
        }
        else
        {
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsReady", "Review the largest files below before taking action."));
        }
    }

    private void SetLargeFilesResultsCaption(string message) => LargeFilesResultsCaption.Text = message;

    private void UpdateLargeFilesSummary()
    {
        var totalCount = _largeFileGroups.Sum(group => group.ItemCount);
        var totalBytes = _largeFileGroups.Aggregate(0L, (current, group) => current + Math.Max(0L, group.TotalBytes));

        if (totalCount == 0)
        {
            LargeFilesSummaryText.Text = Localize("LargeFilesSummaryPlaceholder", "No scan results yet.");
            LargeFilesResultBadge.ClearValue(InfoBadge.ValueProperty);
            LargeFilesResultBadge.Visibility = Visibility.Collapsed;
            return;
        }

        LargeFilesSummaryText.Text = string.Format(
            CultureInfo.CurrentCulture,
            Localize("LargeFilesSummaryDetails", "{0} • {1}"),
            FormatFileCount(totalCount),
            FormatBytes((ulong)Math.Max(0L, totalBytes)));
        LargeFilesResultBadge.Value = totalCount;
        LargeFilesResultBadge.Visibility = Visibility.Visible;
    }

    private void SetLargeFilesStatus(Symbol symbol, string title, string description, int? badgeValue = null)
    {
        LargeFilesStatusGlyph.Symbol = symbol;
        LargeFilesStatusTitle.Text = title;
        LargeFilesStatusDescription.Text = description;
        LargeFilesStatusHero.Background = GetStatusHeroBrush(symbol);
        LargeFilesStatusGlyph.Foreground = GetStatusGlyphBrush(symbol);

        if (badgeValue.HasValue && badgeValue.Value > 0)
        {
            LargeFilesResultBadge.Value = badgeValue.Value;
            LargeFilesResultBadge.Visibility = Visibility.Visible;
        }
    }

    private void SetLargeFilesActivity(string message)
    {
        LargeFilesActivityText.Text = message;
    }

    private void SetLargeFilesBusy(bool isBusy)
    {
        _isLargeFilesBusy = isBusy;
        LargeFilesProgress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        LargeFilesProgress.IsIndeterminate = isBusy;
        LargeFilesCancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        LargeFilesCancelBtn.IsEnabled = isBusy;
        LargeFilesScanBtn.IsEnabled = !isBusy;
        LargeFilesBrowseBtn.IsEnabled = !isBusy;
        LargeFilesRootPathBox.IsEnabled = !isBusy;
        LargeFilesIncludeSubfoldersCheck.IsEnabled = !isBusy;
        LargeFilesRecycleChk.IsEnabled = !isBusy;
        LargeFilesMaxItemsBox.IsEnabled = !isBusy;
        LargeFilesExclusionsBox.IsEnabled = !isBusy;
        LargeFilesExclusionsList.IsEnabled = !isBusy;
        LargeFilesGroupList.IsEnabled = !isBusy;
        UpdateLargeFilesExclusionState();
    }

    private void ShowLargeFilesInfo(string message, InfoBarSeverity severity)
    {
        LargeFilesInfoBar.Message = message;
        LargeFilesInfoBar.Severity = severity;
        LargeFilesInfoBar.IsOpen = true;
    }

    private void LoadLargeFilePreferences()
    {
        var saved = ReadSetting(LargeFilesExclusionsKey);
        if (string.IsNullOrWhiteSpace(saved))
        {
            return;
        }

        var entries = saved.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            _ = AddLargeFileExclusion(entry, save: false, showMessageOnError: false);
        }

        UpdateLargeFilesExclusionState();
    }

    private void PersistLargeFileExclusions()
    {
        var serialized = string.Join('\n', _largeFileExclusions);
        SaveSetting(LargeFilesExclusionsKey, serialized);
    }

    private bool AddLargeFileExclusion(string path, bool save = true, bool showMessageOnError = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var normalized = NormalizeLargeFilePath(path);
            if (_largeFileExclusionLookup.Contains(normalized))
            {
                if (showMessageOnError)
                {
                    ShowLargeFilesInfo(Localize("LargeFilesInfoAlreadyExcluded", "That file is already excluded."), InfoBarSeverity.Informational);
                }

                return false;
            }

            _largeFileExclusionLookup.Add(normalized);
            _largeFileExclusions.Add(normalized);

            if (save)
            {
                PersistLargeFileExclusions();
            }

            UpdateLargeFilesExclusionState();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            if (showMessageOnError)
            {
                ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoExcludeFailed", "Couldn't add exclusion: {0}"), ex.Message), InfoBarSeverity.Error);
            }

            return false;
        }
    }

    private void RemoveLargeFileExclusion(string path, bool save = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalized;
        try
        {
            normalized = NormalizeLargeFilePath(path);
        }
        catch
        {
            normalized = path;
        }

        var comparer = _largeFileExclusionLookup.Comparer ?? (OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        for (var i = _largeFileExclusions.Count - 1; i >= 0; i--)
        {
            if (comparer.Equals(_largeFileExclusions[i], normalized))
            {
                _largeFileExclusions.RemoveAt(i);
                break;
            }
        }

        _largeFileExclusionLookup.Remove(normalized);

        if (save)
        {
            PersistLargeFileExclusions();
        }

        UpdateLargeFilesExclusionState();
    }

    private void UpdateLargeFilesExclusionState()
    {
        var hasExclusions = _largeFileExclusions.Count > 0;
        LargeFilesNoExclusionsText.Visibility = hasExclusions ? Visibility.Collapsed : Visibility.Visible;
        LargeFilesClearExclusionsBtn.IsEnabled = hasExclusions && !_isLargeFilesBusy;
    }

    private static string NormalizeLargeFilePath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private string FormatFileCount(int count) => count == 1
        ? LocalizeFormat("LargeFilesSingleFileLabel", "{0} file", count)
        : LocalizeFormat("LargeFilesMultipleFileLabel", "{0} files", count);

    private void RemoveLargeFileItem(LargeFileItemViewModel item)
    {
        if (item is null)
        {
            return;
        }

        LargeFileGroupViewModel? emptyGroup = null;

        foreach (var group in _largeFileGroups)
        {
            if (group.RemoveItem(item))
            {
                if (group.ItemCount == 0)
                {
                    emptyGroup = group;
                }

                break;
            }
        }

        if (emptyGroup is not null)
        {
            _largeFileGroups.Remove(emptyGroup);
        }

        UpdateLargeFilesSummary();

        if (_largeFileGroups.Sum(group => group.ItemCount) == 0)
        {
            SetLargeFilesStatus(
                Symbol.SaveLocal,
                Localize("LargeFilesStatusReadyTitle", "Ready to explore large files"),
                Localize("LargeFilesStatusReadyDescription", "Choose a location to find the biggest files grouped by type."));
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsPlaceholder", "Scan results will appear here after you run a scan."));
        }
    }

    private string Localize(string key, string fallback)
    {
        var resources = _resources;
        if (resources is null)
        {
            return fallback;
        }

        try
        {
            var value = resources.GetString(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private string LocalizeFormat(string key, string fallback, params object[] args)
    {
        var format = Localize(key, fallback);
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private void SetStatus(Symbol symbol, string title, string description, int? badgeValue = null)
    {
        StatusGlyph.Symbol = symbol;
        StatusTitle.Text = title;
        StatusDescription.Text = description;
        StatusHero.Background = GetStatusHeroBrush(symbol);
        StatusGlyph.Foreground = GetStatusGlyphBrush(symbol);

        if (badgeValue.HasValue && badgeValue.Value > 0)
        {
            ResultBadge.Value = badgeValue.Value;
            ResultBadge.Visibility = Visibility.Visible;
        }
        else
        {
            ResultBadge.ClearValue(InfoBadge.ValueProperty);
            ResultBadge.Visibility = Visibility.Collapsed;
        }
    }

    private Brush GetStatusHeroBrush(Symbol symbol) => symbol switch
    {
        Symbol.Accept => GetBrushResource("Brush.Hero.Positive", "Brush.Hero.Neutral"),
        Symbol.Sync or Symbol.Delete => GetBrushResource("Brush.Hero.Warning", "Brush.Hero.Neutral"),
        Symbol.Important or Symbol.Cancel => GetBrushResource("Brush.Hero.Critical", "Brush.Hero.Neutral"),
        _ => GetBrushResource("Brush.Hero.Neutral"),
    };

    private Brush GetStatusGlyphBrush(Symbol symbol) => symbol switch
    {
        Symbol.Accept => GetBrushResource("Brush.SharedPositive", "Brush.BrandPrimary"),
        Symbol.Sync or Symbol.Delete => GetBrushResource("Brush.SharedCaution", "Brush.BrandSecondary"),
        Symbol.Important or Symbol.Cancel => GetBrushResource("Brush.SharedCritical", "Brush.BrandSecondary"),
        _ => GetBrushResource("Brush.BrandSecondary"),
    };

    private Brush GetBrushResource(string key, string? fallbackKey = null)
    {
        if (Application.Current.Resources.TryGetValue(key, out var brushObj) && brushObj is Brush brush)
        {
            return brush;
        }

        if (fallbackKey is not null &&
            Application.Current.Resources.TryGetValue(fallbackKey, out var fallbackObj) &&
            fallbackObj is Brush fallbackBrush)
        {
            return fallbackBrush;
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    private void UpdateResultsSummary(int count, string? customMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            ResultsCaption.Text = customMessage;
            return;
        }

        ResultsCaption.Text = count > 0
            ? Localize("ResultsAvailable", "Review the folders below before cleaning.")
            : Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan.");
    }

    private void SetActivity(string message)
    {
        ActivityText.Text = message;
        DiskCleanupActivityText.Text = message;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        Progress.IsIndeterminate = isBusy;
        CancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        CancelBtn.IsEnabled = isBusy;
        var showDiskCleanupCancel = isBusy && _isDiskCleanupOperation;
        DiskCleanupCancelBtn.Visibility = showDiskCleanupCancel ? Visibility.Visible : Visibility.Collapsed;
        DiskCleanupCancelBtn.IsEnabled = showDiskCleanupCancel;
        PreviewBtn.IsEnabled = !isBusy;
        DeleteBtn.IsEnabled = !isBusy && _previewCandidates.Count > 0;
        BrowseBtn.IsEnabled = !isBusy;
        RootPathBox.IsEnabled = !isBusy;
        DepthBox.IsEnabled = !isBusy;
        ExcludeBox.IsEnabled = !isBusy;
        RecycleChk.IsEnabled = !isBusy;
        UpdateDiskCleanupActionState();
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        Info.Message = message;
        Info.Severity = severity;
        Info.IsOpen = true;
    }

    private async void OnDiskCleanupAnalyze(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoFinishAnalysis", "Finish the current operation before analyzing disk cleanup."),
                InfoBarSeverity.Warning);
            return;
        }

        _diskCleanupCts?.Cancel();
        _diskCleanupCts?.Dispose();
        _diskCleanupCts = new CancellationTokenSource();

        DiskCleanupInfoBar.IsOpen = false;
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;
        SetActivity(Localize("ActivityDiskCleanupAnalyzing", "Analyzing disk cleanup handlers…"));
        SetBusy(true);

        try
        {
            var items = await _diskCleanupService.AnalyzeAsync(_diskCleanupVolume, _diskCleanupCts.Token);
            ApplyDiskCleanupResults(items);
            UpdateDiskCleanupStatusSummary();
            SetActivity(Localize("ActivityDiskCleanupAnalysisComplete", "Disk cleanup analysis complete."));
            ShowDiskCleanupInfo(
                LocalizeFormat("InfoDiskCleanupAnalyzed", "Analyzed {0} handler(s).", items.Count),
                InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
            ShowDiskCleanupInfo(
                Localize("InfoDiskCleanupAnalysisCancelled", "Disk cleanup analysis cancelled."),
                InfoBarSeverity.Informational);
            SetActivity(Localize("ActivityDiskCleanupAnalysisCancelled", "Disk cleanup analysis cancelled."));
        }
        catch (Exception ex)
        {
            ShowDiskCleanupInfo(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Localize("InfoDiskCleanupAnalysisFailed", "Disk cleanup analysis failed: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
            SetActivity(Localize("ActivityDiskCleanupAnalysisFailed", "Disk cleanup analysis failed."));
        }
        finally
        {
            _isDiskCleanupOperation = false;
            DiskCleanupProgress.Visibility = Visibility.Collapsed;
            SetBusy(false);
            _diskCleanupCts?.Dispose();
            _diskCleanupCts = null;
            UpdateDiskCleanupActionState();
        }
    }

    private async void OnDiskCleanupClean(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoFinishCleaning", "Finish the current operation before cleaning disk handlers."),
                InfoBarSeverity.Warning);
            return;
        }

        var targets = _diskCleanupItems
            .Where(item => item.IsSelected && item.CanSelect)
            .Select(item => item.Item)
            .ToList();

        if (targets.Count == 0)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoSelectCategory", "Select at least one category to clean."),
                InfoBarSeverity.Warning);
            return;
        }

        _diskCleanupCts?.Cancel();
        _diskCleanupCts?.Dispose();
        _diskCleanupCts = new CancellationTokenSource();

        DiskCleanupInfoBar.IsOpen = false;
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;
        SetActivity(Localize("ActivityDiskCleanupRunning", "Running disk cleanup handlers…"));
        SetBusy(true);

        try
        {
            var result = await _diskCleanupService.CleanAsync(_diskCleanupVolume, targets, _diskCleanupCts.Token);

            var severity = result.HasFailures
                ? InfoBarSeverity.Warning
                : result.Freed > 0
                    ? InfoBarSeverity.Success
                    : InfoBarSeverity.Informational;

            var message = result.SuccessCount > 0
                ? LocalizeFormat(
                    "InfoDiskCleanupCleaned",
                    "Cleaned {0} handler(s) and freed {1}.",
                    result.SuccessCount,
                    FormatBytes(result.Freed))
                : Localize("InfoDiskCleanupNoChanges", "No disk cleanup handlers reported any changes.");

            if (result.HasFailures)
            {
                var details = string.Join(Environment.NewLine, result.Failures.Select(f => $"• {f.Name}: {f.Message}"));
                message += Environment.NewLine + details;
            }

            ShowDiskCleanupInfo(message, severity);
            SetActivity(Localize("ActivityDiskCleanupCompleted", "Disk cleanup completed."));

            var refreshed = await _diskCleanupService.AnalyzeAsync(_diskCleanupVolume, _diskCleanupCts.Token);
            ApplyDiskCleanupResults(refreshed);
            UpdateDiskCleanupStatusSummary();
        }
        catch (OperationCanceledException)
        {
            ShowDiskCleanupInfo(
                Localize("InfoDiskCleanupCancelled", "Disk cleanup cancelled."),
                InfoBarSeverity.Informational);
            SetActivity(Localize("ActivityDiskCleanupCancelled", "Disk cleanup cancelled."));
        }
        catch (Exception ex)
        {
            ShowDiskCleanupInfo(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Localize("InfoDiskCleanupFailed", "Disk cleanup failed: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
            SetActivity(Localize("ActivityDiskCleanupFailed", "Disk cleanup failed."));
        }
        finally
        {
            _isDiskCleanupOperation = false;
            DiskCleanupProgress.Visibility = Visibility.Collapsed;
            SetBusy(false);
            _diskCleanupCts?.Dispose();
            _diskCleanupCts = null;
            UpdateDiskCleanupActionState();
        }
    }

    private void ApplyDiskCleanupResults(IReadOnlyCollection<DiskCleanupItem> items)
    {
        foreach (var item in _diskCleanupItems)
        {
            item.PropertyChanged -= OnDiskCleanupItemChanged;
        }

        _diskCleanupItems.Clear();

        foreach (var item in items)
        {
            var viewModel = new DiskCleanupItemViewModel(item);
            viewModel.PropertyChanged += OnDiskCleanupItemChanged;
            _diskCleanupItems.Add(viewModel);
        }

        UpdateDiskCleanupStatusSummary();
        UpdateDiskCleanupActionState();
    }

    private void OnDiskCleanupItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiskCleanupItemViewModel.IsSelected))
        {
            UpdateDiskCleanupActionState();
        }
    }

    private void UpdateDiskCleanupActionState()
    {
        var canInteract = !_isBusy && !_isDiskCleanupOperation;
        DiskCleanupAnalyzeBtn.IsEnabled = !_isBusy;
        DiskCleanupList.IsEnabled = canInteract;
        var hasSelection = canInteract && _diskCleanupItems.Any(item => item.IsSelected && item.CanSelect);
        DiskCleanupCleanBtn.IsEnabled = hasSelection;
    }

    private void UpdateDiskCleanupStatusSummary()
    {
        if (_diskCleanupItems.Count == 0)
        {
            DiskCleanupStatusText.Text = LocalizeFormat(
                "DiskCleanupStatusNoData",
                "No Disk Cleanup handlers reported data for {0}. Try running as Administrator.",
                _diskCleanupVolume);
            return;
        }

        var selectable = _diskCleanupItems.Where(item => item.CanSelect).ToList();
        var totalBytes = selectable.Aggregate(0UL, (current, item) => current + item.Item.Size);

        if (selectable.Count == 0)
        {
            DiskCleanupStatusText.Text = LocalizeFormat(
                "DiskCleanupStatusNoSpace",
                "No reclaimable space detected on {0}.",
                _diskCleanupVolume);
        }
        else
        {
            var label = selectable.Count == 1
                ? Localize("DiskCleanupCategorySingular", "category")
                : Localize("DiskCleanupCategoryPlural", "categories");
            DiskCleanupStatusText.Text = string.Format(
                CultureInfo.CurrentCulture,
                Localize(
                    "DiskCleanupStatusPotential",
                    "Potential savings: {0} across {1} {2} on {3}."),
                FormatBytes(totalBytes),
                selectable.Count,
                label,
                _diskCleanupVolume);
        }

        if (_diskCleanupItems.Any(item => item.Item.RequiresElevation))
        {
            DiskCleanupStatusText.Text += " " + Localize(
                "DiskCleanupStatusNeedsElevation",
                "Some handlers require Administrator privileges.");
        }

        if (_diskCleanupItems.Any(item => !string.IsNullOrWhiteSpace(item.ErrorMessage)))
        {
            DiskCleanupStatusText.Text += " " + Localize(
                "DiskCleanupStatusHasIssues",
                "Some handlers reported issues.");
        }
    }

    private void ShowDiskCleanupInfo(string message, InfoBarSeverity severity)
    {
        DiskCleanupInfoBar.Message = message;
        DiskCleanupInfoBar.Severity = severity;
        DiskCleanupInfoBar.IsOpen = true;
    }

    private static string FormatBytes(ulong value)
    {
        if (value == 0)
        {
            return "0 B";
        }

        string[] suffixes = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        double size = value;
        var index = 0;

        while (size >= 1024 && index < suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:0.##} {1}", size, suffixes[index]);
    }

    private void LoadPreferences()
    {
        _isInitializingSettings = true;

        var savedTheme = ReadSetting(ThemePreferenceKey);
        ApplyThemePreference(savedTheme, save: false);
        SelectThemeOption(_themePreference);

        var savedAccent = ReadSetting(AccentPreferenceKey);
        ApplyAccentPreference(savedAccent, save: false);
        SelectAccentOption(_accentPreference);

        _cleanerSendToRecycleBin = ParseBoolSetting(ReadSetting(CleanerRecyclePreferenceKey), defaultValue: true);
        _cleanerDepthLimit = ParseIntSetting(
            ReadSetting(CleanerDepthPreferenceKey),
            defaultValue: 0,
            min: 0,
            max: 999);
        _cleanerExclusions = ReadSetting(CleanerExclusionsPreferenceKey) ?? string.Empty;

        _automationAutoPreview = ParseBoolSetting(ReadSetting(AutomationAutoPreviewKey), defaultValue: false);
        _automationWeeklyReminder = ParseBoolSetting(ReadSetting(AutomationReminderKey), defaultValue: false);
        _notificationShowCompletion = ParseBoolSetting(ReadSetting(NotificationShowCompletionKey), defaultValue: true);
        _notificationDesktopAlerts = ParseBoolSetting(ReadSetting(NotificationDesktopAlertsKey), defaultValue: false);
        _historyRetentionDays = ParseIntSetting(
            ReadSetting(HistoryRetentionKey),
            HistoryRetentionDefaultDays,
            min: HistoryRetentionMinDays,
            max: HistoryRetentionMaxDays);

        if (CleanerRecycleToggle is not null)
        {
            CleanerRecycleToggle.IsOn = _cleanerSendToRecycleBin;
        }

        if (CleanerDepthPreferenceBox is not null)
        {
            CleanerDepthPreferenceBox.Value = _cleanerDepthLimit;
        }

        if (CleanerExclusionsPreferenceBox is not null)
        {
            CleanerExclusionsPreferenceBox.Text = _cleanerExclusions;
        }

        if (AutomationAutoPreviewToggle is not null)
        {
            AutomationAutoPreviewToggle.IsOn = _automationAutoPreview;
        }

        if (AutomationReminderToggle is not null)
        {
            AutomationReminderToggle.IsOn = _automationWeeklyReminder;
        }

        if (NotificationCompletionToggle is not null)
        {
            NotificationCompletionToggle.IsOn = _notificationShowCompletion;
        }

        if (NotificationDesktopToggle is not null)
        {
            NotificationDesktopToggle.IsOn = _notificationDesktopAlerts;
        }

        if (HistoryRetentionNumberBox is not null)
        {
            HistoryRetentionNumberBox.Value = _historyRetentionDays;
        }

        UpdateCleanerDefaultsSummary();
        UpdateAutomationSummary();
        UpdateNotificationSummary();
        UpdateHistoryRetentionSummary();

        _isInitializingSettings = false;

        ApplyCleanerDefaultsToSession();
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is not RadioButtons radioButtons)
        {
            return;
        }

        if (radioButtons.SelectedItem is RadioButton button && button.Tag is string tag)
        {
            ApplyThemePreference(tag, save: true);
        }
    }

    private void OnAccentPreferenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is not RadioButtons radioButtons)
        {
            return;
        }

        if (radioButtons.SelectedItem is RadioButton button && button.Tag is string tag)
        {
            ApplyAccentPreference(tag, save: true);
        }
    }

    private void ApplyThemePreference(string? preference, bool save)
    {
        var normalized = NormalizeThemePreference(preference);
        _themePreference = normalized;

        var theme = normalized switch
        {
            ThemePreferenceLight => ElementTheme.Light,
            ThemePreferenceDark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        RootNavigation.RequestedTheme = theme;

        if (_backdropConfig is not null)
        {
            _backdropConfig.Theme = theme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                _ => SystemBackdropTheme.Default
            };
        }

        ThemeSummaryText.Text = normalized switch
        {
            ThemePreferenceLight => "Light",
            ThemePreferenceDark => "Dark",
            _ => "Use system setting"
        };

        if (save)
        {
            SaveSetting(ThemePreferenceKey, normalized);
        }
    }

    private static string NormalizeThemePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return ThemePreferenceDefault;
        }

        return preference.Trim().ToLowerInvariant() switch
        {
            ThemePreferenceLight => ThemePreferenceLight,
            ThemePreferenceDark => ThemePreferenceDark,
            ThemePreferenceDefault => ThemePreferenceDefault,
            "system" => ThemePreferenceDefault,
            _ => ThemePreferenceDefault
        };
    }

    private void SelectThemeOption(string preference)
    {
        if (ThemeRadioButtons is null)
        {
            return;
        }

        ThemeRadioButtons.SelectedIndex = preference switch
        {
            ThemePreferenceLight => 0,
            ThemePreferenceDark => 1,
            _ => 2
        };
    }

    private void ApplyAccentPreference(string? preference, bool save)
    {
        var normalized = NormalizeAccentPreference(preference);
        _accentPreference = normalized;

        if (string.Equals(normalized, AccentPreferenceDefault, StringComparison.OrdinalIgnoreCase))
        {
            RestoreAccentColors();
        }
        else if (string.Equals(normalized, AccentPreferenceZest, StringComparison.OrdinalIgnoreCase))
        {
            ApplyAccentColor(GetZestAccentColor());
        }
        else if (TryParseColor(normalized, out var color))
        {
            ApplyAccentColor(color);
        }
        else
        {
            RestoreAccentColors();
            _accentPreference = AccentPreferenceDefault;
        }

        AccentSummaryText.Text = FormatAccentSummary(_accentPreference);

        if (save)
        {
            SaveSetting(AccentPreferenceKey, _accentPreference);
        }
    }

    private static string NormalizeAccentPreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return AccentPreferenceDefault;
        }

        var trimmed = preference.Trim();

        if (trimmed.StartsWith('#'))
        {
            return trimmed;
        }

        var lower = trimmed.ToLowerInvariant();
        return lower switch
        {
            AccentPreferenceZest => AccentPreferenceZest,
            AccentPreferenceDefault => AccentPreferenceDefault,
            "system" => AccentPreferenceDefault,
            _ => trimmed
        };
    }

    private void SelectAccentOption(string preference)
    {
        if (AccentColorRadioButtons is null)
        {
            return;
        }

        AccentColorRadioButtons.SelectedIndex = preference switch
        {
            AccentPreferenceZest => 0,
            AccentPreferenceDefault => 1,
            _ => -1
        };
    }

    private string FormatAccentSummary(string preference)
    {
        if (string.Equals(preference, AccentPreferenceZest, StringComparison.OrdinalIgnoreCase))
        {
            return "Zest";
        }

        if (string.Equals(preference, AccentPreferenceDefault, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(preference))
        {
            return "Use system setting";
        }

        if (preference.StartsWith('#'))
        {
            return string.Format(CultureInfo.CurrentCulture, "Custom ({0})", preference.ToUpperInvariant());
        }

        return preference;
    }

    private void OnCleanerRecyclePreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is ToggleSwitch toggle)
        {
            _cleanerSendToRecycleBin = toggle.IsOn;
            SaveSetting(
                CleanerRecyclePreferenceKey,
                _cleanerSendToRecycleBin.ToString(CultureInfo.InvariantCulture));
            UpdateCleanerDefaultsSummary();
            ApplyCleanerDefaultsToSession();
        }
    }

    private void OnCleanerDepthPreferenceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        var value = sender.Value;
        if (double.IsNaN(value))
        {
            value = 0;
        }

        var depth = (int)Math.Clamp(Math.Round(value), 0, 999);
        sender.Value = depth;
        _cleanerDepthLimit = depth;
        SaveSetting(CleanerDepthPreferenceKey, depth.ToString(CultureInfo.InvariantCulture));
        UpdateCleanerDefaultsSummary();
        ApplyCleanerDefaultsToSession();
    }

    private void OnCleanerExclusionsPreferenceChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is TextBox textBox)
        {
            _cleanerExclusions = textBox.Text?.Trim() ?? string.Empty;
            SaveSetting(CleanerExclusionsPreferenceKey, _cleanerExclusions);
            ApplyCleanerDefaultsToSession();
        }
    }

    private void OnApplyCleanerDefaults(object sender, RoutedEventArgs e)
    {
        ApplyCleanerDefaultsToSession();
        ShowCleanerDefaultsInfo(
            Localize("SettingsCleanerDefaultsApplied", "Defaults applied to current session."),
            InfoBarSeverity.Success);
    }

    private void OnAutomationPreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (AutomationAutoPreviewToggle is not null && sender == AutomationAutoPreviewToggle)
        {
            _automationAutoPreview = AutomationAutoPreviewToggle.IsOn;
            SaveSetting(
                AutomationAutoPreviewKey,
                _automationAutoPreview.ToString(CultureInfo.InvariantCulture));
        }
        else if (AutomationReminderToggle is not null && sender == AutomationReminderToggle)
        {
            _automationWeeklyReminder = AutomationReminderToggle.IsOn;
            SaveSetting(
                AutomationReminderKey,
                _automationWeeklyReminder.ToString(CultureInfo.InvariantCulture));
        }

        UpdateAutomationSummary();
    }

    private void OnNotificationPreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (NotificationCompletionToggle is not null && sender == NotificationCompletionToggle)
        {
            _notificationShowCompletion = NotificationCompletionToggle.IsOn;
            SaveSetting(
                NotificationShowCompletionKey,
                _notificationShowCompletion.ToString(CultureInfo.InvariantCulture));
        }
        else if (NotificationDesktopToggle is not null && sender == NotificationDesktopToggle)
        {
            _notificationDesktopAlerts = NotificationDesktopToggle.IsOn;
            SaveSetting(
                NotificationDesktopAlertsKey,
                _notificationDesktopAlerts.ToString(CultureInfo.InvariantCulture));
        }

        UpdateNotificationSummary();
    }

    private void OnHistoryRetentionChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        var value = sender.Value;
        if (double.IsNaN(value))
        {
            value = HistoryRetentionDefaultDays;
        }

        var days = (int)Math.Clamp(Math.Round(value), HistoryRetentionMinDays, HistoryRetentionMaxDays);
        sender.Value = days;
        _historyRetentionDays = days;
        SaveSetting(HistoryRetentionKey, days.ToString(CultureInfo.InvariantCulture));
        UpdateHistoryRetentionSummary();
    }

    private static ResourceLoader? TryCreateResourceLoader()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            return new ResourceLoader();
        }
        catch
        {
            return null;
        }
    }

    private static ApplicationDataContainer? TryGetLocalSettings()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            return ApplicationData.Current.LocalSettings;
        }
        catch
        {
            return null;
        }
    }

    private string? ReadSetting(string key)
    {
        var settings = _settings;
        if (settings is null)
        {
            return null;
        }

        try
        {
            if (settings.Values.TryGetValue(key, out var value))
            {
                return value switch
                {
                    string text => text,
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => value?.ToString()
                };
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private void SaveSetting(string key, string value)
    {
        var settings = _settings;
        if (settings is null)
        {
            return;
        }

        try
        {
            settings.Values[key] = value;
        }
        catch
        {
            // Ignore persistence failures on unsupported platforms.
        }
    }

    private Color GetZestAccentColor()
    {
        if (Application.Current.Resources.TryGetValue("Color.BrandPrimary", out var value) && value is Color color)
        {
            return color;
        }

        return Color.FromArgb(255, 0x00, 0x67, 0xC0);
    }

    private void CaptureDefaultAccentColors()
    {
        foreach (var key in AccentResourceKeys)
        {
            if (Application.Current.Resources.TryGetValue(key, out var value) && value is Color color)
            {
                _defaultAccentColors[key] = color;
            }
        }
    }

    private void RestoreAccentColors()
    {
        foreach (var key in AccentResourceKeys)
        {
            if (_defaultAccentColors.TryGetValue(key, out var color))
            {
                SetAccentResource(key, color);
            }
        }
    }

    private void ApplyAccentColor(Color color)
    {
        SetAccentResource("SystemAccentColor", color);
        SetAccentResource("SystemAccentColorLight1", Lighten(color, 0.3));
        SetAccentResource("SystemAccentColorLight2", Lighten(color, 0.5));
        SetAccentResource("SystemAccentColorLight3", Lighten(color, 0.7));
        SetAccentResource("SystemAccentColorDark1", Darken(color, 0.2));
        SetAccentResource("SystemAccentColorDark2", Darken(color, 0.35));
        SetAccentResource("SystemAccentColorDark3", Darken(color, 0.5));
    }

    private static void SetAccentResource(string key, Color color)
    {
        Application.Current.Resources[key] = color;
        var brushKey = key + "Brush";
        if (Application.Current.Resources.TryGetValue(brushKey, out var brushObj) && brushObj is SolidColorBrush brush)
        {
            brush.Color = color;
        }
    }

    private static Color Lighten(Color color, double amount) => Lerp(color, Colors.White, amount);

    private static Color Darken(Color color, double amount) => Lerp(color, Colors.Black, amount);

    private static Color Lerp(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (byte)(from.A + (to.A - from.A) * amount),
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan();
        if (span[0] == '#')
        {
            span = span[1..];
        }

        if (span.Length == 6)
        {
            if (uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                var r = (byte)((rgb >> 16) & 0xFF);
                var g = (byte)((rgb >> 8) & 0xFF);
                var b = (byte)(rgb & 0xFF);
                color = Color.FromArgb(255, r, g, b);
                return true;
            }
        }
        else if (span.Length == 8)
        {
            if (uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                var a = (byte)((argb >> 24) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var b = (byte)(argb & 0xFF);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
        }

        return false;
    }

    private static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity is not null && new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private sealed class DriveUsageViewModel
    {
        public DriveUsageViewModel(string name, string details, double usedPercentage, string usageSummary)
        {
            Name = name;
            Details = details;
            UsedPercentage = usedPercentage;
            UsageSummary = usageSummary;
        }

        public string Name { get; }

        public string Details { get; }

        public double UsedPercentage { get; }

        public string UsageSummary { get; }
    }

    private sealed class LargeFileGroupViewModel : INotifyPropertyChanged
    {
        private readonly MainWindow _owner;
        private long _totalBytes;

        public LargeFileGroupViewModel(MainWindow owner, string displayName)
        {
            _owner = owner;
            DisplayName = displayName;
            Items = new ObservableCollection<LargeFileItemViewModel>();
        }

        public string DisplayName { get; }

        public ObservableCollection<LargeFileItemViewModel> Items { get; }

        public long TotalBytes => _totalBytes;

        public int ItemCount => Items.Count;

        public string Summary => string.Format(
            CultureInfo.CurrentCulture,
            "{0} • {1}",
            FormatBytes((ulong)Math.Max(0L, _totalBytes)),
            _owner.FormatFileCount(ItemCount));

        public void AddItem(LargeFileItemViewModel item)
        {
            Items.Add(item);
            _totalBytes += item.Size;
            OnPropertyChanged(nameof(TotalBytes));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(Summary));
        }

        public bool RemoveItem(LargeFileItemViewModel item)
        {
            if (Items.Remove(item))
            {
                _totalBytes -= item.Size;
                OnPropertyChanged(nameof(TotalBytes));
                OnPropertyChanged(nameof(ItemCount));
                OnPropertyChanged(nameof(Summary));
                return true;
            }

            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private sealed class LargeFileItemViewModel
    {
        public LargeFileItemViewModel(LargeFileEntry entry, string extensionDisplay)
        {
            Entry = entry;
            Path = entry.Path;
            Name = entry.Name;
            Directory = string.IsNullOrEmpty(entry.Directory) ? entry.Path : entry.Directory;
            Size = entry.Size;
            SizeDisplay = FormatBytes((ulong)Math.Max(0L, entry.Size));
            TypeName = entry.Type;
            ExtensionDisplay = extensionDisplay;
        }

        public LargeFileEntry Entry { get; }

        public string Path { get; }

        public string Name { get; }

        public string Directory { get; }

        public long Size { get; }

        public string SizeDisplay { get; }

        public string TypeName { get; }

        public string ExtensionDisplay { get; }
    }

    private sealed class DiskCleanupItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public DiskCleanupItemViewModel(DiskCleanupItem item)
        {
            Item = item;
            if (item.CanSelect && (item.Flags.HasFlag(DiskCleanupFlags.RunByDefault) ||
                                   item.Flags.HasFlag(DiskCleanupFlags.EnableByDefault)))
            {
                _isSelected = true;
            }
        }

        internal DiskCleanupItem Item { get; }

        public string Name => Item.Name;

        public string? Description => Item.Description;

        public string FormattedSize => FormatBytes(Item.Size);

        public bool CanSelect => Item.CanSelect;

        public string? ErrorMessage => Item.Error;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
