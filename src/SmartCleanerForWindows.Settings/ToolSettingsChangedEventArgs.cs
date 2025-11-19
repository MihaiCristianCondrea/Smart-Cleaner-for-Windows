namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsChangedEventArgs : EventArgs
{
    public ToolSettingsChangedEventArgs(ToolSettingsSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public ToolSettingsSnapshot Snapshot { get; }
}
