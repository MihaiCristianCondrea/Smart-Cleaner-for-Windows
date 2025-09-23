using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Core;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private List<string> _previewCandidates = new();
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyleObj) &&
            accentStyleObj is Style accentStyle)
        {
            DeleteBtn.Style = accentStyle;
        }

        SetStatus(Symbol.Folder, "Ready when you are", "Select a folder to begin.");
        SetActivity("Waiting for the next action.");
        UpdateResultsSummary(0, "Preview results will appear here once you run a scan.");

        TryEnableMica();
        TryConfigureAppWindow();
        Activated += OnWindowActivated;
        Closed += OnClosed;
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
            var result = await Task.Run(() => DirectoryCleaner.Clean(root, options, _cts.Token));

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
            var badgeValue = hasResults ? result.EmptyFound : (int?)null;

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
        var pendingBadge = pendingCount > 0 ? pendingCount : (int?)null;
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
            var result = await Task.Run(() => DirectoryCleaner.Clean(root, options, _cts.Token));

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

            var badgeValue = result.DeletedCount > 0 ? result.DeletedCount : (int?)null;
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
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            if (_isBusy)
            {
                SetActivity("Cancelling current operation…");
            }
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
            ResultBadge.Value = null;
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
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        Progress.IsIndeterminate = isBusy;
        CancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        CancelBtn.IsEnabled = isBusy;
        PreviewBtn.IsEnabled = !isBusy;
        DeleteBtn.IsEnabled = !isBusy && _previewCandidates.Count > 0;
        BrowseBtn.IsEnabled = !isBusy;
        RootPathBox.IsEnabled = !isBusy;
        DepthBox.IsEnabled = !isBusy;
        ExcludeBox.IsEnabled = !isBusy;
        RecycleChk.IsEnabled = !isBusy;
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        Info.Message = message;
        Info.Severity = severity;
        Info.IsOpen = true;
    }
}
