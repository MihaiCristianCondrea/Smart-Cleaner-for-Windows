using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingField
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("type")]
    public ToolSettingFieldType FieldType { get; init; }

    [JsonPropertyName("defaultValue")]
    public JsonNode? DefaultValue { get; init; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; init; }

    [JsonPropertyName("step")]
    public double? Step { get; init; }

    [JsonPropertyName("options")]
    public IReadOnlyList<ToolSettingOption>? Options { get; init; }
}
