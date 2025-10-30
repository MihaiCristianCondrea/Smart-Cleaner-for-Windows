using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Core.DiskCleanup;
using Smart_Cleaner_for_Windows.Core.Storage;
using Smart_Cleaner_for_Windows.Modules.DiskCleanup.ViewModels;

namespace Smart_Cleaner_for_Windows.Shell;

public sealed partial class MainWindow
{
    private void OnDiskCleanupAnalyze(object sender, RoutedEventArgs e)
    {
        _ = RunDiskCleanupAnalyzeAsync()
            .ContinueWith(t => ReportAsyncHandlerException(t.Exception),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task RunDiskCleanupAnalyzeAsync()
    {
        if (_isBusy)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoFinishAnalysis", "Finish the current operation before analyzing disk cleanup."),
                InfoBarSeverity.Warning);
            return;
        }

        var previousCts = _diskCleanupCts;
        if (previousCts is not null)
        {
            if (ReferenceEquals(_diskCleanupCts, previousCts))
            {
                _diskCleanupCts = null;
            }

            await CancelAndDisposeAsync(previousCts);
        }

        var cts = new CancellationTokenSource();
        _diskCleanupCts = cts;

        DiskCleanupInfoBar.IsOpen = false;/*FIXME: Cannot resolve symbol 'DiskCleanupInfoBar'*/
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;/*FIXME: Cannot resolve symbol 'DiskCleanupProgress'*/
        SetActivity(Localize("ActivityDiskCleanupAnalyzing", "Analyzing disk cleanup handlers…"));
        SetBusy(true);

        try
        {
            var items = await _diskCleanupService.AnalyzeAsync(_diskCleanupVolume, cts.Token);
            ApplyDiskCleanupResults(items);
            UpdateDiskCleanupStatusSummary();
            SetActivity(Localize("ActivityDiskCleanupAnalysisComplete", "Disk cleanup analysis complete."));
            ShowDiskCleanupInfo(
                LocalizeFormat("InfoDiskCleanupAnalyzed", "Analyzed {0} handler(s).", items.Count),
                InfoBarSeverity.Success);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
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
            DiskCleanupProgress.Visibility = Visibility.Collapsed;/*FIXME: Cannot resolve symbol 'DiskCleanupProgress'*/
            SetBusy(false);
            if (ReferenceEquals(_diskCleanupCts, cts))
            {
                _diskCleanupCts = null;
            }

            cts.Dispose();
            UpdateDiskCleanupActionState();
        }
    }

