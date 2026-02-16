using System;
using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

internal static class JsonNodeExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions DeepCloneSerializerOptions = new()
    {
        WriteIndented = false
    };

    public static JsonNode DeepClone(this JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return JsonNode.Parse(node.ToJsonString(DeepCloneSerializerOptions))!;
    }
}
