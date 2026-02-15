using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingField // FIXME: Class 'ToolSettingField' is never instantiated
{
    [JsonPropertyName("key")]
    public required string Key { get; init; } // FIXME: Auto-property accessor 'Key.init' is never used

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; } // FIXME: Auto-property accessor 'DisplayName.init' is never used

    [JsonPropertyName("description")]
    public string? Description { get; init; } // FIXME: Auto-property accessor 'Description.init' is never used

    [JsonPropertyName("type")]
    public ToolSettingFieldType FieldType { get; init; } // FIXME: Auto-property accessor 'FieldType.init' is never used

    [JsonPropertyName("defaultValue")]
    public JsonNode? DefaultValue { get; init; } // FIXME: Auto-property accessor 'DefaultValue.init' is never used

    [JsonPropertyName("minimum")]
    public double? Minimum { get; init; } // FIXME: Auto-property accessor 'Minimum.init' is never used

    [JsonPropertyName("maximum")]
    public double? Maximum { get; init; } // FIXME: Auto-property accessor 'Maximum.init' is never used

    [JsonPropertyName("step")]
    public double? Step { get; init; } // FIXME: Auto-property accessor 'Step.init' is never used

    [JsonPropertyName("options")]
    public IReadOnlyList<ToolSettingOption>? Options { get; init; } // FIXME: 
}
