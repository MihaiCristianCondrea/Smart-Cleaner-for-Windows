using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsSnapshot
{
    public required ToolSettingsDefinition Definition { get; init; }

    public required JsonObject Values { get; init; }
}
