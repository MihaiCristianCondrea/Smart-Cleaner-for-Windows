using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using SmartCleanerForWindows.Core.DiskCleanup;
using SmartCleanerForWindows.Core.FileSystem;
using SmartCleanerForWindows.Core.LargeFiles;
using SmartCleanerForWindows.Core.Networking;
using SmartCleanerForWindows.Core.Storage;
using SmartCleanerForWindows.Diagnostics;
using SmartCleanerForWindows.Modules.Dashboard.ViewModels;
using SmartCleanerForWindows.Modules.DiskCleanup.ViewModels;
using SmartCleanerForWindows.Modules.DiskCleanup.Views;
using SmartCleanerForWindows.Modules.EmptyFolders;
using SmartCleanerForWindows.Modules.EmptyFolders.Contracts;
using SmartCleanerForWindows.Modules.EmptyFolders.ViewModels;
using SmartCleanerForWindows.Modules.EmptyFolders.Views;
using SmartCleanerForWindows.Modules.InternetRepair.ViewModels;
using SmartCleanerForWindows.Modules.InternetRepair.Views;
using SmartCleanerForWindows.Modules.LargeFiles.ViewModels;
using SmartCleanerForWindows.Modules.LargeFiles.Views;
using SmartCleanerForWindows.Settings;
using SmartCleanerForWindows.Shell.Settings;
using Windows.Storage;
using AppDataPaths = SmartCleanerForWindows.Diagnostics.AppDataPaths;
using WindowsColor = Windows.UI.Color;

namespace SmartCleanerForWindows.Shell;

/// <summary>
/// Main dashboard window for Smart Cleaner (tools + integrated settings view).
/// ToolSettingsWindow has been merged into this class (dynamic tool settings UI lives inside SettingsView).
/// </summary>
public sealed partial class MainWindow : IEmptyFolderCleanupView, ILargeFilesWorkflowView, ISettingsWorkflowView
{
    // - XAML namescope resolution (RootNavigation/DashboardView/InitializeComponent) now relies on correct Page include in csproj.
    // - Missing snapshot/application wiring is handled via ApplySnapshot and _settingsSnapshots updates.
    // - Shared status/localization/settings summary helpers are implemented in MainWindow.Shared.cs.
    // - Tool view event subscriptions are now connected in Ensure*View initializers.

    private readonly IDiskCleanupService _diskCleanupService;
    private readonly IStorageOverviewService _storageOverviewService;
    private readonly ILargeFileExplorer _largeFileExplorer;
    private readonly IInternetRepairService _internetRepairService;

    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;

    private readonly EmptyFolderCleanupController _emptyFolderController = null!;
    private readonly List<string> _previewCandidates = [];
    private readonly ObservableCollection<EmptyFolderNode> _emptyFolderRoots = [];
    private readonly ObservableCollection<EmptyFolderNode> _filteredEmptyFolderRoots = [];
    private readonly Dictionary<string, EmptyFolderNode> _emptyFolderLookup = new(FileSystemPathComparer.PathComparer);
    private readonly HashSet<string> _inlineExcludedPaths = new(FileSystemPathComparer.PathComparer);

    private int _totalPreviewCount;
    private string _currentPreviewRoot = string.Empty;
    private string _currentResultSearch = string.Empty;
    private bool _hideExcludedResults;
    private EmptyFolderSortOption _currentResultSort = EmptyFolderSortOption.NameAscending;
    private bool _isBusy;

    private readonly ObservableCollection<DriveUsageViewModel> _driveUsage = [];
    private readonly ObservableCollection<DiskCleanupItemViewModel> _diskCleanupItems = [];
    private readonly ObservableCollection<LargeFileGroupViewModel> _largeFileGroups = [];
    private readonly ObservableCollection<InternetRepairLogEntry> _internetRepairLog = [];

    private readonly EmptyFoldersWorkflowCoordinator _emptyFoldersWorkflow = new();
    private readonly SettingsCoordinator _settingsCoordinator = new();
    private readonly LargeFilesWorkflowCoordinator _largeFilesWorkflow;

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

