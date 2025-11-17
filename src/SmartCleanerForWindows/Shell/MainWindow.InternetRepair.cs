using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartCleanerForWindows.Core.Networking;
using SmartCleanerForWindows.Modules.InternetRepair.ViewModels;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    private void InitializeInternetRepair()
    {
        if (InternetRepairView.InternetRepairLogList is not null)
        {
            InternetRepairView.InternetRepairLogList.ItemsSource = _internetRepairLog;
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
        InternetRepairView.InternetRepairSummaryText.Text = Localize("InternetRepairSummaryReady", "Select the repairs you want to run.");
        InternetRepairView.InternetRepairProgress.IsIndeterminate = false;
        InternetRepairView.InternetRepairProgress.Value = 0;
        InternetRepairView.InternetRepairCancelBtn.Visibility = Visibility.Collapsed;
        InternetRepairView.InternetRepairCancelBtn.IsEnabled = false;
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
        if (InternetRepairView.InternetRepairLogPlaceholder is null)
        {
            return;
        }

        InternetRepairView.InternetRepairLogPlaceholder.Visibility = _internetRepairLog.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private IEnumerable<CheckBox> EnumerateInternetRepairActionCheckBoxes()
    {
        yield return InternetRepairView.InternetRepairWinsockChk;
        yield return InternetRepairView.InternetRepairTcpIpChk;
        yield return InternetRepairView.InternetRepairDnsChk;
        yield return InternetRepairView.InternetRepairReleaseChk;
        yield return InternetRepairView.InternetRepairRenewChk;
        yield return InternetRepairView.InternetRepairProxyChk;
    }

    private void OnInternetRepairActionSelectionChanged(object sender, RoutedEventArgs e) => UpdateInternetRepairSelectionState();

    private void UpdateInternetRepairSelectionState()
    {
        var count = EnumerateInternetRepairActionCheckBoxes().Count(cb => cb.IsChecked == true);
        if (count > 0)
        {
            InternetRepairView.InternetRepairResultBadge.Value = count;
            InternetRepairView.InternetRepairResultBadge.Visibility = Visibility.Visible;
            InternetRepairView.InternetRepairSummaryText.Text = LocalizeFormat(
                "InternetRepairSummarySelection",
                "Ready to run {0} selected repair(s).",
                count);
        }
        else
        {
            InternetRepairView.InternetRepairResultBadge.ClearValue(InfoBadge.ValueProperty);
            InternetRepairView.InternetRepairResultBadge.Visibility = Visibility.Collapsed;
            InternetRepairView.InternetRepairSummaryText.Text = Localize("InternetRepairSummaryReady", "Select the repairs you want to run.");
        }

        if (!_isInternetRepairBusy)
        {
            InternetRepairView.InternetRepairRunBtn.IsEnabled = count > 0;
        }
    }

    private IReadOnlyList<InternetRepairAction> GetSelectedInternetRepairActions()
    {
        var selected = new List<InternetRepairAction>();
        foreach (var check in EnumerateInternetRepairActionCheckBoxes())
        {
            if (check is { IsChecked: true, Tag: string id } && _internetRepairActions.TryGetValue(id, out var action))
            {
                selected.Add(action);
            }
        }

        return selected;
    }

    private async void OnInternetRepairRun(object sender, RoutedEventArgs e)
    {
        try
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

            if (_internetRepairCts is { IsCancellationRequested: false } previousCts)
            {
                try
                {
                    await previousCts.CancelAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    ShowInternetRepairInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
                }
            }

            _internetRepairCts = new CancellationTokenSource();
            _internetRepairLogLookup.Clear();
            _internetRepairLog.Clear();
            InternetRepairView.InternetRepairProgress.Value = 0;
            InternetRepairView.InternetRepairProgress.IsIndeterminate = true;

            SetInternetRepairBusy(true);
            SetInternetRepairStatus(
                Symbol.Sync,
                Localize("InternetRepairStatusRunningTitle", "Applying fixes…"),
                Localize("InternetRepairStatusRunningDescription", "Hang tight while we apply the selected network repairs."));
            SetInternetRepairActivity(Localize("InternetRepairActivityRunning", "Running network repair actions…"));
            InternetRepairView.InternetRepairSummaryText.Text = LocalizeFormat(
                "InternetRepairSummaryRunning",
                "Running {0} repair action(s)…",
                actions.Count);

            try
            {
                IProgress<InternetRepairStepUpdate> progress = new Progress<InternetRepairStepUpdate>(OnInternetRepairProgress);
                var result = await _internetRepairService.RunAsync(actions, progress, _internetRepairCts.Token)
                    .ConfigureAwait(true);
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
                InternetRepairView.InternetRepairSummaryText.Text = Localize("InternetRepairSummaryError", "The last run encountered issues.");
                SetInternetRepairActivity(Localize("ActivitySomethingWentWrong", "Something went wrong."));
            }
            finally
            {
                _internetRepairCts?.Dispose();
                _internetRepairCts = null;
                InternetRepairView.InternetRepairProgress.IsIndeterminate = false;
                InternetRepairView.InternetRepairProgress.Value = 0;
                SetInternetRepairBusy(false);
                UpdateInternetRepairSelectionState();
            }
        }
        catch (Exception ex)
        {
            ShowInternetRepairInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async void OnInternetRepairCancel(object sender, RoutedEventArgs e) // FIXME: Avoid using 'async' for method with the 'void' return type or catch all exceptions in it: any exceptions unhandled by the method might lead to the process crash
    {
        if (_internetRepairCts is { IsCancellationRequested: false } cts)
        {
            try
            {
                await cts.CancelAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowInternetRepairInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
            }
        }
    }

    private void SetInternetRepairBusy(bool isBusy)
    {
        _isInternetRepairBusy = isBusy;
        foreach (var check in EnumerateInternetRepairActionCheckBoxes())
        {
            check.IsEnabled = !isBusy;
        }

        InternetRepairView.InternetRepairRunBtn.IsEnabled = !isBusy && GetSelectedInternetRepairActions().Count > 0;
        InternetRepairView.InternetRepairCancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        InternetRepairView.InternetRepairCancelBtn.IsEnabled = isBusy;
    }

    private void SetInternetRepairStatus(Symbol symbol, string title, string description)
    {
        InternetRepairView.InternetRepairStatusGlyph.Symbol = symbol;
        InternetRepairView.InternetRepairStatusTitle.Text = title;
        InternetRepairView.InternetRepairStatusDescription.Text = description;
        InternetRepairView.InternetRepairStatusHero.Background = GetStatusHeroBrush(symbol);
        InternetRepairView.InternetRepairStatusGlyph.Foreground = GetStatusGlyphBrush(symbol);
    }

    private void SetInternetRepairActivity(string message)
    {
        InternetRepairView.InternetRepairActivityText.Text = message;
    }

    private void ShowInternetRepairInfo(string message, InfoBarSeverity severity)
    {
        InternetRepairView.InternetRepairInfoBar.Message = message;
        InternetRepairView.InternetRepairInfoBar.Severity = severity;
        InternetRepairView.InternetRepairInfoBar.IsOpen = true;
    }

    private void DismissInternetRepairInfo()
    {
        InternetRepairView.InternetRepairInfoBar.IsOpen = false;
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
            InternetRepairView.InternetRepairSummaryText.Text = LocalizeFormat(
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
            InternetRepairView.InternetRepairSummaryText.Text = LocalizeFormat(
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
        InternetRepairView.InternetRepairSummaryText.Text = Localize(
            "InternetRepairSummaryCancelled",
            "Repairs were cancelled before completion.");
        SetInternetRepairActivity(Localize("InternetRepairActivityCancelled", "Repair run cancelled."));
    }
}
