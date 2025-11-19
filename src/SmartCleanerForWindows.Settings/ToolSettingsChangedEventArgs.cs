namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsChangedEventArgs(ToolSettingsSnapshot snapshot) : EventArgs
{
    public ToolSettingsSnapshot Snapshot { get; } = snapshot;
}
