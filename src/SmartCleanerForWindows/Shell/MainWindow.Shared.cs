using System;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using SmartCleanerForWindows.Settings;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    private async System.Threading.Tasks.Task<string?> PickFolderPathAsync()
    {
        var picker = new FolderPicker(AppWindow.Id);
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private object? FindElement(string elementName)
    {
        if (Content is FrameworkElement root)
        {
            return root.FindName(elementName);
        }

        return null;
    }

    private string Localize(string resourceKey, string fallback)
    {
        if (_resources is null)
        {
            return fallback;
        }

        try
        {
            var value = _resources.GetString(resourceKey);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private string LocalizeFormat(string resourceKey, string fallback, params object?[] args)
    {
        var format = Localize(resourceKey, fallback);
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;

        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is not null)
        {
            emptyFoldersView.Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            emptyFoldersView.Progress.IsIndeterminate = isBusy;
            emptyFoldersView.CancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            emptyFoldersView.CancelBtn.IsEnabled = isBusy;
            emptyFoldersView.PreviewBtn.IsEnabled = !isBusy;
            emptyFoldersView.BrowseBtn.IsEnabled = !isBusy;
            emptyFoldersView.RootPathBox.IsEnabled = !isBusy;
            emptyFoldersView.RecycleChk.IsEnabled = !isBusy;
            emptyFoldersView.DepthBox.IsEnabled = !isBusy;
            emptyFoldersView.ExcludeBox.IsEnabled = !isBusy;

            UpdateResultFilterControls();
            UpdateResultsActionState();
        }

        if (_diskCleanupViewInitialized)
        {
            UpdateDiskCleanupActionState();
        }
    }

    private void SetStatus(Symbol symbol, string title, string description, int? badgeValue = null)
    {
        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is null)
        {
            return;
        }

        emptyFoldersView.StatusGlyph.Symbol = symbol;
        emptyFoldersView.StatusTitle.Text = title;
        emptyFoldersView.StatusDescription.Text = description;
        emptyFoldersView.StatusHero.Background = GetStatusHeroBrush(symbol);
        emptyFoldersView.StatusGlyph.Foreground = GetStatusGlyphBrush(symbol);

        if (badgeValue is > 0)
        {
            emptyFoldersView.ResultBadge.Value = badgeValue.Value;
            emptyFoldersView.ResultBadge.Visibility = Visibility.Visible;
        }
        else
        {
            emptyFoldersView.ResultBadge.ClearValue(InfoBadge.ValueProperty);
            emptyFoldersView.ResultBadge.Visibility = Visibility.Collapsed;
        }
    }


    private void UpdateResultBadgeValue(int visibleCount)
    {
        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is null)
        {
            return;
        }

        if (visibleCount > 0)
        {
            emptyFoldersView.ResultBadge.Value = visibleCount;
            emptyFoldersView.ResultBadge.Visibility = Visibility.Visible;
        }
        else
        {
            emptyFoldersView.ResultBadge.ClearValue(InfoBadge.ValueProperty);
            emptyFoldersView.ResultBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void SetActivity(string message)
    {
        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is null)
        {
            return;
        }

        emptyFoldersView.ActivityText.Text = message;
    }

    private void UpdateResultsSummary(int visibleCount, string? detail = null, int? totalCount = null)
    {
        var summary = totalCount is > 0
            ? LocalizeFormat("ResultsSummaryWithTotal", "Showing {0} item(s) (from {1} found).", visibleCount, totalCount.Value)
            : LocalizeFormat("ResultsSummary", "Showing {0} item(s).", visibleCount);

        if (!string.IsNullOrWhiteSpace(detail))
        {
            summary = string.Format(CultureInfo.CurrentCulture, "{0} {1}", summary, detail);
        }

        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is null)
        {
            return;
        }

        emptyFoldersView.ResultsCaption.Text = summary;
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is null)
        {
            return;
        }

        emptyFoldersView.Info.Message = message;
        emptyFoldersView.Info.Severity = severity;
        emptyFoldersView.Info.IsOpen = true;
    }

    private static Brush GetStatusHeroBrush(Symbol symbol)
    {
        var resources = Application.Current?.Resources;
        return symbol switch
        {
            Symbol.Accept => new SolidColorBrush(Colors.SeaGreen),
            Symbol.Important or Symbol.Cancel => new SolidColorBrush(Colors.IndianRed),
            Symbol.Sync => new SolidColorBrush(Colors.SteelBlue),
            _ => resources?["AccentFillColorTertiaryBrush"] as Brush ?? new SolidColorBrush(Colors.DodgerBlue)
        };
    }

    private static Brush GetStatusGlyphBrush(Symbol symbol)
    {
        var resources = Application.Current?.Resources;
        return symbol switch
        {
            Symbol.Accept => new SolidColorBrush(Colors.White),
            Symbol.Important or Symbol.Cancel => new SolidColorBrush(Colors.White),
            Symbol.Sync => new SolidColorBrush(Colors.White),
            _ => resources?["TextOnAccentFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Colors.White)
        };
    }

    private void ApplySnapshot(ToolSettingsSnapshot snapshot)
    {
        _settingsSnapshots[snapshot.Definition.Id] = new ToolSettingsSnapshot
        {
            Definition = snapshot.Definition,
            Values = (JsonObject)snapshot.Values.DeepClone()
        };

        switch (snapshot.Definition.Id)
        {
            case EmptyFoldersToolId:
                ApplyEmptyFolderSettings(snapshot.Values);
                break;
            case DashboardToolId:
                ApplyDashboardSettings(snapshot.Values);
                break;
            case LargeFilesToolId:
                if (_largeFilesViewInitialized)
                {
                    ApplyLargeFilesSettings(snapshot.Values);
                }

                break;
            case InternetRepairToolId:
                ApplyInternetRepairSettings(snapshot.Values);
                break;
        }
    }

    private void ApplyEmptyFolderSettings(JsonObject values)
    {
        _cleanerSendToRecycleBin = _settingsCoordinator.GetBooleanValue(values, "sendToRecycleBin", _cleanerSendToRecycleBin);
        _cleanerDepthLimit = _settingsCoordinator.GetIntegerValue(values, "depthLimit", _cleanerDepthLimit, 0, 999);
        _cleanerExclusions = _settingsCoordinator.GetStringValue(values, "exclusions", _cleanerExclusions);
        _automationAutoPreview = _settingsCoordinator.GetBooleanValue(values, "previewAutomatically", _automationAutoPreview);

        var emptyFoldersView = EnsureEmptyFoldersView();
        if (emptyFoldersView is not null)
        {
            emptyFoldersView.RecycleChk.IsChecked = _cleanerSendToRecycleBin;
            emptyFoldersView.DepthBox.Value = _cleanerDepthLimit;
            emptyFoldersView.ExcludeBox.Text = _cleanerExclusions;
        }

        UpdateCleanerSettingsView();
        UpdateAutomationSettingsView();
        UpdateCleanerDefaultsSummary();
        UpdateAutomationSummary();
    }

    private void ApplyDashboardSettings(JsonObject values)
    {
        _automationWeeklyReminder = _settingsCoordinator.GetBooleanValue(values, "remindWeekly", _automationWeeklyReminder);

        UpdateAutomationSettingsView();
        UpdateAutomationSummary();
    }

    private void ApplyInternetRepairSettings(JsonObject values)
    {
        var internetRepairView = EnsureInternetRepairView();
        if (internetRepairView is null)
        {
            return;
        }

        internetRepairView.InternetRepairDnsChk.IsChecked = _settingsCoordinator.GetBooleanValue(values, "flushDns", internetRepairView.InternetRepairDnsChk.IsChecked == true);
        internetRepairView.InternetRepairWinsockChk.IsChecked = _settingsCoordinator.GetBooleanValue(values, "resetWinsock", internetRepairView.InternetRepairWinsockChk.IsChecked == true);

        UpdateInternetRepairSelectionState();
    }

    private void UpdateCleanerDefaultsSummary()
    {
        var settingsView = EnsureSettingsView();
        if (settingsView?.CleanerDefaultsSummaryText is null)
        {
            return;
        }

        var deleteMode = _cleanerSendToRecycleBin
            ? Localize("SettingsCleanerModeRecycleBin", "Recycle Bin")
            : Localize("SettingsCleanerModePermanent", "Permanent delete");

        var depthSummary = _cleanerDepthLimit > 0
            ? LocalizeFormat("SettingsCleanerDepthLimited", "Depth limit: {0}", _cleanerDepthLimit)
            : Localize("SettingsCleanerDepthUnlimited", "No depth limit");

        settingsView.CleanerDefaultsSummaryText.Text = string.Format(CultureInfo.CurrentCulture, "{0} • {1}", deleteMode, depthSummary);
    }

    private void UpdateAutomationSummary()
    {
        var settingsView = EnsureSettingsView();
        if (settingsView?.AutomationSummaryText is null)
        {
            return;
        }

        var autoPreview = _automationAutoPreview
            ? Localize("SettingsAutomationAutoPreviewOn", "Auto preview enabled")
            : Localize("SettingsAutomationAutoPreviewOff", "Auto preview disabled");

        var reminder = _automationWeeklyReminder
            ? Localize("SettingsAutomationReminderOn", "Weekly reminders on")
            : Localize("SettingsAutomationReminderOff", "Weekly reminders off");

        settingsView.AutomationSummaryText.Text = string.Format(CultureInfo.CurrentCulture, "{0} • {1}", autoPreview, reminder);
    }

    private void UpdateNotificationSummary()
    {
        var settingsView = EnsureSettingsView();
        if (settingsView?.NotificationSummaryText is null)
        {
            return;
        }

        var completion = _notificationShowCompletion
            ? Localize("SettingsNotificationCompletionOn", "Completion notifications on")
            : Localize("SettingsNotificationCompletionOff", "Completion notifications off");

        var desktop = _notificationDesktopAlerts
            ? Localize("SettingsNotificationDesktopOn", "Desktop alerts on")
            : Localize("SettingsNotificationDesktopOff", "Desktop alerts off");

        settingsView.NotificationSummaryText.Text = string.Format(CultureInfo.CurrentCulture, "{0} • {1}", completion, desktop);
    }

    private void UpdateHistoryRetentionSummary()
    {
        var settingsView = EnsureSettingsView();
        if (settingsView?.HistoryRetentionSummaryText is null)
        {
            return;
        }

        settingsView.HistoryRetentionSummaryText.Text = _historyRetentionDays <= 0
            ? Localize("SettingsHistoryRetentionNone", "Clear history after each run")
            : LocalizeFormat("SettingsHistoryRetentionDays", "Keep history for {0} day(s)", _historyRetentionDays);
    }
}
