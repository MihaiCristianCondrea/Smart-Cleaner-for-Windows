using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingOption // FIXME: 				Class 'ToolSettingOption' is never instantiated (0 issues) 				Property 'Value' is never used (0 issues) 				Property 'DisplayName' is never used (0 issues)
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
