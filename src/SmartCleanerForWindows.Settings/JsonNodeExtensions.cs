using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

internal static class JsonNodeExtensions // FIXME: 				Class 'JsonNodeExtensions' is never used (0 issues)
{
    public static JsonNode DeepClone(this JsonNode node) // FIXME: 				Method 'DeepClone' is never used (0 issues)
    {
        return JsonNode.Parse(node.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        }))!;
    }
}
