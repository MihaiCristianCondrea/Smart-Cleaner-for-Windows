using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingOption // FIXME: Class 'ToolSettingOption' is never instantiated
    (string? value, string? displayName)
{
    [JsonPropertyName("value")]
    public string? Value { get; init; } = value; // FIXME: Auto-property accessor 'Value.get' is never used

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; } = displayName; // FIXME: Auto-property accessor 'DisplayName.get' is never used
}