    private readonly Dictionary<string, WindowsColor> _defaultAccentColors = new();

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

    private readonly ObservableCollection<NavigationViewItem> _navigationItems = [];
    private readonly Dictionary<string, Func<UIElement?>> _toolViewLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<UIElement?>> _viewFactoryLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToolSettingsSnapshot> _settingsSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<UIElement> _allViews = [];

    private string? _currentToolId;
    private bool _emptyFoldersViewInitialized;
    private bool _largeFilesViewInitialized;
    private bool _diskCleanupViewInitialized;
    private bool _internetRepairViewInitialized;
    private bool _settingsViewInitialized;
    
    private bool _toolSettingsUiInitialized;
    private NavigationView? _toolSettingsNavigation;
    private Panel? _toolSettingsFieldsHost;
    private readonly Dictionary<string, JsonObject> _toolSettingsPendingValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NavigationViewItem> _toolSettingsMenuItems = [];
    private ToolSettingsDefinition? _toolSettingsActiveDefinition;

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

    internal bool IsFallbackShellActive { get; private set; }
    internal Exception? InitializationFailure { get; private set; }

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
        : this(
            directoryCleaner,
            diskCleanupService,
            new StorageOverviewService(),
            LargeFileExplorer.Default,
            InternetRepairServiceFactory.CreateDefault())
    {
    }

