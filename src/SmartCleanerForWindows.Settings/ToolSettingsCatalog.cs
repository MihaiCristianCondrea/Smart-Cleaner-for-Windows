using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _definitionRoot;

    public ToolSettingsCatalog(string? definitionRoot = null)
    {
        _definitionRoot = definitionRoot ?? ToolSettingsPaths.GetDefinitionRoot();
    }

    public IReadOnlyList<ToolSettingsDefinition> LoadDefinitions()
    {
        if (!Directory.Exists(_definitionRoot))
        {
            return Array.Empty<ToolSettingsDefinition>();
        }

        var definitions = new List<ToolSettingsDefinition>();
        foreach (var file in Directory.EnumerateFiles(_definitionRoot, "settings.json", SearchOption.AllDirectories))
        {
            try
            {
                using var stream = File.OpenRead(file);
                var definition = JsonSerializer.Deserialize<ToolSettingsDefinition>(stream, SerializerOptions);
                if (definition is null)
                {
                    continue;
                }

                definitions.Add(definition with { DefinitionPath = file });
            }
            catch
            {
                // Ignore invalid definition files.
            }
        }

        return definitions
            .OrderBy(def => def.Order)
            .ThenBy(def => def.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
