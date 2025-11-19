using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartCleanerForWindows.Core.FileSystem;
using SmartCleanerForWindows.Core.Storage;
using SmartCleanerForWindows.Core.LargeFiles;
using SmartCleanerForWindows.Core.Networking;
using SmartCleanerForWindows.Modules.Dashboard.ViewModels;
using SmartCleanerForWindows.Modules.DiskCleanup.ViewModels;
using SmartCleanerForWindows.Modules.EmptyFolders;
using SmartCleanerForWindows.Modules.EmptyFolders.Contracts;
using SmartCleanerForWindows.Modules.EmptyFolders.ViewModels;
using SmartCleanerForWindows.Modules.LargeFiles.ViewModels;
using SmartCleanerForWindows.Modules.InternetRepair.ViewModels;
using Serilog;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;
using System.Security.Principal;
using Microsoft.UI.Xaml.Markup;
using SmartCleanerForWindows.Core.DiskCleanup;
using SmartCleanerForWindows.Settings;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow : IEmptyFolderCleanupView
{
    private readonly IDiskCleanupService _diskCleanupService;
    private readonly IStorageOverviewService _storageOverviewService;
    private readonly ILargeFileExplorer _largeFileExplorer;
    private readonly IInternetRepairService _internetRepairService;
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private readonly EmptyFolderCleanupController _emptyFolderController = null!;
    private readonly List<string> _previewCandidates = new();
    private readonly ObservableCollection<EmptyFolderNode> _emptyFolderRoots = new();
    private readonly ObservableCollection<EmptyFolderNode> _filteredEmptyFolderRoots = new();
    private readonly Dictionary<string, EmptyFolderNode> _emptyFolderLookup = new(FileSystemPathComparer.PathComparer);
    private readonly HashSet<string> _inlineExcludedPaths = new(FileSystemPathComparer.PathComparer);
    private int _totalPreviewCount;
    private string _currentPreviewRoot = string.Empty;
    private string _currentResultSearch = string.Empty;
    private bool _hideExcludedResults;
    private EmptyFolderSortOption _currentResultSort = EmptyFolderSortOption.NameAscending;
    private bool _isBusy;
    private readonly ObservableCollection<DriveUsageViewModel> _driveUsage = new();
    private readonly ObservableCollection<DiskCleanupItemViewModel> _diskCleanupItems = new();
    private readonly ObservableCollection<LargeFileGroupViewModel> _largeFileGroups = new();
    private readonly ObservableCollection<string> _largeFileExclusions = new();
    private readonly ObservableCollection<InternetRepairLogEntry> _internetRepairLog = new();
    private readonly HashSet<string> _largeFileExclusionLookup = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly Dictionary<string, InternetRepairAction> _internetRepairActions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, InternetRepairLogEntry> _internetRepairLogLookup = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _diskCleanupCts;
    private CancellationTokenSource? _largeFilesCts;
    private CancellationTokenSource? _internetRepairCts;
    private readonly string _diskCleanupVolume;
    private CancellationTokenSource? _storageOverviewCts;
    private bool _isDiskCleanupOperation;
    private bool _isLargeFilesBusy;
    private bool _isInternetRepairBusy;
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
    private readonly ToolSettingsService _toolSettingsService = ToolSettingsService.CreateDefault();
    private readonly ObservableCollection<NavigationViewItem> _navigationItems = new();
    private readonly Dictionary<string, UIElement> _toolViewLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UIElement> _viewKeyLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToolSettingsSnapshot> _settingsSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<UIElement> _allViews = [];
    private string? _currentToolId;

    private const string ThemePreferenceKey = "Settings.ThemePreference";
    private const string AccentPreferenceKey = "Settings.AccentPreference";
    private const string ThemePreferenceLight = "light";
    private const string ThemePreferenceDark = "dark";
    private const string ThemePreferenceDefault = "default";
    private const string AccentPreferenceZest = "zest";
    private const string AccentPreferenceDefault = "default";
    private const string NotificationShowCompletionKey = "Settings.Notifications.ShowCompletion";
    private const string NotificationDesktopAlertsKey = "Settings.Notifications.DesktopAlerts";
    private const string HistoryRetentionKey = "Settings.History.RetentionDays";
    private const int HistoryRetentionDefaultDays = 30;
    private const int HistoryRetentionMinDays = 0;
    private const int HistoryRetentionMaxDays = 365;
    private const string DashboardToolId = "dashboard";
    private const string EmptyFoldersToolId = "emptyFolders";
    private const string LargeFilesToolId = "largeFiles";
    private const string DiskCleanupToolId = "diskCleanup";
    private const string InternetRepairToolId = "internetRepair";
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
AP/UeAD/1HgA/9R4AP/UeAD/1HgA/9R4AP/UeAD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==
""";
    private static readonly string[] AccentResourceKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3"
    ];

    public MainWindow()
        : this(
            DirectoryCleanerFactory.CreateDefault(),
            DiskCleanupServiceFactory.CreateDefault(),
            new StorageOverviewService(),
            LargeFileExplorer.Default,
            InternetRepairServiceFactory.CreateDefault())
    {
    }

    public MainWindow(IDirectoryCleaner directoryCleaner)
        : this(
            directoryCleaner,
            DiskCleanupServiceFactory.CreateDefault(),
            new StorageOverviewService(),
            LargeFileExplorer.Default,
            InternetRepairServiceFactory.CreateDefault())
    {
    }

    public MainWindow(IDirectoryCleaner directoryCleaner, IDiskCleanupService diskCleanupService)
        : this(directoryCleaner, diskCleanupService, new StorageOverviewService(), LargeFileExplorer.Default, InternetRepairServiceFactory.CreateDefault())
    {
    }

    public MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService)
        : this(directoryCleaner, diskCleanupService, storageOverviewService, LargeFileExplorer.Default, InternetRepairServiceFactory.CreateDefault())
    {
    }

    private MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService,
        ILargeFileExplorer largeFileExplorer,
        IInternetRepairService internetRepairService)
    {
        var directoryCleaner1 = directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner));
        _diskCleanupService = diskCleanupService ?? throw new ArgumentNullException(nameof(diskCleanupService));
        _storageOverviewService = storageOverviewService ?? throw new ArgumentNullException(nameof(storageOverviewService));
        _largeFileExplorer = largeFileExplorer ?? throw new ArgumentNullException(nameof(largeFileExplorer));
        _internetRepairService = internetRepairService ?? throw new ArgumentNullException(nameof(internetRepairService));
        _diskCleanupVolume = _diskCleanupService.GetDefaultVolume();

        if (!TryInitializeComponentWithDiagnostics(out var xamlFailure))
        {
            _toolSettingsService.Dispose();
            BuildFallbackShell(xamlFailure);
            return;
        }

        _emptyFolderController = new EmptyFolderCleanupController(directoryCleaner1, this);
        EmptyFoldersView.CandidatesTree.ItemsSource = _filteredEmptyFolderRoots;

        LargeFilesView.LargeFilesGroupList.ItemsSource = _largeFileGroups;

        DashboardView.NavigateToEmptyFoldersRequested += (_, _) => NavigateToTool(EmptyFoldersToolId);
        DashboardView.NavigateToLargeFilesRequested += (_, _) => NavigateToTool(LargeFilesToolId);
        DashboardView.NavigateToDiskCleanupRequested += (_, _) => NavigateToTool(DiskCleanupToolId);
        DashboardView.NavigateToInternetRepairRequested += (_, _) => NavigateToTool(InternetRepairToolId);

        EmptyFoldersView.BrowseRequested += OnBrowse;
        EmptyFoldersView.CancelRequested += OnCancel;
        EmptyFoldersView.CandidatesSelectionChanged += OnCandidatesSelectionChanged;
        EmptyFoldersView.InlineExclusionsCleared += OnClearInlineExclusions;
        EmptyFoldersView.ResultFiltersCleared += OnClearResultFilters;
        EmptyFoldersView.DeleteRequested += OnDelete;
        EmptyFoldersView.ExcludeSelectedRequested += OnExcludeSelected;
        EmptyFoldersView.IncludeSelectedRequested += OnIncludeSelected;
        EmptyFoldersView.PreviewRequested += OnPreview;
        EmptyFoldersView.ResultSearchChanged += OnResultSearchChanged;
        EmptyFoldersView.ResultSortChanged += OnResultSortChanged;
        EmptyFoldersView.RootPathTextChanged += RootPathBox_TextChanged;
        EmptyFoldersView.HideExcludedToggled += OnHideExcludedToggled;

        LargeFilesView.BrowseRequested += OnLargeFilesBrowse;
        LargeFilesView.CancelRequested += OnLargeFilesCancel;
        LargeFilesView.ClearExclusionsRequested += OnLargeFilesClearExclusions;
        LargeFilesView.RootPathChanged += OnLargeFilesRootPathChanged;
        LargeFilesView.ScanRequested += OnLargeFilesScan;
        LargeFilesView.DeleteRequested += OnLargeFileDelete;
        LargeFilesView.ExcludeRequested += OnLargeFileExclude;
        LargeFilesView.OpenRequested += OnLargeFileOpen;
        LargeFilesView.RemoveExclusionRequested += OnLargeFilesRemoveExclusion;

        DiskCleanupView.AnalyzeRequested += OnDiskCleanupAnalyze;
        DiskCleanupView.CleanRequested += OnDiskCleanupClean;
        DiskCleanupView.CancelRequested += OnCancel;

        InternetRepairView.RunRequested += OnInternetRepairRun;
        InternetRepairView.CancelRequested += OnInternetRepairCancel;
        InternetRepairView.ActionSelectionChanged += OnInternetRepairActionSelectionChanged;

        SettingsView.ThemeSelectionChanged += OnThemeSelectionChanged;
        SettingsView.AccentPreferenceChanged += OnAccentPreferenceChanged;
        SettingsView.CleanerDefaultsApplied += OnApplyCleanerDefaults;
        SettingsView.CleanerRecyclePreferenceToggled += OnCleanerRecyclePreferenceToggled;
        SettingsView.CleanerExclusionsPreferenceChanged += OnCleanerExclusionsPreferenceChanged;
        SettingsView.CleanerDepthPreferenceChanged += OnCleanerDepthPreferenceChanged;
        SettingsView.AutomationPreferenceToggled += OnAutomationPreferenceToggled;
        SettingsView.NotificationPreferenceToggled += OnNotificationPreferenceToggled;
        SettingsView.HistoryRetentionChanged += OnHistoryRetentionChanged;

        CaptureDefaultAccentColors();
        LoadPreferences();

        LargeFilesView.LargeFilesExclusionsList.ItemsSource = _largeFileExclusions;
        LoadLargeFilePreferences();

        InitializeInternetRepair();

        SetLargeFilesStatus(
            Symbol.SaveLocal,
            Localize("LargeFilesStatusReadyTitle", "Ready to explore large files"),
            Localize("LargeFilesStatusReadyDescription", "Choose a location to find the biggest files grouped by type."));
        SetLargeFilesActivity(Localize("ActivityIdle", "Waiting for the next action."));
        UpdateLargeFilesSummary();
        UpdateLargeFilesExclusionState();

        DashboardView.DriveUsageListControl.ItemsSource = _driveUsage;
        _ = UpdateStorageOverviewAsync();

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyleObj) &&
            accentStyleObj is Style accentStyle)
        {
            EmptyFoldersView.DeleteBtn.Style = accentStyle;
            DiskCleanupView.DiskCleanupCleanBtn.Style = accentStyle;
            LargeFilesView.LargeFilesScanBtn.Style = accentStyle;
            InternetRepairView.InternetRepairRunBtn.Style = accentStyle;
        }

        SetStatus(
            Symbol.Folder,
            Localize("StatusReadyTitle", "Ready when you are"),
            Localize("StatusReadyDescription", "Select a folder to begin."));
        SetActivity(Localize("ActivityIdle", "Waiting for the next action."));
        UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));

        DiskCleanupView.DiskCleanupList.ItemsSource = _diskCleanupItems;
        DiskCleanupView.DiskCleanupStatusText.Text = LocalizeFormat(
            "DiskCleanupStatusReady",
            "Ready to analyze disk cleanup handlers for {0}.",
            _diskCleanupVolume);
        if (!IsAdministrator())
        {
            DiskCleanupView.DiskCleanupIntro.Text = Localize(
                "DiskCleanupView.DiskCleanupIntro",
                "Analyze Windows cleanup handlers. Some categories require Administrator privileges.");
        }
        UpdateDiskCleanupActionState();

        TryEnableMica();
        ApplyThemePreference(_themePreference, save: false);
        TryConfigureAppWindow();
        InitializeSystemTitleBar();
        Activated += OnWindowActivated;
        Closed += OnClosed;

        InitializeViewRegistry();
        _toolSettingsService.SettingsChanged += OnToolSettingsChanged;
        BuildToolNavigation();
        NavigateToTool(DashboardToolId);
    }

    private bool TryInitializeComponentWithDiagnostics(out Exception? failure)
    {
        try
        {
            InitializeComponent();
            failure = null;
            return true;
        }
        catch (FileNotFoundException fileEx)
        {
            Log.Error(
                fileEx,
                "XAML load failed: missing file referenced by markup (likely resource dictionary or asset). Message={Message}",
                fileEx.Message);
            failure = fileEx;
        }
        catch (XamlParseException xamlEx)
        {
            Log.Error(
                xamlEx,
                "XAML parse failed while constructing MainWindow. HResult={HResult}, Inner={InnerMessage}",
                xamlEx.HResult,
                xamlEx.InnerException?.Message);
            failure = xamlEx;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected failure during MainWindow InitializeComponent.");
            failure = ex;
        }

        return false;
    }

    private void BuildFallbackShell(Exception? failure)
    {
        var diagnosticText = "Smart Cleaner for Windows could not load the main XAML layout.";
        if (failure is not null)
        {
            diagnosticText += $"\n\n{failure.GetType().Name}: {failure.Message}";
            if (failure.InnerException is not null)
            {
                diagnosticText += $"\nInner: {failure.InnerException.GetType().Name}: {failure.InnerException.Message}";
            }
        }

        var fallbackInfoBrush = new SolidColorBrush(Colors.Gray);
        if (Application.Current?.Resources?.TryGetValue("TextFillColorSecondaryBrush", out var resourceValue) == true
            && resourceValue is SolidColorBrush resolvedBrush)
        {
            fallbackInfoBrush = resolvedBrush;
        }

        Content = new Grid
        {
            Background = Application.Current?.Resources?["ApplicationPageBackgroundThemeBrush"] as Brush,
            Children =
            {
                new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Startup fallback shell loaded",
                            FontSize = 20,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = diagnosticText,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 480,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Check the crash log for details and verify all XAML resources are packaged.",
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 480,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = fallbackInfoBrush
                        }
                    }
                }
            }
        };
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Activated -= OnWindowActivated;

        _cts = SafeCancelAndDispose(_cts);

        _mica?.Dispose();
        _mica = null;
        _backdropConfig = null;

        _diskCleanupCts = SafeCancelAndDispose(_diskCleanupCts);
        _largeFilesCts = SafeCancelAndDispose(_largeFilesCts);
        _internetRepairCts = SafeCancelAndDispose(_internetRepairCts);
        _storageOverviewCts = SafeCancelAndDispose(_storageOverviewCts);

        _toolSettingsService.SettingsChanged -= OnToolSettingsChanged;
        _toolSettingsService.Dispose();
    }

    private static CancellationTokenSource? SafeCancelAndDispose(CancellationTokenSource? cts)
    {
        if (cts is null)
        {
            return null;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }

        cts.Dispose();
        return null;
    }

    private void OnNavigationLoaded(object sender, RoutedEventArgs e)
    {
        if (RootNavigation.SelectedItem is null && _navigationItems.Count > 0)
        {
            RootNavigation.SelectedItem = _navigationItems[0];
            ShowPage(_navigationItems[0]);
            return;
        }

        if (RootNavigation.SelectedItem is not null)
        {
            ShowPage(RootNavigation.SelectedItem);
        }
    }

    private void InitializeViewRegistry()
    {
        _viewKeyLookup.Clear();
        _viewKeyLookup["Dashboard"] = DashboardView;
        _viewKeyLookup["EmptyFolders"] = EmptyFoldersView;
        _viewKeyLookup["LargeFiles"] = LargeFilesView;
        _viewKeyLookup["DiskCleanup"] = DiskCleanupView;
        _viewKeyLookup["InternetRepair"] = InternetRepairView;

        _allViews.Clear();
        foreach (var view in _viewKeyLookup.Values.Distinct())
        {
            _allViews.Add(view);
        }

        if (!_allViews.Contains(SettingsView))
        {
            _allViews.Add(SettingsView);
        }
    }

    private void BuildToolNavigation()
    {
        _toolViewLookup.Clear();
        _navigationItems.Clear();

        foreach (var definition in _toolSettingsService.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.ViewKey))
            {
                continue;
            }

            if (!_viewKeyLookup.TryGetValue(definition.ViewKey!, out var view))
            {
                continue;
            }

            var navItem = CreateNavigationItem(definition);
            _navigationItems.Add(navItem);
            _toolViewLookup[definition.Id] = view;

            if (_toolSettingsService.GetSnapshot(definition.Id) is { } snapshot)
            {
                ApplySnapshot(snapshot);
            }
        }

        RootNavigation.MenuItems.Clear();
        foreach (var item in _navigationItems)
        {
            RootNavigation.MenuItems.Add(item);
        }
    }

    private static NavigationViewItem CreateNavigationItem(ToolSettingsDefinition definition)
    {
        var item = new NavigationViewItem
        {
            Content = definition.Title,
            Tag = definition.Id
        };

        if (!string.IsNullOrWhiteSpace(definition.Description))
        {
            ToolTipService.SetToolTip(item, definition.Description);
        }

        if (!string.IsNullOrWhiteSpace(definition.Icon) &&
            Enum.TryParse(definition.Icon, ignoreCase: true, out Symbol symbol))
        {
            item.Icon = new SymbolIcon(symbol);
        }

        return item;
    }

    private void ApplySnapshot(ToolSettingsSnapshot snapshot)
    {
        _settingsSnapshots[snapshot.Definition.Id] = snapshot;

        if (string.Equals(snapshot.Definition.Id, EmptyFoldersToolId, StringComparison.OrdinalIgnoreCase))
        {
            ApplyEmptyFolderSettings(snapshot.Values);
        }
        else if (string.Equals(snapshot.Definition.Id, DashboardToolId, StringComparison.OrdinalIgnoreCase))
        {
            ApplyDashboardSettings(snapshot.Values);
        }
        else if (string.Equals(snapshot.Definition.Id, LargeFilesToolId, StringComparison.OrdinalIgnoreCase))
        {
            ApplyLargeFilesSettings(snapshot.Values);
        }
    }

    private void ApplyEmptyFolderSettings(JsonObject values)
    {
        _cleanerSendToRecycleBin = values.TryGetPropertyValue("sendToRecycleBin", out var recycleNode) && recycleNode?.GetValue<bool>() == true;
        _cleanerDepthLimit = values.TryGetPropertyValue("depthLimit", out var depthNode)
            ? (int)Math.Max(0, Math.Min(int.MaxValue, depthNode?.GetValue<double>() ?? 0))
            : 0;
        _cleanerExclusions = values.TryGetPropertyValue("exclusions", out var exclusionsNode)
            ? exclusionsNode?.ToString() ?? string.Empty
            : string.Empty;
        _automationAutoPreview = values.TryGetPropertyValue("previewAutomatically", out var previewNode) && previewNode?.GetValue<bool>() == true;
        UpdateCleanerSettingsView();
        ApplyCleanerDefaultsToSession();
    }

    private void ApplyDashboardSettings(JsonObject values)
    {
        _automationWeeklyReminder = values.TryGetPropertyValue("remindWeekly", out var reminderNode) && reminderNode?.GetValue<bool>() == true;
        UpdateAutomationSettingsView();
        UpdateAutomationSummary();
    }

    private void ApplyLargeFilesSettings(JsonObject values)
    {
        var exclusionText = values.TryGetPropertyValue("excludedPaths", out var exclusionNode)
            ? exclusionNode?.ToString() ?? string.Empty
            : string.Empty;

        var segments = exclusionText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _largeFileExclusions.Clear();
        _largeFileExclusionLookup.Clear();

        foreach (var segment in segments)
        {
            if (_largeFileExclusionLookup.Add(segment))
            {
                _largeFileExclusions.Add(segment);
            }
        }

        UpdateLargeFilesExclusionState();
    }

    private void OnToolSettingsChanged(object? sender, ToolSettingsChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => ApplySnapshot(e.Snapshot));
    }

    private NavigationViewItem? FindNavigationItem(string toolId)
    {
        return _navigationItems.FirstOrDefault(
            item => string.Equals(item.Tag as string, toolId, StringComparison.OrdinalIgnoreCase));
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var target = args.IsSettingsSelected ? sender.SettingsItem : args.SelectedItem;
        ShowPage(target);
    }

private void NavigateToTool(string toolId)
    {
        var target = FindNavigationItem(toolId);
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
        var settingsItem = RootNavigation.SettingsItem;
        if (settingsItem is not null && Equals(item, settingsItem))
        {
            _currentToolId = null;
            ActivateView(SettingsView);
            return;
        }

        if (item is not NavigationViewItem { Tag: string toolId } ||
            !_toolViewLookup.TryGetValue(toolId, out var view))
        {
            return;
        }

        if (string.Equals(_currentToolId, toolId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentToolId = toolId;
        ActivateView(view);

        if (string.Equals(toolId, DashboardToolId, StringComparison.OrdinalIgnoreCase))
        {
            _ = UpdateStorageOverviewAsync();
        }
    }

    private void ActivateView(UIElement target)
    {
        foreach (var view in _allViews)
        {
            SetViewVisibility(view, ReferenceEquals(view, target));
        }

        PlayEntranceTransition(target);
    }

    private static void SetViewVisibility(UIElement view, bool shouldBeVisible)
    {
        var desired = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
        if (view.Visibility != desired)
        {
            view.Visibility = desired;
        }
    }

    private static void PlayEntranceTransition(UIElement view)
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

    private static async Task CancelAndDisposeAsync(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            await source.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            source.Dispose();
        }
    }

    private async Task UpdateStorageOverviewAsync()
    {
        var previousCts = _storageOverviewCts;
        if (previousCts is not null)
        {
            if (ReferenceEquals(_storageOverviewCts, previousCts))
            {
                _storageOverviewCts = null;
            }

            await CancelAndDisposeAsync(previousCts);
        }

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
                DashboardView.StorageSummaryTextBlock.Text = "No ready drives detected.";
                DashboardView.StorageTipTextBlock.Text = "Connect or unlock a drive to view usage details.";
                return;
            }

            if (result.Drives.Count == 0)
            {
                DashboardView.StorageSummaryTextBlock.Text = "No accessible drives detected.";
                DashboardView.StorageTipTextBlock.Text = "We couldn't read your drive information. Try running the app with higher permissions.";
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
            DashboardView.StorageSummaryTextBlock.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Monitoring {0} {1}. {2} free of {3}.",
                _driveUsage.Count,
                driveLabel,
                ValueFormatting.FormatBytes(totalFree),
                ValueFormatting.FormatBytes(totalCapacity));

            DashboardView.StorageTipTextBlock.Text = busiestDrive is not null ? GetStorageTip(busiestDrive) : "Storage tips will appear once drives are detected.";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Swallow cancellations when refreshing the storage overview.
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _driveUsage.Clear();
            DashboardView.StorageSummaryTextBlock.Text = "Storage overview is unavailable.";
            DashboardView.StorageTipTextBlock.Text = "We couldn't access drive information. Try again later or adjust your permissions.";

            ShowInfo(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Storage overview failed: {0}",
                    ex.Message),
                InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            _driveUsage.Clear();
            DashboardView.StorageSummaryTextBlock.Text = "Storage overview is unavailable.";
            DashboardView.StorageTipTextBlock.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Something went wrong while loading drives: {0}",
                ex.Message);

            ShowInfo(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Storage overview failed: {0}",
                    ex.Message),
                InfoBarSeverity.Error);
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
        return string.IsNullOrWhiteSpace(label) ? name : string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, label);
    }

    private static string GetStorageTip(DriveUsageViewModel drive)
    {
        var usageDetail = drive.UsageSummary;

        return drive.UsedPercentage switch
        {
            >= 90 => string.Format(CultureInfo.CurrentCulture,
                "{0} is running low on space ({1}). Remove large files or move content to free up storage.", drive.Name,
                usageDetail),
            >= 75 => string.Format(CultureInfo.CurrentCulture,
                "{0} is getting crowded ({1}). Consider cleaning temporary files or uninstalling unused apps.",
                drive.Name, usageDetail),
            _ => string.Format(CultureInfo.CurrentCulture,
                "{0} has plenty of room ({1}). Keep performing periodic cleanups to stay optimized.", drive.Name,
                usageDetail)
        };
    }


    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            
            try
            {
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize folder picker.");
                // Optionally, show a user-friendly error message here.
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            EmptyFoldersView.RootPathBox.Text = folder.Path;
            UpdateResultsActionState();
            SetStatus(
                Symbol.Folder,
                Localize("StatusFolderSelectedTitle", "Folder selected"),
                Localize("StatusFolderSelectedDescription", "Run Preview to identify empty directories."));
            SetActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
            UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));
        }
        catch (Exception ex)
        {
            ShowInfo(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Localize("InfoBrowseFailed", "Couldn't select a folder: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    private void RootPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        ClearPreviewTree();
        UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));
    }

    private void ApplyCleanerDefaultsToSession()
    {
        if (EmptyFoldersView.RecycleChk is not null)
        {
            EmptyFoldersView.RecycleChk.IsChecked = _cleanerSendToRecycleBin;
        }

        if (EmptyFoldersView.DepthBox is not null)
        {
            EmptyFoldersView.DepthBox.Value = _cleanerDepthLimit;
        }

        if (EmptyFoldersView.ExcludeBox is not null)
        {
            EmptyFoldersView.ExcludeBox.Text = _cleanerExclusions;
        }
    }

    private void UpdateCleanerDefaultsSummary()
    {
        if (SettingsView.CleanerDefaultsSummaryText is null)
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

        SettingsView.CleanerDefaultsSummaryText.Text = string.Format(
            CultureInfo.CurrentCulture,
            Localize("SettingsCleanerDefaultsSummary", "{0} • {1}"),
            recycleText,
            depthText);
    }

    private void UpdateAutomationSummary()
    {
        if (SettingsView.AutomationSummaryText is null)
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

        SettingsView.AutomationSummaryText.Text = descriptors.Count > 0
            ? string.Join(" • ", descriptors)
            : Localize("SettingsAutomationDisabled", "Automation disabled");
    }

    private void UpdateNotificationSummary()
    {
        if (SettingsView.NotificationSummaryText is null)
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

        SettingsView.NotificationSummaryText.Text = summary;
    }

    private void UpdateHistoryRetentionSummary()
    {
        if (SettingsView.HistoryRetentionSummaryText is null)
        {
            return;
        }

        var summary = _historyRetentionDays > 0
            ? string.Format(
                CultureInfo.CurrentCulture,
                Localize("SettingsHistoryRetentionSummary", "Keep history for {0} day(s)"),
                _historyRetentionDays)
            : Localize("SettingsHistoryRetentionOff", "Do not keep history");

        SettingsView.HistoryRetentionSummaryText.Text = summary;
    }

    private void ShowCleanerDefaultsInfo(string message, InfoBarSeverity severity)
    {
        if (SettingsView.CleanerDefaultsInfoBar is null)
        {
            return;
        }

        SettingsView.CleanerDefaultsInfoBar.Message = message;
        SettingsView.CleanerDefaultsInfoBar.Severity = severity;
        SettingsView.CleanerDefaultsInfoBar.IsOpen = true;
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
        EmptyFoldersView.StatusGlyph.Symbol = symbol;
        EmptyFoldersView.StatusTitle.Text = title;
        EmptyFoldersView.StatusDescription.Text = description;
        EmptyFoldersView.StatusHero.Background = GetStatusHeroBrush(symbol);
        EmptyFoldersView.StatusGlyph.Foreground = GetStatusGlyphBrush(symbol);

        UpdateResultBadgeValue(badgeValue ?? 0);
    }

    private void UpdateResultBadgeValue(int count)
    {
        if (count > 0)
        {
            EmptyFoldersView.ResultBadge.Value = count;
            EmptyFoldersView.ResultBadge.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyFoldersView.ResultBadge.ClearValue(InfoBadge.ValueProperty);
            EmptyFoldersView.ResultBadge.Visibility = Visibility.Collapsed;
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

    private enum EmptyFolderSortOption
    {
        NameAscending,
        NameDescending,
        DepthDescending,
    }

    private void UpdateResultsSummary(int count, string? customMessage = null, int? totalCount = null)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            EmptyFoldersView.ResultsCaption.Text = customMessage;
            return;
        }

        if (totalCount is > 0)
        {
            if (count == 0)
            {
                EmptyFoldersView.ResultsCaption.Text = Localize(
                    "ResultsFilteredEmpty",
                    "No folders match the current filters. Adjust the search or clear filters to review all items.");
            }
            else if (count == totalCount.Value)
            {
                EmptyFoldersView.ResultsCaption.Text = LocalizeFormat(
                    "ResultsAllVisible",
                    "Showing all {0} folders. Review the list below before cleaning.",
                    totalCount.Value);
            }
            else
            {
                EmptyFoldersView.ResultsCaption.Text = LocalizeFormat(
                    "ResultsFilteredSummary",
                    "Showing {0} of {1} folders after filters.",
                    count,
                    totalCount.Value);
            }

            return;
        }

        EmptyFoldersView.ResultsCaption.Text = count > 0
            ? Localize("ResultsAvailable", "Review the folders below before cleaning.")
            : Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan.");
    }

    private void SetActivity(string message)
    {
        EmptyFoldersView.ActivityText.Text = message;
        DiskCleanupView.DiskCleanupActivityText.Text = message;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        EmptyFoldersView.Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        EmptyFoldersView.Progress.IsIndeterminate = isBusy;
        EmptyFoldersView.CancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        EmptyFoldersView.CancelBtn.IsEnabled = isBusy;
        var showDiskCleanupCancel = isBusy && _isDiskCleanupOperation;
        DiskCleanupView.DiskCleanupCancelBtn.Visibility = showDiskCleanupCancel ? Visibility.Visible : Visibility.Collapsed;
        DiskCleanupView.DiskCleanupCancelBtn.IsEnabled = showDiskCleanupCancel;
        EmptyFoldersView.PreviewBtn.IsEnabled = !isBusy;
        EmptyFoldersView.BrowseBtn.IsEnabled = !isBusy;
        EmptyFoldersView.RootPathBox.IsEnabled = !isBusy;
        EmptyFoldersView.DepthBox.IsEnabled = !isBusy;
        EmptyFoldersView.ExcludeBox.IsEnabled = !isBusy;
        EmptyFoldersView.RecycleChk.IsEnabled = !isBusy;
        UpdateResultFilterControls();
        UpdateResultsActionState();
        UpdateDiskCleanupActionState();
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        EmptyFoldersView.Info.Message = message;
        EmptyFoldersView.Info.Severity = severity;
        EmptyFoldersView.Info.IsOpen = true;
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
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}