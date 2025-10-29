using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Core.Networking;
using Smart_Cleaner_for_Windows.Modules.InternetRepair.ViewModels;

namespace Smart_Cleaner_for_Windows.Shell;

public sealed partial class MainWindow
{
    private void InitializeInternetRepair()
    {
        if (InternetRepairLogList is not null)
        {
            InternetRepairLogList.ItemsSource = _internetRepairLog;
        }

        _internetRepairLog.CollectionChanged += OnInternetRepairLogChanged;
        UpdateInternetRepairLogPlaceholder();

        _internetRepairActions.Clear();
        foreach (var action in CreateDefaultInternetRepairActions())
        {
            _internetRepairActions[action.Id] = action;
        }

        SetInternetRepairStatus(
            Symbol.World,
            Localize("InternetRepairStatusReadyTitle", "Ready to fix common issues"),
            Localize("InternetRepairStatusReadyDescription", "Select the repairs you want to run, then choose Fix now."));
        SetInternetRepairActivity(Localize("InternetRepairActivityIdle", "Waiting to start a repair."));
        InternetRepairSummaryText.Text = Localize("InternetRepairSummaryReady", "Select the repairs you want to run.");
        InternetRepairProgress.IsIndeterminate = false;
        InternetRepairProgress.Value = 0;
        InternetRepairCancelBtn.Visibility = Visibility.Collapsed;
        InternetRepairCancelBtn.IsEnabled = false;
        DismissInternetRepairInfo();
        UpdateInternetRepairSelectionState();
    }

    private static IReadOnlyList<InternetRepairAction> CreateDefaultInternetRepairActions() =>
        new List<InternetRepairAction>
        {
            new(
                "winsock",
                "Reset Winsock catalog",
                "Repairs socket corruption that can break connectivity.",
                "netsh",
                "winsock reset",
                requiresElevation: true,
                successMessage: "Winsock catalog reset."),
            new(
                "ipreset",
                "Reset TCP/IP stack",
                "Rebuilds the IP stack and reverts advanced network tweaks.",
                "netsh",
                "int ip reset",
                requiresElevation: true,
                successMessage: "TCP/IP stack reset."),
            new(
                "dnsflush",
                "Flush DNS cache",
                "Clears cached DNS records to resolve name lookup issues.",
                "ipconfig",
                "/flushdns",
                requiresElevation: false,
                successMessage: "DNS cache cleared."),
            new(
                "iprelease",
                "Release IP address",
                "Drops the current lease so you can request a fresh address.",
                "ipconfig",
                "/release",
                requiresElevation: true,
                successMessage: "IP address released."),
            new(
                "iprenew",
                "Renew IP address",
                "Requests a new DHCP lease from the network.",
                "ipconfig",
                "/renew",
                requiresElevation: true,
                successMessage: "IP address renewed."),
            new(
                "proxy",
                "Reset WinHTTP proxy",
                "Clears system proxy settings that might block traffic.",
                "netsh",
                "winhttp reset proxy",
                requiresElevation: true,
                successMessage: "Proxy settings reset.")
        };

    private void OnInternetRepairLogChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateInternetRepairLogPlaceholder();

