using System;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.UI;

namespace SmartCleanerForWindows.Shell;

internal sealed class SettingsCoordinator
{

    public void LoadPreferences(
        ISettingsWorkflowView view,
        Func<string, string?> readSetting,
        string themePreferenceKey,
        string accentPreferenceKey,
        string notificationShowCompletionKey,
        string notificationDesktopAlertsKey,
        string historyRetentionKey,
        int historyRetentionDefaultDays,
        int historyRetentionMinDays,
        int historyRetentionMaxDays,
        out bool notificationShowCompletion,
        out bool notificationDesktopAlerts,
        out int historyRetentionDays)
    {
        var savedTheme = readSetting(themePreferenceKey);
        view.ApplyThemePreferenceFromCoordinator(savedTheme, save: false);

        var savedAccent = readSetting(accentPreferenceKey);
        view.ApplyAccentPreferenceFromCoordinator(savedAccent, save: false);

        notificationShowCompletion = ParseBool(readSetting(notificationShowCompletionKey), defaultValue: true);
        notificationDesktopAlerts = ParseBool(readSetting(notificationDesktopAlertsKey), defaultValue: false);
        historyRetentionDays = ParseInt(readSetting(historyRetentionKey), historyRetentionDefaultDays, historyRetentionMinDays, historyRetentionMaxDays);

        view.SelectThemeOptionFromCoordinator(savedTheme ?? string.Empty);
        view.SelectAccentOptionFromCoordinator(savedAccent ?? string.Empty);
        view.UpdateCleanerSettingsViewFromCoordinator();
        view.UpdateAutomationSettingsViewFromCoordinator();
        view.SetNotificationValuesFromCoordinator(notificationShowCompletion, notificationDesktopAlerts, historyRetentionDays);
        view.UpdateSettingsSummariesFromCoordinator();
        view.ApplyCleanerDefaultsToSessionFromCoordinator();
    }
    public bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out var result)) return result;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)) return numeric != 0;
        return defaultValue;
    }

    public int ParseInt(string? value, int defaultValue, int min, int max)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            defaultValue = parsed;
        }

        return Math.Clamp(defaultValue, min, max);
    }

    public bool GetBooleanValue(JsonObject values, string key, bool fallback)
    {
        if (!values.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        return node switch
        {
            JsonValue value when value.TryGetValue<bool>(out var boolean) => boolean,
            JsonValue value when value.TryGetValue<int>(out var number) => number != 0,
            JsonValue value when value.TryGetValue<string>(out var text) && bool.TryParse(text, out var parsed) => parsed,
            _ => fallback
        };
    }

    public int GetIntegerValue(JsonObject values, string key, int fallback, int min, int max)
    {
        if (!values.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        var parsed = node switch
        {
            JsonValue value when value.TryGetValue<int>(out var integer) => integer,
            JsonValue value when value.TryGetValue<double>(out var number) => (int)Math.Round(number, MidpointRounding.AwayFromZero),
            JsonValue value when value.TryGetValue<string>(out var text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => integer,
            _ => fallback
        };

        return Math.Clamp(parsed, min, max);
    }

    public string GetStringValue(JsonObject values, string key, string fallback)
    {
        if (!values.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            _ => node.ToString()
        };
    }

    public bool TryParseColor(string? value, out Color color)
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
