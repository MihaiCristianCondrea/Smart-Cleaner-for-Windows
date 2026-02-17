using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SmartCleanerForWindows.Diagnostics;
using Windows.Foundation;

namespace SmartCleanerForWindows.Shell.Settings;

public sealed class SettingsView : UserControl
{
    internal TextBlock? CleanerDefaultsSummaryText { get; private set; }
    internal ToggleSwitch? CleanerRecycleToggle { get; private set; }
    internal NumberBox? CleanerDepthPreferenceBox { get; private set; }
    internal TextBox? CleanerExclusionsPreferenceBox { get; private set; }
    internal InfoBar? CleanerDefaultsInfoBar { get; private set; }

    internal TextBlock? AutomationSummaryText { get; private set; }
    internal ToggleSwitch? AutomationAutoPreviewToggle { get; private set; }
    internal ToggleSwitch? AutomationReminderToggle { get; private set; }

    internal TextBlock? NotificationSummaryText { get; private set; }
    internal ToggleSwitch? NotificationCompletionToggle { get; private set; }
    internal ToggleSwitch? NotificationDesktopToggle { get; private set; }

    internal TextBlock? HistoryRetentionSummaryText { get; private set; }
    internal NumberBox? HistoryRetentionNumberBox { get; private set; }

    internal TextBlock? ThemeSummaryText { get; private set; }
    internal RadioButtons? ThemeRadioButtons { get; private set; }

    internal TextBlock? AccentSummaryText { get; private set; }
    internal RadioButtons? AccentColorRadioButtons { get; private set; }

    public SettingsView()
    {
        Content = BuildContent();
        UiConstructionLog.AttachFrameworkElementDiagnostics(this, "SettingsView");
    }

    public event SelectionChangedEventHandler? ThemeSelectionChanged;
    public event SelectionChangedEventHandler? AccentPreferenceChanged;
    public event RoutedEventHandler? CleanerDefaultsApplied;
    public event RoutedEventHandler? CleanerRecyclePreferenceToggled;
    public event TextChangedEventHandler? CleanerExclusionsPreferenceChanged;
    public event TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>? CleanerDepthPreferenceChanged;
    public event RoutedEventHandler? AutomationPreferenceToggled;
    public event RoutedEventHandler? NotificationPreferenceToggled;
    public event TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>? HistoryRetentionChanged;

