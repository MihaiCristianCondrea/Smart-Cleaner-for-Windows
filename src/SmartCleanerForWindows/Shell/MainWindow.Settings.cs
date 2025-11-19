using System;
using System.Globalization;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;

namespace SmartCleanerForWindows.Shell;

public sealed partial class MainWindow
{
    private static readonly Color WhiteColor = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color BlackColor = Color.FromArgb(255, 0, 0, 0);

    private static bool ParseBoolSetting(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out var result)) return result;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)) return numeric != 0;
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

        _notificationShowCompletion = ParseBoolSetting(ReadSetting(NotificationShowCompletionKey), defaultValue: true);
        _notificationDesktopAlerts = ParseBoolSetting(ReadSetting(NotificationDesktopAlertsKey), defaultValue: false);
        _historyRetentionDays = ParseIntSetting(
            ReadSetting(HistoryRetentionKey),
            HistoryRetentionDefaultDays,
            min: HistoryRetentionMinDays,
            max: HistoryRetentionMaxDays);

        UpdateCleanerSettingsView();
        UpdateAutomationSettingsView();

        if (SettingsView.NotificationCompletionToggle is not null)
        {
            SettingsView.NotificationCompletionToggle.IsOn = _notificationShowCompletion;
        }

        if (SettingsView.NotificationDesktopToggle is not null)
        {
            SettingsView.NotificationDesktopToggle.IsOn = _notificationDesktopAlerts;
        }

        if (SettingsView.HistoryRetentionNumberBox is not null)
        {
            SettingsView.HistoryRetentionNumberBox.Value = _historyRetentionDays;
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
        if (_isInitializingSettings) return;
        if (sender is not RadioButtons radioButtons) return;

        if (radioButtons.SelectedItem is RadioButton { Tag: string tag })
        {
            ApplyThemePreference(tag, save: true);
        }
    }

    private void OnAccentPreferenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingSettings) return;
        if (sender is not RadioButtons radioButtons) return;

        if (radioButtons.SelectedItem is RadioButton { Tag: string tag })
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

        if (SettingsView.ThemeSummaryText is not null)
        {
            SettingsView.ThemeSummaryText.Text = normalized switch
            {
                ThemePreferenceLight => "Light",
                ThemePreferenceDark => "Dark",
                _ => "Use system setting"
            };
        }

        if (save)
        {
            SaveSetting(ThemePreferenceKey, normalized);
        }
    }

    private static string NormalizeThemePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference)) return ThemePreferenceDefault;

        return preference.Trim().ToLowerInvariant() switch
        {
            ThemePreferenceLight => ThemePreferenceLight,
            ThemePreferenceDark => ThemePreferenceDark,
            _ => ThemePreferenceDefault
        };
    }

    private void SelectThemeOption(string preference)
    {
        if (SettingsView.ThemeRadioButtons is null) return;

        SettingsView.ThemeRadioButtons.SelectedIndex = preference switch
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

        if (SettingsView.AccentSummaryText is not null)
        {
            SettingsView.AccentSummaryText.Text = FormatAccentSummary(_accentPreference);
        }

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
            AccentPreferenceDefault or "system" => AccentPreferenceDefault,
            _ => trimmed
        };
    }

    private void SelectAccentOption(string preference)
    {
        if (SettingsView.AccentColorRadioButtons is null) return;

        SettingsView.AccentColorRadioButtons.SelectedIndex = preference switch
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

        return preference.StartsWith('#') ? string.Format(CultureInfo.CurrentCulture, "Custom ({0})", preference.ToUpperInvariant()) : preference;
    }

    private void OnCleanerRecyclePreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings) return;

        if (sender is not ToggleSwitch toggle) return;
        _cleanerSendToRecycleBin = toggle.IsOn;
        PersistEmptyFolderSettings();
        UpdateCleanerDefaultsSummary();
        ApplyCleanerDefaultsToSession();
    }

    private void OnCleanerDepthPreferenceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings) return;

        var value = sender.Value;
        if (double.IsNaN(value))
        {
            value = 0;
        }

        var depth = (int)Math.Clamp(Math.Round(value), 0, 999);
        sender.Value = depth;
        _cleanerDepthLimit = depth;
        PersistEmptyFolderSettings();
        UpdateCleanerDefaultsSummary();
        ApplyCleanerDefaultsToSession();
    }

    private void OnCleanerExclusionsPreferenceChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializingSettings) return;

        if (sender is TextBox textBox)
        {
            _cleanerExclusions = textBox.Text?.Trim() ?? string.Empty;
            PersistEmptyFolderSettings();
            UpdateCleanerDefaultsSummary();
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
        if (_isInitializingSettings) return;
        if (sender is not ToggleSwitch source) return;

        if (SettingsView.AutomationAutoPreviewToggle is not null && ReferenceEquals(source, SettingsView.AutomationAutoPreviewToggle))
        {
            _automationAutoPreview = SettingsView.AutomationAutoPreviewToggle.IsOn;
            PersistEmptyFolderSettings();
        }
        else if (SettingsView.AutomationReminderToggle is not null && ReferenceEquals(source, SettingsView.AutomationReminderToggle))
        {
            _automationWeeklyReminder = SettingsView.AutomationReminderToggle.IsOn;
            PersistDashboardSettings();
        }

        UpdateAutomationSummary();
    }

    private void OnNotificationPreferenceToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializingSettings) return;
        if (sender is not ToggleSwitch source) return;

        if (SettingsView.NotificationCompletionToggle is not null && ReferenceEquals(source, SettingsView.NotificationCompletionToggle))
        {
            _notificationShowCompletion = SettingsView.NotificationCompletionToggle.IsOn;
            SaveSetting(
                NotificationShowCompletionKey,
                _notificationShowCompletion.ToString(CultureInfo.InvariantCulture));
        }
        else if (SettingsView.NotificationDesktopToggle is not null && ReferenceEquals(source, SettingsView.NotificationDesktopToggle))
        {
            _notificationDesktopAlerts = SettingsView.NotificationDesktopToggle.IsOn;
            SaveSetting(
                NotificationDesktopAlertsKey,
                _notificationDesktopAlerts.ToString(CultureInfo.InvariantCulture));
        }

        UpdateNotificationSummary();
    }

    private void OnHistoryRetentionChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isInitializingSettings) return;

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
        if (!OperatingSystem.IsWindows()) return null;

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
        if (!OperatingSystem.IsWindows()) return null;

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
        if (settings is null) return null;

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
        if (settings is null) return;

        try
        {
            settings.Values[key] = value;
        }
        catch
        {
            // Ignore persistence failures on unsupported platforms.
        }
    }

    private void UpdateCleanerSettingsView()
    {
        var previous = _isInitializingSettings;
        _isInitializingSettings = true;

        try
        {
            if (SettingsView.CleanerRecycleToggle is not null)
            {
                SettingsView.CleanerRecycleToggle.IsOn = _cleanerSendToRecycleBin;
            }

            if (SettingsView.CleanerDepthPreferenceBox is not null)
            {
                SettingsView.CleanerDepthPreferenceBox.Value = _cleanerDepthLimit;
            }

            if (SettingsView.CleanerExclusionsPreferenceBox is not null)
            {
                SettingsView.CleanerExclusionsPreferenceBox.Text = _cleanerExclusions;
            }
        }
        finally
        {
            _isInitializingSettings = previous;
        }
    }

    private void UpdateAutomationSettingsView()
    {
        var previous = _isInitializingSettings;
        _isInitializingSettings = true;

        try
        {
            if (SettingsView.AutomationAutoPreviewToggle is not null)
            {
                SettingsView.AutomationAutoPreviewToggle.IsOn = _automationAutoPreview;
            }

            if (SettingsView.AutomationReminderToggle is not null)
            {
                SettingsView.AutomationReminderToggle.IsOn = _automationWeeklyReminder;
            }
        }
        finally
        {
            _isInitializingSettings = previous;
        }
    }

    private void PersistEmptyFolderSettings()
    {
        if (!_settingsSnapshots.TryGetValue(EmptyFoldersToolId, out var snapshot))
        {
            return;
        }

        snapshot.Values["sendToRecycleBin"] = _cleanerSendToRecycleBin;
        snapshot.Values["depthLimit"] = _cleanerDepthLimit;
        snapshot.Values["exclusions"] = _cleanerExclusions;
        snapshot.Values["previewAutomatically"] = _automationAutoPreview;
        _ = _toolSettingsService.UpdateAsync(EmptyFoldersToolId, snapshot.Values);
    }

    private void PersistDashboardSettings()
    {
        if (!_settingsSnapshots.TryGetValue(DashboardToolId, out var snapshot))
        {
            return;
        }

        snapshot.Values["remindWeekly"] = _automationWeeklyReminder;
        _ = _toolSettingsService.UpdateAsync(DashboardToolId, snapshot.Values);
    }

    private static Color GetZestAccentColor()
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

    private static void ApplyAccentColor(Color color)
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

    private static Color Lighten(Color color, double amount) => Lerp(color, WhiteColor, amount);

    private static Color Darken(Color color, double amount) => Lerp(color, BlackColor, amount);

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
        if (span[0] == '#') span = span[1..];

        switch (span.Length)
        {
            case 6 when uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb):
            {
                var r = (byte)((rgb >> 16) & 0xFF);
                var g = (byte)((rgb >> 8) & 0xFF);
                var b = (byte)(rgb & 0xFF);
                color = Color.FromArgb(255, r, g, b);
                return true;
            }
            case 8 when uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb):
            {
                var a = (byte)((argb >> 24) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var b = (byte)(argb & 0xFF);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
            default:
                return false;
        }
    }
}

