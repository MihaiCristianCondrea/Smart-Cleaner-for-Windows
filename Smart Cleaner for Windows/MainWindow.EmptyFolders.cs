using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Core;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow
{
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

}