    private UIElement BuildContent()
    {
        var root = new StackPanel { Padding = new Thickness(32), Spacing = 24 };
        root.Children.Add(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = "Settings", FontSize = 32, FontWeight = new Windows.UI.Text.FontWeight(600) },
                new TextBlock { Text = "Adjust Smart Cleaner to match your preferences.", Opacity = 0.75 }
            }
        });

        root.Children.Add(BuildAppearanceCard());
        root.Children.Add(BuildCleanerDefaultsCard());
        root.Children.Add(BuildAutomationCard());
        root.Children.Add(BuildNotificationsCard());

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private UIElement BuildAppearanceCard()
    {
        ThemeSummaryText = new TextBlock { Opacity = 0.75, Text = "Use system" };
        ThemeRadioButtons = new RadioButtons { ItemsSource = new[] { "Use system", "Light", "Dark" } };
        ThemeRadioButtons.SelectionChanged += OnThemeSelectionChanged;

        AccentSummaryText = new TextBlock { Opacity = 0.75, Text = "Use system accent" };
        AccentColorRadioButtons = new RadioButtons { ItemsSource = new[] { "Use system accent", "Blue", "Green", "Purple", "Orange" } };
        AccentColorRadioButtons.SelectionChanged += OnAccentPreferenceChanged;

        return BuildCard("Appearance", new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = "Theme and accent", FontSize = 20 },
                ThemeSummaryText,
                ThemeRadioButtons,
                AccentSummaryText,
                AccentColorRadioButtons
            }
        });
    }

    private UIElement BuildCleanerDefaultsCard()
    {
        CleanerDefaultsSummaryText = new TextBlock { Opacity = 0.75, Text = "Recycle Bin • No depth limit" };

        CleanerRecycleToggle = new ToggleSwitch
        {
            Header = "Send deleted folders to Recycle Bin",
            OnContent = "Recycle Bin",
            OffContent = "Delete permanently"
        };
        CleanerRecycleToggle.Toggled += OnCleanerRecyclePreferenceToggled;

        CleanerDepthPreferenceBox = new NumberBox
        {
            Header = "Traversal depth limit",
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };
        CleanerDepthPreferenceBox.ValueChanged += OnCleanerDepthPreferenceChanged;

        CleanerExclusionsPreferenceBox = new TextBox
        {
            Header = "Exclusions",
            PlaceholderText = ".git;node_modules;build/*"
        };
        CleanerExclusionsPreferenceBox.TextChanged += OnCleanerExclusionsPreferenceChanged;

        var applyDefaultsButton = new Button
        {
            Content = "Apply defaults",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        applyDefaultsButton.Click += OnApplyCleanerDefaults;

        CleanerDefaultsInfoBar = new InfoBar { IsOpen = false, IsClosable = true, Severity = InfoBarSeverity.Informational };

        return BuildCard("Cleaner defaults", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                CleanerDefaultsSummaryText,
                CleanerRecycleToggle,
                CleanerDepthPreferenceBox,
                CleanerExclusionsPreferenceBox,
                applyDefaultsButton,
                CleanerDefaultsInfoBar
            }
        });
    }

    private UIElement BuildAutomationCard()
    {
        AutomationSummaryText = new TextBlock { Opacity = 0.75, Text = "Automation preferences" };
        AutomationAutoPreviewToggle = new ToggleSwitch { Header = "Auto preview after folder selection" };
        AutomationAutoPreviewToggle.Toggled += OnAutomationPreferenceToggled;

        AutomationReminderToggle = new ToggleSwitch { Header = "Weekly cleanup reminder" };
        AutomationReminderToggle.Toggled += OnAutomationPreferenceToggled;

        return BuildCard("Automation", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                AutomationSummaryText,
                AutomationAutoPreviewToggle,
                AutomationReminderToggle
            }
        });
    }

    private UIElement BuildNotificationsCard()
    {
        NotificationSummaryText = new TextBlock { Opacity = 0.75, Text = "Notification preferences" };

        NotificationCompletionToggle = new ToggleSwitch { Header = "Show completion notifications" };
        NotificationCompletionToggle.Toggled += OnNotificationPreferenceToggled;

        NotificationDesktopToggle = new ToggleSwitch { Header = "Show desktop alerts" };
        NotificationDesktopToggle.Toggled += OnNotificationPreferenceToggled;

        HistoryRetentionSummaryText = new TextBlock { Opacity = 0.75, Text = "Retention period for run history" };

        HistoryRetentionNumberBox = new NumberBox
        {
            Header = "History retention (days)",
            Minimum = 0,
            Maximum = 365,
            Value = 30,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };
        HistoryRetentionNumberBox.ValueChanged += OnHistoryRetentionChanged;

        return BuildCard("Notifications and history", new StackPanel
        {
            Spacing = 12,
            Children =
            {
                NotificationSummaryText,
                NotificationCompletionToggle,
                NotificationDesktopToggle,
                HistoryRetentionSummaryText,
                HistoryRetentionNumberBox
            }
        });
    }

    private static Border BuildCard(string title, UIElement content)
    {
        return new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = title, FontSize = 22, FontWeight = new Windows.UI.Text.FontWeight(600) },
                    content
                }
            }
        };
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e) => ThemeSelectionChanged?.Invoke(sender, e);
    private void OnAccentPreferenceChanged(object sender, SelectionChangedEventArgs e) => AccentPreferenceChanged?.Invoke(sender, e);
    private void OnApplyCleanerDefaults(object sender, RoutedEventArgs e) => CleanerDefaultsApplied?.Invoke(sender, e);
    private void OnCleanerRecyclePreferenceToggled(object sender, RoutedEventArgs e) => CleanerRecyclePreferenceToggled?.Invoke(sender, e);
    private void OnCleanerExclusionsPreferenceChanged(object sender, TextChangedEventArgs e) => CleanerExclusionsPreferenceChanged?.Invoke(sender, e);
    private void OnCleanerDepthPreferenceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => CleanerDepthPreferenceChanged?.Invoke(sender, args);
    private void OnAutomationPreferenceToggled(object sender, RoutedEventArgs e) => AutomationPreferenceToggled?.Invoke(sender, e);
    private void OnNotificationPreferenceToggled(object sender, RoutedEventArgs e) => NotificationPreferenceToggled?.Invoke(sender, e);
    private void OnHistoryRetentionChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => HistoryRetentionChanged?.Invoke(sender, args);
}
