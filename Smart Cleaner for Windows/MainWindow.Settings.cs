using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow
{
    private static bool ParseBoolSetting(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric != 0;
        }

        return defaultValue;
    }

    private static int ParseIntSetting(string? value, int defaultValue, int min, int max)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            defaultValue = parsed;
        }

        return Math.Clamp(defaultValue, min, max);
    }

    private void LoadPreferences()
    {
        _isInitializingSettings = true;

        var savedTheme = ReadSetting(ThemePreferenceKey);
        ApplyThemePreference(savedTheme, save: false);
        SelectThemeOption(_themePreference);

        var savedAccent = ReadSetting(AccentPreferenceKey);
        ApplyAccentPreference(savedAccent, save: false);
        SelectAccentOption(_accentPreference);

        _cleanerSendToRecycleBin = ParseBoolSetting(ReadSetting(CleanerRecyclePreferenceKey), defaultValue: true);
        _cleanerDepthLimit = ParseIntSetting(
            ReadSetting(CleanerDepthPreferenceKey),
            defaultValue: 0,
            min: 0,
            max: 999);
        _cleanerExclusions = ReadSetting(CleanerExclusionsPreferenceKey) ?? string.Empty;

        _automationAutoPreview = ParseBoolSetting(ReadSetting(AutomationAutoPreviewKey), defaultValue: false);
        _automationWeeklyReminder = ParseBoolSetting(ReadSetting(AutomationReminderKey), defaultValue: false);
        _notificationShowCompletion = ParseBoolSetting(ReadSetting(NotificationShowCompletionKey), defaultValue: true);
        _notificationDesktopAlerts = ParseBoolSetting(ReadSetting(NotificationDesktopAlertsKey), defaultValue: false);
        _historyRetentionDays = ParseIntSetting(
            ReadSetting(HistoryRetentionKey),
            HistoryRetentionDefaultDays,
            min: HistoryRetentionMinDays,
            max: HistoryRetentionMaxDays);

        if (CleanerRecycleToggle is not null)
        {
            CleanerRecycleToggle.IsOn = _cleanerSendToRecycleBin;
        }

        if (CleanerDepthPreferenceBox is not null)
        {
            CleanerDepthPreferenceBox.Value = _cleanerDepthLimit;
        }

        if (CleanerExclusionsPreferenceBox is not null)
        {
            CleanerExclusionsPreferenceBox.Text = _cleanerExclusions;
        }

        if (AutomationAutoPreviewToggle is not null)
        {
            AutomationAutoPreviewToggle.IsOn = _automationAutoPreview;
        }

        if (AutomationReminderToggle is not null)
        {
            AutomationReminderToggle.IsOn = _automationWeeklyReminder;
        }

        if (NotificationCompletionToggle is not null)
        {
            NotificationCompletionToggle.IsOn = _notificationShowCompletion;
        }

        if (NotificationDesktopToggle is not null)
        {
            NotificationDesktopToggle.IsOn = _notificationDesktopAlerts;
        }

        if (HistoryRetentionNumberBox is not null)
        {
            HistoryRetentionNumberBox.Value = _historyRetentionDays;
        }

        UpdateCleanerDefaultsSummary();
        UpdateAutomationSummary();
        UpdateNotificationSummary();
        UpdateHistoryRetentionSummary();

        _isInitializingSettings = false;

        ApplyCleanerDefaultsToSession();
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is not RadioButtons radioButtons)
        {
            return;
        }

        if (radioButtons.SelectedItem is RadioButton button && button.Tag is string tag)
        {
            ApplyThemePreference(tag, save: true);
        }
    }

    private void OnAccentPreferenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is not RadioButtons radioButtons)
        {
            return;
        }

        if (radioButtons.SelectedItem is RadioButton button && button.Tag is string tag)
        {
            ApplyAccentPreference(tag, save: true);
        }
    }

    private void ApplyThemePreference(string? preference, bool save)
    {
        var normalized = NormalizeThemePreference(preference);
        _themePreference = normalized;

        var theme = normalized switch
        {
            ThemePreferenceLight => ElementTheme.Light,
            ThemePreferenceDark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        RootNavigation.RequestedTheme = theme;

        if (_backdropConfig is not null)
        {
            _backdropConfig.Theme = theme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                _ => SystemBackdropTheme.Default
            };
        }

        ThemeSummaryText.Text = normalized switch
        {
            ThemePreferenceLight => "Light",
            ThemePreferenceDark => "Dark",
            _ => "Use system setting"
        };

        if (save)
        {
            SaveSetting(ThemePreferenceKey, normalized);
        }
    }

    private static string NormalizeThemePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return ThemePreferenceDefault;
        }

        return preference.Trim().ToLowerInvariant() switch
        {
            ThemePreferenceLight => ThemePreferenceLight,
            ThemePreferenceDark => ThemePreferenceDark,
            ThemePreferenceDefault => ThemePreferenceDefault,
            "system" => ThemePreferenceDefault,
            _ => ThemePreferenceDefault
        };
    }

    private void SelectThemeOption(string preference)
    {
        if (ThemeRadioButtons is null)
        {
            return;
        }

        ThemeRadioButtons.SelectedIndex = preference switch
        {
            ThemePreferenceLight => 0,
            ThemePreferenceDark => 1,
            _ => 2
        };
    }

    private void ApplyAccentPreference(string? preference, bool save)
    {
        var normalized = NormalizeAccentPreference(preference);
        _accentPreference = normalized;

        if (string.Equals(normalized, AccentPreferenceDefault, StringComparison.OrdinalIgnoreCase))
        {
            RestoreAccentColors();
        }
        else if (string.Equals(normalized, AccentPreferenceZest, StringComparison.OrdinalIgnoreCase))
        {
            ApplyAccentColor(GetZestAccentColor());
        }
        else if (TryParseColor(normalized, out var color))
        {
            ApplyAccentColor(color);
        }
        else
        {
            RestoreAccentColors();
            _accentPreference = AccentPreferenceDefault;
        }

        AccentSummaryText.Text = FormatAccentSummary(_accentPreference);

        if (save)
        {
            SaveSetting(AccentPreferenceKey, _accentPreference);
        }
    }

    private static string NormalizeAccentPreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return AccentPreferenceDefault;
        }

        var trimmed = preference.Trim();

        if (trimmed.StartsWith('#'))
        {
            return trimmed;
        }

        var lower = trimmed.ToLowerInvariant();
        return lower switch
        {
            AccentPreferenceZest => AccentPreferenceZest,
            AccentPreferenceDefault => AccentPreferenceDefault,
            "system" => AccentPreferenceDefault,
            _ => trimmed
        };
    }

    private void SelectAccentOption(string preference)
    {
        if (AccentColorRadioButtons is null)
        {
            return;
        }

        AccentColorRadioButtons.SelectedIndex = preference switch
        {
            AccentPreferenceZest => 0,
            AccentPreferenceDefault => 1,
            _ => -1
        };
    }

    private string FormatAccentSummary(string preference)
    {
        if (string.Equals(preference, AccentPreferenceZest, StringComparison.OrdinalIgnoreCase))
        {
            return "Zest";
        }

        if (string.Equals(preference, AccentPreferenceDefault, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(preference))
        {
            return "Use system setting";
        }

        if (preference.StartsWith('#'))
        {
            return string.Format(CultureInfo.CurrentCulture, "Custom ({0})", preference.ToUpperInvariant());
        }

        return preference;
    }

    private void OnCleanerRecyclePreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is ToggleSwitch toggle)
        {
            _cleanerSendToRecycleBin = toggle.IsOn;
            SaveSetting(
                CleanerRecyclePreferenceKey,
                _cleanerSendToRecycleBin.ToString(CultureInfo.InvariantCulture));
            UpdateCleanerDefaultsSummary();
            ApplyCleanerDefaultsToSession();
        }
    }

    private void OnCleanerDepthPreferenceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        var value = sender.Value;
        if (double.IsNaN(value))
        {
            value = 0;
        }

        var depth = (int)Math.Clamp(Math.Round(value), 0, 999);
        sender.Value = depth;
        _cleanerDepthLimit = depth;
        SaveSetting(CleanerDepthPreferenceKey, depth.ToString(CultureInfo.InvariantCulture));
        UpdateCleanerDefaultsSummary();
        ApplyCleanerDefaultsToSession();
    }

    private void OnCleanerExclusionsPreferenceChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (sender is TextBox textBox)
        {
            _cleanerExclusions = textBox.Text?.Trim() ?? string.Empty;
            SaveSetting(CleanerExclusionsPreferenceKey, _cleanerExclusions);
            ApplyCleanerDefaultsToSession();
        }
    }

    private void OnApplyCleanerDefaults(object sender, RoutedEventArgs e)
    {
        ApplyCleanerDefaultsToSession();
        ShowCleanerDefaultsInfo(
            Localize("SettingsCleanerDefaultsApplied", "Defaults applied to current session."),
            InfoBarSeverity.Success);
    }

    private void OnAutomationPreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (AutomationAutoPreviewToggle is not null && sender == AutomationAutoPreviewToggle)
        {
            _automationAutoPreview = AutomationAutoPreviewToggle.IsOn;
            SaveSetting(
                AutomationAutoPreviewKey,
                _automationAutoPreview.ToString(CultureInfo.InvariantCulture));
        }
        else if (AutomationReminderToggle is not null && sender == AutomationReminderToggle)
        {
            _automationWeeklyReminder = AutomationReminderToggle.IsOn;
            SaveSetting(
                AutomationReminderKey,
                _automationWeeklyReminder.ToString(CultureInfo.InvariantCulture));
        }

        UpdateAutomationSummary();
    }

    private void OnNotificationPreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        if (NotificationCompletionToggle is not null && sender == NotificationCompletionToggle)
        {
            _notificationShowCompletion = NotificationCompletionToggle.IsOn;
            SaveSetting(
                NotificationShowCompletionKey,
                _notificationShowCompletion.ToString(CultureInfo.InvariantCulture));
        }
        else if (NotificationDesktopToggle is not null && sender == NotificationDesktopToggle)
        {
            _notificationDesktopAlerts = NotificationDesktopToggle.IsOn;
            SaveSetting(
                NotificationDesktopAlertsKey,
                _notificationDesktopAlerts.ToString(CultureInfo.InvariantCulture));
        }

        UpdateNotificationSummary();
    }

    private void OnHistoryRetentionChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings)
        {
            return;
        }

        var value = sender.Value;
        if (double.IsNaN(value))
        {
            value = HistoryRetentionDefaultDays;
        }

        var days = (int)Math.Clamp(Math.Round(value), HistoryRetentionMinDays, HistoryRetentionMaxDays);
        sender.Value = days;
        _historyRetentionDays = days;
        SaveSetting(HistoryRetentionKey, days.ToString(CultureInfo.InvariantCulture));
        UpdateHistoryRetentionSummary();
    }

    private static ResourceLoader? TryCreateResourceLoader()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            return new ResourceLoader();
        }
        catch
        {
            return null;
        }
    }

    private static ApplicationDataContainer? TryGetLocalSettings()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            return ApplicationData.Current.LocalSettings;
        }
        catch
        {
            return null;
        }
    }

    private string? ReadSetting(string key)
    {
        var settings = _settings;
        if (settings is null)
        {
            return null;
        }

        try
        {
            if (settings.Values.TryGetValue(key, out var value))
            {
                return value switch
                {
                    string text => text,
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => value?.ToString()
                };
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private void SaveSetting(string key, string value)
    {
        var settings = _settings;
        if (settings is null)
        {
            return;
        }

        try
        {
            settings.Values[key] = value;
        }
        catch
        {
            // Ignore persistence failures on unsupported platforms.
        }
    }

    private Color GetZestAccentColor()
    {
        if (Application.Current.Resources.TryGetValue("Color.BrandPrimary", out var value) && value is Color color)
        {
            return color;
        }

        return Color.FromArgb(255, 0x00, 0x67, 0xC0);
    }

    private void CaptureDefaultAccentColors()
    {
        foreach (var key in AccentResourceKeys)
        {
            if (Application.Current.Resources.TryGetValue(key, out var value) && value is Color color)
            {
                _defaultAccentColors[key] = color;
            }
        }
    }

    private void RestoreAccentColors()
    {
        foreach (var key in AccentResourceKeys)
        {
            if (_defaultAccentColors.TryGetValue(key, out var color))
            {
                SetAccentResource(key, color);
            }
        }
    }

    private void ApplyAccentColor(Color color)
    {
        SetAccentResource("SystemAccentColor", color);
        SetAccentResource("SystemAccentColorLight1", Lighten(color, 0.3));
        SetAccentResource("SystemAccentColorLight2", Lighten(color, 0.5));
        SetAccentResource("SystemAccentColorLight3", Lighten(color, 0.7));
        SetAccentResource("SystemAccentColorDark1", Darken(color, 0.2));
        SetAccentResource("SystemAccentColorDark2", Darken(color, 0.35));
        SetAccentResource("SystemAccentColorDark3", Darken(color, 0.5));
    }

    private static void SetAccentResource(string key, Color color)
    {
        Application.Current.Resources[key] = color;
        var brushKey = key + "Brush";
        if (Application.Current.Resources.TryGetValue(brushKey, out var brushObj) && brushObj is SolidColorBrush brush)
        {
            brush.Color = color;
        }
    }

    private static Color Lighten(Color color, double amount) => Lerp(color, Colors.White, amount);

    private static Color Darken(Color color, double amount) => Lerp(color, Colors.Black, amount);

    private static Color Lerp(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (byte)(from.A + (to.A - from.A) * amount),
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan();
        if (span[0] == '#')
        {
            span = span[1..];
        }

        if (span.Length == 6)
        {
            if (uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                var r = (byte)((rgb >> 16) & 0xFF);
                var g = (byte)((rgb >> 8) & 0xFF);
                var b = (byte)(rgb & 0xFF);
                color = Color.FromArgb(255, r, g, b);
                return true;
            }
        }
        else if (span.Length == 8)
        {
            if (uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                var a = (byte)((argb >> 24) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var b = (byte)(argb & 0xFF);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
        }

        return false;
    }
}

