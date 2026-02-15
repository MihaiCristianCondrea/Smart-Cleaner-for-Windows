using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartCleanerForWindows.Core.FileSystem;
using SmartCleanerForWindows.Modules.EmptyFolders.ViewModels;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    private async void OnPreview(object sender, RoutedEventArgs e)
    {
        try
        {
            _emptyFolderController.DismissInfo();

            if (!TryGetRootPath(out var root))
            {
                _emptyFolderController.HandleInvalidRoot();
                return;
            }

            _currentPreviewRoot = PathUtilities.NormalizeDirectoryPath(root);

            CancelActiveOperation();
            _cts = new CancellationTokenSource();

            try
            {
                ResetResultFilters();
                var options = CreateOptions(dryRun: true);
                await _emptyFolderController.PreviewAsync(root, options, _cts.Token).ConfigureAwait(true);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
        catch (Exception ex)
        {
            ShowInfo(string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Localize("InfoPreviewUnexpectedError", "Preview failed: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        try
        {
            _emptyFolderController.DismissInfo();

            if (!TryGetRootPath(out var root))
            {
                _emptyFolderController.HandleInvalidRoot();
                return;
            }

            _currentPreviewRoot = PathUtilities.NormalizeDirectoryPath(root);

            CancelActiveOperation();
            _cts = new CancellationTokenSource();

            try
            {
                var options = CreateOptions(dryRun: false);
                await _emptyFolderController.CleanupAsync(root, options, _previewCandidates.Count, _cts.Token)
                    .ConfigureAwait(true);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
        catch (Exception ex)
        {
            ShowInfo(string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Localize("InfoCleanupUnexpectedError", "Cleanup failed: {0}"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    public void DismissInfo()
    {
        EmptyFoldersView.Info.IsOpen = false;
    }

    public void ShowInvalidRootSelection()
    {
        ShowInfo(Localize("InfoSelectValidFolder", "Select a valid folder."), InfoBarSeverity.Warning);
        SetStatus(
            Symbol.Important,
            Localize("StatusSelectValidFolderTitle", "Select a valid folder"),
            Localize("StatusSelectValidFolderDescription", "Choose a folder before scanning."));
        UpdateResultsSummary(0, Localize("ResultsNeedValidFolder", "Select a valid folder to run a scan."));
        SetActivity(Localize("ActivityWaitingForValidFolder", "Waiting for a valid folder."));
    }

    public void PreparePreview()
    {
        ResetResultFilters();
        ClearPreviewTree();
        SetBusy(true);
        SetActivity(Localize("ActivityScanning", "Scanning for empty folders…"));
        SetStatus(
            Symbol.Sync,
            Localize("StatusScanningTitle", "Scanning in progress…"),
            Localize("StatusScanningDescription", "Looking for empty folders. You can cancel the scan if needed."));
        UpdateResultsSummary(0, Localize("ResultsScanning", "Scanning for empty folders…"));
    }

    public void ShowPreviewResult(DirectoryCleanResult result)
    {
        _totalPreviewCount = result.EmptyFound;

        ClearPreviewTree();
        BuildPreviewTree(result.EmptyDirectories);
        SortPreviewTree();
        var visibleIncludedCount = ApplyPreviewFilters();

        var hasResults = result.EmptyFound > 0;
        var resultsMessage = result.HasFailures
            ? Localize("ResultsMissingDueToAccess", "Some folders might be missing from the preview due to access issues.")
            : hasResults
                ? Localize("ResultsReadyToReview", "Review the folders below before cleaning.")
                : Localize("ResultsNoneDetected", "No empty folders were detected for this location.");

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

        if (!string.IsNullOrWhiteSpace(resultsMessage))
        {
            message += Environment.NewLine + resultsMessage;
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
        int? badgeValue = visibleIncludedCount > 0 ? visibleIncludedCount : null;

        SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
        SetActivity(Localize("ActivityScanComplete", "Scan complete."));

        ShowInfo(message, severity);
    }

    public void ShowPreviewCancelled()
    {
        SetActivity(Localize("ActivityScanCancelled", "Scan cancelled."));
        SetStatus(
            Symbol.Cancel,
            Localize("StatusScanCancelledTitle", "Scan cancelled"),
            Localize("StatusScanCancelledDescription", "Preview was cancelled. Adjust settings or try again."));
        UpdateResultsSummary(
            CountVisibleNodes(_filteredEmptyFolderRoots, includeExcluded: false),
            Localize("ResultsScanCancelled", "Preview was cancelled. Run Preview to refresh the list."),
            _totalPreviewCount == 0 ? null : _totalPreviewCount);
        ShowInfo(Localize("InfoPreviewCancelled", "Preview cancelled."), InfoBarSeverity.Informational);
    }

    public void ShowPreviewError(Exception exception)
    {
        SetActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
        SetStatus(
            Symbol.Important,
            Localize("StatusScanFailedTitle", "Scan failed"),
            Localize("StatusScanFailedDescription", "An unexpected error occurred. Review the message below."));
        UpdateResultsSummary(0, Localize("ResultsScanFailed", "The scan failed. Review the message above and try again."));
        ShowInfo($"Error: {exception.Message}", InfoBarSeverity.Error);
    }

    public void PrepareCleanup(int pendingCount)
    {
        SetBusy(true);
        SetActivity(Localize("ActivityCleaning", "Cleaning empty folders…"));
        int? pendingBadge = pendingCount > 0 ? pendingCount : null;
        SetStatus(
            Symbol.Delete,
            Localize("StatusCleaningTitle", "Cleaning in progress…"),
            Localize("StatusCleaningDescription", "Removing empty folders safely. You can cancel the operation if needed."),
            pendingBadge);
        UpdateResultsSummary(
            pendingCount,
            pendingCount > 0
                ? Localize("ResultsCleaningProgressWithPreview", "Cleaning in progress. We'll refresh the preview afterwards.")
                : Localize("ResultsCleaningProgress", "Cleaning in progress…"),
            _totalPreviewCount == 0 ? null : _totalPreviewCount);
    }

    public void ShowCleanupResult(DirectoryCleanResult result)
    {
        ClearPreviewTree();

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
                ? Localize("StatusCleanNoActionDescription", "No action required. Adjust filters or try another folder.")
                : result.EmptyFound > result.DeletedCount
                    ? Localize("StatusCleanPartialDescription", "Some folders were kept due to issues. Review the details below.")
                    : Localize("StatusCleanSuccessDescription", "Empty folders removed successfully.");

        SetStatus(statusSymbol, statusTitle, statusDescription, badgeValue);
        SetActivity(Localize("ActivityCleanupComplete", "Cleanup complete."));
        UpdateResultsSummary(result.EmptyFound, Localize("ResultsAfterCleanup", "Run Preview to refresh the list."));
        ShowInfo(message, severity);
    }

    public void ShowCleanupCancelled()
    {
        SetActivity(Localize("ActivityCleanCancelled", "Clean cancelled."));
        SetStatus(
            Symbol.Cancel,
            Localize("StatusCleanCancelledTitle", "Clean cancelled"),
            Localize("StatusCleanCancelledDescription", "Cleaning was cancelled. Preview again to refresh the list."));
        UpdateResultsSummary(CountVisibleNodes(_filteredEmptyFolderRoots, includeExcluded: false), Localize("ResultsCleanCancelled", "Cleaning cancelled. Run Preview to review folders."), _totalPreviewCount == 0 ? null : _totalPreviewCount);
        ShowInfo(Localize("InfoCleanCancelled", "Cleaning cancelled."), InfoBarSeverity.Informational);
    }

    public void ShowCleanupError(Exception exception)
    {
        if (exception is UnauthorizedAccessException)
        {
            SetActivity(Localize("ActivityPermissionRequired", "Permission required."));
            SetStatus(
                Symbol.Important,
                Localize("StatusAccessDeniedTitle", "Access denied"),
                Localize("StatusAccessDeniedDescription", "Run the app as Administrator to remove protected folders."));
            UpdateResultsSummary(0, Localize("ResultsAccessDenied", "Some folders could not be removed due to permissions."));
            ShowInfo(Localize("InfoAccessDenied", "Access denied. Try running as Administrator."), InfoBarSeverity.Warning);
            return;
        }

        SetActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
        SetStatus(
            Symbol.Important,
            Localize("StatusCleanFailedTitle", "Clean failed"),
            Localize("StatusCleanFailedDescription", "An unexpected error occurred. Review the message below."));
        UpdateResultsSummary(0, Localize("ResultsCleanFailed", "Cleaning failed. Review the message above and try again."));
        ShowInfo($"Error: {exception.Message}", InfoBarSeverity.Error);
    }

    public void CompleteOperation()
    {
        SetBusy(false);
    }

    private DirectoryCleanOptions CreateOptions(bool dryRun)
    {
        var depthValue = EmptyFoldersView.DepthBox.Value;
        int? maxDepth = null;
        if (double.IsNaN(depthValue))
            return new DirectoryCleanOptions
            {
                DryRun = dryRun,
                SendToRecycleBin = EmptyFoldersView.RecycleChk.IsChecked == true,
                SkipReparsePoints = true,
                DeleteRootWhenEmpty = false,
                MaxDepth = maxDepth,
                ExcludedNamePatterns = ParseExclusions(EmptyFoldersView.ExcludeBox.Text),
                ExcludedFullPaths = _inlineExcludedPaths.ToArray(),
            };
        var depth = (int)Math.Max(0, Math.Round(depthValue, MidpointRounding.AwayFromZero));
        if (depth > 0)
        {
            maxDepth = depth;
        }

        return new DirectoryCleanOptions
        {
            DryRun = dryRun,
            SendToRecycleBin = EmptyFoldersView.RecycleChk.IsChecked == true,
            SkipReparsePoints = true,
            DeleteRootWhenEmpty = false,
            MaxDepth = maxDepth,
            ExcludedNamePatterns = ParseExclusions(EmptyFoldersView.ExcludeBox.Text),
            ExcludedFullPaths = _inlineExcludedPaths.ToArray(),
        };
    }

    private bool TryGetRootPath(out string root)
    {
        root = EmptyFoldersView.RootPathBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    private static IReadOnlyCollection<string> ParseExclusions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

        if (_internetRepairCts is { IsCancellationRequested: false })
        {
            _internetRepairCts.Cancel();
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

        if (cancelled && _isInternetRepairBusy)
        {
            SetInternetRepairActivity(Localize("ActivityCancelling", "Cancelling current operation…"));
        }
    }

    private void ResetResultFilters()
    {
        var view = EmptyFoldersView;
        _currentResultSearch = string.Empty;
        view.ResultsSearchBox.Text = string.Empty;

        _hideExcludedResults = false;
        if (view.HideExcludedToggle.IsOn)
        {
            view.HideExcludedToggle.IsOn = false;
        }

        _currentResultSort = EmptyFolderSortOption.NameAscending;
        if (view.ResultsSortBox.SelectedIndex != 0)
        {
            view.ResultsSortBox.SelectedIndex = 0;
        }

        UpdateResultFilterControls();
    }

    private void ClearPreviewTree()
    {
        var view = EmptyFoldersView;
        foreach (var node in EnumeratePreviewNodes())
        {
            node.ExclusionChanged -= OnInlineExclusionChanged;
        }

        _emptyFolderRoots.Clear();
        _filteredEmptyFolderRoots.Clear();
        _emptyFolderLookup.Clear();
        _previewCandidates.Clear();
        _inlineExcludedPaths.Clear();
        _totalPreviewCount = 0;
        view.CandidatesTree.SelectedItems?.Clear();
        UpdateResultBadgeValue(0); // FIXME: Cannot resolve symbol 'UpdateResultBadgeValue'
        UpdateInlineExclusionSummary();
        UpdateResultFilterControls();
        UpdateResultsActionState();
    }

    private void BuildPreviewTree(IReadOnlyList<string> directories)
    {
        foreach (var directory in directories)
        {
            var normalized = PathUtilities.NormalizeDirectoryPath(directory);
            EnsureNodeForPath(normalized);
        }
    }

    private EmptyFolderNode EnsureNodeForPath(string fullPath) // FIXME: Method 'EnsureNodeForPath' return value is never used
    {
        if (_emptyFolderLookup.TryGetValue(fullPath, out var existing))
        {
            return existing;
        }

        var relative = string.IsNullOrEmpty(_currentPreviewRoot)
            ? fullPath
            : Path.GetRelativePath(_currentPreviewRoot, fullPath);

        if (relative == "." || string.IsNullOrEmpty(relative))
        {
            var rootName = GetNodeName(fullPath);
            var rootNode = new EmptyFolderNode(rootName, fullPath, rootName, depth: 0);
            RegisterNode(rootNode, parent: null);
            return rootNode;
        }

        var segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var currentPath = _currentPreviewRoot;
        EmptyFolderNode? parent = null;
        EmptyFolderNode? lastNode = null;
        var depth = 0;

        foreach (var segment in segments)
        {
            depth++;
            currentPath = string.IsNullOrEmpty(currentPath)
                ? PathUtilities.NormalizeDirectoryPath(segment)
                : PathUtilities.NormalizeDirectoryPath(Path.Combine(currentPath, segment));

            if (_emptyFolderLookup.TryGetValue(currentPath, out var node))
            {
                parent = node;
                lastNode = node;
                continue;
            }

            var relativePath = string.IsNullOrEmpty(_currentPreviewRoot)
                ? currentPath
                : Path.GetRelativePath(_currentPreviewRoot, currentPath);
            relativePath = NormalizeRelativeDisplayPath(relativePath, currentPath);

            node = new EmptyFolderNode(segment, currentPath, relativePath, depth);
            RegisterNode(node, parent);
            parent = node;
            lastNode = node;
        }

        return lastNode!;
    }

    private void RegisterNode(EmptyFolderNode node, EmptyFolderNode? parent)
    {
        _emptyFolderLookup[node.FullPath] = node;
        node.IsVisible = true;
        node.IsSearchMatch = false;
        node.ExclusionChanged += OnInlineExclusionChanged;

        if (parent is null)
        {
            _emptyFolderRoots.Add(node);
        }
        else
        {
            parent.AddChild(node);
        }
    }

    private void SortPreviewTree()
    {
        if (_emptyFolderRoots.Count == 0)
        {
            return;
        }

        Comparison<EmptyFolderNode> comparison = _currentResultSort switch
        {
            EmptyFolderSortOption.NameDescending => (a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(b.Name, a.Name),
            EmptyFolderSortOption.DepthDescending => (a, b) =>
            {
                var lengthComparison = b.RelativePath.Length.CompareTo(a.RelativePath.Length);
                return lengthComparison != 0
                    ? lengthComparison
                    : StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
            },
            _ => (a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name),
        };

        foreach (var root in _emptyFolderRoots)
        {
            SortNode(root, comparison);
        }
    }

    private void SortNode(EmptyFolderNode node, Comparison<EmptyFolderNode> comparison)
    {
        foreach (var child in node.AllChildren)
        {
            SortNode(child, comparison);
        }

        node.SortChildren(comparison);
    }

    private int ApplyPreviewFilters()
    {
        _filteredEmptyFolderRoots.Clear();

        foreach (var root in _emptyFolderRoots)
        {
            if (ApplyPreviewFiltersRecursive(root))
            {
                _filteredEmptyFolderRoots.Add(root);
            }
        }

        var visibleIncludedCount = CountVisibleNodes(_filteredEmptyFolderRoots, includeExcluded: false);
        UpdateResultsSummary(visibleIncludedCount, null, _totalPreviewCount == 0 ? null : _totalPreviewCount);
        UpdateResultBadgeValue(visibleIncludedCount); // FIXME: Cannot resolve symbol 'UpdateResultBadgeValue'
        UpdatePreviewCandidatesFromTree();
        UpdateInlineExclusionSummary();
        UpdateResultFilterControls();
        UpdateResultsActionState();
        return visibleIncludedCount;
    }

    private bool ApplyPreviewFiltersRecursive(EmptyFolderNode node)
    {
        var visibleChildren = new List<EmptyFolderNode>();
        foreach (var child in node.AllChildren)
        {
            if (ApplyPreviewFiltersRecursive(child))
            {
                visibleChildren.Add(child);
            }
        }

        var hasSearch = !string.IsNullOrWhiteSpace(_currentResultSearch);
        var matchesSearch = !hasSearch || node.FullPath.Contains(_currentResultSearch, StringComparison.CurrentCultureIgnoreCase);
        node.IsSearchMatch = hasSearch && matchesSearch;

        var include = matchesSearch || visibleChildren.Count > 0;
        if (_hideExcludedResults && node.IsEffectivelyExcluded)
        {
            include = false;
        }

        node.UpdateVisibleChildren(visibleChildren);
        node.IsVisible = include;
        return include;
    }

    private static int CountVisibleNodes(IEnumerable<EmptyFolderNode> nodes, bool includeExcluded)
    {
        var total = 0;
        foreach (var node in nodes)
        {
            if (!node.IsVisible)
            {
                continue;
            }

            if (includeExcluded || !node.IsEffectivelyExcluded)
            {
                total++;
            }

            total += CountVisibleNodes(node.Children, includeExcluded);
        }

        return total;
    }

    private void UpdatePreviewCandidatesFromTree()
    {
        _previewCandidates.Clear();

        foreach (var node in EnumeratePreviewNodes())
        {
            if (!node.IsEffectivelyExcluded)
            {
                _previewCandidates.Add(node.FullPath);
            }
        }
    }

    private IEnumerable<EmptyFolderNode> EnumeratePreviewNodes()
    {
        foreach (var root in _emptyFolderRoots)
        {
            foreach (var node in root.EnumerateSelfAndDescendants())
            {
                yield return node;
            }
        }
    }

    private void UpdateInlineExclusionSummary()
    {
        var view = EmptyFoldersView;
        view.InlineExclusionSummary.Text = _inlineExcludedPaths.Count > 0
            ? LocalizeFormat("InlineExclusionSummaryCount", "Inline exclusions: {0} folder(s).", _inlineExcludedPaths.Count)
            : Localize("InlineExclusionSummaryNone", "No inline exclusions applied.");
    }

    private void UpdateResultFilterControls()
    {
        var hasFilters = HasActiveFilters();
        var view = EmptyFoldersView;
        view.ResultsSearchBox.IsEnabled = !_isBusy;
        view.ResultsSortBox.IsEnabled = !_isBusy;
        view.HideExcludedToggle.IsEnabled = !_isBusy;
        view.ClearFiltersBtn.IsEnabled = !_isBusy && hasFilters;
        view.CandidatesTree.IsEnabled = !_isBusy;
    }

    private static string GetNodeName(string fullPath)
    {
        var name = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(name))
        {
            name = fullPath;
        }

        return name;
    }

    private static string NormalizeRelativeDisplayPath(string relative, string fullPath)
    {
        if (string.IsNullOrEmpty(relative) || relative == ".")
        {
            return GetNodeName(fullPath);
        }

        return relative.Replace('/', Path.DirectorySeparatorChar);
    }

    private void RefreshPreviewTree()
    {
        SortPreviewTree();
        ApplyPreviewFilters();
    }

    private void OnResultSearchChanged(object sender, TextChangedEventArgs e)
    {
        _currentResultSearch = EmptyFoldersView.ResultsSearchBox.Text?.Trim() ?? string.Empty;
        RefreshPreviewTree();
    }

    private void OnResultSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EmptyFoldersView.ResultsSortBox.SelectedItem is not ComboBoxItem { Tag: string tag }) return;
        _currentResultSort = tag switch
        {
            "NameDescending" => EmptyFolderSortOption.NameDescending,
            "DepthDescending" => EmptyFolderSortOption.DepthDescending,
            _ => EmptyFolderSortOption.NameAscending,
        };

        RefreshPreviewTree();
    }

    private void OnHideExcludedToggled(object sender, RoutedEventArgs e)
    {
        _hideExcludedResults = EmptyFoldersView.HideExcludedToggle.IsOn;
        UpdateResultFilterControls();
        RefreshPreviewTree();
    }

    private void OnClearResultFilters(object sender, RoutedEventArgs e)
    {
        var previousSearch = _currentResultSearch; // FIXME: Local variable 'previousSearch' is never used
        var previousHide = _hideExcludedResults;

        EmptyFoldersView.ResultsSearchBox.Text = string.Empty;
        _currentResultSearch = string.Empty;

        if (EmptyFoldersView.HideExcludedToggle.IsOn)
        {
            EmptyFoldersView.HideExcludedToggle.IsOn = false;
        }
        else if (previousHide)
        {
            _hideExcludedResults = false;
        }

        if (!previousHide)
        {
            RefreshPreviewTree();
        }
    }

    private void OnExcludeSelected(object sender, RoutedEventArgs e)
    {
        foreach (var node in GetSelectedNodes().Where(n => n is { IsInlineToggleEnabled: true, IsDirectlyExcluded: false }))
        {
            node.IsDirectlyExcluded = true;
        }
    }

    private void OnIncludeSelected(object sender, RoutedEventArgs e)
    {
        foreach (var node in GetSelectedNodes().Where(n => n.IsDirectlyExcluded))
        {
            node.IsDirectlyExcluded = false;
        }
    }

    private void OnClearInlineExclusions(object sender, RoutedEventArgs e)
    {
        foreach (var node in EnumeratePreviewNodes().Where(n => n.IsDirectlyExcluded))
        {
            node.IsDirectlyExcluded = false;
        }
    }

    private void OnCandidatesSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        UpdateResultsActionState();
    }

    private IEnumerable<EmptyFolderNode> GetSelectedNodes()
    {
        if (EmptyFoldersView.CandidatesTree.SelectedItems is null)
        {
            return [];
        }

        return EmptyFoldersView.CandidatesTree.SelectedItems.OfType<EmptyFolderNode>();
    }

    private void OnInlineExclusionChanged(object? sender, EventArgs e)
    {
        if (sender is not EmptyFolderNode node)
        {
            return;
        }

        if (node.IsDirectlyExcluded)
        {
            _inlineExcludedPaths.Add(node.FullPath);
        }
        else
        {
            _inlineExcludedPaths.Remove(node.FullPath);
        }

        RefreshPreviewTree();
    }

    private void UpdateResultsActionState()
    {
        var isReady = !_isBusy;
        var selectedNodes = GetSelectedNodes().ToList();
        var canExclude = isReady && selectedNodes.Any(n => n is { IsInlineToggleEnabled: true, IsDirectlyExcluded: false });
        var canInclude = isReady && selectedNodes.Any(n => n.IsDirectlyExcluded);

        EmptyFoldersView.ExcludeSelectedBtn.IsEnabled = canExclude;
        EmptyFoldersView.IncludeSelectedBtn.IsEnabled = canInclude;
        EmptyFoldersView.ClearInlineExclusionsBtn.IsEnabled = isReady && _inlineExcludedPaths.Count > 0;
        EmptyFoldersView.DeleteBtn.IsEnabled = isReady && _previewCandidates.Count > 0;
    }

    private bool HasActiveFilters()
    {
        return !string.IsNullOrWhiteSpace(_currentResultSearch) || _hideExcludedResults;
    }
}
