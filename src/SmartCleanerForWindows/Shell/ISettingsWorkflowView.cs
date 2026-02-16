namespace SmartCleanerForWindows.Shell;

internal interface ISettingsWorkflowView
{
    void ApplyThemePreferenceFromCoordinator(string? preference, bool save);
    void SelectThemeOptionFromCoordinator(string preference);
    void ApplyAccentPreferenceFromCoordinator(string? preference, bool save);
    void SelectAccentOptionFromCoordinator(string preference);
    void UpdateCleanerSettingsViewFromCoordinator();
    void UpdateAutomationSettingsViewFromCoordinator();
    void SetNotificationValuesFromCoordinator(bool showCompletion, bool desktopAlerts, int historyRetentionDays);
    void UpdateSettingsSummariesFromCoordinator();
    void ApplyCleanerDefaultsToSessionFromCoordinator();
}