    private void OnDiskCleanupClean(object sender, RoutedEventArgs e)
    {
        _ = RunDiskCleanupCleanAsync()
            .ContinueWith(t => ReportAsyncHandlerException(t.Exception),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task RunDiskCleanupCleanAsync()
    {
        if (_isBusy)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoFinishCleaning", "Finish the current operation before cleaning disk handlers."),
                InfoBarSeverity.Warning);
            return;
        }

        var targets = _diskCleanupItems
            .Where(item => item is { IsSelected: true, CanSelect: true })
            .Select(item => item.Item)
            .ToList();

        if (targets.Count == 0)
        {
            ShowDiskCleanupInfo(
                Localize("DiskCleanupInfoSelectCategory", "Select at least one category to clean."),
                InfoBarSeverity.Warning);
            return;
        }

        var previousCts = _diskCleanupCts;
        if (previousCts is not null)
        {
            if (ReferenceEquals(_diskCleanupCts, previousCts))
            {
                _diskCleanupCts = null;
            }

            await CancelAndDisposeAsync(previousCts);
        }

        var cts = new CancellationTokenSource();
        _diskCleanupCts = cts;

        DiskCleanupInfoBar.IsOpen = false;/*FIXME: Cannot resolve symbol 'DiskCleanupInfoBar'*/
        _isDiskCleanupOperation = true;
        DiskCleanupProgress.Visibility = Visibility.Visible;/*FIXME: Cannot resolve symbol 'DiskCleanupProgress'*/
        SetActivity(Localize("ActivityDiskCleanupRunning", "Running disk cleanup handlers…"));
        SetBusy(true);

        try
        {
            var result = await _diskCleanupService.CleanAsync(_diskCleanupVolume, targets, cts.Token);

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
                    ValueFormatting.FormatBytes(result.Freed))
                : Localize("InfoDiskCleanupNoChanges", "No disk cleanup handlers reported any changes.");

            if (result.HasFailures)
            {
                var details = string.Join(Environment.NewLine, result.Failures.Select(f => $"• {f.Name}: {f.Message}"));
                message += Environment.NewLine + details;
            }

            ShowDiskCleanupInfo(message, severity);
            SetActivity(Localize("ActivityDiskCleanupCompleted", "Disk cleanup completed."));

            var refreshed = await _diskCleanupService.AnalyzeAsync(_diskCleanupVolume, cts.Token);
            ApplyDiskCleanupResults(refreshed);
            UpdateDiskCleanupStatusSummary();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
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
            DiskCleanupProgress.Visibility = Visibility.Collapsed;/*FIXME:Cannot resolve symbol 'DiskCleanupProgress'*/
            SetBusy(false);
            if (ReferenceEquals(_diskCleanupCts, cts))
            {
                _diskCleanupCts = null;
            }

            cts.Dispose();
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
        DiskCleanupAnalyzeBtn.IsEnabled = !_isBusy;/*FIXME:Cannot resolve symbol 'DiskCleanupAnalyzeBtn'*/
        DiskCleanupList.IsEnabled = canInteract;/*FIXME:Cannot resolve symbol 'DiskCleanupList'*/
        var hasSelection = canInteract && _diskCleanupItems.Any(item => item is { IsSelected: true, CanSelect: true });
        DiskCleanupCleanBtn.IsEnabled = hasSelection;/*FIXME:Cannot resolve symbol 'DiskCleanupCleanBtn'*/
    }

    private void UpdateDiskCleanupStatusSummary()
    {
        if (_diskCleanupItems.Count == 0)
        {
            DiskCleanupStatusText.Text = LocalizeFormat(/*FIXME:Cannot resolve symbol 'DiskCleanupStatusText'*/
                "DiskCleanupStatusNoData",
                "No Disk Cleanup handlers reported data for {0}. Try running as Administrator.",
                _diskCleanupVolume);
            return;
        }

        var selectable = _diskCleanupItems.Where(item => item.CanSelect).ToList();
        var totalBytes = selectable.Aggregate(0UL, (current, item) => current + item.Item.Size);

        if (selectable.Count == 0)
        {
            DiskCleanupStatusText.Text = LocalizeFormat(/*FIXME:Cannot resolve symbol 'DiskCleanupStatusText'*/
                "DiskCleanupStatusNoSpace",
                "No reclaimable space detected on {0}.",
                _diskCleanupVolume);
        }
        else
        {
            var label = selectable.Count == 1
                ? Localize("DiskCleanupCategorySingular", "category")
                : Localize("DiskCleanupCategoryPlural", "categories");
            DiskCleanupStatusText.Text = string.Format(/*FIXME:Cannot resolve symbol 'DiskCleanupStatusText'*/
                CultureInfo.CurrentCulture,
                Localize(
                    "DiskCleanupStatusPotential",
                    "Potential savings: {0} across {1} {2} on {3}."),
                ValueFormatting.FormatBytes(totalBytes),
                selectable.Count,
                label,
                _diskCleanupVolume);
        }

        if (_diskCleanupItems.Any(item => item.Item.RequiresElevation))
        {
            DiskCleanupStatusText.Text += " " + Localize(/*FIXME:Cannot resolve symbol 'DiskCleanupStatusText'*/
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

    private void ReportAsyncHandlerException(Exception? ex)
    {
        if (ex is null) return;

        var message = ex.InnerException?.Message ?? ex.Message;
        ShowDiskCleanupInfo(
            string.Format(
                CultureInfo.CurrentCulture,
                Localize("InfoUnexpectedAsyncError", "Unexpected error: {0}"),
                message),
            InfoBarSeverity.Error);
    }
}
