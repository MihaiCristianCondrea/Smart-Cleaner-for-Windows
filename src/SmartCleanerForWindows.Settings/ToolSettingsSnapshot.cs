using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsSnapshot
{
    public required ToolSettingsDefinition Definition { get; init; }

    public required JsonObject Values { get; init; }

    public JsonNode? GetValue(string key) // FIXME: 				Method 'GetValue' is never used (0 issues)
    {
        return Values.TryGetPropertyValue(key, out var node) ? node : null;
    }
}
