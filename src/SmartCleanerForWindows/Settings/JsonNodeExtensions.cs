using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

internal static class JsonNodeExtensions
{
    public static JsonNode DeepClone(this JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        }))!;
    }
}
