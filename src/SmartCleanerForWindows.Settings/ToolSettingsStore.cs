using System.Text.Json;
using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsStore
{
    private readonly string _userRoot;

    public ToolSettingsStore(string? userRoot = null)
    {
        _userRoot = userRoot ?? ToolSettingsPaths.GetUserSettingsRoot();
        Directory.CreateDirectory(_userRoot);
    }

    public JsonObject LoadValues(ToolSettingsDefinition definition)
    {
        var path = GetUserSettingsPath(definition.Id);
        if (!File.Exists(path))
        {
            var defaults = definition.CreateDefaultValues();
            SaveValues(definition.Id, defaults);
            return defaults;
        }

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var node = JsonNode.Parse(stream) as JsonObject;
            if (node is null)
            {
                var fallback = definition.CreateDefaultValues();
                SaveValues(definition.Id, fallback);
                return fallback;
            }

            MergeDefaults(node, definition);
            return node;
        }
        catch
        {
            var fallback = definition.CreateDefaultValues();
            SaveValues(definition.Id, fallback);
            return fallback;
        }
    }

    public void SaveValues(string toolId, JsonObject values)
    {
        var path = GetUserSettingsPath(toolId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        values.WriteTo(writer);
    }

    public FileSystemWatcher CreateWatcher()
    {
        var watcher = new FileSystemWatcher(_userRoot)
        {
            Filter = "*.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };
        return watcher;
    }

    public string GetUserSettingsPath(string toolId)
    {
        return Path.Combine(_userRoot, $"{toolId}.json");
    }

    private static void MergeDefaults(JsonObject target, ToolSettingsDefinition definition)
    {
        var defaults = definition.CreateDefaultValues();
        foreach (var pair in defaults)
        {
            if (!target.ContainsKey(pair.Key) && pair.Value is not null)
            {
                target[pair.Key] = pair.Value.DeepClone();
            }
        }
    }
}
