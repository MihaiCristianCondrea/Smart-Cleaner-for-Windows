namespace SmartCleanerForWindows.Core.DiskCleanup;

public interface IDiskCleanupVolumeService
{
    string GetDefaultVolume();

    string NormalizeVolume(string? volume);
}