    public MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService)
        : this(
            directoryCleaner,
            diskCleanupService,
            storageOverviewService,
            LargeFileExplorer.Default,
            InternetRepairServiceFactory.CreateDefault())
    {
    }

    private MainWindow(
        IDirectoryCleaner directoryCleaner,
        IDiskCleanupService diskCleanupService,
        IStorageOverviewService storageOverviewService,
        ILargeFileExplorer largeFileExplorer,
        IInternetRepairService internetRepairService)
    {
        var cleaner = directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner));
        _diskCleanupService = diskCleanupService ?? throw new ArgumentNullException(nameof(diskCleanupService));
        _storageOverviewService = storageOverviewService ?? throw new ArgumentNullException(nameof(storageOverviewService));
        _largeFileExplorer = largeFileExplorer ?? throw new ArgumentNullException(nameof(largeFileExplorer));
        _internetRepairService = internetRepairService ?? throw new ArgumentNullException(nameof(internetRepairService));

        _diskCleanupVolume = _diskCleanupService.GetDefaultVolume();
        _largeFilesWorkflow = new LargeFilesWorkflowCoordinator(this);

        // ✅ This is now valid because the class is a Window and XAML generation works.
        if (!TryInitializeComponentWithDiagnostics(out var xamlFailure))
        {
            _toolSettingsService.Dispose();
            IsFallbackShellActive = true;
            InitializationFailure = xamlFailure;
            BuildFallbackShell(xamlFailure);
            return;
        }

        _emptyFolderController = new EmptyFolderCleanupController(cleaner, this);

        DashboardView.NavigateToEmptyFoldersRequested += (_, _) => NavigateToTool(EmptyFoldersToolId);
        DashboardView.NavigateToLargeFilesRequested += (_, _) => NavigateToTool(LargeFilesToolId);
        DashboardView.NavigateToDiskCleanupRequested += (_, _) => NavigateToTool(DiskCleanupToolId);
        DashboardView.NavigateToInternetRepairRequested += (_, _) => NavigateToTool(InternetRepairToolId);

        CaptureDefaultAccentColors();
        LoadPreferences();

        DashboardView.DriveUsageListControl.ItemsSource = _driveUsage;
        _ = UpdateStorageOverviewAsync();

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
            Log.Error("XAML load failed while constructing MainWindow.\n{Details}", XamlDiagnostics.Format(fileEx));
            failure = fileEx;
        }
        catch (XamlParseException xamlEx)
        {
            if (xamlEx.InnerException is FileNotFoundException innerFileEx)
            {
                Log.Error(
                    "XAML parse failed with inner FileNotFoundException while constructing MainWindow.\n{Details}",
                    XamlDiagnostics.Format(innerFileEx));
            }

            Log.Error("XAML parse failed while constructing MainWindow.\n{Details}", XamlDiagnostics.Format(xamlEx));
            failure = xamlEx;
        }
        catch (Exception ex)
        {
            Log.Error("Unexpected failure during MainWindow InitializeComponent.\n{Details}", XamlDiagnostics.Format(ex));
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
        if (Application.Current?.Resources?.TryGetValue("SystemControlForegroundBaseMediumBrush", out var resourceValue) == true
            && resourceValue is SolidColorBrush resolvedBrush)
        {
            fallbackInfoBrush = resolvedBrush;
        }

        var openLogsButton = new Button
        {
            Content = "Open logs folder",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        openLogsButton.Click += OnFallbackOpenLogs;

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
                        },
                        openLogsButton
                    }
                }
            }
        };
    }

    private static void OnFallbackOpenLogs(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppDataPaths.GetLogsDirectory(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open logs directory from fallback shell.");
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Activated -= OnWindowActivated;

        if (_toolSettingsNavigation is not null)
        {
            _toolSettingsNavigation.SelectionChanged -= OnToolSettingsNavigationSelectionChanged;
        }

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
        if (cts is null) return null;

        try { cts.Cancel(); }
        catch (ObjectDisposedException) { }

        cts.Dispose();
        return null;
    }

    // ----------------------------
    // Navigation + view materialization (unchanged, relies on XAML x:Name fields)
    // ----------------------------

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            var settingsView = EnsureSettingsView();
            if (settingsView is not null)
            {
                ShowPage(settingsView);
            }

            _currentToolId = "settings";
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            ShowPage(item);
        }
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
        _viewFactoryLookup.Clear();
        _viewFactoryLookup["Dashboard"] = () => DashboardView;
        _viewFactoryLookup["EmptyFolders"] = EnsureEmptyFoldersView;
        _viewFactoryLookup["LargeFiles"] = EnsureLargeFilesView;
        _viewFactoryLookup["DiskCleanup"] = EnsureDiskCleanupView;
        _viewFactoryLookup["InternetRepair"] = EnsureInternetRepairView;

        _allViews.Clear();
        _allViews.Add(DashboardView);
    }

    private void NavigateToTool(string toolId)
    {
        if (!_toolViewLookup.TryGetValue(toolId, out _))
        {
            return;
        }

        var item = _navigationItems.FirstOrDefault(candidate =>
            candidate.Tag is string tag && string.Equals(tag, toolId, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            return;
        }

        _currentToolId = toolId;
        RootNavigation.SelectedItem = item;
        ShowPage(item);
    }

    private void ShowPage(object? selection)
    {
        UIElement? selectedView = selection switch
        {
            NavigationViewItem { Tag: string toolId } when _toolViewLookup.TryGetValue(toolId, out var viewFactory) => viewFactory(),
            UIElement directView => directView,
            _ => null
        };

        if (selectedView is null)
        {
            return;
        }

        foreach (var view in _allViews)
        {
            if (view != selectedView)
            {
                view.Visibility = Visibility.Collapsed;
            }
        }

        if (!_allViews.Contains(selectedView))
        {
            _allViews.Add(selectedView);
        }

        selectedView.Visibility = Visibility.Visible;
    }

    private TView? EnsureView<TView>(string elementName, ref bool initialized, Action<TView>? configure = null)
        where TView : class
    {
        var view = FindElement(elementName) as TView;
        if (view is null)
        {
            return null;
        }

        if (!initialized)
        {
            configure?.Invoke(view);
            initialized = true;
        }

        if (view is UIElement element)
        {
            element.Visibility = Visibility.Visible;
        }

        return view;
    }

    private EmptyFoldersView? EnsureEmptyFoldersView()
    {
        return EnsureView<EmptyFoldersView>(nameof(EmptyFoldersView), ref _emptyFoldersViewInitialized, _ =>
        {
            EmptyFoldersView.CandidatesTree.ItemsSource = _filteredEmptyFolderRoots;
            EmptyFoldersView.BrowseRequested += OnBrowse;
            EmptyFoldersView.PreviewRequested += OnPreview;
            EmptyFoldersView.DeleteRequested += OnDelete;
            EmptyFoldersView.CancelRequested += OnCancel;
            EmptyFoldersView.ResultSearchChanged += OnResultSearchChanged;
            EmptyFoldersView.ResultSortChanged += OnResultSortChanged;
            EmptyFoldersView.HideExcludedToggled += OnHideExcludedToggled;
            EmptyFoldersView.ResultFiltersCleared += OnClearResultFilters;
            EmptyFoldersView.ExcludeSelectedRequested += OnExcludeSelected;
            EmptyFoldersView.IncludeSelectedRequested += OnIncludeSelected;
            EmptyFoldersView.InlineExclusionsCleared += OnClearInlineExclusions;
            EmptyFoldersView.CandidatesSelectionChanged += OnCandidatesSelectionChanged;
            UpdateResultsActionState();
        });
    }

    private LargeFilesView? EnsureLargeFilesView()
    {
        return EnsureView<LargeFilesView>(nameof(LargeFilesView), ref _largeFilesViewInitialized, _ =>
        {
            LargeFilesView.LargeFilesGroupList.ItemsSource = _largeFileGroups;
            LargeFilesView.LargeFilesExclusionsList.ItemsSource = _largeFilesWorkflow.Exclusions;
            LargeFilesView.BrowseRequested += OnLargeFilesBrowse;
            LargeFilesView.ScanRequested += OnLargeFilesScan;
            LargeFilesView.CancelRequested += OnLargeFilesCancel;
            LargeFilesView.RootPathChanged += OnLargeFilesRootPathChanged;
            LargeFilesView.OpenRequested += OnLargeFileOpen;
            LargeFilesView.DeleteRequested += OnLargeFileDelete;
            LargeFilesView.ExcludeRequested += OnLargeFileExclude;
            LargeFilesView.RemoveExclusionRequested += OnLargeFilesRemoveExclusion;
            LargeFilesView.ClearExclusionsRequested += OnLargeFilesClearExclusions;
            LoadLargeFilePreferences();
            SetLargeFilesStatus(
                Symbol.SaveLocal,
                Localize("LargeFilesStatusReadyTitle", "Ready to explore large files"),
                Localize("LargeFilesStatusReadyDescription", "Choose a location to find the biggest files grouped by type."));
            SetLargeFilesResultsCaption(Localize("LargeFilesResultsPlaceholder", "Scan results will appear here after you run a scan."));
            SetLargeFilesActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
        });
    }

    private DiskCleanupView? EnsureDiskCleanupView()
    {
        return EnsureView<DiskCleanupView>(nameof(DiskCleanupView), ref _diskCleanupViewInitialized, _ =>
        {
            DiskCleanupView.DiskCleanupList.ItemsSource = _diskCleanupItems;
            DiskCleanupView.AnalyzeRequested += OnDiskCleanupAnalyze;
            DiskCleanupView.CleanRequested += OnDiskCleanupClean;
            DiskCleanupView.CancelRequested += OnCancel;
            UpdateDiskCleanupActionState();
        });
    }

    private InternetRepairView? EnsureInternetRepairView()
    {
        return EnsureView<InternetRepairView>(nameof(InternetRepairView), ref _internetRepairViewInitialized, _ =>
        {
            InternetRepairView.InternetRepairLogList.ItemsSource = _internetRepairLog; 
            InternetRepairView.RunRequested += OnInternetRepairRun; 
            InternetRepairView.CancelRequested += OnInternetRepairCancel;
            InternetRepairView.ActionSelectionChanged += OnInternetRepairActionSelectionChanged;
            InitializeInternetRepair();
        });
    }

    private void OnToolSettingsNavigationLoaded(object sender, RoutedEventArgs e)
    {
        // Embedded tool settings host is initialized through SettingsView once it is loaded.
    }

    private async Task UpdateStorageOverviewAsync()
    {
        _storageOverviewCts = SafeCancelAndDispose(_storageOverviewCts);
        _storageOverviewCts = new CancellationTokenSource();

        try
        {
            var result = await _storageOverviewService.GetDriveUsageAsync(_storageOverviewCts.Token).ConfigureAwait(true);

            _driveUsage.Clear();
            foreach (var drive in result.Drives)
            {
                var usedBytes = drive.TotalSize > drive.FreeSpace ? drive.TotalSize - drive.FreeSpace : 0UL;
                var usedPercentage = drive.TotalSize == 0
                    ? 0
                    : Math.Clamp((double)usedBytes / drive.TotalSize * 100.0, 0, 100);

                var details = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.Name
                    : string.Format(CultureInfo.CurrentCulture, "{0} ({1})", drive.Name, drive.VolumeLabel);

                var usageSummary = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} used of {1}",
                    FormatStorageSize(usedBytes),
                    FormatStorageSize(drive.TotalSize));

                _driveUsage.Add(new DriveUsageViewModel(drive.Name, details, usedPercentage, usageSummary));
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation.
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to refresh dashboard storage overview.");
            _driveUsage.Clear();
        }
        finally
        {
            _storageOverviewCts = SafeCancelAndDispose(_storageOverviewCts);
        }
    }

    private static string FormatStorageSize(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", value, units[unitIndex]);
    }

    private void BuildToolNavigation()
    {
        _toolViewLookup.Clear();
        _navigationItems.Clear();

        foreach (var definition in _toolSettingsService.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.ViewKey)) continue;
            if (!_viewFactoryLookup.TryGetValue(definition.ViewKey!, out var viewFactory)) continue;

            var navItem = CreateNavigationItem(definition);
            _navigationItems.Add(navItem);
            _toolViewLookup[definition.Id] = viewFactory;

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
        var item = new NavigationViewItem { Content = definition.Title, Tag = definition.Id };

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

    private void OnToolSettingsChanged(object? sender, ToolSettingsChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplySnapshot(e.Snapshot);

            if (_toolSettingsUiInitialized &&
                _toolSettingsActiveDefinition is not null &&
                string.Equals(e.Snapshot.Definition.Id, _toolSettingsActiveDefinition.Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_currentToolId, e.Snapshot.Definition.Id, StringComparison.OrdinalIgnoreCase))
            {
                _toolSettingsPendingValues[_toolSettingsActiveDefinition.Id] = e.Snapshot.Values;
                RenderToolSettingsFields(e.Snapshot.Definition, e.Snapshot.Values);
            }
        });
    }

    // ----------------------------
    // Folder picker: native WinUI desktop pattern
    // ----------------------------
    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPath = await PickFolderPathAsync().ConfigureAwait(true);
            if (folderPath is null) return;

            EmptyFoldersView.RootPathBox.Text = folderPath;

            UpdateResultsActionState();
            SetStatus(Symbol.Folder,
                Localize("StatusFolderSelectedTitle", "Folder selected"),
                Localize("StatusFolderSelectedDescription", "Run Preview to identify empty directories."));
            SetActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
            UpdateResultsSummary(0, Localize("ResultsPlaceholder", "Preview results will appear here once you run a scan."));
        }
        catch (Exception ex)
        {
            ShowInfo(
                string.Format(CultureInfo.CurrentCulture,
                    Localize("InfoBrowseFailed", "Couldn't select a folder: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    // =========================================================================================
    // MERGED: ToolSettingsWindow logic (dynamic tool settings UI inside SettingsView)
    // =========================================================================================

    private SettingsView? EnsureSettingsView()
    {
        return EnsureView<SettingsView>(nameof(SettingsView), ref _settingsViewInitialized, view =>
        {
            view.ThemeSelectionChanged += OnThemeSelectionChanged;
            view.AccentPreferenceChanged += OnAccentPreferenceChanged;
            view.CleanerDefaultsApplied += OnApplyCleanerDefaults;
            view.CleanerRecyclePreferenceToggled += OnCleanerRecyclePreferenceToggled;
            view.CleanerExclusionsPreferenceChanged += OnCleanerExclusionsPreferenceChanged;
            view.CleanerDepthPreferenceChanged += OnCleanerDepthPreferenceChanged;
            view.AutomationPreferenceToggled += OnAutomationPreferenceToggled;
            view.NotificationPreferenceToggled += OnNotificationPreferenceToggled;
            view.HistoryRetentionChanged += OnHistoryRetentionChanged;

            UpdateCleanerSettingsView();
            UpdateAutomationSettingsView();
            UpdateAutomationSummary();
            UpdateNotificationSummary();
            UpdateCleanerDefaultsSummary();
            UpdateHistoryRetentionSummary();
            
            RoutedEventHandler? loaded = null;
            loaded = (_, _) =>
            {
                view.Loaded -= loaded;
                InitializeToolSettingsUi();
            };
            view.Loaded += loaded;
        });
    }

    private void InitializeToolSettingsUi()
    {
        _toolSettingsNavigation ??= FindElement("ToolsNavigation") as NavigationView;
        _toolSettingsFieldsHost ??= FindElement("FieldsHost") as Panel;

        if (_toolSettingsNavigation is null || _toolSettingsFieldsHost is null)
        {
            Log.Warning("Tool settings UI host not found. Expected elements named ToolsNavigation and FieldsHost inside SettingsView.");
            return;
        }

        _toolSettingsNavigation.SelectionChanged -= OnToolSettingsNavigationSelectionChanged;
        _toolSettingsNavigation.SelectionChanged += OnToolSettingsNavigationSelectionChanged;

        _toolSettingsUiInitialized = true;

        BuildToolSettingsMenu();

        if (_toolSettingsNavigation.SelectedItem is null && _toolSettingsMenuItems.Count > 0)
        {
            _toolSettingsNavigation.SelectedItem = _toolSettingsMenuItems[0];
        }

        if (_toolSettingsNavigation.SelectedItem is NavigationViewItem { Tag: string toolId })
        {
            LoadAndRenderToolSettings(toolId);
        }
    }

    private void BuildToolSettingsMenu()
    {
        if (_toolSettingsNavigation is null) return;

        _toolSettingsMenuItems.Clear();
        _toolSettingsNavigation.MenuItems.Clear();

        foreach (var definition in _toolSettingsService.Definitions)
        {
            var item = new NavigationViewItem
            {
                Content = definition.Title,
                Tag = definition.Id,
                Icon = TryCreateToolSettingsIcon(definition.Icon)
            };

            _toolSettingsMenuItems.Add(item);
            _toolSettingsNavigation.MenuItems.Add(item);
        }
    }

    private static IconElement? TryCreateToolSettingsIcon(string? icon)
    {
        if (Enum.TryParse<Symbol>(icon, ignoreCase: true, out var symbol))
        {
            return new SymbolIcon(symbol);
        }

        return null;
    }

    private void OnToolSettingsNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string toolId) return;
        _currentToolId = toolId;
        LoadAndRenderToolSettings(toolId);
    }

    private void LoadAndRenderToolSettings(string toolId)
    {
        if (_toolSettingsFieldsHost is null) return;

        if (_toolSettingsService.GetSnapshot(toolId) is not { } snapshot) return;

        _currentToolId = toolId;
        _toolSettingsActiveDefinition = snapshot.Definition;
        _toolSettingsPendingValues[toolId] = snapshot.Values;

        RenderToolSettingsFields(snapshot.Definition, snapshot.Values);
    }

    private void RenderToolSettingsFields(ToolSettingsDefinition definition, JsonObject values)
    {
        if (_toolSettingsFieldsHost is null) return;

        _toolSettingsFieldsHost.Children.Clear();

        var header = new TextBlock
        {
            Text = definition.Description ?? definition.Title,
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
            TextWrapping = TextWrapping.Wrap
        };

        _toolSettingsFieldsHost.Children.Add(header);

        foreach (var field in definition.Fields)
        {
            UIElement element = field.FieldType switch
            {
                ToolSettingFieldType.Boolean => CreateToolSettingsToggle(field, values),
                ToolSettingFieldType.Number => CreateToolSettingsNumberBox(field, values),
                _ => CreateToolSettingsTextBox(field, values)
            };

            _toolSettingsFieldsHost.Children.Add(element);
        }
    }

    private UIElement CreateToolSettingsToggle(ToolSettingField field, JsonObject values)
    {
        var container = CreateToolSettingsFieldContainer(field);

        var toggle = new ToggleSwitch
        {
            Header = field.DisplayName,
            IsOn = values.TryGetPropertyValue(field.Key, out var node) && node?.GetValue<bool>() == true,
            Tag = field
        };

        toggle.Toggled += (_, _) => PersistToolSettingsBoolean(toggle);
        container.Children.Add(toggle);

        return container;
    }

    private UIElement CreateToolSettingsNumberBox(ToolSettingField field, JsonObject values)
    {
        var container = CreateToolSettingsFieldContainer(field);

        var numberBox = new NumberBox
        {
            Header = field.DisplayName,
            Value = values.TryGetPropertyValue(field.Key, out var node) ? node?.GetValue<double>() ?? 0 : 0,
            Minimum = field.Minimum ?? double.MinValue,
            Maximum = field.Maximum ?? double.MaxValue,
            SmallChange = field.Step ?? 1,
            Tag = field
        };

        numberBox.ValueChanged += (_, args) => PersistToolSettingsNumber(numberBox, args.NewValue);
        container.Children.Add(numberBox);

        return container;
    }

    private UIElement CreateToolSettingsTextBox(ToolSettingField field, JsonObject values)
    {
        var container = CreateToolSettingsFieldContainer(field);

        var textBox = new TextBox
        {
            Header = field.DisplayName,
            Text = values.TryGetPropertyValue(field.Key, out var node) ? node?.ToString() ?? string.Empty : string.Empty,
            Tag = field
        };

        textBox.TextChanged += (_, _) => PersistToolSettingsText(textBox);
        container.Children.Add(textBox);

        return container;
    }

    private static StackPanel CreateToolSettingsFieldContainer(ToolSettingField field)
    {
        var panel = new StackPanel { Spacing = 4 };

        if (!string.IsNullOrWhiteSpace(field.Description))
        {
            panel.Children.Add(new TextBlock
            {
                Text = field.Description,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
        }

        return panel;
    }

    private void PersistToolSettingsBoolean(ToggleSwitch toggle)
    {
        if (toggle.Tag is not ToolSettingField field || _toolSettingsActiveDefinition is null) return;
        _ = UpdateToolSettingValueAsync(field.Key, JsonValue.Create(toggle.IsOn));
    }

    private void PersistToolSettingsNumber(NumberBox numberBox, double value)
    {
        if (numberBox.Tag is not ToolSettingField field || _toolSettingsActiveDefinition is null) return;
        _ = UpdateToolSettingValueAsync(field.Key, JsonValue.Create(value));
    }

    private void PersistToolSettingsText(TextBox textBox)
    {
        if (textBox.Tag is not ToolSettingField field || _toolSettingsActiveDefinition is null) return;
        _ = UpdateToolSettingValueAsync(field.Key, JsonValue.Create(textBox.Text));
    }

    private async Task UpdateToolSettingValueAsync(string key, JsonNode? value)
    {
        if (_toolSettingsActiveDefinition is null || value is null) return;

        if (!_toolSettingsPendingValues.TryGetValue(_toolSettingsActiveDefinition.Id, out var values))
        {
            values = new JsonObject();
            _toolSettingsPendingValues[_toolSettingsActiveDefinition.Id] = values;
        }

        values[key] = value;

        try
        {
            await _toolSettingsService.UpdateAsync(_toolSettingsActiveDefinition.Id, values);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist tool setting {ToolId}:{Key}", _toolSettingsActiveDefinition.Id, key);
        }
    }

    // =========================================================================================
    // The rest of your members (TryEnableMica/theme/titlebar/prefs + tool handlers) remain in other partials.
    // =========================================================================================

    private enum EmptyFolderSortOption
    {
        NameAscending,
        NameDescending,
        DepthDescending,
    }

    // NOTE: EnsureView<T>, EnsureEmptyFoldersView/EnsureLargeFilesView/etc are assumed unchanged below.
}
