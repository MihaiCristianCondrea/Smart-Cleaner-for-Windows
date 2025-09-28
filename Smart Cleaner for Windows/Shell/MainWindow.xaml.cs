using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Windows.ApplicationModel.Resources;
using Smart_Cleaner_for_Windows.Core.FileSystem;
using Smart_Cleaner_for_Windows.Core.DiskCleanup;
using Smart_Cleaner_for_Windows.Core.Storage;
using Smart_Cleaner_for_Windows.Core.LargeFiles;
using Smart_Cleaner_for_Windows.Modules.Dashboard.ViewModels;
using Smart_Cleaner_for_Windows.Modules.DiskCleanup.ViewModels;
using Smart_Cleaner_for_Windows.Modules.EmptyFolders;
using Smart_Cleaner_for_Windows.Modules.EmptyFolders.Contracts;
using Smart_Cleaner_for_Windows.Modules.LargeFiles.ViewModels;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;
using System.Security.Principal;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow : IEmptyFolderCleanupView
{
    private readonly IDirectoryCleaner _directoryCleaner;
    private readonly IDiskCleanupService _diskCleanupService;
    private readonly IStorageOverviewService _storageOverviewService;
    private readonly ILargeFileExplorer _largeFileExplorer;
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private readonly EmptyFolderCleanupController _emptyFolderController;
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
    private bool _isSystemTitleBarInitialized;

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
            DirectoryCleanerFactory.CreateDefault(),
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

        _emptyFolderController = new EmptyFolderCleanupController(_directoryCleaner, this);

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
        InitializeSystemTitleBar();
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
                    ValueFormatting.FormatBytes(freeValue),
                    ValueFormatting.FormatBytes(capacityValue));
                var usageSummary = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:0}% used ({1})",
                    usedPercentage,
                    ValueFormatting.FormatBytes(usedValue));

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
                ValueFormatting.FormatBytes(totalFree),
                ValueFormatting.FormatBytes(totalCapacity));

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


}
