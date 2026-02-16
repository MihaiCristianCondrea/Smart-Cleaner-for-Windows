using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    void ILargeFilesWorkflowView.SetLargeFilesExclusionState(bool hasExclusions)
    {
        SetLargeFilesExclusionState(hasExclusions);
    }

    void ILargeFilesWorkflowView.ShowLargeFilesInfoMessage(string message, InfoBarSeverity severity)
    {
        ShowLargeFilesInfo(message, severity);
    }

    void ISettingsWorkflowView.ApplyThemePreferenceFromCoordinator(string? preference, bool save)
        => ApplyThemePreference(preference, save);

    void ISettingsWorkflowView.SelectThemeOptionFromCoordinator(string preference)
        => SelectThemeOption(_themePreference);

    void ISettingsWorkflowView.ApplyAccentPreferenceFromCoordinator(string? preference, bool save)
        => ApplyAccentPreference(preference, save);

    void ISettingsWorkflowView.SelectAccentOptionFromCoordinator(string preference)
        => SelectAccentOption(_accentPreference);

    void ISettingsWorkflowView.UpdateCleanerSettingsViewFromCoordinator()
        => UpdateCleanerSettingsView();

    void ISettingsWorkflowView.UpdateAutomationSettingsViewFromCoordinator()
        => UpdateAutomationSettingsView();

    void ISettingsWorkflowView.SetNotificationValuesFromCoordinator(bool showCompletion, bool desktopAlerts, int historyRetentionDays)
    {
        if (SettingsView.NotificationCompletionToggle is not null)
        {
            SettingsView.NotificationCompletionToggle.IsOn = showCompletion;
        }

        if (SettingsView.NotificationDesktopToggle is not null)
        {
            SettingsView.NotificationDesktopToggle.IsOn = desktopAlerts;
        }

        if (SettingsView.HistoryRetentionNumberBox is not null)
        {
            SettingsView.HistoryRetentionNumberBox.Value = historyRetentionDays;
        }
    }

    void ISettingsWorkflowView.UpdateSettingsSummariesFromCoordinator()
    {
        UpdateCleanerDefaultsSummary();
        UpdateAutomationSummary();
        UpdateNotificationSummary();
        UpdateHistoryRetentionSummary();
    }

    void ISettingsWorkflowView.ApplyCleanerDefaultsToSessionFromCoordinator()
        => ApplyCleanerDefaultsToSession();

    private void SetLargeFilesExclusionState(bool hasExclusions)
    {
        LargeFilesView.LargeFilesNoExclusionsText.Visibility = hasExclusions ? Visibility.Collapsed : Visibility.Visible;
        LargeFilesView.LargeFilesClearExclusionsBtn.IsEnabled = hasExclusions && !_isLargeFilesBusy;
    }
}
