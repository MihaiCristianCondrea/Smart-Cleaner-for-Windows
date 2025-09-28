using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Core.DiskCleanup;
using Smart_Cleaner_for_Windows.Core.Storage;
using Smart_Cleaner_for_Windows.Modules.DiskCleanup.ViewModels;

namespace Smart_Cleaner_for_Windows.Shell;

public sealed partial class MainWindow
{
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
                    ValueFormatting.FormatBytes(result.Freed))
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
                ValueFormatting.FormatBytes(totalBytes),
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

}

