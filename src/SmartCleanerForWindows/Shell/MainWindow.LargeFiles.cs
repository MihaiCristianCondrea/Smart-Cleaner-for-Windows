using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualBasic.FileIO;
using SmartCleanerForWindows.Core.LargeFiles;
using SmartCleanerForWindows.Core.Storage;
using SmartCleanerForWindows.Modules.LargeFiles.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    private async void OnLargeFilesBrowse(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                LargeFilesView.LargeFilesRootPathBox.Text = folder.Path;
                ClearLargeFilesResults();
                SetLargeFilesStatus(
                    Symbol.SaveLocal,
                    Localize("LargeFilesStatusFolderSelectedTitle", "Folder selected"),
                    Localize("LargeFilesStatusFolderSelectedDescription", "Run Scan to find the largest files in this location."));
                SetLargeFilesActivity(Localize("ActivityReadyToScan", "Ready to scan the selected folder."));
            }
        }
        catch (Exception ex)
        {
            ShowLargeFilesInfo(string.Format(
                    CultureInfo.CurrentCulture,
                    Localize("LargeFilesInfoBrowseFailed", "Couldn't select a folder: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    private void OnLargeFilesRootPathChanged(object sender, TextChangedEventArgs e)
    {
        LargeFilesView.LargeFilesInfoBar.IsOpen = false;

        if (_isLargeFilesBusy)
        {
            return;
        }

        ClearLargeFilesResults();
        if (!string.IsNullOrWhiteSpace(LargeFilesView.LargeFilesRootPathBox.Text))
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
        try
        {
            if (_isLargeFilesBusy)
            {
                ShowLargeFilesInfo(Localize("LargeFilesInfoScanInProgress", "Finish the current scan before starting a new one."), InfoBarSeverity.Warning);
                return;
            }

            LargeFilesView.LargeFilesInfoBar.IsOpen = false;

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

            if (_largeFilesCts is { } previousCts)
            {
                try
                {
                    if (!previousCts.IsCancellationRequested)
                    {
                        await previousCts.CancelAsync().ConfigureAwait(true);
                    }
                }
                catch (Exception ex)
                {
                    ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoCancelFailed", "Couldn't cancel the previous scan: {0}"), ex.Message), InfoBarSeverity.Error);
                }
                finally
                {
                    previousCts.Dispose();
                }
            }

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
                var result = await _largeFileExplorer.ScanAsync(root, options, _largeFilesCts.Token).ConfigureAwait(true);

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
        catch (Exception ex)
        {
            ShowLargeFilesInfo(string.Format(CultureInfo.CurrentCulture, Localize("LargeFilesInfoUnexpected", "Unexpected error: {0}"), ex.Message), InfoBarSeverity.Error);
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

            var recycleMode = LargeFilesView.LargeFilesRecycleChk.IsChecked == true
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
        root = LargeFilesView.LargeFilesRootPathBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    private LargeFileScanOptions CreateLargeFileOptions()
    {
        var includeSubfolders = LargeFilesView.LargeFilesIncludeSubfoldersCheck.IsChecked != false;
        var maxItemsValue = LargeFilesView.LargeFilesMaxItemsBox.Value;
        var maxItems = 100;
        if (!double.IsNaN(maxItemsValue))
        {
            maxItems = (int)Math.Max(1, Math.Round(maxItemsValue, MidpointRounding.AwayFromZero));
        }

        return new LargeFileScanOptions
        {
            IncludeSubdirectories = includeSubfolders,
            SkipReparsePoints = true,
            MaxResults = maxItems,
            ExcludedNamePatterns = ParseExclusions(LargeFilesView.LargeFilesExclusionsBox.Text),
            ExcludedFullPaths = _largeFileExclusions.ToList(),
        };
    }

    private void ClearLargeFilesResults()
    {
        _largeFileGroups.Clear();
        LargeFilesView.LargeFilesInfoBar.IsOpen = false;
        SetLargeFilesResultsCaption(Localize("LargeFilesResultsPlaceholder", "Scan results will appear here after you run a scan."));
        LargeFilesView.LargeFilesResultBadge.ClearValue(InfoBadge.ValueProperty);
        LargeFilesView.LargeFilesResultBadge.Visibility = Visibility.Collapsed;
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

            var viewModel = new LargeFileGroupViewModel(group.Name, FormatFileCount, ValueFormatting.FormatBytes);
            foreach (var entry in group.Entries)
            {
                var extensionLabel = string.IsNullOrEmpty(entry.Extension)
                    ? Localize("LargeFilesNoExtensionLabel", "No extension")
                    : entry.Extension.ToUpperInvariant();
                var item = new LargeFileItemViewModel(entry, extensionLabel, ValueFormatting.FormatBytes);
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

        SetLargeFilesResultsCaption(hasFailures
            ? Localize("LargeFilesResultsWithIssues",
                "Some results may be missing due to access issues. Review the largest files below.")
            : Localize("LargeFilesResultsReady", "Review the largest files below before taking action."));
    }

    private void SetLargeFilesResultsCaption(string message) => LargeFilesView.LargeFilesResultsCaption.Text = message;

    private void UpdateLargeFilesSummary()
    {
        var totalCount = _largeFileGroups.Sum(group => group.ItemCount);
        var totalBytes = _largeFileGroups.Aggregate(0L, (current, group) => current + Math.Max(0L, group.TotalBytes));

        if (totalCount == 0)
        {
            LargeFilesView.LargeFilesSummaryText.Text = Localize("LargeFilesSummaryPlaceholder", "No scan results yet.");
            LargeFilesView.LargeFilesResultBadge.ClearValue(InfoBadge.ValueProperty);
            LargeFilesView.LargeFilesResultBadge.Visibility = Visibility.Collapsed;
            return;
        }

        LargeFilesView.LargeFilesSummaryText.Text = string.Format(
            CultureInfo.CurrentCulture,
            Localize("LargeFilesSummaryDetails", "{0} • {1}"),
            FormatFileCount(totalCount),
            ValueFormatting.FormatBytes((ulong)Math.Max(0L, totalBytes)));
        LargeFilesView.LargeFilesResultBadge.Value = totalCount;
        LargeFilesView.LargeFilesResultBadge.Visibility = Visibility.Visible;
    }

    private void SetLargeFilesStatus(Symbol symbol, string title, string description, int? badgeValue = null)
    {
        LargeFilesView.LargeFilesStatusGlyph.Symbol = symbol;
        LargeFilesView.LargeFilesStatusTitle.Text = title;
        LargeFilesView.LargeFilesStatusDescription.Text = description;
        LargeFilesView.LargeFilesStatusHero.Background = GetStatusHeroBrush(symbol);
        LargeFilesView.LargeFilesStatusGlyph.Foreground = GetStatusGlyphBrush(symbol);

        if (badgeValue is > 0)
        {
            LargeFilesView.LargeFilesResultBadge.Value = badgeValue.Value;
            LargeFilesView.LargeFilesResultBadge.Visibility = Visibility.Visible;
        }
    }

    private void SetLargeFilesActivity(string message)
    {
        LargeFilesView.LargeFilesActivityText.Text = message;
    }

    private void SetLargeFilesBusy(bool isBusy)
    {
        _isLargeFilesBusy = isBusy;
        LargeFilesView.LargeFilesProgress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        LargeFilesView.LargeFilesProgress.IsIndeterminate = isBusy;
        LargeFilesView.LargeFilesCancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        LargeFilesView.LargeFilesCancelBtn.IsEnabled = isBusy;
        LargeFilesView.LargeFilesScanBtn.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesBrowseBtn.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesRootPathBox.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesIncludeSubfoldersCheck.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesRecycleChk.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesMaxItemsBox.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesExclusionsBox.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesExclusionsList.IsEnabled = !isBusy;
        LargeFilesView.LargeFilesGroupList.IsEnabled = !isBusy;
        UpdateLargeFilesExclusionState();
    }

    private void ShowLargeFilesInfo(string message, InfoBarSeverity severity)
    {
        LargeFilesView.LargeFilesInfoBar.Message = message;
        LargeFilesView.LargeFilesInfoBar.Severity = severity;
        LargeFilesView.LargeFilesInfoBar.IsOpen = true;
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
            if (!_largeFileExclusionLookup.Add(normalized))
            {
                if (showMessageOnError)
                {
                    ShowLargeFilesInfo(Localize("LargeFilesInfoAlreadyExcluded", "That file is already excluded."), InfoBarSeverity.Informational);
                }

                return false;
            }

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

        var comparer = _largeFileExclusionLookup.Comparer;

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
        LargeFilesView.LargeFilesNoExclusionsText.Visibility = hasExclusions ? Visibility.Collapsed : Visibility.Visible;
        LargeFilesView.LargeFilesClearExclusionsBtn.IsEnabled = hasExclusions && !_isLargeFilesBusy;
    }

    private static string NormalizeLargeFilePath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private string FormatFileCount(int count) => count == 1
        ? LocalizeFormat("LargeFilesSingleFileLabel", "{0} file", count)
        : LocalizeFormat("LargeFilesMultipleFileLabel", "{0} files", count);

    private void RemoveLargeFileItem(LargeFileItemViewModel? item)
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

}

