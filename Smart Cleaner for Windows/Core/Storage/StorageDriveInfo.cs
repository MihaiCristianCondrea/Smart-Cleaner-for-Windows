namespace Smart_Cleaner_for_Windows.Core.Storage;

/// <summary>
/// Represents the usage information for a single drive.
/// </summary>
public sealed record StorageDriveInfo(string Name, string? VolumeLabel, ulong TotalSize, ulong FreeSpace);
