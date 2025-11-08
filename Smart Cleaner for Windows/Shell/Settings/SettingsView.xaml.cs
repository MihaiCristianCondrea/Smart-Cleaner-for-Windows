using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Shell.Settings;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public event SelectionChangedEventHandler? ThemeSelectionChanged;

    public event SelectionChangedEventHandler? AccentPreferenceChanged;

    public event RoutedEventHandler? CleanerDefaultsApplied;

    public event RoutedEventHandler? CleanerRecyclePreferenceToggled;

    public event TextChangedEventHandler? CleanerExclusionsPreferenceChanged;

    public event NumberBoxValueChangedEventHandler? CleanerDepthPreferenceChanged; // FIXME: Cannot resolve symbol 'NumberBoxValueChangedEventHandler'

    public event RoutedEventHandler? AutomationPreferenceToggled;

    public event RoutedEventHandler? NotificationPreferenceToggled;

    public event NumberBoxValueChangedEventHandler? HistoryRetentionChanged; // FIXME: Cannot resolve symbol 'NumberBoxValueChangedEventHandler'

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e) => ThemeSelectionChanged?.Invoke(sender, e);

    private void OnAccentPreferenceChanged(object sender, SelectionChangedEventArgs e) =>
        AccentPreferenceChanged?.Invoke(sender, e);

    private void OnApplyCleanerDefaults(object sender, RoutedEventArgs e) => CleanerDefaultsApplied?.Invoke(sender, e);

    private void OnCleanerRecyclePreferenceToggled(object sender, RoutedEventArgs e) =>
        CleanerRecyclePreferenceToggled?.Invoke(sender, e);

    private void OnCleanerExclusionsPreferenceChanged(object sender, TextChangedEventArgs e) =>
        CleanerExclusionsPreferenceChanged?.Invoke(sender, e);

    private void OnCleanerDepthPreferenceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        CleanerDepthPreferenceChanged?.Invoke(sender, args);

    private void OnAutomationPreferenceToggled(object sender, RoutedEventArgs e) =>
        AutomationPreferenceToggled?.Invoke(sender, e);

    private void OnNotificationPreferenceToggled(object sender, RoutedEventArgs e) =>
        NotificationPreferenceToggled?.Invoke(sender, e);

    private void OnHistoryRetentionChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        HistoryRetentionChanged?.Invoke(sender, args);
}
