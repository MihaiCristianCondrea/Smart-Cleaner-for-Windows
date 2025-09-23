using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Smart_Cleaner_for_Windows.Core;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT;
using WinRT.Interop;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow : Window
{
    private readonly IDirectoryCleaner _directoryCleaner;
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private List<string> _previewCandidates = new();
    private bool _isBusy;
    private readonly ObservableCollection<DiskCleanupItemViewModel> _diskCleanupItems = new();
    private CancellationTokenSource? _diskCleanupCts;
    private readonly string _diskCleanupVolume = DiskCleanupManager.GetDefaultVolume();
    private bool _isDiskCleanupOperation;
    private readonly Dictionary<string, Color> _defaultAccentColors = new();
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
        : this(DirectoryCleaner.Default)
    {
    }

    public MainWindow(IDirectoryCleaner directoryCleaner)
    {
        _directoryCleaner = directoryCleaner ?? throw new ArgumentNullException(nameof(directoryCleaner));

        InitializeComponent();

        CaptureDefaultAccentColors();

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyleObj) &&
            accentStyleObj is Style accentStyle)
        {
            DeleteBtn.Style = accentStyle;
            DiskCleanupCleanBtn.Style = accentStyle;
        }

        SetStatus(Symbol.Folder, "Ready when you are", "Select a folder to begin.");
        SetActivity("Waiting for the next action.");
        UpdateResultsSummary(0, "Preview results will appear here once you run a scan.");

        DiskCleanupList.ItemsSource = _diskCleanupItems;
        DiskCleanupStatusText.Text = $"Ready to analyze disk cleanup handlers for {_diskCleanupVolume}.";
        if (!IsAdministrator())
        {
            DiskCleanupIntro.Text = "Analyze Windows cleanup handlers. Some categories require Administrator privileges.";
        }
        UpdateDiskCleanupActionState();

        TryEnableMica();
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
    }

    private void TryEnableMica()
    {
        if (!MicaController.IsSupported())
        {
            return;
        }

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
        _mica.AddSystemBackdropTarget(this.As<global::Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
        _mica.SetSystemBackdropConfiguration(_backdropConfig);
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

    private void OnNavigationLoaded(object sender, RoutedEventArgs e)
    {
        if (RootNavigation.SelectedItem is NavigationViewItem selectedItem)
        {
            ShowPage(selectedItem);
        }
        else
        {
            NavigateTo(DashboardItem);
        }
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            ShowPage(item);
        }
    }

    private void OnNavigateToEmptyFolders(object sender, RoutedEventArgs e) => NavigateTo(EmptyFoldersItem);

    private void OnNavigateToDiskCleanup(object sender, RoutedEventArgs e) => NavigateTo(DiskCleanupItem);

    private void OnNavigateToSettings(object sender, RoutedEventArgs e) => NavigateTo(SettingsItem);

    private void NavigateTo(NavigationViewItem item)
    {
        if (!Equals(RootNavigation.SelectedItem, item))
        {
            RootNavigation.SelectedItem = item;
        }

        ShowPage(item);
    }

    private void ShowPage(NavigationViewItem item)
    {
        DashboardView.Visibility = item == DashboardItem ? Visibility.Visible : Visibility.Collapsed;
        EmptyFoldersView.Visibility = item == EmptyFoldersItem ? Visibility.Visible : Visibility.Collapsed;
        DiskCleanupView.Visibility = item == DiskCleanupItem ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = item == SettingsItem ? Visibility.Visible : Visibility.Collapsed;
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
            SetStatus(Symbol.Folder, "Folder selected", "Run Preview to identify empty directories.");
            SetActivity("Ready to scan the selected folder.");
            UpdateResultsSummary(0, "Preview results will appear here once you run a scan.");
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
        UpdateResultsSummary(0, "Preview results will appear here once you run a scan.");
    }

    private async void OnPreview(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        if (!TryGetRootPath(out var root))
        {
            ShowInfo("Select a valid folder.", InfoBarSeverity.Warning);
            SetStatus(Symbol.Important, "Select a valid folder", "Choose a folder before scanning.");
            UpdateResultsSummary(0, "Select a valid folder to run a scan.");
            SetActivity("Waiting for a valid folder.");
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        _previewCandidates = new List<string>();
        Candidates.ItemsSource = null;
        DeleteBtn.IsEnabled = false;

        SetBusy(true);
        SetActivity("Scanning for empty folders…");
        SetStatus(Symbol.Sync, "Scanning in progress…", "Looking for empty folders. You can cancel the scan if needed.");
        UpdateResultsSummary(0, "Scanning for empty folders…");

        try
        {
            var options = CreateOptions(dryRun: true);
            var result = await Task.Run(() => _directoryCleaner.Clean(root, options, _cts.Token));

            _previewCandidates = new List<string>(result.EmptyDirectories);
            Candidates.ItemsSource = _previewCandidates;
            DeleteBtn.IsEnabled = !_isBusy && _previewCandidates.Count > 0;

            var hasResults = result.EmptyFound > 0;
            var resultsMessage = result.HasFailures
                ? "Some folders might be missing from the preview due to access issues."
                : hasResults
                    ? "Review the folders below before cleaning."
                    : "No empty folders were detected for this location.";
            UpdateResultsSummary(result.EmptyFound, resultsMessage);

            var message = $"Found {result.EmptyFound} empty folder(s).";
            var severity = InfoBarSeverity.Informational;
            if (result.HasFailures)
            {
                message += $" Encountered {result.Failures.Count} issue(s).";
                severity = InfoBarSeverity.Warning;
            }

            var statusTitle = result.HasFailures
                ? "Scan completed with warnings"
                : hasResults
                    ? $"Found {result.EmptyFound} empty folder(s)"
                    : "No empty folders detected";
            var statusDescription = result.HasFailures
                ? "Some items could not be analyzed. Review the message below."
                : hasResults
                    ? "Review the folders list below before cleaning."
                    : "Everything looks tidy. Try adjusting filters if you expected more.";
            var statusSymbol = result.HasFailures
                ? Symbol.Important
                : hasResults
                    ? Symbol.View
                    : Symbol.Accept;
            int? badgeValue = hasResults ? result.EmptyFound : null;

            SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
            SetActivity("Scan complete.");

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            SetActivity("Scan cancelled.");
            SetStatus(Symbol.Cancel, "Scan cancelled", "Preview was cancelled. Adjust settings or try again.");
            UpdateResultsSummary(0, "Preview was cancelled. Run Preview to refresh the list.");
            ShowInfo("Preview cancelled.", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            SetActivity("Something went wrong.");
            SetStatus(Symbol.Important, "Scan failed", "An unexpected error occurred. Review the message below.");
            UpdateResultsSummary(0, "The scan failed. Review the message above and try again.");
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
            ShowInfo("Select a valid folder.", InfoBarSeverity.Warning);
            SetStatus(Symbol.Important, "Select a valid folder", "Choose a folder before cleaning.");
            UpdateResultsSummary(0, "Select a valid folder before cleaning.");
            SetActivity("Waiting for a valid folder.");
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        SetBusy(true);
        SetActivity("Cleaning empty folders…");
        var pendingCount = _previewCandidates.Count;
        int? pendingBadge = pendingCount > 0 ? pendingCount : null;
        SetStatus(
            Symbol.Delete,
            "Cleaning in progress…",
            "Removing empty folders safely. You can cancel the operation if needed.",
            pendingBadge);
        UpdateResultsSummary(pendingCount, pendingCount > 0
            ? "Cleaning in progress. We'll refresh the preview afterwards."
            : "Cleaning in progress…");

        try
        {
            var options = CreateOptions(dryRun: false);
            var result = await Task.Run(() => _directoryCleaner.Clean(root, options, _cts.Token));

            _previewCandidates.Clear();
            Candidates.ItemsSource = null;
            DeleteBtn.IsEnabled = false;

            var message = result.EmptyFound == 0
                ? "No empty folders detected."
                : $"Deleted {result.DeletedCount} folder(s).";
            var severity = result.EmptyFound == 0 ? InfoBarSeverity.Informational : InfoBarSeverity.Success;

            if (result.EmptyFound > result.DeletedCount)
            {
                var remaining = result.EmptyFound - result.DeletedCount;
                message += $" {remaining} item(s) could not be removed.";
            }

            if (result.HasFailures)
            {
                message += $" Encountered {result.Failures.Count} issue(s).";
                severity = InfoBarSeverity.Warning;
            }

            int? badgeValue = result.DeletedCount > 0 ? result.DeletedCount : null;
            var statusSymbol = result.HasFailures || result.EmptyFound > result.DeletedCount
                ? Symbol.Important
                : result.DeletedCount > 0
                    ? Symbol.Accept
                    : Symbol.Message;
            var statusTitle = result.HasFailures
                ? "Clean completed with warnings"
                : result.EmptyFound == 0
                    ? "No empty folders detected"
                    : result.EmptyFound > result.DeletedCount
                        ? "Some folders could not be removed"
                        : $"Removed {result.DeletedCount} folder(s)";
            var statusDescription = result.HasFailures
                ? "Some folders could not be removed. Review the message below."
                : result.EmptyFound == 0
                    ? "Run Preview to check another location."
                    : result.EmptyFound > result.DeletedCount
                        ? "Some items remain because they could not be deleted."
                        : "Your workspace is tidier. Run Preview again to double-check.";

            SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
            SetActivity("Clean complete.");
            UpdateResultsSummary(0, result.DeletedCount > 0
                ? "Clean completed. Run Preview again to scan another location."
                : "No empty folders were removed. Run Preview to check again.");

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            SetActivity("Clean cancelled.");
            SetStatus(Symbol.Cancel, "Clean cancelled", "Deletion was cancelled. Preview again when you're ready.");
            UpdateResultsSummary(0, "Clean cancelled. Run Preview to refresh the list.");
            ShowInfo("Deletion cancelled.", InfoBarSeverity.Informational);
        }
        catch (UnauthorizedAccessException)
        {
            SetActivity("Permission required.");
            SetStatus(Symbol.Important, "Access denied", "Run the app as Administrator to remove protected folders.");
            UpdateResultsSummary(0, "Some folders could not be removed due to permissions.");
            ShowInfo("Access denied. Try running as Administrator.", InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            SetActivity("Something went wrong.");
            SetStatus(Symbol.Important, "Clean failed", "An unexpected error occurred. Review the message below.");
            UpdateResultsSummary(0, "Cleaning failed. Review the details and try again.");
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

        if (cancelled && (_isBusy || _isDiskCleanupOperation))
        {
            SetActivity("Cancelling current operation…");
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

    private void SetStatus(Symbol symbol, string title, string description, int? badgeValue = null)
    {
        StatusGlyph.Symbol = symbol;
        StatusTitle.Text = title;
        StatusDescription.Text = description;

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

    private void UpdateResultsSummary(int count, string? customMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            ResultsCaption.Text = customMessage;
            return;
        }

        ResultsCaption.Text = count > 0
            ? "Review the folders below before cleaning."
            : "Preview results will appear here once you run a scan.";
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
            ShowDiskCleanupInfo("Finish the current operation before analyzing disk cleanup.", InfoBarSeverity.Warning);
            return;
        }

        _diskCleanupCts?.Cancel();
        _diskCleanupCts?.Dispose();
        _diskCleanupCts = new CancellationTokenSource();

        DiskCleanupInfoBar.IsOpen = false;
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;
        SetActivity("Analyzing disk cleanup handlers…");
        SetBusy(true);

        try
        {
            var items = await DiskCleanupManager.AnalyzeAsync(_diskCleanupVolume, _diskCleanupCts.Token);
            ApplyDiskCleanupResults(items);
            UpdateDiskCleanupStatusSummary();
            SetActivity("Disk cleanup analysis complete.");
            ShowDiskCleanupInfo($"Analyzed {items.Count} handler(s).", InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
            ShowDiskCleanupInfo("Disk cleanup analysis cancelled.", InfoBarSeverity.Informational);
            SetActivity("Disk cleanup analysis cancelled.");
        }
        catch (Exception ex)
        {
            ShowDiskCleanupInfo($"Disk cleanup analysis failed: {ex.Message}", InfoBarSeverity.Error);
            SetActivity("Disk cleanup analysis failed.");
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
            ShowDiskCleanupInfo("Finish the current operation before cleaning disk handlers.", InfoBarSeverity.Warning);
            return;
        }

        var targets = _diskCleanupItems
            .Where(item => item.IsSelected && item.CanSelect)
            .Select(item => item.Item)
            .ToList();

        if (targets.Count == 0)
        {
            ShowDiskCleanupInfo("Select at least one category to clean.", InfoBarSeverity.Warning);
            return;
        }

        _diskCleanupCts?.Cancel();
        _diskCleanupCts?.Dispose();
        _diskCleanupCts = new CancellationTokenSource();

        DiskCleanupInfoBar.IsOpen = false;
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;
        SetActivity("Running disk cleanup handlers…");
        SetBusy(true);

        try
        {
            var result = await DiskCleanupManager.CleanAsync(_diskCleanupVolume, targets, _diskCleanupCts.Token);

            var severity = result.HasFailures
                ? InfoBarSeverity.Warning
                : result.Freed > 0
                    ? InfoBarSeverity.Success
                    : InfoBarSeverity.Informational;

            var message = result.SuccessCount > 0
                ? $"Cleaned {result.SuccessCount} handler(s) and freed {FormatBytes(result.Freed)}."
                : "No disk cleanup handlers reported any changes.";

            if (result.HasFailures)
            {
                var details = string.Join(Environment.NewLine, result.Failures.Select(f => $"• {f.Name}: {f.Message}"));
                message += Environment.NewLine + details;
            }

            ShowDiskCleanupInfo(message, severity);
            SetActivity("Disk cleanup completed.");

            var refreshed = await DiskCleanupManager.AnalyzeAsync(_diskCleanupVolume, _diskCleanupCts.Token);
            ApplyDiskCleanupResults(refreshed);
            UpdateDiskCleanupStatusSummary();
        }
        catch (OperationCanceledException)
        {
            ShowDiskCleanupInfo("Disk cleanup cancelled.", InfoBarSeverity.Informational);
            SetActivity("Disk cleanup cancelled.");
        }
        catch (Exception ex)
        {
            ShowDiskCleanupInfo($"Disk cleanup failed: {ex.Message}", InfoBarSeverity.Error);
            SetActivity("Disk cleanup failed.");
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

    private void ApplyDiskCleanupResults(IReadOnlyCollection<DiskCleanupManager.DiskCleanupItem> items)
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
            DiskCleanupStatusText.Text = $"No Disk Cleanup handlers reported data for {_diskCleanupVolume}. Try running as Administrator.";
            return;
        }

        var selectable = _diskCleanupItems.Where(item => item.CanSelect).ToList();
        var totalBytes = selectable.Aggregate(0UL, (current, item) => current + item.Item.Size);

        if (selectable.Count == 0)
        {
            DiskCleanupStatusText.Text = $"No reclaimable space detected on {_diskCleanupVolume}.";
        }
        else
        {
            var label = selectable.Count == 1 ? "category" : "categories";
            DiskCleanupStatusText.Text = $"Potential savings: {FormatBytes(totalBytes)} across {selectable.Count} {label} on {_diskCleanupVolume}.";
        }

        if (_diskCleanupItems.Any(item => item.Item.RequiresElevation))
        {
            DiskCleanupStatusText.Text += " Some handlers require Administrator privileges.";
        }

        if (_diskCleanupItems.Any(item => !string.IsNullOrWhiteSpace(item.ErrorMessage)))
        {
            DiskCleanupStatusText.Text += " Some handlers reported issues.";
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

    private void OnAccentColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (item.Tag is not string tag)
        {
            return;
        }

        if (string.Equals(tag, "default", StringComparison.OrdinalIgnoreCase))
        {
            RestoreAccentColors();
            return;
        }

        if (TryParseColor(tag, out var color))
        {
            ApplyAccentColor(color);
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

    private sealed class DiskCleanupItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public DiskCleanupItemViewModel(DiskCleanupManager.DiskCleanupItem item)
        {
            Item = item;
            if (item.CanSelect && (item.Flags.HasFlag(DiskCleanupManager.DiskCleanupFlags.RunByDefault) ||
                                   item.Flags.HasFlag(DiskCleanupManager.DiskCleanupFlags.EnableByDefault)))
            {
                _isSelected = true;
            }
        }

        internal DiskCleanupManager.DiskCleanupItem Item { get; }

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