    private void UpdateInternetRepairLogPlaceholder()
    {
        if (InternetRepairLogPlaceholder is null)
        {
            return;
        }

        InternetRepairLogPlaceholder.Visibility = _internetRepairLog.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private IEnumerable<CheckBox> EnumerateInternetRepairActionCheckBoxes()
    {
        yield return InternetRepairWinsockChk;
        yield return InternetRepairTcpIpChk;
        yield return InternetRepairDnsChk;
        yield return InternetRepairReleaseChk;
        yield return InternetRepairRenewChk;
        yield return InternetRepairProxyChk;
    }

    private void OnInternetRepairActionSelectionChanged(object sender, RoutedEventArgs e) => UpdateInternetRepairSelectionState();

    private void UpdateInternetRepairSelectionState()
    {
        var count = EnumerateInternetRepairActionCheckBoxes().Count(cb => cb.IsChecked == true);
        if (count > 0)
        {
            InternetRepairResultBadge.Value = count;
            InternetRepairResultBadge.Visibility = Visibility.Visible;
            InternetRepairSummaryText.Text = LocalizeFormat(
                "InternetRepairSummarySelection",
                "Ready to run {0} selected repair(s).",
                count);
        }
        else
        {
            InternetRepairResultBadge.ClearValue(InfoBadge.ValueProperty);
            InternetRepairResultBadge.Visibility = Visibility.Collapsed;
            InternetRepairSummaryText.Text = Localize("InternetRepairSummaryReady", "Select the repairs you want to run.");
        }

        if (!_isInternetRepairBusy)
        {
            InternetRepairRunBtn.IsEnabled = count > 0;
        }
    }

    private IReadOnlyList<InternetRepairAction> GetSelectedInternetRepairActions()
    {
        var selected = new List<InternetRepairAction>();
        foreach (var check in EnumerateInternetRepairActionCheckBoxes())
        {
            if (check.IsChecked == true && check.Tag is string id && _internetRepairActions.TryGetValue(id, out var action))
            {
                selected.Add(action);
            }
        }

        return selected;
    }

    private async void OnInternetRepairRun(object sender, RoutedEventArgs e)
    {
        DismissInternetRepairInfo();

        var actions = GetSelectedInternetRepairActions();
        if (actions.Count == 0)
        {
            ShowInternetRepairInfo(
                Localize("InternetRepairInfoNoSelection", "Select at least one repair before running."),
                InfoBarSeverity.Warning);
            return;
        }

        if (_internetRepairCts is { IsCancellationRequested: false })
        {
            _internetRepairCts.Cancel();
        }

        _internetRepairCts = new CancellationTokenSource();
        _internetRepairLogLookup.Clear();
        _internetRepairLog.Clear();
        InternetRepairProgress.Value = 0;
        InternetRepairProgress.IsIndeterminate = true;

        SetInternetRepairBusy(true);
        SetInternetRepairStatus(
            Symbol.Sync,
            Localize("InternetRepairStatusRunningTitle", "Applying fixes…"),
            Localize("InternetRepairStatusRunningDescription", "Hang tight while we apply the selected network repairs."));
        SetInternetRepairActivity(Localize("InternetRepairActivityRunning", "Running network repair actions…"));
        InternetRepairSummaryText.Text = LocalizeFormat(
            "InternetRepairSummaryRunning",
            "Running {0} repair action(s)…",
            actions.Count);

        try
        {
            var progress = new Progress<InternetRepairStepUpdate>(OnInternetRepairProgress);
            var result = await _internetRepairService.RunAsync(actions, progress, _internetRepairCts.Token);
            HandleInternetRepairCompletion(result);
        }
        catch (OperationCanceledException)
        {
            HandleInternetRepairCancelled();
        }
        catch (Exception ex)
        {
            ShowInternetRepairInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
            SetInternetRepairStatus(
                Symbol.Important,
                Localize("InternetRepairStatusWarningsTitle", "Repairs completed with issues"),
                Localize("InternetRepairStatusWarningsDescription", "Some repairs failed. Review the log for details."));
            InternetRepairSummaryText.Text = Localize("InternetRepairSummaryError", "The last run encountered issues.");
            SetInternetRepairActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
        }
        finally
        {
            _internetRepairCts?.Dispose();
            _internetRepairCts = null;
            InternetRepairProgress.IsIndeterminate = false;
            InternetRepairProgress.Value = 0;
            SetInternetRepairBusy(false);
            UpdateInternetRepairSelectionState();
        }
    }

    private void OnInternetRepairCancel(object sender, RoutedEventArgs e) => _internetRepairCts?.Cancel();

    private void SetInternetRepairBusy(bool isBusy)
    {
        _isInternetRepairBusy = isBusy;
        foreach (var check in EnumerateInternetRepairActionCheckBoxes())
        {
            check.IsEnabled = !isBusy;
        }

        InternetRepairRunBtn.IsEnabled = !isBusy && GetSelectedInternetRepairActions().Count > 0;
        InternetRepairCancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        InternetRepairCancelBtn.IsEnabled = isBusy;
    }

    private void SetInternetRepairStatus(Symbol symbol, string title, string description)
    {
        InternetRepairStatusGlyph.Symbol = symbol;
        InternetRepairStatusTitle.Text = title;
        InternetRepairStatusDescription.Text = description;
        InternetRepairStatusHero.Background = GetStatusHeroBrush(symbol);
        InternetRepairStatusGlyph.Foreground = GetStatusGlyphBrush(symbol);
    }

    private void SetInternetRepairActivity(string message)
    {
        InternetRepairActivityText.Text = message;
    }

    private void ShowInternetRepairInfo(string message, InfoBarSeverity severity)
    {
        InternetRepairInfoBar.Message = message;
        InternetRepairInfoBar.Severity = severity;
        InternetRepairInfoBar.IsOpen = true;
    }

    private void DismissInternetRepairInfo()
    {
        InternetRepairInfoBar.IsOpen = false;
    }

    private void OnInternetRepairProgress(InternetRepairStepUpdate update)
    {
        if (!_internetRepairLogLookup.TryGetValue(update.Action.Id, out var entry))
        {
            entry = new InternetRepairLogEntry(update.Action.DisplayName);
            _internetRepairLogLookup[update.Action.Id] = entry;
            _internetRepairLog.Add(entry);
        }

        switch (update.State)
        {
            case InternetRepairStepState.Starting:
                entry.Update(
                    Symbol.Sync,
                    LocalizeFormat(
                        "InternetRepairLogRunning",
                        "Running {0}…",
                        update.Action.DisplayName));
                break;
            case InternetRepairStepState.Succeeded:
                entry.Update(
                    Symbol.Accept,
                    !string.IsNullOrWhiteSpace(update.Message)
                        ? update.Message!
                        : update.Action.SuccessMessage ??
                          Localize("InternetRepairLogCompleted", "Completed successfully."));
                break;
            case InternetRepairStepState.Failed:
                entry.Update(
                    Symbol.Important,
                    !string.IsNullOrWhiteSpace(update.Message)
                        ? update.Message!
                        : Localize("InternetRepairLogFailed", "Failed. Check the InfoBar for details."));
                break;
            case InternetRepairStepState.Cancelled:
                entry.Update(
                    Symbol.Cancel,
                    Localize("InternetRepairLogCancelled", "Cancelled."));
                break;
        }
    }

    private void HandleInternetRepairCompletion(InternetRepairResult result)
    {
        var success = result.SuccessCount;
        var failure = result.FailureCount;

        if (failure == 0)
        {
            ShowInternetRepairInfo(
                LocalizeFormat(
                    "InternetRepairInfoSuccess",
                    "Completed {0} repair action(s) successfully.",
                    success),
                InfoBarSeverity.Success);
            SetInternetRepairStatus(
                Symbol.Accept,
                Localize("InternetRepairStatusCompleteTitle", "Repairs completed"),
                Localize("InternetRepairStatusCompleteDescription", "Network repairs finished. Test your connection."));
            InternetRepairSummaryText.Text = LocalizeFormat(
                "InternetRepairSummaryCompleted",
                "Last run: {0} action(s) completed without issues.",
                success);
        }
        else
        {
            ShowInternetRepairInfo(
                LocalizeFormat(
                    "InternetRepairInfoPartial",
                    "Completed {0} repair action(s). {1} action(s) failed.",
                    success,
                    failure),
                InfoBarSeverity.Warning);
            SetInternetRepairStatus(
                Symbol.Important,
                Localize("InternetRepairStatusWarningsTitle", "Repairs completed with issues"),
                Localize("InternetRepairStatusWarningsDescription", "Some repairs failed. Review the log for details."));
            InternetRepairSummaryText.Text = LocalizeFormat(
                "InternetRepairSummaryPartial",
                "Last run: {0} succeeded, {1} failed.",
                success,
                failure);
        }

        SetInternetRepairActivity(Localize("InternetRepairActivityCompleted", "Finished running the selected actions."));
    }

    private void HandleInternetRepairCancelled()
    {
        ShowInternetRepairInfo(
            Localize("InternetRepairInfoCancelled", "Repairs cancelled."),
            InfoBarSeverity.Informational);
        SetInternetRepairStatus(
            Symbol.Cancel,
            Localize("InternetRepairStatusCancelledTitle", "Repairs cancelled"),
            Localize("InternetRepairStatusCancelledDescription", "The selected actions were cancelled. Adjust your selection and try again."));
        InternetRepairSummaryText.Text = Localize(
            "InternetRepairSummaryCancelled",
            "Repairs were cancelled before completion.");
        SetInternetRepairActivity(Localize("InternetRepairActivityCancelled", "Repair run cancelled."));
    }
}
