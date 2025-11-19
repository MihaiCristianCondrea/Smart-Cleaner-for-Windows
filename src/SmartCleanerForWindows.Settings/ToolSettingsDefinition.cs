using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

// Change 'class' to 'record'
public sealed record ToolSettingsDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("viewKey")]
    public string? ViewKey { get; init; }

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("defaults")]
    public JsonObject? Defaults { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyList<ToolSettingField> Fields { get; init; } = [];

    [JsonIgnore]
    public string? DefinitionPath { get; init; }

    public JsonObject CreateDefaultValues()
    {
        var root = new JsonObject();

        if (Defaults is not null)
        {
            foreach (var pair in Defaults)
            {
                if (pair.Value is null) continue;
                root[pair.Key] = pair.Value.DeepClone();
            }
        }

        foreach (var field in Fields)
        {
            if (field.DefaultValue is null || root.ContainsKey(field.Key)) continue;
            root[field.Key] = field.DefaultValue.DeepClone();
        }

        return root;
    }
}