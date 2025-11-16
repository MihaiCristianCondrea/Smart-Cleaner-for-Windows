namespace SmartCleanerForWindows.Core.Storage;

/// <summary>
/// Represents the usage information for a single drive.
/// </summary>
public sealed record StorageDriveInfo(string Name, string? VolumeLabel, ulong TotalSize, ulong FreeSpace)
{
    public StorageDriveInfo()
        : this(string.Empty, null, 0, 0)
    {
    }
}
