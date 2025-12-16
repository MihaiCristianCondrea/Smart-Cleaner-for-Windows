using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

/// <summary>
/// Ensures tool settings have definitions available and that user settings files
/// exist on first launch. Mirrors the defensive loading approach used by
/// PowerToys so missing packaged files do not cause startup crashes.
/// </summary>
public static class ToolSettingsBootstrapper
{
    public static (string DefinitionRoot, string UserRoot) Initialize()
    {
        var definitionRoot = ToolSettingsPaths.GetDefinitionRoot();
        var userRoot = ToolSettingsPaths.GetUserSettingsRoot();
        Directory.CreateDirectory(userRoot);

        var catalog = new ToolSettingsCatalog(definitionRoot);
        var definitions = catalog.LoadDefinitions();
        if (definitions.Count == 0)
        {
            Trace.TraceWarning(
                "No tool settings definitions were found under '{0}'. Navigation will be empty until packaged definitions are restored.",
                definitionRoot);
            return (definitionRoot, userRoot);
        }

        foreach (var definition in definitions)
        {
            var userSettingsPath = Path.Combine(userRoot, $"{definition.Id}.json");
            EnsureUserSettingsFile(userSettingsPath, definition);
        }

        return (definitionRoot, userRoot);
    }

    private static void EnsureUserSettingsFile(string path, ToolSettingsDefinition definition)
    {
        if (File.Exists(path))
        {
            return;
        }

        try
        {
            var defaults = definition.CreateDefaultValues();
            WriteJson(path, defaults);
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                "Failed to create default settings for tool '{0}' at '{1}'. {2}",
                definition.Id,
                path,
                ex.Message);
        }
    }

    private static void WriteJson(string path, JsonObject payload)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        payload.WriteTo(writer);
    }
}
