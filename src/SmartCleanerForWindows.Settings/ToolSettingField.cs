using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingField // FIXME: 				Class 'ToolSettingField' is never instantiated (0 issues) 				Auto-property accessor 'Key.init' is never used (0 issues) Class 'ToolSettingField' is never instantiated 
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; } // FIXME: 				Property 'DisplayName' is never used (0 issues)

    [JsonPropertyName("description")]
    public string? Description { get; init; } // FIXME: 				Property 'Description' is never used (0 issues)

    [JsonPropertyName("type")]
    public ToolSettingFieldType FieldType { get; init; } // FIXME: 				Property 'FieldType' is never used (0 issues)

    [JsonPropertyName("defaultValue")]
    public JsonNode? DefaultValue { get; init; } // FIXME: 				Auto-property accessor 'DefaultValue.init' is never used (0 issues)

    [JsonPropertyName("minimum")]
    public double? Minimum { get; init; } // FIXME: 				Property 'Minimum' is never used (0 issues)

    [JsonPropertyName("maximum")]
    public double? Maximum { get; init; } // FIXME: 				Property 'Maximum' is never used (0 issues)

    [JsonPropertyName("step")]
    public double? Step { get; init; } // FIXME: 				Property 'Step' is never used (0 issues)

    [JsonPropertyName("options")]
    public IReadOnlyList<ToolSettingOption>? Options { get; init; } // FIXME: 				Property 'Options' is never used (0 issues)
}
