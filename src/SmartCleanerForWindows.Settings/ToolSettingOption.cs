using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingOption
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
