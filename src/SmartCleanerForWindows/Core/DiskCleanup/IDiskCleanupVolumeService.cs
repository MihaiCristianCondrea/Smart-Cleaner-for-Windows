namespace Smart_Cleaner_for_Windows.Core.DiskCleanup;

public interface IDiskCleanupVolumeService
{
    string GetDefaultVolume();

    string NormalizeVolume(string? volume);
}
